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
    /// Pose Estimation MediaPipe Example
    /// An example of using OpenCV dnn module with Human Pose Estimation.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/main/models/pose_estimation_mediapipe
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class PoseEstimationMediaPipeExample : MonoBehaviour
    {
        /// <summary>
        /// The mask toggle.
        /// </summary>
        public Toggle maskToggle;

        public bool mask;

        /// <summary>
        /// The show Skeleton toggle.
        /// </summary>
        public Toggle showSkeletonToggle;

        public bool showSkeleton;

        public MediaPipePoseSkeletonVisualizer skeletonVisualizer;


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
        /// The person detector.
        /// </summary>
        MediaPipePersonDetector personDetector;

        /// <summary>
        /// The pose estimator.
        /// </summary>
        MediaPipePoseEstimator poseEstimator;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// PERSON_DETECTION_MODEL_FILENAME
        /// </summary>
        protected static readonly string PERSON_DETECTION_MODEL_FILENAME = "OpenCVForUnity/dnn/person_detection_mediapipe_2023mar.onnx";

        /// <summary>
        /// The person detection model filepath.
        /// </summary>
        string person_detection_model_filepath;

        /// <summary>
        /// POSE_ESTIMATION_MODEL_FILENAME
        /// </summary>
        protected static readonly string POSE_ESTIMATION_MODEL_FILENAME = "OpenCVForUnity/dnn/pose_estimation_mediapipe_2023mar.onnx";

        /// <summary>
        /// The pose estimation model filepath.
        /// </summary>
        string pose_estimation_model_filepath;

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
            maskToggle.isOn = mask;
            showSkeletonToggle.isOn = showSkeleton;
            if (skeletonVisualizer != null) skeletonVisualizer.showSkeleton = showSkeleton;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (fpsMonitor != null)
                fpsMonitor.consoleText = "Preparing file access...";

            person_detection_model_filepath = await Utils.getFilePathAsyncTask(PERSON_DETECTION_MODEL_FILENAME, cancellationToken: cts.Token);
            pose_estimation_model_filepath = await Utils.getFilePathAsyncTask(POSE_ESTIMATION_MODEL_FILENAME, cancellationToken: cts.Token);

            if (fpsMonitor != null)
                fpsMonitor.consoleText = "";

            Run();
        }

        // Use this for initialization
        void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            if (string.IsNullOrEmpty(person_detection_model_filepath))
            {
                Debug.LogError(PERSON_DETECTION_MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                personDetector = new MediaPipePersonDetector(person_detection_model_filepath, 0.3f, 0.6f, 5000);// # usually only one person has good performance
            }

            if (string.IsNullOrEmpty(pose_estimation_model_filepath))
            {
                Debug.LogError(POSE_ESTIMATION_MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                poseEstimator = new MediaPipePoseEstimator(pose_estimation_model_filepath, 0.9f);
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

                if (personDetector == null || poseEstimator == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    Imgproc.cvtColor(rgbaMat, bgrMat, Imgproc.COLOR_RGBA2BGR);

                    //TickMeter tm = new TickMeter();
                    //tm.start();

                    Mat persons = personDetector.infer(bgrMat);

                    //tm.stop();
                    //Debug.Log("MediaPipePersonDetector Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                    List<Mat> poses = new List<Mat>();
                    List<Mat> masks = new List<Mat>();

                    // Estimate the pose of each person
                    for (int i = 0; i < persons.rows(); ++i)
                    {
                        //tm.reset();
                        //tm.start();

                        // pose estimator inference
                        List<Mat> results = poseEstimator.infer(bgrMat, persons.row(i), mask);

                        //tm.stop();
                        //Debug.Log("MediaPipePoseEstimator Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                        poses.Add(results[0]);

                        if (!results[1].empty())
                            masks.Add(results[1]);
                    }

                    Imgproc.cvtColor(bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                    //personDetector.visualize(rgbaMat, persons, false, true);

                    foreach (var mask in masks)
                        poseEstimator.visualize_mask(rgbaMat, mask, true);

                    foreach (var pose in poses)
                        poseEstimator.visualize(rgbaMat, pose, false, true);

                    if (skeletonVisualizer != null && skeletonVisualizer.showSkeleton)
                    {
                        if (poses.Count > 0 && !poses[0].empty())
                            skeletonVisualizer.UpdatePose(poses[0]);
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

            if (personDetector != null)
                personDetector.dispose();

            if (poseEstimator != null)
                poseEstimator.dispose();

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
        /// Raises the mask toggle value changed event.
        /// </summary>
        public void OnMaskToggleValueChanged()
        {
            if (maskToggle.isOn != mask)
            {
                mask = maskToggle.isOn;
            }
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