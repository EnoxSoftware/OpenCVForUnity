#if !UNITY_WSA_10_0

using System;
using System.Runtime.InteropServices;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// FaceDetectorYN Example
    /// An example of detecting human face using the FaceDetectorYN class.
    /// Referring to:
    /// https://github.com/opencv/opencv/blob/master/samples/dnn/face_detect.cpp
    /// https://docs.opencv.org/4.5.4/d0/dd4/tutorial_dnn_face.html
    ///
    /// [Tested Models]
    /// face_detection_yunet_2023mar.onnx https://github.com/opencv/opencv_zoo/blob/main/models/face_detection_yunet/face_detection_yunet_2023mar.onnx
    /// yunet_n_320_320.onnx https://github.com/ShiqiYu/libfacedetection.train/blob/master/onnx/yunet_n_320_320.onnx
    /// yunet_n_640_640.onnx https://github.com/ShiqiYu/libfacedetection.train/blob/master/onnx/yunet_n_640_640.onnx
    /// yunet_s_320_320.onnx https://github.com/ShiqiYu/libfacedetection.train/blob/master/onnx/yunet_s_320_320.onnx
    /// yunet_s_640_640.onnx https://github.com/ShiqiYu/libfacedetection.train/blob/master/onnx/yunet_s_640_640.onnx
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class FaceDetectorYNExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// MODEL_FILENAME
        /// </summary>
        protected static readonly string MODEL_FILENAME = "OpenCVForUnityExamples/objdetect/face_detection_yunet_2023mar.onnx";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        /// <summary>
        /// The apply face blurring Toggle.
        /// </summary>
        public Toggle ApplyFaceBlurringToggle;

        // Private Fields
        /// <summary>
        /// The FaceDetectorYN.
        /// </summary>
        private FaceDetectorYN _faceDetector;

        /// <summary>
        /// The size for the network input.
        /// </summary>
        private int _inputSizeW = 320;
        private int _inputSizeH = 320;

        /// <summary>
        /// Filter out faces of score < score_threshold.
        /// </summary>
        private float _scoreThreshold = 0.6f;

        /// <summary>
        /// Suppress bounding boxes of iou >= nms_threshold
        /// </summary>
        private float _nmsThreshold = 0.3f;

        /// <summary>
        /// Keep top_k bounding boxes before NMS.
        /// </summary>
        private int _topK = 5000;

        /// <summary>
        /// The bgr mat.
        /// </summary>
        private Mat _bgrMat;

        /// <summary>
        /// The input mat.
        /// </summary>
        private Mat _inputMat;

        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        private Scalar _bBoxColor = new Scalar(255, 255, 0, 255);

        private Scalar[] _keyPointsColors = new Scalar[] {
            new Scalar(0, 0, 255, 255), // # right eye
            new Scalar(255, 0, 0, 255), // # left eye
            new Scalar(255, 255, 0, 255), // # nose tip
            new Scalar(0, 255, 255, 255), // # mouth right
            new Scalar(0, 255, 0, 255), // # mouth left
            new Scalar(255, 255, 255, 255) };

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            string fd_modelPath = await OpenCVEnv.GetFilePathTaskAsync(MODEL_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            if (string.IsNullOrEmpty(fd_modelPath))
            {
                Debug.LogError(MODEL_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
            }
            else
            {
                _faceDetector = FaceDetectorYN.create(fd_modelPath, "", new Size(_inputSizeW, _inputSizeH), _scoreThreshold, _nmsThreshold, _topK);
            }

            _multiSource2MatHelper.Initialize();
        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_faceDetector == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                    OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
                    return;
                }

                Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

                FaceDetection5LandmarkData[] detections = Detect(_bgrMat);

                for (int i = 0; i < detections.Length; i++)
                {
                    ref readonly var d = ref detections[i];
                    if (ApplyFaceBlurringToggle.isOn)
                    {
                        BlurDetection(d, rgbaMat);
                    }
                    else
                    {
                        DrawDetection(d, rgbaMat);
                    }
                }

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            _faceDetector?.Dispose();

            OpenCVDebug.SetDebugMode(false);

            _cts?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = _multiSource2MatHelper.GetMat();

            _texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("width", rgbaMat.width().ToString());
                _fpsMonitor.Add("height", rgbaMat.height().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            _bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;

            _bgrMat?.Dispose();

            _inputMat?.Dispose();
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            _multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            _multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            _multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            _multiSource2MatHelper.RequestedIsFrontFacing = !_multiSource2MatHelper.RequestedIsFrontFacing;
        }

        // Private Methods
        protected virtual FaceDetection5LandmarkData[] Detect(Mat image)
        {
            // Resize the input image to fit within inputSize dimensions while preserving aspect ratio
            double aspectRatio = (double)image.width() / image.height();
            int targetWidth, targetHeight;

            if (aspectRatio > (double)_inputSizeW / _inputSizeH)
            {
                targetWidth = _inputSizeW;
                targetHeight = (int)(_inputSizeW / aspectRatio);
            }
            else
            {
                targetHeight = _inputSizeH;
                targetWidth = (int)(_inputSizeH * aspectRatio);
            }

            if (_inputMat == null || _inputMat.width() != targetWidth || _inputMat.height() != targetHeight)
            {
                if (_inputMat == null) _inputMat = new Mat();
                _inputMat.create(targetHeight, targetWidth, image.type());
                _faceDetector.setInputSize(new Size(targetWidth, targetHeight));
            }

            Imgproc.resize(image, _inputMat, new Size(targetWidth, targetHeight));

            FaceDetection5LandmarkData[] detections;

            using (Mat faces = new Mat())
            {
                // The detection output faces is a two - dimension array of type CV_32F, whose rows are the detected face instances, columns are the location of a face and 5 facial landmarks.
                // The format of each row is as follows:
                // x1, y1, w, h, x_re, y_re, x_le, y_le, x_nt, y_nt, x_rcm, y_rcm, x_lcm, y_lcm
                // ,  where x1, y1, w, h are the top - left coordinates, width and height of the face bounding box, { x, y}_{ re, le, nt, rcm, lcm}
                // stands for the coordinates of right eye, left eye, nose tip, the right corner and left corner of the mouth respectively.
                _faceDetector.detect(_inputMat, faces);

                detections = new FaceDetection5LandmarkData[faces.rows()];

                float input_w = _inputMat.width();
                float input_h = _inputMat.height();
                float original_w = image.width();
                float original_h = image.height();

                float scaleRatioX = original_w / input_w;
                float scaleRatioY = original_h / input_h;

                for (int i = 0; i < faces.rows(); i++)
                {
                    float[] buf = new float[FaceDetection5LandmarkData.DATA_SIZE];
                    faces.get(i, 0, buf);

                    for (int x = 0; x < 4; x++)
                    {
                        if (x % 2 == 0)
                        {
                            float p = buf[x] * scaleRatioX;
                            p = Mathf.Clamp(p, 0, original_w);
                            buf[x] = p;
                        }
                        else
                        {
                            float p = buf[x] * scaleRatioY;
                            p = Mathf.Clamp(p, 0, original_h);
                            buf[x] = p;
                        }
                    }

                    for (int x = 4; x < 14; x++)
                    {
                        if (x % 2 == 0)
                        {
                            buf[x] *= scaleRatioX;
                        }
                        else
                        {
                            buf[x] *= scaleRatioY;
                        }
                    }

                    GCHandle gch = GCHandle.Alloc(buf, GCHandleType.Pinned);
                    detections[i] = (FaceDetection5LandmarkData)Marshal.PtrToStructure(gch.AddrOfPinnedObject(), typeof(FaceDetection5LandmarkData));
                    gch.Free();
                }
            }

            return detections;
        }

        protected virtual void DrawDetection(in FaceDetection5LandmarkData d, Mat frame)
        {
            Imgproc.rectangle(frame, new Point(d.X, d.Y), new Point(d.X + d.Width, d.Y + d.Height), _bBoxColor, 2);
            Imgproc.circle(frame, new Point(d.RightEye.Item1, d.RightEye.Item2), 2, _keyPointsColors[0], 2);
            Imgproc.circle(frame, new Point(d.LeftEye.Item1, d.LeftEye.Item2), 2, _keyPointsColors[1], 2);
            Imgproc.circle(frame, new Point(d.Nose.Item1, d.Nose.Item2), 2, _keyPointsColors[2], 2);
            Imgproc.circle(frame, new Point(d.RightMouth.Item1, d.RightMouth.Item2), 2, _keyPointsColors[3], 2);
            Imgproc.circle(frame, new Point(d.LeftMouth.Item1, d.LeftMouth.Item2), 2, _keyPointsColors[4], 2);

            string label = d.Score.ToString();
            int[] baseLine = new int[1];
            Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

            float top = Mathf.Max(d.Y, (float)labelSize.height);
            float left = d.X;
            Imgproc.rectangle(frame, new Point(left, top - labelSize.height),
                new Point(left + labelSize.width, top + baseLine[0]), Scalar.all(255), Core.FILLED);
            Imgproc.putText(frame, label, new Point(left, top), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(0, 0, 0, 255));
        }

        protected virtual void BlurDetection(in FaceDetection5LandmarkData d, Mat frame)
        {
            int x = (int)d.X;
            int y = (int)d.Y;
            int width = (int)d.Width;
            int height = (int)d.Height;

            Rect faceRect = new Rect(x, y, width, height);
            Rect frameRect = new Rect(0, 0, frame.cols(), frame.rows());
            Rect intersectRect = faceRect.intersect(frameRect);

            if (intersectRect.width > 0 && intersectRect.height > 0)
            {
                Mat faceRegion = new Mat(frame, intersectRect);
                Imgproc.GaussianBlur(faceRegion, faceRegion, new Size(45, 45), 0);
            }
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public readonly struct FaceDetection5LandmarkData
        {
            // Bounding box
            public readonly float X;
            public readonly float Y;
            public readonly float Width;
            public readonly float Height;

            // Key points
            public readonly Vec2f RightEye;
            public readonly Vec2f LeftEye;
            public readonly Vec2f Nose;
            public readonly Vec2f RightMouth;
            public readonly Vec2f LeftMouth;

            // Confidence score [0, 1]
            public readonly float Score;

            public const int LANDMARK_VEC2F_COUNT = 5;
            public const int LANDMARK_ELEMENT_COUNT = 2 * LANDMARK_VEC2F_COUNT;
            public const int ELEMENT_COUNT = 4 + LANDMARK_ELEMENT_COUNT + 1;
            public const int DATA_SIZE = ELEMENT_COUNT * 4;

            public FaceDetection5LandmarkData(float x, float y, float width, float height, Vec2f rightEye, Vec2f leftEye, Vec2f nose, Vec2f rightMouth, Vec2f leftMouth, float score)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
                RightEye = rightEye;
                LeftEye = leftEye;
                Nose = nose;
                RightMouth = rightMouth;
                LeftMouth = leftMouth;
                Score = score;
            }

            public readonly override string ToString()
            {
                return $"FaceDetection5LandmarkData(X:{X} Y:{Y} Width:{Width} Height:{Height} RightEye:{RightEye.ToString()} LeftEye:{LeftEye.ToString()} Nose:{Nose.ToString()} RightMouth:{RightMouth.ToString()} LeftMouth:{LeftMouth.ToString()} Score:{Score})";
            }
        }
    }
}

#endif
