#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// WebCamTextureToMat Example
    /// An example of converting a WebCamTexture image to OpenCV's Mat format.
    /// </summary>
    public class WebCamTextureToMatExample : MonoBehaviour
    {
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        /// <summary>
        /// The requested device index dropdown.
        /// </summary>
        public Dropdown requestedDeviceIndexDropdown;

        /// <summary>
        /// The requested device name dropdown.
        /// </summary>
        public Dropdown requestedDeviceNameDropdown;

        /// <summary>
        /// Set the name of the device to use.
        /// </summary>
        [SerializeField, TooltipAttribute("Set the name of the device to use.")]
        public string requestedDeviceName = null;

        /// <summary>
        /// Set the width of WebCamTexture.
        /// </summary>
        [SerializeField, TooltipAttribute("Set the width of WebCamTexture.")]
        public int requestedWidth = 640;

        /// <summary>
        /// Set the height of WebCamTexture.
        /// </summary>
        [SerializeField, TooltipAttribute("Set the height of WebCamTexture.")]
        public int requestedHeight = 480;

        /// <summary>
        /// Set FPS of WebCamTexture.
        /// </summary>
        [SerializeField, TooltipAttribute("Set FPS of WebCamTexture.")]
        public int requestedFPS = 30;

        /// <summary>
        /// Set whether to use the front facing camera.
        /// </summary>
        [SerializeField, TooltipAttribute("Set whether to use the front facing camera.")]
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
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();


            // Retrieves available camera devices and populates dropdown menus with options:
            // one for selecting by device index and another by device name.
            // Adds a default "(EMPTY)" option to indicate no device selected.
            WebCamDevice[] devices = WebCamTexture.devices;

            requestedDeviceIndexDropdown.ClearOptions();
            var deviceIndexOptions = new List<string>();
            deviceIndexOptions.Add("Device Index (EMPTY)");
            for (int i = 0; i < devices.Length; i++)
            {
                deviceIndexOptions.Add(i.ToString());
            }
            requestedDeviceIndexDropdown.AddOptions(deviceIndexOptions);

            requestedDeviceNameDropdown.ClearOptions();
            var deviceNameOptions = new List<string>();
            deviceNameOptions.Add("Device Name (EMPTY)");
            for (int i = 0; i < devices.Length; i++)
            {
                deviceNameOptions.Add(i + ": " + devices[i].name);
            }
            requestedDeviceNameDropdown.AddOptions(deviceNameOptions);


            Initialize();
        }

        /// <summary>
        /// Initializes webcam texture.
        /// </summary>
        private void Initialize()
        {
            if (isInitWaiting)
                return;

#if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices (e.g. Google Pixel, Pixel2).
            // https://forum.unity.com/threads/android-webcamtexture-in-low-light-only-some-models.520656/
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            if (requestedIsFrontFacing)
            {
                int rearCameraFPS = requestedFPS;
                requestedFPS = 15;
                StartCoroutine(_Initialize());
                requestedFPS = rearCameraFPS;
            }
            else
            {
                StartCoroutine(_Initialize());
            }
#else
            StartCoroutine(_Initialize());
#endif
        }

        /// <summary>
        /// Initializes webcam texture by coroutine.
        /// </summary>
        private IEnumerator _Initialize()
        {
            if (hasInitDone)
                Dispose();

            isInitWaiting = true;

            // Checks camera permission state.
#if (UNITY_IOS || UNITY_WEBGL) && UNITY_2018_1_OR_NEWER
            UserAuthorization mode = UserAuthorization.WebCam;
            if (!Application.HasUserAuthorization(mode))
            {
                isUserRequestingPermission = true;
                yield return Application.RequestUserAuthorization(mode);

                float timeElapsed = 0;
                while (isUserRequestingPermission)
                {
                    if (timeElapsed > 0.25f)
                    {
                        isUserRequestingPermission = false;
                        break;
                    }
                    timeElapsed += Time.deltaTime;

                    yield return null;
                }
            }

            if (!Application.HasUserAuthorization(mode))
            {
                if (fpsMonitor != null)
                {
                    fpsMonitor.consoleText = "Camera permission is denied.";
                }
                isInitWaiting = false;
                yield break;
            }
#elif UNITY_ANDROID && UNITY_2018_3_OR_NEWER
            string permission = UnityEngine.Android.Permission.Camera;
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
            {
                isUserRequestingPermission = true;
                UnityEngine.Android.Permission.RequestUserPermission(permission);

                float timeElapsed = 0;
                while (isUserRequestingPermission)
                {
                    if (timeElapsed > 0.25f)
                    {
                        isUserRequestingPermission = false;
                        break;
                    }
                    timeElapsed += Time.deltaTime;

                    yield return null;
                }
            }

            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
            {
                if (fpsMonitor != null)
                {
                    fpsMonitor.consoleText = "Camera permission is denied.";
                }
                isInitWaiting = false;
                yield break;
            }
#endif

            // Creates a WebCamTexture with settings closest to the requested name, resolution, and frame rate.
            var devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Debug.LogError("Camera device does not exist.");
                isInitWaiting = false;
                yield break;
            }

            if (!String.IsNullOrEmpty(requestedDeviceName))
            {
                // Try to parse requestedDeviceName as an index
                int requestedDeviceIndex = -1;
                if (Int32.TryParse(requestedDeviceName, out requestedDeviceIndex))
                {
                    if (requestedDeviceIndex >= 0 && requestedDeviceIndex < devices.Length)
                    {
                        webCamDevice = devices[requestedDeviceIndex];
                        webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                    }
                }
                else
                {
                    // Search for a device with a matching name
                    for (int cameraIndex = 0; cameraIndex < devices.Length; cameraIndex++)
                    {
                        if (devices[cameraIndex].name == requestedDeviceName)
                        {
                            webCamDevice = devices[cameraIndex];
                            webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                            break;
                        }
                    }
                }
                if (webCamTexture == null)
                    Debug.Log("Cannot find camera device " + requestedDeviceName + ".");
            }

            if (webCamTexture == null)
            {
                var prioritizedKinds = new WebCamKind[]
                {
                    WebCamKind.WideAngle,
                    WebCamKind.Telephoto,
                    WebCamKind.UltraWideAngle,
                    WebCamKind.ColorAndDepth
                };

                // Checks how many and which cameras are available on the device
                foreach (var kind in prioritizedKinds)
                {
                    foreach (var device in devices)
                    {
                        if (device.kind == kind && device.isFrontFacing == requestedIsFrontFacing)
                        {
                            webCamDevice = device;
                            webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                            break;
                        }
                    }
                    if (webCamTexture != null) break;
                }
            }

            if (webCamTexture == null)
            {
                webCamDevice = devices[0];
                webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
            }

            // Starts the camera.
            webCamTexture.Play();

            while (true)
            {
                if (webCamTexture.didUpdateThisFrame)
                {
                    Debug.Log("name:" + webCamTexture.deviceName + " width:" + webCamTexture.width + " height:" + webCamTexture.height + " fps:" + webCamTexture.requestedFPS);
                    Debug.Log("videoRotationAngle:" + webCamTexture.videoRotationAngle + " videoVerticallyMirrored:" + webCamTexture.videoVerticallyMirrored + " isFrongFacing:" + webCamDevice.isFrontFacing);

                    isInitWaiting = false;
                    hasInitDone = true;

                    OnInited();

                    break;
                }
                else
                {
                    yield return null;
                }
            }
        }

#if ((UNITY_IOS || UNITY_WEBGL) && UNITY_2018_1_OR_NEWER) || (UNITY_ANDROID && UNITY_2018_3_OR_NEWER)
        bool isUserRequestingPermission;

        IEnumerator OnApplicationFocus(bool hasFocus)
        {
            yield return null;

            if (isUserRequestingPermission && hasFocus)
                isUserRequestingPermission = false;
        }
#endif

        /// <summary>
        /// Releases all resource.
        /// </summary>
        private void Dispose()
        {
            isInitWaiting = false;
            hasInitDone = false;

            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                WebCamTexture.Destroy(webCamTexture);
                webCamTexture = null;
            }
            if (rgbaMat != null)
            {
                rgbaMat.Dispose();
                rgbaMat = null;
            }
            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the webcam texture initialized event.
        /// </summary>
        private void OnInited()
        {
            if (colors == null || colors.Length != webCamTexture.width * webCamTexture.height)
                colors = new Color32[webCamTexture.width * webCamTexture.height];
            if (texture == null || texture.width != webCamTexture.width || texture.height != webCamTexture.height)
                texture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

            rgbaMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));
            Utils.matToTexture2D(rgbaMat, texture, colors);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            if (fpsMonitor != null)
            {
                fpsMonitor.Add("deviceName", webCamDevice.name.ToString());
                if (webCamDevice.depthCameraName != null)
                    fpsMonitor.Add("depthCameraName", webCamDevice.depthCameraName.ToString());
                fpsMonitor.Add("kind", webCamDevice.kind.ToString());
                fpsMonitor.Add("isFrontFacing", webCamDevice.isFrontFacing.ToString());
                fpsMonitor.Add("isAutoFocusPointSupported", webCamDevice.isAutoFocusPointSupported.ToString());
                fpsMonitor.Add("width", rgbaMat.width().ToString());
                fpsMonitor.Add("height", rgbaMat.height().ToString());
                fpsMonitor.Add("videoRotationAngle", webCamTexture.videoRotationAngle.ToString());
                fpsMonitor.Add("videoVerticallyMirrored", webCamTexture.videoVerticallyMirrored.ToString());

                if (webCamDevice.availableResolutions != null)
                {
                    fpsMonitor.Add("availableResolutions", "[" + webCamDevice.availableResolutions.Length.ToString() + "]");
                    var resolutions = webCamDevice.availableResolutions;
                    for (int i = 0; i < resolutions.Length; i++)
                    {
                        fpsMonitor.Add(" " + i, resolutions[i].ToString());
                    }
                }

                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (hasInitDone && webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame)
            {
                Utils.webCamTextureToMat(webCamTexture, rgbaMat, colors);

                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Utils.matToTexture2D(rgbaMat, texture, colors);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            Dispose();
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
            if (hasInitDone)
                webCamTexture.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            if (hasInitDone)
                webCamTexture.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            if (hasInitDone)
                webCamTexture.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            if (hasInitDone)
            {
                requestedDeviceName = null;
                requestedIsFrontFacing = !requestedIsFrontFacing;
                Initialize();
            }
        }

        /// <summary>
        /// Raises the requested device index dropdown value changed event.
        /// </summary>
        public void OnRequestedDeviceIndexDropdownValueChanged(int result)
        {
            requestedDeviceNameDropdown.value = 0;

            int index = result - 1; // Offset the default item
            requestedDeviceName = (index >= 0) ? index.ToString() : string.Empty;
            Initialize();
        }

        /// <summary>
        /// Raises the requested device name dropdown value changed event.
        /// </summary>
        public void OnRequestedDeviceNameDropdownValueChanged(int result)
        {
            requestedDeviceIndexDropdown.value = 0;

            WebCamDevice[] devices = WebCamTexture.devices;
            int index = result - 1; // Offset the default item
            requestedDeviceName = (index >= 0) ? devices[index].name : string.Empty;
            Initialize();
        }
    }
}

#endif