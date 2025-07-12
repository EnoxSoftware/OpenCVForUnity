using System;
using OpenCVForUnity.CoreModule;

namespace OpenCVForUnityExample
{
    [System.Serializable]
    public struct CameraParameters
    {
        // Public Fields
        public string CalibrationDate;
        public int FramesCount;
        public int ImageWidth;
        public int ImageHeight;
        public int CalibrationFlags;
        public double[] CameraMatrix;
        public double[] DistortionCoefficients;
        public double AvgReprojectionError;

        // Public Methods
        public CameraParameters(int framesCount, int imageWidth, int imageHeight, int calibrationFlags, double[] cameraMatrix, double[] distortionCoefficients, double avgReprojectionError)
        {
            CalibrationDate = DateTime.Now.ToString();
            FramesCount = framesCount;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
            CalibrationFlags = calibrationFlags;
            CameraMatrix = cameraMatrix;
            DistortionCoefficients = distortionCoefficients;
            AvgReprojectionError = avgReprojectionError;
        }

        public CameraParameters(int framesCount, int imageWidth, int imageHeight, int calibrationFlags, Mat cameraMatrix, Mat distortionCoefficients, double avgReprojectionError)
        {
            double[] cameraMatrixArr = new double[cameraMatrix.total()];
            cameraMatrix.get(0, 0, cameraMatrixArr);

            double[] distortionCoefficientsArr = new double[distortionCoefficients.total()];
            distortionCoefficients.get(0, 0, distortionCoefficientsArr);

            CalibrationDate = DateTime.Now.ToString();
            FramesCount = framesCount;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
            CalibrationFlags = calibrationFlags;
            CameraMatrix = cameraMatrixArr;
            DistortionCoefficients = distortionCoefficientsArr;
            AvgReprojectionError = avgReprojectionError;
        }

        public Mat GetCameraMatrix()
        {
            Mat m = new Mat(3, 3, CvType.CV_64FC1);
            m.put(0, 0, CameraMatrix);
            return m;
        }

        public Mat GetDistortionCoefficients()
        {
            Mat m = new Mat(DistortionCoefficients.Length, 1, CvType.CV_64FC1);
            m.put(0, 0, DistortionCoefficients);
            return m;
        }
    }
}
