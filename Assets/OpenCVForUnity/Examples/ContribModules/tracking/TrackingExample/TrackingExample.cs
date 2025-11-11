using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.TrackingModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.VideoModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Tracking Example
    /// An example of object tracking using the tracking (Tracking API) module.
    /// http://docs.opencv.org/trunk/d5/d07/tutorial_multitracker.html
    ///
    /// https://github.com/opencv/opencv_zoo/tree/main/models/object_tracking_vittrack
    /// https://github.com/opencv/opencv/blob/4.x/samples/dnn/dasiamrpn_tracker.cpp
    /// https://github.com/opencv/opencv/blob/4.x/samples/dnn/nanotrack_tracker.cpp
    ///
    /// [Tested Models]
    /// https://github.com/opencv/opencv_zoo/raw/80f7c6aa030a87b3f9e8ab7d84f62f13d308c10f/models/object_tracking_vittrack/object_tracking_vittrack_2023sep.onnx
    /// https://www.dropbox.com/s/rr1lk9355vzolqv/dasiamrpn_model.onnx?dl=1
    /// https://www.dropbox.com/s/999cqx5zrfi7w4p/dasiamrpn_kernel_r1.onnx?dl=1
    /// https://www.dropbox.com/s/qvmtszx5h339a0w/dasiamrpn_kernel_cls1.onnx?dl=1
    /// https://github.com/HonglinChu/SiamTrackers/raw/c2ff8479624b12ef2dcd830c47f2495a2c4852d4/NanoTrack/models/nanotrackv2/nanotrack_backbone_sim.onnx
    /// https://github.com/HonglinChu/SiamTrackers/raw/c2ff8479624b12ef2dcd830c47f2495a2c4852d4/NanoTrack/models/nanotrackv2/nanotrack_head_sim.onnx
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class TrackingExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// Vit_MODEL_FILENAME
        /// </summary>
        protected static readonly string Vit_MODEL_FILENAME = "OpenCVForUnityExamples/tracking/object_tracking_vittrack_2023sep.onnx";

        /// <summary>
        /// DaSiamRPN_MODEL_FILENAME
        /// </summary>
        protected static readonly string DaSiamRPN_MODEL_FILENAME = "OpenCVForUnityExamples/tracking/dasiamrpn_model.onnx";

        /// <summary>
        /// DaSiamRPN_KERNEL_R1_FILENAME
        /// </summary>
        protected static readonly string DaSiamRPN_KERNEL_R1_FILENAME = "OpenCVForUnityExamples/tracking/dasiamrpn_kernel_r1.onnx";

        /// <summary>
        /// DaSiamRPN_KERNEL_CLS1_FILENAME
        /// </summary>
        protected static readonly string DaSiamRPN_KERNEL_CLS1_FILENAME = "OpenCVForUnityExamples/tracking/dasiamrpn_kernel_cls1.onnx";

        /// <summary>
        /// NANOTRACK_BACKBONE_SIM_FILENAME
        /// </summary>
        protected static readonly string NANOTRACK_BACKBONE_SIM_FILENAME = "OpenCVForUnityExamples/tracking/nanotrack_backbone_sim.onnx";

        /// <summary>
        /// NANOTRACK_HEAD_SIM_FILENAME
        /// </summary>
        protected static readonly string NANOTRACK_HEAD_SIM_FILENAME = "OpenCVForUnityExamples/tracking/nanotrack_head_sim.onnx";

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
        /// <summary>
        /// The Vit model filepath.
        /// </summary>
        private string _vitModelFilepath;

        /// <summary>
        /// The DaSiamRPN model filepath.
        /// </summary>
        private string _daSiamRpnModelFilepath;

        /// <summary>
        /// The DaSiamRPN kernel_r1 filepath.
        /// </summary>
        private string _daSiamRpnKernelR1Filepath;

        /// <summary>
        /// The DaSiamRPN kernel_cls1 filepath.
        /// </summary>
        private string _daSiamRpnKernelCls1Filepath;

        /// <summary>
        /// The NANOTRACK_backbone_sim filepath.
        /// </summary>
        private string _nanotrackBackboneSimFilepath;

        /// <summary>
        /// The NANOTRACK_head_sim filepath.
        /// </summary>
        private string _nanotrackHeadSimFilepath;

        private bool _disableTrackerVit = false;

        private bool _disableTrackerDaSiamRPN = false;

        private bool _disableTrackerNano = false;

        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The flag for requesting the start of the tracker initialization.
        /// </summary>
        private bool _shouldStartTrackerInitialization = false;

        /// <summary>
        /// The flag indicating that tracking has started.
        /// </summary>
        private bool _isTrackingStarted = false;

        /// <summary>
        /// The trackers.
        /// </summary>
        private List<TrackerSetting> _trackers;



        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
#if UNITY_WSA_10_0
        private void Start()
#else
        private async void Start()
#endif
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();

#if UNITY_WSA_10_0

            // Disable the DNN module-dependent Tracker on UWP platforms, as it cannot be used.
            TrackerVitToggle.isOn = TrackerVitToggle.interactable = false;
            _disableTrackerVit = true;
            TrackerDaSiamRPNToggle.isOn = TrackerDaSiamRPNToggle.interactable = false;
            _disableTrackerDaSiamRPN = true;
            TrackerNanoToggle.isOn = TrackerNanoToggle.interactable = false;
            _disableTrackerNano = true;
            Run();

#else

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _vitModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(Vit_MODEL_FILENAME, cancellationToken: _cts.Token);
            _daSiamRpnModelFilepath = await OpenCVEnv.GetFilePathTaskAsync(DaSiamRPN_MODEL_FILENAME, cancellationToken: _cts.Token);
            _daSiamRpnKernelR1Filepath = await OpenCVEnv.GetFilePathTaskAsync(DaSiamRPN_KERNEL_R1_FILENAME, cancellationToken: _cts.Token);
            _daSiamRpnKernelCls1Filepath = await OpenCVEnv.GetFilePathTaskAsync(DaSiamRPN_KERNEL_CLS1_FILENAME, cancellationToken: _cts.Token);
            _nanotrackBackboneSimFilepath = await OpenCVEnv.GetFilePathTaskAsync(NANOTRACK_BACKBONE_SIM_FILENAME, cancellationToken: _cts.Token);
            _nanotrackHeadSimFilepath = await OpenCVEnv.GetFilePathTaskAsync(NANOTRACK_HEAD_SIM_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            CheckFilePaths();
            Run();
#endif
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
                        Tracker tracker = _trackers[i].tracker;
                        string label = _trackers[i].label;
                        Scalar lineColor = _trackers[i].lineColor;
                        Rect boundingBox = _trackers[i].boundingBox;

                        tracker.update(rgbMat, boundingBox);

                        Imgproc.rectangle(rgbMat, boundingBox.tl(), boundingBox.br(), lineColor, 2, 1, 0);

                        //  vit tracker provides confidence values during the tracking process, which can be used to determine if the tracking is currently lost.
                        if (_trackers[i].tracker is TrackerVit)
                        {
                            TrackerVit trackerVit = (TrackerVit)_trackers[i].tracker;
                            float score = trackerVit.getTrackingScore();
                            if (score < 0.4f)
                            {
                                Imgproc.putText(rgbMat, label + " " + String.Format("{0:0.00}", score), new Point(boundingBox.x, boundingBox.y - 5), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 0, 0, 255), 1, Imgproc.LINE_AA, false);
                            }
                            else
                            {
                                Imgproc.putText(rgbMat, label + " " + String.Format("{0:0.00}", score), new Point(boundingBox.x, boundingBox.y - 5), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, lineColor, 1, Imgproc.LINE_AA, false);
                            }
                        }
                        else
                        {
                            Imgproc.putText(rgbMat, label, new Point(boundingBox.x, boundingBox.y - 5), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, lineColor, 1, Imgproc.LINE_AA, false);
                        }
                    }



                    OpenCVMatUtils.MatToTexture2D(rgbMat, _texture);
                }
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            _cts?.Dispose();
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

            // init trackers.
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
                var _params = new TrackerVit_Params();
                _params.set_net(_vitModelFilepath);
                TrackerVit TrackerVit = TrackerVit.create(_params);
                TrackerVit.init(rgbMat, region);
                _trackers.Add(new TrackerSetting(TrackerVit, TrackerVit.GetType().Name.ToString(), new Scalar(255, 255, 0)));
            }

            if (!_disableTrackerDaSiamRPN && TrackerDaSiamRPNToggle.isOn)
            {
                var _params = new TrackerDaSiamRPN_Params();
                _params.set_model(_daSiamRpnModelFilepath);
                _params.set_kernel_r1(_daSiamRpnKernelR1Filepath);
                _params.set_kernel_cls1(_daSiamRpnKernelCls1Filepath);
                TrackerDaSiamRPN trackerDaSiamRPN = TrackerDaSiamRPN.create(_params);
                trackerDaSiamRPN.init(rgbMat, region);
                _trackers.Add(new TrackerSetting(trackerDaSiamRPN, trackerDaSiamRPN.GetType().Name.ToString(), new Scalar(255, 0, 255)));
            }

            if (!_disableTrackerNano && TrackerNanoToggle.isOn)
            {
                var _params = new TrackerNano_Params();
                _params.set_backbone(_nanotrackBackboneSimFilepath);
                _params.set_neckhead(_nanotrackHeadSimFilepath);
                TrackerNano trackerNano = TrackerNano.create(_params);
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
                    .ToList().ForEach(toggle => { if (toggle) toggle.interactable = false; });

                if (!_disableTrackerVit && TrackerVitToggle)
                    TrackerVitToggle.interactable = false;

                if (!_disableTrackerDaSiamRPN && TrackerDaSiamRPNToggle)
                    TrackerDaSiamRPNToggle.interactable = false;

                if (!_disableTrackerNano && TrackerNanoToggle)
                    TrackerNanoToggle.interactable = false;
            }
        }

        private void CheckFilePaths()
        {
            if (string.IsNullOrEmpty(_vitModelFilepath))
            {
                Debug.LogError(Vit_MODEL_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");

                TrackerVitToggle.isOn = TrackerVitToggle.interactable = false;
                _disableTrackerVit = true;
            }

            if (string.IsNullOrEmpty(_daSiamRpnModelFilepath) || string.IsNullOrEmpty(_daSiamRpnKernelR1Filepath) || string.IsNullOrEmpty(_daSiamRpnKernelCls1Filepath))
            {
                Debug.LogError(DaSiamRPN_MODEL_FILENAME + " or " + DaSiamRPN_KERNEL_R1_FILENAME + " or " + DaSiamRPN_KERNEL_CLS1_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");

                TrackerDaSiamRPNToggle.isOn = TrackerDaSiamRPNToggle.interactable = false;
                _disableTrackerDaSiamRPN = true;
            }

            if (string.IsNullOrEmpty(_nanotrackBackboneSimFilepath) || string.IsNullOrEmpty(_nanotrackHeadSimFilepath))
            {
                Debug.LogError(NANOTRACK_BACKBONE_SIM_FILENAME + " or " + NANOTRACK_HEAD_SIM_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");

                TrackerNanoToggle.isOn = TrackerNanoToggle.interactable = false;
                _disableTrackerNano = true;
            }
        }

        private void Run()
        {
            if (string.IsNullOrEmpty(_multiSource2MatHelper.RequestedVideoFilePath))
                _multiSource2MatHelper.RequestedVideoFilePath = VIDEO_FILENAME;
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGB; // Tracking API must handle 3 channels Mat image.
            _multiSource2MatHelper.Initialize();
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
                .ToList().ForEach(toggle => { if (toggle) toggle.interactable = true; });

            if (!_disableTrackerVit && TrackerVitToggle)
                TrackerVitToggle.interactable = true;

            if (!_disableTrackerDaSiamRPN && TrackerDaSiamRPNToggle)
                TrackerDaSiamRPNToggle.interactable = true;

            if (!_disableTrackerNano && TrackerNanoToggle)
                TrackerNanoToggle.interactable = true;
        }



        private class TrackerSetting
        {
            public Tracker tracker;
            public string label;
            public Scalar lineColor;
            public Rect boundingBox;

            public TrackerSetting(Tracker tracker, string label, Scalar lineColor)
            {
                this.tracker = tracker;
                this.label = label;
                this.lineColor = lineColor;
                this.boundingBox = new Rect();
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
