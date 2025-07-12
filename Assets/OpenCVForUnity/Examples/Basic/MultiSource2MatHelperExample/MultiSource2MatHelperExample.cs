using System;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static OpenCVForUnity.UnityIntegration.Helper.Source2Mat.MultiSource2MatHelper;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// MultiSource2MatHelper Example
    ///
    /// An example of image processing by switching between multiple input sources using MultiSource2MatHelper.
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class MultiSource2MatHelperExample : MonoBehaviour
    {
        // Constants
        private enum Source2MatHelperClassNamePreset : int
        {
            WebCamTexture2MatHelper = 0,
            VideoCapture2MatHelper,
            UnityVideoPlayer2MatHelper,
            Image2MatHelper,
            AsyncGPUReadback2MatHelper,
            WebCamTexture2MatAsyncGPUHelper,
        }

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        [Tooltip("Specify the video file path to use exclusively for VideoCapture2MatHelper.")]
        public string requestedVideoFilePathForVideoCapture;

        [Tooltip("Specify the video file path to use exclusively for UnityVideoPlayer2MatHelper.")]
        public string requestedVideoFilePathForUnityVideoPlayer;

        [Space(10)]

        /// <summary>
        /// The change camera botton.
        /// </summary>
        public Button changeCameraBotton;

        /// <summary>
        /// The requested source 2 mat helper class name dropdown.
        /// </summary>
        public Dropdown requestedSource2MatHelperClassNameDropdown;

        /// <summary>
        /// Whether RenderTexture is used when displaying rgbaMat in the scene; if Off, Texture2D is used.
        /// </summary>
        public Toggle outputRenderTextureToggle;

        /// <summary>
        /// The force playback when switching helper toggle.
        /// </summary>
        public Toggle forcePlaybackWhenSwitchingHelperToggle;

        [Space(10)]

        /// <summary>
        /// The cube.
        /// </summary>
        public GameObject cube;

        // Private Fields
        private Source2MatHelperClassNamePreset _requestedSource2MatHelperClassName = Source2MatHelperClassNamePreset.WebCamTexture2MatHelper;

        /// <summary>
        /// The output Texture2D.
        /// </summary>
        private Texture2D _outputTexture2D;

        /// <summary>
        /// The output RenderTexture.
        /// </summary>
        private RenderTexture _outputRenderTexture;

        /// <summary>
        /// The graphicsBuffer for OpenCVUtils.MatToRenderTexture().
        /// </summary>
        private GraphicsBuffer _graphicsBuffer;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// The FPS counter.
        /// Measure how frequently DidUpdateThisFrame() is actually updated.
        /// </summary>
        private FPSCounter _fpsCounter;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            // Get the MultiSource2MatHelper component attached to the current game object
            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();

            // Set the requested ColorFormat
            _multiSource2MatHelper.RequestedSource2MatHelperClassName = MultiSource2MatHelperClassName.WebCamTexture2MatHelper;
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;

            // Initialize the source to Mat helper.
            _multiSource2MatHelper.Initialize();

            // Update GUI state
            changeCameraBotton.interactable = _multiSource2MatHelper.Source2MatHelper is ICameraSource2MatHelper;
            requestedSource2MatHelperClassNameDropdown.value = (int)_multiSource2MatHelper.GetCurrentSource2MatHelperClassName();

            if (!SystemInfo.supportsComputeShaders)
            {
                outputRenderTextureToggle.interactable = false;
            }
        }

        // Public Methods
        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("deviceName", _multiSource2MatHelper.GetDeviceName().ToString());
                _fpsMonitor.Add("width", _multiSource2MatHelper.GetWidth().ToString());
                _fpsMonitor.Add("height", _multiSource2MatHelper.GetHeight().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
                _fpsMonitor.Add("helperClassName", _multiSource2MatHelper.GetCurrentSource2MatHelperClassName().ToString());

                switch (_multiSource2MatHelper.Source2MatHelper)
                {
                    case ICameraSource2MatHelper helper:
                        _fpsMonitor.Add("camera fps", helper.GetFPS().ToString());
                        _fpsMonitor.Add("isFrontFacing", helper.IsFrontFacing().ToString());
                        break;
                    case IVideoSource2MatHelper helper:
                        _fpsMonitor.Add("video path", helper.RequestedVideoFilePath.ToString());
                        _fpsMonitor.Add("video fps", helper.GetFPS().ToString());
                        _fpsMonitor.Add("loop", helper.Loop.ToString());
                        break;
                    case IImageSource2MatHelper helper:
                        _fpsMonitor.Add("image path", helper.RequestedImageFilePath.ToString());
                        _fpsMonitor.Add("repeat", helper.Repeat.ToString());
                        break;
                    case ITextureSource2MatHelper helper:
                        _fpsMonitor.Add("source texture", helper.SourceTexture.ToString());
                        break;
                }

                switch (_multiSource2MatHelper.Source2MatHelper)
                {
                    case IMatUpdateFPSProvider helper:
                        _fpsMonitor.Add("mat update fps", helper.GetMatUpdateFPS().ToString());
                        break;
                }

#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API
                if (_multiSource2MatHelper.Source2MatHelper is WebCamTexture2MatHelper webCamHelper)
                {
                    _fpsMonitor.Add("rotate90Degree", webCamHelper.Rotate90Degree.ToString());
                    _fpsMonitor.Add("flipVertical", webCamHelper.FlipVertical.ToString());
                    _fpsMonitor.Add("flipHorizontal", webCamHelper.FlipHorizontal.ToString());
                }
#endif
            }

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            _fpsCounter = new FPSCounter(1.0f);

            // To ensure that outputTexture does not blink when Source is changed, outputTexture is not destroyed by OnSourceToMatHelperDisposed, but is retained until OnSourceToMatHelperInitialised.
            ReleaseResources();

            // Retrieve the current frame from the Source2MatHelper as a Mat object
            Mat rgbaMat = _multiSource2MatHelper.GetMat();

            if (!outputRenderTextureToggle.isOn)
            {
                // Create a new Texture2D with the same dimensions as the Mat and RGBA32 color format
                _outputTexture2D = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);

                // Convert the Mat to a Texture2D, effectively transferring the image data
                OpenCVMatUtils.MatToTexture2D(rgbaMat, _outputTexture2D);

                // Set the Texture2D as the texture of the RawImage for preview.
                resultPreview.texture = _outputTexture2D;
                resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_outputTexture2D.width / _outputTexture2D.height;
            }
            else
            {
                _graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)rgbaMat.total(), (int)rgbaMat.elemSize());

                _outputRenderTexture = new RenderTexture(rgbaMat.width(), rgbaMat.height(), 0);
                _outputRenderTexture.enableRandomWrite = true;
                _outputRenderTexture.Create();

                try
                {
                    // Convert the Mat to a RenderTexture, effectively transferring the image data
                    OpenCVMatUtils.MatToRenderTexture(rgbaMat, _outputRenderTexture, _graphicsBuffer);
                }
                catch (Exception ex)
                {
                    if (_fpsMonitor != null)
                        _fpsMonitor.ConsoleText = ex.Message;
                }

                // Set the RenderTexture as the texture of the RawImage for preview.
                resultPreview.texture = _outputRenderTexture;
                resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_outputRenderTexture.width / _outputRenderTexture.height;
            }
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _fpsMonitor?.Clear();
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message);

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
            _multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            _multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            _multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            _multiSource2MatHelper.RequestedIsFrontFacing = !_multiSource2MatHelper.RequestedIsFrontFacing;
        }

        /// <summary>
        /// Raises the requested source 2 mat helper class name dropdown value changed event.
        /// </summary>
        public void OnRequestedSource2MatHelperClassNameDropdownValueChanged(int result)
        {
            if ((int)_requestedSource2MatHelperClassName != result)
            {
                if (forcePlaybackWhenSwitchingHelperToggle.isOn && !_multiSource2MatHelper.IsPlaying())
                    _multiSource2MatHelper.Play();

                _requestedSource2MatHelperClassName = (Source2MatHelperClassNamePreset)result;

                switch (_requestedSource2MatHelperClassName)
                {
                    case Source2MatHelperClassNamePreset.WebCamTexture2MatHelper:
                        _multiSource2MatHelper.RequestedSource2MatHelperClassName = MultiSource2MatHelperClassName.WebCamTexture2MatHelper;
                        break;
                    case Source2MatHelperClassNamePreset.VideoCapture2MatHelper:
                        _multiSource2MatHelper.RequestedSource2MatHelperClassName = MultiSource2MatHelperClassName.VideoCapture2MatHelper;

                        if (!string.IsNullOrEmpty(requestedVideoFilePathForVideoCapture))
                            _multiSource2MatHelper.RequestedVideoFilePath = requestedVideoFilePathForVideoCapture;

                        break;
                    case Source2MatHelperClassNamePreset.UnityVideoPlayer2MatHelper:
                        _multiSource2MatHelper.RequestedSource2MatHelperClassName = MultiSource2MatHelperClassName.UnityVideoPlayer2MatHelper;

                        if (!string.IsNullOrEmpty(requestedVideoFilePathForUnityVideoPlayer))
                            _multiSource2MatHelper.RequestedVideoFilePath = requestedVideoFilePathForUnityVideoPlayer;

                        break;
                    case Source2MatHelperClassNamePreset.Image2MatHelper:
                        _multiSource2MatHelper.RequestedSource2MatHelperClassName = MultiSource2MatHelperClassName.Image2MatHelper;
                        break;
                    case Source2MatHelperClassNamePreset.AsyncGPUReadback2MatHelper:
                        _multiSource2MatHelper.RequestedSource2MatHelperClassName = MultiSource2MatHelperClassName.AsyncGPUReadback2MatHelper;
                        break;
                    case Source2MatHelperClassNamePreset.WebCamTexture2MatAsyncGPUHelper:
                        _multiSource2MatHelper.RequestedSource2MatHelperClassName = MultiSource2MatHelperClassName.WebCamTexture2MatAsyncGPUHelper;
                        break;
                }

                // Way to perform different processing depending on the interface inherited by the helper class.
                changeCameraBotton.interactable = _multiSource2MatHelper.Source2MatHelper is ICameraSource2MatHelper;
            }
        }

        /// <summary>
        /// Raises the output RenderTexture toggle value changed event.
        /// </summary>
        public void OnOutputRenderTextureToggleValueChanged()
        {
            if (_multiSource2MatHelper.IsInitialized())
            {
                _multiSource2MatHelper.Initialize();
            }
        }

        // Private Methods
        private void Update()
        {
            // Check if the web camera is playing and if a new frame was updated
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {
                _fpsCounter.MeasureFPS();

                // Retrieve the current frame as a Mat object
                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                switch (_requestedSource2MatHelperClassName)
                {
                    case Source2MatHelperClassNamePreset.WebCamTexture2MatHelper:
                        Imgproc.putText(rgbaMat, "WebCamTexture => Mat", new Point(5, 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        break;
                    case Source2MatHelperClassNamePreset.VideoCapture2MatHelper:
                    case Source2MatHelperClassNamePreset.UnityVideoPlayer2MatHelper:
                        Imgproc.putText(rgbaMat, "Video File => Mat", new Point(5, 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        break;
                    case Source2MatHelperClassNamePreset.Image2MatHelper:
                        Imgproc.putText(rgbaMat, "Image File => Mat", new Point(5, 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        break;
                    case Source2MatHelperClassNamePreset.AsyncGPUReadback2MatHelper:
                        Imgproc.putText(rgbaMat, "Camera => RenderTexture => Mat", new Point(5, 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7,
                            new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        break;
                    case Source2MatHelperClassNamePreset.WebCamTexture2MatAsyncGPUHelper:
                        Imgproc.putText(rgbaMat, "WebCamTexture -> RenderTexture => Mat", new Point(5, 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        break;
                }

                // Add text overlay on the frame
                Imgproc.putText(rgbaMat, "W:" + rgbaMat.width() + " H:" + rgbaMat.height() + " SO:" + Screen.orientation + " MatUpdateFPS:" + _fpsCounter.GetCurrentFPS(), new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                if (!outputRenderTextureToggle.isOn)
                {
                    // Convert the Mat to a Texture2D to display it on a texture
                    OpenCVMatUtils.MatToTexture2D(rgbaMat, _outputTexture2D);
                }
                else
                {
                    // Convert the Mat to a RenderTexture to display it on a texture
                    OpenCVMatUtils.MatToRenderTexture(rgbaMat, _outputRenderTexture, _graphicsBuffer);
                }

                //cube.transform.Rotate(new Vector3(90, 90, 0) * Time.deltaTime, Space.Self);
            }

            cube.transform.Rotate(new Vector3(90, 90, 0) * Time.deltaTime * 0.5f, Space.Self);
        }

        private void OnDestroy()
        {
            // Dispose of the SourceToMatHelper object and release any resources held by it.
            _multiSource2MatHelper?.Dispose();

            ReleaseResources();
        }

        /// <summary>
        /// To release the resources.
        /// </summary>
        private void ReleaseResources()
        {
            // Destroy the texture and set it to null
            if (_outputTexture2D != null) Texture2D.Destroy(_outputTexture2D); _outputTexture2D = null;

            // Destroy the texture and set it to null
            if (_outputRenderTexture != null)
            {
                RenderTexture.Destroy(_outputRenderTexture);
                _outputRenderTexture = null;
            }

            if (_graphicsBuffer != null)
            {
                _graphicsBuffer.Dispose();
                _graphicsBuffer = null;
            }
        }
    }
}
