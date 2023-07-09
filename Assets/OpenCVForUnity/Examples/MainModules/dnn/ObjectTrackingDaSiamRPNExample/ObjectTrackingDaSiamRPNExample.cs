#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Object Tracking DaSiamRPN Example
    /// An example of single object tracking using the DaSiamRPN algorithm.
    /// https://github.com/opencv/opencv/blob/master/samples/dnn/dasiamrpn_tracker.py
    /// </summary>
    [RequireComponent(typeof(VideoCaptureToMatHelper))]
    public class ObjectTrackingDaSiamRPNExample : MonoBehaviour
    {
        /// <summary>
        /// IMAGE_FILENAME
        /// </summary>
        protected static readonly string NET_FILENAME = "OpenCVForUnity/dnn/dasiamrpn_model.onnx";

        /// <summary>
        /// The net filepath.
        /// </summary>
        string net_filepath;

        /// <summary>
        /// KERNEL_R1_FILENAME
        /// </summary>
        protected static readonly string KERNEL_R1_FILENAME = "OpenCVForUnity/dnn/dasiamrpn_kernel_r1.onnx";

        /// <summary>
        /// The kernel_r1 filepath.
        /// </summary>
        string kernel_r1_filepath;

        /// <summary>
        /// KERNEL_CLS1_FILENAME
        /// </summary>
        protected static readonly string KERNEL_CLS1_FILENAME = "OpenCVForUnity/dnn/dasiamrpn_kernel_cls1.onnx";

        /// <summary>
        /// The kernel_cls1 filepath.
        /// </summary>
        string kernel_cls1_filepath;

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The DaSiamRPNTracker.
        /// </summary>
        DaSiamRPNTracker tracker;

        /// <summary>
        /// The tracking color.
        /// </summary>
        Scalar trackingColor = new Scalar(255, 255, 0);

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

#if UNITY_WEBGL
            getFilePath_Coroutine = GetFilePath();
            StartCoroutine(getFilePath_Coroutine);
#else
            net_filepath = Utils.getFilePath(NET_FILENAME);
            kernel_r1_filepath = Utils.getFilePath(KERNEL_R1_FILENAME);
            kernel_cls1_filepath = Utils.getFilePath(KERNEL_CLS1_FILENAME);
            Run();
#endif

        }

#if UNITY_WEBGL
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync(NET_FILENAME, (result) =>
            {
                net_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync(KERNEL_R1_FILENAME, (result) =>
            {
                kernel_r1_filepath = result;
            });
            yield return getFilePathAsync_1_Coroutine;

            var getFilePathAsync_2_Coroutine = Utils.getFilePathAsync(KERNEL_CLS1_FILENAME, (result) =>
            {
                kernel_cls1_filepath = result;
            });
            yield return getFilePathAsync_2_Coroutine;

            getFilePath_Coroutine = null;

            Run();
        }
#endif

        // Use this for initialization
        void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);

            if (string.IsNullOrEmpty(net_filepath) || string.IsNullOrEmpty(kernel_r1_filepath) || string.IsNullOrEmpty(kernel_cls1_filepath))
            {
                Debug.LogError(NET_FILENAME + " or " + KERNEL_R1_FILENAME + " or " + KERNEL_CLS1_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                tracker = new DaSiamRPNTracker(net_filepath, kernel_r1_filepath, kernel_cls1_filepath);
            }

            if (string.IsNullOrEmpty(sourceToMatHelper.requestedVideoFilePath))
                sourceToMatHelper.requestedVideoFilePath = VIDEO_FILENAME;
            sourceToMatHelper.outputColorFormat = VideoCaptureToMatHelper.ColorFormat.RGB; // DaSiamRPNTracker API must handle 3 channels Mat image.
            sourceToMatHelper.Initialize();

            Utils.setDebugMode(false);
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

            if (tracker == null)
            {
                if (sourceToMatHelper.IsPlaying() && sourceToMatHelper.DidUpdateThisFrame())
                {
                    Mat rgbMat = sourceToMatHelper.GetMat();

                    Imgproc.putText(rgbMat, "model file is not loaded.", new Point(5, rgbMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbMat, "Please read console message.", new Point(5, rgbMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                    Utils.matToTexture2D(rgbMat, texture);
                }
                return;
            }


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
                        using (MatOfPoint selectedPointMat = new MatOfPoint(selectedPointList.ToArray()))
                        {
                            Rect region = Imgproc.boundingRect(selectedPointMat);
                            try
                            {
                                tracker.init(rgbMat, ConvertToCenterRef(region));
                            }
                            catch (Exception e)
                            {
                                Debug.Log(e);
                            }

                            selectedPointList.Clear();
                        }
                    }

                    if (tracker.isInitialized)
                    {
                        Rect new_region = tracker.update(rgbMat);

                        if (tracker.score > 0.5)
                        {
                            // draw tracked objects regions.
                            Imgproc.rectangle(rgbMat, ConvertToTopLeftRef(new_region), trackingColor, 2, 1, 0);
                        }
                        else
                        {
                            tracker.reset();
                        }
                    }

                    if (selectedPointList.Count != 1)
                    {
                        //Imgproc.putText (rgbMat, "Please touch the screen, and select tracking regions.", new Point (5, rgbMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        if (fpsMonitor != null)
                        {
                            fpsMonitor.consoleText = "Please touch the screen, and select tracking regions.";
                        }
                    }
                    else
                    {
                        //Imgproc.putText (rgbMat, "Please select the end point of the new tracking region.", new Point (5, rgbMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        if (fpsMonitor != null)
                        {
                            fpsMonitor.consoleText = "Please select the end point of the new tracking region.";
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

        private Rect ConvertToCenterRef(Rect r)
        {
            return new Rect(r.x + r.width / 2, r.y + r.height / 2, r.width, r.height);
        }

        private Rect ConvertToTopLeftRef(Rect r)
        {
            return new Rect(r.x - r.width / 2, r.y - r.height / 2, r.width, r.height);
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
            sourceToMatHelper.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

            if (tracker != null)
            {
                tracker.dispose();
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
        /// Raises the reset tracker button click event.
        /// </summary>
        public void OnResetTrackerButtonClick()
        {
            tracker.reset();
            selectedPointList.Clear();
        }
    }


    public class DaSiamRPNTracker
    {
        string windowing = "cosine";
        int exemplar_size = 127;
        int instance_size = 271;
        int total_stride = 8;
        int score_size;
        float context_amount = 0.5f;
        float[] ratios = new float[] { 0.33f, 0.5f, 1f, 2f, 3f };
        float[] scales = new float[] { 8f };
        int anchor_num;
        float penalty_k = 0.055f;
        float window_influence = 0.42f;
        float lr = 0.295f;

        protected double _score;
        public double score
        {
            get { return _score; }
        }

        Mat window;

        Net net;
        Net kernel_r1;
        Net kernel_cls1;

        int im_h;
        int im_w;
        Point target_pos;
        Size target_sz;
        Scalar avg_chans;
        Mat anchor;

        protected bool _isInitialized;
        public bool isInitialized
        {
            get { return _isInitialized; }
        }

        protected bool _isDisposed;
        public bool isDisposed
        {
            get { return _isDisposed; }
        }

        // # Initialization of used values, initial bounding box, used network
        public DaSiamRPNTracker(string net_filepath, string kernel_r1_filepath, string kernel_cls1_filepath)
        {
            score_size = (int)((instance_size - exemplar_size) / total_stride) + 1;
            anchor_num = ratios.Length * scales.Length;

            Mat _window;
            if (windowing == "cosine")
            {
                exemplar_size = 127;
                instance_size = 271;
                total_stride = 8;
                score_size = 19;

                Mat hanning19Mat = new Mat(1, 19, CvType.CV_32FC1);
                hanning19Mat.put(0, 0, new float[] { 0f, 0.03015369f, 0.11697778f, 0.25f, 0.41317591f, 0.58682409f, 0.75f, 0.88302222f, 0.96984631f, 1f, 0.96984631f,
                0.88302222f, 0.75f, 0.58682409f, 0.41317591f, 0.25f, 0.11697778f, 0.03015369f, 0f });
                _window = outer(hanning19Mat, hanning19Mat);
            }
            else
            {
                _window = Mat.ones(score_size, score_size, CvType.CV_32FC1);
            }
            Mat _window_flatten = flatten(_window);
            window = new Mat(_window_flatten.rows() * 1, _window_flatten.cols() * anchor_num, _window.type());
            tile(_window_flatten, 1, anchor_num, window);


            // # Loading network`s and kernel`s models
            net = Dnn.readNet(net_filepath);
            kernel_r1 = Dnn.readNet(kernel_r1_filepath);
            kernel_cls1 = Dnn.readNet(kernel_cls1_filepath);

            if (net.empty())
            {
                Debug.LogError("model file is not loaded. The model and class names list can be downloaded here: \"https://www.dropbox.com/s/rr1lk9355vzolqv/dasiamrpn_model.onnx?dl=0\". Please copy to “Assets/StreamingAssets/OpenCVForUnity/dnn/” folder. ");
            }
            if (kernel_r1.empty())
            {
                Debug.LogError("model file is not loaded. The model and class names list can be downloaded here: \"https://www.dropbox.com/s/999cqx5zrfi7w4p/dasiamrpn_kernel_r1.onnx?dl=0\". Please copy to “Assets/StreamingAssets/OpenCVForUnity/dnn/” folder. ");
            }
            if (kernel_cls1.empty())
            {
                Debug.LogError("model file is not loaded. The model and class names list can be downloaded here: \"https://www.dropbox.com/s/qvmtszx5h339a0w/dasiamrpn_kernel_cls1.onnx?dl=0\". Please copy to “Assets/StreamingAssets/OpenCVForUnity/dnn/” folder. ");
            }
        }

        public void init(Mat im, Rect init_bb)
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            im_h = im.height();
            im_w = im.width();

            int bb_x = Mathf.Clamp(init_bb.x, 0, im_w);
            int bb_y = Mathf.Clamp(init_bb.y, 0, im_h);
            int bb_w = Mathf.Clamp(init_bb.width, 0, im_w);
            int bb_h = Mathf.Clamp(init_bb.height, 0, im_h);
            target_pos = new Point(bb_x, bb_y);
            target_sz = new Size(bb_w, bb_h);

            avg_chans = Core.mean(im);
            avg_chans = new Scalar(Math.Floor(avg_chans.val[0]), Math.Floor(avg_chans.val[1]), Math.Floor(avg_chans.val[2]), Math.Floor(avg_chans.val[3]));

            // # When we trying to generate ONNX model from the pre-trained .pth model
            // # we are using only one state of the network. In our case used state
            // # with big bounding box, so we were forced to add assertion for
            // # too small bounding boxes - current state of the network can not
            // # work properly with such small bounding boxes
            if (target_sz.width * target_sz.height / (float)(im_h * im_w) < 0.004)
                throw new Exception("Initializing BB is too small-try to restart tracker with larger BB");


            anchor = __generate_anchor();

            double wc_z = target_sz.width + context_amount * (target_sz.width + target_sz.height);
            double hc_z = target_sz.height + context_amount * (target_sz.width + target_sz.height);
            int s_z = (int)Math.Round(Math.Sqrt(wc_z * hc_z));

            Mat z_crop = __get_subwindow_tracking(im, exemplar_size, s_z);
            z_crop = Dnn.blobFromImage(z_crop);

            net.setInput(z_crop);
            Mat z_f = net.forward("onnx_node_output_0!63");
            kernel_r1.setInput(z_f);
            Mat r1 = kernel_r1.forward();
            kernel_cls1.setInput(z_f);
            Mat cls1 = kernel_cls1.forward();
            r1 = r1.reshape(1, new int[] { 20, 256, 4, 4 });
            cls1 = cls1.reshape(1, new int[] { 10, 256, 4, 4 });

            net.setParam(net.getLayerId("onnx_node_output_0!65"), 0, r1);
            net.setParam(net.getLayerId("onnx_node_output_0!68"), 0, cls1);

            _isInitialized = true;
        }


        //# Creating anchor for tracking bounding box
        private Mat __generate_anchor()
        {
            Mat _anchor;
            int score_sz = (int)score_size;

            using (Mat __anchor = Mat.zeros(anchor_num, 4, CvType.CV_32FC1))
            {
                int size = total_stride * total_stride;
                int count = 0;

                foreach (float ratio in ratios)
                {
                    int ws = (int)(Mathf.Sqrt(size / ratio));
                    int hs = (int)(ws * ratio);
                    foreach (float scale in scales)
                    {
                        float wws = ws * scale;
                        float hhs = hs * scale;
                        __anchor.put(count, 0, new float[] { 0, 0, wws, hhs });
                        count += 1;
                    }
                }

                using (Mat __anchor_tile = new Mat(__anchor.rows() * 1, __anchor.cols() * score_sz * score_sz, __anchor.type()))
                {
                    tile(__anchor, 1, score_sz * score_sz, __anchor_tile);
                    _anchor = __anchor_tile.reshape(1, anchor_num * score_sz * score_sz);
                }
            }

            float ori = -(score_sz / 2f) * total_stride;

            float[] _xx_arr = new float[score_sz];
            for (int dx = 0; dx < score_sz; dx++)
            {
                _xx_arr[dx] = ori + total_stride * dx;
            }
            using (Mat _xx = new Mat(1, score_sz, CvType.CV_32FC1))
            {
                _xx.put(0, 0, _xx_arr);

                using (Mat _xx_tile = new Mat(_xx.rows() * _xx.cols(), _xx.cols() * 1, _xx.type()))
                {
                    tile(_xx, _xx.cols(), 1, _xx_tile);
                    using (Mat _xx_tile_t = _xx_tile.t())
                    using (Mat __xx = flatten(_xx_tile))
                    using (Mat __yy = flatten(_xx_tile_t))
                    using (Mat __xx_tile = new Mat(__xx.rows() * anchor_num, __xx.cols() * 1, __xx.type()))
                    using (Mat __yy_tile = new Mat(__yy.rows() * anchor_num, __yy.cols() * 1, __yy.type()))
                    {
                        tile(__xx, anchor_num, 1, __xx_tile);
                        tile(__yy, anchor_num, 1, __yy_tile);

                        using (Mat xx = __xx_tile.reshape(1, _anchor.rows()))
                        using (Mat yy = __yy_tile.reshape(1, _anchor.rows()))
                        using (Mat _anchor_roi_c0 = _anchor.col(0))
                        using (Mat _anchor_roi_c1 = _anchor.col(1))
                        {
                            xx.copyTo(_anchor_roi_c0);
                            yy.copyTo(_anchor_roi_c1);
                        }
                    }
                }
            }

            return _anchor.t(); // Return a transposed anchor.
        }

        // # Function for updating tracker state
        public Rect update(Mat im)
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (!isInitialized)
                return new Rect();

            double wc_z = target_sz.height + context_amount * (target_sz.width + target_sz.height);
            double hc_z = target_sz.width + context_amount * (target_sz.width + target_sz.height);
            double s_z = Math.Sqrt(wc_z * hc_z);
            double scale_z = exemplar_size / s_z;
            float d_search = (instance_size - exemplar_size) / 2f;
            double pad = d_search / scale_z;
            int s_x = (int)Math.Round(s_z + 2.0 * pad);

            // # Region preprocessing part
            Mat x_crop = __get_subwindow_tracking(im, instance_size, s_x);
            x_crop = Dnn.blobFromImage(x_crop);

            _score = __tracker_eval(x_crop, scale_z);

            target_pos.x = Math.Max(0, Math.Min(im_w, target_pos.x));
            target_pos.y = Math.Max(0, Math.Min(im_h, target_pos.y));
            target_sz.width = Math.Max(10, Math.Min(im_w, target_sz.width));
            target_sz.height = Math.Max(10, Math.Min(im_h, target_sz.height));

            return new Rect(target_pos, target_sz);
        }

        public void reset()
        {
            _isInitialized = false;

            if (anchor != null)
            {
                anchor.Dispose();
                anchor = null;
            }
        }

        public void dispose()
        {
            reset();

            _isDisposed = true;

            if (window != null)
            {
                window.Dispose();
                window = null;
            }
            if (net != null)
            {
                net.Dispose();
                net = null;
            }
            if (kernel_r1 != null)
            {
                kernel_r1.Dispose();
                kernel_r1 = null;
            }
            if (kernel_cls1 != null)
            {
                kernel_cls1.Dispose();
                kernel_cls1 = null;
            }

            if (__tracker_eval_score_r1_0 != null)
            {
                __tracker_eval_score_r1_0.Dispose();
                __tracker_eval_tmp_r1_0.Dispose();
                __tracker_eval_tmp_r1_1.Dispose();
                __tracker_eval_tmp_r1_2.Dispose();

                __tracker_eval_func_tmp_r1_0.Dispose();
                __tracker_eval_func_tmp_r1_1.Dispose();
                __tracker_eval_func_tmp_r2_0.Dispose();
                __tracker_eval_func_tmp_r2_1.Dispose();
            }
        }


        Mat __tracker_eval_score_r1_0;
        Mat __tracker_eval_tmp_r1_0;
        Mat __tracker_eval_tmp_r1_1;
        Mat __tracker_eval_tmp_r1_2;

        Mat __tracker_eval_func_tmp_r1_0;
        Mat __tracker_eval_func_tmp_r1_1;
        Mat __tracker_eval_func_tmp_r2_0;
        Mat __tracker_eval_func_tmp_r2_1;

        List<string> outNames;
        List<Mat> outBlobs = new List<Mat>();

        // # Function for updating position of the bounding box
        private double __tracker_eval(Mat x_crop, double scale_z)
        {
            Size target_size = target_sz * scale_z;
            net.setInput(x_crop);

            if (outNames == null)
            {
                outNames = net.getUnconnectedOutLayersNames();
                outNames[0] = "66";
                outNames[1] = "68";
            }

            net.forward(outBlobs, outNames);
            Mat delta = outBlobs[0];
            Mat _score = outBlobs[1];

            delta = delta.reshape(1, new int[] { 4, (int)delta.total() / 4 });
            _score = _score.reshape(1, new int[] { 2, (int)_score.total() / 2 });

            int cols = delta.cols();
            int type = delta.type();

            if (__tracker_eval_score_r1_0 == null)
                __tracker_eval_score_r1_0 = new Mat(1, cols, type);
            if (__tracker_eval_tmp_r1_0 == null)
                __tracker_eval_tmp_r1_0 = new Mat(1, cols, type);
            if (__tracker_eval_tmp_r1_1 == null)
                __tracker_eval_tmp_r1_1 = new Mat(1, cols, type);
            if (__tracker_eval_tmp_r1_2 == null)
                __tracker_eval_tmp_r1_2 = new Mat(1, cols, type);

            if (__tracker_eval_func_tmp_r1_0 == null)
                __tracker_eval_func_tmp_r1_0 = new Mat(1, cols, type);
            if (__tracker_eval_func_tmp_r1_1 == null)
                __tracker_eval_func_tmp_r1_1 = new Mat(1, cols, type);
            if (__tracker_eval_func_tmp_r2_0 == null)
                __tracker_eval_func_tmp_r2_0 = new Mat(2, cols, type);
            if (__tracker_eval_func_tmp_r2_1 == null)
                __tracker_eval_func_tmp_r2_1 = new Mat(2, cols, type);


            Mat score = __tracker_eval_score_r1_0;
            __softmax(_score, score);


            Mat tmp_r1_0 = __tracker_eval_tmp_r1_0;
            Mat tmp_r1_1 = __tracker_eval_tmp_r1_1;
            Mat tmp_r1_2 = __tracker_eval_tmp_r1_2;


            //delta[0, :] = delta[0, :] * self.anchor[:, 2] + self.anchor[:, 0]
            //delta[1, :] = delta[1, :] * self.anchor[:, 3] + self.anchor[:, 1]
            //delta[2, :] = np.exp(delta[2, :]) * self.anchor[:, 2]
            //delta[3, :] = np.exp(delta[3, :]) * self.anchor[:, 3]

            using (Mat delta_roi_r0 = delta.row(0))
            using (Mat delta_roi_r1 = delta.row(1))
            using (Mat delta_roi_r2 = delta.row(2))
            using (Mat delta_roi_r3 = delta.row(3))
            using (Mat anchor_roi_r0 = anchor.row(0))
            using (Mat anchor_roi_r1 = anchor.row(1))
            using (Mat anchor_roi_r2 = anchor.row(2))
            using (Mat anchor_roi_r3 = anchor.row(3))
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

            double target_size_sz_wh = __sz_wh(target_size);
            using (Mat delta_roi_r2 = delta.row(2))
            using (Mat delta_roi_r3 = delta.row(3))
            {
                __sz(delta_roi_r2, delta_roi_r3, tmp_r1_0);
                Core.divide(tmp_r1_0, new Scalar(target_size_sz_wh), tmp_r1_0);
                __change(tmp_r1_0, tmp_r1_1); // s_c


                Core.divide(delta_roi_r2, delta_roi_r3, tmp_r1_0);
                Core.divide(target_size.width / target_size.height, tmp_r1_0, tmp_r1_0);
                __change(tmp_r1_0, tmp_r1_2); // r_c


                Core.multiply(tmp_r1_2, tmp_r1_1, tmp_r1_2);
                Core.subtract(tmp_r1_2, new Scalar(1.0), tmp_r1_2);
                Core.multiply(tmp_r1_2, new Scalar(penalty_k), tmp_r1_2, -1.0);
                Core.exp(tmp_r1_2, tmp_r1_2);
                Mat penalty = tmp_r1_2; // penalty


                Core.multiply(penalty, score, tmp_r1_0);
                Core.multiply(tmp_r1_0, new Scalar(1 - window_influence), tmp_r1_0);
                Core.multiply(window, new Scalar(window_influence), tmp_r1_1);
                Core.add(tmp_r1_0, tmp_r1_1, tmp_r1_0);
                Mat pscore = tmp_r1_0; // pscore

                using (Mat pscore_argmax = new Mat(1, 1, type))
                {
                    argmax_axis1(pscore, pscore_argmax);

                    best_pscore_id = (int)pscore_argmax.get(0, 0)[0];
                    penalty_best_pscore = penalty.get(0, best_pscore_id)[0];
                }
            }

            float[] target = new float[4];
            target[0] = (float)(delta.get(0, best_pscore_id)[0] / scale_z);
            target[1] = (float)(delta.get(1, best_pscore_id)[0] / scale_z);
            target[2] = (float)(delta.get(2, best_pscore_id)[0] / scale_z);
            target[3] = (float)(delta.get(3, best_pscore_id)[0] / scale_z);

            target_size /= scale_z;
            double _lr = penalty_best_pscore * score.get(0, best_pscore_id)[0] * lr;
            double res_x = target[0] + target_pos.x;
            double res_y = target[1] + target_pos.y;
            double res_w = target_size.width * (1.0 - _lr) + target[2] * _lr;
            double res_h = target_size.height * (1.0 - _lr) + target[3] * _lr;
            target_pos = new Point(res_x, res_y);
            target_sz = new Size(res_w, res_h);

            return score.get(0, best_pscore_id)[0];
        }

        /// <summary>
        /// change.
        /// </summary>
        /// <param name="r">Mat[1*C]</param>
        /// <param name="dst">Mat[1*C]</param>
        private void __change(Mat r, Mat dst)
        {
            if (r == null)
                throw new ArgumentNullException("r");
            if (r != null)
                r.ThrowIfDisposed();
            if (r.rows() != 1)
                throw new ArgumentException("r.rows() != 1");

            if (dst == null)
                throw new ArgumentNullException("dst");
            if (dst != null)
                dst.ThrowIfDisposed();
            if (dst.rows() != 1)
                throw new ArgumentException("dst.rows() != 1");
            if (dst.cols() != r.cols() || dst.type() != r.type())
                throw new ArgumentException("dst.cols() != r.cols() || dst.type() != r.type()");


            // return np.maximum(r, 1./r)

            Mat tmp_r1_0 = __tracker_eval_func_tmp_r1_0;

            Core.divide(1.0, r, tmp_r1_0);
            Core.max(r, tmp_r1_0, dst);
        }

        /// <summary>
        /// sz.
        /// </summary>
        /// <param name="w">Mat[1*C]</param>
        /// <param name="h">Mat[1*C]</param>
        /// <param name="dst">Mat[1*C]</param>
        private void __sz(Mat w, Mat h, Mat dst)
        {
            if (w == null)
                throw new ArgumentNullException("w");
            if (w != null)
                w.ThrowIfDisposed();
            if (w.rows() != 1)
                throw new ArgumentException("w.rows() != 1");

            if (h == null)
                throw new ArgumentNullException("h");
            if (h != null)
                h.ThrowIfDisposed();
            if (h.rows() != 1)
                throw new ArgumentException("h.rows() != 1");

            if (dst == null)
                throw new ArgumentNullException("dst");
            if (dst != null)
                dst.ThrowIfDisposed();
            if (dst.rows() != 1)
                throw new ArgumentException("dst.rows() != 1");

            if (w.cols() != h.cols() || w.type() != h.type())
                throw new ArgumentException(" w.cols() != h.cols() || w.type() != h.type()");
            if (h.cols() != dst.cols() || h.type() != dst.type())
                throw new ArgumentException("h.cols() != dst.cols() || h.type() != dst.type()");


            //pad = (w + h) * 0.5
            //sz2 = (w + pad) * (h + pad)
            //return np.sqrt(sz2)

            Mat tmp_r1_0 = __tracker_eval_func_tmp_r1_0;
            Mat tmp_r1_1 = __tracker_eval_func_tmp_r1_1;

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
        private double __sz_wh(Size wh)
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
        private void __softmax(Mat x, Mat dst)
        {
            if (x == null)
                throw new ArgumentNullException("x");
            if (x != null)
                x.ThrowIfDisposed();
            if (x.rows() != 2)
                throw new ArgumentException("x.rows() != 2");

            if (dst == null)
                throw new ArgumentNullException("dst");
            if (dst != null)
                dst.ThrowIfDisposed();
            if (dst.rows() != 1 || dst.cols() != x.cols() || dst.type() != x.type())
                throw new ArgumentException("dst.rows() != 1 || dst.cols() != x.cols() || dst.type() != x.type()");


            //x_max = x.max(0)
            //e_x = np.exp(x - x_max)
            //y = e_x / e_x.sum(axis = 0)

            Mat tmp_r1_0 = __tracker_eval_func_tmp_r1_0;
            Mat tmp_r2_0 = __tracker_eval_func_tmp_r2_0;
            Mat tmp_r2_1 = __tracker_eval_func_tmp_r2_1;

            max_axis0(x, tmp_r1_0);
            tile(tmp_r1_0, 2, 1, tmp_r2_0); // x_max

            Core.subtract(x, tmp_r2_0, tmp_r2_0);
            Core.exp(tmp_r2_0, tmp_r2_0); // e_x

            sum_axis0(tmp_r2_0, tmp_r1_0);
            tile(tmp_r1_0, 2, 1, tmp_r2_1); // e_x_sum

            Core.divide(tmp_r2_0, tmp_r2_1, tmp_r2_0); // y

            using (Mat x_x_max_subtract_roi_r1 = tmp_r2_0.row(1))
            {
                x_x_max_subtract_roi_r1.copyTo(dst);
            }
        }


        Mat te_im_tmp;
        Mat im_patch_original_resize;

        // # Reshaping cropped image for using in the model
        private Mat __get_subwindow_tracking(Mat im, int model_size, int original_sz)
        {
            Size im_sz = im.size();
            double _c = (original_sz + 1) / 2.0;

            int context_xmin = (int)Math.Round(target_pos.x - _c);
            int context_xmax = context_xmin + original_sz - 1;
            int context_ymin = (int)Math.Round(target_pos.y - _c);
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


            double wc_z = im.height() + context_amount * (im.width() + im.height());
            double hc_z = im.width() + context_amount * (im.width() + im.height());
            double s_z = Math.Sqrt(wc_z * hc_z);
            double scale_z = exemplar_size / s_z;
            float d_search = (instance_size - exemplar_size) / 2f;
            double pad = d_search / scale_z;
            int te_im_tmp_sz = (int)Math.Round(s_z + 2.0 * pad);

            if (te_im_tmp == null || te_im_tmp.rows() != te_im_tmp_sz || te_im_tmp.cols() != te_im_tmp_sz)
            {
                te_im_tmp = new Mat(te_im_tmp_sz, te_im_tmp_sz, im.type());
            }


            Mat im_patch_original;

            if (top_pad > 0 || bot_pad > 0 || left_pad > 0 || right_pad > 0)
            {
                using (Mat te_im = new Mat(te_im_tmp, new Rect(0, 0, c + left_pad + right_pad, r + top_pad + bot_pad)))
                using (Mat te_im_roi = new Mat(te_im, new Rect(left_pad, top_pad, c, r)))
                {
                    im.copyTo(te_im_roi);

                    if (top_pad > 0)
                    {
                        using (Mat te_im_roi2 = new Mat(te_im, new Rect(left_pad, 0, c, top_pad)))
                        {
                            te_im_roi2.setTo(avg_chans);
                        }
                    }
                    if (bot_pad > 0)
                    {
                        using (Mat te_im_roi2 = new Mat(te_im, new Rect(left_pad, r + top_pad, c, te_im.rows() - (r + top_pad))))
                        {
                            te_im_roi2.setTo(avg_chans);
                        }
                    }
                    if (left_pad > 0)
                    {
                        using (Mat te_im_roi2 = new Mat(te_im, new Rect(0, 0, left_pad, te_im.rows())))
                        {
                            te_im_roi2.setTo(avg_chans);
                        }
                    }
                    if (right_pad > 0)
                    {
                        using (Mat te_im_roi2 = new Mat(te_im, new Rect(c + left_pad, 0, te_im.cols() - (c + left_pad), te_im.rows())))
                        {
                            te_im_roi2.setTo(avg_chans);
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
                if (im_patch_original_resize == null || im_patch_original_resize.rows() != model_size || im_patch_original_resize.cols() != model_size)
                {
                    im_patch_original_resize = new Mat(model_size, model_size, im_patch_original.type());
                }

                Imgproc.resize(im_patch_original, im_patch_original_resize, new Size(model_size, model_size));
                im_patch_original.Dispose();

                return im_patch_original_resize;
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
        private Mat outer(Mat a, Mat b)
        {
            if (a == null)
                throw new ArgumentNullException("a");
            if (a != null)
                a.ThrowIfDisposed();
            if (b == null)
                throw new ArgumentNullException("b");
            if (b != null)
                b.ThrowIfDisposed();

            if (a.rows() != 1 || a.channels() != 1)
                throw new ArgumentException("a.rows() != 1 || a.channels() != 1");
            if (b.rows() != 1 || b.channels() != 1)
                throw new ArgumentException("b.rows() != 1 || b.channels() != 1");
            if (a.type() != b.type())
                throw new ArgumentException("a.type() != b.type()");

            int rows = a.cols();
            int cols = b.cols();
            int type = a.type();

            Mat dst;

            using (Mat _a = new Mat(cols, rows, type))
            using (Mat _b = new Mat(rows, cols, type))
            {
                Core.repeat(a, cols, 1, _a);
                Core.repeat(b, rows, 1, _b);
                Core.transpose(_a, _a);

                dst = _a.mul(_b);
            }

            return dst;
        }

        /// <summary>
        /// Return a copy of the array collapsed into one dimension.
        /// </summary>
        private Mat flatten(Mat a)
        {
            if (a == null)
                throw new ArgumentNullException("a");
            if (a != null)
                a.ThrowIfDisposed();

            return a.reshape(1, 1);
        }

        /// <summary>
        /// Construct an array by repeating A the number of times given by reps.
        /// </summary>
        private void tile(Mat a, int ny, int nx, Mat dst)
        {
            if (a == null)
                throw new ArgumentNullException("a");
            if (a != null)
                a.ThrowIfDisposed();

            if (dst == null)
                throw new ArgumentNullException("dst");
            if (dst != null)
                dst.ThrowIfDisposed();
            if (dst.rows() != a.rows() * ny || dst.cols() != a.cols() * nx || dst.type() != a.type())
                throw new ArgumentException("dst.rows() != a.rows() * ny || dst.cols() != a.cols() * nx || dst.type() != a.type()");

            Core.repeat(a, ny, nx, dst);
        }

        /// <summary>
        /// Return the maximum along a given axis.
        /// </summary>
        /// <param name="a">Mat[2*C]</param>
        /// <param name="dst">Mat[1*C]</param>
        private void max_axis0(Mat a, Mat dst)
        {
            if (a == null)
                throw new ArgumentNullException("a");
            if (a != null)
                a.ThrowIfDisposed();
            if (a.channels() != 1)
                throw new ArgumentException("a.channels() != 1");

            if (dst == null)
                throw new ArgumentNullException("dst");
            if (dst != null)
                dst.ThrowIfDisposed();
            if (dst.rows() != 1 || dst.cols() != a.cols() || dst.type() != a.type())
                throw new ArgumentException("dst.rows() != 1 || dst.cols() != a.cols() || dst.type() != a.type()");


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
        private void argmax_axis1(Mat a, Mat dst)
        {
            if (a == null)
                throw new ArgumentNullException("a");
            if (a != null)
                a.ThrowIfDisposed();
            if (a.channels() != 1)
                throw new ArgumentException("a.channels() != 1");

            if (dst == null)
                throw new ArgumentNullException("dst");
            if (dst != null)
                dst.ThrowIfDisposed();
            if (dst.rows() != a.rows() || dst.cols() != 1 || dst.type() != a.type())
                throw new ArgumentException("dst.rows() != a.rows() || dst.cols() != 1 || dst.type() != a.type()");


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
            MatUtils.copyToMat(dstArr, dst);
        }

        /// <summary>
        /// Sum of array elements over a given axis.
        /// </summary>
        /// <param name="a">Mat[2*C]</param>
        /// <param name="dst">Mat[1*C]</param>
        private void sum_axis0(Mat a, Mat dst)
        {
            if (a == null)
                throw new ArgumentNullException("a");
            if (a != null)
                a.ThrowIfDisposed();
            if (a.rows() != 2)
                throw new ArgumentException("a.rows() != 2");

            if (dst == null)
                throw new ArgumentNullException("dst");
            if (dst != null)
                dst.ThrowIfDisposed();
            if (dst.rows() != 1 || dst.cols() != a.cols() || dst.type() != a.type())
                throw new ArgumentException("dst.rows() != 1 || dst.cols() != a.cols() || dst.type() != a.type()");


            using (Mat a_roi_r0 = a.row(0))
            using (Mat a_roi_r1 = a.row(1))
            {
                Core.add(a_roi_r0, a_roi_r1, dst);
            }
        }
    }
}

#endif