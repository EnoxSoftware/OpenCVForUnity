#if !UNITY_WSA_10_0 && NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
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
    /// Image Classification MobileNet Example
    /// An example of using OpenCV dnn module with Image Classification MobileNet model.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/master/models/image_classification_mobilenet
    ///
    /// [Tested Models]
    /// https://github.com/opencv/opencv_zoo/raw/6c59fd8aaedf1728138e3c982f3351bf9ee3013a/models/image_classification_mobilenet/image_classification_mobilenetv2_2022apr.onnx
    /// https://raw.githubusercontent.com/opencv/opencv_zoo/326e15b31a70812eb6d616406d6e6a17ceaddb6f/models/image_classification_ppresnet/imagenet_labels.txt
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class ImageClassificationMobilenetExample : MonoBehaviour
    {
        // Constants
        protected static readonly string MODEL_FILENAME = "OpenCVForUnityExamples/dnn/image_classification_mobilenetv2_2022apr.onnx";
        protected static readonly string CLASSES_FILENAME = "OpenCVForUnityExamples/dnn/imagenet_labels.txt";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Header("UI")]
        [Tooltip("When on, in-flight single-task async path runs OpenCV DNN on a background thread (Task.Run) via MatSingleFlightSyncAsyncRunner.")]
        public Toggle UseAsyncInferenceToggle;
        public bool UseAsyncInference = true;

        [Space(10)]

        // Private Fields
        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        private Mat _rgbMat;

        /// <summary>
        /// The net.
        /// </summary>
        private Net _net;

        private Size _inputSize = new Size(224, 224);
        private Scalar _mean = new Scalar(0.485, 0.456, 0.406);
        private Scalar _std = new Scalar(0.229, 0.224, 0.225);

        /// <summary>
        /// The classes.
        /// </summary>
        private List<string> _classes;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// The model filepath.
        /// </summary>
        private string _modelFilepath;

        /// <summary>
        /// The classes filepath.
        /// </summary>
        private string _classesFilepath;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
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

            UpdateUseAsyncInference();
            UpdateInferenceModeToggles(inferenceReinitializing: false);

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _modelFilepath = await OpenCVEnv.GetFilePathTaskAsync(MODEL_FILENAME, cancellationToken: _cts.Token);
            _classesFilepath = await OpenCVEnv.GetFilePathTaskAsync(CLASSES_FILENAME, cancellationToken: _cts.Token);

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

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_net != null && _classes != null)
                {
                    if (_inferenceRunner != null)
                    {
                        Imgproc.cvtColor(rgbaMat, _rgbMat, Imgproc.COLOR_RGBA2RGB);

                        _inferenceRunner.SubmitWork(
                            _rgbMat,
                            syncWork: Infer,
                            asyncWork: async m =>
                            {
                                CancellationToken ct = _inferenceRunner.InFlightAsyncWorkCancellationToken;
                                return await InferAsync(m, ct);
                            });

                        if (_inferenceRunner.TryGetLatestResult(out Mat prob1x1))
                        {
                            Core.MinMaxLocResult minmax = Core.minMaxLoc(prob1x1);
                            //Debug.Log ("Best match " + (int)minmax.maxLoc.x);
                            //Debug.Log ("Best match class " + classes [(int)minmax.maxLoc.x]);
                            //Debug.Log ("Probability: " + minmax.maxVal * 100 + "%");

                            //Imgproc.putText (rgbaMat, "Best match class " + classes [(int)minmax.maxLoc.x], new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                            if (_fpsMonitor != null)
                            {
                                _fpsMonitor.ConsoleText = "Best match class " + _classes[(int)minmax.maxLoc.x];
                            }
                        }
                    }
                }

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

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
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _net, UseAsyncInference);
            }

            _rgbMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _inferenceRunner?.Cancel();

            _rgbMat?.Dispose();

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
            if (UseAsyncInferenceToggle != null && UseAsyncInferenceToggle.isOn != UseAsyncInference)
            {
                if (_inferenceRunner != null)
                    _inferenceRunner.UseAsyncWork = UseAsyncInferenceToggle.isOn;
                UseAsyncInference = UseAsyncInferenceToggle.isOn;
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _net, UseAsyncInference);
            }
        }

        // Private Methods
        /// <summary>
        /// Loads class labels, OpenCV <see cref="Net"/>, and <see cref="MatSingleFlightSyncAsyncRunner"/>
        /// (same role as <c>MediaPipeHandLandmarkerExample.InitializeInference</c> / <c>FaceDetectionYuNetV2Example.InitializeInference</c>).
        /// </summary>
        private void InitializeInference()
        {
            _classes = ReadClassNames(_classesFilepath);
            if (_classes == null)
            {
                Debug.LogError(CLASSES_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("classes file is not loaded.\nPlease read console message.", 20000);
                }
            }

            if (string.IsNullOrEmpty(_modelFilepath))
            {
                Debug.LogError(MODEL_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("model file is not loaded.\nPlease read console message.", 20000);
                }
                return;
            }

            try
            {
                _net = Dnn.readNet(_modelFilepath);
                _inferenceRunner = new MatSingleFlightSyncAsyncRunner(
                    useAsyncWork: UseAsyncInference,
                    asyncWorkCancellationToken: _cts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("ImageClassificationMobilenetExample InitializeInference failed: " + ex);
            }
        }

        /// <summary>
        /// Synchronous OpenCV DNN forward: preprocess <paramref name="rgbInput"/> and return
        /// <c>prob.reshape(1, 1).clone()</c>.
        /// </summary>
        private Mat Infer(Mat rgbInput)
        {
            Mat blob = Dnn.blobFromImage(rgbInput, 1.0 / 255.0, _inputSize, Scalar.all(0), false, true, CvType.CV_32F); // HWC to NCHW, RGB

            int c = 3;
            int h = (int)_inputSize.height;
            int w = (int)_inputSize.width;

            using (Mat blob_CxHxW = blob.reshape(1, new int[] { c, h, w })) // [c, h, w]
            {
                for (int i = 0; i < c; ++i)
                {
                    using (Mat blob_CxHxW_row = blob_CxHxW.row(i))
                    using (Mat blob_1xHW = blob_CxHxW_row.reshape(1, 1)) // [1, h, w] => [1, h * w]
                    {
                        // Subtract blob by mean.
                        Core.subtract(blob_1xHW, (_mean.val[i], 0, 0, 0), blob_1xHW);
                        // Divide blob by std.
                        Core.divide(blob_1xHW, (_std.val[i], 0, 0, 0), blob_1xHW);
                    }
                }
            }

            // How to display the contents of the blob for debugging purposes
            //DebugMat.imshowDNNBlob("blob", blob);

            _net.setInput(blob);

            Mat prob = _net.forward();
            //Debug.Log("prob.ToString(): " + prob.ToString());

            Mat result = prob.reshape(1, 1).clone();
            prob.Dispose();
            blob.Dispose();
            return result;
        }

        /// <summary>
        /// Offloads <see cref="Infer"/> to a thread-pool task (OpenCV DNN), matching
        /// <c>RunCoreProcessingTaskAsync</c> fallback in <c>MediaPipeHandLandmarker</c>.
        /// </summary>
        private async Task<Mat> InferAsync(Mat rgbInput, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if UNITY_WEBGL && !UNITY_EDITOR
            return Infer(rgbInput);
#else
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Infer(rgbInput);
            }, cancellationToken);
#endif
        }

        private async Task DisposeInferenceAsync()
        {
            if (_inferenceRunner != null)
                await _inferenceRunner.DisposeAsync();
            _inferenceRunner = null;

            _net?.Dispose();
            _net = null;
        }

        private List<string> ReadClassNames(string filename)
        {
            List<string> classNames = new List<string>();

            System.IO.StreamReader cReader = null;
            try
            {
                cReader = new System.IO.StreamReader(filename, System.Text.Encoding.Default);

                while (cReader.Peek() >= 0)
                {
                    string name = cReader.ReadLine();
                    classNames.Add(name);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
                return null;
            }
            finally
            {
                if (cReader != null)
                    cReader.Close();
            }

            return classNames;
        }

        /// <summary>
        /// Reserved hook for synchronizing <see cref="UseAsyncInference"/> with platform capabilities.
        /// Does not modify <see cref="UseAsyncInference"/> in this example.
        /// </summary>
        private void UpdateUseAsyncInference()
        {
        }

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
        /// Registers FpsMonitor keys for dnn backend, target, and async inference display.
        /// This sample uses <see cref="Net"/> (<c>cv::dnn::Net</c>). The C# bindings expose no API to read preferred backend/target at runtime,
        /// so when <paramref name="net"/> exists the monitor shows fixed labels (same key names as <see cref="ImageClassificationPPResnetExample"/> with <c>MultiBackendNet</c>).
        /// </summary>
        /// <param name="fpsMonitor">FPS display monitor.</param>
        /// <param name="net">Loaded inference net, or <see langword="null"/> if not loaded.</param>
        private static void UpdateFpsMonitorInferenceInfo(FpsMonitor fpsMonitor, Net net, bool useAsyncInference)
        {
            if (fpsMonitor == null)
                return;

            if (net != null)
            {
                // cv::dnn::Net: No PreferredBackend/PreferredTarget getters in the C# binding; treat as default OpenCV DNN inference.
                fpsMonitor.Add("dnnBackend", "OPENCV");
                fpsMonitor.Add("dnnTarget", "CPU");
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
