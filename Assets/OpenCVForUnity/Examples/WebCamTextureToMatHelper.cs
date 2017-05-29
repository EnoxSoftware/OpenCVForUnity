using OpenCVForUnity;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Web cam texture to mat helper.
    /// v 1.0.0
    /// </summary>
    public class WebCamTextureToMatHelper : MonoBehaviour
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
        /// The requested FPS.
        /// </summary>
        public int requestFPS = 30;

        /// <summary>
        /// The flip vertical.
        /// </summary>
        public bool flipVertical = false;

        /// <summary>
        /// The flip horizontal.
        /// </summary>
        public bool flipHorizontal = false;

        /// <summary>
        /// The timeout frame count.
        /// </summary>
        public int timeoutFrameCount = 300;

        /// <summary>
        /// The on inited event.
        /// </summary>
        public UnityEvent OnInitedEvent;

        /// <summary>
        /// The on disposed event.
        /// </summary>
        public UnityEvent OnDisposedEvent;

        /// <summary>
        /// The on error occurred event.
        /// </summary>
        public ErrorUnityEvent OnErrorOccurredEvent;

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
        /// The rotated rgba mat
        /// </summary>
        Mat rotatedRgbaMat;

        /// <summary>
        /// The colors.
        /// </summary>
        Color32[] colors;

        /// <summary>
        /// The init waiting.
        /// </summary>
        bool initWaiting = false;

        /// <summary>
        /// The init done.
        /// </summary>
        bool initDone = false;

        /// <summary>
        /// The screenOrientation.
        /// </summary>
        ScreenOrientation screenOrientation = ScreenOrientation.Unknown;

        [System.Serializable]
        public enum ErrorCode :int
        {
            CAMERA_DEVICE_NOT_EXIST = 0,
            TIMEOUT = 1,
        }

        [System.Serializable]
        public class ErrorUnityEvent : UnityEngine.Events.UnityEvent<ErrorCode>
        {
            
        }

        // Update is called once per frame
        void Update ()
        {
            if (initDone) {
                if (screenOrientation != Screen.orientation) {
                    StartCoroutine (init ());
                }
            }
        }

        /// <summary>
        /// Init this instance.
        /// </summary>
        /// <param name="deviceName">Device name.</param>
        /// <param name="requestWidth">Request width.</param>
        /// <param name="requestHeight">Request height.</param>
        /// <param name="requestIsFrontFacing">If set to <c>true</c> request is front facing.</param>
        /// <param name="OnInited">On inited.</param>
        public void Init (string deviceName = null, int requestWidth = 640, int requestHeight = 480, bool requestIsFrontFacing = false, int requestFPS = 30)
        {
            if (initWaiting)
                return;

            this.requestDeviceName = deviceName;
            this.requestWidth = requestWidth;
            this.requestHeight = requestHeight;
            this.requestIsFrontFacing = requestIsFrontFacing;
            this.requestFPS = requestFPS;
            if (OnInitedEvent == null)
                OnInitedEvent = new UnityEvent ();
            if (OnDisposedEvent == null)
                OnDisposedEvent = new UnityEvent ();
            if (OnErrorOccurredEvent == null)
                OnErrorOccurredEvent = new ErrorUnityEvent ();

            StartCoroutine (init ());
        }

        /// <summary>
        /// Init this instance by coroutine.
        /// </summary>
        private IEnumerator init ()
        {
            if (initDone)
                dispose ();

            initWaiting = true;

            if (!String.IsNullOrEmpty (requestDeviceName)) {
                //Debug.Log ("deviceName is "+requestDeviceName);
                webCamTexture = new WebCamTexture (requestDeviceName, requestWidth, requestHeight, requestFPS);
            } else {
                //Debug.Log ("deviceName is null");
                // Checks how many and which cameras are available on the device
                for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {
                    if (WebCamTexture.devices [cameraIndex].isFrontFacing == requestIsFrontFacing) {

                        //Debug.Log (cameraIndex + " name " + WebCamTexture.devices [cameraIndex].name + " isFrontFacing " + WebCamTexture.devices [cameraIndex].isFrontFacing);
                        webCamDevice = WebCamTexture.devices [cameraIndex];
                        webCamTexture = new WebCamTexture (webCamDevice.name, requestWidth, requestHeight, requestFPS);

                        break;
                    }
                }
            }

            if (webCamTexture == null) {
                if (WebCamTexture.devices.Length > 0) {
                    webCamDevice = WebCamTexture.devices [0];
                    webCamTexture = new WebCamTexture (webCamDevice.name, requestWidth, requestHeight, requestFPS);
                } else {
                    //Debug.Log("Camera device does not exist.");
                    initWaiting = false;

                    if (OnErrorOccurredEvent != null)
                        OnErrorOccurredEvent.Invoke (ErrorCode.CAMERA_DEVICE_NOT_EXIST);
                    yield break;
                }
            }

            //Debug.Log ("name " + webCamTexture.name + " width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);

            // Starts the camera
            webCamTexture.Play ();

            int initCount = 0;
            bool isTimeout = false;

            while (true) {
                if (initCount > timeoutFrameCount) {
                    isTimeout = true;
                    break;
                }
                // If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/)
                #if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
                else if (webCamTexture.width > 16 && webCamTexture.height > 16) {
                #else
                else if (webCamTexture.didUpdateThisFrame) {
                    #if UNITY_IOS && !UNITY_EDITOR && UNITY_5_2
                    while (webCamTexture.width <= 16) {
                        if (initCount > timeoutFrameCount) {
                            isTimeout = true;
                            break;
                        }else {
                            initCount++;
                        }
                        webCamTexture.GetPixels32 ();
                        yield return new WaitForEndOfFrame ();
                    }
                    if (isTimeout) break;
                    #endif
                    #endif

                    Debug.Log ("name " + webCamTexture.name + " width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);
                    Debug.Log ("videoRotationAngle " + webCamTexture.videoRotationAngle + " videoVerticallyMirrored " + webCamTexture.videoVerticallyMirrored + " isFrongFacing " + webCamDevice.isFrontFacing);

                    if (colors == null || colors.Length != webCamTexture.width * webCamTexture.height)
                        colors = new Color32[webCamTexture.width * webCamTexture.height];
                    rgbaMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);

                    //Debug.Log ("Screen.orientation " + Screen.orientation);
                    screenOrientation = Screen.orientation;

                    #if !UNITY_EDITOR && !(UNITY_STANDALONE || UNITY_WEBGL) 
                    if (screenOrientation == ScreenOrientation.Portrait || screenOrientation == ScreenOrientation.PortraitUpsideDown) {
                        rotatedRgbaMat = new Mat (webCamTexture.width, webCamTexture.height, CvType.CV_8UC4);
                    }
                    #endif

                    initWaiting = false;
                    initDone = true;

                    if (OnInitedEvent != null)
                        OnInitedEvent.Invoke ();

                    break;
                } else {
                    initCount++;
                    yield return 0;
                }
            }

            if (isTimeout) {
                //Debug.Log("Init time out.");
                webCamTexture.Stop ();
                webCamTexture = null;
                initWaiting = false;

                if (OnErrorOccurredEvent != null)
                    OnErrorOccurredEvent.Invoke (ErrorCode.TIMEOUT);
            }
        }

        /// <summary>
        /// Ises the inited.
        /// </summary>
        /// <returns><c>true</c>, if inited was ised, <c>false</c> otherwise.</returns>
        public bool IsInited ()
        {
            return initDone;
        }

        /// <summary>
        /// Play this instance.
        /// </summary>
        public void Play ()
        {
            if (initDone)
                webCamTexture.Play ();
        }

        /// <summary>
        /// Pause this instance.
        /// </summary>
        public void Pause ()
        {
            if (initDone)
                webCamTexture.Pause ();
        }

        /// <summary>
        /// Stop this instance.
        /// </summary>
        public void Stop ()
        {
            if (initDone)
                webCamTexture.Stop ();
        }

        /// <summary>
        /// Ises the playing.
        /// </summary>
        /// <returns><c>true</c>, if playing was ised, <c>false</c> otherwise.</returns>
        public bool IsPlaying ()
        {
            if (!initDone)
                return false;
            return webCamTexture.isPlaying;
        }

        /// <summary>
        /// Gets the web cam texture.
        /// </summary>
        /// <returns>The web cam texture.</returns>
        public WebCamTexture GetWebCamTexture ()
        {
            return (initDone) ? webCamTexture : null;
        }

        /// <summary>
        /// Gets the web cam device.
        /// </summary>
        /// <returns>The web cam device.</returns>
        public WebCamDevice GetWebCamDevice ()
        {
            return webCamDevice;
        }

        /// <summary>
        /// Dids the update this frame.
        /// </summary>
        /// <returns><c>true</c>, if update this frame was dided, <c>false</c> otherwise.</returns>
        public bool DidUpdateThisFrame ()
        {
            if (!initDone)
                return false;

            #if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
            if (webCamTexture.width > 16 && webCamTexture.height > 16) {
                return true;
            } else {
                return false;
            }
            #else
            return webCamTexture.didUpdateThisFrame;
            #endif
        }

        /// <summary>
        /// Gets the mat.
        /// </summary>
        /// <returns>The mat.</returns>
        public Mat GetMat ()
        {
            if (!initDone || !webCamTexture.isPlaying) {
                if (rotatedRgbaMat != null) {
                    return rotatedRgbaMat;
                } else {
                    return rgbaMat;
                }
            }

            Utils.webCamTextureToMat (webCamTexture, rgbaMat, colors);

            if (rotatedRgbaMat != null) {

                using (Mat transposeRgbaMat = rgbaMat.t ()) {
                    Core.flip (transposeRgbaMat, rotatedRgbaMat, 1);
                }

                flipMat (rotatedRgbaMat);

                return rotatedRgbaMat;
            } else {

                flipMat (rgbaMat);

                return rgbaMat;
            }
        }

        /// <summary>
        /// Flips the mat.
        /// </summary>
        /// <param name="mat">Mat.</param>
        private void flipMat (Mat mat)
        {
            int flipCode = int.MinValue;
                
            if (webCamDevice.isFrontFacing) {
                if (webCamTexture.videoRotationAngle == 0) {
                    flipCode = 1;
                } else if (webCamTexture.videoRotationAngle == 90) {
                    flipCode = 1;
                }
                if (webCamTexture.videoRotationAngle == 180) {
                    flipCode = 0;
                } else if (webCamTexture.videoRotationAngle == 270) {
                    flipCode = 0;
                }
            } else {
                if (webCamTexture.videoRotationAngle == 180) {
                    flipCode = -1;
                } else if (webCamTexture.videoRotationAngle == 270) {
                    flipCode = -1;
                }
            }
                
            if (flipVertical) {
                if (flipCode == int.MinValue) {
                    flipCode = 0;
                } else if (flipCode == 0) {
                    flipCode = int.MinValue;
                } else if (flipCode == 1) {
                    flipCode = -1;
                } else if (flipCode == -1) {
                    flipCode = 1;
                }
            }
                
            if (flipHorizontal) {
                if (flipCode == int.MinValue) {
                    flipCode = 1;
                } else if (flipCode == 0) {
                    flipCode = -1;
                } else if (flipCode == 1) {
                    flipCode = int.MinValue;
                } else if (flipCode == -1) {
                    flipCode = 0;
                }
            }
                
            if (flipCode > int.MinValue) {
                Core.flip (mat, mat, flipCode);
            }
        }

        /// <summary>
        /// Gets the buffer colors.
        /// </summary>
        /// <returns>The buffer colors.</returns>
        public Color32[] GetBufferColors ()
        {
            return colors;
        }

        /// <summary>
        /// To release the resources for the init method.
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
            if (rotatedRgbaMat != null) {
                rotatedRgbaMat.Dispose ();
                rotatedRgbaMat = null;
            }

            if (OnDisposedEvent != null)
                OnDisposedEvent.Invoke ();
        }

        /// <summary>
        /// Releases all resource used by the <see cref="WebCamTextureToMatHelper"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="WebCamTextureToMatHelper"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="WebCamTextureToMatHelper"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the <see cref="WebCamTextureToMatHelper"/> so
        /// the garbage collector can reclaim the memory that the <see cref="WebCamTextureToMatHelper"/> was occupying.</remarks>
        public void Dispose ()
        {
            if (initDone)
                dispose ();

            colors = null;
        }
    }
}