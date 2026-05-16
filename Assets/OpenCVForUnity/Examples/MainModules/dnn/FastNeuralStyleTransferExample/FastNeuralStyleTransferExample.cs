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
using Range = OpenCVForUnity.CoreModule.Range;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Fast Neural Style Transfer Example
    /// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/fast_neural_style.py and https://github.com/jcjohnson/fast-neural-style
    ///
    /// [Tested Models]
    /// https://cs.stanford.edu/people/jcjohns/fast-neural-style/models/instance_norm/mosaic.t7
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class FastNeuralStyleTransferExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// The model filename.
        /// </summary>
        protected static readonly string MODEL_FILENAME = "OpenCVForUnityExamples/dnn/mosaic.t7";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Header("UI")]
        [Tooltip("When on, in-flight single-task async path runs style transfer forward on a background thread (Task.Run) via MatSingleFlightSyncAsyncRunner.")]
        public Toggle UseAsyncInferenceToggle;
        public bool UseAsyncInference = true;

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
        /// The bgr mat.
        /// </summary>
        private Mat _bgrMat;
        /// <summary>
        /// The net.
        /// </summary>
        private Net _net;
        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;
        /// <summary>
        /// The model filepath.
        /// </summary>
        private string _modelFilepath;
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

                if (_net != null)
                {
                    if (_inferenceRunner != null)
                    {
                        Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

                        _inferenceRunner.SubmitWork(
                            _bgrMat,
                            syncWork: Infer,
                            asyncWork: async m =>
                            {
                                CancellationToken ct = _inferenceRunner.InFlightAsyncWorkCancellationToken;
                                return await InferAsync(m, ct);
                            });

                        if (_inferenceRunner.TryGetLatestResult(out Mat outMat))
                        {
                            // The DNN model outputs images in multiples of 8. If the input and output mat sizes are different, only the overlapping area is transcribed.
                            if (outMat.width() != rgbaMat.width() || outMat.height() != rgbaMat.height())
                            {
                                // Clone outMat before copying the overlapping region.
                                Mat outMat_clone = outMat.clone();

                                Rect intersectRect = new Rect(0, 0, outMat_clone.width(), outMat_clone.height()).intersect(new Rect(0, 0, rgbaMat.width(), rgbaMat.height()));
                                Mat outMat_roi = new Mat(outMat_clone, intersectRect);
                                Mat bgrMat_roi = new Mat(_bgrMat, intersectRect);

                                outMat_roi.copyTo(bgrMat_roi);

                                outMat_clone.Dispose();
                            }
                            else
                            {
                                outMat.copyTo(_bgrMat);
                            }

                            Imgproc.cvtColor(_bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);
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

            // Fill in the image so that the unprocessed image is not displayed.
            rgbaMat.setTo(new Scalar(0, 0, 0, 255));

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

            _bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _inferenceRunner?.Cancel();

            _bgrMat?.Dispose();

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
        private void InitializeInference()
        {
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
                _net = Dnn.readNetFromTorch(_modelFilepath);
                _inferenceRunner = new MatSingleFlightSyncAsyncRunner(
                    useAsyncWork: UseAsyncInference,
                    asyncWorkCancellationToken: _cts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("FastNeuralStyleTransferExample InitializeInference failed: " + ex);
            }
        }

        /// <summary>
        /// Synchronous OpenCV DNN forward: style transfer and return 8U BGR result.
        /// </summary>
        private Mat Infer(Mat bgrInput)
        {
            Scalar mean = new Scalar(103.939, 116.779, 123.68);
            Mat blob = Dnn.blobFromImage(bgrInput, 1.0, new Size(bgrInput.width(), bgrInput.height()), mean, false, false);

            _net.setInput(blob);

            Mat prob = _net.forward();

            int[] newshape = new int[] { prob.size(2), prob.size(3) };

            using (Mat outMat = new Mat(newshape[0], newshape[1], CvType.CV_32FC3))
            {
                using (Mat B_channel = new Mat(prob, new Range[] { new Range(0, 1), new Range(0, 1), Range.all(), Range.all() }).reshape(1, newshape))
                using (Mat G_channel = new Mat(prob, new Range[] { new Range(0, 1), new Range(1, 2), Range.all(), Range.all() }).reshape(1, newshape))
                using (Mat R_channel = new Mat(prob, new Range[] { new Range(0, 1), new Range(2, 3), Range.all(), Range.all() }).reshape(1, newshape))
                {
                    Core.merge(new List<Mat>() { B_channel, G_channel, R_channel }, outMat);
                }

                Core.add(outMat, mean, outMat);

                Mat result8u = new Mat();
                outMat.convertTo(result8u, CvType.CV_8U);
                prob.Dispose();
                blob.Dispose();
                return result8u;
            }
        }

        private async Task<Mat> InferAsync(Mat bgrInput, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if UNITY_WEBGL && !UNITY_EDITOR
            return Infer(bgrInput);
#else
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Infer(bgrInput);
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
