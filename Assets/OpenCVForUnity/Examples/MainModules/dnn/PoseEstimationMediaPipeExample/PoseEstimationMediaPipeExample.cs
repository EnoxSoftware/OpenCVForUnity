#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.AR;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.UnityIntegration.Worker.DnnModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static OpenCVForUnity.UnityIntegration.Worker.DnnModule.MediaPipePoseEstimator;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Pose Estimation MediaPipe Example
    /// An example of using OpenCV dnn module with Human Pose Estimation.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/main/models/pose_estimation_mediapipe
    ///
    /// [Tested Models]
    /// https://github.com/opencv/opencv_zoo/raw/0d619617a8e9a389150d8c76e417451a19468150/models/person_detection_mediapipe/person_detection_mediapipe_2023mar.onnx
    /// https://github.com/opencv/opencv_zoo/raw/0d619617a8e9a389150d8c76e417451a19468150/models/pose_estimation_mediapipe/pose_estimation_mediapipe_2023mar.onnx
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class PoseEstimationMediaPipeExample : MonoBehaviour
    {
        // Constants
        private readonly byte[] SELECTED_INDICES = {
                        (byte)MediaPipePoseEstimator.KeyPoint.Nose,
                        (byte)MediaPipePoseEstimator.KeyPoint.LeftShoulder,
                        (byte)MediaPipePoseEstimator.KeyPoint.RightShoulder,
                        (byte)MediaPipePoseEstimator.KeyPoint.LeftHip,
                        (byte)MediaPipePoseEstimator.KeyPoint.RightHip,
                        (byte)MediaPipePoseEstimator.KeyPoint.LeftElbow,
                        (byte)MediaPipePoseEstimator.KeyPoint.RightElbow,
                        (byte)MediaPipePoseEstimator.KeyPoint.LeftKnee,
                        (byte)MediaPipePoseEstimator.KeyPoint.RightKnee,
                        (byte)MediaPipePoseEstimator.KeyPoint.LeftWrist,
                        (byte)MediaPipePoseEstimator.KeyPoint.RightWrist,
                        (byte)MediaPipePoseEstimator.KeyPoint.LeftAnkle,
                        (byte)MediaPipePoseEstimator.KeyPoint.RightAnkle,
                    };
        private static readonly string PERSON_DETECTION_MODEL_FILENAME = "OpenCVForUnityExamples/dnn/person_detection_mediapipe_2023mar.onnx";
        private static readonly string POSE_ESTIMATION_MODEL_FILENAME = "OpenCVForUnityExamples/dnn/pose_estimation_mediapipe_2023mar.onnx";

        // Public Fields
        [Header("UI")]
        public Toggle UseAsyncInferenceToggle;
        public bool UseAsyncInference = false;
        public Toggle UseMaskToggle;
        public bool UseMask;
        public Toggle ShowSkeletonToggle;
        public bool ShowSkeleton;

        [Space(10)]

        public MediaPipePoseSkeletonVisualizer SkeletonVisualizer;
        public ARHelper ArHelper;

        // Private Fields

        private Texture2D _texture;
        private MultiSource2MatHelper _multiSource2MatHelper;
        private Mat _bgrMat;

        private MediaPipePersonDetector _personDetector;
        private MediaPipePoseEstimator _poseEstimator;
        private string _personDetectionModelFilepath;
        private string _poseEstimationModelFilepath;

        private FpsMonitor _fpsMonitor;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        private Mat _bgrMatForAsync;
        private Mat _latestDetectedPersons;
        private List<Mat> _latestPoses;
        private List<Mat> _latestMasks;
        private Task _inferenceTask;
        private readonly Queue<Action> _mainThreadQueue = new();
        private readonly object _queueLock = new();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;

            // Update GUI state
#if !UNITY_WEBGL || UNITY_EDITOR
            UseAsyncInferenceToggle.isOn = UseAsyncInference;
#else
            UseAsyncInferenceToggle.isOn = false;
            UseAsyncInferenceToggle.interactable = false;
#endif
            UseMaskToggle.isOn = UseMask;
            ShowSkeletonToggle.isOn = ShowSkeleton;
            if (SkeletonVisualizer != null) SkeletonVisualizer.ShowSkeleton = ShowSkeleton;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _personDetectionModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(PERSON_DETECTION_MODEL_FILENAME, cancellationToken: _cts.Token);
            _poseEstimationModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(POSE_ESTIMATION_MODEL_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        private void Update()
        {
            ProcessMainThreadQueue();

            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_personDetector == null || _poseEstimator == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

                    if (UseAsyncInference)
                    {
                        // asynchronous execution

                        if (_inferenceTask == null || _inferenceTask.IsCompleted)
                        {
                            _bgrMat.copyTo(_bgrMatForAsync); // for asynchronous execution, deep copy
                            bool currentUseMask = UseMask; // Capture the current value
                            _inferenceTask = Task.Run(async () =>
                            {
                                try
                                {
                                    // Person detector inference
                                    var newPersons = await _personDetector.DetectAsync(_bgrMatForAsync);
                                    // Estimate the pose of each person
                                    List<Mat> poses = new List<Mat>();
                                    List<Mat> masks = new List<Mat>();
                                    for (int i = 0; i < newPersons.rows(); ++i)
                                    {
                                        // Pose estimator inference
                                        using (Mat person = newPersons.row(i))
                                        {
                                            var pose = await _poseEstimator.EstimateAsync(_bgrMatForAsync, person, currentUseMask);
                                            if (!pose.empty())
                                                poses.Add(pose);
                                            if (currentUseMask)
                                            {
                                                Mat mask = _poseEstimator.CopyOutput(1);
                                                if (!mask.empty())
                                                    masks.Add(mask);
                                                else
                                                    mask.Dispose();
                                            }
                                        }
                                    }
                                    RunOnMainThread(() =>
                                    {
                                        _latestDetectedPersons?.Dispose();
                                        _latestDetectedPersons = newPersons;
                                        if (_latestPoses != null)
                                        {
                                            foreach (var pose in _latestPoses)
                                                pose.Dispose();
                                        }
                                        _latestPoses = poses;
                                        if (_latestMasks != null)
                                        {
                                            foreach (var mask in _latestMasks)
                                                mask.Dispose();
                                        }
                                        _latestMasks = masks;
                                    });
                                }
                                catch (OperationCanceledException ex)
                                {
                                    Debug.Log($"Inference canceled: {ex}");
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogError($"Inference error: {ex}");
                                }
                            });
                        }

                        Imgproc.cvtColor(_bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                        if (_latestDetectedPersons != null && _latestPoses != null)
                        {
                            //_personDetector.Visualize(rgbaMat, latestDetectedPersons, false, true);
                            foreach (var mask in _latestMasks)
                            {
                                _poseEstimator.VisualizeMask(rgbaMat, mask, true);
                            }
                            foreach (var pose in _latestPoses)
                                _poseEstimator.Visualize(rgbaMat, pose, false, true);

                            if (SkeletonVisualizer != null && SkeletonVisualizer.ShowSkeleton)
                                UpdateSkeleton(_latestPoses);
                        }
                    }
                    else
                    {
                        // synchronous execution

                        // TickMeter tm = new TickMeter();
                        // tm.start();

                        // Person detector inference
                        using (Mat persons = _personDetector.Detect(_bgrMat))
                        {
                            // tm.stop();
                            // Debug.Log("MediaPipePersonDetector Inference time, ms: " + tm.getTimeMilli());

                            List<Mat> poses = new List<Mat>();
                            List<Mat> masks = new List<Mat>();

                            // Estimate the pose of each person
                            for (int i = 0; i < persons.rows(); ++i)
                            {
                                //tm.reset();
                                //tm.start();

                                // Pose estimator inference
                                using (Mat person = persons.row(i))
                                {
                                    var pose = _poseEstimator.Estimate(_bgrMat, person, UseMask, useCopyOutput: true);
                                    if (!pose.empty())
                                        poses.Add(pose);
                                    if (UseMask)
                                        masks.Add(_poseEstimator.CopyOutput(1));
                                }

                                //tm.stop();
                                //Debug.Log("MediaPipePoseEstimator Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());
                            }

                            Imgproc.cvtColor(_bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                            //_personDetector.Visualize(rgbaMat, persons, false, true);
                            foreach (var mask in masks)
                                _poseEstimator.VisualizeMask(rgbaMat, mask, true);
                            foreach (var pose in poses)
                                _poseEstimator.Visualize(rgbaMat, pose, false, true);

                            if (SkeletonVisualizer != null && SkeletonVisualizer.ShowSkeleton)
                                UpdateSkeleton(poses);

                            persons.Dispose();
                            foreach (var pose in poses)
                                pose.Dispose();
                        }
                    }
                }

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }

        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            _personDetector?.Dispose();
            _poseEstimator?.Dispose();

            OpenCVDebug.SetDebugMode(false);

            _cts?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = _multiSource2MatHelper.GetMat();

            _texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);

            // Set the Texture2D as the main texture of the Renderer component attached to the game object
            gameObject.GetComponent<Renderer>().material.mainTexture = _texture;

            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


            // Set the camera's orthographicSize to half of the texture height
            Camera.main.orthographicSize = _texture.height / 2f;

            // Get the camera's aspect ratio
            float cameraAspect = Camera.main.aspect;

            // Get the texture's aspect ratio
            float textureAspect = (float)_texture.width / _texture.height;

            // Calculate imageSizeScale
            float imageSizeScale;
            if (textureAspect > cameraAspect)
            {
                // Calculate the camera width (height is already fixed)
                float cameraWidth = Camera.main.orthographicSize * 2f * cameraAspect;

                // Scale so that the texture width fits within the camera width
                imageSizeScale = cameraWidth / _texture.width;
            }
            else
            {
                // Scale so that the texture height fits within the camera height
                imageSizeScale = 1f; // No scaling needed since height is already fixed
            }
            Debug.Log("imageSizeScale " + imageSizeScale);

            // The calculated imageSizeScale is used to set the scale of the game object on which the texture is displayed.
            transform.localScale = new Vector3(_texture.width * imageSizeScale, _texture.height * imageSizeScale, 1);


            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("width", rgbaMat.width().ToString());
                _fpsMonitor.Add("height", rgbaMat.height().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            _bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
            _bgrMatForAsync = new Mat();

            // Initialize ARHelper.
            ArHelper.Initialize();
            // Set ARCamera parameters.
            ArHelper.ARCamera.SetARCameraParameters(Screen.width, Screen.height, rgbaMat.width(), rgbaMat.height(), Vector2.zero, new Vector2(imageSizeScale, imageSizeScale));
            ArHelper.ARCamera.SetCamMatrixValuesFromImageSize();
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (_inferenceTask != null && !_inferenceTask.IsCompleted) _inferenceTask.Wait(500);

            _bgrMat?.Dispose(); _bgrMat = null;

            _bgrMatForAsync?.Dispose(); _bgrMatForAsync = null;
            _latestDetectedPersons?.Dispose(); _latestDetectedPersons = null;
            if (_latestPoses != null)
            {
                foreach (var pose in _latestPoses)
                    pose.Dispose();
                _latestPoses.Clear();
            }
            _latestPoses = null;
            if (_latestMasks != null)
            {
                foreach (var mask in _latestMasks)
                    mask.Dispose();
                _latestMasks.Clear();
            }
            _latestMasks = null;

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;

            ArHelper?.Dispose(); ArHelper = null;
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
        /// Raises the use mask toggle value changed event.
        /// </summary>
        public void OnUseMaskToggleValueChanged()
        {
            if (UseMaskToggle.isOn != UseMask)
            {
                UseMask = UseMaskToggle.isOn;
            }
        }

        /// <summary>
        /// Raises the show skeleton toggle value changed event.
        /// </summary>
        public void OnShowSkeletonToggleValueChanged()
        {
            if (ShowSkeletonToggle.isOn != ShowSkeleton)
            {
                ShowSkeleton = ShowSkeletonToggle.isOn;
                if (SkeletonVisualizer != null) SkeletonVisualizer.ShowSkeleton = ShowSkeleton;
            }
        }

        /// <summary>
        /// Raises the use async inference toggle value changed event.
        /// </summary>
        public void OnUseAsyncInferenceToggleValueChanged()
        {
            if (UseAsyncInferenceToggle.isOn != UseAsyncInference)
            {
                // Wait for inference to complete before changing the toggle
                if (_inferenceTask != null && !_inferenceTask.IsCompleted) _inferenceTask.Wait(500);

                UseAsyncInference = UseAsyncInferenceToggle.isOn;
            }
        }

        /// <summary>
        /// Called when an ARGameObject enters the ARCamera viewport.
        /// </summary>
        /// <param name="aRHelper"></param>
        /// <param name="arCamera"></param>
        /// <param name="arGameObject"></param>
        public void OnEnterARCameraViewport(ARHelper aRHelper, ARCamera arCamera, ARGameObject arGameObject)
        {
            Debug.Log("OnEnterARCamera arCamera.name " + arCamera.name + " arGameObject.name " + arGameObject.name);

            arGameObject.gameObject.SetActive(true);
        }

        /// <summary>
        /// Called when an ARGameObject exits the ARCamera viewport.
        /// </summary>
        /// <param name="aRHelper"></param>
        /// <param name="arCamera"></param>
        /// <param name="arGameObject"></param>
        public void OnExitARCameraViewport(ARHelper aRHelper, ARCamera arCamera, ARGameObject arGameObject)
        {
            Debug.Log("OnExitARCamera arCamera.name " + arCamera.name + " arGameObject.name " + arGameObject.name);

            arGameObject.gameObject.SetActive(false);
        }

        // Private Methods
        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);


            if (string.IsNullOrEmpty(_personDetectionModelFilepath))
            {
                Debug.LogError(PERSON_DETECTION_MODEL_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
            }
            else
            {
                _personDetector = new MediaPipePersonDetector(_personDetectionModelFilepath, 0.3f, 0.6f, 10); // # usually only one person has good performance
            }

            if (string.IsNullOrEmpty(_poseEstimationModelFilepath))
            {
                Debug.LogError(POSE_ESTIMATION_MODEL_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
            }
            else
            {
                _poseEstimator = new MediaPipePoseEstimator(_poseEstimationModelFilepath, 0.9f);
            }

            _multiSource2MatHelper.Initialize();
        }

        private void UpdateSkeleton(List<Mat> poses)
        {
            if (poses == null || poses.Count == 0)
                return;

            ArHelper.ResetARGameObjectsImagePointsAndObjectPoints();

            if (poses.Count > 0 && !poses[0].empty())
            {
                SkeletonVisualizer.UpdatePose(poses[0]);

                PoseEstimationBlazeData data = _poseEstimator.ToStructuredData(poses[0]);
#if NET_STANDARD_2_1
                ReadOnlySpan<ScreenLandmark> landmarks_screen = data.GetLandmarksScreen();
                ReadOnlySpan<Vec3f> landmarks_world = data.GetLandmarksWorld();
#else
                ScreenLandmark[] landmarks_screen = data.GetLandmarksScreenArray();
                Vec3f[] landmarks_world = data.GetLandmarksWorldArray();
#endif

                Vector2[] imagePoints = new Vector2[SELECTED_INDICES.Length];
                Vector3[] objectPoints = new Vector3[SELECTED_INDICES.Length];

                for (int i = 0; i < SELECTED_INDICES.Length; i++)
                {
                    int index = SELECTED_INDICES[i];
                    ref readonly var landmark_screen = ref landmarks_screen[index];
                    ref readonly var landmark_world = ref landmarks_world[index];
                    imagePoints[i] = new Vector2(landmark_screen.X, landmark_screen.Y);
                    objectPoints[i] = new Vector3(landmark_world.Item1, landmark_world.Item2, landmark_world.Item3);
                }

                ArHelper.ARGameObjects[0].ImagePoints = imagePoints;
                ArHelper.ARGameObjects[0].ObjectPoints = objectPoints;
            }
        }

        private void RunOnMainThread(Action action)
        {
            if (action == null) return;

            lock (_queueLock)
            {
                _mainThreadQueue.Enqueue(action);
            }
        }

        private void ProcessMainThreadQueue()
        {
            while (true)
            {
                Action action = null;
                lock (_queueLock)
                {
                    if (_mainThreadQueue.Count == 0)
                        break;

                    action = _mainThreadQueue.Dequeue();
                }

                try { action?.Invoke(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }
    }
}

#endif
