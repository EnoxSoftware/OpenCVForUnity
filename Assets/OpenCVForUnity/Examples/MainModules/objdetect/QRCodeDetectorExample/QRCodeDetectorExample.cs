using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System.Collections.Generic;
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
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMat;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The QRCode detector.
        /// </summary>
        QRCodeDetector detector;

        /// <summary>
        /// The points.
        /// </summary>
        Mat points;

        /// <summary>
        /// The decoded info
        /// </summary>
        List<string> decodedInfo;

        /// <summary>
        /// The straight qrcode
        /// </summary>
        List<Mat> straightQrcode;

        /// <summary>
        /// The multi source to mat helper.
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

            detector = new QRCodeDetector();

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

            grayMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);

            points = new Mat();
            decodedInfo = new List<string>();
            straightQrcode = new List<Mat>();

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

            if (grayMat != null)
                grayMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

            if (points != null)
                points.Dispose();

            if (decodedInfo != null)
                decodedInfo.Clear();

            if (straightQrcode != null)
                foreach (var item in straightQrcode)
                {
                    item.Dispose();
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

                Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);


                bool result = detector.detectAndDecodeMulti(grayMat, decodedInfo, points, straightQrcode);

                if (result)
                {

                    //Debug.Log(points.dump());
                    //Debug.Log(points.ToString());

                    //Debug.Log("decoded_info.Count " + decoded_info.Count);
                    //Debug.Log("straight_qrcode.Count " + straight_qrcode.Count);

                    for (int i = 0; i < points.rows(); i++)
                    {
                        //Debug.Log(decoded_info[i]);
                        //Debug.Log(straight_qrcode[i].dump());

                        //// draw QRCode contour.
                        float[] points_arr = new float[8];
                        points.get(i, 0, points_arr);
                        Imgproc.line(rgbaMat, new Point(points_arr[0], points_arr[1]), new Point(points_arr[2], points_arr[3]), new Scalar(255, 0, 0, 255), 2);
                        Imgproc.line(rgbaMat, new Point(points_arr[2], points_arr[3]), new Point(points_arr[4], points_arr[5]), new Scalar(255, 0, 0, 255), 2);
                        Imgproc.line(rgbaMat, new Point(points_arr[4], points_arr[5]), new Point(points_arr[6], points_arr[7]), new Scalar(255, 0, 0, 255), 2);
                        Imgproc.line(rgbaMat, new Point(points_arr[6], points_arr[7]), new Point(points_arr[0], points_arr[1]), new Scalar(255, 0, 0, 255), 2);

                        if (decodedInfo.Count > i && decodedInfo[i] != null)
                            Imgproc.putText(rgbaMat, decodedInfo[i], new Point(points_arr[0], points_arr[1]), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    }

                }
                else
                {
                    Imgproc.putText(rgbaMat, "Decoding failed.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }

                Utils.matToTexture2D(rgbaMat, texture);
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