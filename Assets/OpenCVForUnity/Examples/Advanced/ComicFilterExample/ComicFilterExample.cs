using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Comic Filter Example
    /// An example of image processing (comic filter) using the Imgproc class.
    /// Referring to http://dev.classmethod.jp/smartphone/opencv-manga-2/.
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class ComicFilterExample : MonoBehaviour
    {
        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMat;

        /// <summary>
        /// The line mat.
        /// </summary>
        Mat lineMat;

        /// <summary>
        /// The mask mat.
        /// </summary>
        Mat maskMat;

        /// <summary>
        /// The background mat.
        /// </summary>
        Mat bgMat;

        /// <summary>
        /// The dst mat.
        /// </summary>
        Mat dstMat;

        /// <summary>
        /// The gray pixels.
        /// </summary>
        byte[] grayPixels;

        /// <summary>
        /// The mask pixels.
        /// </summary>
        byte[] maskPixels;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        #if UNITY_ANDROID && !UNITY_EDITOR
        float rearCameraRequestedFPS;
        #endif

        // Use this for initialization
        void Start ()
        {
            fpsMonitor = GetComponent<FpsMonitor> ();

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();

            #if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices. (Pixel, pixel 2)
            // https://forum.unity.com/threads/android-webcamtexture-in-low-light-only-some-models.520656/
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            rearCameraRequestedFPS = webCamTextureToMatHelper.requestedFPS;
            if (webCamTextureToMatHelper.requestedIsFrontFacing) {                
                webCamTextureToMatHelper.requestedFPS = 15;
                webCamTextureToMatHelper.Initialize ();
            } else {
                webCamTextureToMatHelper.Initialize ();
            }
            #else
            webCamTextureToMatHelper.Initialize ();
            #endif
        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInitialized");
        
            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();
        
            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
        
            gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
        
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null){
                fpsMonitor.Add ("width", webCamTextureMat.width ().ToString());
                fpsMonitor.Add ("height", webCamTextureMat.height ().ToString());
                fpsMonitor.Add ("orientation", Screen.orientation.ToString());
            }
        

            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();
        
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }


            grayMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC1);
            lineMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC1);
            maskMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC1);
            
            //create a striped background.
            bgMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC1, new Scalar (255));
            for (int i = 0; i < bgMat.rows ()*2.5f; i=i+4) {
                Imgproc.line (bgMat, new Point (0, 0 + i), new Point (bgMat.cols (), -bgMat.cols () + i), new Scalar (0), 1);
            }
            
            dstMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC1);
            
            grayPixels = new byte[grayMat.cols () * grayMat.rows () * grayMat.channels ()];
            maskPixels = new byte[maskMat.cols () * maskMat.rows () * maskMat.channels ()];
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

            grayMat.Dispose ();
            lineMat.Dispose ();
            maskMat.Dispose ();
        
            bgMat.Dispose ();
            dstMat.Dispose ();

            grayPixels = null;
            maskPixels = null;

            if (texture != null) {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode){
            Debug.Log ("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update ()
        {
            if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {
            
                Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

                Imgproc.cvtColor (rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
            
                //                      Utils.webCamTextureToMat (webCamTexture, grayMat, colors);
            
                bgMat.copyTo (dstMat);

                Imgproc.GaussianBlur (grayMat, lineMat, new Size (3, 3), 0);


                grayMat.get (0, 0, grayPixels);
            
                for (int i = 0; i < grayPixels.Length; i++) {
            
                    maskPixels [i] = 0;
                        
                    if (grayPixels [i] < 70) {
                        grayPixels [i] = 0;
            
                        maskPixels [i] = 1;
                    } else if (70 <= grayPixels [i] && grayPixels [i] < 120) {
                        grayPixels [i] = 100;

                    } else {
                        grayPixels [i] = 255;
                        maskPixels [i] = 1;
                    }
                }
                    
                grayMat.put (0, 0, grayPixels);
                maskMat.put (0, 0, maskPixels);
                grayMat.copyTo (dstMat, maskMat);
            
            
                Imgproc.Canny (lineMat, lineMat, 20, 120);
                    
                lineMat.copyTo (maskMat);
                    
                Core.bitwise_not (lineMat, lineMat);
            
                lineMat.copyTo (dstMat, maskMat);

                //          Imgproc.putText (dstMat, "W:" + dstMat.width () + " H:" + dstMat.height () + " SO:" + Screen.orientation, new Point (5, dstMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (0), 2, Imgproc.LINE_AA, false);
            
                //      Imgproc.cvtColor(dstMat,rgbaMat,Imgproc.COLOR_GRAY2RGBA);
                //              Utils.matToTexture2D (rgbaMat, texture);
            
                Utils.matToTexture2D (dstMat, texture, webCamTextureToMatHelper.GetBufferColors());
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            webCamTextureToMatHelper.Dispose ();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("OpenCVForUnityExample");
            #else
            Application.LoadLevel ("OpenCVForUnityExample");
            #endif
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
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (!webCamTextureToMatHelper.IsFrontFacing ()) {
                rearCameraRequestedFPS = webCamTextureToMatHelper.requestedFPS;
                webCamTextureToMatHelper.Initialize (!webCamTextureToMatHelper.IsFrontFacing (), 15, webCamTextureToMatHelper.rotate90Degree);
            } else {                
                webCamTextureToMatHelper.Initialize (!webCamTextureToMatHelper.IsFrontFacing (), rearCameraRequestedFPS, webCamTextureToMatHelper.rotate90Degree);
            }
            #else
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing ();
            #endif
        }
    }
}