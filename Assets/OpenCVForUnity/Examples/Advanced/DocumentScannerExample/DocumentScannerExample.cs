using System;
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
    /// Document Scanner Example
    /// An example of document scanning (like receipts, business cards etc) using the Imgproc class.
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
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

        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

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

            IsDebugModeToggle.isOn = IsDebugMode;
        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                // change the color space to YUV.
                Imgproc.cvtColor(rgbaMat, _yuvMat, Imgproc.COLOR_RGBA2RGB);
                Imgproc.cvtColor(_yuvMat, _yuvMat, Imgproc.COLOR_RGB2YUV);
                // grap only the Y component.
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

                bool found = (maxAreaContour.size().area() > 0);
                if (found)
                {
                    // trasform the prospective of original image.
                    using (Mat transformedMat = PerspectiveTransform(rgbaMat, maxAreaContour))
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
                        for (int i = 0; i < maxAreaContour.toArray().Length; i++)
                        {
                            var pt = maxAreaContour.get(i, 0);
                            Imgproc.putText(rgbaMat, i.ToString(), new Point(pt[0], pt[1]), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, _debugCornerNumberColor, 1, Imgproc.LINE_AA, false);
                        }
                    }
                }

                rgbaMat.copyTo(_inputDisplayAreaMat);

                OpenCVMatUtils.MatToTexture2D(_displayMat, _texture);
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

            _texture = new Texture2D(_displayMat.cols(), _displayMat.rows(), TextureFormat.RGBA32, false);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("width", _displayMat.width().ToString());
                _fpsMonitor.Add("height", _displayMat.height().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
                _fpsMonitor.ConsoleText = "Please place a document paper (receipt or business card) on a plain background.";
            }

            _yuvMat = new Mat();
            _yMat = new Mat();
            _contourColor = new Scalar(255, 0, 0, 255);
            _debugContourColor = new Scalar(255, 255, 0, 255);
            _debugCornerNumberColor = new Scalar(255, 255, 255, 255);

#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API
            // If the WebCam is front facing, flip the Mat horizontally. Required for successful detection.
            if (_multiSource2MatHelper.Source2MatHelper is WebCamTexture2MatHelper webCamHelper)
                webCamHelper.FlipHorizontal = webCamHelper.IsFrontFacing();
#endif
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;

            _yuvMat?.Dispose();
            _yMat?.Dispose();
            _displayMat?.Dispose();
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
        private void Find4PointContours(Mat image, List<MatOfPoint> contours)
        {
            contours.Clear();
            List<MatOfPoint> tmp_contours = new List<MatOfPoint>();
            Mat hierarchy = new Mat();
            Imgproc.findContours(image, tmp_contours, hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

            foreach (var cnt in tmp_contours)
            {
                MatOfInt hull = new MatOfInt();
                Imgproc.convexHull(cnt, hull, false);

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

                double arclen = Imgproc.arcLength(ptsFC2, true);
                Imgproc.approxPolyDP(ptsFC2, approxFC2, 0.01 * arclen, true);
                approxFC2.convertTo(approxSC2, CvType.CV_32S);

                if (approxSC2.size().area() != 4)
                    continue;

                contours.Add(approxSC2);
            }
        }

        private MatOfPoint GetMaxAreaContour(List<MatOfPoint> contours)
        {
            if (contours.Count == 0)
                return new MatOfPoint();

            int index = -1;
            double area = 0;
            for (int i = 0; i < contours.Count; i++)
            {
                double tmp = Imgproc.contourArea(contours[i]);
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
            if (corners.size().area() <= 0 || corners.rows() < 4)
                return corners;

            // rearrange the points in the order of upper left, upper right, lower right, lower left.
            using (Mat x = new Mat(corners.size(), CvType.CV_32SC1))
            using (Mat y = new Mat(corners.size(), CvType.CV_32SC1))
            using (Mat d = new Mat(corners.size(), CvType.CV_32SC1))
            using (Mat dst = new Mat(corners.size(), CvType.CV_32SC2))
            {
                Core.extractChannel(corners, x, 0);
                Core.extractChannel(corners, y, 1);

                // the sum of the upper left points is the smallest and the sum of the lower right points is the largest.
                Core.add(x, y, d);
                Core.MinMaxLocResult result = Core.minMaxLoc(d);
                dst.put(0, 0, corners.get((int)result.minLoc.y, 0));
                dst.put(2, 0, corners.get((int)result.maxLoc.y, 0));

                // the difference in the upper right point is the smallest, and the difference in the lower left is the largest.
                Core.subtract(y, x, d);
                result = Core.minMaxLoc(d);
                dst.put(1, 0, corners.get((int)result.minLoc.y, 0));
                dst.put(3, 0, corners.get((int)result.maxLoc.y, 0));

                dst.copyTo(corners);
            }
            return corners;
        }

        private Mat PerspectiveTransform(Mat image, MatOfPoint corners)
        {
            if (corners.size().area() <= 0 || corners.rows() < 4)
                return image;

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
            Mat dst = new Mat(4, 1, CvType.CV_32FC2);
            dst.put(0, 0, 0, 0, maxWidth - 1, 0, maxWidth - 1, maxHeight - 1, 0, maxHeight - 1);

            // compute and apply the perspective transformation matrix.
            Mat outputMat = new Mat(maxHeight, maxWidth, image.type(), new Scalar(0, 0, 0, 255));
            Mat perspectiveTransform = Imgproc.getPerspectiveTransform(src, dst);
            Imgproc.warpPerspective(image, outputMat, perspectiveTransform, new Size(outputMat.cols(), outputMat.rows()));

            // return the transformed image.
            return outputMat;
        }
    }
}
