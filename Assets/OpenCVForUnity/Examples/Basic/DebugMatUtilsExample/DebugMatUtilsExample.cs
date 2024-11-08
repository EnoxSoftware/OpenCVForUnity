using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.TrackingModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.VideoioModule;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Mat Basic Processing Example
    /// </summary>
    public class DebugMatUtilsExample : MonoBehaviour
    {
        private IEnumerator enumerator;

        public ScrollRect exampleCodeScrollRect;
        public UnityEngine.UI.Text exampleCodeText;

        // Use this for initialization
        IEnumerator Start()
        {
            // fix the screen orientation.
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            // wait for the screen orientation to change.
            yield return null;
        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            Screen.orientation = ScreenOrientation.AutoRotation;

            DisposeEnumerator();
        }

        private void UpdateScrollRect()
        {
            exampleCodeScrollRect.verticalNormalizedPosition = 1f;
        }

        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        public void OnLayoutTypeDropdownValueChanged(int result)
        {
            //Debug.Log("OnLayoutTypeDropdownValueChanged "+ result);

            DisposeEnumerator();
            DebugMatUtils.clear();
            DebugMatUtils.setup((DebugMatUtils.LayoutType)result);
        }

        void DisposeEnumerator()
        {
            if (enumerator != null)
            {
                (enumerator as IDisposable)?.Dispose();
                StopCoroutine(enumerator);
                enumerator = null;
            }
        }

        void StartEnumerator(IEnumerator enumerator)
        {
            this.enumerator = enumerator;
            StartCoroutine(this.enumerator);
        }

        public void OnFaceDetectionExampleButtonClick()
        {
            //
            // FaceDetectionExample
            //

            DebugMatUtils.destroyAllWindows();

            DisposeEnumerator();
            StartEnumerator(FaceDetectionExample());

            exampleCodeText.text = @"
            //
            // FaceDetectionExample
            //

            string HAAR_CASCADE_FILENAME = ""OpenCVForUnity / objdetect / haarcascade_frontalface_alt.xml"";

            string cascade_filepath = null;

#if UNITY_WEBGL
            IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
            getFilePath_Coroutine = Utils.getFilePathAsync(HAAR_CASCADE_FILENAME,
                (result) =>
                {
                    getFilePath_Coroutine = null;

                    if (string.IsNullOrEmpty(result))
                    {
                        Debug.LogError(HAAR_CASCADE_FILENAME + "" is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder."");
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
            cascade_filepath = Utils.getFilePath(HAAR_CASCADE_FILENAME);
            if (string.IsNullOrEmpty(cascade_filepath))
            {
                Debug.LogError(HAAR_CASCADE_FILENAME + "" is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder."");
            }
#endif

            Texture2D imgTexture = Resources.Load(""face"") as Texture2D;

            using (CascadeClassifier cascade = new CascadeClassifier(cascade_filepath))
            using (Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4))
            {

                Utils.texture2DToMat(imgTexture, imgMat);

                //The specified Mat can be displayed in the debug window. Click to enlarge the image.
                <color=#ff0000>DebugMatUtils.imshow(""imgMat"", imgMat);</color>

                if (cascade == null)
                {
                    Imgproc.putText(imgMat, ""model file is not loaded."", new Point(5, imgMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(imgMat, ""Please read console message."", new Point(5, imgMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                    //If you specify a winname that has already been registered, the window with the same winname will be overwritten.
                    <color=#ff0000>DebugMatUtils.imshow(""imgMat"", imgMat);</color>
                    yield break;
                }

                using (Mat grayMat = new Mat())
                {
                    Imgproc.cvtColor(imgMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
                    <color=#ff0000>DebugMatUtils.imshow(""grayMat"", grayMat);</color>
                    Imgproc.equalizeHist(grayMat, grayMat);
                    <color=#ff0000>DebugMatUtils.imshow(""equalizeHist"", grayMat);</color>

                    using (MatOfRect faces = new MatOfRect())
                    {

                        if (cascade != null)
                            cascade.detectMultiScale(grayMat, faces, 1.1, 2, 2,
                                new Size(20, 20), new Size());

                        //If the dump flag is enabled, the Mat value can be dumped.
                        <color=#ff0000>DebugMatUtils.imshow(""faces"", faces, true, null);</color>

                        OpenCVForUnity.CoreModule.Rect[] rects = faces.toArray();
                        for (int i = 0; i < rects.Length; i++)
                        {
                            //If roi is specified, a portion of the Mat is displayed.
                            <color=#ff0000>DebugMatUtils.imshow(""facesMat"", imgMat, false, rects[i]);</color>

                            Imgproc.rectangle(imgMat, new Point(rects[i].x, rects[i].y), new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 0, 0, 255), 2);
                        }
                    }
                }

                <color=#ff0000>DebugMatUtils.imshow(""result"", imgMat);</color>
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

            string HAAR_CASCADE_FILENAME = "OpenCVForUnity/objdetect/haarcascade_frontalface_alt.xml";

            string cascade_filepath = null;

#if UNITY_WEBGL
            IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
            getFilePath_Coroutine = Utils.getFilePathAsync(HAAR_CASCADE_FILENAME,
                (result) =>
                {
                    getFilePath_Coroutine = null;

                    if (string.IsNullOrEmpty(result))
                    {
                        Debug.LogError(HAAR_CASCADE_FILENAME + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
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
            cascade_filepath = Utils.getFilePath(HAAR_CASCADE_FILENAME);
            if (string.IsNullOrEmpty(cascade_filepath))
            {
                Debug.LogError(HAAR_CASCADE_FILENAME + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
            }
#endif

            Texture2D imgTexture = Resources.Load("face") as Texture2D;

            using (CascadeClassifier cascade = new CascadeClassifier(cascade_filepath))
            using (Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4))
            {

                Utils.texture2DToMat(imgTexture, imgMat);

                //The specified Mat can be displayed in the debug window. Click to enlarge the image.
                DebugMatUtils.imshow("imgMat", imgMat);

                if (cascade == null)
                {
                    Imgproc.putText(imgMat, "model file is not loaded.", new Point(5, imgMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(imgMat, "Please read console message.", new Point(5, imgMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                    //If you specify a winname that has already been registered, the window with the same winname will be overwritten.
                    DebugMatUtils.imshow("imgMat", imgMat);
                    yield break;
                }

                using (Mat grayMat = new Mat())
                {
                    Imgproc.cvtColor(imgMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
                    DebugMatUtils.imshow("grayMat", grayMat);
                    Imgproc.equalizeHist(grayMat, grayMat);
                    DebugMatUtils.imshow("equalizeHist", grayMat);

                    using (MatOfRect faces = new MatOfRect())
                    {

                        if (cascade != null)
                            cascade.detectMultiScale(grayMat, faces, 1.1, 2, 2,
                                new Size(20, 20), new Size());

                        //If the dump flag is enabled, the Mat value can be dumped.
                        DebugMatUtils.imshow("faces", faces, true, null);

                        OpenCVForUnity.CoreModule.Rect[] rects = faces.toArray();
                        for (int i = 0; i < rects.Length; i++)
                        {
                            //If roi is specified, a portion of the Mat is displayed.
                            DebugMatUtils.imshow("facesMat", imgMat, false, rects[i]);

                            Imgproc.rectangle(imgMat, new Point(rects[i].x, rects[i].y), new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 0, 0, 255), 2);
                        }
                    }
                }

                DebugMatUtils.imshow("result", imgMat);
            }

            yield break;
        }

        public void OnVideoCaptureExampleButtonClick()
        {
            //
            // VideoCaptureExample
            //

            DebugMatUtils.destroyAllWindows();

            DisposeEnumerator();
            StartEnumerator(VideoCaptureExample());

            exampleCodeText.text = @"
            //
            // VideoCaptureExample
            //

            string VIDEO_FILENAME = ""OpenCVForUnity / 768x576_mjpeg.mjpeg"";

            string video_filepath = null;

#if UNITY_WEBGL
            IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
            getFilePath_Coroutine = Utils.getFilePathAsync(VIDEO_FILENAME, (result) =>
            {
                getFilePath_Coroutine = null;

                video_filepath = result;
            });
            yield return StartCoroutine(getFilePath_Coroutine);
#else
            video_filepath = Utils.getFilePath(VIDEO_FILENAME);
#endif

            using (VideoCapture capture = new VideoCapture())
            using (Mat rgbMat = new Mat())
            {
                capture.open(video_filepath);

                while (capture.grab())
                {

                    capture.retrieve(rgbMat);

                    //It is possible to display Mat updated every frame. If the size of the Mat is the same as the old Mat, there is no new allocation.
                    <color=#ff0000>DebugMatUtils.imshow(""bgrMat"", rgbMat);</color>

                    Imgproc.cvtColor(rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);
                    <color=#ff0000>DebugMatUtils.imshow(""rgbMat"", rgbMat);</color>

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

            string VIDEO_FILENAME = "OpenCVForUnity/768x576_mjpeg.mjpeg";

            string video_filepath = null;

#if UNITY_WEBGL
            IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
            getFilePath_Coroutine = Utils.getFilePathAsync(VIDEO_FILENAME, (result) =>
            {
                getFilePath_Coroutine = null;

                video_filepath = result;
            });
            yield return StartCoroutine(getFilePath_Coroutine);
#else
            video_filepath = Utils.getFilePath(VIDEO_FILENAME);
#endif

            using (VideoCapture capture = new VideoCapture())
            using (Mat rgbMat = new Mat())
            {
                capture.open(video_filepath);

                while (capture.grab())
                {

                    capture.retrieve(rgbMat);

                    //It is possible to display Mat updated every frame. If the size of the Mat is the same as the old Mat, there is no new allocation.
                    DebugMatUtils.imshow("bgrMat", rgbMat);

                    Imgproc.cvtColor(rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);
                    DebugMatUtils.imshow("rgbMat", rgbMat);

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

            DebugMatUtils.destroyAllWindows();

            DisposeEnumerator();
            StartEnumerator(TrackingExample());

            exampleCodeText.text = @"
            //
            // TrackingExample
            //

            string VIDEO_FILENAME = ""OpenCVForUnity / 768x576_mjpeg.mjpeg"";

            string video_filepath = null;

#if UNITY_WEBGL
            IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
            getFilePath_Coroutine = Utils.getFilePathAsync(VIDEO_FILENAME, (result) =>
            {
                getFilePath_Coroutine = null;

                video_filepath = result;
            });
            yield return StartCoroutine(getFilePath_Coroutine);
#else
            video_filepath = Utils.getFilePath(VIDEO_FILENAME);
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
                    <color=#ff0000>DebugMatUtils.imshow(""bgrMat"", rgbMat);</color>

                    Imgproc.cvtColor(rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);
                    <color=#ff0000>DebugMatUtils.imshow(""rgbMat"", rgbMat);</color>

                    tracker.update(rgbMat, region);
                    <color=#ff0000>DebugMatUtils.imshow(""trackedRegion"", rgbMat, false, region);</color>

                    Imgproc.rectangle(rgbMat, region.tl(), region.br(), new Scalar(255, 0, 0), 4);
                    <color=#ff0000>DebugMatUtils.imshow(""result"", rgbMat);</color>

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

            string VIDEO_FILENAME = "OpenCVForUnity/768x576_mjpeg.mjpeg";

            string video_filepath = null;

#if UNITY_WEBGL
            IEnumerator getFilePath_Coroutine;
#endif

#if UNITY_WEBGL
            getFilePath_Coroutine = Utils.getFilePathAsync(VIDEO_FILENAME, (result) =>
            {
                getFilePath_Coroutine = null;

                video_filepath = result;
            });
            yield return StartCoroutine(getFilePath_Coroutine);
#else
            video_filepath = Utils.getFilePath(VIDEO_FILENAME);
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
                    DebugMatUtils.imshow("bgrMat", rgbMat);

                    Imgproc.cvtColor(rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);
                    DebugMatUtils.imshow("rgbMat", rgbMat);

                    tracker.update(rgbMat, region);
                    DebugMatUtils.imshow("trackedRegion", rgbMat, false, region);

                    Imgproc.rectangle(rgbMat, region.tl(), region.br(), new Scalar(255, 0, 0), 4);
                    DebugMatUtils.imshow("result", rgbMat);

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

            DebugMatUtils.destroyAllWindows();

            DisposeEnumerator();

            Texture2D imgTexture = Resources.Load("face") as Texture2D;
            DebugMatUtils.imshow("imgTexture_all", imgTexture);
            DebugMatUtils.imshow("imgTexture", imgTexture, true, DebugMatUtils.DumpMode.GetPixels32Mode, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);
            Utils.texture2DToMat(imgTexture, imgMat);
            DebugMatUtils.imshow("imgMat_all", imgMat);

            DebugMatUtils.imshow("imgMat", imgMat, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));

            Mat imgMat_32F = new Mat();
            imgMat.convertTo(imgMat_32F, CvType.CV_32F, 1.0 / 255.0);
            DebugMatUtils.imshow("imgMat_32F", imgMat_32F, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));

            Mat imgMat_64F = new Mat();
            imgMat.convertTo(imgMat_64F, CvType.CV_64F, 1.0 / 255.0);
            DebugMatUtils.imshow("imgMat_64F", imgMat_64F, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));


            imgMat_64F = imgMat_64F * 0.5;
            DebugMatUtils.imshow("imgMat_64F*0.5", imgMat_64F, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));

            exampleCodeText.text = @"
            //
            // DumpExample
            //

            Texture2D imgTexture = Resources.Load(""face"") as Texture2D;
            <color=#ff0000>DebugMatUtils.imshow(""imgTexture"", imgTexture);</color>
            <color=#ff0000>DebugMatUtils.imshow(""imgTexture"", imgTexture, true, DebugMatUtils.DumpMode.GetPixels32Mode, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));</color>

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);
            Utils.texture2DToMat(imgTexture, imgMat);
            <color=#ff0000>DebugMatUtils.imshow(""imgMat_all"", imgMat);</color>

            <color=#ff0000>DebugMatUtils.imshow(""imgMat"", imgMat, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));</color>

            Mat imgMat_32F = new Mat();
            imgMat.convertTo(imgMat_32F, CvType.CV_32F, 1.0 / 255.0);
            <color=#ff0000>DebugMatUtils.imshow(""imgMat_32F"", imgMat_32F, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));</color>

            Mat imgMat_64F = new Mat();
            imgMat.convertTo(imgMat_64F, CvType.CV_64F, 1.0 / 255.0);
            <color=#ff0000>DebugMatUtils.imshow(""imgMat_64F"", imgMat_64F, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));</color>


            imgMat_64F = imgMat_64F * 0.5;
            <color=#ff0000>DebugMatUtils.imshow(""imgMat_64F*0.5"", imgMat_64F, true, new OpenCVForUnity.CoreModule.Rect(180, 230, 20, 20));</color>
            ";

            UpdateScrollRect();
        }

        public void OnCVExceptionHandlingExampleButtonClick()
        {
            //
            // CVExceptionHandlingExample
            //

            DebugMatUtils.destroyAllWindows();

            DisposeEnumerator();

            // 32F, channels=1, 3x3
            Mat m1 = new Mat(3, 3, CvType.CV_32FC1);
            m1.put(0, 0, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f);
            DebugMatUtils.imshow("m1", m1, true);

            // 8U, channels=1, 3x3
            Mat m2 = new Mat(3, 3, CvType.CV_8UC1);
            m2.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            DebugMatUtils.imshow("m2", m2, true);

            // CVException handling
            // Publish CVException to Debug.LogError.
            Utils.setDebugMode(true, false, (str) =>
            {
                DebugMatUtils.imshow(null, str);
            });

            Mat m3 = m1 / m2; // element type is different.

            Utils.setDebugMode(false);

            // Throw CVException.
            Utils.setDebugMode(true, true, (str) =>
            {
                DebugMatUtils.imshow(null, str);
            });
            try
            {
                Mat m4 = m1 / m2; // element type is different.
            }
            catch (Exception e)
            {
                Debug.Log("CVException: " + e);
            }
            Utils.setDebugMode(false);

            exampleCodeText.text = @"
            //
            // CVExceptionHandlingExample
            //

            // 32F, channels=1, 3x3
            Mat m1 = new Mat(3, 3, CvType.CV_32FC1);
            m1.put(0, 0, 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f);
            <color=#ff0000>DebugMatUtils.imshow(""m1"", m1, true);</color>

            // 8U, channels=1, 3x3
            Mat m2 = new Mat(3, 3, CvType.CV_8UC1);
            m2.put(0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
            <color=#ff0000>DebugMatUtils.imshow(""m2"", m2, true);</color>

            // CVException handling
            // Publish CVException to Debug.LogError.
            Utils.setDebugMode(true, false, (str) =>
            {
                <color=#ff0000>DebugMatUtils.imshow(null, str);</color>
            });

            Mat m3 = m1 / m2; // element type is different.

            Utils.setDebugMode(false);

            // Throw CVException.
            Utils.setDebugMode(true, true, (str) =>
            {
                <color=#ff0000>DebugMatUtils.imshow(null, str);</color>
            });
            try
            {
                Mat m4 = m1 / m2; // element type is different.
            }
            catch (Exception e)
            {
                Debug.Log(""CVException: "" + e);
            }
            Utils.setDebugMode(false);
            ";

            UpdateScrollRect();
        }
    }
}