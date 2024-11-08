#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// BarcodeDetector Example
    /// An example of detecting Barcode using the BarcodeDetector class.
    /// https://github.com/opencv/opencv_contrib/blob/master/modules/barcode/samples/barcode.cpp
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class BarcodeDetectorExample : MonoBehaviour
    {
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The BarcodeDetector detector.
        /// </summary>
        BarcodeDetector detector;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        MultiSource2MatHelper multiSource2MatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            multiSource2MatHelper.outputColorFormat = Source2MatHelperColorFormat.RGBA;

            detector = new BarcodeDetector();

            // When using super resolution.
            // Please, download 'sr.*' from https://github.com/WeChatCV/opencv_3rdparty/tree/wechat_qrcode and put them into the StreamingAssets/OpenCVForUnity/barcode directory.
            //detector = new BarcodeDetector(Utils.getFilePath("OpenCVForUnity/barcode/sr.prototxt"), Utils.getFilePath("OpenCVForUnity/barcode/sr.caffemodel"));

            multiSource2MatHelper.Initialize();
        }

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = multiSource2MatHelper.GetMat();

            texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(rgbaMat, texture);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", rgbaMat.width().ToString());
                fpsMonitor.Add("height", rgbaMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API
            // If the WebCam is front facing, flip the Mat horizontally. Required for successful detection.
            if (multiSource2MatHelper.source2MatHelper is WebCamTexture2MatHelper webCamHelper)
                webCamHelper.flipHorizontal = webCamHelper.IsFrontFacing();
#endif
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (multiSource2MatHelper.IsPlaying() && multiSource2MatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = multiSource2MatHelper.GetMat();

                List<string> decoded_info = new List<string>();
                List<string> decoded_type = new List<string>();
                Mat corners = new Mat();

                bool result_detection = detector.detectAndDecodeWithType(rgbaMat, decoded_info, decoded_type, corners);

                // draw Barcode contours.
                if (result_detection)
                    DrawBarcodeResults(rgbaMat, corners, decoded_info, decoded_type);

                corners.Dispose();

                Utils.matToTexture2D(rgbaMat, texture);
            }
        }

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

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            multiSource2MatHelper.Dispose();

            if (detector != null)
                detector.Dispose();
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
            multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            multiSource2MatHelper.requestedIsFrontFacing = !multiSource2MatHelper.requestedIsFrontFacing;
        }
    }
}

#endif