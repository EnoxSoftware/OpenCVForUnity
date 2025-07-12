using System;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// QRCodeDetector Example
    /// An example of QRCode detection using the QRCodeDetector class.
    /// https://github.com/opencv/opencv/blob/master/samples/cpp/qrcode.cpp
    /// </summary>
    public class QRCodeDetectorImageExample : MonoBehaviour
    {
        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        // Unity Lifecycle Methods
        private void Start()
        {
            Run();
        }

        private void Update()
        {

        }

        private void OnDestroy()
        {

        }

        // Public Methods
        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        // Private Methods
        private void Run()
        {
            Texture2D imgTexture = Resources.Load("link_github_ocv") as Texture2D;

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            OpenCVMatUtils.Texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString());

            Mat grayMat = new Mat();
            Imgproc.cvtColor(imgMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

            Mat points = new Mat();
            List<string> decodedInfo = new List<string>();
            List<Mat> straightQrcode = new List<Mat>();

            QRCodeDetector detector = new QRCodeDetector();

            bool result = detector.detectAndDecodeMulti(grayMat, decodedInfo, points, straightQrcode);

            if (result)
            {

                // Debug.Log(points.dump());
                // Debug.Log(points.ToString());

                // Debug.Log("decodedInfo.Count " + decodedInfo.Count);
                // Debug.Log("straightQrcode.Count " + straightQrcode.Count);

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
                // draw QRCode contour using non-allocating methods.
                ReadOnlySpan<float> qrCodeCorners = points.AsSpan<float>();
#else
                // draw QRCode contour using allocating methods.
                float[] qrCodeCorners = new float[points.total() * points.channels()];
                points.get(0, 0, qrCodeCorners);
#endif

                // Debug.Log("qrCodeCorners.Length " + qrCodeCorners.Length);

                for (int i = 0; i < qrCodeCorners.Length; i += 8)
                {
                    // Draw QR code bounding box by connecting the 4 corners
                    for (int cornerIndex = 0; cornerIndex < 4; cornerIndex++)
                    {
                        int currentCorner = i + cornerIndex * 2;
                        int nextCorner = i + ((cornerIndex + 1) % 4) * 2;

                        Imgproc.line(imgMat,
                            new Point(qrCodeCorners[currentCorner], qrCodeCorners[currentCorner + 1]),
                            new Point(qrCodeCorners[nextCorner], qrCodeCorners[nextCorner + 1]),
                            new Scalar(255, 0, 0, 255), 2);
                    }

                    // Display decoded information
                    int qrCodeIndex = i / 8;
                    if (decodedInfo.Count > qrCodeIndex && decodedInfo[qrCodeIndex] != null)
                    {
                        Imgproc.putText(imgMat, decodedInfo[qrCodeIndex],
                            new Point(qrCodeCorners[i], qrCodeCorners[i + 1]),
                            Imgproc.FONT_HERSHEY_SIMPLEX, 0.7,
                            new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    }
                }

                // Display straightQrcode using imshow
                for (int i = 0; i < straightQrcode.Count; i++)
                {
                    DebugMat.imshow("straightQrcode[" + i + "]", straightQrcode[i], false, null, decodedInfo[i]);
                }
            }
            else
            {
                Imgproc.putText(imgMat, "Decoding failed.", new Point(5, imgMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
            }

            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUtils.MatToTexture2D(imgMat, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
        }
    }
}
