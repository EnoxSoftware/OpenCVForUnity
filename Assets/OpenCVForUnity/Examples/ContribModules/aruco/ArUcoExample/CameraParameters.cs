using UnityEngine;
using System;
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    [System.Serializable]
    public struct CameraParameters
    {
        public string calibration_date; 
        public int frames_count;
        public int image_width;
        public int image_height;
        public int calibration_flags;
        public double[] camera_matrix;
        public double[] distortion_coefficients;
        public double avg_reprojection_error;

        public CameraParameters (int frames_count, int image_width, int image_height, int calibration_flags, double[] camera_matrix, double[] distortion_coefficients, double avg_reprojection_error)
        {
            this.calibration_date = DateTime.Now.ToString ();
            this.frames_count = frames_count;
            this.image_width = image_width;
            this.image_height = image_height;
            this.calibration_flags = calibration_flags;
            this.camera_matrix = camera_matrix;
            this.distortion_coefficients = distortion_coefficients;
            this.avg_reprojection_error = avg_reprojection_error;
        }

        public CameraParameters (int frames_count, int image_width, int image_height, int calibration_flags, Mat camera_matrix, Mat distortion_coefficients, double avg_reprojection_error)
        {
            double[] camera_matrixArr = new double[camera_matrix.total()];
            camera_matrix.get (0, 0, camera_matrixArr);

            double[] distortion_coefficientsArr = new double[distortion_coefficients.total()];
            distortion_coefficients.get (0, 0, distortion_coefficientsArr);

            this.calibration_date = DateTime.Now.ToString ();
            this.frames_count = frames_count;
            this.image_width = image_width;
            this.image_height = image_height;
            this.calibration_flags = calibration_flags;
            this.camera_matrix = camera_matrixArr;
            this.distortion_coefficients = distortion_coefficientsArr;
            this.avg_reprojection_error = avg_reprojection_error;
        }

        public Mat GetCameraMatrix ()
        {
            Mat m = new Mat (3, 3, CvType.CV_64FC1);
            m.put (0, 0, camera_matrix);
            return m;
        }

        public Mat GetDistortionCoefficients ()
        {
            Mat m = new Mat (distortion_coefficients.Length, 1, CvType.CV_64FC1);
            m.put (0, 0, distortion_coefficients);
            return m;
        }
    }
}
