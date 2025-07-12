#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API

using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
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
        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        /// <summary>
        /// The requested device index dropdown.
        /// </summary>
        public Dropdown RequestedDeviceIndexDropdown;

        /// <summary>
        /// The requested device name dropdown.
        /// </summary>
        public Dropdown RequestedDeviceNameDropdown;

        /// <summary>
        /// Set the name of the device to use.
        /// </summary>
        [SerializeField, TooltipAttribute("Set the name of the device to use.")]
        public string RequestedDeviceName = null;

        /// <summary>
        /// Set the width of WebCamTexture.
        /// </summary>
        [SerializeField, TooltipAttribute("Set the width of WebCamTexture.")]
        public int RequestedWidth = 640;

        /// <summary>
        /// Set the height of WebCamTexture.
        /// </summary>
        [SerializeField, TooltipAttribute("Set the height of WebCamTexture.")]
        public int RequestedHeight = 480;

        /// <summary>
        /// Set FPS of WebCamTexture.
        /// </summary>
        [SerializeField, TooltipAttribute("Set FPS of WebCamTexture.")]
        public int RequestedFPS = 30;

        /// <summary>
        /// Set whether to use the front facing camera.
        /// </summary>
        [SerializeField, TooltipAttribute("Set whether to use the front facing camera.")]
        public bool RequestedIsFrontFacing = false;

        // Private Fields
        /// <summary>
        /// The webcam texture.
        /// </summary>
        private WebCamTexture _webCamTexture;

        /// <summary>
        /// The webcam device.
        /// </summary>
        private WebCamDevice _webCamDevice;

        /// <summary>
        /// The rgba mat.
        /// </summary>
        private Mat _rgbaMat;

        /// <summary>
        /// The colors.
        /// </summary>
        private Color32[] _colors;

        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// Indicates whether this instance is waiting for initialization to complete.
        /// </summary>
        private bool _isInitWaiting = false;

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        private bool _hasInitDone = false;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

#if ((UNITY_IOS || UNITY_WEBGL) && UNITY_2018_1_OR_NEWER) || (UNITY_ANDROID && UNITY_2018_3_OR_NEWER)
        private bool _isUserRequestingPermission;
#endif

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();


            // Retrieves available camera devices and populates dropdown menus with options:
            // one for selecting by device index and another by device name.
            // Adds a default "(EMPTY)" option to indicate no device selected.
            WebCamDevice[] devices = WebCamTexture.devices;

            RequestedDeviceIndexDropdown.ClearOptions();
            var deviceIndexOptions = new List<string>();
            deviceIndexOptions.Add("Device Index (EMPTY)");
            for (int i = 0; i < devices.Length; i++)
            {
                deviceIndexOptions.Add(i.ToString());
            }
            RequestedDeviceIndexDropdown.AddOptions(deviceIndexOptions);

            RequestedDeviceNameDropdown.ClearOptions();
            var deviceNameOptions = new List<string>();
            deviceNameOptions.Add("Device Name (EMPTY)");
            for (int i = 0; i < devices.Length; i++)
            {
                deviceNameOptions.Add(i + ": " + devices[i].name);
            }
            RequestedDeviceNameDropdown.AddOptions(deviceNameOptions);


            Initialize();
        }

        private void Update()
        {
            if (_hasInitDone && _webCamTexture.isPlaying && _webCamTexture.didUpdateThisFrame)
            {
                OpenCVMatUtils.WebCamTextureToMat(_webCamTexture, _rgbaMat, _colors);

                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                OpenCVMatUtils.MatToTexture2D(_rgbaMat, _texture, _colors);
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        // Public Methods
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
            if (_hasInitDone)
                _webCamTexture.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            if (_hasInitDone)
                _webCamTexture.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            if (_hasInitDone)
                _webCamTexture.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            if (_hasInitDone)
            {
                RequestedDeviceName = null;
                RequestedIsFrontFacing = !RequestedIsFrontFacing;
                Initialize();
            }
        }

        /// <summary>
        /// Raises the requested device index dropdown value changed event.
        /// </summary>
        public void OnRequestedDeviceIndexDropdownValueChanged(int result)
        {
            RequestedDeviceNameDropdown.value = 0;

            int index = result - 1; // Offset the default item
            RequestedDeviceName = (index >= 0) ? index.ToString() : string.Empty;
            Initialize();
        }

        /// <summary>
        /// Raises the requested device name dropdown value changed event.
        /// </summary>
        public void OnRequestedDeviceNameDropdownValueChanged(int result)
        {
            RequestedDeviceIndexDropdown.value = 0;

            WebCamDevice[] devices = WebCamTexture.devices;
            int index = result - 1; // Offset the default item
            RequestedDeviceName = (index >= 0) ? devices[index].name : string.Empty;
            Initialize();
        }

        // Private Methods
        /// <summary>
        /// Initializes webcam texture.
        /// </summary>
        private void Initialize()
        {
            if (_isInitWaiting)
                return;

#if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices (e.g. Google Pixel, Pixel2).
            // https://forum.unity.com/threads/android-webcamtexture-in-low-light-only-some-models.520656/
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            if (RequestedIsFrontFacing)
            {
                int rearCameraFPS = RequestedFPS;
                RequestedFPS = 15;
                StartCoroutine(_Initialize());
                RequestedFPS = rearCameraFPS;
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
            if (_hasInitDone)
                Dispose();

            _isInitWaiting = true;

            // Checks camera permission state.
#if (UNITY_IOS || UNITY_WEBGL) && UNITY_2018_1_OR_NEWER
            UserAuthorization mode = UserAuthorization.WebCam;
            if (!Application.HasUserAuthorization(mode))
            {
                _isUserRequestingPermission = true;
                yield return Application.RequestUserAuthorization(mode);

                float timeElapsed = 0;
                while (_isUserRequestingPermission)
                {
                    if (timeElapsed > 0.25f)
                    {
                        _isUserRequestingPermission = false;
                        break;
                    }
                    timeElapsed += Time.deltaTime;

                    yield return null;
                }
            }

            if (!Application.HasUserAuthorization(mode))
            {
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "Camera permission is denied.";
                }
                _isInitWaiting = false;
                yield break;
            }
#elif UNITY_ANDROID && UNITY_2018_3_OR_NEWER
            string permission = UnityEngine.Android.Permission.Camera;
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
            {
                _isUserRequestingPermission = true;
                UnityEngine.Android.Permission.RequestUserPermission(permission);

                float timeElapsed = 0;
                while (_isUserRequestingPermission)
                {
                    if (timeElapsed > 0.25f)
                    {
                        _isUserRequestingPermission = false;
                        break;
                    }
                    timeElapsed += Time.deltaTime;

                    yield return null;
                }
            }

            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
            {
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "Camera permission is denied.";
                }
                _isInitWaiting = false;
                yield break;
            }
#endif

            // Creates a WebCamTexture with settings closest to the requested name, resolution, and frame rate.
            var devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Debug.LogError("Camera device does not exist.");
                _isInitWaiting = false;
                yield break;
            }

            if (!String.IsNullOrEmpty(RequestedDeviceName))
            {
                // Try to parse requestedDeviceName as an index
                int requestedDeviceIndex = -1;
                if (Int32.TryParse(RequestedDeviceName, out requestedDeviceIndex))
                {
                    if (requestedDeviceIndex >= 0 && requestedDeviceIndex < devices.Length)
                    {
                        _webCamDevice = devices[requestedDeviceIndex];
                        _webCamTexture = new WebCamTexture(_webCamDevice.name, RequestedWidth, RequestedHeight, RequestedFPS);
                    }
                }
                else
                {
                    // Search for a device with a matching name
                    for (int cameraIndex = 0; cameraIndex < devices.Length; cameraIndex++)
                    {
                        if (devices[cameraIndex].name == RequestedDeviceName)
                        {
                            _webCamDevice = devices[cameraIndex];
                            _webCamTexture = new WebCamTexture(_webCamDevice.name, RequestedWidth, RequestedHeight, RequestedFPS);
                            break;
                        }
                    }
                }
                if (_webCamTexture == null)
                    Debug.Log("Cannot find camera device " + RequestedDeviceName + ".");
            }

            if (_webCamTexture == null)
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
                        if (device.kind == kind && device.isFrontFacing == RequestedIsFrontFacing)
                        {
                            _webCamDevice = device;
                            _webCamTexture = new WebCamTexture(_webCamDevice.name, RequestedWidth, RequestedHeight, RequestedFPS);
                            break;
                        }
                    }
                    if (_webCamTexture != null) break;
                }
            }

            if (_webCamTexture == null)
            {
                _webCamDevice = devices[0];
                _webCamTexture = new WebCamTexture(_webCamDevice.name, RequestedWidth, RequestedHeight, RequestedFPS);
            }

            // Starts the camera.
            _webCamTexture.Play();

            while (true)
            {
                if (_webCamTexture.didUpdateThisFrame)
                {
                    Debug.Log("name:" + _webCamTexture.deviceName + " width:" + _webCamTexture.width + " height:" + _webCamTexture.height + " fps:" + _webCamTexture.requestedFPS);
                    Debug.Log("videoRotationAngle:" + _webCamTexture.videoRotationAngle + " videoVerticallyMirrored:" + _webCamTexture.videoVerticallyMirrored + " isFrongFacing:" + _webCamDevice.isFrontFacing);

                    _isInitWaiting = false;
                    _hasInitDone = true;

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
        private IEnumerator OnApplicationFocus(bool hasFocus)
        {
            yield return null;

            if (_isUserRequestingPermission && hasFocus)
                _isUserRequestingPermission = false;
        }
#endif

        /// <summary>
        /// Releases all resource.
        /// </summary>
        private void Dispose()
        {
            _isInitWaiting = false;
            _hasInitDone = false;

            if (_webCamTexture != null)
            {
                _webCamTexture.Stop();
                WebCamTexture.Destroy(_webCamTexture);
                _webCamTexture = null;
            }
            _rgbaMat?.Dispose(); _rgbaMat = null;
            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
        }

        /// <summary>
        /// Raises the webcam texture initialized event.
        /// </summary>
        private void OnInited()
        {
            if (_colors == null || _colors.Length != _webCamTexture.width * _webCamTexture.height)
                _colors = new Color32[_webCamTexture.width * _webCamTexture.height];
            if (_texture == null || _texture.width != _webCamTexture.width || _texture.height != _webCamTexture.height)
                _texture = new Texture2D(_webCamTexture.width, _webCamTexture.height, TextureFormat.RGBA32, false);

            _rgbaMat = new Mat(_webCamTexture.height, _webCamTexture.width, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));
            OpenCVMatUtils.MatToTexture2D(_rgbaMat, _texture, _colors);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;


            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("deviceName", _webCamDevice.name.ToString());
                if (_webCamDevice.depthCameraName != null)
                    _fpsMonitor.Add("depthCameraName", _webCamDevice.depthCameraName.ToString());
                _fpsMonitor.Add("kind", _webCamDevice.kind.ToString());
                _fpsMonitor.Add("isFrontFacing", _webCamDevice.isFrontFacing.ToString());
                _fpsMonitor.Add("isAutoFocusPointSupported", _webCamDevice.isAutoFocusPointSupported.ToString());
                _fpsMonitor.Add("width", _rgbaMat.width().ToString());
                _fpsMonitor.Add("height", _rgbaMat.height().ToString());
                _fpsMonitor.Add("videoRotationAngle", _webCamTexture.videoRotationAngle.ToString());
                _fpsMonitor.Add("videoVerticallyMirrored", _webCamTexture.videoVerticallyMirrored.ToString());

                if (_webCamDevice.availableResolutions != null)
                {
                    _fpsMonitor.Add("availableResolutions", "[" + _webCamDevice.availableResolutions.Length.ToString() + "]");
                    var resolutions = _webCamDevice.availableResolutions;
                    for (int i = 0; i < resolutions.Length; i++)
                    {
                        _fpsMonitor.Add(" " + i, resolutions[i].ToString());
                    }
                }

                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }
        }
    }
}

#endif
