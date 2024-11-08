#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System;
using System.Collections.Generic;
using System.IO;
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
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class TextRecognitionCRNNExample : MonoBehaviour
    {
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        // Preprocess input image by resizing to a specific width. It should be multiple by 32.
        const float detection_inputSize_w = 320f; // 736f;

        // Preprocess input image by resizing to a specific height. It should be multiple by 32.
        const float detection_inputSize_h = 320f; // 736f;

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

        TextDetectionModel_DB detectonModel;

        TextRecognitionModel recognitonModel;

        Mat croppedMat;
        Mat croppedGrayMat;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        MultiSource2MatHelper multiSource2MatHelper;

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

            multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            multiSource2MatHelper.outputColorFormat = Source2MatHelperColorFormat.BGR;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (fpsMonitor != null)
                fpsMonitor.consoleText = "Preparing file access...";

            detectionmodel_filepath = await Utils.getFilePathAsyncTask(DETECTIONMODEL_FILENAME, cancellationToken: cts.Token);
            recognitionmodel_filepath = await Utils.getFilePathAsyncTask(RECOGNTIONMODEL_FILENAME, cancellationToken: cts.Token);
            charsettxt_filepath = await Utils.getFilePathAsyncTask(CHARSETTXT_FILENAME, cancellationToken: cts.Token);

            if (fpsMonitor != null)
                fpsMonitor.consoleText = "";

            Run();
        }

        // Use this for initialization
        void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);

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

            Utils.setDebugMode(false);

            multiSource2MatHelper.Initialize();
        }

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat bgrMat = multiSource2MatHelper.GetMat();

            // Fill in the image so that the unprocessed image is not displayed.
            bgrMat.setTo(new Scalar(0, 0, 0, 255));

            texture = new Texture2D(bgrMat.cols(), bgrMat.rows(), TextureFormat.RGB24, false);
            Utils.matToTexture2D(bgrMat, texture);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", multiSource2MatHelper.GetWidth().ToString());
                fpsMonitor.Add("height", multiSource2MatHelper.GetHeight().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API
            // If the WebCam is front facing, flip the Mat horizontally. Required for successful detection.
            if (multiSource2MatHelper.source2MatHelper is WebCamTexture2MatHelper webCamHelper)
                webCamHelper.flipHorizontal = webCamHelper.IsFrontFacing();
#endif
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (multiSource2MatHelper.IsPlaying() && multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat bgrMat = multiSource2MatHelper.GetMat();

                if (detectonModel == null || recognitonModel == null)
                {
                    Imgproc.putText(bgrMat, "model file is not loaded.", new Point(5, bgrMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(bgrMat, "Please read console message.", new Point(5, bgrMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    MatOfRotatedRect detectons = new MatOfRotatedRect();
                    MatOfFloat confidences = new MatOfFloat();
                    detectonModel.detectTextRectangles(bgrMat, detectons, confidences);

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
                        fourPointsTransform(bgrMat, croppedMat, vertices);
                        Imgproc.cvtColor(croppedMat, croppedGrayMat, Imgproc.COLOR_BGR2GRAY);

                        string recognitionResult = recognitonModel.recognize(croppedGrayMat);

                        recognition_arr[i] = recognitionResult;
                    }

                    // Draw results.
                    for (int i = 0; i < detectons_arr.Length; ++i)
                    {
                        Point[] vertices = new Point[4];
                        detectons_arr[i].points(vertices);

                        for (int j = 0; j < 4; ++j)
                            Imgproc.line(bgrMat, vertices[j], vertices[(j + 1) % 4], new Scalar(0, 255, 0), 2);

                        if (confidences_arr[i] < detection_confidences_threshold)
                        {
                            for (int j = 0; j < 4; ++j)
                                Imgproc.line(bgrMat, vertices[j], vertices[(j + 1) % 4], new Scalar(255, 0, 0), 2);
                        }

                        Imgproc.putText(bgrMat, recognition_arr[i], vertices[1], Imgproc.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar(0, 0, 255), 2, Imgproc.LINE_AA, false);

                        //Debug.Log("[" + recognition_arr[i] + "] " + confidences_arr[i]);
                    }
                }

                Imgproc.cvtColor(bgrMat, bgrMat, Imgproc.COLOR_BGR2RGB);

                //Imgproc.putText (bgrMat, "W:" + bgrMat.width () + " H:" + bgrMat.height () + " SO:" + Screen.orientation, new Point (5, img.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Utils.matToTexture2D(bgrMat, texture);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            multiSource2MatHelper.Dispose();

            if (detectonModel != null)
                detectonModel.Dispose();

            if (recognitonModel != null)
                recognitonModel.Dispose();

            if (croppedMat != null)
            {
                croppedMat.Dispose();
                croppedMat = null;
            }

            if (croppedGrayMat != null)
            {
                croppedGrayMat.Dispose();
                croppedGrayMat = null;
            }

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

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            multiSource2MatHelper.requestedIsFrontFacing = !multiSource2MatHelper.requestedIsFrontFacing;
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