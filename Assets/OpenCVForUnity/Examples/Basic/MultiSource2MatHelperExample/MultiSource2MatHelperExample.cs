using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static OpenCVForUnity.UnityUtils.Helper.MultiSource2MatHelper;

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
        enum Source2MatHelperClassNamePreset : int
        {
            WebCamTexture2MatHelper = 0,
            VideoCapture2MatHelper,
            UnityVideoPlayer2MatHelper,
            Image2MatHelper,
            AsyncGPUReadback2MatHelper,
            WebCamTexture2MatAsyncGPUHelper,
        }

        Source2MatHelperClassNamePreset requestedSource2MatHelperClassName = Source2MatHelperClassNamePreset.WebCamTexture2MatHelper;


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

        /// <summary>
        /// The output Texture2D.
        /// </summary>
        Texture2D outputTexture2D;

        /// <summary>
        /// The output RenderTexture.
        /// </summary>
        RenderTexture outputRenderTexture;

        /// <summary>
        /// The graphicsBuffer for Utils.matToRenderTexture().
        /// </summary>
        GraphicsBuffer graphicsBuffer;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        MultiSource2MatHelper multiSource2MatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            // Get the MultiSource2MatHelper component attached to the current game object
            multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();

            // Set the requested ColorFormat
            multiSource2MatHelper.requestedSource2MatHelperClassName = MultiSource2MatHelperClassName.WebCamTexture2MatHelper;
            multiSource2MatHelper.outputColorFormat = Source2MatHelperColorFormat.RGBA;

            // Initialize the source to Mat helper.
            multiSource2MatHelper.Initialize();

            // Update GUI state
            changeCameraBotton.interactable = multiSource2MatHelper.source2MatHelper is ICameraSource2MatHelper;
            requestedSource2MatHelperClassNameDropdown.value = (int)multiSource2MatHelper.GetCurrentSource2MatHelperClassName();

            if (!SystemInfo.supportsComputeShaders)
            {
                outputRenderTextureToggle.interactable = false;
            }
        }

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("deviceName", multiSource2MatHelper.GetDeviceName().ToString());
                fpsMonitor.Add("width", multiSource2MatHelper.GetWidth().ToString());
                fpsMonitor.Add("height", multiSource2MatHelper.GetHeight().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
                fpsMonitor.Add("helperClassName", multiSource2MatHelper.GetCurrentSource2MatHelperClassName().ToString());

                switch (multiSource2MatHelper.source2MatHelper)
                {
                    case ICameraSource2MatHelper helper:
                        fpsMonitor.Add("camera fps", helper.GetFPS().ToString());
                        fpsMonitor.Add("isFrontFacing", helper.IsFrontFacing().ToString());
                        break;
                    case IVideoSource2MatHelper helper:
                        fpsMonitor.Add("video path", helper.requestedVideoFilePath.ToString());
                        fpsMonitor.Add("video fps", helper.GetFPS().ToString());
                        fpsMonitor.Add("loop", helper.loop.ToString());
                        break;
                    case IImageSource2MatHelper helper:
                        fpsMonitor.Add("image path", helper.requestedImageFilePath.ToString());
                        fpsMonitor.Add("repeat", helper.repeat.ToString());
                        break;
                    case ITextureSource2MatHelper helper:
                        fpsMonitor.Add("source texture", helper.sourceTexture.ToString());
                        break;
                }

#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API
                if (multiSource2MatHelper.source2MatHelper is WebCamTexture2MatHelper webCamHelper)
                {
                    fpsMonitor.Add("rotate90Degree", webCamHelper.rotate90Degree.ToString());
                    fpsMonitor.Add("flipVertical", webCamHelper.flipVertical.ToString());
                    fpsMonitor.Add("flipHorizontal", webCamHelper.flipHorizontal.ToString());
                }
#endif
            }

            if (fpsMonitor != null)
                fpsMonitor.consoleText = "";

            // To ensure that outputTexture does not blink when Source is changed, outputTexture is not destroyed by OnSourceToMatHelperDisposed, but is retained until OnSourceToMatHelperInitialised.
            ReleaseResources();

            // Retrieve the current frame from the Source2MatHelper as a Mat object
            Mat rgbaMat = multiSource2MatHelper.GetMat();

            if (!outputRenderTextureToggle.isOn)
            {
                // Create a new Texture2D with the same dimensions as the Mat and RGBA32 color format
                outputTexture2D = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);

                // Convert the Mat to a Texture2D, effectively transferring the image data
                Utils.matToTexture2D(rgbaMat, outputTexture2D);

                // Set the Texture2D as the texture of the RawImage for preview.
                resultPreview.texture = outputTexture2D;
                resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)outputTexture2D.width / outputTexture2D.height;
            }
            else
            {
                graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)rgbaMat.total(), (int)rgbaMat.elemSize());

                outputRenderTexture = new RenderTexture(rgbaMat.width(), rgbaMat.height(), 0);
                outputRenderTexture.enableRandomWrite = true;
                outputRenderTexture.Create();

                try
                {
                    // Convert the Mat to a RenderTexture, effectively transferring the image data
                    Utils.matToRenderTexture(rgbaMat, outputRenderTexture, graphicsBuffer);
                }
                catch (Exception ex)
                {
                    if (fpsMonitor != null)
                        fpsMonitor.consoleText = ex.Message;
                }

                // Set the RenderTexture as the texture of the RawImage for preview.
                resultPreview.texture = outputRenderTexture;
                resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)outputRenderTexture.width / outputRenderTexture.height;


            }


            if (forcePlaybackWhenSwitchingHelperToggle.isOn && !multiSource2MatHelper.IsPlaying())
                multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (fpsMonitor != null)
                fpsMonitor.Clear();
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        // Update is called once per frame
        void Update()
        {
            // Check if the web camera is playing and if a new frame was updated
            if (multiSource2MatHelper.IsPlaying() && multiSource2MatHelper.DidUpdateThisFrame())
            {
                // Retrieve the current frame as a Mat object
                Mat rgbaMat = multiSource2MatHelper.GetMat();

                switch (requestedSource2MatHelperClassName)
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
                Imgproc.putText(rgbaMat, "W:" + rgbaMat.width() + " H:" + rgbaMat.height() + " SO:" + Screen.orientation, new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                if (!outputRenderTextureToggle.isOn)
                {
                    // Convert the Mat to a Texture2D to display it on a texture
                    Utils.matToTexture2D(rgbaMat, outputTexture2D);
                }
                else
                {
                    // Convert the Mat to a RenderTexture to display it on a texture
                    Utils.matToRenderTexture(rgbaMat, outputRenderTexture, graphicsBuffer);
                }
            }

            cube.transform.Rotate(new Vector3(90, 90, 0) * Time.deltaTime, Space.Self);
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            // Dispose of the SourceToMatHelper object and release any resources held by it.
            multiSource2MatHelper.Dispose();

            ReleaseResources();
        }

        /// <summary>
        /// To release the resources.
        /// </summary>
        void ReleaseResources()
        {
            // Destroy the texture and set it to null
            if (outputTexture2D != null)
            {
                Texture2D.Destroy(outputTexture2D);
                outputTexture2D = null;
            }

            // Destroy the texture and set it to null
            if (outputRenderTexture != null)
            {
                RenderTexture.Destroy(outputRenderTexture);
                outputRenderTexture = null;
            }

            if (graphicsBuffer != null)
            {
                graphicsBuffer.Dispose();
                graphicsBuffer = null;
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
            multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            multiSource2MatHelper.requestedIsFrontFacing = !multiSource2MatHelper.requestedIsFrontFacing;
        }

        /// <summary>
        /// Raises the requested source 2 mat helper class name dropdown value changed event.
        /// </summary>
        public void OnRequestedSource2MatHelperClassNameDropdownValueChanged(int result)
        {
            if ((int)requestedSource2MatHelperClassName != result)
            {
                requestedSource2MatHelperClassName = (Source2MatHelperClassNamePreset)result;

                switch (requestedSource2MatHelperClassName)
                {
                    case Source2MatHelperClassNamePreset.WebCamTexture2MatHelper:
                        multiSource2MatHelper.requestedSource2MatHelperClassName = MultiSource2MatHelperClassName.WebCamTexture2MatHelper;
                        break;
                    case Source2MatHelperClassNamePreset.VideoCapture2MatHelper:
                        multiSource2MatHelper.requestedSource2MatHelperClassName = MultiSource2MatHelperClassName.VideoCapture2MatHelper;

                        if (!string.IsNullOrEmpty(requestedVideoFilePathForVideoCapture))
                            multiSource2MatHelper.requestedVideoFilePath = requestedVideoFilePathForVideoCapture;

                        break;
                    case Source2MatHelperClassNamePreset.UnityVideoPlayer2MatHelper:
                        multiSource2MatHelper.requestedSource2MatHelperClassName = MultiSource2MatHelperClassName.UnityVideoPlayer2MatHelper;

                        if (!string.IsNullOrEmpty(requestedVideoFilePathForUnityVideoPlayer))
                            multiSource2MatHelper.requestedVideoFilePath = requestedVideoFilePathForUnityVideoPlayer;

                        break;
                    case Source2MatHelperClassNamePreset.Image2MatHelper:
                        multiSource2MatHelper.requestedSource2MatHelperClassName = MultiSource2MatHelperClassName.Image2MatHelper;
                        break;
                    case Source2MatHelperClassNamePreset.AsyncGPUReadback2MatHelper:
                        multiSource2MatHelper.requestedSource2MatHelperClassName = MultiSource2MatHelperClassName.AsyncGPUReadback2MatHelper;
                        break;
                    case Source2MatHelperClassNamePreset.WebCamTexture2MatAsyncGPUHelper:
                        multiSource2MatHelper.requestedSource2MatHelperClassName = MultiSource2MatHelperClassName.WebCamTexture2MatAsyncGPUHelper;
                        break;
                }

                // Way to perform different processing depending on the interface inherited by the helper class.
                changeCameraBotton.interactable = multiSource2MatHelper.source2MatHelper is ICameraSource2MatHelper;
            }
        }

        /// <summary>
        /// Raises the output RenderTexture toggle value changed event.
        /// </summary>
        public void OnOutputRenderTextureToggleValueChanged()
        {
            if (multiSource2MatHelper.IsInitialized())
            {
                multiSource2MatHelper.Initialize();
            }
        }
    }
}