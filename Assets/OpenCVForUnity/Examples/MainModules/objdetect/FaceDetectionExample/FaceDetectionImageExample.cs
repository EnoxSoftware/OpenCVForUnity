using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using OpenCVForUnity.XobjdetectModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Face Detection Image Example
    /// Detects faces and eyes in a static image using two cascade classifiers.
    ///
    /// Demonstrates:
    /// - Loading Haar cascade XML files for frontal face and eye detection
    /// - Texture2D-to-Mat conversion and grayscale preprocessing
    /// - Nested detection: eyes are searched only inside each face ROI
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="CascadeClassifier"/>, <see cref="MatOfRect"/>, <see cref="Objdetect"/>
    /// - <see cref="Imgproc"/>: cvtColor, equalizeHist, rectangle
    /// - <see cref="OpenCVMatUnityUtils"/>: Texture2DToMat, MatToTexture2D
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://docs.opencv.org/4.x/db/d28/tutorial_cascade_classifier.html
    /// https://github.com/opencv/opencv/tree/4.x/data/haarcascades
    /// </para>
    /// </remarks>
    public class FaceDetectionImageExample : MonoBehaviour
    {
        // Constants
        private static readonly string HAAR_CASCADE_FRONTALFACE_FILEPATH = "OpenCVForUnityExamples/objdetect/haarcascade_frontalface_alt.xml";

        private static readonly string HAAR_CASCADE_EYE_FILEPATH = "OpenCVForUnityExamples/objdetect/haarcascade_eye_tree_eyeglasses.xml";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
        private CascadeClassifier _cascadeFrontalface;

        private CascadeClassifier _cascadeEye;

        private FpsMonitor _fpsMonitor;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            string cascade_frontalface_filepath = await OpenCVForUnityEnv.GetFilePathAsync(HAAR_CASCADE_FRONTALFACE_FILEPATH, cancellationToken: _cts.Token);
            string cascade_eye_filepath = await OpenCVForUnityEnv.GetFilePathAsync(HAAR_CASCADE_EYE_FILEPATH, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            if (string.IsNullOrEmpty(cascade_frontalface_filepath))
            {
                Debug.LogError(HAAR_CASCADE_FRONTALFACE_FILEPATH + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("cascade classifier file is not loaded.\nPlease read console message.", 20000);
                }
            }
            else
            {
                // Create a cascade classifier from the file path.
                _cascadeFrontalface = new CascadeClassifier(cascade_frontalface_filepath);
            }

            if (string.IsNullOrEmpty(cascade_eye_filepath))
            {
                Debug.LogError(HAAR_CASCADE_EYE_FILEPATH + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("cascade classifier file is not loaded.\nPlease read console message.", 20000);
                }
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
            _cts?.Cancel();

            _cascadeFrontalface?.Dispose();
            _cascadeFrontalface = null;

            _cascadeEye?.Dispose();
            _cascadeEye = null;

            _cts?.Dispose();
            _cts = null;
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
            OpenCVMatUnityUtils.Texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString(), this);

            if (_cascadeFrontalface == null || _cascadeEye == null)
            {
                Texture2D texture2d = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);
                OpenCVMatUnityUtils.MatToTexture2D(imgMat, texture2d);
                gameObject.GetComponent<Renderer>().material.mainTexture = texture2d;
                return;
            }

            // Cascade detection runs on a single-channel image; equalizeHist boosts local contrast.
            Mat grayMat = new Mat();
            Imgproc.cvtColor(imgMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
            Imgproc.equalizeHist(grayMat, grayMat);

            // Full-frame face search; eye detection runs later inside each face ROI.
            MatOfRect faces = new MatOfRect();
            _cascadeFrontalface.detectMultiScale(
                grayMat, // Matrix of the type CV_8U containing an image where objects are detected.
                faces,
                1.1, // Parameter specifying how much the image size is reduced at each image scale.
                2, // Parameter specifying how many neighbors each candidate rectangle should have to retain it.
                0 | Xobjdetect.CASCADE_SCALE_IMAGE, // 	Parameter with the same meaning for an old cascade as in the function cvHaarDetectObjects. It is not used for a new cascade.
                new Size(50, 50)); // Minimum possible object size. Objects smaller than that are ignored.

            // Draw a rectangle around the faces.
            Rect[] facesArray = faces.toArray();
            for (int i = 0; i < facesArray.Length; i++)
            {
                Debug.Log("detect faces " + facesArray[i], this);
                Imgproc.rectangle(imgMat, new Point(facesArray[i].x, facesArray[i].y), new Point(facesArray[i].x + facesArray[i].width, facesArray[i].y + facesArray[i].height), new Scalar(255, 0, 0, 255), 2);

                // Mat(grayMat, rect) is a submatrix view — no pixel copy, coordinates stay relative to the ROI.
                Mat faceROI = new Mat(grayMat, new Rect(facesArray[i].x, facesArray[i].y, facesArray[i].width, facesArray[i].height));
                MatOfRect eyes = new MatOfRect();
                // Eye size limits are relative to the face ROI, not the full image.
                int minSize = (int)(Mathf.Max(faceROI.width(), faceROI.height()) * 0.1);
                int maxSize = (int)(Mathf.Max(faceROI.width(), faceROI.height()) * 0.3);
                _cascadeEye.detectMultiScale(
                    faceROI,
                    eyes,
                    1.1,
                    2,
                    0 | Xobjdetect.CASCADE_SCALE_IMAGE,
                    new Size(minSize, minSize),
                    new Size(maxSize, maxSize));

                // Draw a rectangle around the eyes.
                Rect[] eyesArray = eyes.toArray();
                for (int j = 0; j < eyesArray.Length; j++)
                {
                    Debug.Log("detect eyes " + eyesArray[j], this);
                    Imgproc.rectangle(imgMat, new Point(facesArray[i].x + eyesArray[j].x, facesArray[i].y + eyesArray[j].y), new Point(facesArray[i].x + eyesArray[j].x + eyesArray[j].width, facesArray[i].y + eyesArray[j].y + eyesArray[j].height), new Scalar(0, 255, 0, 255), 2);
                }

                eyes.Dispose();
            }
            faces.Dispose();

            // Convert the Mat back to Texture2D.
            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUnityUtils.MatToTexture2D(imgMat, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;

            imgMat.Dispose();
            grayMat.Dispose();
        }
    }
}
