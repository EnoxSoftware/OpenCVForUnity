using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Face Detection Image Example
    /// An example of human face detection using the CascadeClassifier class.
    /// http://docs.opencv.org/3.2.0/db/d28/tutorial_cascade_classifier.html
    /// </summary>
    public class FaceDetectionImageExample : MonoBehaviour
    {
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        CascadeClassifier cascade;

        /// <summary>
        /// HAAR_CASCADE_FILENAME
        /// </summary>
        protected static readonly string HAAR_CASCADE_FILENAME = "OpenCVForUnity/objdetect/haarcascade_frontalface_alt.xml";

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // Use this for initialization
        async void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (fpsMonitor != null)
                fpsMonitor.consoleText = "Preparing file access...";

            string cascade_filepath = await Utils.getFilePathAsyncTask(HAAR_CASCADE_FILENAME, cancellationToken: cts.Token);

            if (fpsMonitor != null)
                fpsMonitor.consoleText = "";


            if (string.IsNullOrEmpty(cascade_filepath))
            {
                Debug.LogError(HAAR_CASCADE_FILENAME + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
            }
            else
            {
                cascade = new CascadeClassifier(cascade_filepath);
            }

            Run();
        }

        private void Run()
        {
            Texture2D imgTexture = Resources.Load("face") as Texture2D;

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            Utils.texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString());

            if (cascade == null)
            {
                Imgproc.putText(imgMat, "model file is not loaded.", new Point(5, imgMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(imgMat, "Please read console message.", new Point(5, imgMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Texture2D _texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);
                Utils.matToTexture2D(imgMat, _texture);
                gameObject.GetComponent<Renderer>().material.mainTexture = _texture;
                return;
            }


            Mat grayMat = new Mat();
            Imgproc.cvtColor(imgMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
            Imgproc.equalizeHist(grayMat, grayMat);


            MatOfRect faces = new MatOfRect();

            if (cascade != null)
                cascade.detectMultiScale(grayMat, faces, 1.1, 2, 2,
                    new Size(20, 20), new Size());

            OpenCVForUnity.CoreModule.Rect[] rects = faces.toArray();
            for (int i = 0; i < rects.Length; i++)
            {
                Debug.Log("detect faces " + rects[i]);

                Imgproc.rectangle(imgMat, new Point(rects[i].x, rects[i].y), new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 0, 0, 255), 2);
            }


            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);

            Utils.matToTexture2D(imgMat, texture);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
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
    }
}