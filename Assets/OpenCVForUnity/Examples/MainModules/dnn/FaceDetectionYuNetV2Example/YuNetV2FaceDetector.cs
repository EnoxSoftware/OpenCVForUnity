#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using System;
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

            for (int i = results.rows() - 1; i >= 0; --i)
            {
                float[] box = new float[4];
                results.get(i, 0, box);
                float[] conf = new float[1];
                results.get(i, 14, conf);
                float[] landmarks = new float[10];
                results.get(i, 4, landmarks);

                float left = box[0];
                float top = box[1];
                float right = box[0] + box[2];
                float bottom = box[1] + box[3];

                Scalar bbc = bBoxColor;
                Scalar bbcolor = isRGB ? bbc : new Scalar(bbc.val[2], bbc.val[1], bbc.val[0], bbc.val[3]);

                Imgproc.rectangle(image, new Point(left, top), new Point(right, bottom), bbcolor, 2);

                string label = String.Format("{0:0.0000}", conf[0]);
                int[] baseLine = new int[1];
                Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

                top = Mathf.Max((float)top, (float)labelSize.height);
                Imgproc.rectangle(image, new Point(left, top - labelSize.height),
                    new Point(left + labelSize.width, top + baseLine[0]), bbcolor, Core.FILLED);
                Imgproc.putText(image, label, new Point(left, top), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(0, 0, 0, 255), 1, Imgproc.LINE_AA);

                // draw landmark points
                for (int j = 0; j < 10; j += 2)
                {
                    Scalar c = keyPointsColors[(j / 2) % keyPointsColors.Length];
                    Scalar color = isRGB ? c : new Scalar(c.val[2], c.val[1], c.val[0], c.val[3]);

                    Imgproc.circle(image, new Point(landmarks[j], landmarks[j + 1]), 2, color, 2);
                }
            }

            // Print results
            if (print_results)
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < results.rows(); ++i)
                {
                    float[] box = new float[4];
                    results.get(i, 0, box);
                    float[] conf = new float[1];
                    results.get(i, 14, conf);
                    float[] landmarks = new float[10];
                    results.get(i, 4, landmarks);

                    sb.AppendLine(String.Format("-----------face {0}-----------", i + 1));
                    sb.AppendLine(String.Format("conf: {0:0.0000}", conf[0]));
                    sb.AppendLine(String.Format("box: {0:0} {1:0} {2:0} {3:0}", box[0], box[1], box[2], box[3]));
                    sb.Append("landmarks: ");
                    foreach (var p in landmarks)
                    {
                        sb.Append(String.Format("{0:0} ", p));
                    }
                    sb.AppendLine();
                }

                Debug.Log(sb);
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
    }
}
#endif