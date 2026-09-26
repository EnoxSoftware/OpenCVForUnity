#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using OpenCVForUnity.Wechat_qrcodeModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// WeChat QR Code Detector Example
    /// Detects and decodes QR codes from input frames using the WeChat QR code model.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Real-time frame capture via MultiSourceToMatHelper
    /// - DNN-based vs built-in WeChat QR code detection (toggle in UI)
    /// - Overlaying decoded text and corner polygons on each frame
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Scalar"/>, <see cref="Point"/>
    /// - <see cref="WeChatQRCode"/>: detectAndDecode
    /// - <see cref="Imgproc"/>: cvtColor, line, putText
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://huggingface.co/opencv/opencv_contribution/tree/main/wechat_qr
    /// </para>
    /// <para>
    /// [Tested Models]
    /// detect.onnx https://huggingface.co/opencv/opencv_contribution/resolve/main/wechat_qr/detect_2026april.onnx
    /// sr.onnx https://huggingface.co/opencv/opencv_contribution/resolve/main/wechat_qr/sr_2026april.onnx
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class WeChatQRCodeDetectorExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// Path to an ONNX file for the QR code detector network.
        /// </summary>
        private static readonly string DETECT_MODEL_FILEPATH = "OpenCVForUnityExamples/wechat_qrcode/detect.onnx";

        /// <summary>
        /// Path to an ONNX file for the super resolution network.
        /// </summary>
        private static readonly string SR_MODEL_FILEPATH = "OpenCVForUnityExamples/wechat_qrcode/sr.onnx";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Header("UI")]
        public Toggle UseDNNToggle;
        public bool UseDNN = true;

        // Private Fields
        private string _detectModelFilepath;
        private string _srModelFilepath;
        private Mat _grayMat;
        private Texture2D _texture;
        private WeChatQRCode _detector;
        private List<Mat> _points;
        private List<string> _decodedInfo;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            WireSourceToMatControlPanelHooks();

            // Reflect initial toggle value
            UseDNNToggle.isOn = UseDNN;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            _detectModelFilepath = await OpenCVForUnityEnv.GetFilePathAsync(DETECT_MODEL_FILEPATH, cancellationToken: _cts.Token);
            _srModelFilepath = await OpenCVForUnityEnv.GetFilePathAsync(SR_MODEL_FILEPATH, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            RecreateDetector();

            if (_detector == null)
            {
                return;
            }

            _multiSourceToMatHelper.Initialize();
        }

        private void OnDestroy()
        {
            UnwireSourceToMatControlPanelHooks();

            _cts?.Cancel();

            _detector?.Dispose();
            _detector = null;

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

            // WeChatQRCode expects a single-channel grayscale image.
            Imgproc.cvtColor(rgbaMat, _grayMat, Imgproc.COLOR_RGBA2GRAY);

            if (_detector != null)
            {
                // Reuses _points list; each entry is a Mat of 4 corner (x,y) coordinates.
                _decodedInfo = _detector.detectAndDecode(_grayMat, _points);

                if (_points.Count > 0)
                {
                    // Debug.Log($"Total points detected: {points.Count}");
                    for (int i = 0; i < _points.Count; i++)
                    {
                        // Each corner is stored as (x0,y0,x1,y1,...,x3,y3) — 8 floats total.
                        ReadOnlySpan<float> qrCodeCorners = _points[i].AsSpan<float>();

                        // Connect the four corners to draw the QR code outline on the color frame.
                        for (int j = 0; j < 4; j++)
                        {
                            int currentIndex = j * 2;
                            int nextIndex = ((j + 1) % 4) * 2;
                            Imgproc.line(rgbaMat,
                                new Point(qrCodeCorners[currentIndex], qrCodeCorners[currentIndex + 1]),
                                new Point(qrCodeCorners[nextIndex], qrCodeCorners[nextIndex + 1]),
                                new Scalar(255, 0, 0, 255), 2);
                        }

                        // Display decoded information
                        if (_decodedInfo.Count > i && _decodedInfo[i] != null)
                        {
                            Imgproc.putText(rgbaMat, _decodedInfo[i],
                                new Point(qrCodeCorners[0], qrCodeCorners[1]),
                                Imgproc.FONT_HERSHEY_SIMPLEX, 0.7,
                                new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        }
                    }
                }
                else
                {
                    Imgproc.putText(rgbaMat, "Decoding failed.",
                        new Point(5, rgbaMat.rows() - 10),
                        Imgproc.FONT_HERSHEY_SIMPLEX, 0.7,
                        new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
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

#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API
            // If the WebCam is front facing, flip the Mat horizontally. Required for successful detection.
            if (_multiSourceToMatHelper.ActiveHelper is WebCamTextureToMatHelper webCamHelper)
            {
                _multiSourceToMatHelper.FlipHorizontal = webCamHelper.IsFrontFacing;
            }
#endif

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

        /// <summary>
        /// Raises the use DNN toggle value changed event.
        /// </summary>
        public void OnUseDNNToggleValueChanged()
        {
            if (UseDNNToggle.isOn != UseDNN)
            {
                UseDNN = UseDNNToggle.isOn;
                RecreateDetector();
                WireSourceToMatControlPanelHooks();
                if (_detector == null)
                {
                    return;
                }

                _multiSourceToMatHelper.Initialize();
            }
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

            if (_points != null)
            {
                foreach (Mat item in _points)
                {
                    item?.Dispose();
                }

                _points.Clear();
            }

            _decodedInfo?.Clear();
        }

        private void CreateOrRecreateProcessingResources(Mat rgbaMat)
        {
            if (rgbaMat == null)
            {
                return;
            }

            DisposeFrameProcessingResources();

            _grayMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);

            // Reused each frame by detectAndDecode; do not recreate in Update().
            _points = new List<Mat>();
            _decodedInfo = new List<string>();
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

        /// <summary>
        /// Recreates the WeChat QR detector for the current <see cref="UseDNN"/> setting.
        /// DNN mode needs two ONNX model files in StreamingAssets; otherwise uses the built-in detector.
        /// </summary>
        private void RecreateDetector()
        {
            // Recreate detector when DNN toggle changes; must dispose the old instance first.
            _detector?.Dispose();

            // DNN mode needs two ONNX model files in StreamingAssets; otherwise use built-in detector.
            if (UseDNN)
            {
                if (string.IsNullOrEmpty(_detectModelFilepath) || string.IsNullOrEmpty(_srModelFilepath))
                {
                    Debug.LogError(DETECT_MODEL_FILEPATH + " or " + SR_MODEL_FILEPATH +
                        " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);
                    if (_fpsMonitor != null)
                    {
                        _fpsMonitor.ConsoleText = "model file is not loaded.\nPlease read console message.";
                    }

                    _detector = null;
                }
                else
                {
                    _detector = new WeChatQRCode(_detectModelFilepath, _srModelFilepath);
                }
            }
            else
            {
                _detector = new WeChatQRCode();
            }
        }
    }
}

#endif
