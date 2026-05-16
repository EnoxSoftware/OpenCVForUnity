#if !UNITY_WSA_10_0 && NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE

using System;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.UnityIntegration.Runner;
using OpenCVForUnity.UnityIntegration.Worker.DnnModule;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static OpenCVForUnity.UnityIntegration.Helper.Source2Mat.MultiSource2MatHelper;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Face Identification Estimator Example
    /// An example of using OpenCV dnn module with Face Detection and Recognition.
    /// This example demonstrates face detection, face registration, and face identification.
    ///
    /// [Tested Models]
    /// Face Detection: face_detection_yunet_2023mar.onnx https://github.com/opencv/opencv_zoo/blob/main/models/face_detection_yunet/face_detection_yunet_2023mar.onnx
    /// Face Recognition: face_recognition_sface_2021dec.onnx https://github.com/opencv/opencv_zoo/blob/main/models/face_recognition_sface/face_recognition_sface_2021dec.onnx
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
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

        [Header("Point Selection")]
        [Tooltip("TextureSelector for point selection on the result preview.")]
        public TextureSelector PointSelector;

        // Private Fields
        private Texture2D _texture;
        private MultiSource2MatHelper _multiSource2MatHelper;
        private Mat _bgrMat;
        private FaceIdentificationEstimator _faceIdentificationEstimator;
        private string _configFilepath;
        private string _faceDetectionModelFilepath;
        private string _faceRecognitionModelFilepath;
        private FpsMonitor _fpsMonitor;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private MatSingleFlightSyncAsyncRunner _inferenceRunner;
        private bool _shouldUpdateFromPoint = false;

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
            UpdateUseAsyncInference();
            UpdateInferenceModeToggles(inferenceReinitializing: false);

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            if (!string.IsNullOrEmpty(Config))
            {
                _configFilepath = await OpenCVEnv.GetFilePathTaskAsync(Config, cancellationToken: _cts.Token);
                if (string.IsNullOrEmpty(_configFilepath)) Debug.Log("The file:" + Config + " did not exist.");
            }
            if (!string.IsNullOrEmpty(FaceDetectionModel))
            {
                _faceDetectionModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(FaceDetectionModel, cancellationToken: _cts.Token);
                if (string.IsNullOrEmpty(_faceDetectionModelFilepath)) Debug.Log("The file:" + FaceDetectionModel + " did not exist.");
            }
            if (!string.IsNullOrEmpty(FaceRecognitionModel))
            {
                _faceRecognitionModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(FaceRecognitionModel, cancellationToken: _cts.Token);
                if (string.IsNullOrEmpty(_faceRecognitionModelFilepath)) Debug.Log("The file:" + FaceRecognitionModel + " did not exist.");
            }

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = _multiSource2MatHelper.GetMat();

            _texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;

            UpdateUseAsyncInference();
            UpdateInferenceModeToggles(inferenceReinitializing: false);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("width", rgbaMat.width().ToString());
                _fpsMonitor.Add("height", rgbaMat.height().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _faceIdentificationEstimator, UseAsyncInference);
                _fpsMonitor.Toast("Touch a detected face to register it.", 2000);
            }

            _bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);

            if (_faceIdentificationEstimator != null)
            {
                _inferenceRunner = new MatSingleFlightSyncAsyncRunner(
                    useAsyncWork: UseAsyncInference,
                    asyncWorkCancellationToken: _cts.Token,
                    disposeAsyncAfterWorkTask: async () =>
                    {
                        await _faceIdentificationEstimator.WaitForCompletionTaskAsync();
                    });
            }
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            try
            {
                _faceIdentificationEstimator?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            _inferenceRunner?.Cancel();

            _bgrMat?.Dispose(); _bgrMat = null;

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
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

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_faceIdentificationEstimator != null && _inferenceRunner != null)
                {
                    Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

                    _inferenceRunner.SubmitWork(
                        _bgrMat,
                        syncWork: m => _faceIdentificationEstimator.Estimate(m, useCopyOutput: true),
                        asyncWork: async m =>
                        {
                            CancellationToken ct = _inferenceRunner.InFlightAsyncWorkCancellationToken;
                            return await _faceIdentificationEstimator.EstimateTaskAsync(m, ct);
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

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private async void OnDestroy()
        {
            Debug.Log("OnDestroy");

            _multiSource2MatHelper?.Dispose();
            _multiSource2MatHelper = null;

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
        /// Raises the use async inference toggle value changed event.
        /// </summary>
        public void OnUseAsyncInferenceToggleValueChanged()
        {
            if (UseAsyncInferenceToggle.isOn != UseAsyncInference)
            {
                if (_inferenceRunner != null)
                    _inferenceRunner.UseAsyncWork = UseAsyncInferenceToggle.isOn;
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
                Debug.Log("All registered faces cleared.");

                _faceIdentificationEstimator.ResetFaceRecognitionForAllTrackedFaces();
                Debug.Log("Face recognition reset for all tracked faces.");

                // Clear all DebugMat windows
                DebugMat.destroyAllWindows();
                Debug.Log("All DebugMat windows cleared.");
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
                    UseAsyncInferenceToggle.interactable = false;
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
            if (_inferenceRunner != null)
                await _inferenceRunner.DisposeAsync();
            _inferenceRunner = null;

            _faceIdentificationEstimator?.Dispose();
            _faceIdentificationEstimator = null;
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
                Debug.LogWarning("No face detection estimator or no faces detected.");
                return;
            }

            if (image == null)
            {
                Debug.LogWarning("Input image is null.");
                return;
            }

            // Convert detection results to structured data for efficient access
#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            Span<FaceIdentificationEstimator.FaceIdentificationData> facesData = _faceIdentificationEstimator.ToStructuredDataAsSpan(detectedFaces);
#else
            FaceIdentificationEstimator.FaceIdentificationData[] facesData = _faceIdentificationEstimator.ToStructuredData(detectedFaces);
#endif

            // Find the face containing the selected point
            int bestFaceIndex = FindFaceContainingPoint(facesData, selectedPoint);

            if (bestFaceIndex >= 0)
            {
                // Get the selected face data
#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
                ref readonly var selectedFaceData = ref facesData[bestFaceIndex];
#else
                var selectedFaceData = facesData[bestFaceIndex];
#endif

                // Check if this face already has a faceId
                int existingFaceId = (int)selectedFaceData.FaceId;
                Debug.Log($"Selected face ID: {existingFaceId}");
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

                    Debug.Log($"Selected face is already registered with ID: {existingFaceId}, existing confidence: {existingConfidence:F3}, current confidence: {currentConfidence:F3}");

                    if (currentConfidence > existingConfidence)
                    {
                        Debug.Log($"Updating face ID {existingFaceId} with higher confidence: {existingConfidence:F3} -> {currentConfidence:F3}");
                        _faceIdentificationEstimator.RegisterFaceFromDetection(image, faceRow, faceId, faceName);
                    }
                    else
                    {
                        Debug.Log($"Face ID {existingFaceId} already has higher or equal confidence: {existingConfidence:F3} >= {currentConfidence:F3}, skipping update");
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
                    Debug.Log($"Face registered successfully: {faceName} (ID: {faceId})");
                }

                faceRow.Dispose();

                // Display the registered face using DebugMat
                DisplayRegisteredFace(faceId);
            }
            else
            {
                Debug.LogWarning("No face found near the selected point.");
            }
        }

        /// <summary>
        /// Displays the registered face using DebugMat with annotations including face ID, name, confidence score, and colored border.
        /// </summary>
        /// <param name="faceId">The face ID.</param>
        private void DisplayRegisteredFace(int faceId)
        {
            if (_faceIdentificationEstimator == null)
                return;

            Mat alignedFace = _faceIdentificationEstimator.GetAlignedFace(faceId);
            if (alignedFace == null || alignedFace.empty())
                return;

            // Get face name
            string faceName = _faceIdentificationEstimator.GetFaceName(faceId);
            if (faceName == null)
                faceName = $"Face_{faceId}";

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
#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
        private int FindFaceContainingPoint(Span<FaceIdentificationEstimator.FaceIdentificationData> facesData, Point selectedPoint)
#else
        private int FindFaceContainingPoint(FaceIdentificationEstimator.FaceIdentificationData[] facesData, Point selectedPoint)
#endif
        {
            if (facesData == null || facesData.Length == 0)
                return -1;

            for (int i = 0; i < facesData.Length; i++)
            {
#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
                ref readonly var faceData = ref facesData[i];
#else
                var faceData = facesData[i];
#endif

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
        /// Initializes the face identification estimator with model files and starts the multi-source to mat helper.
        /// </summary>
        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);


            if (string.IsNullOrEmpty(_faceDetectionModelFilepath) || string.IsNullOrEmpty(_faceRecognitionModelFilepath))
            {
                Debug.LogError("model files are not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader] to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
                if (_fpsMonitor != null)
                    _fpsMonitor.Toast("model files are not loaded.\nPlease read console message.", 20000);
            }
            else
            {
                _faceIdentificationEstimator = new FaceIdentificationEstimator(_faceDetectionModelFilepath, _faceRecognitionModelFilepath, new Size(InpWidth, InpHeight), ConfThreshold, NmsThreshold, TopK);
            }

            _multiSource2MatHelper.Initialize();
        }

        /// <summary>
        /// Registers FpsMonitor keys for dnn backend, target, and async inference display.
        /// Uses the same key names as <see cref="ImageClassificationPPResnetExample"/> <c>UpdateFpsMonitorInferenceInfo</c>.
        /// </summary>
        private static void UpdateFpsMonitorInferenceInfo(FpsMonitor fpsMonitor, FaceIdentificationEstimator faceIdentificationEstimator, bool useAsyncInference)
        {
            if (fpsMonitor == null)
                return;

            if (faceIdentificationEstimator != null)
            {
                fpsMonitor.Add("dnnBackend", MultiBackendDnn.GetBackendDisplayString(faceIdentificationEstimator.DnnBackend));
                fpsMonitor.Add("dnnTarget", MultiBackendDnn.GetTargetDisplayString(faceIdentificationEstimator.DnnTarget));
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
