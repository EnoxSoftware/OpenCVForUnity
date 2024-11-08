#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnityExample.DnnModel;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Facial Expression Recognition Example
    /// An example of using OpenCV dnn module with Facial Expression Recognition.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/master/models/facial_expression_recognition
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class FacialExpressionRecognitionExample : MonoBehaviour
    {
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        MultiSource2MatHelper multiSource2MatHelper;

        /// <summary>
        /// The bgr mat.
        /// </summary>
        Mat bgrMat;

        /// <summary>
        /// The facial expression recognizer.
        /// </summary>
        FacialExpressionRecognizer facialExpressionRecognizer;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// FACIAL_EXPRESSION_RECOGNITION_MODEL_FILENAME
        /// </summary>
        protected static readonly string FACIAL_EXPRESSION_RECOGNITION_MODEL_FILENAME = "OpenCVForUnity/dnn/facial_expression_recognition_mobilefacenet_2022july.onnx";

        /// <summary>
        /// The facial expression recognition model filepath.
        /// </summary>
        string facial_expression_recognition_model_filepath;

        /// <summary>
        /// FACE_RECOGNITION_MODEL_FILENAME
        /// </summary>
        protected static readonly string FACE_RECOGNITION_MODEL_FILENAME = "OpenCVForUnity/dnn/face_recognition_sface_2021dec.onnx";

        /// <summary>
        /// The face recognition model filepath.
        /// </summary>
        string face_recognition_model_filepath;


        /// <summary>
        /// The YuNetV2FaceDetector.
        /// </summary>
        YuNetV2FaceDetector faceDetector;

        int inputSizeW = 320;
        int inputSizeH = 320;
        float scoreThreshold = 0.8f;
        float nmsThreshold = 0.3f;
        int topK = 5000;

        /// <summary>
        /// FACE_DETECTION_MODEL_FILENAME
        /// </summary>
        protected static readonly string FACE_DETECTION_MODEL_FILENAME = "OpenCVForUnity/dnn/face_detection_yunet_2023mar.onnx";

        /// <summary>
        /// The face detection model filepath.
        /// </summary>
        string face_detection_model_filepath;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // Use this for initialization
        async void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            multiSource2MatHelper.outputColorFormat = Source2MatHelperColorFormat.RGBA;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (fpsMonitor != null)
                fpsMonitor.consoleText = "Preparing file access...";

            face_detection_model_filepath = await Utils.getFilePathAsyncTask(FACE_DETECTION_MODEL_FILENAME, cancellationToken: cts.Token);
            facial_expression_recognition_model_filepath = await Utils.getFilePathAsyncTask(FACIAL_EXPRESSION_RECOGNITION_MODEL_FILENAME, cancellationToken: cts.Token);
            face_recognition_model_filepath = await Utils.getFilePathAsyncTask(FACE_RECOGNITION_MODEL_FILENAME, cancellationToken: cts.Token);

            if (fpsMonitor != null)
                fpsMonitor.consoleText = "";

            Run();
        }

        // Use this for initialization
        void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            if (string.IsNullOrEmpty(face_detection_model_filepath))
            {
                Debug.LogError(FACE_DETECTION_MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                faceDetector = new YuNetV2FaceDetector(face_detection_model_filepath, "", new Size(inputSizeW, inputSizeH), scoreThreshold, nmsThreshold, topK);
            }

            if (string.IsNullOrEmpty(facial_expression_recognition_model_filepath) || string.IsNullOrEmpty(face_recognition_model_filepath))
            {
                Debug.LogError(FACIAL_EXPRESSION_RECOGNITION_MODEL_FILENAME + " or " + FACE_RECOGNITION_MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                facialExpressionRecognizer = new FacialExpressionRecognizer(facial_expression_recognition_model_filepath, face_recognition_model_filepath, "");
            }

            multiSource2MatHelper.Initialize();
        }

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = multiSource2MatHelper.GetMat();

            texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(rgbaMat, texture);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", rgbaMat.width().ToString());
                fpsMonitor.Add("height", rgbaMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (bgrMat != null)
                bgrMat.Dispose();

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

                Mat rgbaMat = multiSource2MatHelper.GetMat();

                if (faceDetector == null || facialExpressionRecognizer == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    Imgproc.cvtColor(rgbaMat, bgrMat, Imgproc.COLOR_RGBA2BGR);

                    //TickMeter tm = new TickMeter();
                    //tm.start();

                    Mat faces = faceDetector.infer(bgrMat);

                    //tm.stop();
                    //Debug.Log("YuNetFaceDetector Inference time, ms: " + tm.getTimeMilli());

                    List<Mat> expressions = new List<Mat>();

                    // Estimate the expression of each face
                    for (int i = 0; i < faces.rows(); ++i)
                    {
                        //tm.reset();
                        //tm.start();

                        // Facial expression recognizer inference
                        Mat facialExpression = facialExpressionRecognizer.infer(bgrMat, faces.row(i));

                        //tm.stop();
                        //Debug.Log("FacialExpressionRecognizer Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                        if (!facialExpression.empty())
                            expressions.Add(facialExpression);
                    }

                    Imgproc.cvtColor(bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                    //faceDetector.visualize(rgbaMat, faces, false, true);
                    facialExpressionRecognizer.visualize(rgbaMat, expressions, faces, false, true);
                }

                Utils.matToTexture2D(rgbaMat, texture);
            }

        }


        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            multiSource2MatHelper.Dispose();

            if (faceDetector != null)
                faceDetector.dispose();

            if (facialExpressionRecognizer != null)
                facialExpressionRecognizer.dispose();

            Utils.setDebugMode(false);

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
    }
}

#endif