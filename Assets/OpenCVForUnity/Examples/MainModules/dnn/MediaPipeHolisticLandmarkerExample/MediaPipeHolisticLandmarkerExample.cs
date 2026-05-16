#if !UNITY_WSA_10_0 && NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.AR;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.UnityIntegration.Worker;
using OpenCVForUnity.UnityIntegration.Runner;
using OpenCVForUnity.UnityIntegration.Worker.DnnModule;
using OpenCVForUnity.UnityIntegration.Worker.Utils;
using OpenCVForUnity.UnityIntegration.Worker.DnnModule.MediaPipe;
using OpenCVForUnity.UnityIntegration.Worker.DnnModule.MediaPipe.SkeletonVisualizer;
#if OPENCV_SENTIS_AVAILABLE
using Unity.InferenceEngine;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static OpenCVForUnity.UnityIntegration.Helper.Source2Mat.MultiSource2MatHelper;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// MediaPipe Holistic Landmarker sample (full-body, hand, and face inference using <see cref="MediaPipeHolisticLandmarker"/>).
    /// Referring to https://github.com/google-ai-edge/mediapipe
    ///
    /// [Tested Models]
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeHolisticLandmarkerExample/pose_detector.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeHolisticLandmarkerExample/lite_pose_landmarks_detector.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeHolisticLandmarkerExample/hand_landmarks_detector.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeHolisticLandmarkerExample/hand_roi_refinement.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeHolisticLandmarkerExample/face_detector.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeHolisticLandmarkerExample/face_landmarks_detector.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeHolisticLandmarkerExample/face_blendshapes.onnx
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class MediaPipeHolisticLandmarkerExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// Thirteen PnP correspondences from nose to ankles, assuming the full body is in frame. Does not use visibility; uses every index that is in range.
        /// </summary>
        private static readonly byte[] SELECTED_INDICES =
        {
            (byte)MediaPipePoseLandmarker.KeyPoint.Nose,
            (byte)MediaPipePoseLandmarker.KeyPoint.LeftShoulder,
            (byte)MediaPipePoseLandmarker.KeyPoint.RightShoulder,
            (byte)MediaPipePoseLandmarker.KeyPoint.LeftHip,
            (byte)MediaPipePoseLandmarker.KeyPoint.RightHip,
            (byte)MediaPipePoseLandmarker.KeyPoint.LeftElbow,
            (byte)MediaPipePoseLandmarker.KeyPoint.RightElbow,
            (byte)MediaPipePoseLandmarker.KeyPoint.LeftKnee,
            (byte)MediaPipePoseLandmarker.KeyPoint.RightKnee,
            (byte)MediaPipePoseLandmarker.KeyPoint.LeftWrist,
            (byte)MediaPipePoseLandmarker.KeyPoint.RightWrist,
            (byte)MediaPipePoseLandmarker.KeyPoint.LeftAnkle,
            (byte)MediaPipePoseLandmarker.KeyPoint.RightAnkle,
        };

        // Public Fields
        [Header("UI")]
        [Tooltip("ON: Sentis. OFF: OpenCV DNN. Assign OnUseSentisInferenceToggleValueChanged to this toggle's On Value Changed in the Inspector.")]
        public Toggle UseSentisInferenceToggle;
        [Tooltip("Sentis backend selector. Dropdown option order must match Enum.GetValues(typeof(BackendType)) (numeric order). Assign OnSentisBackendDropdownValueChanged to On Value Changed (int). Value changes reinitialize inference.")]
        public Dropdown SentisBackendDropdown;
#if OPENCV_SENTIS_AVAILABLE
        [Tooltip("When enabled, runs Holistic subgraph inference with Sentis (MultiBackendDnn.DNN_BACKEND_UNITY_SENTIS). Inspector paths may stay .onnx; at runtime they are rewritten to .sentis and loaded from StreamingAssets (place a matching .sentis beside the onnx file).")]
        public bool UseSentisInference = true;
        [Tooltip("When using Sentis: dnnTarget selects Sentis BackendType (CPU / GPU, etc.).")]
        public BackendType SentisBackendType = BackendType.GPUCompute;
#endif
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
        public MediaPipeHolisticLandmarker.MediaPipeHolisticRunningMode HolisticRunningMode =
            MediaPipeHolisticLandmarker.MediaPipeHolisticRunningMode.VIDEO;
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
        private MultiSource2MatHelper _multiSource2MatHelper;
        private Mat _bgrMat;

        private MediaPipeHolisticLandmarker _holisticLandmarkerWorker;
        private string _holisticPoseDetectorFilepathOnnx;
        private string _holisticPoseLandmarksFilepathOnnx;
        private string _holisticHandLandmarksFilepathOnnx;
        private string _holisticHandRoiRefinementFilepathOnnx;
        private string _holisticFaceDetectorFilepathOnnx;
        private string _holisticFaceLandmarksFilepathOnnx;
        private string _holisticFaceBlendshapesFilepathOnnx;
#if OPENCV_SENTIS_AVAILABLE
        private string _holisticPoseDetectorFilepathSentis;
        private string _holisticPoseLandmarksFilepathSentis;
        private string _holisticHandLandmarksFilepathSentis;
        private string _holisticHandRoiRefinementFilepathSentis;
        private string _holisticFaceDetectorFilepathSentis;
        private string _holisticFaceLandmarksFilepathSentis;
        private string _holisticFaceBlendshapesFilepathSentis;
        /// <summary>
        /// <see cref="BackendType"/> values in <see cref="Enum.GetValues(System.Type)"/> order (sorted by underlying numeric value). Dropdown options must use the same order.
        /// </summary>
        private static readonly BackendType[] SentisBackendTypesInEnumOrder =
            (BackendType[])Enum.GetValues(typeof(BackendType));
#endif
        private bool _inferenceReinitializing;
        private FpsMonitor _fpsMonitor;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private MatSingleFlightSyncAsyncRunner _inferenceRunner;

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();

            // WebCamTexture2MatHelper does not work on WebGPU, so use WebCamTexture2MatAsyncGPUHelper instead.
#if UNITY_6000_0_OR_NEWER
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.WebGPU && _multiSource2MatHelper.RequestedSource2MatHelperClassName == MultiSource2MatHelperClassName.WebCamTexture2MatHelper)
            {
                _multiSource2MatHelper.RequestedSource2MatHelperClassName = MultiSource2MatHelperClassName.WebCamTexture2MatAsyncGPUHelper;
            }
#endif
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;

            // Update GUI state
            UpdateUseSentisInference();
            UpdateUseAsyncInference();
            UpdateInferenceModeToggles(inferenceReinitializing: false);
            if (ShowSkeletonToggle != null) ShowSkeletonToggle.SetIsOnWithoutNotify(ShowSkeleton);
            if (SkeletonVisualizerPose != null) SkeletonVisualizerPose.ShowSkeleton = ShowSkeleton;
            if (SkeletonVisualizerRightHand != null) SkeletonVisualizerRightHand.ShowSkeleton = ShowSkeleton;
            if (SkeletonVisualizerLeftHand != null) SkeletonVisualizerLeftHand.ShowSkeleton = ShowSkeleton;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _holisticPoseDetectorFilepathOnnx = await OpenCVEnv.GetFilePathTaskAsync(
                HolisticPoseDetectorModelFileName,
                cancellationToken: _cts.Token);
            _holisticPoseLandmarksFilepathOnnx = await OpenCVEnv.GetFilePathTaskAsync(
                HolisticPoseLandmarksModelFileName,
                cancellationToken: _cts.Token);
#if OPENCV_SENTIS_AVAILABLE
            _holisticPoseDetectorFilepathSentis = await OpenCVEnv.GetFilePathTaskAsync(
                StreamingAssetPathOnnxToSentisIfNeeded(HolisticPoseDetectorModelFileName),
                cancellationToken: _cts.Token);
            _holisticPoseLandmarksFilepathSentis = await OpenCVEnv.GetFilePathTaskAsync(
                StreamingAssetPathOnnxToSentisIfNeeded(HolisticPoseLandmarksModelFileName),
                cancellationToken: _cts.Token);
#endif

            _holisticHandLandmarksFilepathOnnx = null;
            _holisticHandRoiRefinementFilepathOnnx = null;
#if OPENCV_SENTIS_AVAILABLE
            _holisticHandLandmarksFilepathSentis = null;
            _holisticHandRoiRefinementFilepathSentis = null;
#endif
            if (HolisticInferHands
                && !string.IsNullOrWhiteSpace(HolisticHandLandmarksModelFileName)
                && !string.IsNullOrWhiteSpace(HolisticHandRoiRefinementModelFileName))
            {
                _holisticHandLandmarksFilepathOnnx = await OpenCVEnv.GetFilePathTaskAsync(
                    HolisticHandLandmarksModelFileName,
                    cancellationToken: _cts.Token);
                _holisticHandRoiRefinementFilepathOnnx = await OpenCVEnv.GetFilePathTaskAsync(
                    HolisticHandRoiRefinementModelFileName,
                    cancellationToken: _cts.Token);
#if OPENCV_SENTIS_AVAILABLE
                _holisticHandLandmarksFilepathSentis = await OpenCVEnv.GetFilePathTaskAsync(
                    StreamingAssetPathOnnxToSentisIfNeeded(HolisticHandLandmarksModelFileName),
                    cancellationToken: _cts.Token);
                _holisticHandRoiRefinementFilepathSentis = await OpenCVEnv.GetFilePathTaskAsync(
                    StreamingAssetPathOnnxToSentisIfNeeded(HolisticHandRoiRefinementModelFileName),
                    cancellationToken: _cts.Token);
#endif
            }

            _holisticFaceDetectorFilepathOnnx = null;
            _holisticFaceLandmarksFilepathOnnx = null;
#if OPENCV_SENTIS_AVAILABLE
            _holisticFaceDetectorFilepathSentis = null;
            _holisticFaceLandmarksFilepathSentis = null;
#endif
            if (HolisticInferFace
                && !string.IsNullOrWhiteSpace(HolisticFaceDetectorModelFileName)
                && !string.IsNullOrWhiteSpace(HolisticFaceLandmarksModelFileName))
            {
                _holisticFaceDetectorFilepathOnnx = await OpenCVEnv.GetFilePathTaskAsync(
                    HolisticFaceDetectorModelFileName,
                    cancellationToken: _cts.Token);
                _holisticFaceLandmarksFilepathOnnx = await OpenCVEnv.GetFilePathTaskAsync(
                    HolisticFaceLandmarksModelFileName,
                    cancellationToken: _cts.Token);
#if OPENCV_SENTIS_AVAILABLE
                _holisticFaceDetectorFilepathSentis = await OpenCVEnv.GetFilePathTaskAsync(
                    StreamingAssetPathOnnxToSentisIfNeeded(HolisticFaceDetectorModelFileName),
                    cancellationToken: _cts.Token);
                _holisticFaceLandmarksFilepathSentis = await OpenCVEnv.GetFilePathTaskAsync(
                    StreamingAssetPathOnnxToSentisIfNeeded(HolisticFaceLandmarksModelFileName),
                    cancellationToken: _cts.Token);
#endif
            }

            _holisticFaceBlendshapesFilepathOnnx = null;
#if OPENCV_SENTIS_AVAILABLE
            _holisticFaceBlendshapesFilepathSentis = null;
#endif
            if (HolisticOutputFaceBlendshapes
                && HolisticInferFace
                && !string.IsNullOrWhiteSpace(HolisticFaceBlendshapesModelFileName))
            {
                _holisticFaceBlendshapesFilepathOnnx = await OpenCVEnv.GetFilePathTaskAsync(
                    HolisticFaceBlendshapesModelFileName,
                    cancellationToken: _cts.Token);
#if OPENCV_SENTIS_AVAILABLE
                _holisticFaceBlendshapesFilepathSentis = await OpenCVEnv.GetFilePathTaskAsync(
                    StreamingAssetPathOnnxToSentisIfNeeded(HolisticFaceBlendshapesModelFileName),
                    cancellationToken: _cts.Token);
#endif
            }

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        private void Update()
        {
            if (_inferenceReinitializing)
                return;

            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = _multiSource2MatHelper.GetMat();
                Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

                if (_inferenceRunner != null && _holisticLandmarkerWorker != null)
                {
                    _inferenceRunner.SubmitWork(
                        _bgrMat,
                        syncWork: m => _holisticLandmarkerWorker.Detect(m, useCopyOutput: true),
                        asyncWork: async m =>
                        {
                            CancellationToken ct = _inferenceRunner.InFlightAsyncWorkCancellationToken;
                            return await _holisticLandmarkerWorker.DetectTaskAsync(m, ct);
                        });
                    if (_inferenceRunner.TryGetLatestResult(out Mat[] holisticResults))
                    {
                        UpdateSkeletonFromHolisticResults(holisticResults);

                        if (holisticResults.Length > 0 && holisticResults[0] != null && !holisticResults[0].empty())
                            VisualizeHolisticOnRgba(rgbaMat, holisticResults);
                    }
                }

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private async void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            await DisposeInferenceAsync();

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
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _holisticLandmarkerWorker, UseAsyncInference);
            }

            _bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);

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

            _inferenceRunner?.Cancel();

            _holisticLandmarkerWorker?.Cancel();

            _bgrMat?.Dispose(); _bgrMat = null;

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;

            ArHelper?.Dispose();
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
        /// Invoke from <c>UseSentisInferenceToggle</c> On Value Changed. Switches the inference backend.
        /// No-op when <c>OPENCV_SENTIS_AVAILABLE</c> is not defined.
        /// </summary>
        public async void OnUseSentisInferenceToggleValueChanged()
        {
#if !OPENCV_SENTIS_AVAILABLE
            await Task.CompletedTask;
            return;
#else
            if (UseSentisInferenceToggle == null || _inferenceReinitializing)
                return;

            bool newSentis = UseSentisInferenceToggle.isOn;
            if (newSentis == UseSentisInference)
                return;

            _inferenceReinitializing = true;
            UpdateInferenceModeToggles(inferenceReinitializing: true);

            await DisposeInferenceAsync();

            UseSentisInference = newSentis;
            UpdateUseAsyncInference();

            InitializeInference();
            UpdateFpsMonitorInferenceInfo(_fpsMonitor, _holisticLandmarkerWorker, UseAsyncInference);

            _inferenceReinitializing = false;
            UpdateInferenceModeToggles(inferenceReinitializing: false);
#endif
        }

        /// <summary>
        /// Invoke from <c>SentisBackendDropdown</c> On Value Changed. Switches Sentis backend type and reinitializes inference.
        /// No-op when <c>OPENCV_SENTIS_AVAILABLE</c> is not defined.
        /// </summary>
        public async void OnSentisBackendDropdownValueChanged(int index)
        {
#if !OPENCV_SENTIS_AVAILABLE
            await Task.CompletedTask;
            return;
#else
            if (SentisBackendDropdown == null || _inferenceReinitializing)
                return;

            int n = SentisBackendTypesInEnumOrder.Length;
            if (n == 0)
                return;
            int maxIdx = Mathf.Min(SentisBackendDropdown.options.Count, n) - 1;
            if (maxIdx < 0)
                return;
            BackendType newBackend = SentisBackendTypesInEnumOrder[Mathf.Clamp(index, 0, maxIdx)];
            if (newBackend == SentisBackendType)
                return;

            _inferenceReinitializing = true;
            UpdateInferenceModeToggles(inferenceReinitializing: true);

            await DisposeInferenceAsync();

            SentisBackendType = newBackend;
            UpdateUseSentisInference();
            UpdateUseAsyncInference();

            InitializeInference();
            UpdateFpsMonitorInferenceInfo(_fpsMonitor, _holisticLandmarkerWorker, UseAsyncInference);

            _inferenceReinitializing = false;
            UpdateInferenceModeToggles(inferenceReinitializing: false);
#endif
        }

        /// <summary>
        /// Raises the show skeleton toggle value changed event.
        /// </summary>
        public void OnShowSkeletonToggleValueChanged()
        {
            if (ShowSkeletonToggle.isOn != ShowSkeleton)
            {
                ShowSkeleton = ShowSkeletonToggle.isOn;
                if (SkeletonVisualizerPose != null) SkeletonVisualizerPose.ShowSkeleton = ShowSkeleton;
                if (SkeletonVisualizerRightHand != null) SkeletonVisualizerRightHand.ShowSkeleton = ShowSkeleton;
                if (SkeletonVisualizerLeftHand != null) SkeletonVisualizerLeftHand.ShowSkeleton = ShowSkeleton;
            }
        }

        /// <summary>
        /// Raises the use async inference toggle value changed event.
        /// </summary>
        public void OnUseAsyncInferenceToggleValueChanged()
        {
            if (_inferenceReinitializing)
                return;
            if (UseAsyncInferenceToggle.isOn != UseAsyncInference)
            {
                if (_inferenceRunner != null)
                    _inferenceRunner.UseAsyncWork = UseAsyncInferenceToggle.isOn;
                UseAsyncInference = UseAsyncInferenceToggle.isOn;
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _holisticLandmarkerWorker, UseAsyncInference);
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
        /// <summary>
        /// Updates async inference and (when <c>OPENCV_SENTIS_AVAILABLE</c>) Sentis toggle interactability and visible state to match
        /// the current <see cref="UseAsyncInference"/> / <see cref="UseSentisInference"/> (UI only; call
        /// <see cref="UpdateUseAsyncInference"/> first so field values are up to date).
        /// When <c>OPENCV_SENTIS_AVAILABLE</c> and not re-initializing, also calls <see cref="UpdateSentisBackendDropdown"/>, keeps the Sentis inference toggle interactive, and sets the backend dropdown interactability from <see cref="UseSentisInference"/>.
        /// </summary>
        /// <param name="inferenceReinitializing">
        /// When <see langword="true"/>, inference is re-initializing: Sentis and async inference controls are disabled.
        /// When <see langword="false"/> after completion (or at startup), normal enable/disable and visible state sync apply.
        /// </param>
        private void UpdateInferenceModeToggles(bool inferenceReinitializing)
        {
            if (inferenceReinitializing)
            {
                if (UseSentisInferenceToggle != null)
                    UseSentisInferenceToggle.interactable = false;
                if (SentisBackendDropdown != null)
                    SentisBackendDropdown.interactable = false;
                if (UseAsyncInferenceToggle != null)
                    UseAsyncInferenceToggle.interactable = false;
                return;
            }

            if (UseAsyncInferenceToggle != null)
            {
                UseAsyncInferenceToggle.SetIsOnWithoutNotify(UseAsyncInference);
                UseAsyncInferenceToggle.interactable = true;
            }
#if OPENCV_SENTIS_AVAILABLE
            if (UseSentisInferenceToggle != null)
            {
                UseSentisInferenceToggle.SetIsOnWithoutNotify(UseSentisInference);
                UseSentisInferenceToggle.interactable = true;
            }
            if (SentisBackendDropdown != null)
                SentisBackendDropdown.interactable = UseSentisInference;
            UpdateSentisBackendDropdown();
#else
            if (UseSentisInferenceToggle != null)
            {
                UseSentisInferenceToggle.SetIsOnWithoutNotify(false);
                UseSentisInferenceToggle.interactable = false;
            }
            if (SentisBackendDropdown != null)
                SentisBackendDropdown.interactable = false;
#endif
        }

#if OPENCV_SENTIS_AVAILABLE
        /// <summary>
        /// Aligns the dropdown with <see cref="SentisBackendType"/> without raising change events. Option order must match <see cref="SentisBackendTypesInEnumOrder"/>.
        /// </summary>
        private void UpdateSentisBackendDropdown()
        {
            if (SentisBackendDropdown == null || SentisBackendDropdown.options.Count == 0)
                return;
            if (SentisBackendTypesInEnumOrder.Length == 0)
                return;
            int idx = Array.IndexOf(SentisBackendTypesInEnumOrder, SentisBackendType);
            if (idx < 0)
                idx = 0;
            int maxIdx = Mathf.Min(SentisBackendDropdown.options.Count, SentisBackendTypesInEnumOrder.Length) - 1;
            SentisBackendDropdown.SetValueWithoutNotify(Mathf.Clamp(idx, 0, maxIdx));
        }
#endif

        /// <summary>
        /// When <c>OPENCV_SENTIS_AVAILABLE</c>, if <see cref="SystemInfo.supportsComputeShaders"/> is <see langword="false"/> and
        /// <see cref="SentisBackendType"/> is <see cref="BackendType.GPUCompute"/>, sets <see cref="SentisBackendType"/> to <see cref="BackendType.GPUPixel"/>.
        /// </summary>
        private void UpdateUseSentisInference()
        {
#if OPENCV_SENTIS_AVAILABLE
            if (!SystemInfo.supportsComputeShaders && SentisBackendType == BackendType.GPUCompute)
                SentisBackendType = BackendType.GPUPixel;
#endif
        }

        /// <summary>
        /// Reserved hook for synchronizing <see cref="UseAsyncInference"/> with platform capabilities.
        /// Does not modify <see cref="UseAsyncInference"/> in this example.
        /// </summary>
        private void UpdateUseAsyncInference()
        {
        }

        /// <summary>
        /// Updates <paramref name="fpsMonitor"/> with dnn backend, target, and async mode from
        /// <paramref name="worker"/> and <paramref name="useAsyncInference"/> (or "-" when a value is not available).
        /// </summary>
        private static void UpdateFpsMonitorInferenceInfo(FpsMonitor fpsMonitor, DnnInferenceWorkerBase worker, bool useAsyncInference)
        {
            if (fpsMonitor == null)
                return;

            if (worker != null)
            {
                int be = worker.DnnBackend;
                int tgt = worker.DnnTarget;
                fpsMonitor.Add("dnnBackend", MultiBackendDnn.GetBackendDisplayString(be));
                fpsMonitor.Add("dnnTarget", MultiBackendDnn.GetTargetDisplayString(tgt));
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

#if OPENCV_SENTIS_AVAILABLE
        /// <summary>
        /// When using Sentis: if the StreamingAssets-relative path ends with <c>.onnx</c>, replace it with <c>.sentis</c>.
        /// </summary>
        private static string StreamingAssetPathOnnxToSentisIfNeeded(string streamingAssetsRelativePath)
        {
            if (string.IsNullOrEmpty(streamingAssetsRelativePath))
                return streamingAssetsRelativePath;
            if (!streamingAssetsRelativePath.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase))
                return streamingAssetsRelativePath;
            return Path.ChangeExtension(streamingAssetsRelativePath, ".sentis");
        }
#endif

        /// <summary>
        /// Disposes the inference runner and <see cref="MediaPipeHolisticLandmarker"/> asynchronously (used from <see cref="OnDestroy"/> and when switching backends).
        /// </summary>
        private async Task DisposeInferenceAsync()
        {
            if (_inferenceRunner != null)
                await _inferenceRunner.DisposeAsync();
            _inferenceRunner = null;

            _holisticLandmarkerWorker?.Dispose();
            _holisticLandmarkerWorker = null;
        }

        /// <summary>
        /// Initializes inference from the resolved model path and current backend settings (Sentis asset path when using Sentis; otherwise ONNX).
        /// </summary>
        private void InitializeInference()
        {
            string poseDet;
            string poseLm;
            string handLm;
            string handRoi;
            string faceDet;
            string faceLm;
            string blendForWorker;
#if OPENCV_SENTIS_AVAILABLE
            if (UseSentisInference)
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
#endif
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
                    + " Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("model file is not loaded.\nPlease read console message.", 20000);
                }
                return;
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

#if OPENCV_SENTIS_AVAILABLE
                if (UseSentisInference)
                {
                    _holisticLandmarkerWorker = new MediaPipeHolisticLandmarker(
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
                        dnnBackend: MultiBackendDnn.DNN_BACKEND_UNITY_SENTIS,
                        dnnTarget: (int)SentisBackendType);
                    Debug.Log("MediaPipeHolisticLandmarker initialized (Sentis / DNN_BACKEND_UNITY_SENTIS, backend=" + SentisBackendType + ").");
                }
                else
#endif
                {
                    _holisticLandmarkerWorker = new MediaPipeHolisticLandmarker(
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
                        minPosePresenceConfidence: HolisticMinPosePresenceConfidence);
                    Debug.Log("MediaPipeHolisticLandmarker initialized (OpenCV DNN).");
                }

                _inferenceRunner = new MatSingleFlightSyncAsyncRunner(
                    useAsyncWork: UseAsyncInference,
                    asyncWorkCancellationToken: _cts.Token,
                    disposeAsyncAfterWorkTask: async () =>
                    {
                        await _holisticLandmarkerWorker.WaitForCompletionTaskAsync();
                    });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to initialize MediaPipeHolisticLandmarker: " + ex.Message);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("MediaPipe Holistic Landmarker failed to initialize.\nPlease read console message.", 20000);
                }
            }
        }

        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            InitializeInference();

            _multiSource2MatHelper.Initialize();
        }

        /// <summary>
        /// Overlays the Holistic result pack onto the RGBA image.
        /// </summary>
        private void VisualizeHolisticOnRgba(Mat rgbaMat, Mat[] pack)
        {
            if (_holisticLandmarkerWorker == null || pack == null || pack.Length == 0 || pack[0] == null || pack[0].empty())
                return;
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
                Span<MediaPipePoseLandmarker.PoseLandmarkerEstimationData> dataSpan =
                    PoseLandmarksPackedMatAsSpan(landmarkMat);
                if (dataSpan.Length > 0)
                {
                    ref readonly MediaPipePoseLandmarker.PoseLandmarkerEstimationData data = ref dataSpan[0];
                    ReadOnlySpan<Vec5f> landmarksScreen5 = data.GetNormLandmarks();
                    ReadOnlySpan<Vec5f> landmarksWorld5 = data.GetWorldLandmarks();
                    float fw = _bgrMat.cols();
                    float fh = _bgrMat.rows();

                    var landmarksWorld3 = new Vec3f[landmarksWorld5.Length];
                    for (int i = 0; i < landmarksWorld5.Length; i++)
                        landmarksWorld3[i] = new Vec3f(landmarksWorld5[i].Item1, landmarksWorld5[i].Item2, landmarksWorld5[i].Item3);

                    if (SkeletonVisualizerPose != null && SkeletonVisualizerPose.ShowSkeleton)
                        SkeletonVisualizerPose.UpdatePose(landmarksWorld3);

                    if (ArHelper != null)
                    {
                        // Full-body assumption: pass thirteen fixed nose-to-ankle points to PnP (does not read visibility; skip PnP if any index is out of range).
                        int nLm = MediaPipePoseLandmarker.PoseLandmarkerEstimationData.LANDMARK_VEC5F_COUNT;
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
                            ref readonly Vec5f screen5 = ref landmarksScreen5[index];
                            ref readonly Vec5f world5 = ref landmarksWorld5[index];
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
                ArHelper.ResetARGameObjectsImagePointsAndObjectPoints();
        }

        /// <summary>
        /// Updates hand skeletons from slots 1/2 (<see cref="MediaPipeHandLandmarker.HandLandmarkerEstimationData"/> for left/right hands), using world landmarks like <see cref="MediaPipeHandLandmarkerExample"/>.
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
                    Span<MediaPipeHandLandmarker.HandLandmarkerEstimationData> span =
                        HandLandmarksPackedMatAsSpan(leftMat);
                    hasLeft = span.Length > 0;
                    if (hasLeft)
                    {
                        ref readonly MediaPipeHandLandmarker.HandLandmarkerEstimationData data = ref span[0];
                        SkeletonVisualizerLeftHand.UpdatePose(data.GetWorldLandmarks());
                    }
                    else
                        SkeletonVisualizerLeftHand.ClearLine();
                }
                else
                    SkeletonVisualizerLeftHand.ClearLine();
            }

            // Right hand
            if (SkeletonVisualizerRightHand != null && SkeletonVisualizerRightHand.ShowSkeleton)
            {
                Mat rightMat = holisticResults != null && holisticResults.Length > 2 ? holisticResults[2] : null;
                bool hasRight = rightMat != null && !rightMat.empty() && rightMat.rows() > 0;
                if (hasRight)
                {
                    Span<MediaPipeHandLandmarker.HandLandmarkerEstimationData> span =
                        HandLandmarksPackedMatAsSpan(rightMat);
                    hasRight = span.Length > 0;
                    if (hasRight)
                    {
                        ref readonly MediaPipeHandLandmarker.HandLandmarkerEstimationData data = ref span[0];
                        SkeletonVisualizerRightHand.UpdatePose(data.GetWorldLandmarks());
                    }
                    else
                        SkeletonVisualizerRightHand.ClearLine();
                }
                else
                    SkeletonVisualizerRightHand.ClearLine();
            }
        }

        /// <summary>
        /// Interprets a row-packed matrix with the same layout as <see cref="MediaPipeHandLandmarker.Detect(Mat, bool)"/> (Holistic slots 2 / 3).
        /// </summary>
        private static Span<MediaPipeHandLandmarker.HandLandmarkerEstimationData> HandLandmarksPackedMatAsSpan(Mat result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            result.ThrowIfDisposed();
            if (result.empty())
                return Span<MediaPipeHandLandmarker.HandLandmarkerEstimationData>.Empty;

            int elementCount = MediaPipeHandLandmarker.HandLandmarkerEstimationData.ELEMENT_COUNT;
            if (result.cols() < elementCount)
                throw new ArgumentException("Invalid result matrix. It must have at least " + elementCount + " columns.");

            if (!result.isContinuous())
                throw new ArgumentException("result is not continuous.");

            return result.AsSpan<MediaPipeHandLandmarker.HandLandmarkerEstimationData>();
        }

        /// <summary>
        /// Holistic slot 0 (<c>POSE_LANDMARKS</c>) is a row-packed matrix with the same layout as the primary output of <see cref="MediaPipePoseLandmarker"/>.
        /// </summary>
        private static Span<MediaPipePoseLandmarker.PoseLandmarkerEstimationData> PoseLandmarksPackedMatAsSpan(Mat result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            result.ThrowIfDisposed();
            if (result.empty())
                return Span<MediaPipePoseLandmarker.PoseLandmarkerEstimationData>.Empty;

            int elementCount = MediaPipePoseLandmarker.PoseLandmarkerEstimationData.ELEMENT_COUNT;
            if (result.cols() < elementCount)
                throw new ArgumentException("Invalid result matrix. It must have at least " + elementCount + " columns.");

            if (!result.isContinuous())
                throw new ArgumentException("result is not continuous.");

            return result.AsSpan<MediaPipePoseLandmarker.PoseLandmarkerEstimationData>();
        }
    }
}

#endif
