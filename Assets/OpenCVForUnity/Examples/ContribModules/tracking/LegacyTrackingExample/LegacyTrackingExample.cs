using System.Collections.Generic;
using System.Linq;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.TrackingModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
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

#if ENABLE_INPUT_SYSTEM
        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }
#endif

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
                }
            }
#else
            //Mouse
            if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                _storedTouchPoint = new Point(Input.mousePosition.x, Input.mousePosition.y);
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

                            Rect _region = Imgproc.boundingRect(selectedPointMat);
                            Rect2d region = new Rect2d(_region.tl(), _region.size());

                            // init trackers.
                            if (TrackerBoostingToggle.isOn)
                            {
                                legacy_TrackerBoosting trackerBoosting = legacy_TrackerBoosting.create();
                                trackerBoosting.init(rgbMat, region);
                                _trackers.Add(new TrackerSetting(trackerBoosting, trackerBoosting.GetType().Name.ToString(), new Scalar(255, 0, 0)));
                            }

                            if (TrackerCSRTToggle.isOn)
                            {
                                legacy_TrackerCSRT trackerCSRT = legacy_TrackerCSRT.create();
                                trackerCSRT.init(rgbMat, region);
                                _trackers.Add(new TrackerSetting(trackerCSRT, trackerCSRT.GetType().Name.ToString(), new Scalar(0, 255, 0)));
                            }

                            if (TrackerKCFToggle.isOn)
                            {
                                legacy_TrackerKCF trackerKCF = legacy_TrackerKCF.create();
                                trackerKCF.init(rgbMat, region);
                                _trackers.Add(new TrackerSetting(trackerKCF, trackerKCF.GetType().Name.ToString(), new Scalar(0, 0, 255)));
                            }

                            if (TrackerMedianFlowToggle.isOn)
                            {
                                legacy_TrackerMedianFlow trackerMedianFlow = legacy_TrackerMedianFlow.create();
                                trackerMedianFlow.init(rgbMat, region);
                                _trackers.Add(new TrackerSetting(trackerMedianFlow, trackerMedianFlow.GetType().Name.ToString(), new Scalar(255, 255, 0)));
                            }

                            if (TrackerMILToggle.isOn)
                            {
                                legacy_TrackerMIL trackerMIL = legacy_TrackerMIL.create();
                                trackerMIL.init(rgbMat, region);
                                _trackers.Add(new TrackerSetting(trackerMIL, trackerMIL.GetType().Name.ToString(), new Scalar(255, 0, 255)));
                            }

                            if (TrackerMOSSEToggle.isOn)
                            {
                                legacy_TrackerMOSSE trackerMOSSE = legacy_TrackerMOSSE.create();
                                trackerMOSSE.init(rgbMat, region);
                                _trackers.Add(new TrackerSetting(trackerMOSSE, trackerMOSSE.GetType().Name.ToString(), new Scalar(0, 255, 255)));
                            }

                            if (TrackerTLDToggle.isOn)
                            {
                                legacy_TrackerTLD trackerTLD = legacy_TrackerTLD.create();
                                trackerTLD.init(rgbMat, region);
                                _trackers.Add(new TrackerSetting(trackerTLD, trackerTLD.GetType().Name.ToString(), new Scalar(255, 255, 255)));
                            }
                        }

                        _selectedPointList.Clear();

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
