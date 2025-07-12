#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

        [Space(10)]

        // Private Fields
        private Scalar _detectionInputMean = new Scalar(122.67891434, 116.66876762, 104.00698793);
        private Scalar _recognitionInputMean = new Scalar(127.5);

        private string _detectionModelFilepath;
        private string _recognitionModelFilepath;
        private string _charsetTxtFilepath;

        private TextDetectionModel_DB _detectionModel;
        private TextRecognitionModel _recognitionModel;

        private Mat _croppedMat;
        private Mat _croppedGrayMat;

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

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.BGR;

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

                if (_detectionModel == null || _recognitionModel == null)
                {
                    Imgproc.putText(bgrMat, "model file is not loaded.", new Point(5, bgrMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(bgrMat, "Please read console message.", new Point(5, bgrMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    MatOfRotatedRect detectons = new MatOfRotatedRect();
                    MatOfFloat confidences = new MatOfFloat();
                    _detectionModel.detectTextRectangles(bgrMat, detectons, confidences);

                    RotatedRect[] detectons_arr = detectons.toArray();
                    Array.Reverse(detectons_arr);
                    float[] confidences_arr = new float[detectons_arr.Length];
                    if (confidences.total() > 0)
                        confidences_arr = confidences.toArray();
                    Array.Reverse(confidences_arr);
                    string[] recognition_arr = new string[detectons_arr.Length];

                    for (int i = 0; i < detectons_arr.Length; ++i)
                    {
                        if (confidences_arr[i] < DETECTION_CONFIDENCES_THRESHOLD)
                            continue;

                        Point[] vertices = new Point[4];
                        detectons_arr[i].points(vertices);

                        // Create transformed and cropped image.
                        FourPointsTransform(bgrMat, _croppedMat, vertices);
                        Imgproc.cvtColor(_croppedMat, _croppedGrayMat, Imgproc.COLOR_BGR2GRAY);

                        string recognitionResult = _recognitionModel.recognize(_croppedGrayMat);

                        recognition_arr[i] = recognitionResult;
                    }

                    // Draw results.
                    for (int i = 0; i < detectons_arr.Length; ++i)
                    {
                        Point[] vertices = new Point[4];
                        detectons_arr[i].points(vertices);

                        for (int j = 0; j < 4; ++j)
                            Imgproc.line(bgrMat, vertices[j], vertices[(j + 1) % 4], new Scalar(0, 255, 0), 2);

                        if (confidences_arr[i] < DETECTION_CONFIDENCES_THRESHOLD)
                        {
                            for (int j = 0; j < 4; ++j)
                                Imgproc.line(bgrMat, vertices[j], vertices[(j + 1) % 4], new Scalar(255, 0, 0), 2);
                        }

                        Imgproc.putText(bgrMat, recognition_arr[i], vertices[1], Imgproc.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar(0, 0, 255), 2, Imgproc.LINE_AA, false);

                        //Debug.Log("[" + recognition_arr[i] + "] " + confidences_arr[i]);
                    }

                    detectons.Dispose();
                    confidences.Dispose();
                }

                Imgproc.cvtColor(bgrMat, bgrMat, Imgproc.COLOR_BGR2RGB);

                //Imgproc.putText (bgrMat, "W:" + bgrMat.width () + " H:" + bgrMat.height () + " SO:" + Screen.orientation, new Point (5, img.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                OpenCVMatUtils.MatToTexture2D(bgrMat, _texture);
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            _detectionModel?.Dispose();
            _recognitionModel?.Dispose();

            _croppedMat?.Dispose(); _croppedMat = null;
            _croppedGrayMat?.Dispose(); _croppedGrayMat = null;

            _cts?.Dispose();
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

        // Private Methods
        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            if (string.IsNullOrEmpty(_detectionModelFilepath) || string.IsNullOrEmpty(_recognitionModelFilepath) || string.IsNullOrEmpty(_charsetTxtFilepath))
            {
                Debug.LogError(DETECTION_MODEL_FILENAME + " or " + RECOGNITION_MODEL_FILENAME + " or " + CHARSET_TXT_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
            }
            else
            {
                // Create TextDetectionModel.
                _detectionModel = new TextDetectionModel_DB(_detectionModelFilepath);
                _detectionModel.setBinaryThreshold(DETECTION_BINARY_THRESHOLD);
                _detectionModel.setPolygonThreshold(DETECTION_POLYGON_THRESHOLD);
                _detectionModel.setUnclipRatio(DETECTION_UNCLIP_RATIO);
                _detectionModel.setMaxCandidates(DETECTION_MAX_CANDIDATES);
                _detectionModel.setInputParams(DETECTION_INPUT_SCALE, new Size(DETECTION_INPUT_SIZE_W, DETECTION_INPUT_SIZE_H), _detectionInputMean);

                // Create TextRecognitonModel.
                _recognitionModel = new TextRecognitionModel(_recognitionModelFilepath);
                _recognitionModel.setDecodeType("CTC-greedy");
                _recognitionModel.setVocabulary(LoadCharset(_charsetTxtFilepath));
                _recognitionModel.setInputParams(RECOGNITION_INPUT_SCALE, new Size(RECOGNITION_INPUT_SIZE_W, RECOGNITION_INPUT_SIZE_H), _recognitionInputMean);

                _croppedMat = new Mat(new Size(RECOGNITION_INPUT_SIZE_W, RECOGNITION_INPUT_SIZE_H), CvType.CV_8SC3);
                _croppedGrayMat = new Mat(_croppedMat.size(), CvType.CV_8SC1);
            }

            OpenCVDebug.SetDebugMode(false);

            _multiSource2MatHelper.Initialize();
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
    }
}

#endif
