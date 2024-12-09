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
    /// Hand Pose Estimation MediaPipe Example
    /// An example of using OpenCV dnn module with Hand Pose Estimation.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/master/models/handpose_estimation_mediapipe
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class HandPoseEstimationMediaPipeExample : MonoBehaviour
    {
        /// <summary>
        /// The show Skeleton toggle.
        /// </summary>
        public Toggle showSkeletonToggle;

        /// <summary>
        /// Whether to show the skeleton?
        /// </summary>
        public bool showSkeleton;

        /// <summary>
        /// MediaPipeHandPoseSkeletonVisualizer
        /// </summary>
        public MediaPipeHandPoseSkeletonVisualizer skeletonVisualizer;

        /// <summary>
        /// ARHelper
        /// </summary>
        public ARHelper arHelper;

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
        /// The palm detector.
        /// </summary>
        MediaPipePalmDetector palmDetector;

        /// <summary>
        /// The handpose estimator.
        /// </summary>
        MediaPipeHandPoseEstimator handPoseEstimator;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// PALM_DETECTION_MODEL_FILENAME
        /// </summary>
        protected static readonly string PALM_DETECTION_MODEL_FILENAME = "OpenCVForUnity/dnn/palm_detection_mediapipe_2023feb.onnx";

        /// <summary>
        /// The palm detection model filepath.
        /// </summary>
        string palm_detection_model_filepath;

        /// <summary>
        /// HANDPOSE_ESTIMATION_MODEL_FILENAME
        /// </summary>
        protected static readonly string HANDPOSE_ESTIMATION_MODEL_FILENAME = "OpenCVForUnity/dnn/handpose_estimation_mediapipe_2023feb.onnx";

        /// <summary>
        /// The handpose estimation model filepath.
        /// </summary>
        string handpose_estimation_model_filepath;

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

            // Update GUI state
            showSkeletonToggle.isOn = showSkeleton;
            if (skeletonVisualizer != null) skeletonVisualizer.showSkeleton = showSkeleton;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (fpsMonitor != null)
                fpsMonitor.consoleText = "Preparing file access...";

            palm_detection_model_filepath = await Utils.getFilePathAsyncTask(PALM_DETECTION_MODEL_FILENAME, cancellationToken: cts.Token);
            handpose_estimation_model_filepath = await Utils.getFilePathAsyncTask(HANDPOSE_ESTIMATION_MODEL_FILENAME, cancellationToken: cts.Token);

            if (fpsMonitor != null)
                fpsMonitor.consoleText = "";

            Run();
        }

        // Use this for initialization
        void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            if (string.IsNullOrEmpty(palm_detection_model_filepath))
            {
                Debug.LogError(PALM_DETECTION_MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                palmDetector = new MediaPipePalmDetector(palm_detection_model_filepath, 0.3f, 0.6f);
            }

            if (string.IsNullOrEmpty(handpose_estimation_model_filepath))
            {
                Debug.LogError(HANDPOSE_ESTIMATION_MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                handPoseEstimator = new MediaPipeHandPoseEstimator(handpose_estimation_model_filepath, 0.9f);
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
            Debug.Log("rgbaMat.width() " + rgbaMat.width() + " rgbaMat.height() " + rgbaMat.height());

            texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(rgbaMat, texture);

            // Set the Texture2D as the main texture of the Renderer component attached to the game object
            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            // Adjust the scale of the game object to match the dimensions of the texture
            gameObject.transform.localScale = new Vector3(rgbaMat.cols(), rgbaMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            // Adjust the orthographic size of the main Camera to fit the aspect ratio of the image
            float width = rgbaMat.width();
            float height = rgbaMat.height();
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }


            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", rgbaMat.width().ToString());
                fpsMonitor.Add("height", rgbaMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);

            // If the screen aspect ratio changes, e.g. due to device rotation, camMatrix needs to be set to an appropriate value. camMatrix is recalculated by specifying an array of zero elements in camMatrixValue.
            arHelper.Initialize(Screen.width, Screen.height, rgbaMat.width(), rgbaMat.height(), new double[0]);
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

            arHelper.Dispose();
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

                if (palmDetector == null || handPoseEstimator == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    Imgproc.cvtColor(rgbaMat, bgrMat, Imgproc.COLOR_RGBA2BGR);

                    //TickMeter tm = new TickMeter();
                    //tm.start();

                    Mat palms = palmDetector.infer(bgrMat);

                    //tm.stop();
                    //Debug.Log("MediaPipePalmDetector Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                    List<Mat> hands = new List<Mat>();

                    // Estimate the pose of each hand
                    for (int i = 0; i < palms.rows(); ++i)
                    {
                        //tm.reset();
                        //tm.start();

                        // Handpose estimator inference
                        Mat handpose = handPoseEstimator.infer(bgrMat, palms.row(i));

                        //tm.stop();
                        //Debug.Log("MediaPipeHandPoseEstimator Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                        if (!handpose.empty())
                            hands.Add(handpose);
                    }

                    Imgproc.cvtColor(bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                    //palmDetector.visualize(rgbaMat, palms, false, true);

                    foreach (var hand in hands)
                        handPoseEstimator.visualize(rgbaMat, hand, false, true);


                    if (skeletonVisualizer != null && skeletonVisualizer.showSkeleton)
                    {
                        if (hands.Count > 0 && !hands[0].empty())
                        {
                            skeletonVisualizer.UpdatePose(hands[0]);

                            MediaPipeHandPoseEstimator.EstimationData data = handPoseEstimator.getData(hands[0]);

                            Vector3[] landmarks_screen = data.landmarks_screen;
                            Vector3[] landmarks_world = data.landmarks_world;

                            Vector2[] imagePoints = new Vector2[landmarks_screen.Length];
                            for (int i = 0; i < imagePoints.Length; i++)
                            {
                                imagePoints[i] = new Vector2(landmarks_screen[i].x, landmarks_screen[i].y);
                            }
                            Vector3[] objectPoints = new Vector3[landmarks_world.Length];
                            for (int i = 0; i < objectPoints.Length; i++)
                            {
                                objectPoints[i] = new Vector3(landmarks_world[i].x, landmarks_world[i].y, landmarks_world[i].z);
                            }

                            arHelper.imagePoints = imagePoints;
                            arHelper.objectPoints = objectPoints;
                        }
                    }
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

            if (palmDetector != null)
                palmDetector.dispose();

            if (handPoseEstimator != null)
                handPoseEstimator.dispose();

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

        /// <summary>
        /// Raises the show skeleton toggle value changed event.
        /// </summary>
        public void OnShowSkeletonToggleValueChanged()
        {
            if (showSkeletonToggle.isOn != showSkeleton)
            {
                showSkeleton = showSkeletonToggle.isOn;
                if (skeletonVisualizer != null) skeletonVisualizer.showSkeleton = showSkeleton;
            }
        }
    }
}

#endif