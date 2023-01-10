using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.TrackingModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.VideoModule;
using System;
using System.Collections;
using System.Collections.Generic;
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
    /// </summary>
    [RequireComponent(typeof(VideoCaptureToMatHelper))]
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
        /// The trackerGOTURN Toggle.
        /// </summary>
        public Toggle trackerGOTURNToggle;

        /// <summary>
        /// The trackerDaSiamRPN Toggle.
        /// </summary>
        public Toggle trackerDaSiamRPNToggle;

        /// <summary>
        /// The trackerNano Toggle.
        /// </summary>
        public Toggle trackerNanoToggle;

        /// <summary>
        /// GOTURN_MODELTXT_FILENAME
        /// </summary>
        protected static readonly string GOTURN_MODELTXT_FILENAME = "OpenCVForUnity/tracking/goturn.prototxt";

        /// <summary>
        /// The GOTURN modelTxt filepath.
        /// </summary>
        string GOTURN_modelTxt_filepath;

        /// <summary>
        /// GOTURN_MODELBIN_FILENAME
        /// </summary>
        protected static readonly string GOTURN_MODELBIN_FILENAME = "OpenCVForUnity/tracking/goturn.caffemodel";

        /// <summary>
        /// The GOTURN modelBin filepath.
        /// </summary>
        string GOTURN_modelBin_filepath;

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

        bool disableTrackerGOTURN = false;

        bool disableTrackerDaSiamRPN = false;

        bool disableTrackerNano = false;

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

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


#if UNITY_WSA_10_0
            
            // Disable the DNN module-dependent Tracker on UWP platforms, as it cannot be used.
            trackerGOTURNToggle.isOn = trackerGOTURNToggle.interactable = false;
            disableTrackerGOTURN = true;
            trackerDaSiamRPNToggle.isOn = trackerDaSiamRPNToggle.interactable = false;
            disableTrackerDaSiamRPN = true;
            trackerNanoToggle.isOn = trackerNanoToggle.interactable = false;
            disableTrackerNano = true;
            Run();

#elif UNITY_WEBGL

            getFilePath_Coroutine = GetFilePath();
            StartCoroutine(getFilePath_Coroutine);

#else

            GOTURN_modelTxt_filepath = Utils.getFilePath(GOTURN_MODELTXT_FILENAME);
            GOTURN_modelBin_filepath = Utils.getFilePath(GOTURN_MODELBIN_FILENAME);
            DaSiamRPN_model_filepath = Utils.getFilePath(DaSiamRPN_MODEL_FILENAME);
            DaSiamRPN_kernel_r1_filepath = Utils.getFilePath(DaSiamRPN_KERNEL_R1_FILENAME);
            DaSiamRPN_kernel_cls1_filepath = Utils.getFilePath(DaSiamRPN_KERNEL_CLS1_FILENAME);
            NANOTRACK_backbone_sim_filepath = Utils.getFilePath(NANOTRACK_BACKBONE_SIM_FILENAME);
            NANOTRACK_head_sim_filepath = Utils.getFilePath(NANOTRACK_HEAD_SIM_FILENAME);
            CheckFilePaths();
            Run();

#endif
        }

#if UNITY_WEBGL
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync(GOTURN_MODELTXT_FILENAME, (result) =>
            {
                GOTURN_modelTxt_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync(GOTURN_MODELBIN_FILENAME, (result) =>
            {
                GOTURN_modelBin_filepath = result;
            });
            yield return getFilePathAsync_1_Coroutine;

            var getFilePathAsync_2_Coroutine = Utils.getFilePathAsync(DaSiamRPN_MODEL_FILENAME, (result) =>
            {
                DaSiamRPN_model_filepath = result;
            });
            yield return getFilePathAsync_2_Coroutine;

            var getFilePathAsync_3_Coroutine = Utils.getFilePathAsync(DaSiamRPN_KERNEL_R1_FILENAME, (result) =>
            {
                DaSiamRPN_kernel_r1_filepath = result;
            });
            yield return getFilePathAsync_3_Coroutine;

            var getFilePathAsync_4_Coroutine = Utils.getFilePathAsync(DaSiamRPN_KERNEL_CLS1_FILENAME, (result) =>
            {
                DaSiamRPN_kernel_cls1_filepath = result;
            });
            yield return getFilePathAsync_4_Coroutine;

            var getFilePathAsync_5_Coroutine = Utils.getFilePathAsync(NANOTRACK_BACKBONE_SIM_FILENAME, (result) =>
            {
                NANOTRACK_backbone_sim_filepath = result;
            });
            yield return getFilePathAsync_5_Coroutine;

            var getFilePathAsync_6_Coroutine = Utils.getFilePathAsync(NANOTRACK_HEAD_SIM_FILENAME, (result) =>
            {
                NANOTRACK_head_sim_filepath = result;
            });
            yield return getFilePathAsync_6_Coroutine;

            getFilePath_Coroutine = null;

            CheckFilePaths();
            Run();
        }
#endif

        void CheckFilePaths()
        {
            if (string.IsNullOrEmpty(GOTURN_modelTxt_filepath) || string.IsNullOrEmpty(GOTURN_modelBin_filepath))
            {
                Debug.LogError(GOTURN_MODELTXT_FILENAME + " or " + GOTURN_MODELBIN_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/tracking/setup_tracking_module.pdf” to make the necessary setup.");

                trackerGOTURNToggle.isOn = trackerGOTURNToggle.interactable = false;
                disableTrackerGOTURN = true;
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

                            if (!disableTrackerGOTURN && trackerGOTURNToggle.isOn)
                            {
                                var _params = new TrackerGOTURN_Params();
                                _params.set_modelTxt(GOTURN_modelTxt_filepath);
                                _params.set_modelBin(GOTURN_modelBin_filepath);
                                TrackerGOTURN trackerGOTURN = TrackerGOTURN.create(_params);
                                trackerGOTURN.init(rgbMat, region);
                                trackers.Add(new TrackerSetting(trackerGOTURN, trackerGOTURN.GetType().Name.ToString(), new Scalar(255, 255, 0)));
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

                            trackerKCFToggle.interactable = trackerCSRTToggle.interactable = trackerMILToggle.interactable = false;

                            if (!disableTrackerGOTURN)
                                trackerGOTURNToggle.interactable = false;

                            if (!disableTrackerDaSiamRPN)
                                trackerDaSiamRPNToggle.interactable = false;

                            if (!disableTrackerNano)
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

            trackerKCFToggle.interactable = trackerCSRTToggle.interactable = trackerMILToggle.interactable = true;

            if (!disableTrackerGOTURN)
                trackerGOTURNToggle.interactable = true;

            if (!disableTrackerDaSiamRPN)
                trackerDaSiamRPNToggle.interactable = true;

            if (!disableTrackerNano)
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
#if UNITY_WEBGL
            if (getFilePath_Coroutine != null)
            {
                StopCoroutine(getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose();
            }
#endif
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