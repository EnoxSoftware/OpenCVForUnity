using System.Collections.Generic;
using System.Linq;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.TrackingModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Legacy Tracking Example
    /// An example of object tracking using the TrackingModule.legacy_Tracker Class.
    /// http://docs.opencv.org/trunk/d5/d07/tutorial_multitracker.html
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class LegacyTrackingExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// VIDEO_FILENAME
        /// </summary>
        protected static readonly string VIDEO_FILENAME = "OpenCVForUnityExamples/768x576_mjpeg.mjpeg";

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
        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The trackers.
        /// </summary>
        private List<TrackerSetting> _trackers;

        /// <summary>
        /// The flag for requesting the start of the tracker initialization.
        /// </summary>
        private bool _shouldStartTrackerInitialization = false;

        /// <summary>
        /// The flag indicating that tracking has started.
        /// </summary>
        private bool _isTrackingStarted = false;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            if (string.IsNullOrEmpty(_multiSource2MatHelper.RequestedVideoFilePath))
                _multiSource2MatHelper.RequestedVideoFilePath = VIDEO_FILENAME;
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGB; // Tracking API must handle 3 channels Mat image.
            _multiSource2MatHelper.Initialize();
        }

        private void Update()
        {
            if (!_isTrackingStarted)
            {
                // Pre-tracking phase: handle rectangle selection only when tracking has not started
                if (_multiSource2MatHelper.IsPaused())
                {
                    Mat rgbMat = _multiSource2MatHelper.GetMat();

                    if (_shouldStartTrackerInitialization)
                    {
                        var (gameObject, currentSelectionState, currentSelectionPoints) = TextureRectangleSelector.GetSelectionStatus();
                        // Convert rectangle points to OpenCV Rect using TextureSelector utility method
                        // Note: currentSelectionPoints is guaranteed to have 2 elements (start and end points) when RECTANGLE_SELECTION_COMPLETED
                        var selectedRegion = TextureSelector.ConvertSelectionPointsToOpenCVRect(currentSelectionPoints);

                        // Initialize trackers with the selected region
                        InitializeTrackersWithRegion(rgbMat, selectedRegion);

                        // Set tracking started flag
                        _isTrackingStarted = true;

                        // Disable TextureRectangleSelector when tracking starts
                        TextureRectangleSelector.enabled = false;

                        // Resume playback after tracker initialization
                        _multiSource2MatHelper.Play();

                        _shouldStartTrackerInitialization = false;

                        Debug.Log("Tracker initialization completed");
                    }

                    // Draw current selection state on the Mat
                    TextureRectangleSelector.DrawSelection(rgbMat, true);

                    OpenCVMatUtils.MatToTexture2D(rgbMat, _texture);
                }
                else if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
                {
                    Mat rgbMat = _multiSource2MatHelper.GetMat();
                    OpenCVMatUtils.MatToTexture2D(rgbMat, _texture);
                }
            }
            else
            {
                // Post-tracking phase: handle tracker updates only when tracking has started
                if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
                {
                    Mat rgbMat = _multiSource2MatHelper.GetMat();

                    // update trackers.
                    for (int i = 0; i < _trackers.Count; i++)
                    {
                        legacy_Tracker tracker = _trackers[i].tracker;
                        string label = _trackers[i].label;
                        Scalar lineColor = _trackers[i].lineColor;
                        Rect2d boundingBox = _trackers[i].boundingBox;

                        tracker.update(rgbMat, boundingBox);

                        Imgproc.rectangle(rgbMat, boundingBox.tl(), boundingBox.br(), lineColor, 2, 1, 0);
                        Imgproc.putText(rgbMat, label, new Point(boundingBox.x, boundingBox.y - 5), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, lineColor, 1, Imgproc.LINE_AA, false);
                    }

                    OpenCVMatUtils.MatToTexture2D(rgbMat, _texture);
                }
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbMat = _multiSource2MatHelper.GetMat();

            _texture = new Texture2D(rgbMat.cols(), rgbMat.rows(), TextureFormat.RGB24, false);
            OpenCVMatUtils.MatToTexture2D(rgbMat, _texture);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Please select a rectangle region to start tracking.";
            }

            _trackers = new List<TrackerSetting>();

            _isTrackingStarted = false;

            // Enable TextureRectangleSelector when tracking stops
            TextureRectangleSelector.enabled = true;

            // Reset TextureRectangleSelector state
            TextureRectangleSelector.ResetSelectionStatus();
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;

            ResetTrackers();
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
                        _multiSource2MatHelper.Pause();
                        break;

                    case TextureSelector.TextureSelectionState.RECTANGLE_SELECTION_CANCELLED:
                        // Resume playback when rectangle selection is cancelled
                        _multiSource2MatHelper.Play();
                        break;

                    case TextureSelector.TextureSelectionState.RECTANGLE_SELECTION_COMPLETED:
                        // Set flag to initialize trackers in Update method
                        _shouldStartTrackerInitialization = true;
                        break;
                }
            }
        }

        // Private Methods
        private void InitializeTrackersWithRegion(Mat rgbMat, Rect region)
        {
            if (!_multiSource2MatHelper.IsInitialized())
                return;

            if (rgbMat == null)
                return;

            ResetTrackers();

            Rect2d region2d = new Rect2d(region.tl(), region.size());

            // init trackers.
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
                    .ToList().ForEach(toggle => { if (toggle) toggle.interactable = false; });
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
                .ToList().ForEach(toggle => { if (toggle) toggle.interactable = true; });
        }

        private class TrackerSetting
        {
            public legacy_Tracker tracker;
            public string label;
            public Scalar lineColor;
            public Rect2d boundingBox;

            public TrackerSetting(legacy_Tracker tracker, string label, Scalar lineColor)
            {
                this.tracker = tracker;
                this.label = label;
                this.lineColor = lineColor;
                this.boundingBox = new Rect2d();
            }

            public void Dispose()
            {
                if (tracker != null)
                {
                    tracker.Dispose();
                    tracker = null;
                }
            }
        }
    }
}
