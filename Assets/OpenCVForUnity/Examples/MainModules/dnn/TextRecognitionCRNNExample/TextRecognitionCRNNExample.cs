#if !UNITY_WSA_10_0 && NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.UnityIntegration.Runner;
using OpenCVForUnity.UnityIntegration.Worker.DnnModule;
using OpenCVForUnity.UtilsModule;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static OpenCVForUnity.UnityIntegration.Helper.Source2Mat.MultiSource2MatHelper;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Text Recognition CRNN Example
    /// This example demonstrates text detection and recognition model using the TextDetectionMode and TextRecognitionModel class.
    /// Referring to:
    /// https://github.com/opencv/opencv_zoo/tree/master/models/text_detection_db
    /// https://github.com/opencv/opencv_zoo/tree/master/models/text_recognition_crnn
    /// https://docs.opencv.org/4.x/d4/d43/tutorial_dnn_text_spotting.html
    ///
    /// [Tested Models]
    /// https://github.com/opencv/opencv_zoo/raw/6a66e0d6e47a693e6d0dd01bbb18e920f3fbae75/models/text_detection_db/text_detection_DB_IC15_resnet18_2021sep.onnx
    /// https://github.com/opencv/opencv_zoo/raw/8a42017a12fe9ed80279737c0b903307371b0e3d/models/text_recognition_crnn/text_recognition_CRNN_EN_2021sep.onnx
    /// https://github.com/opencv/opencv_zoo/raw/8a42017a12fe9ed80279737c0b903307371b0e3d/models/text_recognition_crnn/charset_36_EN.txt
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class TextRecognitionCRNNExample : MonoBehaviour
    {
        // Constants
        private const float DETECTION_INPUT_SIZE_W = 320f; // 736f;
        private const float DETECTION_INPUT_SIZE_H = 320f; // 736f;
        private const double DETECTION_INPUT_SCALE = 1.0 / 255.0;
        private const float DETECTION_BINARY_THRESHOLD = 0.3f;
        private const float DETECTION_POLYGON_THRESHOLD = 0.5f;
        private const int DETECTION_MAX_CANDIDATES = 200;
        private const double DETECTION_UNCLIP_RATIO = 2.0;
        private const float DETECTION_CONFIDENCES_THRESHOLD = 0.7f;
        private const float RECOGNITION_INPUT_SIZE_W = 100f;
        private const float RECOGNITION_INPUT_SIZE_H = 32f;
        private const double RECOGNITION_INPUT_SCALE = 1.0 / 127.5;
        private static readonly string DETECTION_MODEL_FILENAME = "OpenCVForUnityExamples/dnn/text_detection_DB_IC15_resnet18_2021sep.onnx";
        private static readonly string RECOGNITION_MODEL_FILENAME = "OpenCVForUnityExamples/dnn/text_recognition_CRNN_EN_2021sep.onnx";
        private static readonly string CHARSET_TXT_FILENAME = "OpenCVForUnityExamples/dnn/charset_36_EN.txt";

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
        private Scalar _detectionInputMean = new Scalar(122.67891434, 116.66876762, 104.00698793);
        private Scalar _recognitionInputMean = new Scalar(127.5);

        private string _detectionModelFilepath;
        private string _recognitionModelFilepath;
        private string _charsetTxtFilepath;

        private TextDetectionModel_DB _detectionModel;
        private TextRecognitionModel _recognitionModel;

        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

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
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.BGR;

            UpdateUseAsyncInference();
            UpdateInferenceModeToggles(inferenceReinitializing: false);

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _detectionModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(DETECTION_MODEL_FILENAME, cancellationToken: _cts.Token);
            _recognitionModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(RECOGNITION_MODEL_FILENAME, cancellationToken: _cts.Token);
            _charsetTxtFilepath = await OpenCVEnv.GetFilePathTaskAsync(CHARSET_TXT_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat bgrMat = _multiSource2MatHelper.GetMat();

                if (_detectionModel != null && _recognitionModel != null)
                {
                    if (_inferenceRunner != null)
                    {
                        _inferenceRunner.SubmitWork(
                            bgrMat,
                            syncWork: Infer,
                            asyncWork: async m =>
                            {
                                CancellationToken ct = _inferenceRunner.InFlightAsyncWorkCancellationToken;
                                return await InferAsync(m, ct);
                            });

                        if (_inferenceRunner.TryGetLatestResult(out Mat[] inferMats))
                            Visualize(bgrMat, inferMats, printResult: false, isRGB: false);
                    }
                    else
                    {
                        Mat[] inferMats = Infer(bgrMat);
                        Visualize(bgrMat, inferMats, printResult: false, isRGB: false);
                        foreach (Mat m in inferMats)
                            m.Dispose();
                    }
                }

                Imgproc.cvtColor(bgrMat, bgrMat, Imgproc.COLOR_BGR2RGB);

                //Imgproc.putText (bgrMat, "W:" + bgrMat.width () + " H:" + bgrMat.height () + " SO:" + Screen.orientation, new Point (5, img.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                OpenCVMatUtils.MatToTexture2D(bgrMat, _texture);
            }
        }

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
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat bgrMat = _multiSource2MatHelper.GetMat();

            // Fill in the image so that the unprocessed image is not displayed.
            bgrMat.setTo(new Scalar(0, 0, 0, 255));

            _texture = new Texture2D(bgrMat.cols(), bgrMat.rows(), TextureFormat.RGB24, false);
            OpenCVMatUtils.MatToTexture2D(bgrMat, _texture);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;


            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("width", _multiSource2MatHelper.GetWidth().ToString());
                _fpsMonitor.Add("height", _multiSource2MatHelper.GetHeight().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _detectionModel, _recognitionModel, UseAsyncInference);
            }

#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API
            // If the WebCam is front facing, flip the Mat horizontally. Required for successful detection.
            if (_multiSource2MatHelper.Source2MatHelper is WebCamTexture2MatHelper webCamHelper)
                webCamHelper.FlipHorizontal = webCamHelper.IsFrontFacing();
#endif
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _inferenceRunner?.Cancel();

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
        /// Raises the use async inference toggle value changed event.
        /// </summary>
        public void OnUseAsyncInferenceToggleValueChanged()
        {
            if (UseAsyncInferenceToggle != null && UseAsyncInferenceToggle.isOn != UseAsyncInference)
            {
                if (_inferenceRunner != null)
                    _inferenceRunner.UseAsyncWork = UseAsyncInferenceToggle.isOn;
                UseAsyncInference = UseAsyncInferenceToggle.isOn;
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _detectionModel, _recognitionModel, UseAsyncInference);
            }
        }

        // Private Methods
        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            InitializeInference();

            OpenCVDebug.SetDebugMode(false);

            _multiSource2MatHelper.Initialize();
        }

        /// <summary>
        /// Creates text detection / recognition models and <see cref="MatSingleFlightSyncAsyncRunner"/>
        /// (same role as <see cref="FaceDetectionYuNetV2Example.InitializeInference"/>).
        /// </summary>
        private void InitializeInference()
        {
            if (string.IsNullOrEmpty(_detectionModelFilepath) || string.IsNullOrEmpty(_recognitionModelFilepath) || string.IsNullOrEmpty(_charsetTxtFilepath))
            {
                Debug.LogError(DETECTION_MODEL_FILENAME + " or " + RECOGNITION_MODEL_FILENAME + " or " + CHARSET_TXT_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("model file is not loaded.\nPlease read console message.", 20000);
                }
                return;
            }

            try
            {
                _detectionModel = new TextDetectionModel_DB(_detectionModelFilepath);
                _detectionModel.setBinaryThreshold(DETECTION_BINARY_THRESHOLD);
                _detectionModel.setPolygonThreshold(DETECTION_POLYGON_THRESHOLD);
                _detectionModel.setUnclipRatio(DETECTION_UNCLIP_RATIO);
                _detectionModel.setMaxCandidates(DETECTION_MAX_CANDIDATES);
                _detectionModel.setInputParams(DETECTION_INPUT_SCALE, new Size(DETECTION_INPUT_SIZE_W, DETECTION_INPUT_SIZE_H), _detectionInputMean);

                _recognitionModel = new TextRecognitionModel(_recognitionModelFilepath);
                _recognitionModel.setDecodeType("CTC-greedy");
                _recognitionModel.setVocabulary(LoadCharset(_charsetTxtFilepath));
                _recognitionModel.setInputParams(RECOGNITION_INPUT_SCALE, new Size(RECOGNITION_INPUT_SIZE_W, RECOGNITION_INPUT_SIZE_H), _recognitionInputMean);

                _inferenceRunner = new MatSingleFlightSyncAsyncRunner(
                    useAsyncWork: UseAsyncInference,
                    asyncWorkCancellationToken: _cts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("TextRecognitionCRNNExample InitializeInference failed: " + ex);
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
                    UseAsyncInferenceToggle.interactable = false;
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
            if (_inferenceRunner != null)
                await _inferenceRunner.DisposeAsync();
            _inferenceRunner = null;

            _detectionModel?.Dispose();
            _detectionModel = null;
            _recognitionModel?.Dispose();
            _recognitionModel = null;
        }

        /// <summary>
        /// Draws text detection and recognition results from a <see cref="Mat"/> array whose layout matches
        /// <see cref="Infer"/>.
        /// <c>results[0]</c> is detections (<see cref="MatOfRotatedRect"/>), <c>results[1]</c> is confidences
        /// (<see cref="MatOfFloat"/>), and <c>results[2]</c> is packed recognition strings (UTF-8 with separator).
        /// </summary>
        /// <param name="image">Destination image for visualization.</param>
        /// <param name="results">Output matrices from <see cref="Infer"/> (length at least 3).</param>
        /// <param name="printResult">If true, prints the decoded result to the console.</param>
        /// <param name="isRGB">If true, treats <paramref name="image"/> as RGB instead of BGR for drawing colors.</param>
        private void Visualize(Mat image, Mat[] results, bool printResult = false, bool isRGB = false)
        {
            if (image != null)
                image.ThrowIfDisposed();
            if (results == null || results.Length < 3)
                return;

            Mat detectonsMat = results[0];
            Mat confidencesMat = results[1];
            Mat recognitionsMat = results[2];

            if (detectonsMat == null || detectonsMat.empty()
                || confidencesMat == null || confidencesMat.empty()
                || recognitionsMat == null || recognitionsMat.empty())
                return;

            RotatedRect[] detectons_arr = new MatOfRotatedRect(detectonsMat).toArray();
            float[] confidences_arr = new MatOfFloat(confidencesMat).toArray();

            List<string> recognitionList = new List<string>();
            Converters.Mat_to_vector_string(recognitionsMat, recognitionList);
            string[] recognition_arr = recognitionList.ToArray();

            Array.Reverse(detectons_arr);
            Array.Reverse(confidences_arr);
            Array.Reverse(recognition_arr);

            Scalar BgrScalarForImage(Scalar bgr)
            {
                if (!isRGB)
                    return bgr;
                return new Scalar(bgr.val[2], bgr.val[1], bgr.val[0]);
            }

            Scalar colorGreen = BgrScalarForImage(new Scalar(0, 255, 0));
            Scalar colorLowConfidence = BgrScalarForImage(new Scalar(255, 0, 0));
            Scalar colorRed = BgrScalarForImage(new Scalar(0, 0, 255));

            StringBuilder sb = new StringBuilder(1024);
            for (int i = 0; i < detectons_arr.Length; ++i)
            {
                Point[] vertices = new Point[4];
                detectons_arr[i].points(vertices);

                for (int j = 0; j < 4; ++j)
                    Imgproc.line(image, vertices[j], vertices[(j + 1) % 4], colorGreen, 2);

                if (confidences_arr[i] < DETECTION_CONFIDENCES_THRESHOLD)
                {
                    for (int j = 0; j < 4; ++j)
                        Imgproc.line(image, vertices[j], vertices[(j + 1) % 4], colorLowConfidence, 2);
                }

                Imgproc.putText(image, recognition_arr[i], vertices[1], Imgproc.FONT_HERSHEY_SIMPLEX, 0.8, colorRed, 2, Imgproc.LINE_AA, false);

                sb.Append("[").Append(recognition_arr[i]).Append("] ").Append(confidences_arr[i]).AppendLine();
            }

            if (printResult)
                Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Runs text detection and recognition using <see cref="_detectionModel"/> and <see cref="_recognitionModel"/>;
        /// returns Mats in order: detections, confidences, recognitions.
        /// </summary>
        /// <returns>Index 0: MatOfRotatedRect (detection order). Index 1: MatOfFloat. Index 2: recognition strings as CV_8UC1 row (UTF-8 with separator).</returns>
        private Mat[] Infer(Mat img)
        {
            Mat croppedMat = new Mat(new Size(RECOGNITION_INPUT_SIZE_W, RECOGNITION_INPUT_SIZE_H), CvType.CV_8SC3);
            Mat croppedGrayMat = new Mat(croppedMat.size(), CvType.CV_8SC1);

            try
            {
                MatOfRotatedRect detectons = new MatOfRotatedRect();
                MatOfFloat confidences = new MatOfFloat();

                _detectionModel.detectTextRectangles(img, detectons, confidences);

                RotatedRect[] detectonsArr = detectons.toArray();
                float[] confidencesArr = new float[detectonsArr.Length];
                if (!confidences.empty())
                    confidencesArr = new MatOfFloat(confidences).toArray();

                List<string> recognitionStrings = new List<string>(detectonsArr.Length);
                for (int k = 0; k < detectonsArr.Length; k++)
                    recognitionStrings.Add(null);

                for (int i = 0; i < detectonsArr.Length; ++i)
                {
                    if (confidencesArr[i] < DETECTION_CONFIDENCES_THRESHOLD)
                        continue;

                    Point[] vertices = new Point[4];
                    detectonsArr[i].points(vertices);

                    // Create transformed and cropped image.
                    FourPointsTransform(img, croppedMat, vertices);
                    Imgproc.cvtColor(croppedMat, croppedGrayMat, Imgproc.COLOR_BGR2GRAY);

                    string recognitionResult = _recognitionModel.recognize(croppedGrayMat);

                    recognitionStrings[i] = recognitionResult;
                }

                Mat recognitionsMat = recognitionStrings.Count > 0
                    ? Converters.vector_string_to_Mat(recognitionStrings)
                    : new Mat(1, 0, CvType.CV_8UC1);

                return new Mat[] { detectons, confidences, recognitionsMat };
            }
            finally
            {
                croppedMat.Dispose();
                croppedGrayMat.Dispose();
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
            return Infer(img);
#else
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Infer(img);
            }, cancellationToken);
#endif
        }

        private void FourPointsTransform(Mat src, Mat dst, Point[] vertices)
        {
            Size outputSize = dst.size();

            Point[] targetVertices = new Point[] { new Point(0, outputSize.height - 1),
                new Point(0, 0), new Point(outputSize.width - 1, 0),
                new Point(outputSize.width - 1, outputSize.height - 1),
            };

            MatOfPoint2f verticesMat = new MatOfPoint2f(vertices);
            MatOfPoint2f targetVerticesMat = new MatOfPoint2f(targetVertices);
            Mat rotationMatrix = Imgproc.getPerspectiveTransform(verticesMat, targetVerticesMat);

            Imgproc.warpPerspective(src, dst, rotationMatrix, outputSize);
        }

        private List<string> LoadCharset(string charsetPath)
        {
            return new List<string>(File.ReadAllLines(charsetPath));
        }

        private static void UpdateFpsMonitorInferenceInfo(FpsMonitor fpsMonitor, TextDetectionModel_DB detectionModel, TextRecognitionModel recognitionModel, bool useAsyncInference)
        {
            if (fpsMonitor == null)
                return;

            if (detectionModel != null && recognitionModel != null)
            {
                // TextDetectionModel_DB / TextRecognitionModel: no PreferredBackend/PreferredTarget getters in the C# binding; show as default OpenCV DNN inference.
                fpsMonitor.Add("dnnBackend", "OPENCV");
                fpsMonitor.Add("dnnTarget", "CPU");
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
