using System.Collections.Generic;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.FaceModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Optimization;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
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
    /// Please copy to "Assets/StreamingAssets/OpenCVForUnityExamples/face/" folder.
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class FaceMarkExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// FACEMARK_CASCADE_FRONTALFACE_FILENAME
        /// </summary>
        protected static readonly string FACEMARK_CASCADE_FRONTALFACE_FILENAME = "OpenCVForUnityExamples/objdetect/haarcascade_frontalface_alt.xml";

        /// <summary>
        /// FACEMARK_CASCADE_FILENAME
        /// </summary>
        protected static readonly string FACEMARK_MODEL_FILENAME = "OpenCVForUnityExamples/face/lbfmodel.yaml";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
        /// <summary>
        /// The gray mat.
        /// </summary>
        private Mat _grayMat;

        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The cascade classifier.
        /// </summary>
        private CascadeClassifier _cascade;

        /// <summary>
        /// The faces.
        /// </summary>
        private MatOfRect _faces;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The image optimization helper.
        /// </summary>
        private ImageOptimizationHelper _imageOptimizationHelper;

        /// <summary>
        /// The facemark.
        /// </summary>
        private Facemark _facemark;

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

            _imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            string facemark_cascade_filepath = await OpenCVEnv.GetFilePathTaskAsync(FACEMARK_CASCADE_FRONTALFACE_FILENAME, cancellationToken: _cts.Token);
            string facemark_model_filepath = await OpenCVEnv.GetFilePathTaskAsync(FACEMARK_MODEL_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";


            if (string.IsNullOrEmpty(facemark_cascade_filepath))
            {
                Debug.LogError(FACEMARK_CASCADE_FRONTALFACE_FILENAME + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
            }
            else
            {
                // setup face detector
                _cascade = new CascadeClassifier(facemark_cascade_filepath);
            }

            if (string.IsNullOrEmpty(facemark_model_filepath))
            {
                Debug.LogError(FACEMARK_MODEL_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
            }
            else
            {
                // setup landmarks detector
                _facemark = Face.createFacemarkLBF();
                _facemark.loadModel(facemark_model_filepath);
            }

            _multiSource2MatHelper.Initialize();
        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame() && !_imageOptimizationHelper.IsCurrentFrameSkipped())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_facemark == null || _cascade == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", (5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", (5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                    OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
                    return;
                }

                // get a downscaled image for the detection process
                Mat downScaleRgbaMat = _imageOptimizationHelper.GetDownScaleMat((rgbaMat));

                Imgproc.cvtColor(downScaleRgbaMat, _grayMat, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.equalizeHist(_grayMat, _grayMat);

                // detect faces
                int minSize = (int)(Mathf.Max(_grayMat.width(), _grayMat.height()) * 0.1);
                _cascade.detectMultiScale(_grayMat, _faces, 1.1, 2, 0 | Objdetect.CASCADE_SCALE_IMAGE,
                    (minSize, minSize));

                if (_faces.total() > 0)
                {
                    // fit landmarks for each found face
                    List<MatOfPoint2f> landmarks = new List<MatOfPoint2f>();
                    _facemark.fit(_grayMat, _faces, landmarks);

                    // restore the original size of the detected faces
                    using (MatOfRect originalSizeFaces = _imageOptimizationHelper.RestoreOriginalSizeMatOfRect(_faces))
                    {
                        // draw the detected faces
                        Rect[] rects = originalSizeFaces.toArray();
                        for (int i = 0; i < rects.Length; i++)
                        {
                            //Debug.Log ("detect faces " + rects [i]);

                            Imgproc.rectangle(rgbaMat, (rects[i].x, rects[i].y), (rects[i].x + rects[i].width, rects[i].y + rects[i].height), (255, 0, 0, 255), 2);
                        }
                    }

                    for (int i = 0; i < landmarks.Count; i++)
                    {
                        // restore the original size of the detected landmarks
                        using (MatOfPoint2f originalSizeLandmarks = _imageOptimizationHelper.RestoreOriginalSizeMatOfPoint2f(landmarks[i]))
                        {
                            // draw the detected landmarks
                            List<Point> points = originalSizeLandmarks.toList();
                            DrawFaceLandmark(rgbaMat, points, new Scalar(0, 255, 0, 255), 2);

                            //Scalar circleColor = new Scalar(255, 0, 0, 255);
                            //foreach (Point p in points)
                            //    Imgproc.circle(rgbaMat, p, 2, circleColor, -1);
                        }
                    }

                    foreach (MatOfPoint2f landmark in landmarks)
                        landmark.Dispose();
                }

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper.Dispose();
            _imageOptimizationHelper.Dispose();

            _cascade?.Dispose();

            _facemark?.Dispose();

            _cts?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
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

            Mat downScaleMat = _imageOptimizationHelper.GetDownScaleMat((rgbaMat));
            _grayMat = new Mat(downScaleMat.rows(), downScaleMat.cols(), CvType.CV_8UC1);

            _faces = new MatOfRect();
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _grayMat?.Dispose();


            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;

            _faces?.Dispose();
        }

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
                Scalar textColor = new Scalar(255, 255, 255, 255);
                for (int i = 0; i < points.Count; ++i)
                    Imgproc.putText(imgMat, i.ToString(), points[i], Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, textColor, 1, Imgproc.LINE_AA, false);
            }
        }
    }
}
