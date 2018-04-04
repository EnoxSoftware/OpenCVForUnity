using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;
using Rect = OpenCVForUnity.Rect;
using PositionsVector = System.Collections.Generic.List<OpenCVForUnity.Rect>;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Asynchronous Face Detection WebCamTexture Example
    /// Referring to https://github.com/Itseez/opencv/blob/master/modules/objdetect/src/detection_based_tracker.cpp.
    /// </summary>
    [RequireComponent (typeof(WebCamTextureToMatHelper))]
    public class AsynchronousFaceDetectionWebCamTextureExample : MonoBehaviour
    {
        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMat;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The cascade.
        /// </summary>
        CascadeClassifier cascade;

        /// <summary>
        /// The lbpcascade_frontalface_xml_filepath.
        /// </summary>
        string lbpcascade_frontalface_xml_filepath;
        
        /// <summary>
        /// The haarcascade_frontalface_alt_xml_filepath.
        /// </summary>
        string haarcascade_frontalface_alt_xml_filepath;

        /// <summary>
        /// The rects where regions.
        /// </summary>
        Rect[] rectsWhereRegions;

        /// <summary>
        /// The detected objects in regions.
        /// </summary>
        List<Rect> detectedObjectsInRegions = new List<Rect> ();

        /// <summary>
        /// The result objects.
        /// </summary>
        List<Rect> resultObjects = new List<Rect> ();

        // for Thread
        CascadeClassifier cascade4Thread;
        Mat grayMat4Thread;
        MatOfRect detectionResult;
        System.Object sync = new System.Object ();

        bool _isThreadRunning = false;

        bool isThreadRunning {
            get {
                lock (sync)
                    return _isThreadRunning;
            }
            set {
                lock (sync)
                    _isThreadRunning = value;
            }
        }

        bool _shouldStopThread = false;

        bool shouldStopThread {
            get {
                lock (sync)
                    return _shouldStopThread;
            }
            set {
                lock (sync)
                    _shouldStopThread = value;
            }
        }

        bool _shouldDetectInMultiThread = false;

        bool shouldDetectInMultiThread {
            get {
                lock (sync)
                    return _shouldDetectInMultiThread;
            }
            set {
                lock (sync)
                    _shouldDetectInMultiThread = value;
            }
        }

        bool _didUpdateTheDetectionResult = false;

        bool didUpdateTheDetectionResult {
            get {
                lock (sync)
                    return _didUpdateTheDetectionResult;
            }
            set {
                lock (sync)
                    _didUpdateTheDetectionResult = value;
            }
        }

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        #if UNITY_ANDROID && !UNITY_EDITOR
        float rearCameraRequestedFPS;
        #endif

        // for tracker
        List<TrackedObject> trackedObjects = new List<TrackedObject> ();
        List<float> weightsPositionsSmoothing = new List<float> ();
        List<float> weightsSizesSmoothing = new List<float> ();
        Parameters parameters;
        InnerParameters innerParameters;

        #if UNITY_WEBGL && !UNITY_EDITOR
        Stack<IEnumerator> coroutines = new Stack<IEnumerator> ();
        #endif

        // Use this for initialization
        void Start ()
        {   
            fpsMonitor = GetComponent<FpsMonitor> ();

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            var getFilePath_Coroutine = GetFilePath ();
            coroutines.Push (getFilePath_Coroutine);
            StartCoroutine (getFilePath_Coroutine);
            #else
            lbpcascade_frontalface_xml_filepath = Utils.getFilePath ("lbpcascade_frontalface.xml");
            haarcascade_frontalface_alt_xml_filepath = Utils.getFilePath ("haarcascade_frontalface_alt.xml");
            Run ();
            #endif
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator GetFilePath ()
        {
            var getFilePathAsync_lbpcascade_frontalface_xml_filepath_Coroutine = Utils.getFilePathAsync ("lbpcascade_frontalface.xml", (result) => {
                lbpcascade_frontalface_xml_filepath = result;
            });
            coroutines.Push (getFilePathAsync_lbpcascade_frontalface_xml_filepath_Coroutine);
            yield return StartCoroutine (getFilePathAsync_lbpcascade_frontalface_xml_filepath_Coroutine);
            
            var getFilePathAsync_haarcascade_frontalface_alt_xml_filepath_Coroutine = Utils.getFilePathAsync ("haarcascade_frontalface_alt.xml", (result) => {
                haarcascade_frontalface_alt_xml_filepath = result;
            });
            coroutines.Push (getFilePathAsync_haarcascade_frontalface_alt_xml_filepath_Coroutine);
            yield return StartCoroutine (getFilePathAsync_haarcascade_frontalface_alt_xml_filepath_Coroutine);
            
            coroutines.Clear ();
            
            Run ();
        }
        #endif

        private void Run ()
        {
            weightsPositionsSmoothing.Add (1);
            weightsSizesSmoothing.Add (0.5f);
            weightsSizesSmoothing.Add (0.3f);
            weightsSizesSmoothing.Add (0.2f);
            
            //parameters.minObjectSize = 96;
            //parameters.maxObjectSize = int.MaxValue;
            //parameters.scaleFactor = 1.1f;
            //parameters.minNeighbors = 2;
            parameters.maxTrackLifetime = 5;
            
            innerParameters.numLastPositionsToTrack = 4;
            innerParameters.numStepsToWaitBeforeFirstShow = 6;
            innerParameters.numStepsToTrackWithoutDetectingIfObjectHasNotBeenShown = 3;
            innerParameters.numStepsToShowWithoutDetecting = 3;
            innerParameters.coeffTrackingWindowSize = 2.0f;
            innerParameters.coeffObjectSizeToTrack = 0.85f;
            innerParameters.coeffObjectSpeedUsingInPrediction = 0.8f;

            #if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices. (Pixel, pixel 2)
            // https://forum.unity.com/threads/android-webcamtexture-in-low-light-only-some-models.520656/
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            rearCameraRequestedFPS = webCamTextureToMatHelper.requestedFPS;
            if (webCamTextureToMatHelper.requestedIsFrontFacing) {                
                webCamTextureToMatHelper.requestedFPS = 15;
                webCamTextureToMatHelper.Initialize ();
            } else {
                webCamTextureToMatHelper.Initialize ();
            }
            #else
            webCamTextureToMatHelper.Initialize ();
            #endif
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInitialized");
            
            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();

            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
            
            gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
            
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null){
                fpsMonitor.Add ("width", webCamTextureMat.width ().ToString());
                fpsMonitor.Add ("height", webCamTextureMat.height ().ToString());
                fpsMonitor.Add ("orientation", Screen.orientation.ToString());
            }

            
            float width = webCamTextureMat.width ();
            float height = webCamTextureMat.height ();
            
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }
            

            grayMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC1);
            cascade = new CascadeClassifier ();
            cascade.load (lbpcascade_frontalface_xml_filepath);
            #if !UNITY_WSA_10_0
            if (cascade.empty ()) {
                Debug.LogError ("cascade file is not loaded.Please copy from “OpenCVForUnity/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }
            #endif
            InitThread ();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

            #if !UNITY_WEBGL
            StopThread ();
            #else
            StopCoroutine ("ThreadWorker");
            #endif

            if (grayMat4Thread != null)
                grayMat4Thread.Dispose ();

            if (cascade4Thread != null)
                cascade4Thread.Dispose ();

            if (grayMat != null)
                grayMat.Dispose ();

            if (texture != null) {
                Texture2D.Destroy(texture);
                texture = null;
            }

            if (cascade != null)
                cascade.Dispose ();
                
            trackedObjects.Clear ();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred (WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log ("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update ()
        {
            if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {
                
                Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

                Imgproc.cvtColor (rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.equalizeHist (grayMat, grayMat);

                if (!shouldDetectInMultiThread) {
                    grayMat.copyTo (grayMat4Thread);

                    shouldDetectInMultiThread = true;
                }

                OpenCVForUnity.Rect[] rects;
                
                if (didUpdateTheDetectionResult) {
                    didUpdateTheDetectionResult = false;

                    //Debug.Log("DetectionBasedTracker::process: get _rectsWhereRegions were got from resultDetect");
                    rectsWhereRegions = detectionResult.toArray ();
                
                    rects = rectsWhereRegions;
                    for (int i = 0; i < rects.Length; i++) {
                        Imgproc.rectangle (rgbaMat, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (0, 0, 255, 255), 2);
                    }

                } else {
                    //Debug.Log("DetectionBasedTracker::process: get _rectsWhereRegions from previous positions");
                    rectsWhereRegions = new Rect[trackedObjects.Count];
                
                    for (int i = 0; i < trackedObjects.Count; i++) {
                        int n = trackedObjects [i].lastPositions.Count;
                        //if (n > 0) UnityEngine.Debug.LogError("n > 0 is false");
                
                        Rect r = trackedObjects [i].lastPositions [n - 1].clone ();
                        if (r.area () == 0) {
                            Debug.Log ("DetectionBasedTracker::process: ERROR: ATTENTION: strange algorithm's behavior: trackedObjects[i].rect() is empty");
                            continue;
                        }
                
                        //correction by speed of rectangle
                        if (n > 1) {
                            Point center = CenterRect (r);
                            Point center_prev = CenterRect (trackedObjects [i].lastPositions [n - 2]);
                            Point shift = new Point ((center.x - center_prev.x) * innerParameters.coeffObjectSpeedUsingInPrediction,
                                              (center.y - center_prev.y) * innerParameters.coeffObjectSpeedUsingInPrediction);
                
                            r.x += (int)Math.Round (shift.x);
                            r.y += (int)Math.Round (shift.y);
                        }
                        rectsWhereRegions [i] = r;
                    }

                    rects = rectsWhereRegions;
                    for (int i = 0; i < rects.Length; i++) {
                        Imgproc.rectangle (rgbaMat, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (0, 255, 0, 255), 2);
                    }
                }

                detectedObjectsInRegions.Clear ();
                if (rectsWhereRegions.Length > 0) {
                
                    int len = rectsWhereRegions.Length;
                    for (int i = 0; i < len; i++) {
                        DetectInRegion (grayMat, rectsWhereRegions [i], detectedObjectsInRegions);
                    }
                }

                UpdateTrackedObjects (detectedObjectsInRegions);
                GetObjects (resultObjects);

                rects = resultObjects.ToArray ();
                for (int i = 0; i < rects.Length; i++) {
                    //Debug.Log ("detect faces " + rects [i]);
                    Imgproc.rectangle (rgbaMat, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (255, 0, 0, 255), 2);
                }

                #if UNITY_WEBGL
                Imgproc.putText (rgbaMat, "WebGL platform does not support multi-threading.", new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
                #endif

                Utils.matToTexture2D (rgbaMat, texture, webCamTextureToMatHelper.GetBufferColors ());
            }
        }

        private void DetectInRegion (Mat img, Rect r, List<Rect> detectedObjectsInRegions)
        {
            Rect r0 = new Rect (new Point (), img.size ());
            Rect r1 = new Rect (r.x, r.y, r.width, r.height);
            Rect.inflate (r1, (int)((r1.width * innerParameters.coeffTrackingWindowSize) - r1.width) / 2,
                (int)((r1.height * innerParameters.coeffTrackingWindowSize) - r1.height) / 2);
            r1 = Rect.intersect (r0, r1);
            
            if (r1 != null && (r1.width <= 0) || (r1.height <= 0)) {
                Debug.Log ("DetectionBasedTracker::detectInRegion: Empty intersection");
                return;
            }
            
            
            int d = Math.Min (r.width, r.height);
            d = (int)Math.Round (d * innerParameters.coeffObjectSizeToTrack);
            
            
            MatOfRect tmpobjects = new MatOfRect ();
            
            Mat img1 = new Mat (img, r1);//subimage for rectangle -- without data copying
            
            cascade.detectMultiScale (img1, tmpobjects, 1.1, 2, 0 | Objdetect.CASCADE_DO_CANNY_PRUNING | Objdetect.CASCADE_SCALE_IMAGE | Objdetect.CASCADE_FIND_BIGGEST_OBJECT, new Size (d, d), new Size ());
            
            
            Rect[] tmpobjectsArray = tmpobjects.toArray ();
            int len = tmpobjectsArray.Length;
            for (int i = 0; i < len; i++) {
                Rect tmp = tmpobjectsArray [i];
                Rect curres = new Rect (new Point (tmp.x + r1.x, tmp.y + r1.y), tmp.size ());
                detectedObjectsInRegions.Add (curres);
            }
        }

        public Point CenterRect (Rect r)
        {
            return new Point (r.x + (r.width / 2), r.y + (r.height / 2));
        }

        private void InitThread ()
        {
            StopThread ();
            
            grayMat4Thread = new Mat ();
            
            cascade4Thread = new CascadeClassifier ();
            cascade4Thread.load (haarcascade_frontalface_alt_xml_filepath);
            #if !UNITY_WSA_10_0
            if (cascade4Thread.empty ()) {
                Debug.LogError ("cascade4Thread file is not loaded.Please copy from “OpenCVForUnity/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }
            #endif
            
            shouldDetectInMultiThread = false;

            #if !UNITY_WEBGL
            StartThread (ThreadWorker);
            #else
            StartCoroutine ("ThreadWorker");
            #endif
        }

        private void StartThread (Action action)
        {
            shouldStopThread = false;

            #if UNITY_METRO && NETFX_CORE
            System.Threading.Tasks.Task.Run(() => action());
            #elif UNITY_METRO
            action.BeginInvoke(ar => action.EndInvoke(ar), null);
            #else
            ThreadPool.QueueUserWorkItem (_ => action ());
            #endif

            Debug.Log ("Thread Start");
        }

        private void StopThread ()
        {
            if (!isThreadRunning)
                return;

            shouldStopThread = true;

            while (isThreadRunning) {
                //Wait threading stop
            } 
            Debug.Log ("Thread Stop");
        }

        #if !UNITY_WEBGL
        private void ThreadWorker ()
        {
            isThreadRunning = true;

            while (!shouldStopThread) {
                if (!shouldDetectInMultiThread)
                    continue;

                Detect ();

                shouldDetectInMultiThread = false;
                didUpdateTheDetectionResult = true;
            }

            isThreadRunning = false;
        }
        #else
        private IEnumerator ThreadWorker ()
        {
            while (true) {
                while (!shouldDetectInMultiThread) {
                    yield return null;
                }

                Detect ();

                shouldDetectInMultiThread = false;
                didUpdateTheDetectionResult = true;
            }
        }
        #endif


        private void Detect ()
        {
            MatOfRect objects = new MatOfRect ();
            if (cascade4Thread != null)
                cascade4Thread.detectMultiScale (grayMat4Thread, objects, 1.1, 2, Objdetect.CASCADE_SCALE_IMAGE, // TODO: objdetect.CV_HAAR_SCALE_IMAGE
                    new Size (grayMat4Thread.height () * 0.2, grayMat4Thread.height () * 0.2), new Size ());

            //Thread.Sleep(200);

            detectionResult = objects;
        }


        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            webCamTextureToMatHelper.Dispose ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            foreach (var coroutine in coroutines) {
                StopCoroutine (coroutine);
                ((IDisposable)coroutine).Dispose ();
            }
            #endif
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("OpenCVForUnityExample");
            #else
            Application.LoadLevel ("OpenCVForUnityExample");
            #endif
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick ()
        {
            webCamTextureToMatHelper.Play ();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick ()
        {
            webCamTextureToMatHelper.Pause ();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick ()
        {
            webCamTextureToMatHelper.Stop ();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick ()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (!webCamTextureToMatHelper.IsFrontFacing ()) {
                rearCameraRequestedFPS = webCamTextureToMatHelper.requestedFPS;
                webCamTextureToMatHelper.Initialize (!webCamTextureToMatHelper.IsFrontFacing (), 15, webCamTextureToMatHelper.rotate90Degree);
            } else {                
                webCamTextureToMatHelper.Initialize (!webCamTextureToMatHelper.IsFrontFacing (), rearCameraRequestedFPS, webCamTextureToMatHelper.rotate90Degree);
            }
            #else
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing ();
            #endif
        }

        //
        // tracker
        //
        private void GetObjects (List<Rect> result)
        {
            result.Clear ();
                
            for (int i = 0; i < trackedObjects.Count; i++) {
                Rect r = CalcTrackedObjectPositionToShow (i);
                if (r.area () == 0) {
                    continue;
                }
                result.Add (r);
                //LOGD("DetectionBasedTracker::process: found a object with SIZE %d x %d, rect={%d, %d, %d x %d}", r.width, r.height, r.x, r.y, r.width, r.height);
            }
        }

        private enum TrackedState : int
        {
            NEW_RECTANGLE = -1,
            INTERSECTED_RECTANGLE = -2
        }

        private void UpdateTrackedObjects (List<Rect> detectedObjects)
        {
            int N1 = (int)trackedObjects.Count;
            int N2 = (int)detectedObjects.Count;
                
            for (int i = 0; i < N1; i++) {
                trackedObjects [i].numDetectedFrames++;
            }
                
            int[] correspondence = new int[N2];
            for (int i = 0; i < N2; i++) {
                correspondence [i] = (int)TrackedState.NEW_RECTANGLE;
            }
                
                
            for (int i = 0; i < N1; i++) {
                TrackedObject curObject = trackedObjects [i];
                    
                int bestIndex = -1;
                int bestArea = -1;
                    
                int numpositions = (int)curObject.lastPositions.Count;
                    
                //if (numpositions > 0) UnityEngine.Debug.LogError("numpositions > 0 is false");
                    
                Rect prevRect = curObject.lastPositions [numpositions - 1];
                    
                for (int j = 0; j < N2; j++) {
                    if (correspondence [j] >= 0) {
                        //Debug.Log("DetectionBasedTracker::updateTrackedObjects: j=" + i + " is rejected, because it has correspondence=" + correspondence[j]);
                        continue;
                    }
                    if (correspondence [j] != (int)TrackedState.NEW_RECTANGLE) {
                        //Debug.Log("DetectionBasedTracker::updateTrackedObjects: j=" + j + " is rejected, because it is intersected with another rectangle");
                        continue;
                    }
                        
                    Rect r = Rect.intersect (prevRect, detectedObjects [j]);
                    if (r != null && (r.width > 0) && (r.height > 0)) {
                        //LOGD("DetectionBasedTracker::updateTrackedObjects: There is intersection between prevRect and detectedRect, r={%d, %d, %d x %d}",
                        //        r.x, r.y, r.width, r.height);
                        correspondence [j] = (int)TrackedState.INTERSECTED_RECTANGLE;
                            
                        if (r.area () > bestArea) {
                            //LOGD("DetectionBasedTracker::updateTrackedObjects: The area of intersection is %d, it is better than bestArea=%d", r.area(), bestArea);
                            bestIndex = j;
                            bestArea = (int)r.area ();
                        }
                    }
                }
                    
                if (bestIndex >= 0) {
                    //LOGD("DetectionBasedTracker::updateTrackedObjects: The best correspondence for i=%d is j=%d", i, bestIndex);
                    correspondence [bestIndex] = i;
                        
                    for (int j = 0; j < N2; j++) {
                        if (correspondence [j] >= 0)
                            continue;
                            
                        Rect r = Rect.intersect (detectedObjects [j], detectedObjects [bestIndex]);
                        if (r != null && (r.width > 0) && (r.height > 0)) {
                            //LOGD("DetectionBasedTracker::updateTrackedObjects: Found intersection between "
                            //    "rectangles j=%d and bestIndex=%d, rectangle j=%d is marked as intersected", j, bestIndex, j);
                            correspondence [j] = (int)TrackedState.INTERSECTED_RECTANGLE;
                        }
                    }
                } else {
                    //LOGD("DetectionBasedTracker::updateTrackedObjects: There is no correspondence for i=%d ", i);
                    curObject.numFramesNotDetected++;
                }
            }
                
            //LOGD("DetectionBasedTracker::updateTrackedObjects: start second cycle");
            for (int j = 0; j < N2; j++) {
                int i = correspondence [j];
                if (i >= 0) {//add position
                    //Debug.Log("DetectionBasedTracker::updateTrackedObjects: add position");
                    trackedObjects [i].lastPositions.Add (detectedObjects [j]);
                    while ((int)trackedObjects [i].lastPositions.Count > (int)innerParameters.numLastPositionsToTrack) {
                        trackedObjects [i].lastPositions.Remove (trackedObjects [i].lastPositions [0]);
                    }
                    trackedObjects [i].numFramesNotDetected = 0;
                } else if (i == (int)TrackedState.NEW_RECTANGLE) { //new object
                    //Debug.Log("DetectionBasedTracker::updateTrackedObjects: new object");
                    trackedObjects.Add (new TrackedObject (detectedObjects [j]));
                } else {
                    //Debug.Log ("DetectionBasedTracker::updateTrackedObjects: was auxiliary intersection");
                }
            }
                
            int t = 0;
            TrackedObject it;
            while (t < trackedObjects.Count) {
                it = trackedObjects [t];
                    
                if ((it.numFramesNotDetected > parameters.maxTrackLifetime)
                    ||
                    ((it.numDetectedFrames <= innerParameters.numStepsToWaitBeforeFirstShow)
                    &&
                    (it.numFramesNotDetected > innerParameters.numStepsToTrackWithoutDetectingIfObjectHasNotBeenShown))) {
                    //int numpos = (int)it.lastPositions.Count;
                    //if (numpos > 0) UnityEngine.Debug.LogError("numpos > 0 is false");
                    //Rect r = it.lastPositions [numpos - 1];
                    //Debug.Log("DetectionBasedTracker::updateTrackedObjects: deleted object " + r.x + " " + r.y + " " + r.width + " " + r.height);
                        
                    trackedObjects.Remove (it);
                        
                } else {
                    t++;
                }
            }
        }

        private Rect CalcTrackedObjectPositionToShow (int i)
        {
            if ((i < 0) || (i >= trackedObjects.Count)) {
                Debug.Log ("DetectionBasedTracker::calcTrackedObjectPositionToShow: ERROR: wrong i=" + i);
                return new Rect ();
            }
            if (trackedObjects [i].numDetectedFrames <= innerParameters.numStepsToWaitBeforeFirstShow) {
                //Debug.Log("DetectionBasedTracker::calcTrackedObjectPositionToShow: " + "trackedObjects[" + i + "].numDetectedFrames=" + trackedObjects[i].numDetectedFrames + " <= numStepsToWaitBeforeFirstShow=" + innerParameters.numStepsToWaitBeforeFirstShow + " --- return empty Rect()");
                return new Rect ();
            }
            if (trackedObjects [i].numFramesNotDetected > innerParameters.numStepsToShowWithoutDetecting) {
                return new Rect ();
            }
                
            List<Rect> lastPositions = trackedObjects [i].lastPositions;
                
            int N = lastPositions.Count;
            if (N <= 0) {
                Debug.Log ("DetectionBasedTracker::calcTrackedObjectPositionToShow: ERROR: no positions for i=" + i);
                return new Rect ();
            }
                
            int Nsize = Math.Min (N, (int)weightsSizesSmoothing.Count);
            int Ncenter = Math.Min (N, (int)weightsPositionsSmoothing.Count);
                
            Point center = new Point ();
            double w = 0, h = 0;
            if (Nsize > 0) {
                double sum = 0;
                for (int j = 0; j < Nsize; j++) {
                    int k = N - j - 1;
                    w += lastPositions [k].width * weightsSizesSmoothing [j];
                    h += lastPositions [k].height * weightsSizesSmoothing [j];
                    sum += weightsSizesSmoothing [j];
                }
                w /= sum;
                h /= sum;
            } else {
                w = lastPositions [N - 1].width;
                h = lastPositions [N - 1].height;
            }
                
            if (Ncenter > 0) {
                double sum = 0;
                for (int j = 0; j < Ncenter; j++) {
                    int k = N - j - 1;
                    Point tl = lastPositions [k].tl ();
                    Point br = lastPositions [k].br ();
                    Point c1;
                    //c1=tl;
                    //c1=c1* 0.5f;//
                    c1 = new Point (tl.x * 0.5f, tl.y * 0.5f);
                    Point c2;
                    //c2=br;
                    //c2=c2*0.5f;
                    c2 = new Point (br.x * 0.5f, br.y * 0.5f);
                    //c1=c1+c2;
                    c1 = new Point (c1.x + c2.x, c1.y + c2.y);
                        
                    //center=center+  (c1  * weightsPositionsSmoothing[j]);
                    center = new Point (center.x + (c1.x * weightsPositionsSmoothing [j]), center.y + (c1.y * weightsPositionsSmoothing [j]));
                    sum += weightsPositionsSmoothing [j];
                }
                //center *= (float)(1 / sum);
                center = new Point (center.x * (1 / sum), center.y * (1 / sum));
            } else {
                int k = N - 1;
                Point tl = lastPositions [k].tl ();
                Point br = lastPositions [k].br ();
                Point c1;
                //c1=tl;
                //c1=c1* 0.5f;
                c1 = new Point (tl.x * 0.5f, tl.y * 0.5f);
                Point c2;
                //c2=br;
                //c2=c2*0.5f;
                c2 = new Point (br.x * 0.5f, br.y * 0.5f);
                    
                //center=c1+c2;
                center = new Point (c1.x + c2.x, c1.y + c2.y);
            }
            //Point2f tl=center-(Point2f(w,h)*0.5);
            Point tl2 = new Point (center.x - (w * 0.5f), center.y - (h * 0.5f));
            //Rect res(cvRound(tl.x), cvRound(tl.y), cvRound(w), cvRound(h));
            Rect res = new Rect ((int)Math.Round (tl2.x), (int)Math.Round (tl2.y), (int)Math.Round (w), (int)Math.Round (h));
            //LOGD("DetectionBasedTracker::calcTrackedObjectPositionToShow: Result for i=%d: {%d, %d, %d x %d}", i, res.x, res.y, res.width, res.height);
                
            return res;
        }

        private struct Parameters
        {
            //public int minObjectSize;
            //public int maxObjectSize;
            //public float scaleFactor;
            //public int minNeighbors;

            public int maxTrackLifetime;
            //public int minDetectionPeriod; //the minimal time between run of the big object detector (on the whole frame) in ms (1000 mean 1 sec), default=0
        };

        private struct InnerParameters
        {
            public int numLastPositionsToTrack;
            public int numStepsToWaitBeforeFirstShow;
            public int numStepsToTrackWithoutDetectingIfObjectHasNotBeenShown;
            public int numStepsToShowWithoutDetecting;
            public float coeffTrackingWindowSize;
            public float coeffObjectSizeToTrack;
            public float coeffObjectSpeedUsingInPrediction;
        };

        private class TrackedObject
        {
            public PositionsVector lastPositions;
            public int numDetectedFrames;
            public int numFramesNotDetected;
            public int id;
            static private int _id = 0;

            public TrackedObject (OpenCVForUnity.Rect rect)
            {
                lastPositions = new PositionsVector ();

                numDetectedFrames = 1;
                numFramesNotDetected = 0;

                lastPositions.Add (rect.clone ());

                _id = GetNextId ();
                id = _id;
            }

            static int GetNextId ()
            {
                _id++;
                return _id;
            }
        }
    }
}