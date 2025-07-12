#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.Wechat_qrcodeModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// WeChatQRCode Example
    /// An example of detecting QRCode using the WeChatQRCode class.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/main/models/qrcode_wechatqrcode
    ///
    /// [Tested Models]
    /// detect_2021nov.prototxt https://github.com/opencv/opencv_zoo/raw/661ca25ce59ccf7505cc79bf788bfb4a888ff314/models/qrcode_wechatqrcode/detect_2021nov.prototxt
    /// detect_2021nov.caffemodel https://github.com/opencv/opencv_zoo/raw/661ca25ce59ccf7505cc79bf788bfb4a888ff314/models/qrcode_wechatqrcode/detect_2021nov.caffemodel
    /// sr_2021nov.prototxt https://github.com/opencv/opencv_zoo/raw/661ca25ce59ccf7505cc79bf788bfb4a888ff314/models/qrcode_wechatqrcode/sr_2021nov.prototxt
    /// sr_2021nov.caffemodel https://github.com/opencv/opencv_zoo/raw/661ca25ce59ccf7505cc79bf788bfb4a888ff314/models/qrcode_wechatqrcode/sr_2021nov.caffemodel
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class WeChatQRCodeDetectorExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// Path to a .prototxt file contains trained detection network.
        /// </summary>
        private static readonly string DETECTPROTOTXT_FILENAME = "OpenCVForUnityExamples/wechat_qrcode/detect_2021nov.prototxt";

        /// <summary>
        /// Path to a binary .caffemodel file contains trained detection network.
        /// </summary>
        private static readonly string DETECTMODEL_FILENAME = "OpenCVForUnityExamples/wechat_qrcode/detect_2021nov.caffemodel";

        /// <summary>
        /// Path to a .prototxt file contains trained super resolution network.
        /// </summary>
        private static readonly string SRPROTOTXT_FILENAME = "OpenCVForUnityExamples/wechat_qrcode/sr_2021nov.prototxt";

        /// <summary>
        /// Path to a binary .caffemodel file contains trained super resolution network.
        /// </summary>
        private static readonly string SRMODEL_FILENAME = "OpenCVForUnityExamples/wechat_qrcode/sr_2021nov.caffemodel";

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
        /// <summary>
        /// The detect prototxt filepath.
        /// </summary>
        private string _detectprototxtFilepath;

        /// <summary>
        /// The detect model filepath.
        /// </summary>
        private string _detectmodelFilepath;

        /// <summary>
        /// The sr prototxt filepath.
        /// </summary>
        private string _srprototxtFilepath;

        /// <summary>
        /// The sr model filepath.
        /// </summary>
        private string _srmodelFilepath;

        /// <summary>
        /// The gray mat.
        /// </summary>
        private Mat _grayMat;

        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The QRCode detector.
        /// </summary>
        private WeChatQRCode _detector;

        /// <summary>
        /// The points.
        /// </summary>
        private List<Mat> _points;

        /// <summary>
        /// The decoded info
        /// </summary>
        private List<string> _decodedInfo;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

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

            // Reflect initial toggle value
            UseDNNToggle.isOn = UseDNN;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _detectmodelFilepath = await OpenCVEnv.GetFilePathTaskAsync(DETECTMODEL_FILENAME, cancellationToken: _cts.Token);
            _detectprototxtFilepath = await OpenCVEnv.GetFilePathTaskAsync(DETECTPROTOTXT_FILENAME, cancellationToken: _cts.Token);
            _srmodelFilepath = await OpenCVEnv.GetFilePathTaskAsync(SRMODEL_FILENAME, cancellationToken: _cts.Token);
            _srprototxtFilepath = await OpenCVEnv.GetFilePathTaskAsync(SRPROTOTXT_FILENAME, cancellationToken: _cts.Token);


            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();

        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                Imgproc.cvtColor(rgbaMat, _grayMat, Imgproc.COLOR_RGBA2GRAY);

                if (_detector == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    _decodedInfo = _detector.detectAndDecode(_grayMat, _points);

                    if (_points.Count > 0)
                    {
                        // Debug.Log($"Total points detected: {points.Count}");
                        for (int i = 0; i < _points.Count; i++)
                        {
                            // Get coordinates of each point
#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
                            // draw QRCode contour using non-allocating methods.
                            ReadOnlySpan<float> qrCodeCorners = _points[i].AsSpan<float>();
#else
                            // draw QRCode contour using allocating methods.
                            float[] qrCodeCorners = new float[8];
                            _points[i].get(0, 0, qrCodeCorners);
#endif

                            // Draw QR code bounding box by connecting the 4 corners
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

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            _detector?.Dispose();

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

            _grayMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);

            _points = new List<Mat>();
            _decodedInfo = new List<string>();

#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API
            // If the WebCam is front facing, flip the Mat horizontally. Required for successful detection.
            if (_multiSource2MatHelper.Source2MatHelper is WebCamTexture2MatHelper webCamHelper)
                webCamHelper.FlipHorizontal = webCamHelper.IsFrontFacing();
#endif
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _grayMat?.Dispose();

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;

            foreach (var item in _points)
            {
                item?.Dispose();
            }
            _points?.Clear();

            _decodedInfo?.Clear();
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

        /// <summary>
        /// Raises the use DNN toggle value changed event.
        /// </summary>
        public void OnUseDNNToggleValueChanged()
        {
            if (UseDNNToggle.isOn != UseDNN)
            {
                UseDNN = UseDNNToggle.isOn;
                Run();
            }
        }

        // Private Methods
        private void Run()
        {
            // Dispose existing detector
            _detector?.Dispose();

            // Create new detector
            if (UseDNN)
            {
                if (string.IsNullOrEmpty(_detectprototxtFilepath) || string.IsNullOrEmpty(_detectmodelFilepath) ||
                    string.IsNullOrEmpty(_srprototxtFilepath) || string.IsNullOrEmpty(_srmodelFilepath))
                {
                    Debug.LogError(DETECTPROTOTXT_FILENAME + " or " + DETECTMODEL_FILENAME + " or " +
                        SRPROTOTXT_FILENAME + " or " + SRMODEL_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
                    _detector = null;
                }
                else
                {
                    _detector = new WeChatQRCode(_detectprototxtFilepath, _detectmodelFilepath, _srprototxtFilepath, _srmodelFilepath);
                }
            }
            else
            {
                _detector = new WeChatQRCode();
            }

            _multiSource2MatHelper.Initialize();
        }
    }
}

#endif
