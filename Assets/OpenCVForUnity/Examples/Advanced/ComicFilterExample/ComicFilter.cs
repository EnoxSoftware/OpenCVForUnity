using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UtilsModule;
using System;

namespace OpenCVForUnityExample
{

    public class ComicFilter
    {

        Mat grayMat;
        Mat maskMat;
        Mat screentoneMat;
        Mat grayDstMat;

        Mat grayLUT;
        Mat contrastAdjustmentsLUT;
        Mat kernel_dilate;
        Mat kernel_erode;
        Size blurSize;
        int blackThresh;
        bool drawMainLine;
        bool useNoiseFilter;


        public ComicFilter(int blackThresh = 60, int grayThresh = 120, int thickness = 5, bool useNoiseFilter = true)
        {
            this.blackThresh = blackThresh;
            this.drawMainLine = (thickness != 0);
            this.useNoiseFilter = useNoiseFilter;

            grayLUT = new Mat(1, 256, CvType.CV_8UC1);
            byte[] lutArray = new byte[256];
            for (int i = 0; i < lutArray.Length; i++)
            {
                if (blackThresh <= i && i < grayThresh)
                    lutArray[i] = 255;
            }
            MatUtils.copyToMat(lutArray, grayLUT);

            if (drawMainLine)
            {
                kernel_dilate = new Mat(thickness, thickness, CvType.CV_8UC1, new Scalar(1));

                int erode = (thickness >= 5) ? 2 : 1;
                kernel_erode = new Mat(erode, erode, CvType.CV_8UC1, new Scalar(1));

                int blur = (thickness >= 4) ? thickness - 1 : 3;
                blurSize = new Size(blur, blur);

                contrastAdjustmentsLUT = new Mat(1, 256, CvType.CV_8UC1);
                byte[] contrastAdjustmentsLUTArray = new byte[256];
                for (int i = 0; i < contrastAdjustmentsLUTArray.Length; i++)
                {
                    int a = (int)(i * 1.5f);
                    contrastAdjustmentsLUTArray[i] = (a > byte.MaxValue) ? (byte)255 : (byte)a;

                }
                MatUtils.copyToMat(contrastAdjustmentsLUTArray, contrastAdjustmentsLUT);
            }
        }

        public void Process(Mat src, Mat dst, bool isBGR = false)
        {
            if (src == null)
                throw new ArgumentNullException("src == null");
            if (dst == null)
                throw new ArgumentNullException("dst == null");

            if (grayMat != null && (grayMat.width() != src.width() || grayMat.height() != src.height()))
            {
                grayMat.Dispose();
                grayMat = null;
                maskMat.Dispose();
                maskMat = null;
                screentoneMat.Dispose();
                screentoneMat = null;
                grayDstMat.Dispose();
                grayDstMat = null;
            }
            grayMat = grayMat ?? new Mat(src.height(), src.width(), CvType.CV_8UC1);
            maskMat = maskMat ?? new Mat(src.height(), src.width(), CvType.CV_8UC1);
            grayDstMat = grayDstMat ?? new Mat(src.height(), src.width(), CvType.CV_8UC1);

            if (screentoneMat == null)
            {
                // create a striped screentone.
                screentoneMat = new Mat(src.height(), src.width(), CvType.CV_8UC1, new Scalar(255));
                for (int i = 0; i < screentoneMat.rows() * 2.5f; i = i + 4)
                {
                    Imgproc.line(screentoneMat, new Point(0, 0 + i), new Point(screentoneMat.cols(), -screentoneMat.cols() + i), new Scalar(0), 1);
                }
            }

            if (src.type() == CvType.CV_8UC1)
            {
                src.copyTo(grayMat);
            }
            else if (dst.type() == CvType.CV_8UC3)
            {
                Imgproc.cvtColor(src, grayMat, (isBGR) ? Imgproc.COLOR_BGR2GRAY : Imgproc.COLOR_RGB2GRAY);
            }
            else
            {
                Imgproc.cvtColor(src, grayMat, (isBGR) ? Imgproc.COLOR_BGRA2GRAY : Imgproc.COLOR_RGBA2GRAY);
            }


            // binarize.
            Imgproc.threshold(grayMat, grayDstMat, blackThresh, 255.0, Imgproc.THRESH_BINARY);

            // draw striped screentone.
            Core.LUT(grayMat, grayLUT, maskMat);
            screentoneMat.copyTo(grayDstMat, maskMat);

            // draw main line.
            if (drawMainLine)
            {
                Core.LUT(grayMat, contrastAdjustmentsLUT, maskMat); // = grayMat.convertTo(maskMat, -1, 1.5, 0);

                if (useNoiseFilter)
                {
                    Imgproc.blur(maskMat, grayMat, blurSize);
                    Imgproc.dilate(grayMat, maskMat, kernel_dilate);
                }
                else
                {
                    Imgproc.dilate(maskMat, grayMat, kernel_dilate);
                }
                Core.absdiff(grayMat, maskMat, grayMat);
                Imgproc.threshold(grayMat, maskMat, 25, 255.0, Imgproc.THRESH_BINARY);
                if (useNoiseFilter)
                {
                    Imgproc.erode(maskMat, grayMat, kernel_erode);
                    Core.bitwise_not(grayMat, maskMat);
                    maskMat.copyTo(grayDstMat, grayMat);
                }
                else
                {
                    Core.bitwise_not(maskMat, grayMat);
                    grayMat.copyTo(grayDstMat, maskMat);
                }
            }


            if (dst.type() == CvType.CV_8UC1)
            {
                grayDstMat.copyTo(dst);
            }
            else if (dst.type() == CvType.CV_8UC3)
            {
                Imgproc.cvtColor(grayDstMat, dst, (isBGR) ? Imgproc.COLOR_GRAY2BGR : Imgproc.COLOR_GRAY2RGB);
            }
            else
            {
                Imgproc.cvtColor(grayDstMat, dst, (isBGR) ? Imgproc.COLOR_GRAY2BGRA : Imgproc.COLOR_GRAY2RGBA);
            }
        }

        public void Dispose()
        {
            foreach (var mat in new[] { grayMat, maskMat, screentoneMat, grayDstMat, grayLUT, kernel_dilate, kernel_erode, contrastAdjustmentsLUT })
                if (mat != null) mat.Dispose();

            grayDstMat =
            screentoneMat =
            maskMat =
            grayMat =
            grayLUT =
            kernel_dilate =
            kernel_erode =
            contrastAdjustmentsLUT = null;
        }
    }
}