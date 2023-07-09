#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using OpenCVRect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample.DnnModel
{
    /// <summary>
    /// Referring to https://github.com/AlexeyAB/darknet.
    /// https://gist.github.com/YashasSamaga/48bdb167303e10f4d07b754888ddbdcf
    /// </summary>
    public class YOLOv4ObjectDetector
    {
        Size input_size;
        float conf_threshold;
        float nms_threshold;
        int topK;
        int backend;
        int target;

        int num_classes = 80;

        DetectionModel detection_model;

        List<string> classNames;

        List<Scalar> palette;

        Mat maxSizeImg;

        MatOfInt classIds;
        MatOfFloat confidences;
        MatOfRect boxes;

        public YOLOv4ObjectDetector(string modelFilepath, string configFilepath, string classesFilepath, Size inputSize, float confThreshold = 0.25f, float nmsThreshold = 0.45f, int topK = 1000, int backend = Dnn.DNN_BACKEND_OPENCV, int target = Dnn.DNN_TARGET_CPU)
        {
            // initialize
            if (!string.IsNullOrEmpty(modelFilepath))
            {
                detection_model = new DetectionModel(modelFilepath, configFilepath);
                detection_model.setInputParams(1.0 / 255.0, inputSize, new Scalar(0, 0, 0), true, false);
                detection_model.setNmsAcrossClasses(false);// Perform classwise NMS.
                detection_model.setPreferableBackend(this.backend);
                detection_model.setPreferableTarget(this.target);
            }

            if (!string.IsNullOrEmpty(classesFilepath))
            {
                classNames = readClassNames(classesFilepath);
                num_classes = classNames.Count;
            }

            input_size = new Size(inputSize.width > 0 ? inputSize.width : 640, inputSize.height > 0 ? inputSize.height : 640);
            conf_threshold = Mathf.Clamp01(confThreshold);
            nms_threshold = Mathf.Clamp01(nmsThreshold);
            this.topK = topK;
            this.backend = backend;
            this.target = target;

            classIds = new MatOfInt();
            confidences = new MatOfFloat();
            boxes = new MatOfRect();

            palette = new List<Scalar>();
            palette.Add(new Scalar(255, 56, 56, 255));
            palette.Add(new Scalar(255, 157, 151, 255));
            palette.Add(new Scalar(255, 112, 31, 255));
            palette.Add(new Scalar(255, 178, 29, 255));
            palette.Add(new Scalar(207, 210, 49, 255));
            palette.Add(new Scalar(72, 249, 10, 255));
            palette.Add(new Scalar(146, 204, 23, 255));
            palette.Add(new Scalar(61, 219, 134, 255));
            palette.Add(new Scalar(26, 147, 52, 255));
            palette.Add(new Scalar(0, 212, 187, 255));
            palette.Add(new Scalar(44, 153, 168, 255));
            palette.Add(new Scalar(0, 194, 255, 255));
            palette.Add(new Scalar(52, 69, 147, 255));
            palette.Add(new Scalar(100, 115, 255, 255));
            palette.Add(new Scalar(0, 24, 236, 255));
            palette.Add(new Scalar(132, 56, 255, 255));
            palette.Add(new Scalar(82, 0, 133, 255));
            palette.Add(new Scalar(203, 56, 255, 255));
            palette.Add(new Scalar(255, 149, 200, 255));
            palette.Add(new Scalar(255, 55, 199, 255));
        }

        protected virtual Mat preprocess(Mat image)
        {
            // Add padding to make it square.
            int max = Mathf.Max(image.cols(), image.rows());

            if (maxSizeImg == null)
                maxSizeImg = new Mat(max, max, image.type());
            if (maxSizeImg.width() != max || maxSizeImg.height() != max)
                maxSizeImg.create(max, max, image.type());

            Imgproc.rectangle(maxSizeImg, new OpenCVRect(0, 0, maxSizeImg.width(), maxSizeImg.height()), Scalar.all(114), -1);

            Mat _maxSizeImg_roi = new Mat(maxSizeImg, new OpenCVRect((max - image.cols()) / 2, (max - image.rows()) / 2, image.cols(), image.rows()));
            image.copyTo(_maxSizeImg_roi);

            return maxSizeImg;// [max, max, 3]
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
            detection_model.detect(input_blob, classIds, confidences, boxes, conf_threshold, nms_threshold);

            // Postprocess
            int num = classIds.rows();
            Mat results = new Mat(num, 6, CvType.CV_32FC1);

            float maxSize = Mathf.Max((float)image.size().width, (float)image.size().height);
            float x_shift = (maxSize - (float)image.size().width) / 2f;
            float y_shift = (maxSize - (float)image.size().height) / 2f;

            for (int i = 0; i < num; ++i)
            {
                int[] classId_arr = new int[1];
                classIds.get(i, 0, classId_arr);
                int id = classId_arr[0];

                float[] confidence_arr = new float[1];
                confidences.get(i, 0, confidence_arr);
                float confidence = confidence_arr[0];

                int[] box_arr = new int[4];
                boxes.get(i, 0, box_arr);
                int x = box_arr[0] - (int)x_shift;
                int y = box_arr[1] - (int)y_shift;
                int w = box_arr[2];
                int h = box_arr[3];

                results.put(i, 0, new float[] { x, y, x + w, y + h, confidence, id });
            }

            return results;
        }

        protected virtual Mat postprocess(Mat output_blob, Size original_shape)
        {
            return output_blob;
        }

        public virtual void visualize(Mat image, Mat results, bool print_results = false, bool isRGB = false)
        {
            if (image.IsDisposed)
                return;

            if (results.empty() || results.cols() < 6)
                return;

            for (int i = results.rows() - 1; i >= 0; --i)
            {
                float[] box = new float[4];
                results.get(i, 0, box);
                float[] conf = new float[1];
                results.get(i, 4, conf);
                float[] cls = new float[1];
                results.get(i, 5, cls);

                float left = box[0];
                float top = box[1];
                float right = box[2];
                float bottom = box[3];
                int classId = (int)cls[0];

                Scalar c = palette[classId % palette.Count];
                Scalar color = isRGB ? c : new Scalar(c.val[2], c.val[1], c.val[0], c.val[3]);

                Imgproc.rectangle(image, new Point(left, top), new Point(right, bottom), color, 2);

                string label = String.Format("{0:0.00}", conf[0]);
                if (classNames != null && classNames.Count != 0)
                {
                    if (classId < (int)classNames.Count)
                    {
                        label = classNames[classId] + " " + label;
                    }
                }

                int[] baseLine = new int[1];
                Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

                top = Mathf.Max((float)top, (float)labelSize.height);
                Imgproc.rectangle(image, new Point(left, top - labelSize.height),
                    new Point(left + labelSize.width, top + baseLine[0]), color, Core.FILLED);
                Imgproc.putText(image, label, new Point(left, top), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, Scalar.all(255), 1, Imgproc.LINE_AA);
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
                    results.get(i, 4, conf);
                    float[] cls = new float[1];
                    results.get(i, 5, cls);

                    int classId = (int)cls[0];
                    string label = String.Format("{0:0}", cls[0]);
                    if (classNames != null && classNames.Count != 0)
                    {
                        if (classId < (int)classNames.Count)
                        {
                            label = classNames[classId] + " " + label;
                        }
                    }

                    sb.AppendLine(String.Format("-----------object {0}-----------", i + 1));
                    sb.AppendLine(String.Format("conf: {0:0.0000}", conf[0]));
                    sb.AppendLine(String.Format("cls: {0:0}", label));
                    sb.AppendLine(String.Format("box: {0:0} {1:0} {2:0} {3:0}", box[0], box[1], box[2], box[3]));
                }

                Debug.Log(sb);
            }
        }

        public virtual void dispose()
        {
            if (detection_model != null)
                detection_model.Dispose();

            if (maxSizeImg != null)
                maxSizeImg.Dispose();

            maxSizeImg = null;

            if (classIds != null)
                classIds.Dispose();
            if (confidences != null)
                confidences.Dispose();
            if (boxes != null)
                boxes.Dispose();

            classIds = null;
            confidences = null;
            boxes = null;
        }

        protected virtual List<string> readClassNames(string filename)
        {
            List<string> classNames = new List<string>();

            System.IO.StreamReader cReader = null;
            try
            {
                cReader = new System.IO.StreamReader(filename, System.Text.Encoding.Default);

                while (cReader.Peek() >= 0)
                {
                    string name = cReader.ReadLine();
                    classNames.Add(name);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
                return null;
            }
            finally
            {
                if (cReader != null)
                    cReader.Close();
            }

            return classNames;
        }
    }
}
#endif