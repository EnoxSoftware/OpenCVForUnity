using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;

namespace OpenCVForUnityExample
{
    public class ColorBlobDetector
    {
        // Lower and Upper bounds for range checking in HSV color space
        private Scalar mLowerBound = new Scalar(0);
        private Scalar mUpperBound = new Scalar(0);
        // Minimum contour area in percent for contours filtering
        private static double mMinContourArea = 0.1;
        // Color radius for range checking in HSV color space
        private Scalar mColorRadius = new Scalar(25, 50, 50, 0);
        private Mat mSpectrum = new Mat();
        private List<MatOfPoint> mContours = new List<MatOfPoint>();

        // Cache
        private Mat mPyrDownMat = new Mat();
        private Mat mHsvMat = new Mat();
        private Mat mMask = new Mat();
        private Mat mDilatedMask = new Mat();
        private Mat mHierarchy = new Mat();

        public void SetColorRadius(Scalar radius)
        {
            mColorRadius = radius;
        }

        public void SetHsvColor(Scalar hsvColor)
        {
            double minH = (hsvColor.val[0] >= mColorRadius.val[0]) ? hsvColor.val[0] - mColorRadius.val[0] : 0;
            double maxH = (hsvColor.val[0] + mColorRadius.val[0] <= 255) ? hsvColor.val[0] + mColorRadius.val[0] : 255;

            mLowerBound.val[0] = minH;
            mUpperBound.val[0] = maxH;

            mLowerBound.val[1] = hsvColor.val[1] - mColorRadius.val[1];
            mUpperBound.val[1] = hsvColor.val[1] + mColorRadius.val[1];

            mLowerBound.val[2] = hsvColor.val[2] - mColorRadius.val[2];
            mUpperBound.val[2] = hsvColor.val[2] + mColorRadius.val[2];

            mLowerBound.val[3] = 0;
            mUpperBound.val[3] = 255;

            using (Mat spectrumHsv = new Mat(1, (int)(maxH - minH), CvType.CV_8UC3))
            {
                for (int j = 0; j < maxH - minH; j++)
                {
                    byte[] tmp = { (byte)(minH + j), (byte)255, (byte)255 };
                    spectrumHsv.put(0, j, tmp);
                }

                Imgproc.cvtColor(spectrumHsv, mSpectrum, Imgproc.COLOR_HSV2RGB_FULL, 4);
            }
        }

        public Mat GetSpectrum()
        {
            return mSpectrum;
        }

        public void SetMinContourArea(double area)
        {
            mMinContourArea = area;
        }

        public void Process(Mat rgbaImage)
        {
            Imgproc.pyrDown(rgbaImage, mPyrDownMat);
            Imgproc.pyrDown(mPyrDownMat, mPyrDownMat);

            Imgproc.cvtColor(mPyrDownMat, mHsvMat, Imgproc.COLOR_RGB2HSV_FULL);

            Core.inRange(mHsvMat, mLowerBound, mUpperBound, mMask);
            Imgproc.dilate(mMask, mDilatedMask, new Mat());

            List<MatOfPoint> contours = new List<MatOfPoint>();

            Imgproc.findContours(mDilatedMask, contours, mHierarchy, Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

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
            mContours.Clear();
            foreach (MatOfPoint each in contours)
            {
                MatOfPoint contour = each;
                if (Imgproc.contourArea(contour) > mMinContourArea * maxArea)
                {
                    Core.multiply(contour, new Scalar(4, 4), contour);
                    mContours.Add(contour);
                }
            }
        }

        public List<MatOfPoint> GetContours()
        {
            return mContours;
        }

        public void Dispose()
        {
            mSpectrum.Dispose();
            mPyrDownMat.Dispose();
            mHsvMat.Dispose();
            mMask.Dispose();
            mDilatedMask.Dispose();
            mHierarchy.Dispose();
        }
    }
}