#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace OpenCVForUnityExample.DnnModel
{
    /// <summary>
    /// Referring to https://github.com/opencv/opencv_zoo/tree/main/models/facial_expression_recognition
    /// </summary>
    public class FacialExpressionRecognizer
    {
        int backend;
        int target;

        string inputName = "data";
        string outputName = "label";
        Size input_size = new Size(112, 112);
        Scalar mean = new Scalar(0.5, 0.5, 0.5);
        Scalar std = new Scalar(0.5, 0.5, 0.5);

        Net facial_expression_recognition_net;

        List<string> classNames;

        List<Scalar> palette;

        Mat input_sizeMat;

        Mat getDataMat;

        FaceRecognizerSF faceRecognizer;

        public FacialExpressionRecognizer(string modelFilepath, string SF_modelFilepath, string SF_configFilepath, int backend = Dnn.DNN_BACKEND_OPENCV, int target = Dnn.DNN_TARGET_CPU)
        {
            // initialize
            if (!string.IsNullOrEmpty(modelFilepath))
            {
                facial_expression_recognition_net = Dnn.readNet(modelFilepath);
            }

            if (!string.IsNullOrEmpty(SF_modelFilepath))
            {
                faceRecognizer = FaceRecognizerSF.create(SF_modelFilepath, SF_configFilepath, backend, target);
            }

            this.backend = backend;
            this.target = target;

            facial_expression_recognition_net.setPreferableBackend(this.backend);
            facial_expression_recognition_net.setPreferableTarget(this.target);

            classNames = new List<string>();
            classNames.Add("angry");
            classNames.Add("disgust");
            classNames.Add("fearful");
            classNames.Add("happy");
            classNames.Add("neutral");
            classNames.Add("sad");
            classNames.Add("surprised");

            palette = new List<Scalar>();
            palette.Add(new Scalar(255, 56, 56, 255));
            palette.Add(new Scalar(82, 0, 133, 255));
            palette.Add(new Scalar(52, 69, 147, 255));
            palette.Add(new Scalar(255, 178, 29, 255));
            palette.Add(new Scalar(55, 55, 55, 255));
            palette.Add(new Scalar(100, 115, 255, 255));
            palette.Add(new Scalar(255, 112, 31, 255));
        }

        protected virtual Mat preprocess(Mat image, Mat bbox = null)
        {
            if (input_sizeMat == null)
                input_sizeMat = new Mat(input_size, CvType.CV_8UC3);

            if (bbox != null && faceRecognizer != null)
            {
                alignCrop(image, bbox, input_sizeMat);
            }
            else
            {
                Imgproc.resize(image, input_sizeMat, input_size);
            }

            // Create a 4D blob from a frame.
            Mat blob;

            blob = Dnn.blobFromImage(input_sizeMat, 1.0 / 255.0, input_sizeMat.size(), Scalar.all(0), true, false, CvType.CV_32F); // HWC to NCHW, BGR to RGB

            int c = input_sizeMat.channels();
            int h = input_sizeMat.height();
            int w = input_sizeMat.width();

            Mat blob_cxhxw = blob.reshape(1, new int[] { c, h, w });// [c, h, w]

            for (int i = 0; i < c; ++i)
            {
                Mat blob_1xhw = blob_cxhxw.row(i).reshape(1, 1);// [1, h, w] => [1, h * w]

                // Subtract blob by mean.
                Core.subtract(blob_1xhw, new Scalar(mean.val[i]), blob_1xhw);
                // Divide blob by std.
                Core.divide(blob_1xhw, new Scalar(std.val[i]), blob_1xhw);
            }

            return blob;// [1, 112, 112, 3]
        }

        public virtual Mat infer(Mat image, Mat bbox = null)
        {
            // cheack
            if (image.channels() != 3)
            {
                Debug.Log("The input image must be in BGR format.");
                return new Mat();
            }

            // Preprocess
            Mat input_blob = preprocess(image, bbox);

            // Forward
            facial_expression_recognition_net.setInput(input_blob, inputName);

            Mat output_blob = facial_expression_recognition_net.forward(outputName);

            // Postprocess
            Mat results = postprocess(output_blob);

            input_blob.Dispose();

            return results;
        }

        protected virtual Mat postprocess(Mat output_blob)
        {
            Mat results = softmax(output_blob);

            return results;// [1, 7]
        }

        protected virtual Mat softmax(Mat src)
        {
            Mat dst = src.clone();

            Core.MinMaxLocResult result = Core.minMaxLoc(src);
            Scalar max = new Scalar(result.maxVal);
            Core.subtract(src, max, dst);
            Core.exp(dst, dst);
            Scalar sum = Core.sumElems(dst);
            Core.divide(dst, sum, dst);

            return dst;
        }

        public virtual void visualize(Mat image, List<Mat> results, Mat faces, bool print_results = false, bool isRGB = false)
        {
            if (image.IsDisposed)
                return;

            if (results.Count != faces.rows())
                return;

            StringBuilder sb = null;

            if (print_results)
                sb = new StringBuilder();

            for (int i = 0; i < results.Count; ++i)
            {
                float[] face_box = new float[4];
                faces.get(i, 0, face_box);

                float left = face_box[0] + 2;
                float top = face_box[1] + 2;
                float right = face_box[0] + face_box[2] - 2;
                float bottom = face_box[1] + face_box[3] - 2;

                ClassificationData bmData = getBestMatchData(results[i]);
                int classId = (int)bmData.cls;
                string label = getClassLabel(bmData.cls) + ", " + String.Format("{0:0.0000}", bmData.conf);

                Scalar c = palette[classId % palette.Count];
                Scalar color = isRGB ? c : new Scalar(c.val[2], c.val[1], c.val[0], c.val[3]);

                // draw box
                Imgproc.rectangle(image, new Point(left, top), new Point(right, bottom), color, 2);

                // draw label
                int[] baseLine = new int[1];
                Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

                top = Mathf.Max((float)top, (float)labelSize.height);
                Imgproc.rectangle(image, new Point(left, top + 2),
                    new Point(left + labelSize.width, top + labelSize.height + baseLine[0] + 2), color, Core.FILLED);
                Imgproc.putText(image, label, new Point(left, top + labelSize.height + 2), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, Scalar.all(255), 1, Imgproc.LINE_AA);

                // Print results
                if (print_results)
                {
                    sb.AppendLine(String.Format("-----------expression {0}-----------", i + 1));
                    sb.AppendLine(String.Format("Best match: " + getClassLabel(bmData.cls) + ", " + bmData));
                }
            }

            if (print_results)
                Debug.Log(sb);
        }

        public virtual void dispose()
        {
            if (facial_expression_recognition_net != null)
                facial_expression_recognition_net.Dispose();

            if (input_sizeMat != null)
                input_sizeMat.Dispose();

            input_sizeMat = null;

            if (getDataMat != null)
                getDataMat.Dispose();

            getDataMat = null;

            if (faceRecognizer != null)
                faceRecognizer.Dispose();
        }

        private void alignCrop(Mat src_img, Mat face_box, Mat aligned_img)
        {
            // The alignCrop method of FaceRecognizerSF is used here, because the implementation of the alignment and crop process is cumbersome.
            // This method returns an image of 112x112 pixels, the same as the Facial Expression Recognition model input.
            faceRecognizer.alignCrop(src_img, face_box, aligned_img);
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct ClassificationData
        {
            public readonly float cls;
            public readonly float conf;

            // sizeof(ClassificationData)
            public const int Size = 2 * sizeof(float);

            public ClassificationData(int cls, float conf)
            {
                this.cls = cls;
                this.conf = conf;
            }

            public override string ToString()
            {
                return "cls:" + cls + " conf:" + conf;
            }
        };

        public virtual ClassificationData[] getData(Mat results)
        {
            if (results.empty())
                return new ClassificationData[0];

            int num = results.cols();

            if (getDataMat == null)
            {
                getDataMat = new Mat(num, 2, CvType.CV_32FC1);
                float[] arange = Enumerable.Range(0, num).Select(i => (float)i).ToArray();
                getDataMat.col(0).put(0, 0, arange);
            }

            Mat results_numx1 = results.reshape(1, num);
            results_numx1.copyTo(getDataMat.col(1));

            var dst = new ClassificationData[num];
            OpenCVForUnity.UtilsModule.MatUtils.copyFromMat(getDataMat, dst);

            return dst;
        }

        public virtual ClassificationData[] getSortedData(Mat results, int topK = 5)
        {
            if (results.empty())
                return new ClassificationData[0];

            int num = results.cols();

            if (topK < 1 || topK > num) topK = num;
            var sortedData = getData(results).OrderByDescending(x => x.conf).Take(topK).ToArray();

            return sortedData;
        }

        public virtual ClassificationData getBestMatchData(Mat results)
        {
            if (results.empty())
                return new ClassificationData();

            Core.MinMaxLocResult minmax = Core.minMaxLoc(results);

            return new ClassificationData((int)minmax.maxLoc.x, (float)minmax.maxVal);
        }

        public virtual string getClassLabel(float id)
        {
            int classId = (int)id;
            string className = string.Empty;
            if (classNames != null && classNames.Count != 0)
            {
                if (classId >= 0 && classId < (int)classNames.Count)
                {
                    className = classNames[classId];
                }
            }
            if (string.IsNullOrEmpty(className))
                className = classId.ToString();

            return className;
        }
    }
}
#endif