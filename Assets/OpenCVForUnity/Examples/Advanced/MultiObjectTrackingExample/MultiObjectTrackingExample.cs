#if !UNITY_WSA_10_0

using System;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.MOT;
using OpenCVForUnity.Extensions.MOT.ByteTrack;
using OpenCVForUnity.Extensions.Runner;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.Extensions.Worker.DataStruct;
using OpenCVForUnity.Extensions.Worker.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using OpenCVForUnity.UnityIntegration.Worker.DnnModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenCVDebug = OpenCVForUnity.Extensions.OpenCVDebug;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Multi Object Tracking (MOT) Example
    /// Detects objects with YOLOX and assigns persistent track IDs using ByteTrack.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - YOLOX object detection via OpenCV DNN or Unity Sentis (toggle in UI)
    /// - Async inference pipeline with InferenceRunner
    /// - ByteTrack multi-object tracking on detection bounding boxes
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Size"/>, <see cref="Scalar"/>, <see cref="Point"/>
    /// - <see cref="Imgproc"/>: cvtColor
    /// - <see cref="YOLOXObjectDetectorMultiBackend"/>, <see cref="BYTETracker"/>, <see cref="MatSingleFlightSyncAsyncRunner"/>
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://github.com/ifzhang/ByteTrack
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class MultiObjectTrackingExample : MonoBehaviour
    {
        // Public Fields
        [Header("Output")]
        [Tooltip("The RawImage for previewing the result.")]
        public RawImage ResultPreview;

        [Header("UI")]
        [Tooltip("Inference framework selector. Options are built at runtime from InferenceFrameworkUtils.GetSelectionValuesInEnumOrder(). Assign OnInferenceFrameworkDropdownValueChanged to On Value Changed (int).")]
        public Dropdown InferenceFrameworkDropdown;

        [Tooltip("Sentis inference target selector (GPU Compute / GPU Pixel / CPU). Options are built at runtime from SentisInferenceUtils.GetTargetValuesInEnumOrder(). Assign OnSentisInferenceTargetDropdownValueChanged to On Value Changed (int). Value changes reinitialize inference.")]
        public Dropdown SentisInferenceTargetDropdown;

        [Tooltip("Selected inference framework (OpenCV DNN or Unity Sentis). When Unity Sentis is selected, Inspector model paths may stay .onnx; at runtime they are rewritten to .sentis and loaded from StreamingAssets (place a matching .sentis beside the onnx file).")]
        public InferenceFrameworkSelectionKind InferenceFramework = InferenceFrameworkSelectionKind.UnitySentis;

        [Tooltip("When using Sentis: selects the Sentis inference target (GPU Compute / GPU Pixel / CPU).")]
        public SentisInferenceTargetKind SentisInferenceTarget = SentisInferenceTargetKind.GPUCompute;

        [Tooltip("When enabled, submits inference work asynchronously. Assign OnUseAsyncInferenceToggleValueChanged to On Value Changed.")]
        public Toggle UseAsyncInferenceToggle;

        public bool UseAsyncInference = true;

        public Toggle ShowObjectDetectorResultToggle;
        public bool ShowObjectDetectorResult;
        public Toggle EnableByteTrackToggle;
        public bool EnableByteTrack;

        [Header("Model Settings")]
        [Tooltip("Path to a binary file of model contains trained weights.")]
        public string Model = "OpenCVForUnityExamples/dnn/yolox_tiny.onnx";

        [Tooltip("Optional path to a text file with names of classes to label detected objects.")]
        public string Classes = "OpenCVForUnityExamples/dnn/coco.names";

        [Tooltip("Confidence threshold.")]
        public float ConfThreshold = 0.25f;

        [Tooltip("Non-maximum suppression threshold.")]
        public float NmsThreshold = 0.45f;

        [Tooltip("Maximum detections per image.")]
        public int TopK = 300;

        [Tooltip("Preprocess input image by resizing to a specific width.")]
        public int InpWidth = 416;

        [Tooltip("Preprocess input image by resizing to a specific height.")]
        public int InpHeight = 416;

        // Private Fields
        private YOLOXObjectDetectorMultiBackend _objectDetector;
        private string _modelFilepathSentis;
        private MatSingleFlightSyncAsyncRunner _inferenceRunner;
        private bool _inferenceReinitializing;
        private BYTETracker _byteTracker;
        private BYTETrackInfoVisualizer _byteTrackInfoVisualizer;
        private bool _disableObjectDetector = false;
        private string _classesFilepath;
        private string _modelFilepathOnnx;
        private Texture2D _texture;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private Mat _bgrMat;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            WireSourceToMatControlPanelHooks();

            // Update GUI state
            ShowObjectDetectorResultToggle.isOn = ShowObjectDetectorResult;
            EnableByteTrackToggle.isOn = EnableByteTrack;
            UpdateUseAsyncInference();
            SyncInferenceModeUi(inferenceReinitializing: false);

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            if (!string.IsNullOrEmpty(Classes))
            {
                _classesFilepath = await OpenCVForUnityEnv.GetFilePathAsync(Classes, cancellationToken: _cts.Token);
                if (string.IsNullOrEmpty(_classesFilepath))
                {
                    Debug.LogError("classes: " + Classes + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);
                    if (_fpsMonitor != null)
                    {
                        _fpsMonitor.ConsoleText = "classes file is not loaded.\nPlease read console message.";
                    }
                }
            }
            if (!string.IsNullOrEmpty(Model))
            {
                _modelFilepathOnnx = await OpenCVForUnityEnv.GetFilePathAsync(Model, cancellationToken: _cts.Token);
                if (OpenCVForUnityEnv.IsSentisIntegrationAvailable)
                {
                    string sentisModelFileName = MultiBackendDnn.ResolveSentisModelPathFromOnnxPath(Model);
                    _modelFilepathSentis = await OpenCVForUnityEnv.GetFilePathAsync(
                        sentisModelFileName,
                        cancellationToken: _cts.Token);
                }
            }

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            CheckFilePaths();

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            if (!TryInitializeInference())
            {
                return;
            }

            _byteTrackInfoVisualizer = new BYTETrackInfoVisualizer(_classesFilepath);

            _multiSourceToMatHelper.Initialize();
        }

        private async void OnDestroy()
        {
            UnwireSourceToMatControlPanelHooks();

            _cts?.Cancel();

            await DisposeInferenceAsync();

            _byteTracker?.Dispose();
            _byteTracker = null;
            _byteTrackInfoVisualizer?.Dispose();
            _byteTrackInfoVisualizer = null;

            OpenCVDebug.SetDebugMode(false);

            _cts?.Dispose();
            _cts = null;
        }

        // Public Methods
        /// <summary>
        /// Raises the helper frame mat updated event.
        /// Updates the preview texture when a new frame is available during playback.
        /// </summary>
        public void OnSourceToMatHelperFrameMatUpdated()
        {
            if (_inferenceReinitializing)
            {
                return;
            }

            if (!_multiSourceToMatHelper.IsPlaying)
            {
                return;
            }

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;

            if (_objectDetector != null && !_disableObjectDetector && _inferenceRunner != null)
            {
                // YOLOX expects BGR input; helper provides RGBA.
                Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

                // SubmitWork queues sync or async detection; TryGetLatestResult reads the newest output.
                _inferenceRunner.SubmitWork(
                    _bgrMat,
                    syncWork: m => _objectDetector.Detect(m, useCopyOutput: true),
                    asyncWork: async m =>
                    {
                        CancellationToken ct = _inferenceRunner.InFlightAsyncWorkCancellationToken;
                        return await _objectDetector.DetectAsync(m, ct);
                    });

                if (_inferenceRunner.TryGetLatestResult(out Mat results))
                {
                    if (ShowObjectDetectorResult)
                    {
                        _objectDetector.Visualize(rgbaMat, results, false, true);
                    }

                    if (EnableByteTrack && _byteTrackInfoVisualizer != null)
                    {
                        // ByteTrack assigns stable IDs across frames from detection boxes.
                        BBox[] inputs = ConvertToBBoxes(results);
                        _byteTracker.Update(inputs);
                        BYTETrackInfo[] outputs = _byteTracker.GetActiveTrackInfos();
                        _byteTrackInfoVisualizer.Visualize(rgbaMat, outputs, false, true);
                    }
                }
            }

            OpenCVMatUnityUtils.MatToTexture2D(rgbaMat, _texture);
        }

        /// <summary>
        /// Raises the helper initialized event.
        /// Recreates the preview texture and starts playback on first initialization.
        /// Skips Play when re-initialization has already restored Playing or Paused.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized", this);

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;
            int fps = GetSourceFps();

            RecreatePreviewTexture();
            CreateOrRecreateProcessingResources(rgbaMat, fps);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Clear();
                UpdateFpsMonitorPlaybackState();
                _fpsMonitor.Add("HelperKind", _multiSourceToMatHelper.RequestedHelperKind.ToString());
                _fpsMonitor.Add("Width", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("Height", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("Rotate90Degree", _multiSourceToMatHelper.Rotate90Degree.ToString());
                _fpsMonitor.Add("FlipVertical", _multiSourceToMatHelper.FlipVertical.ToString());
                _fpsMonitor.Add("FlipHorizontal", _multiSourceToMatHelper.FlipHorizontal.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
                _fpsMonitor.Add("source fps", fps.ToString());
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _objectDetector, UseAsyncInference, InferenceFramework);
            }

            if (!_multiSourceToMatHelper.IsPlaying && !_multiSourceToMatHelper.IsPaused)
            {
                _multiSourceToMatHelper.Play();
                UpdateFpsMonitorPlaybackState();
            }
        }

        /// <summary>
        /// Raises the helper frame mat layout changed event.
        /// Recreates the preview texture when rotation or output size changes.
        /// </summary>
        public void OnSourceToMatHelperFrameMatLayoutChanged()
        {
            Debug.Log("OnSourceToMatHelperFrameMatLayoutChanged", this);

            RecreatePreviewTexture();
            CreateOrRecreateProcessingResources(_multiSourceToMatHelper.FrameMat, GetSourceFps());

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("Width", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("Height", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
            }
        }

        /// <summary>
        /// Raises the helper released event.
        /// </summary>
        public void OnSourceToMatHelperReleased()
        {
            Debug.Log("OnSourceToMatHelperReleased", this);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Clear();
            }

            DisposeFrameProcessingResources();
            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed", this);

            _objectDetector?.Cancel();

            _inferenceRunner?.Cancel();

            DisposeFrameProcessingResources();
            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(SourceToMatErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message, this);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        /// <summary>
        /// Raises the back button click event.
        /// Stops playback and disposes the helper before scene transition (required on WebGL).
        /// </summary>
        public async void OnBackButtonClick()
        {
            if (_multiSourceToMatHelper.IsPlaying || _multiSourceToMatHelper.IsPaused)
            {
                await _multiSourceToMatHelper.StopAsync();
            }

            await _multiSourceToMatHelper.DisposeAsync();

            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterPlay"/>.
        /// </summary>
        public void OnControlPanelAfterPlay()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterPause"/>.
        /// </summary>
        public void OnControlPanelAfterPause()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterStop"/>.
        /// </summary>
        public void OnControlPanelAfterStop()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnRotate90Changed"/>.
        /// </summary>
        /// <param name="isOn">New Rotate90Degree value applied by the panel.</param>
        public void OnControlPanelRotate90Changed(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("Rotate90Degree", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnFlipVerticalChanged"/>.
        /// </summary>
        /// <param name="isOn">New FlipVertical value applied by the panel.</param>
        public void OnControlPanelFlipVerticalChanged(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("FlipVertical", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnFlipHorizontalChanged"/>.
        /// </summary>
        /// <param name="isOn">New FlipHorizontal value applied by the panel.</param>
        public void OnControlPanelFlipHorizontalChanged(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("FlipHorizontal", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnHelperKindChanged"/>.
        /// </summary>
        /// <param name="kindIndex">Dropdown index matching <see cref="MultiSourceHelperKind"/>.</param>
        public void OnControlPanelHelperKindChanged(int kindIndex)
        {
            if (_fpsMonitor == null || !Enum.IsDefined(typeof(MultiSourceHelperKind), kindIndex))
            {
                return;
            }

            _fpsMonitor.Add("HelperKind", ((MultiSourceHelperKind)kindIndex).ToString());
        }

        /// <summary>
        /// Raises the reset trackers button click event.
        /// </summary>
        public void OnResetTrackersButtonClick()
        {
            ResetTrackers();
        }

        /// <summary>
        /// Raises the show object detector result toggle value changed event.
        /// </summary>
        public void OnShowObjectDetectorResultToggleValueChanged()
        {
            if (ShowObjectDetectorResultToggle.isOn != ShowObjectDetectorResult)
            {
                ShowObjectDetectorResult = ShowObjectDetectorResultToggle.isOn;
            }
        }

        /// <summary>
        /// Raises the enable byte track toggle value changed event.
        /// </summary>
        public void OnEnableByteTrackToggleValueChanged()
        {
            if (EnableByteTrackToggle.isOn != EnableByteTrack)
            {
                EnableByteTrack = EnableByteTrackToggle.isOn;
            }
        }

        /// <summary>
        /// Invoke from <c>InferenceFrameworkDropdown</c> On Value Changed. Switches the inference framework.
        /// </summary>
        public async void OnInferenceFrameworkDropdownValueChanged(int index)
        {
            if (InferenceFrameworkDropdown == null || _inferenceReinitializing)
            {
                return;
            }

            InferenceFrameworkSelectionKind[] kinds =
                InferenceFrameworkUtils.GetSelectionValuesInEnumOrder();
            if (kinds.Length == 0)
            {
                return;
            }

            InferenceFrameworkSelectionKind newFramework =
                kinds[Mathf.Clamp(index, 0, kinds.Length - 1)];
            if (newFramework == InferenceFramework)
            {
                return;
            }

            await ReinitializeInferenceAsync(() => InferenceFramework = newFramework);
        }

        /// <summary>
        /// Invoke from <c>SentisInferenceTargetDropdown</c> On Value Changed.
        /// Switches Sentis inference target and reinitializes inference.
        /// </summary>
        public async void OnSentisInferenceTargetDropdownValueChanged(int index)
        {
            if (SentisInferenceTargetDropdown == null || _inferenceReinitializing)
            {
                return;
            }

            SentisInferenceTargetKind[] targetKinds = SentisInferenceUtils.GetTargetValuesInEnumOrder();
            if (targetKinds.Length == 0)
            {
                return;
            }

            SentisInferenceTargetKind newTarget =
                targetKinds[Mathf.Clamp(index, 0, targetKinds.Length - 1)];
            if (newTarget == SentisInferenceTarget)
            {
                return;
            }

            await ReinitializeInferenceAsync(() => SentisInferenceTarget = newTarget);
        }

        /// <summary>
        /// Invoke from <c>UseAsyncInferenceToggle</c> On Value Changed.
        /// Toggles async inference on the active runner without reinitializing the detector.
        /// </summary>
        public void OnUseAsyncInferenceToggleValueChanged()
        {
            if (_inferenceReinitializing)
            {
                return;
            }

            if (UseAsyncInferenceToggle == null)
            {
                return;
            }

            if (UseAsyncInferenceToggle.isOn != UseAsyncInference)
            {
                if (_inferenceRunner != null)
                {
                    _inferenceRunner.UseAsyncWork = UseAsyncInferenceToggle.isOn;
                }

                UseAsyncInference = UseAsyncInferenceToggle.isOn;
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _objectDetector, UseAsyncInference, InferenceFramework);
            }
        }

        // Private Methods
        private void RecreatePreviewTexture()
        {
            Mat frameMat = _multiSourceToMatHelper.FrameMat;
            if (frameMat == null)
            {
                return;
            }

            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
                _texture = null;
            }

            bool isRgb = _multiSourceToMatHelper.OutputColorFormat == SourceToMatColorFormat.RGB;
            _texture = new Texture2D(frameMat.cols(), frameMat.rows(), isRgb ? TextureFormat.RGB24 : TextureFormat.RGBA32, false);
            OpenCVMatUnityUtils.MatToTexture2D(frameMat, _texture);

            if (ResultPreview != null)
            {
                ResultPreview.texture = _texture;
                AspectRatioFitter aspectRatioFitter = ResultPreview.GetComponent<AspectRatioFitter>();
                if (aspectRatioFitter != null)
                {
                    aspectRatioFitter.aspectRatio = (float)_texture.width / _texture.height;
                }
            }
        }

        private int GetSourceFps()
        {
            if (_multiSourceToMatHelper.MatSource is ICameraMatSource cameraHelper)
            {
                return (int)cameraHelper.FPS;
            }

            if (_multiSourceToMatHelper.MatSource is IVideoFileMatSource videoHelper)
            {
                return (int)videoHelper.FPS;
            }

            return 30;
        }

        private void DisposeFrameProcessingResources()
        {
            _byteTracker?.Dispose();
            _byteTracker = null;
            _bgrMat?.Dispose();
            _bgrMat = null;
        }

        private void CreateOrRecreateProcessingResources(Mat rgbaMat, int fps)
        {
            if (rgbaMat == null)
            {
                return;
            }

            DisposeFrameProcessingResources();

            _byteTracker = new BYTETracker(fps, 30, mot20: false);
            _bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
        }

        private void CleanupPreviewResources()
        {
            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
                _texture = null;
            }

            UpdateFpsMonitorPlaybackState();
        }

        private void UpdateFpsMonitorPlaybackState()
        {
            if (_fpsMonitor == null || _multiSourceToMatHelper == null)
            {
                return;
            }

            _fpsMonitor.Add("PlaybackState", GetPlaybackStateText());
        }

        private string GetPlaybackStateText()
        {
            if (!_multiSourceToMatHelper.IsInitialized)
            {
                return "Uninitialized";
            }

            if (_multiSourceToMatHelper.IsPlaying)
            {
                return "Playing";
            }

            if (_multiSourceToMatHelper.IsPaused)
            {
                return "Paused";
            }

            return "Ready";
        }

        private void WireSourceToMatControlPanelHooks()
        {
            _controlPanel = GetComponent<SourceToMatControlPanel>();
            if (_controlPanel == null)
            {
                return;
            }

            _controlPanel.OnAfterPlay.AddListener(OnControlPanelAfterPlay);
            _controlPanel.OnAfterPause.AddListener(OnControlPanelAfterPause);
            _controlPanel.OnAfterStop.AddListener(OnControlPanelAfterStop);
            _controlPanel.OnRotate90Changed.AddListener(OnControlPanelRotate90Changed);
            _controlPanel.OnFlipVerticalChanged.AddListener(OnControlPanelFlipVerticalChanged);
            _controlPanel.OnFlipHorizontalChanged.AddListener(OnControlPanelFlipHorizontalChanged);
            _controlPanel.OnHelperKindChanged.AddListener(OnControlPanelHelperKindChanged);
        }

        private void UnwireSourceToMatControlPanelHooks()
        {
            if (_controlPanel == null)
            {
                return;
            }

            _controlPanel.OnAfterPlay.RemoveListener(OnControlPanelAfterPlay);
            _controlPanel.OnAfterPause.RemoveListener(OnControlPanelAfterPause);
            _controlPanel.OnAfterStop.RemoveListener(OnControlPanelAfterStop);
            _controlPanel.OnRotate90Changed.RemoveListener(OnControlPanelRotate90Changed);
            _controlPanel.OnFlipVerticalChanged.RemoveListener(OnControlPanelFlipVerticalChanged);
            _controlPanel.OnFlipHorizontalChanged.RemoveListener(OnControlPanelFlipHorizontalChanged);
            _controlPanel.OnHelperKindChanged.RemoveListener(OnControlPanelHelperKindChanged);
            _controlPanel = null;
        }

        private void CheckFilePaths()
        {
            bool useSentis = InferenceFramework == InferenceFrameworkSelectionKind.UnitySentis && OpenCVForUnityEnv.IsSentisIntegrationAvailable;
            string modelPath = useSentis ? _modelFilepathSentis : _modelFilepathOnnx;
            if (string.IsNullOrEmpty(modelPath))
            {
                ShowObjectDetectorResultToggle.isOn = ShowObjectDetectorResultToggle.interactable = false;
                _disableObjectDetector = true;
            }
        }

        private void ResetTrackers()
        {
            _byteTracker?.Reset();

            if (!_disableObjectDetector)
            {
                ShowObjectDetectorResultToggle.interactable = true;
            }
        }

        // Inference UI
        /// <summary>
        /// Locks inference UI during reinitialization, otherwise syncs the async toggle and
        /// delegates Framework / Target dropdown state to <see cref="UpdateInferenceFramework"/>.
        /// </summary>
        private void SyncInferenceModeUi(bool inferenceReinitializing)
        {
            if (inferenceReinitializing)
            {
                if (InferenceFrameworkDropdown != null)
                {
                    InferenceFrameworkDropdown.interactable = false;
                }

                if (SentisInferenceTargetDropdown != null)
                {
                    SentisInferenceTargetDropdown.interactable = false;
                }

                if (UseAsyncInferenceToggle != null)
                {
                    UseAsyncInferenceToggle.interactable = false;
                }

                return;
            }

            if (UseAsyncInferenceToggle != null)
            {
                UseAsyncInferenceToggle.SetIsOnWithoutNotify(UseAsyncInference);
                UseAsyncInferenceToggle.interactable = true;
            }

            UpdateInferenceFramework();
        }

        /// <summary>
        /// Applies framework fallback when Sentis is unavailable, resolves the default Sentis target,
        /// and populates Framework / Target dropdown options from Utils.
        /// </summary>
        private void UpdateInferenceFramework()
        {

            InferenceFrameworkSelectionKind[] kinds =
                InferenceFrameworkUtils.GetSelectionValuesInEnumOrder();
            if (Array.IndexOf(kinds, InferenceFramework) < 0 && kinds.Length > 0)
            {
                InferenceFramework = kinds[0];
            }

            if (OpenCVForUnityEnv.IsSentisIntegrationAvailable)
            {
                SentisInferenceTarget =
                    SentisInferenceUtils.ResolveDefaultTargetForDevice(SentisInferenceTarget);
            }

            PopulateInferenceFrameworkDropdown();
            PopulateSentisInferenceTargetDropdown();

            bool useSentis = InferenceFramework == InferenceFrameworkSelectionKind.UnitySentis;
            if (SentisInferenceTargetDropdown != null)
            {
                SentisInferenceTargetDropdown.interactable =
                    OpenCVForUnityEnv.IsSentisIntegrationAvailable && useSentis;
            }
        }

        /// <summary>
        /// Builds Framework dropdown options from <see cref="InferenceFrameworkUtils.GetSelectionValuesInEnumOrder"/>.
        /// </summary>
        private void PopulateInferenceFrameworkDropdown()
        {
            if (InferenceFrameworkDropdown == null)
            {
                return;
            }

            InferenceFrameworkSelectionKind[] kinds =
                InferenceFrameworkUtils.GetSelectionValuesInEnumOrder();

            InferenceFrameworkDropdown.ClearOptions();
            for (int i = 0; i < kinds.Length; i++)
            {
                InferenceFrameworkDropdown.options.Add(new Dropdown.OptionData(
                    InferenceFrameworkUtils.GetSelectionDisplayName(kinds[i])));
            }

            int idx = Array.IndexOf(kinds, InferenceFramework);
            InferenceFrameworkDropdown.SetValueWithoutNotify(Mathf.Max(0, idx));
            InferenceFrameworkDropdown.RefreshShownValue();
            InferenceFrameworkDropdown.interactable = kinds.Length > 1;
        }

        /// <summary>
        /// Builds Sentis Target dropdown options from <see cref="SentisInferenceUtils.GetTargetValuesInEnumOrder"/>.
        /// </summary>
        private void PopulateSentisInferenceTargetDropdown()
        {
            if (SentisInferenceTargetDropdown == null)
            {
                return;
            }

            SentisInferenceTargetKind[] targetKinds =
                SentisInferenceUtils.GetTargetValuesInEnumOrder();
            SentisInferenceTargetDropdown.ClearOptions();
            for (int i = 0; i < targetKinds.Length; i++)
            {
                SentisInferenceTargetDropdown.options.Add(new Dropdown.OptionData(
                    SentisInferenceUtils.GetTargetDisplayName(targetKinds[i])));
            }

            int idx = Array.IndexOf(targetKinds, SentisInferenceTarget);
            if (idx < 0 && targetKinds.Length > 0)
            {
                SentisInferenceTarget = targetKinds[0];
                idx = 0;
            }

            SentisInferenceTargetDropdown.SetValueWithoutNotify(Mathf.Max(0, idx));
            SentisInferenceTargetDropdown.RefreshShownValue();
        }

        /// <summary>
        /// Reserved hook for synchronizing <see cref="UseAsyncInference"/> with platform capabilities.
        /// Does not modify <see cref="UseAsyncInference"/> in this example.
        /// </summary>
        private void UpdateUseAsyncInference()
        {
        }

        private async Task ReinitializeInferenceAsync(Action applySelection)
        {
            _inferenceReinitializing = true;
            SyncInferenceModeUi(inferenceReinitializing: true);

            await DisposeInferenceAsync();

            applySelection();
            UpdateUseAsyncInference();
            TryInitializeInference();
            UpdateFpsMonitorInferenceInfo(_fpsMonitor, _objectDetector, UseAsyncInference, InferenceFramework);

            _inferenceReinitializing = false;
            SyncInferenceModeUi(inferenceReinitializing: false);
        }

        private async Task DisposeInferenceAsync()
        {
            var runner = _inferenceRunner;
            _inferenceRunner = null;
            if (runner != null)
            {
                await runner.DisposeAsync();
            }

            var objectDetector = _objectDetector;
            _objectDetector = null;
            if (objectDetector != null)
            {
                await objectDetector.DisposeAsync();
            }
        }

        private bool TryInitializeInference()
        {
            bool useSentis = InferenceFramework == InferenceFrameworkSelectionKind.UnitySentis && OpenCVForUnityEnv.IsSentisIntegrationAvailable;
            string modelPath = useSentis ? _modelFilepathSentis : _modelFilepathOnnx;
            if (string.IsNullOrEmpty(modelPath))
            {
                Debug.LogError("model: " + Model + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "model file is not loaded.\nPlease read console message.";
                }

                return false;
            }

            try
            {
                if (useSentis)
                {
                    _objectDetector = new YOLOXObjectDetectorMultiBackend(
                        modelPath,
                        _classesFilepath,
                        new Size(InpWidth, InpHeight),
                        ConfThreshold,
                        NmsThreshold,
                        TopK,
                        SentisInferenceBackendKind.UnitySentis,
                        SentisInferenceTarget);
                    Debug.Log(
                        "MultiObjectTrackingExample YOLOXObjectDetectorMultiBackend initialized (Sentis / UnitySentis, backend="
                        + SentisInferenceUtils.GetTargetDisplayName(SentisInferenceTarget) + ").",
                        this);
                }
                else
                {
                    _objectDetector = new YOLOXObjectDetectorMultiBackend(modelPath, _classesFilepath, new Size(InpWidth, InpHeight), ConfThreshold, NmsThreshold, TopK,
                        OpenCVDnnInferenceBackendKind.OpenCv,
                        OpenCVDnnInferenceTargetKind.Cpu);
                    Debug.Log("MultiObjectTrackingExample YOLOXObjectDetectorMultiBackend initialized (OpenCV DNN).", this);
                }

                var objectDetector = _objectDetector;
                _inferenceRunner = new MatSingleFlightSyncAsyncRunner(
                    useAsyncWork: UseAsyncInference,
                    asyncWorkCancellationToken: _cts.Token,
                    disposeAsyncAfterWorkTask: async () =>
                    {
                        await objectDetector.WaitForCompletionAsync();
                    });
                return _objectDetector != null && _inferenceRunner != null;
            }
            catch (Exception ex)
            {
                Debug.LogError("MultiObjectTrackingExample TryInitializeInference failed: " + ex, this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "Failed to initialize inference.\nPlease read console message.";
                }

                if (_objectDetector != null)
                {
                    _objectDetector.Dispose();
                    _objectDetector = null;
                }

                _inferenceRunner = null;
                return false;
            }
        }

        /// <summary>
        /// Updates <paramref name="fpsMonitor"/> with dnn backend, target, and async mode from
        /// <paramref name="worker"/> and <paramref name="useAsyncInference"/> (or "-" when a value is not available).
        /// </summary>
        private static void UpdateFpsMonitorInferenceInfo(
            FpsMonitor fpsMonitor,
            OpenCVForUnity.Extensions.Worker.DnnModule.DnnInferenceWorkerBase worker,
            bool useAsyncInference,
            InferenceFrameworkSelectionKind inferenceFramework)
        {
            if (fpsMonitor == null)
            {
                return;
            }

            fpsMonitor.Add(
                "inferenceFramework",
                InferenceFrameworkUtils.GetSelectionDisplayName(inferenceFramework));

            if (worker != null)
            {
                int be = worker.DnnBackend;
                int tgt = worker.DnnTarget;
                fpsMonitor.Add("dnnBackend", MultiBackendNet.GetBackendDisplayString(be));
                fpsMonitor.Add("dnnTarget", MultiBackendNet.GetTargetDisplayString(tgt));
            }
            else
            {
                fpsMonitor.Add("dnnBackend", "-");
                fpsMonitor.Add("dnnTarget", "-");
            }

            string useAsyncText = worker != null
                ? useAsyncInference.ToString()
                : "-";
            fpsMonitor.Add("useAsyncInference", useAsyncText);
        }

        private BBox[] ConvertToBBoxes(Mat result)
        {
            if (result.empty() || result.cols() < 6)
            {
                return new BBox[0];
            }

            Span<ObjectDetectionData> data = _objectDetector.ToStructuredDataAsSpan(result);

            BBox[] inputs = new BBox[data.Length];
            for (int i = 0; i < data.Length; ++i)
            {
                ref readonly var d = ref data[i];
                inputs[i] = new BBox(d);
            }

            return inputs;
        }
    }
}
#endif
