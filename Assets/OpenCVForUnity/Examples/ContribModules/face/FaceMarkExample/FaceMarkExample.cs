using System;
using System.Collections.Generic;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.FaceModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Optimization;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using OpenCVForUnity.XobjdetectModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Face Mark Example
    /// Detects faces and fits 68-point facial landmarks on input frames.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Haar cascade face detection on a downscaled grayscale frame
    /// - LBF facemark model fitting for each detected face
    /// - Restoring coordinates to full resolution and drawing landmark contours
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Scalar"/>, <see cref="Point"/>, <see cref="Rect"/>
    /// - <see cref="CascadeClassifier"/>: detectMultiScale
    /// - <see cref="Facemark"/>, Face.createFacemarkLBF, loadModel, fit
    /// - <see cref="Imgproc"/>: cvtColor, equalizeHist, rectangle, line
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="ImageOptimizationHelper"/>
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://github.com/spmallick/GSOC2017/blob/master/data/lbfmodel.yaml
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class FaceMarkExample : MonoBehaviour
    {
        // Constants
        private static readonly string FACEMARK_CASCADE_FRONTALFACE_FILEPATH = "OpenCVForUnityExamples/objdetect/haarcascade_frontalface_alt.xml";

        private static readonly string FACEMARK_MODEL_FILEPATH = "OpenCVForUnityExamples/face/lbfmodel.yaml";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
        private Mat _grayMat;
        private Texture2D _texture;
        private CascadeClassifier _cascade;
        private MatOfRect _faces;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private ImageOptimizationHelper _imageOptimizationHelper;
        private Facemark _facemark;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            WireSourceToMatControlPanelHooks();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            string facemark_cascade_filepath = await OpenCVForUnityEnv.GetFilePathAsync(FACEMARK_CASCADE_FRONTALFACE_FILEPATH, cancellationToken: _cts.Token);
            string facemark_model_filepath = await OpenCVForUnityEnv.GetFilePathAsync(FACEMARK_MODEL_FILEPATH, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            if (string.IsNullOrEmpty(facemark_cascade_filepath))
            {
                Debug.LogError(FACEMARK_CASCADE_FRONTALFACE_FILEPATH + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "cascade classifier file is not loaded.\nPlease read console message.";
                }
            }
            else
            {
                // setup face detector —requires haarcascade_frontalface_alt.xml in StreamingAssets
                _cascade = new CascadeClassifier(facemark_cascade_filepath);
            }

            if (string.IsNullOrEmpty(facemark_model_filepath))
            {
                Debug.LogError(FACEMARK_MODEL_FILEPATH + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "model file is not loaded.\nPlease read console message.";
                }
            }
            else
            {
                // setup landmarks detector —LBF model must be downloaded to StreamingAssets/face/
                _facemark = Face.createFacemarkLBF();
                _facemark.loadModel(facemark_model_filepath);
            }

            if (_cascade == null || _facemark == null)
            {
                return;
            }

            _multiSourceToMatHelper.Initialize();
        }

        private void OnDestroy()
        {
            UnwireSourceToMatControlPanelHooks();

            _cts?.Cancel();

            _cascade?.Dispose();
            _cascade = null;

            _facemark?.Dispose();
            _facemark = null;

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

            if (_imageOptimizationHelper.IsCurrentFrameSkipped())
            {
                return;
            }

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;

            if (_cascade != null && _facemark != null)
            {
                // Run detection on a smaller Mat for speed; landmarks are scaled back later.
                Mat downScaleRgbaMat = _imageOptimizationHelper.GetDownScaleMat((rgbaMat));

                Imgproc.cvtColor(downScaleRgbaMat, _grayMat, Imgproc.COLOR_RGBA2GRAY);
                // Histogram equalization improves cascade detection under uneven lighting.
                Imgproc.equalizeHist(_grayMat, _grayMat);

                // minSize filters out small false positives relative to frame size.
                int minSize = (int)(Mathf.Max(_grayMat.width(), _grayMat.height()) * 0.1);
                _cascade.detectMultiScale(_grayMat, _faces, 1.1, 2, 0 | Xobjdetect.CASCADE_SCALE_IMAGE,
                    (minSize, minSize));

                if (_faces.total() > 0)
                {
                    // fit() outputs one MatOfPoint2f landmark set per detected face.
                    List<MatOfPoint2f> landmarks = new List<MatOfPoint2f>();
                    _facemark.fit(_grayMat, _faces, landmarks);

                    // Map face rectangles from downscaled coordinates back to full resolution.
                    using (MatOfRect originalSizeFaces = _imageOptimizationHelper.RestoreOriginalSizeMatOfRect(_faces))
                    {
                        // draw the detected faces
                        Rect[] rects = originalSizeFaces.toArray();
                        for (int i = 0; i < rects.Length; i++)
                        {
                            //Debug.Log ("detect faces " + rects [i]);

                            Imgproc.rectangle(rgbaMat, (rects[i].x, rects[i].y), (rects[i].x + rects[i].width, rects[i].y + rects[i].height), (255, 0, 0, 255), 2);
                        }
                    }

                    for (int i = 0; i < landmarks.Count; i++)
                    {
                        // Map landmark points from downscaled coordinates back to full resolution.
                        using (MatOfPoint2f originalSizeLandmarks = _imageOptimizationHelper.RestoreOriginalSizeMatOfPoint2f(landmarks[i]))
                        {
                            // draw the detected landmarks
                            List<Point> points = originalSizeLandmarks.toList();
                            DrawFaceLandmark(rgbaMat, points, new Scalar(0, 255, 0, 255), 2);

                            //Scalar circleColor = new Scalar(255, 0, 0, 255);
                            //foreach (Point p in points)
                            //    Imgproc.circle(rgbaMat, p, 2, circleColor, -1);
                        }
                    }

                    foreach (MatOfPoint2f landmark in landmarks)
                    {
                        landmark.Dispose();
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
            _grayMat?.Dispose();
            _grayMat = null;
            _faces?.Dispose();
            _faces = null;
        }

        private void CreateOrRecreateProcessingResources(Mat rgbaMat)
        {
            if (rgbaMat == null)
            {
                return;
            }

            DisposeFrameProcessingResources();

            Mat downScaleMat = _imageOptimizationHelper.GetDownScaleMat(rgbaMat);
            _grayMat = new Mat(downScaleMat.rows(), downScaleMat.cols(), CvType.CV_8UC1);
            _faces = new MatOfRect();
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

        private void DrawFaceLandmark(Mat imgMat, List<Point> points, Scalar color, int thickness, bool drawIndexNumbers = false)
        {
            if (points.Count == 5)
            {

                Imgproc.line(imgMat, points[0], points[1], color, thickness);
                Imgproc.line(imgMat, points[1], points[4], color, thickness);
                Imgproc.line(imgMat, points[4], points[3], color, thickness);
                Imgproc.line(imgMat, points[3], points[2], color, thickness);

            }
            else if (points.Count == 68)
            {

                for (int i = 1; i <= 16; ++i)
                {
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                }

                for (int i = 28; i <= 30; ++i)
                {
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                }

                for (int i = 18; i <= 21; ++i)
                {
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                }

                for (int i = 23; i <= 26; ++i)
                {
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                }

                for (int i = 31; i <= 35; ++i)
                {
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                }

                Imgproc.line(imgMat, points[30], points[35], color, thickness);

                for (int i = 37; i <= 41; ++i)
                {
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                }

                Imgproc.line(imgMat, points[36], points[41], color, thickness);

                for (int i = 43; i <= 47; ++i)
                {
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                }

                Imgproc.line(imgMat, points[42], points[47], color, thickness);

                for (int i = 49; i <= 59; ++i)
                {
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                }

                Imgproc.line(imgMat, points[48], points[59], color, thickness);

                for (int i = 61; i <= 67; ++i)
                {
                    Imgproc.line(imgMat, points[i], points[i - 1], color, thickness);
                }

                Imgproc.line(imgMat, points[60], points[67], color, thickness);
            }
            else
            {
                for (int i = 0; i < points.Count; i++)
                {
                    Imgproc.circle(imgMat, points[i], 2, color, -1);
                }
            }

            // Draw the index number of facelandmark points.
            if (drawIndexNumbers)
            {
                Scalar textColor = new Scalar(255, 255, 255, 255);
                for (int i = 0; i < points.Count; ++i)
                {
                    Imgproc.putText(imgMat, i.ToString(), points[i], Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, textColor, 1, Imgproc.LINE_AA, false);
                }
            }
        }
    }
}
