using System;
using System.Collections;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.TrackingModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.VideoioModule;
using OpenCVForUnity.XobjdetectModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenCVDebug = OpenCVForUnity.Extensions.OpenCVDebug;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// DebugMat Example
    /// An interactive playground for inspecting OpenCV <see cref="Mat"/> data while learning common image-processing workflows.
    /// Each UI button runs a small demo and shows the corresponding sample code in <see cref="ExampleCodeText"/>.
    ///
    /// Demonstrations included in this scene:
    /// - Face detection on a still image (Haar cascade)
    /// - Video file playback frame by frame
    /// - Object tracking on video frames (CSRT tracker)
    /// - Numeric dump of Mat / Texture2D pixels (debug helper)
    /// - OpenCV exception handling when Mat element types do not match
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="MatOfRect"/>, <see cref="Scalar"/>, <see cref="Core"/>: image buffers, results, and element-wise operations
    /// - <see cref="ImgprocModule.Imgproc"/>: color conversion, histogram equalization, drawing rectangles
    /// - <see cref="ObjdetectModule.CascadeClassifier"/>: Haar-cascade face detection
    /// - <see cref="VideoioModule.VideoCapture"/>, <see cref="VideoioModule.Videoio"/>: read frames from a video file
    /// - <see cref="TrackingModule.TrackerCSRT"/>: track a rectangle region across frames
    /// - <see cref="OpenCVDebug"/>, <see cref="DebugMat"/>: visualize Mat contents and OpenCV error messages in the Editor
    ///
    /// Unity integration:
    /// - <see cref="OpenCVMatUnityUtils.Texture2DToMat"/>: copy Unity <see cref="Texture2D"/> pixels into a Mat
    /// - <see cref="OpenCVForUnityEnv.GetFilePath"/>: resolve StreamingAssets paths at runtime
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://docs.opencv.org/4.x/db/d28/tutorial_cascade_classifier.html
    /// https://github.com/opencv/opencv/tree/4.x/data/haarcascades
    /// http://docs.opencv.org/3.2.0/dd/d43/tutorial_py_video_display.html
    /// https://docs.opencv.org/4.x/d5/d07/tutorial_multitracker.html
    /// </para>
    /// </remarks>
    public class DebugMatExample : MonoBehaviour
    {
        // Public Fields
        public ScrollRect ExampleCodeScrollRect;
        public UnityEngine.UI.Text ExampleCodeText;

        // Private Fields
        private IEnumerator _enumerator;

        // Unity Lifecycle Methods
        private IEnumerator Start()
        {
            // fix the screen orientation.
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            // wait for the screen orientation to change.
            yield return null;
        }

        private void Update()
        {

        }

        private void OnDestroy()
        {
            Screen.orientation = ScreenOrientation.AutoRotation;

            DisposeEnumerator();
        }

        // Private Methods
        private void UpdateScrollRect()
        {
            ExampleCodeScrollRect.verticalNormalizedPosition = 1f;
        }

        private void DisposeEnumerator()
        {
            // Stop the currently running demo coroutine before starting another button demo.
            if (_enumerator != null)
            {
                (_enumerator as IDisposable)?.Dispose();
                StopCoroutine(_enumerator);
                _enumerator = null;
            }
        }

        private void StartEnumerator(IEnumerator enumerator)
        {
            _enumerator = enumerator;
            StartCoroutine(_enumerator);
        }

        // Public Methods
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        public void OnLayoutTypeDropdownValueChanged(int result)
        {
            //Debug.Log("OnLayoutTypeDropdownValueChanged "+ result);

            DisposeEnumerator();
            DebugMat.clear();
            // Change how DebugMat arranges multiple preview windows on screen.
            DebugMat.setup((DebugMat.LayoutType)result);
        }

        public void OnFaceDetectionExampleButtonClick()
        {
            // ---------------------------------------------------
            //  FaceDetectionExample
            // ---------------------------------------------------

            DebugMat.destroyAllWindows();

            DisposeEnumerator();
            StartEnumerator(FaceDetectionExample());

            ExampleCodeText.text = @"
            // ---------------------------------------------------
            //  FaceDetectionExample
            // ---------------------------------------------------
            // Uses CascadeClassifier on a single still image.

            string HAAR_CASCADE_FILEPATH = ""OpenCVForUnityExamples/objdetect/haarcascade_frontalface_alt.xml"";

            string cascade_filepath = null;

#if UNITY_WEBGL
            IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
            // WebGL cannot access StreamingAssets synchronously; wait for the async path resolver.
            getFilePath_Coroutine = OpenCVForUnityEnv.GetFilePathCoroutine(HAAR_CASCADE_FILEPATH,
                (result) =>
                {
                    getFilePath_Coroutine = null;

                    if (string.IsNullOrEmpty(result))
                    {
                        Debug.LogError(HAAR_CASCADE_FILEPATH + "" is not loaded. Please move from ""OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/"" to ""Assets/StreamingAssets/OpenCVForUnityExamples/"" folder."");
                    }
                    else
                    {
                        cascade_filepath = result;
                    }
                },
                    (result, progress) =>
                    {
                        Debug.Log(""getFilePathAsync() progress : "" + result + "" "" + Mathf.CeilToInt(progress * 100) + ""%"");
                    });
            yield return StartCoroutine(getFilePath_Coroutine);
#else
            cascade_filepath = OpenCVForUnityEnv.GetFilePath(HAAR_CASCADE_FILEPATH);
            if (string.IsNullOrEmpty(cascade_filepath))
            {
                Debug.LogError(HAAR_CASCADE_FILEPATH + "" is not loaded. Please move from ""OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/"" to ""Assets/StreamingAssets/OpenCVForUnityExamples/"" folder."");
            }
#endif

            Texture2D imgTexture = Resources.Load(""face"") as Texture2D;

            using (CascadeClassifier cascade = new CascadeClassifier(cascade_filepath))
            // CV_8UC4 matches Texture2D RGBA32 layout copied by Texture2DToMat.
            using (Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4))
            {

                OpenCVMatUnityUtils.Texture2DToMat(imgTexture, imgMat);

                //The specified Mat can be displayed in the debug window. Click to enlarge the image.
                <color=#ff0000>DebugMat.imshow(""imgMat"", imgMat);</color>

                if (cascade == null)
                {
                    Imgproc.putText(imgMat, ""model file is not loaded."", new Point(5, imgMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(imgMat, ""Please read console message."", new Point(5, imgMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                    //If you specify a winname that has already been registered, the window with the same winname will be overwritten.
                    <color=#ff0000>DebugMat.imshow(""imgMat"", imgMat);</color>
                    yield break;
                }

                using (Mat grayMat = new Mat())
                {
                    // CascadeClassifier expects a single-channel grayscale image.
                    Imgproc.cvtColor(imgMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
                    <color=#ff0000>DebugMat.imshow(""grayMat"", grayMat);</color>
                    // equalizeHist improves contrast and helps detection on uneven lighting.
                    Imgproc.equalizeHist(grayMat, grayMat);
                    <color=#ff0000>DebugMat.imshow(""equalizeHist"", grayMat);</color>

                    // MatOfRect receives output rectangles from detectMultiScale.
                    using (MatOfRect faces = new MatOfRect())
                    {

                        if (cascade != null)
                            cascade.detectMultiScale(grayMat, faces, 1.1, 2, 0 | Objdetect.CASCADE_SCALE_IMAGE,
                                new Size(30, 30));

                        //If the dump flag is enabled, the Mat value can be dumped.
                        <color=#ff0000>DebugMat.imshow(""faces"", faces, true, null);</color>

                        OpenCVForUnity.CoreModule.Rect[] rects = faces.toArray();
                        for (int i = 0; i < rects.Length; i++)
                        {
                            //If roi is specified, a portion of the Mat is displayed.
                            <color=#ff0000>DebugMat.imshow(""facesMat"", imgMat, false, rects[i]);</color>

                            Imgproc.rectangle(imgMat, new Point(rects[i].x, rects[i].y), new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 0, 0, 255), 8);
                        }
                    }
                }

                <color=#ff0000>DebugMat.imshow(""result"", imgMat);</color>
            }

            yield break;
            ";

            UpdateScrollRect();
        }

        public IEnumerator FaceDetectionExample()
        {
            // ---------------------------------------------------
            //  FaceDetectionExample
            // ---------------------------------------------------
            // Uses CascadeClassifier on a single still image.

            const string HAAR_CASCADE_FILEPATH = "OpenCVForUnityExamples/objdetect/haarcascade_frontalface_alt.xml";

            string cascade_filepath = null;

#if UNITY_WEBGL
            IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
            // WebGL cannot access StreamingAssets synchronously; wait for the async path resolver.
            getFilePath_Coroutine = OpenCVForUnityEnv.GetFilePathCoroutine(HAAR_CASCADE_FILEPATH,
                (result) =>
                {
                    getFilePath_Coroutine = null;

                    if (string.IsNullOrEmpty(result))
                    {
                        Debug.LogError(HAAR_CASCADE_FILEPATH + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                    }
                    else
                    {
                        cascade_filepath = result;
                    }
                },
                    (result, progress) =>
                    {
                        Debug.Log("getFilePathAsync() progress : " + result + " " + Mathf.CeilToInt(progress * 100) + "%", this);
                    });
            yield return StartCoroutine(getFilePath_Coroutine);
#else
            cascade_filepath = OpenCVForUnityEnv.GetFilePath(HAAR_CASCADE_FILEPATH);
            if (string.IsNullOrEmpty(cascade_filepath))
            {
                Debug.LogError(HAAR_CASCADE_FILEPATH + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
            }
#endif

            Texture2D imgTexture = Resources.Load("face") as Texture2D;

            using (CascadeClassifier cascade = new CascadeClassifier(cascade_filepath))
            // CV_8UC4 matches Texture2D RGBA32 layout copied by Texture2DToMat.
            using (Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4))
            {

                OpenCVMatUnityUtils.Texture2DToMat(imgTexture, imgMat);

                //The specified Mat can be displayed in the debug window. Click to enlarge the image.
                DebugMat.imshow("imgMat", imgMat);

                if (cascade == null)
                {
                    Imgproc.putText(imgMat, "model file is not loaded.", new Point(5, imgMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(imgMat, "Please read console message.", new Point(5, imgMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                    //If you specify a winname that has already been registered, the window with the same winname will be overwritten.
                    DebugMat.imshow("imgMat", imgMat);
                    yield break;
                }

                using (Mat grayMat = new Mat())
                {
                    // CascadeClassifier expects a single-channel grayscale image.
                    Imgproc.cvtColor(imgMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
                    DebugMat.imshow("grayMat", grayMat);
                    // equalizeHist improves contrast and helps detection on uneven lighting.
                    Imgproc.equalizeHist(grayMat, grayMat);
                    DebugMat.imshow("equalizeHist", grayMat);

                    // MatOfRect receives output rectangles from detectMultiScale.
                    using (MatOfRect faces = new MatOfRect())
                    {

                        if (cascade != null)
                        {
                            cascade.detectMultiScale(grayMat, faces, 1.1, 2, 0 | Xobjdetect.CASCADE_SCALE_IMAGE,
                                new Size(30, 30));
                        }

                        //If the dump flag is enabled, the Mat value can be dumped.
                        DebugMat.imshow("faces", faces, true, null);

                        OpenCVForUnity.CoreModule.Rect[] rects = faces.toArray();
                        for (int i = 0; i < rects.Length; i++)
                        {
                            //If roi is specified, a portion of the Mat is displayed.
                            DebugMat.imshow("facesMat", imgMat, false, rects[i]);

                            Imgproc.rectangle(imgMat, new Point(rects[i].x, rects[i].y), new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 0, 0, 255), 8);
                        }
                    }
                }

                DebugMat.imshow("result", imgMat);
            }

            yield break;
        }

        public void OnVideoCaptureExampleButtonClick()
        {
            // ---------------------------------------------------
            //  VideoCaptureExample
            // ---------------------------------------------------

            DebugMat.destroyAllWindows();

            DisposeEnumerator();
            StartEnumerator(VideoCaptureExample());

            ExampleCodeText.text = @"
            // ---------------------------------------------------
            //  VideoCaptureExample
            // ---------------------------------------------------
            // Reads a video file with grab()/retrieve() and shows each frame in DebugMat.

            const string VIDEO_FILEPATH = ""OpenCVForUnityExamples/768x576_mjpeg.mjpeg"";

            string video_filepath = null;

#if UNITY_WEBGL
            IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
            getFilePath_Coroutine = OpenCVForUnityEnv.GetFilePathAsync(VIDEO_FILEPATH, (result) =>
            {
                getFilePath_Coroutine = null;

                video_filepath = result;
            });
            yield return StartCoroutine(getFilePath_Coroutine);
#else
            video_filepath = OpenCVForUnityEnv.GetFilePath(VIDEO_FILEPATH);
#endif

            using (VideoCapture capture = new VideoCapture())
            using (Mat rgbMat = new Mat())
            {
                capture.open(video_filepath);

                // grab() advances to the next frame; retrieve() decodes it into rgbMat.
                while (capture.grab())
                {

                    capture.retrieve(rgbMat);

                    //It is possible to display Mat updated every frame. If the size of the Mat is the same as the old Mat, there is no new allocation.
                    // VideoCapture returns BGR byte order by default.
                    <color=#ff0000>DebugMat.imshow(""bgrMat"", rgbMat);</color>

                    // Convert to RGB when you need Unity-friendly channel order for display or Texture2D output.
                    Imgproc.cvtColor(rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);
                    <color=#ff0000>DebugMat.imshow(""rgbMat"", rgbMat);</color>

                    // Loop the sample clip when the end is reached.
                    if (capture.get(Videoio.CAP_PROP_POS_FRAMES) >= capture.get(Videoio.CAP_PROP_FRAME_COUNT))
                        capture.set(Videoio.CAP_PROP_POS_FRAMES, 0);

                    yield return null;
                }
            }

            yield break;
            ";

            UpdateScrollRect();
        }

        public IEnumerator VideoCaptureExample()
        {
            // ---------------------------------------------------
            //  VideoCaptureExample
            // ---------------------------------------------------
            // Reads a video file with grab()/retrieve() and shows each frame in DebugMat.

            const string VIDEO_FILEPATH = "OpenCVForUnityExamples/768x576_mjpeg.mjpeg";

            string video_filepath = null;

#if UNITY_WEBGL
            IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
            getFilePath_Coroutine = OpenCVForUnityEnv.GetFilePathCoroutine(VIDEO_FILEPATH, (result) =>
            {
                getFilePath_Coroutine = null;

                video_filepath = result;
            });
            yield return StartCoroutine(getFilePath_Coroutine);
#else
            video_filepath = OpenCVForUnityEnv.GetFilePath(VIDEO_FILEPATH);
#endif

            using (VideoCapture capture = new VideoCapture())
            using (Mat rgbMat = new Mat())
            {
                capture.open(video_filepath);

                // grab() advances to the next frame; retrieve() decodes it into rgbMat.
                while (capture.grab())
                {

                    capture.retrieve(rgbMat);

                    //It is possible to display Mat updated every frame. If the size of the Mat is the same as the old Mat, there is no new allocation.
                    // VideoCapture returns BGR byte order by default.
                    DebugMat.imshow("bgrMat", rgbMat);

                    // Convert to RGB when you need Unity-friendly channel order for display or Texture2D output.
                    Imgproc.cvtColor(rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);
                    DebugMat.imshow("rgbMat", rgbMat);

                    // Loop the sample clip when the end is reached.
                    if (capture.get(Videoio.CAP_PROP_POS_FRAMES) >= capture.get(Videoio.CAP_PROP_FRAME_COUNT))
                    {
                        capture.set(Videoio.CAP_PROP_POS_FRAMES, 0);
                    }

                    yield return null;
                }
            }

            yield break;
        }

        public void OnTrackingExampleButtonClick()
        {
            // ---------------------------------------------------
            //  TrackingExample
            // ---------------------------------------------------

            DebugMat.destroyAllWindows();

            DisposeEnumerator();
            StartEnumerator(TrackingExample());

            ExampleCodeText.text = @"
            // ---------------------------------------------------
            //  TrackingExample
            // ---------------------------------------------------
            // Initializes TrackerCSRT on one frame, then updates the rectangle on later frames.

            const string VIDEO_FILEPATH = ""OpenCVForUnity/768x576_mjpeg.mjpeg"";

            string video_filepath = null;

#if UNITY_WEBGL
            IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
            getFilePath_Coroutine = OpenCVForUnityEnv.GetFilePathAsync(VIDEO_FILEPATH, (result) =>
            {
                getFilePath_Coroutine = null;

                video_filepath = result;
            });
            yield return StartCoroutine(getFilePath_Coroutine);
#else
            video_filepath = OpenCVForUnityEnv.GetFilePath(VIDEO_FILEPATH);
#endif

            using (VideoCapture capture = new VideoCapture())
            using (Mat rgbMat = new Mat())
            using (TrackerCSRT tracker = TrackerCSRT.create(new TrackerCSRT_Params()))
            {

                capture.open(video_filepath);

                // Read the first frame and tell the tracker which object region to follow.
                capture.grab();
                capture.retrieve(rgbMat);

                OpenCVForUnity.CoreModule.Rect region = new OpenCVForUnity.CoreModule.Rect(610, 235, 90, 110);
                tracker.init(rgbMat, region);

                // Jump to a later frame so movement is visible in the demo loop.
                capture.set(Videoio.CAP_PROP_POS_FRAMES, 23);

                while (capture.grab())
                {

                    capture.retrieve(rgbMat);
                    <color=#ff0000>DebugMat.imshow(""bgrMat"", rgbMat);</color>

                    Imgproc.cvtColor(rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);
                    <color=#ff0000>DebugMat.imshow(""rgbMat"", rgbMat);</color>

                    // tracker.update() moves region to the object location in the current frame.
                    tracker.update(rgbMat, region);
                    <color=#ff0000>DebugMat.imshow(""trackedRegion"", rgbMat, false, region);</color>

                    Imgproc.rectangle(rgbMat, region.tl(), region.br(), new Scalar(255, 0, 0), 4);
                    <color=#ff0000>DebugMat.imshow(""result"", rgbMat);</color>

                    if (capture.get(Videoio.CAP_PROP_POS_FRAMES) >= 360)
                        capture.set(Videoio.CAP_PROP_POS_FRAMES, 23);

                    yield return null;
                }
            }

            yield break;
            ";

            UpdateScrollRect();
        }

        public IEnumerator TrackingExample()
        {
            // ---------------------------------------------------
            //  TrackingExample
            // ---------------------------------------------------
            // Initializes TrackerCSRT on one frame, then updates the rectangle on later frames.

            const string VIDEO_FILEPATH = "OpenCVForUnityExamples/768x576_mjpeg.mjpeg";

            string video_filepath = null;

#if UNITY_WEBGL
            IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
            getFilePath_Coroutine = OpenCVForUnityEnv.GetFilePathCoroutine(VIDEO_FILEPATH, (result) =>
            {
                getFilePath_Coroutine = null;

                video_filepath = result;
            });
            yield return StartCoroutine(getFilePath_Coroutine);
#else
            video_filepath = OpenCVForUnityEnv.GetFilePath(VIDEO_FILEPATH);
#endif

            using (VideoCapture capture = new VideoCapture())
            using (Mat rgbMat = new Mat())
            using (TrackerCSRT tracker = TrackerCSRT.create(new TrackerCSRT_Params()))
            {

                capture.open(video_filepath);

                // Read the first frame and tell the tracker which object region to follow.
                capture.grab();
                capture.retrieve(rgbMat);

                OpenCVForUnity.CoreModule.Rect region = new OpenCVForUnity.CoreModule.Rect(610, 235, 90, 110);
                tracker.init(rgbMat, region);

                // Jump to a later frame so movement is visible in the demo loop.
                capture.set(Videoio.CAP_PROP_POS_FRAMES, 23);

                while (capture.grab())
                {

                    capture.retrieve(rgbMat);
                    DebugMat.imshow("bgrMat", rgbMat);

                    Imgproc.cvtColor(rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);
                    DebugMat.imshow("rgbMat", rgbMat);

                    // tracker.update() moves region to the object location in the current frame.
                    tracker.update(rgbMat, region);
                    DebugMat.imshow("trackedRegion", rgbMat, false, region);

                    Imgproc.rectangle(rgbMat, region.tl(), region.br(), new Scalar(255, 0, 0), 4);
                    DebugMat.imshow("result", rgbMat);

                    if (capture.get(Videoio.CAP_PROP_POS_FRAMES) >= 360)
                    {
                        capture.set(Videoio.CAP_PROP_POS_FRAMES, 23);
                    }

                    yield return null;
                }
            }

            yield break;
        }

        public void OnDumpExampleButtonClick()
        {
            // ---------------------------------------------------
            //  DumpExample
            // ---------------------------------------------------
            // Shows raw pixel / matrix values through DebugMat instead of only an image preview.

            DebugMat.destroyAllWindows();

            DisposeEnumerator();

            Texture2D imgTexture = Resources.Load("face") as Texture2D;
            DebugMat.imshow("imgTexture_all", imgTexture);
            // DumpMode prints numeric values; the Rect limits output to a small ROI for readability.
            DebugMat.imshow("imgTexture", imgTexture, true, DebugMat.DumpMode.GetPixels32Mode, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);
            OpenCVMatUnityUtils.Texture2DToMat(imgTexture, imgMat);
            DebugMat.imshow("imgMat_all", imgMat);

            DebugMat.imshow("imgMat", imgMat, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));

            Mat imgMat_32F = new Mat();
            // convertTo changes element type; here 8-bit [0,255] becomes float [0,1].
            imgMat.convertTo(imgMat_32F, CvType.CV_32F, 1.0 / 255.0);
            DebugMat.imshow("imgMat_32F", imgMat_32F, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));

            Mat imgMat_64F = new Mat();
            imgMat.convertTo(imgMat_64F, CvType.CV_64F, 1.0 / 255.0);
            DebugMat.imshow("imgMat_64F", imgMat_64F, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));

            Core.multiply(imgMat_64F, Scalar.all(0.5), imgMat_64F);
            DebugMat.imshow("Core.multiply(imgMat_64F, Scalar.all(0.5), imgMat_64F);", imgMat_64F, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));

            ExampleCodeText.text = @"
            // ---------------------------------------------------
            //  DumpExample
            // ---------------------------------------------------
            // Shows raw pixel / matrix values through DebugMat instead of only an image preview.

            Texture2D imgTexture = Resources.Load(""face"") as Texture2D;
            <color=#ff0000>DebugMat.imshow(""imgTexture"", imgTexture);</color>
            // DumpMode prints numeric values; the Rect limits output to a small ROI for readability.
            <color=#ff0000>DebugMat.imshow(""imgTexture"", imgTexture, true, DebugMat.DumpMode.GetPixels32Mode, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));</color>

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);
            OpenCVMatUnityUtils.Texture2DToMat(imgTexture, imgMat);
            <color=#ff0000>DebugMat.imshow(""imgMat_all"", imgMat);</color>

            <color=#ff0000>DebugMat.imshow(""imgMat"", imgMat, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));</color>

            Mat imgMat_32F = new Mat();
            // convertTo changes element type; here 8-bit [0,255] becomes float [0,1].
            imgMat.convertTo(imgMat_32F, CvType.CV_32F, 1.0 / 255.0);
            <color=#ff0000>DebugMat.imshow(""imgMat_32F"", imgMat_32F, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));</color>

            Mat imgMat_64F = new Mat();
            imgMat.convertTo(imgMat_64F, CvType.CV_64F, 1.0 / 255.0);
            <color=#ff0000>DebugMat.imshow(""imgMat_64F"", imgMat_64F, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));</color>

            Core.multiply(imgMat_64F, Scalar.all(0.5), imgMat_64F);
            <color=#ff0000>DebugMat.imshow(""Core.multiply(imgMat_64F, Scalar.all(0.5), imgMat_64F)"", imgMat_64F, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));</color>
            ";

            UpdateScrollRect();
        }

        public void OnCVExceptionHandlingExampleButtonClick()
        {
            // ---------------------------------------------------
            //  CVExceptionHandlingExample
            // ---------------------------------------------------
            // Demonstrates what happens when Mat element types do not match in Core operations.

            DebugMat.destroyAllWindows();

            DisposeEnumerator();

            // 32F, channels=1, 3x3
            Mat m1 = new Mat(3, 3, CvType.CV_32FC1);
            m1.put(0, 0, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f);
            DebugMat.imshow("m1", m1, true);

            // 8U, channels=1, 3x3
            Mat m2 = new Mat(3, 3, CvType.CV_8UC1);
            m2.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            DebugMat.imshow("m2", m2, true);

            // CVException handling
            // Publish CVException to Debug.LogError.
            OpenCVDebug.SetDebugMode(true, false, (str) =>
            {
                DebugMat.imshow(null, str);
            });

            Mat m3 = new Mat();
            // Core.divide requires compatible element types; float Mat / byte Mat triggers CVException.
            Core.divide(m1, m2, m3); // element type is different.

            OpenCVDebug.SetDebugMode(false);

            // Throw CVException.
            OpenCVDebug.SetDebugMode(true, true, (str) =>
            {
                DebugMat.imshow(null, str);
            });
            try
            {
                Mat m4 = new Mat();
                Core.divide(m1, m2, m4); // element type is different.
            }
            catch (Exception e)
            {
                Debug.Log("CVException: " + e, this);
            }
            OpenCVDebug.SetDebugMode(false);

            ExampleCodeText.text = @"
            // ---------------------------------------------------
            //  CVExceptionHandlingExample
            // ---------------------------------------------------
            // Demonstrates what happens when Mat element types do not match in Core operations.

            // 32F, channels=1, 3x3
            Mat m1 = new Mat(3, 3, CvType.CV_32FC1);
            m1.put(0, 0, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f);
            <color=#ff0000>DebugMat.imshow(""m1"", m1, true);</color>

            // 8U, channels=1, 3x3
            Mat m2 = new Mat(3, 3, CvType.CV_8UC1);
            m2.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            <color=#ff0000>DebugMat.imshow(""m2"", m2, true);</color>

            // CVException handling
            // Publish CVException to Debug.LogError.
            OpenCVDebug.SetDebugMode(true, false, (str) =>
            {
                <color=#ff0000>DebugMat.imshow(null, str);</color>
            });

            Mat m3 = new Mat();
            // Core.divide requires compatible element types; float Mat / byte Mat triggers CVException.
            Core.divide(m1, m2, m3); // element type is different.

            OpenCVDebug.SetDebugMode(false);

            // Throw CVException.
            OpenCVDebug.SetDebugMode(true, true, (str) =>
            {
                <color=#ff0000>DebugMat.imshow(null, str);</color>
            });
            try
            {
                Mat m4 = new Mat();
                Core.divide(m1, m2, m4); // element type is different.
            }
            catch (Exception e)
            {
                Debug.Log(""CVException: "" + e);
            }
            OpenCVDebug.SetDebugMode(false);
            ";

            UpdateScrollRect();
        }
    }
}
