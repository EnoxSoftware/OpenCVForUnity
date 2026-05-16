#if !UNITY_WSA_10_0 && NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Worker;
using OpenCVForUnity.UnityIntegration.Runner;
using OpenCVForUnity.UnityIntegration.Worker.DnnModule;
using OpenCVForUnity.UnityIntegration.Worker.Utils;
using OpenCVForUnity.UnityIntegration.Helper.AR;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
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
    /// MediaPipe Hand Landmarker sample (hand pose and hand landmark inference using <see cref="MediaPipeHandLandmarker"/>).
    /// Referring to https://github.com/google-ai-edge/mediapipe
    ///
    /// [Tested Models]
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeHandLandmarkerExample/hand_detector.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeHandLandmarkerExample/hand_landmarks_detector.onnx
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class MediaPipeHandLandmarkerExample : MonoBehaviour
    {
        // Constants
        private static readonly byte[] SELECTED_INDICES = {
            (byte)MediaPipeHandLandmarker.KeyPoint.Wrist, // Wrist
            (byte)MediaPipeHandLandmarker.KeyPoint.Thumb4, // Thumb tip
            (byte)MediaPipeHandLandmarker.KeyPoint.Index4, // Index finger tip
            (byte)MediaPipeHandLandmarker.KeyPoint.Middle4, // Middle finger tip
            (byte)MediaPipeHandLandmarker.KeyPoint.Ring4, // Ring finger tip
            (byte)MediaPipeHandLandmarker.KeyPoint.Pinky4, // Little finger tip
            (byte)MediaPipeHandLandmarker.KeyPoint.Thumb1, // Thumb finger base
            (byte)MediaPipeHandLandmarker.KeyPoint.Index1, // Index finger base
            (byte)MediaPipeHandLandmarker.KeyPoint.Middle1, // Middle finger base
            (byte)MediaPipeHandLandmarker.KeyPoint.Ring1, // Ring finger base
            (byte)MediaPipeHandLandmarker.KeyPoint.Pinky1, // Little finger base
        };

        // Public Fields
        [Header("UI")]
        [Tooltip("ON: Sentis. OFF: OpenCV DNN. Assign OnUseSentisInferenceToggleValueChanged to this toggle's On Value Changed in the Inspector.")]
        public Toggle UseSentisInferenceToggle;
        [Tooltip("Sentis backend selector. Dropdown option order must match Enum.GetValues(typeof(BackendType)) (numeric order). Assign OnSentisBackendDropdownValueChanged to On Value Changed (int). Value changes reinitialize inference.")]
        public Dropdown SentisBackendDropdown;
#if OPENCV_SENTIS_AVAILABLE
        [Tooltip("When enabled, runs palm and hand inference with Sentis (MultiBackendDnn.DNN_BACKEND_UNITY_SENTIS). Inspector paths may stay .onnx; at runtime they are rewritten to .sentis and loaded from StreamingAssets (place a matching .sentis beside the onnx file).")]
        public bool UseSentisInference = true;
        [Tooltip("When using Sentis: dnnTarget selects Sentis BackendType (CPU / GPU, etc.).")]
        public BackendType SentisBackendType = BackendType.GPUCompute;
#endif
        public Toggle UseAsyncInferenceToggle;
        public bool UseAsyncInference = true;
        public Toggle ShowSkeletonToggle;
        public bool ShowSkeleton;

        [Header("Inference")]
        [Tooltip("StreamingAssets-relative path to the palm detection model (palmModelFilepath).")]
        public string HandLandmarkerPalmModelFileName = "OpenCVForUnityExamples/dnn/mediapipe/hand_detector.onnx";
        [Tooltip("StreamingAssets-relative path to the hand landmark model (handLandmarksModelFilepath).")]
        public string HandLandmarkerHandLandmarksModelFileName = "OpenCVForUnityExamples/dnn/mediapipe/hand_landmarks_detector.onnx";
        [Tooltip("MediaPipeHandLandmarker running mode. IMAGE is single-shot inference; VIDEO is for continuous frames.")]
        public MediaPipeHandLandmarker.MediaPipeHandRunningMode HandLandmarkerRunningMode = MediaPipeHandLandmarker.MediaPipeHandRunningMode.VIDEO;
        [Tooltip("numHands when initializing MediaPipeHandLandmarker (number of hands to detect simultaneously).")]
        [Range(1, 10)]
        public int HandLandmarkerNumHands = 2;
        [Tooltip("Hand detection confidence threshold (minHandDetectionConfidence).")]
        [Range(0f, 1f)]
        public float HandLandmarkerMinHandDetectionConfidence = 0.5f;
        [Tooltip("Hand presence confidence threshold (minHandPresenceConfidence).")]
        [Range(0f, 1f)]
        public float HandLandmarkerMinHandPresenceConfidence = 0.5f;
        [Tooltip("Tracking confidence threshold (minTrackingConfidence).")]
        [Range(0f, 1f)]
        public float HandLandmarkerMinTrackingConfidence = 0.5f;

        [Header("Visualize")]
        [Tooltip("Whether Hand Landmarker Visualize calls print results to the console.")]
        public bool HandLandmarkerVisualizePrintResult = false;

        [Space(10)]
        [Header("Show 3D Skeleton")]
        public ARHelper ArHelperRightHand;
        public MediaPipeHandPoseSkeletonVisualizer SkeletonVisualizerRightHand;
        public ARHelper ArHelperLeftHand;
        public MediaPipeHandPoseSkeletonVisualizer SkeletonVisualizerLeftHand;

        // Private Fields
        private Texture2D _texture;
        private MultiSource2MatHelper _multiSource2MatHelper;
        private Mat _bgrMat;
        private MediaPipeHandLandmarker _handLandmarkerWorker;
        private string _handLandmarkerPalmModelFilepathOnnx;
        private string _handLandmarkerLandmarksModelFilepathOnnx;
#if OPENCV_SENTIS_AVAILABLE
        private string _handLandmarkerPalmModelFilepathSentis;
        private string _handLandmarkerLandmarksModelFilepathSentis;
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

            if (SkeletonVisualizerRightHand != null) SkeletonVisualizerRightHand.ShowSkeleton = ShowSkeleton;
            if (SkeletonVisualizerLeftHand != null) SkeletonVisualizerLeftHand.ShowSkeleton = ShowSkeleton;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _handLandmarkerPalmModelFilepathOnnx = await OpenCVEnv.GetFilePathTaskAsync(
                HandLandmarkerPalmModelFileName,
                cancellationToken: _cts.Token);
            _handLandmarkerLandmarksModelFilepathOnnx = await OpenCVEnv.GetFilePathTaskAsync(
                HandLandmarkerHandLandmarksModelFileName,
                cancellationToken: _cts.Token);
#if OPENCV_SENTIS_AVAILABLE
            _handLandmarkerPalmModelFilepathSentis = await OpenCVEnv.GetFilePathTaskAsync(
                StreamingAssetPathOnnxToSentisIfNeeded(HandLandmarkerPalmModelFileName),
                cancellationToken: _cts.Token);
            _handLandmarkerLandmarksModelFilepathSentis = await OpenCVEnv.GetFilePathTaskAsync(
                StreamingAssetPathOnnxToSentisIfNeeded(HandLandmarkerHandLandmarksModelFileName),
                cancellationToken: _cts.Token);
#endif

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

                if (_inferenceRunner != null && _handLandmarkerWorker != null)
                {
                    _inferenceRunner.SubmitWork(
                        _bgrMat,
                        syncWork: m => _handLandmarkerWorker.Detect(m, useCopyOutput: true),
                        asyncWork: async m =>
                        {
                            CancellationToken ct = _inferenceRunner.InFlightAsyncWorkCancellationToken;
                            return await _handLandmarkerWorker.DetectTaskAsync(m, ct);
                        });

                    if (_inferenceRunner.TryGetLatestResult(out Mat handLandmarkerResults))
                    {
                        UpdateSkeletonFromHandLandmarkerResults(handLandmarkerResults);

                        _handLandmarkerWorker.Visualize(rgbaMat, handLandmarkerResults, printResult: HandLandmarkerVisualizePrintResult, isRGB: true);
                    }
                }

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private async void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            await DisposeInferenceAsync();

            _cts?.Dispose();

            OpenCVDebug.SetDebugMode(false);
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
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _handLandmarkerWorker, UseAsyncInference);
            }

            _bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);

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

            _inferenceRunner?.Cancel();

            _bgrMat?.Dispose(); _bgrMat = null;

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;

            ArHelperRightHand?.Dispose();
            ArHelperLeftHand?.Dispose();
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

            UpdateFpsMonitorInferenceInfo(_fpsMonitor, _handLandmarkerWorker, UseAsyncInference);

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

            UpdateFpsMonitorInferenceInfo(_fpsMonitor, _handLandmarkerWorker, UseAsyncInference);

            _inferenceReinitializing = false;
            UpdateInferenceModeToggles(inferenceReinitializing: false);
#endif
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
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _handLandmarkerWorker, UseAsyncInference);
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
                if (SkeletonVisualizerRightHand != null) SkeletonVisualizerRightHand.ShowSkeleton = ShowSkeleton;
                if (SkeletonVisualizerLeftHand != null) SkeletonVisualizerLeftHand.ShowSkeleton = ShowSkeleton;
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
        /// Disposes the inference runner and <see cref="MediaPipeHandLandmarker"/> asynchronously (used from <see cref="OnDestroy"/> and when switching backends).
        /// </summary>
        private async Task DisposeInferenceAsync()
        {
            if (_inferenceRunner != null)
                await _inferenceRunner.DisposeAsync();
            _inferenceRunner = null;

            _handLandmarkerWorker?.Dispose();
            _handLandmarkerWorker = null;
        }

        /// <summary>
        /// Initializes inference from the resolved model path and current backend settings (Sentis asset path when using Sentis; otherwise ONNX).
        /// </summary>
        private void InitializeInference()
        {
            string palmPath = _handLandmarkerPalmModelFilepathOnnx;
            string landmarksPath = _handLandmarkerLandmarksModelFilepathOnnx;
#if OPENCV_SENTIS_AVAILABLE
            if (UseSentisInference)
            {
                palmPath = _handLandmarkerPalmModelFilepathSentis;
                landmarksPath = _handLandmarkerLandmarksModelFilepathSentis;
            }
#endif
            if (string.IsNullOrEmpty(palmPath) || string.IsNullOrEmpty(landmarksPath))
            {
                Debug.LogError(HandLandmarkerPalmModelFileName + " or " + HandLandmarkerHandLandmarksModelFileName + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
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
                    _handLandmarkerWorker = new MediaPipeHandLandmarker(
                        palmPath,
                        landmarksPath,
                        HandLandmarkerRunningMode,
                        numHands: Mathf.Max(1, HandLandmarkerNumHands),
                        minHandDetectionConfidence: HandLandmarkerMinHandDetectionConfidence,
                        minHandPresenceConfidence: HandLandmarkerMinHandPresenceConfidence,
                        minTrackingConfidence: HandLandmarkerMinTrackingConfidence,
                        dnnBackend: MultiBackendDnn.DNN_BACKEND_UNITY_SENTIS,
                        dnnTarget: (int)SentisBackendType);
                    Debug.Log("MediaPipeHandLandmarker initialized (Sentis / DNN_BACKEND_UNITY_SENTIS, backend=" + SentisBackendType + ").");
                }
                else
#endif
                {
                    _handLandmarkerWorker = new MediaPipeHandLandmarker(
                        palmPath,
                        landmarksPath,
                        HandLandmarkerRunningMode,
                        numHands: Mathf.Max(1, HandLandmarkerNumHands),
                        minHandDetectionConfidence: HandLandmarkerMinHandDetectionConfidence,
                        minHandPresenceConfidence: HandLandmarkerMinHandPresenceConfidence,
                        minTrackingConfidence: HandLandmarkerMinTrackingConfidence);
                    Debug.Log("MediaPipeHandLandmarker initialized (OpenCV DNN).");
                }

                _inferenceRunner = new MatSingleFlightSyncAsyncRunner(
                    useAsyncWork: UseAsyncInference,
                    asyncWorkCancellationToken: _cts.Token,
                    disposeAsyncAfterWorkTask: async () =>
                    {
                        await _handLandmarkerWorker.WaitForCompletionTaskAsync();
                    });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("MediaPipeHandLandmarkerExample InitializeInference failed: " + ex);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("MediaPipe Hand Landmarker failed to initialize.\nPlease read console message.", 20000);
                }
            }
        }

        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            // MediaPipe Hand Landmarker worker (uses HAND_LANDMARKER_* ONNX / Sentis models).
            InitializeInference();

            _multiSource2MatHelper.Initialize();
        }

        /// <summary>
        /// Updates skeleton display and AR ImagePoints / ObjectPoints from the MediaPipeHandLandmarker Detect / DetectTaskAsync return value (rows = hands).
        /// </summary>
        /// <param name="handLandmarkerResults">Return value of <c>Detect</c> / <c>DetectTaskAsync</c> (rows = hands). Null or zero rows when nothing is detected.</param>
        private void UpdateSkeletonFromHandLandmarkerResults(Mat handLandmarkerResults)
        {
            bool leftImageObjectPointsAssigned = false;
            bool rightImageObjectPointsAssigned = false;

            if (_handLandmarkerWorker != null && handLandmarkerResults != null && !handLandmarkerResults.empty() && handLandmarkerResults.rows() > 0
                && _bgrMat != null)
            {
                Span<MediaPipeHandLandmarker.HandLandmarkerEstimationData> dataSpan =
                    _handLandmarkerWorker.ToStructuredDataAsSpan(handLandmarkerResults);
                float fw = _bgrMat.cols();
                float fh = _bgrMat.rows();
                for (int h = 0; h < dataSpan.Length; h++)
                {
                    ref readonly MediaPipeHandLandmarker.HandLandmarkerEstimationData data = ref dataSpan[h];
                    Vec3f[] landmarksScreen = data.GetNormLandmarksArray();
                    Vec3f[] landmarksWorld = data.GetWorldLandmarksArray();
                    float handedness = data.Handedness;

                    var imagePoints = new Vector2[SELECTED_INDICES.Length];
                    var objectPoints = new Vector3[SELECTED_INDICES.Length];
                    bool buildOk = true;
                    for (int i = 0; i < SELECTED_INDICES.Length; i++)
                    {
                        int index = SELECTED_INDICES[i];
                        if (landmarksScreen == null || landmarksWorld == null
                            || index >= landmarksScreen.Length || index >= landmarksWorld.Length)
                        {
                            buildOk = false;
                            break;
                        }
                        ref readonly var screen = ref landmarksScreen[index];
                        ref readonly var world = ref landmarksWorld[index];
                        // Image landmarks from the landmarker are normalized coordinates (equivalent to NormalizedLandmark in the reference implementation).
                        imagePoints[i] = new Vector2(screen.Item1 * fw, screen.Item2 * fh);
                        objectPoints[i] = new Vector3(world.Item1, world.Item2, world.Item3);
                    }

                    if (!buildOk)
                        continue;

                    if (!MediaPipeHandLandmarker.HandLandmarkerEstimationData.IsRightHandDominant(handedness)) // Left hand or absent (0)
                    {
                        if (SkeletonVisualizerLeftHand != null && SkeletonVisualizerLeftHand.ShowSkeleton)
                            SkeletonVisualizerLeftHand.UpdatePose(landmarksWorld);
                        if (ArHelperLeftHand != null && ArHelperLeftHand.ARGameObjects != null && ArHelperLeftHand.ARGameObjects.Count > 0 && ArHelperLeftHand.ARGameObjects[0] != null)
                        {
                            ArHelperLeftHand.ARGameObjects[0].ImagePoints = imagePoints;
                            ArHelperLeftHand.ARGameObjects[0].ObjectPoints = objectPoints;
                            leftImageObjectPointsAssigned = true;
                        }
                    }
                    else // Right hand
                    {
                        if (SkeletonVisualizerRightHand != null && SkeletonVisualizerRightHand.ShowSkeleton)
                            SkeletonVisualizerRightHand.UpdatePose(landmarksWorld);
                        if (ArHelperRightHand != null && ArHelperRightHand.ARGameObjects != null && ArHelperRightHand.ARGameObjects.Count > 0 && ArHelperRightHand.ARGameObjects[0] != null)
                        {
                            ArHelperRightHand.ARGameObjects[0].ImagePoints = imagePoints;
                            ArHelperRightHand.ARGameObjects[0].ObjectPoints = objectPoints;
                            rightImageObjectPointsAssigned = true;
                        }
                    }
                }
            }

            if (ArHelperLeftHand != null && !leftImageObjectPointsAssigned)
                ArHelperLeftHand.ResetARGameObjectsImagePointsAndObjectPoints();
            if (ArHelperRightHand != null && !rightImageObjectPointsAssigned)
                ArHelperRightHand.ResetARGameObjectsImagePointsAndObjectPoints();
        }
    }
}

#endif
