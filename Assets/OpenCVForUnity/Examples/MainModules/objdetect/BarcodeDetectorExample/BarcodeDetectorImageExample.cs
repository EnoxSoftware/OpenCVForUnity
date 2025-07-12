#if !UNITY_WSA_10_0

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
    /// BarcodeDetector Example
    /// An example of Barcode detection using the BarcodeDetector class.
    /// </summary>
    public class BarcodeDetectorImageExample : MonoBehaviour
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
            Texture2D imgTexture = Resources.Load("book") as Texture2D;

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            OpenCVMatUtils.Texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString());

            BarcodeDetector detector = new BarcodeDetector();

            // When using super resolution.
            // Please, download 'sr.*' from https://github.com/WeChatCV/opencv_3rdparty/tree/wechat_qrcode and put them into the StreamingAssets/OpenCVForUnityExamples/barcode directory.
            //BarcodeDetector detector = new BarcodeDetector(Utils.getFilePath("OpenCVForUnityExamples/barcode/sr.prototxt"), OpenCVEnv.GetFilePath("OpenCVForUnityExamples/barcode/sr.caffemodel"));

            List<string> decoded_info = new List<string>();
            List<string> decoded_type = new List<string>();
            Mat corners = new Mat();

            bool result_detection = detector.detectAndDecodeWithType(imgMat, decoded_info, decoded_type, corners);

            // draw Barcode contours.
            if (result_detection)
                DrawBarcodeResults(imgMat, corners, decoded_info, decoded_type);

            corners.Dispose();

            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUtils.MatToTexture2D(imgMat, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
        }

        // Private Methods
        private void DrawBarcodeResults(Mat frame, Mat corners, List<string> decoded_info, List<string> decoded_type)
        {
            if (!corners.empty())
            {
                using (Mat corners_32S = new Mat())
                {
                    corners.convertTo(corners_32S, CvType.CV_32SC2);

                    for (int i = 0; i < corners_32S.rows(); i++)
                    {
                        using (Mat cornersMat_row = corners_32S.row(i))
                        using (MatOfPoint cornersMat = new MatOfPoint(cornersMat_row.reshape(2, 4)))
                        {
                            if (decoded_info.Count > i)
                            {
                                DrawBarcodeContour(frame, cornersMat, decoded_info[i], decoded_type[i]);
                            }
                            else
                            {
                                DrawBarcodeContour(frame, cornersMat);
                            }
                        }

                        if (decoded_info.Count > i)
                        {
                            if (!string.IsNullOrEmpty(decoded_info[i]))
                            {
                                Debug.Log("TYPE: " + decoded_type[i] + " INFO: " + decoded_info[i]);
                            }
                            else
                            {
                                Debug.Log("can't decode 1D barcode");
                            }
                        }
                        else
                        {
                            Debug.Log("decode information is not available (disabled)");
                        }
                    }
                }
            }
            else
            {
                Debug.Log("Barcode is not detected");
            }
        }

        private void DrawBarcodeContour(Mat color_image, MatOfPoint corners, string decoded_info = "", string decoded_type = "")
        {
            if (!corners.empty())
            {
                bool decodable = decoded_type != "";

                double show_radius = (color_image.rows() > color_image.cols()) ? (2.813 * color_image.rows()) / color_image.cols() : (2.813 * color_image.cols()) / color_image.rows();
                double contour_radius = show_radius * 0.4;

                List<MatOfPoint> barcode_contours = new List<MatOfPoint>();
                barcode_contours.Add(corners);

                Imgproc.drawContours(color_image, barcode_contours, 0, decodable ? new Scalar(0, 255, 0, 255) : new Scalar(255, 0, 0, 255), Mathf.RoundToInt((float)contour_radius));

                int[] p = new int[8];
                corners.get(0, 0, p);
                Imgproc.putText(color_image, decoded_info, new Point(p[0] + 5, p[1] - 25), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(color_image, decoded_type, new Point(p[0] + 5, p[1] - 5), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                for (int i = 0; i < 8; i = i + 2)
                {
                    Scalar color = new Scalar(Random.Range(0, 255), Random.Range(0, 255), Random.Range(0, 255), 255);
                    Imgproc.circle(color_image, new Point(p[i], p[i + 1]), Mathf.RoundToInt((float)contour_radius), color, -1);
                }
            }
        }
    }
}

#endif
