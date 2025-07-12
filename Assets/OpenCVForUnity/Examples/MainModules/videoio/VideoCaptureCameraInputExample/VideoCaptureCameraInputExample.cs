using System;
using System.Collections;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.VideoioModule;
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
        // Enums
        /// <summary>
        /// Device ID preset enum
        /// </summary>
        public enum DeviceIdPreset : byte
        {
            _0 = 0,
            _1,
            _2,
            _3,
            _4,
        }

        /// <summary>
        /// FPS preset enum
        /// </summary>
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

        /// <summary>
        /// Resolution preset enum
        /// </summary>
        public enum ResolutionPreset : byte
        {
            _50x50 = 0,
            _640x480,
            _1280x720,
            _1920x1080,
            _9999x9999,
        }

        /// <summary>
        /// FOURCC preset enum
        /// </summary>
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

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        /// <summary>
        /// The open botton.
        /// </summary>
        public Button OpenButton;

        /// <summary>
        /// The release botton.
        /// </summary>
        public Button ReleaseButton;

        /// <summary>
        /// The requested device id dropdown.
        /// </summary>
        public Dropdown RequestedDeviceIdDropdown;

        /// <summary>
        /// The requested device id.
        /// </summary>
        public DeviceIdPreset RequestedDeviceId = DeviceIdPreset._0;

        /// <summary>
        /// The requested resolution dropdown.
        /// </summary>
        public Dropdown RequestedResolutionDropdown;

        /// <summary>
        /// The requested resolution.
        /// </summary>
        public ResolutionPreset RequestedResolution = ResolutionPreset._640x480;

        /// <summary>
        /// The requested FPS dropdown.
        /// </summary>
        public Dropdown RequestedFPSDropdown;

        /// <summary>
        /// The requested FPS.
        /// </summary>
        public FPSPreset RequestedFPS = FPSPreset._30;

        /// <summary>
        /// The requested FOURCC dropdown.
        /// </summary>
        public Dropdown RequestedFOURCCDropdown;

        /// <summary>
        /// The requested FOURCC.
        /// </summary>
        public FOURCCPreset RequestedFOURCC = FOURCCPreset.None;

        /// <summary>
        /// The requested CONVERT_RGB toggle.
        /// </summary>
        public Toggle RequestedCONVERT_RGBToggle;

        // Private Fields
        /// <summary>
        /// The videocapture.
        /// </summary>
        private VideoCapture _capture;

        /// <summary>
        /// The input mat.
        /// </summary>
        private Mat _inputMat;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        private Mat _rgbMat;

        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// Indicates whether the video is playing.
        /// </summary>
        private bool _isPlaying = false;

        /// <summary>
        /// The requested width.
        /// </summary>
        private int _requestedWidth = 640;

        /// <summary>
        /// The requested height.
        /// </summary>
        private int _requestedHeight = 480;

        /// <summary>
        /// The prop FOURCC.
        /// </summary>
        private FOURCCPreset _propFOURCC = FOURCCPreset.None;

        /// <summary>
        /// The prop ConvertRGB.
        /// </summary>
        private bool _propConvertRGB = true;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

#if (UNITY_IOS && UNITY_2018_1_OR_NEWER) || (UNITY_ANDROID && UNITY_2018_3_OR_NEWER)
        private bool _isUserRequestingPermission;
#endif

        // Unity Lifecycle Methods
        private IEnumerator Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            // Tricks for Adding camera permission settings to AndroidManifest.xml.
            Debug.Log(WebCamTexture.devices.Length);

            // Checks user camera permission state for Android and iOS.
            yield return RequestUserCameraPermission();

            _capture = new VideoCapture();

            // Update GUI state
            RequestedDeviceIdDropdown.value = Array.IndexOf(System.Enum.GetNames(typeof(DeviceIdPreset)), RequestedDeviceId.ToString());
            RequestedResolutionDropdown.value = (int)RequestedResolution;
            RequestedFPSDropdown.value = Array.IndexOf(System.Enum.GetNames(typeof(FPSPreset)), RequestedFPS.ToString());
            RequestedFOURCCDropdown.value = Array.IndexOf(System.Enum.GetNames(typeof(FOURCCPreset)), RequestedFOURCC.ToString());
            RequestedCONVERT_RGBToggle.isOn = true;

            ReleaseButton.interactable = false;

            Dimensions(RequestedResolution, out _requestedWidth, out _requestedHeight);
        }

        private void Update()
        {
            if (_isPlaying)
            {
                if (_capture.grab())
                {
                    _capture.retrieve(_inputMat);

                    CvtColor(_inputMat, _rgbMat, _propFOURCC, _propConvertRGB);

                    OpenCVMatUtils.MatToTexture2D(_rgbMat, _texture);
                }
            }
        }

        private void OnDestroy()
        {
            _capture?.release();

            _inputMat?.Dispose();

            _rgbMat?.Dispose();

            if (_texture != null)
                Texture2D.Destroy(_texture);
        }

#if (UNITY_IOS && UNITY_2018_1_OR_NEWER) || (UNITY_ANDROID && UNITY_2018_3_OR_NEWER)
        private IEnumerator OnApplicationFocus(bool hasFocus)
        {
            yield return null;

            if (_isUserRequestingPermission && hasFocus)
                _isUserRequestingPermission = false;
        }
#endif

        // Public Methods
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
            if (_capture.isOpened())
                return;

            _capture.open((int)RequestedDeviceId);

            if (!_capture.isOpened())
            {
                Debug.LogError("_capture.isOpened() is false. " + "DeviceId:" + (int)RequestedDeviceId);

                if (_fpsMonitor != null)
                    _fpsMonitor.ConsoleText = "_capture.isOpened() is false. " + "DeviceId:" + (int)RequestedDeviceId;

                _capture.release();

                return;
            }

            _capture.set(Videoio.CAP_PROP_FRAME_WIDTH, _requestedWidth);
            _capture.set(Videoio.CAP_PROP_FRAME_HEIGHT, _requestedHeight);
            _capture.set(Videoio.CAP_PROP_FPS, (int)RequestedFPS);

            switch (RequestedFOURCC)
            {
                case FOURCCPreset.None:
                    //
                    break;
                case FOURCCPreset.MJPG:
                    _capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('M', 'J', 'P', 'G'));
                    break;
                case FOURCCPreset.RGB3:
                    _capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('R', 'G', 'B', '3'));
                    break;
                case FOURCCPreset.BGR3:
                    _capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('B', 'G', 'R', '3'));
                    break;
                case FOURCCPreset.GRAY:
                    _capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('G', 'R', 'A', 'Y'));
                    break;
                case FOURCCPreset.NV21:
                    _capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('N', 'V', '2', '1'));
                    break;
                case FOURCCPreset.YV12:
                    _capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('Y', 'V', '1', '2'));
                    break;
                case FOURCCPreset.YUYV:
                    _capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('Y', 'U', 'Y', 'V'));
                    break;
                case FOURCCPreset.YUY2:
                    _capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('Y', 'U', 'Y', '2'));
                    break;
                case FOURCCPreset.NV12:
                    _capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('N', 'V', '1', '2'));
                    break;
                case FOURCCPreset.H264:
                    _capture.set(Videoio.CAP_PROP_FOURCC, VideoWriter.fourcc('H', '2', '6', '4'));
                    break;
                default:
                    //
                    break;
            }

            if (!RequestedCONVERT_RGBToggle.isOn)
            {
                _capture.set(Videoio.CAP_PROP_CONVERT_RGB, 0);
            }
            else
            {
                _capture.set(Videoio.CAP_PROP_CONVERT_RGB, 1);
            }

            Debug.Log("CAP_PROP_FORMAT: " + _capture.get(Videoio.CAP_PROP_FORMAT));
            Debug.Log("CAP_PROP_FPS: " + _capture.get(Videoio.CAP_PROP_FPS));
            Debug.Log("CAP_PROP_FRAME_WIDTH: " + _capture.get(Videoio.CAP_PROP_FRAME_WIDTH));
            Debug.Log("CAP_PROP_FRAME_HEIGHT: " + _capture.get(Videoio.CAP_PROP_FRAME_HEIGHT));
            double ext = _capture.get(Videoio.CAP_PROP_FOURCC);
            Debug.Log("CAP_PROP_FOURCC: " + (char)((int)ext & 0XFF) + (char)(((int)ext & 0XFF00) >> 8) + (char)(((int)ext & 0XFF0000) >> 16) + (char)(((int)ext & 0XFF000000) >> 24));
            Debug.Log("CAP_PROP_CONVERT_RGB: " + _capture.get(Videoio.CAP_PROP_CONVERT_RGB));

            Debug.Log("CAP_PROP_MODE: " + _capture.get(Videoio.CAP_PROP_MODE));
            Debug.Log("CAP_PROP_BRIGHTNESS: " + _capture.get(Videoio.CAP_PROP_BRIGHTNESS));
            Debug.Log("CAP_PROP_CONTRAST: " + _capture.get(Videoio.CAP_PROP_CONTRAST));
            Debug.Log("CAP_PROP_SATURATION: " + _capture.get(Videoio.CAP_PROP_SATURATION));
            Debug.Log("CAP_PROP_HUE: " + _capture.get(Videoio.CAP_PROP_HUE));
            Debug.Log("CAP_PROP_GAIN: " + _capture.get(Videoio.CAP_PROP_GAIN));
            Debug.Log("CAP_PROP_EXPOSURE: " + _capture.get(Videoio.CAP_PROP_EXPOSURE));
            Debug.Log("CAP_PROP_AUTO_EXPOSURE: " + _capture.get(Videoio.CAP_PROP_AUTO_EXPOSURE));
            Debug.Log("CAP_PROP_GAMMA: " + _capture.get(Videoio.CAP_PROP_GAMMA));
            Debug.Log("CAP_PROP_ZOOM: " + _capture.get(Videoio.CAP_PROP_ZOOM));
            Debug.Log("CAP_PROP_FOCUS: " + _capture.get(Videoio.CAP_PROP_FOCUS));
            Debug.Log("CAP_PROP_AUTOFOCUS: " + _capture.get(Videoio.CAP_PROP_AUTOFOCUS));
            Debug.Log("CAP_PROP_BUFFERSIZE: " + _capture.get(Videoio.CAP_PROP_BUFFERSIZE));
            Debug.Log("CAP_PROP_SAR_NUM: " + _capture.get(Videoio.CAP_PROP_SAR_NUM));
            Debug.Log("CAP_PROP_SAR_DEN: " + _capture.get(Videoio.CAP_PROP_SAR_DEN));
            Debug.Log("CAP_PROP_BACKEND: " + _capture.get(Videoio.CAP_PROP_BACKEND));
            Debug.Log("CAP_PROP_AUTO_WB: " + _capture.get(Videoio.CAP_PROP_AUTO_WB));
            Debug.Log("CAP_PROP_WB_TEMPERATURE: " + _capture.get(Videoio.CAP_PROP_WB_TEMPERATURE));
            Debug.Log("CAP_PROP_CODEC_PIXEL_FORMAT: " + _capture.get(Videoio.CAP_PROP_CODEC_PIXEL_FORMAT));

            _inputMat = new Mat();

            int grabFrameCount = 0;
            while (!(_capture.grab() && _capture.retrieve(_inputMat)))
            {
                if (grabFrameCount > 30)
                {
                    Debug.LogError("The grab() and retrieve(() method is not work.");

                    if (_fpsMonitor != null)
                        _fpsMonitor.ConsoleText = "The grab() and retrieve(() method is not work.";

                    _capture.release();

                    break;
                }
                grabFrameCount++;
            }

            if (_inputMat.width() == 0 && _inputMat.height() == 0)
            {
                Debug.LogError("The _inputMat size returned was 0x0.");

                if (_fpsMonitor != null)
                    _fpsMonitor.ConsoleText = "The _inputMat size returned was 0x0.";

                _capture.release();

                return;
            }

#if UNITY_IOS
            int frameWidth = (int)_capture.get(Videoio.CAP_PROP_FRAME_HEIGHT);
            int frameHeight = (int)_capture.get(Videoio.CAP_PROP_FRAME_WIDTH);
#else
            int frameWidth = (int)_capture.get(Videoio.CAP_PROP_FRAME_WIDTH);
            int frameHeight = (int)_capture.get(Videoio.CAP_PROP_FRAME_HEIGHT);
#endif

            _rgbMat = new Mat(frameHeight, frameWidth, CvType.CV_8UC3);
            _texture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGB24, false);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;

            string strFOURCC = "" + (char)((int)ext & 0XFF) + (char)(((int)ext & 0XFF00) >> 8) + (char)(((int)ext & 0XFF0000) >> 16) + (char)(((int)ext & 0XFF000000) >> 24);
            switch (strFOURCC)
            {
                case "MJPG":
                    _propFOURCC = FOURCCPreset.MJPG;
                    break;
                case "RGB3":
                    _propFOURCC = FOURCCPreset.RGB3;
                    break;
                case "BGR3":
                    _propFOURCC = FOURCCPreset.BGR3;
                    break;
                case "GRAY":
                    _propFOURCC = FOURCCPreset.GRAY;
                    break;
                case "NV21":
                    _propFOURCC = FOURCCPreset.NV21;
                    break;
                case "YV12":
                    _propFOURCC = FOURCCPreset.YV12;
                    break;
                case "YUYV":
                    _propFOURCC = FOURCCPreset.YUYV;
                    break;
                case "YUY2":
                    _propFOURCC = FOURCCPreset.YUY2;
                    break;
                case "NV12":
                    _propFOURCC = FOURCCPreset.NV12;
                    break;
                case "H264":
                    _propFOURCC = FOURCCPreset.H264;
                    break;
                default:
                    //_propFOURCC = FOURCCPreset.None;
                    _propFOURCC = RequestedFOURCC;
                    break;
            }

            if (_capture.get(Videoio.CAP_PROP_CONVERT_RGB) == 1)
            {
                _propConvertRGB = true;
            }
            else
            {
                _propConvertRGB = false;
            }

            // The frame size returned by VideoCapture property may differ from the returned Mat.
            // On iOS platform, the height and width of the frame size are swapped.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
                _fpsMonitor.Add("getBackendName", _capture.getBackendName());
                _fpsMonitor.Add("CAP_PROP_FORMAT", _capture.get(Videoio.CAP_PROP_FORMAT).ToString());
                _fpsMonitor.Add("CAP_PROP_FPS", _capture.get(Videoio.CAP_PROP_FPS).ToString());
                _fpsMonitor.Add("CAP_PROP_FRAME_WIDTH", _capture.get(Videoio.CAP_PROP_FRAME_WIDTH).ToString());
                _fpsMonitor.Add("CAP_PROP_FRAME_HEIGHT", _capture.get(Videoio.CAP_PROP_FRAME_HEIGHT).ToString());
                _fpsMonitor.Add("CAP_PROP_FOURCC", "" + (char)((int)ext & 0XFF) + (char)(((int)ext & 0XFF00) >> 8) + (char)(((int)ext & 0XFF0000) >> 16) + (char)(((int)ext & 0XFF000000) >> 24));
                _fpsMonitor.Add("CAP_PROP_CONVERT_RGB", _capture.get(Videoio.CAP_PROP_CONVERT_RGB).ToString());
                _fpsMonitor.Add("_inputMat", "size:" + _inputMat.width() + "x" + _inputMat.height() + " type:" + CvType.typeToString(_inputMat.type()));
            }

            OpenCVDebug.SetDebugMode(true, true);
            try
            {
                CvtColor(_inputMat, _rgbMat, _propFOURCC, _propConvertRGB);
                OpenCVMatUtils.MatToTexture2D(_rgbMat, _texture);

                OpenButton.interactable =
                RequestedDeviceIdDropdown.interactable =
                RequestedResolutionDropdown.interactable =
                RequestedFPSDropdown.interactable =
                RequestedFOURCCDropdown.interactable =
                RequestedCONVERT_RGBToggle.interactable = false;
                ReleaseButton.interactable = true;

                _isPlaying = true;
            }
            catch (Exception e)
            {
                Debug.LogError("CVException: " + e);

                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = "CVException: " + e;
                }

                _capture.release();
            }
            OpenCVDebug.SetDebugMode(false);
        }

        /// <summary>
        /// Raises the release button click event.
        /// </summary>
        public void OnReleaseButtonClick()
        {
            if (!_capture.isOpened())
                return;

            _capture.release();

            _inputMat?.Dispose();

            _rgbMat?.Dispose();

            if (_texture != null)
                Texture2D.Destroy(_texture);

            OpenButton.interactable =
            RequestedDeviceIdDropdown.interactable =
            RequestedResolutionDropdown.interactable =
            RequestedFPSDropdown.interactable =
            RequestedFOURCCDropdown.interactable =
            RequestedCONVERT_RGBToggle.interactable = true;
            ReleaseButton.interactable = false;

            _isPlaying = false;

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }
        }

        /// <summary>
        /// Raises the requested device id dropdown value changed event.
        /// </summary>
        public void OnRequestedDeviceIdDropdownValueChanged(int result)
        {
            string[] enumNames = Enum.GetNames(typeof(DeviceIdPreset));
            byte value = (byte)System.Enum.Parse(typeof(DeviceIdPreset), enumNames[result], true);

            if ((byte)RequestedDeviceId != value)
            {
                RequestedDeviceId = (DeviceIdPreset)value;
            }

            //Debug.Log((int)RequestedDeviceId);
        }

        /// <summary>
        /// Raises the requested resolution dropdown value changed event.
        /// </summary>
        public void OnRequestedResolutionDropdownValueChanged(int result)
        {
            if ((int)RequestedResolution != result)
            {
                RequestedResolution = (ResolutionPreset)result;

                Dimensions(RequestedResolution, out _requestedWidth, out _requestedHeight);
            }

            //Debug.Log((int)_requestedWidth + " " + (int)_requestedHeight);
        }

        /// <summary>
        /// Raises the requested FOURCC dropdown value changed event.
        /// </summary>
        public void OnRequestedFOURCCDropdownValueChanged(int result)
        {
            string[] enumNames = Enum.GetNames(typeof(FOURCCPreset));
            byte value = (byte)System.Enum.Parse(typeof(FOURCCPreset), enumNames[result], true);

            if ((byte)RequestedFOURCC != value)
            {
                RequestedFOURCC = (FOURCCPreset)value;
            }

            //Debug.Log((int)RequestedFOURCC);
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
            }

            //Debug.Log((int)RequestedFPS);
        }

        /// <summary>
        /// Raises the Requested CONVERT_RGB toggle value changed event.
        /// </summary>
        public void OnRequestedCONVERT_RGBToggleValueChanged()
        {
            //Debug.Log(RequestedCONVERT_RGBToggle.isOn);
        }

        // Private Methods
        protected virtual IEnumerator RequestUserCameraPermission()
        {
#if UNITY_IOS && UNITY_2018_1_OR_NEWER
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
                yield break;
            }
#else
            yield return true;
#endif
        }

        protected void CvtColor(Mat src, Mat dst, FOURCCPreset FOURCC, bool convertRGB)
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
