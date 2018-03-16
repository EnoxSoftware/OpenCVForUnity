using OpenCVForUnity;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Webcam texture to mat helper.
    /// v 1.0.6
    /// </summary>
    public class WebCamTextureToMatHelper : MonoBehaviour
    {
        /// <summary>
        /// Set the name of the device to use. (or device index)
        /// </summary>
        [SerializeField, FormerlySerializedAs("requestedDeviceName"), TooltipAttribute ("Set the name of the device to use. (or device index)")]
        protected string _requestedDeviceName = null;
        public string requestedDeviceName {
            get { return _requestedDeviceName; } 
            set { _requestedDeviceName = value; }
        }

        /// <summary>
        /// Set the width of WebCamTexture.
        /// </summary>
        [SerializeField, FormerlySerializedAs("requestedWidth"), TooltipAttribute ("Set the width of WebCamTexture.")]
        protected int _requestedWidth = 640;
        public int requestedWidth {
            get { return _requestedWidth; } 
            set { _requestedWidth = (int)Mathf.Clamp(value, 0f, float.MaxValue); }
        }

        /// <summary>
        /// Set the height of WebCamTexture.
        /// </summary>
        [SerializeField, FormerlySerializedAs("requestedHeight"), TooltipAttribute ("Set the height of WebCamTexture.")]
        protected int _requestedHeight = 480;
        public int requestedHeight {
            get { return _requestedHeight; } 
            set { _requestedHeight = (int)Mathf.Clamp(value, 0f, float.MaxValue); }
        }

        /// <summary>
        /// Set whether to use the front facing camera.
        /// </summary>
        [SerializeField, FormerlySerializedAs("requestedIsFrontFacing"), TooltipAttribute ("Set whether to use the front facing camera.")]
        protected bool _requestedIsFrontFacing = false;
        public bool requestedIsFrontFacing {
            get { return _requestedIsFrontFacing; } 
            set { _requestedIsFrontFacing = value; }
        }

        /// <summary>
        /// Set FPS of WebCamTexture.
        /// </summary>
        [SerializeField, FormerlySerializedAs("requestedFPS"), TooltipAttribute ("Set FPS of WebCamTexture.")]
        protected int _requestedFPS = 30;
        public int requestedFPS {
            get { return _requestedFPS; } 
            set { _requestedFPS = (int)Mathf.Clamp(value, 0f, float.MaxValue); }
        }

        /// <summary>
        /// Sets whether to rotate WebCamTexture 90 degrees.
        /// </summary>
        [SerializeField, FormerlySerializedAs("requestedRotate90Degree"), TooltipAttribute ("Sets whether to rotate WebCamTexture 90 degrees.")]
        protected bool _requestedRotate90Degree = false;
        public bool requestedRotate90Degree {
            get { return _requestedRotate90Degree; } 
            set { _requestedRotate90Degree = value; }
        }

        /// <summary>
        /// Determines if flips vertically.
        /// </summary>
        [SerializeField, FormerlySerializedAs("flipVertical"), TooltipAttribute ("Determines if flips vertically.")]
        protected bool _flipVertical = false;
        public bool flipVertical {
            get { return _flipVertical; } 
            set { _flipVertical = value; }
        }

        /// <summary>
        /// Determines if flips horizontal.
        /// </summary>
        [SerializeField, FormerlySerializedAs("flipHorizontal"), TooltipAttribute ("Determines if flips horizontal.")]
        protected bool _flipHorizontal = false;
        public bool flipHorizontal {
            get { return _flipHorizontal; } 
            set { _flipHorizontal = value; }
        }
            
        /// <summary>
        /// The number of frames before the initialization process times out.
        /// </summary>
        [SerializeField, FormerlySerializedAs("timeoutFrameCount"), TooltipAttribute ("The number of frames before the initialization process times out.")]
        protected int _timeoutFrameCount = 300;
        public int timeoutFrameCount {
            get { return _timeoutFrameCount; } 
            set { _timeoutFrameCount = (int)Mathf.Clamp(value, 0f, float.MaxValue); }
        }

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
        /// The frame mat.
        /// </summary>
        protected Mat frameMat;

        /// <summary>
        /// The rotated frame mat
        /// </summary>
        protected Mat rotatedFrameMat;

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
        /// The initialization coroutine.
        /// </summary>
        protected IEnumerator initCoroutine;

        /// <summary>
        /// Orientation of the screen.
        /// </summary>
        protected ScreenOrientation screenOrientation;

        [System.Serializable]
        public enum ErrorCode :int
        {
            UNKNOWN = 0,
            CAMERA_DEVICE_NOT_EXIST = 1,
            TIMEOUT = 2,
        }

        [System.Serializable]
        public class ErrorUnityEvent : UnityEngine.Events.UnityEvent<ErrorCode>
        {
            
        }

        protected void OnValidate()
        {
            _requestedWidth = (int)Mathf.Clamp(_requestedWidth, 0f, float.MaxValue);
            _requestedHeight = (int)Mathf.Clamp(_requestedHeight, 0f, float.MaxValue);
            _requestedFPS = (int)Mathf.Clamp(_requestedFPS, 0f, float.MaxValue);
            _timeoutFrameCount = (int)Mathf.Clamp(_timeoutFrameCount, 0f, float.MaxValue);
        }

        // Update is called once per frame
        protected virtual void Update ()
        {
            if (hasInitDone) {
                if (screenOrientation != Screen.orientation) {
                    initCoroutine = _Initialize ();
                    StartCoroutine (initCoroutine);
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
            {
                CancelInitCoroutine ();
                ReleaseResources ();
            }
            
            if (onInitialized == null)
                onInitialized = new UnityEvent ();
            if (onDisposed == null)
                onDisposed = new UnityEvent ();
            if (onErrorOccurred == null)
                onErrorOccurred = new ErrorUnityEvent ();
            
            initCoroutine = _Initialize ();
            StartCoroutine (initCoroutine);
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        /// <param name="deviceName">Device name.</param>
        /// <param name="requestedWidth">Requested width.</param>
        /// <param name="requestedHeight">Requested height.</param>
        /// <param name="requestedIsFrontFacing">If set to <c>true</c> requested to using the front camera.</param>
        /// <param name="requestedFPS">Requested FPS.</param>
        /// <param name="requestedRotate90Degree">If set to <c>true</c> requested to rotate WebCamTexture 90 degrees.</param>
        public virtual void Initialize (string deviceName, int requestedWidth, int requestedHeight, bool requestedIsFrontFacing = false, int requestedFPS = 30, bool requestedRotate90Degree = false)
        {
            if (isInitWaiting)
            {
                CancelInitCoroutine ();
                ReleaseResources ();
            }

            this._requestedDeviceName = deviceName;
            this._requestedWidth = requestedWidth;
            this._requestedHeight = requestedHeight;
            this._requestedIsFrontFacing = requestedIsFrontFacing;
            this._requestedFPS = requestedFPS;
            this._requestedRotate90Degree = requestedRotate90Degree;
            if (onInitialized == null)
                onInitialized = new UnityEvent ();
            if (onDisposed == null)
                onDisposed = new UnityEvent ();
            if (onErrorOccurred == null)
                onErrorOccurred = new ErrorUnityEvent ();

            initCoroutine = _Initialize ();
            StartCoroutine (initCoroutine);
        }

        /// <summary>
        /// Initializes this instance by coroutine.
        /// </summary>
        protected virtual IEnumerator _Initialize ()
        {
            if (hasInitDone)
            {
                ReleaseResources ();

                if (onDisposed != null)
                    onDisposed.Invoke ();
            }

            isInitWaiting = true;

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
                            webCamTexture = new WebCamTexture (requestedDeviceName, requestedWidth, requestedHeight, requestedFPS);
                        }
                    }
                }

                if (webCamTexture == null)
                    Debug.Log ("Cannot find webcam device " + requestedDeviceName + ".");
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

                    Debug.Log ("WebCamTextureToMatHelper:: " + "devicename:" + webCamTexture.deviceName + " name:" + webCamTexture.name + " width:" + webCamTexture.width + " height:" + webCamTexture.height + " fps:" + webCamTexture.requestedFPS
                        + " videoRotationAngle:" + webCamTexture.videoRotationAngle + " videoVerticallyMirrored:" + webCamTexture.videoVerticallyMirrored + " isFrongFacing:" + webCamDevice.isFrontFacing);

                    if (colors == null || colors.Length != webCamTexture.width * webCamTexture.height)
                        colors = new Color32[webCamTexture.width * webCamTexture.height];

                    frameMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
                    screenOrientation = Screen.orientation;

                    #if !UNITY_EDITOR && !(UNITY_STANDALONE || UNITY_WEBGL) 
                    if (screenOrientation == ScreenOrientation.Portrait || screenOrientation == ScreenOrientation.PortraitUpsideDown) {
                        rotatedFrameMat = new Mat (webCamTexture.width, webCamTexture.height, CvType.CV_8UC4);
                    }
                    #endif

                    if (requestedRotate90Degree) {
                        if (rotatedFrameMat == null)
                            rotatedFrameMat = new Mat (webCamTexture.width, webCamTexture.height, CvType.CV_8UC4);
                    }

                    isInitWaiting = false;
                    hasInitDone = true;
                    initCoroutine = null;

                    if (onInitialized != null)
                        onInitialized.Invoke ();

                    break;
                } else {
                    initFrameCount++;
                    yield return null;
                }
            }

            if (isTimeout) {
                webCamTexture.Stop ();
                webCamTexture = null;
                isInitWaiting = false;
                initCoroutine = null;

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
        /// Returns the camera to world matrix.
        /// </summary>
        /// <returns>The camera to world matrix.</returns>
        public virtual Matrix4x4 GetCameraToWorldMatrix ()
        {
            return Camera.main.cameraToWorldMatrix;
        }

        /// <summary>
        /// Returns the projection matrix matrix.
        /// </summary>
        /// <returns>The projection matrix.</returns>
        public virtual Matrix4x4 GetProjectionMatrix ()
        {
            return Camera.main.projectionMatrix;
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
        /// The Mat object's type is 'CV_8UC4' (RGBA).
        /// </summary>
        /// <returns>The mat of the current frame.</returns>
        public virtual Mat GetMat ()
        {
            if (!hasInitDone || !webCamTexture.isPlaying) {
                if (rotatedFrameMat != null) {
                    return rotatedFrameMat;
                } else {
                    return frameMat;
                }
            }

            Utils.webCamTextureToMat (webCamTexture, frameMat, colors);

            if (rotatedFrameMat != null) {

                Core.rotate (frameMat, rotatedFrameMat, Core.ROTATE_90_CLOCKWISE);

                FlipMat (rotatedFrameMat);

                return rotatedFrameMat;
            } else {

                FlipMat (frameMat);

                return frameMat;
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
                
            if (_flipVertical) {
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
                
            if (_flipHorizontal) {
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
        /// Cancel Init Coroutine.
        /// </summary>
        protected virtual void CancelInitCoroutine ()
        {
            if (initCoroutine != null) {
                StopCoroutine (initCoroutine);
                ((IDisposable)initCoroutine).Dispose ();
                initCoroutine = null;
            }
        }

        /// <summary>
        /// To release the resources.
        /// </summary>
        protected virtual void ReleaseResources ()
        {
            isInitWaiting = false;
            hasInitDone = false;

            if (webCamTexture != null) {
                webCamTexture.Stop ();
                webCamTexture = null;
            }
            if (frameMat != null) {
                frameMat.Dispose ();
                frameMat = null;
            }
            if (rotatedFrameMat != null) {
                rotatedFrameMat.Dispose ();
                rotatedFrameMat = null;
            }
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
            if (colors != null)
                colors = null;

            if (isInitWaiting)
            {
                CancelInitCoroutine ();
                ReleaseResources ();
            }
            else if (hasInitDone)
            {
                ReleaseResources ();

                if (onDisposed != null)
                    onDisposed.Invoke ();
            }
        }
    }
}