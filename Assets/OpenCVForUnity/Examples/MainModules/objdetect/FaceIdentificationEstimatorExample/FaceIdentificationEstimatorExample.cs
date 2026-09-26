#if !UNITY_WSA_10_0

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.Runner;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.Extensions.Worker.DnnModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Interaction;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenCVDebug = OpenCVForUnity.Extensions.OpenCVDebug;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Face Identification Estimator Example
    /// Detects faces, registers identities by touch, and recognizes registered faces in input frames.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - YuNet + SFace DNN pipeline via FaceIdentificationEstimator worker
    /// - Sync/async inference with MatSingleFlightSyncAsyncRunner
    /// - Touch-to-register workflow using TextureSelector point selection
    /// - Saving and loading registered aligned faces as PNG files to persistent data path (metadata encoded in filename)
    /// - DebugMat preview of aligned registered faces
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="FaceIdentificationEstimator"/>, <see cref="MatSingleFlightSyncAsyncRunner"/>, <see cref="Imgproc"/>
    /// - <see cref="Imgcodecs"/>: imread, imwrite
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// Face Detection: face_detection_yunet_2023mar.onnx https://github.com/opencv/opencv_zoo/blob/main/models/face_detection_yunet/face_detection_yunet_2023mar.onnx
    /// Face Recognition: face_recognition_sface_2021dec.onnx https://github.com/opencv/opencv_zoo/blob/main/models/face_recognition_sface/face_recognition_sface_2021dec.onnx
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class FaceIdentificationEstimatorExample : MonoBehaviour
    {
        // Public Fields
        [Header("Output")]
        [Tooltip("The RawImage for previewing the result.")]
        public RawImage ResultPreview;

        [Header("UI")]
        public Toggle UseAsyncInferenceToggle;
        public bool UseAsyncInference = true;

        [Header("Model Settings")]
        [Tooltip("Path to a binary file of face detection model contains trained weights.")]
        public string FaceDetectionModel = "OpenCVForUnityExamples/objdetect/face_detection_yunet_2023mar.onnx";

        [Tooltip("Path to a binary file of face recognition model contains trained weights.")]
        public string FaceRecognitionModel = "OpenCVForUnityExamples/objdetect/face_recognition_sface_2021dec.onnx";

        [Tooltip("Path to a text file of model contains network configuration.")]
        public string Config;

        [Tooltip("Confidence threshold.")]
        public float ConfThreshold = 0.6f;

        [Tooltip("Non-maximum suppression threshold.")]
        public float NmsThreshold = 0.3f;

        [Tooltip("Maximum detections per image.")]
        public int TopK = 100;

        [Tooltip("Preprocess input image by resizing to a specific width.")]
        public int InpWidth = 320;

        [Tooltip("Preprocess input image by resizing to a specific height.")]
        public int InpHeight = 320;

        [Header("Face Registration")]
        [Tooltip("Input field for face name registration.")]
        public InputField FaceNameInput;

        [Tooltip("Button to clear all registered faces.")]
        public Button ClearFacesButton;

        [Tooltip("Button to save registered faces to persistent data path.")]
        public Button SaveFacesButton;

        [Tooltip("Button to load registered faces from persistent data path.")]
        public Button LoadFacesButton;

        [Header("Point Selection")]
        [Tooltip("TextureSelector for point selection on the result preview.")]
        public TextureSelector PointSelector;

        // Private Fields
        private Texture2D _texture;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private Mat _bgrMat;
        private FaceIdentificationEstimator _faceIdentificationEstimator;
        private string _configFilepath;
        private string _faceDetectionModelFilepath;
        private string _faceRecognitionModelFilepath;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private MatSingleFlightSyncAsyncRunner _inferenceRunner;
        private bool _shouldUpdateFromPoint = false;

        private const string SAVE_DIRECTORY_NAME = "FaceIdentificationEstimatorExample";
        private const string FACE_IMAGE_FORMAT = "png";

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            WireSourceToMatControlPanelHooks();

            // Update GUI state
            UpdateUseAsyncInference();
            UpdateInferenceModeToggles(inferenceReinitializing: false);

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            if (!string.IsNullOrEmpty(Config))
            {
                _configFilepath = await OpenCVForUnityEnv.GetFilePathAsync(Config, cancellationToken: _cts.Token);
                if (string.IsNullOrEmpty(_configFilepath))
                {
                    Debug.Log("The file:" + Config + " did not exist.", this);
                }
            }
            if (!string.IsNullOrEmpty(FaceDetectionModel))
            {
                _faceDetectionModelFilepath = await OpenCVForUnityEnv.GetFilePathAsync(FaceDetectionModel, cancellationToken: _cts.Token);
                if (string.IsNullOrEmpty(_faceDetectionModelFilepath))
                {
                    Debug.Log("The file:" + FaceDetectionModel + " did not exist.", this);
                }
            }
            if (!string.IsNullOrEmpty(FaceRecognitionModel))
            {
                _faceRecognitionModelFilepath = await OpenCVForUnityEnv.GetFilePathAsync(FaceRecognitionModel, cancellationToken: _cts.Token);
                if (string.IsNullOrEmpty(_faceRecognitionModelFilepath))
                {
                    Debug.Log("The file:" + FaceRecognitionModel + " did not exist.", this);
                }
            }

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            // Load YuNet and SFace models from StreamingAssets and create the estimator and runner.
            if (!TryInitializeInference())
            {
                return;
            }

            _multiSourceToMatHelper.Initialize();
        }

        private async void OnDestroy()
        {
            Debug.Log("OnDestroy", this);

            UnwireSourceToMatControlPanelHooks();

            _cts?.Cancel();

            await DisposeInferenceAsync();

            // Clear all DebugMat windows on destroy
            DebugMat.destroyAllWindows();

            _cts?.Dispose();
            _cts = null;

            OpenCVDebug.SetDebugMode(false);
        }

        // Public Methods
        /// <summary>
        /// Raises the helper frame mat updated event.
        /// Updates the preview texture when a new frame is available during playback.
        /// </summary>
        public void OnSourceToMatHelperFrameMatUpdated()
        {
            if (!_multiSourceToMatHelper.IsPlaying)
            {
                return;
            }

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;

            if (_faceIdentificationEstimator != null && _inferenceRunner != null)
            {
                // Worker expects BGR; helper delivers RGBA from the camera.
                Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

                // Single-flight runner drops stale frames so only the latest Mat is inferred.
                _inferenceRunner.SubmitWork(
                    _bgrMat,
                    syncWork: m => _faceIdentificationEstimator.Estimate(m, useCopyOutput: true),
                    asyncWork: async m =>
                    {
                        CancellationToken ct = _inferenceRunner.InFlightAsyncWorkCancellationToken;
                        return await _faceIdentificationEstimator.EstimateAsync(m, ct);
                    });

                if (_inferenceRunner.TryGetLatestResult(out Mat faces))
                {
                    _faceIdentificationEstimator.Visualize(rgbaMat, faces, false, true);

                    // Check for point selection completion and register face
                    if (_shouldUpdateFromPoint)
                    {
                        var (gameObject, currentSelectionState, currentSelectionPoints) = PointSelector.GetSelectionStatus();
                        var p = TextureSelector.ConvertSelectionPointsToOpenCVPoint(currentSelectionPoints);
                        RegisterSelectedFace(_bgrMat, faces, p);

                        // Update face recognition for all tracked faces with the new registered face
                        _faceIdentificationEstimator.UpdateFaceRecognitionForAllTrackedFaces(_bgrMat, true);

                        PointSelector.ResetSelectionStatus();
                        _shouldUpdateFromPoint = false;
                    }
                }
            }

            // Draw current selection overlay
            PointSelector.DrawSelection(rgbaMat, true);

            OpenCVMatUnityUtils.MatToTexture2D(rgbaMat, _texture);
        }

        /// <summary>
        /// Raises the helper initialized event.
        /// Recreates the preview texture and starts playback on first initialization.
        /// Skips Play when re-initialization has already restored Playing or Paused.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized", this);

            RecreatePreviewTexture();
            CreateOrRecreateProcessingResources(_multiSourceToMatHelper.FrameMat);

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
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _faceIdentificationEstimator, UseAsyncInference);
                _fpsMonitor.Toast("Touch a detected face to register it.", 2000);
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
        public void OnSourceToMatHelperFrameMatLayoutChanged()
        {
            Debug.Log("OnSourceToMatHelperFrameMatLayoutChanged", this);

            RecreatePreviewTexture();
            CreateOrRecreateProcessingResources(_multiSourceToMatHelper.FrameMat);

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
        public void OnSourceToMatHelperReleased()
        {
            Debug.Log("OnSourceToMatHelperReleased", this);

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

            _faceIdentificationEstimator?.Cancel();

            _inferenceRunner?.Cancel();

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
        /// Raises the use async inference toggle value changed event.
        /// </summary>
        public void OnUseAsyncInferenceToggleValueChanged()
        {
            if (UseAsyncInferenceToggle.isOn != UseAsyncInference)
            {
                if (_inferenceRunner != null)
                {
                    _inferenceRunner.UseAsyncWork = UseAsyncInferenceToggle.isOn;
                }

                UseAsyncInference = UseAsyncInferenceToggle.isOn;
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _faceIdentificationEstimator, UseAsyncInference);
            }
        }

        /// <summary>
        /// Clears all registered faces, resets face recognition for all tracked faces, and clears all DebugMat windows.
        /// </summary>
        public void OnClearFacesButtonClick()
        {
            if (_faceIdentificationEstimator != null)
            {
                _faceIdentificationEstimator.ClearRegisteredFaces();
                Debug.Log("All registered faces cleared.", this);

                _faceIdentificationEstimator.ResetFaceRecognitionForAllTrackedFaces();
                Debug.Log("Face recognition reset for all tracked faces.", this);

                // Clear all DebugMat windows
                DebugMat.destroyAllWindows();
                Debug.Log("All DebugMat windows cleared.", this);
            }
        }

        /// <summary>
        /// Saves all registered aligned face images as PNG files to the scene-specific persistent data directory.
        /// Metadata (faceId, confidence, name) is encoded in each filename; name is Base64-encoded.
        /// </summary>
        public void OnSaveFacesButtonClick()
        {
            if (_faceIdentificationEstimator == null)
            {
                Debug.LogWarning("Face identification estimator is not initialized.", this);
                return;
            }

            int[] faceIds = _faceIdentificationEstimator.GetRegisteredFaceIds();
            string saveDirectoryPath = GetSaveDirectoryPath();

            if (faceIds == null || faceIds.Length == 0)
            {
                if (Directory.Exists(saveDirectoryPath))
                {
                    DeleteFaceImageFiles(saveDirectoryPath);
                    Debug.Log($"Deleted saved face image(s) from: {saveDirectoryPath}", this);
                }
                else
                {
                    Debug.Log("No registered faces to save.", this);
                }

                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("Saved face data cleared.", 2000);
                }

                return;
            }

            if (!Directory.Exists(saveDirectoryPath))
            {
                Directory.CreateDirectory(saveDirectoryPath);
            }

            DeleteFaceImageFiles(saveDirectoryPath);

            using MatOfInt compressionParams = new MatOfInt(Imgcodecs.IMWRITE_PNG_COMPRESSION, 0);
            int savedCount = 0;

            foreach (int faceId in faceIds)
            {
                Mat alignedFace = _faceIdentificationEstimator.GetAlignedFace(faceId);
                if (alignedFace == null || alignedFace.empty())
                {
                    alignedFace?.Dispose();
                    Debug.LogWarning($"Skipped saving face ID {faceId}: aligned face is empty.", this);
                    continue;
                }

                string faceName = _faceIdentificationEstimator.GetFaceName(faceId) ?? $"Face_{faceId}";
                float confidence = _faceIdentificationEstimator.GetFaceDetectionConfidence(faceId);
                string fileName = BuildFaceFileName(faceId, confidence, faceName);
                string savePath = Path.Combine(saveDirectoryPath, fileName);

                if (!Imgcodecs.imwrite(savePath, alignedFace, compressionParams))
                {
                    Debug.LogWarning($"Failed to save face ID {faceId} to: {savePath}", this);
                }
                else
                {
                    savedCount++;
                }

                alignedFace.Dispose();
            }

            Debug.Log($"Saved {savedCount} registered face(s) to: {saveDirectoryPath}", this);
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Toast($"Saved {savedCount} face(s).", 2000);
            }
        }

        /// <summary>
        /// Loads aligned face images from the scene-specific persistent data directory and registers them.
        /// Metadata is decoded from each filename; name is Base64-decoded.
        /// </summary>
        public void OnLoadFacesButtonClick()
        {
            if (_faceIdentificationEstimator == null)
            {
                Debug.LogWarning("Face identification estimator is not initialized.", this);
                return;
            }

            string saveDirectoryPath = GetSaveDirectoryPath();
            if (!Directory.Exists(saveDirectoryPath))
            {
                Debug.LogWarning($"Save directory does not exist: {saveDirectoryPath}", this);
                return;
            }

            string[] faceImagePaths = Directory.GetFiles(saveDirectoryPath, "*." + FACE_IMAGE_FORMAT);
            if (faceImagePaths.Length == 0)
            {
                Debug.LogWarning($"No face image files found in: {saveDirectoryPath}", this);
                return;
            }

            Array.Sort(faceImagePaths, StringComparer.OrdinalIgnoreCase);

            _faceIdentificationEstimator.ClearRegisteredFaces();
            DebugMat.destroyAllWindows();

            int loadedCount = 0;

            foreach (string faceImagePath in faceImagePaths)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(faceImagePath);
                if (!TryParseFaceFileName(fileNameWithoutExtension, out int faceId, out float confidence, out string faceName))
                {
                    Debug.LogWarning($"Skipped invalid face filename: {Path.GetFileName(faceImagePath)}", this);
                    continue;
                }

                Mat alignedFace = Imgcodecs.imread(faceImagePath, Imgcodecs.IMREAD_COLOR);
                if (alignedFace == null || alignedFace.empty())
                {
                    alignedFace?.Dispose();
                    Debug.LogWarning($"Failed to load face image: {faceImagePath}", this);
                    continue;
                }

                try
                {
                    _faceIdentificationEstimator.RegisterFace(alignedFace, faceId, faceName, confidence);
                    DisplayRegisteredFace(faceId);
                    loadedCount++;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to register face from {faceImagePath}: {e.Message}", this);
                }
                finally
                {
                    alignedFace.Dispose();
                }
            }

            if (loadedCount > 0 && _bgrMat != null)
            {
                _faceIdentificationEstimator.ResetFaceRecognitionForAllTrackedFaces();
                _faceIdentificationEstimator.UpdateFaceRecognitionForAllTrackedFaces(_bgrMat, skipExistingFaceIds: false);
            }

            Debug.Log($"Loaded {loadedCount} registered face(s) from: {saveDirectoryPath}", this);
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Toast($"Loaded {loadedCount} face(s).", 2000);
            }
        }

        /// <summary>
        /// Handles the texture selection state changed event from TextureSelector.
        /// This should be wired in the Inspector to TextureSelector.OnTextureSelectionStateChanged.
        /// </summary>
        /// <param name="touchedObject">The GameObject that was touched.</param>
        /// <param name="touchState">The touch state.</param>
        /// <param name="texturePoints">The texture coordinates array (OpenCV format: top-left origin).</param>
        public void OnTextureSelectionStateChanged(GameObject touchedObject, TextureSelector.TextureSelectionState touchState, Vector2[] texturePoints)
        {
            switch (touchState)
            {
                case TextureSelector.TextureSelectionState.POINT_SELECTION_COMPLETED:
                    _shouldUpdateFromPoint = true;
                    break;
            }
        }

        // Private Methods
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

            bool isRgb = _multiSourceToMatHelper.OutputColorFormat == SourceToMatColorFormat.RGB;
            _texture = new Texture2D(frameMat.cols(), frameMat.rows(), isRgb ? TextureFormat.RGB24 : TextureFormat.RGBA32, false);
            OpenCVMatUnityUtils.MatToTexture2D(frameMat, _texture);

            if (ResultPreview != null)
            {
                ResultPreview.texture = _texture;
                AspectRatioFitter aspectRatioFitter = ResultPreview.GetComponent<AspectRatioFitter>();
                if (aspectRatioFitter != null)
                {
                    aspectRatioFitter.aspectRatio = (float)_texture.width / _texture.height;
                }
            }
        }

        private void DisposeFrameProcessingResources()
        {
            _bgrMat?.Dispose();
            _bgrMat = null;
        }

        private void CreateOrRecreateProcessingResources(Mat rgbaMat)
        {
            if (rgbaMat == null)
            {
                return;
            }

            DisposeFrameProcessingResources();

            _bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
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
        /// Creates <see cref="FaceIdentificationEstimator"/> from the resolved StreamingAssets model paths and
        /// <see cref="MatSingleFlightSyncAsyncRunner"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when inference resources were created; otherwise <see langword="false"/>.
        /// </returns>
        private bool TryInitializeInference()
        {
            if (string.IsNullOrEmpty(_faceDetectionModelFilepath) || string.IsNullOrEmpty(_faceRecognitionModelFilepath))
            {
                Debug.LogError("model files are not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader] to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "model files are not loaded.\nPlease read console message.";
                }

                return false;
            }

            try
            {
                _faceIdentificationEstimator = new FaceIdentificationEstimator(_faceDetectionModelFilepath, _faceRecognitionModelFilepath, new Size(InpWidth, InpHeight), ConfThreshold, NmsThreshold, TopK);

                FaceIdentificationEstimator faceIdentificationEstimator = _faceIdentificationEstimator;
                _inferenceRunner = new MatSingleFlightSyncAsyncRunner(
                    useAsyncWork: UseAsyncInference,
                    asyncWorkCancellationToken: _cts.Token,
                    disposeAsyncAfterWorkTask: async () =>
                    {
                        await faceIdentificationEstimator.WaitForCompletionAsync();
                    });

                return _faceIdentificationEstimator != null && _inferenceRunner != null;
            }
            catch (Exception ex)
            {
                Debug.LogError("FaceIdentificationEstimatorExample TryInitializeInference failed: " + ex, this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "Failed to initialize inference.\nPlease read console message.";
                }

                if (_faceIdentificationEstimator != null)
                {
                    _faceIdentificationEstimator.Dispose();
                    _faceIdentificationEstimator = null;
                }

                _inferenceRunner = null;
                return false;
            }
        }

        /// <summary>
        /// Reserved hook for synchronizing <see cref="UseAsyncInference"/> with platform capabilities.
        /// Does not modify <see cref="UseAsyncInference"/> in this example.
        /// </summary>
        private void UpdateUseAsyncInference()
        {
        }

        /// <summary>
        /// Updates the async inference toggle interactability and visible state.
        /// </summary>
        /// <param name="inferenceReinitializing">When <see langword="true"/>, disables the toggle while inference is re-initializing.</param>
        private void UpdateInferenceModeToggles(bool inferenceReinitializing)
        {
            if (inferenceReinitializing)
            {
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
        }

        /// <summary>
        /// Awaits <see cref="MatSingleFlightSyncAsyncRunner.DisposeAsync"/> then disposes the face identification estimator worker.
        /// </summary>
        private async Task DisposeInferenceAsync()
        {
            var runner = _inferenceRunner;
            _inferenceRunner = null;
            if (runner != null)
            {
                await runner.DisposeAsync();
            }

            var faceIdentificationEstimator = _faceIdentificationEstimator;
            _faceIdentificationEstimator = null;
            if (faceIdentificationEstimator != null)
            {
                await faceIdentificationEstimator.DisposeAsync();
            }
        }

        /// <summary>
        /// Returns the persistent data directory path dedicated to this example scene.
        /// </summary>
        private static string GetSaveDirectoryPath()
        {
            return Path.Combine(Application.persistentDataPath, SAVE_DIRECTORY_NAME);
        }

        /// <summary>
        /// Deletes all face image files in the specified directory.
        /// </summary>
        private static void DeleteFaceImageFiles(string saveDirectoryPath)
        {
            string[] existingFiles = Directory.GetFiles(saveDirectoryPath, "*." + FACE_IMAGE_FORMAT);
            foreach (string existingFile in existingFiles)
            {
                File.Delete(existingFile);
            }
        }

        /// <summary>
        /// Builds a face image filename that embeds faceId, confidence, and Base64-encoded name.
        /// Format: {faceId}_{confidence:F3}_{base64Name}.png
        /// </summary>
        private static string BuildFaceFileName(int faceId, float confidence, string faceName)
        {
            string encodedName = EncodeFaceNameForFileName(faceName);
            string confidenceText = confidence.ToString("F3", CultureInfo.InvariantCulture);
            return $"{faceId}_{confidenceText}_{encodedName}.{FACE_IMAGE_FORMAT}";
        }

        /// <summary>
        /// Parses a face image filename and decodes embedded metadata.
        /// </summary>
        private static bool TryParseFaceFileName(string fileNameWithoutExtension, out int faceId, out float confidence, out string faceName)
        {
            faceId = 0;
            confidence = 0f;
            faceName = null;

            if (string.IsNullOrEmpty(fileNameWithoutExtension))
            {
                return false;
            }

            string[] parts = fileNameWithoutExtension.Split(new[] { '_' }, 3);
            if (parts.Length < 3)
            {
                return false;
            }

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out faceId))
            {
                return false;
            }

            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out confidence))
            {
                return false;
            }

            if (!TryDecodeFaceNameFromFileName(parts[2], out faceName))
            {
                return false;
            }

            return !string.IsNullOrEmpty(faceName);
        }

        /// <summary>
        /// Encodes a face name as URL-safe Base64 without padding for use in filenames.
        /// </summary>
        private static string EncodeFaceNameForFileName(string faceName)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(faceName))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        /// <summary>
        /// Decodes a URL-safe Base64 face name from a filename segment.
        /// </summary>
        private static bool TryDecodeFaceNameFromFileName(string encodedName, out string faceName)
        {
            faceName = null;

            if (string.IsNullOrEmpty(encodedName))
            {
                return false;
            }

            try
            {
                string base64 = RestoreBase64Padding(encodedName.Replace('-', '+').Replace('_', '/'));
                faceName = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Restores Base64 padding removed during URL-safe encoding.
        /// </summary>
        private static string RestoreBase64Padding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2:
                    return base64 + "==";
                case 3:
                    return base64 + "=";
                default:
                    return base64;
            }
        }

        /// <summary>
        /// Registers the face that was selected by point selection.
        /// If the selected face already has a registered face ID, it updates the registration only if the current confidence is higher.
        /// If the selected face is new, it creates a new registration with the name from FaceNameInput or a default name.
        /// After registration, it displays the registered face using DebugMat.
        /// </summary>
        /// <param name="image">The input image containing the faces.</param>
        /// <param name="detectedFaces">The detected faces matrix.</param>
        /// <param name="selectedPoint">The selected point coordinates.</param>
        private void RegisterSelectedFace(Mat image, Mat detectedFaces, Point selectedPoint)
        {
            if (_faceIdentificationEstimator == null || detectedFaces == null || detectedFaces.empty())
            {
                Debug.LogWarning("No face detection estimator or no faces detected.", this);
                return;
            }

            if (image == null)
            {
                Debug.LogWarning("Input image is null.", this);
                return;
            }

            // Convert detection results to structured data for efficient access
            Span<FaceIdentificationEstimator.FaceIdentificationData> facesData = _faceIdentificationEstimator.ToStructuredDataAsSpan(detectedFaces);

            // Find the face containing the selected point
            int bestFaceIndex = FindFaceContainingPoint(facesData, selectedPoint);

            if (bestFaceIndex >= 0)
            {
                // Get the selected face data
                ref readonly var selectedFaceData = ref facesData[bestFaceIndex];

                // Check if this face already has a faceId
                int existingFaceId = (int)selectedFaceData.FaceId;
                Debug.Log($"Selected face ID: {existingFaceId}", this);
                float currentConfidence = selectedFaceData.Score;

                // Create a face row for alignment using the conversion method
                Mat faceRow = FaceIdentificationEstimator.ConvertFaceDetectionDataToMat(selectedFaceData.FaceDetection);

                int faceId;
                string faceName;

                if (existingFaceId >= 0)
                {
                    // Face is already recognized - use existing face name
                    faceId = existingFaceId;

                    // Get existing face name
                    string existingFaceName = _faceIdentificationEstimator.GetFaceName(existingFaceId);
                    faceName = existingFaceName ?? $"Face_{existingFaceId}";

                    float existingConfidence = _faceIdentificationEstimator.GetFaceDetectionConfidence(existingFaceId);

                    Debug.Log($"Selected face is already registered with ID: {existingFaceId}, existing confidence: {existingConfidence:F3}, current confidence: {currentConfidence:F3}", this);

                    if (currentConfidence > existingConfidence)
                    {
                        Debug.Log($"Updating face ID {existingFaceId} with higher confidence: {existingConfidence:F3} -> {currentConfidence:F3}", this);
                        _faceIdentificationEstimator.RegisterFaceFromDetection(image, faceRow, faceId, faceName);
                    }
                    else
                    {
                        Debug.Log($"Face ID {existingFaceId} already has higher or equal confidence: {existingConfidence:F3} >= {currentConfidence:F3}, skipping update", this);
                    }
                }
                else
                {
                    // New face registration - generate new face name
                    faceId = _faceIdentificationEstimator.RegisteredFaceCount + 1;

                    if (FaceNameInput != null && !string.IsNullOrEmpty(FaceNameInput.text?.Trim()))
                    {
                        faceName = FaceNameInput.text.Trim();
                    }
                    else
                    {
                        faceName = $"Face_{faceId}";
                    }

                    _faceIdentificationEstimator.RegisterFaceFromDetection(image, faceRow, faceId, faceName);
                    Debug.Log($"Face registered successfully: {faceName} (ID: {faceId})", this);
                }

                faceRow.Dispose();

                // Display the registered face using DebugMat
                DisplayRegisteredFace(faceId);
            }
            else
            {
                Debug.LogWarning("No face found near the selected point.", this);
            }
        }

        /// <summary>
        /// Displays the registered face using DebugMat with annotations including face ID, name, confidence score, and colored border.
        /// </summary>
        /// <param name="faceId">The face ID.</param>
        private void DisplayRegisteredFace(int faceId)
        {
            if (_faceIdentificationEstimator == null)
            {
                return;
            }

            Mat alignedFace = _faceIdentificationEstimator.GetAlignedFace(faceId);
            if (alignedFace == null || alignedFace.empty())
            {
                return;
            }

            // Get face name
            string faceName = _faceIdentificationEstimator.GetFaceName(faceId);
            if (faceName == null)
            {
                faceName = $"Face_{faceId}";
            }

            // Create a copy for drawing text
            Mat displayFace = alignedFace.clone();

            // Get image dimensions for proper text positioning (BGR mat)
            int imgWidth = displayFace.cols();
            int imgHeight = displayFace.rows();

            // Prepare text color and draw border around the entire Mat using it
            Scalar textColor = _faceIdentificationEstimator.GetColorForFaceId(faceId);
            Imgproc.rectangle(displayFace, new Point(0, 0), new Point(imgWidth - 1, imgHeight - 1), textColor, 2);

            // Draw face ID and name on the image
            string displayText = $"FaceId: {faceId} ({faceName})";

            // Calculate font scale to fit text within image width
            double fontScale = 0.5;
            int thickness = 1;

            // Get text size to check if it fits
            Size textSize = Imgproc.getTextSize(displayText, Imgproc.FONT_HERSHEY_SIMPLEX, fontScale, thickness, null);

            // Adjust font scale if text is too wide
            if (textSize.width > imgWidth - 10)
            {
                fontScale = (imgWidth - 10) / (double)textSize.width * fontScale;
            }

            // Draw label inside a filled rectangle attached to top-left of the Mat
            int[] baseLineTop = new int[1];
            var labelSizeTop = Imgproc.getTextSizeAsValueTuple(displayText, Imgproc.FONT_HERSHEY_SIMPLEX, fontScale, thickness, baseLineTop);
            double rectLeftTop = 0d;
            double rectTopTop = 0d;
            Imgproc.rectangle(displayFace,
                new Point(rectLeftTop, rectTopTop),
                new Point(rectLeftTop + labelSizeTop.width, rectTopTop + labelSizeTop.height + baseLineTop[0]),
                textColor, Core.FILLED);
            Imgproc.putText(displayFace, displayText, new Point(rectLeftTop, rectTopTop + labelSizeTop.height), Imgproc.FONT_HERSHEY_SIMPLEX, fontScale, new Scalar(255, 255, 255, 255), thickness, Imgproc.LINE_AA, false);

            // Draw confidence score at bottom-left
            float confidence = _faceIdentificationEstimator.GetFaceDetectionConfidence(faceId);
            string confidenceText = $"Confidence: {confidence:F3}";
            Scalar confidenceColor = _faceIdentificationEstimator.GetColorForFaceId(faceId);

            // Calculate font scale for confidence text
            double confidenceFontScale = 0.4;
            int confidenceThickness = 1;

            Size confidenceTextSize = Imgproc.getTextSize(confidenceText, Imgproc.FONT_HERSHEY_SIMPLEX, confidenceFontScale, confidenceThickness, null);

            // Adjust font scale if confidence text is too wide
            if (confidenceTextSize.width > imgWidth - 10)
            {
                confidenceFontScale = (imgWidth - 10) / (double)confidenceTextSize.width * confidenceFontScale;
            }

            // Draw confidence inside a filled rectangle attached to bottom-right of the Mat
            int[] baseLineBottom = new int[1];
            var labelSizeBottom = Imgproc.getTextSizeAsValueTuple(confidenceText, Imgproc.FONT_HERSHEY_SIMPLEX, confidenceFontScale, confidenceThickness, baseLineBottom);
            double rectRight = imgWidth;
            double rectBottom = imgHeight;
            double rectLeftBottom = rectRight - labelSizeBottom.width;
            double rectTopBottom = rectBottom - (labelSizeBottom.height + baseLineBottom[0]);
            Imgproc.rectangle(displayFace,
                new Point(rectLeftBottom, rectTopBottom),
                new Point(rectRight, rectBottom),
                confidenceColor, Core.FILLED);
            Imgproc.putText(displayFace, confidenceText, new Point(rectLeftBottom, rectBottom - baseLineBottom[0]), Imgproc.FONT_HERSHEY_SIMPLEX, confidenceFontScale, new Scalar(255, 255, 255, 255), confidenceThickness, Imgproc.LINE_AA, false);
            // Convert BGR to RGB for proper display just before imshow
            Mat rgbFace = new Mat();
            Imgproc.cvtColor(displayFace, rgbFace, Imgproc.COLOR_BGR2RGB);
            DebugMat.imshow($"FaceId: {faceId} ({faceName})", rgbFace, false, null, $"FaceId: {faceId} Name: {faceName} Confidence: {confidence:F3}");

            displayFace.Dispose();
            rgbFace.Dispose();
            alignedFace.Dispose();
        }

        /// <summary>
        /// Finds the face that contains the selected point within its bounding box.
        /// </summary>
        /// <param name="facesData">The detected faces structured data.</param>
        /// <param name="selectedPoint">The selected point.</param>
        /// <returns>The index of the face containing the point, or -1 if no face contains the point.</returns>
        private int FindFaceContainingPoint(Span<FaceIdentificationEstimator.FaceIdentificationData> facesData, Point selectedPoint)
        {
            if (facesData == null || facesData.Length == 0)
            {
                return -1;
            }

            for (int i = 0; i < facesData.Length; i++)
            {
                ref readonly var faceData = ref facesData[i];

                // Extract bounding box coordinates (x, y, width, height)
                float x = faceData.X;
                float y = faceData.Y;
                float width = faceData.Width;
                float height = faceData.Height;

                // Check if the selected point is within the face bounding box
                if (selectedPoint.x >= x && selectedPoint.x <= x + width &&
                    selectedPoint.y >= y && selectedPoint.y <= y + height)
                {
                    return i; // Return the first face that contains the point
                }
            }

            return -1; // No face contains the selected point
        }

        /// <summary>
        /// Updates <paramref name="fpsMonitor"/> with dnn backend, target, and async mode from
        /// <paramref name="faceIdentificationEstimator"/> and <paramref name="useAsyncInference"/> (or "-" when a value is not available).
        /// </summary>
        private static void UpdateFpsMonitorInferenceInfo(FpsMonitor fpsMonitor, FaceIdentificationEstimator faceIdentificationEstimator, bool useAsyncInference)
        {
            if (fpsMonitor == null)
            {
                return;
            }

            if (faceIdentificationEstimator != null)
            {
                fpsMonitor.Add("dnnBackend", OpenCVDnnInferenceNet.GetBackendDisplayString(faceIdentificationEstimator.DnnBackend));
                fpsMonitor.Add("dnnTarget", OpenCVDnnInferenceNet.GetTargetDisplayString(faceIdentificationEstimator.DnnTarget));
            }
            else
            {
                fpsMonitor.Add("dnnBackend", "-");
                fpsMonitor.Add("dnnTarget", "-");
            }
            fpsMonitor.Add("useAsyncInference", useAsyncInference.ToString());
        }
    }
}
#endif
