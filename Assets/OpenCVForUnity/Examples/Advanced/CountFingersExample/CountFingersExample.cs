using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Count Fingers Example
    /// The techniques used are color segmentation using HSV color space to find the hand contour, and convex hull and convex defect algorithms to count the number of fingers.
    /// Referring to https://www.youtube.com/watch?v=KuGpOxOcpds.
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class CountFingersExample : MonoBehaviour
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
        /// The number of fingers text.
        /// </summary>
        public UnityEngine.UI.Text NumberOfFingersText;

        /// <summary>
        /// The threashold slider.
        /// </summary>
        public Slider ThreasholdSlider;

        /// <summary>
        /// The texture selector (point selection).
        /// </summary>
        public TextureSelector TexturePointSelector;

        // Private Fields
        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The BLOB color hsv.
        /// </summary>
        private Scalar _blobColorHsv;

        ///// <summary>
        ///// The BLOB color rgba.
        ///// </summary>
        //Scalar blobColorRgba;

        /// <summary>
        /// The detector.
        /// </summary>
        private ColorBlobDetector _detector;

        /// <summary>
        /// The spectrum mat.
        /// </summary>
        private Mat _spectrumMat;

        /// <summary>
        /// Indicates whether is color selected.
        /// </summary>
        private bool _isColorSelected = false;

        /// <summary>
        /// The spectrum size.
        /// </summary>
        private Size _spectrumSize;

        /// <summary>
        /// The contour color.
        /// </summary>
        private Scalar _contourColor;

        /// <summary>
        /// The contour color white.
        /// </summary>
        private Scalar _contourColorWhite;

        /// <summary>
        /// The number of fingers.
        /// </summary>
        private int _numberOfFingers = 0;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The flag to request color update from selected point.
        /// </summary>
        private bool _shouldUpdateColorFromPoint = false;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;
            _multiSource2MatHelper.Initialize();
        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_shouldUpdateColorFromPoint)
                {
                    var (gameObject, currentSelectionState, currentSelectionPoints) = TexturePointSelector.GetSelectionStatus();
                    var p = TextureSelector.ConvertSelectionPointsToOpenCVPoint(currentSelectionPoints);
                    SelectHandColor(rgbaMat, p);

                    TexturePointSelector.ResetSelectionStatus();

                    _shouldUpdateColorFromPoint = false;
                }

                HandPoseEstimationProcess(rgbaMat);

                // Draw current selection overlay
                TexturePointSelector.DrawSelection(rgbaMat, true);

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
                _fpsMonitor.Toast("Touch the screen to specify the hand color.", 2000);
            }

            _detector = new ColorBlobDetector();
            _spectrumMat = new Mat();
            _blobColorHsv = new Scalar(255);
            _spectrumSize = new Size(200, 64);
            _contourColor = new Scalar(255, 0, 0, 255);
            _contourColorWhite = new Scalar(255, 255, 255, 255);

            // Hide UI elements initially
            NumberOfFingersText.gameObject.SetActive(false);
            ThreasholdSlider.gameObject.SetActive(false);

            // Reset TexturePointSelector state
            TexturePointSelector.ResetSelectionStatus();
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _spectrumMat?.Dispose(); _spectrumMat = null;
            _detector?.Dispose(); _detector = null;
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
        /// Handles the texture selection state changed event from TextureSelector.
        /// This should be wired in the Inspector to TextureSelector.OnTextureSelectionStateChanged.
        /// </summary>
        /// <param name="touchedObject">The GameObject that was touched.</param>
        /// <param name="touchState">The touch state.</param>
        /// <param name="texturePoints">The texture coordinates array (OpenCV format: top-left origin).</param>
        public void OnTextureSelectionStateChanged(GameObject touchedObject, TextureSelector.TextureSelectionState touchState, Vector2[] texturePoints)
        {
            switch (touchState)
            {
                case TextureSelector.TextureSelectionState.POINT_SELECTION_COMPLETED:
                    _shouldUpdateColorFromPoint = true;
                    break;
                case TextureSelector.TextureSelectionState.OUTSIDE_TEXTURE_SELECTED:
                    _isColorSelected = false;
                    NumberOfFingersText.gameObject.SetActive(false);
                    ThreasholdSlider.gameObject.SetActive(false);
                    break;
            }
        }

        // Private Methods
        private void HandPoseEstimationProcess(Mat rgbaMat)
        {
            //Imgproc.blur(mRgba, mRgba, new Size(5,5));
            Imgproc.GaussianBlur(rgbaMat, rgbaMat, new Size(3, 3), 1, 1);
            //Imgproc.medianBlur(mRgba, mRgba, 3);

            if (!_isColorSelected)
                return;

            List<MatOfPoint> contours = _detector.GetContours();
            _detector.Process(rgbaMat);

            //Debug.Log ("Contours count: " + contours.Count);

            if (contours.Count <= 0)
            {
                return;
            }

            RotatedRect rect = Imgproc.minAreaRect(new MatOfPoint2f(contours[0].toArray()));

            double boundWidth = rect.size.width;
            double boundHeight = rect.size.height;
            int boundPos = 0;

            for (int i = 1; i < contours.Count; i++)
            {
                rect = Imgproc.minAreaRect(new MatOfPoint2f(contours[i].toArray()));
                if (rect.size.width * rect.size.height > boundWidth * boundHeight)
                {
                    boundWidth = rect.size.width;
                    boundHeight = rect.size.height;
                    boundPos = i;
                }
            }

            MatOfPoint contour = contours[boundPos];

            OpenCVForUnity.CoreModule.Rect boundRect = Imgproc.boundingRect(new MatOfPoint(contour.toArray()));
            Imgproc.rectangle(rgbaMat, boundRect.tl(), boundRect.br(), _contourColorWhite, 2, 8, 0);

            //Debug.Log(
            //    " Row start [" +
            //        (int)boundRect.tl().y + "] row end [" +
            //        (int)boundRect.br().y + "] Col start [" +
            //        (int)boundRect.tl().x + "] Col end [" +
            //        (int)boundRect.br().x + "]");

            double a = boundRect.br().y - boundRect.tl().y;
            a = a * 0.7;
            a = boundRect.tl().y + a;

            //Debug.Log (" A [" + a + "] br y - tl y = [" + (boundRect.br ().y - boundRect.tl ().y) + "]");

            Imgproc.rectangle(rgbaMat, boundRect.tl(), new Point(boundRect.br().x, a), _contourColor, 2, 8, 0);

            MatOfPoint2f pointMat = new MatOfPoint2f();
            Imgproc.approxPolyDP(new MatOfPoint2f(contour.toArray()), pointMat, 3, true);
            contour = new MatOfPoint(pointMat.toArray());

            MatOfInt hull = new MatOfInt();
            MatOfInt4 convexDefect = new MatOfInt4();
            Imgproc.convexHull(new MatOfPoint(contour.toArray()), hull);

            if (hull.toArray().Length < 3)
                return;

            Imgproc.convexityDefects(new MatOfPoint(contour.toArray()), hull, convexDefect);

            List<MatOfPoint> hullPoints = new List<MatOfPoint>();
            List<Point> listPo = new List<Point>();
            for (int j = 0; j < hull.toList().Count; j++)
            {
                listPo.Add(contour.toList()[hull.toList()[j]]);
            }

            MatOfPoint e = new MatOfPoint();
            e.fromList(listPo);
            hullPoints.Add(e);

            List<Point> listPoDefect = new List<Point>();

            if (convexDefect.rows() > 0)
            {
                List<int> convexDefectList = convexDefect.toList();
                List<Point> contourList = contour.toList();
                for (int j = 0; j < convexDefectList.Count; j = j + 4)
                {
                    Point farPoint = contourList[convexDefectList[j + 2]];
                    int depth = convexDefectList[j + 3];
                    if (depth > ThreasholdSlider.value && farPoint.y < a)
                    {
                        listPoDefect.Add(contourList[convexDefectList[j + 2]]);
                    }
                    //Debug.Log ("convexDefectList [" + j + "] " + convexDefectList [j + 3]);
                }
            }

            //Debug.Log ("hull: " + hull.toList ());
            //if (convexDefect.rows () > 0) {
            //  Debug.Log ("defects: " + convexDefect.toList ());
            //}

            Imgproc.drawContours(rgbaMat, hullPoints, -1, _contourColor, 3);

            //int defectsTotal = (int)convexDefect.total();
            //Debug.Log ("Defect total " + defectsTotal);

            _numberOfFingers = listPoDefect.Count;
            if (_numberOfFingers > 5)
                _numberOfFingers = 5;

            //Debug.Log ("numberOfFingers " + numberOfFingers);

            //Imgproc.putText (rgbaMat, "" + numberOfFingers, new Point (rgbaMat.cols () / 2, rgbaMat.rows () / 2), Imgproc.FONT_HERSHEY_PLAIN, 4.0, new Scalar (255, 255, 255, 255), 6, Imgproc.LINE_AA, false);
            NumberOfFingersText.text = _numberOfFingers.ToString();

            foreach (Point p in listPoDefect)
            {
                Imgproc.circle(rgbaMat, p, 6, new Scalar(255, 0, 255, 255), -1);
            }
        }

        private void SelectHandColor(Mat img, Point touchPoint)
        {
            int cols = img.cols();
            int rows = img.rows();

            int x = (int)touchPoint.x;
            int y = (int)touchPoint.y;

            //Debug.Log ("Touch image coordinates: (" + x + ", " + y + ")");

            if ((x < 0) || (y < 0) || (x > cols) || (y > rows))
                return;

            OpenCVForUnity.CoreModule.Rect touchedRect = new OpenCVForUnity.CoreModule.Rect();

            touchedRect.x = (x > 5) ? x - 5 : 0;
            touchedRect.y = (y > 5) ? y - 5 : 0;

            touchedRect.width = (x + 5 < cols) ? x + 5 - touchedRect.x : cols - touchedRect.x;
            touchedRect.height = (y + 5 < rows) ? y + 5 - touchedRect.y : rows - touchedRect.y;

            using (Mat touchedRegionRgba = img.submat(touchedRect))
            using (Mat touchedRegionHsv = new Mat())
            {
                Imgproc.cvtColor(touchedRegionRgba, touchedRegionHsv, Imgproc.COLOR_RGB2HSV_FULL);

                // Calculate average color of touched region
                _blobColorHsv = Core.sumElems(touchedRegionHsv);
                int pointCount = touchedRect.width * touchedRect.height;
                for (int i = 0; i < _blobColorHsv.val.Length; i++)
                    _blobColorHsv.val[i] /= pointCount;

                //blobColorRgba = ConverScalarHsv2Rgba (blobColorHsv);
                //Debug.Log ("Touched rgba color: (" + mBlobColorRgba.val [0] + ", " + mBlobColorRgba.val [1] +
                //  ", " + mBlobColorRgba.val [2] + ", " + mBlobColorRgba.val [3] + ")");

                _detector.SetHsvColor(_blobColorHsv);

                Imgproc.resize(_detector.GetSpectrum(), _spectrumMat, _spectrumSize);

                _isColorSelected = true;

                // Show UI elements when color is selected
                NumberOfFingersText.gameObject.SetActive(true);
                ThreasholdSlider.gameObject.SetActive(true);
            }
        }
    }
}
