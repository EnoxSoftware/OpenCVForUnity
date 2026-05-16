#if !UNITY_WSA_10_0 && NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Worker;
using OpenCVForUnity.UnityIntegration.Worker.Utils;
using OpenCVForUnity.UnityIntegration.Helper.AR;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.UnityIntegration.Runner;
using OpenCVForUnity.UnityIntegration.Worker.DnnModule;
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
    /// MediaPipe Face Landmarker sample (face landmark inference using <see cref="MediaPipeFaceLandmarker"/>).
    /// Referring to https://github.com/google-ai-edge/mediapipe
    ///
    /// [Tested Models]
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeFaceLandmarkerExample/face_detector.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeFaceLandmarkerExample/face_landmarks_detector.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeFaceLandmarkerExample/face_blendshapes.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeFaceLandmarkerExample/geometry_pipeline_metadata_including_iris_landmarks.pbtxt
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class MediaPipeFaceLandmarkerExample : MonoBehaviour
    {
        // Constants
        private const float CANONICAL_MESH_SCALE = 0.01f;

        /// <summary>Face mesh vertex indices used for AR ImagePoints / ObjectPoints (same topology as MediaPipe Face Mesh).</summary>
        private static readonly int[] SELECTED_INDICES = {
            1,   // Nose tip
            33,  // Left eye inner corner
            263, // Right eye inner corner
            61,  // Mouth left corner
            291, // Mouth right corner
            152, // Chin
            10,  // Forehead / top of head
            234, // Left cheek
            454, // Right cheek
        };

        // Public Fields
        [Header("UI")]
        [Tooltip("ON: Sentis. OFF: OpenCV DNN. Assign OnUseSentisInferenceToggleValueChanged to this toggle's On Value Changed in the Inspector.")]
        public Toggle UseSentisInferenceToggle;
        [Tooltip("Sentis backend selector. Dropdown option order must match Enum.GetValues(typeof(BackendType)) (numeric order). Assign OnSentisBackendDropdownValueChanged to On Value Changed (int). Value changes reinitialize inference.")]
        public Dropdown SentisBackendDropdown;
#if OPENCV_SENTIS_AVAILABLE
        [Tooltip("When enabled, runs face detection, face landmarks, and blendshape inference with Sentis (MultiBackendDnn.DNN_BACKEND_UNITY_SENTIS). Inspector paths may stay .onnx; at runtime they are rewritten to .sentis and loaded from StreamingAssets (place a matching .sentis beside the onnx file).")]
        public bool UseSentisInference = true;
        [Tooltip("When using Sentis: dnnTarget selects Sentis BackendType (CPU / GPU, etc.).")]
        public BackendType SentisBackendType = BackendType.GPUCompute;
#endif
        public Toggle UseAsyncInferenceToggle;
        public bool UseAsyncInference = true;
        public Toggle ShowSkeletonToggle;
        public bool ShowSkeleton;

        [Header("Inference")]
        [Tooltip("StreamingAssets-relative path to the face detection model (faceDetectorModelFilepath).")]
        public string FaceLandmarkerFaceDetectorModelFileName = "OpenCVForUnityExamples/dnn/mediapipe/face_detector.onnx";
        [Tooltip("StreamingAssets-relative path to the face landmark model (faceLandmarksModelFilepath).")]
        public string FaceLandmarkerFaceLandmarksModelFileName = "OpenCVForUnityExamples/dnn/mediapipe/face_landmarks_detector.onnx";
        [Tooltip("MediaPipeFaceLandmarker running mode. IMAGE is single-shot inference; VIDEO is for continuous frames.")]
        public MediaPipeFaceLandmarker.MediaPipeFaceRunningMode FaceLandmarkerRunningMode = MediaPipeFaceLandmarker.MediaPipeFaceRunningMode.VIDEO;
        [Tooltip("numFaces when initializing MediaPipeFaceLandmarker (number of faces to detect simultaneously).")]
        [Range(1, 10)]
        public int FaceLandmarkerNumFaces = 1;
        [Tooltip("Face detection confidence threshold (minFaceDetectionConfidence).")]
        [Range(0f, 1f)]
        public float FaceLandmarkerMinFaceDetectionConfidence = 0.5f;
        [Tooltip("Face presence confidence threshold (minFacePresenceConfidence).")]
        [Range(0f, 1f)]
        public float FaceLandmarkerMinFacePresenceConfidence = 0.5f;
        [Tooltip("Tracking confidence threshold (minTrackingConfidence).")]
        [Range(0f, 1f)]
        public float FaceLandmarkerMinFaceTrackingConfidence = 0.5f;
        [Tooltip("Face Landmarker outputFaceBlendshapes setting: whether to include blendshapes in the inference result.")]
        public bool FaceLandmarkerOutputFaceBlendshapes = true;
        [Tooltip("StreamingAssets-relative path to faceBlendshapesModelFilepath.")]
        public string FaceLandmarkerFaceBlendshapesModelFileName = "OpenCVForUnityExamples/dnn/mediapipe/face_blendshapes.onnx";
        [Tooltip("Face Landmarker outputFacialTransformationMatrixes setting: whether to output the 4x4 facial pose matrices.")]
        public bool FaceLandmarkerOutputFacialTransformationMatrixes = true;
        [Tooltip("StreamingAssets-relative path to faceGeometryPipelineMetadataFilepath.")]
        public string FaceLandmarkerFaceGeometryPipelineMetadataFileName =
            "OpenCVForUnityExamples/dnn/mediapipe/geometry_pipeline_metadata_including_iris_landmarks.pbtxt";

        [Header("Visualize")]
        [Tooltip("Whether Face Landmarker Visualize calls print results to the console.")]
        public bool FaceLandmarkerVisualizePrintResult = false;

        [Space(10)]
        [Header("Show 3D Skeleton")]
        public ARHelper ArHelper;
        public MediaPipeFaceSkeletonVisualizer SkeletonVisualizer;

        // Private Fields
        private Texture2D _texture;
        private MultiSource2MatHelper _multiSource2MatHelper;
        private Mat _bgrMat;

        private MediaPipeFaceLandmarker _faceLandmarkerWorker;
        private string _faceLandmarkerFaceDetectionModelFilepathOnnx;
        private string _faceLandmarkerEstimationModelFilepathOnnx;
        private string _faceLandmarkerFaceBlendshapesModelFilepathOnnx;
#if OPENCV_SENTIS_AVAILABLE
        private string _faceLandmarkerFaceDetectionModelFilepathSentis;
        private string _faceLandmarkerEstimationModelFilepathSentis;
        private string _faceLandmarkerFaceBlendshapesModelFilepathSentis;
        /// <summary>
        /// <see cref="BackendType"/> values in <see cref="Enum.GetValues(System.Type)"/> order (sorted by underlying numeric value). Dropdown options must use the same order.
        /// </summary>
        private static readonly BackendType[] SentisBackendTypesInEnumOrder =
            (BackendType[])Enum.GetValues(typeof(BackendType));
#endif
        private string _faceLandmarkerFaceGeometryPipelineMetadataFilepath;
        private Vec3f[] _canonicalWorldLandmarks;

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
            if (SkeletonVisualizer != null) SkeletonVisualizer.ShowSkeleton = ShowSkeleton;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _faceLandmarkerFaceDetectionModelFilepathOnnx = await OpenCVEnv.GetFilePathTaskAsync(
                FaceLandmarkerFaceDetectorModelFileName,
                cancellationToken: _cts.Token);
            _faceLandmarkerEstimationModelFilepathOnnx = await OpenCVEnv.GetFilePathTaskAsync(
                FaceLandmarkerFaceLandmarksModelFileName,
                cancellationToken: _cts.Token);
            _faceLandmarkerFaceBlendshapesModelFilepathOnnx = !string.IsNullOrWhiteSpace(FaceLandmarkerFaceBlendshapesModelFileName)
                ? await OpenCVEnv.GetFilePathTaskAsync(
                    FaceLandmarkerFaceBlendshapesModelFileName,
                    cancellationToken: _cts.Token)
                : null;
#if OPENCV_SENTIS_AVAILABLE
            _faceLandmarkerFaceDetectionModelFilepathSentis = await OpenCVEnv.GetFilePathTaskAsync(
                StreamingAssetPathOnnxToSentisIfNeeded(FaceLandmarkerFaceDetectorModelFileName),
                cancellationToken: _cts.Token);
            _faceLandmarkerEstimationModelFilepathSentis = await OpenCVEnv.GetFilePathTaskAsync(
                StreamingAssetPathOnnxToSentisIfNeeded(FaceLandmarkerFaceLandmarksModelFileName),
                cancellationToken: _cts.Token);
            _faceLandmarkerFaceBlendshapesModelFilepathSentis = !string.IsNullOrWhiteSpace(FaceLandmarkerFaceBlendshapesModelFileName)
                ? await OpenCVEnv.GetFilePathTaskAsync(
                    StreamingAssetPathOnnxToSentisIfNeeded(FaceLandmarkerFaceBlendshapesModelFileName),
                    cancellationToken: _cts.Token)
                : null;
#endif
            _faceLandmarkerFaceGeometryPipelineMetadataFilepath = !string.IsNullOrWhiteSpace(FaceLandmarkerFaceGeometryPipelineMetadataFileName)
                ? await OpenCVEnv.GetFilePathTaskAsync(
                    FaceLandmarkerFaceGeometryPipelineMetadataFileName,
                    cancellationToken: _cts.Token)
                : null;

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

                if (_inferenceRunner != null && _faceLandmarkerWorker != null)
                {
                    _inferenceRunner.SubmitWork(
                        _bgrMat,
                        syncWork: m => _faceLandmarkerWorker.Detect(m, useCopyOutput: true),
                        asyncWork: async m =>
                        {
                            CancellationToken ct = _inferenceRunner.InFlightAsyncWorkCancellationToken;
                            return await _faceLandmarkerWorker.DetectTaskAsync(m, ct);
                        });
                    if (_inferenceRunner.TryGetLatestResult(out Mat[] faceLandmarkerResults))
                    {
                        UpdateSkeletonFromFaceLandmarkerResults(faceLandmarkerResults);

                        if (faceLandmarkerResults.Length > 0 && faceLandmarkerResults[0] != null && !faceLandmarkerResults[0].empty())
                            VisualizeFaceLandmarkerOnRgba(rgbaMat, faceLandmarkerResults);
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
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _faceLandmarkerWorker, UseAsyncInference);
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

            _faceLandmarkerWorker?.Cancel();

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
            UpdateFpsMonitorInferenceInfo(_fpsMonitor, _faceLandmarkerWorker, UseAsyncInference);

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
            UpdateFpsMonitorInferenceInfo(_fpsMonitor, _faceLandmarkerWorker, UseAsyncInference);

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
                if (SkeletonVisualizer != null) SkeletonVisualizer.ShowSkeleton = ShowSkeleton;
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
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _faceLandmarkerWorker, UseAsyncInference);
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
        /// Disposes the inference runner and <see cref="MediaPipeFaceLandmarker"/> asynchronously (used from <see cref="OnDestroy"/> and when switching backends).
        /// </summary>
        private async Task DisposeInferenceAsync()
        {
            if (_inferenceRunner != null)
                await _inferenceRunner.DisposeAsync();
            _inferenceRunner = null;

            _faceLandmarkerWorker?.Dispose();
            _faceLandmarkerWorker = null;
        }

        /// <summary>
        /// Initializes inference from the resolved model path and current backend settings (Sentis asset path when using Sentis; otherwise ONNX).
        /// </summary>
        private void InitializeInference()
        {
            string detPath = _faceLandmarkerFaceDetectionModelFilepathOnnx;
            string estPath = _faceLandmarkerEstimationModelFilepathOnnx;
            string blendPath = null;
#if OPENCV_SENTIS_AVAILABLE
            if (UseSentisInference)
            {
                detPath = _faceLandmarkerFaceDetectionModelFilepathSentis;
                estPath = _faceLandmarkerEstimationModelFilepathSentis;
            }
#endif
            if (!string.IsNullOrWhiteSpace(FaceLandmarkerFaceBlendshapesModelFileName))
            {
#if OPENCV_SENTIS_AVAILABLE
                blendPath = UseSentisInference
                    ? _faceLandmarkerFaceBlendshapesModelFilepathSentis
                    : _faceLandmarkerFaceBlendshapesModelFilepathOnnx;
#else
                blendPath = _faceLandmarkerFaceBlendshapesModelFilepathOnnx;
#endif
            }

            if (string.IsNullOrEmpty(detPath) || string.IsNullOrEmpty(estPath))
            {
                Debug.LogError(FaceLandmarkerFaceDetectorModelFileName + " or " + FaceLandmarkerFaceLandmarksModelFileName + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("model file is not loaded.\nPlease read console message.", 20000);
                }
                return;
            }

            try
            {
#if OPENCV_SENTIS_AVAILABLE
                if (UseSentisInference)
                {
                    _faceLandmarkerWorker = new MediaPipeFaceLandmarker(
                        detPath,
                        estPath,
                        FaceLandmarkerRunningMode,
                        numFaces: Mathf.Max(1, FaceLandmarkerNumFaces),
                        minFaceDetectionConfidence: FaceLandmarkerMinFaceDetectionConfidence,
                        minFacePresenceConfidence: FaceLandmarkerMinFacePresenceConfidence,
                        minTrackingConfidence: FaceLandmarkerMinFaceTrackingConfidence,
                        outputFaceBlendshapes: FaceLandmarkerOutputFaceBlendshapes,
                        faceBlendshapesModelFilepath: blendPath,
                        outputFacialTransformationMatrixes: FaceLandmarkerOutputFacialTransformationMatrixes,
                        faceGeometryPipelineMetadataFilepath: _faceLandmarkerFaceGeometryPipelineMetadataFilepath,
                        dnnBackend: MultiBackendDnn.DNN_BACKEND_UNITY_SENTIS,
                        dnnTarget: (int)SentisBackendType);
                    Debug.Log("MediaPipeFaceLandmarker initialized (Sentis / DNN_BACKEND_UNITY_SENTIS, backend=" + SentisBackendType + ").");
                }
                else
#endif
                {
                    _faceLandmarkerWorker = new MediaPipeFaceLandmarker(
                        detPath,
                        estPath,
                        FaceLandmarkerRunningMode,
                        numFaces: Mathf.Max(1, FaceLandmarkerNumFaces),
                        minFaceDetectionConfidence: FaceLandmarkerMinFaceDetectionConfidence,
                        minFacePresenceConfidence: FaceLandmarkerMinFacePresenceConfidence,
                        minTrackingConfidence: FaceLandmarkerMinFaceTrackingConfidence,
                        outputFaceBlendshapes: FaceLandmarkerOutputFaceBlendshapes,
                        faceBlendshapesModelFilepath: blendPath,
                        outputFacialTransformationMatrixes: FaceLandmarkerOutputFacialTransformationMatrixes,
                        faceGeometryPipelineMetadataFilepath: _faceLandmarkerFaceGeometryPipelineMetadataFilepath);
                    Debug.Log("MediaPipeFaceLandmarker initialized (OpenCV DNN).");
                }

                _inferenceRunner = new MatSingleFlightSyncAsyncRunner(
                    useAsyncWork: UseAsyncInference,
                    asyncWorkCancellationToken: _cts.Token,
                    disposeAsyncAfterWorkTask: async () =>
                    {
                        await _faceLandmarkerWorker.WaitForCompletionTaskAsync();
                    });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Failed to initialize MediaPipeFaceLandmarker: " + ex.Message);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("MediaPipe Face Landmarker failed to initialize.\nPlease read console message.", 20000);
                }
            }
        }

        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            _canonicalWorldLandmarks = LoadCanonicalMeshWorldLandmarks(_faceLandmarkerFaceGeometryPipelineMetadataFilepath);

            InitializeInference();

            _multiSource2MatHelper.Initialize();
        }

        /// <summary>
        /// Face Landmarker overlay. When extra outputs (blendshapes / facial transformation) are enabled, visualizes the full result pack.
        /// </summary>
        private void VisualizeFaceLandmarkerOnRgba(Mat rgbaMat, Mat[] pack)
        {
            if (_faceLandmarkerWorker == null || pack == null || pack.Length == 0 || pack[0] == null || pack[0].empty())
                return;
            bool hasBlendshape = FaceLandmarkerOutputFaceBlendshapes && pack.Length > 1 && pack[1] != null && !pack[1].empty();
            bool hasFacialTransformation = FaceLandmarkerOutputFacialTransformationMatrixes && pack.Length > 2 && pack[2] != null && !pack[2].empty();
            if (hasBlendshape || hasFacialTransformation)
                _faceLandmarkerWorker.Visualize(rgbaMat, pack, printResult: FaceLandmarkerVisualizePrintResult, isRGB: true);
            else
                _faceLandmarkerWorker.Visualize(rgbaMat, pack[0], printResult: FaceLandmarkerVisualizePrintResult, isRGB: true);
        }

        /// <summary>
        /// Updates skeleton display and AR ImagePoints / ObjectPoints from row index [0] (faces as rows) of the MediaPipeFaceLandmarker Detect / DetectTaskAsync return value.
        /// </summary>
        /// <param name="faceLandmarkerResults">Result of <c>Detect</c> / <c>DetectTaskAsync</c> (packed mats). Primary landmarks are in [0] (rows = faces). Null or empty when nothing is detected.</param>
        private void UpdateSkeletonFromFaceLandmarkerResults(Mat[] faceLandmarkerResults)
        {
            if (ArHelper == null)
                return;

            bool imageObjectPointsAssigned = false;

            Mat landmarkMat = null;
            if (faceLandmarkerResults != null && faceLandmarkerResults.Length > 0)
                landmarkMat = faceLandmarkerResults[0];

            if (_faceLandmarkerWorker != null && landmarkMat != null && !landmarkMat.empty() && landmarkMat.rows() > 0
                && _bgrMat != null && _canonicalWorldLandmarks != null)
            {
                Span<MediaPipeFaceLandmarker.FaceLandmarkerEstimationData> dataSpan =
                    _faceLandmarkerWorker.ToStructuredDataAsSpan(landmarkMat);
                if (dataSpan.Length > 0)
                {
                    // Update AR for the first face only
                    ref readonly MediaPipeFaceLandmarker.FaceLandmarkerEstimationData data = ref dataSpan[0];
                    ReadOnlySpan<Vec3f> landmarks = data.GetNormLandmarks();
                    float fw = _bgrMat.cols();
                    float fh = _bgrMat.rows();

                    if (SkeletonVisualizer != null && SkeletonVisualizer.ShowSkeleton)
                        SkeletonVisualizer.UpdateFace(_canonicalWorldLandmarks);

                    var imagePoints = new Vector2[SELECTED_INDICES.Length];
                    var objectPoints = new Vector3[SELECTED_INDICES.Length];
                    bool buildOk = true;
                    for (int i = 0; i < SELECTED_INDICES.Length; i++)
                    {
                        int index = SELECTED_INDICES[i];
                        ref readonly var lm = ref landmarks[index];
                        // Image landmarks from the landmarker are normalized coordinates (equivalent to NormalizedLandmark in the reference implementation).
                        imagePoints[i] = new Vector2(lm.Item1 * fw, lm.Item2 * fh);
                        if (index >= _canonicalWorldLandmarks.Length)
                        {
                            buildOk = false;
                            break;
                        }
                        Vec3f canonical = _canonicalWorldLandmarks[index];
                        objectPoints[i] = new Vector3(canonical.Item1, canonical.Item2, canonical.Item3);
                    }

                    if (buildOk && ArHelper.ARGameObjects != null && ArHelper.ARGameObjects.Count > 0 && ArHelper.ARGameObjects[0] != null)
                    {
                        ArHelper.ARGameObjects[0].ImagePoints = imagePoints;
                        ArHelper.ARGameObjects[0].ObjectPoints = objectPoints;
                        imageObjectPointsAssigned = true;
                    }
                }
            }

            // If ImagePoints / ObjectPoints were not set, clear AR points (e.g. viewport exit).
            if (!imageObjectPointsAssigned)
                ArHelper.ResetARGameObjectsImagePointsAndObjectPoints();
        }

        private Vec3f[] LoadCanonicalMeshWorldLandmarks(string metadataPath)
        {
            if (string.IsNullOrEmpty(metadataPath) || !File.Exists(metadataPath))
            {
                Debug.LogWarning("Canonical metadata not found: " + metadataPath);
                return null;
            }

            const int vertexStride = 5; // xyzuv
            int expectedVertexCount = MediaPipeFaceLandmarker.FaceLandmarkerEstimationData.LANDMARK_VEC3F_COUNT;
            var vertexFloats = new List<float>(expectedVertexCount * vertexStride);

            using (var sr = new StringReader(File.ReadAllText(metadataPath)))
            {
                string line;
                bool inCanonicalMesh = false;
                while ((line = sr.ReadLine()) != null)
                {
                    string t = line.Trim();

                    if (!inCanonicalMesh)
                    {
                        if (t.StartsWith("canonical_mesh"))
                            inCanonicalMesh = true;
                        continue;
                    }

                    if (t.StartsWith("vertex_buffer:"))
                    {
                        string num = t.Substring("vertex_buffer:".Length).Trim();
                        if (float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                            vertexFloats.Add(v);
                    }
                    else if (t.StartsWith("index_buffer:"))
                    {
                        break;
                    }
                }
            }

            if (vertexFloats.Count < vertexStride)
            {
                Debug.LogWarning("Failed to parse canonical_mesh vertex_buffer.");
                return null;
            }

            int actualVertexCount = vertexFloats.Count / vertexStride;
            int copyCount = Mathf.Min(actualVertexCount, expectedVertexCount);
            var landmarks = new Vec3f[expectedVertexCount];
            for (int i = 0; i < copyCount; i++)
            {
                int baseIdx = i * vertexStride;
                float x = vertexFloats[baseIdx + 0] * CANONICAL_MESH_SCALE;
                float y = vertexFloats[baseIdx + 1] * CANONICAL_MESH_SCALE;
                float z = vertexFloats[baseIdx + 2] * CANONICAL_MESH_SCALE;
                landmarks[i] = new Vec3f(x, y, z);
            }

            if (actualVertexCount != expectedVertexCount)
                Debug.LogWarning($"canonical mesh vertex count mismatch. expected={expectedVertexCount}, actual={actualVertexCount}");

            return landmarks;
        }
    }
}

#endif
