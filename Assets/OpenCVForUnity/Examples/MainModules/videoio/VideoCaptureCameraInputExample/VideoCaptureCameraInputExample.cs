using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.VideoioModule;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// VideoCapture Camera Input Example
    /// An example of input a camera stream using the VideoCapture class.
    /// Works well on Windows and MacOS platforms.
    /// </summary>
    public class VideoCaptureCameraInputExample : MonoBehaviour
    {
        /// <summary>
        /// The open botton.
        /// </summary>
        public Button openButton;

        /// <summary>
        /// The release botton.
        /// </summary>
        public Button releaseButton;

        /// <summary>
        /// The requested device id dropdown.
        /// </summary>
        public Dropdown requestedDeviceIdDropdown;

        /// <summary>
        /// The requested device id.
        /// </summary>
        public DeviceIdPreset requestedDeviceId = DeviceIdPreset._0;

        /// <summary>
        /// The requested resolution dropdown.
        /// </summary>
        public Dropdown requestedResolutionDropdown;

        /// <summary>
        /// The requested resolution.
        /// </summary>
        public ResolutionPreset requestedResolution = ResolutionPreset._640x480;

        /// <summary>
        /// The requested FPS dropdown.
        /// </summary>
        public Dropdown requestedFPSDropdown;

        /// <summary>
        /// The requested FPS.
        /// </summary>
        public FPSPreset requestedFPS = FPSPreset._30;

        /// <summary>
        /// The requested FOURCC dropdown.
        /// </summary>
        public Dropdown requestedFOURCCDropdown;

        /// <summary>
        /// The requested FOURCC.
        /// </summary>
        public FOURCCPreset requestedFOURCC = FOURCCPreset.None;

        /// <summary>
        /// The requested CONVERT_RGB toggle.
        /// </summary>
        public Toggle requestedCONVERT_RGBToggle;

        /// <summary>
        /// The videocapture.
        /// </summary>
        VideoCapture capture;

        /// <summary>
        /// The input mat.
        /// </summary>
        Mat inputMat;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        Mat rgbMat;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// Indicates whether the video is playing.
        /// </summary>
        bool isPlaying = false;

        /// <summary>
        /// The requested width.
        /// </summary>
        int requestedWidth = 640;

        /// <summary>
        /// The requested height.
        /// </summary>
        int requestedHeight = 480;

        /// <summary>
        /// The prop FOURCC.
        /// </summary>
        FOURCCPreset propFOURCC = FOURCCPreset.None;

        /// <summary>
        /// The prop ConvertRGB.
        /// </summary>
        bool propConvertRGB = true;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;


        // Use this for initialization
        IEnumerator Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();


            // Tricks for Adding camera permission settings to AndroidManifest.xml.
            Debug.Log(WebCamTexture.devices.Length);

            // Checks user camera permission state for Android and iOS.
            yield return RequestUserCameraPermission();


            capture = new VideoCapture();

            // Update GUI state
            requestedDeviceIdDropdown.value = Array.IndexOf(System.Enum.GetNames(typeof(DeviceIdPreset)), requestedDeviceId.ToString());
            requestedResolutionDropdown.value = (int)requestedResolution;
            requestedFPSDropdown.value = Array.IndexOf(System.Enum.GetNames(typeof(FPSPreset)), requestedFPS.ToString());
            requestedFOURCCDropdown.value = Array.IndexOf(System.Enum.GetNames(typeof(FOURCCPreset)), requestedFOURCC.ToString());
            requestedCONVERT_RGBToggle.isOn = true;

            releaseButton.interactable = false;

            Dimensions(requestedResolution, out requestedWidth, out requestedHeight);
        }

        protected virtual IEnumerator RequestUserCameraPermission()
        {
#if UNITY_IOS && UNITY_2018_1_OR_NEWER
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
                yield break;
            }
#else
            yield return true;
#endif
        }

#if (UNITY_IOS && UNITY_2018_1_OR_NEWER) || (UNITY_ANDROID && UNITY_2018_3_OR_NEWER)
        bool isUserRequestingPermission;

        IEnumerator OnApplicationFocus(bool hasFocus)
        {
            yield return null;

            if (isUserRequestingPermission && hasFocus)
                isUserRequestingPermission = false;
        }
#endif

        // Update is called once per frame
        void Update()
        {
            if (isPlaying)
            {
                if (capture.grab())
                {
                    capture.retrieve(inputMat);

                    cvtColor(inputMat, rgbMat, propFOURCC, propConvertRGB);

                    Utils.matToTexture2D(rgbMat, texture);
                }
            }
        }

        protected void cvtColor(Mat src, Mat dst, FOURCCPreset FOURCC, bool convertRGB)
        {
            if (convertRGB)
            {
                src.copyTo(dst);
                return;
            }

            switch (FOURCC)
            {
                case FOURCCPreset.MJPG:
                    dst = Imgcodecs.imdecode(src, Imgcodecs.IMREAD_COLOR);
                    break;
                case FOURCCPreset.RGB3:
                    src.copyTo(dst);
                    break;
                case FOURCCPreset.BGR3:
                    Imgproc.cvtColor(src, dst, Imgproc.COLOR_BGR2RGB);
                    break;
                case FOURCCPreset.GRAY:
                    Imgproc.cvtColor(src, dst, Imgproc.COLOR_GRAY2RGB);
                    break;
                case FOURCCPreset.NV21:
                    if (src.channels() != 1) src = src.reshape(1, dst.rows() + (dst.rows() / 2));
                    Imgproc.cvtColor(src, dst, Imgproc.COLOR_YUV2RGB_NV21);
                    break;
                case FOURCCPreset.YV12:
                    if (src.channels() != 1) src = src.reshape(1, dst.rows() + (dst.rows() / 2));
                    Imgproc.cvtColor(src, dst, Imgproc.COLOR_YUV2RGB_YV12);
                    break;
                case FOURCCPreset.YUYV:
                    if (src.channels() != 2) src = src.reshape(2, dst.rows());
                    Imgproc.cvtColor(src, dst, Imgproc.COLOR_YUV2RGB_YUYV);
                    break;
                case FOURCCPreset.YUY2:
                    if (src.channels() != 2) src = src.reshape(2, dst.rows());
                    Imgproc.cvtColor(src, dst, Imgproc.COLOR_YUV2RGB_YUY2);
                    break;
                case FOURCCPreset.NV12:
                    if (src.channels() != 1) src = src.reshape(1, dst.rows() + (dst.rows() / 2));
                    Imgproc.cvtColor(src, dst, Imgproc.COLOR_YUV2RGB_NV12);
                    break;
                case FOURCCPreset.H264:
                    src.copyTo(dst);
                    break;
                default:
                    src.copyTo(dst);
                    break;
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            capture.release();

            if (inputMat != null)
                inputMat.Dispose();

            if (rgbMat != null)
                rgbMat.Dispose();

            Texture2D.Destroy(texture);
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the open button click event.
        /// </summary>
        public void OnOpenButtonClick()
        {
            if (capture.isOpened())
                return;

            capture.open((int)requestedDeviceId);

            if (!capture.isOpened())
            {
                Debug.LogError("capture.isOpened() is false. " + "DeviceId:" + (int)requestedDeviceId);

                if (fpsMonitor != null)
                    fpsMonitor.consoleText = "capture.isOpened() is false. " + "DeviceId:" + (int)requestedDeviceId;

                capture.release();

                return;
            }

            capture.set(Videoio.CAP_PROP_FRAME_WIDTH, requestedWidth);
            capture.set(Videoio.CAP_PROP_FRAME_HEIGHT, requestedHeight);
            capture.set(Videoio.CAP_PROP_FPS, (int)requestedFPS);

            switch (requestedFOURCC)
            {
                case FOURCCPreset.None:
                    //
                    break;
                case FOURCCPreset.MJPG:
                    capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('M', 'J', 'P', 'G'));
                    break;
                case FOURCCPreset.RGB3:
                    capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('R', 'G', 'B', '3'));
                    break;
                case FOURCCPreset.BGR3:
                    capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('B', 'G', 'R', '3'));
                    break;
                case FOURCCPreset.GRAY:
                    capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('G', 'R', 'A', 'Y'));
                    break;
                case FOURCCPreset.NV21:
                    capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('N', 'V', '2', '1'));
                    break;
                case FOURCCPreset.YV12:
                    capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('Y', 'V', '1', '2'));
                    break;
                case FOURCCPreset.YUYV:
                    capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('Y', 'U', 'Y', 'V'));
                    break;
                case FOURCCPreset.YUY2:
                    capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('Y', 'U', 'Y', '2'));
                    break;
                case FOURCCPreset.NV12:
                    capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('N', 'V', '1', '2'));
                    break;
                case FOURCCPreset.H264:
                    capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('H', '2', '6', '4'));
                    break;
                default:
                    //
                    break;
            }

            if (!requestedCONVERT_RGBToggle.isOn)
            {
                capture.set(Videoio.CAP_PROP_CONVERT_RGB, 0);
            }
            else
            {
                capture.set(Videoio.CAP_PROP_CONVERT_RGB, 1);
            }

            Debug.Log("CAP_PROP_FORMAT: " + capture.get(Videoio.CAP_PROP_FORMAT));
            Debug.Log("CAP_PROP_FPS: " + capture.get(Videoio.CAP_PROP_FPS));
            Debug.Log("CAP_PROP_FRAME_WIDTH: " + capture.get(Videoio.CAP_PROP_FRAME_WIDTH));
            Debug.Log("CAP_PROP_FRAME_HEIGHT: " + capture.get(Videoio.CAP_PROP_FRAME_HEIGHT));
            double ext = capture.get(Videoio.CAP_PROP_FOURCC);
            Debug.Log("CAP_PROP_FOURCC: " + (char)((int)ext & 0XFF) + (char)(((int)ext & 0XFF00) >> 8) + (char)(((int)ext & 0XFF0000) >> 16) + (char)(((int)ext & 0XFF000000) >> 24));
            Debug.Log("CAP_PROP_CONVERT_RGB: " + capture.get(Videoio.CAP_PROP_CONVERT_RGB));

            Debug.Log("CAP_PROP_MODE: " + capture.get(Videoio.CAP_PROP_MODE));
            Debug.Log("CAP_PROP_BRIGHTNESS: " + capture.get(Videoio.CAP_PROP_BRIGHTNESS));
            Debug.Log("CAP_PROP_CONTRAST: " + capture.get(Videoio.CAP_PROP_CONTRAST));
            Debug.Log("CAP_PROP_SATURATION: " + capture.get(Videoio.CAP_PROP_SATURATION));
            Debug.Log("CAP_PROP_HUE: " + capture.get(Videoio.CAP_PROP_HUE));
            Debug.Log("CAP_PROP_GAIN: " + capture.get(Videoio.CAP_PROP_GAIN));
            Debug.Log("CAP_PROP_EXPOSURE: " + capture.get(Videoio.CAP_PROP_EXPOSURE));
            Debug.Log("CAP_PROP_AUTO_EXPOSURE: " + capture.get(Videoio.CAP_PROP_AUTO_EXPOSURE));
            Debug.Log("CAP_PROP_GAMMA: " + capture.get(Videoio.CAP_PROP_GAMMA));
            Debug.Log("CAP_PROP_ZOOM: " + capture.get(Videoio.CAP_PROP_ZOOM));
            Debug.Log("CAP_PROP_FOCUS: " + capture.get(Videoio.CAP_PROP_FOCUS));
            Debug.Log("CAP_PROP_AUTOFOCUS: " + capture.get(Videoio.CAP_PROP_AUTOFOCUS));
            Debug.Log("CAP_PROP_BUFFERSIZE: " + capture.get(Videoio.CAP_PROP_BUFFERSIZE));
            Debug.Log("CAP_PROP_SAR_NUM: " + capture.get(Videoio.CAP_PROP_SAR_NUM));
            Debug.Log("CAP_PROP_SAR_DEN: " + capture.get(Videoio.CAP_PROP_SAR_DEN));
            Debug.Log("CAP_PROP_BACKEND: " + capture.get(Videoio.CAP_PROP_BACKEND));
            Debug.Log("CAP_PROP_AUTO_WB: " + capture.get(Videoio.CAP_PROP_AUTO_WB));
            Debug.Log("CAP_PROP_WB_TEMPERATURE: " + capture.get(Videoio.CAP_PROP_WB_TEMPERATURE));
            Debug.Log("CAP_PROP_CODEC_PIXEL_FORMAT: " + capture.get(Videoio.CAP_PROP_CODEC_PIXEL_FORMAT));


            inputMat = new Mat();

            int grabFrameCount = 0;
            while (!(capture.grab() && capture.retrieve(inputMat)))
            {
                if (grabFrameCount > 30)
                {
                    Debug.LogError("The grab() and retrieve(() method is not work.");

                    if (fpsMonitor != null)
                        fpsMonitor.consoleText = "The grab() and retrieve(() method is not work.";

                    capture.release();

                    break;
                }
                grabFrameCount++;
            }



            if (inputMat.width() == 0 && inputMat.height() == 0)
            {
                Debug.LogError("The inputMat size returned was 0x0.");

                if (fpsMonitor != null)
                    fpsMonitor.consoleText = "The inputMat size returned was 0x0.";

                capture.release();

                return;
            }

#if UNITY_IOS
            int frameWidth = (int)capture.get(Videoio.CAP_PROP_FRAME_HEIGHT);
            int frameHeight = (int)capture.get(Videoio.CAP_PROP_FRAME_WIDTH);
#else
            int frameWidth = (int)capture.get(Videoio.CAP_PROP_FRAME_WIDTH);
            int frameHeight = (int)capture.get(Videoio.CAP_PROP_FRAME_HEIGHT);
#endif

            rgbMat = new Mat(frameHeight, frameWidth, CvType.CV_8UC3);
            texture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGB24, false);

            gameObject.transform.localScale = new Vector3((float)frameWidth, (float)frameHeight, 1);
            float widthScale = (float)Screen.width / (float)frameWidth;
            float heightScale = (float)Screen.height / (float)frameHeight;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = ((float)frameWidth * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = (float)frameHeight / 2;
            }

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;


            string strFOURCC = "" + (char)((int)ext & 0XFF) + (char)(((int)ext & 0XFF00) >> 8) + (char)(((int)ext & 0XFF0000) >> 16) + (char)(((int)ext & 0XFF000000) >> 24);
            switch (strFOURCC)
            {
                case "MJPG":
                    propFOURCC = FOURCCPreset.MJPG;
                    break;
                case "RGB3":
                    propFOURCC = FOURCCPreset.RGB3;
                    break;
                case "BGR3":
                    propFOURCC = FOURCCPreset.BGR3;
                    break;
                case "GRAY":
                    propFOURCC = FOURCCPreset.GRAY;
                    break;
                case "NV21":
                    propFOURCC = FOURCCPreset.NV21;
                    break;
                case "YV12":
                    propFOURCC = FOURCCPreset.YV12;
                    break;
                case "YUYV":
                    propFOURCC = FOURCCPreset.YUYV;
                    break;
                case "YUY2":
                    propFOURCC = FOURCCPreset.YUY2;
                    break;
                case "NV12":
                    propFOURCC = FOURCCPreset.NV12;
                    break;
                case "H264":
                    propFOURCC = FOURCCPreset.H264;
                    break;
                default:
                    //propFOURCC = FOURCCPreset.None;
                    propFOURCC = requestedFOURCC;
                    break;
            }

            if (capture.get(Videoio.CAP_PROP_CONVERT_RGB) == 1)
            {
                propConvertRGB = true;
            }
            else
            {
                propConvertRGB = false;
            }


            // The frame size returned by VideoCapture property may differ from the returned Mat.
            // On iOS platform, the height and width of the frame size are swapped.
            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "";
                fpsMonitor.Add("getBackendName", capture.getBackendName());
                fpsMonitor.Add("CAP_PROP_FORMAT", capture.get(Videoio.CAP_PROP_FORMAT).ToString());
                fpsMonitor.Add("CAP_PROP_FPS", capture.get(Videoio.CAP_PROP_FPS).ToString());
                fpsMonitor.Add("CAP_PROP_FRAME_WIDTH", capture.get(Videoio.CAP_PROP_FRAME_WIDTH).ToString());
                fpsMonitor.Add("CAP_PROP_FRAME_HEIGHT", capture.get(Videoio.CAP_PROP_FRAME_HEIGHT).ToString());
                fpsMonitor.Add("CAP_PROP_FOURCC", "" + (char)((int)ext & 0XFF) + (char)(((int)ext & 0XFF00) >> 8) + (char)(((int)ext & 0XFF0000) >> 16) + (char)(((int)ext & 0XFF000000) >> 24));
                fpsMonitor.Add("CAP_PROP_CONVERT_RGB", capture.get(Videoio.CAP_PROP_CONVERT_RGB).ToString());
                fpsMonitor.Add("inputMat", "size:" + inputMat.width() + "x" + inputMat.height() + " type:" + CvType.typeToString(inputMat.type()));
            }

            Utils.setDebugMode(true, true);
            try
            {
                cvtColor(inputMat, rgbMat, propFOURCC, propConvertRGB);
                Utils.matToTexture2D(rgbMat, texture);

                openButton.interactable =
                requestedDeviceIdDropdown.interactable =
                requestedResolutionDropdown.interactable =
                requestedFPSDropdown.interactable =
                requestedFOURCCDropdown.interactable =
                requestedCONVERT_RGBToggle.interactable = false;
                releaseButton.interactable = true;

                isPlaying = true;
            }
            catch (Exception e)
            {
                Debug.LogError("CVException: " + e);

                if (fpsMonitor != null)
                {
                    fpsMonitor.consoleText = "CVException: " + e;
                }

                capture.release();
            }
            Utils.setDebugMode(false);

        }

        /// <summary>
        /// Raises the release button click event.
        /// </summary>
        public void OnReleaseButtonClick()
        {
            if (!capture.isOpened())
                return;

            capture.release();

            if (inputMat != null)
                inputMat.Dispose();

            if (rgbMat != null)
                rgbMat.Dispose();

            Texture2D.Destroy(texture);

            openButton.interactable =
            requestedDeviceIdDropdown.interactable =
            requestedResolutionDropdown.interactable =
            requestedFPSDropdown.interactable =
            requestedFOURCCDropdown.interactable =
            requestedCONVERT_RGBToggle.interactable = true;
            releaseButton.interactable = false;

            isPlaying = false;

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "";
            }
        }

        /// <summary>
        /// Raises the requested device id dropdown value changed event.
        /// </summary>
        public void OnRequestedDeviceIdDropdownValueChanged(int result)
        {
            string[] enumNames = Enum.GetNames(typeof(DeviceIdPreset));
            byte value = (byte)System.Enum.Parse(typeof(DeviceIdPreset), enumNames[result], true);

            if ((byte)requestedDeviceId != value)
            {
                requestedDeviceId = (DeviceIdPreset)value;
            }

            //Debug.Log((int)requestedDeviceId);
        }

        /// <summary>
        /// Raises the requested resolution dropdown value changed event.
        /// </summary>
        public void OnRequestedResolutionDropdownValueChanged(int result)
        {
            if ((int)requestedResolution != result)
            {
                requestedResolution = (ResolutionPreset)result;

                Dimensions(requestedResolution, out requestedWidth, out requestedHeight);
            }

            //Debug.Log((int)requestedWidth + " " + (int)requestedHeight);
        }

        /// <summary>
        /// Raises the requested FOURCC dropdown value changed event.
        /// </summary>
        public void OnRequestedFOURCCDropdownValueChanged(int result)
        {

            string[] enumNames = Enum.GetNames(typeof(FOURCCPreset));
            byte value = (byte)System.Enum.Parse(typeof(FOURCCPreset), enumNames[result], true);

            if ((byte)requestedFOURCC != value)
            {
                requestedFOURCC = (FOURCCPreset)value;
            }

            //Debug.Log((int)requestedFOURCC);
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
            }

            //Debug.Log((int)requestedFPS);
        }

        /// <summary>
        /// Raises the Requested CONVERT_RGB toggle value changed event.
        /// </summary>
        public void OnRequestedCONVERT_RGBToggleValueChanged()
        {

            //Debug.Log(requestedCONVERT_RGBToggle.isOn);
        }

        public enum DeviceIdPreset : byte
        {
            _0 = 0,
            _1,
            _2,
            _3,
            _4,
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
        public enum FOURCCPreset : byte
        {
            None = 0,
            MJPG,
            RGB3,
            BGR3,
            GRAY,
            NV21,
            YV12,
            YUYV,
            YUY2,
            NV12,
            H264,
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