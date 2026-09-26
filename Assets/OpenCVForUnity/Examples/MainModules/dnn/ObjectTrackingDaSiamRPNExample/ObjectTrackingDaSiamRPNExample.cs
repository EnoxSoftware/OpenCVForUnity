#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.Extensions;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Interaction;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenCVDebug = OpenCVForUnity.Extensions.OpenCVDebug;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// DNN engine selection for <see cref="ObjectTrackingDaSiamRPNExample"/> and <see cref="DaSiamRPNTracker"/>.
    /// Values match <see cref="Dnn.ENGINE_CLASSIC"/>, <see cref="Dnn.ENGINE_NEW"/>, and <see cref="Dnn.ENGINE_AUTO"/>.
    /// </summary>
    public enum DaSiamRpnDnnEngineSelection
    {
        Classic = Dnn.ENGINE_CLASSIC,
        New = Dnn.ENGINE_NEW,
        Auto = Dnn.ENGINE_AUTO,
    }

    /// <summary>
    /// Object Tracking DaSiamRPN Example (legacy)
    /// Single-object visual tracking with DaSiamRPN after an initial ROI selection.
    /// Prefer the TrackingAPI TrackerDaSiamRPN class for new projects.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Loading three DaSiamRPN ONNX nets from StreamingAssets
    /// - Template initialization from a user-selected ROI and per-frame correlation tracking
    /// - Drawing the tracked bounding box on the RGB preview Mat
    /// - Low-level <see cref="Net"/> inference with a selectable DNN engine (Inspector: <see cref="DnnEngine"/>)
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Net"/>, <see cref="Size"/>, <see cref="Scalar"/>, <see cref="Point"/>
    /// - <see cref="Dnn"/>: readNet, blobFromImage, forward
    /// - <see cref="Imgproc"/>: rectangle
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// This legacy example uses low-level <see cref="Net"/> APIs. The default DNN engine is
    /// <see cref="DaSiamRpnDnnEngineSelection.Classic"/> (<c>Dnn.ENGINE_CLASSIC</c>, OpenCV 4.x compatible).
    /// Change <see cref="DnnEngine"/> in the Inspector to <see cref="DaSiamRpnDnnEngineSelection.New"/> to compare
    /// with the OpenCV 5 graph engine. For native <see cref="TrackerDaSiamRPN"/>, see TrackingExample.
    /// </para>
    /// <para>
    /// Referring to:
    /// https://github.com/opencv/opencv/blob/4.x/samples/dnn/dasiamrpn_tracker.cpp
    /// </para>
    /// <para>
    /// [Tested Models]
    /// https://www.dropbox.com/s/rr1lk9355vzolqv/dasiamrpn_model.onnx?dl=1
    /// https://www.dropbox.com/s/999cqx5zrfi7w4p/dasiamrpn_kernel_r1.onnx?dl=1
    /// https://www.dropbox.com/s/qvmtszx5h339a0w/dasiamrpn_kernel_cls1.onnx?dl=1
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class ObjectTrackingDaSiamRPNExample : MonoBehaviour
    {
        // Constants
        private static readonly string NET_FILEPATH = "OpenCVForUnityExamples/dnn/dasiamrpn_model.onnx";

        private static readonly string KERNEL_R1_FILEPATH = "OpenCVForUnityExamples/dnn/dasiamrpn_kernel_r1.onnx";

        private static readonly string KERNEL_CLS1_FILEPATH = "OpenCVForUnityExamples/dnn/dasiamrpn_kernel_cls1.onnx";

        private static readonly string VIDEO_FILEPATH = "OpenCVForUnityExamples/768x576_mjpeg.mjpeg";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]
        [Header("UI")]
        /// <summary>
        /// The texture rectangle selector component.
        /// </summary>
        public TextureSelector TextureRectangleSelector;

        [Space(10)]
        [Header("DNN")]
        /// <summary>
        /// DNN engine for the three ONNX nets. Classic is recommended for this legacy low-level Net example.
        /// </summary>
        [Tooltip("Classic: OpenCV 4.x compatible engine (default). New: OpenCV 5 graph engine. Auto: new first, then fallback.")]
        public DaSiamRpnDnnEngineSelection DnnEngine = DaSiamRpnDnnEngineSelection.Classic;

        // Private Fields
        private string _netFilepath;
        private string _kernelR1Filepath;
        private string _kernelCls1Filepath;
        private Texture2D _texture;
        private Mat _overlayMat;
        private DaSiamRPNTracker _tracker;
        private Scalar _trackingColor = new Scalar(255, 255, 0);
        private bool _shouldStartTrackerInitialization = false;
        private bool _isTrackingStarted = false;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            _netFilepath = await OpenCVForUnityEnv.GetFilePathAsync(NET_FILEPATH, cancellationToken: _cts.Token);
            _kernelR1Filepath = await OpenCVForUnityEnv.GetFilePathAsync(KERNEL_R1_FILEPATH, cancellationToken: _cts.Token);
            _kernelCls1Filepath = await OpenCVForUnityEnv.GetFilePathAsync(KERNEL_CLS1_FILEPATH, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            if (string.IsNullOrEmpty(_netFilepath) || string.IsNullOrEmpty(_kernelR1Filepath) || string.IsNullOrEmpty(_kernelCls1Filepath))
            {
                Debug.LogError(NET_FILEPATH + " or " + KERNEL_R1_FILEPATH + " or " + KERNEL_CLS1_FILEPATH + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "model file is not loaded.\nPlease read console message.";
                }
            }
            else
            {
                _tracker = new DaSiamRPNTracker(_netFilepath, _kernelR1Filepath, _kernelCls1Filepath, (int)DnnEngine);
            }

            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGB; // DaSiamRPNTracker API must handle 3 channels Mat image.

            WireSourceToMatControlPanelHooks();

            if (string.IsNullOrEmpty(_multiSourceToMatHelper.PerKindSettings.VideoCapture.RequestedVideoFilePath))
            {
                _multiSourceToMatHelper.PerKindSettings.VideoCapture.RequestedVideoFilePath = VIDEO_FILEPATH;
            }

            OpenCVDebug.SetDebugMode(false);

            if (_tracker == null)
            {
                return;
            }

            _multiSourceToMatHelper.Initialize();
        }

        private void Update()
        {
            if (!_multiSourceToMatHelper.IsInitialized)
            {
                return;
            }

            if (_tracker == null)
            {
                if (_multiSourceToMatHelper.IsPlaying && _multiSourceToMatHelper.DidUpdateThisFrame)
                {
                    Mat rgbMat = _multiSourceToMatHelper.FrameMat;

                    OpenCVMatUnityUtils.MatToTexture2D(rgbMat, _texture);
                }
                return;
            }

            if (!_isTrackingStarted)
            {
                if (_multiSourceToMatHelper.IsPaused)
                {
                    Mat sourceMat = _multiSourceToMatHelper.FrameMat;
                    if (sourceMat == null || _texture == null)
                    {
                        return;
                    }

                    if (_shouldStartTrackerInitialization)
                    {
                        var (_, _, currentSelectionPoints) = TextureRectangleSelector.GetSelectionStatus();
                        Rect selectedRegion = TextureSelector.ConvertSelectionPointsToOpenCVRect(currentSelectionPoints);
                        InitializeTrackerWithRegion(sourceMat, selectedRegion);
                        if (_isTrackingStarted)
                        {
                            Debug.Log("Tracker initialization completed", this);
                        }
                    }

                    if (_overlayMat == null)
                    {
                        CreateOrRecreateProcessingResources(sourceMat);
                    }

                    // FrameMat is not refreshed while paused; copy before drawing the selection overlay.
                    sourceMat.copyTo(_overlayMat);
                    TextureRectangleSelector.DrawSelection(_overlayMat, true);

                    OpenCVMatUnityUtils.MatToTexture2D(_overlayMat, _texture);
                }
                else if (_multiSourceToMatHelper.IsPlaying && _multiSourceToMatHelper.DidUpdateThisFrame)
                {
                    Mat rgbMat = _multiSourceToMatHelper.FrameMat;
                    OpenCVMatUnityUtils.MatToTexture2D(rgbMat, _texture);
                }
            }
            else
            {
                if (_multiSourceToMatHelper.IsPlaying && _multiSourceToMatHelper.DidUpdateThisFrame)
                {
                    Mat rgbMat = _multiSourceToMatHelper.FrameMat;

                    if (_tracker.IsInitialized)
                    {
                        // Run DaSiamRPN correlation tracking on the RGB frame Mat.
                        Rect new_region = _tracker.Update(rgbMat);

                        if (_tracker.Score > 0.5)
                        {
                            Imgproc.rectangle(rgbMat, ConvertToTopLeftRef(new_region), _trackingColor, 2, 1, 0);
                        }
                        else
                        {
                            _tracker.Reset();
                            _isTrackingStarted = false;
                            TextureRectangleSelector.enabled = true;
                            TextureRectangleSelector.ResetSelectionStatus();
                            if (_fpsMonitor != null)
                            {
                                _fpsMonitor.ConsoleText = "Please select a rectangle region to start tracking.";
                            }
                        }
                    }

                    // Publish tracked RGB Mat to Unity texture for RawImage preview.
                    OpenCVMatUnityUtils.MatToTexture2D(rgbMat, _texture);
                }
            }
        }

        private void OnDestroy()
        {
            UnwireSourceToMatControlPanelHooks();

            _cts?.Cancel();

            DisposeFrameProcessingResources();
            CleanupPreviewResources();

            _tracker?.Dispose();
            _tracker = null;

            _cts?.Dispose();
            _cts = null;
        }

        // Public Methods
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
                UpdateFpsMonitorInferenceInfo(_fpsMonitor, _tracker);
                _fpsMonitor.ConsoleText = "Please select a rectangle region to start tracking.";
            }

            _isTrackingStarted = false;
            _shouldStartTrackerInitialization = false;

            TextureRectangleSelector.enabled = true;
            TextureRectangleSelector.ResetSelectionStatus();

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

            _tracker?.Reset();
            _isTrackingStarted = false;
            _shouldStartTrackerInitialization = false;

            DisposeFrameProcessingResources();
            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed", this);

            _tracker?.Reset();
            _isTrackingStarted = false;
            _shouldStartTrackerInitialization = false;

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
        /// Raises the reset tracker button click event.
        /// </summary>
        public void OnResetTrackerButtonClick()
        {
            if (_tracker != null)
            {
                _tracker.Reset();
            }

            _isTrackingStarted = false;
            _shouldStartTrackerInitialization = false;

            TextureRectangleSelector.enabled = true;
            TextureRectangleSelector.ResetSelectionStatus();

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Please select a rectangle region to start tracking.";
            }
        }

        /// <summary>
        /// Called from TextureSelector OnTextureSelectionStateChanged (wire in the Inspector).
        /// </summary>
        /// <param name="touchedObject">Touched GameObject.</param>
        /// <param name="touchState">Selection state.</param>
        /// <param name="texturePoints">Texture coordinates (OpenCV style: origin top-left).</param>
        public void OnTextureSelectionStateChanged(GameObject touchedObject, TextureSelector.TextureSelectionState touchState, Vector2[] texturePoints)
        {
            if (!_isTrackingStarted)
            {
                switch (touchState)
                {
                    case TextureSelector.TextureSelectionState.RECTANGLE_SELECTION_STARTED:
                        _multiSourceToMatHelper.Pause();
                        break;

                    case TextureSelector.TextureSelectionState.RECTANGLE_SELECTION_CANCELLED:
                        _multiSourceToMatHelper.Play();
                        break;

                    case TextureSelector.TextureSelectionState.RECTANGLE_SELECTION_COMPLETED:
                        _shouldStartTrackerInitialization = true;
                        break;
                }
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
            _overlayMat?.Dispose();
            _overlayMat = null;
        }

        private void CreateOrRecreateProcessingResources(Mat frameMat)
        {
            if (frameMat == null)
            {
                return;
            }

            DisposeFrameProcessingResources();
            _overlayMat = new Mat(frameMat.rows(), frameMat.cols(), frameMat.type());
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

        private void InitializeTrackerWithRegion(Mat rgbMat, Rect region)
        {
            if (!_multiSourceToMatHelper.IsInitialized || rgbMat == null || _tracker == null)
            {
                _shouldStartTrackerInitialization = false;
                return;
            }

            try
            {
                _tracker.Init(rgbMat, ConvertToCenterRef(region));
                _isTrackingStarted = true;
                TextureRectangleSelector.enabled = false;
                _multiSourceToMatHelper.Play();

                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "";
                }
            }
            catch (Exception e)
            {
                Debug.Log(e, this);
                _multiSourceToMatHelper.Play();
                TextureRectangleSelector.enabled = true;
                TextureRectangleSelector.ResetSelectionStatus();
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "Tracker init failed. Please select a larger region.";
                }
            }

            _shouldStartTrackerInitialization = false;
        }

        private Rect ConvertToCenterRef(Rect r)
        {
            return new Rect(r.x + r.width / 2, r.y + r.height / 2, r.width, r.height);
        }

        private Rect ConvertToTopLeftRef(Rect r)
        {
            return new Rect(r.x - r.width / 2, r.y - r.height / 2, r.width, r.height);
        }

        /// <summary>
        /// Updates <paramref name="fpsMonitor"/> with dnn backend, target, and async mode from
        /// <paramref name="tracker"/> (or "-" when a value is not available).
        /// </summary>
        private static void UpdateFpsMonitorInferenceInfo(FpsMonitor fpsMonitor, DaSiamRPNTracker tracker)
        {
            if (fpsMonitor == null)
            {
                return;
            }

            if (tracker != null)
            {
                // cv::dnn::Net: No PreferredBackend/PreferredTarget getters in the C# binding; treat as default OpenCV DNN inference.
                fpsMonitor.Add("dnnBackend", "OPENCV");
                fpsMonitor.Add("dnnTarget", "CPU");
                fpsMonitor.Add("dnnEngine", tracker.DnnEngineName);
            }
            else
            {
                fpsMonitor.Add("dnnBackend", "-");
                fpsMonitor.Add("dnnTarget", "-");
            }
        }
    }

    /// <summary>
    /// Legacy DaSiamRPN tracker implemented with low-level <see cref="Net"/> APIs.
    /// </summary>
    /// <remarks>
    /// Loads ONNX models with the DNN engine passed to the constructor (see <see cref="DaSiamRpnDnnEngineSelection"/>).
    /// <see cref="DaSiamRpnDnnEngineSelection.Classic"/> is recommended for this port; setParam uses ONNX tensor names
    /// and forward outputs are resolved by blob size so Classic and New engines can both be compared.
    /// </remarks>
    public class DaSiamRPNTracker
    {
        // Private Fields
        private readonly int _dnnEngine;
        private string _windowing = "cosine";
        private int _exemplarSize = 127;
        private int _instanceSize = 271;
        private int _totalStride = 8;
        private int _scoreSize;
        private float _contextAmount = 0.5f;
        private float[] _ratios = new float[] { 0.33f, 0.5f, 1f, 2f, 3f };
        private float[] _scales = new float[] { 8f };
        private int _anchorNum;
        private float _penaltyK = 0.055f;
        private float _windowInfluence = 0.42f;
        private float _lr = 0.295f;

        private Mat _window;

        private Net _net;
        private Net _kernelR1;
        private Net _kernelCls1;

        private int _imH;
        private int _imW;
        private Point _targetPos;
        private Size _targetSz;
        private Scalar _avgChans;
        private Mat _anchor;

        private Mat _trackerEvalScoreR1_0;
        private Mat _trackerEvalTmpR1_0;
        private Mat _trackerEvalTmpR1_1;
        private Mat _trackerEvalTmpR1_2;

        private Mat _trackerEvalFuncTmpR1_0;
        private Mat _trackerEvalFuncTmpR1_1;
        private Mat _trackerEvalFuncTmpR2_0;
        private Mat _trackerEvalFuncTmpR2_1;

        private List<string> _outNames;
        private List<Mat> _outBlobs = new List<Mat>();

        private Mat _teImTmp;
        private Mat _imPatchOriginalResize;

        // Protected Fields
        protected double _score;
        public double Score
        {
            get
            {
                return _score;
            }
        }

        protected bool _isInitialized;
        public bool IsInitialized
        {
            get
            {
                return _isInitialized;
            }
        }

        protected bool _isDisposed;
        public bool IsDisposed
        {
            get
            {
                return _isDisposed;
            }
        }

        /// <summary>Selected DNN engine id (<see cref="Dnn.ENGINE_CLASSIC"/> etc.).</summary>
        public int DnnEngine
        {
            get
            {
                return _dnnEngine;
            }
        }

        /// <summary>Human-readable DNN engine label for UI.</summary>
        public string DnnEngineName
        {
            get
            {
                switch (_dnnEngine)
                {
                    case Dnn.ENGINE_CLASSIC: return "CLASSIC";
                    case Dnn.ENGINE_NEW: return "NEW";
                    case Dnn.ENGINE_AUTO: return "AUTO";
                    default: return _dnnEngine.ToString();
                }
            }
        }

        // Constructor
        public DaSiamRPNTracker(string netFilepath, string kernelR1Filepath, string kernelCls1Filepath, int dnnEngine = Dnn.ENGINE_CLASSIC)
        {
            _dnnEngine = dnnEngine;
            _scoreSize = (int)((_instanceSize - _exemplarSize) / _totalStride) + 1;
            _anchorNum = _ratios.Length * _scales.Length;

            Mat window;
            if (_windowing == "cosine")
            {
                _exemplarSize = 127;
                _instanceSize = 271;
                _totalStride = 8;
                _scoreSize = 19;

                Mat hanning19Mat = new Mat(1, 19, CvType.CV_32FC1);
                hanning19Mat.put(0, 0, new float[] { 0f, 0.03015369f, 0.11697778f, 0.25f, 0.41317591f, 0.58682409f, 0.75f, 0.88302222f, 0.96984631f, 1f, 0.96984631f,
                0.88302222f, 0.75f, 0.58682409f, 0.41317591f, 0.25f, 0.11697778f, 0.03015369f, 0f });
                window = Outer(hanning19Mat, hanning19Mat);
            }
            else
            {
                window = Mat.ones(_scoreSize, _scoreSize, CvType.CV_32FC1);
            }
            Mat windowFlatten = Flatten(window);
            _window = new Mat(windowFlatten.rows() * 1, windowFlatten.cols() * _anchorNum, window.type());
            Tile(windowFlatten, 1, _anchorNum, _window);

            // # Loading network`s and kernel`s models from StreamingAssets-resolved ONNX paths.
            _net = Dnn.readNet(netFilepath, "", "", _dnnEngine);
            _kernelR1 = Dnn.readNet(kernelR1Filepath, "", "", _dnnEngine);
            _kernelCls1 = Dnn.readNet(kernelCls1Filepath, "", "", _dnnEngine);

            if (_net.empty())
            {
                Debug.LogError("model file is not loaded. The model and class names list can be downloaded here: \"https://www.dropbox.com/s/rr1lk9355vzolqv/dasiamrpn_model.onnx?dl=0\". Please copy to \"Assets/StreamingAssets/OpenCVForUnityExamples/dnn/\" folder. ");
            }
            if (_kernelR1.empty())
            {
                Debug.LogError("model file is not loaded. The model and class names list can be downloaded here: \"https://www.dropbox.com/s/999cqx5zrfi7w4p/dasiamrpn_kernel_r1.onnx?dl=0\". Please copy to \"Assets/StreamingAssets/OpenCVForUnityExamples/dnn/\" folder. ");
            }
            if (_kernelCls1.empty())
            {
                Debug.LogError("model file is not loaded. The model and class names list can be downloaded here: \"https://www.dropbox.com/s/qvmtszx5h339a0w/dasiamrpn_kernel_cls1.onnx?dl=0\". Please copy to \"Assets/StreamingAssets/OpenCVForUnityExamples/dnn/\" folder. ");
            }
        }

        // Public Methods
        public void Init(Mat im, Rect initBb)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            _imH = im.height();
            _imW = im.width();

            int bbX = Mathf.Clamp(initBb.x, 0, _imW);
            int bbY = Mathf.Clamp(initBb.y, 0, _imH);
            int bbW = Mathf.Clamp(initBb.width, 0, _imW);
            int bbH = Mathf.Clamp(initBb.height, 0, _imH);
            _targetPos = new Point(bbX, bbY);
            _targetSz = new Size(bbW, bbH);

            _avgChans = Core.mean(im);
            _avgChans = new Scalar(Math.Floor(_avgChans.val[0]), Math.Floor(_avgChans.val[1]), Math.Floor(_avgChans.val[2]), Math.Floor(_avgChans.val[3]));

            // # When we trying to generate ONNX model from the pre-trained .pth model
            // # we are using only one state of the network. In our case used state
            // # with big bounding box, so we were forced to add assertion for
            // # too small bounding boxes - current state of the network can not
            // # work properly with such small bounding boxes
            if (_targetSz.width * _targetSz.height / (float)(_imH * _imW) < 0.004)
            {
                throw new Exception("Initializing BB is too small-try to restart tracker with larger BB");
            }

            _anchor = GenerateAnchor();

            double wcZ = _targetSz.width + _contextAmount * (_targetSz.width + _targetSz.height);
            double hcZ = _targetSz.height + _contextAmount * (_targetSz.width + _targetSz.height);
            int sZ = (int)Math.Round(Math.Sqrt(wcZ * hcZ));

            Mat zCrop = GetSubwindowTracking(im, _exemplarSize, sZ);
            zCrop = Dnn.blobFromImage(zCrop);

            _net.setInput(zCrop);
            Mat zF = _net.forward("onnx_node_output_0!63");
            _kernelR1.setInput(zF);
            Mat r1 = _kernelR1.forward();
            _kernelCls1.setInput(zF);
            Mat cls1 = _kernelCls1.forward();
            r1 = r1.reshape(1, new int[] { 20, 256, 4, 4 });
            cls1 = cls1.reshape(1, new int[] { 10, 256, 4, 4 });

            _net.setParam("onnx_node_output_0!65", 0, r1);
            _net.setParam("onnx_node_output_0!68", 0, cls1);

            _isInitialized = true;
        }

        public Rect Update(Mat im)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (!IsInitialized)
            {
                return new Rect();
            }

            double wcZ = _targetSz.height + _contextAmount * (_targetSz.width + _targetSz.height);
            double hcZ = _targetSz.width + _contextAmount * (_targetSz.width + _targetSz.height);
            double sZ = Math.Sqrt(wcZ * hcZ);
            double scaleZ = _exemplarSize / sZ;
            float dSearch = (_instanceSize - _exemplarSize) / 2f;
            double pad = dSearch / scaleZ;
            int sX = (int)Math.Round(sZ + 2.0 * pad);

            // # Region preprocessing part
            Mat xCrop = GetSubwindowTracking(im, _instanceSize, sX);
            xCrop = Dnn.blobFromImage(xCrop);

            _score = TrackerEval(xCrop, scaleZ);

            _targetPos.x = Math.Max(0, Math.Min(_imW, _targetPos.x));
            _targetPos.y = Math.Max(0, Math.Min(_imH, _targetPos.y));
            _targetSz.width = Math.Max(10, Math.Min(_imW, _targetSz.width));
            _targetSz.height = Math.Max(10, Math.Min(_imH, _targetSz.height));

            return new Rect(_targetPos, _targetSz);
        }

        public void Reset()
        {
            _isInitialized = false;

            _anchor?.Dispose();
            _anchor = null;
        }

        public void Dispose()
        {
            Reset();

            _isDisposed = true;

            _window?.Dispose();
            _window = null;
            _net?.Dispose();
            _net = null;
            _kernelR1?.Dispose();
            _kernelR1 = null;
            _kernelCls1?.Dispose();
            _kernelCls1 = null;

            if (_trackerEvalScoreR1_0 != null)
            {
                _trackerEvalScoreR1_0.Dispose();
                _trackerEvalScoreR1_0 = null;
                _trackerEvalTmpR1_0.Dispose();
                _trackerEvalTmpR1_0 = null;
                _trackerEvalTmpR1_1.Dispose();
                _trackerEvalTmpR1_1 = null;
                _trackerEvalTmpR1_2.Dispose();
                _trackerEvalTmpR1_2 = null;

                _trackerEvalFuncTmpR1_0.Dispose();
                _trackerEvalFuncTmpR1_0 = null;
                _trackerEvalFuncTmpR1_1.Dispose();
                _trackerEvalFuncTmpR1_1 = null;
                _trackerEvalFuncTmpR2_0.Dispose();
                _trackerEvalFuncTmpR2_0 = null;
                _trackerEvalFuncTmpR2_1.Dispose();
                _trackerEvalFuncTmpR2_1 = null;
            }
        }

        // Private Methods

        private Mat GenerateAnchor()
        {
            Mat anchor;
            int score_sz = (int)_scoreSize;

            using (Mat tmp_anchor = Mat.zeros(_anchorNum, 4, CvType.CV_32FC1))
            {
                int size = _totalStride * _totalStride;
                int count = 0;

                foreach (float ratio in _ratios)
                {
                    int ws = (int)(Mathf.Sqrt(size / ratio));
                    int hs = (int)(ws * ratio);
                    foreach (float scale in _scales)
                    {
                        float wws = ws * scale;
                        float hhs = hs * scale;
                        tmp_anchor.put(count, 0, new float[] { 0, 0, wws, hhs });
                        count += 1;
                    }
                }

                using (Mat tmp_anchor_tile = new Mat(tmp_anchor.rows() * 1, tmp_anchor.cols() * score_sz * score_sz, tmp_anchor.type()))
                {
                    Tile(tmp_anchor, 1, score_sz * score_sz, tmp_anchor_tile);
                    anchor = tmp_anchor_tile.reshape(1, _anchorNum * score_sz * score_sz);
                }
            }

            float ori = -(score_sz / 2f) * _totalStride;

            float[] xx_arr = new float[score_sz];
            for (int dx = 0; dx < score_sz; dx++)
            {
                xx_arr[dx] = ori + _totalStride * dx;
            }
            using (Mat tmp_xx = new Mat(1, score_sz, CvType.CV_32FC1))
            {
                tmp_xx.put(0, 0, xx_arr);

                using (Mat tmp_xx_tile = new Mat(tmp_xx.rows() * tmp_xx.cols(), tmp_xx.cols() * 1, tmp_xx.type()))
                {
                    Tile(tmp_xx, tmp_xx.cols(), 1, tmp_xx_tile);
                    using (Mat tmp_xx_tile_t = tmp_xx_tile.t())
                    using (Mat tmp_xx2 = Flatten(tmp_xx_tile))
                    using (Mat tmp_yy2 = Flatten(tmp_xx_tile_t))
                    using (Mat tmp_xx_tile2 = new Mat(tmp_xx2.rows() * _anchorNum, tmp_xx2.cols() * 1, tmp_xx2.type()))
                    using (Mat tmp_yy_tile2 = new Mat(tmp_yy2.rows() * _anchorNum, tmp_yy2.cols() * 1, tmp_yy2.type()))
                    {
                        Tile(tmp_xx2, _anchorNum, 1, tmp_xx_tile2);
                        Tile(tmp_yy2, _anchorNum, 1, tmp_yy_tile2);

                        using (Mat xx = tmp_xx_tile2.reshape(1, anchor.rows()))
                        using (Mat yy = tmp_yy_tile2.reshape(1, anchor.rows()))
                        using (Mat tmp_anchor_roi_c0 = anchor.col(0))
                        using (Mat tmp_anchor_roi_c1 = anchor.col(1))
                        {
                            xx.copyTo(tmp_anchor_roi_c0);
                            yy.copyTo(tmp_anchor_roi_c1);
                        }
                    }
                }
            }

            return anchor.t(); // Return a transposed anchor.
        }

        /// <summary>
        /// change.
        /// </summary>
        /// <param name="r">Mat[1*C]</param>
        /// <param name="dst">Mat[1*C]</param>
        private void Change(Mat r, Mat dst)
        {
            if (r == null)
            {
                throw new ArgumentNullException("r");
            }

            if (r != null)
            {
                r.ThrowIfDisposed();
            }

            if (r.rows() != 1)
            {
                throw new ArgumentException("r.rows() != 1");
            }

            if (dst == null)
            {
                throw new ArgumentNullException("dst");
            }

            if (dst != null)
            {
                dst.ThrowIfDisposed();
            }

            if (dst.rows() != 1)
            {
                throw new ArgumentException("dst.rows() != 1");
            }

            if (dst.cols() != r.cols() || dst.type() != r.type())
            {
                throw new ArgumentException("dst.cols() != r.cols() || dst.type() != r.type()");
            }

            // return np.maximum(r, 1./r)

            Mat tmp_r1_0 = _trackerEvalFuncTmpR1_0;

            Core.divide(1.0, r, tmp_r1_0);
            Core.max(r, tmp_r1_0, dst);
        }

        /// <summary>
        /// sz.
        /// </summary>
        /// <param name="w">Mat[1*C]</param>
        /// <param name="h">Mat[1*C]</param>
        /// <param name="dst">Mat[1*C]</param>
        private void Sz(Mat w, Mat h, Mat dst)
        {
            if (w == null)
            {
                throw new ArgumentNullException("w");
            }

            if (w != null)
            {
                w.ThrowIfDisposed();
            }

            if (w.rows() != 1)
            {
                throw new ArgumentException("w.rows() != 1");
            }

            if (h == null)
            {
                throw new ArgumentNullException("h");
            }

            if (h != null)
            {
                h.ThrowIfDisposed();
            }

            if (h.rows() != 1)
            {
                throw new ArgumentException("h.rows() != 1");
            }

            if (dst == null)
            {
                throw new ArgumentNullException("dst");
            }

            if (dst != null)
            {
                dst.ThrowIfDisposed();
            }

            if (dst.rows() != 1)
            {
                throw new ArgumentException("dst.rows() != 1");
            }

            if (w.cols() != h.cols() || w.type() != h.type())
            {
                throw new ArgumentException(" w.cols() != h.cols() || w.type() != h.type()");
            }

            if (h.cols() != dst.cols() || h.type() != dst.type())
            {
                throw new ArgumentException("h.cols() != dst.cols() || h.type() != dst.type()");
            }

            //pad = (w + h) * 0.5
            //sz2 = (w + pad) * (h + pad)
            //return np.sqrt(sz2)

            Mat tmp_r1_0 = _trackerEvalFuncTmpR1_0;
            Mat tmp_r1_1 = _trackerEvalFuncTmpR1_1;

            Core.add(w, h, tmp_r1_0);
            Core.multiply(tmp_r1_0, new Scalar(0.5), tmp_r1_0); // pad

            Core.add(w, tmp_r1_0, tmp_r1_1);
            Core.add(h, tmp_r1_0, dst);
            Core.multiply(tmp_r1_1, dst, tmp_r1_0); // sz2

            Core.sqrt(tmp_r1_0, dst);
        }

        /// <summary>
        /// sz_wh
        /// </summary>
        /// <param name="wh">Size</param>
        /// <returns></returns>
        private double SzWh(Size wh)
        {
            //pad = (wh[0] + wh[1]) * 0.5
            //sz2 = (wh[0] + pad) * (wh[1] + pad)
            //return np.sqrt(sz2)

            double pad = (wh.width + wh.height) * 0.5;
            double sz2 = (wh.width + pad) * (wh.height + pad);

            return Math.Sqrt(sz2);
        }

        /// <summary>
        /// softmax.
        /// </summary>
        /// <param name="x">Mat[2*C]</param>
        /// <param name="dst">Mat[1*C]</param>
        private void Softmax(Mat x, Mat dst)
        {
            if (x == null)
            {
                throw new ArgumentNullException("x");
            }

            if (x != null)
            {
                x.ThrowIfDisposed();
            }

            if (x.rows() != 2)
            {
                throw new ArgumentException("x.rows() != 2");
            }

            if (dst == null)
            {
                throw new ArgumentNullException("dst");
            }

            if (dst != null)
            {
                dst.ThrowIfDisposed();
            }

            if (dst.rows() != 1 || dst.cols() != x.cols() || dst.type() != x.type())
            {
                throw new ArgumentException("dst.rows() != 1 || dst.cols() != x.cols() || dst.type() != x.type()");
            }

            //x_max = x.max(0)
            //e_x = np.exp(x - x_max)
            //y = e_x / e_x.sum(axis = 0)

            Mat tmp_r1_0 = _trackerEvalFuncTmpR1_0;
            Mat tmp_r2_0 = _trackerEvalFuncTmpR2_0;
            Mat tmp_r2_1 = _trackerEvalFuncTmpR2_1;

            MaxAxis0(x, tmp_r1_0);
            Tile(tmp_r1_0, 2, 1, tmp_r2_0); // x_max

            Core.subtract(x, tmp_r2_0, tmp_r2_0);
            Core.exp(tmp_r2_0, tmp_r2_0); // e_x

            SumAxis0(tmp_r2_0, tmp_r1_0);
            Tile(tmp_r1_0, 2, 1, tmp_r2_1); // e_x_sum

            Core.divide(tmp_r2_0, tmp_r2_1, tmp_r2_0); // y

            using (Mat x_x_max_subtract_roi_r1 = tmp_r2_0.row(1))
            {
                x_x_max_subtract_roi_r1.copyTo(dst);
            }
        }

        // # Reshaping cropped image for using in the model
        private Mat GetSubwindowTracking(Mat im, int model_size, int original_sz)
        {
            Size im_sz = im.size();
            double ct = (original_sz + 1) / 2.0;

            int context_xmin = (int)Math.Round(_targetPos.x - ct);
            int context_xmax = context_xmin + original_sz - 1;
            int context_ymin = (int)Math.Round(_targetPos.y - ct);
            int context_ymax = context_ymin + original_sz - 1;
            int left_pad = (int)Math.Max(0.0, -context_xmin);
            int top_pad = (int)Math.Max(0.0, -context_ymin);
            int right_pad = (int)Math.Max(0.0, context_xmax - im_sz.width + 1);
            int bot_pad = (int)Math.Max(0.0, context_ymax - im_sz.height + 1);
            context_xmin += left_pad;
            context_xmax += left_pad;
            context_ymin += top_pad;
            context_ymax += top_pad;
            int r = (int)im_sz.height;
            int c = (int)im_sz.width;

            double wc_z = im.height() + _contextAmount * (im.width() + im.height());
            double hc_z = im.width() + _contextAmount * (im.width() + im.height());
            double z = Math.Sqrt(wc_z * hc_z);
            double scale_z = _exemplarSize / z;
            float d_search = (_instanceSize - _exemplarSize) / 2f;
            double pad = d_search / scale_z;
            int te_im_tmp_sz = (int)Math.Round(z + 2.0 * pad);

            if (_teImTmp == null || _teImTmp.rows() != te_im_tmp_sz || _teImTmp.cols() != te_im_tmp_sz)
            {
                _teImTmp = new Mat(te_im_tmp_sz, te_im_tmp_sz, im.type());
            }

            Mat im_patch_original;

            if (top_pad > 0 || bot_pad > 0 || left_pad > 0 || right_pad > 0)
            {
                using (Mat te_im = new Mat(_teImTmp, new Rect(0, 0, c + left_pad + right_pad, r + top_pad + bot_pad)))
                using (Mat te_im_roi = new Mat(te_im, new Rect(left_pad, top_pad, c, r)))
                {
                    im.copyTo(te_im_roi);

                    if (top_pad > 0)
                    {
                        using (Mat te_im_roi2 = new Mat(te_im, new Rect(left_pad, 0, c, top_pad)))
                        {
                            te_im_roi2.setTo(_avgChans);
                        }
                    }
                    if (bot_pad > 0)
                    {
                        using (Mat te_im_roi2 = new Mat(te_im, new Rect(left_pad, r + top_pad, c, te_im.rows() - (r + top_pad))))
                        {
                            te_im_roi2.setTo(_avgChans);
                        }
                    }
                    if (left_pad > 0)
                    {
                        using (Mat te_im_roi2 = new Mat(te_im, new Rect(0, 0, left_pad, te_im.rows())))
                        {
                            te_im_roi2.setTo(_avgChans);
                        }
                    }
                    if (right_pad > 0)
                    {
                        using (Mat te_im_roi2 = new Mat(te_im, new Rect(c + left_pad, 0, te_im.cols() - (c + left_pad), te_im.rows())))
                        {
                            te_im_roi2.setTo(_avgChans);
                        }
                    }
                    im_patch_original = new Mat(te_im, new Rect(context_xmin, context_ymin, context_xmax - context_xmin + 1, context_ymax - context_ymin + 1));
                }
            }
            else
            {
                im_patch_original = new Mat(im, new Rect(context_xmin, context_ymin, context_xmax - context_xmin + 1, context_ymax - context_ymin + 1));
            }

            if (model_size != original_sz)
            {
                if (_imPatchOriginalResize == null || _imPatchOriginalResize.rows() != model_size || _imPatchOriginalResize.cols() != model_size)
                {
                    _imPatchOriginalResize = new Mat(model_size, model_size, im_patch_original.type());
                }

                Imgproc.resize(im_patch_original, _imPatchOriginalResize, new Size(model_size, model_size));
                im_patch_original.Dispose();

                return _imPatchOriginalResize;
            }
            else
            {
                return im_patch_original;
            }
        }

        /// <summary>
        /// Compute the outer product of two vectors.
        /// </summary>
        /// <param name="a">Mat[1*C]</param>
        /// <param name="b">Mat[1*C]</param>
        private Mat Outer(Mat a, Mat b)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (a != null)
            {
                a.ThrowIfDisposed();
            }

            if (b == null)
            {
                throw new ArgumentNullException("b");
            }

            if (b != null)
            {
                b.ThrowIfDisposed();
            }

            if (a.rows() != 1 || a.channels() != 1)
            {
                throw new ArgumentException("a.rows() != 1 || a.channels() != 1");
            }

            if (b.rows() != 1 || b.channels() != 1)
            {
                throw new ArgumentException("b.rows() != 1 || b.channels() != 1");
            }

            if (a.type() != b.type())
            {
                throw new ArgumentException("a.type() != b.type()");
            }

            int rows = a.cols();
            int cols = b.cols();
            int type = a.type();

            Mat dst;

            using (Mat tmp_a = new Mat(cols, rows, type))
            using (Mat tmp_b = new Mat(rows, cols, type))
            {
                Core.repeat(a, cols, 1, tmp_a);
                Core.repeat(b, rows, 1, tmp_b);
                Core.transpose(tmp_a, tmp_a);

                dst = tmp_a.mul(tmp_b);
            }

            return dst;
        }

        /// <summary>
        /// Return a copy of the array collapsed into one dimension.
        /// </summary>
        private Mat Flatten(Mat a)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (a != null)
            {
                a.ThrowIfDisposed();
            }

            return a.reshape(1, 1);
        }

        /// <summary>
        /// Construct an array by repeating A the number of times given by reps.
        /// </summary>
        private void Tile(Mat a, int ny, int nx, Mat dst)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (a != null)
            {
                a.ThrowIfDisposed();
            }

            if (dst == null)
            {
                throw new ArgumentNullException("dst");
            }

            if (dst != null)
            {
                dst.ThrowIfDisposed();
            }

            if (dst.rows() != a.rows() * ny || dst.cols() != a.cols() * nx || dst.type() != a.type())
            {
                throw new ArgumentException("dst.rows() != a.rows() * ny || dst.cols() != a.cols() * nx || dst.type() != a.type()");
            }

            Core.repeat(a, ny, nx, dst);
        }

        /// <summary>
        /// Return the maximum along a given axis.
        /// </summary>
        /// <param name="a">Mat[2*C]</param>
        /// <param name="dst">Mat[1*C]</param>
        private void MaxAxis0(Mat a, Mat dst)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (a != null)
            {
                a.ThrowIfDisposed();
            }

            if (a.channels() != 1)
            {
                throw new ArgumentException("a.channels() != 1");
            }

            if (dst == null)
            {
                throw new ArgumentNullException("dst");
            }

            if (dst != null)
            {
                dst.ThrowIfDisposed();
            }

            if (dst.rows() != 1 || dst.cols() != a.cols() || dst.type() != a.type())
            {
                throw new ArgumentException("dst.rows() != 1 || dst.cols() != a.cols() || dst.type() != a.type()");
            }

            using (Mat a_roi_r0 = a.row(0))
            {
                a_roi_r0.copyTo(dst);

                int len = a.rows();
                for (int i = 1; i < len; i++)
                {
                    using (Mat a_roi_r = a.row(i))
                    {
                        Core.max(dst, a_roi_r, dst);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the indices of the maximum values along an axis.
        /// </summary>
        /// <param name="a">Mat[R*C]</param>
        /// <param name="dst">Mat[R*1]</param>
        private void ArgmaxAxis1(Mat a, Mat dst)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (a != null)
            {
                a.ThrowIfDisposed();
            }

            if (a.channels() != 1)
            {
                throw new ArgumentException("a.channels() != 1");
            }

            if (dst == null)
            {
                throw new ArgumentNullException("dst");
            }

            if (dst != null)
            {
                dst.ThrowIfDisposed();
            }

            if (dst.rows() != a.rows() || dst.cols() != 1 || dst.type() != a.type())
            {
                throw new ArgumentException("dst.rows() != a.rows() || dst.cols() != 1 || dst.type() != a.type()");
            }

            int len = a.rows();
            float[] dstArr = new float[len];
            for (int i = 0; i < len; i++)
            {
                using (Mat a_roi_r = a.row(i))
                {
                    Core.MinMaxLocResult r = Core.minMaxLoc(a_roi_r);
                    dstArr[i] = (float)r.maxLoc.x;
                }
            }
            MatBufferUtils.CopyToMat(dstArr, dst);
        }

        /// <summary>
        /// Sum of array elements over a given axis.
        /// </summary>
        /// <param name="a">Mat[2*C]</param>
        /// <param name="dst">Mat[1*C]</param>
        private void SumAxis0(Mat a, Mat dst)
        {
            if (a == null)
            {
                throw new ArgumentNullException("a");
            }

            if (a != null)
            {
                a.ThrowIfDisposed();
            }

            if (a.rows() != 2)
            {
                throw new ArgumentException("a.rows() != 2");
            }

            if (dst == null)
            {
                throw new ArgumentNullException("dst");
            }

            if (dst != null)
            {
                dst.ThrowIfDisposed();
            }

            if (dst.rows() != 1 || dst.cols() != a.cols() || dst.type() != a.type())
            {
                throw new ArgumentException("dst.rows() != 1 || dst.cols() != a.cols() || dst.type() != a.type()");
            }

            using (Mat a_roi_r0 = a.row(0))
            using (Mat a_roi_r1 = a.row(1))
            {
                Core.add(a_roi_r0, a_roi_r1, dst);
            }
        }

        private double TrackerEval(Mat x_crop, double scale_z)
        {
            Size target_size = _targetSz * scale_z;
            _net.setInput(x_crop);

            if (_outNames == null)
            {
                _outNames = _net.getUnconnectedOutLayersNames();
            }

            _net.forward(_outBlobs, _outNames);
            // OpenCV 5 DNN: output order may differ from classic engine (delta=4*A*S*S, score=2*A*S*S).
            Mat deltaMat;
            Mat scoreMat;
            if (_outBlobs[0].total() >= _outBlobs[1].total())
            {
                deltaMat = _outBlobs[0];
                scoreMat = _outBlobs[1];
            }
            else
            {
                deltaMat = _outBlobs[1];
                scoreMat = _outBlobs[0];
            }

            deltaMat = deltaMat.reshape(1, new int[] { 4, (int)deltaMat.total() / 4 });
            scoreMat = scoreMat.reshape(1, new int[] { 2, (int)scoreMat.total() / 2 });

            int cols = deltaMat.cols();
            int type = deltaMat.type();

            if (_trackerEvalScoreR1_0 == null)
            {
                _trackerEvalScoreR1_0 = new Mat(1, cols, type);
            }

            if (_trackerEvalTmpR1_0 == null)
            {
                _trackerEvalTmpR1_0 = new Mat(1, cols, type);
            }

            if (_trackerEvalTmpR1_1 == null)
            {
                _trackerEvalTmpR1_1 = new Mat(1, cols, type);
            }

            if (_trackerEvalTmpR1_2 == null)
            {
                _trackerEvalTmpR1_2 = new Mat(1, cols, type);
            }

            if (_trackerEvalFuncTmpR1_0 == null)
            {
                _trackerEvalFuncTmpR1_0 = new Mat(1, cols, type);
            }

            if (_trackerEvalFuncTmpR1_1 == null)
            {
                _trackerEvalFuncTmpR1_1 = new Mat(1, cols, type);
            }

            if (_trackerEvalFuncTmpR2_0 == null)
            {
                _trackerEvalFuncTmpR2_0 = new Mat(2, cols, type);
            }

            if (_trackerEvalFuncTmpR2_1 == null)
            {
                _trackerEvalFuncTmpR2_1 = new Mat(2, cols, type);
            }

            Mat score = _trackerEvalScoreR1_0;
            Softmax(scoreMat, score);

            Mat tmp_r1_0 = _trackerEvalTmpR1_0;
            Mat tmp_r1_1 = _trackerEvalTmpR1_1;
            Mat tmp_r1_2 = _trackerEvalTmpR1_2;

            //delta[0, :] = delta[0, :] * self.anchor[:, 2] + self.anchor[:, 0]
            //delta[1, :] = delta[1, :] * self.anchor[:, 3] + self.anchor[:, 1]
            //delta[2, :] = np.exp(delta[2, :]) * self.anchor[:, 2]
            //delta[3, :] = np.exp(delta[3, :]) * self.anchor[:, 3]

            using (Mat delta_roi_r0 = deltaMat.row(0))
            using (Mat delta_roi_r1 = deltaMat.row(1))
            using (Mat delta_roi_r2 = deltaMat.row(2))
            using (Mat delta_roi_r3 = deltaMat.row(3))
            using (Mat anchor_roi_r0 = _anchor.row(0))
            using (Mat anchor_roi_r1 = _anchor.row(1))
            using (Mat anchor_roi_r2 = _anchor.row(2))
            using (Mat anchor_roi_r3 = _anchor.row(3))
            {
                Core.multiply(delta_roi_r0, anchor_roi_r2, tmp_r1_0);
                Core.add(tmp_r1_0, anchor_roi_r0, tmp_r1_0);
                tmp_r1_0.copyTo(delta_roi_r0);

                Core.multiply(delta_roi_r1, anchor_roi_r3, tmp_r1_0);
                Core.add(tmp_r1_0, anchor_roi_r1, tmp_r1_0);
                tmp_r1_0.copyTo(delta_roi_r1);

                Core.exp(delta_roi_r2, tmp_r1_0);
                Core.multiply(tmp_r1_0, anchor_roi_r2, tmp_r1_0);
                tmp_r1_0.copyTo(delta_roi_r2);

                Core.exp(delta_roi_r3, tmp_r1_0);
                Core.multiply(tmp_r1_0, anchor_roi_r3, tmp_r1_0);
                tmp_r1_0.copyTo(delta_roi_r3);
            }

            //s_c = __change(__sz(delta[2, :], delta[3, :]) / (__sz_wh(target_size)))
            //r_c = __change((target_size[0] / target_size[1]) / (delta[2, :] / delta[3, :]))
            //penalty = np.exp(-(r_c * s_c - 1.) * self.penalty_k)
            //pscore = penalty * score
            //pscore = pscore * (1 - self.window_influence) + self.window * self.window_influence
            //best_pscore_id = np.argmax(pscore)

            int best_pscore_id;
            double penalty_best_pscore;

            double target_size_sz_wh = SzWh(target_size);
            using (Mat delta_roi_r2 = deltaMat.row(2))
            using (Mat delta_roi_r3 = deltaMat.row(3))
            {
                Sz(delta_roi_r2, delta_roi_r3, tmp_r1_0);
                Core.divide(tmp_r1_0, new Scalar(target_size_sz_wh), tmp_r1_0);
                Change(tmp_r1_0, tmp_r1_1); // s_c

                Core.divide(delta_roi_r2, delta_roi_r3, tmp_r1_0);
                Core.divide(target_size.width / target_size.height, tmp_r1_0, tmp_r1_0);
                Change(tmp_r1_0, tmp_r1_2); // r_c

                Core.multiply(tmp_r1_2, tmp_r1_1, tmp_r1_2);
                Core.subtract(tmp_r1_2, new Scalar(1.0), tmp_r1_2);
                Core.multiply(tmp_r1_2, new Scalar(_penaltyK), tmp_r1_2, -1.0);
                Core.exp(tmp_r1_2, tmp_r1_2);
                Mat penalty = tmp_r1_2; // penalty

                Core.multiply(penalty, score, tmp_r1_0);
                Core.multiply(tmp_r1_0, new Scalar(1 - _windowInfluence), tmp_r1_0);
                Core.multiply(_window, new Scalar(_windowInfluence), tmp_r1_1);
                Core.add(tmp_r1_0, tmp_r1_1, tmp_r1_0);
                Mat pscore = tmp_r1_0; // pscore

                using (Mat pscore_argmax = new Mat(1, 1, type))
                {
                    ArgmaxAxis1(pscore, pscore_argmax);

                    best_pscore_id = (int)pscore_argmax.get(0, 0)[0];
                    penalty_best_pscore = penalty.get(0, best_pscore_id)[0];
                }
            }

            float[] target = new float[4];
            target[0] = (float)(deltaMat.get(0, best_pscore_id)[0] / scale_z);
            target[1] = (float)(deltaMat.get(1, best_pscore_id)[0] / scale_z);
            target[2] = (float)(deltaMat.get(2, best_pscore_id)[0] / scale_z);
            target[3] = (float)(deltaMat.get(3, best_pscore_id)[0] / scale_z);

            target_size /= scale_z;
            double lr = penalty_best_pscore * score.get(0, best_pscore_id)[0] * _lr;
            double res_x = target[0] + _targetPos.x;
            double res_y = target[1] + _targetPos.y;
            double res_w = target_size.width * (1.0 - lr) + target[2] * lr;
            double res_h = target_size.height * (1.0 - lr) + target[3] * lr;
            _targetPos = new Point(res_x, res_y);
            _targetSz = new Size(res_w, res_h);

            return score.get(0, best_pscore_id)[0];
        }
    }
}
#endif
