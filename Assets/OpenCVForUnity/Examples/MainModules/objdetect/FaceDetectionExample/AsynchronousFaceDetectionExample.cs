using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.MOT;
using OpenCVForUnity.Extensions.MOT.ByteTrack;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using OpenCVForUnity.XobjdetectModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;
#if UNITY_WEBGL
using System.Collections;
#endif

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Asynchronous Face Detection Example
    /// Combines background full-frame cascade detection with main-thread local refinement and ByteTrack tracking.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Background Task-based Haar detection to keep the UI thread responsive
    /// - Fast LBP cascade detection inside tracked regions on the main thread
    /// - BYTETracker multi-object tracking with stable face IDs across frames
    /// - WebGL fallback using a coroutine instead of multi-threading
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="CascadeClassifier"/>, <see cref="MatOfRect"/>, <see cref="Objdetect"/>
    /// - <see cref="Imgproc"/>: cvtColor, equalizeHist, rectangle
    /// - <see cref="BYTETracker"/>, <see cref="MultiSourceToMatHelper"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// http://docs.opencv.org/3.2.0/db/d28/tutorial_cascade_classifier.html
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class AsynchronousFaceDetectionExample : MonoBehaviour
    {
        // Constants
        private static readonly string LBP_CASCADE_FRONTALFACE_FILEPATH = "OpenCVForUnityExamples/objdetect/lbpcascade_frontalface.xml";
        //private static readonly string LBP_CASCADE_FRONTALFACE_FILEPATH = "OpenCVForUnityExamples/objdetect/haarcascade_frontalface_alt.xml";

        private static readonly string HAAR_CASCADE_FRONTALFACE_FILEPATH = "OpenCVForUnityExamples/objdetect/haarcascade_frontalface_alt.xml";

        private static readonly (int, int, int, int) YELLOW_COLOR_TUPLE = (255, 255, 0, 255);
        private static readonly (int, int, int, int) GREEN_COLOR_TUPLE = (0, 255, 0, 255);
#if UNITY_WEBGL
        private static readonly (int, int, int, int) WHITE_COLOR_TUPLE = (255, 255, 255, 255);
#endif

        // Detection and tracking parameters
        private static readonly float COEFF_TRACKING_WINDOW_SIZE = 2.0f;                    // Multiplier for expanding tracking window size
        private static readonly float COEFF_OBJECT_MIN_SIZE_TO_TRACK = 0.85f;               // Minimum object size ratio for local region detection
        private static readonly float COEFF_OBJECT_MIN_SIZE_TO_TRACK_BACKGROUND = 0.1f;    // Minimum object size ratio for background detection (10% of image)
        private static readonly float COEFF_OBJECT_MAX_SIZE_TO_TRACK_BACKGROUND = 0.8f;     // Maximum object size ratio for background detection (80% of image)

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
        private Mat _grayMat;
        private Texture2D _texture;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private CascadeClassifier _cascade;
        private string _lbpCascadeFilepath;
        private string _haarCascadeFilepath;
        private Rect[] _rectsWhereRegions;
        private List<Rect> _detectedObjectsInRegions = new List<Rect>();
        private BYTETracker _byteTracker;
        private BYTETrackInfoVisualizer _byteTrackInfoVisualizer;

        // Modern async/await pattern fields
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _detectionTask;

#if !UNITY_WEBGL
        private readonly ConcurrentQueue<Mat> _detectionQueue = new ConcurrentQueue<Mat>();
        private volatile bool _shouldDetect = false;
        private volatile bool _isDetectionRunning = false;
        private MatOfRect _latestDetectionResult;
        private readonly object _detectionResultLock = new object();
#else
        // WebGL fallback fields
        private CascadeClassifier _cascade4Thread;
        private Mat _grayMat4Thread;
        private bool _shouldDetectInMultiThread = false;
        private bool _didUpdateTheDetectionResult = false;
        private MatOfRect _detectionResult;
#endif

        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            WireSourceToMatControlPanelHooks();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            _lbpCascadeFilepath = await OpenCVForUnityEnv.GetFilePathAsync(LBP_CASCADE_FRONTALFACE_FILEPATH, cancellationToken: _cts.Token);
            _haarCascadeFilepath = await OpenCVForUnityEnv.GetFilePathAsync(HAAR_CASCADE_FRONTALFACE_FILEPATH, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            if (string.IsNullOrEmpty(_lbpCascadeFilepath) || string.IsNullOrEmpty(_haarCascadeFilepath))
            {
                Debug.LogError(LBP_CASCADE_FRONTALFACE_FILEPATH + " or " + HAAR_CASCADE_FRONTALFACE_FILEPATH + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "cascade classifier file is not loaded.\nPlease read console message.";
                }

                return;
            }

            _multiSourceToMatHelper.Initialize();
        }

        private void Update()
        {
#if !UNITY_WEBGL
            if (_detectionTask == null || _detectionTask.IsCompleted)
            {
                return;
            }
#endif

            if (_multiSourceToMatHelper.IsPlaying && _multiSourceToMatHelper.DidUpdateThisFrame)
            {
                if (_texture == null)
                {
                    return;
                }

                Mat rgbaMat = _multiSourceToMatHelper.FrameMat;
                if (rgbaMat == null)
                {
                    return;
                }

                if (rgbaMat.cols() != _texture.width || rgbaMat.rows() != _texture.height)
                {
                    return;
                }

                if (_cascade != null)
                {
                    // Background thread owns Haar full-frame detection; main thread uses LBP in ROIs only.
                    Imgproc.cvtColor(rgbaMat, _grayMat, Imgproc.COLOR_RGBA2GRAY);
                    Imgproc.equalizeHist(_grayMat, _grayMat);

#if UNITY_WEBGL
                    // WebGL: Synchronous processing (no Task.Run)
                    if (!_shouldDetectInMultiThread)
                    {
                        _grayMat.copyTo(_grayMat4Thread);
                        _shouldDetectInMultiThread = true;
                    }
#else
                    // Queue one gray frame clone per cycle; worker consumes it on a background thread.
                    if (!_shouldDetect && !_isDetectionRunning)
                    {
                        var grayMatCopy = _grayMat.clone();
                        _detectionQueue.Enqueue(grayMatCopy);
                        _shouldDetect = true;
                    }
#endif

                    Rect[] rects;

#if UNITY_WEBGL
                    // WebGL: Check for detection results
                    if (_didUpdateTheDetectionResult)
                    {
                        _didUpdateTheDetectionResult = false;
                        //Debug.Log("get _rectsWhereRegions were got from resultDetect");
                        _rectsWhereRegions = _detectionResult.toArray();
                        _detectionResult.Dispose();
                        _detectionResult = null;
#else
                    // Check for new detection results
                    MatOfRect detectionResult = null;
                    lock (_detectionResultLock)
                    {
                        if (_latestDetectionResult != null)
                        {
                            detectionResult = _latestDetectionResult;
                            _latestDetectionResult = null;
                        }
                    }

                    if (detectionResult != null)
                    {
                        //Debug.Log("get _rectsWhereRegions were got from resultDetect");

                        _rectsWhereRegions = detectionResult.toArray();
                        detectionResult.Dispose();
#endif

                        rects = _rectsWhereRegions;
                        // Yellow = fresh full-frame detections from the background worker.
                        for (int i = 0; i < rects.Length; i++)
                        {
                            Imgproc.rectangle(rgbaMat, (rects[i].x, rects[i].y),
                                                 (rects[i].x + rects[i].width, rects[i].y + rects[i].height), YELLOW_COLOR_TUPLE, 1);
                        }
                    }
                    else
                    {
                        // Green = tracker-predicted regions until the next full-frame detection arrives.
                        BYTETrackInfo[] activeTracks = _byteTracker.GetActiveTrackInfos();
                        _rectsWhereRegions = new Rect[activeTracks.Length];

                        for (int i = 0; i < activeTracks.Length; i++)
                        {
                            BBox bbox = activeTracks[i].BBox;
                            _rectsWhereRegions[i] = new Rect((int)bbox.X, (int)bbox.Y, (int)bbox.Width, (int)bbox.Height);
                        }

                        rects = _rectsWhereRegions;
                        for (int i = 0; i < rects.Length; i++)
                        {
                            Imgproc.rectangle(rgbaMat, (rects[i].x, rects[i].y),
                                                 (rects[i].x + rects[i].width, rects[i].y + rects[i].height), GREEN_COLOR_TUPLE, 1);
                        }
                    }

                    _detectedObjectsInRegions.Clear();
                    if (_rectsWhereRegions.Length > 0)
                    {
                        int len = _rectsWhereRegions.Length;
                        for (int i = 0; i < len; i++)
                        {
                            DetectInRegion(_grayMat, _rectsWhereRegions[i], _detectedObjectsInRegions);
                        }
                    }

                    // update tracking info with BYTETracker
                    BBox[] detections = ConvertToBBoxes(_detectedObjectsInRegions);
                    _byteTracker.Update(detections);

                    // visualize result
                    BYTETrackInfo[] trackInfos = _byteTracker.GetActiveTrackInfos();
                    _byteTrackInfoVisualizer.Visualize(rgbaMat, trackInfos, false, true);

#if UNITY_WEBGL
                    Imgproc.putText(rgbaMat, "WebGL platform does not support multi-threading.",
                                     (5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, WHITE_COLOR_TUPLE, 1, Imgproc.LINE_AA, false);
#endif

                }

                OpenCVMatUnityUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

#if UNITY_WEBGL
        private void OnDestroy()
        {
            // WebGL: Stop coroutine
            StopCoroutine("WebGLThreadWorker");
#else
        private async void OnDestroy()
        {
            await StopDetectionAsync();
#endif

            UnwireSourceToMatControlPanelHooks();

            _cts?.Cancel();

            _byteTracker?.Dispose();
            _byteTracker = null;
            _byteTrackInfoVisualizer?.Dispose();
            _byteTrackInfoVisualizer = null;

            _cts?.Dispose();
            _cts = null;
        }

        // Public Methods
#if UNITY_WEBGL
        /// <summary>
        /// Raises the helper initialized event.
        /// Recreates the preview texture and starts playback on first initialization.
        /// Skips Play when re-initialization has already restored Playing or Paused.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
#else
        /// <summary>
        /// Raises the helper initialized event.
        /// Recreates the preview texture and starts playback on first initialization.
        /// Skips Play when re-initialization has already restored Playing or Paused.
        /// </summary>
        public async void OnSourceToMatHelperInitialized()
        {
#endif

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
            }

#if UNITY_WEBGL
            // WebGL: Initialize synchronous processing
            InitWebGLThread();
#else
            await RestartDetectionAsync();
#endif

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

#if UNITY_WEBGL
            StopCoroutine("WebGLThreadWorker");
#else
            CancelDetectionSynchronously();
#endif

            DisposeFrameProcessingResources();
            CleanupPreviewResources();
        }

#if UNITY_WEBGL
        /// <summary>
        /// Raises the helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed", this);

            // WebGL: Stop coroutine
            StopCoroutine("WebGLThreadWorker");
            _grayMat4Thread?.Dispose();
            _grayMat4Thread = null;
            _cascade4Thread?.Dispose();
            _cascade4Thread = null;

            _grayMat?.Dispose();
            _grayMat = null;
            _cascade?.Dispose();
            _cascade = null;

            CleanupPreviewResources();

            _byteTracker?.Dispose();
            _byteTracker = null;
            _byteTrackInfoVisualizer?.Dispose();
            _byteTrackInfoVisualizer = null;
        }
#else
        /// <summary>
        /// Raises the helper disposed event.
        /// </summary>
        public async void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed", this);

            await StopDetectionAsync();

            _grayMat?.Dispose();
            _grayMat = null;
            _cascade?.Dispose();
            _cascade = null;

            CleanupPreviewResources();

            _byteTracker?.Dispose();
            _byteTracker = null;
            _byteTrackInfoVisualizer?.Dispose();
            _byteTrackInfoVisualizer = null;
        }
#endif

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
            _grayMat?.Dispose();
            _grayMat = null;
            _cascade?.Dispose();
            _cascade = null;
        }

        private void CreateOrRecreateProcessingResources(Mat rgbaMat)
        {
            if (rgbaMat == null)
            {
                return;
            }

            _grayMat?.Dispose();
            _grayMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);

            _cascade?.Dispose();
            _cascade = null;
            if (string.IsNullOrEmpty(_lbpCascadeFilepath))
            {
                Debug.LogError(LBP_CASCADE_FRONTALFACE_FILEPATH + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("cascade classifier file is not loaded.\nPlease read console message.", 20000);
                }
            }
            else
            {
                _cascade = new CascadeClassifier(_lbpCascadeFilepath);
            }

            int fps = 30;
            if (_multiSourceToMatHelper.MatSource is ICameraMatSource cameraHelper)
            {
                fps = (int)cameraHelper.FPS;
            }
            else if (_multiSourceToMatHelper.MatSource is IVideoFileMatSource videoHelper)
            {
                fps = (int)videoHelper.FPS;
            }

            _byteTracker?.Dispose();
            _byteTracker = new BYTETracker(fps, 30, mot20: false);
            _byteTrackInfoVisualizer?.Dispose();
            _byteTrackInfoVisualizer = new BYTETrackInfoVisualizer();
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

        private void DetectInRegion(Mat img, Rect r, List<Rect> detectedObjectsInRegions)
        {
            Rect r0 = new Rect(0, 0, img.width(), img.height());
            Rect r1 = new Rect(r.x, r.y, r.width, r.height);
            Rect.inflate(r1, (int)((r1.width * COEFF_TRACKING_WINDOW_SIZE) - r1.width) / 2,
                (int)((r1.height * COEFF_TRACKING_WINDOW_SIZE) - r1.height) / 2);
            r1 = Rect.intersect(r0, r1);

            if (r1 != null && (r1.width <= 0) || (r1.height <= 0))
            {
                Debug.Log("Empty intersection", this);
                return;
            }

            using (MatOfRect tmpobjects = new MatOfRect())
            using (Mat img1 = new Mat(img, r1)) // submat view — coordinates in DetectInRegion are offset by r1 origin
            {
                int minSize = (int)(Mathf.Min(r.width, r.height) * COEFF_OBJECT_MIN_SIZE_TO_TRACK);
                _cascade.detectMultiScale(img1, tmpobjects, 1.05, 2,
                    0 | Xobjdetect.CASCADE_DO_CANNY_PRUNING | Xobjdetect.CASCADE_SCALE_IMAGE | Xobjdetect.CASCADE_FIND_BIGGEST_OBJECT, (minSize, minSize));

                Rect[] tmpobjectsArray = tmpobjects.toArray();
                for (int i = 0; i < tmpobjectsArray.Length; i++)
                {
                    Rect tmp = tmpobjectsArray[i];
                    Rect curres = new Rect(tmp.x + r1.x, tmp.y + r1.y, tmp.width, tmp.height);
                    detectedObjectsInRegions.Add(curres);
                }
            }
        }

        /// <summary>
        /// convert detection result to BBox array
        /// </summary>
        /// <param name="detectedObjects">detected objects list</param>
        /// <returns>BBox array</returns>
        private BBox[] ConvertToBBoxes(List<Rect> detectedObjects)
        {
            BBox[] bboxes = new BBox[detectedObjects.Count];
            for (int i = 0; i < detectedObjects.Count; i++)
            {
                Rect rect = detectedObjects[i];
                bboxes[i] = new BBox(rect.x, rect.y, rect.width, rect.height, 1.0f, 0);
            }
            return bboxes;
        }

#if !UNITY_WEBGL

        /// <summary>
        /// Starts the background detection task
        /// </summary>
        private void StartDetectionTask()
        {
            // Create a new CancellationTokenSource (existing ones may have been disposed)
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _detectionTask = Task.Run(async () => await DetectionWorker(), _cts.Token);
            Debug.Log("Detection task started", this);
        }

        /// <summary>
        /// Requests cancellation of the background detection task without awaiting completion.
        /// Used from <see cref="OnSourceToMatHelperReleased"/> before preview resources are torn down.
        /// </summary>
        private void CancelDetectionSynchronously()
        {
            _shouldDetect = false;

            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }

        /// <summary>
        /// Stops any running detection task and starts a new one.
        /// Called from <see cref="OnSourceToMatHelperInitialized"/> after preview resources are recreated.
        /// </summary>
        private async Task RestartDetectionAsync()
        {
            await StopDetectionAsync();
            StartDetectionTask();
        }

        /// <summary>
        /// Stops the background detection task asynchronously
        /// </summary>
        private async Task StopDetectionAsync()
        {
            if (_detectionTask == null || _detectionTask.IsCompleted)
            {
                return;
            }

            _cts.Cancel();
            _shouldDetect = false;

            try
            {
                await _detectionTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error stopping detection task: {ex.Message}", this);
            }

            Debug.Log("Detection task stopped", this);
        }

        /// <summary>
        /// Background worker for face detection
        /// </summary>
        private async Task DetectionWorker()
        {
            CascadeClassifier cascade4Thread = null;
            Mat grayMat4Thread = null;

            try
            {
                // Initialize thread-specific resources
                if (!string.IsNullOrEmpty(_haarCascadeFilepath))
                {
                    cascade4Thread = new CascadeClassifier(_haarCascadeFilepath);
                }
                else
                {
                    Debug.LogError(HAAR_CASCADE_FRONTALFACE_FILEPATH + " is not loaded.", this);
                    return;
                }

                while (!_cts.Token.IsCancellationRequested)
                {
                    if (_shouldDetect && _detectionQueue.TryDequeue(out Mat grayMat))
                    {
                        _isDetectionRunning = true;

                        try
                        {
                            // Perform detection
                            MatOfRect objects = new MatOfRect();
                            if (cascade4Thread != null)
                            {
                                int max = Mathf.Max(grayMat.width(), grayMat.height());
                                int minSize = (int)(max * COEFF_OBJECT_MIN_SIZE_TO_TRACK_BACKGROUND);
                                int maxSize = (int)(max * COEFF_OBJECT_MAX_SIZE_TO_TRACK_BACKGROUND);
                                cascade4Thread.detectMultiScale(grayMat, objects, 1.1, 5, 0 | Xobjdetect.CASCADE_SCALE_IMAGE, (minSize, minSize), (maxSize, maxSize));
                            }

                            // Update detection result thread-safely
                            lock (_detectionResultLock)
                            {
                                _latestDetectionResult?.Dispose();
                                _latestDetectionResult = objects;
                            }

                            _shouldDetect = false;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error in detection worker: {ex.Message}", this);
                        }
                        finally
                        {
                            _isDetectionRunning = false;
                            grayMat?.Dispose();
                        }
                    }

                    // Small delay to prevent busy waiting
                    await Task.Delay(16, _cts.Token); // ~60 FPS
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error in detection worker: {ex.Message}", this);
            }
            finally
            {
                // Cleanup thread-specific resources
                cascade4Thread?.Dispose();
                grayMat4Thread?.Dispose();

                // Clear any remaining items in queue
                while (_detectionQueue.TryDequeue(out Mat mat))
                {
                    mat?.Dispose();
                }

                lock (_detectionResultLock)
                {
                    _latestDetectionResult?.Dispose();
                    _latestDetectionResult = null;
                }
            }
        }

#else

        /// <summary>
        /// Initialize WebGL thread processing
        /// </summary>
        private void InitWebGLThread()
        {
            _grayMat4Thread = new Mat();

            if (string.IsNullOrEmpty(_haarCascadeFilepath))
            {
                Debug.LogError(HAAR_CASCADE_FRONTALFACE_FILEPATH + " is not loaded.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("cascade classifier file is not loaded.\nPlease read console message.", 20000);
                }
            }
            else
            {
                _cascade4Thread = new CascadeClassifier(_haarCascadeFilepath);
            }

            _shouldDetectInMultiThread = false;
            StartCoroutine("WebGLThreadWorker");
        }

        /// <summary>
        /// WebGL thread worker coroutine
        /// </summary>
        private IEnumerator WebGLThreadWorker()
        {
            while (true)
            {
                while (!_shouldDetectInMultiThread)
                {
                    yield return null;
                }

                DetectWebGL();

                _shouldDetectInMultiThread = false;
                _didUpdateTheDetectionResult = true;
            }
        }

        /// <summary>
        /// WebGL detection method
        /// </summary>
        private void DetectWebGL()
        {
            MatOfRect objects = new MatOfRect();
            if (_cascade4Thread != null)
            {
                int max = Mathf.Max(_grayMat4Thread.width(), _grayMat4Thread.height());
                int minSize = (int)(max * COEFF_OBJECT_MIN_SIZE_TO_TRACK_BACKGROUND);
                int maxSize = (int)(max * COEFF_OBJECT_MAX_SIZE_TO_TRACK_BACKGROUND);
                _cascade4Thread.detectMultiScale(_grayMat4Thread, objects, 1.1, 5, 0 | Xobjdetect.CASCADE_SCALE_IMAGE, (minSize, minSize), (maxSize, maxSize));
            }

            _detectionResult = objects;
        }
#endif
    }
}
