using System;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Comic Filter implementation for image processing.
    /// </summary>
    public class ComicFilter : IDisposable
    {
        // Private Fields
        private Mat _grayMat;
        private Mat _maskMat;
        private Mat _screentoneMat;
        private Mat _grayDstMat;

        private Mat _grayLUT;
        private Mat _contrastAdjustmentsLUT;
        private Mat _kernelDilate;
        private Mat _kernelErode;
        private Size _blurSize;
        private int _blackThresh;
        private bool _drawMainLine;
        private bool _useNoiseFilter;

        private bool _disposed = false;

        // Public Methods
        /// <summary>
        /// Initializes a new instance of the ComicFilter class.
        /// </summary>
        /// <param name="blackThresh">Black threshold value.</param>
        /// <param name="grayThresh">Gray threshold value.</param>
        /// <param name="thickness">Line thickness.</param>
        /// <param name="useNoiseFilter">Whether to use noise filter.</param>
        public ComicFilter(int blackThresh = 60, int grayThresh = 120, int thickness = 5, bool useNoiseFilter = true)
        {
            _blackThresh = blackThresh;
            _drawMainLine = (thickness != 0);
            _useNoiseFilter = useNoiseFilter;

            _grayLUT = new Mat(1, 256, CvType.CV_8UC1);
            byte[] lutArray = new byte[256];
            for (int i = 0; i < lutArray.Length; i++)
            {
                if (blackThresh <= i && i < grayThresh)
                    lutArray[i] = 255;
            }
            OpenCVMatUtils.CopyToMat(lutArray, _grayLUT);

            if (_drawMainLine)
            {
                _kernelDilate = new Mat(thickness, thickness, CvType.CV_8UC1, new Scalar(1));

                int erode = (thickness >= 5) ? 2 : 1;
                _kernelErode = new Mat(erode, erode, CvType.CV_8UC1, new Scalar(1));

                int blur = (thickness >= 4) ? thickness - 1 : 3;
                _blurSize = new Size(blur, blur);

                _contrastAdjustmentsLUT = new Mat(1, 256, CvType.CV_8UC1);
                byte[] contrastAdjustmentsLUTArray = new byte[256];
                for (int i = 0; i < contrastAdjustmentsLUTArray.Length; i++)
                {
                    int a = (int)(i * 1.5f);
                    contrastAdjustmentsLUTArray[i] = (a > byte.MaxValue) ? (byte)255 : (byte)a;
                }
                OpenCVMatUtils.CopyToMat(contrastAdjustmentsLUTArray, _contrastAdjustmentsLUT);
            }
        }

        /// <summary>
        /// Finalizer for the ComicFilter class.
        /// </summary>
        ~ComicFilter()
        {
            Dispose(false);
        }

        /// <summary>
        /// Processes the input image and applies comic filter effect.
        /// </summary>
        /// <param name="src">Source image matrix.</param>
        /// <param name="dst">Destination image matrix.</param>
        /// <param name="isBGR">Whether the image is in BGR format.</param>
        public void Process(Mat src, Mat dst, bool isBGR = false)
        {
            ThrowIfDisposed();

            if (src != null) src.ThrowIfDisposed();
            if (dst != null) dst.ThrowIfDisposed();

            if (_grayMat != null && (_grayMat.width() != src.width() || _grayMat.height() != src.height()))
            {
                _grayMat.Dispose();
                _grayMat = null;
                _maskMat.Dispose();
                _maskMat = null;
                _screentoneMat.Dispose();
                _screentoneMat = null;
                _grayDstMat.Dispose();
                _grayDstMat = null;
            }
            _grayMat = _grayMat ?? new Mat(src.height(), src.width(), CvType.CV_8UC1);
            _maskMat = _maskMat ?? new Mat(src.height(), src.width(), CvType.CV_8UC1);
            _grayDstMat = _grayDstMat ?? new Mat(src.height(), src.width(), CvType.CV_8UC1);

            if (_screentoneMat == null)
            {
                // create a striped screentone.
                _screentoneMat = new Mat(src.height(), src.width(), CvType.CV_8UC1, new Scalar(255));
                for (int i = 0; i < _screentoneMat.rows() * 2.5f; i = i + 4)
                {
                    Imgproc.line(_screentoneMat, new Point(0, 0 + i), new Point(_screentoneMat.cols(), -_screentoneMat.cols() + i), new Scalar(0), 1);
                }
            }

            if (src.type() == CvType.CV_8UC1)
            {
                src.copyTo(_grayMat);
            }
            else if (dst.type() == CvType.CV_8UC3)
            {
                Imgproc.cvtColor(src, _grayMat, (isBGR) ? Imgproc.COLOR_BGR2GRAY : Imgproc.COLOR_RGB2GRAY);
            }
            else
            {
                Imgproc.cvtColor(src, _grayMat, (isBGR) ? Imgproc.COLOR_BGRA2GRAY : Imgproc.COLOR_RGBA2GRAY);
            }

            // binarize.
            Imgproc.threshold(_grayMat, _grayDstMat, _blackThresh, 255.0, Imgproc.THRESH_BINARY);

            // draw striped screentone.
            Core.LUT(_grayMat, _grayLUT, _maskMat);
            _screentoneMat.copyTo(_grayDstMat, _maskMat);

            // draw main line.
            if (_drawMainLine)
            {
                Core.LUT(_grayMat, _contrastAdjustmentsLUT, _maskMat); // = _grayMat.convertTo(_maskMat, -1, 1.5, 0);

                if (_useNoiseFilter)
                {
                    Imgproc.blur(_maskMat, _grayMat, _blurSize);
                    Imgproc.dilate(_grayMat, _maskMat, _kernelDilate);
                }
                else
                {
                    Imgproc.dilate(_maskMat, _grayMat, _kernelDilate);
                }
                Core.absdiff(_grayMat, _maskMat, _grayMat);
                Imgproc.threshold(_grayMat, _maskMat, 25, 255.0, Imgproc.THRESH_BINARY);
                if (_useNoiseFilter)
                {
                    Imgproc.erode(_maskMat, _grayMat, _kernelErode);
                    Core.bitwise_not(_grayMat, _maskMat);
                    _maskMat.copyTo(_grayDstMat, _grayMat);
                }
                else
                {
                    Core.bitwise_not(_maskMat, _grayMat);
                    _grayMat.copyTo(_grayDstMat, _maskMat);
                }
            }

            if (dst.type() == CvType.CV_8UC1)
            {
                _grayDstMat.copyTo(dst);
            }
            else if (dst.type() == CvType.CV_8UC3)
            {
                Imgproc.cvtColor(_grayDstMat, dst, (isBGR) ? Imgproc.COLOR_GRAY2BGR : Imgproc.COLOR_GRAY2RGB);
            }
            else
            {
                Imgproc.cvtColor(_grayDstMat, dst, (isBGR) ? Imgproc.COLOR_GRAY2BGRA : Imgproc.COLOR_GRAY2RGBA);
            }
        }

        /// <summary>
        /// Disposes the ComicFilter and releases resources.
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
                foreach (var mat in new[] { _grayMat, _maskMat, _screentoneMat, _grayDstMat, _grayLUT, _kernelDilate, _kernelErode, _contrastAdjustmentsLUT })
                    mat?.Dispose();

                _grayDstMat =
                _screentoneMat =
                _maskMat =
                _grayMat =
                _grayLUT =
                _kernelDilate =
                _kernelErode =
                _contrastAdjustmentsLUT = null;
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
