#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// LibFaceDetection V3 Example
    /// Referring to https://github.com/ShiqiYu/libfacedetection.train/blob/master/tasks/task1/detect.py.
    /// </summary>
    public class LibFaceDetectionV3WebCamTextureExample : DnnObjectDetectionWebCamTextureExample
    {
        protected double[][] min_boxes = new double[][] { new double[] { 10.0, 16.0, 24.0 }, new double[] { 32.0, 48.0 }, new double[] { 64.0, 96.0 }, new double[] { 128.0, 192.0, 256.0 } };
        protected double[] strides = new double[] { 8.0, 16.0, 32.0, 64.0 };
        protected Mat priors;
        protected Scalar[] pointsColors = new Scalar[] { new Scalar(0, 0, 255, 255), new Scalar(255, 0, 0, 255), new Scalar(255, 255, 0, 255), new Scalar(0, 255, 255, 255), new Scalar(0, 255, 0, 255), new Scalar(255, 255, 255, 255) };

        protected override void Run()
        {
            priors = generatePriors(inpWidth > 0 ? inpWidth : 320, inpHeight > 0 ? inpHeight : 240);

            base.Run();
        }

        protected override void postprocess(Mat frame, List<Mat> outs, Net net, int backend = Dnn.DNN_BACKEND_OPENCV)
        {
            List<int> classIdsList = new List<int>();
            List<float> confidencesList = new List<float>();
            List<Rect2d> boxesList = new List<Rect2d>();
            List<Point[]> pointsList = new List<Point[]>();

            if (outs.Count == 2)
            {
                // reshape mat : outs[0]:[1, x, 4] to [x, 4], outs[1]:[1, x, 2] to [x, 2]
                Mat boxes_m = outs[0].reshape(1, new int[] { outs[0].size(1), outs[0].size(2) });
                Mat scores_m = outs[1].reshape(1, new int[] { outs[1].size(1), outs[1].size(2) });

                //Debug.Log("boxes_m: " + boxes_m);
                //Debug.Log("scores_m: " + scores_m);
                //Debug.Log("priors: " + priors);

                convertLocationsToBoxes(boxes_m, priors, 0.1f, 0.2f);
                centerFormToCornerForm(boxes_m);

                Mat boxes_0_4 = new Mat(boxes_m, new Range(0, boxes_m.rows()), new Range(0, 4));
                float[] boxes_arr = new float[boxes_0_4.rows() * boxes_0_4.cols()];
                MatUtils.copyFromMat(boxes_0_4, boxes_arr);

                Mat scores_1_2 = new Mat(scores_m, new Range(0, scores_m.rows()), new Range(1, 2));
                float[] confidences_arr = new float[scores_1_2.rows()];
                MatUtils.copyFromMat(scores_1_2, confidences_arr);

                for (int i = 0; i < boxes_m.rows(); i++)
                {
                    float confidence = confidences_arr[i];

                    if (confidence > confThreshold)
                    {
                        int boxes_index = i * 4;

                        float left = boxes_arr[boxes_index] * frame.cols();
                        float top = boxes_arr[boxes_index + 1] * frame.rows();
                        float right = boxes_arr[boxes_index + 2] * frame.cols();
                        float bottom = boxes_arr[boxes_index + 3] * frame.rows();
                        float width = right - left + 1f;
                        float height = bottom - top + 1f;

                        classIdsList.Add(0);
                        confidencesList.Add(confidence);
                        boxesList.Add(new Rect2d(left, top, width, height));
                    }
                }

                if (boxes_m.cols() > 4 && boxes_m.cols() % 2 == 0)
                {
                    Mat points = new Mat(boxes_m, new Range(0, boxes_m.rows()), new Range(4, boxes_m.cols()));
                    float[] points_arr = new float[points.rows() * points.cols()];
                    MatUtils.copyFromMat(points, points_arr);

                    for (int i = 0; i < boxes_m.rows(); i++)
                    {
                        float confidence = confidences_arr[i];

                        if (confidence > confThreshold)
                        {
                            int points_index = i * points.cols();

                            Point[] p_arr = new Point[points.cols() / 2];
                            for (int index = 0; index < points.cols() / 2; index++)
                            {
                                float x = points_arr[points_index + index * 2] * frame.cols();
                                float y = points_arr[points_index + index * 2 + 1] * frame.rows();
                                p_arr[index] = new Point(x, y);
                            }
                            pointsList.Add(p_arr);
                        }
                    }
                }
            }

            MatOfRect2d boxes = new MatOfRect2d();
            boxes.fromList(boxesList);

            MatOfFloat confidences = new MatOfFloat();
            confidences.fromList(confidencesList);

            MatOfInt indices = new MatOfInt();

            Dnn.NMSBoxes(boxes, confidences, confThreshold, nmsThreshold, indices);

            //Debug.Log("indices.dump () " + indices.dump());
            //Debug.Log ("indices.ToString () "+indices.ToString());

            for (int i = 0; i < indices.total(); ++i)
            {
                int idx = (int)indices.get(i, 0)[0];
                Rect2d box = boxesList[idx];
                drawPred(classIdsList[idx], confidencesList[idx], box.x, box.y,
                    box.x + box.width, box.y + box.height, frame);

                if (pointsList.Count > 0)
                    drawPredPoints(pointsList[idx], frame);
            }

            indices.Dispose();
            boxes.Dispose();
            confidences.Dispose();
        }

        protected virtual Mat generatePriors(int width = 320, int height = 240)
        {

            double[][] shrinkage_list = new double[2][];
            double[][] feature_map_list = new double[2][];

            var feature_map_w = new double[strides.Length];
            for (int i = 0; i < strides.Length; i++) feature_map_w[i] = (int)Math.Floor(width / strides[i]);
            feature_map_list[0] = feature_map_w;

            var feature_map_h = new double[strides.Length];
            for (int i = 0; i < strides.Length; i++) feature_map_h[i] = (int)Math.Floor(height / strides[i]);
            feature_map_list[1] = feature_map_h;

            for (int i = 0; i < 2; i++) shrinkage_list[i] = strides;


            int priors_size = 0;
            for (int index = 0; index < feature_map_list[0].Length; index++)
                priors_size += (int)(feature_map_list[1][index] * feature_map_list[0][index] * min_boxes[index].Length);

            float[] priors_arr = new float[priors_size * 4];

            int count = 0;
            for (int index = 0; index < feature_map_list[0].Length; index++)
            {
                var scale_w = width / shrinkage_list[0][index];
                var scale_h = height / shrinkage_list[1][index];
                for (int j = 0; j < feature_map_list[1][index]; j++)
                {
                    for (int i = 0; i < feature_map_list[0][index]; i++)
                    {
                        var x_center = (i + 0.5) / scale_w;
                        var y_center = (j + 0.5) / scale_h;

                        foreach (var min_box in min_boxes[index])
                        {
                            var w = min_box / width;
                            var h = min_box / height;

                            priors_arr[count * 4] = (float)x_center;
                            priors_arr[count * 4 + 1] = (float)y_center;
                            priors_arr[count * 4 + 2] = (float)w;
                            priors_arr[count * 4 + 3] = (float)h;

                            count++;
                        }
                    }
                }
            }

            Mat priors = new Mat(priors_size, 4, CvType.CV_32FC1);
            MatUtils.copyToMat(priors_arr, priors);

            // Clamp values
            Imgproc.threshold(priors, priors, 0, 0, Imgproc.THRESH_TOZERO);
            Imgproc.threshold(priors, priors, 1.0, 0, Imgproc.THRESH_TRUNC);

            return priors;
        }

        protected virtual void convertLocationsToBoxes(Mat locations, Mat priors, float center_variance = 0.1f, float size_variance = 0.2f)
        {

            if (locations.rows() != priors.rows() || locations.cols() < 4 || priors.cols() < 4 || locations.type() != CvType.CV_32FC1 || priors.type() != CvType.CV_32FC1)
                return;

            Mat locations_0_2 = new Mat(locations, new Range(0, locations.rows()), new Range(0, 2));
            Mat locations_2_4 = new Mat(locations, new Range(0, locations.rows()), new Range(2, 4));
            Mat priors_0_2 = new Mat(priors, new Range(0, priors.rows()), new Range(0, 2));
            Mat priors_2_4 = new Mat(priors, new Range(0, priors.rows()), new Range(2, 4));

            Core.multiply(locations_0_2, priors_2_4, locations_0_2, center_variance);
            Core.add(locations_0_2, priors_0_2, locations_0_2);
            Core.multiply(locations_2_4, Scalar.all(size_variance), locations_2_4);
            Core.exp(locations_2_4, locations_2_4);
            Core.multiply(locations_2_4, priors_2_4, locations_2_4);

            if (locations.cols() > 4)
            {
                for (int index = 4; index < locations.cols(); index = index + 2)
                {
                    Mat _locations = new Mat(locations, new Range(0, locations.rows()), new Range(index, index + 2));

                    Core.multiply(_locations, priors_2_4, _locations, center_variance);
                    Core.add(_locations, priors_0_2, _locations);
                }
            }
        }

        protected virtual void centerFormToCornerForm(Mat locations)
        {

            if (locations.cols() < 4 || locations.type() != CvType.CV_32FC1)
                return;

            Mat locations_0_2 = new Mat(locations, new Range(0, locations.rows()), new Range(0, 2));
            Mat locations_2_4 = new Mat(locations, new Range(0, locations.rows()), new Range(2, 4));

            Core.divide(locations_2_4, Scalar.all(2), locations_2_4);
            Core.subtract(locations_0_2, locations_2_4, locations_0_2);

            Core.multiply(locations_2_4, Scalar.all(2), locations_2_4);
            Core.add(locations_0_2, locations_2_4, locations_2_4);
        }

        protected virtual void drawPredPoints(Point[] points, Mat frame)
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (i < pointsColors.Length)
                {
                    Imgproc.circle(frame, points[i], 2, pointsColors[i], 2);
                }
                else
                {
                    Imgproc.circle(frame, points[i], 2, pointsColors[pointsColors.Length - 1], 2);
                }
            }
        }
    }
}
#endif

#endif