#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.Extensions.Worker.DnnModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using OpenCVForUnity.UnityIntegration.Worker.DnnModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenCVDebug = OpenCVForUnity.Extensions.OpenCVDebug;
using Range = OpenCVForUnity.CoreModule.Range;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Colorization Example
    /// Recolors a grayscale photograph using an ONNX colorization DNN (Lab color space, L-channel in / ab channels out).
    ///
    /// Demonstrates:
    /// - Loading image and ONNX model files from StreamingAssets
    /// - Preprocessing the L channel, running synchronous <see cref="MultiBackendNet"/> forward, and merging ab channels back to BGR
    /// - Toggling Sentis vs OpenCV DNN inference and side-by-side gray vs colorized preview in a single <see cref="Mat"/>
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Size"/>, <see cref="Scalar"/>
    /// - <see cref="Dnn"/>: blobFromImage
    /// - <see cref="Imgproc"/>: cvtColor, resize, putText
    /// - <see cref="Imgcodecs"/>: imread
    /// - <see cref="MultiBackendNet"/>, <see cref="MultiBackendDnn"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://github.com/opencv/opencv/blob/master/samples/dnn/colorization.cpp
    /// https://github.com/opencv/opencv/blob/master/samples/dnn/colorization.py
    /// </para>
    /// <para>
    /// [Tested Models]
    /// https://github.com/EnoxSoftware/OpenCVForUnityExampleAssets/releases/download/dnn%2FColorizationExample/colorizer.onnx
    /// </para>
    /// </remarks>
    public class ColorizationExample : MonoBehaviour
    {
        // Constants
        private const int IN_WIDTH = 256;
        private const int IN_HEIGHT = 256;

        private static readonly string IMAGE_FILEPATH = "OpenCVForUnityExamples/dnn/ansel_adams3.jpg";

        private static readonly string ONNX_MODEL_FILEPATH = "OpenCVForUnityExamples/dnn/colorizer.onnx";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Header("UI")]
        [Tooltip("Inference framework selector. Options are built at runtime from InferenceFrameworkUtils.GetSelectionValuesInEnumOrder(). Assign OnInferenceFrameworkDropdownValueChanged to On Value Changed (int).")]
        public Dropdown InferenceFrameworkDropdown;

        [Tooltip("Sentis inference target selector (GPU Compute / GPU Pixel / CPU). Options are built at runtime from SentisInferenceUtils.GetTargetValuesInEnumOrder(). Assign OnSentisInferenceTargetDropdownValueChanged to On Value Changed (int). Value changes reinitialize inference.")]
        public Dropdown SentisInferenceTargetDropdown;

        [Tooltip("When Unity Sentis is selected, Inspector model paths may stay .onnx; at runtime they are rewritten to .sentis and loaded from StreamingAssets (place a matching .sentis beside the onnx file).")]
        public InferenceFrameworkSelectionKind InferenceFramework = InferenceFrameworkSelectionKind.UnitySentis;

        [Tooltip("When using Sentis: selects the Sentis inference target (GPU Compute / GPU Pixel / CPU).")]
        public SentisInferenceTargetKind SentisInferenceTarget = SentisInferenceTargetKind.GPUCompute;

        // Private Fields
        private FpsMonitor _fpsMonitor;

        private string _imageFilepath;

        private string _modelFilepathOnnx;
        private string _modelFilepathSentis;

        private Mat _imgGray;

        /// <summary>
        /// The net (<see cref="MultiBackendNet"/>; loaded via <see cref="MultiBackendDnn.ReadNet"/>).
        /// </summary>
        private MultiBackendNet _net;

        private readonly List<Mat> _forwardOutputBlobs = new List<Mat>();
        private List<string> _unconnectedOutLayerNames;

        private Texture2D _texture;

        private bool _isReady;
        private bool _inferenceReinitializing;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            SyncInferenceModeUi(inferenceReinitializing: false);

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            _imageFilepath = await OpenCVForUnityEnv.GetFilePathAsync(IMAGE_FILEPATH, cancellationToken: _cts.Token);
            _modelFilepathOnnx = await OpenCVForUnityEnv.GetFilePathAsync(ONNX_MODEL_FILEPATH, cancellationToken: _cts.Token);
            if (OpenCVForUnityEnv.IsSentisIntegrationAvailable)
            {
                _modelFilepathSentis = await OpenCVForUnityEnv.GetFilePathAsync(
                    MultiBackendDnn.ResolveSentisModelPathFromOnnxPath(ONNX_MODEL_FILEPATH),
                    cancellationToken: _cts.Token);
            }

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            LoadImageOnce();

            if (!TryInitializeInference())
            {
                return;
            }

            _isReady = true;
            Run();
        }

        private async void OnDestroy()
        {
            _cts?.Cancel();
            await DisposeInferenceAsync();
            _cts?.Dispose();
            _cts = null;

            _imgGray?.Dispose();
            _imgGray = null;

            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
            }

            _texture = null;

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
        /// Invoke from <c>InferenceFrameworkDropdown</c> On Value Changed. Switches the inference framework.
        /// </summary>
        public async void OnInferenceFrameworkDropdownValueChanged(int index)
        {
            if (!_isReady) { return; }
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
            if (!_isReady) { return; }
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

        // Private Methods
        private void LoadImageOnce()
        {
            if (_imgGray != null)
            {
                return;
            }

            if (string.IsNullOrEmpty(_imageFilepath))
            {
                Debug.LogError(IMAGE_FILEPATH + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "image file is not loaded.\nPlease read console message.";
                }
                _imgGray = new Mat(368, 368, CvType.CV_8UC1, new Scalar(0));
                return;
            }

            Mat loaded = Imgcodecs.imread(_imageFilepath, Imgcodecs.IMREAD_GRAYSCALE);
            if (loaded.empty())
            {
                loaded.Dispose();
                _imgGray = new Mat(368, 368, CvType.CV_8UC1, new Scalar(0));
            }
            else
            {
                _imgGray = loaded;
            }

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("width", _imgGray.cols().ToString());
                _fpsMonitor.Add("height", _imgGray.rows().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }
        }

        /// <summary>
        /// Initializes inference from the resolved model path and current backend settings.
        /// </summary>
        private bool TryInitializeInference()
        {
            bool useSentis = InferenceFramework == InferenceFrameworkSelectionKind.UnitySentis && OpenCVForUnityEnv.IsSentisIntegrationAvailable;
            string modelPath = useSentis ? _modelFilepathSentis : _modelFilepathOnnx;

            if (string.IsNullOrEmpty(modelPath))
            {
                Debug.LogError(ONNX_MODEL_FILEPATH + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "model file is not loaded.\nPlease read console message.";
                }
                return false;
            }

            try
            {
                _net = MultiBackendDnn.ReadNet(modelPath);
                if (useSentis)
                {
                    _net.SetPreferableBackend(SentisInferenceBackendKind.UnitySentis);
                    _net.SetPreferableTarget(SentisInferenceTarget);
                }
                else
                {
                    _net.SetPreferableBackend(OpenCVDnnInferenceBackendKind.OpenCv);
                    _net.SetPreferableTarget(OpenCVDnnInferenceTargetKind.Cpu);
                }

                _unconnectedOutLayerNames = _net.GetUnconnectedOutLayersNames();
                return _net != null;
            }
            catch (Exception ex)
            {
                Debug.LogError("ColorizationExample TryInitializeInference failed: " + ex, this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "Failed to initialize inference.\nPlease read console message.";
                }

                if (_net != null)
                {
                    _net.Dispose();
                    _net = null;
                }
                _unconnectedOutLayerNames = null;
                return false;
            }
        }

        private void Run()
        {
            if (!_isReady || _imgGray == null)
            {
                return;
            }

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            Mat colorized = new Mat(_imgGray.rows(), _imgGray.cols(), CvType.CV_8UC3);
            bool ranColorization = false;
            double inferenceMs = 0;

            if (_net != null)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                if (Infer(_imgGray, colorized))
                {
                    sw.Stop();
                    inferenceMs = sw.Elapsed.TotalMilliseconds;
                    ranColorization = true;
                    Debug.Log("Inference time " + inferenceMs + "ms", this);
                    UpdateFpsMonitorInferenceInfo(_fpsMonitor, _net, InferenceFramework);
                }
                else
                {
                    sw.Stop();
                }
            }

            if (!ranColorization)
            {
                Imgproc.cvtColor(_imgGray, colorized, Imgproc.COLOR_GRAY2BGR);
            }
            else
            {
                Imgproc.putText(colorized, inferenceMs.ToString("F1") + "ms", new Point(10, _imgGray.height() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2);
            }

            Imgproc.putText(colorized, "colorized", new Point(10, 20), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2);
            Imgproc.cvtColor(colorized, colorized, Imgproc.COLOR_BGR2RGB);

            UpdatePreviewTexture(colorized);
            colorized.Dispose();

            OpenCVDebug.SetDebugMode(false);
        }

        /// <summary>
        /// Runs synchronous colorization forward into <paramref name="colorized"/> (8U BGR).
        /// </summary>
        /// <returns><see langword="true"/> when inference succeeded.</returns>
        private bool Infer(Mat imgGray, Mat colorized)
        {
            Mat imgL = new Mat();
            Mat imgLResized = new Mat();
            try
            {
                imgGray.convertTo(imgL, CvType.CV_32F, 100.0 / 255.0);
                Imgproc.resize(imgL, imgLResized, new Size(IN_WIDTH, IN_HEIGHT), 0, 0, Imgproc.INTER_CUBIC);

                Mat inputBlob = Dnn.blobFromImage(imgLResized, 1.0, new Size(IN_WIDTH, IN_HEIGHT), new Scalar(0), false, false);
                _net.SetInput(inputBlob);
                _net.Forward(_forwardOutputBlobs, _unconnectedOutLayerNames);
                Mat result = _forwardOutputBlobs[0];

                using (Mat result_a = new Mat(result, new Range[] { new Range(0, 1), new Range(0, 1), new Range(0, result.size(2)), new Range(0, result.size(3)) }))
                using (Mat result_b = new Mat(result, new Range[] { new Range(0, 1), new Range(1, 2), new Range(0, result.size(2)), new Range(0, result.size(3)) }))
                {
                    Mat resultAReshaped = result_a.reshape(1, result.size(2));
                    Mat resultBReshaped = result_b.reshape(1, result.size(2));
                    Mat a = new Mat(imgGray.size(), CvType.CV_32F);
                    Mat b = new Mat(imgGray.size(), CvType.CV_32F);
                    try
                    {
                        Imgproc.resize(resultAReshaped, a, imgGray.size());
                        Imgproc.resize(resultBReshaped, b, imgGray.size());

                        Mat lab = new Mat();
                        Mat colorBgr = new Mat();
                        try
                        {
                            List<Mat> chn = new List<Mat> { imgL, a, b };
                            Core.merge(chn, lab);
                            Imgproc.cvtColor(lab, colorBgr, Imgproc.COLOR_Lab2BGR);
                            colorBgr.convertTo(colorized, CvType.CV_8U, 255.0);
                        }
                        finally
                        {
                            lab.Dispose();
                            colorBgr.Dispose();
                        }
                    }
                    finally
                    {
                        resultAReshaped.Dispose();
                        resultBReshaped.Dispose();
                        a.Dispose();
                        b.Dispose();
                    }
                }

                inputBlob.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("ColorizationExample Infer failed: " + ex, this);
                return false;
            }
            finally
            {
                imgL.Dispose();
                imgLResized.Dispose();
            }
        }

        private void UpdatePreviewTexture(Mat colorizedRgb)
        {
            Mat display = new Mat(_imgGray.rows() * 2, _imgGray.cols(), CvType.CV_8UC3);
            try
            {
                using (Mat grayRgb = new Mat())
                using (Mat displayUpperHalf = new Mat(display, new Range(0, _imgGray.rows())))
                using (Mat displayLowerHalf = new Mat(display, new Range(_imgGray.rows(), display.rows())))
                {
                    Imgproc.cvtColor(_imgGray, grayRgb, Imgproc.COLOR_GRAY2RGB);
                    Imgproc.putText(grayRgb, "gray", new Point(10, 20), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2);

                    grayRgb.copyTo(displayUpperHalf);
                    colorizedRgb.copyTo(displayLowerHalf);
                }

                if (_texture == null
                    || _texture.width != display.cols()
                    || _texture.height != display.rows())
                {
                    if (_texture != null)
                    {
                        Texture2D.Destroy(_texture);
                    }

                    _texture = new Texture2D(display.cols(), display.rows(), TextureFormat.RGBA32, false);
                    ResultPreview.texture = _texture;
                    ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;
                }

                OpenCVMatUnityUtils.MatToTexture2D(display, _texture);
            }
            finally
            {
                display.Dispose();
            }
        }

        // Inference UI
        /// <summary>
        /// Locks inference UI during reinitialization, otherwise delegates Framework / Target
        /// dropdown state to <see cref="UpdateInferenceFramework"/>.
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

                return;
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

        private async Task ReinitializeInferenceAsync(Action applySelection)
        {
            if (!_isReady)
            {
                return;
            }

            _inferenceReinitializing = true;
            SyncInferenceModeUi(inferenceReinitializing: true);

            await DisposeInferenceAsync();

            applySelection();
            TryInitializeInference();
            Run();
            UpdateFpsMonitorInferenceInfo(_fpsMonitor, _net, InferenceFramework);

            _inferenceReinitializing = false;
            SyncInferenceModeUi(inferenceReinitializing: false);
        }

        private async Task DisposeInferenceAsync()
        {
            for (int i = 0; i < _forwardOutputBlobs.Count; i++)
            {
                _forwardOutputBlobs[i]?.Dispose();
            }
            _forwardOutputBlobs.Clear();

            _net?.Dispose();
            _net = null;
            _unconnectedOutLayerNames = null;

            await Task.CompletedTask;
        }

        /// <summary>
        /// Updates <paramref name="fpsMonitor"/> with inference framework, dnn backend, and target
        /// (or "-" when a value is not available).
        /// </summary>
        private static void UpdateFpsMonitorInferenceInfo(
            FpsMonitor fpsMonitor,
            MultiBackendNet net,
            InferenceFrameworkSelectionKind inferenceFramework)
        {
            if (fpsMonitor == null)
            {
                return;
            }

            fpsMonitor.Add(
                "inferenceFramework",
                InferenceFrameworkUtils.GetSelectionDisplayName(inferenceFramework));

            if (net != null)
            {
                int be;
                int tgt;
                try
                {
                    be = net.PreferredBackend;
                    tgt = net.PreferredTarget;
                }
                catch (InvalidOperationException)
                {
                    fpsMonitor.Add("dnnBackend", "-");
                    fpsMonitor.Add("dnnTarget", "-");
                    fpsMonitor.Add("useAsyncInference", "False");
                    return;
                }
                fpsMonitor.Add("dnnBackend", MultiBackendNet.GetBackendDisplayString(be));
                fpsMonitor.Add("dnnTarget", MultiBackendNet.GetTargetDisplayString(tgt));
            }
            else
            {
                fpsMonitor.Add("dnnBackend", "-");
                fpsMonitor.Add("dnnTarget", "-");
            }
        }
    }
}
#endif
