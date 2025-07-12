#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API

using System;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// WebCamTexture2MatHelper Example
    /// </summary>
    [RequireComponent(typeof(WebCamTexture2MatHelper))]
    public class WebCamTexture2MatHelperExample : MonoBehaviour
    {
        // Enums
        public enum FPSPreset : int
        {
            _0 = 0,
            _1 = 1,
            _5 = 5,
            _10 = 10,
            _15 = 15,
            _30 = 30,
            _60 = 60,
        }

        public enum ResolutionPreset : byte
        {
            _50x50 = 0,
            _640x480,
            _1280x720,
            _1920x1080,
            _9999x9999,
        }

        // Constants

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        /// <summary>
        /// The requested resolution dropdown.
        /// </summary>
        public Dropdown RequestedResolutionDropdown;

        /// <summary>
        /// The requested resolution.
        /// </summary>
        public ResolutionPreset RequestedResolution = ResolutionPreset._640x480;

        /// <summary>
        /// The requestedFPS dropdown.
        /// </summary>
        public Dropdown RequestedFPSDropdown;

        /// <summary>
        /// The requestedFPS.
        /// </summary>
        public FPSPreset RequestedFPS = FPSPreset._30;

        /// <summary>
        /// The rotate 90 degree toggle.
        /// </summary>
        public Toggle Rotate90DegreeToggle;

        /// <summary>
        /// The flip vertical toggle.
        /// </summary>
        public Toggle FlipVerticalToggle;

        /// <summary>
        /// The flip horizontal toggle.
        /// </summary>
        public Toggle FlipHorizontalToggle;

        // Private Fields
        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        private WebCamTexture2MatHelper _webCamTexture2MatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            // Get the WebCamTexture2MatHelper component attached to the current game object
            _webCamTexture2MatHelper = gameObject.GetComponent<WebCamTexture2MatHelper>();

            // Set the requested width, height, FPS and ColorFormat
            int width, height;
            Dimensions(RequestedResolution, out width, out height);
            _webCamTexture2MatHelper.RequestedWidth = width;
            _webCamTexture2MatHelper.RequestedHeight = height;
            _webCamTexture2MatHelper.RequestedFPS = (int)RequestedFPS;
            _webCamTexture2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;

            // Initialize the webcam texture to Mat helper, which starts the webcam and prepares the conversion
            _webCamTexture2MatHelper.Initialize();

            // Update GUI state
            RequestedResolutionDropdown.value = (int)RequestedResolution;
            string[] enumNames = System.Enum.GetNames(typeof(FPSPreset));
            int index = Array.IndexOf(enumNames, RequestedFPS.ToString());
            RequestedFPSDropdown.value = index;
            Rotate90DegreeToggle.isOn = _webCamTexture2MatHelper.Rotate90Degree;
            FlipVerticalToggle.isOn = _webCamTexture2MatHelper.FlipVertical;
            FlipHorizontalToggle.isOn = _webCamTexture2MatHelper.FlipHorizontal;
        }

        private void Update()
        {
            // Check if the web camera is playing and if a new frame was updated
            if (_webCamTexture2MatHelper.IsPlaying() && _webCamTexture2MatHelper.DidUpdateThisFrame())
            {
                // Retrieve the current frame as a Mat object
                Mat rgbaMat = _webCamTexture2MatHelper.GetMat();

                // Add text overlay on the frame
                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                // Convert the Mat to a Texture2D to display it on a texture
                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private void OnDestroy()
        {
            // Dispose of the webCamTexture2MatHelper object and release any resources held by it.
            _webCamTexture2MatHelper?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTexture2MatHelperInitialized()
        {
            Debug.Log("OnWebCamTexture2MatHelperInitialized");

            // Retrieve the current frame from the WebCamTexture2MatHelper as a Mat object
            Mat webCamTextureMat = _webCamTexture2MatHelper.GetMat();

            // Create a new Texture2D with the same dimensions as the Mat and RGBA32 color format
            _texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);

            // Convert the Mat to a Texture2D, effectively transferring the image data
            OpenCVMatUtils.MatToTexture2D(webCamTextureMat, _texture);

            // Set the Texture2D as the texture of the RawImage for preview.
            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;


            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("DeviceName", _webCamTexture2MatHelper.GetDeviceName().ToString());
                _fpsMonitor.Add("Width", _webCamTexture2MatHelper.GetWidth().ToString());
                _fpsMonitor.Add("Height", _webCamTexture2MatHelper.GetHeight().ToString());
                _fpsMonitor.Add("videoRotationAngle", _webCamTexture2MatHelper.GetWebCamTexture().videoRotationAngle.ToString());
                _fpsMonitor.Add("videoVerticallyMirrored", _webCamTexture2MatHelper.GetWebCamTexture().videoVerticallyMirrored.ToString());
                _fpsMonitor.Add("FPS", _webCamTexture2MatHelper.GetFPS().ToString());
                _fpsMonitor.Add("IsFrontFacing", _webCamTexture2MatHelper.IsFrontFacing().ToString());
                _fpsMonitor.Add("Rotate90Degree", _webCamTexture2MatHelper.Rotate90Degree.ToString());
                _fpsMonitor.Add("FlipVertical", _webCamTexture2MatHelper.FlipVertical.ToString());
                _fpsMonitor.Add("FlipHorizontal", _webCamTexture2MatHelper.FlipHorizontal.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
            }
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTexture2MatHelperDisposed()
        {
            Debug.Log("OnWebCamTexture2MatHelperDisposed");

            // Destroy the texture and set it to null
            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnWebCamTexture2MatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnWebCamTexture2MatHelperErrorOccurred " + errorCode + ":" + message);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            // Load the specified scene when the back button is clicked
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            _webCamTexture2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            _webCamTexture2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            _webCamTexture2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            _webCamTexture2MatHelper.RequestedIsFrontFacing = !_webCamTexture2MatHelper.RequestedIsFrontFacing;
        }

        /// <summary>
        /// Raises the requested resolution dropdown value changed event.
        /// </summary>
        public void OnRequestedResolutionDropdownValueChanged(int result)
        {
            if ((int)RequestedResolution != result)
            {
                RequestedResolution = (ResolutionPreset)result;

                int width, height;
                Dimensions(RequestedResolution, out width, out height);

                _webCamTexture2MatHelper.Initialize(width, height);
            }
        }

        /// <summary>
        /// Raises the requestedFPS dropdown value changed event.
        /// </summary>
        public void OnRequestedFPSDropdownValueChanged(int result)
        {
            string[] enumNames = Enum.GetNames(typeof(FPSPreset));
            int value = (int)System.Enum.Parse(typeof(FPSPreset), enumNames[result], true);

            if ((int)RequestedFPS != value)
            {
                RequestedFPS = (FPSPreset)value;

                _webCamTexture2MatHelper.RequestedFPS = (int)RequestedFPS;
            }
        }

        /// <summary>
        /// Raises the rotate 90 degree toggle value changed event.
        /// </summary>
        public void OnRotate90DegreeToggleValueChanged()
        {
            if (Rotate90DegreeToggle.isOn != _webCamTexture2MatHelper.Rotate90Degree)
            {
                _webCamTexture2MatHelper.Rotate90Degree = Rotate90DegreeToggle.isOn;

                if (_fpsMonitor != null)
                    _fpsMonitor.Add("Rotate90Degree", _webCamTexture2MatHelper.Rotate90Degree.ToString());
            }
        }

        /// <summary>
        /// Raises the flip vertical toggle value changed event.
        /// </summary>
        public void OnFlipVerticalToggleValueChanged()
        {
            if (FlipVerticalToggle.isOn != _webCamTexture2MatHelper.FlipVertical)
            {
                _webCamTexture2MatHelper.FlipVertical = FlipVerticalToggle.isOn;

                if (_fpsMonitor != null)
                    _fpsMonitor.Add("FlipVertical", _webCamTexture2MatHelper.FlipVertical.ToString());
            }
        }

        /// <summary>
        /// Raises the flip horizontal toggle value changed event.
        /// </summary>
        public void OnFlipHorizontalToggleValueChanged()
        {
            if (FlipHorizontalToggle.isOn != _webCamTexture2MatHelper.FlipHorizontal)
            {
                _webCamTexture2MatHelper.FlipHorizontal = FlipHorizontalToggle.isOn;

                if (_fpsMonitor != null)
                    _fpsMonitor.Add("FlipHorizontal", _webCamTexture2MatHelper.FlipHorizontal.ToString());
            }
        }

        // Private Methods
        private void Dimensions(ResolutionPreset preset, out int width, out int height)
        {
            switch (preset)
            {
                case ResolutionPreset._50x50:
                    width = 50;
                    height = 50;
                    break;
                case ResolutionPreset._640x480:
                    width = 640;
                    height = 480;
                    break;
                case ResolutionPreset._1280x720:
                    width = 1280;
                    height = 720;
                    break;
                case ResolutionPreset._1920x1080:
                    width = 1920;
                    height = 1080;
                    break;
                case ResolutionPreset._9999x9999:
                    width = 9999;
                    height = 9999;
                    break;
                default:
                    width = height = 0;
                    break;
            }
        }
    }
}

#endif
