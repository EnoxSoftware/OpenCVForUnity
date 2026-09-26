#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.Extensions;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using Range = OpenCVForUnity.CoreModule.Range;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Face Detection YuNet Example (legacy model)
    /// Real-time face detection with bounding boxes and five facial landmarks using the older YuNet ONNX model.
    /// Prefer <see cref="FaceDetectionYuNetV2Example"/> for the current YuNet API and models.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Extending <see cref="DnnObjectDetectionExample"/> with YuNet-specific prior-box decoding
    /// - Converting RGBA frames to BGR and running synchronous <see cref="Net"/> forward (loc/conf/iou outputs)
    /// - NMS and drawing face boxes plus landmark points on the preview <see cref="Mat"/>
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Net"/>, <see cref="Size"/>, <see cref="Scalar"/>, <see cref="Point"/>
    /// - <see cref="Dnn"/>: blobFromImage, forward, NMSBoxes
    /// - <see cref="Imgproc"/>: cvtColor, circle
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://github.com/opencv/opencv/blob/ed6ca0d7fab5381c6aa6062c49c3c99ee828fadb/modules/objdetect/src/face_detect.cpp
    /// </para>
    /// <para>
    /// [Tested Models]
    /// face_detection_yunet_2022mar.onnx: https://github.com/opencv/opencv_zoo/raw/4563a91ba98172b14d7af8bce621b6d1ae7ae0c6/models/face_detection_yunet/face_detection_yunet_2022mar.onnx
    /// </para>
    /// <para>
    /// <b>Scene Inspector restore reference</b> (FaceDetectionYuNetExample.unity — re-apply on this component if serialized values are lost):
    /// </para>
    /// <list type="bullet">
    /// <item><description>Model: <c>OpenCVForUnityExamples/dnn/face_detection_yunet_2022mar.onnx</c></description></item>
    /// <item><description>Config / Classes: empty</description></item>
    /// <item><description>ConfThreshold: <c>0.6</c></description></item>
    /// <item><description>NmsThreshold: <c>0.3</c></description></item>
    /// <item><description>Scale: <c>1</c></description></item>
    /// <item><description>Mean (BGR): <c>(0, 0, 0, 0)</c></description></item>
    /// <item><description>SwapRB: <c>false</c></description></item>
    /// <item><description>InpWidth: <c>120</c></description></item>
    /// <item><description>InpHeight: <c>160</c></description></item>
    /// <item><description>KeepTopK: <c>5000</c></description></item>
    /// </list>
    /// </remarks>
    public class FaceDetectionYuNetExample : DnnObjectDetectionExample
    {
        // Public Fields
        [TooltipAttribute("Keep keep_top_k for results outputing.")]
        public int KeepTopK = 5000;

        // Protected Fields
        protected Scalar[] _pointsColors = new Scalar[] {
            new Scalar(0, 0, 255, 255), // # right eye
            new Scalar(255, 0, 0, 255), // # left eye
            new Scalar(255, 255, 0, 255), // # nose tip
            new Scalar(0, 255, 255, 255), // # mouth right
            new Scalar(0, 255, 0, 255), // # mouth left
            new Scalar(255, 255, 255, 255) };

        protected PriorBox _pb;
        protected MatOfRect2d _boxes;
        protected MatOfFloat _confidences;
        protected MatOfInt _indices;

        /// <summary>
        /// YuNet blob spatial size derived at init from <see cref="DnnObjectDetectionExample.InpWidth"/>,
        /// <see cref="DnnObjectDetectionExample.InpHeight"/>, and frame aspect ratio.
        /// </summary>
        protected Size _dnnInputShape;

        // Public Methods
        public override void OnSourceToMatHelperInitialized()
        {
            base.OnSourceToMatHelperInitialized();

            // Resize the input image to fit within inputSize dimensions while preserving aspect ratio
            double aspectRatio = (double)_bgrMat.width() / _bgrMat.height();
            int targetWidth, targetHeight;

            if (aspectRatio > (double)InpWidth / InpHeight)
            {
                targetWidth = InpWidth;
                targetHeight = (int)(InpWidth / aspectRatio);
            }
            else
            {
                targetHeight = InpHeight;
                targetWidth = (int)(InpHeight * aspectRatio);
            }
            _dnnInputShape = new Size(targetWidth, targetHeight);

            Size output_shape = _bgrMat.size();
            _pb = new PriorBox(_dnnInputShape, output_shape);
        }

        public override void OnSourceToMatHelperDisposed()
        {
            base.OnSourceToMatHelperDisposed();

            _pb?.Dispose(); _pb = null;

            _boxes?.Dispose();
            _confidences?.Dispose();
            _indices?.Dispose();

            _boxes = null;
            _confidences = null;
            _indices = null;
        }

        // Protected Methods
        protected override void ProcessFrameMatUpdated(Mat rgbaMat)
        {
            if (_net == null || _bgrMat == null)
            {
                return;
            }

            // Convert RGBA camera frame to BGR for YuNet blobFromImage input.
            Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

            Mat blob = Dnn.blobFromImage(_bgrMat, Scale, _dnnInputShape, Mean, SwapRB, false);

            // Run synchronous YuNet forward (loc, conf, iou output blobs).
            _net.setInput(blob);

            //TickMeter tm = new TickMeter();
            //tm.start();

            List<Mat> outs = new List<Mat>();
            List<string> output_names = new List<string>();
            output_names.Add("loc");
            output_names.Add("conf");
            output_names.Add("iou");
            _net.forward(outs, output_names);

            //tm.stop();
            //Debug.Log("Inference time, ms: " + tm.getTimeMilli());

            Postprocess(rgbaMat, outs, _net, Dnn.DNN_BACKEND_OPENCV);

            blob.Dispose();
            foreach (var out_mat in outs)
            {
                out_mat.Dispose();
            }
        }

        protected override void Postprocess(Mat frame, List<Mat> outs, Net net, int backend = Dnn.DNN_BACKEND_OPENCV)
        {

            // # Decode bboxes and landmarks
            Mat dets = _pb.Decode(outs[0], outs[1], outs[2]); // "loc", "conf", "iou"

            // # Ignore low scores + NMS
            int num = dets.rows();

            // MatOf types are logical vectors and do not fix a storage shape.
            // Allocate NMS inputs explicitly as N×1 to match dets.colRange columns so copyTo works as-is.
            if (_boxes == null || _boxes.rows() != num)
            {
                _boxes ??= new MatOfRect2d();
                _boxes.create(num, 1, CvType.CV_64FC4);
            }

            if (_confidences == null || _confidences.rows() != num)
            {
                _confidences ??= new MatOfFloat();
                _confidences.create(num, 1, CvType.CV_32FC1);
            }

            if (_indices == null)
            {
                _indices = new MatOfInt();
            }

            Mat bboxes = dets.colRange(0, 4);
            // Reshape N×1 CV_64FC4 _boxes to an N×4 CV_64FC1 view and convertTo the bbox column in place.
            using (Mat boxesMC1View = _boxes.reshape(1, num))
            {
                bboxes.convertTo(boxesMC1View, CvType.CV_64FC1);
            }

            Mat scores = dets.colRange(14, 15);
            scores.copyTo(_confidences);

            Dnn.NMSBoxes(_boxes, _confidences, ConfThreshold, NmsThreshold, _indices, 1f, KeepTopK);

            if (_indices.empty())
            {
                return;
            }

            // # Draw boudning boxes and landmarks on the original image
            ReadOnlySpan<int> allIndices = _indices.AsSpan<int>();
            for (int i = 0; i < allIndices.Length; ++i)
            {
                int idx = allIndices[i];

                float[] bbox_arr = new float[4];
                bboxes.get(idx, 0, bbox_arr);
                float[] confidence_arr = new float[1];
                _confidences.get(idx, 0, confidence_arr);
                DrawPred(0, confidence_arr[0], bbox_arr[0], bbox_arr[1], bbox_arr[0] + bbox_arr[2], bbox_arr[1] + bbox_arr[3], frame);

                Mat landmarks = dets.colRange(4, 14);
                float[] landmarks_arr = new float[10];
                landmarks.get(idx, 0, landmarks_arr);
                Point[] points = new Point[] { new Point(landmarks_arr[0], landmarks_arr[1]), new Point(landmarks_arr[2], landmarks_arr[3]),
                    new Point(landmarks_arr[4], landmarks_arr[5]), new Point(landmarks_arr[6], landmarks_arr[7]), new Point(landmarks_arr[8], landmarks_arr[9])};
                DrawPredPoints(points, frame);
            }
        }

        protected virtual void DrawPredPoints(Point[] points, Mat frame)
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (i < _pointsColors.Length)
                {
                    Imgproc.circle(frame, points[i], 2, _pointsColors[i], 2);
                }
                else
                {
                    Imgproc.circle(frame, points[i], 2, _pointsColors[_pointsColors.Length - 1], 2);
                }
            }
        }

        protected class PriorBox
        {
            // Private Fields
            private float[][] _minSizes = new float[][]{
                new float[]{10.0f,  16.0f,  24.0f},
                new float[]{32.0f,  48.0f},
                new float[]{64.0f,  96.0f},
                new float[]{128.0f, 192.0f, 256.0f}
            };

            private int[] _steps = new int[] { 8, 16, 32, 64 };
            private float[] _variance = new float[] { 0.1f, 0.2f };

            private int _inW;
            private int _inH;
            private int _outW;
            private int _outH;

            private List<Size> _featureMapSizes;
            private Mat _priors;

            private Mat _dets;
            private Mat _ones;
            private Mat _scale;

            private Mat _priors02;
            private Mat _priors24;
            private Mat _bboxes;
            private Mat _bboxes02;
            private Mat _bboxes24;
            private Mat _landmarks;
            private Mat _landmarks02;
            private Mat _landmarks24;
            private Mat _landmarks46;
            private Mat _landmarks68;
            private Mat _landmarks810;
            private Mat _scores;
            private Mat _ones01;
            private Mat _ones02;
            private Mat _bboxScale;
            private Mat _landmarkScale;

            // Public Methods
            public PriorBox(Size input_shape, Size output_shape)
            {
                // initialize
                _inW = (int)input_shape.width;
                _inH = (int)input_shape.height;
                _outW = (int)output_shape.width;
                _outH = (int)output_shape.height;

                Size feature_map_2nd = new Size((int)((int)((input_shape.width + 1) / 2) / 2), (int)((int)((input_shape.height + 1) / 2) / 2));
                Size feature_map_3rd = new Size((int)(feature_map_2nd.width / 2), (int)(feature_map_2nd.height / 2));
                Size feature_map_4th = new Size((int)(feature_map_3rd.width / 2), (int)(feature_map_3rd.height / 2));
                Size feature_map_5th = new Size((int)(feature_map_4th.width / 2), (int)(feature_map_4th.height / 2));
                Size feature_map_6th = new Size((int)(feature_map_5th.width / 2), (int)(feature_map_5th.height / 2));

                _featureMapSizes = new List<Size>();
                _featureMapSizes.Add(feature_map_3rd);
                _featureMapSizes.Add(feature_map_4th);
                _featureMapSizes.Add(feature_map_5th);
                _featureMapSizes.Add(feature_map_6th);

                _priors = GeneratePrior();
                _priors02 = _priors.colRange(new Range(0, 2));
                _priors24 = _priors.colRange(new Range(2, 4));
            }

            /// <summary>
            /// Decodes the locations (x1, y1, w, h,...) and scores (c) from the priors, and the given loc and conf.
            /// </summary>
            /// <param name="loc">loc produced from loc layers of shape [num_priors, 14]. '14' for [x_c, y_c, w, h,...].</param>
            /// <param name="conf">conf produced from conf layers of shape [num_priors, 2]. '2' for [p_non_face, p_face].</param>
            /// <param name="iou">iou produced from iou layers of shape [num_priors, 1]. '1' for [iou].</param>
            /// <returns>dets is concatenated by bboxes, landmarks and scores. num * [x1, y1, w, h, x_re, y_re, x_le, y_le, x_n, y_n, x_mr, y_mr, x_ml, y_ml, score]</returns>
            public Mat Decode(Mat loc, Mat conf, Mat iou)
            {
                Mat loc_m = loc; // [num*14]
                Mat conf_m = conf; // [num*2]
                Mat iou_m = iou; // [num*1]

                int num = loc_m.rows();

                if (_dets == null || (_dets != null && _dets.IsDisposed))
                {
                    _dets = new Mat(num, 15, CvType.CV_32FC1);
                    _ones = Mat.ones(num, 2, CvType.CV_32FC1);
                    _scale = new Mat(num, 1, CvType.CV_32FC4, new Scalar(_outW, _outH, _outW, _outH));
                    _scale = _scale.reshape(1, num);

                    _bboxes = _dets.colRange(new Range(0, 4));
                    _bboxes02 = _bboxes.colRange(new Range(0, 2));
                    _bboxes24 = _bboxes.colRange(new Range(2, 4));
                    _landmarks = _dets.colRange(new Range(4, 14));
                    _landmarks02 = _landmarks.colRange(new Range(0, 2));
                    _landmarks24 = _landmarks.colRange(new Range(2, 4));
                    _landmarks46 = _landmarks.colRange(new Range(4, 6));
                    _landmarks68 = _landmarks.colRange(new Range(6, 8));
                    _landmarks810 = _landmarks.colRange(new Range(8, 10));
                    _scores = _dets.colRange(new Range(14, 15));
                    _ones01 = _ones.colRange(0, 1);
                    _ones02 = _ones.colRange(0, 2);
                    _bboxScale = _scale.colRange(0, 4);
                    _landmarkScale = _scale.colRange(0, 2);
                }

                Mat loc_0_2 = loc_m.colRange(new Range(0, 2));
                Mat loc_2_4 = loc_m.colRange(new Range(2, 4));
                Mat loc_2_3 = loc_m.colRange(new Range(2, 3));
                Mat loc_3_4 = loc_m.colRange(new Range(3, 4));

                // # get bboxes
                Core.multiply(loc_0_2, _priors24, _bboxes02, _variance[0]);
                Core.add(_priors02, _bboxes02, _bboxes02);
                Core.multiply(loc_2_3, _ones01, loc_2_3, _variance[0]);
                Core.multiply(loc_3_4, _ones01, loc_3_4, _variance[1]);
                Core.exp(loc_2_4, _bboxes24);
                Core.multiply(_priors24, _bboxes24, _bboxes24);

                // # (x_c, y_c, w, h) -> (x1, y1, w, h)
                Core.divide(_bboxes24, _ones02, loc_2_4, 0.5);
                Core.subtract(_bboxes02, loc_2_4, _bboxes02);

                // # scale recover
                Core.multiply(_bboxes, _bboxScale, _bboxes);

                Mat loc_4_6 = loc_m.colRange(new Range(4, 6));
                Mat loc_6_8 = loc_m.colRange(new Range(6, 8));
                Mat loc_8_10 = loc_m.colRange(new Range(8, 10));
                Mat loc_10_12 = loc_m.colRange(new Range(10, 12));
                Mat loc_12_14 = loc_m.colRange(new Range(12, 14));

                // # get landmarks
                Core.multiply(loc_4_6, _priors24, _landmarks02, _variance[0]);
                Core.add(_priors02, _landmarks02, _landmarks02);
                Core.multiply(loc_6_8, _priors24, _landmarks24, _variance[0]);
                Core.add(_priors02, _landmarks24, _landmarks24);
                Core.multiply(loc_8_10, _priors24, _landmarks46, _variance[0]);
                Core.add(_priors02, _landmarks46, _landmarks46);
                Core.multiply(loc_10_12, _priors24, _landmarks68, _variance[0]);
                Core.add(_priors02, _landmarks68, _landmarks68);
                Core.multiply(loc_12_14, _priors24, _landmarks810, _variance[0]);
                Core.add(_priors02, _landmarks810, _landmarks810);

                // # scale recover
                Core.multiply(_landmarks02, _landmarkScale, _landmarks02);
                Core.multiply(_landmarks24, _landmarkScale, _landmarks24);
                Core.multiply(_landmarks46, _landmarkScale, _landmarks46);
                Core.multiply(_landmarks68, _landmarkScale, _landmarks68);
                Core.multiply(_landmarks810, _landmarkScale, _landmarks810);

                // # get score
                Mat cls_scores = conf_m.colRange(new Range(1, 2));
                Mat iou_scores = iou_m;
                Imgproc.threshold(iou_scores, iou_scores, 0, 0, Imgproc.THRESH_TOZERO);
                Imgproc.threshold(iou_scores, iou_scores, 1.0, 0, Imgproc.THRESH_TRUNC);
                Core.multiply(cls_scores, iou_scores, _scores);
                Core.sqrt(_scores, _scores);

                return _dets;
            }

            public void Dispose()
            {
                _priors?.Dispose();

                _dets?.Dispose();
                _ones?.Dispose();
                _scale?.Dispose();
            }

            // Private Methods
            private Mat GeneratePrior()
            {
                int priors_size = 0;
                for (int index = 0; index < _featureMapSizes.Count; index++)
                {
                    priors_size += (int)(_featureMapSizes[index].width * _featureMapSizes[index].height * _minSizes[index].Length);
                }

                Mat anchors = new Mat(priors_size, 4, CvType.CV_32FC1);
                int count = 0;
                for (int i = 0; i < _featureMapSizes.Count; i++)
                {
                    Size feature_map_size = _featureMapSizes[i];
                    float[] min_size = _minSizes[i];

                    for (int h = 0; h < feature_map_size.height; h++)
                    {
                        for (int w = 0; w < feature_map_size.width; w++)
                        {
                            for (int j = 0; j < min_size.Length; j++)
                            {
                                float kx = min_size[j] / _inW;
                                float ky = min_size[j] / _inH;

                                float cx = (float)((w + 0.5) * _steps[i] / _inW);
                                float cy = (float)((h + 0.5) * _steps[i] / _inH);

                                anchors.put(count, 0, new float[] { cx, cy, kx, ky });

                                count++;
                            }
                        }
                    }
                }

                return anchors;
            }
        }
    }
}

#endif
