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
using static OpenCVForUnity.UnityIntegration.Worker.DnnModule.MediaPipeHandPoseEstimator;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Human Pose Stream Estimation MediaPipe Example
    /// An example of estimating HumanPose from stream using MediaPipeHandPoseStreamEstimator and MediaPipePoseStreamEstimator.
    /// Using StreamEstimator specialized for continuous streams enables more stable tracking.
    ///
    /// [Tested Models]
    /// Hand Pose Models:
    /// https://github.com/opencv/opencv_zoo/raw/6c68bc48c6f96042b29b3425174e431ccac38376/models/palm_detection_mediapipe/palm_detection_mediapipe_2023feb.onnx
    /// https://github.com/opencv/opencv_zoo/raw/05a07912a619f3dd491ba22ca489245c7847c9ff/models/handpose_estimation_mediapipe/handpose_estimation_mediapipe_2023feb.onnx
    /// Human Pose Models:
    /// https://github.com/opencv/opencv_zoo/raw/6c68bc48c6f96042b29b3425174e431ccac38376/models/person_detection_mediapipe/person_detection_mediapipe_2023mar.onnx
    /// https://github.com/opencv/opencv_zoo/raw/05a07912a619f3dd491ba22ca489245c7847c9ff/models/pose_estimation_mediapipe/pose_estimation_mediapipe_2023mar.onnx
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class HumanPoseStreamEstimationMediaPipeExample : MonoBehaviour
    {
        // Constants
        // Hand Pose Constants
        private static readonly byte[] HAND_SELECTED_INDICES = {
            (byte)MediaPipeHandPoseEstimator.KeyPoint.Wrist, // Wrist
            (byte)MediaPipeHandPoseEstimator.KeyPoint.Thumb4, // Thumb tip
            (byte)MediaPipeHandPoseEstimator.KeyPoint.Index4, // Index finger tip
            (byte)MediaPipeHandPoseEstimator.KeyPoint.Middle4, // Middle finger tip
            (byte)MediaPipeHandPoseEstimator.KeyPoint.Ring4, // Ring finger tip
            (byte)MediaPipeHandPoseEstimator.KeyPoint.Pinky4, // Little finger tip
            (byte)MediaPipeHandPoseEstimator.KeyPoint.Thumb1, // Thumb finger base
            (byte)MediaPipeHandPoseEstimator.KeyPoint.Index1, // Index finger base
            (byte)MediaPipeHandPoseEstimator.KeyPoint.Middle1, // Middle finger base
            (byte)MediaPipeHandPoseEstimator.KeyPoint.Ring1, // Ring finger base
            (byte)MediaPipeHandPoseEstimator.KeyPoint.Pinky1, // Little finger base
        };

        // Pose Constants
        private readonly byte[] POSE_SELECTED_INDICES = {
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

        // Model File Names
        private static readonly string PALM_DETECTION_MODEL_FILENAME = "OpenCVForUnityExamples/dnn/palm_detection_mediapipe_2023feb.onnx";
        private static readonly string HANDPOSE_ESTIMATION_MODEL_FILENAME = "OpenCVForUnityExamples/dnn/handpose_estimation_mediapipe_2023feb.onnx";
        private static readonly string PERSON_DETECTION_MODEL_FILENAME = "OpenCVForUnityExamples/dnn/person_detection_mediapipe_2023mar.onnx";
        private static readonly string POSE_ESTIMATION_MODEL_FILENAME = "OpenCVForUnityExamples/dnn/pose_estimation_mediapipe_2023mar.onnx";

        // Public Fields
        [Header("UI")]
        public Toggle UseAsyncInferenceToggle;
        public bool UseAsyncInference = false;
        public Toggle ShowSkeletonToggle;
        public bool ShowSkeleton;
        [Space(10)]
        [Header("Estimation Settings")]
        public Toggle EnableHandPoseEstimationToggle;
        public bool EnableHandPoseEstimation = true;
        public Toggle EnablePoseEstimationToggle;
        public bool EnablePoseEstimation = true;
        [Space(10)]
        [Header("Hand Pose")]
        public MediaPipeHandPoseSkeletonVisualizer SkeletonVisualizerRightHand;
        public ARHelper ArHelperRightHand;
        public MediaPipeHandPoseSkeletonVisualizer SkeletonVisualizerLeftHand;
        public ARHelper ArHelperLeftHand;
        [Space(10)]
        [Header("Pose")]
        public MediaPipePoseSkeletonVisualizer SkeletonVisualizerBody;
        public ARHelper ArHelperBody;

        // Private Fields
        private Texture2D _texture;
        private MultiSource2MatHelper _multiSource2MatHelper;
        private Mat _bgrMat;

        // Hand Pose Estimators
        private MediaPipeHandPoseStreamEstimator _handPoseStreamEstimator;
        private string _palmDetectionModelFilepath;
        private string _handposeEstimationModelFilepath;

        // Pose Estimators
        private MediaPipePoseStreamEstimator _poseStreamEstimator;
        private string _personDetectionModelFilepath;
        private string _poseEstimationModelFilepath;

        // Common Fields
        private FpsMonitor _fpsMonitor;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Mat _bgrMatForAsync;
        private Mat[] _latestHandPose;
        private Mat _latestPose;
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
            ShowSkeletonToggle.isOn = ShowSkeleton;
            EnableHandPoseEstimationToggle.isOn = EnableHandPoseEstimation;
            EnablePoseEstimationToggle.isOn = EnablePoseEstimation;
            if (SkeletonVisualizerRightHand != null) SkeletonVisualizerRightHand.ShowSkeleton = ShowSkeleton;
            if (SkeletonVisualizerLeftHand != null) SkeletonVisualizerLeftHand.ShowSkeleton = ShowSkeleton;
            if (SkeletonVisualizerBody != null) SkeletonVisualizerBody.ShowSkeleton = ShowSkeleton;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _palmDetectionModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(PALM_DETECTION_MODEL_FILENAME, cancellationToken: _cts.Token);
            _handposeEstimationModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(HANDPOSE_ESTIMATION_MODEL_FILENAME, cancellationToken: _cts.Token);
            _personDetectionModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(PERSON_DETECTION_MODEL_FILENAME, cancellationToken: _cts.Token);
            _poseEstimationModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(POSE_ESTIMATION_MODEL_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);


            // Initialize Hand Pose Estimator
            if (string.IsNullOrEmpty(_palmDetectionModelFilepath) || string.IsNullOrEmpty(_handposeEstimationModelFilepath))
            {
                Debug.LogError("Hand pose model files are not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader] to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
            }
            else
            {
                _handPoseStreamEstimator = new MediaPipeHandPoseStreamEstimator(MediaPipeHandPoseStreamEstimator.HandType.Both, _palmDetectionModelFilepath, _handposeEstimationModelFilepath, 0.6f, 0.9f);
            }

            // Initialize Pose Estimator
            if (string.IsNullOrEmpty(_personDetectionModelFilepath) || string.IsNullOrEmpty(_poseEstimationModelFilepath))
            {
                Debug.LogError("Pose model files are not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader] to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
            }
            else
            {
                _poseStreamEstimator = new MediaPipePoseStreamEstimator(_personDetectionModelFilepath, _poseEstimationModelFilepath, 0.6f, 0.9f);
            }

            _multiSource2MatHelper.Initialize();
        }

        private void Update()
        {
            ProcessMainThreadQueue();

            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_handPoseStreamEstimator == null && _poseStreamEstimator == null)
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
                            _inferenceTask = Task.Run(async () =>
                            {
                                try
                                {
                                    // Hand pose stream estimator inference
                                    Mat[] handPose = null;
                                    if (EnableHandPoseEstimation && _handPoseStreamEstimator != null)
                                    {
                                        handPose = await _handPoseStreamEstimator.EstimateAsync(_bgrMatForAsync);
                                    }

                                    // Pose stream estimator inference
                                    Mat pose = null;
                                    if (EnablePoseEstimation && _poseStreamEstimator != null)
                                    {
                                        pose = await _poseStreamEstimator.EstimateAsync(_bgrMatForAsync, useMask: false);
                                    }

                                    RunOnMainThread(() =>
                                    {
                                        if (_latestHandPose != null)
                                        {
                                            foreach (var result in _latestHandPose)
                                            {
                                                result?.Dispose();
                                            }
                                        }
                                        _latestHandPose = handPose;

                                        _latestPose?.Dispose();
                                        _latestPose = pose;
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

                        // Visualize Hand Pose
                        if (EnableHandPoseEstimation && _handPoseStreamEstimator != null)
                        {
                            _handPoseStreamEstimator.Visualize(rgbaMat, _latestHandPose, false, true);
                        }

                        // Visualize Pose
                        if (EnablePoseEstimation && _poseStreamEstimator != null)
                        {
                            _poseStreamEstimator.Visualize(rgbaMat, _latestPose, false, true);
                        }

                        if (ShowSkeleton)
                        {
                            UpdateSkeleton(_latestHandPose, _latestPose);
                        }
                    }
                    else
                    {
                        // synchronous execution

                        // TickMeter tm = new TickMeter();
                        // tm.start();

                        // Hand pose stream estimator inference
                        Mat[] handPoseResults = null;
                        if (EnableHandPoseEstimation && _handPoseStreamEstimator != null)
                        {
                            handPoseResults = _handPoseStreamEstimator.Estimate(_bgrMat, useCopyOutput: true);
                        }

                        // Pose stream estimator inference
                        Mat poseResult = null;
                        if (EnablePoseEstimation && _poseStreamEstimator != null)
                        {
                            poseResult = _poseStreamEstimator.Estimate(_bgrMat, useCopyOutput: true, useMask: false);
                        }

                        try
                        {
                            // tm.stop();
                            // Debug.Log("MediaPipeHandPoseStreamEstimator Inference time, ms: " + tm.getTimeMilli());

                            Imgproc.cvtColor(_bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                            // Visualize Hand Pose
                            if (EnableHandPoseEstimation && _handPoseStreamEstimator != null)
                            {
                                _handPoseStreamEstimator.Visualize(rgbaMat, handPoseResults, false, true);
                            }

                            // Visualize Pose
                            if (EnablePoseEstimation && _poseStreamEstimator != null)
                            {
                                _poseStreamEstimator.Visualize(rgbaMat, poseResult, false, true);
                            }

                            if (ShowSkeleton)
                                UpdateSkeleton(handPoseResults, poseResult);
                        }
                        finally
                        {
                            // Dispose the results
                            if (handPoseResults != null)
                            {
                                foreach (var result in handPoseResults)
                                {
                                    result?.Dispose();
                                }
                            }
                            poseResult?.Dispose();
                        }
                    }
                }

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        // Public Methods
        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = _multiSource2MatHelper.GetMat();
            Debug.Log("rgbaMat.width() " + rgbaMat.width() + " rgbaMat.height() " + rgbaMat.height());

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
            ArHelperRightHand.Initialize();
            ArHelperLeftHand.Initialize();
            ArHelperBody.Initialize();
            // Set ARCamera parameters.
            ArHelperRightHand.ARCamera.SetARCameraParameters(Screen.width, Screen.height, rgbaMat.width(), rgbaMat.height(), Vector2.zero, new Vector2(imageSizeScale, imageSizeScale));
            ArHelperRightHand.ARCamera.SetCamMatrixValuesFromImageSize();
            ArHelperLeftHand.ARCamera.SetARCameraParameters(Screen.width, Screen.height, rgbaMat.width(), rgbaMat.height(), Vector2.zero, new Vector2(imageSizeScale, imageSizeScale));
            ArHelperLeftHand.ARCamera.SetCamMatrixValuesFromImageSize();
            ArHelperBody.ARCamera.SetARCameraParameters(Screen.width, Screen.height, rgbaMat.width(), rgbaMat.height(), Vector2.zero, new Vector2(imageSizeScale, imageSizeScale));
            ArHelperBody.ARCamera.SetCamMatrixValuesFromImageSize();
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
            if (_latestHandPose != null)
            {
                foreach (var result in _latestHandPose)
                {
                    result?.Dispose();
                }
                _latestHandPose = null;
            }
            _latestPose?.Dispose(); _latestPose = null;

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;

            ArHelperRightHand?.Dispose();
            ArHelperLeftHand?.Dispose();
            ArHelperBody?.Dispose();
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

        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        public void OnPlayButtonClick()
        {
            _multiSource2MatHelper.Play();
        }

        public void OnPauseButtonClick()
        {
            _multiSource2MatHelper.Pause();
        }

        public void OnStopButtonClick()
        {
            _multiSource2MatHelper.Stop();
        }

        public void OnChangeCameraButtonClick()
        {
            _multiSource2MatHelper.RequestedIsFrontFacing = !_multiSource2MatHelper.RequestedIsFrontFacing;
        }

        public void OnShowSkeletonToggleValueChanged()
        {
            if (ShowSkeletonToggle.isOn != ShowSkeleton)
            {
                ShowSkeleton = ShowSkeletonToggle.isOn;
                if (SkeletonVisualizerRightHand != null) SkeletonVisualizerRightHand.ShowSkeleton = ShowSkeleton;
                if (SkeletonVisualizerLeftHand != null) SkeletonVisualizerLeftHand.ShowSkeleton = ShowSkeleton;
                if (SkeletonVisualizerBody != null) SkeletonVisualizerBody.ShowSkeleton = ShowSkeleton;
            }
        }

        public void OnEnableHandPoseEstimationToggleValueChanged()
        {
            if (EnableHandPoseEstimationToggle.isOn != EnableHandPoseEstimation)
            {
                EnableHandPoseEstimation = EnableHandPoseEstimationToggle.isOn;
            }
        }

        public void OnEnablePoseEstimationToggleValueChanged()
        {
            if (EnablePoseEstimationToggle.isOn != EnablePoseEstimation)
            {
                EnablePoseEstimation = EnablePoseEstimationToggle.isOn;
            }
        }

        public void OnUseAsyncInferenceToggleValueChanged()
        {
            if (UseAsyncInferenceToggle.isOn != UseAsyncInference)
            {
                // Wait for inference to complete before changing the toggle
                if (_inferenceTask != null && !_inferenceTask.IsCompleted) _inferenceTask.Wait(500);

                UseAsyncInference = UseAsyncInferenceToggle.isOn;

                _handPoseStreamEstimator?.ResetTracking();
                _poseStreamEstimator?.ResetTracking();
            }
        }

        public void OnEnterARCameraViewport(ARHelper aRHelper, ARCamera arCamera, ARGameObject arGameObject)
        {
            Debug.Log("OnEnterARCamera arCamera.name " + arCamera.name + " arGameObject.name " + arGameObject.name);

            arGameObject.gameObject.SetActive(true);
        }

        public void OnExitARCameraViewport(ARHelper aRHelper, ARCamera arCamera, ARGameObject arGameObject)
        {
            Debug.Log("OnExitARCamera arCamera.name " + arCamera.name + " arGameObject.name " + arGameObject.name);

            arGameObject.gameObject.SetActive(false);
        }

        // Private Methods
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

        private void UpdateSkeleton(Mat[] handPoseResults, Mat humanPoseResult)
        {
            ArHelperRightHand.ResetARGameObjectsImagePointsAndObjectPoints();
            ArHelperLeftHand.ResetARGameObjectsImagePointsAndObjectPoints();
            ArHelperBody.ResetARGameObjectsImagePointsAndObjectPoints();

            // Process Hand Pose
            if (EnableHandPoseEstimation && handPoseResults != null)
            {
                // Process right hand (index 0)
                Mat rightHandPose = handPoseResults[0];
                if (rightHandPose != null && !rightHandPose.empty())
                {
                    ProcessHandPose(rightHandPose, true); // true = right hand
                }

                // Process left hand (index 1)
                Mat leftHandPose = handPoseResults[1];
                if (leftHandPose != null && !leftHandPose.empty())
                {
//leftHandPose.dump
//Debug.Log("leftHandPose.dump() " + leftHandPose.dump());

                    ProcessHandPose(leftHandPose, false); // false = left hand
                }
            }

            // Process Pose
            if (EnablePoseEstimation && humanPoseResult != null && !humanPoseResult.empty())
            {
                ProcessPose(humanPoseResult);
            }
        }

        private void ProcessHandPose(Mat handPose, bool isRightHand)
        {
            HandPoseEstimationBlazeData data = _handPoseStreamEstimator.ToStructuredData(handPose);

#if NET_STANDARD_2_1
            ReadOnlySpan<Vec3f> landmarks_screen = data.GetLandmarksScreen();
            ReadOnlySpan<Vec3f> landmarks_world = data.GetLandmarksWorld();
#else
            Vec3f[] landmarks_screen = data.GetLandmarksScreenArray();
            Vec3f[] landmarks_world = data.GetLandmarksWorldArray();
#endif

            // Create arrays with the required number of elements
            Vector2[] imagePoints = new Vector2[HAND_SELECTED_INDICES.Length];
            Vector3[] objectPoints = new Vector3[HAND_SELECTED_INDICES.Length];

            // Store only the elements at the specified indices
            for (int i = 0; i < HAND_SELECTED_INDICES.Length; i++)
            {
                int index = HAND_SELECTED_INDICES[i];
                ref readonly var landmark_screen = ref landmarks_screen[index];
                ref readonly var landmark_world = ref landmarks_world[index];
                imagePoints[i] = new Vector2(landmark_screen.Item1, landmark_screen.Item2);
                objectPoints[i] = new Vector3(landmark_world.Item1, landmark_world.Item2, landmark_world.Item3);
            }

            if (isRightHand)
            {
                if (SkeletonVisualizerRightHand != null && SkeletonVisualizerRightHand.ShowSkeleton)
                {
                    SkeletonVisualizerRightHand.UpdatePose(handPose);

                    if (ArHelperRightHand.ARGameObjects.Count > 0)
                    {
                        ArHelperRightHand.ARGameObjects[0].ImagePoints = imagePoints;
                        ArHelperRightHand.ARGameObjects[0].ObjectPoints = objectPoints;
                    }
                }
            }
            else
            {
                if (SkeletonVisualizerLeftHand != null && SkeletonVisualizerLeftHand.ShowSkeleton)
                {
                    SkeletonVisualizerLeftHand.UpdatePose(handPose);

                    if (ArHelperLeftHand.ARGameObjects.Count > 0)
                    {
                        ArHelperLeftHand.ARGameObjects[0].ImagePoints = imagePoints;
                        ArHelperLeftHand.ARGameObjects[0].ObjectPoints = objectPoints;
                    }
                }
            }
        }

        private void ProcessPose(Mat humanPose)
        {
            if (SkeletonVisualizerBody != null && SkeletonVisualizerBody.ShowSkeleton)
            {
                SkeletonVisualizerBody.UpdatePose(humanPose);

                MediaPipePoseEstimator.PoseEstimationBlazeData data = _poseStreamEstimator.ToStructuredData(humanPose);
#if NET_STANDARD_2_1
                ReadOnlySpan<MediaPipePoseEstimator.ScreenLandmark> landmarks_screen = data.GetLandmarksScreen();
                ReadOnlySpan<Vec3f> landmarks_world = data.GetLandmarksWorld();
#else
                MediaPipePoseEstimator.ScreenLandmark[] landmarks_screen = data.GetLandmarksScreenArray();
                Vec3f[] landmarks_world = data.GetLandmarksWorldArray();
#endif

                Vector2[] imagePoints = new Vector2[POSE_SELECTED_INDICES.Length];
                Vector3[] objectPoints = new Vector3[POSE_SELECTED_INDICES.Length];

                for (int i = 0; i < POSE_SELECTED_INDICES.Length; i++)
                {
                    int index = POSE_SELECTED_INDICES[i];
                    ref readonly var landmark_screen = ref landmarks_screen[index];
                    ref readonly var landmark_world = ref landmarks_world[index];
                    imagePoints[i] = new Vector2(landmark_screen.X, landmark_screen.Y);
                    objectPoints[i] = new Vector3(landmark_world.Item1, landmark_world.Item2, landmark_world.Item3);
                }

                if (ArHelperBody.ARGameObjects.Count > 0)
                {
                    ArHelperBody.ARGameObjects[0].ImagePoints = imagePoints;
                    ArHelperBody.ARGameObjects[0].ObjectPoints = objectPoints;
                }
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            _handPoseStreamEstimator?.Dispose();
            _poseStreamEstimator?.Dispose();

            OpenCVDebug.SetDebugMode(false);

            _cts?.Dispose();
        }
    }
}

#endif
