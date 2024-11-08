#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Text Recognition CRNN Example
    /// This example demonstrates text detection and recognition model using the TextDetectionMode and TextRecognitionModel class.
    /// https://github.com/opencv/opencv_zoo/tree/master/models/text_detection_db
    /// https://github.com/opencv/opencv_zoo/tree/master/models/text_recognition_crnn
    /// https://docs.opencv.org/4.x/d4/d43/tutorial_dnn_text_spotting.html
    /// </summary>
    public class TextRecognitionCRNNImageExample : MonoBehaviour
    {
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        // Preprocess input image by resizing to a specific width. It should be multiple by 32.
        const float detection_inputSize_w = 736f;

        // Preprocess input image by resizing to a specific height. It should be multiple by 32.
        const float detection_inputSize_h = 736f;

        const double detection_inputScale = 1.0 / 255.0;

        Scalar detection_inputMean = new Scalar(122.67891434, 116.66876762, 104.00698793);

        // Threshold of the binary map.
        const float detection_binary_threshold = 0.3f;

        // Threshold of polygons.
        const float detection_polygon_threshold = 0.5f;

        // Max candidates of polygons.
        const int detection_max_candidates = 200;

        // The unclip ratio of the detected text region, which determines the output size.
        const double detection_unclip_ratio = 2.0;

        const float detection_confidences_threshold = 0.7f;


        // Preprocess input image by resizing to a specific width.
        const float recogniton_inputSize_w = 100f;

        // Preprocess input image by resizing to a specific height.
        const float recogniton_inputSize_h = 32f;

        const double recogniton_inputScale = 1.0 / 127.5;

        Scalar recogniton_inputMean = new Scalar(127.5);


        /// <summary>
        /// Path to a binary .onnx file contains trained detection network.
        /// </summary>
        string DETECTIONMODEL_FILENAME = "OpenCVForUnity/dnn/text_detection_DB_IC15_resnet18_2021sep.onnx";

        /// <summary>
        /// The detection model filepath.
        /// </summary>
        string detectionmodel_filepath;

        /// <summary>
        /// Path to a binary .onnx file contains trained recognition network.
        /// </summary>
        string RECOGNTIONMODEL_FILENAME = "OpenCVForUnity/dnn/text_recognition_CRNN_EN_2021sep.onnx";

        /// <summary>
        /// The recognition model filepath.
        /// </summary>
        string recognitionmodel_filepath;

        /// <summary>
        /// Path to a .txt file contains charset.
        /// </summary>
        string CHARSETTXT_FILENAME = "OpenCVForUnity/dnn/charset_36_EN.txt";

        /// <summary>
        /// The charset txt filepath.
        /// </summary>
        string charsettxt_filepath;

        /// <summary>
        /// IMAGE_FILENAME
        /// </summary>
        string IMAGE_FILENAME = "OpenCVForUnity/text/test_text.jpg";

        /// <summary>
        /// The image filepath.
        /// </summary>
        string image_filepath;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // Use this for initialization
        async void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (fpsMonitor != null)
                fpsMonitor.consoleText = "Preparing file access...";

            detectionmodel_filepath = await Utils.getFilePathAsyncTask(DETECTIONMODEL_FILENAME, cancellationToken: cts.Token);
            recognitionmodel_filepath = await Utils.getFilePathAsyncTask(RECOGNTIONMODEL_FILENAME, cancellationToken: cts.Token);
            charsettxt_filepath = await Utils.getFilePathAsyncTask(CHARSETTXT_FILENAME, cancellationToken: cts.Token);
            image_filepath = await Utils.getFilePathAsyncTask(IMAGE_FILENAME, cancellationToken: cts.Token);

            if (fpsMonitor != null)
                fpsMonitor.consoleText = "";

            Run();
        }

        // Use this for initialization
        void Run()
        {

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            Mat img = Imgcodecs.imread(image_filepath, Imgcodecs.IMREAD_COLOR);
            if (img.empty())
            {
                Debug.LogError(IMAGE_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
                img = new Mat(368, 368, CvType.CV_8UC3, new Scalar(0, 0, 0));
            }

            TextDetectionModel_DB detectonModel = null;
            TextRecognitionModel recognitonModel = null;
            Mat croppedMat = null;
            Mat croppedGrayMat = null;

            if (string.IsNullOrEmpty(detectionmodel_filepath) || string.IsNullOrEmpty(recognitionmodel_filepath) || string.IsNullOrEmpty(charsettxt_filepath))
            {
                Debug.LogError(DETECTIONMODEL_FILENAME + " or " + RECOGNTIONMODEL_FILENAME + " or " + CHARSETTXT_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                // Create TextDetectionModel.
                detectonModel = new TextDetectionModel_DB(detectionmodel_filepath);
                detectonModel.setBinaryThreshold(detection_binary_threshold);
                detectonModel.setPolygonThreshold(detection_polygon_threshold);
                detectonModel.setUnclipRatio(detection_unclip_ratio);
                detectonModel.setMaxCandidates(detection_max_candidates);
                detectonModel.setInputParams(detection_inputScale, new Size(detection_inputSize_w, detection_inputSize_h), detection_inputMean);

                // Create TextRecognitonModel.
                recognitonModel = new TextRecognitionModel(recognitionmodel_filepath);
                recognitonModel.setDecodeType("CTC-greedy");
                recognitonModel.setVocabulary(loadCharset(charsettxt_filepath));
                recognitonModel.setInputParams(recogniton_inputScale, new Size(recogniton_inputSize_w, recogniton_inputSize_h), recogniton_inputMean);

                croppedMat = new Mat(new Size(recogniton_inputSize_w, recogniton_inputSize_h), CvType.CV_8SC3);
                croppedGrayMat = new Mat(croppedMat.size(), CvType.CV_8SC1);
            }

            if (detectonModel == null || recognitonModel == null)
            {
                Imgproc.putText(img, "model file is not loaded.", new Point(5, img.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(img, "Please read console message.", new Point(5, img.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
            }
            else
            {
                TickMeter tickMeter = new TickMeter();

                MatOfRotatedRect detectons = new MatOfRotatedRect();
                MatOfFloat confidences = new MatOfFloat();

                tickMeter.start();
                detectonModel.detectTextRectangles(img, detectons, confidences);
                tickMeter.stop();

                RotatedRect[] detectons_arr = detectons.toArray();
                Array.Reverse(detectons_arr);
                float[] confidences_arr = new float[detectons_arr.Length];
                if (confidences.total() > 0)
                    confidences_arr = confidences.toArray();
                Array.Reverse(confidences_arr);
                string[] recognition_arr = new string[detectons_arr.Length];

                for (int i = 0; i < detectons_arr.Length; ++i)
                {
                    if (confidences_arr[i] < detection_confidences_threshold)
                        continue;

                    Point[] vertices = new Point[4];
                    detectons_arr[i].points(vertices);

                    // Create transformed and cropped image.
                    fourPointsTransform(img, croppedMat, vertices);
                    Imgproc.cvtColor(croppedMat, croppedGrayMat, Imgproc.COLOR_BGR2GRAY);

                    //
                    DebugMatUtils.imshow("croppedMat_" + i, croppedGrayMat);
                    //

                    tickMeter.start();
                    string recognitionResult = recognitonModel.recognize(croppedGrayMat);
                    tickMeter.stop();

                    recognition_arr[i] = recognitionResult;
                }

                // Draw results.
                StringBuilder sb = new StringBuilder(1024);
                for (int i = 0; i < detectons_arr.Length; ++i)
                {
                    Point[] vertices = new Point[4];
                    detectons_arr[i].points(vertices);

                    for (int j = 0; j < 4; ++j)
                        Imgproc.line(img, vertices[j], vertices[(j + 1) % 4], new Scalar(0, 255, 0), 2);

                    if (confidences_arr[i] < detection_confidences_threshold)
                    {
                        for (int j = 0; j < 4; ++j)
                            Imgproc.line(img, vertices[j], vertices[(j + 1) % 4], new Scalar(0, 255, 255), 2);
                    }

                    Imgproc.putText(img, recognition_arr[i], vertices[1], Imgproc.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar(0, 0, 255), 2, Imgproc.LINE_AA, false);

                    sb.Append("[").Append(recognition_arr[i]).Append("] ").Append(confidences_arr[i]).AppendLine();
                }
                Debug.Log(sb.ToString());

                Debug.Log("Inference time, ms: " + tickMeter.getTimeMilli());

                detectonModel.Dispose();
                recognitonModel.Dispose();
                croppedMat.Dispose();
                croppedGrayMat.Dispose();
            }

            Imgproc.cvtColor(img, img, Imgproc.COLOR_BGR2RGB);

            Texture2D texture = new Texture2D(img.cols(), img.rows(), TextureFormat.RGB24, false);

            Utils.matToTexture2D(img, texture);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            Utils.setDebugMode(false);
        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// Raises the disable event.
        /// </summary>
        void OnDisable()
        {
            if (cts != null)
                cts.Dispose();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        protected void fourPointsTransform(Mat src, Mat dst, Point[] vertices)
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

        protected List<string> loadCharset(string charsetPath)
        {
            return new List<string>(File.ReadAllLines(charsetPath));
        }
    }
}
#endif