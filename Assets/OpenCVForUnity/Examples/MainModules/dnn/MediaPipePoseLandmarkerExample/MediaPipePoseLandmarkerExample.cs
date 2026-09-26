#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.Runner;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.Extensions.Worker.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.AR;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using OpenCVForUnity.UnityIntegration.Worker.DnnModule;
using OpenCVForUnity.UnityIntegration.Worker.DnnModule.MediaPipe;
using OpenCVForUnity.UnityIntegration.Worker.DnnModule.MediaPipe.SkeletonVisualizer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenCVDebug = OpenCVForUnity.Extensions.OpenCVDebug;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// MediaPipe Pose Landmarker Example
    /// Estimates full-body pose with 33 landmarks from input frames with optional 3D skeleton visualization.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Loading pose detection and landmark ONNX/Sentis models from StreamingAssets
    /// - Toggling Sentis vs OpenCV DNN inference and optional async detection
    /// - Converting RGBA frames to BGR, running MediaPipePoseLandmarkerMultiBackend, and visualizing landmarks on Mat
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Size"/>, <see cref="Scalar"/>, <see cref="Point"/>
    /// - <see cref="Imgproc"/>: cvtColor
    /// - <see cref="MediaPipePoseLandmarkerMultiBackend"/>, <see cref="MatSingleFlightSyncAsyncRunner"/>, <see cref="MultiBackendDnn"/>
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://github.com/google-ai-edge/mediapipe
    /// </para>
    /// <para>
    /// [Tested Models]
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipePoseLandmarkerExample/pose_detector.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipePoseLandmarkerExample/full_pose_landmarks_detector.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipePoseLandmarkerExample/heavy_pose_landmarks_detector.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipePoseLandmarkerExample/lite_pose_landmarks_detector.onnx
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class MediaPipePoseLandmarkerExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// Thirteen PnP correspondences from nose to ankles, assuming the full body is in frame. Does not use visibility; uses every index that is in range.
        /// </summary>
        private static readonly byte[] SELECTED_INDICES =
        {
            (byte)MediaPipePoseLandmarkerMultiBackend.KeyPoint.Nose,
            (byte)MediaPipePoseLandmarkerMultiBackend.KeyPoint.LeftShoulder,
            (byte)MediaPipePoseLandmarkerMultiBackend.KeyPoint.RightShoulder,
            (byte)MediaPipePoseLandmarkerMultiBackend.KeyPoint.LeftHip,
            (byte)MediaPipePoseLandmarkerMultiBackend.KeyPoint.RightHip,
            (byte)MediaPipePoseLandmarkerMultiBackend.KeyPoint.LeftElbow,
            (byte)MediaPipePoseLandmarkerMultiBackend.KeyPoint.RightElbow,
            (byte)MediaPipePoseLandmarkerMultiBackend.KeyPoint.LeftKnee,
            (byte)MediaPipePoseLandmarkerMultiBackend.KeyPoint.RightKnee,
            (byte)MediaPipePoseLandmarkerMultiBackend.KeyPoint.LeftWrist,
            (byte)MediaPipePoseLandmarkerMultiBackend.KeyPoint.RightWrist,
            (byte)MediaPipePoseLandmarkerMultiBackend.KeyPoint.LeftAnkle,
            (byte)MediaPipePoseLandmarkerMultiBackend.KeyPoint.RightAnkle,
        };

        // Public Fields
        [Header("UI")]
        [Tooltip("Inference framework selector. Options are built at runtime from InferenceFrameworkUtils.GetSelectionValuesInEnumOrder(). Assign OnInferenceFrameworkDropdownValueChanged to On Value Changed (int).")]
        public Dropdown InferenceFrameworkDropdown;

        [Tooltip("Sentis inference target selector (GPU Compute / GPU Pixel / CPU). Options are built at runtime from SentisInferenceUtils.GetTargetValuesInEnumOrder(). Assign OnSentisInferenceTargetDropdownValueChanged to On Value Changed (int). Value changes reinitialize inference.")]
        public Dropdown SentisInferenceTargetDropdown;

        [Tooltip("Selected inference framework (OpenCV DNN or Unity Sentis). When Unity Sentis is selected, Inspector model paths may stay .onnx; at runtime they are rewritten to .sentis and loaded from StreamingAssets (place a matching .sentis beside the onnx file).")]
        public InferenceFrameworkSelectionKind InferenceFramework = InferenceFrameworkSelectionKind.UnitySentis;

        [Tooltip("When using Sentis: selects the Sentis inference target (GPU Compute / GPU Pixel / CPU).")]
        public SentisInferenceTargetKind SentisInferenceTarget = SentisInferenceTargetKind.GPUCompute;

        [Tooltip("When enabled, submits inference work asynchronously. Assign OnUseAsyncInferenceToggleValueChanged to On Value Changed.")]
        public Toggle UseAsyncInferenceToggle;

        public bool UseAsyncInference = true;
        public Toggle ShowSkeletonToggle;
        public bool ShowSkeleton;

        [Header("Inference")]
        [Tooltip("StreamingAssets-relative path to the person/pose detector model (poseDetectorModelFilepath).")]
        public string PoseLandmarkerPoseDetectorModelFileName = "OpenCVForUnityExamples/dnn/mediapipe/pose_detector.onnx";
        [Tooltip("StreamingAssets-relative path to the pose landmarks model (poseLandmarksModelFilepath).")]
        public string PoseLandmarkerPoseLandmarksModelFileName = "OpenCVForUnityExamples/dnn/mediapipe/full_pose_landmarks_detector.onnx";
        [Tooltip("MediaPipePoseLandmarkerMultiBackend running mode. IMAGE is single-shot inference; VIDEO is for continuous frames.")]
        public MediaPipePoseLandmarkerMultiBackend.MediaPipePoseRunningMode PoseLandmarkerRunningMode = MediaPipePoseLandmarkerMultiBackend.MediaPipePoseRunningMode.VIDEO;
        [Tooltip("numPoses when initializing MediaPipePoseLandmarkerMultiBackend (maximum number of poses to detect simultaneously).")]
        [Range(1, 10)]
        public int PoseLandmarkerNumPoses = 1;
        [Tooltip("Pose detection confidence threshold (minPoseDetectionConfidence).")]
        [Range(0f, 1f)]
        public float PoseLandmarkerMinPoseDetectionConfidence = 0.5f;
        [Tooltip("Pose presence confidence threshold (minPosePresenceConfidence).")]
        [Range(0f, 1f)]
        public float PoseLandmarkerMinPosePresenceConfidence = 0.5f;
        [Tooltip("Tracking confidence threshold (minTrackingConfidence).")]
        [Range(0f, 1f)]
        public float PoseLandmarkerMinTrackingConfidence = 0.5f;
        [Tooltip("Pose Landmarker outputSegmentationMasks setting: whether to include a segmentation mask in the inference result.")]
        public bool PoseLandmarkerOutputSegmentationMasks = false;

        [Header("Visualize")]
        [Tooltip("Whether Pose Landmarker Visualize calls print results to the console.")]
        public bool PoseLandmarkerVisualizePrintResult = false;

        [Space(10)]
        [Header("Show 3D Skeleton")]
        public ARHelper ArHelper;
        public MediaPipePoseSkeletonVisualizer SkeletonVisualizer;

        // Private Fields
        private Texture2D _texture;
        private float _imageSizeScale = 1f;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private Mat _bgrMat;
        private MediaPipePoseLandmarkerMultiBackend _poseLandmarkerWorker;
        private string _poseLandmarkerPersonDetectionModelFilepathOnnx;
        private string _poseLandmarkerEstimationModelFilepathOnnx;
        private string _poseLandmarkerPersonDetectionModelFilepathSentis;
        private string _poseLandmarkerEstimationModelFilepathSentis;
        private bool _inferenceReinitializing;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private MatSingleFlightSyncAsyncRunner _inferenceRunner;

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            WireSourceToMatControlPanelHooks();

            UpdateUseAsyncInference();
            SyncInferenceModeUi(inferenceReinitializing: false);
            if (ShowSkeletonToggle != null)
            {
                ShowSkeletonToggle.SetIsOnWithoutNotify(ShowSkeleton);
            }

            if (SkeletonVisualizer != null)
            {
                SkeletonVisualizer.ShowSkeleton = ShowSkeleton;
            }

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            _poseLandmarkerPersonDetectionModelFilepathOnnx = await OpenCVForUnityEnv.GetFilePathAsync(
                PoseLandmarkerPoseDetectorModelFileName,
                cancellationToken: _cts.Token);
            _poseLandmarkerEstimationModelFilepathOnnx = await OpenCVForUnityEnv.GetFilePathAsync(
                PoseLandmarkerPoseLandmarksModelFileName,
                cancellationToken: _cts.Token);
            if (OpenCVForUnityEnv.IsSentisIntegrationAvailable)
            {
                // Resolve companion .sentis paths when Sentis integration is enabled.
                _poseLandmarkerPersonDetectionModelFilepathSentis = await OpenCVForUnityEnv.GetFilePathAsync(
                    MultiBackendDnn.ResolveSentisModelPathFromOnnxPath(PoseLandmarkerPoseDetectorModelFileName),
                    cancellationToken: _cts.Token);
                _poseLandmarkerEstimationModelFilepathSentis = await OpenCVForUnityEnv.GetFilePathAsync(
                    MultiBackendDnn.ResolveSentisModelPathFromOnnxPath(PoseLandmarkerPoseLandmarksModelFileName),
                    cancellationToken: _cts.Token);
            }

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            if (!TryInitializeInference())
            {
                return;
            }

            _multiSourceToMatHelper.Initialize();
        }

        private async void OnDestroy()
        {
            UnwireSourceToMatControlPanelHooks();

            _cts?.Cancel();

            await DisposeInferenceAsync();

            OpenCVDebug.SetDebugMode(false);

            _cts?.Dispose();
            _cts = null;
        }

        // Public Methods
        /// <summary>
        /// Raises the helper frame mat updated event.
        /// Updates the preview texture when a new frame is available during playback.
        /// </summary>
        public void OnSourceToMatHelperFrameMatUpdated()
        {
            if (_inferenceReinitializing)
            {
                return;
            }

            if (!_multiSourceToMatHelper.IsPlaying)
            {
                return;
            }

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;
            // Convert RGBA camera frame to BGR for MediaPipe pose landmarker input.
            Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

            if (_inferenceRunner != null && _poseLandmarkerWorker != null)
            {
                // Submit sync or async pose landmark detection; output Mat holds landmark results.
                _inferenceRunner.SubmitWork(
                    _bgrMat,
                    syncWork: m => _poseLandmarkerWorker.Detect(m, useCopyOutput: true),
                    asyncWork: async m =>
                    {
                        CancellationToken ct = _inferenceRunner.InFlightAsyncWorkCancellationToken;
                        return await _poseLandmarkerWorker.DetectAsync(m, ct);
                    });
                if (_inferenceRunner.TryGetLatestResult(out Mat[] poseLandmarkerResults))
                {
                    UpdateSkeletonFromPoseLandmarkerResults(poseLandmarkerResults);

                    if (poseLandmarkerResults.Length > 0 && poseLandmarkerResults[0] != null && !poseLandmarkerResults[0].empty())
                    {
                        VisualizePoseLandmarkerOnRgba(rgbaMat, poseLandmarkerResults);
                    }
                }
            }

            // Publish annotated RGBA Mat to Unity texture for RawImage preview.
            OpenCVMatUnityUtils.MatToTexture2D(rgbaMat, _texture);
        }

        /// <summary>
        /// Raises the helper initialized event.
        /// Recreates the preview texture and starts playback on first initialization.
        /// Skips Play when re-initialization has already restored Playing or Paused.
        /// </summary>
        public async void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized", this);

            ResetArAndSkeletonForSourceChange();

            // Start() already called TryInitializeInference before the first Initialize(); re-init only after Release.
            if (_poseLandmarkerWorker == null || _inferenceRunner == null)
            {
                await ReinitializeInferenceForSourceChangeAsync();
            }

            RecreatePreviewTexture();
            CreateOrRecreateProcessingResources(_multiSourceToMatHelper.FrameMat);

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;

            // Initialize ARHelper.
            ArHelper.Initialize();
            // Set ARCamera parameters.
            ArHelper.ARCamera.SetARCameraParameters(Screen.width, Screen.height, rgbaMat.width(), rgbaMat.height(), Vector2.zero, new Vector2(_imageSizeScale, _imageSizeScale));
            ArHelper.ARCamera.SetCamMatrixValuesFromImageSize();

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Clear();
                UpdateFpsMonitorPlaybackState();
                _fpsMonitor.Add("HelperKind", _multiSourceToMatHelper.RequestedHelperKind.ToString());
                _fpsMonitor.Add("Width", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("Height", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("Rotate90Degree", _multiSourceToMatHelper.Rotate90Degree.ToString());
                _fpsMonitor.Add("FlipVertical", _multiSourceToMatHelper.FlipVertical.ToString());
                _fpsMonitor.Add("FlipHorizontal", _multiSourceToMatHelper.FlipHorizontal.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _poseLandmarkerWorker, UseAsyncInference, InferenceFramework);
            }

            if (!_multiSourceToMatHelper.IsPlaying && !_multiSourceToMatHelper.IsPaused)
            {
                _multiSourceToMatHelper.Play();
                UpdateFpsMonitorPlaybackState();
            }
        }

        /// <summary>
        /// Raises the helper frame mat layout changed event.
        /// Recreates the preview texture when rotation or output size changes.
        /// </summary>
        public async void OnSourceToMatHelperFrameMatLayoutChanged()
        {
            Debug.Log("OnSourceToMatHelperFrameMatLayoutChanged", this);

            ResetArAndSkeletonForSourceChange();
            await ReinitializeInferenceForSourceChangeAsync();

            RecreatePreviewTexture();
            CreateOrRecreateProcessingResources(_multiSourceToMatHelper.FrameMat);

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;

            // Initialize ARHelper.
            ArHelper.Initialize();
            // Set ARCamera parameters.
            ArHelper.ARCamera.SetARCameraParameters(Screen.width, Screen.height, rgbaMat.width(), rgbaMat.height(), Vector2.zero, new Vector2(_imageSizeScale, _imageSizeScale));
            ArHelper.ARCamera.SetCamMatrixValuesFromImageSize();

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("Width", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("Height", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
            }
        }

        /// <summary>
        /// Raises the helper released event.
        /// </summary>
        public async void OnSourceToMatHelperReleased()
        {
            Debug.Log("OnSourceToMatHelperReleased", this);

            ResetArAndSkeletonForSourceChange();
            await DisposeInferenceAsync();

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Clear();
            }

            DisposeFrameProcessingResources();
            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed", this);

            _inferenceRunner?.Cancel();

            _poseLandmarkerWorker?.Cancel();

            if (ArHelper != null)
            {
                ArHelper.Dispose();
            }

            DisposeFrameProcessingResources();
            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(SourceToMatErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message, this);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        /// <summary>
        /// Raises the back button click event.
        /// Stops playback and disposes the helper before scene transition (required on WebGL).
        /// </summary>
        public async void OnBackButtonClick()
        {
            if (_multiSourceToMatHelper.IsPlaying || _multiSourceToMatHelper.IsPaused)
            {
                await _multiSourceToMatHelper.StopAsync();
            }

            await _multiSourceToMatHelper.DisposeAsync();

            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterPlay"/>.
        /// </summary>
        public void OnControlPanelAfterPlay()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterPause"/>.
        /// </summary>
        public void OnControlPanelAfterPause()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterStop"/>.
        /// </summary>
        public void OnControlPanelAfterStop()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnRotate90Changed"/>.
        /// </summary>
        /// <param name="isOn">New Rotate90Degree value applied by the panel.</param>
        public void OnControlPanelRotate90Changed(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("Rotate90Degree", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnFlipVerticalChanged"/>.
        /// </summary>
        /// <param name="isOn">New FlipVertical value applied by the panel.</param>
        public void OnControlPanelFlipVerticalChanged(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("FlipVertical", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnFlipHorizontalChanged"/>.
        /// </summary>
        /// <param name="isOn">New FlipHorizontal value applied by the panel.</param>
        public void OnControlPanelFlipHorizontalChanged(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("FlipHorizontal", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnHelperKindChanged"/>.
        /// </summary>
        /// <param name="kindIndex">Dropdown index matching <see cref="MultiSourceHelperKind"/>.</param>
        public void OnControlPanelHelperKindChanged(int kindIndex)
        {
            if (_fpsMonitor == null || !Enum.IsDefined(typeof(MultiSourceHelperKind), kindIndex))
            {
                return;
            }

            _fpsMonitor.Add("HelperKind", ((MultiSourceHelperKind)kindIndex).ToString());
        }

        /// <summary>
        /// Raises the show skeleton toggle value changed event.
        /// </summary>
        public void OnShowSkeletonToggleValueChanged()
        {
            if (ShowSkeletonToggle.isOn != ShowSkeleton)
            {
                ShowSkeleton = ShowSkeletonToggle.isOn;
                if (SkeletonVisualizer != null)
                {
                    SkeletonVisualizer.ShowSkeleton = ShowSkeleton;
                }
            }
        }

        /// <summary>
        /// Invoke from <c>InferenceFrameworkDropdown</c> On Value Changed. Switches the inference framework.
        /// </summary>
        public async void OnInferenceFrameworkDropdownValueChanged(int index)
        {
            if (InferenceFrameworkDropdown == null || _inferenceReinitializing)
            {
                return;
            }

            InferenceFrameworkSelectionKind[] kinds =
                InferenceFrameworkUtils.GetSelectionValuesInEnumOrder();
            if (kinds.Length == 0)
            {
                return;
            }

            InferenceFrameworkSelectionKind newFramework =
                kinds[Mathf.Clamp(index, 0, kinds.Length - 1)];
            if (newFramework == InferenceFramework)
            {
                return;
            }

            await ReinitializeInferenceAsync(() => InferenceFramework = newFramework);
        }

        /// <summary>
        /// Invoke from <c>SentisInferenceTargetDropdown</c> On Value Changed.
        /// Switches Sentis inference target and reinitializes inference.
        /// </summary>
        public async void OnSentisInferenceTargetDropdownValueChanged(int index)
        {
            if (SentisInferenceTargetDropdown == null || _inferenceReinitializing)
            {
                return;
            }

            SentisInferenceTargetKind[] targetKinds = SentisInferenceUtils.GetTargetValuesInEnumOrder();
            if (targetKinds.Length == 0)
            {
                return;
            }

            SentisInferenceTargetKind newTarget =
                targetKinds[Mathf.Clamp(index, 0, targetKinds.Length - 1)];
            if (newTarget == SentisInferenceTarget)
            {
                return;
            }

            await ReinitializeInferenceAsync(() => SentisInferenceTarget = newTarget);
        }

        /// <summary>
        /// Invoke from <c>UseAsyncInferenceToggle</c> On Value Changed.
        /// Toggles async inference on the active runner without reinitializing the detector.
        /// </summary>
        public void OnUseAsyncInferenceToggleValueChanged()
        {
            if (_inferenceReinitializing)
            {
                return;
            }

            if (UseAsyncInferenceToggle.isOn != UseAsyncInference)
            {
                if (_inferenceRunner != null)
                {
                    _inferenceRunner.UseAsyncWork = UseAsyncInferenceToggle.isOn;
                }

                UseAsyncInference = UseAsyncInferenceToggle.isOn;
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _poseLandmarkerWorker, UseAsyncInference, InferenceFramework);
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
            Debug.Log("OnEnterARCamera arCamera.name " + arCamera.name + " arGameObject.name " + arGameObject.name, this);

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
            Debug.Log("OnExitARCamera arCamera.name " + arCamera.name + " arGameObject.name " + arGameObject.name, this);

            arGameObject.gameObject.SetActive(false);
        }

        // Private Methods
        /// <summary>
        /// Resets AR tracking and skeleton renderers when <see cref="MultiSourceToMatHelper"/>
        /// releases or re-initializes the input (OnReleased / OnInitialized / OnFrameMatLayoutChanged).
        /// </summary>
        private void ResetArAndSkeletonForSourceChange()
        {
            if (ArHelper != null)
            {
                ArHelper.ResetARGameObjectsTrackingState();
            }

            if (SkeletonVisualizer != null)
            {
                SkeletonVisualizer.ResetVisualization();
            }
        }

        private void RecreatePreviewTexture()
        {
            Mat frameMat = _multiSourceToMatHelper.FrameMat;
            if (frameMat == null)
            {
                return;
            }

            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
                _texture = null;
            }

            Debug.Log("rgbaMat.width() " + frameMat.width() + " rgbaMat.height() " + frameMat.height(), this);

            _texture = new Texture2D(frameMat.cols(), frameMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUnityUtils.MatToTexture2D(frameMat, _texture);

            // Set the Texture2D as the main texture of the Renderer component attached to the game object
            gameObject.GetComponent<Renderer>().material.mainTexture = _texture;

            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation, this);

            // Set the camera's orthographicSize to half of the texture height
            Camera.main.orthographicSize = _texture.height / 2f;

            // Get the camera's aspect ratio
            float cameraAspect = Camera.main.aspect;

            // Get the texture's aspect ratio
            float textureAspect = (float)_texture.width / _texture.height;

            // Calculate imageSizeScale
            if (textureAspect > cameraAspect)
            {
                // Calculate the camera width (height is already fixed)
                float cameraWidth = Camera.main.orthographicSize * 2f * cameraAspect;

                // Scale so that the texture width fits within the camera width
                _imageSizeScale = cameraWidth / _texture.width;
            }
            else
            {
                // Scale so that the texture height fits within the camera height
                _imageSizeScale = 1f; // No scaling needed since height is already fixed
            }
            Debug.Log("imageSizeScale " + _imageSizeScale, this);

            // The calculated imageSizeScale is used to set the scale of the game object on which the texture is displayed.
            transform.localScale = new Vector3(_texture.width * _imageSizeScale, _texture.height * _imageSizeScale, 1);
        }

        private void DisposeFrameProcessingResources()
        {
            _bgrMat?.Dispose();
            _bgrMat = null;
        }

        private void CreateOrRecreateProcessingResources(Mat frameMat)
        {
            if (frameMat == null)
            {
                return;
            }

            DisposeFrameProcessingResources();

            _bgrMat = new Mat(frameMat.rows(), frameMat.cols(), CvType.CV_8UC3);
        }

        private void CleanupPreviewResources()
        {
            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
                _texture = null;
            }

            UpdateFpsMonitorPlaybackState();
        }

        private void UpdateFpsMonitorPlaybackState()
        {
            if (_fpsMonitor == null || _multiSourceToMatHelper == null)
            {
                return;
            }

            _fpsMonitor.Add("PlaybackState", GetPlaybackStateText());
        }

        private string GetPlaybackStateText()
        {
            if (!_multiSourceToMatHelper.IsInitialized)
            {
                return "Uninitialized";
            }

            if (_multiSourceToMatHelper.IsPlaying)
            {
                return "Playing";
            }

            if (_multiSourceToMatHelper.IsPaused)
            {
                return "Paused";
            }

            return "Ready";
        }

        private void WireSourceToMatControlPanelHooks()
        {
            _controlPanel = GetComponent<SourceToMatControlPanel>();
            if (_controlPanel == null)
            {
                return;
            }

            _controlPanel.OnAfterPlay.AddListener(OnControlPanelAfterPlay);
            _controlPanel.OnAfterPause.AddListener(OnControlPanelAfterPause);
            _controlPanel.OnAfterStop.AddListener(OnControlPanelAfterStop);
            _controlPanel.OnRotate90Changed.AddListener(OnControlPanelRotate90Changed);
            _controlPanel.OnFlipVerticalChanged.AddListener(OnControlPanelFlipVerticalChanged);
            _controlPanel.OnFlipHorizontalChanged.AddListener(OnControlPanelFlipHorizontalChanged);
            _controlPanel.OnHelperKindChanged.AddListener(OnControlPanelHelperKindChanged);
        }

        private void UnwireSourceToMatControlPanelHooks()
        {
            if (_controlPanel == null)
            {
                return;
            }

            _controlPanel.OnAfterPlay.RemoveListener(OnControlPanelAfterPlay);
            _controlPanel.OnAfterPause.RemoveListener(OnControlPanelAfterPause);
            _controlPanel.OnAfterStop.RemoveListener(OnControlPanelAfterStop);
            _controlPanel.OnRotate90Changed.RemoveListener(OnControlPanelRotate90Changed);
            _controlPanel.OnFlipVerticalChanged.RemoveListener(OnControlPanelFlipVerticalChanged);
            _controlPanel.OnFlipHorizontalChanged.RemoveListener(OnControlPanelFlipHorizontalChanged);
            _controlPanel.OnHelperKindChanged.RemoveListener(OnControlPanelHelperKindChanged);
            _controlPanel = null;
        }

        /// <summary>
        /// Updates <paramref name="fpsMonitor"/> with dnn backend, target, and async mode from
        /// <paramref name="worker"/> and <paramref name="useAsyncInference"/> (or "-" when a value is not available).
        /// </summary>
        private static void UpdateFpsMonitorInferenceInfo(
            FpsMonitor fpsMonitor,
            OpenCVForUnity.Extensions.Worker.DnnModule.DnnInferenceWorkerBase worker,
            bool useAsyncInference,
            InferenceFrameworkSelectionKind inferenceFramework)
        {
            if (fpsMonitor == null)
            {
                return;
            }

            fpsMonitor.Add(
                "inferenceFramework",
                InferenceFrameworkUtils.GetSelectionDisplayName(inferenceFramework));

            if (worker != null)
            {
                int be = worker.DnnBackend;
                int tgt = worker.DnnTarget;
                fpsMonitor.Add("dnnBackend", MultiBackendNet.GetBackendDisplayString(be));
                fpsMonitor.Add("dnnTarget", MultiBackendNet.GetTargetDisplayString(tgt));
            }
            else
            {
                fpsMonitor.Add("dnnBackend", "-");
                fpsMonitor.Add("dnnTarget", "-");
            }

            string useAsyncText = worker != null
                ? useAsyncInference.ToString()
                : "-";
            fpsMonitor.Add("useAsyncInference", useAsyncText);
        }

        // Inference UI
        /// <summary>
        /// Locks inference UI during reinitialization, otherwise syncs the async toggle and
        /// delegates Framework / Target dropdown state to <see cref="UpdateInferenceFramework"/>.
        /// </summary>
        private void SyncInferenceModeUi(bool inferenceReinitializing)
        {
            if (inferenceReinitializing)
            {
                if (InferenceFrameworkDropdown != null)
                {
                    InferenceFrameworkDropdown.interactable = false;
                }

                if (SentisInferenceTargetDropdown != null)
                {
                    SentisInferenceTargetDropdown.interactable = false;
                }

                if (UseAsyncInferenceToggle != null)
                {
                    UseAsyncInferenceToggle.interactable = false;
                }

                return;
            }

            if (UseAsyncInferenceToggle != null)
            {
                UseAsyncInferenceToggle.SetIsOnWithoutNotify(UseAsyncInference);
                UseAsyncInferenceToggle.interactable = true;
            }

            UpdateInferenceFramework();
        }

        /// <summary>
        /// Applies framework fallback when Sentis is unavailable, resolves the default Sentis target,
        /// and populates Framework / Target dropdown options from Utils.
        /// </summary>
        private void UpdateInferenceFramework()
        {

            InferenceFrameworkSelectionKind[] kinds =
                InferenceFrameworkUtils.GetSelectionValuesInEnumOrder();
            if (Array.IndexOf(kinds, InferenceFramework) < 0 && kinds.Length > 0)
            {
                InferenceFramework = kinds[0];
            }

            if (OpenCVForUnityEnv.IsSentisIntegrationAvailable)
            {
                SentisInferenceTarget =
                    SentisInferenceUtils.ResolveDefaultTargetForDevice(SentisInferenceTarget);
            }

            PopulateInferenceFrameworkDropdown();
            PopulateSentisInferenceTargetDropdown();

            bool useSentis = InferenceFramework == InferenceFrameworkSelectionKind.UnitySentis;
            if (SentisInferenceTargetDropdown != null)
            {
                SentisInferenceTargetDropdown.interactable =
                    OpenCVForUnityEnv.IsSentisIntegrationAvailable && useSentis;
            }
        }

        /// <summary>
        /// Builds Framework dropdown options from <see cref="InferenceFrameworkUtils.GetSelectionValuesInEnumOrder"/>.
        /// </summary>
        private void PopulateInferenceFrameworkDropdown()
        {
            if (InferenceFrameworkDropdown == null)
            {
                return;
            }

            InferenceFrameworkSelectionKind[] kinds =
                InferenceFrameworkUtils.GetSelectionValuesInEnumOrder();

            InferenceFrameworkDropdown.ClearOptions();
            for (int i = 0; i < kinds.Length; i++)
            {
                InferenceFrameworkDropdown.options.Add(new Dropdown.OptionData(
                    InferenceFrameworkUtils.GetSelectionDisplayName(kinds[i])));
            }

            int idx = Array.IndexOf(kinds, InferenceFramework);
            InferenceFrameworkDropdown.SetValueWithoutNotify(Mathf.Max(0, idx));
            InferenceFrameworkDropdown.RefreshShownValue();
            InferenceFrameworkDropdown.interactable = kinds.Length > 1;
        }

        /// <summary>
        /// Builds Sentis Target dropdown options from <see cref="SentisInferenceUtils.GetTargetValuesInEnumOrder"/>.
        /// </summary>
        private void PopulateSentisInferenceTargetDropdown()
        {
            if (SentisInferenceTargetDropdown == null)
            {
                return;
            }

            SentisInferenceTargetKind[] targetKinds =
                SentisInferenceUtils.GetTargetValuesInEnumOrder();
            SentisInferenceTargetDropdown.ClearOptions();
            for (int i = 0; i < targetKinds.Length; i++)
            {
                SentisInferenceTargetDropdown.options.Add(new Dropdown.OptionData(
                    SentisInferenceUtils.GetTargetDisplayName(targetKinds[i])));
            }

            int idx = Array.IndexOf(targetKinds, SentisInferenceTarget);
            if (idx < 0 && targetKinds.Length > 0)
            {
                SentisInferenceTarget = targetKinds[0];
                idx = 0;
            }

            SentisInferenceTargetDropdown.SetValueWithoutNotify(Mathf.Max(0, idx));
            SentisInferenceTargetDropdown.RefreshShownValue();
        }

        /// <summary>
        /// Reserved hook for synchronizing <see cref="UseAsyncInference"/> with platform capabilities.
        /// Does not modify <see cref="UseAsyncInference"/> in this example.
        /// </summary>
        private void UpdateUseAsyncInference()
        {
        }

        /// <summary>
        /// Fully re-initializes the pose landmarker worker and inference runner after
        /// <see cref="MultiSourceToMatHelper"/> input or layout changes.
        /// </summary>
        private Task ReinitializeInferenceForSourceChangeAsync()
        {
            return ReinitializeInferenceAsync(() => { });
        }

        private async Task ReinitializeInferenceAsync(Action applySelection)
        {
            _inferenceReinitializing = true;
            SyncInferenceModeUi(inferenceReinitializing: true);

            await DisposeInferenceAsync();

            applySelection();
            UpdateUseAsyncInference();
            TryInitializeInference();
            UpdateFpsMonitorInferenceInfo(_fpsMonitor, _poseLandmarkerWorker, UseAsyncInference, InferenceFramework);

            _inferenceReinitializing = false;
            SyncInferenceModeUi(inferenceReinitializing: false);
        }

        private async Task DisposeInferenceAsync()
        {
            var runner = _inferenceRunner;
            _inferenceRunner = null;
            if (runner != null)
            {
                await runner.DisposeAsync();
            }

            var poseLandmarkerWorker = _poseLandmarkerWorker;
            _poseLandmarkerWorker = null;
            if (poseLandmarkerWorker != null)
            {
                await poseLandmarkerWorker.DisposeAsync();
            }
        }

        /// <summary>
        /// Initializes inference from the resolved model path and current backend settings (Sentis asset path when using Sentis; otherwise ONNX).
        /// </summary>
        private bool TryInitializeInference()
        {
            bool useSentis = InferenceFramework == InferenceFrameworkSelectionKind.UnitySentis && OpenCVForUnityEnv.IsSentisIntegrationAvailable;
            string detPath = useSentis ? _poseLandmarkerPersonDetectionModelFilepathSentis : _poseLandmarkerPersonDetectionModelFilepathOnnx;
            string lmPath = useSentis ? _poseLandmarkerEstimationModelFilepathSentis : _poseLandmarkerEstimationModelFilepathOnnx;
            if (string.IsNullOrEmpty(detPath) || string.IsNullOrEmpty(lmPath))
            {
                Debug.LogError(PoseLandmarkerPoseDetectorModelFileName + " or " + PoseLandmarkerPoseLandmarksModelFileName + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "model file is not loaded.\nPlease read console message.";
                }
                return false;
            }

            try
            {
                if (useSentis)
                {
                    _poseLandmarkerWorker = new MediaPipePoseLandmarkerMultiBackend(
                        detPath,
                        lmPath,
                        PoseLandmarkerRunningMode,
                        numPoses: Mathf.Max(1, PoseLandmarkerNumPoses),
                        minPoseDetectionConfidence: PoseLandmarkerMinPoseDetectionConfidence,
                        minPosePresenceConfidence: PoseLandmarkerMinPosePresenceConfidence,
                        minTrackingConfidence: PoseLandmarkerMinTrackingConfidence,
                        outputSegmentationMasks: PoseLandmarkerOutputSegmentationMasks,
                        dnnBackend: SentisInferenceBackendKind.UnitySentis,
                        dnnTarget: SentisInferenceTarget);
                    Debug.Log(
                        "MediaPipePoseLandmarkerMultiBackend initialized (Sentis / UnitySentis, backend="
                        + SentisInferenceUtils.GetTargetDisplayName(SentisInferenceTarget) + ").",
                        this);
                }
                else
                {
                    _poseLandmarkerWorker = new MediaPipePoseLandmarkerMultiBackend(
                        detPath,
                        lmPath,
                        PoseLandmarkerRunningMode,
                        numPoses: Mathf.Max(1, PoseLandmarkerNumPoses),
                        minPoseDetectionConfidence: PoseLandmarkerMinPoseDetectionConfidence,
                        minPosePresenceConfidence: PoseLandmarkerMinPosePresenceConfidence,
                        minTrackingConfidence: PoseLandmarkerMinTrackingConfidence,
                        outputSegmentationMasks: PoseLandmarkerOutputSegmentationMasks,
                        dnnBackend: OpenCVDnnInferenceBackendKind.OpenCv,
                        dnnTarget: OpenCVDnnInferenceTargetKind.Cpu);
                    Debug.Log("MediaPipePoseLandmarkerMultiBackend initialized (OpenCV DNN).", this);
                }

                var poseLandmarkerWorker = _poseLandmarkerWorker;
                _inferenceRunner = new MatSingleFlightSyncAsyncRunner(
                    useAsyncWork: UseAsyncInference,
                    asyncWorkCancellationToken: _cts.Token,
                    disposeAsyncAfterWorkTask: async () =>
                    {
                        await poseLandmarkerWorker.WaitForCompletionAsync();
                    });
                return _poseLandmarkerWorker != null && _inferenceRunner != null;
            }
            catch (Exception ex)
            {
                Debug.LogError("MediaPipePoseLandmarkerExample TryInitializeInference failed: " + ex, this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "MediaPipe Pose Landmarker failed to initialize.\nPlease read console message.";
                }

                if (_poseLandmarkerWorker != null)
                {
                    _poseLandmarkerWorker.Dispose();
                    _poseLandmarkerWorker = null;
                }

                _inferenceRunner = null;
                return false;
            }
        }

        /// <summary>
        /// Pose Landmarker overlay. When <see cref="PoseLandmarkerOutputSegmentationMasks"/> is off, draws landmarks only (does not overlay segmentation slot [1] even if present).
        /// </summary>
        private void VisualizePoseLandmarkerOnRgba(Mat rgbaMat, Mat[] pack)
        {
            if (_poseLandmarkerWorker == null || pack == null || pack.Length == 0 || pack[0] == null || pack[0].empty())
            {
                return;
            }

            if (PoseLandmarkerOutputSegmentationMasks && pack.Length > 1 && pack[1] != null && !pack[1].empty())
            {
                _poseLandmarkerWorker.Visualize(rgbaMat, pack, printResult: PoseLandmarkerVisualizePrintResult, isRGB: true);
            }
            else
            {
                _poseLandmarkerWorker.Visualize(rgbaMat, pack[0], printResult: PoseLandmarkerVisualizePrintResult, isRGB: true);
            }
        }

        /// <summary>
        /// Updates skeleton display and AR ImagePoints / ObjectPoints from index [0] (rows = poses) of the MediaPipePoseLandmarkerMultiBackend detect output.
        /// </summary>
        /// <param name="poseLandmarkerResults">Latest packed outputs from the helper (landmarks in [0], optional segmentation in [1]).</param>
        private void UpdateSkeletonFromPoseLandmarkerResults(Mat[] poseLandmarkerResults)
        {
            if (ArHelper == null)
            {
                return;
            }

            bool imageObjectPointsAssigned = false;

            Mat landmarkMat = null;
            if (poseLandmarkerResults != null && poseLandmarkerResults.Length > 0)
            {
                landmarkMat = poseLandmarkerResults[0];
            }

            if (_poseLandmarkerWorker != null && landmarkMat != null && !landmarkMat.empty() && landmarkMat.rows() > 0
                && _bgrMat != null)
            {
                Span<MediaPipePoseLandmarkerMultiBackend.PoseLandmarkerEstimationData> dataSpan =
                    _poseLandmarkerWorker.ToStructuredDataAsSpan(landmarkMat);
                if (dataSpan.Length > 0)
                {
                    // First pose only (single-person AR, same idea as Blaze UpdateSkeleton path).
                    ref readonly MediaPipePoseLandmarkerMultiBackend.PoseLandmarkerEstimationData data = ref dataSpan[0];
                    ReadOnlySpan<OpenCVForUnity.Extensions.Vec5f> landmarksScreen5 = data.GetNormLandmarks();
                    ReadOnlySpan<OpenCVForUnity.Extensions.Vec5f> landmarksWorld5 = data.GetWorldLandmarks();
                    float fw = _bgrMat.cols();
                    float fh = _bgrMat.rows();

                    var landmarksWorld3 = new OpenCVForUnity.Extensions.Vec3f[landmarksWorld5.Length];
                    for (int i = 0; i < landmarksWorld5.Length; i++)
                    {
                        landmarksWorld3[i] = new OpenCVForUnity.Extensions.Vec3f(landmarksWorld5[i].Item1, landmarksWorld5[i].Item2, landmarksWorld5[i].Item3);
                    }

                    if (SkeletonVisualizer != null && SkeletonVisualizer.ShowSkeleton)
                    {
                        SkeletonVisualizer.UpdatePose(landmarksWorld3);
                    }

                    // Full-body assumption: pass thirteen fixed nose-to-ankle points to PnP (does not read visibility; skip PnP if any index is out of range).
                    int nLm = MediaPipePoseLandmarkerMultiBackend.PoseLandmarkerEstimationData.LANDMARK_VEC5F_COUNT;
                    var imagePointList = new List<Vector2>(SELECTED_INDICES.Length);
                    var objectPointList = new List<Vector3>(SELECTED_INDICES.Length);
                    bool buildOk = true;
                    for (int i = 0; i < SELECTED_INDICES.Length; i++)
                    {
                        int index = SELECTED_INDICES[i];
                        if (index < 0 || index >= nLm || index >= landmarksScreen5.Length || index >= landmarksWorld5.Length)
                        {
                            buildOk = false;
                            break;
                        }

                        ref readonly OpenCVForUnity.Extensions.Vec5f screen5 = ref landmarksScreen5[index];
                        ref readonly OpenCVForUnity.Extensions.Vec5f world5 = ref landmarksWorld5[index];
                        imagePointList.Add(new Vector2(screen5.Item1 * fw, screen5.Item2 * fh));
                        objectPointList.Add(new Vector3(world5.Item1, world5.Item2, world5.Item3));
                    }

                    if (buildOk && ArHelper.ARGameObjects != null && ArHelper.ARGameObjects.Count > 0 && ArHelper.ARGameObjects[0] != null)
                    {
                        ArHelper.ARGameObjects[0].ImagePoints = imagePointList.ToArray();
                        ArHelper.ARGameObjects[0].ObjectPoints = objectPointList.ToArray();
                        imageObjectPointsAssigned = true;
                    }
                }
            }

            if (!imageObjectPointsAssigned)
            {
                ArHelper.ResetARGameObjectsImagePointsAndObjectPoints();
            }
        }
    }
}
#endif
