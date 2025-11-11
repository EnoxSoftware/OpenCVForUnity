#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.UnityIntegration.Worker.DnnModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        public bool UseAsyncInference = false;

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
        private Mat _bgrMatForAsync;
        private Mat _latestDetectedFaces;
        private Task _inferenceTask;
        private readonly Queue<Action> _mainThreadQueue = new();
        private readonly object _queueLock = new();
        private bool _shouldUpdateFromPoint = false;

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


            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("width", rgbaMat.width().ToString());
                _fpsMonitor.Add("height", rgbaMat.height().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
                _fpsMonitor.Toast("Touch a detected face to register it.", 2000);
            }

            _bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
            _bgrMatForAsync = new Mat();
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
            _latestDetectedFaces?.Dispose(); _latestDetectedFaces = null;

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
            ProcessMainThreadQueue();

            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_faceIdentificationEstimator == null)
                {
                    Imgproc.putText(rgbaMat, "model files are not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
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
                                    // Face identification inference
                                    var newFaces = await _faceIdentificationEstimator.EstimateAsync(_bgrMatForAsync);
                                    RunOnMainThread(() =>
                                        {
                                            _latestDetectedFaces?.Dispose();
                                            _latestDetectedFaces = newFaces;
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

                        if (_latestDetectedFaces != null)
                        {
                            _faceIdentificationEstimator.Visualize(rgbaMat, _latestDetectedFaces, false, true);

                            // Check for point selection completion and register face
                            if (_shouldUpdateFromPoint)
                            {
                                var (gameObject, currentSelectionState, currentSelectionPoints) = PointSelector.GetSelectionStatus();
                                var p = TextureSelector.ConvertSelectionPointsToOpenCVPoint(currentSelectionPoints);
                                RegisterSelectedFace(_bgrMat, _latestDetectedFaces, p);

                                // Update face recognition for all tracked faces with the new registered face
                                _faceIdentificationEstimator.UpdateFaceRecognitionForAllTrackedFaces(_bgrMat, true);

                                PointSelector.ResetSelectionStatus();
                                _shouldUpdateFromPoint = false;
                            }
                        }
                    }
                    else
                    {
                        // synchronous execution

                        // TickMeter tm = new TickMeter();
                        // tm.start();

                        // Face identification inference
                        using (Mat faces = _faceIdentificationEstimator.Estimate(_bgrMat))
                        {
                            // tm.stop();
                            // Debug.Log("FaceIdentificationEstimator Inference time, ms: " + tm.getTimeMilli());

                            Imgproc.cvtColor(_bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

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
                }

                // Draw current selection overlay
                PointSelector.DrawSelection(rgbaMat, true);

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            _faceIdentificationEstimator?.Dispose();

            // Clear all DebugMat windows on destroy
            DebugMat.destroyAllWindows();

            OpenCVDebug.SetDebugMode(false);

            _cts?.Dispose();
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
        /// Waits for any ongoing inference to complete before changing the toggle state.
        /// </summary>
        public void OnUseAsyncInferenceToggleValueChanged()
        {
            if (UseAsyncInferenceToggle.isOn != UseAsyncInference)
            {
                // Wait for inference to complete before changing the toggle
                if (_inferenceTask != null && !_inferenceTask.IsCompleted) _inferenceTask.Wait(500);

                UseAsyncInference = UseAsyncInferenceToggle.isOn;
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
            }
            else
            {
                _faceIdentificationEstimator = new FaceIdentificationEstimator(_faceDetectionModelFilepath, _faceRecognitionModelFilepath, new Size(InpWidth, InpHeight), ConfThreshold, NmsThreshold, TopK);
            }

            _multiSource2MatHelper.Initialize();
        }

        /// <summary>
        /// Enqueues an action to be executed on the main thread.
        /// </summary>
        /// <param name="action">The action to execute on the main thread.</param>
        private void RunOnMainThread(Action action)
        {
            if (action == null) return;

            lock (_queueLock)
            {
                _mainThreadQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// Processes all actions queued for execution on the main thread.
        /// </summary>
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
    }
}

#endif
