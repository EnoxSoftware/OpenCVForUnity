#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace OpenCVForUnityExample.DnnModel
{
    /// <summary>
    /// Referring to https://github.com/opencv/opencv_zoo/tree/main/models/face_detection_yunet
    /// </summary>
    public class YuNetV2FaceDetector
    {
        Size input_size;
        float conf_threshold;
        float nms_threshold;
        int topK;
        int backend;
        int target;

        protected Scalar bBoxColor = new Scalar(0, 255, 0, 255);

        protected Scalar[] keyPointsColors = new Scalar[] {
            new Scalar(0, 0, 255, 255), // # right eye
            new Scalar(255, 0, 0, 255), // # left eye
            new Scalar(255, 255, 0, 255), // # nose tip
            new Scalar(0, 255, 255, 255), // # mouth right
            new Scalar(0, 255, 0, 255), // # mouth left
            new Scalar(255, 255, 255, 255) };

        FaceDetectorYN detection_model;

        Mat input_sizeMat;

        public YuNetV2FaceDetector(string modelFilepath, string configFilepath, Size inputSize, float confThreshold = 0.6f, float nmsThreshold = 0.3f, int topK = 5000, int backend = Dnn.DNN_BACKEND_OPENCV, int target = Dnn.DNN_TARGET_CPU)
        {
            // initialize
            if (!string.IsNullOrEmpty(modelFilepath))
            {
                detection_model = FaceDetectorYN.create(modelFilepath, configFilepath, inputSize, confThreshold, nmsThreshold, topK, backend, target);
            }

            input_size = new Size(inputSize.width > 0 ? inputSize.width : 320, inputSize.height > 0 ? inputSize.height : 320);
            conf_threshold = Mathf.Clamp01(confThreshold);
            nms_threshold = Mathf.Clamp01(nmsThreshold);
            this.topK = topK;
            this.backend = backend;
            this.target = target;
        }

        protected virtual Mat preprocess(Mat image)
        {
            int h = (int)input_size.height;
            int w = (int)input_size.width;

            if (input_sizeMat == null)
                input_sizeMat = new Mat(new Size(w, h), CvType.CV_8UC3);// [h, w]

            Imgproc.resize(image, input_sizeMat, new Size(w, h));

            return input_sizeMat;// [h, w, 3]
        }

        public virtual Mat infer(Mat image)
        {
            // cheack
            if (image.channels() != 3)
            {
                Debug.Log("The input image must be in BGR format.");
                return new Mat();
            }

            // Preprocess
            Mat input_blob = preprocess(image);

            // Forward
            Mat results = new Mat();
            detection_model.detect(input_blob, results);

            // Postprocess
            // scale_boxes
            float x_factor = image.width() / (float)input_size.width;
            float y_factor = image.height() / (float)input_size.height;

            for (int i = 0; i < results.rows(); ++i)
            {
                float[] results_arr = new float[14];
                results.get(i, 0, results_arr);
                for (int j = 0; j < 14; ++j)
                {
                    if (j % 2 == 0)
                    {
                        results_arr[j] = results_arr[j] * x_factor;
                    }
                    else
                    {
                        results_arr[j] = results_arr[j] * y_factor;
                    }
                }

                results.put(i, 0, results_arr);
            }

            return results;
        }

        protected virtual Mat postprocess(Mat output_blob)
        {
            return output_blob;
        }

        public virtual void visualize(Mat image, Mat results, bool print_results = false, bool isRGB = false)
        {
            if (image.IsDisposed)
                return;

            if (results.empty() || results.cols() < 15)
                return;

            DetectionData[] data = getData(results);

            foreach (var d in data.Reverse())
            {
                float left = d.xy.x;
                float top = d.xy.y;
                float right = d.xy.x + d.wh.x;
                float bottom = d.xy.y + d.wh.y;
                float score = d.score;

                Scalar bbc = bBoxColor;
                Scalar bbcolor = isRGB ? bbc : new Scalar(bbc.val[2], bbc.val[1], bbc.val[0], bbc.val[3]);

                Imgproc.rectangle(image, new Point(left, top), new Point(right, bottom), bbcolor, 2);

                string label = score.ToString("F4");
                int[] baseLine = new int[1];
                Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

                top = Mathf.Max((float)top, (float)labelSize.height);
                Imgproc.rectangle(image, new Point(left, top - labelSize.height),
                    new Point(left + labelSize.width, top + baseLine[0]), bbcolor, Core.FILLED);
                Imgproc.putText(image, label, new Point(left, top), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(0, 0, 0, 255), 1, Imgproc.LINE_AA);

                // draw landmark points
                Imgproc.circle(image, new Point(d.rightEye.x, d.rightEye.y), 2,
                    isRGB ? keyPointsColors[0] : new Scalar(keyPointsColors[0].val[2], keyPointsColors[0].val[1], keyPointsColors[0].val[0], keyPointsColors[0].val[3]), 2);
                Imgproc.circle(image, new Point(d.leftEye.x, d.leftEye.y), 2,
                    isRGB ? keyPointsColors[1] : new Scalar(keyPointsColors[1].val[2], keyPointsColors[1].val[1], keyPointsColors[1].val[0], keyPointsColors[1].val[3]), 2);
                Imgproc.circle(image, new Point(d.nose.x, d.nose.y), 2,
                    isRGB ? keyPointsColors[2] : new Scalar(keyPointsColors[2].val[2], keyPointsColors[2].val[1], keyPointsColors[2].val[0], keyPointsColors[2].val[3]), 2);
                Imgproc.circle(image, new Point(d.rightMouth.x, d.rightMouth.y), 2,
                    isRGB ? keyPointsColors[3] : new Scalar(keyPointsColors[3].val[2], keyPointsColors[3].val[1], keyPointsColors[3].val[0], keyPointsColors[3].val[3]), 2);
                Imgproc.circle(image, new Point(d.leftMouth.x, d.leftMouth.y), 2,
                    isRGB ? keyPointsColors[4] : new Scalar(keyPointsColors[4].val[2], keyPointsColors[4].val[1], keyPointsColors[4].val[0], keyPointsColors[4].val[3]), 2);
            }

            // Print results
            if (print_results)
            {
                StringBuilder sb = new StringBuilder(128);

                for (int i = 0; i < data.Length; ++i)
                {
                    var d = data[i];
                    float left = d.xy.x;
                    float top = d.xy.y;
                    float right = d.xy.x + d.wh.x;
                    float bottom = d.xy.y + d.wh.y;
                    float score = d.score;

                    sb.AppendFormat("-----------face {0}-----------", i + 1);
                    sb.AppendLine();
                    sb.AppendFormat("score: {0:F4}", score);
                    sb.AppendLine();
                    sb.AppendFormat("box: {0:F0} {1:F0} {2:F0} {3:F0}", left, top, right, bottom);
                    sb.AppendLine();
                    sb.Append("landmarks: ");
                    sb.AppendFormat("{0:F0} {1:F0} ", d.rightEye.x, d.rightEye.y);
                    sb.AppendFormat("{0:F0} {1:F0} ", d.leftEye.x, d.leftEye.y);
                    sb.AppendFormat("{0:F0} {1:F0} ", d.nose.x, d.nose.y);
                    sb.AppendFormat("{0:F0} {1:F0} ", d.rightMouth.x, d.rightMouth.y);
                    sb.AppendFormat("{0:F0} {1:F0} ", d.leftMouth.x, d.leftMouth.y);
                    sb.AppendLine();
                }

                Debug.Log(sb.ToString());
            }
        }

        public virtual void dispose()
        {
            if (detection_model != null)
                detection_model.Dispose();

            if (input_sizeMat != null)
                input_sizeMat.Dispose();

            input_sizeMat = null;
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct DetectionData
        {
            // Bounding box
            public readonly Vector2 xy;
            public readonly Vector2 wh;

            // Key points
            public readonly Vector2 rightEye;
            public readonly Vector2 leftEye;
            public readonly Vector2 nose;
            public readonly Vector2 rightMouth;
            public readonly Vector2 leftMouth;

            // Confidence score [0, 1]
            public readonly float score;

            // sizeof(DetectionData)
            public const int Size = 15 * sizeof(float);

            public DetectionData(Vector2 xy, Vector2 wh, Vector2 rightEye, Vector2 leftEye, Vector2 nose, Vector2 rightMouth, Vector2 leftMouth, float score)
            {
                this.xy = xy;
                this.wh = wh;
                this.rightEye = rightEye;
                this.leftEye = leftEye;
                this.nose = nose;
                this.rightMouth = rightMouth;
                this.leftMouth = leftMouth;
                this.score = score;
            }

            public override string ToString()
            {
                return "xy:" + xy.ToString() + " wh:" + wh.ToString() + " rightEye:" + rightEye.ToString() + " leftEye:" + leftEye.ToString()
                    + " nose:" + nose.ToString() + " rightMouth:" + rightMouth.ToString() + " leftMouth:" + leftMouth.ToString() + " score:" + score.ToString();
            }
        };

        public virtual DetectionData[] getData(Mat results)
        {
            if (results.empty())
                return new DetectionData[0];

            var dst = new DetectionData[results.rows()];
            MatUtils.copyFromMat(results, dst);

            return dst;
        }
    }
}
#endif