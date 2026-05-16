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
        public bool UseAsyncInference = true;

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
            UpdateUseAsyncInference();
            UpdateInferenceModeToggles(inferenceReinitializing: false);

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

            InitializeInference();

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
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _faceDetector, UseAsyncInference);
            }

            _bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            try
            {
                _faceDetector?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            try
            {
                _facialExpressionRecognizer?.Cancel();
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

        // Update is called once per frame
        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_faceDetector != null && _facialExpressionRecognizer != null)
                {
                    if (_inferenceRunner != null)
                    {
                        Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

                        _inferenceRunner.SubmitWork(
                            _bgrMat,
                            syncWork: m =>
                            {
                                Mat faces = _faceDetector.Detect(m, useCopyOutput: true);
                                Mat expressions = _facialExpressionRecognizer.Recognize(m, faces, useCopyOutput: true);
                                return new Mat[] { faces, expressions };
                            },
                            asyncWork: async m =>
                            {
                                CancellationToken ct = _inferenceRunner.InFlightAsyncWorkCancellationToken;
                                Mat faces = await _faceDetector.DetectTaskAsync(m, ct);
                                Mat expressions = await _facialExpressionRecognizer.RecognizeTaskAsync(m, faces, ct);
                                return new Mat[] { faces, expressions };
                            });

                        if (_inferenceRunner.TryGetLatestResult(out Mat[] latest) && latest.Length >= 2)
                        {
                            _facialExpressionRecognizer.Visualize(rgbaMat, latest[1], latest[0], false, true);
                        }
                    }
                }

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }

        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        private async void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();
            _multiSource2MatHelper = null;

            _cts?.Cancel();

            await DisposeInferenceAsync();

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
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _faceDetector, UseAsyncInference);
            }
        }

        // Private Methods
        /// <summary>
        /// Creates <see cref="YuNetV2FaceDetector"/>, <see cref="FacialExpressionRecognizer"/>, and
        /// <see cref="MatSingleFlightSyncAsyncRunner"/> from resolved StreamingAssets paths (same role as
        /// <c>MediaPipeHandLandmarkerExample.InitializeInference</c> / <c>FaceDetectionYuNetV2Example.InitializeInference</c>).
        /// </summary>
        private void InitializeInference()
        {
            if (string.IsNullOrEmpty(_faceDetectionModelFilepath))
            {
                Debug.LogError(FACE_DETECTION_MODEL_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("model file is not loaded.\nPlease read console message.", 20000);
                }
            }
            if (string.IsNullOrEmpty(_facialExpressionRecognitionModelFilepath))
            {
                Debug.LogError(FACIAL_EXPRESSION_RECOGNITION_MODEL_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("model file is not loaded.\nPlease read console message.", 20000);
                }
            }
            if (string.IsNullOrEmpty(_faceDetectionModelFilepath) || string.IsNullOrEmpty(_facialExpressionRecognitionModelFilepath))
                return;

            try
            {
                _faceDetector = new YuNetV2FaceDetector(_faceDetectionModelFilepath, "", new Size(_inputSizeW, _inputSizeH), _scoreThreshold, _nmsThreshold, _topK);
                _facialExpressionRecognizer = new FacialExpressionRecognizer(_facialExpressionRecognitionModelFilepath);

                _inferenceRunner = new MatSingleFlightSyncAsyncRunner(
                    useAsyncWork: UseAsyncInference,
                    asyncWorkCancellationToken: _cts.Token,
                    disposeAsyncAfterWorkTask: async () =>
                    {
                        await _faceDetector.WaitForCompletionTaskAsync();
                        await _facialExpressionRecognizer.WaitForCompletionTaskAsync();
                    });
            }
            catch (Exception ex)
            {
                Debug.LogWarning("FacialExpressionRecognitionExample InitializeInference failed: " + ex);
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
        /// Awaits <see cref="MatSingleFlightSyncAsyncRunner.DisposeAsync"/> then disposes face detector and expression recognizer workers.
        /// </summary>
        private async Task DisposeInferenceAsync()
        {
            if (_inferenceRunner != null)
                await _inferenceRunner.DisposeAsync();
            _inferenceRunner = null;

            _faceDetector?.Dispose();
            _faceDetector = null;
            _facialExpressionRecognizer?.Dispose();
            _facialExpressionRecognizer = null;
        }

        /// <summary>
        /// Registers FpsMonitor keys for dnn backend, target, and async inference display.
        /// Uses the same key names as <see cref="ImageClassificationPPResnetExample"/> <c>UpdateFpsMonitorInferenceInfo</c>.
        /// Fps backend/target values come only from <see cref="YuNetV2FaceDetector"/> (<c>FaceDetectorYN</c>), matching <see cref="FaceDetectionYuNetV2Example"/>.
        /// </summary>
        private static void UpdateFpsMonitorInferenceInfo(FpsMonitor fpsMonitor, YuNetV2FaceDetector faceDetector, bool useAsyncInference)
        {
            if (fpsMonitor == null)
                return;

            if (faceDetector != null)
            {
                fpsMonitor.Add("dnnBackend", MultiBackendDnn.GetBackendDisplayString(faceDetector.DnnBackend));
                fpsMonitor.Add("dnnTarget", MultiBackendDnn.GetTargetDisplayString(faceDetector.DnnTarget));
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
