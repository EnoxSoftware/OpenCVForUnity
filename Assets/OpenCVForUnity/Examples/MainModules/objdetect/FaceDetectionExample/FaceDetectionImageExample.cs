using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Face Detection Image Example
    /// An example of human face detection using the CascadeClassifier class.
    /// https://docs.opencv.org/4.x/db/d28/tutorial_cascade_classifier.html
    /// https://github.com/opencv/opencv/tree/4.x/data/haarcascades
    /// </summary>
    public class FaceDetectionImageExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// HAAR_CASCADE_FRONTALFACE_FILENAME
        /// </summary>
        protected static readonly string HAAR_CASCADE_FRONTALFACE_FILENAME = "OpenCVForUnityExamples/objdetect/haarcascade_frontalface_alt.xml";

        /// <summary>
        /// HAAR_CASCADE_EYE_FILENAME
        /// </summary>
        protected static readonly string HAAR_CASCADE_EYE_FILENAME = "OpenCVForUnityExamples/objdetect/haarcascade_eye_tree_eyeglasses.xml";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
        /// <summary>
        /// The frontalface cascade classifier.
        /// </summary>
        private CascadeClassifier _cascadeFrontalface;

        /// <summary>
        /// The eye cascade classifier.
        /// </summary>
        private CascadeClassifier _cascadeEye;

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

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            string cascade_frontalface_filepath = await OpenCVEnv.GetFilePathTaskAsync(HAAR_CASCADE_FRONTALFACE_FILENAME, cancellationToken: _cts.Token);
            string cascade_eye_filepath = await OpenCVEnv.GetFilePathTaskAsync(HAAR_CASCADE_EYE_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            if (string.IsNullOrEmpty(cascade_frontalface_filepath))
            {
                Debug.LogError(HAAR_CASCADE_FRONTALFACE_FILENAME + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
            }
            else
            {
                // Create a cascade classifier from the file path.
                _cascadeFrontalface = new CascadeClassifier(cascade_frontalface_filepath);
            }

            if (string.IsNullOrEmpty(cascade_eye_filepath))
            {
                Debug.LogError(HAAR_CASCADE_EYE_FILENAME + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
            }
            else
            {
                // Create a cascade classifier from the file path.
                _cascadeEye = new CascadeClassifier(cascade_eye_filepath);
            }

            Run();
        }

        private void Update()
        {

        }

        private void OnDestroy()
        {
            _cascadeFrontalface?.Dispose();

            _cascadeEye?.Dispose();

            _cts?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        // Private Methods
        private void Run()
        {
            Texture2D imgTexture = Resources.Load("face") as Texture2D;
            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            // Convert the Texture2D to Mat.
            OpenCVMatUtils.Texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString());

            if (_cascadeFrontalface == null || _cascadeEye == null)
            {
                Imgproc.putText(imgMat, "model file is not loaded.", new Point(5, imgMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(imgMat, "Please read console message.", new Point(5, imgMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Texture2D _texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);
                OpenCVMatUtils.MatToTexture2D(imgMat, _texture);
                gameObject.GetComponent<Renderer>().material.mainTexture = _texture;
                return;
            }

            // Convert the Mat to gray scale and equalize the histogram.
            Mat grayMat = new Mat();
            Imgproc.cvtColor(imgMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
            Imgproc.equalizeHist(grayMat, grayMat);

            // Detect faces.
            MatOfRect faces = new MatOfRect();
            _cascadeFrontalface.detectMultiScale(
                grayMat, // Matrix of the type CV_8U containing an image where objects are detected.
                faces,
                1.1, // Parameter specifying how much the image size is reduced at each image scale.
                2, // Parameter specifying how many neighbors each candidate rectangle should have to retain it.
                0 | Objdetect.CASCADE_SCALE_IMAGE, // 	Parameter with the same meaning for an old cascade as in the function cvHaarDetectObjects. It is not used for a new cascade.
                new Size(50, 50)); // Minimum possible object size. Objects smaller than that are ignored.

            // Draw a rectangle around the faces.
            Rect[] facesArray = faces.toArray();
            for (int i = 0; i < facesArray.Length; i++)
            {
                Debug.Log("detect faces " + facesArray[i]);
                Imgproc.rectangle(imgMat, new Point(facesArray[i].x, facesArray[i].y), new Point(facesArray[i].x + facesArray[i].width, facesArray[i].y + facesArray[i].height), new Scalar(255, 0, 0, 255), 2);

                // Detect eyes.
                Mat faceROI = new Mat(grayMat, new Rect(facesArray[i].x, facesArray[i].y, facesArray[i].width, facesArray[i].height));
                MatOfRect eyes = new MatOfRect();
                int minSize = (int)(Mathf.Max(faceROI.width(), faceROI.height()) * 0.1);
                int maxSize = (int)(Mathf.Max(faceROI.width(), faceROI.height()) * 0.3);
                _cascadeEye.detectMultiScale(
                    faceROI,
                    eyes,
                    1.1,
                    2,
                    0 | Objdetect.CASCADE_SCALE_IMAGE,
                    new Size(minSize, minSize),
                    new Size(maxSize, maxSize));

                // Draw a rectangle around the eyes.
                Rect[] eyesArray = eyes.toArray();
                for (int j = 0; j < eyesArray.Length; j++)
                {
                    Debug.Log("detect eyes " + eyesArray[j]);
                    Imgproc.rectangle(imgMat, new Point(facesArray[i].x + eyesArray[j].x, facesArray[i].y + eyesArray[j].y), new Point(facesArray[i].x + eyesArray[j].x + eyesArray[j].width, facesArray[i].y + eyesArray[j].y + eyesArray[j].height), new Scalar(0, 255, 0, 255), 2);
                }

                eyes.Dispose();
            }
            faces.Dispose();

            // Convert the Mat back to Texture2D.
            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(imgMat, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;

            imgMat.Dispose();
            grayMat.Dispose();
        }
    }
}
