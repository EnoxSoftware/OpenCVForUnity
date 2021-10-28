#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVRect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// QRCodeDetector WebCamTexture Example
    /// An example of detecting QRCode in a image of WebCamTexture using the QRCodeDetector class.
    /// https://github.com/opencv/opencv/blob/master/samples/cpp/qrcode.cpp
    /// </summary>
    [RequireComponent (typeof(WebCamTextureToMatHelper))]
    public class QRCodeDetectorWebCamTextureExample : MonoBehaviour
    {
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
        /// The image size rect.
        /// </summary>
        OpenCVRect imageSizeRect;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        // Use this for initialization
        void Start ()
        {
            fpsMonitor = GetComponent<FpsMonitor> ();

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();


            detector = new QRCodeDetector ();

            #if UNITY_ANDROID && !UNITY_EDITOR
            // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
            webCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
            #endif
            webCamTextureToMatHelper.Initialize ();

        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInitialized");
            
            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();

            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);
            Utils.fastMatToTexture2D(webCamTextureMat, texture);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null) {
                fpsMonitor.Add ("width", webCamTextureMat.width ().ToString ());
                fpsMonitor.Add ("height", webCamTextureMat.height ().ToString ());
                fpsMonitor.Add ("orientation", Screen.orientation.ToString ());
            }


            float width = webCamTextureMat.width ();
            float height = webCamTextureMat.height ();
            
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }

            grayMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC1);
            imageSizeRect = new OpenCVRect (0, 0, grayMat.width (), grayMat.height ());

            points = new Mat ();

            // If the WebCam is front facing, flip the Mat horizontally. Required for successful detection of QR code.
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
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

            if (grayMat != null)
                grayMat.Dispose ();

            if (texture != null) {
                Texture2D.Destroy (texture);
                texture = null;
            }

            if (points != null)
                points.Dispose ();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred (WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log ("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update ()
        {
            if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {
                
                Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

                Imgproc.cvtColor (rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

                bool result = detector.detect (grayMat, points);

                if (result) {

//                    Debug.Log (points.dump ());

                    float[] points_arr = new float[8];
                    points.get (0, 0, points_arr);

                    bool decode = true;
                    // Whether all points are in the image area or not.
                    for (int i = 0; i < 8; i = i + 2) {
                        if (!imageSizeRect.contains (new Point (points_arr [i], points_arr [i + 1]))) {
                            decode = false;
//                            Debug.Log ("The point exists out of the image area.");
                            break;
                        }
                    }

                    // draw QRCode contour.
                    Imgproc.line (rgbaMat, new Point (points_arr [0], points_arr [1]), new Point (points_arr [2], points_arr [3]), new Scalar (255, 0, 0, 255), 2);
                    Imgproc.line (rgbaMat, new Point (points_arr [2], points_arr [3]), new Point (points_arr [4], points_arr [5]), new Scalar (255, 0, 0, 255), 2);
                    Imgproc.line (rgbaMat, new Point (points_arr [4], points_arr [5]), new Point (points_arr [6], points_arr [7]), new Scalar (255, 0, 0, 255), 2);
                    Imgproc.line (rgbaMat, new Point (points_arr [6], points_arr [7]), new Point (points_arr [0], points_arr [1]), new Scalar (255, 0, 0, 255), 2);

                    if (decode) {
                        string decode_info = detector.decode (grayMat, points);
//                        Debug.Log (decode_info);
                        Imgproc.putText (rgbaMat, "DECODE INFO: " + decode_info, new Point (5, grayMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    }
                }

                Utils.fastMatToTexture2D (rgbaMat, texture);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            webCamTextureToMatHelper.Dispose ();

            if (detector != null)
                detector.Dispose ();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick ()
        {
            SceneManager.LoadScene ("OpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick ()
        {
            webCamTextureToMatHelper.Play ();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick ()
        {
            webCamTextureToMatHelper.Pause ();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick ()
        {
            webCamTextureToMatHelper.Stop ();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick ()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.requestedIsFrontFacing;
        }
    }
}

#endif