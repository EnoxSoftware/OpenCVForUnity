#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections.Generic;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// LibFaceDetection V3 Example
    /// Referring to https://github.com/ShiqiYu/libfacedetection/tree/master/example/opencv_dnn/python.
    /// </summary>
    public class LibFaceDetectionV3WebCamTextureExample : DnnObjectDetectionWebCamTextureExample
    {
        protected Scalar[] pointsColors = new Scalar[] { new Scalar(0, 0, 255, 255), new Scalar(255, 0, 0, 255), new Scalar(255, 255, 0, 255),
            new Scalar(0, 255, 255, 255), new Scalar(0, 255, 0, 255), new Scalar(255, 255, 255, 255) };

        PriorBox pb;
        Mat boxes_m_c1;
        Mat boxes_m_c4;
        Mat confidences_m;
        MatOfRect2d boxes;
        MatOfFloat confidences;
        MatOfInt indices;

        public override void OnWebCamTextureToMatHelperInitialized()
        {
            base.OnWebCamTextureToMatHelperInitialized();

            Size input_shape = new Size(inpWidth > 0 ? inpWidth : 320, inpHeight > 0 ? inpHeight : 240);
            Size output_shape = bgrMat.size();
            pb = new PriorBox(input_shape, output_shape);
        }

        public override void OnWebCamTextureToMatHelperDisposed()
        {
            base.OnWebCamTextureToMatHelperDisposed();

            if (pb != null)
            {
                pb.dispose();
                pb = null;
            }

            if (boxes_m_c1 != null)
                boxes_m_c1.Dispose();
            if (boxes_m_c4 != null)
                boxes_m_c4.Dispose();
            if (confidences_m != null)
                confidences_m.Dispose();
            if (boxes != null)
                boxes.Dispose();
            if (confidences != null)
                confidences.Dispose();
            if (indices != null)
                indices.Dispose();

            boxes_m_c1 = null;
            boxes_m_c4 = null;
            confidences_m = null;
            boxes = null;
            confidences = null;
            indices = null;
        }

        protected override void postprocess(Mat frame, List<Mat> outs, Net net, int backend = Dnn.DNN_BACKEND_OPENCV)
        {
            // # Decode bboxes and landmarks
            Mat dets = pb.decode(outs[0], outs[1], outs[2]);


            // # Ignore low scores + NMS
            int num = dets.rows();

            if (boxes_m_c1 == null)
                boxes_m_c1 = new Mat(num, 4, CvType.CV_64FC1);
            if (boxes_m_c4 == null)
                boxes_m_c4 = new Mat(num, 1, CvType.CV_64FC4);
            if (confidences_m == null)
                confidences_m = new Mat(num, 1, CvType.CV_32FC1);

            if (boxes == null)
                boxes = new MatOfRect2d(boxes_m_c4);
            if (confidences == null)
                confidences = new MatOfFloat(confidences_m);
            if (indices == null)
                indices = new MatOfInt();

            Mat bboxes = dets.colRange(0, 4);
            bboxes.convertTo(boxes_m_c1, CvType.CV_64FC1);

            // x1,y1,x2,y2 => x,y,w,h
            Mat boxes_m_0_2 = boxes_m_c1.colRange(0, 2);
            Mat boxes_m_2_4 = boxes_m_c1.colRange(2, 4);
            Core.subtract(boxes_m_2_4, boxes_m_0_2, boxes_m_2_4);

            MatUtils.copyToMat(new IntPtr(boxes_m_c1.dataAddr()), boxes_m_c4);

            Mat scores = dets.colRange(14, 15);
            scores.copyTo(confidences_m);

            Dnn.NMSBoxes(boxes, confidences, confThreshold, nmsThreshold, indices);


            // # Draw boudning boxes and landmarks on the original image
            for (int i = 0; i < indices.total(); ++i)
            {
                int idx = (int)indices.get(i, 0)[0];

                float[] bbox_arr = new float[4];
                bboxes.get(idx, 0, bbox_arr);
                float[] confidence_arr = new float[1];
                confidences.get(idx, 0, confidence_arr);
                drawPred(0, confidence_arr[0], bbox_arr[0], bbox_arr[1], bbox_arr[2], bbox_arr[3], frame);

                Mat landmarks = dets.colRange(4, 14);
                float[] landmarks_arr = new float[10];
                landmarks.get(idx, 0, landmarks_arr);
                Point[] points = new Point[] { new Point(landmarks_arr[0], landmarks_arr[1]), new Point(landmarks_arr[2], landmarks_arr[3]),
                    new Point(landmarks_arr[4], landmarks_arr[5]), new Point(landmarks_arr[6], landmarks_arr[7]), new Point(landmarks_arr[8], landmarks_arr[9])};
                drawPredPoints(points, frame);
            }
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

        private class PriorBox
        {
            float[][] min_sizes = new float[][]{
                new float[]{10.0f,  16.0f,  24.0f},
                new float[]{32.0f,  48.0f},
                new float[]{64.0f,  96.0f},
                new float[]{128.0f, 192.0f, 256.0f}
            };

            int[] steps = new int[] { 8, 16, 32, 64 };
            float[] variance = new float[] { 0.1f, 0.2f };

            int in_w;
            int in_h;
            int out_w;
            int out_h;

            List<Size> feature_map_sizes;
            Mat priors;

            Mat dets;
            Mat ones;
            Mat scale;

            Mat priors_0_2;
            Mat priors_2_4;
            Mat bboxes;
            Mat bboxes_0_2;
            Mat bboxes_2_4;
            Mat landmarks;
            Mat landmarks_0_2;
            Mat landmarks_2_4;
            Mat landmarks_4_6;
            Mat landmarks_6_8;
            Mat landmarks_8_10;
            Mat scores;
            Mat ones_0_1;
            Mat ones_0_2;
            Mat bbox_scale;
            Mat landmark_scale;

            public PriorBox(Size input_shape, Size output_shape)
            {
                // initialize
                in_w = (int)input_shape.width;
                in_h = (int)input_shape.height;
                out_w = (int)output_shape.width;
                out_h = (int)output_shape.height;

                Size feature_map_2nd = new Size((int)((int)((in_w + 1) / 2) / 2), (int)((int)((in_h + 1) / 2) / 2));
                Size feature_map_3rd = new Size((int)(feature_map_2nd.width / 2), (int)(feature_map_2nd.height / 2));
                Size feature_map_4th = new Size((int)(feature_map_3rd.width / 2), (int)(feature_map_3rd.height / 2));
                Size feature_map_5th = new Size((int)(feature_map_4th.width / 2), (int)(feature_map_4th.height / 2));
                Size feature_map_6th = new Size((int)(feature_map_5th.width / 2), (int)(feature_map_5th.height / 2));

                feature_map_sizes = new List<Size>();
                feature_map_sizes.Add(feature_map_3rd);
                feature_map_sizes.Add(feature_map_4th);
                feature_map_sizes.Add(feature_map_5th);
                feature_map_sizes.Add(feature_map_6th);

                priors = generate_prior();
                priors_0_2 = priors.colRange(new Range(0, 2));
                priors_2_4 = priors.colRange(new Range(2, 4));
            }

            private Mat generate_prior()
            {
                int priors_size = 0;
                for (int index = 0; index < feature_map_sizes.Count; index++)
                    priors_size += (int)(feature_map_sizes[index].width * feature_map_sizes[index].height * min_sizes[index].Length);

                Mat anchors = new Mat(priors_size, 4, CvType.CV_32FC1);
                int count = 0;
                for (int i = 0; i < feature_map_sizes.Count; i++)
                {
                    Size feature_map_size = feature_map_sizes[i];
                    float[] min_size = min_sizes[i];

                    for (int _h = 0; _h < feature_map_size.height; _h++)
                    {
                        for (int _w = 0; _w < feature_map_size.width; _w++)
                        {
                            for (int j = 0; j < min_size.Length; j++)
                            {
                                float s_kx = min_size[j] / in_w;
                                float s_ky = min_size[j] / in_h;

                                float cx = (float)((_w + 0.5) * steps[i] / in_w);
                                float cy = (float)((_h + 0.5) * steps[i] / in_h);

                                anchors.put(count, 0, new float[] { cx, cy, s_kx, s_ky });

                                count++;
                            }
                        }
                    }
                }

                return anchors;
            }

            /// <summary>
            /// Decodes the locations (x1, y1, x2, y2...) and scores (c) from the priors, and the given loc and conf.
            /// </summary>
            /// <param name="loc"></param>
            /// <param name="conf"></param>
            /// <param name="iou"></param>
            /// <returns>dets is concatenated by bboxes, landmarks and scoress. num * [x1, y1, x2, y2, x_re, y_re, x_le, y_le, x_ml, y_ml, x_n, y_n, x_mr, y_ml, score]</returns>
            public Mat decode(Mat loc, Mat conf, Mat iou)
            {
                Mat loc_m = loc.reshape(1, new int[] { loc.size(1), loc.size(2) }); // [1*num*14] to [num*14]
                Mat conf_m = conf.reshape(1, new int[] { conf.size(1), conf.size(2) }); // [1*num*2] to [num*2]
                Mat iou_m = iou.reshape(1, new int[] { iou.size(1), iou.size(2) }); // [1*num*1] to [num*1]

                int num = loc_m.rows();

                if (dets == null || (dets != null && dets.IsDisposed))
                {
                    dets = new Mat(num, 15, CvType.CV_32FC1);
                    ones = Mat.ones(num, 2, CvType.CV_32FC1);
                    scale = new Mat(num, 1, CvType.CV_32FC4, new Scalar(out_w, out_h, out_w, out_h));
                    scale = scale.reshape(1, num);

                    bboxes = dets.colRange(new Range(0, 4));
                    bboxes_0_2 = bboxes.colRange(new Range(0, 2));
                    bboxes_2_4 = bboxes.colRange(new Range(2, 4));
                    landmarks = dets.colRange(new Range(4, 14));
                    landmarks_0_2 = landmarks.colRange(new Range(0, 2));
                    landmarks_2_4 = landmarks.colRange(new Range(2, 4));
                    landmarks_4_6 = landmarks.colRange(new Range(4, 6));
                    landmarks_6_8 = landmarks.colRange(new Range(6, 8));
                    landmarks_8_10 = landmarks.colRange(new Range(8, 10));
                    scores = dets.colRange(new Range(14, 15));
                    ones_0_1 = ones.colRange(0, 1);
                    ones_0_2 = ones.colRange(0, 2);
                    bbox_scale = scale.colRange(0, 4);
                    landmark_scale = scale.colRange(0, 2);
                }


                Mat loc_0_2 = loc_m.colRange(new Range(0, 2));
                Mat loc_2_4 = loc_m.colRange(new Range(2, 4));
                Mat loc_2_3 = loc_m.colRange(new Range(2, 3));
                Mat loc_3_4 = loc_m.colRange(new Range(3, 4));

                // # get bboxes
                Core.multiply(loc_0_2, priors_2_4, bboxes_0_2, variance[0]);
                Core.add(priors_0_2, bboxes_0_2, bboxes_0_2);
                Core.multiply(loc_2_3, ones_0_1, loc_2_3, variance[0]);
                Core.multiply(loc_3_4, ones_0_1, loc_3_4, variance[1]);
                Core.exp(loc_2_4, bboxes_2_4);
                Core.multiply(priors_2_4, bboxes_2_4, bboxes_2_4);

                // # (x_c, y_c, w, h) -> (x1, y1, x2, y2)
                Core.divide(bboxes_2_4, ones_0_2, loc_2_4, 0.5);
                Core.subtract(bboxes_0_2, loc_2_4, bboxes_0_2);
                Core.add(bboxes_2_4, bboxes_0_2, bboxes_2_4);

                // # scale recover
                Core.multiply(bboxes, bbox_scale, bboxes);


                Mat loc_4_6 = loc_m.colRange(new Range(4, 6));
                Mat loc_6_8 = loc_m.colRange(new Range(6, 8));
                Mat loc_8_10 = loc_m.colRange(new Range(8, 10));
                Mat loc_10_12 = loc_m.colRange(new Range(10, 12));
                Mat loc_12_14 = loc_m.colRange(new Range(12, 14));

                // # get landmarks
                Core.multiply(loc_4_6, priors_2_4, landmarks_0_2, variance[0]);
                Core.add(priors_0_2, landmarks_0_2, landmarks_0_2);
                Core.multiply(loc_6_8, priors_2_4, landmarks_2_4, variance[0]);
                Core.add(priors_0_2, landmarks_2_4, landmarks_2_4);
                Core.multiply(loc_8_10, priors_2_4, landmarks_4_6, variance[0]);
                Core.add(priors_0_2, landmarks_4_6, landmarks_4_6);
                Core.multiply(loc_10_12, priors_2_4, landmarks_6_8, variance[0]);
                Core.add(priors_0_2, landmarks_6_8, landmarks_6_8);
                Core.multiply(loc_12_14, priors_2_4, landmarks_8_10, variance[0]);
                Core.add(priors_0_2, landmarks_8_10, landmarks_8_10);

                // # scale recover
                Core.multiply(landmarks_0_2, landmark_scale, landmarks_0_2);
                Core.multiply(landmarks_2_4, landmark_scale, landmarks_2_4);
                Core.multiply(landmarks_4_6, landmark_scale, landmarks_4_6);
                Core.multiply(landmarks_6_8, landmark_scale, landmarks_6_8);
                Core.multiply(landmarks_8_10, landmark_scale, landmarks_8_10);


                // # get score
                Mat cls_scores = conf_m.colRange(new Range(1, 2));
                Mat iou_scores = iou_m;
                Imgproc.threshold(iou_scores, iou_scores, 0, 0, Imgproc.THRESH_TOZERO);
                Imgproc.threshold(iou_scores, iou_scores, 1.0, 0, Imgproc.THRESH_TRUNC);
                Core.multiply(cls_scores, iou_scores, scores);
                Core.sqrt(scores, scores);

                return dets;
            }

            public void dispose()
            {
                if (priors != null)
                    priors.Dispose();

                if (dets != null)
                    dets.Dispose();
                if (ones != null)
                    ones.Dispose();
                if (scale != null)
                    scale.Dispose();
            }
        }
    }
}
#endif

#endif