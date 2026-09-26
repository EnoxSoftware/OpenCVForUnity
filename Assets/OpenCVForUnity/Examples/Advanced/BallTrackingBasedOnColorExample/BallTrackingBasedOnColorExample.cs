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
    /// Ball Tracking Based on Color Example
    /// Tracks a colored ball in input frames using HSV segmentation, morphology, and contour analysis.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - RGBA to HSV conversion and Core.inRange color masking
    /// - Morphological erode/dilate to remove noise from the mask
    /// - Contour detection with minEnclosingCircle and moments for centroid
    /// - Tap-to-calibrate HSV range from a screen point
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Scalar"/>, <see cref="Point"/>, <see cref="Size"/>, <see cref="Moments"/>
    /// - <see cref="Core"/>: inRange, sumElems, meanStdDev
    /// - <see cref="Imgproc"/>: cvtColor, GaussianBlur, getStructuringElement, erode, dilate, findContours, contourArea, minEnclosingCircle, moments, circle, line, drawContours
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class BallTrackingBasedOnColorExample : MonoBehaviour
    {
        // Constants
        private const int MAX_BUFFER_SIZE = 64;

        private const double MIN_RADIUS = 10.0;

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        [Header("Color Detection")]
        /// <summary>
        /// The lower HSV boundary for target ball detection.
        /// </summary>
        public Scalar BallColorLower = new Scalar(81, 61, 112);

        /// <summary>
        /// The upper HSV boundary for target ball detection.
        /// </summary>
        public Scalar BallColorUpper = new Scalar(112, 182, 255);

        [Header("UI")]
        /// <summary>
        /// The texture selector (point selection).
        /// </summary>
        public TextureSelector TexturePointSelector;

        [Header("Debug")]
        /// <summary>
        /// Toggle for debug mode to visualize mask overlay.
        /// </summary>
        public Toggle IsDebugModeToggle;

        // Private Fields
        private Texture2D _texture;
        private Mat _rgbMat;
        private Mat _hsvMat;
        private Mat _maskMat;
        private Queue<Point> _trackedPoints;
        private float _lastDetectedRadius = 0;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;
        private bool _shouldUpdateColorFromPoint = false;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Please touch the screen, and select the color of the ball.";
            }

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
            if (rgbaMat == null || _texture == null || _rgbMat == null)
            {
                return;
            }

            if (_shouldUpdateColorFromPoint)
            {
                var (gameObject, currentSelectionState, currentSelectionPoints) = TexturePointSelector.GetSelectionStatus();
                var p = TextureSelector.ConvertSelectionPointsToOpenCVPoint(currentSelectionPoints);
                UpdateColorFromPoint(rgbaMat, p);

                TexturePointSelector.ResetSelectionStatus();

                _shouldUpdateColorFromPoint = false;
            }

            // MultiSourceToMatHelper provides RGBA; convert to RGB before COLOR_RGB2HSV.
            Imgproc.cvtColor(rgbaMat, _rgbMat, Imgproc.COLOR_RGBA2RGB);

            // Convert to HSV color space (OpenCV H: 0-180, S/V: 0-255).
            Imgproc.cvtColor(_rgbMat, _hsvMat, Imgproc.COLOR_RGB2HSV);

            // Apply Gaussian blur to reduce noise
            Imgproc.GaussianBlur(_hsvMat, _hsvMat, new Size(11, 11), 0);

            // Create mask for target ball color
            Core.inRange(_hsvMat, BallColorLower, BallColorUpper, _maskMat);

            // Apply morphological operations to remove noise (erode then dilate = opening).
            Mat kernel = Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(5, 5));
            Imgproc.erode(_maskMat, _maskMat, kernel, new Point(-1, -1), 2);
            Imgproc.dilate(_maskMat, _maskMat, kernel, new Point(-1, -1), 2);

            // Find contours and track the ball
            Point ballCenter = DetectBall(_maskMat);
            if (ballCenter.x >= 0 && ballCenter.y >= 0)
            {
                // Add the center point to tracking queue
                _trackedPoints.Enqueue(ballCenter);
                if (_trackedPoints.Count > MAX_BUFFER_SIZE)
                {
                    _trackedPoints.Dequeue();
                }

                // Draw the ball
                DrawBall(_rgbMat, ballCenter);
            }
            else
            {
                // Add invalid point to maintain trail length
                _trackedPoints.Enqueue(new Point(-1, -1));
                if (_trackedPoints.Count > MAX_BUFFER_SIZE)
                {
                    _trackedPoints.Dequeue();
                }
            }

            // Draw the trail
            DrawTrail(_rgbMat);

            // Draw debug mask contours if debug mode is enabled
            if (IsDebugModeToggle != null && IsDebugModeToggle.isOn)
            {
                DrawMaskContours(_rgbMat, _maskMat);
            }

            kernel.Dispose();

            //Imgproc.putText (_rgbMat, "W:" + _rgbMat.width () + " H:" + _rgbMat.height () + " SO:" + Screen.orientation, new Point (5, _rgbMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

            Imgproc.cvtColor(_rgbMat, rgbaMat, Imgproc.COLOR_RGB2RGBA);

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

            _trackedPoints = new Queue<Point>();

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
            }

            SetColorRangeFromClickedColor(BallColorLower, BallColorUpper);

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

            _trackedPoints?.Clear();

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
            _rgbMat?.Dispose();
            _rgbMat = null;
            _hsvMat?.Dispose();
            _hsvMat = null;
            _maskMat?.Dispose();
            _maskMat = null;
        }

        private void CreateOrRecreateProcessingResources(Mat rgbaMat)
        {
            if (rgbaMat == null)
            {
                return;
            }

            DisposeFrameProcessingResources();

            _rgbMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
            _hsvMat = new Mat();
            _maskMat = new Mat();
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

        /// <summary>
        /// Updates color range from the selected point.
        /// </summary>
        /// <param name="img">The image mat.</param>
        /// <param name="touchPoint">The touch point.</param>
        private void UpdateColorFromPoint(Mat img, Point touchPoint)
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
            using (Mat touchedRegionRgb = new Mat())
            using (Mat touchedRegionHsv = new Mat())
            {
                Imgproc.cvtColor(touchedRegionRgba, touchedRegionRgb, Imgproc.COLOR_RGBA2RGB);
                Imgproc.cvtColor(touchedRegionRgb, touchedRegionHsv, Imgproc.COLOR_RGB2HSV);

                // Calculate average color of touched region
                Scalar averageHsv = Core.sumElems(touchedRegionHsv);
                int pointCount = touchedRect.width * touchedRect.height;
                for (int i = 0; i < averageHsv.val.Length; i++)
                {
                    averageHsv.val[i] /= pointCount;
                }

                // Get HSV color range with adaptive analysis
                var colorRange = GetHSVColorRangeAtPoint(touchedRegionHsv, averageHsv);

                if (colorRange.HasValue)
                {
                    // Set color range from analyzed bounds
                    SetColorRangeFromClickedColor(colorRange.Value.lower, colorRange.Value.upper);
                }
            }
        }

        /// <summary>
        /// Gets HSV color range at the specified point with adaptive analysis.
        /// </summary>
        /// <param name="hsvMat">The HSV mat to analyze.</param>
        /// <param name="primaryHsv">The primary HSV color.</param>
        /// <returns>Tuple of (lowerHSV, upperHSV) or null if invalid.</returns>
        private (Scalar lower, Scalar upper)? GetHSVColorRangeAtPoint(Mat hsvMat, Scalar primaryHsv)
        {
            if (hsvMat == null || hsvMat.empty())
            {
                return null;
            }

            double primaryH = primaryHsv.val[0];
            double primaryS = primaryHsv.val[1];
            double primaryV = primaryHsv.val[2];

            // Calculate standard deviation for tolerance adjustment
            MatOfDouble mean = new MatOfDouble();
            MatOfDouble stddev = new MatOfDouble();
            Core.meanStdDev(hsvMat, mean, stddev);

            // Get stddev values for tolerance adjustment
            double[] stddevArray = stddev.toArray();

            // Define base tolerance values considering HSV color space characteristics
            double baseHTolerance = 15.0;  // Hue: relatively stable, moderate tolerance
            double baseSTolerance = 60.0;  // Saturation: moderate variation due to lighting
            double baseVTolerance = 100.0; // Value: high variation due to lighting conditions

            // Adjust tolerance based on local color variation (but keep within reasonable bounds)
            double hTolerance = Math.Max(8.0, Math.Min(25.0, baseHTolerance + stddevArray[0] * 0.5));
            double sTolerance = Math.Max(30.0, Math.Min(80.0, baseSTolerance + stddevArray[1] * 0.3));
            double vTolerance = Math.Max(60.0, Math.Min(150.0, baseVTolerance + stddevArray[2] * 0.4));

            // Calculate HSV range centered on the clicked point's color
            double hLower = Math.Max(0, primaryH - hTolerance);
            double hUpper = Math.Min(179, primaryH + hTolerance);
            double sLower = Math.Max(0, primaryS - sTolerance);
            double sUpper = Math.Min(255, primaryS + sTolerance);
            double vLower = Math.Max(0, primaryV - vTolerance);
            double vUpper = Math.Min(255, primaryV + vTolerance);

            // Create HSV range scalars
            Scalar lowerHSV = new Scalar(hLower, sLower, vLower);
            Scalar upperHSV = new Scalar(hUpper, sUpper, vUpper);

            // Dispose resources
            mean.Dispose();
            stddev.Dispose();

            return (lowerHSV, upperHSV);
        }

        /// <summary>
        /// Sets color range from provided HSV bounds.
        /// </summary>
        /// <param name="lowerHSV">Lower HSV boundary.</param>
        /// <param name="upperHSV">Upper HSV boundary.</param>
        private void SetColorRangeFromClickedColor(Scalar lowerHSV, Scalar upperHSV)
        {
            BallColorLower = lowerHSV;
            BallColorUpper = upperHSV;

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("H", lowerHSV.val[0].ToString("F1") + " - " + upperHSV.val[0].ToString("F1"));
                _fpsMonitor.Add("S", lowerHSV.val[1].ToString("F1") + " - " + upperHSV.val[1].ToString("F1"));
                _fpsMonitor.Add("V", lowerHSV.val[2].ToString("F1") + " - " + upperHSV.val[2].ToString("F1"));
            }
        }

        /// <summary>
        /// Detects the ball in the given mask and returns its center point.
        /// </summary>
        /// <param name="mask">The mask to search for contours.</param>
        /// <returns>The center point of the detected ball, or (-1, -1) if no ball is found.</returns>
        private Point DetectBall(Mat mask)
        {
            List<MatOfPoint> contours = new List<MatOfPoint>();
            Mat hierarchy = new Mat();

            Imgproc.findContours(mask, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

            if (contours.Count > 0)
            {
                // Find the largest contour
                MatOfPoint largestContour = contours[0];
                double maxArea = Geometry.contourArea(largestContour);

                for (int i = 1; i < contours.Count; i++)
                {
                    double area = Geometry.contourArea(contours[i]);
                    if (area > maxArea)
                    {
                        maxArea = area;
                        largestContour = contours[i];
                    }
                }

                // Calculate the minimum enclosing circle (radius filter) and centroid via moments.
                Point[] points = largestContour.toArray();
                Point center = new Point();
                float[] radius = new float[1];
                Geometry.minEnclosingCircle(new MatOfPoint2f(points), center, radius);

                // Only proceed if the radius meets the minimum size
                if (radius[0] > MIN_RADIUS)
                {
                    // Calculate the centroid using moments
                    OpenCVForUnity.GeometryModule.Moments moments = Geometry.moments(largestContour);
                    if (moments.m00 != 0)
                    {
                        Point centroid = new Point(moments.m10 / moments.m00, moments.m01 / moments.m00);

                        // Store the detected radius for drawing
                        _lastDetectedRadius = radius[0];

                        // Dispose resources
                        hierarchy.Dispose();
                        foreach (var contour in contours)
                        {
                            contour.Dispose();
                        }

                        return centroid;
                    }
                }
            }

            // Dispose resources
            hierarchy.Dispose();
            foreach (var contour in contours)
            {
                contour.Dispose();
            }

            return new Point(-1, -1);
        }

        /// <summary>
        /// Finds the radius of the ball at the given center point.
        /// </summary>
        /// <param name="center">The center point of the ball.</param>
        /// <returns>The radius of the ball.</returns>
        private int FindBallRadius(Point center)
        {
            if (_maskMat == null || _maskMat.empty())
            {
                return 0;
            }

            int maxRadius = 100; // Maximum expected radius
            int minRadius = 5;   // Minimum expected radius
            int bestRadius = 0;

            // Check 8 directions around the center
            int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1 };
            int[] dy = { 0, 1, 1, 1, 0, -1, -1, -1 };

            for (int r = minRadius; r < maxRadius; r++)
            {
                bool allInMask = true;

                // Check all 8 directions at distance r
                for (int dir = 0; dir < 8; dir++)
                {
                    int testX = (int)(center.x + dx[dir] * r);
                    int testY = (int)(center.y + dy[dir] * r);

                    // Check bounds
                    if (testX >= 0 && testX < _maskMat.cols() &&
                        testY >= 0 && testY < _maskMat.rows())
                    {
                        double[] pixel = _maskMat.get(testY, testX);
                        if (pixel == null || pixel[0] < 128) // Not in mask (background)
                        {
                            allInMask = false;
                            break;
                        }
                    }
                    else
                    {
                        allInMask = false;
                        break;
                    }
                }

                if (allInMask)
                {
                    bestRadius = r;
                }
                else
                {
                    break; // Found the edge
                }
            }

            return bestRadius;
        }

        /// <summary>
        /// Draws the ball circle and center point on the frame.
        /// </summary>
        /// <param name="frame">The frame to draw on.</param>
        /// <param name="center">The center point of the ball.</param>
        private void DrawBall(Mat frame, Point center)
        {
            // Use the radius detected from contour analysis first
            int radius = (int)_lastDetectedRadius;

            // If no radius was detected, try to find it from the mask
            if (radius <= 0)
            {
                radius = FindBallRadius(center);
            }

            // Debug information
            //Debug.Log($"DrawBall: center=({center.x:F1}, {center.y:F1}), radius={radius} (from contour: {_lastDetectedRadius:F1})");

            if (radius > 0)
            {
                // Draw the circle around the ball
                Imgproc.circle(frame, center, radius, new Scalar(255, 255, 0), 2);

                // Draw the center point
                Imgproc.circle(frame, center, 5, new Scalar(255, 0, 0), -1);
            }
        }

        /// <summary>
        /// Draws the trail of tracked points on the frame.
        /// </summary>
        /// <param name="frame">The frame to draw on.</param>
        private void DrawTrail(Mat frame)
        {
            if (_trackedPoints == null || _trackedPoints.Count < 2)
            {
                return;
            }

            Point[] points = _trackedPoints.ToArray();

            for (int i = 1; i < points.Length; i++)
            {
                if (points[i - 1].x >= 0 && points[i - 1].y >= 0 &&
                    points[i].x >= 0 && points[i].y >= 0)
                {
                    // Calculate thickness based on distance from current point
                    int thickness = (int)(Mathf.Sqrt(MAX_BUFFER_SIZE / (float)(i + 1)) * 2.5f);
                    thickness = Mathf.Max(1, thickness);

                    // Draw the connecting line
                    Imgproc.line(frame, points[i - 1], points[i], new Scalar(255, 0, 0), thickness);
                }
            }
        }

        /// <summary>
        /// Draws mask contours on the RGB mat for debug visualization.
        /// </summary>
        /// <param name="rgbMat">The RGB mat to draw contours on.</param>
        /// <param name="maskMat">The mask mat to find contours from.</param>
        private void DrawMaskContours(Mat rgbMat, Mat maskMat)
        {
            if (rgbMat == null || maskMat == null || rgbMat.empty() || maskMat.empty())
            {
                return;
            }

            // Find contours in the mask
            List<MatOfPoint> contours = new List<MatOfPoint>();
            Mat hierarchy = new Mat();
            Imgproc.findContours(maskMat, contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

            // Draw contours with white color
            Scalar contourColor = new Scalar(255, 255, 255); // White color in BGR format
            int thickness = 2;

            foreach (var contour in contours)
            {
                // Only draw contours with sufficient area to avoid noise
                double area = Geometry.contourArea(contour);
                if (area > 100) // Minimum area threshold
                {
                    Imgproc.drawContours(rgbMat, new List<MatOfPoint> { contour }, -1, contourColor, thickness);
                }
            }

            // Dispose resources
            hierarchy.Dispose();
            foreach (var contour in contours)
            {
                contour.Dispose();
            }
        }
    }
}
