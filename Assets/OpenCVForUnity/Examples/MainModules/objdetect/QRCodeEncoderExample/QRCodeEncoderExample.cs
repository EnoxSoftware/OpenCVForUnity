using System;
using System.IO;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// QRCode Encoder Example
    /// An example of QRCode encoding using the QRCodeEncoder class.
    /// https://docs.opencv.org/4.x/d2/dbb/classcv_1_1QRCodeEncoder.html
    /// </summary>
    public class QRCodeEncoderExample : MonoBehaviour
    {
        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        /// <summary>
        /// The size of the output QRCode image (px).
        /// </summary>
        public int QrCodeSize = 1000;

        /// <summary>
        /// The encoded info string.
        /// </summary>
        public string EncodedInfo = "";

        /// <summary>
        /// The encoded info input field.
        /// </summary>
        public InputField EncodedInfoInputField;

        /// <summary>
        /// The save path input field.
        /// </summary>
        public InputField SavePathInputField;

        // Private Fields
        /// <summary>
        /// The QRcode encoder.
        /// </summary>
        private QRCodeEncoder _qrCodeEncoder;

        /// <summary>
        /// The QRCode img mat.
        /// </summary>
        private Mat _qrCodeImg;

        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        // Unity Lifecycle Methods
        private void Start()
        {
            _qrCodeImg = new Mat(QrCodeSize, QrCodeSize, CvType.CV_8UC3);
            _texture = new Texture2D(_qrCodeImg.cols(), _qrCodeImg.rows(), TextureFormat.RGB24, false);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;

            EncodedInfoInputField.text = EncodedInfo;

            _qrCodeEncoder = QRCodeEncoder.create();

            CreateQRCodeImg();
        }

        private void Update()
        {

        }

        private void OnDestroy()
        {
            _qrCodeEncoder?.Dispose();

            _qrCodeImg?.Dispose();
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
        /// Raises the encoded info input field value changed event.
        /// </summary>
        public void OnEncodedInfoInputFieldValueChanged(string result)
        {
            EncodedInfo = result;
        }

        /// <summary>
        /// Raises the create QRCode img button click event.
        /// </summary>
        public void OnCreateQRCodeImgButtonClick()
        {
            CreateQRCodeImg();
        }

        /// <summary>
        /// Raises the save QRCode img button click event.
        /// </summary>
        public void OnSaveQRCodeImgButtonClick()
        {
            SaveQRCodeImg();
        }

        // Private Methods
        private void CreateQRCodeImg()
        {
            if (_qrCodeImg.cols() != QrCodeSize)
            {
                _qrCodeImg.Dispose();
                _qrCodeImg = new Mat(QrCodeSize, QrCodeSize, CvType.CV_8UC3);
                _texture = new Texture2D(_qrCodeImg.cols(), _qrCodeImg.rows(), TextureFormat.RGB24, false);
            }
            else
            {
                _qrCodeImg.setTo(Scalar.all(255));
            }

            // Encode QRCode.
            using (Mat qrcodeGRAY = new Mat())
            {
                _qrCodeEncoder.encode(EncodedInfo, qrcodeGRAY);

                if (!qrcodeGRAY.empty())
                {
                    using (Mat qrcodeRGB = new Mat(qrcodeGRAY.size(), CvType.CV_8UC3))
                    {
                        Imgproc.cvtColor(qrcodeGRAY, qrcodeRGB, Imgproc.COLOR_GRAY2RGB);
                        Imgproc.resize(qrcodeRGB, _qrCodeImg, _qrCodeImg.size(), 0, 0, Imgproc.INTER_NEAREST);
                    }
                }
                else
                {
                    Imgproc.putText(_qrCodeImg, "Too much encoded info.", new Point(5, _qrCodeImg.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 2, new Scalar(0, 0, 0, 255), 2, Imgproc.LINE_AA, false);
                }
            }

            OpenCVMatUtils.MatToTexture2D(_qrCodeImg, _texture);
        }

        private void SaveQRCodeImg()
        {
            // save the QRCodeImg.
            string saveDirectoryPath = Path.Combine(Application.persistentDataPath, "QRCodeEncoderExample");
            string savePath = "";
#if UNITY_WEBGL && !UNITY_EDITOR
            string format = "jpg";
            MatOfInt compressionParams = new MatOfInt(Imgcodecs.IMWRITE_JPEG_QUALITY, 100);
#else
            string format = "png";
            MatOfInt compressionParams = new MatOfInt(Imgcodecs.IMWRITE_PNG_COMPRESSION, 0);
#endif

            savePath = Path.Combine(saveDirectoryPath, Uri.EscapeDataString(EncodedInfo) + "." + format);

            if (!Directory.Exists(saveDirectoryPath))
            {
                Directory.CreateDirectory(saveDirectoryPath);
            }

            Imgcodecs.imwrite(savePath, _qrCodeImg, compressionParams);

            SavePathInputField.text = savePath;
            Debug.Log("savePath: " + savePath);
        }
    }
}
