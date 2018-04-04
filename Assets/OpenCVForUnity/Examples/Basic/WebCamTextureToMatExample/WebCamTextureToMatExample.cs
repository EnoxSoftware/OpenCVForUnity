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
    /// WebCamTextureToMat Example
    /// An example of converting a WebCamTexture image to OpenCV's Mat format.
    /// </summary>
    public class WebCamTextureToMatExample : MonoBehaviour
    {
        /// <summary>
        /// Set this to specify the name of the device to use.
        /// </summary>
        public string requestedDeviceName = null;

        /// <summary>
        /// Set the requested width of the camera device.
        /// </summary>
        public int requestedWidth = 640;
        
        /// <summary>
        /// Set the requested height of the camera device.
        /// </summary>
        public int requestedHeight = 480;
        
        /// <summary>
        /// Set the requested to using the front camera.
        /// </summary>
        public bool requestedIsFrontFacing = false;

        /// <summary>
        /// The webcam texture.
        /// </summary>
        WebCamTexture webCamTexture;

        /// <summary>
        /// The webcam device.
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
        /// Indicates whether this instance is waiting for initialization to complete.
        /// </summary>
        bool isInitWaiting = false;

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        bool hasInitDone = false;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        // Use this for initialization
        void Start ()
        {
            fpsMonitor = GetComponent<FpsMonitor> ();

            Initialize ();
        }

        /// <summary>
        /// Initialize of web cam texture.
        /// </summary>
        private void Initialize ()
        {
            if (isInitWaiting)
                return;

            StartCoroutine (_Initialize ());
        }

        /// <summary>
        /// Initialize of webcam texture.
        /// </summary>
        /// <param name="deviceName">Device name.</param>
        /// <param name="requestedWidth">Requested width.</param>
        /// <param name="requestedHeight">Requested height.</param>
        /// <param name="requestedIsFrontFacing">If set to <c>true</c> requested to using the front camera.</param>
        private void Initialize (string deviceName, int requestedWidth, int requestedHeight, bool requestedIsFrontFacing)
        {
            if (isInitWaiting)
                return;

            this.requestedDeviceName = deviceName;
            this.requestedWidth = requestedWidth;
            this.requestedHeight = requestedHeight;
            this.requestedIsFrontFacing = requestedIsFrontFacing;

            StartCoroutine (_Initialize ());
        }

        /// <summary>
        /// Initialize of webcam texture by coroutine.
        /// </summary>
        private IEnumerator _Initialize ()
        {
            if (hasInitDone)
                Dispose ();

            isInitWaiting = true;

            int requestedFPS = 30;
            #if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices. (Pixel, pixel 2)
            // https://forum.unity.com/threads/android-webcamtexture-in-low-light-only-some-models.520656/
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            requestedFPS = requestedIsFrontFacing ? 15 : 30;
            #endif

            // Creates the camera
            if (!String.IsNullOrEmpty (requestedDeviceName)) {
                int requestedDeviceIndex = -1;
                if (Int32.TryParse (requestedDeviceName, out requestedDeviceIndex)) {
                    if (requestedDeviceIndex >= 0 && requestedDeviceIndex < WebCamTexture.devices.Length) {
                        webCamDevice = WebCamTexture.devices [requestedDeviceIndex];
                        webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                    }
                } else {
                    for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {
                        if (WebCamTexture.devices [cameraIndex].name == requestedDeviceName) {
                            webCamDevice = WebCamTexture.devices [cameraIndex];
                            webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                            break;
                        }
                    }
                }
                if (webCamTexture == null)
                    Debug.Log ("Cannot find camera device " + requestedDeviceName + ".");
            }

            if (webCamTexture == null) {
                // Checks how many and which cameras are available on the device
                for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {                   
                    if (WebCamTexture.devices [cameraIndex].isFrontFacing == requestedIsFrontFacing) {
                        webCamDevice = WebCamTexture.devices [cameraIndex];
                        webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                        break;
                    }
                }
            }

            if (webCamTexture == null) {
                if (WebCamTexture.devices.Length > 0) {
                    webCamDevice = WebCamTexture.devices [0];
                    webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                } else {
                    Debug.LogError ("Camera device does not exist.");
                    isInitWaiting = false;
                    yield break;
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

                    Debug.Log ("name:" + webCamTexture.deviceName + " width:" + webCamTexture.width + " height:" + webCamTexture.height + " fps:" + webCamTexture.requestedFPS);
                    Debug.Log ("videoRotationAngle:" + webCamTexture.videoRotationAngle + " videoVerticallyMirrored:" + webCamTexture.videoVerticallyMirrored + " isFrongFacing:" + webCamDevice.isFrontFacing);

                    isInitWaiting = false;
                    hasInitDone = true;

                    OnInited ();

                    break;
                } else {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Releases all resource.
        /// </summary>
        private void Dispose ()
        {
            isInitWaiting = false;
            hasInitDone = false;

            if (webCamTexture != null) {
                webCamTexture.Stop ();
                WebCamTexture.Destroy (webCamTexture);
                webCamTexture = null;
            }
            if (rgbaMat != null) {
                rgbaMat.Dispose ();
                rgbaMat = null;
            }
            if (texture != null) {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Initialize completion handler of the webcam texture.
        /// </summary>
        private void OnInited ()
        {
            if (colors == null || colors.Length != webCamTexture.width * webCamTexture.height)
                colors = new Color32[webCamTexture.width * webCamTexture.height];
            if (texture == null || texture.width != webCamTexture.width || texture.height != webCamTexture.height)
                texture = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

            rgbaMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3 (webCamTexture.width, webCamTexture.height, 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null){
                fpsMonitor.Add ("width", rgbaMat.width ().ToString());
                fpsMonitor.Add ("height", rgbaMat.height ().ToString());
                fpsMonitor.Add ("orientation", Screen.orientation.ToString());
            }


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
            if (hasInitDone && webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame) {
                Utils.webCamTextureToMat (webCamTexture, rgbaMat, colors);

                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Utils.matToTexture2D (rgbaMat, texture, colors);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            Dispose ();
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
            if (hasInitDone)
                webCamTexture.Play ();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick ()
        {
            if (hasInitDone)
                webCamTexture.Pause ();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick ()
        {
            if (hasInitDone)
                webCamTexture.Stop ();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick ()
        {
            if (hasInitDone)
                Initialize (null, requestedWidth, requestedHeight, !webCamDevice.isFrontFacing);
        }
    }
}