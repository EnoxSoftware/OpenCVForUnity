#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Object Tracking DaSiamRPN Example
    /// (##### Usually use the newly added TrackingAPI TrackerDaSiamRPN class instead of this example. #####)
    /// An example of single object tracking using the DaSiamRPN algorithm.
    /// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/dasiamrpn_tracker.py
    ///
    /// [Tested Models]
    /// https://www.dropbox.com/s/rr1lk9355vzolqv/dasiamrpn_model.onnx?dl=1
    /// https://www.dropbox.com/s/999cqx5zrfi7w4p/dasiamrpn_kernel_r1.onnx?dl=1
    /// https://www.dropbox.com/s/qvmtszx5h339a0w/dasiamrpn_kernel_cls1.onnx?dl=1
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class ObjectTrackingDaSiamRPNExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// IMAGE_FILENAME
        /// </summary>
        protected static readonly string NET_FILENAME = "OpenCVForUnityExamples/dnn/dasiamrpn_model.onnx";

        /// <summary>
        /// KERNEL_R1_FILENAME
        /// </summary>
        protected static readonly string KERNEL_R1_FILENAME = "OpenCVForUnityExamples/dnn/dasiamrpn_kernel_r1.onnx";

        /// <summary>
        /// KERNEL_CLS1_FILENAME
        /// </summary>
        protected static readonly string KERNEL_CLS1_FILENAME = "OpenCVForUnityExamples/dnn/dasiamrpn_kernel_cls1.onnx";

        /// <summary>
        /// VIDEO_FILENAME
        /// </summary>
        protected static readonly string VIDEO_FILENAME = "OpenCVForUnityExamples/768x576_mjpeg.mjpeg";

        // Private Fields
        /// <summary>
        /// The net filepath.
        /// </summary>
        private string _netFilepath;

        /// <summary>
        /// The kernel_r1 filepath.
        /// </summary>
        private string _kernelR1Filepath;

        /// <summary>
        /// The kernel_cls1 filepath.
        /// </summary>
        private string _kernelCls1Filepath;

        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The DaSiamRPNTracker.
        /// </summary>
        private DaSiamRPNTracker _tracker;

        /// <summary>
        /// The tracking color.
        /// </summary>
        private Scalar _trackingColor = new Scalar(255, 255, 0);

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
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _netFilepath = await OpenCVEnv.GetFilePathTaskAsync(NET_FILENAME, cancellationToken: _cts.Token);
            _kernelR1Filepath = await OpenCVEnv.GetFilePathTaskAsync(KERNEL_R1_FILENAME, cancellationToken: _cts.Token);
            _kernelCls1Filepath = await OpenCVEnv.GetFilePathTaskAsync(KERNEL_CLS1_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            if (string.IsNullOrEmpty(_netFilepath) || string.IsNullOrEmpty(_kernelR1Filepath) || string.IsNullOrEmpty(_kernelCls1Filepath))
            {
                Debug.LogError(NET_FILENAME + " or " + KERNEL_R1_FILENAME + " or " + KERNEL_CLS1_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
            }
            else
            {
                _tracker = new DaSiamRPNTracker(_netFilepath, _kernelR1Filepath, _kernelCls1Filepath);
            }

            if (string.IsNullOrEmpty(_multiSource2MatHelper.RequestedVideoFilePath))
                _multiSource2MatHelper.RequestedVideoFilePath = VIDEO_FILENAME;
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGB; // DaSiamRPNTracker API must handle 3 channels Mat image.
            _multiSource2MatHelper.Initialize();

            OpenCVDebug.SetDebugMode(false);
        }

        private void Update()
        {
            if (!_multiSource2MatHelper.IsInitialized())
                return;

            if (_tracker == null)
            {
                if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
                {
                    Mat rgbMat = _multiSource2MatHelper.GetMat();

                    Imgproc.putText(rgbMat, "model file is not loaded.", new Point(5, rgbMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbMat, "Please read console message.", new Point(5, rgbMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                    OpenCVMatUtils.MatToTexture2D(rgbMat, _texture);
                }
                return;
            }

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
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    _storedTouchPoint = new Point(mouse.position.ReadValue().x, mouse.position.ReadValue().y);
                }
            }
#endif
#else
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            //Touch
            int touchCount = Input.touchCount;
            if (touchCount == 1)
            {
                Touch t = Input.GetTouch(0);
                if(t.phase == TouchPhase.Ended && !EventSystem.current.IsPointerOverGameObject(t.fingerId)) {
                    _storedTouchPoint = new Point(t.position.x, t.position.y);
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
                if (_multiSource2MatHelper.IsPaused())
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
                        using (Mat selectedPointMat = new MatOfPoint(_selectedPointList.ToArray()))
                        {
                            Rect region = Imgproc.boundingRect(selectedPointMat);
                            try
                            {
                                _tracker.Init(rgbMat, ConvertToCenterRef(region));
                            }
                            catch (Exception e)
                            {
                                Debug.Log(e);
                            }

                            _selectedPointList.Clear();
                        }
                    }

                    if (_tracker.IsInitialized)
                    {
                        Rect new_region = _tracker.Update(rgbMat);

                        if (_tracker.Score > 0.5)
                        {
                            // draw tracked objects regions.
                            Imgproc.rectangle(rgbMat, ConvertToTopLeftRef(new_region), _trackingColor, 2, 1, 0);
                        }
                        else
                        {
                            _tracker.Reset();
                        }
                    }

                    if (_selectedPointList.Count != 1)
                    {
                        //Imgproc.putText (rgbMat, "Please touch the screen, and select tracking regions.", new Point (5, rgbMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        if (_fpsMonitor != null)
                        {
                            _fpsMonitor.ConsoleText = "Please touch the screen, and select tracking regions.";
                        }
                    }
                    else
                    {
                        //Imgproc.putText (rgbMat, "Please select the end point of the new tracking region.", new Point (5, rgbMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        if (_fpsMonitor != null)
                        {
                            _fpsMonitor.ConsoleText = "Please select the end point of the new tracking region.";
                        }
                    }

                    OpenCVMatUtils.MatToTexture2D(rgbMat, _texture);
                }
            }
            else
            {
                if (!_multiSource2MatHelper.IsPaused())
                    _multiSource2MatHelper.Pause();

                if (_storedTouchPoint != null)
                {
                    ConvertScreenPointToTexturePoint(_storedTouchPoint, _storedTouchPoint, gameObject, _texture.width, _texture.height);
                    OnTouch(_storedTouchPoint, _texture.width, _texture.height);
                    _storedTouchPoint = null;
                }
            }
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            EnhancedTouchSupport.Enable();
#endif
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            EnhancedTouchSupport.Disable();
#endif
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;

            _tracker?.Dispose();

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


            _selectedPointList = new List<Point>();
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
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
        /// Raises the reset tracker button click event.
        /// </summary>
        public void OnResetTrackerButtonClick()
        {
            _tracker.Reset();
            _selectedPointList.Clear();
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

        private Rect ConvertToCenterRef(Rect r)
        {
            return new Rect(r.x + r.width / 2, r.y + r.height / 2, r.width, r.height);
        }

        private Rect ConvertToTopLeftRef(Rect r)
        {
            return new Rect(r.x - r.width / 2, r.y - r.height / 2, r.width, r.height);
        }
    }


    public class DaSiamRPNTracker
    {
        // Private Fields
        private string _windowing = "cosine";
        private int _exemplarSize = 127;
        private int _instanceSize = 271;
        private int _totalStride = 8;
        private int _scoreSize;
        private float _contextAmount = 0.5f;
        private float[] _ratios = new float[] { 0.33f, 0.5f, 1f, 2f, 3f };
        private float[] _scales = new float[] { 8f };
        private int _anchorNum;
        private float _penaltyK = 0.055f;
        private float _windowInfluence = 0.42f;
        private float _lr = 0.295f;

        private Mat _window;

        private Net _net;
        private Net _kernelR1;
        private Net _kernelCls1;

        private int _imH;
        private int _imW;
        private Point _targetPos;
        private Size _targetSz;
        private Scalar _avgChans;
        private Mat _anchor;

        private Mat _trackerEvalScoreR1_0;
        private Mat _trackerEvalTmpR1_0;
        private Mat _trackerEvalTmpR1_1;
        private Mat _trackerEvalTmpR1_2;

        private Mat _trackerEvalFuncTmpR1_0;
        private Mat _trackerEvalFuncTmpR1_1;
        private Mat _trackerEvalFuncTmpR2_0;
        private Mat _trackerEvalFuncTmpR2_1;

        private List<string> _outNames;
        private List<Mat> _outBlobs = new List<Mat>();

        private Mat _teImTmp;
        private Mat _imPatchOriginalResize;

        // Protected Fields
        protected double _score;
        public double Score
        {
            get { return _score; }
        }

        protected bool _isInitialized;
        public bool IsInitialized
        {
            get { return _isInitialized; }
        }

        protected bool _isDisposed;
        public bool IsDisposed
        {
            get { return _isDisposed; }
        }

        // Constructor
        public DaSiamRPNTracker(string netFilepath, string kernelR1Filepath, string kernelCls1Filepath)
        {
            _scoreSize = (int)((_instanceSize - _exemplarSize) / _totalStride) + 1;
            _anchorNum = _ratios.Length * _scales.Length;

            Mat window;
            if (_windowing == "cosine")
            {
                _exemplarSize = 127;
                _instanceSize = 271;
                _totalStride = 8;
                _scoreSize = 19;

                Mat hanning19Mat = new Mat(1, 19, CvType.CV_32FC1);
                hanning19Mat.put(0, 0, new float[] { 0f, 0.03015369f, 0.11697778f, 0.25f, 0.41317591f, 0.58682409f, 0.75f, 0.88302222f, 0.96984631f, 1f, 0.96984631f,
                0.88302222f, 0.75f, 0.58682409f, 0.41317591f, 0.25f, 0.11697778f, 0.03015369f, 0f });
                window = Outer(hanning19Mat, hanning19Mat);
            }
            else
            {
                window = Mat.ones(_scoreSize, _scoreSize, CvType.CV_32FC1);
            }
            Mat windowFlatten = Flatten(window);
            _window = new Mat(windowFlatten.rows() * 1, windowFlatten.cols() * _anchorNum, window.type());
            Tile(windowFlatten, 1, _anchorNum, _window);


            // # Loading network`s and kernel`s models
            _net = Dnn.readNet(netFilepath);
            _kernelR1 = Dnn.readNet(kernelR1Filepath);
            _kernelCls1 = Dnn.readNet(kernelCls1Filepath);

            if (_net.empty())
            {
                Debug.LogError("model file is not loaded. The model and class names list can be downloaded here: \"https://www.dropbox.com/s/rr1lk9355vzolqv/dasiamrpn_model.onnx?dl=0\". Please copy to \"Assets/StreamingAssets/OpenCVForUnityExamples/dnn/\" folder. ");
            }
            if (_kernelR1.empty())
            {
                Debug.LogError("model file is not loaded. The model and class names list can be downloaded here: \"https://www.dropbox.com/s/999cqx5zrfi7w4p/dasiamrpn_kernel_r1.onnx?dl=0\". Please copy to \"Assets/StreamingAssets/OpenCVForUnityExamples/dnn/\" folder. ");
            }
            if (_kernelCls1.empty())
            {
                Debug.LogError("model file is not loaded. The model and class names list can be downloaded here: \"https://www.dropbox.com/s/qvmtszx5h339a0w/dasiamrpn_kernel_cls1.onnx?dl=0\". Please copy to \"Assets/StreamingAssets/OpenCVForUnityExamples/dnn/\" folder. ");
            }
        }

        // Public Methods
        public void Init(Mat im, Rect initBb)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            _imH = im.height();
            _imW = im.width();

            int bbX = Mathf.Clamp(initBb.x, 0, _imW);
            int bbY = Mathf.Clamp(initBb.y, 0, _imH);
            int bbW = Mathf.Clamp(initBb.width, 0, _imW);
            int bbH = Mathf.Clamp(initBb.height, 0, _imH);
            _targetPos = new Point(bbX, bbY);
            _targetSz = new Size(bbW, bbH);

            _avgChans = Core.mean(im);
            _avgChans = new Scalar(Math.Floor(_avgChans.val[0]), Math.Floor(_avgChans.val[1]), Math.Floor(_avgChans.val[2]), Math.Floor(_avgChans.val[3]));

            // # When we trying to generate ONNX model from the pre-trained .pth model
            // # we are using only one state of the network. In our case used state
            // # with big bounding box, so we were forced to add assertion for
            // # too small bounding boxes - current state of the network can not
            // # work properly with such small bounding boxes
            if (_targetSz.width * _targetSz.height / (float)(_imH * _imW) < 0.004)
                throw new Exception("Initializing BB is too small-try to restart tracker with larger BB");


            _anchor = GenerateAnchor();

            double wcZ = _targetSz.width + _contextAmount * (_targetSz.width + _targetSz.height);
            double hcZ = _targetSz.height + _contextAmount * (_targetSz.width + _targetSz.height);
            int sZ = (int)Math.Round(Math.Sqrt(wcZ * hcZ));

            Mat zCrop = GetSubwindowTracking(im, _exemplarSize, sZ);
            zCrop = Dnn.blobFromImage(zCrop);

            _net.setInput(zCrop);
            Mat zF = _net.forward("onnx_node_output_0!63");
            _kernelR1.setInput(zF);
            Mat r1 = _kernelR1.forward();
            _kernelCls1.setInput(zF);
            Mat cls1 = _kernelCls1.forward();
            r1 = r1.reshape(1, new int[] { 20, 256, 4, 4 });
            cls1 = cls1.reshape(1, new int[] { 10, 256, 4, 4 });

            _net.setParam(_net.getLayerId("onnx_node_output_0!65"), 0, r1);
            _net.setParam(_net.getLayerId("onnx_node_output_0!68"), 0, cls1);

            _isInitialized = true;
        }

        public Rect Update(Mat im)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);

            if (!IsInitialized)
                return new Rect();

            double wcZ = _targetSz.height + _contextAmount * (_targetSz.width + _targetSz.height);
            double hcZ = _targetSz.width + _contextAmount * (_targetSz.width + _targetSz.height);
            double sZ = Math.Sqrt(wcZ * hcZ);
            double scaleZ = _exemplarSize / sZ;
            float dSearch = (_instanceSize - _exemplarSize) / 2f;
            double pad = dSearch / scaleZ;
            int sX = (int)Math.Round(sZ + 2.0 * pad);

            // # Region preprocessing part
            Mat xCrop = GetSubwindowTracking(im, _instanceSize, sX);
            xCrop = Dnn.blobFromImage(xCrop);

            _score = TrackerEval(xCrop, scaleZ);

            _targetPos.x = Math.Max(0, Math.Min(_imW, _targetPos.x));
            _targetPos.y = Math.Max(0, Math.Min(_imH, _targetPos.y));
            _targetSz.width = Math.Max(10, Math.Min(_imW, _targetSz.width));
            _targetSz.height = Math.Max(10, Math.Min(_imH, _targetSz.height));

            return new Rect(_targetPos, _targetSz);
        }

        public void Reset()
        {
            _isInitialized = false;

            _anchor?.Dispose(); _anchor = null;
        }

        public void Dispose()
        {
            Reset();

            _isDisposed = true;

            _window?.Dispose(); _window = null;
            _net?.Dispose(); _net = null;
            _kernelR1?.Dispose(); _kernelR1 = null;
            _kernelCls1?.Dispose(); _kernelCls1 = null;

            if (_trackerEvalScoreR1_0 != null)
            {
                _trackerEvalScoreR1_0.Dispose();
                _trackerEvalTmpR1_0.Dispose();
                _trackerEvalTmpR1_1.Dispose();
                _trackerEvalTmpR1_2.Dispose();

                _trackerEvalFuncTmpR1_0.Dispose();
                _trackerEvalFuncTmpR1_1.Dispose();
                _trackerEvalFuncTmpR2_0.Dispose();
                _trackerEvalFuncTmpR2_1.Dispose();
            }
        }

        // Private Methods
        private Mat GenerateAnchor()
        {
            Mat anchor;
            int score_sz = (int)_scoreSize;

            using (Mat __anchor = Mat.zeros(_anchorNum, 4, CvType.CV_32FC1))
            {
                int size = _totalStride * _totalStride;
                int count = 0;

                foreach (float ratio in _ratios)
                {
                    int ws = (int)(Mathf.Sqrt(size / ratio));
                    int hs = (int)(ws * ratio);
                    foreach (float scale in _scales)
                    {
                        float wws = ws * scale;
                        float hhs = hs * scale;
                        __anchor.put(count, 0, new float[] { 0, 0, wws, hhs });
                        count += 1;
                    }
                }

                using (Mat __anchor_tile = new Mat(__anchor.rows() * 1, __anchor.cols() * score_sz * score_sz, __anchor.type()))
                {
                    Tile(__anchor, 1, score_sz * score_sz, __anchor_tile);
                    anchor = __anchor_tile.reshape(1, _anchorNum * score_sz * score_sz);
                }
            }

            float ori = -(score_sz / 2f) * _totalStride;

            float[] _xx_arr = new float[score_sz];
            for (int dx = 0; dx < score_sz; dx++)
            {
                _xx_arr[dx] = ori + _totalStride * dx;
            }
            using (Mat _xx = new Mat(1, score_sz, CvType.CV_32FC1))
            {
                _xx.put(0, 0, _xx_arr);

                using (Mat _xx_tile = new Mat(_xx.rows() * _xx.cols(), _xx.cols() * 1, _xx.type()))
                {
                    Tile(_xx, _xx.cols(), 1, _xx_tile);
                    using (Mat _xx_tile_t = _xx_tile.t())
                    using (Mat __xx = Flatten(_xx_tile))
                    using (Mat __yy = Flatten(_xx_tile_t))
                    using (Mat __xx_tile = new Mat(__xx.rows() * _anchorNum, __xx.cols() * 1, __xx.type()))
                    using (Mat __yy_tile = new Mat(__yy.rows() * _anchorNum, __yy.cols() * 1, __yy.type()))
                    {
                        Tile(__xx, _anchorNum, 1, __xx_tile);
                        Tile(__yy, _anchorNum, 1, __yy_tile);

                        using (Mat xx = __xx_tile.reshape(1, anchor.rows()))
                        using (Mat yy = __yy_tile.reshape(1, anchor.rows()))
                        using (Mat _anchor_roi_c0 = anchor.col(0))
                        using (Mat _anchor_roi_c1 = anchor.col(1))
                        {
                            xx.copyTo(_anchor_roi_c0);
                            yy.copyTo(_anchor_roi_c1);
                        }
                    }
                }
            }

            return anchor.t(); // Return a transposed anchor.
        }

        /// <summary>
        /// change.
        /// </summary>
        /// <param name="r">Mat[1*C]</param>
        /// <param name="dst">Mat[1*C]</param>
        private void Change(Mat r, Mat dst)
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

            Mat tmp_r1_0 = _trackerEvalFuncTmpR1_0;

            Core.divide(1.0, r, tmp_r1_0);
            Core.max(r, tmp_r1_0, dst);
        }

        /// <summary>
        /// sz.
        /// </summary>
        /// <param name="w">Mat[1*C]</param>
        /// <param name="h">Mat[1*C]</param>
        /// <param name="dst">Mat[1*C]</param>
        private void Sz(Mat w, Mat h, Mat dst)
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

            Mat tmp_r1_0 = _trackerEvalFuncTmpR1_0;
            Mat tmp_r1_1 = _trackerEvalFuncTmpR1_1;

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
        private double SzWh(Size wh)
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
        private void Softmax(Mat x, Mat dst)
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

            Mat tmp_r1_0 = _trackerEvalFuncTmpR1_0;
            Mat tmp_r2_0 = _trackerEvalFuncTmpR2_0;
            Mat tmp_r2_1 = _trackerEvalFuncTmpR2_1;

            MaxAxis0(x, tmp_r1_0);
            Tile(tmp_r1_0, 2, 1, tmp_r2_0); // x_max

            Core.subtract(x, tmp_r2_0, tmp_r2_0);
            Core.exp(tmp_r2_0, tmp_r2_0); // e_x

            SumAxis0(tmp_r2_0, tmp_r1_0);
            Tile(tmp_r1_0, 2, 1, tmp_r2_1); // e_x_sum

            Core.divide(tmp_r2_0, tmp_r2_1, tmp_r2_0); // y

            using (Mat x_x_max_subtract_roi_r1 = tmp_r2_0.row(1))
            {
                x_x_max_subtract_roi_r1.copyTo(dst);
            }
        }

        // # Reshaping cropped image for using in the model
        private Mat GetSubwindowTracking(Mat im, int model_size, int original_sz)
        {
            Size im_sz = im.size();
            double _c = (original_sz + 1) / 2.0;

            int context_xmin = (int)Math.Round(_targetPos.x - _c);
            int context_xmax = context_xmin + original_sz - 1;
            int context_ymin = (int)Math.Round(_targetPos.y - _c);
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


            double wc_z = im.height() + _contextAmount * (im.width() + im.height());
            double hc_z = im.width() + _contextAmount * (im.width() + im.height());
            double s_z = Math.Sqrt(wc_z * hc_z);
            double scale_z = _exemplarSize / s_z;
            float d_search = (_instanceSize - _exemplarSize) / 2f;
            double pad = d_search / scale_z;
            int te_im_tmp_sz = (int)Math.Round(s_z + 2.0 * pad);

            if (_teImTmp == null || _teImTmp.rows() != te_im_tmp_sz || _teImTmp.cols() != te_im_tmp_sz)
            {
                _teImTmp = new Mat(te_im_tmp_sz, te_im_tmp_sz, im.type());
            }


            Mat im_patch_original;

            if (top_pad > 0 || bot_pad > 0 || left_pad > 0 || right_pad > 0)
            {
                using (Mat te_im = new Mat(_teImTmp, new Rect(0, 0, c + left_pad + right_pad, r + top_pad + bot_pad)))
                using (Mat te_im_roi = new Mat(te_im, new Rect(left_pad, top_pad, c, r)))
                {
                    im.copyTo(te_im_roi);

                    if (top_pad > 0)
                    {
                        using (Mat te_im_roi2 = new Mat(te_im, new Rect(left_pad, 0, c, top_pad)))
                        {
                            te_im_roi2.setTo(_avgChans);
                        }
                    }
                    if (bot_pad > 0)
                    {
                        using (Mat te_im_roi2 = new Mat(te_im, new Rect(left_pad, r + top_pad, c, te_im.rows() - (r + top_pad))))
                        {
                            te_im_roi2.setTo(_avgChans);
                        }
                    }
                    if (left_pad > 0)
                    {
                        using (Mat te_im_roi2 = new Mat(te_im, new Rect(0, 0, left_pad, te_im.rows())))
                        {
                            te_im_roi2.setTo(_avgChans);
                        }
                    }
                    if (right_pad > 0)
                    {
                        using (Mat te_im_roi2 = new Mat(te_im, new Rect(c + left_pad, 0, te_im.cols() - (c + left_pad), te_im.rows())))
                        {
                            te_im_roi2.setTo(_avgChans);
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
                if (_imPatchOriginalResize == null || _imPatchOriginalResize.rows() != model_size || _imPatchOriginalResize.cols() != model_size)
                {
                    _imPatchOriginalResize = new Mat(model_size, model_size, im_patch_original.type());
                }

                Imgproc.resize(im_patch_original, _imPatchOriginalResize, new Size(model_size, model_size));
                im_patch_original.Dispose();

                return _imPatchOriginalResize;
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
        private Mat Outer(Mat a, Mat b)
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
        private Mat Flatten(Mat a)
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
        private void Tile(Mat a, int ny, int nx, Mat dst)
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
        private void MaxAxis0(Mat a, Mat dst)
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
        private void ArgmaxAxis1(Mat a, Mat dst)
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
            OpenCVMatUtils.CopyToMat(dstArr, dst);
        }

        /// <summary>
        /// Sum of array elements over a given axis.
        /// </summary>
        /// <param name="a">Mat[2*C]</param>
        /// <param name="dst">Mat[1*C]</param>
        private void SumAxis0(Mat a, Mat dst)
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

        private double TrackerEval(Mat x_crop, double scale_z)
        {
            Size target_size = _targetSz * scale_z;
            _net.setInput(x_crop);

            if (_outNames == null)
            {
                _outNames = _net.getUnconnectedOutLayersNames();
                _outNames[0] = "66";
                _outNames[1] = "68";
            }

            _net.forward(_outBlobs, _outNames);
            Mat delta = _outBlobs[0];
            Mat _score = _outBlobs[1];

            delta = delta.reshape(1, new int[] { 4, (int)delta.total() / 4 });
            _score = _score.reshape(1, new int[] { 2, (int)_score.total() / 2 });

            int cols = delta.cols();
            int type = delta.type();

            if (_trackerEvalScoreR1_0 == null)
                _trackerEvalScoreR1_0 = new Mat(1, cols, type);
            if (_trackerEvalTmpR1_0 == null)
                _trackerEvalTmpR1_0 = new Mat(1, cols, type);
            if (_trackerEvalTmpR1_1 == null)
                _trackerEvalTmpR1_1 = new Mat(1, cols, type);
            if (_trackerEvalTmpR1_2 == null)
                _trackerEvalTmpR1_2 = new Mat(1, cols, type);

            if (_trackerEvalFuncTmpR1_0 == null)
                _trackerEvalFuncTmpR1_0 = new Mat(1, cols, type);
            if (_trackerEvalFuncTmpR1_1 == null)
                _trackerEvalFuncTmpR1_1 = new Mat(1, cols, type);
            if (_trackerEvalFuncTmpR2_0 == null)
                _trackerEvalFuncTmpR2_0 = new Mat(2, cols, type);
            if (_trackerEvalFuncTmpR2_1 == null)
                _trackerEvalFuncTmpR2_1 = new Mat(2, cols, type);


            Mat score = _trackerEvalScoreR1_0;
            Softmax(_score, score);


            Mat tmp_r1_0 = _trackerEvalTmpR1_0;
            Mat tmp_r1_1 = _trackerEvalTmpR1_1;
            Mat tmp_r1_2 = _trackerEvalTmpR1_2;


            //delta[0, :] = delta[0, :] * self.anchor[:, 2] + self.anchor[:, 0]
            //delta[1, :] = delta[1, :] * self.anchor[:, 3] + self.anchor[:, 1]
            //delta[2, :] = np.exp(delta[2, :]) * self.anchor[:, 2]
            //delta[3, :] = np.exp(delta[3, :]) * self.anchor[:, 3]

            using (Mat delta_roi_r0 = delta.row(0))
            using (Mat delta_roi_r1 = delta.row(1))
            using (Mat delta_roi_r2 = delta.row(2))
            using (Mat delta_roi_r3 = delta.row(3))
            using (Mat anchor_roi_r0 = _anchor.row(0))
            using (Mat anchor_roi_r1 = _anchor.row(1))
            using (Mat anchor_roi_r2 = _anchor.row(2))
            using (Mat anchor_roi_r3 = _anchor.row(3))
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

            double target_size_sz_wh = SzWh(target_size);
            using (Mat delta_roi_r2 = delta.row(2))
            using (Mat delta_roi_r3 = delta.row(3))
            {
                Sz(delta_roi_r2, delta_roi_r3, tmp_r1_0);
                Core.divide(tmp_r1_0, new Scalar(target_size_sz_wh), tmp_r1_0);
                Change(tmp_r1_0, tmp_r1_1); // s_c


                Core.divide(delta_roi_r2, delta_roi_r3, tmp_r1_0);
                Core.divide(target_size.width / target_size.height, tmp_r1_0, tmp_r1_0);
                Change(tmp_r1_0, tmp_r1_2); // r_c


                Core.multiply(tmp_r1_2, tmp_r1_1, tmp_r1_2);
                Core.subtract(tmp_r1_2, new Scalar(1.0), tmp_r1_2);
                Core.multiply(tmp_r1_2, new Scalar(_penaltyK), tmp_r1_2, -1.0);
                Core.exp(tmp_r1_2, tmp_r1_2);
                Mat penalty = tmp_r1_2; // penalty


                Core.multiply(penalty, score, tmp_r1_0);
                Core.multiply(tmp_r1_0, new Scalar(1 - _windowInfluence), tmp_r1_0);
                Core.multiply(_window, new Scalar(_windowInfluence), tmp_r1_1);
                Core.add(tmp_r1_0, tmp_r1_1, tmp_r1_0);
                Mat pscore = tmp_r1_0; // pscore

                using (Mat pscore_argmax = new Mat(1, 1, type))
                {
                    ArgmaxAxis1(pscore, pscore_argmax);

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
            double lr = penalty_best_pscore * score.get(0, best_pscore_id)[0] * _lr;
            double res_x = target[0] + _targetPos.x;
            double res_y = target[1] + _targetPos.y;
            double res_w = target_size.width * (1.0 - lr) + target[2] * lr;
            double res_h = target_size.height * (1.0 - lr) + target[3] * lr;
            _targetPos = new Point(res_x, res_y);
            _targetSz = new Size(res_w, res_h);

            return score.get(0, best_pscore_id)[0];
        }
    }
}

#endif
