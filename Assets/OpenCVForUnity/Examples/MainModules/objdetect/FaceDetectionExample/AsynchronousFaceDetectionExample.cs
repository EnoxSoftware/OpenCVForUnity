using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.UnityIntegration.MOT;
using OpenCVForUnity.UnityIntegration.MOT.ByteTrack;
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
    ///
    /// This example demonstrates asynchronous face detection using OpenCV's Cascade Classifier
    /// combined with ByteTrack for multi-object tracking. The implementation features:
    ///
    /// - Multi-threaded face detection: Heavy detection processing runs on a separate thread
    ///   to avoid blocking the main thread and maintain smooth UI responsiveness
    /// - Local region detection: Lightweight detection is performed in tracked regions
    ///   on the main thread for real-time performance
    /// - ByteTrack integration: Advanced multi-object tracking algorithm for stable
    ///   face tracking across frames with ID persistence
    /// - Real-time visualization: Detected faces are displayed with tracking information
    ///   including track IDs and bounding boxes
    ///
    /// The system uses two cascade classifiers:
    /// - LBP Cascade: For local region detection (main thread)
    /// - Haar Cascade: For full-frame detection (background thread)
    ///
    /// Key features:
    /// - Asynchronous processing maintains UI responsiveness
    /// - Robust tracking handles face occlusion and re-entry
    /// - Configurable detection parameters for different scenarios
    /// - Cross-platform support (with WebGL limitations for threading)
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class AsynchronousFaceDetectionExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// LBP_CASCADE_FRONTALFACE_FILENAME
        /// </summary>
        protected static readonly string LBP_CASCADE_FRONTALFACE_FILENAME = "OpenCVForUnityExamples/objdetect/lbpcascade_frontalface.xml";
        //protected static readonly string LBP_CASCADE_FRONTALFACE_FILENAME = "OpenCVForUnityExamples/objdetect/haarcascade_frontalface_alt.xml";

        /// <summary>
        /// HAAR_CASCADE_FRONTALFACE_FILENAME
        /// </summary>
        protected static readonly string HAAR_CASCADE_FRONTALFACE_FILENAME = "OpenCVForUnityExamples/objdetect/haarcascade_frontalface_alt.xml";

        private static readonly (int, int, int, int) YELLOW_COLOR_TUPLE = (255, 255, 0, 255);
        private static readonly (int, int, int, int) GREEN_COLOR_TUPLE = (0, 255, 0, 255);
        private static readonly (int, int, int, int) WHITE_COLOR_TUPLE = (255, 255, 255, 255);

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
        private MultiSource2MatHelper _multiSource2MatHelper;
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

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _lbpCascadeFilepath = await OpenCVEnv.GetFilePathTaskAsync(LBP_CASCADE_FRONTALFACE_FILENAME, cancellationToken: _cts.Token);
            _haarCascadeFilepath = await OpenCVEnv.GetFilePathTaskAsync(HAAR_CASCADE_FRONTALFACE_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            _multiSource2MatHelper.Initialize();
        }

        private void Update()
        {
#if !UNITY_WEBGL
            if (_detectionTask == null || _detectionTask.IsCompleted)
            {
                return;
            }
#endif

            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_cascade == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.",
                                     (5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, WHITE_COLOR_TUPLE, 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.",
                                     (5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, WHITE_COLOR_TUPLE, 2, Imgproc.LINE_AA, false);

                    OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
                    return;
                }

                Imgproc.cvtColor(rgbaMat, _grayMat, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.equalizeHist(_grayMat, _grayMat);

#if UNITY_WEBGL
                // WebGL: Synchronous processing
                if (!_shouldDetectInMultiThread)
                {
                    _grayMat.copyTo(_grayMat4Thread);
                    _shouldDetectInMultiThread = true;
                }
#else
                // Queue detection for background processing
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
                    for (int i = 0; i < rects.Length; i++)
                    {
                        Imgproc.rectangle(rgbaMat, (rects[i].x, rects[i].y),
                                             (rects[i].x + rects[i].width, rects[i].y + rects[i].height), YELLOW_COLOR_TUPLE, 1);
                    }
                }
                else
                {
                    //Debug.Log("get _rectsWhereRegions from previous positions");

                    // get current track info from BYTETracker
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

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
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

            _multiSource2MatHelper?.Dispose();
            _byteTracker?.Dispose();
            _byteTrackInfoVisualizer?.Dispose();
            _cts?.Dispose();
        }

        // Public Methods
#if UNITY_WEBGL
        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
#else
        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public async void OnSourceToMatHelperInitialized()
        {
            // Wait for any existing detection task to complete before initializing
            await WaitForDetectionTaskCompletion();
            
            // Add a delay to ensure the order is maintained so that StopDetectionAsync is after
            await Task.Delay(50);
#endif

            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = _multiSource2MatHelper.GetMat();

            _texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("width", rgbaMat.width().ToString());
                _fpsMonitor.Add("height", rgbaMat.height().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            _grayMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);

            if (string.IsNullOrEmpty(_lbpCascadeFilepath))
            {
                Debug.LogError(LBP_CASCADE_FRONTALFACE_FILENAME + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
            }
            else
            {
                _cascade = new CascadeClassifier(_lbpCascadeFilepath);
            }

            // BYTETracker初期化
            int fps = 30;
            if (_multiSource2MatHelper.Source2MatHelper is ICameraSource2MatHelper cameraHelper)
            {
                fps = (int)cameraHelper.GetFPS();
            }
            else if (_multiSource2MatHelper.Source2MatHelper is IVideoSource2MatHelper videoHelper)
            {
                fps = (int)videoHelper.GetFPS();
            }

            _byteTracker = new BYTETracker(fps, 30, mot20: false);
            _byteTrackInfoVisualizer = new BYTETrackInfoVisualizer();

#if UNITY_WEBGL
            // WebGL: Initialize synchronous processing
            InitWebGLThread();
#else
            // Start background detection task
            StartDetectionTask();
#endif
        }


#if UNITY_WEBGL
        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            // WebGL: Stop coroutine
            StopCoroutine("WebGLThreadWorker");
            _grayMat4Thread?.Dispose(); _grayMat4Thread = null;
            _cascade4Thread?.Dispose(); _cascade4Thread = null;
            
            _grayMat?.Dispose(); _grayMat = null;
            _cascade?.Dispose(); _cascade = null;

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;

            _byteTracker?.Dispose(); _byteTracker = null;
            _byteTrackInfoVisualizer?.Dispose(); _byteTrackInfoVisualizer = null;
        }
#else
        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public async void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _grayMat?.Dispose(); _grayMat = null;
            _cascade?.Dispose(); _cascade = null;

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;

            _byteTracker?.Dispose(); _byteTracker = null;
            _byteTrackInfoVisualizer?.Dispose(); _byteTrackInfoVisualizer = null;

            await StopDetectionAsync();
        }
#endif


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
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            _multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            _multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            _multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            _multiSource2MatHelper.RequestedIsFrontFacing = !_multiSource2MatHelper.RequestedIsFrontFacing;
        }

        // Private Methods
        private void DetectInRegion(Mat img, Rect r, List<Rect> detectedObjectsInRegions)
        {
            Rect r0 = new Rect(0, 0, img.width(), img.height());
            Rect r1 = new Rect(r.x, r.y, r.width, r.height);
            Rect.inflate(r1, (int)((r1.width * COEFF_TRACKING_WINDOW_SIZE) - r1.width) / 2,
                (int)((r1.height * COEFF_TRACKING_WINDOW_SIZE) - r1.height) / 2);
            r1 = Rect.intersect(r0, r1);

            if (r1 != null && (r1.width <= 0) || (r1.height <= 0))
            {
                Debug.Log("Empty intersection");
                return;
            }

            using (MatOfRect tmpobjects = new MatOfRect())
            using (Mat img1 = new Mat(img, r1)) // submat for rectangle -- without data copying)
            {
                int minSize = (int)(Mathf.Min(r.width, r.height) * COEFF_OBJECT_MIN_SIZE_TO_TRACK);
                _cascade.detectMultiScale(img1, tmpobjects, 1.05, 2,
                    0 | Objdetect.CASCADE_DO_CANNY_PRUNING | Objdetect.CASCADE_SCALE_IMAGE | Objdetect.CASCADE_FIND_BIGGEST_OBJECT, (minSize, minSize));

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
            Debug.Log("Detection task started");
        }

        /// <summary>
        /// Waits for the detection task to complete if it's running
        /// </summary>
        private async Task WaitForDetectionTaskCompletion()
        {
            if (_detectionTask != null && !_detectionTask.IsCompleted)
            {
                Debug.Log("Waiting for existing detection task to complete...");
                
                // If the task is already cancelled, wait for a short time and then check if it is completed
                if (_cts.Token.IsCancellationRequested)
                {
                    Debug.Log("Task is already cancelled, waiting for completion...");
                    while (!_detectionTask.IsCompleted)
                    {
                        await Task.Delay(10);
                    }
                }
                else
                {
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
                        Debug.LogWarning($"Existing detection task completed with error: {ex.Message}");
                    }
                }
            }
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
                Debug.LogError($"Error stopping detection task: {ex.Message}");
            }

            Debug.Log("Detection task stopped");
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
                    Debug.LogError(HAAR_CASCADE_FRONTALFACE_FILENAME + " is not loaded.");
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
                                cascade4Thread.detectMultiScale(grayMat, objects, 1.1, 5, 0 | Objdetect.CASCADE_SCALE_IMAGE, (minSize, minSize), (maxSize, maxSize));
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
                            Debug.LogError($"Error in detection worker: {ex.Message}");
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
                Debug.LogError($"Unexpected error in detection worker: {ex.Message}");
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
                Debug.LogError(HAAR_CASCADE_FRONTALFACE_FILENAME + " is not loaded.");
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
                _cascade4Thread.detectMultiScale(_grayMat4Thread, objects, 1.1, 5, 0 | Objdetect.CASCADE_SCALE_IMAGE, (minSize, minSize), (maxSize, maxSize));
            }

            _detectionResult = objects;
        }
#endif
    }
}
