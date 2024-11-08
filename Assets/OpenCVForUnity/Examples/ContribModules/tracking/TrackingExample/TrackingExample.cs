using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.TrackingModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.VideoModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
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
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class TrackingExample : MonoBehaviour
    {
        /// <summary>
        /// The trackerKFC Toggle.
        /// </summary>
        public Toggle trackerKCFToggle;

        /// <summary>
        /// The trackerCSRT Toggle.
        /// </summary>
        public Toggle trackerCSRTToggle;

        /// <summary>
        /// The trackerMIL Toggle.
        /// </summary>
        public Toggle trackerMILToggle;

        /// <summary>
        /// The trackerVit Toggle.
        /// </summary>
        public Toggle trackerVitToggle;

        /// <summary>
        /// The trackerDaSiamRPN Toggle.
        /// </summary>
        public Toggle trackerDaSiamRPNToggle;

        /// <summary>
        /// The trackerNano Toggle.
        /// </summary>
        public Toggle trackerNanoToggle;

        /// <summary>
        /// Vit_MODEL_FILENAME
        /// </summary>
        protected static readonly string Vit_MODEL_FILENAME = "OpenCVForUnity/tracking/object_tracking_vittrack_2023sep.onnx";

        /// <summary>
        /// The Vit model filepath.
        /// </summary>
        string Vit_model_filepath;

        /// <summary>
        /// DaSiamRPN_MODEL_FILENAME
        /// </summary>
        protected static readonly string DaSiamRPN_MODEL_FILENAME = "OpenCVForUnity/tracking/dasiamrpn_model.onnx";

        /// <summary>
        /// The DaSiamRPN model filepath.
        /// </summary>
        string DaSiamRPN_model_filepath;

        /// <summary>
        /// DaSiamRPN_KERNEL_R1_FILENAME
        /// </summary>
        protected static readonly string DaSiamRPN_KERNEL_R1_FILENAME = "OpenCVForUnity/tracking/dasiamrpn_kernel_r1.onnx";

        /// <summary>
        /// The DaSiamRPN kernel_r1 filepath.
        /// </summary>
        string DaSiamRPN_kernel_r1_filepath;

        /// <summary>
        /// DaSiamRPN_KERNEL_CLS1_FILENAME
        /// </summary>
        protected static readonly string DaSiamRPN_KERNEL_CLS1_FILENAME = "OpenCVForUnity/tracking/dasiamrpn_kernel_cls1.onnx";

        /// <summary>
        /// The DaSiamRPN kernel_cls1 filepath.
        /// </summary>
        string DaSiamRPN_kernel_cls1_filepath;

        /// <summary>
        /// NANOTRACK_BACKBONE_SIM_FILENAME
        /// </summary>
        protected static readonly string NANOTRACK_BACKBONE_SIM_FILENAME = "OpenCVForUnity/tracking/nanotrack_backbone_sim.onnx";

        /// <summary>
        /// The NANOTRACK_backbone_sim filepath.
        /// </summary>
        string NANOTRACK_backbone_sim_filepath;

        /// <summary>
        /// NANOTRACK_HEAD_SIM_FILENAME
        /// </summary>
        protected static readonly string NANOTRACK_HEAD_SIM_FILENAME = "OpenCVForUnity/tracking/nanotrack_head_sim.onnx";

        /// <summary>
        /// The NANOTRACK_head_sim filepath.
        /// </summary>
        string NANOTRACK_head_sim_filepath;

        bool disableTrackerVit = false;

        bool disableTrackerDaSiamRPN = false;

        bool disableTrackerNano = false;

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
        /// The multi source to mat helper.
        /// </summary>
        MultiSource2MatHelper multiSource2MatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// VIDEO_FILENAME
        /// </summary>
        protected static readonly string VIDEO_FILENAME = "OpenCVForUnity/768x576_mjpeg.mjpeg";

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // Use this for initialization
        async void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();

#if UNITY_WSA_10_0
            
            // Disable the DNN module-dependent Tracker on UWP platforms, as it cannot be used.
            trackerVitToggle.isOn = trackerVitToggle.interactable = false;
            disableTrackerVit = true;
            trackerDaSiamRPNToggle.isOn = trackerDaSiamRPNToggle.interactable = false;
            disableTrackerDaSiamRPN = true;
            trackerNanoToggle.isOn = trackerNanoToggle.interactable = false;
            disableTrackerNano = true;
            Run();

#else

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (fpsMonitor != null)
                fpsMonitor.consoleText = "Preparing file access...";

            Vit_model_filepath = await Utils.getFilePathAsyncTask(Vit_MODEL_FILENAME, cancellationToken: cts.Token);
            DaSiamRPN_model_filepath = await Utils.getFilePathAsyncTask(DaSiamRPN_MODEL_FILENAME, cancellationToken: cts.Token);
            DaSiamRPN_kernel_r1_filepath = await Utils.getFilePathAsyncTask(DaSiamRPN_KERNEL_R1_FILENAME, cancellationToken: cts.Token);
            DaSiamRPN_kernel_cls1_filepath = await Utils.getFilePathAsyncTask(DaSiamRPN_KERNEL_CLS1_FILENAME, cancellationToken: cts.Token);
            NANOTRACK_backbone_sim_filepath = await Utils.getFilePathAsyncTask(NANOTRACK_BACKBONE_SIM_FILENAME, cancellationToken: cts.Token);
            NANOTRACK_head_sim_filepath = await Utils.getFilePathAsyncTask(NANOTRACK_HEAD_SIM_FILENAME, cancellationToken: cts.Token);

            if (fpsMonitor != null)
                fpsMonitor.consoleText = "";

            CheckFilePaths();
            Run();
#endif
        }

        void CheckFilePaths()
        {
            if (string.IsNullOrEmpty(Vit_model_filepath))
            {
                Debug.LogError(Vit_MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/tracking/setup_tracking_module.pdf” to make the necessary setup.");

                trackerVitToggle.isOn = trackerVitToggle.interactable = false;
                disableTrackerVit = true;
            }

            if (string.IsNullOrEmpty(DaSiamRPN_model_filepath) || string.IsNullOrEmpty(DaSiamRPN_kernel_r1_filepath) || string.IsNullOrEmpty(DaSiamRPN_kernel_cls1_filepath))
            {
                Debug.LogError(DaSiamRPN_MODEL_FILENAME + " or " + DaSiamRPN_KERNEL_R1_FILENAME + " or " + DaSiamRPN_KERNEL_CLS1_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/tracking/setup_tracking_module.pdf” to make the necessary setup.");

                trackerDaSiamRPNToggle.isOn = trackerDaSiamRPNToggle.interactable = false;
                disableTrackerDaSiamRPN = true;
            }

            if (string.IsNullOrEmpty(NANOTRACK_backbone_sim_filepath) || string.IsNullOrEmpty(NANOTRACK_head_sim_filepath))
            {
                Debug.LogError(NANOTRACK_BACKBONE_SIM_FILENAME + " or " + NANOTRACK_HEAD_SIM_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/tracking/setup_tracking_module.pdf” to make the necessary setup.");

                trackerNanoToggle.isOn = trackerNanoToggle.interactable = false;
                disableTrackerNano = true;
            }
        }

        void Run()
        {
            if (string.IsNullOrEmpty(multiSource2MatHelper.requestedVideoFilePath))
                multiSource2MatHelper.requestedVideoFilePath = VIDEO_FILENAME;
            multiSource2MatHelper.outputColorFormat = Source2MatHelperColorFormat.RGB; // Tracking API must handle 3 channels Mat image.
            multiSource2MatHelper.Initialize();
        }

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbMat = multiSource2MatHelper.GetMat();

            texture = new Texture2D(rgbMat.cols(), rgbMat.rows(), TextureFormat.RGB24, false);
            Utils.matToTexture2D(rgbMat, texture);

            // Set the Texture2D as the main texture of the Renderer component attached to the game object
            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

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


            trackers = new List<TrackerSetting>();

            selectedPointList = new List<Point>();
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

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

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!multiSource2MatHelper.IsInitialized())
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
                if (!multiSource2MatHelper.IsPlaying())
                    multiSource2MatHelper.Play();

                if (multiSource2MatHelper.IsPlaying() && multiSource2MatHelper.DidUpdateThisFrame())
                {
                    Mat rgbMat = multiSource2MatHelper.GetMat();

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
                            Rect region = Imgproc.boundingRect(selectedPointMat);

                            // init trackers.
                            if (trackerKCFToggle.isOn)
                            {
                                TrackerKCF trackerKCF = TrackerKCF.create(new TrackerKCF_Params());
                                trackerKCF.init(rgbMat, region);
                                trackers.Add(new TrackerSetting(trackerKCF, trackerKCF.GetType().Name.ToString(), new Scalar(255, 0, 0)));
                            }

                            if (trackerCSRTToggle.isOn)
                            {
                                TrackerCSRT trackerCSRT = TrackerCSRT.create(new TrackerCSRT_Params());
                                trackerCSRT.init(rgbMat, region);
                                trackers.Add(new TrackerSetting(trackerCSRT, trackerCSRT.GetType().Name.ToString(), new Scalar(0, 255, 0)));
                            }

                            if (trackerMILToggle.isOn)
                            {
                                TrackerMIL trackerMIL = TrackerMIL.create(new TrackerMIL_Params());
                                trackerMIL.init(rgbMat, region);
                                trackers.Add(new TrackerSetting(trackerMIL, trackerMIL.GetType().Name.ToString(), new Scalar(0, 0, 255)));
                            }

                            if (!disableTrackerVit && trackerVitToggle.isOn)
                            {
                                var _params = new TrackerVit_Params();
                                _params.set_net(Vit_model_filepath);
                                TrackerVit TrackerVit = TrackerVit.create(_params);
                                TrackerVit.init(rgbMat, region);
                                trackers.Add(new TrackerSetting(TrackerVit, TrackerVit.GetType().Name.ToString(), new Scalar(255, 255, 0)));
                            }

                            if (!disableTrackerDaSiamRPN && trackerDaSiamRPNToggle.isOn)
                            {
                                var _params = new TrackerDaSiamRPN_Params();
                                _params.set_model(DaSiamRPN_model_filepath);
                                _params.set_kernel_r1(DaSiamRPN_kernel_r1_filepath);
                                _params.set_kernel_cls1(DaSiamRPN_kernel_cls1_filepath);
                                TrackerDaSiamRPN trackerDaSiamRPN = TrackerDaSiamRPN.create(_params);
                                trackerDaSiamRPN.init(rgbMat, region);
                                trackers.Add(new TrackerSetting(trackerDaSiamRPN, trackerDaSiamRPN.GetType().Name.ToString(), new Scalar(255, 0, 255)));
                            }

                            if (!disableTrackerNano && trackerNanoToggle.isOn)
                            {
                                var _params = new TrackerNano_Params();
                                _params.set_backbone(NANOTRACK_backbone_sim_filepath);
                                _params.set_neckhead(NANOTRACK_head_sim_filepath);
                                TrackerNano trackerNano = TrackerNano.create(_params);
                                trackerNano.init(rgbMat, region);
                                trackers.Add(new TrackerSetting(trackerNano, trackerNano.GetType().Name.ToString(), new Scalar(0, 255, 255)));
                            }
                        }

                        selectedPointList.Clear();

                        if (trackers.Count > 0)
                        {
                            if (fpsMonitor != null)
                            {
                                fpsMonitor.consoleText = "";
                            }

                            new[] { trackerKCFToggle, trackerCSRTToggle, trackerMILToggle }
                                .ToList().ForEach(toggle => { if (toggle) toggle.interactable = false; });

                            if (!disableTrackerVit && trackerVitToggle)
                                trackerVitToggle.interactable = false;

                            if (!disableTrackerDaSiamRPN && trackerDaSiamRPNToggle)
                                trackerDaSiamRPNToggle.interactable = false;

                            if (!disableTrackerNano && trackerNanoToggle)
                                trackerNanoToggle.interactable = false;
                        }
                    }

                    // update trackers.
                    for (int i = 0; i < trackers.Count; i++)
                    {
                        Tracker tracker = trackers[i].tracker;
                        string label = trackers[i].label;
                        Scalar lineColor = trackers[i].lineColor;
                        Rect boundingBox = trackers[i].boundingBox;

                        tracker.update(rgbMat, boundingBox);

                        Imgproc.rectangle(rgbMat, boundingBox.tl(), boundingBox.br(), lineColor, 2, 1, 0);

                        //  vit tracker provides confidence values during the tracking process, which can be used to determine if the tracking is currently lost.
                        if (trackers[i].tracker is TrackerVit)
                        {
                            TrackerVit trackerVit = (TrackerVit)trackers[i].tracker;
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
                if (multiSource2MatHelper.IsPlaying())
                    multiSource2MatHelper.Pause();

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

            new[] { trackerKCFToggle, trackerCSRTToggle, trackerMILToggle }
                .ToList().ForEach(toggle => { if (toggle) toggle.interactable = true; });

            if (!disableTrackerVit && trackerVitToggle)
                trackerVitToggle.interactable = true;

            if (!disableTrackerDaSiamRPN && trackerDaSiamRPNToggle)
                trackerDaSiamRPNToggle.interactable = true;

            if (!disableTrackerNano && trackerNanoToggle)
                trackerNanoToggle.interactable = true;
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
        /// Raises the disable event.
        /// </summary>
        void OnDisable()
        {
            if (cts != null)
                cts.Dispose();
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (multiSource2MatHelper != null)
                multiSource2MatHelper.Dispose();
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