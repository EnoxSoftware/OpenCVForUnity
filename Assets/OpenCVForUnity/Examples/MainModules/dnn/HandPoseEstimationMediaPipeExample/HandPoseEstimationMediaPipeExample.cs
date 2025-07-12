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
    /// Hand Pose Estimation MediaPipe Example
    /// An example of using OpenCV dnn module with Hand Pose Estimation.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/master/models/handpose_estimation_mediapipe
    ///
    /// [Tested Models]
    /// https://github.com/opencv/opencv_zoo/raw/6c68bc48c6f96042b29b3425174e431ccac38376/models/palm_detection_mediapipe/palm_detection_mediapipe_2023feb.onnx
    /// https://github.com/opencv/opencv_zoo/raw/05a07912a619f3dd491ba22ca489245c7847c9ff/models/handpose_estimation_mediapipe/handpose_estimation_mediapipe_2023feb.onnx
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class HandPoseEstimationMediaPipeExample : MonoBehaviour
    {
        // Constants
        private static readonly byte[] SELECTED_INDICES = {
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
        private static readonly string PALM_DETECTION_MODEL_FILENAME = "OpenCVForUnityExamples/dnn/palm_detection_mediapipe_2023feb.onnx";
        private static readonly string HANDPOSE_ESTIMATION_MODEL_FILENAME = "OpenCVForUnityExamples/dnn/handpose_estimation_mediapipe_2023feb.onnx";

        // Public Fields
        [Header("UI")]
        public Toggle UseAsyncInferenceToggle;
        public bool UseAsyncInference = false;
        public Toggle ShowSkeletonToggle;
        public bool ShowSkeleton;
        [Space(10)]
        public MediaPipeHandPoseSkeletonVisualizer SkeletonVisualizerRightHand;
        public ARHelper ArHelperRightHand;
        public MediaPipeHandPoseSkeletonVisualizer SkeletonVisualizerLeftHand;
        public ARHelper ArHelperLeftHand;

        // Private Fields
        private Texture2D _texture;
        private MultiSource2MatHelper _multiSource2MatHelper;
        private Mat _bgrMat;
        private MediaPipePalmDetector _palmDetector;
        private MediaPipeHandPoseEstimator _handPoseEstimator;
        private string _palmDetectionModelFilepath;
        private string _handposeEstimationModelFilepath;
        private FpsMonitor _fpsMonitor;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Mat _bgrMatForAsync;
        private Mat _latestDetectedPalms;
        private List<Mat> _latestHandposes;
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
            if (SkeletonVisualizerRightHand != null) SkeletonVisualizerRightHand.ShowSkeleton = ShowSkeleton;
            if (SkeletonVisualizerLeftHand != null) SkeletonVisualizerLeftHand.ShowSkeleton = ShowSkeleton;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _palmDetectionModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(PALM_DETECTION_MODEL_FILENAME, cancellationToken: _cts.Token);
            _handposeEstimationModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(HANDPOSE_ESTIMATION_MODEL_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);


            if (string.IsNullOrEmpty(_palmDetectionModelFilepath))
            {
                Debug.LogError(PALM_DETECTION_MODEL_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
            }
            else
            {
                _palmDetector = new MediaPipePalmDetector(_palmDetectionModelFilepath, 0.3f, 0.6f, 20);
            }

            if (string.IsNullOrEmpty(_handposeEstimationModelFilepath))
            {
                Debug.LogError(HANDPOSE_ESTIMATION_MODEL_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
            }
            else
            {
                _handPoseEstimator = new MediaPipeHandPoseEstimator(_handposeEstimationModelFilepath, 0.9f);
            }

            _multiSource2MatHelper.Initialize();
        }

        private void Update()
        {
            ProcessMainThreadQueue();

            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_palmDetector == null || _handPoseEstimator == null)
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
                                    // Palm detector inference
                                    var newPalms = await _palmDetector.DetectAsync(_bgrMatForAsync);
                                    // Estimate the handpose of each palm
                                    List<Mat> handposes = new List<Mat>();
                                    for (int i = 0; i < newPalms.rows(); ++i)
                                    {
                                        // Handpose estimator inference
                                        using (Mat palm = newPalms.row(i))
                                        {
                                            var handpose = await _handPoseEstimator.EstimateAsync(_bgrMatForAsync, palm);
                                            if (!handpose.empty())
                                                handposes.Add(handpose);
                                        }
                                    }
                                    RunOnMainThread(() =>
                                    {
                                        _latestDetectedPalms?.Dispose();
                                        _latestDetectedPalms = newPalms;
                                        if (_latestHandposes != null)
                                        {
                                            foreach (var handpose in _latestHandposes)
                                                handpose.Dispose();
                                        }
                                        _latestHandposes = handposes;
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

                        if (_latestDetectedPalms != null && _latestHandposes != null)
                        {
                            //_palmDetector.Visualize(rgbaMat, latestDetectedPalms, false, true);
                            foreach (var handpose in _latestHandposes)
                                _handPoseEstimator.Visualize(rgbaMat, handpose, false, true);

                            if (ShowSkeleton)
                                UpdateSkeleton(_latestHandposes);
                        }
                    }
                    else
                    {
                        // synchronous execution

                        // TickMeter tm = new TickMeter();
                        // tm.start();

                        // Palm detector inference
                        using (Mat palms = _palmDetector.Detect(_bgrMat))
                        {
                            // tm.stop();
                            // Debug.Log("MediaPipePalmDetector Inference time, ms: " + tm.getTimeMilli());

                            List<Mat> handposes = new List<Mat>();

                            // Estimate the handpose of each palm
                            for (int i = 0; i < palms.rows(); ++i)
                            {
                                //tm.reset();
                                //tm.start();

                                // Handpose estimator inference
                                using (Mat palm = palms.row(i))
                                {
                                    var handpose = _handPoseEstimator.Estimate(_bgrMat, palm, true);
                                    if (!handpose.empty())
                                        handposes.Add(handpose);
                                }

                                //tm.stop();
                                //Debug.Log("MediaPipeHandPoseEstimator Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());
                            }

                            Imgproc.cvtColor(_bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                            //_palmDetector.Visualize(rgbaMat, palms, false, true);
                            foreach (var handpose in handposes)
                                _handPoseEstimator.Visualize(rgbaMat, handpose, false, true);

                            if (ShowSkeleton)
                                UpdateSkeleton(handposes);

                            palms.Dispose();
                            foreach (var handpose in handposes)
                                handpose.Dispose();
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
            // Set ARCamera parameters.
            ArHelperRightHand.ARCamera.SetARCameraParameters(Screen.width, Screen.height, rgbaMat.width(), rgbaMat.height(), Vector2.zero, new Vector2(imageSizeScale, imageSizeScale));
            ArHelperRightHand.ARCamera.SetCamMatrixValuesFromImageSize();
            ArHelperLeftHand.ARCamera.SetARCameraParameters(Screen.width, Screen.height, rgbaMat.width(), rgbaMat.height(), Vector2.zero, new Vector2(imageSizeScale, imageSizeScale));
            ArHelperLeftHand.ARCamera.SetCamMatrixValuesFromImageSize();
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
            _latestDetectedPalms?.Dispose(); _latestDetectedPalms = null;
            if (_latestHandposes != null)
            {
                foreach (var handpose in _latestHandposes)
                    handpose.Dispose();
                _latestHandposes.Clear();
            }
            _latestHandposes = null;

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;

            ArHelperRightHand?.Dispose(); ArHelperRightHand = null;
            ArHelperLeftHand?.Dispose(); ArHelperLeftHand = null;
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
            }
        }

        public void OnUseAsyncInferenceToggleValueChanged()
        {
            if (UseAsyncInferenceToggle.isOn != UseAsyncInference)
            {
                // Wait for inference to complete before changing the toggle
                if (_inferenceTask != null && !_inferenceTask.IsCompleted) _inferenceTask.Wait(500);

                UseAsyncInference = UseAsyncInferenceToggle.isOn;
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

        private void UpdateSkeleton(List<Mat> handposes)
        {
            if (handposes == null || handposes.Count == 0)
                return;

            ArHelperRightHand.ResetARGameObjectsImagePointsAndObjectPoints();
            ArHelperLeftHand.ResetARGameObjectsImagePointsAndObjectPoints();

            foreach (var handpose in handposes)
            {
                HandPoseEstimationBlazeData data = _handPoseEstimator.ToStructuredData(handpose);

#if NET_STANDARD_2_1
                ReadOnlySpan<Vec3f> landmarks_screen = data.GetLandmarksScreen();
                ReadOnlySpan<Vec3f> landmarks_world = data.GetLandmarksWorld();
#else
                Vec3f[] landmarks_screen = data.GetLandmarksScreenArray();
                Vec3f[] landmarks_world = data.GetLandmarksWorldArray();
#endif

                // Create arrays with the required number of elements
                Vector2[] imagePoints = new Vector2[SELECTED_INDICES.Length];
                Vector3[] objectPoints = new Vector3[SELECTED_INDICES.Length];

                // Store only the elements at the specified indices
                for (int i = 0; i < SELECTED_INDICES.Length; i++)
                {
                    int index = SELECTED_INDICES[i];
                    ref readonly var landmark_screen = ref landmarks_screen[index];
                    ref readonly var landmark_world = ref landmarks_world[index];
                    imagePoints[i] = new Vector2(landmark_screen.Item1, landmark_screen.Item2);
                    objectPoints[i] = new Vector3(landmark_world.Item1, landmark_world.Item2, landmark_world.Item3);
                }

                if (data.Handedness <= 0.5f) // Left
                {
                    if (SkeletonVisualizerLeftHand != null && SkeletonVisualizerLeftHand.ShowSkeleton)
                    {
                        SkeletonVisualizerLeftHand.UpdatePose(handpose);

                        ArHelperLeftHand.ARGameObjects[0].ImagePoints = imagePoints;
                        ArHelperLeftHand.ARGameObjects[0].ObjectPoints = objectPoints;
                    }
                }
                else // Right
                {
                    if (SkeletonVisualizerRightHand != null && SkeletonVisualizerRightHand.ShowSkeleton)
                    {
                        SkeletonVisualizerRightHand.UpdatePose(handpose);

                        ArHelperRightHand.ARGameObjects[0].ImagePoints = imagePoints;
                        ArHelperRightHand.ARGameObjects[0].ObjectPoints = objectPoints;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            _palmDetector?.Dispose();
            _handPoseEstimator?.Dispose();

            OpenCVDebug.SetDebugMode(false);

            _cts?.Dispose();
        }
    }
}

#endif
