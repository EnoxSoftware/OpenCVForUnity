using System;
using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// WebCamTexture to mat example.
    /// </summary>
    public class WebCamTextureToMatExample : MonoBehaviour
    {
        /// <summary>
        /// The name of the device.
        /// </summary>
        public string requestDeviceName = null;

        /// <summary>
        /// The width.
        /// </summary>
        public int requestWidth = 640;

        /// <summary>
        /// The height.
        /// </summary>
        public int requestHeight = 480;

        /// <summary>
        /// Should use front facing.
        /// </summary>
        public bool requestIsFrontFacing = false;

        /// <summary>
        /// The web cam texture.
        /// </summary>
        WebCamTexture webCamTexture;

        /// <summary>
        /// The web cam device.
        /// </summary>
        WebCamDevice webCamDevice;

        /// <summary>
        /// The rgba mat.
        /// </summary>
        Mat rgbaMat;

        /// <summary>
        /// The colors.
        /// </summary>
        Color32[] colors;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The init waiting.
        /// </summary>
        bool initWaiting = false;

        /// <summary>
        /// The init done.
        /// </summary>
        bool initDone = false;

        // Use this for initialization
        void Start ()
        {
            init ();
        }

        /// <summary>
        /// Init of web cam texture.
        /// </summary>
        private void init ()
        {
            if (initWaiting)
                return;

            StartCoroutine (init_coroutine ());
        }

        /// <summary>
        /// Init of web cam texture.
        /// </summary>
        /// <param name="deviceName">Device name.</param>
        /// <param name="requestWidth">Request width.</param>
        /// <param name="requestHeight">Request height.</param>
        /// <param name="requestIsFrontFacing">If set to <c>true</c> request is front facing.</param>
        /// <param name="OnInited">On inited.</param>
        private void init (string deviceName, int requestWidth, int requestHeight, bool requestIsFrontFacing)
        {
            if (initWaiting)
                return;

            this.requestDeviceName = deviceName;
            this.requestWidth = requestWidth;
            this.requestHeight = requestHeight;
            this.requestIsFrontFacing = requestIsFrontFacing;

            StartCoroutine (init_coroutine ());
        }

        /// <summary>
        /// Init of web cam texture by coroutine.
        /// </summary>
        private IEnumerator init_coroutine ()
        {
            if (initDone)
                dispose ();

            initWaiting = true;

            if (!String.IsNullOrEmpty (requestDeviceName)) {
                //Debug.Log ("deviceName is "+requestDeviceName);
                webCamTexture = new WebCamTexture (requestDeviceName, requestWidth, requestHeight);
            } else {
                //Debug.Log ("deviceName is null");
                // Checks how many and which cameras are available on the device
                for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {
                    if (WebCamTexture.devices [cameraIndex].isFrontFacing == requestIsFrontFacing) {

                        //Debug.Log (cameraIndex + " name " + WebCamTexture.devices [cameraIndex].name + " isFrontFacing " + WebCamTexture.devices [cameraIndex].isFrontFacing);
                        webCamDevice = WebCamTexture.devices [cameraIndex];
                        webCamTexture = new WebCamTexture (webCamDevice.name, requestWidth, requestHeight);

                        break;
                    }
                }
            }

            if (webCamTexture == null) {
                if (WebCamTexture.devices.Length > 0) {
                    webCamDevice = WebCamTexture.devices [0];
                    webCamTexture = new WebCamTexture (webCamDevice.name, requestWidth, requestHeight);
                } else {
                    webCamTexture = new WebCamTexture (requestWidth, requestHeight);
                }
            }

            // Starts the camera.
            webCamTexture.Play ();

            while (true) {
                // If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/).
                #if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
                if (webCamTexture.width > 16 && webCamTexture.height > 16) {
                #else
                if (webCamTexture.didUpdateThisFrame) {
                    #if UNITY_IOS && !UNITY_EDITOR && UNITY_5_2                                    
                    while (webCamTexture.width <= 16) {
                        webCamTexture.GetPixels32 ();
                        yield return new WaitForEndOfFrame ();
                    } 
                    #endif
                    #endif

                    Debug.Log ("name " + webCamTexture.name + " width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);
                    Debug.Log ("videoRotationAngle " + webCamTexture.videoRotationAngle + " videoVerticallyMirrored " + webCamTexture.videoVerticallyMirrored + " isFrongFacing " + webCamDevice.isFrontFacing);

                    initWaiting = false;
                    initDone = true;

                    onInited ();

                    break;
                } else {
                    yield return 0;
                }
            }
        }

        /// <summary>
        /// Releases all resource.
        /// </summary>
        private void dispose ()
        {
            initWaiting = false;
            initDone = false;

            if (webCamTexture != null) {
                webCamTexture.Stop ();
                webCamTexture = null;
            }
            if (rgbaMat != null) {
                rgbaMat.Dispose ();
                rgbaMat = null;
            }
        }

        /// <summary>
        /// Init completion handler of the web camera texture.
        /// </summary>
        private void onInited ()
        {
            if (colors == null || colors.Length != webCamTexture.width * webCamTexture.height)
                colors = new Color32[webCamTexture.width * webCamTexture.height];
            if (texture == null || texture.width != webCamTexture.width || texture.height != webCamTexture.height)
                texture = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

            rgbaMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3 (webCamTexture.width, webCamTexture.height, 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


            float width = rgbaMat.width ();
            float height = rgbaMat.height ();

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }
        }

        // Update is called once per frame
        void Update ()
        {
            if (initDone && webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame) {
                Utils.webCamTextureToMat (webCamTexture, rgbaMat, colors);

                Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Utils.matToTexture2D (rgbaMat, texture, colors);
            }
        }

        /// <summary>
        /// Raises the disable event.
        /// </summary>
        void OnDisable ()
        {
            dispose ();
        }

        /// <summary>
        /// Raises the back button event.
        /// </summary>
        public void OnBackButton ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("OpenCVForUnityExample");
            #else
            Application.LoadLevel ("OpenCVForUnityExample");
            #endif
        }

        /// <summary>
        /// Raises the play button event.
        /// </summary>
        public void OnPlayButton ()
        {
            if (initDone)
                webCamTexture.Play ();
        }

        /// <summary>
        /// Raises the pause button event.
        /// </summary>
        public void OnPauseButton ()
        {
            if (initDone)
                webCamTexture.Pause ();
        }

        /// <summary>
        /// Raises the stop button event.
        /// </summary>
        public void OnStopButton ()
        {
            if (initDone)
                webCamTexture.Stop ();
        }

        /// <summary>
        /// Raises the change camera button event.
        /// </summary>
        public void OnChangeCameraButton ()
        {
            if (initDone)
                init (null, requestWidth, requestHeight, !requestIsFrontFacing);
        }
    }
}