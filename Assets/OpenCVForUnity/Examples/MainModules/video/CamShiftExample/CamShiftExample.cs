using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.VideoModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// CamShift Example
    /// An example of object tracking using the Video.Camshift function.
    /// Referring to http://www.computervisiononline.com/blog/tutorial-using-camshift-track-objects-video.
    /// http://docs.opencv.org/3.2.0/db/df8/tutorial_py_meanshift.html
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class CamShiftExample : MonoBehaviour
    {
        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        [Header("UI")]

        /// <summary>
        /// The texture touched point getter component.
        /// </summary>
        public TextureSelector TextureRectangleSelector;

        // Private Fields
        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The roi rect.
        /// </summary>
        private OpenCVForUnity.CoreModule.Rect _roiRect;

        /// <summary>
        /// The hsv mat.
        /// </summary>
        private Mat _hsvMat;

        /// <summary>
        /// The roi hist mat.
        /// </summary>
        private Mat _roiHistMat;

        /// <summary>
        /// The termination.
        /// </summary>
        private TermCriteria _termination;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The flag for requesting the start of the CamShift Method.
        /// </summary>
        private bool _shouldStartCamShift = false;

        /// <summary>
        /// The flag indicating that CamShift tracking has started.
        /// </summary>
        private bool _isCamShiftStarted = false;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _termination = new TermCriteria(TermCriteria.EPS | TermCriteria.COUNT, 10, 1);

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;
            _multiSource2MatHelper.Initialize();
        }


        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                Imgproc.cvtColor(rgbaMat, _hsvMat, Imgproc.COLOR_RGBA2RGB);
                Imgproc.cvtColor(_hsvMat, _hsvMat, Imgproc.COLOR_RGB2HSV);

                if (_shouldStartCamShift)
                {

                    // Get rectangle selection points from TextureRectangleSelector
                    var (gameObject, currentSelectionState, currentSelectionPoints) = TextureRectangleSelector.GetSelectionStatus();
                    // Convert rectangle points to OpenCV Rect using TextureSelector utility method
                    // Note: currentSelectionPoints is guaranteed to have 2 elements (start and end points) when RECTANGLE_SELECTION_COMPLETED
                    _roiRect = TextureSelector.ConvertSelectionPointsToOpenCVRect(currentSelectionPoints);

                    _roiHistMat?.Dispose(); _roiHistMat = null;
                    _roiHistMat = new Mat();

                    using (Mat roiHSVMat = new Mat(_hsvMat, _roiRect))
                    using (Mat maskMat = new Mat())
                    {
                        Imgproc.calcHist(new List<Mat>(new Mat[] { roiHSVMat }), new MatOfInt(0), maskMat, _roiHistMat, new MatOfInt(16), new MatOfFloat(0, 180));
                        Core.normalize(_roiHistMat, _roiHistMat, 0, 255, Core.NORM_MINMAX);

                        // Set CamShift started flag
                        _isCamShiftStarted = true;

                        //Debug.Log ("_roiHistMat " + _roiHistMat.ToString ());
                    }

                    _shouldStartCamShift = false;
                }

                if (_isCamShiftStarted)
                {
                    // Create points array for CamShift tracking
                    Point[] points = new Point[4];

                    using (Mat backProj = new Mat())
                    {
                        Imgproc.calcBackProject(new List<Mat>(new Mat[] { _hsvMat }), new MatOfInt(0), _roiHistMat, backProj, new MatOfFloat(0, 180), 1.0);

                        RotatedRect r = Video.CamShift(backProj, _roiRect, _termination);
                        r.points(points);
                    }

                    // Draw tracking rectangle and lines after CamShift processing
                    for (int i = 0; i < 4; i++)
                    {
                        Imgproc.line(rgbaMat, points[i], points[(i + 1) % 4], new Scalar(255, 0, 0, 255), 2);
                    }

                    Imgproc.rectangle(rgbaMat, _roiRect.tl(), _roiRect.br(), new Scalar(0, 255, 0, 255), 2);
                }


                // Draw current selection state on the Mat
                TextureRectangleSelector.DrawSelection(rgbaMat, true);

                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();
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

                _fpsMonitor.ConsoleText = "Please touch the screen to select a rectangle for tracking.";
            }

            _hsvMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);

            _isCamShiftStarted = false;
            _shouldStartCamShift = false;

            // Reset TextureRectangleSelector state
            TextureRectangleSelector.ResetSelectionStatus();
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _hsvMat?.Dispose();
            _roiHistMat?.Dispose();

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
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
        /// Handles the texture selection state changed event.
        /// This method should be connected to the TextureSelector's OnTextureSelectionStateChanged event in the Inspector.
        /// </summary>
        /// <param name="touchedObject">The GameObject that was touched.</param>
        /// <param name="touchState">The touch state.</param>
        /// <param name="texturePoints">The texture coordinates array (OpenCV format: top-left origin).</param>
        public void OnTextureSelectionStateChanged(GameObject touchedObject, TextureSelector.TextureSelectionState touchState, Vector2[] texturePoints)
        {
            if (_isCamShiftStarted)
            {
                switch (touchState)
                {
                    case TextureSelector.TextureSelectionState.RECTANGLE_SELECTION_STARTED:
                    case TextureSelector.TextureSelectionState.OUTSIDE_TEXTURE_SELECTED:
                        // Reset CamShift when new selection starts or when touching outside texture
                        _isCamShiftStarted = false;
                        Debug.Log("Resetting CamShift due to new selection or outside touch.");
                        break;
                }
            }
            else
            {
                switch (touchState)
                {
                    case TextureSelector.TextureSelectionState.RECTANGLE_SELECTION_COMPLETED:
                        // Start CamShift when rectangle selection is completed
                        _shouldStartCamShift = true;
                        Debug.Log("Rectangle selection completed, starting CamShift tracking.");
                        break;
                }
            }
        }
    }
}
