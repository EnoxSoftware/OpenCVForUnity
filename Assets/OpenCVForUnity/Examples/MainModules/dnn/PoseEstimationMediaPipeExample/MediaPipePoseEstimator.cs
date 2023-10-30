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
    /// Referring to https://github.com/opencv/opencv_zoo/tree/main/models/pose_estimation_mediapipe
    /// </summary>
    public class MediaPipePoseEstimator
    {
        float conf_threshold;
        int backend;
        int target;

        Net pose_estimation_net;

        Size input_size = new Size(256, 256);

        // # RoI will be larger so the performance will be better, but preprocess will be slower. Default to 1.
        double PERSON_BOX_PRE_ENLARGE_FACTOR = 1.0;
        double PERSON_BOX_ENLARGE_FACTOR = 1.25;

        public Mat tmpImage;
        Mat tmpRotatedImage;

        Mat mask_warp;
        Mat invert_rotation_mask_32F;

        Mat colorMat;

        public MediaPipePoseEstimator(string modelFilepath, float confThreshold = 0.5f, int backend = Dnn.DNN_BACKEND_OPENCV, int target = Dnn.DNN_TARGET_CPU)
        {
            // initialize
            if (!string.IsNullOrEmpty(modelFilepath))
            {
                pose_estimation_net = Dnn.readNet(modelFilepath);
            }

            conf_threshold = Mathf.Clamp01(confThreshold);
            this.backend = backend;
            this.target = target;

            pose_estimation_net.setPreferableBackend(this.backend);
            pose_estimation_net.setPreferableTarget(this.target);
        }

        protected virtual Mat preprocess(Mat image, Mat person, out Mat rotated_person_bbox, out double angle, out Mat rotation_matrix, out Mat pad_bias)
        {
            // '''
            // Rotate input for inference.
            // Parameters:
            //   image - input image of BGR channel order
            //   face_bbox - human face bounding box found in image of format[[x1, y1], [x2, y2]] (top - left and bottom - right points)
            //   person_landmarks - 4 landmarks(2 full body points, 2 upper body points) of shape[4, 2]
            // Returns:
            //   rotated_person - rotated person image for inference
            //   rotate_person_bbox - person box of interest range
            //   angle - rotate angle for person
            //   rotation_matrix - matrix for rotation and de - rotation
            //   pad_bias - pad pixels of interest range
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

            // Apply the pad_bias to person_bbox and person_landmarks.
            Mat new_person = person.clone();
            Mat person_bbox_and_landmark = new_person.colRange(new OpenCVRange(0, 12)).reshape(2, 6);
            Core.add(person_bbox_and_landmark, new Scalar(pad, pad), person_bbox_and_landmark);

            // # crop and pad image to interest range
            float[] person_keypoints = new float[8];
            person_bbox_and_landmark.get(2, 0, person_keypoints);
            Point mid_hip_point = new Point(person_keypoints[0], person_keypoints[1]);
            Point full_body_point = new Point(person_keypoints[2], person_keypoints[3]);

            // # get RoI
            Mat full_body_vector = new Mat(1, 1, CvType.CV_32FC2, new Scalar(mid_hip_point.x - full_body_point.x, mid_hip_point.y - full_body_point.y));
            double full_dist = Core.norm(full_body_vector);
            OpenCVRect full_bbox_rect = new OpenCVRect(
                new Point((float)(mid_hip_point.x - full_dist), (float)(mid_hip_point.y - full_dist)),
                new Point((float)(mid_hip_point.x + full_dist), (float)(mid_hip_point.y + full_dist)));

            // # enlarge to make sure full body can be cover
            Point center_bbox = mid_hip_point;
            Point wh_bbox = full_bbox_rect.br() - full_bbox_rect.tl();
            Point new_half_size = wh_bbox * PERSON_BOX_PRE_ENLARGE_FACTOR / 2;
            full_bbox_rect = new OpenCVRect(
                center_bbox - new_half_size,
                center_bbox + new_half_size);

            // Rotate input to have vertically oriented person image
            // compute rotation
            Mat p1 = person_bbox_and_landmark.row(2); // mid_hip_point
            Mat p2 = person_bbox_and_landmark.row(3); // full_body_point
            float[] p1_arr = new float[2];
            p1.get(0, 0, p1_arr);
            float[] p2_arr = new float[2];
            p2.get(0, 0, p2_arr);
            double radians = Math.PI / 2 - Math.Atan2(-(p2_arr[1] - p1_arr[1]), p2_arr[0] - p1_arr[0]);
            radians = radians - 2 * Math.PI * Math.Floor((radians + Math.PI) / (2 * Math.PI));
            angle = Mathf.Rad2Deg * radians;

            // get rotation matrix
            rotation_matrix = Imgproc.getRotationMatrix2D(center_bbox, angle, 1.0);

            // # get landmark bounding box
            Point _rotated_person_bbox_tl = full_bbox_rect.tl();
            Point _rotated_person_bbox_br = full_bbox_rect.br();
            rotated_person_bbox = new Mat(2, 2, CvType.CV_64FC1);
            rotated_person_bbox.put(0, 0, new double[] { _rotated_person_bbox_tl.x, _rotated_person_bbox_tl.y, _rotated_person_bbox_br.x, _rotated_person_bbox_br.y });

            // crop bounding box
            int[] diff = new int[] {
                    Math.Max((int)-_rotated_person_bbox_tl.x, 0),
                    Math.Max((int)-_rotated_person_bbox_tl.y, 0),
                    Math.Max((int)_rotated_person_bbox_br.x - tmpRotatedImage.width(), 0),
                    Math.Max((int)_rotated_person_bbox_br.y - tmpRotatedImage.height(), 0)
                };
            Point tl = new Point(_rotated_person_bbox_tl.x + diff[0], _rotated_person_bbox_tl.y + diff[1]);
            Point br = new Point(_rotated_person_bbox_br.x + diff[2], _rotated_person_bbox_br.y + diff[3]);
            OpenCVRect rotated_person_bbox_rect = new OpenCVRect(tl, br);
            OpenCVRect rotated_image_rect = new OpenCVRect(0, 0, tmpRotatedImage.width(), tmpRotatedImage.height());

            // get rotated image
            OpenCVRect warp_roi_rect = rotated_image_rect.intersect(rotated_person_bbox_rect);
            Mat _tmpImage_warp_roi = new Mat(tmpImage, warp_roi_rect);
            Mat _tmpRotatedImage_warp_roi = new Mat(tmpRotatedImage, warp_roi_rect);
            Point warp_roi_center_palm_bbox = center_bbox - warp_roi_rect.tl();
            Mat warp_roi_rotation_matrix = Imgproc.getRotationMatrix2D(warp_roi_center_palm_bbox, angle, 1.0);
            Imgproc.warpAffine(_tmpImage_warp_roi, _tmpRotatedImage_warp_roi, warp_roi_rotation_matrix, _tmpImage_warp_roi.size());

            // get rotated_person_bbox-size rotated image
            OpenCVRect crop_rect = rotated_image_rect.intersect(
                new OpenCVRect(0, 0, (int)_rotated_person_bbox_br.x - (int)_rotated_person_bbox_tl.x, (int)_rotated_person_bbox_br.y - (int)_rotated_person_bbox_tl.y));
            Mat _tmpImage_crop_roi = new Mat(tmpImage, crop_rect);
            Imgproc.rectangle(_tmpImage_crop_roi, new OpenCVRect(0, 0, _tmpImage_crop_roi.width(), _tmpImage_crop_roi.height()), Scalar.all(0), -1);
            OpenCVRect crop2_rect = rotated_image_rect.intersect(new OpenCVRect(diff[0], diff[1], _tmpRotatedImage_warp_roi.width(), _tmpRotatedImage_warp_roi.height()));
            Mat _tmpImage_crop2_roi = new Mat(tmpImage, crop2_rect);
            if (_tmpRotatedImage_warp_roi.size() == _tmpImage_crop2_roi.size())
                _tmpRotatedImage_warp_roi.copyTo(_tmpImage_crop2_roi);


            Mat blob = Dnn.blobFromImage(_tmpImage_crop_roi, 1.0 / 255.0, input_size, new Scalar(0, 0, 0), true, false, CvType.CV_32F);

            // NCHW => NHWC
            Core.transposeND(blob, new MatOfInt(0, 2, 3, 1), blob);

            new_person.Dispose();

            return blob;
        }

        public virtual List<Mat> infer(Mat image, Mat person, bool mask = false, bool heatmap = false)
        {
            // Preprocess
            Mat rotated_person_bbox;
            double angle;
            Mat rotation_matrix;
            Mat pad_bias;
            Mat input_blob = preprocess(image, person, out rotated_person_bbox, out angle, out rotation_matrix, out pad_bias);

            // Forward
            pose_estimation_net.setInput(input_blob);
            List<Mat> output_blob = new List<Mat>();
            pose_estimation_net.forward(output_blob, pose_estimation_net.getUnconnectedOutLayersNames());

            // Postprocess
            List<Mat> results = new List<Mat>();
            Mat box_landmark_conf = postprocess(output_blob, rotated_person_bbox, angle, rotation_matrix, pad_bias, image.size());
            results.Add(box_landmark_conf);

            if (mask)
            {
                Mat invert_rotation_mask = postprocess_mask(output_blob, rotated_person_bbox, angle, rotation_matrix, pad_bias, image.size());
                results.Add(invert_rotation_mask);
            }
            else
            {
                results.Add(new Mat());
            }

            if (heatmap)
            {
                // # 64*64*39 heatmap: currently only used for refining landmarks, requires sigmod processing before use
                // # TODO: refine landmarks with heatmap. reference: https://github.com/tensorflow/tfjs-models/blob/master/pose-detection/src/blazepose_tfjs/detector.ts#L577-L582
                results.Add(output_blob[3].reshape(1, new int[] { 64, 64, 39 }).clone()); // shape: (1, 64, 64, 39) -> (64, 64, 39)
            }
            else
            {
                results.Add(new Mat());
            }


            input_blob.Dispose();
            for (int i = 0; i < output_blob.Count; i++)
            {
                output_blob[i].Dispose();
            }

            // results[0] = [bbox_coords, landmarks_coords, landmarks_coords_world, conf]
            // results[1] = (optional) [invert_rotation_mask]
            // results[2] = (optional) [heatmap]
            return results;
        }

        protected virtual Mat postprocess(List<Mat> output_blob, Mat rotated_person_bbox, double angle, Mat rotation_matrix, Mat pad_bias, Size img_size)
        {
            Mat landmarks = output_blob[0];
            float conf = (float)output_blob[1].get(0, 0)[0];
            Mat landmarks_world = output_blob[4];

            if (conf < conf_threshold)
                return new Mat();

            landmarks = landmarks.reshape(1, 39); // shape: (1, 195) -> (39, 5)
            landmarks_world = landmarks_world.reshape(1, 39); // shape: (1, 117) -> (39, 3)

            // # recover sigmoid score
            Mat _ladmarls_col3_5 = landmarks.colRange(new OpenCVRange(3, 5));
            sigmoid(_ladmarls_col3_5);

            Mat _ladmarks_col0_3 = landmarks.colRange(new OpenCVRange(0, 3)).clone();

            // transform coords back to the input coords
            double[] rotated_person_bbox_arr = new double[4];
            rotated_person_bbox.get(0, 0, rotated_person_bbox_arr);
            Point _rotated_palm_bbox_tl = new Point(rotated_person_bbox_arr[0], rotated_person_bbox_arr[1]);
            Point _rotated_palm_bbox_br = new Point(rotated_person_bbox_arr[2], rotated_person_bbox_arr[3]);
            Point wh_rotated_person_bbox = _rotated_palm_bbox_br - _rotated_palm_bbox_tl;
            Point scale_factor = new Point(wh_rotated_person_bbox.x / input_size.width, wh_rotated_person_bbox.y / input_size.height);

            Mat _landmarks_39x1_c3 = _ladmarks_col0_3.reshape(3, 39);
            Core.subtract(_landmarks_39x1_c3, new Scalar(input_size.width / 2.0, input_size.height / 2.0, 0.0), _landmarks_39x1_c3);
            double max_scale_factor = Math.Max(scale_factor.x, scale_factor.y);
            Core.multiply(_landmarks_39x1_c3, new Scalar(scale_factor.x, scale_factor.y, max_scale_factor), _landmarks_39x1_c3); //  # depth scaling

            _ladmarks_col0_3.copyTo(landmarks.colRange(new OpenCVRange(0, 3)));

            Mat coords_rotation_matrix = Imgproc.getRotationMatrix2D(new Point(0, 0), angle, 1.0);

            Mat rotated_landmarks = landmarks.clone();
            Mat _a = new Mat(1, 2, CvType.CV_64FC1);
            Mat _b = new Mat(1, 2, CvType.CV_64FC1);
            float[] _a_arr = new float[2];
            double[] _b_arr = new double[6];
            coords_rotation_matrix.get(0, 0, _b_arr);

            for (int i = 0; i < 39; ++i)
            {
                landmarks.get(i, 0, _a_arr);
                _a.put(0, 0, new double[] { _a_arr[0], _a_arr[1] });

                _b.put(0, 0, new double[] { _b_arr[0], _b_arr[3] });
                rotated_landmarks.put(i, 0, new float[] { (float)_a.dot(_b) });
                _b.put(0, 0, new double[] { _b_arr[1], _b_arr[4] });
                rotated_landmarks.put(i, 1, new float[] { (float)_a.dot(_b) });
            }

            Mat rotated_landmarks_world = landmarks_world.clone();
            for (int i = 0; i < 39; ++i)
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
            center.put(0, 0, new double[] { (rotated_person_bbox_arr[0] + rotated_person_bbox_arr[2]) / 2.0, (rotated_person_bbox_arr[1] + rotated_person_bbox_arr[3]) / 2.0, 1.0 });
            Mat original_center = new Mat(2, 1, CvType.CV_64FC1);
            original_center.put(0, 0, new double[] { inverse_rotation_matrix.row(0).dot(center.reshape(1, 1)), inverse_rotation_matrix.row(1).dot(center.reshape(1, 1)) });

            Mat _rotated_landmarks_col0_3 = rotated_landmarks.colRange(new OpenCVRange(0, 3)).clone();

            Core.add(_rotated_landmarks_col0_3.reshape(3, 39)
                , new Scalar(original_center.get(0, 0)[0] + pad_bias.get(0, 0)[0], original_center.get(1, 0)[0] + pad_bias.get(1, 0)[0], 0.0)
                , _ladmarks_col0_3.reshape(3, 39));

            _rotated_landmarks_col0_3.copyTo(rotated_landmarks.colRange(new OpenCVRange(0, 3)));
            _rotated_landmarks_col0_3.Dispose();
            _ladmarks_col0_3.copyTo(landmarks.colRange(new OpenCVRange(0, 3)));
            _ladmarks_col0_3.Dispose();

            // get bounding box from rotated_landmarks
            Point[] landmarks_points = new Point[39];
            for (int i = 0; i < 39; ++i)
            {
                landmarks.get(i, 0, _a_arr);
                landmarks_points[i] = new Point(_a_arr[0], _a_arr[1]);
            }
            MatOfPoint points = new MatOfPoint(landmarks_points);
            OpenCVRect bbox = Imgproc.boundingRect(points);
            Point center_bbox = (bbox.tl() + bbox.br()) / 2;
            Point wh_bbox = bbox.br() - bbox.tl();
            Point new_half_size = wh_bbox * PERSON_BOX_ENLARGE_FACTOR / 2;
            bbox = new OpenCVRect(
                center_bbox - new_half_size,
                center_bbox + new_half_size);


            // # [0: 4]: person bounding box found in image of format [x1, y1, x2, y2] (top-left and bottom-right points)
            // # [4: 199]: screen landmarks with format [x1, y1, z1, v1, p1, x2, y2 ... x39, y39, z39, v39, p39], z value is relative to HIP
            // # [199: 316]: world landmarks with format [x1, y1, z1, x2, y2 ... x39, y39, z39], 3D metric x, y, z coordinate
            // # [316]: confidence 
            Mat results = new Mat(317, 1, CvType.CV_32FC1);
            results.put(0, 0, new float[] { (float)bbox.tl().x, (float)bbox.tl().y, (float)bbox.br().x, (float)bbox.br().y });
            Mat results_col4_199_39x5 = results.rowRange(new OpenCVRange(4, 199)).reshape(1, 39);
            landmarks.colRange(new OpenCVRange(0, 5)).copyTo(results_col4_199_39x5);
            Mat results_col199_316_39x3 = results.rowRange(new OpenCVRange(199, 316)).reshape(1, 39);
            rotated_landmarks_world.colRange(new OpenCVRange(0, 3)).copyTo(results_col199_316_39x3);
            results.put(316, 0, new float[] { conf });

            // # 2*2 person bbox: [[x1, y1], [x2, y2]]
            // # 39*5 screen landmarks: 33 keypoints and 6 auxiliary points with [x, y, z, visibility, presence], z value is relative to HIP
            // #    Visibility is probability that a keypoint is located within the frame and not occluded by another bigger body part or another object
            // #    Presence is probability that a keypoint is located within the frame
            // # 39*3 world landmarks: 33 keypoints and 6 auxiliary points with [x, y, z] 3D metric x, y, z coordinate
            // # conf: confidence of prediction
            return results;
        }

        protected virtual Mat postprocess_mask(List<Mat> output_blob, Mat rotated_person_bbox, double angle, Mat rotation_matrix, Mat pad_bias, Size img_size)
        {
            Mat mask = output_blob[2];
            mask = mask.reshape(1, 256); // shape: (1, 256, 256, 1) -> (256, 256)

            if (mask_warp == null)
                mask_warp = new Mat(mask.size(), CvType.CV_32FC1);

            if (invert_rotation_mask_32F == null)
                invert_rotation_mask_32F = new Mat(img_size, CvType.CV_32FC1, new Scalar(0));
            if (invert_rotation_mask_32F.width() != img_size.width || invert_rotation_mask_32F.height() != img_size.height)
            {
                invert_rotation_mask_32F.create(img_size, CvType.CV_32FC1);
                Imgproc.rectangle(invert_rotation_mask_32F, new OpenCVRect(0, 0, invert_rotation_mask_32F.width(), invert_rotation_mask_32F.height()), Scalar.all(0), -1);
            }

            // # invert rotation for mask
            double[] rotated_person_bbox_arr = new double[4];
            rotated_person_bbox.get(0, 0, rotated_person_bbox_arr);
            Point _rotated_person_bbox_tl = new Point(rotated_person_bbox_arr[0] + pad_bias.get(0, 0)[0], rotated_person_bbox_arr[1] + pad_bias.get(1, 0)[0]);
            Point _rotated_person_bbox_br = new Point(rotated_person_bbox_arr[2] + pad_bias.get(0, 0)[0], rotated_person_bbox_arr[3] + pad_bias.get(1, 0)[0]);
            Mat invert_rotation_matrix = Imgproc.getRotationMatrix2D(new Point(mask.width() / 2, mask.height() / 2), -angle, 1.0);
            Imgproc.warpAffine(mask, mask_warp, invert_rotation_matrix, mask.size());

            // create invert_rotation_mask (32F)
            // crop bounding box
            int[] diff = new int[] {
                    Math.Max((int)-_rotated_person_bbox_tl.x, 0),
                    Math.Max((int)-_rotated_person_bbox_tl.y, 0),
                    Math.Max((int)_rotated_person_bbox_br.x - invert_rotation_mask_32F.width(), 0),
                    Math.Max((int)_rotated_person_bbox_br.y - invert_rotation_mask_32F.height(), 0)
                };

            Point wh_rotated_person_bbox = _rotated_person_bbox_br - _rotated_person_bbox_tl;
            Point scale_factor = new Point(wh_rotated_person_bbox.x / input_size.width, wh_rotated_person_bbox.y / input_size.height);
            int x = (int)Math.Round(diff[0] / scale_factor.x);
            int y = (int)Math.Round(diff[1] / scale_factor.y);
            int w = Math.Min((int)Math.Round((wh_rotated_person_bbox.x - diff[0] - diff[2]) / scale_factor.x), mask_warp.width());
            int h = Math.Min((int)Math.Round((wh_rotated_person_bbox.y - diff[1] - diff[3]) / scale_factor.y), mask_warp.height());

            OpenCVRect mask_warp_crop_rect = new OpenCVRect(x, y, w, h);
            Mat _mask_warp_crop_roi = new Mat(mask_warp, mask_warp_crop_rect);
            OpenCVRect rotated_person_bbox_rect = new OpenCVRect(_rotated_person_bbox_tl, _rotated_person_bbox_br);
            OpenCVRect invert_rotation_mask_32F_rect = new OpenCVRect(0, 0, invert_rotation_mask_32F.width(), invert_rotation_mask_32F.height());
            OpenCVRect invert_rotation_mask_32F_crop_rect = invert_rotation_mask_32F_rect.intersect(rotated_person_bbox_rect);
            Mat _invert_rotation_mask_32F_crop_roi = new Mat(invert_rotation_mask_32F, invert_rotation_mask_32F_crop_rect);
            Imgproc.resize(_mask_warp_crop_roi, _invert_rotation_mask_32F_crop_roi,
                new Size(_invert_rotation_mask_32F_crop_roi.width(), _invert_rotation_mask_32F_crop_roi.height()));

            // # binarize mask
            Imgproc.threshold(_invert_rotation_mask_32F_crop_roi, _invert_rotation_mask_32F_crop_roi, 0, 255, Imgproc.THRESH_BINARY);

            // create invert_rotation_mask (8U)
            Mat invert_rotation_mask = new Mat(img_size, CvType.CV_8UC1, new Scalar(0));
            Mat _invert_rotation_mask_crop_roi = new Mat(invert_rotation_mask, invert_rotation_mask_32F_crop_rect);
            _invert_rotation_mask_32F_crop_roi.convertTo(_invert_rotation_mask_crop_roi, CvType.CV_8U);


            // # img_size.width*img_size.height img_height*img_width mask: gray mask, where 255 indicates the full body of a person and 0 means background
            return invert_rotation_mask;
        }

        public virtual void visualize(Mat image, Mat results, bool print_results = false, bool isRGB = false)
        {
            if (image.IsDisposed)
                return;

            if (results.empty() || results.rows() < 317)
                return;

            StringBuilder sb = null;

            if (print_results)
                sb = new StringBuilder();

            Scalar line_color = new Scalar(255, 255, 255, 255);
            Scalar point_color = (isRGB) ? new Scalar(255, 0, 0, 255) : new Scalar(0, 0, 255, 255);

            float[] conf = new float[1];
            results.get(316, 0, conf);
            float[] bbox = new float[4];
            results.get(0, 0, bbox);

            int auxiliary_points_num = 6;
            Mat results_col4_199_39x5 = results.rowRange(new OpenCVRange(4, 199 - (5 * auxiliary_points_num))).reshape(1, 39 - auxiliary_points_num);
            float[] landmarks_screen_xy = new float[(39 - auxiliary_points_num) * 2];
            results_col4_199_39x5.colRange(new OpenCVRange(0, 2)).get(0, 0, landmarks_screen_xy);

            float[] landmarks_screen_xyz = new float[(39 - auxiliary_points_num) * 3];
            results_col4_199_39x5.colRange(new OpenCVRange(0, 3)).get(0, 0, landmarks_screen_xyz);

            // # only show visible keypoints which presence bigger than 0.8
            float[] landmarks_presence = new float[(39 - auxiliary_points_num)];
            results_col4_199_39x5.colRange(new OpenCVRange(4, 5)).get(0, 0, landmarks_presence);


            Mat results_col199_316_39x3 = results.rowRange(new OpenCVRange(199, 316 - (3 * auxiliary_points_num))).reshape(1, 39 - auxiliary_points_num);
            float[] landmarks_world = new float[(39 - auxiliary_points_num) * 3];
            results_col199_316_39x3.get(0, 0, landmarks_world);

            // # draw box
            Imgproc.rectangle(image, new Point(bbox[0], bbox[1]), new Point(bbox[2], bbox[3]), new Scalar(0, 255, 0, 255), 2);
            Imgproc.putText(image, String.Format("{0:0.0000}", conf[0]), new Point(bbox[0], bbox[1] + 12), Imgproc.FONT_HERSHEY_DUPLEX, 0.5, point_color);

            // # Draw line between each key points
            draw_lines(landmarks_screen_xy, landmarks_presence, false);

            // # z value is relative to HIP, but we use constant to instead
            for (int j = 0; j < landmarks_screen_xyz.Length / 3; ++j)
            {
                int idx = j * 3;
                if (landmarks_presence[j] > 0.8)
                    Imgproc.circle(image, new Point(landmarks_screen_xyz[idx], landmarks_screen_xyz[idx + 1]), 2, point_color, -1);
            }

            // Print results
            if (print_results)
            {
                sb.AppendLine("-----------pose-----------");
                sb.AppendLine(String.Format("conf: {0:0.00}", conf[0]));
                sb.AppendLine(String.Format("person box: {0:0} {1:0} {2:0} {3:0}", bbox[0], bbox[1], bbox[2], bbox[3]));
                sb.AppendLine("pose landmarks: ");
                foreach (var p in landmarks_screen_xyz)
                {
                    sb.Append(String.Format("{0:0} ", p));
                }
                sb.AppendLine();
                sb.AppendLine("pose world landmarks: ");
                foreach (var p in landmarks_world)
                {
                    sb.Append(String.Format("{0:0.000000} ", p));
                }
            }

            if (print_results)
                Debug.Log(sb);


            void draw_lines(float[] landmarks, float[] _landmarks_presence, bool is_draw_point = true, int thickness = 2)
            {
                void _draw_by_presence(int idx1, int idx2)
                {
                    if (_landmarks_presence[idx1] > 0.8 && _landmarks_presence[idx2] > 0.8)
                    {
                        idx1 = idx1 * 2;
                        idx2 = idx2 * 2;
                        Imgproc.line(image, new Point(landmarks[idx1], landmarks[idx1 + 1]), new Point(landmarks[idx2], landmarks[idx2 + 1]), line_color, thickness);
                    }
                }

                // Draw line between each key points
                _draw_by_presence(0, 1);
                _draw_by_presence(1, 2);
                _draw_by_presence(2, 3);
                _draw_by_presence(3, 7);
                _draw_by_presence(0, 4);
                _draw_by_presence(4, 5);
                _draw_by_presence(5, 6);
                _draw_by_presence(6, 8);

                _draw_by_presence(9, 10);

                _draw_by_presence(12, 14);
                _draw_by_presence(14, 16);
                _draw_by_presence(16, 22);
                _draw_by_presence(16, 18);
                _draw_by_presence(16, 20);
                _draw_by_presence(18, 20);

                _draw_by_presence(11, 13);
                _draw_by_presence(13, 15);
                _draw_by_presence(15, 21);
                _draw_by_presence(15, 19);
                _draw_by_presence(15, 17);
                _draw_by_presence(17, 19);

                _draw_by_presence(11, 12);
                _draw_by_presence(11, 23);
                _draw_by_presence(23, 24);
                _draw_by_presence(24, 12);

                _draw_by_presence(24, 26);
                _draw_by_presence(26, 28);
                _draw_by_presence(28, 30);
                _draw_by_presence(28, 32);
                _draw_by_presence(30, 32);

                _draw_by_presence(23, 25);
                _draw_by_presence(25, 27);
                _draw_by_presence(27, 31);
                _draw_by_presence(27, 29);
                _draw_by_presence(29, 31);

                if (is_draw_point)
                {
                    for (int j = 0; j < landmarks.Length / 2; ++j)
                    {
                        int idx = j * 2;
                        if (_landmarks_presence[j] > 0.8)
                            Imgproc.circle(image, new Point(landmarks[idx], landmarks[idx + 1]), 2, point_color, -1);
                    }
                }
            }
        }

        public virtual void visualize_mask(Mat image, Mat mask, bool isRGB = false)
        {
            if (image.IsDisposed)
                return;

            if (mask.empty())
                return;

            if (image.size() != mask.size() && mask.type() == CvType.CV_8UC1)
                return;

            Scalar color = new Scalar(0, 255, 0, 255);

            Imgproc.Canny(mask, mask, 100, 200);
            Mat kernel = Mat.ones(2, 2, CvType.CV_8UC1);// # expansion edge to 2 pixels
            Imgproc.dilate(mask, mask, kernel, new Point(), 1);

            if (colorMat == null)
                colorMat = new Mat(image.size(), image.type(), color);
            if (colorMat.width() != image.width() || colorMat.height() != image.height())
            {
                colorMat.create(image.size(), image.type());
                Imgproc.rectangle(colorMat, new OpenCVRect(0, 0, colorMat.width(), colorMat.height()), color, -1);
            }

            colorMat.copyTo(image, mask);
        }

        public virtual void dispose()
        {
            if (pose_estimation_net != null)
                pose_estimation_net.Dispose();

            if (tmpImage != null)
                tmpImage.Dispose();

            if (tmpRotatedImage != null)
                tmpRotatedImage.Dispose();

            if (mask_warp != null)
                mask_warp.Dispose();
            if (invert_rotation_mask_32F != null)
                invert_rotation_mask_32F.Dispose();

            mask_warp = null;
            invert_rotation_mask_32F = null;

            if (colorMat != null)
                colorMat.Dispose();

            colorMat = null;
        }

        protected virtual void sigmoid(Mat mat)
        {
            if (mat == null)
                throw new ArgumentNullException("mat");
            if (mat != null)
                mat.ThrowIfDisposed();

            //python: 1 / (1 + np.exp(-x))

            Core.multiply(mat, Scalar.all(-1), mat);
            Core.exp(mat, mat);
            Core.add(mat, Scalar.all(1f), mat);
            using (Mat _mat = new Mat(mat.size(), mat.type(), Scalar.all(1f)))
            {
                Core.divide(_mat, mat, mat);
            }
        }
    }
}
#endif