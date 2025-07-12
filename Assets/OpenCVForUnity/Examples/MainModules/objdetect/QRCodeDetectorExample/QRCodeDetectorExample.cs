using System;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// QRCodeDetector Example
    /// An example of detecting QRCode using the QRCodeDetector class.
    /// https://github.com/opencv/opencv/blob/master/samples/cpp/qrcode.cpp
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class QRCodeDetectorExample : MonoBehaviour
    {
        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
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
        private QRCodeDetector _detector;

        /// <summary>
        /// The points.
        /// </summary>
        private Mat _points;

        /// <summary>
        /// The decoded info
        /// </summary>
        private List<string> _decodedInfo;

        /// <summary>
        /// The straight qrcode
        /// </summary>
        private List<Mat> _straightQrcode;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;

            _detector = new QRCodeDetector();

            _multiSource2MatHelper.Initialize();
        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                Imgproc.cvtColor(rgbaMat, _grayMat, Imgproc.COLOR_RGBA2GRAY);

                bool result = _detector.detectAndDecodeMulti(_grayMat, _decodedInfo, _points, _straightQrcode);

                if (result)
                {
                    // Debug.Log(_points.dump());
                    // Debug.Log(_points.ToString());

                    // Debug.Log("_decodedInfo.Count " + _decodedInfo.Count);
                    // Debug.Log("_straightQrcode.Count " + _straightQrcode.Count);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
                    // draw QRCode contour using non-allocating methods.
                    ReadOnlySpan<float> qrCodeCorners = _points.AsSpan<float>();
#else
                    // draw QRCode contour using allocating methods.
                    float[] qrCodeCorners = new float[_points.total() * _points.channels()];
                    _points.get(0, 0, qrCodeCorners);
#endif

                    // Debug.Log("qrCodeCorners.Length " + qrCodeCorners.Length);

                    for (int i = 0; i < qrCodeCorners.Length; i += 8)
                    {
                        // Draw QR code bounding box by connecting the 4 corners
                        for (int cornerIndex = 0; cornerIndex < 4; cornerIndex++)
                        {
                            int currentCorner = i + cornerIndex * 2;
                            int nextCorner = i + ((cornerIndex + 1) % 4) * 2;

                            Imgproc.line(rgbaMat,
                                new Point(qrCodeCorners[currentCorner], qrCodeCorners[currentCorner + 1]),
                                new Point(qrCodeCorners[nextCorner], qrCodeCorners[nextCorner + 1]),
                                new Scalar(255, 0, 0, 255), 2);
                        }

                        // Display decoded information
                        int qrCodeIndex = i / 8;
                        if (_decodedInfo.Count > qrCodeIndex && _decodedInfo[qrCodeIndex] != null)
                        {
                            Imgproc.putText(rgbaMat, _decodedInfo[qrCodeIndex],
                                new Point(qrCodeCorners[i], qrCodeCorners[i + 1]),
                                Imgproc.FONT_HERSHEY_SIMPLEX, 0.7,
                                new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        }
                    }

                    // Display straightQrcode using imshow
                    for (int i = 0; i < _straightQrcode.Count; i++)
                    {
                        DebugMat.imshow("straightQrcode[" + i + "]", _straightQrcode[i], false, null, _decodedInfo[i]);
                    }
                }
                else
                {
                    Imgproc.putText(rgbaMat, "Decoding failed.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            _detector?.Dispose();
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

            _points = new Mat();
            _decodedInfo = new List<string>();
            _straightQrcode = new List<Mat>();

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

            _points?.Dispose();

            _decodedInfo?.Clear();

            foreach (var item in _straightQrcode)
            {
                item?.Dispose();
            }
            _straightQrcode?.Clear();
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
    }
}
