using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.TrackingModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [RequireComponent(typeof(VideoCaptureToMatHelper))]
    public class LegacyTrackingExample : MonoBehaviour
    {
        /// <summary>
        /// The trackerBoosting Toggle.
        /// </summary>
        public Toggle trackerBoostingToggle;

        /// <summary>
        /// The trackerCSRT Toggle.
        /// </summary>
        public Toggle trackerCSRTToggle;

        /// <summary>
        /// The trackerKCF Toggle.
        /// </summary>
        public Toggle trackerKCFToggle;

        /// <summary>
        /// The trackerMedianFlow Toggle.
        /// </summary>
        public Toggle trackerMedianFlowToggle;

        /// <summary>
        /// The trackerMIL Toggle.
        /// </summary>
        public Toggle trackerMILToggle;

        /// <summary>
        /// The trackerMOSSE Toggle.
        /// </summary>
        public Toggle trackerMOSSEToggle;

        /// <summary>
        /// The trackerTLD Toggle.
        /// </summary>
        public Toggle trackerTLDToggle;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The trackers.
        /// </summary>
        List<TrackerSetting> trackers;

        /// <summary>
        /// The selected point list.
        /// </summary>
        List<Point> selectedPointList;

        /// <summary>
        /// The stored touch point.
        /// </summary>
        Point storedTouchPoint;

        /// <summary>
        /// The video capture to mat helper.
        /// </summary>
        VideoCaptureToMatHelper sourceToMatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// VIDEO_FILENAME
        /// </summary>
        protected static readonly string VIDEO_FILENAME = "OpenCVForUnity/768x576_mjpeg.mjpeg";

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            sourceToMatHelper = gameObject.GetComponent<VideoCaptureToMatHelper>();
            if (string.IsNullOrEmpty(sourceToMatHelper.requestedVideoFilePath))
                sourceToMatHelper.requestedVideoFilePath = VIDEO_FILENAME;
            sourceToMatHelper.outputColorFormat = VideoCaptureToMatHelper.ColorFormat.RGB; // Tracking API must handle 3 channels Mat image.
            sourceToMatHelper.Initialize();
        }

        /// <summary>
        /// Raises the video capture to mat helper initialized event.
        /// </summary>
        public void OnVideoCaptureToMatHelperInitialized()
        {
            Debug.Log("OnVideoCaptureToMatHelperInitialized");

            Mat rgbMat = sourceToMatHelper.GetMat();

            texture = new Texture2D(rgbMat.cols(), rgbMat.rows(), TextureFormat.RGB24, false);
            Utils.matToTexture2D(rgbMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(rgbMat.cols(), rgbMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


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


            trackers = new List<TrackerSetting>();

            selectedPointList = new List<Point>();
        }

        /// <summary>
        /// Raises the video capture to mat helper disposed event.
        /// </summary>
        public void OnVideoCaptureToMatHelperDisposed()
        {
            Debug.Log("OnVideoCaptureToMatHelperDisposed");

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

            ResetTrackers();
        }

        /// <summary>
        /// Raises the video capture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnVideoCaptureToMatHelperErrorOccurred(VideoCaptureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnVideoCaptureToMatHelperErrorOccurred " + errorCode);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!sourceToMatHelper.IsInitialized())
                return;

#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            //Touch
            int touchCount = Input.touchCount;
            if (touchCount == 1)
            {
                Touch t = Input.GetTouch(0);
                if(t.phase == TouchPhase.Ended && !EventSystem.current.IsPointerOverGameObject (t.fingerId)) {
                    storedTouchPoint = new Point (t.position.x, t.position.y);
                    //Debug.Log ("touch X " + t.position.x);
                    //Debug.Log ("touch Y " + t.position.y);
                }
            }
#else
            //Mouse
            if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                storedTouchPoint = new Point(Input.mousePosition.x, Input.mousePosition.y);
                //Debug.Log ("mouse X " + Input.mousePosition.x);
                //Debug.Log ("mouse Y " + Input.mousePosition.y);
            }
#endif

            if (selectedPointList.Count != 1)
            {
                if (!sourceToMatHelper.IsPlaying())
                    sourceToMatHelper.Play();

                if (sourceToMatHelper.IsPlaying() && sourceToMatHelper.DidUpdateThisFrame())
                {
                    Mat rgbMat = sourceToMatHelper.GetMat();

                    if (storedTouchPoint != null)
                    {
                        ConvertScreenPointToTexturePoint(storedTouchPoint, storedTouchPoint, gameObject, texture.width, texture.height);
                        OnTouch(storedTouchPoint, texture.width, texture.height);
                        storedTouchPoint = null;
                    }

                    if (selectedPointList.Count == 1)
                    {
                        foreach (var point in selectedPointList)
                        {
                            Imgproc.circle(rgbMat, point, 6, new Scalar(0, 0, 255), 2);
                        }
                    }
                    else if (selectedPointList.Count == 2)
                    {
                        ResetTrackers();

                        using (MatOfPoint selectedPointMat = new MatOfPoint(selectedPointList.ToArray()))
                        {

                            Rect _region = Imgproc.boundingRect(selectedPointMat);
                            Rect2d region = new Rect2d(_region.tl(), _region.size());

                            // init trackers.
                            if (trackerBoostingToggle.isOn)
                            {
                                legacy_TrackerBoosting trackerBoosting = legacy_TrackerBoosting.create();
                                trackerBoosting.init(rgbMat, region);
                                trackers.Add(new TrackerSetting(trackerBoosting, trackerBoosting.GetType().Name.ToString(), new Scalar(255, 0, 0)));
                            }

                            if (trackerCSRTToggle.isOn)
                            {
                                legacy_TrackerCSRT trackerCSRT = legacy_TrackerCSRT.create();
                                trackerCSRT.init(rgbMat, region);
                                trackers.Add(new TrackerSetting(trackerCSRT, trackerCSRT.GetType().Name.ToString(), new Scalar(0, 255, 0)));
                            }

                            if (trackerKCFToggle.isOn)
                            {
                                legacy_TrackerKCF trackerKCF = legacy_TrackerKCF.create();
                                trackerKCF.init(rgbMat, region);
                                trackers.Add(new TrackerSetting(trackerKCF, trackerKCF.GetType().Name.ToString(), new Scalar(0, 0, 255)));
                            }

                            if (trackerMedianFlowToggle.isOn)
                            {
                                legacy_TrackerMedianFlow trackerMedianFlow = legacy_TrackerMedianFlow.create();
                                trackerMedianFlow.init(rgbMat, region);
                                trackers.Add(new TrackerSetting(trackerMedianFlow, trackerMedianFlow.GetType().Name.ToString(), new Scalar(255, 255, 0)));
                            }

                            if (trackerMILToggle.isOn)
                            {
                                legacy_TrackerMIL trackerMIL = legacy_TrackerMIL.create();
                                trackerMIL.init(rgbMat, region);
                                trackers.Add(new TrackerSetting(trackerMIL, trackerMIL.GetType().Name.ToString(), new Scalar(255, 0, 255)));
                            }

                            if (trackerMOSSEToggle.isOn)
                            {
                                legacy_TrackerMOSSE trackerMOSSE = legacy_TrackerMOSSE.create();
                                trackerMOSSE.init(rgbMat, region);
                                trackers.Add(new TrackerSetting(trackerMOSSE, trackerMOSSE.GetType().Name.ToString(), new Scalar(0, 255, 255)));
                            }

                            if (trackerTLDToggle.isOn)
                            {
                                legacy_TrackerTLD trackerTLD = legacy_TrackerTLD.create();
                                trackerTLD.init(rgbMat, region);
                                trackers.Add(new TrackerSetting(trackerTLD, trackerTLD.GetType().Name.ToString(), new Scalar(255, 255, 255)));
                            }
                        }

                        selectedPointList.Clear();

                        if (trackers.Count > 0)
                        {
                            if (fpsMonitor != null)
                            {
                                fpsMonitor.consoleText = "";
                            }

                            trackerBoostingToggle.interactable = trackerCSRTToggle.interactable = trackerKCFToggle.interactable = trackerMedianFlowToggle.interactable =
                                trackerMILToggle.interactable = trackerMOSSEToggle.interactable = trackerTLDToggle.interactable = false;
                        }
                    }

                    // update trackers.
                    for (int i = 0; i < trackers.Count; i++)
                    {
                        legacy_Tracker tracker = trackers[i].tracker;
                        string label = trackers[i].label;
                        Scalar lineColor = trackers[i].lineColor;
                        Rect2d boundingBox = trackers[i].boundingBox;

                        tracker.update(rgbMat, boundingBox);

                        Imgproc.rectangle(rgbMat, boundingBox.tl(), boundingBox.br(), lineColor, 2, 1, 0);
                        Imgproc.putText(rgbMat, label, new Point(boundingBox.x, boundingBox.y - 5), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, lineColor, 1, Imgproc.LINE_AA, false);
                    }

                    if (trackers.Count == 0)
                    {
                        if (selectedPointList.Count != 1)
                        {
                            //Imgproc.putText (rgbMat, "Please touch the screen, and select tracking regions.", new Point (5, rgbMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                            if (fpsMonitor != null)
                            {
                                fpsMonitor.consoleText = "Please touch the screen, and select tracking regions.";
                            }
                        }
                        else
                        {
                            //Imgproc.putText (rgbMat, "Please select the end point of the new tracking region.", new Point (5, rgbMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                            if (fpsMonitor != null)
                            {
                                fpsMonitor.consoleText = "Please select the end point of the new tracking region.";
                            }
                        }
                    }

                    Utils.matToTexture2D(rgbMat, texture);
                }
            }
            else
            {
                if (sourceToMatHelper.IsPlaying())
                    sourceToMatHelper.Pause();

                if (storedTouchPoint != null)
                {
                    ConvertScreenPointToTexturePoint(storedTouchPoint, storedTouchPoint, gameObject, texture.width, texture.height);
                    OnTouch(storedTouchPoint, texture.width, texture.height);
                    storedTouchPoint = null;
                }
            }
        }

        private void ResetTrackers()
        {
            if (trackers != null)
            {
                foreach (var t in trackers)
                {
                    t.Dispose();
                }
                trackers.Clear();
            }

            trackerBoostingToggle.interactable = trackerCSRTToggle.interactable = trackerKCFToggle.interactable= trackerMedianFlowToggle.interactable = 
                    trackerMILToggle.interactable = trackerMOSSEToggle.interactable = trackerTLDToggle.interactable = true;
        }

        private void OnTouch(Point touchPoint, int textureWidth = -1, int textureHeight = -1)
        {
            if (selectedPointList.Count < 2)
            {
                selectedPointList.Add(touchPoint);
                if (!(new OpenCVForUnity.CoreModule.Rect(0, 0, textureWidth, textureHeight).contains(selectedPointList[selectedPointList.Count - 1])))
                {
                    selectedPointList.RemoveAt(selectedPointList.Count - 1);
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

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (sourceToMatHelper != null)
                sourceToMatHelper.Dispose();
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

            selectedPointList.Clear();
        }

        class TrackerSetting
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