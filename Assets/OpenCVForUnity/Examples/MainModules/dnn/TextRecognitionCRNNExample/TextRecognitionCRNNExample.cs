#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.Extensions.Runner;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.GeometryModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using OpenCVForUnity.UtilsModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenCVDebug = OpenCVForUnity.Extensions.OpenCVDebug;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Text Recognition CRNN Example
    /// Detects text regions with PPOCR and recognizes strings with CRNN on each input frame.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Loading PPOCR detection and CRNN recognition models from StreamingAssets
    /// - Resizing to 736x736, polygon detection, Net-based CTC-greedy recognition, and coordinate restore
    /// - Optional async inference via MatSingleFlightSyncAsyncRunner
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Size"/>, <see cref="Scalar"/>, <see cref="Point"/>
    /// - <see cref="TextDetectionModel_DB"/>, <see cref="Net"/>, <see cref="Dnn"/>
    /// - <see cref="Imgproc"/>: resize, cvtColor, warpPerspective, polylines
    /// - <see cref="MatSingleFlightSyncAsyncRunner"/>
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://github.com/opencv/opencv_zoo/tree/master/models/text_detection_ppocr
    /// https://github.com/opencv/opencv_zoo/tree/master/models/text_recognition_crnn
    /// https://docs.opencv.org/4.x/d4/d43/tutorial_dnn_text_spotting.html
    /// </para>
    /// <para>
    /// [Tested Models]
    /// https://huggingface.co/opencv/text_detection_ppocr/resolve/main/text_detection_en_ppocrv3_2023may.onnx
    /// https://github.com/opencv/opencv_zoo/raw/8a42017a12fe9ed80279737c0b903307371b0e3d/models/text_recognition_crnn/text_recognition_CRNN_EN_2021sep.onnx
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class TextRecognitionCRNNExample : MonoBehaviour
    {
        // Constants
        private const float DETECTION_INPUT_SIZE_W = 736f;
        private const float DETECTION_INPUT_SIZE_H = 736f;
#if UNITY_6000_5_OR_NEWER
        [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
        private static readonly Scalar DETECTION_INPUT_MEAN = new Scalar(123.675, 116.28, 103.53);
#if UNITY_6000_5_OR_NEWER
        [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
        private static readonly Scalar DETECTION_INPUT_SCALE = new Scalar(
            1.0 / (255.0 * 0.229),
            1.0 / (255.0 * 0.224),
            1.0 / (255.0 * 0.225));
        private const float DETECTION_BINARY_THRESHOLD = 0.3f;
        private const float DETECTION_POLYGON_THRESHOLD = 0.5f;
        private const int DETECTION_MAX_CANDIDATES = 200;
        private const double DETECTION_UNCLIP_RATIO = 2.0;
        private const float RECOGNITION_INPUT_SIZE_W = 100f;
        private const float RECOGNITION_INPUT_SIZE_H = 32f;
        private const string DETECTION_MODEL_FILEPATH = "OpenCVForUnityExamples/dnn/text_detection_en_ppocrv3_2023may.onnx";
        //private const string DETECTION_MODEL_FILEPATH = "OpenCVForUnityExamples/dnn/text_detection_cn_ppocrv3_2023may.onnx";
        private const string RECOGNITION_MODEL_FILEPATH = "OpenCVForUnityExamples/dnn/text_recognition_CRNN_EN_2021sep.onnx";
        //private const string RECOGNITION_MODEL_FILEPATH = "OpenCVForUnityExamples/dnn/text_recognition_CRNN_CN_2021nov.onnx";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Header("UI")]
        public Toggle UseAsyncInferenceToggle;
        public bool UseAsyncInference = true;

        [Space(10)]

        // Private Fields
        private string _detectionModelFilepath;
        private string _recognitionModelFilepath;
        private PpOcrTextDetector _detector;
        private CrnnNetRecognizer _recognizer;
        private Texture2D _texture;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private MatSingleFlightSyncAsyncRunner _inferenceRunner;

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.BGR; // PPOCR detection and CRNN recognition models require a 3-channel BGR Mat.

            WireSourceToMatControlPanelHooks();

            UpdateUseAsyncInference();
            UpdateInferenceModeToggles(inferenceReinitializing: false);

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            _detectionModelFilepath = await OpenCVForUnityEnv.GetFilePathAsync(DETECTION_MODEL_FILEPATH, cancellationToken: _cts.Token);
            _recognitionModelFilepath = await OpenCVForUnityEnv.GetFilePathAsync(RECOGNITION_MODEL_FILEPATH, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            // Load PPOCR and CRNN models from StreamingAssets and create inference runner.
            if (!TryInitializeInference())
            {
                return;
            }

            OpenCVDebug.SetDebugMode(false);

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
            if (!_multiSourceToMatHelper.IsPlaying)
            {
                return;
            }

            Mat bgrMat = _multiSourceToMatHelper.FrameMat;

            if (_detector != null && _recognizer != null)
            {
                if (_inferenceRunner != null)
                {
                    // Submit sync or async PPOCR+CRNN pipeline on the BGR frame.
                    _inferenceRunner.SubmitWork(
                        bgrMat,
                        syncWork: Infer,
                        asyncWork: async m =>
                        {
                            CancellationToken ct = _inferenceRunner.InFlightAsyncWorkCancellationToken;
                            return await InferAsync(m, ct);
                        });

                    if (_inferenceRunner.TryGetLatestResult(out Mat[] inferMats))
                    {
                        Visualize(bgrMat, inferMats, printResult: false, isRGB: false);
                    }
                }
                else
                {
                    Mat[] inferMats = Infer(bgrMat);
                    Visualize(bgrMat, inferMats, printResult: false, isRGB: false);
                    foreach (Mat m in inferMats)
                    {
                        m.Dispose();
                    }
                }
            }

            // Convert annotated BGR Mat to RGB for Unity texture upload.
            Imgproc.cvtColor(bgrMat, bgrMat, Imgproc.COLOR_BGR2RGB);

            OpenCVMatUnityUtils.MatToTexture2D(bgrMat, _texture);
        }

        /// <summary>
        /// Raises the helper initialized event.
        /// Recreates the preview texture and starts playback on first initialization.
        /// Skips Play when re-initialization has already restored Playing or Paused.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized", this);

            Mat bgrMat = _multiSourceToMatHelper.FrameMat;

            // Fill in the image so that the unprocessed image is not displayed.
            bgrMat.setTo(new Scalar(0, 0, 0, 255));

            RecreatePreviewTexture();

#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API
            // If the WebCam is front facing, flip the Mat horizontally. Required for successful detection.
            if (_multiSourceToMatHelper.ActiveHelper is WebCamTextureToMatHelper webCamHelper)
            {
                _multiSourceToMatHelper.FlipHorizontal = webCamHelper.IsFrontFacing;
            }
#endif

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
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, UseAsyncInference);
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

            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed", this);

            _inferenceRunner?.Cancel();

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
        /// Raises the use async inference toggle value changed event.
        /// </summary>
        public void OnUseAsyncInferenceToggleValueChanged()
        {
            if (UseAsyncInferenceToggle != null && UseAsyncInferenceToggle.isOn != UseAsyncInference)
            {
                if (_inferenceRunner != null)
                {
                    _inferenceRunner.UseAsyncWork = UseAsyncInferenceToggle.isOn;
                }

                UseAsyncInference = UseAsyncInferenceToggle.isOn;
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, UseAsyncInference);
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

            _texture = new Texture2D(frameMat.cols(), frameMat.rows(), TextureFormat.RGB24, false);
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
        /// Creates text detection / recognition models and <see cref="MatSingleFlightSyncAsyncRunner"/>
        /// (same role as <see cref="FaceDetectionYuNetV2Example.InitializeInference"/>).
        /// </summary>
        private bool TryInitializeInference()
        {
            if (string.IsNullOrEmpty(_detectionModelFilepath) || string.IsNullOrEmpty(_recognitionModelFilepath))
            {
                Debug.LogError(DETECTION_MODEL_FILEPATH + " or " + RECOGNITION_MODEL_FILEPATH + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "model file is not loaded.\nPlease read console message.";
                }
                return false;
            }

            try
            {
                _detector = new PpOcrTextDetector(_detectionModelFilepath);
                _recognizer = new CrnnNetRecognizer(_recognitionModelFilepath);

                _inferenceRunner = new MatSingleFlightSyncAsyncRunner(
                    useAsyncWork: UseAsyncInference,
                    asyncWorkCancellationToken: _cts.Token);

                return _detector != null && _recognizer != null && _inferenceRunner != null;
            }
            catch (Exception ex)
            {
                Debug.LogError("TextRecognitionCRNNExample TryInitializeInference failed: " + ex, this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "Failed to initialize inference.\nPlease read console message.";
                }

                if (_detector != null)
                {
                    _detector.Dispose();
                    _detector = null;
                }

                if (_recognizer != null)
                {
                    _recognizer.Dispose();
                    _recognizer = null;
                }

                _inferenceRunner = null;
                return false;
            }
        }

        /// <summary>
        /// Reserved hook for synchronizing <see cref="UseAsyncInference"/> with platform capabilities.
        /// Does not modify <see cref="UseAsyncInference"/> in this example.
        /// </summary>
        private void UpdateUseAsyncInference()
        {
        }

        /// <summary>
        /// Updates the async inference toggle interactability and visible state.
        /// </summary>
        /// <param name="inferenceReinitializing">When <see langword="true"/>, disables the toggle while inference is re-initializing.</param>
        private void UpdateInferenceModeToggles(bool inferenceReinitializing)
        {
            if (inferenceReinitializing)
            {
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
        }

        /// <summary>
        /// Awaits <see cref="MatSingleFlightSyncAsyncRunner.DisposeAsync"/> then disposes DNN models.
        /// </summary>
        private async Task DisposeInferenceAsync()
        {
            var runner = _inferenceRunner;
            _inferenceRunner = null;
            if (runner != null)
            {
                await runner.DisposeAsync();
            }

            var detector = _detector;
            _detector = null;
            var recognizer = _recognizer;
            _recognizer = null;
            detector?.Dispose();
            recognizer?.Dispose();
        }

        /// <summary>
        /// Draws text detection and recognition results from a <see cref="Mat"/> array whose layout matches
        /// <see cref="Infer"/>.
        /// <c>results[0]</c> is packed polygon detections (<c>rows x 4</c>, <see cref="CvType.CV_32SC2"/>),
        /// <c>results[1]</c> is confidences (<see cref="MatOfFloat"/>), and <c>results[2]</c> is packed recognition strings.
        /// </summary>
        /// <param name="image">Destination image for visualization.</param>
        /// <param name="results">Output matrices from <see cref="Infer"/> (length at least 3).</param>
        /// <param name="printResult">If true, prints the decoded result to the console.</param>
        /// <param name="isRGB">If true, treats <paramref name="image"/> as RGB instead of BGR for drawing colors.</param>
        private void Visualize(Mat image, Mat[] results, bool printResult = false, bool isRGB = false)
        {
            if (image != null)
            {
                image.ThrowIfDisposed();
            }

            if (results == null || results.Length < 3)
            {
                return;
            }

            Mat detectionsMat = results[0];
            Mat confidencesMat = results[1];
            Mat recognitionsMat = results[2];

            if (detectionsMat == null || detectionsMat.empty() || detectionsMat.rows() == 0
                || confidencesMat == null || confidencesMat.empty())
            {
                return;
            }

            int detectionCount = detectionsMat.rows();
            float[] confidencesArr = new MatOfFloat(confidencesMat).toArray();

            List<string> recognitionList = new List<string>();
            if (recognitionsMat != null && !recognitionsMat.empty())
            {
                Converters.Mat_to_vector_string(recognitionsMat, recognitionList);
            }

            while (recognitionList.Count < detectionCount)
            {
                recognitionList.Add(string.Empty);
            }

            Array.Reverse(confidencesArr);
            recognitionList.Reverse();

            Scalar BgrScalarForImage(Scalar bgr)
            {
                if (!isRGB)
                {
                    return bgr;
                }

                return new Scalar(bgr.val[2], bgr.val[1], bgr.val[0]);
            }

            Scalar colorGreen = BgrScalarForImage(new Scalar(0, 255, 0));
            Scalar colorRed = BgrScalarForImage(new Scalar(0, 0, 255));

            StringBuilder sb = new StringBuilder(1024);
            for (int i = 0; i < detectionCount; ++i)
            {
                int rowIndex = detectionCount - 1 - i;
                float confidence = rowIndex < confidencesArr.Length ? confidencesArr[rowIndex] : 0f;

                Point[] vertices = GetPolygonVertices(detectionsMat, rowIndex);
                using MatOfPoint contour = new MatOfPoint(vertices);
                Imgproc.polylines(image, new List<MatOfPoint> { contour }, true, colorGreen, 2);

                string recognitionText = recognitionList[i] ?? string.Empty;
                if (vertices.Length > 1)
                {
                    Imgproc.putText(image, recognitionText, vertices[1], Imgproc.FONT_HERSHEY_SIMPLEX, 0.8, colorRed, 2, Imgproc.LINE_AA, false);
                }

                sb.Append("[").Append(recognitionText).Append("] ").Append(confidence).AppendLine();
            }

            if (printResult)
            {
                Debug.Log(sb.ToString(), this);
            }
        }

        /// <summary>
        /// Runs text detection and recognition; returns Mats in order: detections, confidences, recognitions.
        /// </summary>
        /// <returns>Index 0: packed polygon detections (rows x 4, CV_32SC2). Index 1: MatOfFloat. Index 2: recognition strings.</returns>
        private Mat[] Infer(Mat img)
        {
            Mat resizedMat = new Mat();
            Size detectionInputSize = new Size(DETECTION_INPUT_SIZE_W, DETECTION_INPUT_SIZE_H);

            try
            {
                double scaleWidth = img.cols() / detectionInputSize.width;
                double scaleHeight = img.rows() / detectionInputSize.height;

                Imgproc.resize(img, resizedMat, detectionInputSize);

                Mat[] detectionResult = _detector.Infer(resizedMat);
                Mat polygons = detectionResult[0];
                Mat confidences = detectionResult[1];

                int detectionCount = polygons.rows();
                List<string> recognitionStrings = new List<string>(detectionCount);
                for (int k = 0; k < detectionCount; k++)
                {
                    recognitionStrings.Add(string.Empty);
                }

                for (int i = 0; i < detectionCount; ++i)
                {
                    using Mat boxMat = polygons.row(i).reshape(2, 4);

                    using Mat outputBlob = _recognizer.Infer(resizedMat, boxMat);
                    recognitionStrings[i] = _recognizer.Decode(outputBlob);
                }

                ScalePolygonsInPlace(polygons, scaleWidth, scaleHeight);

                Mat recognitionsMat = recognitionStrings.Count > 0
                    ? Converters.vector_string_to_Mat(recognitionStrings)
                    : new Mat(1, 0, CvType.CV_8UC1);

                return new Mat[] { polygons, confidences, recognitionsMat };
            }
            finally
            {
                resizedMat.Dispose();
            }
        }

        /// <summary>
        /// Offloads <see cref="Infer"/> to a thread-pool task (OpenCV DNN), matching
        /// <see cref="ImageClassificationMobilenetExample.InferAsync"/>.
        /// </summary>
        private async Task<Mat[]> InferAsync(Mat img, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if UNITY_WEBGL && !UNITY_EDITOR
            return await Task.FromResult(Infer(img));
#else
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Infer(img);
            }, cancellationToken);
#endif
        }

        /// <summary>
        /// Reads four polygon vertices from a packed detections <see cref="Mat"/> row.
        /// </summary>
        private static Point[] GetPolygonVertices(Mat packedPolygons, int rowIndex)
        {
            ReadOnlySpan<int> row = packedPolygons.AsSpan<int>(rowIndex);
            Point[] vertices = new Point[4];
            for (int j = 0; j < 4; j++)
            {
                int baseIdx = j * 2;
                vertices[j] = new Point(row[baseIdx], row[baseIdx + 1]);
            }

            return vertices;
        }

        /// <summary>
        /// Scales packed polygon coordinates in place from detector input space to the original image space.
        /// </summary>
        private static void ScalePolygonsInPlace(Mat polygons, double scaleWidth, double scaleHeight)
        {
            if (polygons == null || polygons.empty() || polygons.rows() == 0)
            {
                return;
            }

            Span<int> coords = polygons.AsSpan<int>();
            for (int i = 0; i < coords.Length; i += 2)
            {
                coords[i] = (int)(coords[i] * scaleWidth);
                coords[i + 1] = (int)(coords[i + 1] * scaleHeight);
            }
        }

        /// <summary>
        /// Updates <paramref name="fpsMonitor"/> with dnn backend, target, and async mode.
        /// </summary>
        private static void UpdateFpsMonitorInferenceInfo(FpsMonitor fpsMonitor, bool useAsyncInference)
        {
            if (fpsMonitor == null)
            {
                return;
            }

            fpsMonitor.Add("dnnBackend", "OPENCV");
            fpsMonitor.Add("dnnTarget", "CPU");
            fpsMonitor.Add("useAsyncInference", useAsyncInference.ToString());
        }

        /// <summary>
        /// PPOCR text detector matching opencv_zoo demo.py (<c>ppocr_det.PPOCRDet</c>).
        /// </summary>
        private sealed class PpOcrTextDetector : IDisposable
        {
            private readonly TextDetectionModel_DB _model;
            private readonly Size _inputSize;

            public PpOcrTextDetector(string modelPath)
            {
                _inputSize = new Size(DETECTION_INPUT_SIZE_W, DETECTION_INPUT_SIZE_H);
                _model = new TextDetectionModel_DB(modelPath);
                _model.setBinaryThreshold(DETECTION_BINARY_THRESHOLD);
                _model.setPolygonThreshold(DETECTION_POLYGON_THRESHOLD);
                _model.setUnclipRatio(DETECTION_UNCLIP_RATIO);
                _model.setMaxCandidates(DETECTION_MAX_CANDIDATES);
                _model.setInputSize(_inputSize);
                _model.setInputMean(DETECTION_INPUT_MEAN);
                _model.setInputScale(DETECTION_INPUT_SCALE);
            }

            public Mat[] Infer(Mat image)
            {
                if (image.cols() != _inputSize.width || image.rows() != _inputSize.height)
                {
                    throw new ArgumentException("Input image size must match detector input size.");
                }

                List<MatOfPoint> detections = new List<MatOfPoint>();
                MatOfFloat confidences = new MatOfFloat();
                _model.detect(image, detections, confidences);

                return new Mat[] { PackDetectionsToMat(detections), confidences };
            }

            private static Mat PackDetectionsToMat(List<MatOfPoint> detections)
            {
                if (detections == null || detections.Count == 0)
                {
                    return new Mat(0, 4, CvType.CV_32SC2);
                }

                Mat packed = new Mat(detections.Count, 4, CvType.CV_32SC2);
                for (int i = 0; i < detections.Count; i++)
                {
                    Mat detection = detections[i];
                    int pointCount = Math.Min((int)detection.total(), 4);
                    ReadOnlySpan<int> src = detection.AsSpan<int>();
                    Span<int> dstRow = packed.AsSpan<int>(i);
                    int copyLength = Math.Min(src.Length, pointCount * 2);
                    src.Slice(0, copyLength).CopyTo(dstRow.Slice(0, copyLength));
                }

                return packed;
            }

            public void Dispose()
            {
                _model?.Dispose();
            }
        }

        /// <summary>
        /// CRNN recognizer using OpenCV Net and manual CTC-greedy decode (demo.cpp CRNN).
        /// </summary>
        private sealed class CrnnNetRecognizer : IDisposable
        {
            private readonly Net _net;
            private readonly IReadOnlyList<string> _charset;
            private readonly Size _inputSize;
            private readonly MatOfPoint2f _targetVertices;
            private readonly bool _useGrayscaleInput;

            public CrnnNetRecognizer(string modelPath)
            {
                _inputSize = new Size(RECOGNITION_INPUT_SIZE_W, RECOGNITION_INPUT_SIZE_H);
                _net = Dnn.readNet(modelPath);
                _net.setPreferableBackend(Dnn.DNN_BACKEND_OPENCV);
                _net.setPreferableTarget(Dnn.DNN_TARGET_CPU);
                _charset = TextRecognitionCrnnCharset.GetCharsetForModel(modelPath);
                _useGrayscaleInput = modelPath.IndexOf("CN", StringComparison.Ordinal) < 0
                    && modelPath.IndexOf("CH", StringComparison.Ordinal) < 0;

                _targetVertices = new MatOfPoint2f(new Point[]
                {
                    new Point(0, (int)_inputSize.height - 1),
                    new Point(0, 0),
                    new Point((int)_inputSize.width - 1, 0),
                    new Point((int)_inputSize.width - 1, (int)_inputSize.height - 1),
                });
            }

            public Mat Infer(Mat image, Mat rbbox)
            {
                using Mat inputBlob = Preprocess(image, rbbox);
                _net.setInput(inputBlob);
                return _net.forward();
            }

            public string Decode(Mat outputBlob)
            {
                outputBlob?.ThrowIfDisposed();

                using Mat character = outputBlob.reshape(1, outputBlob.size(0));
                StringBuilder text = new StringBuilder(character.rows());
                for (int i = 0; i < character.rows(); ++i)
                {
                    using Mat row = character.row(i);
                    Core.MinMaxLocResult minmax = Core.minMaxLoc(row);
                    if (minmax.maxLoc.x != 0)
                    {
                        text.Append(_charset[(int)minmax.maxLoc.x - 1]);
                    }
                    else
                    {
                        text.Append('-');
                    }
                }

                StringBuilder filtered = new StringBuilder(text.Length);
                for (int i = 0; i < text.Length; ++i)
                {
                    char current = text[i];
                    if (current != '-' && !(i > 0 && current == text[i - 1]))
                    {
                        filtered.Append(current);
                    }
                }

                return filtered.ToString();
            }

            private Mat Preprocess(Mat image, Mat rbbox)
            {
                using Mat vertices = new Mat();
                rbbox.reshape(2, 4).convertTo(vertices, CvType.CV_32FC2);
                using Mat rotationMatrix = Geometry.getPerspectiveTransform(vertices, _targetVertices);
                using Mat cropped = new Mat();
                Imgproc.warpPerspective(image, cropped, rotationMatrix, _inputSize);

                Mat processed = cropped;
                Mat grayMat = null;
                if (_useGrayscaleInput)
                {
                    grayMat = new Mat();
                    Imgproc.cvtColor(cropped, grayMat, Imgproc.COLOR_BGR2GRAY);
                    processed = grayMat;
                }

                try
                {
                    return Dnn.blobFromImage(processed, 1.0 / 127.5, _inputSize, new Scalar(127.5));
                }
                finally
                {
                    grayMat?.Dispose();
                }
            }

            public void Dispose()
            {
                _net?.Dispose();
                _targetVertices?.Dispose();
            }
        }
    }
}
#endif
