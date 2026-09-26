using System;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.GeometryModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Document Scanner Example
    /// Detects a quadrilateral document in input frames and applies perspective correction.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Edge detection on the Y (luminance) channel of YUV color space
    /// - Finding a 4-point contour and ordering corner points for warpPerspective
    /// - Split preview: original with overlay and corrected document output
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="MatOfPoint"/>, <see cref="MatOfPoint2f"/>, <see cref="Scalar"/>, <see cref="Point"/>, <see cref="Size"/>
    /// - <see cref="Core"/>: extractChannel, add, subtract, minMaxLoc
    /// - <see cref="Imgproc"/>: cvtColor, GaussianBlur, Canny, findContours, convexHull, arcLength, approxPolyDP, contourArea, drawContours, putText, getPerspectiveTransform, warpPerspective
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class DocumentScannerExample : MonoBehaviour
    {
        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        /// <summary>
        /// Determines if debug mode.
        /// </summary>
        public bool IsDebugMode = false;

        /// <summary>
        /// The debug mode toggle.
        /// </summary>
        public Toggle IsDebugModeToggle;

        // Private Fields
        private Mat _yuvMat;
        private Mat _yMat;
        private Mat _displayMat;
        private Mat _inputDisplayAreaMat;
        private Mat _outputDisplayAreaMat;
        private Scalar _contourColor;
        private Scalar _debugContourColor;
        private Scalar _debugCornerNumberColor;
        private Texture2D _texture;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            WireSourceToMatControlPanelHooks();

            IsDebugModeToggle.isOn = IsDebugMode;

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

            // change the color space to YUV.
            Imgproc.cvtColor(rgbaMat, _yuvMat, Imgproc.COLOR_RGBA2RGB);
            Imgproc.cvtColor(_yuvMat, _yuvMat, Imgproc.COLOR_RGB2YUV);
            // Use only the Y (luminance) channel; less sensitive to color/lighting variation.
            Core.extractChannel(_yuvMat, _yMat, 0);

            // blur the image to reduce high frequency noises.
            Imgproc.GaussianBlur(_yMat, _yMat, new Size(3, 3), 0);
            // find edges in the image.
            Imgproc.Canny(_yMat, _yMat, 50, 200, 3);

            // find contours.
            List<MatOfPoint> contours = new List<MatOfPoint>();
            Find4PointContours(_yMat, contours);

            // pick the contour of the largest area and rearrange the points in a consistent order.
            MatOfPoint maxAreaContour = GetMaxAreaContour(contours);
            maxAreaContour = OrderCornerPoints(maxAreaContour);

            bool found = maxAreaContour.checkVector(CvType.channels(CvType.CV_32SC2)) == 4;
            if (found)
            {
                // warpPerspective maps the 4 corners to a front-facing rectangle.
                using (Mat transformedMat = PerspectiveTransform(rgbaMat, maxAreaContour))
                {
                    if (!transformedMat.empty())
                    {
                        _outputDisplayAreaMat.setTo(new Scalar(0, 0, 0, 255));

                        if (transformedMat.width() <= _outputDisplayAreaMat.width() && transformedMat.height() <= _outputDisplayAreaMat.height()
                            && transformedMat.total() >= _outputDisplayAreaMat.total() / 16)
                        {
                            int x = _outputDisplayAreaMat.width() / 2 - transformedMat.width() / 2;
                            int y = _outputDisplayAreaMat.height() / 2 - transformedMat.height() / 2;
                            using (Mat dstAreaMat = new Mat(_outputDisplayAreaMat, new OpenCVForUnity.CoreModule.Rect(x, y, transformedMat.width(), transformedMat.height())))
                            {
                                transformedMat.copyTo(dstAreaMat);
                            }
                        }
                    }
                }
            }

            if (IsDebugMode)
            {
                // draw edge image.
                Imgproc.cvtColor(_yMat, rgbaMat, Imgproc.COLOR_GRAY2RGBA);

                // draw all found conours.
                Imgproc.drawContours(rgbaMat, contours, -1, _debugContourColor, 1);
            }

            if (found)
            {
                // draw max area contour.
                Imgproc.drawContours(rgbaMat, new List<MatOfPoint> { maxAreaContour }, -1, _contourColor, 2);

                if (IsDebugMode)
                {
                    // draw corner numbers.
                    Point[] cornerPts = maxAreaContour.toArray();
                    for (int i = 0; i < cornerPts.Length; i++)
                    {
                        Point pt = cornerPts[i];
                        Imgproc.putText(rgbaMat, i.ToString(), new Point(pt.x, pt.y), Imgproc.FONT_HERSHEY_SIMPLEX, 1.5, _debugCornerNumberColor, 1, Imgproc.LINE_AA, false);
                    }
                }
            }

            rgbaMat.copyTo(_inputDisplayAreaMat);

            OpenCVMatUnityUtils.MatToTexture2D(_displayMat, _texture);
        }

        /// <summary>
        /// Raises the helper initialized event.
        /// Recreates the preview texture and starts playback on first initialization.
        /// Skips Play when re-initialization has already restored Playing or Paused.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized", this);

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;

            CreateOrRecreateProcessingResources(rgbaMat);

#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API
            // If the WebCam is front facing, flip the Mat horizontally. Required for successful detection.
            if (_multiSourceToMatHelper.ActiveHelper is WebCamTextureToMatHelper webCamHelper)
            {
                _multiSourceToMatHelper.FlipHorizontal = webCamHelper.IsFrontFacing;
            }
#endif

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Clear();
                UpdateFpsMonitorPlaybackState();
                _fpsMonitor.Add("HelperKind", _multiSourceToMatHelper.RequestedHelperKind.ToString());
                _fpsMonitor.Add("Width", _displayMat.width().ToString());
                _fpsMonitor.Add("Height", _displayMat.height().ToString());
                _fpsMonitor.Add("Rotate90Degree", _multiSourceToMatHelper.Rotate90Degree.ToString());
                _fpsMonitor.Add("FlipVertical", _multiSourceToMatHelper.FlipVertical.ToString());
                _fpsMonitor.Add("FlipHorizontal", _multiSourceToMatHelper.FlipHorizontal.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
                _fpsMonitor.ConsoleText = "Please place a document paper (receipt or business card) on a plain background.";
            }

            _contourColor = new Scalar(255, 0, 0, 255);
            _debugContourColor = new Scalar(255, 255, 0, 255);
            _debugCornerNumberColor = new Scalar(0, 255, 255, 255);

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

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;
            if (rgbaMat == null)
            {
                return;
            }

            CreateOrRecreateProcessingResources(rgbaMat);

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
        /// Raises the is debug mode toggle value changed event.
        /// </summary>
        public void OnIsDebugModeToggleValueChanged()
        {
            if (IsDebugMode != IsDebugModeToggle.isOn)
            {
                IsDebugMode = IsDebugModeToggle.isOn;
            }
        }

        // Private Methods
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

        private void DisposeFrameProcessingResources()
        {
            _yuvMat?.Dispose();
            _yuvMat = null;
            _yMat?.Dispose();
            _yMat = null;
            _inputDisplayAreaMat = null;
            _outputDisplayAreaMat = null;
            _displayMat?.Dispose();
            _displayMat = null;
        }

        private void CreateOrRecreateProcessingResources(Mat rgbaMat)
        {
            if (rgbaMat == null)
            {
                return;
            }

            DisposeFrameProcessingResources();

            if (rgbaMat.width() < rgbaMat.height())
            {
                _displayMat = new Mat(rgbaMat.rows(), rgbaMat.cols() * 2, rgbaMat.type(), new Scalar(0, 0, 0, 255));
                _inputDisplayAreaMat = new Mat(_displayMat, new OpenCVForUnity.CoreModule.Rect(0, 0, rgbaMat.width(), rgbaMat.height()));
                _outputDisplayAreaMat = new Mat(_displayMat, new OpenCVForUnity.CoreModule.Rect(rgbaMat.width(), 0, rgbaMat.width(), rgbaMat.height()));
            }
            else
            {
                _displayMat = new Mat(rgbaMat.rows() * 2, rgbaMat.cols(), rgbaMat.type(), new Scalar(0, 0, 0, 255));
                _inputDisplayAreaMat = new Mat(_displayMat, new OpenCVForUnity.CoreModule.Rect(0, 0, rgbaMat.width(), rgbaMat.height()));
                _outputDisplayAreaMat = new Mat(_displayMat, new OpenCVForUnity.CoreModule.Rect(0, rgbaMat.height(), rgbaMat.width(), rgbaMat.height()));
            }

            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
                _texture = null;
            }

            _texture = new Texture2D(_displayMat.cols(), _displayMat.rows(), TextureFormat.RGBA32, false);

            if (ResultPreview != null)
            {
                ResultPreview.texture = _texture;
                AspectRatioFitter aspectRatioFitter = ResultPreview.GetComponent<AspectRatioFitter>();
                if (aspectRatioFitter != null)
                {
                    aspectRatioFitter.aspectRatio = (float)_texture.width / _texture.height;
                }
            }

            _yuvMat = new Mat();
            _yMat = new Mat();
        }

        private void Find4PointContours(Mat image, List<MatOfPoint> contours)
        {
            contours.Clear();
            List<MatOfPoint> tmp_contours = new List<MatOfPoint>();
            Mat hierarchy = new Mat();
            Imgproc.findContours(image, tmp_contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

            foreach (var cnt in tmp_contours)
            {
                MatOfInt hull = new MatOfInt();
                Geometry.convexHull(cnt, hull, false);

                Point[] cnt_arr = cnt.toArray();
                int[] hull_arr = hull.toArray();
                Point[] pts = new Point[hull_arr.Length];
                for (int i = 0; i < hull_arr.Length; i++)
                {
                    pts[i] = cnt_arr[hull_arr[i]];
                }

                MatOfPoint2f ptsFC2 = new MatOfPoint2f(pts);
                MatOfPoint2f approxFC2 = new MatOfPoint2f();
                MatOfPoint approxSC2 = new MatOfPoint();

                double arclen = Geometry.arcLength(ptsFC2, true);
                Geometry.approxPolyDP(ptsFC2, approxFC2, 0.01 * arclen, true);
                approxFC2.convertTo(approxSC2, CvType.CV_32S);

                if (approxSC2.checkVector(CvType.channels(CvType.CV_32SC2)) != 4)
                {
                    continue;
                }

                contours.Add(approxSC2);
            }
        }

        private MatOfPoint GetMaxAreaContour(List<MatOfPoint> contours)
        {
            if (contours.Count == 0)
            {
                return new MatOfPoint();
            }

            int index = -1;
            double area = 0;
            for (int i = 0; i < contours.Count; i++)
            {
                double tmp = Geometry.contourArea(contours[i]);
                if (area < tmp)
                {
                    area = tmp;
                    index = i;
                }
            }
            return contours[index];
        }

        private MatOfPoint OrderCornerPoints(MatOfPoint corners)
        {
            if (corners.empty() || corners.checkVector(CvType.channels(CvType.CV_32SC2)) < 4)
            {
                return corners;
            }

            // rearrange the points in the order of upper left, upper right, lower right, lower left.
            Point[] pts = corners.toArray();
            int minSumIdx = 0;
            int maxSumIdx = 0;
            int minDiffIdx = 0;
            int maxDiffIdx = 0;
            double minSum = double.MaxValue;
            double maxSum = double.MinValue;
            double minDiff = double.MaxValue;
            double maxDiff = double.MinValue;

            for (int i = 0; i < 4; i++)
            {
                double sum = pts[i].x + pts[i].y;
                if (sum < minSum)
                {
                    minSum = sum;
                    minSumIdx = i;
                }

                if (sum > maxSum)
                {
                    maxSum = sum;
                    maxSumIdx = i;
                }

                double diff = pts[i].y - pts[i].x;
                if (diff < minDiff)
                {
                    minDiff = diff;
                    minDiffIdx = i;
                }

                if (diff > maxDiff)
                {
                    maxDiff = diff;
                    maxDiffIdx = i;
                }
            }

            corners.fromArray(new Point[]
            {
                pts[minSumIdx],
                pts[minDiffIdx],
                pts[maxSumIdx],
                pts[maxDiffIdx],
            });
            return corners;
        }

        private Mat PerspectiveTransform(Mat image, MatOfPoint corners)
        {
            if (corners.checkVector(CvType.channels(CvType.CV_32SC2)) < 4)
            {
                return new Mat();
            }

            Point[] pts = corners.toArray();
            Point tl = pts[0];
            Point tr = pts[1];
            Point br = pts[2];
            Point bl = pts[3];

            double widthA = Math.Sqrt((br.x - bl.x) * (br.x - bl.x) + (br.y - bl.y) * (br.y - bl.y));
            double widthB = Math.Sqrt((tr.x - tl.x) * (tr.x - tl.x) + (tr.y - tl.y) * (tr.y - tl.y));
            int maxWidth = Math.Max((int)widthA, (int)widthB);

            double heightA = Math.Sqrt((tr.x - br.x) * (tr.x - br.x) + (tr.y - br.y) * (tr.y - br.y));
            double heightB = Math.Sqrt((tl.x - bl.x) * (tl.x - bl.x) + (tl.y - bl.y) * (tl.y - bl.y));
            int maxHeight = Math.Max((int)heightA, (int)heightB);

            maxWidth = (maxWidth < 1) ? 1 : maxWidth;
            maxHeight = (maxHeight < 1) ? 1 : maxHeight;

            Mat src = new Mat();
            corners.convertTo(src, CvType.CV_32FC2);
            Mat dst = new Mat(1, 4, CvType.CV_32FC2);
            dst.put(0, 0, 0, 0, maxWidth - 1, 0, maxWidth - 1, maxHeight - 1, 0, maxHeight - 1);

            // compute and apply the perspective transformation matrix.
            Mat outputMat = new Mat(maxHeight, maxWidth, image.type(), new Scalar(0, 0, 0, 255));
            Mat perspectiveTransform = Geometry.getPerspectiveTransform(src, dst);
            Imgproc.warpPerspective(image, outputMat, perspectiveTransform, outputMat.size());

            // return the transformed image.
            return outputMat;
        }
    }
}
