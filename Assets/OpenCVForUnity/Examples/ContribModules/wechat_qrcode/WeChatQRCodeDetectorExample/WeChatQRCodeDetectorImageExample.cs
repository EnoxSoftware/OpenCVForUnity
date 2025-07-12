#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.Wechat_qrcodeModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// WeChatQRCode Example
    /// An example of QRCode detection using the WeChatQRCode class.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/main/models/qrcode_wechatqrcode
    ///
    /// [Tested Models]
    /// detect_2021nov.prototxt https://github.com/opencv/opencv_zoo/raw/661ca25ce59ccf7505cc79bf788bfb4a888ff314/models/qrcode_wechatqrcode/detect_2021nov.prototxt
    /// detect_2021nov.caffemodel https://github.com/opencv/opencv_zoo/raw/661ca25ce59ccf7505cc79bf788bfb4a888ff314/models/qrcode_wechatqrcode/detect_2021nov.caffemodel
    /// sr_2021nov.prototxt https://github.com/opencv/opencv_zoo/raw/661ca25ce59ccf7505cc79bf788bfb4a888ff314/models/qrcode_wechatqrcode/sr_2021nov.prototxt
    /// sr_2021nov.caffemodel https://github.com/opencv/opencv_zoo/raw/661ca25ce59ccf7505cc79bf788bfb4a888ff314/models/qrcode_wechatqrcode/sr_2021nov.caffemodel
    /// </summary>
    public class WeChatQRCodeDetectorImageExample : MonoBehaviour
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
        /// The CancellationTokenSource.
        /// </summary>
        private CancellationTokenSource _cts = new CancellationTokenSource();

        private Texture2D _imgTexture;

        // Unity Lifecycle Methods
        private async void Start()
        {
            // Reflect initial toggle value
            UseDNNToggle.isOn = UseDNN;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            _detectmodelFilepath = await OpenCVEnv.GetFilePathTaskAsync(DETECTMODEL_FILENAME, cancellationToken: _cts.Token);
            _detectprototxtFilepath = await OpenCVEnv.GetFilePathTaskAsync(DETECTPROTOTXT_FILENAME, cancellationToken: _cts.Token);
            _srmodelFilepath = await OpenCVEnv.GetFilePathTaskAsync(SRMODEL_FILENAME, cancellationToken: _cts.Token);
            _srprototxtFilepath = await OpenCVEnv.GetFilePathTaskAsync(SRPROTOTXT_FILENAME, cancellationToken: _cts.Token);

            // Load initial image
            _imgTexture = Resources.Load("link_github_ocv") as Texture2D;

            Run();
        }

        private void Update()
        {

        }

        private void OnDestroy()
        {
            _cts?.Dispose();
        }

        private void OnDisable()
        {
            _cts?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
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
            // Initialize image
            Mat imgMat = new Mat(_imgTexture.height, _imgTexture.width, CvType.CV_8UC4);
            OpenCVMatUtils.Texture2DToMat(_imgTexture, imgMat);
            Mat grayMat = new Mat();
            Imgproc.cvtColor(imgMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

            // Create detector
            WeChatQRCode detector = null;
            if (UseDNN)
            {
                if (string.IsNullOrEmpty(_detectprototxtFilepath) || string.IsNullOrEmpty(_detectmodelFilepath) ||
                    string.IsNullOrEmpty(_srprototxtFilepath) || string.IsNullOrEmpty(_srmodelFilepath))
                {
                    Debug.LogError(DETECTPROTOTXT_FILENAME + " or " + DETECTMODEL_FILENAME + " or " +
                        SRPROTOTXT_FILENAME + " or " + SRMODEL_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
                }
                else
                {
                    detector = new WeChatQRCode(_detectprototxtFilepath, _detectmodelFilepath, _srprototxtFilepath, _srmodelFilepath);
                }
            }
            else
            {
                detector = new WeChatQRCode();
            }

            if (detector == null)
            {
                Imgproc.putText(imgMat, "model file is not loaded.", new Point(5, imgMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(imgMat, "Please read console message.", new Point(5, imgMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
            }
            else
            {
                List<Mat> points = new List<Mat>();
                List<string> decodedInfo = detector.detectAndDecode(grayMat, points);

                if (points.Count > 0)
                {
                    // Debug.Log($"Total points detected: {points.Count}");
                    for (int i = 0; i < points.Count; i++)
                    {
                        // Get coordinates of each point
#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
                        // draw QRCode contour using non-allocating methods.
                        ReadOnlySpan<float> qrCodeCorners = points[i].AsSpan<float>();
#else
                        // draw QRCode contour using allocating methods.
                        float[] qrCodeCorners = new float[8];
                        points[i].get(0, 0, qrCodeCorners);
#endif

                        // Draw QR code bounding box by connecting the 4 corners
                        for (int j = 0; j < 4; j++)
                        {
                            int currentIndex = j * 2;
                            int nextIndex = ((j + 1) % 4) * 2;
                            Imgproc.line(imgMat,
                                new Point(qrCodeCorners[currentIndex], qrCodeCorners[currentIndex + 1]),
                                new Point(qrCodeCorners[nextIndex], qrCodeCorners[nextIndex + 1]),
                                new Scalar(255, 0, 0, 255), 2);
                        }

                        // Display decoded information
                        if (decodedInfo.Count > i && decodedInfo[i] != null)
                        {
                            Imgproc.putText(imgMat, decodedInfo[i],
                                new Point(qrCodeCorners[0], qrCodeCorners[1]),
                                Imgproc.FONT_HERSHEY_SIMPLEX, 0.7,
                                new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        }
                    }
                }
                else
                {
                    Imgproc.putText(imgMat, "Decoding failed.",
                        new Point(5, imgMat.rows() - 10),
                        Imgproc.FONT_HERSHEY_SIMPLEX, 0.7,
                        new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
            }

            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(imgMat, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;

            // Release resources
            detector?.Dispose();
            imgMat?.Dispose();
            grayMat?.Dispose();
        }
    }
}

#endif
