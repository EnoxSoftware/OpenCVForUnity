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
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif
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
        /// The trackers.
        /// </summary>
        private List<TrackerSetting> _trackers;

        /// <summary>
        /// The selected point list.
        /// </summary>
        private List<Point> _selectedPointList;

        /// <summary>
        /// The stored touch point.
        /// </summary>
        private Point _storedTouchPoint;

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
            if (!_multiSource2MatHelper.IsInitialized())
                return;

#if ENABLE_INPUT_SYSTEM
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            // Touch input for mobile platforms
            if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count == 1)
            {
                foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
                {
                    if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended)
                    {
                        if (!EventSystem.current.IsPointerOverGameObject(touch.finger.index))
                        {
                            _storedTouchPoint = new Point(touch.screenPosition.x, touch.screenPosition.y);
                            // Debug.Log("touch X " + touch.screenPosition.x);
                            // Debug.Log("touch Y " + touch.screenPosition.y);
                        }
                    }
                }
            }
#else
            // Mouse input for non-mobile platforms
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasReleasedThisFrame)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                _storedTouchPoint = new Point(mouse.position.ReadValue().x, mouse.position.ReadValue().y);
            }
#endif
#else
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            //Touch
            int touchCount = Input.touchCount;
            if (touchCount == 1)
            {
                Touch t = Input.GetTouch(0);
                if(t.phase == TouchPhase.Ended && !EventSystem.current.IsPointerOverGameObject (t.fingerId)) {
                    _storedTouchPoint = new Point (t.position.x, t.position.y);
                    //Debug.Log ("touch X " + t.position.x);
                    //Debug.Log ("touch Y " + t.position.y);
                }
            }
#else
            //Mouse
            if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                _storedTouchPoint = new Point(Input.mousePosition.x, Input.mousePosition.y);
                //Debug.Log ("mouse X " + Input.mousePosition.x);
                //Debug.Log ("mouse Y " + Input.mousePosition.y);
            }
#endif
#endif

            if (_selectedPointList.Count != 1)
            {
                if (!_multiSource2MatHelper.IsPlaying())
                    _multiSource2MatHelper.Play();

                if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
                {
                    Mat rgbMat = _multiSource2MatHelper.GetMat();

                    if (_storedTouchPoint != null)
                    {
                        ConvertScreenPointToTexturePoint(_storedTouchPoint, _storedTouchPoint, gameObject, _texture.width, _texture.height);
                        OnTouch(_storedTouchPoint, _texture.width, _texture.height);
                        _storedTouchPoint = null;
                    }

                    if (_selectedPointList.Count == 1)
                    {
                        foreach (var point in _selectedPointList)
                        {
                            Imgproc.circle(rgbMat, point, 6, new Scalar(0, 0, 255), 2);
                        }
                    }
                    else if (_selectedPointList.Count == 2)
                    {
                        ResetTrackers();

                        using (MatOfPoint selectedPointMat = new MatOfPoint(_selectedPointList.ToArray()))
                        {
                            Rect region = Imgproc.boundingRect(selectedPointMat);

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
                        }

                        _selectedPointList.Clear();

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

                    if (_trackers.Count == 0)
                    {
                        if (_selectedPointList.Count != 1)
                        {
                            //Imgproc.putText (rgbMat, "Please touch the screen, and select tracking regions.", new Point (5, rgbMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                            if (_fpsMonitor != null)
                            {
                                _fpsMonitor.ConsoleText = "Please touch the screen, and select tracking regions.";
                            }
                        }
                        else
                        {
                            //Imgproc.putText (rgbMat, "Please select the end point of the new tracking region.", new Point (5, rgbMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                            if (_fpsMonitor != null)
                            {
                                _fpsMonitor.ConsoleText = "Please select the end point of the new tracking region.";
                            }
                        }
                    }

                    OpenCVMatUtils.MatToTexture2D(rgbMat, _texture);
                }
            }
            else
            {
                if (_multiSource2MatHelper.IsPlaying())
                    _multiSource2MatHelper.Pause();

                if (_storedTouchPoint != null)
                {
                    ConvertScreenPointToTexturePoint(_storedTouchPoint, _storedTouchPoint, gameObject, _texture.width, _texture.height);
                    OnTouch(_storedTouchPoint, _texture.width, _texture.height);
                    _storedTouchPoint = null;
                }
            }
        }

#if ENABLE_INPUT_SYSTEM
        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }
#endif

        private void OnDisable()
        {
            _cts?.Dispose();

#if ENABLE_INPUT_SYSTEM
            EnhancedTouchSupport.Disable();
#endif
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

            // Set the Texture2D as the main texture of the Renderer component attached to the game object
            gameObject.GetComponent<Renderer>().material.mainTexture = _texture;

            // Adjust the scale of the game object to match the dimensions of the texture
            gameObject.transform.localScale = new Vector3(rgbMat.cols(), rgbMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            // Adjust the orthographic size of the main Camera to fit the aspect ratio of the image
            float width = rgbMat.width();
            float height = rgbMat.height();
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }


            _trackers = new List<TrackerSetting>();

            _selectedPointList = new List<Point>();
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

            _selectedPointList.Clear();
        }

        // Private Methods
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

        private void OnTouch(Point touchPoint, int textureWidth = -1, int textureHeight = -1)
        {
            if (_selectedPointList.Count < 2)
            {
                _selectedPointList.Add(touchPoint);
                if (!new Rect(0, 0, textureWidth, textureHeight).contains(_selectedPointList[_selectedPointList.Count - 1]))
                {
                    _selectedPointList.RemoveAt(_selectedPointList.Count - 1);
                }
            }
        }

        /// <summary>
        /// Converts the screen point to texture point.
        /// </summary>
        /// <param name="screenPoint">Screen point.</param>
        /// <param name="dstPoint">Dst point.</param>
        /// <param name="texturQuad">Texture quad.</param>
        /// <param name="textureWidth">Texture width.</param>
        /// <param name="textureHeight">Texture height.</param>
        /// <param name="camera">Camera.</param>
        private void ConvertScreenPointToTexturePoint(Point screenPoint, Point dstPoint, GameObject textureQuad, int textureWidth = -1, int textureHeight = -1, Camera camera = null)
        {
            if (textureWidth < 0 || textureHeight < 0)
            {
                Renderer r = textureQuad.GetComponent<Renderer>();
                if (r != null && r.material != null && r.material.mainTexture != null)
                {
                    textureWidth = r.material.mainTexture.width;
                    textureHeight = r.material.mainTexture.height;
                }
                else
                {
                    textureWidth = (int)textureQuad.transform.localScale.x;
                    textureHeight = (int)textureQuad.transform.localScale.y;
                }
            }

            if (camera == null)
                camera = Camera.main;

            Vector3 quadPosition = textureQuad.transform.localPosition;
            Vector3 quadScale = textureQuad.transform.localScale;

            Vector2 tl = camera.WorldToScreenPoint(new Vector3(quadPosition.x - quadScale.x / 2, quadPosition.y + quadScale.y / 2, quadPosition.z));
            Vector2 tr = camera.WorldToScreenPoint(new Vector3(quadPosition.x + quadScale.x / 2, quadPosition.y + quadScale.y / 2, quadPosition.z));
            Vector2 br = camera.WorldToScreenPoint(new Vector3(quadPosition.x + quadScale.x / 2, quadPosition.y - quadScale.y / 2, quadPosition.z));
            Vector2 bl = camera.WorldToScreenPoint(new Vector3(quadPosition.x - quadScale.x / 2, quadPosition.y - quadScale.y / 2, quadPosition.z));

            using (Mat srcRectMat = new Mat(4, 1, CvType.CV_32FC2))
            using (Mat dstRectMat = new Mat(4, 1, CvType.CV_32FC2))
            {
                srcRectMat.put(0, 0, tl.x, tl.y, tr.x, tr.y, br.x, br.y, bl.x, bl.y);
                dstRectMat.put(0, 0, 0, 0, quadScale.x, 0, quadScale.x, quadScale.y, 0, quadScale.y);

                using (Mat perspectiveTransform = Imgproc.getPerspectiveTransform(srcRectMat, dstRectMat))
                using (MatOfPoint2f srcPointMat = new MatOfPoint2f(screenPoint))
                using (MatOfPoint2f dstPointMat = new MatOfPoint2f())
                {
                    Core.perspectiveTransform(srcPointMat, dstPointMat, perspectiveTransform);

                    dstPoint.x = dstPointMat.get(0, 0)[0] * textureWidth / quadScale.x;
                    dstPoint.y = dstPointMat.get(0, 0)[1] * textureHeight / quadScale.y;
                }
            }
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
