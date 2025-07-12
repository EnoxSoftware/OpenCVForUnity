#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using Range = OpenCVForUnity.CoreModule.Range;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Face Detection YuNet Example
    /// (##### The inference model used in this example is an older version. Normally, you should use the example "FaceDetectionYuNetV2Example" which uses a newer model. #####)
    /// Referring to https://github.com/opencv/opencv/blob/ed6ca0d7fab5381c6aa6062c49c3c99ee828fadb/modules/objdetect/src/face_detect.cpp
    ///
    /// [Tested Models]
    /// face_detection_yunet_2022mar.onnx: https://github.com/opencv/opencv_zoo/raw/4563a91ba98172b14d7af8bce621b6d1ae7ae0c6/models/face_detection_yunet/face_detection_yunet_2022mar.onnx
    /// </summary>
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
        protected Mat _boxesMC1;
        protected Mat _boxesMC4;
        protected Mat _confidencesM;
        protected MatOfRect2d _boxes;
        protected MatOfFloat _confidences;
        protected MatOfInt _indices;

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

            _boxesMC1?.Dispose();
            _boxesMC4?.Dispose();
            _confidencesM?.Dispose();
            _boxes?.Dispose();
            _confidences?.Dispose();
            _indices?.Dispose();

            _boxesMC1 = null;
            _boxesMC4 = null;
            _confidencesM = null;
            _boxes = null;
            _confidences = null;
            _indices = null;
        }

        // Protected Methods
        protected override void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_net == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

                    Mat blob = Dnn.blobFromImage(_bgrMat, Scale, _dnnInputShape, Mean, SwapRB, false);

                    // Run a model.
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
                        out_mat.Dispose();
                }

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        protected override void Postprocess(Mat frame, List<Mat> outs, Net net, int backend = Dnn.DNN_BACKEND_OPENCV)
        {

            // # Decode bboxes and landmarks
            Mat dets = _pb.Decode(outs[0], outs[1], outs[2]); // "loc", "conf", "iou"


            // # Ignore low scores + NMS
            int num = dets.rows();

            if (_boxesMC1 == null)
                _boxesMC1 = new Mat(num, 4, CvType.CV_64FC1);
            if (_boxesMC4 == null)
                _boxesMC4 = new Mat(num, 1, CvType.CV_64FC4);
            if (_confidencesM == null)
                _confidencesM = new Mat(num, 1, CvType.CV_32FC1);

            if (_boxes == null)
                _boxes = new MatOfRect2d(_boxesMC4);
            if (_confidences == null)
                _confidences = new MatOfFloat(_confidencesM);
            if (_indices == null)
                _indices = new MatOfInt();

            Mat bboxes = dets.colRange(0, 4);
            bboxes.convertTo(_boxesMC1, CvType.CV_64FC1);
            OpenCVMatUtils.CopyToMat(new IntPtr(_boxesMC1.dataAddr()), _boxesMC4);

            Mat scores = dets.colRange(14, 15);
            scores.copyTo(_confidencesM);

            Dnn.NMSBoxes(_boxes, _confidences, ConfThreshold, NmsThreshold, _indices, 1f, KeepTopK);


            // # Draw boudning boxes and landmarks on the original image
            for (int i = 0; i < _indices.total(); ++i)
            {
                int idx = (int)_indices.get(i, 0)[0];

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
                    priors_size += (int)(_featureMapSizes[index].width * _featureMapSizes[index].height * _minSizes[index].Length);

                Mat anchors = new Mat(priors_size, 4, CvType.CV_32FC1);
                int count = 0;
                for (int i = 0; i < _featureMapSizes.Count; i++)
                {
                    Size feature_map_size = _featureMapSizes[i];
                    float[] min_size = _minSizes[i];

                    for (int _h = 0; _h < feature_map_size.height; _h++)
                    {
                        for (int _w = 0; _w < feature_map_size.width; _w++)
                        {
                            for (int j = 0; j < min_size.Length; j++)
                            {
                                float s_kx = min_size[j] / _inW;
                                float s_ky = min_size[j] / _inH;

                                float cx = (float)((_w + 0.5) * _steps[i] / _inW);
                                float cy = (float)((_h + 0.5) * _steps[i] / _inH);

                                anchors.put(count, 0, new float[] { cx, cy, s_kx, s_ky });

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
