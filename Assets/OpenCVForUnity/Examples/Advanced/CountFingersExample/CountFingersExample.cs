using System;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.GeometryModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Interaction;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Count Fingers Example
    /// Estimates the number of raised fingers from a hand contour in input frames.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - HSV skin-color segmentation via <see cref="ColorBlobDetector"/>
    /// - Convex hull and convexity defects to count finger valleys
    /// - Tap-to-calibrate hand color before detection starts
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Scalar"/>, <see cref="Point"/>, <see cref="RotatedRect"/>, <see cref="Rect"/>
    /// - <see cref="MatOfPoint"/>, <see cref="MatOfPoint2f"/>, <see cref="MatOfInt"/>, <see cref="MatOfInt4"/>
    /// - <see cref="Core"/>: sumElems
    /// - <see cref="Imgproc"/>: GaussianBlur, minAreaRect, boundingRect, approxPolyDP, convexHull, convexityDefects, drawContours, circle, cvtColor
    /// - <see cref="ColorBlobDetector"/>, <see cref="MultiSourceToMatHelper"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://www.youtube.com/watch?v=KuGpOxOcpds
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
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
        private Texture2D _texture;
        private Scalar _blobColorHsv;

        //Scalar blobColorRgba;

        private ColorBlobDetector _detector;
        private Mat _spectrumMat;

        /// <summary>
        /// Indicates whether is color selected.
        /// </summary>
        private bool _isColorSelected = false;
        private Size _spectrumSize;
        private Scalar _contourColor;
        private Scalar _contourColorWhite;
        private int _numberOfFingers = 0;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private bool _shouldUpdateColorFromPoint = false;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            WireSourceToMatControlPanelHooks();

            _multiSourceToMatHelper.Initialize();
        }

        private void OnDestroy()
        {
            UnwireSourceToMatControlPanelHooks();
        }

        // Public Methods
        /// <summary>
        /// Raises the helper frame mat updated event.
        /// Updates the preview texture when a new frame is available during playback.
        /// </summary>
        public void OnSourceToMatHelperFrameMatUpdated()
        {
            if (!_multiSourceToMatHelper.IsPlaying)
            {
                return;
            }

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;

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

            OpenCVMatUnityUtils.MatToTexture2D(rgbaMat, _texture);
        }

        /// <summary>
        /// Raises the helper initialized event.
        /// Recreates the preview texture and starts playback on first initialization.
        /// Skips Play when re-initialization has already restored Playing or Paused.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized", this);

            RecreatePreviewTexture();
            CreateOrRecreateProcessingResources(_multiSourceToMatHelper.FrameMat);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Clear();
                UpdateFpsMonitorPlaybackState();
                _fpsMonitor.Add("HelperKind", _multiSourceToMatHelper.RequestedHelperKind.ToString());
                _fpsMonitor.Add("Width", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("Height", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("Rotate90Degree", _multiSourceToMatHelper.Rotate90Degree.ToString());
                _fpsMonitor.Add("FlipVertical", _multiSourceToMatHelper.FlipVertical.ToString());
                _fpsMonitor.Add("FlipHorizontal", _multiSourceToMatHelper.FlipHorizontal.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
                _fpsMonitor.Toast("Touch the screen to specify the hand color.", 2000);
            }

            _blobColorHsv = new Scalar(255);
            _spectrumSize = new Size(200, 64);
            _contourColor = new Scalar(255, 0, 0, 255);
            _contourColorWhite = new Scalar(255, 255, 255, 255);

            // Hide UI elements initially
            NumberOfFingersText.gameObject.SetActive(false);
            ThreasholdSlider.gameObject.SetActive(false);

            // Reset TexturePointSelector state
            TexturePointSelector.ResetSelectionStatus();

            if (!_multiSourceToMatHelper.IsPlaying && !_multiSourceToMatHelper.IsPaused)
            {
                _multiSourceToMatHelper.Play();
                UpdateFpsMonitorPlaybackState();
            }
        }

        /// <summary>
        /// Raises the helper frame mat layout changed event.
        /// Recreates the preview texture when rotation or output size changes.
        /// </summary>
        public void OnSourceToMatHelperFrameMatLayoutChanged()
        {
            Debug.Log("OnSourceToMatHelperFrameMatLayoutChanged", this);

            RecreatePreviewTexture();
            CreateOrRecreateProcessingResources(_multiSourceToMatHelper.FrameMat);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("Width", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("Height", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
            }
        }

        /// <summary>
        /// Raises the helper released event.
        /// </summary>
        public void OnSourceToMatHelperReleased()
        {
            Debug.Log("OnSourceToMatHelperReleased", this);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Clear();
            }

            DisposeFrameProcessingResources();
            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed", this);

            DisposeFrameProcessingResources();
            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(SourceToMatErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message, this);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        /// <summary>
        /// Raises the back button click event.
        /// Stops playback and disposes the helper before scene transition (required on WebGL).
        /// </summary>
        public async void OnBackButtonClick()
        {
            if (_multiSourceToMatHelper.IsPlaying || _multiSourceToMatHelper.IsPaused)
            {
                await _multiSourceToMatHelper.StopAsync();
            }

            await _multiSourceToMatHelper.DisposeAsync();

            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterPlay"/>.
        /// </summary>
        public void OnControlPanelAfterPlay()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterPause"/>.
        /// </summary>
        public void OnControlPanelAfterPause()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterStop"/>.
        /// </summary>
        public void OnControlPanelAfterStop()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnRotate90Changed"/>.
        /// </summary>
        /// <param name="isOn">New Rotate90Degree value applied by the panel.</param>
        public void OnControlPanelRotate90Changed(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("Rotate90Degree", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnFlipVerticalChanged"/>.
        /// </summary>
        /// <param name="isOn">New FlipVertical value applied by the panel.</param>
        public void OnControlPanelFlipVerticalChanged(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("FlipVertical", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnFlipHorizontalChanged"/>.
        /// </summary>
        /// <param name="isOn">New FlipHorizontal value applied by the panel.</param>
        public void OnControlPanelFlipHorizontalChanged(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("FlipHorizontal", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnHelperKindChanged"/>.
        /// </summary>
        /// <param name="kindIndex">Dropdown index matching <see cref="MultiSourceHelperKind"/>.</param>
        public void OnControlPanelHelperKindChanged(int kindIndex)
        {
            if (_fpsMonitor == null || !Enum.IsDefined(typeof(MultiSourceHelperKind), kindIndex))
            {
                return;
            }

            _fpsMonitor.Add("HelperKind", ((MultiSourceHelperKind)kindIndex).ToString());
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
                case TextureSelector.TextureSelectionState.POINT_SELECTION_CANCELLED:
                    _isColorSelected = false;
                    NumberOfFingersText.gameObject.SetActive(false);
                    ThreasholdSlider.gameObject.SetActive(false);
                    break;
            }
        }

        // Private Methods
        private void RecreatePreviewTexture()
        {
            Mat frameMat = _multiSourceToMatHelper.FrameMat;
            if (frameMat == null)
            {
                return;
            }

            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
                _texture = null;
            }

            bool isRgb = _multiSourceToMatHelper.OutputColorFormat == SourceToMatColorFormat.RGB;
            _texture = new Texture2D(frameMat.cols(), frameMat.rows(), isRgb ? TextureFormat.RGB24 : TextureFormat.RGBA32, false);
            OpenCVMatUnityUtils.MatToTexture2D(frameMat, _texture);

            if (ResultPreview != null)
            {
                ResultPreview.texture = _texture;
                AspectRatioFitter aspectRatioFitter = ResultPreview.GetComponent<AspectRatioFitter>();
                if (aspectRatioFitter != null)
                {
                    aspectRatioFitter.aspectRatio = (float)_texture.width / _texture.height;
                }
            }
        }

        private void DisposeFrameProcessingResources()
        {
            _spectrumMat?.Dispose();
            _spectrumMat = null;
            _detector?.Dispose();
            _detector = null;
        }

        private void CreateOrRecreateProcessingResources(Mat rgbaMat)
        {
            DisposeFrameProcessingResources();

            _detector = new ColorBlobDetector();
            _spectrumMat = new Mat();
        }

        private void CleanupPreviewResources()
        {
            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
                _texture = null;
            }

            UpdateFpsMonitorPlaybackState();
        }

        private void UpdateFpsMonitorPlaybackState()
        {
            if (_fpsMonitor == null || _multiSourceToMatHelper == null)
            {
                return;
            }

            _fpsMonitor.Add("PlaybackState", GetPlaybackStateText());
        }

        private string GetPlaybackStateText()
        {
            if (!_multiSourceToMatHelper.IsInitialized)
            {
                return "Uninitialized";
            }

            if (_multiSourceToMatHelper.IsPlaying)
            {
                return "Playing";
            }

            if (_multiSourceToMatHelper.IsPaused)
            {
                return "Paused";
            }

            return "Ready";
        }

        private void WireSourceToMatControlPanelHooks()
        {
            _controlPanel = GetComponent<SourceToMatControlPanel>();
            if (_controlPanel == null)
            {
                return;
            }

            _controlPanel.OnAfterPlay.AddListener(OnControlPanelAfterPlay);
            _controlPanel.OnAfterPause.AddListener(OnControlPanelAfterPause);
            _controlPanel.OnAfterStop.AddListener(OnControlPanelAfterStop);
            _controlPanel.OnRotate90Changed.AddListener(OnControlPanelRotate90Changed);
            _controlPanel.OnFlipVerticalChanged.AddListener(OnControlPanelFlipVerticalChanged);
            _controlPanel.OnFlipHorizontalChanged.AddListener(OnControlPanelFlipHorizontalChanged);
            _controlPanel.OnHelperKindChanged.AddListener(OnControlPanelHelperKindChanged);
        }

        private void UnwireSourceToMatControlPanelHooks()
        {
            if (_controlPanel == null)
            {
                return;
            }

            _controlPanel.OnAfterPlay.RemoveListener(OnControlPanelAfterPlay);
            _controlPanel.OnAfterPause.RemoveListener(OnControlPanelAfterPause);
            _controlPanel.OnAfterStop.RemoveListener(OnControlPanelAfterStop);
            _controlPanel.OnRotate90Changed.RemoveListener(OnControlPanelRotate90Changed);
            _controlPanel.OnFlipVerticalChanged.RemoveListener(OnControlPanelFlipVerticalChanged);
            _controlPanel.OnFlipHorizontalChanged.RemoveListener(OnControlPanelFlipHorizontalChanged);
            _controlPanel.OnHelperKindChanged.RemoveListener(OnControlPanelHelperKindChanged);
            _controlPanel = null;
        }

        private void HandPoseEstimationProcess(Mat rgbaMat)
        {
            //Imgproc.blur(mRgba, mRgba, new Size(5,5));
            Imgproc.GaussianBlur(rgbaMat, rgbaMat, new Size(3, 3), 1, 1);
            //Imgproc.medianBlur(mRgba, mRgba, 3);

            if (!_isColorSelected)
            {
                return;
            }

            List<MatOfPoint> contours = _detector.GetContours();
            // Process() updates contours for the next frame; GetContours() returns the previous result.
            _detector.Process(rgbaMat);

            //Debug.Log ("Contours count: " + contours.Count);

            if (contours.Count <= 0)
            {
                return;
            }

            RotatedRect rect = Geometry.minAreaRect(new MatOfPoint2f(contours[0].toArray()));

            double boundWidth = rect.size.width;
            double boundHeight = rect.size.height;
            int boundPos = 0;

            for (int i = 1; i < contours.Count; i++)
            {
                rect = Geometry.minAreaRect(new MatOfPoint2f(contours[i].toArray()));
                if (rect.size.width * rect.size.height > boundWidth * boundHeight)
                {
                    boundWidth = rect.size.width;
                    boundHeight = rect.size.height;
                    boundPos = i;
                }
            }

            MatOfPoint contour = contours[boundPos];

            OpenCVForUnity.CoreModule.Rect boundRect = Geometry.boundingRect(new MatOfPoint(contour.toArray()));
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

            // Ignore convex defects below this line (wrist / palm region).
            Imgproc.rectangle(rgbaMat, boundRect.tl(), new Point(boundRect.br().x, a), _contourColor, 2, 8, 0);

            MatOfPoint2f pointMat = new MatOfPoint2f();
            Geometry.approxPolyDP(new MatOfPoint2f(contour.toArray()), pointMat, 3, true);
            contour = new MatOfPoint(pointMat.toArray());

            MatOfInt hull = new MatOfInt();
            MatOfInt4 convexDefect = new MatOfInt4();
            Geometry.convexHull(new MatOfPoint(contour.toArray()), hull);

            if (hull.toArray().Length < 3)
            {
                return;
            }

            Geometry.convexityDefects(new MatOfPoint(contour.toArray()), hull, convexDefect);

            Point[] contourPts = contour.toArray();
            int[] hullIndices = hull.toArray();
            Point[] hullPts = new Point[hullIndices.Length];
            for (int j = 0; j < hullIndices.Length; j++)
            {
                hullPts[j] = contourPts[hullIndices[j]];
            }

            List<MatOfPoint> hullPoints = new List<MatOfPoint>();
            hullPoints.Add(new MatOfPoint(hullPts));

            List<Point> listPoDefect = new List<Point>();

            if (!convexDefect.empty())
            {
                List<int> convexDefectList = convexDefect.toList();
                List<Point> contourList = contour.toList();
                for (int j = 0; j < convexDefectList.Count; j = j + 4)
                {
                    Point farPoint = contourList[convexDefectList[j + 2]];
                    int depth = convexDefectList[j + 3];
                    // depth exceeds slider threshold and point is above wrist line => count as a finger gap.
                    if (depth > ThreasholdSlider.value && farPoint.y < a)
                    {
                        listPoDefect.Add(contourList[convexDefectList[j + 2]]);
                    }
                    //Debug.Log ("convexDefectList [" + j + "] " + convexDefectList [j + 3]);
                }
            }

            //Debug.Log ("hull: " + hull.toList ());
            //if (convexDefect.empty()) {
            //  Debug.Log ("defects: " + convexDefect.toList ());
            //}

            Imgproc.drawContours(rgbaMat, hullPoints, -1, _contourColor, 3);

            //int defectsTotal = (int)convexDefect.total();
            //Debug.Log ("Defect total " + defectsTotal);

            _numberOfFingers = listPoDefect.Count;
            if (_numberOfFingers > 5)
            {
                _numberOfFingers = 5;
            }

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
            {
                return;
            }

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
                {
                    _blobColorHsv.val[i] /= pointCount;
                }

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
