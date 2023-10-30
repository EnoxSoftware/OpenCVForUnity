#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// BarcodeDetector WebCam Example
    /// An example of detecting Barcode in a image of WebCamTexture using the BarcodeDetector class.
    /// https://github.com/opencv/opencv_contrib/blob/master/modules/barcode/samples/barcode.cpp
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class BarcodeDetectorWebCamExample : MonoBehaviour
    {
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
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

            detector = new BarcodeDetector();

            // When using super resolution.
            // Please, download 'sr.*' from https://github.com/WeChatCV/opencv_3rdparty/tree/wechat_qrcode and put them into the StreamingAssets/OpenCVForUnity/barcode directory.
            //detector = new BarcodeDetector(Utils.getFilePath("OpenCVForUnity/barcode/sr.prototxt"), Utils.getFilePath("OpenCVForUnity/barcode/sr.caffemodel"));


#if UNITY_ANDROID && !UNITY_EDITOR
            // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
            webCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
#endif
            webCamTextureToMatHelper.Initialize();

        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(webCamTextureMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", webCamTextureMat.width().ToString());
                fpsMonitor.Add("height", webCamTextureMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }


            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }

            // If the WebCam is front facing, flip the Mat horizontally. Required for successful detection of Barcode.
            if (webCamTextureToMatHelper.IsFrontFacing() && !webCamTextureToMatHelper.flipHorizontal)
            {
                webCamTextureToMatHelper.flipHorizontal = true;
            }
            else if (!webCamTextureToMatHelper.IsFrontFacing() && webCamTextureToMatHelper.flipHorizontal)
            {
                webCamTextureToMatHelper.flipHorizontal = false;
            }
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update()
        {
            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

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
            webCamTextureToMatHelper.Dispose();

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
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.requestedIsFrontFacing;
        }
    }
}

#endif