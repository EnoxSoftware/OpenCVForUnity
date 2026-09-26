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
    /// MediaPipe Holistic Landmarker Example
    /// Combined full-body pose, hand, and face landmark inference from input frames.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Loading multiple MediaPipe ONNX/Sentis models (pose, hand, face) from StreamingAssets
    /// - Toggling Sentis vs OpenCV DNN inference and optional async holistic detection
    /// - Converting RGBA frames to BGR, running MediaPipeHolisticLandmarkerMultiBackend, and visualizing all landmarks on Mat
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Size"/>, <see cref="Scalar"/>, <see cref="Point"/>
    /// - <see cref="Imgproc"/>: cvtColor
    /// - <see cref="MediaPipeHolisticLandmarkerMultiBackend"/>, <see cref="MatSingleFlightSyncAsyncRunner"/>, <see cref="MultiBackendDnn"/>
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://github.com/google-ai-edge/mediapipe
    /// </para>
    /// <para>
    /// [Tested Models]
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeHolisticLandmarkerExample/pose_detector.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeHolisticLandmarkerExample/lite_pose_landmarks_detector.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeHolisticLandmarkerExample/hand_landmarks_detector.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeHolisticLandmarkerExample/hand_roi_refinement.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeHolisticLandmarkerExample/face_detector.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeHolisticLandmarkerExample/face_landmarks_detector.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeHolisticLandmarkerExample/face_blendshapes.onnx
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class MediaPipeHolisticLandmarkerExample : MonoBehaviour
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

        [Header("Inference (Holistic)")]
        [Tooltip("StreamingAssets-relative path to the pose detector model (poseDetectorModelFilepath).")]
        public string HolisticPoseDetectorModelFileName = "OpenCVForUnityExamples/dnn/mediapipe/pose_detector.onnx";
        [Tooltip("StreamingAssets-relative path to the pose landmarks model (poseLandmarksModelFilepath).")]
        public string HolisticPoseLandmarksModelFileName = "OpenCVForUnityExamples/dnn/mediapipe/lite_pose_landmarks_detector.onnx";
        [Tooltip("StreamingAssets-relative path to the hand landmarks model (handLandmarksModelFilepath).")]
        public string HolisticHandLandmarksModelFileName = "OpenCVForUnityExamples/dnn/mediapipe/hand_landmarks_detector.onnx";
        [Tooltip("StreamingAssets-relative path to the hand ROI refinement model (handRoiRefinementModelFilepath).")]
        public string HolisticHandRoiRefinementModelFileName = "OpenCVForUnityExamples/dnn/mediapipe/hand_roi_refinement.onnx";
        [Tooltip("StreamingAssets-relative path to the face detector model (faceDetectorModelFilepath).")]
        public string HolisticFaceDetectorModelFileName = "OpenCVForUnityExamples/dnn/mediapipe/face_detector.onnx";
        [Tooltip("StreamingAssets-relative path to the face landmarks model (faceLandmarksModelFilepath).")]
        public string HolisticFaceLandmarksModelFileName = "OpenCVForUnityExamples/dnn/mediapipe/face_landmarks_detector.onnx";
        [Tooltip("IMAGE is single-shot inference; VIDEO keeps internal stream state for continuous frames.")]
        public MediaPipeHolisticLandmarkerMultiBackend.MediaPipeHolisticRunningMode HolisticRunningMode =
            MediaPipeHolisticLandmarkerMultiBackend.MediaPipeHolisticRunningMode.VIDEO;
        [Tooltip("Whether to output and overlay pose segmentation masks.")]
        public bool HolisticOutputPoseSegmentationMasks = false;
        [Tooltip("Whether to output face blendshapes (requires face_blendshapes.onnx).")]
        public bool HolisticOutputFaceBlendshapes = false;
        [Tooltip("StreamingAssets-relative path to the face blendshapes model (faceBlendshapesModelFilepath).")]
        public string HolisticFaceBlendshapesModelFileName = "OpenCVForUnityExamples/dnn/mediapipe/face_blendshapes.onnx";
        [Tooltip("Face detection confidence threshold (minFaceDetectionConfidence).")]
        [Range(0f, 1f)]
        public float HolisticMinFaceDetectionConfidence = 0.5f;
        [Tooltip("Face detection duplicate suppression threshold (minFaceSuppressionThreshold).")]
        [Range(0f, 1f)]
        public float HolisticMinFaceSuppressionThreshold = 0.3f;
        [Tooltip("Face presence confidence threshold (minFacePresenceConfidence).")]
        [Range(0f, 1f)]
        public float HolisticMinFacePresenceConfidence = 0.5f;
        [Tooltip("Hand landmark confidence threshold (minHandLandmarksConfidence).")]
        [Range(0f, 1f)]
        public float HolisticMinHandLandmarksConfidence = 0.5f;
        [Tooltip("Pose detection confidence threshold (minPoseDetectionConfidence).")]
        [Range(0f, 1f)]
        public float HolisticMinPoseDetectionConfidence = 0.5f;
        [Tooltip("Pose detection duplicate suppression threshold (minPoseSuppressionThreshold).")]
        [Range(0f, 1f)]
        public float HolisticMinPoseSuppressionThreshold = 0.3f;
        [Tooltip("Pose presence confidence threshold (minPosePresenceConfidence).")]
        [Range(0f, 1f)]
        public float HolisticMinPosePresenceConfidence = 0.5f;

        [Header("Inference (Optional Switches)")]
        [Tooltip("When enabled, runs hand inference (both hands and world). When off, hand model paths are omitted and hand outputs are disabled internally.")]
        public bool HolisticInferHands = true;
        [Tooltip("When enabled, runs face detection and face landmarks. When off, face model paths are omitted and face outputs are disabled internally.")]
        public bool HolisticInferFace = true;

        [Header("Visualize")]
        [Tooltip("Whether Holistic Visualize calls print results to the console.")]
        public bool HolisticVisualizePrintResult = false;

        [Space(10)]
        [Header("Show 3D Skeleton")]
        public ARHelper ArHelper;
        public MediaPipePoseSkeletonVisualizer SkeletonVisualizerPose;
        public MediaPipeHandPoseSkeletonVisualizer SkeletonVisualizerRightHand;
        public MediaPipeHandPoseSkeletonVisualizer SkeletonVisualizerLeftHand;

        // Private Fields
        private Texture2D _texture;
        private float _imageSizeScale = 1f;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private Mat _bgrMat;
        private MediaPipeHolisticLandmarkerMultiBackend _holisticLandmarkerWorker;
        private string _holisticPoseDetectorFilepathOnnx;
        private string _holisticPoseLandmarksFilepathOnnx;
        private string _holisticHandLandmarksFilepathOnnx;
        private string _holisticHandRoiRefinementFilepathOnnx;
        private string _holisticFaceDetectorFilepathOnnx;
        private string _holisticFaceLandmarksFilepathOnnx;
        private string _holisticFaceBlendshapesFilepathOnnx;
        private string _holisticPoseDetectorFilepathSentis;
        private string _holisticPoseLandmarksFilepathSentis;
        private string _holisticHandLandmarksFilepathSentis;
        private string _holisticHandRoiRefinementFilepathSentis;
        private string _holisticFaceDetectorFilepathSentis;
        private string _holisticFaceLandmarksFilepathSentis;
        private string _holisticFaceBlendshapesFilepathSentis;
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

            if (SkeletonVisualizerPose != null)
            {
                SkeletonVisualizerPose.ShowSkeleton = ShowSkeleton;
            }

            if (SkeletonVisualizerRightHand != null)
            {
                SkeletonVisualizerRightHand.ShowSkeleton = ShowSkeleton;
            }

            if (SkeletonVisualizerLeftHand != null)
            {
                SkeletonVisualizerLeftHand.ShowSkeleton = ShowSkeleton;
            }

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            _holisticPoseDetectorFilepathOnnx = await OpenCVForUnityEnv.GetFilePathAsync(
                HolisticPoseDetectorModelFileName,
                cancellationToken: _cts.Token);
            _holisticPoseLandmarksFilepathOnnx = await OpenCVForUnityEnv.GetFilePathAsync(
                HolisticPoseLandmarksModelFileName,
                cancellationToken: _cts.Token);
            if (OpenCVForUnityEnv.IsSentisIntegrationAvailable)
            {
                _holisticPoseDetectorFilepathSentis = await OpenCVForUnityEnv.GetFilePathAsync(
                    MultiBackendDnn.ResolveSentisModelPathFromOnnxPath(HolisticPoseDetectorModelFileName),
                    cancellationToken: _cts.Token);
                _holisticPoseLandmarksFilepathSentis = await OpenCVForUnityEnv.GetFilePathAsync(
                    MultiBackendDnn.ResolveSentisModelPathFromOnnxPath(HolisticPoseLandmarksModelFileName),
                    cancellationToken: _cts.Token);
            }

            _holisticHandLandmarksFilepathOnnx = null;
            _holisticHandRoiRefinementFilepathOnnx = null;
            _holisticHandLandmarksFilepathSentis = null;
            _holisticHandRoiRefinementFilepathSentis = null;
            if (HolisticInferHands
                && !string.IsNullOrWhiteSpace(HolisticHandLandmarksModelFileName)
                && !string.IsNullOrWhiteSpace(HolisticHandRoiRefinementModelFileName))
            {
                _holisticHandLandmarksFilepathOnnx = await OpenCVForUnityEnv.GetFilePathAsync(
                    HolisticHandLandmarksModelFileName,
                    cancellationToken: _cts.Token);
                _holisticHandRoiRefinementFilepathOnnx = await OpenCVForUnityEnv.GetFilePathAsync(
                    HolisticHandRoiRefinementModelFileName,
                    cancellationToken: _cts.Token);
                if (OpenCVForUnityEnv.IsSentisIntegrationAvailable)
                {
                    _holisticHandLandmarksFilepathSentis = await OpenCVForUnityEnv.GetFilePathAsync(
                        MultiBackendDnn.ResolveSentisModelPathFromOnnxPath(HolisticHandLandmarksModelFileName),
                        cancellationToken: _cts.Token);
                    _holisticHandRoiRefinementFilepathSentis = await OpenCVForUnityEnv.GetFilePathAsync(
                        MultiBackendDnn.ResolveSentisModelPathFromOnnxPath(HolisticHandRoiRefinementModelFileName),
                        cancellationToken: _cts.Token);
                }
            }

            _holisticFaceDetectorFilepathOnnx = null;
            _holisticFaceLandmarksFilepathOnnx = null;
            _holisticFaceDetectorFilepathSentis = null;
            _holisticFaceLandmarksFilepathSentis = null;
            if (HolisticInferFace
                && !string.IsNullOrWhiteSpace(HolisticFaceDetectorModelFileName)
                && !string.IsNullOrWhiteSpace(HolisticFaceLandmarksModelFileName))
            {
                _holisticFaceDetectorFilepathOnnx = await OpenCVForUnityEnv.GetFilePathAsync(
                    HolisticFaceDetectorModelFileName,
                    cancellationToken: _cts.Token);
                _holisticFaceLandmarksFilepathOnnx = await OpenCVForUnityEnv.GetFilePathAsync(
                    HolisticFaceLandmarksModelFileName,
                    cancellationToken: _cts.Token);
                if (OpenCVForUnityEnv.IsSentisIntegrationAvailable)
                {
                    _holisticFaceDetectorFilepathSentis = await OpenCVForUnityEnv.GetFilePathAsync(
                        MultiBackendDnn.ResolveSentisModelPathFromOnnxPath(HolisticFaceDetectorModelFileName),
                        cancellationToken: _cts.Token);
                    _holisticFaceLandmarksFilepathSentis = await OpenCVForUnityEnv.GetFilePathAsync(
                        MultiBackendDnn.ResolveSentisModelPathFromOnnxPath(HolisticFaceLandmarksModelFileName),
                        cancellationToken: _cts.Token);
                }
            }

            _holisticFaceBlendshapesFilepathOnnx = null;
            _holisticFaceBlendshapesFilepathSentis = null;
            if (HolisticOutputFaceBlendshapes
                && HolisticInferFace
                && !string.IsNullOrWhiteSpace(HolisticFaceBlendshapesModelFileName))
            {
                _holisticFaceBlendshapesFilepathOnnx = await OpenCVForUnityEnv.GetFilePathAsync(
                    HolisticFaceBlendshapesModelFileName,
                    cancellationToken: _cts.Token);
                if (OpenCVForUnityEnv.IsSentisIntegrationAvailable)
                {
                    _holisticFaceBlendshapesFilepathSentis = await OpenCVForUnityEnv.GetFilePathAsync(
                        MultiBackendDnn.ResolveSentisModelPathFromOnnxPath(HolisticFaceBlendshapesModelFileName),
                        cancellationToken: _cts.Token);
                }
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
            // Convert RGBA camera frame to BGR for MediaPipe holistic landmarker input.
            Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

            if (_inferenceRunner != null && _holisticLandmarkerWorker != null)
            {
                // Submit sync or async holistic detection; output Mat[] holds pose/hand/face results.
                _inferenceRunner.SubmitWork(
                    _bgrMat,
                    syncWork: m => _holisticLandmarkerWorker.Detect(m, useCopyOutput: true),
                    asyncWork: async m =>
                    {
                        CancellationToken ct = _inferenceRunner.InFlightAsyncWorkCancellationToken;
                        return await _holisticLandmarkerWorker.DetectAsync(m, ct);
                    });
                if (_inferenceRunner.TryGetLatestResult(out Mat[] holisticResults))
                {
                    UpdateSkeletonFromHolisticResults(holisticResults);

                    if (holisticResults.Length > 0 && holisticResults[0] != null && !holisticResults[0].empty())
                    {
                        VisualizeHolisticOnRgba(rgbaMat, holisticResults);
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
            if (_holisticLandmarkerWorker == null || _inferenceRunner == null)
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
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _holisticLandmarkerWorker, UseAsyncInference, InferenceFramework);
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

            _holisticLandmarkerWorker?.Cancel();

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
                if (SkeletonVisualizerPose != null)
                {
                    SkeletonVisualizerPose.ShowSkeleton = ShowSkeleton;
                }

                if (SkeletonVisualizerRightHand != null)
                {
                    SkeletonVisualizerRightHand.ShowSkeleton = ShowSkeleton;
                }

                if (SkeletonVisualizerLeftHand != null)
                {
                    SkeletonVisualizerLeftHand.ShowSkeleton = ShowSkeleton;
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
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _holisticLandmarkerWorker, UseAsyncInference, InferenceFramework);
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

            if (SkeletonVisualizerPose != null)
            {
                SkeletonVisualizerPose.ResetVisualization();
            }

            if (SkeletonVisualizerRightHand != null)
            {
                SkeletonVisualizerRightHand.ResetVisualization();
            }

            if (SkeletonVisualizerLeftHand != null)
            {
                SkeletonVisualizerLeftHand.ResetVisualization();
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
        /// Fully re-initializes the holistic landmarker worker and inference runner after
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
            UpdateFpsMonitorInferenceInfo(_fpsMonitor, _holisticLandmarkerWorker, UseAsyncInference, InferenceFramework);

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

            var holisticLandmarkerWorker = _holisticLandmarkerWorker;
            _holisticLandmarkerWorker = null;
            if (holisticLandmarkerWorker != null)
            {
                await holisticLandmarkerWorker.DisposeAsync();
            }
        }

        /// <summary>
        /// Initializes inference from the resolved model path and current backend settings (Sentis asset path when using Sentis; otherwise ONNX).
        /// </summary>
        private bool TryInitializeInference()
        {
            bool useSentis = InferenceFramework == InferenceFrameworkSelectionKind.UnitySentis && OpenCVForUnityEnv.IsSentisIntegrationAvailable;
            string poseDet;
            string poseLm;
            string handLm;
            string handRoi;
            string faceDet;
            string faceLm;
            string blendForWorker;
            if (useSentis)
            {
                poseDet = _holisticPoseDetectorFilepathSentis;
                poseLm = _holisticPoseLandmarksFilepathSentis;
                handLm = _holisticHandLandmarksFilepathSentis;
                handRoi = _holisticHandRoiRefinementFilepathSentis;
                faceDet = _holisticFaceDetectorFilepathSentis;
                faceLm = _holisticFaceLandmarksFilepathSentis;
                blendForWorker = _holisticFaceBlendshapesFilepathSentis;
            }
            else
            {
                poseDet = _holisticPoseDetectorFilepathOnnx;
                poseLm = _holisticPoseLandmarksFilepathOnnx;
                handLm = _holisticHandLandmarksFilepathOnnx;
                handRoi = _holisticHandRoiRefinementFilepathOnnx;
                faceDet = _holisticFaceDetectorFilepathOnnx;
                faceLm = _holisticFaceLandmarksFilepathOnnx;
                blendForWorker = _holisticFaceBlendshapesFilepathOnnx;
            }

            bool posePathsReady = !string.IsNullOrEmpty(poseDet) && !string.IsNullOrEmpty(poseLm);
            bool handPathsReady = !HolisticInferHands
                || (!string.IsNullOrEmpty(handLm) && !string.IsNullOrEmpty(handRoi));
            bool facePathsReady = !HolisticInferFace
                || (!string.IsNullOrEmpty(faceDet) && !string.IsNullOrEmpty(faceLm));

            if (!posePathsReady || !handPathsReady || !facePathsReady)
            {
                Debug.LogError("MediaPipe Holistic Landmarker model file(s) are not loaded. Pose requires \"" + HolisticPoseDetectorModelFileName + "\" and \"" + HolisticPoseLandmarksModelFileName + "\"."
                    + (HolisticInferHands ? " Hand inference requires \"" + HolisticHandLandmarksModelFileName + "\" and \"" + HolisticHandRoiRefinementModelFileName + "\"." : "")
                    + (HolisticInferFace ? " Face inference requires \"" + HolisticFaceDetectorModelFileName + "\" and \"" + HolisticFaceLandmarksModelFileName + "\"." : "")
                    + " Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "model file is not loaded.\nPlease read console message.";
                }
                return false;
            }

            try
            {
                bool wantBlend = HolisticOutputFaceBlendshapes
                    && HolisticInferFace
                    && !string.IsNullOrEmpty(blendForWorker);

                handLm = HolisticInferHands ? handLm : null;
                handRoi = HolisticInferHands ? handRoi : null;
                faceDet = HolisticInferFace ? faceDet : null;
                faceLm = HolisticInferFace ? faceLm : null;

                if (useSentis)
                {
                    _holisticLandmarkerWorker = new MediaPipeHolisticLandmarkerMultiBackend(
                        poseDet,
                        poseLm,
                        handLm,
                        handRoi,
                        faceDet,
                        faceLm,
                        HolisticRunningMode,
                        outputPoseSegmentationMasks: HolisticOutputPoseSegmentationMasks,
                        outputFaceBlendshapes: wantBlend,
                        faceBlendshapesModelFilepath: wantBlend ? blendForWorker : null,
                        minFaceDetectionConfidence: HolisticMinFaceDetectionConfidence,
                        minFaceSuppressionThreshold: HolisticMinFaceSuppressionThreshold,
                        minFacePresenceConfidence: HolisticMinFacePresenceConfidence,
                        minHandLandmarksConfidence: HolisticMinHandLandmarksConfidence,
                        minPoseDetectionConfidence: HolisticMinPoseDetectionConfidence,
                        minPoseSuppressionThreshold: HolisticMinPoseSuppressionThreshold,
                        minPosePresenceConfidence: HolisticMinPosePresenceConfidence,
                        dnnBackend: SentisInferenceBackendKind.UnitySentis,
                        dnnTarget: SentisInferenceTarget);
                    Debug.Log(
                        "MediaPipeHolisticLandmarkerMultiBackend initialized (Sentis / UnitySentis, backend="
                        + SentisInferenceUtils.GetTargetDisplayName(SentisInferenceTarget) + ").",
                        this);
                }
                else
                {
                    _holisticLandmarkerWorker = new MediaPipeHolisticLandmarkerMultiBackend(
                        poseDet,
                        poseLm,
                        handLm,
                        handRoi,
                        faceDet,
                        faceLm,
                        HolisticRunningMode,
                        outputPoseSegmentationMasks: HolisticOutputPoseSegmentationMasks,
                        outputFaceBlendshapes: wantBlend,
                        faceBlendshapesModelFilepath: wantBlend ? blendForWorker : null,
                        minFaceDetectionConfidence: HolisticMinFaceDetectionConfidence,
                        minFaceSuppressionThreshold: HolisticMinFaceSuppressionThreshold,
                        minFacePresenceConfidence: HolisticMinFacePresenceConfidence,
                        minHandLandmarksConfidence: HolisticMinHandLandmarksConfidence,
                        minPoseDetectionConfidence: HolisticMinPoseDetectionConfidence,
                        minPoseSuppressionThreshold: HolisticMinPoseSuppressionThreshold,
                        minPosePresenceConfidence: HolisticMinPosePresenceConfidence,
                        dnnBackend: OpenCVDnnInferenceBackendKind.OpenCv,
                        dnnTarget: OpenCVDnnInferenceTargetKind.Cpu);
                    Debug.Log("MediaPipeHolisticLandmarkerMultiBackend initialized (OpenCV DNN).", this);
                }

                var holisticLandmarkerWorker = _holisticLandmarkerWorker;
                _inferenceRunner = new MatSingleFlightSyncAsyncRunner(
                    useAsyncWork: UseAsyncInference,
                    asyncWorkCancellationToken: _cts.Token,
                    disposeAsyncAfterWorkTask: async () =>
                    {
                        await holisticLandmarkerWorker.WaitForCompletionAsync();
                    });
                return _holisticLandmarkerWorker != null && _inferenceRunner != null;
            }
            catch (Exception ex)
            {
                Debug.LogError("MediaPipeHolisticLandmarkerExample TryInitializeInference failed: " + ex, this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "MediaPipe Holistic Landmarker failed to initialize.\nPlease read console message.";
                }

                if (_holisticLandmarkerWorker != null)
                {
                    _holisticLandmarkerWorker.Dispose();
                    _holisticLandmarkerWorker = null;
                }

                _inferenceRunner = null;
                return false;
            }
        }

        /// <summary>
        /// Overlays the Holistic result pack onto the RGBA image.
        /// </summary>
        private void VisualizeHolisticOnRgba(Mat rgbaMat, Mat[] pack)
        {
            if (_holisticLandmarkerWorker == null || pack == null || pack.Length == 0 || pack[0] == null || pack[0].empty())
            {
                return;
            }

            _holisticLandmarkerWorker.Visualize(rgbaMat, pack, printResult: HolisticVisualizePrintResult, isRGB: true);
        }

        /// <summary>
        /// Updates pose, face, and left/right hand skeletons and body AR points from the Holistic result pack.
        /// </summary>
        private void UpdateSkeletonFromHolisticResults(Mat[] holisticResults)
        {
            UpdatePoseSkeletonAndBodyArFromHolistic(holisticResults);
            UpdateHandSkeletonsFromHolistic(holisticResults);
        }

        /// <summary>
        /// Updates body skeleton and <see cref="ArHelper"/> from slot 0 (pose).
        /// </summary>
        private void UpdatePoseSkeletonAndBodyArFromHolistic(Mat[] holisticResults)
        {
            bool imageObjectPointsAssigned = false;

            Mat landmarkMat = holisticResults != null && holisticResults.Length > 0 ? holisticResults[0] : null;

            if (landmarkMat != null && !landmarkMat.empty() && landmarkMat.rows() > 0
                && _bgrMat != null)
            {
                Span<MediaPipePoseLandmarkerMultiBackend.PoseLandmarkerEstimationData> dataSpan =
                    PoseLandmarksPackedMatAsSpan(landmarkMat);
                if (dataSpan.Length > 0)
                {
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

                    if (SkeletonVisualizerPose != null && SkeletonVisualizerPose.ShowSkeleton)
                    {
                        SkeletonVisualizerPose.UpdatePose(landmarksWorld3);
                    }

                    if (ArHelper != null)
                    {
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
            }

            if (ArHelper != null && !imageObjectPointsAssigned)
            {
                ArHelper.ResetARGameObjectsImagePointsAndObjectPoints();
            }
        }

        /// <summary>
        /// Updates hand skeletons from slots 1/2 (<see cref="MediaPipeHandLandmarkerMultiBackend.HandLandmarkerEstimationData"/> for left/right hands), using world landmarks like <see cref="MediaPipeHandLandmarkerExample"/>.
        /// Drawing only when <see cref="MediaPipeSkeletonVisualizerBase.ShowSkeleton"/> is on (same as pose skeleton).
        /// Clears lines with <see cref="MediaPipeSkeletonVisualizerBase.ClearLine"/> when there is no hand result for a frame; does not change ShowSkeleton.
        /// </summary>
        private void UpdateHandSkeletonsFromHolistic(Mat[] holisticResults)
        {
            // Left hand (same as pose SkeletonVisualizerPose: update/clear only when ShowSkeleton is on)
            if (SkeletonVisualizerLeftHand != null && SkeletonVisualizerLeftHand.ShowSkeleton)
            {
                Mat leftMat = holisticResults != null && holisticResults.Length > 1 ? holisticResults[1] : null;
                bool hasLeft = leftMat != null && !leftMat.empty() && leftMat.rows() > 0;
                if (hasLeft)
                {
                    Span<MediaPipeHandLandmarkerMultiBackend.HandLandmarkerEstimationData> span =
                        HandLandmarksPackedMatAsSpan(leftMat);
                    hasLeft = span.Length > 0;
                    if (hasLeft)
                    {
                        ref readonly MediaPipeHandLandmarkerMultiBackend.HandLandmarkerEstimationData data = ref span[0];
                        SkeletonVisualizerLeftHand.UpdatePose(data.GetWorldLandmarks());
                    }
                    else
                    {
                        SkeletonVisualizerLeftHand.ClearLine();
                    }
                }
                else
                {
                    SkeletonVisualizerLeftHand.ClearLine();
                }
            }

            // Right hand
            if (SkeletonVisualizerRightHand != null && SkeletonVisualizerRightHand.ShowSkeleton)
            {
                Mat rightMat = holisticResults != null && holisticResults.Length > 2 ? holisticResults[2] : null;
                bool hasRight = rightMat != null && !rightMat.empty() && rightMat.rows() > 0;
                if (hasRight)
                {
                    Span<MediaPipeHandLandmarkerMultiBackend.HandLandmarkerEstimationData> span =
                        HandLandmarksPackedMatAsSpan(rightMat);
                    hasRight = span.Length > 0;
                    if (hasRight)
                    {
                        ref readonly MediaPipeHandLandmarkerMultiBackend.HandLandmarkerEstimationData data = ref span[0];
                        SkeletonVisualizerRightHand.UpdatePose(data.GetWorldLandmarks());
                    }
                    else
                    {
                        SkeletonVisualizerRightHand.ClearLine();
                    }
                }
                else
                {
                    SkeletonVisualizerRightHand.ClearLine();
                }
            }
        }

        /// <summary>
        /// Interprets a row-packed matrix with the same layout as <see cref="MediaPipeHandLandmarkerMultiBackend.Detect(OpenCVForUnity.CoreModule.Mat, bool)"/> (Holistic slots 2 / 3).
        /// </summary>
        private static Span<MediaPipeHandLandmarkerMultiBackend.HandLandmarkerEstimationData> HandLandmarksPackedMatAsSpan(Mat result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            result.ThrowIfDisposed();
            if (result.empty())
            {
                return Span<MediaPipeHandLandmarkerMultiBackend.HandLandmarkerEstimationData>.Empty;
            }

            int elementCount = MediaPipeHandLandmarkerMultiBackend.HandLandmarkerEstimationData.ELEMENT_COUNT;
            if (result.cols() < elementCount)
            {
                throw new ArgumentException("Invalid result matrix. It must have at least " + elementCount + " columns.");
            }

            if (!result.isContinuous())
            {
                throw new ArgumentException("result is not continuous.");
            }

            return result.AsSpan<MediaPipeHandLandmarkerMultiBackend.HandLandmarkerEstimationData>();
        }

        /// <summary>
        /// Holistic slot 0 (<c>POSE_LANDMARKS</c>) is a row-packed matrix with the same layout as the primary output of <see cref="MediaPipePoseLandmarkerMultiBackend"/>.
        /// </summary>
        private static Span<MediaPipePoseLandmarkerMultiBackend.PoseLandmarkerEstimationData> PoseLandmarksPackedMatAsSpan(Mat result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            result.ThrowIfDisposed();
            if (result.empty())
            {
                return Span<MediaPipePoseLandmarkerMultiBackend.PoseLandmarkerEstimationData>.Empty;
            }

            int elementCount = MediaPipePoseLandmarkerMultiBackend.PoseLandmarkerEstimationData.ELEMENT_COUNT;
            if (result.cols() < elementCount)
            {
                throw new ArgumentException("Invalid result matrix. It must have at least " + elementCount + " columns.");
            }

            if (!result.isContinuous())
            {
                throw new ArgumentException("result is not continuous.");
            }

            return result.AsSpan<MediaPipePoseLandmarkerMultiBackend.PoseLandmarkerEstimationData>();
        }
    }
}
#endif
