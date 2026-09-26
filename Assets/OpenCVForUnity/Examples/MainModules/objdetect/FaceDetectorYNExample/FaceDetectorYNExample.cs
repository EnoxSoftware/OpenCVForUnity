#if !UNITY_WSA_10_0

using System;
using System.Runtime.InteropServices;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenCVDebug = OpenCVForUnity.Extensions.OpenCVDebug;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// FaceDetectorYN Example
    /// Detects faces with 5 landmarks using the YuNet DNN model on each input frame.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Loading an ONNX face detector and configuring score/NMS thresholds
    /// - Aspect-ratio-preserving resize before inference and coordinate rescaling afterward
    /// - Optional face blurring or bounding-box/landmark visualization
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="FaceDetectorYN"/>, <see cref="Mat"/>
    /// - <see cref="Imgproc"/>: cvtColor, resize, rectangle, circle, GaussianBlur, putText
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://github.com/opencv/opencv/blob/master/samples/dnn/face_detect.cpp
    /// https://docs.opencv.org/4.5.4/d0/dd4/tutorial_dnn_face.html
    /// </para>
    /// <para>
    /// [Tested Models]
    /// face_detection_yunet_2023mar.onnx https://github.com/opencv/opencv_zoo/blob/main/models/face_detection_yunet/face_detection_yunet_2023mar.onnx
    /// yunet_n_320_320.onnx https://github.com/ShiqiYu/libfacedetection.train/blob/master/onnx/yunet_n_320_320.onnx
    /// yunet_n_640_640.onnx https://github.com/ShiqiYu/libfacedetection.train/blob/master/onnx/yunet_n_640_640.onnx
    /// yunet_s_320_320.onnx https://github.com/ShiqiYu/libfacedetection.train/blob/master/onnx/yunet_s_320_320.onnx
    /// yunet_s_640_640.onnx https://github.com/ShiqiYu/libfacedetection.train/blob/master/onnx/yunet_s_640_640.onnx
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class FaceDetectorYNExample : MonoBehaviour
    {
        // Constants
        private static readonly string MODEL_FILEPATH = "OpenCVForUnityExamples/objdetect/face_detection_yunet_2023mar.onnx";

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
        private FaceDetectorYN _faceDetector;
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
        private Mat _bgrMat;
        private Mat _inputMat;
        private Texture2D _texture;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;
        private Scalar _bBoxColor = new Scalar(255, 255, 0, 255);

        private Scalar[] _keyPointsColors = new Scalar[] {
            new Scalar(0, 0, 255, 255), // # right eye
            new Scalar(255, 0, 0, 255), // # left eye
            new Scalar(255, 255, 0, 255), // # nose tip
            new Scalar(0, 255, 255, 255), // # mouth right
            new Scalar(0, 255, 0, 255), // # mouth left
            new Scalar(255, 255, 255, 255) };

        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            WireSourceToMatControlPanelHooks();

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            string fd_modelPath = await OpenCVForUnityEnv.GetFilePathAsync(MODEL_FILEPATH, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            if (string.IsNullOrEmpty(fd_modelPath))
            {
                Debug.LogError(MODEL_FILEPATH + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "model file is not loaded.\nPlease read console message.";
                }
            }
            else
            {
                _faceDetector = FaceDetectorYN.create(fd_modelPath, "", new Size(_inputSizeW, _inputSizeH), _scoreThreshold, _nmsThreshold, _topK);
            }

            if (_faceDetector == null)
            {
                return;
            }

            _multiSourceToMatHelper.Initialize();
        }

        private void OnDestroy()
        {
            UnwireSourceToMatControlPanelHooks();

            _cts?.Cancel();

            _faceDetector?.Dispose();
            _faceDetector = null;

            OpenCVDebug.SetDebugMode(false);

            _cts?.Dispose();
            _cts = null;
        }

        // Public Methods
        /// <summary>
        /// Raises the helper frame mat updated event.
        /// Updates the preview texture when a new frame is available during playback.
        /// </summary>
        public void OnSourceToMatHelperFrameMatUpdated()
        {
            if (!_multiSourceToMatHelper.IsPlaying)
            {
                return;
            }

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;

            if (_faceDetector != null)
            {
                // FaceDetectorYN expects BGR input, not RGBA from the camera helper.
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
            }

            OpenCVMatUnityUtils.MatToTexture2D(rgbaMat, _texture);
        }

        /// <summary>
        /// Raises the helper initialized event.
        /// Recreates the preview texture and starts playback on first initialization.
        /// Skips Play when re-initialization has already restored Playing or Paused.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized", this);

            RecreatePreviewTexture();
            CreateOrRecreateProcessingResources(_multiSourceToMatHelper.FrameMat);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Clear();
                UpdateFpsMonitorPlaybackState();
                _fpsMonitor.Add("HelperKind", _multiSourceToMatHelper.RequestedHelperKind.ToString());
                _fpsMonitor.Add("Width", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("Height", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("Rotate90Degree", _multiSourceToMatHelper.Rotate90Degree.ToString());
                _fpsMonitor.Add("FlipVertical", _multiSourceToMatHelper.FlipVertical.ToString());
                _fpsMonitor.Add("FlipHorizontal", _multiSourceToMatHelper.FlipHorizontal.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
            }

            if (!_multiSourceToMatHelper.IsPlaying && !_multiSourceToMatHelper.IsPaused)
            {
                _multiSourceToMatHelper.Play();
                UpdateFpsMonitorPlaybackState();
            }
        }

        /// <summary>
        /// Raises the helper frame mat layout changed event.
        /// Recreates the preview texture when rotation or output size changes.
        /// </summary>
        public void OnSourceToMatHelperFrameMatLayoutChanged()
        {
            Debug.Log("OnSourceToMatHelperFrameMatLayoutChanged", this);

            RecreatePreviewTexture();
            CreateOrRecreateProcessingResources(_multiSourceToMatHelper.FrameMat);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("Width", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("Height", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
            }
        }

        /// <summary>
        /// Raises the helper released event.
        /// </summary>
        public void OnSourceToMatHelperReleased()
        {
            Debug.Log("OnSourceToMatHelperReleased", this);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Clear();
            }

            DisposeFrameProcessingResources();
            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed", this);

            DisposeFrameProcessingResources();
            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(SourceToMatErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message, this);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        /// <summary>
        /// Raises the back button click event.
        /// Stops playback and disposes the helper before scene transition (required on WebGL).
        /// </summary>
        public async void OnBackButtonClick()
        {
            if (_multiSourceToMatHelper.IsPlaying || _multiSourceToMatHelper.IsPaused)
            {
                await _multiSourceToMatHelper.StopAsync();
            }

            await _multiSourceToMatHelper.DisposeAsync();

            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterPlay"/>.
        /// </summary>
        public void OnControlPanelAfterPlay()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterPause"/>.
        /// </summary>
        public void OnControlPanelAfterPause()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterStop"/>.
        /// </summary>
        public void OnControlPanelAfterStop()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnRotate90Changed"/>.
        /// </summary>
        /// <param name="isOn">New Rotate90Degree value applied by the panel.</param>
        public void OnControlPanelRotate90Changed(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("Rotate90Degree", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnFlipVerticalChanged"/>.
        /// </summary>
        /// <param name="isOn">New FlipVertical value applied by the panel.</param>
        public void OnControlPanelFlipVerticalChanged(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("FlipVertical", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnFlipHorizontalChanged"/>.
        /// </summary>
        /// <param name="isOn">New FlipHorizontal value applied by the panel.</param>
        public void OnControlPanelFlipHorizontalChanged(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("FlipHorizontal", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnHelperKindChanged"/>.
        /// </summary>
        /// <param name="kindIndex">Dropdown index matching <see cref="MultiSourceHelperKind"/>.</param>
        public void OnControlPanelHelperKindChanged(int kindIndex)
        {
            if (_fpsMonitor == null || !Enum.IsDefined(typeof(MultiSourceHelperKind), kindIndex))
            {
                return;
            }

            _fpsMonitor.Add("HelperKind", ((MultiSourceHelperKind)kindIndex).ToString());
        }

        // Private Methods
        private void RecreatePreviewTexture()
        {
            Mat frameMat = _multiSourceToMatHelper.FrameMat;
            if (frameMat == null)
            {
                return;
            }

            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
                _texture = null;
            }

            bool isRgb = _multiSourceToMatHelper.OutputColorFormat == SourceToMatColorFormat.RGB;
            _texture = new Texture2D(frameMat.cols(), frameMat.rows(), isRgb ? TextureFormat.RGB24 : TextureFormat.RGBA32, false);
            OpenCVMatUnityUtils.MatToTexture2D(frameMat, _texture);

            if (ResultPreview != null)
            {
                ResultPreview.texture = _texture;
                AspectRatioFitter aspectRatioFitter = ResultPreview.GetComponent<AspectRatioFitter>();
                if (aspectRatioFitter != null)
                {
                    aspectRatioFitter.aspectRatio = (float)_texture.width / _texture.height;
                }
            }
        }

        private void DisposeFrameProcessingResources()
        {
            _bgrMat?.Dispose();
            _bgrMat = null;

            _inputMat?.Dispose();
            _inputMat = null;
        }

        private void CreateOrRecreateProcessingResources(Mat frameMat)
        {
            if (frameMat == null)
            {
                return;
            }

            DisposeFrameProcessingResources();

            _bgrMat = new Mat(frameMat.rows(), frameMat.cols(), CvType.CV_8UC3);
        }

        private void CleanupPreviewResources()
        {
            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
                _texture = null;
            }

            UpdateFpsMonitorPlaybackState();
        }

        private void UpdateFpsMonitorPlaybackState()
        {
            if (_fpsMonitor == null || _multiSourceToMatHelper == null)
            {
                return;
            }

            _fpsMonitor.Add("PlaybackState", GetPlaybackStateText());
        }

        private string GetPlaybackStateText()
        {
            if (!_multiSourceToMatHelper.IsInitialized)
            {
                return "Uninitialized";
            }

            if (_multiSourceToMatHelper.IsPlaying)
            {
                return "Playing";
            }

            if (_multiSourceToMatHelper.IsPaused)
            {
                return "Paused";
            }

            return "Ready";
        }

        private void WireSourceToMatControlPanelHooks()
        {
            _controlPanel = GetComponent<SourceToMatControlPanel>();
            if (_controlPanel == null)
            {
                return;
            }

            _controlPanel.OnAfterPlay.AddListener(OnControlPanelAfterPlay);
            _controlPanel.OnAfterPause.AddListener(OnControlPanelAfterPause);
            _controlPanel.OnAfterStop.AddListener(OnControlPanelAfterStop);
            _controlPanel.OnRotate90Changed.AddListener(OnControlPanelRotate90Changed);
            _controlPanel.OnFlipVerticalChanged.AddListener(OnControlPanelFlipVerticalChanged);
            _controlPanel.OnFlipHorizontalChanged.AddListener(OnControlPanelFlipHorizontalChanged);
            _controlPanel.OnHelperKindChanged.AddListener(OnControlPanelHelperKindChanged);
        }

        private void UnwireSourceToMatControlPanelHooks()
        {
            if (_controlPanel == null)
            {
                return;
            }

            _controlPanel.OnAfterPlay.RemoveListener(OnControlPanelAfterPlay);
            _controlPanel.OnAfterPause.RemoveListener(OnControlPanelAfterPause);
            _controlPanel.OnAfterStop.RemoveListener(OnControlPanelAfterStop);
            _controlPanel.OnRotate90Changed.RemoveListener(OnControlPanelRotate90Changed);
            _controlPanel.OnFlipVerticalChanged.RemoveListener(OnControlPanelFlipVerticalChanged);
            _controlPanel.OnFlipHorizontalChanged.RemoveListener(OnControlPanelFlipHorizontalChanged);
            _controlPanel.OnHelperKindChanged.RemoveListener(OnControlPanelHelperKindChanged);
            _controlPanel = null;
        }

        protected virtual FaceDetection5LandmarkData[] Detect(Mat image)
        {
            // Preserve aspect ratio so the network sees undistorted proportions; setInputSize must match _inputMat.
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
                if (_inputMat == null)
                {
                    _inputMat = new Mat();
                }

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

                // Each row: x, y, w, h, 5 landmark pairs, score — scale bbox back to original image coordinates.
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
            public readonly OpenCVForUnity.Extensions.Vec2f RightEye;
            public readonly OpenCVForUnity.Extensions.Vec2f LeftEye;
            public readonly OpenCVForUnity.Extensions.Vec2f Nose;
            public readonly OpenCVForUnity.Extensions.Vec2f RightMouth;
            public readonly OpenCVForUnity.Extensions.Vec2f LeftMouth;

            // Confidence score [0, 1]
            public readonly float Score;

            public const int LANDMARK_VEC2F_COUNT = 5;
            public const int LANDMARK_ELEMENT_COUNT = 2 * LANDMARK_VEC2F_COUNT;
            public const int ELEMENT_COUNT = 4 + LANDMARK_ELEMENT_COUNT + 1;
            public const int DATA_SIZE = ELEMENT_COUNT * 4;

            public FaceDetection5LandmarkData(float x, float y, float width, float height, OpenCVForUnity.Extensions.Vec2f rightEye, OpenCVForUnity.Extensions.Vec2f leftEye, OpenCVForUnity.Extensions.Vec2f nose, OpenCVForUnity.Extensions.Vec2f rightMouth, OpenCVForUnity.Extensions.Vec2f leftMouth, float score)
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

            public override readonly string ToString()
            {
                return $"FaceDetection5LandmarkData(X:{X} Y:{Y} Width:{Width} Height:{Height} RightEye:{RightEye.ToString()} LeftEye:{LeftEye.ToString()} Nose:{Nose.ToString()} RightMouth:{RightMouth.ToString()} LeftMouth:{LeftMouth.ToString()} Score:{Score})";
            }
        }
    }
}

#endif
