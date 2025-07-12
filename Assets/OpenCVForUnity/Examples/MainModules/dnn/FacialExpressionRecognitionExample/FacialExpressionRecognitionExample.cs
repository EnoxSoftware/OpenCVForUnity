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
    /// Facial Expression Recognition Example
    /// An example of using OpenCV dnn module with Facial Expression Recognition.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/master/models/facial_expression_recognition
    ///
    /// [Tested Models]
    /// facial_expression_recognition_mobilefacenet_2022july.onnx https://github.com/opencv/opencv_zoo/blob/main/models/facial_expression_recognition/facial_expression_recognition_mobilefacenet_2022july.onnx
    /// face_recognition_sface_2021dec.onnx https://github.com/opencv/opencv_zoo/blob/main/models/face_recognition/face_recognition_sface_2021dec.onnx
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class FacialExpressionRecognitionExample : MonoBehaviour
    {
        // Constants
        private static readonly string FACIAL_EXPRESSION_RECOGNITION_MODEL_FILENAME = "OpenCVForUnityExamples/dnn/facial_expression_recognition_mobilefacenet_2022july.onnx";
        private static readonly string FACE_DETECTION_MODEL_FILENAME = "OpenCVForUnityExamples/dnn/face_detection_yunet_2023mar.onnx";

        // Public Fields
        [Header("Output")]
        [Tooltip("The RawImage for previewing the result.")]
        public RawImage ResultPreview;

        [Header("UI")]
        public Toggle UseAsyncInferenceToggle;
        public bool UseAsyncInference = false;

        // Private Fields
        private Texture2D _texture;
        private MultiSource2MatHelper _multiSource2MatHelper;
        private Mat _bgrMat;

        private FacialExpressionRecognizer _facialExpressionRecognizer;
        private string _facialExpressionRecognitionModelFilepath;

        private YuNetV2FaceDetector _faceDetector;
        private int _inputSizeW = 320;
        private int _inputSizeH = 320;
        private float _scoreThreshold = 0.6f;
        private float _nmsThreshold = 0.3f;
        private int _topK = 100;
        private string _faceDetectionModelFilepath;

        private FpsMonitor _fpsMonitor;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        private Mat _bgrMatForAsync;
        private Mat _latestDetectedFaces;
        private Mat _latestRecognizedFacialExpressions;
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

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _faceDetectionModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(FACE_DETECTION_MODEL_FILENAME, cancellationToken: _cts.Token);
            _facialExpressionRecognitionModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(FACIAL_EXPRESSION_RECOGNITION_MODEL_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);


            if (string.IsNullOrEmpty(_faceDetectionModelFilepath))
            {
                Debug.LogError(FACE_DETECTION_MODEL_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
            }
            else
            {
                _faceDetector = new YuNetV2FaceDetector(_faceDetectionModelFilepath, "", new Size(_inputSizeW, _inputSizeH), _scoreThreshold, _nmsThreshold, _topK);
            }

            if (string.IsNullOrEmpty(_facialExpressionRecognitionModelFilepath))
            {
                Debug.LogError(FACIAL_EXPRESSION_RECOGNITION_MODEL_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
            }
            else
            {
                _facialExpressionRecognizer = new FacialExpressionRecognizer(_facialExpressionRecognitionModelFilepath);
            }

            _multiSource2MatHelper.Initialize();
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
            _latestRecognizedFacialExpressions?.Dispose(); _latestRecognizedFacialExpressions = null;

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

        // Update is called once per frame
        private void Update()
        {
            ProcessMainThreadQueue();

            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_faceDetector == null || _facialExpressionRecognizer == null)
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
                                    // Face detector inference
                                    var newFaces = await _faceDetector.DetectAsync(_bgrMatForAsync);
                                    // Facial expression recognizer inference
                                    var newFacialExpressions = await _facialExpressionRecognizer.RecognizeAsync(_bgrMatForAsync, newFaces);
                                    RunOnMainThread(() =>
                                    {
                                        _latestDetectedFaces?.Dispose();
                                        _latestDetectedFaces = newFaces;
                                        _latestRecognizedFacialExpressions?.Dispose();
                                        _latestRecognizedFacialExpressions = newFacialExpressions;
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

                        if (_latestDetectedFaces != null && _latestRecognizedFacialExpressions != null)
                        {
                            _facialExpressionRecognizer.Visualize(rgbaMat, _latestRecognizedFacialExpressions, _latestDetectedFaces, false, true);
                        }
                    }
                    else
                    {
                        // synchronous execution

                        // TickMeter tm = new TickMeter();
                        // tm.start();

                        // Face detector inference
                        using (Mat faces = _faceDetector.Detect(_bgrMat))
                        {
                            // tm.stop();
                            // Debug.Log("YuNetV2FaceDetector Inference time, ms: " + tm.getTimeMilli());

                            //tm.reset();
                            //tm.start();

                            // Facial expression recognizer inference
                            using (Mat facialExpressions = _facialExpressionRecognizer.Recognize(_bgrMat, faces))
                            {
                                //tm.stop();
                                //Debug.Log("FacialExpressionRecognizer Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                                Imgproc.cvtColor(_bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                                _facialExpressionRecognizer.Visualize(rgbaMat, facialExpressions, faces, false, true);
                            }
                        }
                    }
                }

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }

        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            _faceDetector?.Dispose();
            _facialExpressionRecognizer?.Dispose();

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
    }
}

#endif
