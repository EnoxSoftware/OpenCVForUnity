using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.TrackingModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Interaction;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using OpenCVForUnity.VideoModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Tracking Example
    /// Tracks a user-selected rectangle region across consecutive input frames using multiple OpenCV trackers.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Pausing video to draw a ROI, then initializing one or more trackers in parallel
    /// - Comparing classical trackers (KCF, CSRT, MIL) with DNN-based ones (Vit, DaSiamRPN, Nano)
    /// - Per-frame bounding-box update and optional Vit tracking-confidence display
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Scalar"/>, <see cref="Point"/>, <see cref="Rect"/>
    /// - <see cref="Tracker"/>, <see cref="TrackerKCF"/>, <see cref="TrackerCSRT"/>, <see cref="TrackerMIL"/>
    /// - <see cref="TrackerVit"/>, <see cref="TrackerDaSiamRPN"/>, <see cref="TrackerNano"/>
    /// - <see cref="Imgproc"/>: rectangle, putText
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// http://docs.opencv.org/trunk/d5/d07/tutorial_multitracker.html
    /// https://github.com/opencv/opencv_zoo/tree/main/models/object_tracking_vittrack
    /// https://github.com/opencv/opencv/blob/4.x/samples/dnn/dasiamrpn_tracker.cpp
    /// https://github.com/opencv/opencv/blob/4.x/samples/dnn/nanotrack_tracker.cpp
    /// </para>
    /// <para>
    /// [Tested Models]
    /// https://github.com/opencv/opencv_zoo/raw/80f7c6aa030a87b3f9e8ab7d84f62f13d308c10f/models/object_tracking_vittrack/object_tracking_vittrack_2023sep.onnx
    /// https://www.dropbox.com/s/rr1lk9355vzolqv/dasiamrpn_model.onnx?dl=1
    /// https://www.dropbox.com/s/999cqx5zrfi7w4p/dasiamrpn_kernel_r1.onnx?dl=1
    /// https://www.dropbox.com/s/qvmtszx5h339a0w/dasiamrpn_kernel_cls1.onnx?dl=1
    /// https://github.com/HonglinChu/SiamTrackers/raw/c2ff8479624b12ef2dcd830c47f2495a2c4852d4/NanoTrack/models/nanotrackv2/nanotrack_backbone_sim.onnx
    /// https://github.com/HonglinChu/SiamTrackers/raw/c2ff8479624b12ef2dcd830c47f2495a2c4852d4/NanoTrack/models/nanotrackv2/nanotrack_head_sim.onnx
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class TrackingExample : MonoBehaviour
    {
        // Constants
        private static readonly string VIT_MODEL_FILEPATH = "OpenCVForUnityExamples/tracking/object_tracking_vittrack_2023sep.onnx";

        private static readonly string DASIAMRPN_MODEL_FILEPATH = "OpenCVForUnityExamples/tracking/dasiamrpn_model.onnx";

        private static readonly string DASIAMRPN_KERNEL_R1_FILEPATH = "OpenCVForUnityExamples/tracking/dasiamrpn_kernel_r1.onnx";

        private static readonly string DASIAMRPN_KERNEL_CLS1_FILEPATH = "OpenCVForUnityExamples/tracking/dasiamrpn_kernel_cls1.onnx";

        private static readonly string NANOTRACK_BACKBONE_SIM_FILEPATH = "OpenCVForUnityExamples/tracking/nanotrack_backbone_sim.onnx";

        private static readonly string NANOTRACK_HEAD_SIM_FILEPATH = "OpenCVForUnityExamples/tracking/nanotrack_head_sim.onnx";

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
        /// The texture touched point getter component.
        /// </summary>
        public TextureSelector TextureRectangleSelector;

        /// <summary>
        /// The trackerKFC Toggle.
        /// </summary>
        public Toggle TrackerKCFToggle;

        /// <summary>
        /// The trackerCSRT Toggle.
        /// </summary>
        public Toggle TrackerCSRTToggle;

        /// <summary>
        /// The trackerMIL Toggle.
        /// </summary>
        public Toggle TrackerMILToggle;

        /// <summary>
        /// The trackerVit Toggle.
        /// </summary>
        public Toggle TrackerVitToggle;

        /// <summary>
        /// The trackerDaSiamRPN Toggle.
        /// </summary>
        public Toggle TrackerDaSiamRPNToggle;

        /// <summary>
        /// The trackerNano Toggle.
        /// </summary>
        public Toggle TrackerNanoToggle;

        // Private Fields
        private string _vitModelFilepath;
        private string _daSiamRpnModelFilepath;
        private string _daSiamRpnKernelR1Filepath;
        private string _daSiamRpnKernelCls1Filepath;
        private string _nanotrackBackboneSimFilepath;
        private string _nanotrackHeadSimFilepath;
        private bool _disableTrackerVit = false;
        private bool _disableTrackerDaSiamRPN = false;
        private bool _disableTrackerNano = false;
        private Texture2D _texture;
        private Mat _overlayMat;
        private bool _shouldStartTrackerInitialization = false;
        private bool _isTrackingStarted = false;
        private List<TrackerSetting> _trackers;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
#if UNITY_WSA_10_0
        private void Start()
#else
        private async void Start()
#endif
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();

#if UNITY_WSA_10_0

            // Disable the DNN module-dependent Tracker on UWP platforms, as it cannot be used.
            TrackerVitToggle.isOn = TrackerVitToggle.interactable = false;
            _disableTrackerVit = true;
            TrackerDaSiamRPNToggle.isOn = TrackerDaSiamRPNToggle.interactable = false;
            _disableTrackerDaSiamRPN = true;
            TrackerNanoToggle.isOn = TrackerNanoToggle.interactable = false;
            _disableTrackerNano = true;
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGB; // Tracking API requires a 3-channel BGR/RGB Mat.
            WireSourceToMatControlPanelHooks();

            if (string.IsNullOrEmpty(_multiSourceToMatHelper.PerKindSettings.VideoCapture.RequestedVideoFilePath))
            {
                _multiSourceToMatHelper.PerKindSettings.VideoCapture.RequestedVideoFilePath = VIDEO_FILEPATH;
            }

            _multiSourceToMatHelper.Initialize();
#else

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            _vitModelFilepath = await OpenCVForUnityEnv.GetFilePathAsync(VIT_MODEL_FILEPATH, cancellationToken: _cts.Token);
            _daSiamRpnModelFilepath = await OpenCVForUnityEnv.GetFilePathAsync(DASIAMRPN_MODEL_FILEPATH, cancellationToken: _cts.Token);
            _daSiamRpnKernelR1Filepath = await OpenCVForUnityEnv.GetFilePathAsync(DASIAMRPN_KERNEL_R1_FILEPATH, cancellationToken: _cts.Token);
            _daSiamRpnKernelCls1Filepath = await OpenCVForUnityEnv.GetFilePathAsync(DASIAMRPN_KERNEL_CLS1_FILEPATH, cancellationToken: _cts.Token);
            _nanotrackBackboneSimFilepath = await OpenCVForUnityEnv.GetFilePathAsync(NANOTRACK_BACKBONE_SIM_FILEPATH, cancellationToken: _cts.Token);
            _nanotrackHeadSimFilepath = await OpenCVForUnityEnv.GetFilePathAsync(NANOTRACK_HEAD_SIM_FILEPATH, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            CheckFilePaths();

            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGB; // Tracking API requires a 3-channel BGR/RGB Mat.

            WireSourceToMatControlPanelHooks();

            if (string.IsNullOrEmpty(_multiSourceToMatHelper.PerKindSettings.VideoCapture.RequestedVideoFilePath))
            {
                _multiSourceToMatHelper.PerKindSettings.VideoCapture.RequestedVideoFilePath = VIDEO_FILEPATH;
            }

            _multiSourceToMatHelper.Initialize();
#endif
        }

        private void Update()
        {
            if (!_isTrackingStarted)
            {
                // Pre-tracking phase: handle rectangle selection only when tracking has not started
                if (_multiSourceToMatHelper.IsPaused)
                {
                    Mat sourceMat = _multiSourceToMatHelper.FrameMat;
                    if (sourceMat == null || _texture == null)
                    {
                        return;
                    }

                    if (_shouldStartTrackerInitialization)
                    {
                        var (gameObject, currentSelectionState, currentSelectionPoints) = TextureRectangleSelector.GetSelectionStatus();
                        // Convert UI rectangle (top-left origin) to OpenCV Rect for tracker.init().
                        var selectedRegion = TextureSelector.ConvertSelectionPointsToOpenCVRect(currentSelectionPoints);

                        // Each enabled toggle creates a separate tracker on the same ROI.
                        InitializeTrackersWithRegion(sourceMat, selectedRegion);

                        // Set tracking started flag
                        _isTrackingStarted = true;

                        // Disable TextureRectangleSelector when tracking starts
                        TextureRectangleSelector.enabled = false;

                        // Resume playback after tracker initialization
                        _multiSourceToMatHelper.Play();

                        _shouldStartTrackerInitialization = false;

                        Debug.Log("Tracker initialization completed", this);
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
                // Post-tracking phase: handle tracker updates only when tracking has started
                if (_multiSourceToMatHelper.IsPlaying && _multiSourceToMatHelper.DidUpdateThisFrame)
                {
                    Mat rgbMat = _multiSourceToMatHelper.FrameMat;

                    // tracker.update() modifies boundingBox in place for the current frame.
                    for (int i = 0; i < _trackers.Count; i++)
                    {
                        Tracker tracker = _trackers[i].Tracker;
                        string label = _trackers[i].Label;
                        Scalar lineColor = _trackers[i].LineColor;
                        Rect boundingBox = _trackers[i].BoundingBox;

                        tracker.update(rgbMat, boundingBox);

                        Imgproc.rectangle(rgbMat, boundingBox.tl(), boundingBox.br(), lineColor, 2, 1, 0);

                        // Vit tracker exposes a confidence score; lower values suggest tracking loss.
                        if (_trackers[i].Tracker is TrackerVit trackerVit)
                        {
                            float score = trackerVit.getTrackingScore();
                            if (score < 0.4f)
                            {
                                Imgproc.putText(rgbMat, label + " " + string.Format("{0:0.00}", score), new Point(boundingBox.x, boundingBox.y - 5), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 0, 0, 255), 1, Imgproc.LINE_AA, false);
                            }
                            else
                            {
                                Imgproc.putText(rgbMat, label + " " + string.Format("{0:0.00}", score), new Point(boundingBox.x, boundingBox.y - 5), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, lineColor, 1, Imgproc.LINE_AA, false);
                            }
                        }
                        else
                        {
                            Imgproc.putText(rgbMat, label, new Point(boundingBox.x, boundingBox.y - 5), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, lineColor, 1, Imgproc.LINE_AA, false);
                        }
                    }

                    OpenCVMatUnityUtils.MatToTexture2D(rgbMat, _texture);
                }
            }
        }

        private void OnDestroy()
        {
            UnwireSourceToMatControlPanelHooks();

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        // Public Methods
        /// <summary>
        /// Raises the helper frame mat updated event.
        /// Frame processing runs in <see cref="Update"/> because rectangle selection requires per-frame work while paused.
        /// </summary>
        public void OnSourceToMatHelperFrameMatUpdated()
        {
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
                _fpsMonitor.ConsoleText = "Please select a rectangle region to start tracking.";
            }

            _trackers = new List<TrackerSetting>();

            _isTrackingStarted = false;

            // Enable TextureRectangleSelector when tracking stops
            TextureRectangleSelector.enabled = true;

            // Reset TextureRectangleSelector state
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

            ResetTrackers();

            DisposeFrameProcessingResources();
            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed", this);

            ResetTrackers();

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
        /// Raises the reset trackers button click event.
        /// </summary>
        public void OnResetTrackersButtonClick()
        {
            ResetTrackers();

            _isTrackingStarted = false;

            // Enable TextureRectangleSelector when tracking stops
            TextureRectangleSelector.enabled = true;

            // Reset TextureRectangleSelector state
            TextureRectangleSelector.ResetSelectionStatus();

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Please select a rectangle region to start tracking.";
            }
        }

        /// <summary>
        /// Handles the texture selection state changed event.
        /// This method should be connected to the TextureSelector's OnTextureSelectionStateChanged event in the Inspector.
        /// </summary>
        /// <param name="touchedObject">The GameObject that was touched.</param>
        /// <param name="touchState">The touch state.</param>
        /// <param name="texturePoints">The texture coordinates array (OpenCV format: top-left origin).</param>
        public void OnTextureSelectionStateChanged(GameObject touchedObject, TextureSelector.TextureSelectionState touchState, Vector2[] texturePoints)
        {
            // Only handle rectangle selection when tracking has not started
            if (!_isTrackingStarted)
            {
                switch (touchState)
                {
                    case TextureSelector.TextureSelectionState.RECTANGLE_SELECTION_STARTED:
                        // Pause when rectangle selection starts
                        _multiSourceToMatHelper.Pause();
                        break;

                    case TextureSelector.TextureSelectionState.RECTANGLE_SELECTION_CANCELLED:
                        // Resume playback when rectangle selection is cancelled
                        _multiSourceToMatHelper.Play();
                        break;

                    case TextureSelector.TextureSelectionState.RECTANGLE_SELECTION_COMPLETED:
                        // Set flag to initialize trackers in Update method
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

        private void InitializeTrackersWithRegion(Mat rgbMat, Rect region)
        {
            if (!_multiSourceToMatHelper.IsInitialized)
            {
                return;
            }

            if (rgbMat == null)
            {
                return;
            }

            ResetTrackers();

            // init() must be called once on a paused frame before tracker.update() each frame.
            if (TrackerKCFToggle.isOn)
            {
                TrackerKCF trackerKCF = TrackerKCF.create(new TrackerKCF_Params());
                trackerKCF.init(rgbMat, region);
                _trackers.Add(new TrackerSetting(trackerKCF, trackerKCF.GetType().Name.ToString(), new Scalar(255, 0, 0)));
            }

            if (TrackerCSRTToggle.isOn)
            {
                TrackerCSRT trackerCSRT = TrackerCSRT.create(new TrackerCSRT_Params());
                trackerCSRT.init(rgbMat, region);
                _trackers.Add(new TrackerSetting(trackerCSRT, trackerCSRT.GetType().Name.ToString(), new Scalar(0, 255, 0)));
            }

            if (TrackerMILToggle.isOn)
            {
                TrackerMIL trackerMIL = TrackerMIL.create(new TrackerMIL_Params());
                trackerMIL.init(rgbMat, region);
                _trackers.Add(new TrackerSetting(trackerMIL, trackerMIL.GetType().Name.ToString(), new Scalar(0, 0, 255)));
            }

            if (!_disableTrackerVit && TrackerVitToggle.isOn)
            {
                var @params = new TrackerVit_Params();
                @params.set_net(_vitModelFilepath);
                TrackerVit trackerVit = TrackerVit.create(@params);
                trackerVit.init(rgbMat, region);
                _trackers.Add(new TrackerSetting(trackerVit, trackerVit.GetType().Name.ToString(), new Scalar(255, 255, 0)));
            }

            if (!_disableTrackerDaSiamRPN && TrackerDaSiamRPNToggle.isOn)
            {
                var @params = new TrackerDaSiamRPN_Params();
                @params.set_model(_daSiamRpnModelFilepath);
                @params.set_kernel_r1(_daSiamRpnKernelR1Filepath);
                @params.set_kernel_cls1(_daSiamRpnKernelCls1Filepath);
                TrackerDaSiamRPN trackerDaSiamRPN = TrackerDaSiamRPN.create(@params);
                trackerDaSiamRPN.init(rgbMat, region);
                _trackers.Add(new TrackerSetting(trackerDaSiamRPN, trackerDaSiamRPN.GetType().Name.ToString(), new Scalar(255, 0, 255)));
            }

            if (!_disableTrackerNano && TrackerNanoToggle.isOn)
            {
                var @params = new TrackerNano_Params();
                @params.set_backbone(_nanotrackBackboneSimFilepath);
                @params.set_neckhead(_nanotrackHeadSimFilepath);
                TrackerNano trackerNano = TrackerNano.create(@params);
                trackerNano.init(rgbMat, region);
                _trackers.Add(new TrackerSetting(trackerNano, trackerNano.GetType().Name.ToString(), new Scalar(0, 255, 255)));
            }

            if (_trackers.Count > 0)
            {
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "";
                }

                new[] { TrackerKCFToggle, TrackerCSRTToggle, TrackerMILToggle }
                    .ToList().ForEach(toggle => { if (toggle) { toggle.interactable = false; } });

                if (!_disableTrackerVit && TrackerVitToggle)
                {
                    TrackerVitToggle.interactable = false;
                }

                if (!_disableTrackerDaSiamRPN && TrackerDaSiamRPNToggle)
                {
                    TrackerDaSiamRPNToggle.interactable = false;
                }

                if (!_disableTrackerNano && TrackerNanoToggle)
                {
                    TrackerNanoToggle.interactable = false;
                }
            }
        }

        private void CheckFilePaths()
        {
            if (string.IsNullOrEmpty(_vitModelFilepath))
            {
                Debug.LogError(VIT_MODEL_FILEPATH + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);

                TrackerVitToggle.isOn = TrackerVitToggle.interactable = false;
                _disableTrackerVit = true;
            }

            if (string.IsNullOrEmpty(_daSiamRpnModelFilepath) || string.IsNullOrEmpty(_daSiamRpnKernelR1Filepath) || string.IsNullOrEmpty(_daSiamRpnKernelCls1Filepath))
            {
                Debug.LogError(DASIAMRPN_MODEL_FILEPATH + " or " + DASIAMRPN_KERNEL_R1_FILEPATH + " or " + DASIAMRPN_KERNEL_CLS1_FILEPATH + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);

                TrackerDaSiamRPNToggle.isOn = TrackerDaSiamRPNToggle.interactable = false;
                _disableTrackerDaSiamRPN = true;
            }

            if (string.IsNullOrEmpty(_nanotrackBackboneSimFilepath) || string.IsNullOrEmpty(_nanotrackHeadSimFilepath))
            {
                Debug.LogError(NANOTRACK_BACKBONE_SIM_FILEPATH + " or " + NANOTRACK_HEAD_SIM_FILEPATH + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);

                TrackerNanoToggle.isOn = TrackerNanoToggle.interactable = false;
                _disableTrackerNano = true;
            }
        }

        private void ResetTrackers()
        {
            if (_trackers != null)
            {
                foreach (var t in _trackers)
                {
                    t.Dispose();
                }
                _trackers.Clear();
            }

            new[] { TrackerKCFToggle, TrackerCSRTToggle, TrackerMILToggle }
                .ToList().ForEach(toggle => { if (toggle) { toggle.interactable = true; } });

            if (!_disableTrackerVit && TrackerVitToggle)
            {
                TrackerVitToggle.interactable = true;
            }

            if (!_disableTrackerDaSiamRPN && TrackerDaSiamRPNToggle)
            {
                TrackerDaSiamRPNToggle.interactable = true;
            }

            if (!_disableTrackerNano && TrackerNanoToggle)
            {
                TrackerNanoToggle.interactable = true;
            }
        }

        private class TrackerSetting
        {
            public Tracker Tracker;
            public string Label;
            public Scalar LineColor;
            public Rect BoundingBox;

            public TrackerSetting(Tracker tracker, string label, Scalar lineColor)
            {
                Tracker = tracker;
                Label = label;
                LineColor = lineColor;
                BoundingBox = new Rect();
            }

            public void Dispose()
            {
                if (Tracker != null)
                {
                    Tracker.Dispose();
                    Tracker = null;
                }
            }
        }
    }
}
