using System;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Color Blob Detector for detecting colored regions in images.
    /// </summary>
    public class ColorBlobDetector : IDisposable
    {
        // Private Fields
        // Lower and Upper bounds for range checking in HSV color space
        private Scalar _lowerBound = new Scalar(0);
        private Scalar _upperBound = new Scalar(0);
        // Minimum contour area in percent for contours filtering
        private static double _minContourArea = 0.1;
        // Color radius for range checking in HSV color space
        private Scalar _colorRadius = new Scalar(25, 50, 50, 0);
        private Mat _spectrum = new Mat();
        private List<MatOfPoint> _contours = new List<MatOfPoint>();

        // Caches
        private Mat _pyrDownMat = new Mat();
        private Mat _hsvMat = new Mat();
        private Mat _mask = new Mat();
        private Mat _dilatedMask = new Mat();
        private Mat _hierarchy = new Mat();

        private bool _disposed = false;

        // Public Methods
        /// <summary>
        /// Initializes a new instance of the ColorBlobDetector class.
        /// </summary>
        public ColorBlobDetector() { }

        /// <summary>
        /// Finalizer for the ColorBlobDetector class.
        /// </summary>
        ~ColorBlobDetector()
        {
            Dispose(false);
        }

        /// <summary>
        /// Sets the color radius for HSV color range checking.
        /// </summary>
        /// <param name="radius">Color radius.</param>
        public void SetColorRadius(Scalar radius)
        {
            ThrowIfDisposed();

            _colorRadius = radius;
        }

        /// <summary>
        /// Sets the HSV color for blob detection.
        /// </summary>
        /// <param name="hsvColor">HSV color value.</param>
        public void SetHsvColor(Scalar hsvColor)
        {
            ThrowIfDisposed();

            double minH = (hsvColor.val[0] >= _colorRadius.val[0]) ? hsvColor.val[0] - _colorRadius.val[0] : 0;
            double maxH = (hsvColor.val[0] + _colorRadius.val[0] <= 255) ? hsvColor.val[0] + _colorRadius.val[0] : 255;

            _lowerBound.val[0] = minH;
            _upperBound.val[0] = maxH;

            _lowerBound.val[1] = hsvColor.val[1] - _colorRadius.val[1];
            _upperBound.val[1] = hsvColor.val[1] + _colorRadius.val[1];

            _lowerBound.val[2] = hsvColor.val[2] - _colorRadius.val[2];
            _upperBound.val[2] = hsvColor.val[2] + _colorRadius.val[2];

            _lowerBound.val[3] = 0;
            _upperBound.val[3] = 255;

            using (Mat spectrumHsv = new Mat(1, (int)(maxH - minH), CvType.CV_8UC3))
            {
                for (int j = 0; j < maxH - minH; j++)
                {
                    byte[] tmp = { (byte)(minH + j), (byte)255, (byte)255 };
                    spectrumHsv.put(0, j, tmp);
                }

                Imgproc.cvtColor(spectrumHsv, _spectrum, Imgproc.COLOR_HSV2RGB_FULL, 4);
            }
        }

        /// <summary>
        /// Gets the spectrum matrix.
        /// </summary>
        /// <returns>The spectrum matrix.</returns>
        public Mat GetSpectrum()
        {
            ThrowIfDisposed();

            return _spectrum;
        }

        /// <summary>
        /// Sets the minimum contour area for filtering.
        /// </summary>
        /// <param name="area">Minimum contour area.</param>
        public void SetMinContourArea(double area)
        {
            ThrowIfDisposed();

            _minContourArea = area;
        }

        /// <summary>
        /// Processes the input image to detect color blobs.
        /// </summary>
        /// <param name="rgbaImage">Input RGBA image.</param>
        public void Process(Mat rgbaImage)
        {
            ThrowIfDisposed();

            if (rgbaImage != null) rgbaImage.ThrowIfDisposed();

            Imgproc.pyrDown(rgbaImage, _pyrDownMat);
            Imgproc.pyrDown(_pyrDownMat, _pyrDownMat);

            Imgproc.cvtColor(_pyrDownMat, _hsvMat, Imgproc.COLOR_RGB2HSV_FULL);

            Core.inRange(_hsvMat, _lowerBound, _upperBound, _mask);
            Imgproc.dilate(_mask, _dilatedMask, new Mat());

            List<MatOfPoint> contours = new List<MatOfPoint>();

            Imgproc.findContours(_dilatedMask, contours, _hierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

            // Find max contour area
            double maxArea = 0;
            foreach (MatOfPoint each in contours)
            {
                MatOfPoint wrapper = each;
                double area = Imgproc.contourArea(wrapper);
                if (area > maxArea)
                    maxArea = area;
            }

            // Filter contours by area and resize to fit the original image size
            _contours.Clear();
            foreach (MatOfPoint each in contours)
            {
                MatOfPoint contour = each;
                if (Imgproc.contourArea(contour) > _minContourArea * maxArea)
                {
                    Core.multiply(contour, new Scalar(4, 4), contour);
                    _contours.Add(contour);
                }
            }
        }

        /// <summary>
        /// Gets the detected contours.
        /// </summary>
        /// <returns>List of detected contours.</returns>
        public List<MatOfPoint> GetContours()
        {
            ThrowIfDisposed();

            return _contours;
        }

        /// <summary>
        /// Disposes the ColorBlobDetector and releases resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Private Methods
        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _spectrum.Dispose(); _spectrum = null;
                _pyrDownMat.Dispose(); _pyrDownMat = null;
                _hsvMat.Dispose(); _hsvMat = null;
                _mask.Dispose(); _mask = null;
                _dilatedMask.Dispose(); _dilatedMask = null;
                _hierarchy.Dispose(); _hierarchy = null;
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
    }
}
