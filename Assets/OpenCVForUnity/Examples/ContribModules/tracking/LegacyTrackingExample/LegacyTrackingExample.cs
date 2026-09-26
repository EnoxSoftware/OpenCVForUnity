using System;
using System.Collections.Generic;
using System.Linq;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.TrackingModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Interaction;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Legacy Tracking Example
    /// Tracks a user-selected rectangle region using the deprecated legacy_Tracker API.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Pausing video to draw a ROI, then initializing one or more legacy trackers
    /// - Side-by-side comparison of Boosting, CSRT, KCF, MedianFlow, MIL, MOSSE, and TLD
    /// - Per-frame bounding-box update with Rect2d (double-precision) output
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Scalar"/>, <see cref="Point"/>, <see cref="Rect"/>, <see cref="Rect2d"/>
    /// - <see cref="legacy_Tracker"/>, <see cref="legacy_TrackerBoosting"/>, <see cref="legacy_TrackerCSRT"/>, <see cref="legacy_TrackerKCF"/>, <see cref="legacy_TrackerMedianFlow"/>, <see cref="legacy_TrackerMIL"/>, <see cref="legacy_TrackerMOSSE"/>, <see cref="legacy_TrackerTLD"/>
    /// - <see cref="Imgproc"/>: rectangle, putText
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// http://docs.opencv.org/trunk/d5/d07/tutorial_multitracker.html
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class LegacyTrackingExample : MonoBehaviour
    {
        // Constants
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
        /// The trackerBoosting Toggle.
        /// </summary>
        public Toggle TrackerBoostingToggle;

        /// <summary>
        /// The trackerCSRT Toggle.
        /// </summary>
        public Toggle TrackerCSRTToggle;

        /// <summary>
        /// The trackerKCF Toggle.
        /// </summary>
        public Toggle TrackerKCFToggle;

        /// <summary>
        /// The trackerMedianFlow Toggle.
        /// </summary>
        public Toggle TrackerMedianFlowToggle;

        /// <summary>
        /// The trackerMIL Toggle.
        /// </summary>
        public Toggle TrackerMILToggle;

        /// <summary>
        /// The trackerMOSSE Toggle.
        /// </summary>
        public Toggle TrackerMOSSEToggle;

        /// <summary>
        /// The trackerTLD Toggle.
        /// </summary>
        public Toggle TrackerTLDToggle;

        // Private Fields
        private Texture2D _texture;
        private Mat _overlayMat;
        private List<TrackerSetting> _trackers;
        private bool _shouldStartTrackerInitialization = false;
        private bool _isTrackingStarted = false;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGB; // Tracking API requires a 3-channel BGR/RGB Mat.

            WireSourceToMatControlPanelHooks();

            if (string.IsNullOrEmpty(_multiSourceToMatHelper.PerKindSettings.VideoCapture.RequestedVideoFilePath))
            {
                _multiSourceToMatHelper.PerKindSettings.VideoCapture.RequestedVideoFilePath = VIDEO_FILEPATH;
            }

            _multiSourceToMatHelper.Initialize();
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

                        // Each enabled toggle creates a separate legacy tracker on the same ROI.
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

                    // legacy_Tracker.update() modifies Rect2d boundingBox in place.
                    for (int i = 0; i < _trackers.Count; i++)
                    {
                        legacy_Tracker tracker = _trackers[i].Tracker;
                        string label = _trackers[i].Label;
                        Scalar lineColor = _trackers[i].LineColor;
                        Rect2d boundingBox = _trackers[i].BoundingBox;

                        tracker.update(rgbMat, boundingBox);

                        Imgproc.rectangle(rgbMat, boundingBox.tl(), boundingBox.br(), lineColor, 2, 1, 0);
                        Imgproc.putText(rgbMat, label, new Point(boundingBox.x, boundingBox.y - 5), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, lineColor, 1, Imgproc.LINE_AA, false);
                    }

                    OpenCVMatUnityUtils.MatToTexture2D(rgbMat, _texture);
                }
            }
        }

        private void OnDestroy()
        {
            UnwireSourceToMatControlPanelHooks();
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

            // Legacy API uses Rect2d instead of Rect for init/update.
            Rect2d region2d = new Rect2d(region.tl(), region.size());

            // init() must be called once on a paused frame before tracker.update() each frame.
            if (TrackerBoostingToggle.isOn)
            {
                legacy_TrackerBoosting trackerBoosting = legacy_TrackerBoosting.create();
                trackerBoosting.init(rgbMat, region2d);
                _trackers.Add(new TrackerSetting(trackerBoosting, trackerBoosting.GetType().Name.ToString(), new Scalar(255, 0, 0)));
            }

            if (TrackerCSRTToggle.isOn)
            {
                legacy_TrackerCSRT trackerCSRT = legacy_TrackerCSRT.create();
                trackerCSRT.init(rgbMat, region2d);
                _trackers.Add(new TrackerSetting(trackerCSRT, trackerCSRT.GetType().Name.ToString(), new Scalar(0, 255, 0)));
            }

            if (TrackerKCFToggle.isOn)
            {
                legacy_TrackerKCF trackerKCF = legacy_TrackerKCF.create();
                trackerKCF.init(rgbMat, region2d);
                _trackers.Add(new TrackerSetting(trackerKCF, trackerKCF.GetType().Name.ToString(), new Scalar(0, 0, 255)));
            }

            if (TrackerMedianFlowToggle.isOn)
            {
                legacy_TrackerMedianFlow trackerMedianFlow = legacy_TrackerMedianFlow.create();
                trackerMedianFlow.init(rgbMat, region2d);
                _trackers.Add(new TrackerSetting(trackerMedianFlow, trackerMedianFlow.GetType().Name.ToString(), new Scalar(255, 255, 0)));
            }

            if (TrackerMILToggle.isOn)
            {
                legacy_TrackerMIL trackerMIL = legacy_TrackerMIL.create();
                trackerMIL.init(rgbMat, region2d);
                _trackers.Add(new TrackerSetting(trackerMIL, trackerMIL.GetType().Name.ToString(), new Scalar(255, 0, 255)));
            }

            if (TrackerMOSSEToggle.isOn)
            {
                legacy_TrackerMOSSE trackerMOSSE = legacy_TrackerMOSSE.create();
                trackerMOSSE.init(rgbMat, region2d);
                _trackers.Add(new TrackerSetting(trackerMOSSE, trackerMOSSE.GetType().Name.ToString(), new Scalar(0, 255, 255)));
            }

            if (TrackerTLDToggle.isOn)
            {
                legacy_TrackerTLD trackerTLD = legacy_TrackerTLD.create();
                trackerTLD.init(rgbMat, region2d);
                _trackers.Add(new TrackerSetting(trackerTLD, trackerTLD.GetType().Name.ToString(), new Scalar(255, 255, 255)));
            }

            if (_trackers.Count > 0)
            {
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "";
                }

                new[] { TrackerBoostingToggle, TrackerCSRTToggle, TrackerKCFToggle, TrackerMedianFlowToggle, TrackerMILToggle, TrackerMOSSEToggle, TrackerTLDToggle }
                    .ToList().ForEach(toggle => { if (toggle) { toggle.interactable = false; } });
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

            new[] { TrackerBoostingToggle, TrackerCSRTToggle, TrackerKCFToggle, TrackerMedianFlowToggle, TrackerMILToggle, TrackerMOSSEToggle, TrackerTLDToggle }
                .ToList().ForEach(toggle => { if (toggle) { toggle.interactable = true; } });
        }

        private class TrackerSetting
        {
            public legacy_Tracker Tracker;
            public string Label;
            public Scalar LineColor;
            public Rect2d BoundingBox;

            public TrackerSetting(legacy_Tracker tracker, string label, Scalar lineColor)
            {
                Tracker = tracker;
                Label = label;
                LineColor = lineColor;
                BoundingBox = new Rect2d();
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
