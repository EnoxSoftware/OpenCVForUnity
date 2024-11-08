using OpenCVForUnity.CoreModule;
using OpenCVForUnity.FaceModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// FaceMark Example
    /// An example of detecting facial landmark using the face (FaceMark API) module.
    /// The facemark model file can be downloaded here: https://github.com/spmallick/GSOC2017/blob/master/data/lbfmodel.yaml
    /// Please copy to “Assets/StreamingAssets/face/” folder.
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class FaceMarkExample : MonoBehaviour
    {
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMat;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The cascade.
        /// </summary>
        CascadeClassifier cascade;

        /// <summary>
        /// The faces.
        /// </summary>
        MatOfRect faces;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        MultiSource2MatHelper multiSource2MatHelper;

        /// <summary>
        /// The facemark.
        /// </summary>
        Facemark facemark;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// FACEMARK_CASCADE_FILENAME
        /// </summary>
        protected static readonly string FACEMARK_CASCADE_FILENAME = "OpenCVForUnity/objdetect/lbpcascade_frontalface.xml";

        /// <summary>
        /// The facemark cascade filepath.
        /// </summary>
        string facemark_cascade_filepath;

        /// <summary>
        /// FACEMARK_CASCADE_FILENAME
        /// </summary>
        protected static readonly string FACEMARK_MODEL_FILENAME = "OpenCVForUnity/face/lbfmodel.yaml";

        /// <summary>
        /// The facemark model filepath.
        /// </summary>
        string facemark_model_filepath;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // Use this for initialization
        async void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            multiSource2MatHelper.outputColorFormat = Source2MatHelperColorFormat.RGBA;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (fpsMonitor != null)
                fpsMonitor.consoleText = "Preparing file access...";

            facemark_cascade_filepath = await Utils.getFilePathAsyncTask(FACEMARK_CASCADE_FILENAME, cancellationToken: cts.Token);
            facemark_model_filepath = await Utils.getFilePathAsyncTask(FACEMARK_MODEL_FILENAME, cancellationToken: cts.Token);

            if (fpsMonitor != null)
                fpsMonitor.consoleText = "";

            Run();
        }

        // Use this for initialization
        void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);

            if (string.IsNullOrEmpty(facemark_model_filepath))
            {
                Debug.LogError(FACEMARK_MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/face/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                // setup landmarks detector
                facemark = Face.createFacemarkLBF();
                facemark.loadModel(facemark_model_filepath);
            }

            if (string.IsNullOrEmpty(facemark_cascade_filepath))
            {
                Debug.LogError(FACEMARK_CASCADE_FILENAME + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
            }
            else
            {
                // setup face detector
                cascade = new CascadeClassifier(facemark_cascade_filepath);
            }

            multiSource2MatHelper.Initialize();

            Utils.setDebugMode(false);
        }

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = multiSource2MatHelper.GetMat();

            texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(rgbaMat, texture);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", rgbaMat.width().ToString());
                fpsMonitor.Add("height", rgbaMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            grayMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);

            faces = new MatOfRect();
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (grayMat != null)
                grayMat.Dispose();


            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

            if (faces != null)
                faces.Dispose();
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
            if (multiSource2MatHelper.IsPlaying() && multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = multiSource2MatHelper.GetMat();

                if (facemark == null || cascade == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                    Utils.matToTexture2D(rgbaMat, texture);
                    return;
                }

                Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.equalizeHist(grayMat, grayMat);

                // detect faces
                cascade.detectMultiScale(grayMat, faces, 1.1, 2, 2, // TODO: objdetect.CV_HAAR_SCALE_IMAGE
                    new Size(grayMat.cols() * 0.2, grayMat.rows() * 0.2), new Size());

                if (faces.total() > 0)
                {
                    // fit landmarks for each found face
                    List<MatOfPoint2f> landmarks = new List<MatOfPoint2f>();
                    facemark.fit(grayMat, faces, landmarks);


                    Rect[] rects = faces.toArray();
                    for (int i = 0; i < rects.Length; i++)
                    {
                        //Debug.Log ("detect faces " + rects [i]);

                        Imgproc.rectangle(rgbaMat, new Point(rects[i].x, rects[i].y), new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 0, 0, 255), 2);
                    }

                    // draw them
                    for (int i = 0; i < landmarks.Count; i++)
                    {
                        MatOfPoint2f lm = landmarks[i];
                        float[] lm_float = new float[lm.total() * lm.channels()];
                        MatUtils.copyFromMat<float>(lm, lm_float);

                        DrawFaceLandmark(rgbaMat, ConvertArrayToPointList(lm_float), new Scalar(0, 255, 0, 255), 2);

                        //for (int j = 0; j < lm_float.Length; j = j + 2)
                        //{
                        //    Point p = new Point(lm_float[j], lm_float[j + 1]);
                        //    Imgproc.circle(rgbaMat, p, 2, new Scalar(255, 0, 0, 255), 1);
                        //}
                    }
                }

                Utils.matToTexture2D(rgbaMat, texture);
            }
        }

        private void DrawFaceLandmark(Mat imgMat, List<Point> points, Scalar color, int thickness, bool drawIndexNumbers = false)
        {
            if (points.Count == 5)
            {

                Imgproc.line(imgMat, points[0], points[1], color, thickness);
                Imgproc.line(imgMat, points[1], points[4], color, thickness);
                Imgproc.line(imgMat, points[4], points[3], color, thickness);
                Imgproc.line(imgMat, points[3], points[2], color, thickness);

            }
            else if (points.Count == 68)
            {

                for (int i = 1; i <= 16; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);

                for (int i = 28; i <= 30; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);

                for (int i = 18; i <= 21; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                for (int i = 23; i <= 26; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                for (int i = 31; i <= 35; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[30], points[35], color, thickness);

                for (int i = 37; i <= 41; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[36], points[41], color, thickness);

                for (int i = 43; i <= 47; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[42], points[47], color, thickness);

                for (int i = 49; i <= 59; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[48], points[59], color, thickness);

                for (int i = 61; i <= 67; ++i)
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                Imgproc.line(imgMat, points[60], points[67], color, thickness);
            }
            else
            {
                for (int i = 0; i < points.Count; i++)
                {
                    Imgproc.circle(imgMat, points[i], 2, color, -1);
                }
            }

            // Draw the index number of facelandmark points.
            if (drawIndexNumbers)
            {
                for (int i = 0; i < points.Count; ++i)
                    Imgproc.putText(imgMat, i.ToString(), points[i], Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
            }
        }

        private List<Point> ConvertArrayToPointList(float[] arr, List<Point> pts = null)
        {
            if (pts == null)
            {
                pts = new List<Point>();
            }

            if (pts.Count != arr.Length / 2)
            {
                pts.Clear();
                for (int i = 0; i < arr.Length / 2; i++)
                {
                    pts.Add(new Point());
                }
            }

            for (int i = 0; i < pts.Count; ++i)
            {
                pts[i].x = arr[i * 2];
                pts[i].y = arr[i * 2 + 1];
            }

            return pts;
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            multiSource2MatHelper.Dispose();

            if (cascade != null)
                cascade.Dispose();

            if (facemark != null)
                facemark.Dispose();

            if (cts != null)
                cts.Dispose();
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
            multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            multiSource2MatHelper.requestedIsFrontFacing = !multiSource2MatHelper.requestedIsFrontFacing;
        }
    }
}
