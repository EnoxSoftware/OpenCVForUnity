using UnityEngine;
using System.Collections;
using UnityEngine.UI;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// GreenScreen example.
    /// Referring to https://gist.github.com/drscotthawley/2d6bbffce9dda5f3057b4879c3bd4422.
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class GreenScreenExample : MonoBehaviour
    {
        /// <summary>
        /// The thresh.
        /// </summary>
        [Range(0 , 255)]
        public float thresh = 50.0f;
        
        /// <summary>
        /// The background raw image.
        /// </summary>
        public RawImage bgRawImage;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The background mat.
        /// </summary>
        Mat bgMat;

        /// <summary>
        /// The foreground mask mat.
        /// </summary>
        Mat fgMaskMat;

        /// <summary>
        /// The background mask mat.
        /// </summary>
        Mat bgMaskMat;

        /// <summary>
        /// The green mat.
        /// </summary>
        Mat greenMat;

        /// <summary>
        /// The background texture.
        /// </summary>
        Texture2D bgTexture;

        // Use this for initialization
        void Start ()
        {
            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();
            webCamTextureToMatHelper.Initialize ();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();

            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();
                                    
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }


            bgMat = new Mat(webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC4);
            fgMaskMat = new Mat(webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC1);
            bgMaskMat = new Mat(webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC1);
            greenMat = new Mat(webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC4, new Scalar(0,255,0,255));

            bgTexture = new Texture2D (bgMat.cols (), bgMat.rows (), TextureFormat.RGBA32, false);
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

            if(bgMat != null){
                bgMat.Dispose();
                bgMat = null;
            }
            if(fgMaskMat != null){
                fgMaskMat.Dispose();
                fgMaskMat = null;
            }
            if(bgMaskMat != null){
                bgMaskMat.Dispose();
                bgMaskMat = null;
            }
            if(greenMat != null){
                greenMat.Dispose();
                greenMat = null;
            }
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
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

                if (Input.GetKeyUp(KeyCode.Space) || Input.touchCount > 0)
                {
                    rgbaMat.copyTo(bgMat);

                    setBgTexture(bgMat);
                }

                //set fgMaskMat
                findFgMaskMat(rgbaMat, bgMat, thresh);

                //set bgMaskMat
                Core.bitwise_not(fgMaskMat, bgMaskMat);

                //copy greenMat using bgMaskMat
                greenMat.copyTo(rgbaMat, bgMaskMat);

                Imgproc.putText (rgbaMat, "SPACE KEY or TOUCH SCREEN: Reset backgroud img", new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.6, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Utils.matToTexture2D (rgbaMat, texture, webCamTextureToMatHelper.GetBufferColors());
            }
        }

        /// <summary>
        /// Finds the foreground mask mat.
        /// </summary>
        /// <param name="fgMat">Fg mat.</param>
        /// <param name="bgMat">Background mat.</param>
        /// <param name="thresh">Thresh.</param>
        private void findFgMaskMat(Mat fgMat, Mat bgMat, float thresh=13.0f){
            Mat diff1 = new Mat();
            Core.absdiff( fgMat, bgMat, diff1);
            Mat diff2 = new Mat();
            Core.absdiff( bgMat, fgMat, diff2);
            Mat diff = diff1 + diff2;

            Imgproc.threshold(diff, diff, thresh, 0, Imgproc.THRESH_TOZERO);

            Imgproc.cvtColor(diff, fgMaskMat, Imgproc.COLOR_RGBA2GRAY);

            Imgproc.threshold(fgMaskMat, fgMaskMat, 10, 0, Imgproc.THRESH_TOZERO);

            Imgproc.threshold(fgMaskMat, fgMaskMat, 0, 255, Imgproc.THRESH_BINARY);

            diff1.Dispose();
            diff2.Dispose();
            diff.Dispose();
        }

        /// <summary>
        /// Sets the background texture.
        /// </summary>
        /// <param name="bgMat">Background mat.</param>
        private void setBgTexture(Mat bgMat){
            Utils.matToTexture2D(bgMat, bgTexture);
            
            bgRawImage.texture = bgTexture;
            bgRawImage.rectTransform.localScale = new Vector3 (1.0f, (float)bgMat.height () / (float)bgMat.width (), 1.0f);
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
            webCamTextureToMatHelper.Initialize (null, webCamTextureToMatHelper.requestedWidth, webCamTextureToMatHelper.requestedHeight, !webCamTextureToMatHelper.requestedIsFrontFacing);
        }
    }
}