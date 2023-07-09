#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using OpenCVRange = OpenCVForUnity.CoreModule.Range;
using OpenCVRect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample.DnnModel
{
    /// <summary>
    /// Referring to https://github.com/opencv/opencv_zoo/tree/master/models/handpose_estimation_mediapipe
    /// </summary>
    public class MediaPipeHandPoseEstimator
    {
        float conf_threshold;
        int backend;
        int target;

        Net handpose_estimation_net;

        Size input_size = new Size(224, 224);
        //int[] PALM_LANDMARK_IDS = new int[] { 0, 5, 9, 13, 17, 1, 2 };
        int PALM_LANDMARKS_INDEX_OF_PALM_BASE = 0;
        int PALM_LANDMARKS_INDEX_OF_MIDDLE_FINGER_BASE = 2;
        Point PALM_BOX_PRE_SHIFT_VECTOR = new Point(0, 0);
        //double PALM_BOX_PRE_ENLARGE_FACTOR = 4.0;
        Point PALM_BOX_SHIFT_VECTOR = new Point(0, -0.4);
        double PALM_BOX_ENLARGE_FACTOR = 3.0;
        Point HAND_BOX_SHIFT_VECTOR = new Point(0, -0.1);
        double HAND_BOX_ENLARGE_FACTOR = 1.65;

        Mat tmpImage;
        Mat tmpRotatedImage;

        public MediaPipeHandPoseEstimator(string modelFilepath, float confThreshold = 0.8f, int backend = Dnn.DNN_BACKEND_OPENCV, int target = Dnn.DNN_TARGET_CPU)
        {
            // initialize
            if (!string.IsNullOrEmpty(modelFilepath))
            {
                handpose_estimation_net = Dnn.readNet(modelFilepath);
            }

            conf_threshold = Mathf.Clamp01(confThreshold);
            this.backend = backend;
            this.target = target;

            handpose_estimation_net.setPreferableBackend(this.backend);
            handpose_estimation_net.setPreferableTarget(this.target);
        }

        protected virtual Mat preprocess(Mat image, Mat palm, out Mat rotated_palm_bbox, out double angle, out Mat rotation_matrix, out Mat pad_bias)
        {
            // '''
            // Rotate input for inference.
            // Parameters:
            //  image - input image of BGR channel order
            //  palm_bbox - palm bounding box found in image of format[[x1, y1], [x2, y2]] (top - left and bottom - right points)
            //            palm_landmarks - 7 landmarks(5 finger base points, 2 palm base points) of shape[7, 2]
            // Returns:
            //        rotated_hand - rotated hand image for inference
            //        rotate_palm_bbox - palm box of interest range
            //        angle - rotate angle for hand
            //        rotation_matrix - matrix for rotation and de - rotation
            //        pad_bias - pad pixels of interest range
            // '''

            // Generate an image with padding added after the squarify process.
            int maxSize = Math.Max(image.width(), image.height());
            int tmpImageSize = (int)(maxSize * 1.5);
            if (tmpImage != null && (tmpImage.width() != tmpImageSize || tmpImage.height() != tmpImageSize))
            {
                tmpImage.Dispose();
                tmpImage = null;
                tmpRotatedImage.Dispose();
                tmpRotatedImage = null;
            }
            if (tmpImage == null)
            {
                tmpImage = new Mat(tmpImageSize, tmpImageSize, image.type(), Scalar.all(0));
                tmpRotatedImage = tmpImage.clone();
            }

            int pad = (tmpImageSize - maxSize) / 2;
            pad_bias = new Mat(2, 1, CvType.CV_32FC1);
            pad_bias.put(0, 0, new float[] { -pad, -pad });

            Mat _tmpImage_roi = new Mat(tmpImage, new OpenCVRect(pad, pad, image.width(), image.height()));
            image.copyTo(_tmpImage_roi);

            // Apply the pad_bias to palm_bbox and palm_landmarks.
            Mat new_palm = palm.clone();
            Mat palm_bbox_and_landmark = new_palm.colRange(new OpenCVRange(0, 18)).reshape(2, 9);
            Core.add(palm_bbox_and_landmark, new Scalar(pad, pad), palm_bbox_and_landmark);

            // Rotate input to have vertically oriented hand image
            // compute rotation
            Mat palm_bbox = new_palm.colRange(new OpenCVRange(0, 4)).reshape(1, 2);
            Mat palm_landmarks = new_palm.colRange(new OpenCVRange(4, 18)).reshape(1, 7);

            Mat p1 = palm_landmarks.row(PALM_LANDMARKS_INDEX_OF_PALM_BASE);
            Mat p2 = palm_landmarks.row(PALM_LANDMARKS_INDEX_OF_MIDDLE_FINGER_BASE);
            float[] p1_arr = new float[2];
            p1.get(0, 0, p1_arr);
            float[] p2_arr = new float[2];
            p2.get(0, 0, p2_arr);
            double radians = Math.PI / 2 - Math.Atan2(-(p2_arr[1] - p1_arr[1]), p2_arr[0] - p1_arr[0]);
            radians = radians - 2 * Math.PI * Math.Floor((radians + Math.PI) / (2 * Math.PI));
            angle = Mathf.Rad2Deg * radians;

            // get bbox center
            float[] palm_bbox_arr = new float[4];
            palm_bbox.get(0, 0, palm_bbox_arr);
            Point center_palm_bbox = new Point((palm_bbox_arr[0] + palm_bbox_arr[2]) / 2, (palm_bbox_arr[1] + palm_bbox_arr[3]) / 2);

            // get rotation matrix
            rotation_matrix = Imgproc.getRotationMatrix2D(center_palm_bbox, angle, 1.0);

            // get bounding boxes from rotated palm landmarks
            Mat rotated_palm_landmarks = new Mat(2, 7, CvType.CV_32FC1);
            Mat _a = new Mat(1, 3, CvType.CV_64FC1);
            Mat _b = new Mat(1, 3, CvType.CV_64FC1);
            float[] _a_arr = new float[2];
            double[] _b_arr = new double[3];

            Point[] rotated_palm_landmarks_points = new Point[7];

            for (int i = 0; i < 7; ++i)
            {
                palm_landmarks.get(i, 0, _a_arr);
                _a.put(0, 0, new double[] { _a_arr[0], _a_arr[1], 1f });
                rotation_matrix.get(0, 0, _b_arr);
                _b.put(0, 0, new double[] { _b_arr[0], _b_arr[1], _b_arr[2] });
                double x = _a.dot(_b);
                rotated_palm_landmarks.put(0, i, new float[] { (float)x });

                rotation_matrix.get(1, 0, _b_arr);
                _b.put(0, 0, new double[] { _b_arr[0], _b_arr[1], _b_arr[2] });
                double y = _a.dot(_b);
                rotated_palm_landmarks.put(1, i, new float[] { (float)y });

                rotated_palm_landmarks_points[i] = new Point(x, y);
            }

            // get landmark bounding box
            MatOfPoint points = new MatOfPoint(rotated_palm_landmarks_points);
            OpenCVRect _rotated_palm_bbox = Imgproc.boundingRect(points);
            rotated_palm_bbox = new Mat(2, 2, CvType.CV_64FC1);

            // shift bounding box
            Point _rotated_palm_bbox_tl = _rotated_palm_bbox.tl();
            Point _rotated_palm_bbox_br = _rotated_palm_bbox.br();
            Point wh_rotated_palm_bbox = _rotated_palm_bbox_br - _rotated_palm_bbox_tl;
            Point shift_vector = new Point(PALM_BOX_SHIFT_VECTOR.x * wh_rotated_palm_bbox.x, PALM_BOX_SHIFT_VECTOR.y * wh_rotated_palm_bbox.y);
            _rotated_palm_bbox_tl = _rotated_palm_bbox_tl + shift_vector;
            _rotated_palm_bbox_br = _rotated_palm_bbox_br + shift_vector;

            // squarify bounding boxx
            Point center_rotated_plam_bbox = new Point((_rotated_palm_bbox_tl.x + _rotated_palm_bbox_br.x) / 2, (_rotated_palm_bbox_tl.y + _rotated_palm_bbox_br.y) / 2);
            wh_rotated_palm_bbox = _rotated_palm_bbox_br - _rotated_palm_bbox_tl;
            double new_half_size = Math.Max(wh_rotated_palm_bbox.x, wh_rotated_palm_bbox.y) / 2.0;
            _rotated_palm_bbox_tl = new Point(center_rotated_plam_bbox.x - new_half_size, center_rotated_plam_bbox.y - new_half_size);
            _rotated_palm_bbox_br = new Point(center_rotated_plam_bbox.x + new_half_size, center_rotated_plam_bbox.y + new_half_size);

            // enlarge bounding box
            center_rotated_plam_bbox = new Point((_rotated_palm_bbox_tl.x + _rotated_palm_bbox_br.x) / 2, (_rotated_palm_bbox_tl.y + _rotated_palm_bbox_br.y) / 2);
            wh_rotated_palm_bbox = _rotated_palm_bbox_br - _rotated_palm_bbox_tl;
            Point new_half_size2 = new Point(wh_rotated_palm_bbox.x * PALM_BOX_ENLARGE_FACTOR / 2.0, wh_rotated_palm_bbox.y * PALM_BOX_ENLARGE_FACTOR / 2.0);
            OpenCVRect _rotated_palm_bbox_rect = new OpenCVRect((int)(center_rotated_plam_bbox.x - new_half_size2.x), (int)(center_rotated_plam_bbox.y - new_half_size2.y)
                , (int)(new_half_size2.x * 2), (int)(new_half_size2.y * 2));
            _rotated_palm_bbox_tl = _rotated_palm_bbox_rect.tl();
            _rotated_palm_bbox_br = _rotated_palm_bbox_rect.br();
            rotated_palm_bbox.put(0, 0, new double[] { _rotated_palm_bbox_tl.x, _rotated_palm_bbox_tl.y, _rotated_palm_bbox_br.x, _rotated_palm_bbox_br.y });

            // crop bounding box
            int[] diff = new int[] {
                    Math.Max((int)-_rotated_palm_bbox_tl.x, 0),
                    Math.Max((int)-_rotated_palm_bbox_tl.y, 0),
                    Math.Max((int)_rotated_palm_bbox_br.x - tmpRotatedImage.width(), 0),
                    Math.Max((int)_rotated_palm_bbox_br.y - tmpRotatedImage.height(), 0)
                };
            Point tl = new Point(_rotated_palm_bbox_tl.x + diff[0], _rotated_palm_bbox_tl.y + diff[1]);
            Point br = new Point(_rotated_palm_bbox_br.x + diff[2], _rotated_palm_bbox_br.y + diff[3]);
            OpenCVRect rotated_palm_bbox_rect = new OpenCVRect(tl, br);
            OpenCVRect rotated_image_rect = new OpenCVRect(0, 0, tmpRotatedImage.width(), tmpRotatedImage.height());

            // get rotated image
            OpenCVRect warp_roi_rect = rotated_image_rect.intersect(rotated_palm_bbox_rect);
            Mat _tmpImage_warp_roi = new Mat(tmpImage, warp_roi_rect);
            Mat _tmpRotatedImage_warp_roi = new Mat(tmpRotatedImage, warp_roi_rect);
            Point warp_roi_center_palm_bbox = center_palm_bbox - warp_roi_rect.tl();
            Mat warp_roi_rotation_matrix = Imgproc.getRotationMatrix2D(warp_roi_center_palm_bbox, angle, 1.0);
            Imgproc.warpAffine(_tmpImage_warp_roi, _tmpRotatedImage_warp_roi, warp_roi_rotation_matrix, _tmpImage_warp_roi.size());

            // get rotated_palm_bbox-size rotated image
            OpenCVRect crop_rect = rotated_image_rect.intersect(
                new OpenCVRect(0, 0, (int)_rotated_palm_bbox_br.x - (int)_rotated_palm_bbox_tl.x, (int)_rotated_palm_bbox_br.y - (int)_rotated_palm_bbox_tl.y));
            Mat _tmpImage_crop_roi = new Mat(tmpImage, crop_rect);
            Imgproc.rectangle(_tmpImage_crop_roi, new OpenCVRect(0, 0, _tmpImage_crop_roi.width(), _tmpImage_crop_roi.height()), Scalar.all(0), -1);
            OpenCVRect crop2_rect = rotated_image_rect.intersect(new OpenCVRect(diff[0], diff[1], _tmpRotatedImage_warp_roi.width(), _tmpRotatedImage_warp_roi.height()));
            Mat _tmpImage_crop2_roi = new Mat(tmpImage, crop2_rect);
            if (_tmpRotatedImage_warp_roi.size() == _tmpImage_crop2_roi.size())
                _tmpRotatedImage_warp_roi.copyTo(_tmpImage_crop2_roi);


            Mat blob = Dnn.blobFromImage(_tmpImage_crop_roi, 1.0 / 255.0, input_size, new Scalar(0, 0, 0), true, false, CvType.CV_32F);

            // NCHW => NHWC
            Core.transposeND(blob, new MatOfInt(0, 2, 3, 1), blob);

            new_palm.Dispose();

            return blob;
        }

        public virtual Mat infer(Mat image, Mat palm)
        {
            // Preprocess
            Mat rotated_palm_bbox;
            double angle;
            Mat rotation_matrix;
            Mat pad_bias;
            Mat input_blob = preprocess(image, palm, out rotated_palm_bbox, out angle, out rotation_matrix, out pad_bias);

            // Forward
            handpose_estimation_net.setInput(input_blob);
            List<Mat> output_blob = new List<Mat>();
            handpose_estimation_net.forward(output_blob, handpose_estimation_net.getUnconnectedOutLayersNames());

            // Postprocess
            Mat results = postprocess(output_blob, rotated_palm_bbox, angle, rotation_matrix, pad_bias);

            input_blob.Dispose();
            for (int i = 0; i < output_blob.Count; i++)
            {
                output_blob[i].Dispose();
            }

            return results;// [bbox_coords, landmarks_coords, landmarks_coords_world, handedness, conf]
        }

        protected virtual Mat postprocess(List<Mat> output_blob, Mat rotated_palm_bbox, double angle, Mat rotation_matrix, Mat pad_bias)
        {
            Mat landmarks = output_blob[0];
            float conf = (float)output_blob[1].get(0, 0)[0];
            float handedness = (float)output_blob[2].get(0, 0)[0];
            Mat landmarks_world = output_blob[3];

            if (conf < conf_threshold)
                return new Mat();

            landmarks = landmarks.reshape(1, 21); // shape: (1, 63) -> (21, 3)
            landmarks_world = landmarks_world.reshape(1, 21); // shape: (1, 63) -> (21, 3)

            // transform coords back to the input coords
            double[] rotated_palm_bbox_arr = new double[4];
            rotated_palm_bbox.get(0, 0, rotated_palm_bbox_arr);
            Point _rotated_palm_bbox_tl = new Point(rotated_palm_bbox_arr[0], rotated_palm_bbox_arr[1]);
            Point _rotated_palm_bbox_br = new Point(rotated_palm_bbox_arr[2], rotated_palm_bbox_arr[3]);
            Point wh_rotated_palm_bbox = _rotated_palm_bbox_br - _rotated_palm_bbox_tl;
            Point scale_factor = new Point(wh_rotated_palm_bbox.x / input_size.width, wh_rotated_palm_bbox.y / input_size.height);

            Mat _landmarks_21x1_c3 = landmarks.reshape(3, 21);
            Core.subtract(_landmarks_21x1_c3, new Scalar(input_size.width / 2.0, input_size.height / 2.0, 0.0), _landmarks_21x1_c3);
            double max_scale_factor = Math.Max(scale_factor.x, scale_factor.y);
            Core.multiply(_landmarks_21x1_c3, new Scalar(scale_factor.x, scale_factor.y, max_scale_factor), _landmarks_21x1_c3); //  # depth scaling

            Mat coords_rotation_matrix = Imgproc.getRotationMatrix2D(new Point(0, 0), angle, 1.0);

            Mat rotated_landmarks = landmarks.clone();
            Mat _a = new Mat(1, 2, CvType.CV_64FC1);
            Mat _b = new Mat(1, 2, CvType.CV_64FC1);
            float[] _a_arr = new float[2];
            double[] _b_arr = new double[6];
            coords_rotation_matrix.get(0, 0, _b_arr);

            for (int i = 0; i < 21; ++i)
            {
                landmarks.get(i, 0, _a_arr);
                _a.put(0, 0, new double[] { _a_arr[0], _a_arr[1] });

                _b.put(0, 0, new double[] { _b_arr[0], _b_arr[3] });
                rotated_landmarks.put(i, 0, new float[] { (float)_a.dot(_b) });
                _b.put(0, 0, new double[] { _b_arr[1], _b_arr[4] });
                rotated_landmarks.put(i, 1, new float[] { (float)_a.dot(_b) });
            }

            Mat rotated_landmarks_world = landmarks_world.clone();
            for (int i = 0; i < 21; ++i)
            {
                landmarks_world.get(i, 0, _a_arr);
                _a.put(0, 0, new double[] { _a_arr[0], _a_arr[1] });

                _b.put(0, 0, new double[] { _b_arr[0], _b_arr[3] });
                rotated_landmarks_world.put(i, 0, new float[] { (float)_a.dot(_b) });
                _b.put(0, 0, new double[] { _b_arr[1], _b_arr[4] });
                rotated_landmarks_world.put(i, 1, new float[] { (float)_a.dot(_b) });
            }

            // invert rotation
            double[] rotation_matrix_arr = new double[6];
            rotation_matrix.get(0, 0, rotation_matrix_arr);
            Mat rotation_component = new Mat(2, 2, CvType.CV_64FC1);
            rotation_component.put(0, 0, new double[] { rotation_matrix_arr[0], rotation_matrix_arr[3], rotation_matrix_arr[1], rotation_matrix_arr[4] });
            Mat translation_component = new Mat(2, 1, CvType.CV_64FC1);
            translation_component.put(0, 0, new double[] { rotation_matrix_arr[2], rotation_matrix_arr[5] });
            Mat inverted_translation = new Mat(2, 1, CvType.CV_64FC1);
            inverted_translation.put(0, 0, new double[] { -rotation_component.row(0).dot(translation_component.reshape(1, 1)), -rotation_component.row(1).dot(translation_component.reshape(1, 1)) });

            Mat inverse_rotation_matrix = new Mat(2, 3, CvType.CV_64FC1);
            rotation_component.copyTo(inverse_rotation_matrix.colRange(new OpenCVRange(0, 2)));
            inverted_translation.copyTo(inverse_rotation_matrix.colRange(new OpenCVRange(2, 3)));

            // get box center
            Mat center = new Mat(3, 1, CvType.CV_64FC1);
            center.put(0, 0, new double[] { (rotated_palm_bbox_arr[0] + rotated_palm_bbox_arr[2]) / 2.0, (rotated_palm_bbox_arr[1] + rotated_palm_bbox_arr[3]) / 2.0, 1.0 });
            Mat original_center = new Mat(2, 1, CvType.CV_64FC1);
            original_center.put(0, 0, new double[] { inverse_rotation_matrix.row(0).dot(center.reshape(1, 1)), inverse_rotation_matrix.row(1).dot(center.reshape(1, 1)) });

            Core.add(rotated_landmarks.reshape(3, 21)
                , new Scalar(original_center.get(0, 0)[0] + pad_bias.get(0, 0)[0], original_center.get(1, 0)[0] + pad_bias.get(1, 0)[0], 0.0)
                , landmarks.reshape(3, 21));

            // get bounding box from rotated_landmarks
            Point[] landmarks_points = new Point[21];
            for (int i = 0; i < 21; ++i)
            {
                landmarks.get(i, 0, _a_arr);
                landmarks_points[i] = new Point(_a_arr[0], _a_arr[1]);
            }
            MatOfPoint points = new MatOfPoint(landmarks_points);
            OpenCVRect bbox = Imgproc.boundingRect(points);

            // shift bounding box
            Point wh_bbox = bbox.br() - bbox.tl();
            Point shift_vector = new Point(HAND_BOX_SHIFT_VECTOR.x * wh_bbox.x, HAND_BOX_SHIFT_VECTOR.y * wh_bbox.y);
            bbox = bbox + shift_vector;

            // enlarge bounding box
            Point center_bbox = new Point((bbox.tl().x + bbox.br().x) / 2, (bbox.tl().y + bbox.br().y) / 2);
            wh_bbox = bbox.br() - bbox.tl();
            Point new_half_size = new Point(wh_bbox.x * HAND_BOX_ENLARGE_FACTOR / 2.0, wh_bbox.y * HAND_BOX_ENLARGE_FACTOR / 2.0);
            bbox = new OpenCVRect(new Point(center_bbox.x - new_half_size.x, center_bbox.y - new_half_size.y), new Point(center_bbox.x + new_half_size.x, center_bbox.y + new_half_size.y));


            Mat results = new Mat(132, 1, CvType.CV_32FC1);
            results.put(0, 0, new float[] { (float)bbox.tl().x, (float)bbox.tl().y, (float)bbox.br().x, (float)bbox.br().y });
            Mat results_col4_67_21x3 = results.rowRange(new OpenCVRange(4, 67)).reshape(1, 21);
            landmarks.colRange(new OpenCVRange(0, 3)).copyTo(results_col4_67_21x3);
            Mat results_col67_130_21x3 = results.rowRange(new OpenCVRange(67, 130)).reshape(1, 21);
            rotated_landmarks_world.colRange(new OpenCVRange(0, 3)).copyTo(results_col67_130_21x3);
            results.put(130, 0, new float[] { handedness });
            results.put(131, 0, new float[] { conf });

            // # [0: 4]: hand bounding box found in image of format [x1, y1, x2, y2] (top-left and bottom-right points)
            // # [4: 67]: screen landmarks with format [x1, y1, z1, x2, y2 ... x21, y21, z21], z value is relative to WRIST
            // # [67: 130]: world landmarks with format [x1, y1, z1, x2, y2 ... x21, y21, z21], 3D metric x, y, z coordinate
            // # [130]: handedness, (left)[0, 1](right) hand
            // # [131]: confidence
            return results;//np.r_[bbox.reshape(-1), landmarks.reshape(-1), rotated_landmarks_world.reshape(-1), handedness[0][0], conf]
        }

        public virtual void visualize(Mat image, List<Mat> results, bool print_results = false, bool isRGB = false)
        {
            if (image.IsDisposed)
                return;

            if (results.Count < 1)
                return;

            StringBuilder sb = null;

            if (print_results)
                sb = new StringBuilder();

            Scalar line_color = new Scalar(255, 255, 255, 255);
            Scalar point_color = (isRGB) ? new Scalar(255, 0, 0, 255) : new Scalar(0, 0, 255, 255);
            for (int i = 0; i < results.Count; ++i)
            {
                Mat result = results[i];

                if (result.empty() || result.rows() < 132)
                    continue;

                float[] conf = new float[1];
                result.get(131, 0, conf);
                float[] handedness = new float[1];
                result.get(130, 0, handedness);
                string handedness_text = (handedness[0] <= 0.5f) ? "Left" : "Right";
                float[] bbox = new float[4];
                result.get(0, 0, bbox);

                Mat results_col4_67_21x3 = result.rowRange(new OpenCVRange(4, 67)).reshape(1, 21);
                float[] landmarks_screen_xy = new float[42];
                results_col4_67_21x3.colRange(new OpenCVRange(0, 2)).get(0, 0, landmarks_screen_xy);

                float[] landmarks_screen_xyz = new float[63];
                results_col4_67_21x3.get(0, 0, landmarks_screen_xyz);

                Mat results_col67_130_21x3 = result.rowRange(new OpenCVRange(67, 130)).reshape(1, 21);
                float[] landmarks_world = new float[63];
                results_col67_130_21x3.get(0, 0, landmarks_world);

                // # draw box
                Imgproc.rectangle(image, new Point(bbox[0], bbox[1]), new Point(bbox[2], bbox[3]), new Scalar(0, 255, 0, 255), 2);

                // # draw handedness
                Imgproc.putText(image, handedness_text, new Point(bbox[0], bbox[1] + 12), Imgproc.FONT_HERSHEY_DUPLEX, 0.5, point_color);

                // # Draw line between each key points
                draw_lines(landmarks_screen_xy, false);

                // # z value is relative to WRIST
                for (int j = 0; j < 63; j += 3)
                {
                    int r = Mathf.Max((int)(5 - landmarks_screen_xyz[j + 2] / 5), 0);
                    r = Mathf.Min(r, 14);
                    Imgproc.circle(image, new Point(landmarks_screen_xyz[j], landmarks_screen_xyz[j + 1]), r, point_color, -1);
                }


                // Print results
                if (print_results)
                {
                    sb.AppendLine(String.Format("-----------hand {0}-----------", i + 1));
                    sb.AppendLine(String.Format("conf: {0:0.00}", conf[0]));
                    sb.AppendLine("handedness: " + handedness_text);
                    sb.AppendLine(String.Format("hand box: {0:0} {1:0} {2:0} {3:0}", bbox[0], bbox[1], bbox[2], bbox[3]));
                    sb.AppendLine("hand landmarks: ");
                    foreach (var p in landmarks_screen_xyz)
                    {
                        sb.Append(String.Format("{0:0} ", p));
                    }
                    sb.AppendLine();
                    sb.AppendLine("hand world landmarks: ");
                    foreach (var p in landmarks_world)
                    {
                        sb.Append(String.Format("{0:0.000000} ", p));
                    }
                }
            }

            if (print_results)
                Debug.Log(sb);


            void draw_lines(float[] landmarks, bool is_draw_point = true, int thickness = 2)
            {
                // Draw line between each key points
                Imgproc.line(image, new Point(landmarks[0], landmarks[1]), new Point(landmarks[2], landmarks[3]), line_color, thickness);
                Imgproc.line(image, new Point(landmarks[2], landmarks[3]), new Point(landmarks[4], landmarks[5]), line_color, thickness);
                Imgproc.line(image, new Point(landmarks[4], landmarks[5]), new Point(landmarks[6], landmarks[7]), line_color, thickness);
                Imgproc.line(image, new Point(landmarks[6], landmarks[7]), new Point(landmarks[8], landmarks[9]), line_color, thickness);

                Imgproc.line(image, new Point(landmarks[0], landmarks[1]), new Point(landmarks[10], landmarks[11]), line_color, thickness);
                Imgproc.line(image, new Point(landmarks[10], landmarks[11]), new Point(landmarks[12], landmarks[13]), line_color, thickness);
                Imgproc.line(image, new Point(landmarks[12], landmarks[13]), new Point(landmarks[14], landmarks[15]), line_color, thickness);
                Imgproc.line(image, new Point(landmarks[14], landmarks[15]), new Point(landmarks[16], landmarks[17]), line_color, thickness);

                Imgproc.line(image, new Point(landmarks[0], landmarks[1]), new Point(landmarks[18], landmarks[19]), line_color, thickness);
                Imgproc.line(image, new Point(landmarks[18], landmarks[19]), new Point(landmarks[20], landmarks[21]), line_color, thickness);
                Imgproc.line(image, new Point(landmarks[20], landmarks[21]), new Point(landmarks[22], landmarks[23]), line_color, thickness);
                Imgproc.line(image, new Point(landmarks[22], landmarks[23]), new Point(landmarks[24], landmarks[25]), line_color, thickness);

                Imgproc.line(image, new Point(landmarks[0], landmarks[1]), new Point(landmarks[26], landmarks[27]), line_color, thickness);
                Imgproc.line(image, new Point(landmarks[26], landmarks[27]), new Point(landmarks[28], landmarks[29]), line_color, thickness);
                Imgproc.line(image, new Point(landmarks[28], landmarks[29]), new Point(landmarks[30], landmarks[31]), line_color, thickness);
                Imgproc.line(image, new Point(landmarks[30], landmarks[31]), new Point(landmarks[32], landmarks[33]), line_color, thickness);

                Imgproc.line(image, new Point(landmarks[0], landmarks[1]), new Point(landmarks[34], landmarks[35]), line_color, thickness);
                Imgproc.line(image, new Point(landmarks[34], landmarks[35]), new Point(landmarks[36], landmarks[37]), line_color, thickness);
                Imgproc.line(image, new Point(landmarks[36], landmarks[37]), new Point(landmarks[38], landmarks[39]), line_color, thickness);
                Imgproc.line(image, new Point(landmarks[38], landmarks[39]), new Point(landmarks[40], landmarks[41]), line_color, thickness);

                if (is_draw_point)
                {
                    for (int j = 0; j < 42; j += 2)
                    {
                        Imgproc.circle(image, new Point(landmarks[j], landmarks[j + 1]), 2, point_color, -1);
                    }
                }
            }
        }

        public virtual void dispose()
        {
            if (handpose_estimation_net != null)
                handpose_estimation_net.Dispose();

            if (tmpImage != null)
                tmpImage.Dispose();

            if (tmpRotatedImage != null)
                tmpRotatedImage.Dispose();
        }
    }
}
#endif