using OpenCVForUnity.CoreModule;

namespace OpenCVForUnityExample.MOT.ByteTrack
{
    public class KalmanFilter
    {
        private Mat mean_;
        private Mat covariance_;
        private Mat motion_mat_;
        private Mat update_mat_;

        private float std_weight_position_;
        private float std_weight_velocity_;

        public KalmanFilter(float std_weight_position = 1f / 20, float std_weight_velocity = 1f / 160)
        {
            std_weight_position_ = std_weight_position;
            std_weight_velocity_ = std_weight_velocity;

            motion_mat_ = new Mat(8, 8, CvType.CV_32F);
            float[] transitionMatrixArr = {
                        1, 0, 0, 0, 1, 0, 0, 0,
                        0, 1, 0, 0, 0, 1, 0, 0,
                        0, 0, 1, 0, 0, 0, 1, 0,
                        0, 0, 0, 1, 0, 0, 0, 1,
                        0, 0, 0, 0, 1, 0, 0, 0,
                        0, 0, 0, 0, 0, 1, 0, 0,
                        0, 0, 0, 0, 0, 0, 1, 0,
                        0, 0, 0, 0, 0, 0, 0, 1
                    };
            motion_mat_.put(0, 0, transitionMatrixArr);

            update_mat_ = new Mat(4, 8, CvType.CV_32F);
            float[] measurementMatrixArr = {
                        1, 0, 0, 0, 0, 0, 0, 0,
                        0, 1, 0, 0, 0, 0, 0, 0,
                        0, 0, 1, 0, 0, 0, 0, 0,
                        0, 0, 0, 1, 0, 0, 0, 0
                    };
            update_mat_.put(0, 0, measurementMatrixArr);
        }

        public void Initiate(IRectBase measurement)
        {
            var rectData = RectToXyAh(measurement);
            float[] rectDataArr = new float[4];
            rectData.get(0, 0, rectDataArr);
            mean_ = new Mat(1, 8, CvType.CV_32F);
            mean_.put(0, 0, rectDataArr);
            mean_.put(0, 4, 0f, 0f, 0f, 0f);

            var std = new float[] {
                2 * std_weight_position_ * measurement.Height,
                2 * std_weight_position_ * measurement.Height,
                1e-2f,
                2 * std_weight_position_ * measurement.Height,
                10 * std_weight_velocity_ * measurement.Height,
                10 * std_weight_velocity_ * measurement.Height,
                1e-5f,
                10 * std_weight_velocity_ * measurement.Height
            };
            covariance_ = new Mat(std.Length, std.Length, CvType.CV_32F, new Scalar(0));
            for (int i = 0; i < std.Length; i++)
            {
                covariance_.put(i, i, std[i] * std[i]);
            }
        }

        public IRectBase Predict(bool mean_eight_to_zero)
        {
            if (mean_eight_to_zero) mean_.put(0, 7, 0f);

            var std = new float[] {
                std_weight_position_ * (float)mean_.get(0, 3)[0],
                std_weight_position_ * (float)mean_.get(0, 3)[0],
                1e-2f,
                std_weight_position_ * (float)mean_.get(0, 3)[0],
                std_weight_velocity_ * (float)mean_.get(0, 3)[0],
                std_weight_velocity_ * (float)mean_.get(0, 3)[0],
                1e-5f,
                std_weight_velocity_ * (float)mean_.get(0, 3)[0]
            };
            Mat motion_cov = new Mat(std.Length, std.Length, CvType.CV_32F, new Scalar(0));
            for (int i = 0; i < std.Length; i++)
            {
                motion_cov.put(i, i, std[i] * std[i]);
            }

            Core.gemm(motion_mat_, mean_, 1, new Mat(), 0, mean_, Core.GEMM_2_T);
            Core.transpose(mean_, mean_);
            Core.gemm(covariance_, motion_mat_, 1, new Mat(), 0, covariance_, Core.GEMM_2_T);
            Core.gemm(motion_mat_, covariance_, 1, motion_cov, 1, covariance_);

            return XyAhToRect(mean_.submat(0, 1, 0, 4));
        }

        public IRectBase Update(IRectBase measurement)
        {
            var projected_mean = new Mat(1, 4, CvType.CV_32F);
            var projected_cov = new Mat(4, 4, CvType.CV_32F);
            Project(ref projected_mean, ref projected_cov);

            var B = new Mat();
            Core.gemm(covariance_, update_mat_, 1, new Mat(), 0, B, Core.GEMM_2_T);
            Core.transpose(B, B);

            Mat inverted_projected_cov = new Mat();
            Core.invert(projected_cov, inverted_projected_cov);
            Mat kalman_gain = new Mat();
            Core.gemm(inverted_projected_cov, B, 1, new Mat(), 0, kalman_gain);
            Core.transpose(kalman_gain, kalman_gain);

            Mat innovation = new Mat();
            Core.subtract(RectToXyAh(measurement), projected_mean, innovation);

            Mat tmp = new Mat();
            Core.gemm(innovation, kalman_gain, 1, new Mat(), 0, tmp, Core.GEMM_2_T);
            Core.add(mean_, tmp, mean_);

            Core.gemm(kalman_gain, projected_cov, 1, new Mat(), 0, tmp);
            Core.gemm(tmp, kalman_gain, 1, new Mat(), 0, tmp, Core.GEMM_2_T);
            Core.subtract(covariance_, tmp, covariance_);

            return XyAhToRect(mean_.submat(0, 1, 0, 4));
        }

        private void Project(ref Mat projected_mean, ref Mat projected_covariance)
        {
            var std = new float[] {
                std_weight_position_ * (float)mean_.get(0, 3)[0],
                std_weight_position_ * (float)mean_.get(0, 3)[0],
                1e-1f,
                std_weight_position_ * (float)mean_.get(0, 3)[0]
            };

            Core.gemm(update_mat_, mean_, 1, new Mat(), 0, projected_mean, Core.GEMM_2_T);
            Core.transpose(projected_mean, projected_mean);
            Mat tmp = new Mat();
            Core.gemm(covariance_, update_mat_, 1, new Mat(), 0, tmp, Core.GEMM_2_T);
            Core.gemm(update_mat_, tmp, 1, new Mat(), 0, projected_covariance);

            Mat stdDiag = new Mat(std.Length, std.Length, CvType.CV_32F, new Scalar(0));
            for (int i = 0; i < std.Length; i++)
            {
                stdDiag.put(i, i, std[i]);
            }
            Core.multiply(stdDiag, stdDiag, stdDiag);
            Core.add(projected_covariance, stdDiag, projected_covariance);
        }

        private Mat RectToXyAh(IRectBase rect)
        {
            var xyah = new Mat(1, 4, CvType.CV_32F);
            xyah.put(0, 0, new float[] {
                rect.Left + rect.Width / 2,
                rect.Top + rect.Height / 2,
                rect.Width / rect.Height,
                rect.Height
            });
            return xyah;
        }

        private IRectBase XyAhToRect(Mat xyah)
        {
            var xyah_array = new float[4];
            xyah.get(0, 0, xyah_array);
            var xyah_width = xyah_array[2] * xyah_array[3];
            return new TlwhRect(xyah_array[1] - xyah_array[3] / 2, xyah_array[0] - xyah_width / 2, xyah_width, xyah_array[3]);
        }
    }
}
