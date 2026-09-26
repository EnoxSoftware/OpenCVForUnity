#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.Extensions.Runner;
using OpenCVForUnity.Extensions.SourceToMat;
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
using Range = OpenCVForUnity.CoreModule.Range;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Fast Neural Style Transfer Example
    /// Applies an ONNX fast-neural-style model to each input frame for artistic style transfer.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Loading a .onnx style model from StreamingAssets via <see cref="MultiBackendDnn.ReadNet"/>
    /// - Toggling Sentis vs OpenCV DNN inference and optional async inference via MatSingleFlightSyncAsyncRunner
    /// - Converting RGBA frames to BGR, resizing to 224x224 for model input, and upscaling styled output back to the frame size
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Size"/>, <see cref="Scalar"/>
    /// - <see cref="Dnn"/>: blobFromImage
    /// - <see cref="Imgproc"/>: cvtColor, resize
    /// - <see cref="MultiBackendNet"/>, <see cref="MultiBackendDnn"/>, <see cref="MatSingleFlightSyncAsyncRunner"/>
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://github.com/opencv/opencv/blob/master/samples/dnn/fast_neural_style.py
    /// https://github.com/onnx/models/tree/main/validated/vision/style_transfer/fast_neural_style
    /// </para>
    /// <para>
    /// [Tested Models]
    /// https://github.com/onnx/models/raw/main/validated/vision/style_transfer/fast_neural_style/model/mosaic-9.onnx
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class FastNeuralStyleTransferExample : MonoBehaviour
    {
        // Constants
        private static readonly string MODEL_FILEPATH = "OpenCVForUnityExamples/dnn/mosaic-9.onnx";
        private readonly Size _inputSize = new Size(224, 224);

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
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

        // Private Fields
        private Texture2D _texture;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private Mat _bgrMat;
        private Mat _bgr224Mat;
        /// <summary>
        /// The net (<see cref="MultiBackendNet"/>; loaded via <see cref="MultiBackendDnn.ReadNet"/>).
        /// </summary>
        private MultiBackendNet _net;
        private readonly List<Mat> _forwardOutputBlobs = new List<Mat>();
        private List<string> _unconnectedOutLayerNames;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;
        private string _modelFilepathOnnx;
        private string _modelFilepathSentis;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private MatSingleFlightSyncAsyncRunner _inferenceRunner;
        private bool _inferenceReinitializing;

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            WireSourceToMatControlPanelHooks();

            UpdateUseAsyncInference();
            SyncInferenceModeUi(inferenceReinitializing: false);

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            _modelFilepathOnnx = await OpenCVForUnityEnv.GetFilePathAsync(MODEL_FILEPATH, cancellationToken: _cts.Token);
            if (OpenCVForUnityEnv.IsSentisIntegrationAvailable)
            {
                _modelFilepathSentis = await OpenCVForUnityEnv.GetFilePathAsync(
                    MultiBackendDnn.ResolveSentisModelPathFromOnnxPath(MODEL_FILEPATH),
                    cancellationToken: _cts.Token);
            }

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            // Load ONNX style-transfer model from StreamingAssets (OpenCV DNN or Sentis per toggle).
            if (!TryInitializeInference())
            {
                return;
            }

            _multiSourceToMatHelper.Initialize();
        }

        private async void OnDestroy()
        {
            UnwireSourceToMatControlPanelHooks();

            _cts?.Cancel();

            await DisposeInferenceAsync();

            _cts?.Dispose();
            _cts = null;

            OpenCVDebug.SetDebugMode(false);
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

            if (_net != null)
            {
                if (_inferenceRunner != null)
                {
                    // Convert RGBA camera frame to BGR for DNN style-transfer input.
                    Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

                    // Submit sync or async forward; TryGetLatestResult returns the styled output Mat when ready.
                    _inferenceRunner.SubmitWork(
                        _bgrMat,
                        syncWork: Infer,
                        asyncWork: async m =>
                        {
                            CancellationToken ct = _inferenceRunner.InFlightAsyncWorkCancellationToken;
                            return await InferAsync(m, ct);
                        });

                    if (_inferenceRunner.TryGetLatestResult(out Mat outMat))
                    {
                        outMat.copyTo(_bgrMat);
                        Imgproc.cvtColor(_bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);
                    }
                }
            }

            // Publish styled RGBA Mat to Unity texture for RawImage preview.
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

            // Fill in the image so that the unprocessed image is not displayed.
            rgbaMat.setTo(new Scalar(0, 0, 0, 255));

            RecreatePreviewTexture();
            CreateOrRecreateProcessingResources(rgbaMat);

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
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _net, UseAsyncInference, InferenceFramework);
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

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;
            if (rgbaMat != null)
            {
                // Fill so the unprocessed image is not displayed at the new size.
                rgbaMat.setTo(new Scalar(0, 0, 0, 255));
            }

            RecreatePreviewTexture();
            CreateOrRecreateProcessingResources(rgbaMat);

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

            if (UseAsyncInferenceToggle != null && UseAsyncInferenceToggle.isOn != UseAsyncInference)
            {
                if (_inferenceRunner != null)
                {
                    _inferenceRunner.UseAsyncWork = UseAsyncInferenceToggle.isOn;
                }

                UseAsyncInference = UseAsyncInferenceToggle.isOn;
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _net, UseAsyncInference, InferenceFramework);
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

        private void DisposeFrameProcessingResources()
        {
            _bgrMat?.Dispose();
            _bgrMat = null;
            _bgr224Mat?.Dispose();
            _bgr224Mat = null;
        }

        private void CreateOrRecreateProcessingResources(Mat frameMat)
        {
            if (frameMat == null)
            {
                return;
            }

            DisposeFrameProcessingResources();

            _bgrMat = new Mat(frameMat.rows(), frameMat.cols(), CvType.CV_8UC3);
            _bgr224Mat = new Mat((int)_inputSize.height, (int)_inputSize.width, CvType.CV_8UC3);
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

        /// <summary>
        /// Initializes inference from the resolved model path and current backend settings.
        /// </summary>
        private bool TryInitializeInference()
        {
            bool useSentis = InferenceFramework == InferenceFrameworkSelectionKind.UnitySentis && OpenCVForUnityEnv.IsSentisIntegrationAvailable;
            string modelPath = useSentis ? _modelFilepathSentis : _modelFilepathOnnx;

            if (string.IsNullOrEmpty(modelPath))
            {
                Debug.LogError(MODEL_FILEPATH + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "model file is not loaded.\nPlease read console message.";
                }
                return false;
            }

            try
            {
                _net = MultiBackendDnn.ReadNet(modelPath);
                if (useSentis)
                {
                    _net.SetPreferableBackend(SentisInferenceBackendKind.UnitySentis);
                    _net.SetPreferableTarget(SentisInferenceTarget);
                }
                else
                {
                    _net.SetPreferableBackend(OpenCVDnnInferenceBackendKind.OpenCv);
                    _net.SetPreferableTarget(OpenCVDnnInferenceTargetKind.Cpu);
                }

                _unconnectedOutLayerNames = _net.GetUnconnectedOutLayersNames();

                _inferenceRunner = new MatSingleFlightSyncAsyncRunner(
                    useAsyncWork: UseAsyncInference,
                    asyncWorkCancellationToken: _cts.Token);

                return _net != null && _inferenceRunner != null;
            }
            catch (Exception ex)
            {
                Debug.LogError("FastNeuralStyleTransferExample TryInitializeInference failed: " + ex.Message, this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "Failed to initialize inference.\nPlease read console message.";
                }

                _inferenceRunner = null;
                if (_net != null)
                {
                    _net.Dispose();
                    _net = null;
                }
                _unconnectedOutLayerNames = null;
                return false;
            }
        }

        /// <summary>
        /// Resizes <paramref name="bgrInput"/> to <see cref="_inputSize"/> and builds the NCHW blob
        /// (same layout as <c>cv::dnn::blobFromImage</c> with <c>swapRB=true</c>).
        /// </summary>
        private Mat CreateStyleTransferBlobFromBgr(Mat bgrInput)
        {
            Imgproc.resize(bgrInput, _bgr224Mat, _inputSize);
            return Dnn.blobFromImage(_bgr224Mat, 1.0, _inputSize, new Scalar(0), true, false);
        }

        /// <summary>
        /// Upscales a 224x224 8U BGR style-transfer result to <paramref name="outputSize"/>.
        /// </summary>
        private static Mat UpscaleStyleTransferResult(Mat result224, Size outputSize)
        {
            Mat resultFull = new Mat();
            Imgproc.resize(result224, resultFull, outputSize);
            return resultFull;
        }

        /// <summary>
        /// Converts a style-transfer forward output (NCHW float BGR) into an independent 8U BGR <see cref="Mat"/>.
        /// </summary>
        private static Mat PostProcessStyleTransferOutput(Mat prob)
        {
            int h = prob.size(2);
            int w = prob.size(3);
            int[] newshape = new int[] { h, w };

            using (Mat outMat = new Mat(h, w, CvType.CV_32FC3))
            {
                using (Mat tmp_B_channel = new Mat(prob, new Range[] { new Range(0, 1), new Range(0, 1), Range.all(), Range.all() }).reshape(1, newshape))
                using (Mat tmp_G_channel = new Mat(prob, new Range[] { new Range(0, 1), new Range(1, 2), Range.all(), Range.all() }).reshape(1, newshape))
                using (Mat tmp_R_channel = new Mat(prob, new Range[] { new Range(0, 1), new Range(2, 3), Range.all(), Range.all() }).reshape(1, newshape))
                {
                    Core.merge(new List<Mat>() { tmp_B_channel, tmp_G_channel, tmp_R_channel }, outMat);
                }

                Mat result8u = new Mat();
                outMat.convertTo(result8u, CvType.CV_8U);
                return result8u;
            }
        }

        /// <summary>
        /// Synchronous style-transfer forward; returns 8U BGR at <paramref name="bgrInput"/> resolution.
        /// </summary>
        private Mat Infer(Mat bgrInput)
        {
            Mat blob = CreateStyleTransferBlobFromBgr(bgrInput);

            _net.SetInput(blob);
            _net.Forward(_forwardOutputBlobs, _unconnectedOutLayerNames);
            Mat prob = _forwardOutputBlobs[0];

            using (Mat result224 = PostProcessStyleTransferOutput(prob))
            {
                blob.Dispose();
                return UpscaleStyleTransferResult(result224, bgrInput.size());
            }
        }

        private async Task<Mat> InferAsync(Mat bgrInput, CancellationToken cancellationToken)
        {
            if (_net.ActiveInferenceFramework == InferenceFrameworkKind.UnitySentis
                && _net is IDnnAsyncInferenceNet)
            {
                Mat blob = CreateStyleTransferBlobFromBgr(bgrInput);

                _net.SetInput(blob);
                await _net.ForwardAsync(_forwardOutputBlobs, _unconnectedOutLayerNames, cancellationToken);
                Mat prob = _forwardOutputBlobs[0];

                using (Mat result224 = PostProcessStyleTransferOutput(prob))
                {
                    blob.Dispose();
                    return UpscaleStyleTransferResult(result224, bgrInput.size());
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
#if UNITY_WEBGL && !UNITY_EDITOR
            return await Task.FromResult(Infer(bgrInput));
#else
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Infer(bgrInput);
            }, cancellationToken);
#endif
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
            UpdateFpsMonitorInferenceInfo(_fpsMonitor, _net, UseAsyncInference, InferenceFramework);

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

            for (int i = 0; i < _forwardOutputBlobs.Count; i++)
            {
                _forwardOutputBlobs[i]?.Dispose();
            }
            _forwardOutputBlobs.Clear();

            _net?.Dispose();
            _net = null;
            _unconnectedOutLayerNames = null;
        }

        /// <summary>
        /// Updates <paramref name="fpsMonitor"/> with dnn backend, target, and async mode from
        /// <paramref name="net"/> and <paramref name="useAsyncInference"/> (or "-" when a value is not available).
        /// </summary>
        private static void UpdateFpsMonitorInferenceInfo(
            FpsMonitor fpsMonitor,
            MultiBackendNet net,
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

            if (net != null)
            {
                int be;
                int tgt;
                try
                {
                    be = net.PreferredBackend;
                    tgt = net.PreferredTarget;
                }
                catch (InvalidOperationException)
                {
                    fpsMonitor.Add("dnnBackend", "-");
                    fpsMonitor.Add("dnnTarget", "-");
                    fpsMonitor.Add("useAsyncInference", useAsyncInference.ToString());
                    return;
                }
                fpsMonitor.Add("dnnBackend", MultiBackendNet.GetBackendDisplayString(be));
                fpsMonitor.Add("dnnTarget", MultiBackendNet.GetTargetDisplayString(tgt));
            }
            else
            {
                fpsMonitor.Add("dnnBackend", "-");
                fpsMonitor.Add("dnnTarget", "-");
            }
            fpsMonitor.Add("useAsyncInference", useAsyncInference.ToString());
        }
    }
}
#endif
