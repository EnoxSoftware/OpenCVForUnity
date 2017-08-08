using OpenCVForUnity;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Webcam texture to mat helper.
    /// v 1.0.2
    /// </summary>
    public class WebCamTextureToMatHelper : MonoBehaviour
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
        /// Set the requested frame rate of the camera device (in frames per second).
        /// </summary>
        public int requestedFPS = 30;

        /// <summary>
        /// Determines if flips vertically.
        /// </summary>
        public bool flipVertical = false;

        /// <summary>
        /// Determines if flips horizontal.
        /// </summary>
        public bool flipHorizontal = false;

        /// <summary>
        /// The timeout frame count.
        /// </summary>
        public int timeoutFrameCount = 300;

        /// <summary>
        /// UnityEvent that is triggered when this instance is initialized.
        /// </summary>
        public UnityEvent onInitialized;

        /// <summary>
        /// UnityEvent that is triggered when this instance is disposed.
        /// </summary>
        public UnityEvent onDisposed;

        /// <summary>
        /// UnityEvent that is triggered when this instance is error Occurred.
        /// </summary>
        public ErrorUnityEvent onErrorOccurred;

        /// <summary>
        /// The webcam texture.
        /// </summary>
        protected WebCamTexture webCamTexture;

        /// <summary>
        /// The webcam device.
        /// </summary>
        protected WebCamDevice webCamDevice;

        /// <summary>
        /// The rgba mat.
        /// </summary>
        protected Mat rgbaMat;

        /// <summary>
        /// The rotated rgba mat
        /// </summary>
        protected Mat rotatedRgbaMat;

        /// <summary>
        /// The buffer colors.
        /// </summary>
        protected Color32[] colors;

        /// <summary>
        /// Indicates whether this instance is waiting for initialization to complete.
        /// </summary>
        protected bool isInitWaiting = false;

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        protected bool hasInitDone = false;

        /// <summary>
        /// Orientation of the screen.
        /// </summary>
        protected ScreenOrientation screenOrientation = ScreenOrientation.Unknown;

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
        protected virtual void Update ()
        {
            if (hasInitDone) {
                if (screenOrientation != Screen.orientation) {
                    StartCoroutine (_Initialize ());
                }
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        protected virtual void OnDestroy ()
        {
            Dispose ();
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public virtual void Initialize ()
        {
            if (isInitWaiting)
                return;
            
            if (onInitialized == null)
                onInitialized = new UnityEvent ();
            if (onDisposed == null)
                onDisposed = new UnityEvent ();
            if (onErrorOccurred == null)
                onErrorOccurred = new ErrorUnityEvent ();
            
            StartCoroutine (_Initialize ());
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="deviceName">Device name.</param>
        /// <param name="requestedWidth">Requested width.</param>
        /// <param name="requestedHeight">Requested height.</param>
        /// <param name="requestedIsFrontFacing">If set to <c>true</c> requested to using the front camera.</param>
        /// <param name="requestedFPS">Requested FPS.</param>
        public virtual void Initialize (string deviceName, int requestedWidth, int requestedHeight, bool requestedIsFrontFacing = false, int requestedFPS = 30)
        {
            if (isInitWaiting)
                return;

            this.requestedDeviceName = deviceName;
            this.requestedWidth = requestedWidth;
            this.requestedHeight = requestedHeight;
            this.requestedIsFrontFacing = requestedIsFrontFacing;
            this.requestedFPS = requestedFPS;
            if (onInitialized == null)
                onInitialized = new UnityEvent ();
            if (onDisposed == null)
                onDisposed = new UnityEvent ();
            if (onErrorOccurred == null)
                onErrorOccurred = new ErrorUnityEvent ();

            StartCoroutine (_Initialize ());
        }

        /// <summary>
        /// Initializes this instance by coroutine.
        /// </summary>
        protected virtual IEnumerator _Initialize ()
        {
            if (hasInitDone)
                _Dispose ();

            isInitWaiting = true;

            if (!String.IsNullOrEmpty (requestedDeviceName)) {
                webCamTexture = new WebCamTexture (requestedDeviceName, requestedWidth, requestedHeight, requestedFPS);
            } else {
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
                    isInitWaiting = false;

                    if (onErrorOccurred != null)
                        onErrorOccurred.Invoke (ErrorCode.CAMERA_DEVICE_NOT_EXIST);

                    yield break;
                }
            }

            // Starts the camera
            webCamTexture.Play ();

            int initFrameCount = 0;
            bool isTimeout = false;

            while (true) {
                if (initFrameCount > timeoutFrameCount) {
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
                        if (initFrameCount > timeoutFrameCount) {
                            isTimeout = true;
                            break;
                        }else {
                            initFrameCount++;
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
                    screenOrientation = Screen.orientation;

                    #if !UNITY_EDITOR && !(UNITY_STANDALONE || UNITY_WEBGL) 
                    if (screenOrientation == ScreenOrientation.Portrait || screenOrientation == ScreenOrientation.PortraitUpsideDown) {
                        rotatedRgbaMat = new Mat (webCamTexture.width, webCamTexture.height, CvType.CV_8UC4);
                    }
                    #endif

                    isInitWaiting = false;
                    hasInitDone = true;

                    if (onInitialized != null)
                        onInitialized.Invoke ();

                    break;
                } else {
                    initFrameCount++;
                    yield return 0;
                }
            }

            if (isTimeout) {
                webCamTexture.Stop ();
                webCamTexture = null;
                isInitWaiting = false;

                if (onErrorOccurred != null)
                    onErrorOccurred.Invoke (ErrorCode.TIMEOUT);
            }
        }

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        /// <returns><c>true</c>, if this instance has been initialized, <c>false</c> otherwise.</returns>
        public virtual bool IsInitialized ()
        {
            return hasInitDone;
        }

        /// <summary>
        /// Starts the webcam texture.
        /// </summary>
        public virtual void Play ()
        {
            if (hasInitDone)
                webCamTexture.Play ();
        }

        /// <summary>
        /// Pauses the webcam texture
        /// </summary>
        public virtual void Pause ()
        {
            if (hasInitDone)
                webCamTexture.Pause ();
        }

        /// <summary>
        /// Stops the webcam texture.
        /// </summary>
        public virtual void Stop ()
        {
            if (hasInitDone)
                webCamTexture.Stop ();
        }

        /// <summary>
        /// Indicates whether the webcam texture is currently playing.
        /// </summary>
        /// <returns><c>true</c>, if the webcam texture is playing, <c>false</c> otherwise.</returns>
        public virtual bool IsPlaying ()
        {
            if (!hasInitDone)
                return false;
            return webCamTexture.isPlaying;
        }

        /// <summary>
        /// Returns the webcam texture.
        /// </summary>
        /// <returns>The webcam texture.</returns>
        public virtual WebCamTexture GetWebCamTexture ()
        {
            return (hasInitDone) ? webCamTexture : null;
        }

        /// <summary>
        /// Returns the webcam device.
        /// </summary>
        /// <returns>The webcam device.</returns>
        public virtual WebCamDevice GetWebCamDevice ()
        {
            return webCamDevice;
        }

        /// <summary>
        /// Indicates whether the video buffer of the frame has been updated.
        /// </summary>
        /// <returns><c>true</c>, if the video buffer has been updated <c>false</c> otherwise.</returns>
        public virtual bool DidUpdateThisFrame ()
        {
            if (!hasInitDone)
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
        /// Gets the mat of the current frame.
        /// </summary>
        /// <returns>The mat.</returns>
        public virtual Mat GetMat ()
        {
            if (!hasInitDone || !webCamTexture.isPlaying) {
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

                FlipMat (rotatedRgbaMat);

                return rotatedRgbaMat;
            } else {

                FlipMat (rgbaMat);

                return rgbaMat;
            }
        }

        /// <summary>
        /// Flips the mat.
        /// </summary>
        /// <param name="mat">Mat.</param>
        protected virtual void FlipMat (Mat mat)
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
        public virtual Color32[] GetBufferColors ()
        {
            return colors;
        }

        /// <summary>
        /// To release the resources for the initialized method.
        /// </summary>
        protected virtual void _Dispose ()
        {
            isInitWaiting = false;
            hasInitDone = false;

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

            if (onDisposed != null)
                onDisposed.Invoke ();
        }

        /// <summary>
        /// Releases all resource used by the <see cref="WebCamTextureToMatHelper"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="WebCamTextureToMatHelper"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="WebCamTextureToMatHelper"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the <see cref="WebCamTextureToMatHelper"/> so
        /// the garbage collector can reclaim the memory that the <see cref="WebCamTextureToMatHelper"/> was occupying.</remarks>
        public virtual void Dispose ()
        {
            if (hasInitDone)
                _Dispose ();

            if (colors != null)
                colors = null;
        }
    }
}