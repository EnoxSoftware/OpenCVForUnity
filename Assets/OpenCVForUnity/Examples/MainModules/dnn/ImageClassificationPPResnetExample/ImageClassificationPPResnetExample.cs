#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
#if !UNITY_WSA_10_0
using OpenCVForUnity.DnnModule;
#endif
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.UnityIntegration.Runner;
using OpenCVForUnity.UnityIntegration.Worker.DnnModule;
using OpenCVForUnity.UnityIntegration.Worker.Utils;
#if OPENCV_SENTIS_AVAILABLE
using System.IO;
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
    /// Image Classification PPResnet Example
    /// An example of using OpenCV dnn module with Image Classification PPResnet model.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/master/models/image_classification_ppresnet
    ///
    /// [Tested Models]
    /// https://github.com/opencv/opencv_zoo/raw/c8812a7668ea3f285797c0c450d0912add9248f2/models/image_classification_ppresnet/image_classification_ppresnet50_2022jan.onnx
    /// https://raw.githubusercontent.com/opencv/opencv_zoo/326e15b31a70812eb6d616406d6e6a17ceaddb6f/models/image_classification_ppresnet/imagenet_labels.txt
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class ImageClassificationPPResnetExample : MonoBehaviour
    {
        // Constants
        protected static readonly string MODEL_FILENAME = "OpenCVForUnityExamples/dnn/image_classification_ppresnet50_2022jan.onnx";
        protected static readonly string CLASSES_FILENAME = "OpenCVForUnityExamples/dnn/imagenet_labels.txt";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        [Header("UI")]
        [Tooltip("ON: Sentis. OFF: OpenCV DNN. Assign OnUseSentisInferenceToggleValueChanged to this toggle's On Value Changed in the Inspector.")]
        public Toggle UseSentisInferenceToggle;
        [Tooltip("Sentis backend selector. Dropdown option order must match Enum.GetValues(typeof(BackendType)) (numeric order). Assign OnSentisBackendDropdownValueChanged to On Value Changed (int). Value changes reinitialize inference.")]
        public Dropdown SentisBackendDropdown;
#if OPENCV_SENTIS_AVAILABLE
        [Tooltip("When enabled, runs PPResNet classification with Sentis (MultiBackendDnn.DNN_BACKEND_UNITY_SENTIS). Inspector paths may stay .onnx; at runtime they are rewritten to .sentis and loaded from StreamingAssets (place a matching .sentis beside the onnx file).")]
        public bool UseSentisInference = true;
        [Tooltip("When using Sentis: dnnTarget selects Sentis BackendType (CPU / GPU, etc.).")]
        public BackendType SentisBackendType = BackendType.GPUCompute;
#endif
        [Tooltip("When on, in-flight single-task async path runs PPResNet forward on a background thread (Task.Run) via MatSingleFlightSyncAsyncRunner.")]
        public Toggle UseAsyncInferenceToggle;
        public bool UseAsyncInference = true;

        // Private Fields
        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        private Mat _rgbMat;

        /// <summary>
        /// The net (<see cref="MultiBackendNet"/>; loaded via <see cref="MultiBackendDnn.readNet"/>).
        /// </summary>
        private MultiBackendNet _net;

        private readonly List<Mat> _forwardOutputBlobs = new List<Mat>();
        private List<string> _unconnectedOutLayerNames;
#if OPENCV_SENTIS_AVAILABLE
        private bool _inferenceReinitializing;
#endif

        private Size _inputSize = new Size(224, 224);
        private Scalar _mean = new Scalar(0.485, 0.456, 0.406);
        private Scalar _std = new Scalar(0.229, 0.224, 0.225);

        /// <summary>
        /// The classes.
        /// </summary>
        private List<string> _classes;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// Resolved ONNX model path for <c>MultiBackendDnn.readNet</c>.
        /// </summary>
        private string _modelFilepathOnnx;
#if OPENCV_SENTIS_AVAILABLE
        private string _modelFilepathSentis;
        /// <summary>
        /// <see cref="BackendType"/> values in <see cref="Enum.GetValues(System.Type)"/> order (sorted by underlying numeric value). Dropdown options must use the same order.
        /// </summary>
        private static readonly BackendType[] SentisBackendTypesInEnumOrder =
            (BackendType[])Enum.GetValues(typeof(BackendType));
#endif

        /// <summary>
        /// The classes filepath.
        /// </summary>
        private string _classesFilepath;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        private CancellationTokenSource _cts = new CancellationTokenSource();

        private MatSingleFlightSyncAsyncRunner _inferenceRunner;

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

            UpdateUseSentisInference();
            UpdateUseAsyncInference();
            UpdateInferenceModeToggles(inferenceReinitializing: false);

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _modelFilepathOnnx = await OpenCVEnv.GetFilePathTaskAsync(MODEL_FILENAME, cancellationToken: _cts.Token);
#if OPENCV_SENTIS_AVAILABLE
            _modelFilepathSentis = await OpenCVEnv.GetFilePathTaskAsync(StreamingAssetPathOnnxToSentisIfNeeded(MODEL_FILENAME), cancellationToken: _cts.Token);
#endif
            _classesFilepath = await OpenCVEnv.GetFilePathTaskAsync(CLASSES_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            InitializeInference();

            _multiSource2MatHelper.Initialize();
        }

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


            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("width", rgbaMat.width().ToString());
                _fpsMonitor.Add("height", rgbaMat.height().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _net, UseAsyncInference);
            }

            _rgbMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _inferenceRunner?.Cancel();

            _rgbMat?.Dispose();

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

        // Update is called once per frame
        private void Update()
        {
#if OPENCV_SENTIS_AVAILABLE
            if (_inferenceReinitializing)
                return;
#endif
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_net != null && _classes != null)
                {
                    if (_inferenceRunner != null)
                    {
                        Imgproc.cvtColor(rgbaMat, _rgbMat, Imgproc.COLOR_RGBA2RGB);

                        _inferenceRunner.SubmitWork(
                            _rgbMat,
                            syncWork: Infer,
                            asyncWork: async m =>
                            {
                                CancellationToken ct = _inferenceRunner.InFlightAsyncWorkCancellationToken;
                                return await InferAsync(m, ct);
                            });

                        if (_inferenceRunner.TryGetLatestResult(out Mat prob1x1))
                        {
                            Core.MinMaxLocResult minmax = Core.minMaxLoc(prob1x1);
                            //Debug.Log ("Best match " + (int)minmax.maxLoc.x);
                            //Debug.Log ("Best match class " + classes [(int)minmax.maxLoc.x]);
                            //Debug.Log ("Probability: " + minmax.maxVal * 100 + "%");

                            //Imgproc.putText (rgbaMat, "Best match class " + classes [(int)minmax.maxLoc.x], new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                            if (_fpsMonitor != null)
                            {
                                _fpsMonitor.ConsoleText = "Best match class " + _classes[(int)minmax.maxLoc.x];
                            }
                        }
                    }
                }

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        // Unity Lifecycle Methods
        private async void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();
            _multiSource2MatHelper = null;

            _cts?.Cancel();

            await DisposeInferenceAsync();

            _cts?.Dispose();
            _cts = null;

            OpenCVDebug.SetDebugMode(false);
        }

        // Public Methods
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

            InitializeInference();

            if (_fpsMonitor != null)
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _net, UseAsyncInference);

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
            if (newBackend == SentisBackendType)
                return;

            _inferenceReinitializing = true;
            UpdateInferenceModeToggles(inferenceReinitializing: true);

            await DisposeInferenceAsync();

            SentisBackendType = newBackend;
            UpdateUseSentisInference();
            UpdateUseAsyncInference();

            InitializeInference();

            if (_fpsMonitor != null)
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _net, UseAsyncInference);

            _inferenceReinitializing = false;
            UpdateInferenceModeToggles(inferenceReinitializing: false);
#endif
        }

        /// <summary>
        /// Raises the use async inference toggle value changed event.
        /// </summary>
        public void OnUseAsyncInferenceToggleValueChanged()
        {
#if OPENCV_SENTIS_AVAILABLE
            if (_inferenceReinitializing)
                return;
#endif
            if (UseAsyncInferenceToggle == null)
                return;
            if (UseAsyncInferenceToggle.isOn != UseAsyncInference)
            {
                if (_inferenceRunner != null)
                    _inferenceRunner.UseAsyncWork = UseAsyncInferenceToggle.isOn;
                UseAsyncInference = UseAsyncInferenceToggle.isOn;
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _net, UseAsyncInference);
            }
        }

        // Private Methods
        /// <summary>
        /// Updates <paramref name="fpsMonitor"/> with dnn backend and target for a
        /// <see cref="MultiBackendNet"/> created via <see cref="MultiBackendDnn.readNet"/>, using
        /// <see cref="MultiBackendDnn.GetBackendDisplayString(int)"/> / <see cref="MultiBackendDnn.GetTargetDisplayString(int)"/>;
        /// and the async mode flag (see <see cref="MatSingleFlightSyncAsyncRunner"/> in <c>ImageClassificationMobilenetExample</c>).
        /// </summary>
        private static void UpdateFpsMonitorInferenceInfo(FpsMonitor fpsMonitor, MultiBackendNet net, bool useAsyncInference)
        {
            if (fpsMonitor == null)
                return;

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
                fpsMonitor.Add("dnnBackend", MultiBackendDnn.GetBackendDisplayString(be));
                fpsMonitor.Add("dnnTarget", MultiBackendDnn.GetTargetDisplayString(tgt));
            }
            else
            {
                fpsMonitor.Add("dnnBackend", "-");
                fpsMonitor.Add("dnnTarget", "-");
            }
            fpsMonitor.Add("useAsyncInference", useAsyncInference.ToString());
        }

        /// <summary>
        /// Matches <c>cv::dnn::blobFromImage</c> with <c>crop=true</c>: scale so the image covers <see cref="_inputSize"/>, center-crop, then NCHW <c>CV_32F</c> with scale <c>1/255</c> (mean applied later in <see cref="Infer"/>).
        /// On non-UWP, delegates to <c>Dnn.blobFromImage</c>; on UWP, builds the blob without <c>OpenCVForUnity.DnnModule</c>.
        /// </summary>
        private Mat CreatePpResnetBlobFromRgb(Mat rgbInput)
        {
#if !UNITY_WSA_10_0
            return Dnn.blobFromImage(rgbInput, 1.0 / 255.0, _inputSize, Scalar.all(0), false, true, CvType.CV_32F);
#else
            int h = (int)_inputSize.height;
            int w = (int)_inputSize.width;
            int hw = h * w;
            using (Mat rgb224_8u = new Mat(h, w, CvType.CV_8UC3))
            {
                LetterboxCenterCropRgb8uTo(rgbInput, _inputSize, rgb224_8u);
                using (Mat floatHwc = new Mat(h, w, CvType.CV_32FC3))
                {
                    rgb224_8u.convertTo(floatHwc, CvType.CV_32F, 1.0 / 255.0, 0.0);
                    var spl = new List<Mat>();
                    Core.split(floatHwc, spl);
                    try
                    {
                        using (Mat blobFlat = new Mat(1, 3 * hw, CvType.CV_32FC1))
                        {
                            spl[0].reshape(1, new int[] { 1, hw }).copyTo(blobFlat.colRange(0, hw));
                            spl[1].reshape(1, new int[] { 1, hw }).copyTo(blobFlat.colRange(hw, 2 * hw));
                            spl[2].reshape(1, new int[] { 1, hw }).copyTo(blobFlat.colRange(2 * hw, 3 * hw));
                            using (Mat blobView = blobFlat.reshape(1, new int[] { 1, 3, h, w }))
                                return blobView.clone();
                        }
                    }
                    finally
                    {
                        for (int i = 0; i < spl.Count; i++)
                            spl[i]?.Dispose();
                        spl.Clear();
                    }
                }
            }
#endif
        }

        /// <summary>
        /// RGB 8-bit: scale so the frame fully covers <paramref name="dstSize"/>, center-crop to <paramref name="dstSize"/> (same geometry as <c>cv::dnn::blobFromImage(..., crop=true)</c>).
        /// </summary>
        private static void LetterboxCenterCropRgb8uTo(Mat srcRgb8u, Size dstSize, Mat dstRgb8uOut)
        {
            int tw = (int)dstSize.width;
            int th = (int)dstSize.height;
            int sw = srcRgb8u.cols();
            int sh = srcRgb8u.rows();
            if (sw <= 0 || sh <= 0 || tw <= 0 || th <= 0)
                return;
            double scale = Math.Max(tw / (double)sw, th / (double)sh);
            int newW = (int)Math.Round(sw * scale);
            int newH = (int)Math.Round(sh * scale);
            using (Mat resized = new Mat())
            {
                Imgproc.resize(srcRgb8u, resized, new Size(newW, newH));
                int x0 = Math.Max(0, (newW - tw) / 2);
                int y0 = Math.Max(0, (newH - th) / 2);
                using (Mat roi = new Mat(resized, new OpenCVForUnity.CoreModule.Rect(x0, y0, tw, th)))
                    roi.copyTo(dstRgb8uOut);
            }
        }

        /// <summary>
        /// Awaits <see cref="MatSingleFlightSyncAsyncRunner.DisposeAsync"/>, then disposes forward output blobs and the <see cref="MultiBackendNet"/> (used from <see cref="OnDestroy"/> and when switching backends).
        /// </summary>
        private async Task DisposeInferenceAsync()
        {
            if (_inferenceRunner != null)
                await _inferenceRunner.DisposeAsync();
            _inferenceRunner = null;

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
        /// Initializes inference from the resolved model path and current backend settings.
        /// Loads class names, constructs <see cref="MultiBackendNet"/>, and creates <see cref="MatSingleFlightSyncAsyncRunner"/> like <c>ImageClassificationMobilenetExample</c>.
        /// </summary>
        private void InitializeInference()
        {
#if UNITY_WSA_10_0 && !OPENCV_SENTIS_AVAILABLE
            const string uwpSentisRequired =
                "ImageClassificationPPResnetExample: Universal Windows Platform (UNITY_WSA_10_0) requires Sentis.";
            Debug.LogError(uwpSentisRequired);
            if (_fpsMonitor != null)
                _fpsMonitor.Toast(uwpSentisRequired, 20000);
            return;
#else
            _classes = ReadClassNames(_classesFilepath);
            if (_classes == null)
            {
                Debug.LogError(CLASSES_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("classes file is not loaded.\nPlease read console message.", 20000);
                }
            }

            string modelPath = _modelFilepathOnnx;
#if OPENCV_SENTIS_AVAILABLE
            if (UseSentisInference)
                modelPath = _modelFilepathSentis;
#endif

            if (string.IsNullOrEmpty(modelPath))
            {
                Debug.LogError(MODEL_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("model file is not loaded.\nPlease read console message.", 20000);
                }
                return;
            }

            try
            {
                _net = MultiBackendDnn.readNet(modelPath);
#if OPENCV_SENTIS_AVAILABLE
                if (UseSentisInference)
                {
                    _net.setPreferableBackend(MultiBackendDnn.DNN_BACKEND_UNITY_SENTIS);
                    _net.setPreferableTarget((int)SentisBackendType);
                }
                else
#endif
                {
#if !UNITY_WSA_10_0
                    _net.setPreferableBackend(Dnn.DNN_BACKEND_OPENCV);
                    _net.setPreferableTarget(Dnn.DNN_TARGET_CPU);
#endif
                }
                _unconnectedOutLayerNames = _net.getUnconnectedOutLayersNames();

                _inferenceRunner = new MatSingleFlightSyncAsyncRunner(
                    useAsyncWork: UseAsyncInference,
                    asyncWorkCancellationToken: _cts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to load PPResNet model: " + ex.Message);
                _inferenceRunner = null;
                _net?.Dispose();
                _net = null;
                _unconnectedOutLayerNames = null;
            }
#endif
        }

        /// <summary>
        /// Synchronous <see cref="MultiBackendNet"/> forward: preprocess <paramref name="rgbInput"/> and return
        /// <c>prob.reshape(1, 1).clone()</c> from the first unconnected output blob (same as
        /// <c>ImageClassificationMobilenetExample.Infer</c>).
        /// </summary>
        private Mat Infer(Mat rgbInput)
        {
            Mat blob = CreatePpResnetBlobFromRgb(rgbInput); // NCHW, RGB; UWP path avoids DnnModule

            int c = 3;
            int h = (int)_inputSize.height;
            int w = (int)_inputSize.width;

            using (Mat blob_CxHxW = blob.reshape(1, new int[] { c, h, w })) // [c, h, w]
            {
                for (int i = 0; i < c; ++i)
                {
                    using (Mat blob_CxHxW_row = blob_CxHxW.row(i))
                    using (Mat blob_1xHW = blob_CxHxW_row.reshape(1, 1)) // [1, h, w] => [1, h * w]
                    {
                        Core.subtract(blob_1xHW, (_mean.val[i], 0, 0, 0), blob_1xHW);
                        Core.divide(blob_1xHW, (_std.val[i], 0, 0, 0), blob_1xHW);
                    }
                }
            }

            _net.setInput(blob);
            _net.forward(_forwardOutputBlobs, _unconnectedOutLayerNames);
            Mat prob = _forwardOutputBlobs[0];

            Mat result = prob.reshape(1, 1).clone();
            blob.Dispose();
            return result;
        }

        /// <summary>
        /// Offloads <see cref="Infer"/> to a thread-pool task (OpenCV DNN), matching the OpenCV fallback in
        /// <c>MediaPipeHandLandmarker.RunCoreProcessingTaskAsync</c>.
        /// </summary>
        private async Task<Mat> InferAsync(Mat rgbInput, CancellationToken cancellationToken)
        {

#if OPENCV_SENTIS_AVAILABLE
            if (_net.UsesSentis)
            {
                Mat blob = CreatePpResnetBlobFromRgb(rgbInput);

                int c = 3;
                int h = (int)_inputSize.height;
                int w = (int)_inputSize.width;

                using (Mat blob_CxHxW = blob.reshape(1, new int[] { c, h, w })) // [c, h, w]
                {
                    for (int i = 0; i < c; ++i)
                    {
                        using (Mat blob_CxHxW_row = blob_CxHxW.row(i))
                        using (Mat blob_1xHW = blob_CxHxW_row.reshape(1, 1)) // [1, h, w] => [1, h * w]
                        {
                            Core.subtract(blob_1xHW, (_mean.val[i], 0, 0, 0), blob_1xHW);
                            Core.divide(blob_1xHW, (_std.val[i], 0, 0, 0), blob_1xHW);
                        }
                    }
                }

                _net.setInput(blob);
                await _net.forwardTaskAsync(_forwardOutputBlobs, _unconnectedOutLayerNames, cancellationToken);
                Mat prob = _forwardOutputBlobs[0];

                Mat result = prob.reshape(1, 1).clone();
                blob.Dispose();
                return result;
            }
#endif

            cancellationToken.ThrowIfCancellationRequested();
#if UNITY_WEBGL && !UNITY_EDITOR
            return Infer(rgbInput);
#else
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Infer(rgbInput);
            }, cancellationToken);
#endif
        }

        /// <summary>
        /// On Universal Windows Platform (<c>UNITY_WSA_10_0</c>) with <c>OPENCV_SENTIS_AVAILABLE</c>, sets <see cref="UseSentisInference"/> to <see langword="true"/> (OpenCV DNN is unavailable).
        /// When <c>OPENCV_SENTIS_AVAILABLE</c>, if <see cref="SystemInfo.supportsComputeShaders"/> is <see langword="false"/> and <see cref="SentisBackendType"/> is <see cref="BackendType.GPUCompute"/>, sets <see cref="SentisBackendType"/> to <see cref="BackendType.GPUPixel"/>.
        /// </summary>
        private void UpdateUseSentisInference()
        {
#if UNITY_WSA_10_0 && OPENCV_SENTIS_AVAILABLE
            UseSentisInference = true;
#endif
#if OPENCV_SENTIS_AVAILABLE
            if (!SystemInfo.supportsComputeShaders && SentisBackendType == BackendType.GPUCompute)
                SentisBackendType = BackendType.GPUPixel;
#endif
        }

        /// <summary>
        /// Reserved hook for synchronizing <see cref="UseAsyncInference"/> with platform capabilities.
        /// Does not modify <see cref="UseAsyncInference"/> in this example.
        /// </summary>
        private void UpdateUseAsyncInference()
        {
        }

        private List<string> ReadClassNames(string filename)
        {
            List<string> classNames = new List<string>();

            System.IO.StreamReader cReader = null;
            try
            {
                cReader = new System.IO.StreamReader(filename, System.Text.Encoding.Default);

                while (cReader.Peek() >= 0)
                {
                    string name = cReader.ReadLine();
                    classNames.Add(name);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
                return null;
            }
            finally
            {
                if (cReader != null)
                    cReader.Close();
            }

            return classNames;
        }

        /// <summary>
        /// Updates async inference and (when <c>OPENCV_SENTIS_AVAILABLE</c>) Sentis toggle interactability and visible state to match
        /// the current <see cref="UseAsyncInference"/> / <see cref="UseSentisInference"/> (UI only; call
        /// <see cref="UpdateUseSentisInference"/> first so field values are up to date).
        /// On Universal Windows Platform (<c>UNITY_WSA_10_0</c>), the Sentis inference toggle is forced on and not interactable; the backend dropdown stays enabled.
        /// When <c>OPENCV_SENTIS_AVAILABLE</c> and not re-initializing on other platforms, also calls <see cref="UpdateSentisBackendDropdown"/>, keeps the Sentis inference toggle interactive, and sets the backend dropdown interactability from <see cref="UseSentisInference"/>.
        /// </summary>
        /// <param name="inferenceReinitializing">
        /// When <see langword="true"/>, inference is re-initializing: Sentis and async inference controls are disabled.
        /// When <see langword="false"/> after completion (or at startup), normal enable/disable and visible state sync apply.
        /// </param>
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
#if UNITY_WSA_10_0
            if (UseSentisInferenceToggle != null)
            {
                UseSentisInferenceToggle.SetIsOnWithoutNotify(true);
                UseSentisInferenceToggle.interactable = false;
            }
            if (SentisBackendDropdown != null)
                SentisBackendDropdown.interactable = true;
            UpdateSentisBackendDropdown();
#else
            if (UseSentisInferenceToggle != null)
            {
                UseSentisInferenceToggle.SetIsOnWithoutNotify(UseSentisInference);
                UseSentisInferenceToggle.interactable = true;
            }
            if (SentisBackendDropdown != null)
                SentisBackendDropdown.interactable = UseSentisInference;
            UpdateSentisBackendDropdown();
#endif
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
        /// <summary>
        /// Aligns the dropdown with <see cref="SentisBackendType"/> without raising change events. Option order must match <see cref="SentisBackendTypesInEnumOrder"/>.
        /// </summary>
        private void UpdateSentisBackendDropdown()
        {
            if (SentisBackendDropdown == null || SentisBackendDropdown.options.Count == 0)
                return;
            if (SentisBackendTypesInEnumOrder.Length == 0)
                return;
            int idx = Array.IndexOf(SentisBackendTypesInEnumOrder, SentisBackendType);
            if (idx < 0)
                idx = 0;
            int maxIdx = Mathf.Min(SentisBackendDropdown.options.Count, SentisBackendTypesInEnumOrder.Length) - 1;
            SentisBackendDropdown.SetValueWithoutNotify(Mathf.Clamp(idx, 0, maxIdx));
        }

        /// <summary>
        /// When using Sentis: if the StreamingAssets-relative path ends with <c>.onnx</c>, replace it with <c>.sentis</c>.
        /// </summary>
        private static string StreamingAssetPathOnnxToSentisIfNeeded(string streamingAssetsRelativePath)
        {
            if (string.IsNullOrEmpty(streamingAssetsRelativePath))
                return streamingAssetsRelativePath;
            if (!streamingAssetsRelativePath.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase))
                return streamingAssetsRelativePath;
            return Path.ChangeExtension(streamingAssetsRelativePath, ".sentis");
        }
#endif
    }
}

#endif
