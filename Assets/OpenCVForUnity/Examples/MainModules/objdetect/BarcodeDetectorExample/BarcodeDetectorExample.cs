#if !UNITY_WSA_10_0

using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
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
        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The BarcodeDetector detector.
        /// </summary>
        private BarcodeDetector _detector;

        /// <summary>
        /// The webcam texture to mat helper.
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

            _detector = new BarcodeDetector();

            // When using super resolution.
            // Please, download 'sr.*' from https://github.com/WeChatCV/opencv_3rdparty/tree/wechat_qrcode and put them into the StreamingAssets/OpenCVForUnityExamples/barcode directory.
            //_detector = new BarcodeDetector(Utils.getFilePath("OpenCVForUnityExamples/barcode/sr.prototxt"), OpenCVEnv.GetFilePath("OpenCVForUnityExamples/barcode/sr.caffemodel"));

            _multiSource2MatHelper.Initialize();
        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                List<string> decoded_info = new List<string>();
                List<string> decoded_type = new List<string>();
                Mat corners = new Mat();

                bool result_detection = _detector.detectAndDecodeWithType(rgbaMat, decoded_info, decoded_type, corners);

                // draw Barcode contours.
                if (result_detection)
                    DrawBarcodeResults(rgbaMat, corners, decoded_info, decoded_type);

                corners.Dispose();

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

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
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
