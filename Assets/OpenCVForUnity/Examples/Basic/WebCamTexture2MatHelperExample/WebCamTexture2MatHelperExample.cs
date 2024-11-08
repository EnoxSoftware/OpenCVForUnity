#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System;
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
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        /// <summary>
        /// The requested resolution dropdown.
        /// </summary>
        public Dropdown requestedResolutionDropdown;

        /// <summary>
        /// The requested resolution.
        /// </summary>
        public ResolutionPreset requestedResolution = ResolutionPreset._640x480;

        /// <summary>
        /// The requestedFPS dropdown.
        /// </summary>
        public Dropdown requestedFPSDropdown;

        /// <summary>
        /// The requestedFPS.
        /// </summary>
        public FPSPreset requestedFPS = FPSPreset._30;

        /// <summary>
        /// The rotate 90 degree toggle.
        /// </summary>
        public Toggle rotate90DegreeToggle;

        /// <summary>
        /// The flip vertical toggle.
        /// </summary>
        public Toggle flipVerticalToggle;

        /// <summary>
        /// The flip horizontal toggle.
        /// </summary>
        public Toggle flipHorizontalToggle;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTexture2MatHelper webCamTexture2MatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            // Get the WebCamTexture2MatHelper component attached to the current game object
            webCamTexture2MatHelper = gameObject.GetComponent<WebCamTexture2MatHelper>();

            // Set the requested width, height, FPS and ColorFormat
            int width, height;
            Dimensions(requestedResolution, out width, out height);
            webCamTexture2MatHelper.requestedWidth = width;
            webCamTexture2MatHelper.requestedHeight = height;
            webCamTexture2MatHelper.requestedFPS = (int)requestedFPS;
            webCamTexture2MatHelper.outputColorFormat = Source2MatHelperColorFormat.RGBA;

            // Initialize the webcam texture to Mat helper, which starts the webcam and prepares the conversion
            webCamTexture2MatHelper.Initialize();

            // Update GUI state
            requestedResolutionDropdown.value = (int)requestedResolution;
            string[] enumNames = System.Enum.GetNames(typeof(FPSPreset));
            int index = Array.IndexOf(enumNames, requestedFPS.ToString());
            requestedFPSDropdown.value = index;
            rotate90DegreeToggle.isOn = webCamTexture2MatHelper.rotate90Degree;
            flipVerticalToggle.isOn = webCamTexture2MatHelper.flipVertical;
            flipHorizontalToggle.isOn = webCamTexture2MatHelper.flipHorizontal;
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTexture2MatHelperInitialized()
        {
            Debug.Log("OnWebCamTexture2MatHelperInitialized");

            // Retrieve the current frame from the WebCamTexture2MatHelper as a Mat object
            Mat webCamTextureMat = webCamTexture2MatHelper.GetMat();

            // Create a new Texture2D with the same dimensions as the Mat and RGBA32 color format
            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);

            // Convert the Mat to a Texture2D, effectively transferring the image data
            Utils.matToTexture2D(webCamTextureMat, texture);

            // Set the Texture2D as the texture of the RawImage for preview.
            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            if (fpsMonitor != null)
            {
                fpsMonitor.Add("deviceName", webCamTexture2MatHelper.GetDeviceName().ToString());
                fpsMonitor.Add("width", webCamTexture2MatHelper.GetWidth().ToString());
                fpsMonitor.Add("height", webCamTexture2MatHelper.GetHeight().ToString());
                fpsMonitor.Add("videoRotationAngle", webCamTexture2MatHelper.GetWebCamTexture().videoRotationAngle.ToString());
                fpsMonitor.Add("videoVerticallyMirrored", webCamTexture2MatHelper.GetWebCamTexture().videoVerticallyMirrored.ToString());
                fpsMonitor.Add("camera fps", webCamTexture2MatHelper.GetFPS().ToString());
                fpsMonitor.Add("isFrontFacing", webCamTexture2MatHelper.IsFrontFacing().ToString());
                fpsMonitor.Add("rotate90Degree", webCamTexture2MatHelper.rotate90Degree.ToString());
                fpsMonitor.Add("flipVertical", webCamTexture2MatHelper.flipVertical.ToString());
                fpsMonitor.Add("flipHorizontal", webCamTexture2MatHelper.flipHorizontal.ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTexture2MatHelperDisposed()
        {
            Debug.Log("OnWebCamTexture2MatHelperDisposed");

            // Destroy the texture and set it to null
            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnWebCamTexture2MatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnWebCamTexture2MatHelperErrorOccurred " + errorCode + ":" + message);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        // Update is called once per frame
        void Update()
        {
            // Check if the web camera is playing and if a new frame was updated
            if (webCamTexture2MatHelper.IsPlaying() && webCamTexture2MatHelper.DidUpdateThisFrame())
            {
                // Retrieve the current frame as a Mat object
                Mat rgbaMat = webCamTexture2MatHelper.GetMat();

                // Add text overlay on the frame
                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                // Convert the Mat to a Texture2D to display it on a texture
                Utils.matToTexture2D(rgbaMat, texture);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            // Dispose of the webCamTexture2MatHelper object and release any resources held by it.
            webCamTexture2MatHelper.Dispose();
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
            webCamTexture2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTexture2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTexture2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTexture2MatHelper.requestedIsFrontFacing = !webCamTexture2MatHelper.requestedIsFrontFacing;
        }

        /// <summary>
        /// Raises the requested resolution dropdown value changed event.
        /// </summary>
        public void OnRequestedResolutionDropdownValueChanged(int result)
        {
            if ((int)requestedResolution != result)
            {
                requestedResolution = (ResolutionPreset)result;

                int width, height;
                Dimensions(requestedResolution, out width, out height);

                webCamTexture2MatHelper.Initialize(width, height);
            }
        }

        /// <summary>
        /// Raises the requestedFPS dropdown value changed event.
        /// </summary>
        public void OnRequestedFPSDropdownValueChanged(int result)
        {
            string[] enumNames = Enum.GetNames(typeof(FPSPreset));
            int value = (int)System.Enum.Parse(typeof(FPSPreset), enumNames[result], true);

            if ((int)requestedFPS != value)
            {
                requestedFPS = (FPSPreset)value;

                webCamTexture2MatHelper.requestedFPS = (int)requestedFPS;
            }
        }

        /// <summary>
        /// Raises the rotate 90 degree toggle value changed event.
        /// </summary>
        public void OnRotate90DegreeToggleValueChanged()
        {
            if (rotate90DegreeToggle.isOn != webCamTexture2MatHelper.rotate90Degree)
            {
                webCamTexture2MatHelper.rotate90Degree = rotate90DegreeToggle.isOn;

                if (fpsMonitor != null)
                    fpsMonitor.Add("rotate90Degree", webCamTexture2MatHelper.rotate90Degree.ToString());
            }
        }

        /// <summary>
        /// Raises the flip vertical toggle value changed event.
        /// </summary>
        public void OnFlipVerticalToggleValueChanged()
        {
            if (flipVerticalToggle.isOn != webCamTexture2MatHelper.flipVertical)
            {
                webCamTexture2MatHelper.flipVertical = flipVerticalToggle.isOn;

                if (fpsMonitor != null)
                    fpsMonitor.Add("flipVertical", webCamTexture2MatHelper.flipVertical.ToString());
            }
        }

        /// <summary>
        /// Raises the flip horizontal toggle value changed event.
        /// </summary>
        public void OnFlipHorizontalToggleValueChanged()
        {
            if (flipHorizontalToggle.isOn != webCamTexture2MatHelper.flipHorizontal)
            {
                webCamTexture2MatHelper.flipHorizontal = flipHorizontalToggle.isOn;

                if (fpsMonitor != null)
                    fpsMonitor.Add("flipHorizontal", webCamTexture2MatHelper.flipHorizontal.ToString());
            }
        }

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