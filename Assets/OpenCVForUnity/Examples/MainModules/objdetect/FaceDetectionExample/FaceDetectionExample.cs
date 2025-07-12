using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Face Detection Example
    /// An example of detecting human face using the CascadeClassifier class.
    /// http://docs.opencv.org/3.2.0/db/d28/tutorial_cascade_classifier.html
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class FaceDetectionExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// LBP_CASCADE_FRONTALFACE_FILENAME
        /// </summary>
        protected static readonly string LBP_CASCADE_FRONTALFACE_FILENAME = "OpenCVForUnityExamples/objdetect/lbpcascade_frontalface.xml";

        /// <summary>
        /// HAAR_CASCADE_FRONTALFACE_FILENAME
        /// </summary>
        protected static readonly string HAAR_CASCADE_FRONTALFACE_FILENAME = "OpenCVForUnityExamples/objdetect/haarcascade_frontalface_alt.xml";

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
        /// The current cascade classifier.
        /// </summary>
        private CascadeClassifier _currentCascade;

        /// <summary>
        /// The lbp cascade classifier.
        /// </summary>
        private CascadeClassifier _lbpCascade;

        /// <summary>
        /// The haar cascade classifier.
        /// </summary>
        private CascadeClassifier _haarCascade;

        /// <summary>
        /// Ratio to image size to determine the minimum possible object size for detection.
        /// </summary>
        private double _minSizeRatio = 0.15;

        /// <summary>
        /// Ratio to image size to determine the maximum possible object size for detection.
        /// </summary>
        private double _maxSizeRatio = 0.85;

        /// <summary>
        /// The faces.
        /// </summary>
        private MatOfRect _faces;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

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

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            string lbp_cascade_filepath = await OpenCVEnv.GetFilePathTaskAsync(LBP_CASCADE_FRONTALFACE_FILENAME, cancellationToken: _cts.Token);
            string haar_cascade_filepath = await OpenCVEnv.GetFilePathTaskAsync(HAAR_CASCADE_FRONTALFACE_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            if (string.IsNullOrEmpty(lbp_cascade_filepath))
            {
                Debug.LogError(LBP_CASCADE_FRONTALFACE_FILENAME + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
            }
            else
            {
                _lbpCascade = new CascadeClassifier(lbp_cascade_filepath);
            }

            if (string.IsNullOrEmpty(haar_cascade_filepath))
            {
                Debug.LogError(HAAR_CASCADE_FRONTALFACE_FILENAME + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
            }
            else
            {
                _haarCascade = new CascadeClassifier(haar_cascade_filepath);
            }

            _currentCascade = _lbpCascade;

            _multiSource2MatHelper.Initialize();
        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                // Get the current frame mat
                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_currentCascade == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                    OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
                    return;
                }

                // Convert the mat to gray and equalize
                Imgproc.cvtColor(rgbaMat, _grayMat, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.equalizeHist(_grayMat, _grayMat);

                // Detect faces.
                int minSize = (int)(Mathf.Max(_grayMat.width(), _grayMat.height()) * _minSizeRatio);
                int maxSize = (int)(Mathf.Max(_grayMat.width(), _grayMat.height()) * _maxSizeRatio);
                _currentCascade.detectMultiScale(_grayMat, _faces, 1.1, 2, 0 | Objdetect.CASCADE_SCALE_IMAGE,
                    new Size(minSize, minSize), new Size(maxSize, maxSize));

                // Draw the detected faces
                OpenCVForUnity.CoreModule.Rect[] rects = _faces.toArray();
                for (int i = 0; i < rects.Length; i++)
                {
                    //Debug.Log ("detect faces " + rects [i]);

                    Imgproc.rectangle(rgbaMat, new Point(rects[i].x, rects[i].y), new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 0, 0, 255), 2);
                }

                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point(5, rgbaMat.rows () - 70), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(rgbaMat, "minSizeRatio:" + _minSizeRatio.ToString("F2") + " minSize:" + minSize, new Point(5, rgbaMat.rows() - 40), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(rgbaMat, "maxSizeRatio:" + _maxSizeRatio.ToString("F2") + " maxSize:" + maxSize, new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            _lbpCascade?.Dispose();

            _haarCascade?.Dispose();

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

            _grayMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);
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

        /// <summary>
        /// Raises the current cascade dropdown value changed event.
        /// </summary>
        /// <param name="result"></param>
        public void OnCurrentCascadeDropdownValueChanged(int result)
        {
            if (result == 0)
            {
                _currentCascade = _lbpCascade;
            }
            else if (result == 1)
            {
                _currentCascade = _haarCascade;
            }
        }

        /// <summary>
        /// Raises the min size ratio slider value changed event.
        /// </summary>
        /// <param name="result"></param>
        public void OnMinSizeRatioSliderValueChanged(float result)
        {
            _minSizeRatio = result;
        }

        /// <summary>
        /// Raises the max size ratio slider value changed event.
        /// </summary>
        /// <param name="result"></param>
        public void OnMaxSizeRatioSliderValueChanged(float result)
        {
            _maxSizeRatio = result;
        }
    }
}
