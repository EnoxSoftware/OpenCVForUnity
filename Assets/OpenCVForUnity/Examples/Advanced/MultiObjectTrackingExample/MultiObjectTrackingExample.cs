#if !UNITY_WSA_10_0 && NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.UnityIntegration.MOT;
using OpenCVForUnity.UnityIntegration.MOT.ByteTrack;
using OpenCVForUnity.UnityIntegration.Runner;
using OpenCVForUnity.UnityIntegration.Worker;
using OpenCVForUnity.UnityIntegration.Worker.DnnModule;
using OpenCVForUnity.UnityIntegration.Worker.DataStruct;
using OpenCVForUnity.UnityIntegration.Worker.Utils;
#if OPENCV_SENTIS_AVAILABLE
using Unity.InferenceEngine;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static OpenCVForUnity.UnityIntegration.Helper.Source2Mat.MultiSource2MatHelper;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Multi Object Tracking (MOT) Example
    /// An example of tracking object detection results using the MOT (Multi Object Tracking) algorithm.
    ///
    /// ByteTrack: https://github.com/ifzhang/ByteTrack
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class MultiObjectTrackingExample : MonoBehaviour
    {
        // Public Fields
        [Header("Output")]
        [Tooltip("The RawImage for previewing the result.")]
        public RawImage ResultPreview;

        [Header("UI")]
        [Tooltip("ON: Sentis. OFF: OpenCV DNN. Assign OnUseSentisInferenceToggleValueChanged to this toggle's On Value Changed in the Inspector.")]
        public Toggle UseSentisInferenceToggle;
        [Tooltip("Sentis backend selector. Dropdown option order must match Enum.GetValues(typeof(BackendType)) (numeric order). Assign OnSentisBackendDropdownValueChanged to On Value Changed (int). Value changes reinitialize inference.")]
        public Dropdown SentisBackendDropdown;
#if OPENCV_SENTIS_AVAILABLE
        [Tooltip("When enabled, runs YOLOX inference with Sentis (MultiBackendDnn.DNN_BACKEND_UNITY_SENTIS). Inspector paths may stay .onnx; at runtime they are rewritten to .sentis and loaded from StreamingAssets (place a matching .sentis beside the onnx file).")]
        public bool UseSentisInference = true;
        [Tooltip("When using Sentis: backend / target selects Sentis BackendType (CPU / GPU, etc.).")]
        public BackendType YoloSentisBackendType = BackendType.GPUCompute;
#endif
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
        private YOLOXObjectDetector _objectDetector;
#if OPENCV_SENTIS_AVAILABLE
        private string _modelFilepathSentis;
        /// <summary>
        /// <see cref="BackendType"/> values in <see cref="Enum.GetValues(System.Type)"/> order (sorted by underlying numeric value). Dropdown options must use the same order.
        /// </summary>
        private static readonly BackendType[] SentisBackendTypesInEnumOrder =
            (BackendType[])Enum.GetValues(typeof(BackendType));
#endif
        private MatSingleFlightSyncAsyncRunner _inferenceRunner;
        private bool _inferenceReinitializing;

        private BYTETracker _byteTracker;
        private BYTETrackInfoVisualizer _byteTrackInfoVisualizer;
        private bool _disableObjectDetector = false;
        private string _classesFilepath;
        private string _modelFilepathOnnx;

        private Texture2D _texture;
        private MultiSource2MatHelper _multiSource2MatHelper;
        private Mat _bgrMat;

        private FpsMonitor _fpsMonitor;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();

            // WebCamTexture2MatHelper does not work on WebGPU, so use WebCamTexture2MatAsyncGPUHelper instead.
#if UNITY_6000_0_OR_NEWER
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.WebGPU && _multiSource2MatHelper.RequestedSource2MatHelperClassName == MultiSource2MatHelperClassName.WebCamTexture2MatHelper)
            {
                _multiSource2MatHelper.RequestedSource2MatHelperClassName = MultiSource2MatHelperClassName.WebCamTexture2MatAsyncGPUHelper;
            }
#endif
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;

            // Update GUI state
            ShowObjectDetectorResultToggle.isOn = ShowObjectDetectorResult;
            EnableByteTrackToggle.isOn = EnableByteTrack;
            UpdateUseSentisInference();
            UpdateUseAsyncInference();
            UpdateInferenceModeToggles(inferenceReinitializing: false);

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            if (!string.IsNullOrEmpty(Classes))
            {
                _classesFilepath = await OpenCVEnv.GetFilePathTaskAsync(Classes, cancellationToken: _cts.Token);
                if (string.IsNullOrEmpty(_classesFilepath))
                {
                    Debug.LogError("classes: " + Classes + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
                    if (_fpsMonitor != null)
                        _fpsMonitor.Toast("classes file is not loaded.\nPlease read console message.", 20000);
                }
            }
            if (!string.IsNullOrEmpty(Model))
            {
                _modelFilepathOnnx = await OpenCVEnv.GetFilePathTaskAsync(Model, cancellationToken: _cts.Token);
#if OPENCV_SENTIS_AVAILABLE
                string sentisModelFileName = StreamingAssetPathOnnxToSentisIfNeeded(Model);
                _modelFilepathSentis = await OpenCVEnv.GetFilePathTaskAsync(
                    sentisModelFileName,
                    cancellationToken: _cts.Token);
#endif
            }

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            CheckFilePaths();
            Run();
        }

        private void Update()
        {
            if (_inferenceReinitializing)
                return;

            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_objectDetector != null && !_disableObjectDetector && _inferenceRunner != null)
                {
                    Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

                    _inferenceRunner.SubmitWork(
                        _bgrMat,
                        syncWork: m => _objectDetector.Detect(m, useCopyOutput: true),
                        asyncWork: async m =>
                        {
                            CancellationToken ct = _inferenceRunner.InFlightAsyncWorkCancellationToken;
                            return await _objectDetector.DetectTaskAsync(m, ct);
                        });

                    if (_inferenceRunner.TryGetLatestResult(out Mat results))
                    {
                        if (ShowObjectDetectorResult)
                            _objectDetector.Visualize(rgbaMat, results, false, true);

                        if (EnableByteTrack && _byteTrackInfoVisualizer != null)
                        {
                            BBox[] inputs = ConvertToBBoxes(results);
                            _byteTracker.Update(inputs);
                            BYTETrackInfo[] outputs = _byteTracker.GetActiveTrackInfos();
                            _byteTrackInfoVisualizer.Visualize(rgbaMat, outputs, false, true);
                        }
                    }
                }

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private async void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            await DisposeInferenceAsync();

            _byteTracker?.Dispose();
            _byteTrackInfoVisualizer?.Dispose();

            OpenCVDebug.SetDebugMode(false);

            _cts?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = _multiSource2MatHelper.GetMat();

            _texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;


            int fps = 30;
            if (_multiSource2MatHelper.Source2MatHelper is ICameraSource2MatHelper cameraHelper)
            {
                fps = (int)cameraHelper.GetFPS();
            }
            else if (_multiSource2MatHelper.Source2MatHelper is IVideoSource2MatHelper videoHelper)
            {
                fps = (int)videoHelper.GetFPS();
            }

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("width", rgbaMat.width().ToString());
                _fpsMonitor.Add("height", rgbaMat.height().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
                _fpsMonitor.Add("source fps", fps.ToString());
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _objectDetector, UseAsyncInference);
            }

            _byteTracker = new BYTETracker(fps, 30, mot20: false);

            _bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            try
            {
                _objectDetector?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            _inferenceRunner?.Cancel();

            _byteTracker?.Dispose(); _byteTracker = null;
            _bgrMat?.Dispose(); _bgrMat = null;
            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            _multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            _multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            _multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            _multiSource2MatHelper.RequestedIsFrontFacing = !_multiSource2MatHelper.RequestedIsFrontFacing;
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
        /// Invoke from <c>UseSentisInferenceToggle</c> On Value Changed. Switches the inference backend.
        /// No-op when <c>OPENCV_SENTIS_AVAILABLE</c> is not defined.
        /// </summary>
        public async void OnUseSentisInferenceToggleValueChanged()
        {
#if !OPENCV_SENTIS_AVAILABLE
            await Task.CompletedTask;
            return;
#else
            if (UseSentisInferenceToggle == null || _inferenceReinitializing)
                return;

            bool newSentis = UseSentisInferenceToggle.isOn;
            if (newSentis == UseSentisInference)
                return;

            _inferenceReinitializing = true;
            UpdateInferenceModeToggles(inferenceReinitializing: true);

            await DisposeInferenceAsync();

            UseSentisInference = newSentis;
            UpdateUseAsyncInference();

            CheckFilePaths();
            InitializeInference();

            UpdateFpsMonitorInferenceInfo(_fpsMonitor, _objectDetector, UseAsyncInference);

            _inferenceReinitializing = false;
            UpdateInferenceModeToggles(inferenceReinitializing: false);
#endif
        }

        /// <summary>
        /// Invoke from <c>SentisBackendDropdown</c> On Value Changed. Switches Sentis backend type and reinitializes inference.
        /// No-op when <c>OPENCV_SENTIS_AVAILABLE</c> is not defined.
        /// </summary>
        public async void OnSentisBackendDropdownValueChanged(int index)
        {
#if !OPENCV_SENTIS_AVAILABLE
            await Task.CompletedTask;
            return;
#else
            if (SentisBackendDropdown == null || _inferenceReinitializing)
                return;

            int n = SentisBackendTypesInEnumOrder.Length;
            if (n == 0)
                return;
            int maxIdx = Mathf.Min(SentisBackendDropdown.options.Count, n) - 1;
            if (maxIdx < 0)
                return;
            BackendType newBackend = SentisBackendTypesInEnumOrder[Mathf.Clamp(index, 0, maxIdx)];
            if (newBackend == YoloSentisBackendType)
                return;

            _inferenceReinitializing = true;
            UpdateInferenceModeToggles(inferenceReinitializing: true);

            await DisposeInferenceAsync();

            YoloSentisBackendType = newBackend;
            UpdateUseSentisInference();
            UpdateUseAsyncInference();

            InitializeInference();

            UpdateFpsMonitorInferenceInfo(_fpsMonitor, _objectDetector, UseAsyncInference);

            _inferenceReinitializing = false;
            UpdateInferenceModeToggles(inferenceReinitializing: false);
#endif
        }

        /// <summary>
        /// Raises the use async inference toggle value changed event.
        /// </summary>
        public void OnUseAsyncInferenceToggleValueChanged()
        {
            if (_inferenceReinitializing)
                return;
            if (UseAsyncInferenceToggle == null)
                return;
            if (UseAsyncInferenceToggle.isOn != UseAsyncInference)
            {
                if (_inferenceRunner != null)
                    _inferenceRunner.UseAsyncWork = UseAsyncInferenceToggle.isOn;
                UseAsyncInference = UseAsyncInferenceToggle.isOn;
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _objectDetector, UseAsyncInference);
            }
        }

        // Private Methods
        private void CheckFilePaths()
        {
#if OPENCV_SENTIS_AVAILABLE
            string modelPath = UseSentisInference ? _modelFilepathSentis : _modelFilepathOnnx;
#else
            string modelPath = _modelFilepathOnnx;
#endif
            if (string.IsNullOrEmpty(modelPath))
            {
                ShowObjectDetectorResultToggle.isOn = ShowObjectDetectorResultToggle.interactable = false;
                _disableObjectDetector = true;
            }
        }

        private void Run()
        {
            OpenCVDebug.SetDebugMode(true);

            InitializeInference();

            _byteTrackInfoVisualizer = new BYTETrackInfoVisualizer(_classesFilepath);

            _multiSource2MatHelper.Initialize();
        }

        private void ResetTrackers()
        {
            _byteTracker?.Reset();

            if (!_disableObjectDetector)
                ShowObjectDetectorResultToggle.interactable = true;
        }

        /// <summary>
        /// Updates async inference and (when <c>OPENCV_SENTIS_AVAILABLE</c>) Sentis toggle interactability and visible state to match
        /// the current <see cref="UseAsyncInference"/> / <see cref="UseSentisInference"/> (UI only; call
        /// <see cref="UpdateUseAsyncInference"/> first so field values are up to date).
        /// </summary>
        private void UpdateInferenceModeToggles(bool inferenceReinitializing)
        {
            if (inferenceReinitializing)
            {
                if (UseSentisInferenceToggle != null)
                    UseSentisInferenceToggle.interactable = false;
                if (SentisBackendDropdown != null)
                    SentisBackendDropdown.interactable = false;
                if (UseAsyncInferenceToggle != null)
                    UseAsyncInferenceToggle.interactable = false;
                return;
            }

            if (UseAsyncInferenceToggle != null)
            {
                UseAsyncInferenceToggle.SetIsOnWithoutNotify(UseAsyncInference);
                UseAsyncInferenceToggle.interactable = true;
            }
#if OPENCV_SENTIS_AVAILABLE
            if (UseSentisInferenceToggle != null)
            {
                UseSentisInferenceToggle.SetIsOnWithoutNotify(UseSentisInference);
                UseSentisInferenceToggle.interactable = true;
            }
            if (SentisBackendDropdown != null)
                SentisBackendDropdown.interactable = UseSentisInference;
            UpdateSentisBackendDropdown();
#else
            if (UseSentisInferenceToggle != null)
            {
                UseSentisInferenceToggle.SetIsOnWithoutNotify(false);
                UseSentisInferenceToggle.interactable = false;
            }
            if (SentisBackendDropdown != null)
                SentisBackendDropdown.interactable = false;
#endif
        }

#if OPENCV_SENTIS_AVAILABLE
        private void UpdateSentisBackendDropdown()
        {
            if (SentisBackendDropdown == null || SentisBackendDropdown.options.Count == 0)
                return;
            if (SentisBackendTypesInEnumOrder.Length == 0)
                return;
            int idx = Array.IndexOf(SentisBackendTypesInEnumOrder, YoloSentisBackendType);
            if (idx < 0)
                idx = 0;
            int maxIdx = Mathf.Min(SentisBackendDropdown.options.Count, SentisBackendTypesInEnumOrder.Length) - 1;
            SentisBackendDropdown.SetValueWithoutNotify(Mathf.Clamp(idx, 0, maxIdx));
        }
#endif

        private void UpdateUseSentisInference()
        {
#if OPENCV_SENTIS_AVAILABLE
            if (!SystemInfo.supportsComputeShaders && YoloSentisBackendType == BackendType.GPUCompute)
                YoloSentisBackendType = BackendType.GPUPixel;
#endif
        }

        private void UpdateUseAsyncInference()
        {
        }

        private async Task DisposeInferenceAsync()
        {
            if (_inferenceRunner != null)
                await _inferenceRunner.DisposeAsync();
            _inferenceRunner = null;

            _objectDetector?.Dispose();
            _objectDetector = null;
        }

        private void InitializeInference()
        {
            string modelPath = _modelFilepathOnnx;
#if OPENCV_SENTIS_AVAILABLE
            if (UseSentisInference)
                modelPath = _modelFilepathSentis;
#endif
            if (string.IsNullOrEmpty(modelPath))
            {
                Debug.LogError("model: " + Model + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
                if (_fpsMonitor != null)
                    _fpsMonitor.Toast("model file is not loaded.\nPlease read console message.", 20000);
                return;
            }

            try
            {
#if OPENCV_SENTIS_AVAILABLE
                if (UseSentisInference)
                {
                    _objectDetector = new YOLOXObjectDetector(
                        modelPath,
                        _classesFilepath,
                        new Size(InpWidth, InpHeight),
                        ConfThreshold,
                        NmsThreshold,
                        TopK,
                        MultiBackendDnn.DNN_BACKEND_UNITY_SENTIS,
                        (int)YoloSentisBackendType);
                    Debug.Log("MultiObjectTrackingExample YOLOXObjectDetector initialized (Sentis / DNN_BACKEND_UNITY_SENTIS, backend=" + YoloSentisBackendType + ").");
                }
                else
#endif
                {
                    _objectDetector = new YOLOXObjectDetector(modelPath, _classesFilepath, new Size(InpWidth, InpHeight), ConfThreshold, NmsThreshold, TopK);
                    Debug.Log("MultiObjectTrackingExample YOLOXObjectDetector initialized (OpenCV DNN).");
                }

                _inferenceRunner = new MatSingleFlightSyncAsyncRunner(
                    useAsyncWork: UseAsyncInference,
                    asyncWorkCancellationToken: _cts.Token,
                    disposeAsyncAfterWorkTask: async () =>
                    {
                        await _objectDetector.WaitForCompletionTaskAsync();
                    });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("MultiObjectTrackingExample InitializeInference failed: " + ex);
            }
        }

        private static void UpdateFpsMonitorInferenceInfo(FpsMonitor fpsMonitor, DnnInferenceWorkerBase worker, bool useAsyncInference)
        {
            if (fpsMonitor == null)
                return;

            if (worker != null)
            {
                int be = worker.DnnBackend;
                int tgt = worker.DnnTarget;
                fpsMonitor.Add("dnnBackend", MultiBackendDnn.GetBackendDisplayString(be));
                fpsMonitor.Add("dnnTarget", MultiBackendDnn.GetTargetDisplayString(tgt));
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

#if OPENCV_SENTIS_AVAILABLE
        private static string StreamingAssetPathOnnxToSentisIfNeeded(string streamingAssetsRelativePath)
        {
            if (string.IsNullOrEmpty(streamingAssetsRelativePath))
                return streamingAssetsRelativePath;
            if (!streamingAssetsRelativePath.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase))
                return streamingAssetsRelativePath;
            return Path.ChangeExtension(streamingAssetsRelativePath, ".sentis");
        }
#endif

        private BBox[] ConvertToBBoxes(Mat result)
        {
            if (result.empty() || result.cols() < 6)
                return new BBox[0];

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
