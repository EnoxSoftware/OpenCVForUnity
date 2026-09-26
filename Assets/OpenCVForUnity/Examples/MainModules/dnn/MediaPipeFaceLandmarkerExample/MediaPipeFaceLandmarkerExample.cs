#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    /// MediaPipe Face Landmarker Example
    /// Detects faces, 468 facial landmarks, blendshapes, and optional 3D pose from input frames.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Loading multiple MediaPipe ONNX/Sentis models and geometry metadata from StreamingAssets
    /// - Toggling Sentis vs OpenCV DNN inference and optional async detection
    /// - Converting RGBA frames to BGR, running MediaPipeFaceLandmarkerMultiBackend, and visualizing landmarks on Mat
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Size"/>, <see cref="Scalar"/>, <see cref="Point"/>
    /// - <see cref="Imgproc"/>: cvtColor
    /// - <see cref="MediaPipeFaceLandmarkerMultiBackend"/>, <see cref="MatSingleFlightSyncAsyncRunner"/>, <see cref="MultiBackendDnn"/>
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://github.com/google-ai-edge/mediapipe
    /// </para>
    /// <para>
    /// [Tested Models]
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeFaceLandmarkerExample/face_detector.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeFaceLandmarkerExample/face_landmarks_detector.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeFaceLandmarkerExample/face_blendshapes.onnx
    /// https://raw.githubusercontent.com/EnoxSoftware/OpenCVForUnityExampleAssets/f4f791d5f330cdef388369f78120457f23f8998d/dnn/MediaPipeFaceLandmarkerExample/geometry_pipeline_metadata_including_iris_landmarks.pbtxt
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
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
        [Tooltip("StreamingAssets-relative path to the face detection model (faceDetectorModelFilepath).")]
        public string FaceLandmarkerFaceDetectorModelFileName = "OpenCVForUnityExamples/dnn/mediapipe/face_detector.onnx";
        [Tooltip("StreamingAssets-relative path to the face landmark model (faceLandmarksModelFilepath).")]
        public string FaceLandmarkerFaceLandmarksModelFileName = "OpenCVForUnityExamples/dnn/mediapipe/face_landmarks_detector.onnx";
        [Tooltip("MediaPipeFaceLandmarkerMultiBackend running mode. IMAGE is single-shot inference; VIDEO is for continuous frames.")]
        public MediaPipeFaceLandmarkerMultiBackend.MediaPipeFaceRunningMode FaceLandmarkerRunningMode = MediaPipeFaceLandmarkerMultiBackend.MediaPipeFaceRunningMode.VIDEO;
        [Tooltip("numFaces when initializing MediaPipeFaceLandmarkerMultiBackend (number of faces to detect simultaneously).")]
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
        private float _imageSizeScale = 1f;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private Mat _bgrMat;
        private MediaPipeFaceLandmarkerMultiBackend _faceLandmarkerWorker;
        private string _faceLandmarkerFaceDetectionModelFilepathOnnx;
        private string _faceLandmarkerEstimationModelFilepathOnnx;
        private string _faceLandmarkerFaceBlendshapesModelFilepathOnnx;
        private string _faceLandmarkerFaceDetectionModelFilepathSentis;
        private string _faceLandmarkerEstimationModelFilepathSentis;
        private string _faceLandmarkerFaceBlendshapesModelFilepathSentis;
        private string _faceLandmarkerFaceGeometryPipelineMetadataFilepath;
        private OpenCVForUnity.Extensions.Vec3f[] _canonicalWorldLandmarks;
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

            _faceLandmarkerFaceDetectionModelFilepathOnnx = await OpenCVForUnityEnv.GetFilePathAsync(
                FaceLandmarkerFaceDetectorModelFileName,
                cancellationToken: _cts.Token);
            _faceLandmarkerEstimationModelFilepathOnnx = await OpenCVForUnityEnv.GetFilePathAsync(
                FaceLandmarkerFaceLandmarksModelFileName,
                cancellationToken: _cts.Token);
            _faceLandmarkerFaceBlendshapesModelFilepathOnnx = !string.IsNullOrWhiteSpace(FaceLandmarkerFaceBlendshapesModelFileName)
                ? await OpenCVForUnityEnv.GetFilePathAsync(
                    FaceLandmarkerFaceBlendshapesModelFileName,
                    cancellationToken: _cts.Token)
                : null;
            if (OpenCVForUnityEnv.IsSentisIntegrationAvailable)
            {
                // Resolve companion .sentis paths when Sentis integration is enabled.
                _faceLandmarkerFaceDetectionModelFilepathSentis = await OpenCVForUnityEnv.GetFilePathAsync(
                    MultiBackendDnn.ResolveSentisModelPathFromOnnxPath(FaceLandmarkerFaceDetectorModelFileName),
                    cancellationToken: _cts.Token);
                _faceLandmarkerEstimationModelFilepathSentis = await OpenCVForUnityEnv.GetFilePathAsync(
                    MultiBackendDnn.ResolveSentisModelPathFromOnnxPath(FaceLandmarkerFaceLandmarksModelFileName),
                    cancellationToken: _cts.Token);
                _faceLandmarkerFaceBlendshapesModelFilepathSentis = !string.IsNullOrWhiteSpace(FaceLandmarkerFaceBlendshapesModelFileName)
                    ? await OpenCVForUnityEnv.GetFilePathAsync(
                        MultiBackendDnn.ResolveSentisModelPathFromOnnxPath(FaceLandmarkerFaceBlendshapesModelFileName),
                        cancellationToken: _cts.Token)
                    : null;
            }
            _faceLandmarkerFaceGeometryPipelineMetadataFilepath = !string.IsNullOrWhiteSpace(FaceLandmarkerFaceGeometryPipelineMetadataFileName)
                ? await OpenCVForUnityEnv.GetFilePathAsync(
                    FaceLandmarkerFaceGeometryPipelineMetadataFileName,
                    cancellationToken: _cts.Token)
                : null;

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            _canonicalWorldLandmarks = LoadCanonicalMeshWorldLandmarks(_faceLandmarkerFaceGeometryPipelineMetadataFilepath);

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
            // Convert RGBA camera frame to BGR for MediaPipe face landmarker input.
            Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

            if (_inferenceRunner != null && _faceLandmarkerWorker != null)
            {
                // Submit sync or async face landmark detection; output Mat[] holds landmark results.
                _inferenceRunner.SubmitWork(
                    _bgrMat,
                    syncWork: m => _faceLandmarkerWorker.Detect(m, useCopyOutput: true),
                    asyncWork: async m =>
                    {
                        CancellationToken ct = _inferenceRunner.InFlightAsyncWorkCancellationToken;
                        return await _faceLandmarkerWorker.DetectAsync(m, ct);
                    });
                if (_inferenceRunner.TryGetLatestResult(out Mat[] faceLandmarkerResults))
                {
                    UpdateSkeletonFromFaceLandmarkerResults(faceLandmarkerResults);

                    if (faceLandmarkerResults.Length > 0 && faceLandmarkerResults[0] != null && !faceLandmarkerResults[0].empty())
                    {
                        VisualizeFaceLandmarkerOnRgba(rgbaMat, faceLandmarkerResults);
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
            if (_faceLandmarkerWorker == null || _inferenceRunner == null)
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
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _faceLandmarkerWorker, UseAsyncInference, InferenceFramework);
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

            _faceLandmarkerWorker?.Cancel();

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
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _faceLandmarkerWorker, UseAsyncInference, InferenceFramework);
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
        /// Fully re-initializes the face landmarker worker and inference runner after
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
            UpdateFpsMonitorInferenceInfo(_fpsMonitor, _faceLandmarkerWorker, UseAsyncInference, InferenceFramework);

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

            var faceLandmarkerWorker = _faceLandmarkerWorker;
            _faceLandmarkerWorker = null;
            if (faceLandmarkerWorker != null)
            {
                await faceLandmarkerWorker.DisposeAsync();
            }
        }

        /// <summary>
        /// Initializes inference from the resolved model path and current backend settings (Sentis asset path when using Sentis; otherwise ONNX).
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when inference resources were created; otherwise <see langword="false"/>.
        /// </returns>
        private bool TryInitializeInference()
        {
            bool useSentis = InferenceFramework == InferenceFrameworkSelectionKind.UnitySentis && OpenCVForUnityEnv.IsSentisIntegrationAvailable;
            string detPath = useSentis ? _faceLandmarkerFaceDetectionModelFilepathSentis : _faceLandmarkerFaceDetectionModelFilepathOnnx;
            string estPath = useSentis ? _faceLandmarkerEstimationModelFilepathSentis : _faceLandmarkerEstimationModelFilepathOnnx;
            string blendPath = null;
            if (!string.IsNullOrWhiteSpace(FaceLandmarkerFaceBlendshapesModelFileName))
            {
                blendPath = useSentis
                    ? _faceLandmarkerFaceBlendshapesModelFilepathSentis
                    : _faceLandmarkerFaceBlendshapesModelFilepathOnnx;
            }

            if (string.IsNullOrEmpty(detPath) || string.IsNullOrEmpty(estPath))
            {
                Debug.LogError(FaceLandmarkerFaceDetectorModelFileName + " or " + FaceLandmarkerFaceLandmarksModelFileName + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);
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
                    _faceLandmarkerWorker = new MediaPipeFaceLandmarkerMultiBackend(
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
                        dnnBackend: SentisInferenceBackendKind.UnitySentis,
                        dnnTarget: SentisInferenceTarget);
                    Debug.Log(
                        "MediaPipeFaceLandmarkerMultiBackend initialized (Sentis / UnitySentis, backend="
                        + SentisInferenceUtils.GetTargetDisplayName(SentisInferenceTarget) + ").",
                        this);
                }
                else
                {
                    _faceLandmarkerWorker = new MediaPipeFaceLandmarkerMultiBackend(
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
                        dnnBackend: OpenCVDnnInferenceBackendKind.OpenCv,
                        dnnTarget: OpenCVDnnInferenceTargetKind.Cpu);
                    Debug.Log("MediaPipeFaceLandmarkerMultiBackend initialized (OpenCV DNN).", this);
                }

                MediaPipeFaceLandmarkerMultiBackend faceLandmarkerWorker = _faceLandmarkerWorker;
                _inferenceRunner = new MatSingleFlightSyncAsyncRunner(
                    useAsyncWork: UseAsyncInference,
                    asyncWorkCancellationToken: _cts.Token,
                    disposeAsyncAfterWorkTask: async () =>
                    {
                        await faceLandmarkerWorker.WaitForCompletionAsync();
                    });

                return _faceLandmarkerWorker != null && _inferenceRunner != null;
            }
            catch (Exception ex)
            {
                Debug.LogError("MediaPipeFaceLandmarkerExample TryInitializeInference failed: " + ex, this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "MediaPipe Face Landmarker failed to initialize.\nPlease read console message.";
                }

                if (_faceLandmarkerWorker != null)
                {
                    _faceLandmarkerWorker.Dispose();
                    _faceLandmarkerWorker = null;
                }

                _inferenceRunner = null;
                return false;
            }
        }

        /// <summary>
        /// Face Landmarker overlay. When extra outputs (blendshapes / facial transformation) are enabled, visualizes the full result pack.
        /// </summary>
        private void VisualizeFaceLandmarkerOnRgba(Mat rgbaMat, Mat[] pack)
        {
            if (_faceLandmarkerWorker == null || pack == null || pack.Length == 0 || pack[0] == null || pack[0].empty())
            {
                return;
            }

            bool hasBlendshape = FaceLandmarkerOutputFaceBlendshapes && pack.Length > 1 && pack[1] != null && !pack[1].empty();
            bool hasFacialTransformation = FaceLandmarkerOutputFacialTransformationMatrixes && pack.Length > 2 && pack[2] != null && !pack[2].empty();
            if (hasBlendshape || hasFacialTransformation)
            {
                _faceLandmarkerWorker.Visualize(rgbaMat, pack, printResult: FaceLandmarkerVisualizePrintResult, isRGB: true);
            }
            else
            {
                _faceLandmarkerWorker.Visualize(rgbaMat, pack[0], printResult: FaceLandmarkerVisualizePrintResult, isRGB: true);
            }
        }

        /// <summary>
        /// Updates skeleton display and AR ImagePoints / ObjectPoints from row index [0] (faces as rows) of the MediaPipeFaceLandmarkerMultiBackend Detect / DetectAsync return value.
        /// </summary>
        /// <param name="faceLandmarkerResults">Result of <c>Detect</c> / <c>DetectAsync</c> (packed mats). Primary landmarks are in [0] (rows = faces). Null or empty when nothing is detected.</param>
        private void UpdateSkeletonFromFaceLandmarkerResults(Mat[] faceLandmarkerResults)
        {
            if (ArHelper == null)
            {
                return;
            }

            bool imageObjectPointsAssigned = false;

            Mat landmarkMat = null;
            if (faceLandmarkerResults != null && faceLandmarkerResults.Length > 0)
            {
                landmarkMat = faceLandmarkerResults[0];
            }

            if (_faceLandmarkerWorker != null && landmarkMat != null && !landmarkMat.empty() && landmarkMat.rows() > 0
                && _bgrMat != null && _canonicalWorldLandmarks != null)
            {
                Span<MediaPipeFaceLandmarkerMultiBackend.FaceLandmarkerEstimationData> dataSpan =
                    _faceLandmarkerWorker.ToStructuredDataAsSpan(landmarkMat);
                if (dataSpan.Length > 0)
                {
                    // Update AR for the first face only
                    ref readonly MediaPipeFaceLandmarkerMultiBackend.FaceLandmarkerEstimationData data = ref dataSpan[0];
                    ReadOnlySpan<OpenCVForUnity.Extensions.Vec3f> landmarks = data.GetNormLandmarks();
                    float fw = _bgrMat.cols();
                    float fh = _bgrMat.rows();

                    if (SkeletonVisualizer != null && SkeletonVisualizer.ShowSkeleton)
                    {
                        SkeletonVisualizer.UpdateFace(_canonicalWorldLandmarks);
                    }

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
                        OpenCVForUnity.Extensions.Vec3f canonical = _canonicalWorldLandmarks[index];
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
            {
                ArHelper.ResetARGameObjectsImagePointsAndObjectPoints();
            }
        }

        private OpenCVForUnity.Extensions.Vec3f[] LoadCanonicalMeshWorldLandmarks(string metadataPath)
        {
            if (string.IsNullOrEmpty(metadataPath) || !File.Exists(metadataPath))
            {
                Debug.LogWarning("Canonical metadata not found: " + metadataPath, this);
                return null;
            }

            const int VERTEX_STRIDE = 5; // xyzuv
            int expectedVertexCount = MediaPipeFaceLandmarkerMultiBackend.FaceLandmarkerEstimationData.LANDMARK_VEC3F_COUNT;
            var vertexFloats = new List<float>(expectedVertexCount * VERTEX_STRIDE);

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
                        {
                            inCanonicalMesh = true;
                        }

                        continue;
                    }

                    if (t.StartsWith("vertex_buffer:"))
                    {
                        string num = t.Substring("vertex_buffer:".Length).Trim();
                        if (float.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                        {
                            vertexFloats.Add(v);
                        }
                    }
                    else if (t.StartsWith("index_buffer:"))
                    {
                        break;
                    }
                }
            }

            if (vertexFloats.Count < VERTEX_STRIDE)
            {
                Debug.LogWarning("Failed to parse canonical_mesh vertex_buffer.", this);
                return null;
            }

            int actualVertexCount = vertexFloats.Count / VERTEX_STRIDE;
            int copyCount = Mathf.Min(actualVertexCount, expectedVertexCount);
            var landmarks = new OpenCVForUnity.Extensions.Vec3f[expectedVertexCount];
            for (int i = 0; i < copyCount; i++)
            {
                int baseIdx = i * VERTEX_STRIDE;
                float x = vertexFloats[baseIdx + 0] * CANONICAL_MESH_SCALE;
                float y = vertexFloats[baseIdx + 1] * CANONICAL_MESH_SCALE;
                float z = vertexFloats[baseIdx + 2] * CANONICAL_MESH_SCALE;
                landmarks[i] = new OpenCVForUnity.Extensions.Vec3f(x, y, z);
            }

            if (actualVertexCount != expectedVertexCount)
            {
                Debug.LogWarning($"canonical mesh vertex count mismatch. expected={expectedVertexCount}, actual={actualVertexCount}", this);
            }

            return landmarks;
        }
    }
}
#endif
