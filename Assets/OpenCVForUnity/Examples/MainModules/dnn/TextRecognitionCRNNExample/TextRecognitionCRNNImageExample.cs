#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UtilsModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Text Recognition CRNN Example
    /// This example demonstrates text detection and recognition model using the TextDetectionMode and TextRecognitionModel class.
    /// Referring to :
    /// https://github.com/opencv/opencv_zoo/tree/master/models/text_detection_db
    /// https://github.com/opencv/opencv_zoo/tree/master/models/text_recognition_crnn
    /// https://docs.opencv.org/4.x/d4/d43/tutorial_dnn_text_spotting.html
    ///
    /// [Tested Models]
    /// https://github.com/opencv/opencv_zoo/raw/6a66e0d6e47a693e6d0dd01bbb18e920f3fbae75/models/text_detection_db/text_detection_DB_IC15_resnet18_2021sep.onnx
    /// https://github.com/opencv/opencv_zoo/raw/8a42017a12fe9ed80279737c0b903307371b0e3d/models/text_recognition_crnn/text_recognition_CRNN_EN_2021sep.onnx
    /// https://github.com/opencv/opencv_zoo/raw/8a42017a12fe9ed80279737c0b903307371b0e3d/models/text_recognition_crnn/charset_36_EN.txt
    /// </summary>
    public class TextRecognitionCRNNImageExample : MonoBehaviour
    {
        // Constants
        private const float DETECTION_INPUT_SIZE_W = 736f;
        private const float DETECTION_INPUT_SIZE_H = 736f;
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
        private static readonly string IMAGE_FILENAME = "OpenCVForUnityExamples/text/test_text.jpg";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
        private Scalar _detectionInputMean = new Scalar(122.67891434, 116.66876762, 104.00698793);
        private Scalar _recognitionInputMean = new Scalar(127.5);

        private string _detectionModelFilepath;
        private string _recognitionModelFilepath;
        private string _charsetTxtFilepath;
        private string _imageFilepath;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _detectionModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(DETECTION_MODEL_FILENAME, cancellationToken: _cts.Token);
            _recognitionModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(RECOGNITION_MODEL_FILENAME, cancellationToken: _cts.Token);
            _charsetTxtFilepath = await OpenCVEnv.GetFilePathTaskAsync(CHARSET_TXT_FILENAME, cancellationToken: _cts.Token);
            _imageFilepath = await OpenCVEnv.GetFilePathTaskAsync(IMAGE_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        private void Update()
        {

        }

        private void OnDestroy()
        {
            _cts?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        // Private Methods
        private void Run()
        {

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);


            Mat img = Imgcodecs.imread(_imageFilepath, Imgcodecs.IMREAD_COLOR);
            if (img.empty())
            {
                Debug.LogError(IMAGE_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("image file is not loaded.\nPlease read console message.", 20000);
                }
                img = new Mat(368, 368, CvType.CV_8UC3, new Scalar(0, 0, 0));
            }

            TextDetectionModel_DB detectonModel = null;
            TextRecognitionModel recognitonModel = null;

            if (string.IsNullOrEmpty(_detectionModelFilepath) || string.IsNullOrEmpty(_recognitionModelFilepath) || string.IsNullOrEmpty(_charsetTxtFilepath))
            {
                Debug.LogError(DETECTION_MODEL_FILENAME + " or " + RECOGNITION_MODEL_FILENAME + " or " + CHARSET_TXT_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("model file is not loaded.\nPlease read console message.", 20000);
                }
            }
            else
            {
                // Create TextDetectionModel.
                detectonModel = new TextDetectionModel_DB(_detectionModelFilepath);
                detectonModel.setBinaryThreshold(DETECTION_BINARY_THRESHOLD);
                detectonModel.setPolygonThreshold(DETECTION_POLYGON_THRESHOLD);
                detectonModel.setUnclipRatio(DETECTION_UNCLIP_RATIO);
                detectonModel.setMaxCandidates(DETECTION_MAX_CANDIDATES);
                detectonModel.setInputParams(DETECTION_INPUT_SCALE, new Size(DETECTION_INPUT_SIZE_W, DETECTION_INPUT_SIZE_H), _detectionInputMean);

                // Create TextRecognitonModel.
                recognitonModel = new TextRecognitionModel(_recognitionModelFilepath);
                recognitonModel.setDecodeType("CTC-greedy");
                recognitonModel.setVocabulary(LoadCharset(_charsetTxtFilepath));
                recognitonModel.setInputParams(RECOGNITION_INPUT_SCALE, new Size(RECOGNITION_INPUT_SIZE_W, RECOGNITION_INPUT_SIZE_H), _recognitionInputMean);
            }

            if (detectonModel != null && recognitonModel != null)
            {
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Add("width", img.cols().ToString());
                    _fpsMonitor.Add("height", img.rows().ToString());
                    _fpsMonitor.Add("orientation", Screen.orientation.ToString());
                    UpdateFpsMonitorInferenceInfo(_fpsMonitor, detectonModel, recognitonModel);
                }

                Mat[] inferMats = Infer(img, detectonModel, recognitonModel);
                Visualize(img, inferMats, printResult: true, isRGB: false);

                foreach (Mat m in inferMats)
                    m.Dispose();
                detectonModel.Dispose();
                recognitonModel.Dispose();
            }

            Imgproc.cvtColor(img, img, Imgproc.COLOR_BGR2RGB);

            Texture2D texture = new Texture2D(img.cols(), img.rows(), TextureFormat.RGB24, false);

            OpenCVMatUtils.MatToTexture2D(img, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            OpenCVDebug.SetDebugMode(false);
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
            Scalar colorYellow = BgrScalarForImage(new Scalar(0, 255, 255));
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
                        Imgproc.line(image, vertices[j], vertices[(j + 1) % 4], colorYellow, 2);
                }

                Imgproc.putText(image, recognition_arr[i], vertices[1], Imgproc.FONT_HERSHEY_SIMPLEX, 0.8, colorRed, 2, Imgproc.LINE_AA, false);

                sb.Append("[").Append(recognition_arr[i]).Append("] ").Append(confidences_arr[i]).AppendLine();
            }

            if (printResult)
                Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Runs text detection and recognition; returns Mats in order: detections, confidences, recognitions.
        /// </summary>
        /// <returns>Index 0: MatOfRotatedRect (detection order). Index 1: MatOfFloat. Index 2: recognition strings as CV_8UC1 row (UTF-8 with separator).</returns>
        private Mat[] Infer(
            Mat img,
            TextDetectionModel_DB detectonModel,
            TextRecognitionModel recognitonModel)
        {
            TickMeter tickMeter = new TickMeter();
            Mat croppedMat = new Mat(new Size(RECOGNITION_INPUT_SIZE_W, RECOGNITION_INPUT_SIZE_H), CvType.CV_8SC3);
            Mat croppedGrayMat = new Mat(croppedMat.size(), CvType.CV_8SC1);

            try
            {
                MatOfRotatedRect detectons = new MatOfRotatedRect();
                MatOfFloat confidences = new MatOfFloat();

                tickMeter.start();
                detectonModel.detectTextRectangles(img, detectons, confidences);
                tickMeter.stop();

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

                    //
                    DebugMat.imshow("croppedMat_" + i, croppedGrayMat);
                    //

                    tickMeter.start();
                    string recognitionResult = recognitonModel.recognize(croppedGrayMat);
                    tickMeter.stop();

                    recognitionStrings[i] = recognitionResult;
                }

                Mat recognitionsMat = recognitionStrings.Count > 0
                    ? Converters.vector_string_to_Mat(recognitionStrings)
                    : new Mat(1, 0, CvType.CV_8UC1);

                Debug.Log("Inference time, ms: " + tickMeter.getTimeMilli());

                return new Mat[] { detectons, confidences, recognitionsMat };
            }
            finally
            {
                croppedMat.Dispose();
                croppedGrayMat.Dispose();
            }
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

        private static void UpdateFpsMonitorInferenceInfo(FpsMonitor fpsMonitor, TextDetectionModel_DB detectionModel, TextRecognitionModel recognitionModel)
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
            fpsMonitor.Add("useAsyncInference", "False");
        }
    }
}
#endif
