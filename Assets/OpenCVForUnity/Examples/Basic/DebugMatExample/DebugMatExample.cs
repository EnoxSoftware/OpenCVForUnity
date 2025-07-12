using System;
using System.Collections;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.TrackingModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.VideoioModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Mat DebugMat Example
    /// </summary>
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
            DebugMat.setup((DebugMat.LayoutType)result);
        }

        public void OnFaceDetectionExampleButtonClick()
        {
            //
            // FaceDetectionExample
            //

            DebugMat.destroyAllWindows();

            DisposeEnumerator();
            StartEnumerator(FaceDetectionExample());

            ExampleCodeText.text = @"
            //
            // FaceDetectionExample
            //

            string HAAR_CASCADE_FILENAME = ""OpenCVForUnityExamples/objdetect/haarcascade_frontalface_alt.xml"";

            string cascade_filepath = null;

#if UNITY_WEBGL
            IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
            getFilePath_Coroutine = OpenCVEnv.GetFilePathCoroutine(HAAR_CASCADE_FILENAME,
                (result) =>
                {
                    getFilePath_Coroutine = null;

                    if (string.IsNullOrEmpty(result))
                    {
                        Debug.LogError(HAAR_CASCADE_FILENAME + "" is not loaded. Please move from ""OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/"" to ""Assets/StreamingAssets/OpenCVForUnityExamples/"" folder."");
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
            cascade_filepath = OpenCVEnv.GetFilePath(HAAR_CASCADE_FILENAME);
            if (string.IsNullOrEmpty(cascade_filepath))
            {
                Debug.LogError(HAAR_CASCADE_FILENAME + "" is not loaded. Please move from ""OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/"" to ""Assets/StreamingAssets/OpenCVForUnityExamples/"" folder."");
            }
#endif

            Texture2D imgTexture = Resources.Load(""face"") as Texture2D;

            using (CascadeClassifier cascade = new CascadeClassifier(cascade_filepath))
            using (Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4))
            {

                OpenCVMatUtils.Texture2DToMat(imgTexture, imgMat);

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
                    Imgproc.cvtColor(imgMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
                    <color=#ff0000>DebugMat.imshow(""grayMat"", grayMat);</color>
                    Imgproc.equalizeHist(grayMat, grayMat);
                    <color=#ff0000>DebugMat.imshow(""equalizeHist"", grayMat);</color>

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
            //
            // FaceDetectionExample
            //

            string HAAR_CASCADE_FILENAME = "OpenCVForUnityExamples/objdetect/haarcascade_frontalface_alt.xml";

            string cascade_filepath = null;

#if UNITY_WEBGL
            IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
            getFilePath_Coroutine = OpenCVEnv.GetFilePathCoroutine(HAAR_CASCADE_FILENAME,
                (result) =>
                {
                    getFilePath_Coroutine = null;

                    if (string.IsNullOrEmpty(result))
                    {
                        Debug.LogError(HAAR_CASCADE_FILENAME + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
                    }
                    else
                    {
                        cascade_filepath = result;
                    }
                },
                    (result, progress) =>
                    {
                        Debug.Log("getFilePathAsync() progress : " + result + " " + Mathf.CeilToInt(progress * 100) + "%");
                    });
            yield return StartCoroutine(getFilePath_Coroutine);
#else
            cascade_filepath = OpenCVEnv.GetFilePath(HAAR_CASCADE_FILENAME);
            if (string.IsNullOrEmpty(cascade_filepath))
            {
                Debug.LogError(HAAR_CASCADE_FILENAME + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
            }
#endif

            Texture2D imgTexture = Resources.Load("face") as Texture2D;

            using (CascadeClassifier cascade = new CascadeClassifier(cascade_filepath))
            using (Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4))
            {

                OpenCVMatUtils.Texture2DToMat(imgTexture, imgMat);

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
                    Imgproc.cvtColor(imgMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
                    DebugMat.imshow("grayMat", grayMat);
                    Imgproc.equalizeHist(grayMat, grayMat);
                    DebugMat.imshow("equalizeHist", grayMat);

                    using (MatOfRect faces = new MatOfRect())
                    {

                        if (cascade != null)
                            cascade.detectMultiScale(grayMat, faces, 1.1, 2, 0 | Objdetect.CASCADE_SCALE_IMAGE,
                                new Size(30, 30));

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
            //
            // VideoCaptureExample
            //

            DebugMat.destroyAllWindows();

            DisposeEnumerator();
            StartEnumerator(VideoCaptureExample());

            ExampleCodeText.text = @"
            //
            // VideoCaptureExample
            //

            string VIDEO_FILENAME = ""OpenCVForUnityExamples/768x576_mjpeg.mjpeg"";

            string video_filepath = null;

#if UNITY_WEBGL
            IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
            getFilePath_Coroutine = OpenCVEnv.GetFilePathAsync(VIDEO_FILENAME, (result) =>
            {
                getFilePath_Coroutine = null;

                video_filepath = result;
            });
            yield return StartCoroutine(getFilePath_Coroutine);
#else
            video_filepath = OpenCVEnv.GetFilePath(VIDEO_FILENAME);
#endif

            using (VideoCapture capture = new VideoCapture())
            using (Mat rgbMat = new Mat())
            {
                capture.open(video_filepath);

                while (capture.grab())
                {

                    capture.retrieve(rgbMat);

                    //It is possible to display Mat updated every frame. If the size of the Mat is the same as the old Mat, there is no new allocation.
                    <color=#ff0000>DebugMat.imshow(""bgrMat"", rgbMat);</color>

                    Imgproc.cvtColor(rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);
                    <color=#ff0000>DebugMat.imshow(""rgbMat"", rgbMat);</color>

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
            //
            // VideoCaptureExample
            //

            string VIDEO_FILENAME = "OpenCVForUnityExamples/768x576_mjpeg.mjpeg";

            string video_filepath = null;

#if UNITY_WEBGL
            IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
            getFilePath_Coroutine = OpenCVEnv.GetFilePathCoroutine(VIDEO_FILENAME, (result) =>
            {
                getFilePath_Coroutine = null;

                video_filepath = result;
            });
            yield return StartCoroutine(getFilePath_Coroutine);
#else
            video_filepath = OpenCVEnv.GetFilePath(VIDEO_FILENAME);
#endif

            using (VideoCapture capture = new VideoCapture())
            using (Mat rgbMat = new Mat())
            {
                capture.open(video_filepath);

                while (capture.grab())
                {

                    capture.retrieve(rgbMat);

                    //It is possible to display Mat updated every frame. If the size of the Mat is the same as the old Mat, there is no new allocation.
                    DebugMat.imshow("bgrMat", rgbMat);

                    Imgproc.cvtColor(rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);
                    DebugMat.imshow("rgbMat", rgbMat);

                    if (capture.get(Videoio.CAP_PROP_POS_FRAMES) >= capture.get(Videoio.CAP_PROP_FRAME_COUNT))
                        capture.set(Videoio.CAP_PROP_POS_FRAMES, 0);

                    yield return null;
                }
            }

            yield break;
        }

        public void OnTrackingExampleButtonClick()
        {
            //
            // TrackingExample
            //

            DebugMat.destroyAllWindows();

            DisposeEnumerator();
            StartEnumerator(TrackingExample());

            ExampleCodeText.text = @"
            //
            // TrackingExample
            //

            string VIDEO_FILENAME = ""OpenCVForUnity/768x576_mjpeg.mjpeg"";

            string video_filepath = null;

#if UNITY_WEBGL
            IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
            getFilePath_Coroutine = OpenCVEnv.GetFilePathAsync(VIDEO_FILENAME, (result) =>
            {
                getFilePath_Coroutine = null;

                video_filepath = result;
            });
            yield return StartCoroutine(getFilePath_Coroutine);
#else
            video_filepath = OpenCVEnv.GetFilePath(VIDEO_FILENAME);
#endif

            using (VideoCapture capture = new VideoCapture())
            using (Mat rgbMat = new Mat())
            using (TrackerCSRT tracker = TrackerCSRT.create(new TrackerCSRT_Params()))
            {

                capture.open(video_filepath);

                capture.grab();
                capture.retrieve(rgbMat);

                OpenCVForUnity.CoreModule.Rect region = new OpenCVForUnity.CoreModule.Rect(610, 235, 90, 110);
                tracker.init(rgbMat, region);

                capture.set(Videoio.CAP_PROP_POS_FRAMES, 23);

                while (capture.grab())
                {

                    capture.retrieve(rgbMat);
                    <color=#ff0000>DebugMat.imshow(""bgrMat"", rgbMat);</color>

                    Imgproc.cvtColor(rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);
                    <color=#ff0000>DebugMat.imshow(""rgbMat"", rgbMat);</color>

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
            //
            // TrackingExample
            //

            string VIDEO_FILENAME = "OpenCVForUnityExamples/768x576_mjpeg.mjpeg";

            string video_filepath = null;

#if UNITY_WEBGL
            IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
            getFilePath_Coroutine = OpenCVEnv.GetFilePathCoroutine(VIDEO_FILENAME, (result) =>
            {
                getFilePath_Coroutine = null;

                video_filepath = result;
            });
            yield return StartCoroutine(getFilePath_Coroutine);
#else
            video_filepath = OpenCVEnv.GetFilePath(VIDEO_FILENAME);
#endif

            using (VideoCapture capture = new VideoCapture())
            using (Mat rgbMat = new Mat())
            using (TrackerCSRT tracker = TrackerCSRT.create(new TrackerCSRT_Params()))
            {

                capture.open(video_filepath);

                capture.grab();
                capture.retrieve(rgbMat);

                OpenCVForUnity.CoreModule.Rect region = new OpenCVForUnity.CoreModule.Rect(610, 235, 90, 110);
                tracker.init(rgbMat, region);

                capture.set(Videoio.CAP_PROP_POS_FRAMES, 23);

                while (capture.grab())
                {

                    capture.retrieve(rgbMat);
                    DebugMat.imshow("bgrMat", rgbMat);

                    Imgproc.cvtColor(rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);
                    DebugMat.imshow("rgbMat", rgbMat);

                    tracker.update(rgbMat, region);
                    DebugMat.imshow("trackedRegion", rgbMat, false, region);

                    Imgproc.rectangle(rgbMat, region.tl(), region.br(), new Scalar(255, 0, 0), 4);
                    DebugMat.imshow("result", rgbMat);

                    if (capture.get(Videoio.CAP_PROP_POS_FRAMES) >= 360)
                        capture.set(Videoio.CAP_PROP_POS_FRAMES, 23);

                    yield return null;
                }
            }

            yield break;
        }

        public void OnDumpExampleButtonClick()
        {
            //
            // DumpExample
            //

            DebugMat.destroyAllWindows();

            DisposeEnumerator();

            Texture2D imgTexture = Resources.Load("face") as Texture2D;
            DebugMat.imshow("imgTexture_all", imgTexture);
            DebugMat.imshow("imgTexture", imgTexture, true, DebugMat.DumpMode.GetPixels32Mode, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);
            OpenCVMatUtils.Texture2DToMat(imgTexture, imgMat);
            DebugMat.imshow("imgMat_all", imgMat);

            DebugMat.imshow("imgMat", imgMat, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));

            Mat imgMat_32F = new Mat();
            imgMat.convertTo(imgMat_32F, CvType.CV_32F, 1.0 / 255.0);
            DebugMat.imshow("imgMat_32F", imgMat_32F, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));

            Mat imgMat_64F = new Mat();
            imgMat.convertTo(imgMat_64F, CvType.CV_64F, 1.0 / 255.0);
            DebugMat.imshow("imgMat_64F", imgMat_64F, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));

            Core.multiply(imgMat_64F, Scalar.all(0.5), imgMat_64F);
            DebugMat.imshow("Core.multiply(imgMat_64F, Scalar.all(0.5), imgMat_64F);", imgMat_64F, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));

            ExampleCodeText.text = @"
            //
            // DumpExample
            //

            Texture2D imgTexture = Resources.Load(""face"") as Texture2D;
            <color=#ff0000>DebugMat.imshow(""imgTexture"", imgTexture);</color>
            <color=#ff0000>DebugMat.imshow(""imgTexture"", imgTexture, true, DebugMat.DumpMode.GetPixels32Mode, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));</color>

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);
            OpenCVMatUtils.Texture2DToMat(imgTexture, imgMat);
            <color=#ff0000>DebugMat.imshow(""imgMat_all"", imgMat);</color>

            <color=#ff0000>DebugMat.imshow(""imgMat"", imgMat, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));</color>

            Mat imgMat_32F = new Mat();
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
            //
            // CVExceptionHandlingExample
            //

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
                Debug.Log("CVException: " + e);
            }
            OpenCVDebug.SetDebugMode(false);

            ExampleCodeText.text = @"
            //
            // CVExceptionHandlingExample
            //

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
