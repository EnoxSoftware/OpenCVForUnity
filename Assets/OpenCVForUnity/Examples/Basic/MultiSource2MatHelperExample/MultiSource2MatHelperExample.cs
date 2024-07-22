using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
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
            Image2MatHelper,
            AsyncGPUReadback2MatHelper,
        }

        Source2MatHelperClassNamePreset requestedSource2MatHelperClassName = Source2MatHelperClassNamePreset.WebCamTexture2MatHelper;

        /// <summary>
        /// The change camera botton.
        /// </summary>
        public Button changeCameraBotton;

        /// <summary>
        /// The requested source 2 mat helper class name dropdown.
        /// </summary>
        public Dropdown requestedSource2MatHelperClassNameDropdown;

        //
        /// <summary>
        /// The cube.
        /// </summary>
        [Space(10)]
        public GameObject cube;
        //

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        MultiSource2MatHelper multiSourceToMatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            // Get the MultiSource2MatHelper component attached to the current game object
            multiSourceToMatHelper = gameObject.GetComponent<MultiSource2MatHelper>();

            // Set the requested ColorFormat
            multiSourceToMatHelper.requestedSource2MatHelperClassName = MultiSource2MatHelperClassName.WebCamTexture2MatHelper;
            multiSourceToMatHelper.outputColorFormat = Source2MatHelperColorFormat.RGBA;

            // Initialize the source to Mat helper.
            multiSourceToMatHelper.Initialize();

            // Update GUI state
            changeCameraBotton.interactable = multiSourceToMatHelper.source2MatHelper is ICameraSource2MatHelper;
            requestedSource2MatHelperClassNameDropdown.value = (int)multiSourceToMatHelper.GetCurrentSource2MatHelperClassName();
        }

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            // Retrieve the current frame from the Source2MatHelper as a Mat object
            Mat rgbaMat = multiSourceToMatHelper.GetMat();

            // Create a new Texture2D with the same dimensions as the Mat and RGBA32 color format
            texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);

            // Convert the Mat to a Texture2D, effectively transferring the image data
            Utils.matToTexture2D(rgbaMat, texture);

            // Set the Texture2D as the main texture of the Renderer component attached to the game object
            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            // Adjust the scale of the game object to match the dimensions of the texture
            gameObject.transform.localScale = new Vector3(rgbaMat.cols(), rgbaMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


            if (fpsMonitor != null)
            {
                fpsMonitor.Add("deviceName", multiSourceToMatHelper.GetDeviceName().ToString());
                fpsMonitor.Add("width", multiSourceToMatHelper.GetWidth().ToString());
                fpsMonitor.Add("height", multiSourceToMatHelper.GetHeight().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
                fpsMonitor.Add("helperClassName", multiSourceToMatHelper.GetCurrentSource2MatHelperClassName().ToString());

                switch (multiSourceToMatHelper.source2MatHelper)
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

                if (multiSourceToMatHelper.source2MatHelper is WebCamTexture2MatHelper webCamHelper)
                {
                    fpsMonitor.Add("rotate90Degree", webCamHelper.rotate90Degree.ToString());
                    fpsMonitor.Add("flipVertical", webCamHelper.flipVertical.ToString());
                    fpsMonitor.Add("flipHorizontal", webCamHelper.flipHorizontal.ToString());
                }
            }


            // Get the width and height of the webCamTextureMat
            float width = rgbaMat.width();
            float height = rgbaMat.height();

            // Calculate the scale factors for width and height based on the screen dimensions
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;

            // Adjust the orthographic size of the main Camera to fit the aspect ratio of the image
            if (widthScale < heightScale)
            {
                // If the width scale is smaller, adjust the orthographic size based on width and screen height
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                // If the height scale is smaller or equal, adjust the orthographic size based on height
                Camera.main.orthographicSize = height / 2;
            }
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            // Destroy the texture and set it to null
            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

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
            if (multiSourceToMatHelper.IsPlaying() && multiSourceToMatHelper.DidUpdateThisFrame())
            {
                // Retrieve the current frame as a Mat object
                Mat rgbaMat = multiSourceToMatHelper.GetMat();

                switch (requestedSource2MatHelperClassName)
                {
                    case Source2MatHelperClassNamePreset.WebCamTexture2MatHelper:
                        Imgproc.putText(rgbaMat, "WebCamTexture => Mat", new Point(5, 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        break;
                    case Source2MatHelperClassNamePreset.VideoCapture2MatHelper:
                        Imgproc.putText(rgbaMat, "Video File => Mat", new Point(5, 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        break;
                    case Source2MatHelperClassNamePreset.Image2MatHelper:
                        Imgproc.putText(rgbaMat, "Image File => Mat", new Point(5, 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        break;
                    case Source2MatHelperClassNamePreset.AsyncGPUReadback2MatHelper:
                        Imgproc.putText(rgbaMat, "Camera => RenderTexture => Mat", new Point(5, 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        break;
                }

                // Add text overlay on the frame
                Imgproc.putText(rgbaMat, "W:" + rgbaMat.width() + " H:" + rgbaMat.height() + " SO:" + Screen.orientation, new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                // Convert the Mat to a Texture2D to display it on a texture
                Utils.matToTexture2D(rgbaMat, texture);
            }

            //
            cube.transform.Rotate(new Vector3(90, 90, 0) * Time.deltaTime, Space.Self);
            //
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            // Dispose of the SourceToMatHelper object and release any resources held by it.
            multiSourceToMatHelper.Dispose();
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
            multiSourceToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            multiSourceToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            multiSourceToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            multiSourceToMatHelper.requestedIsFrontFacing = !multiSourceToMatHelper.requestedIsFrontFacing;
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
                        multiSourceToMatHelper.requestedSource2MatHelperClassName = MultiSource2MatHelperClassName.WebCamTexture2MatHelper;
                        break;
                    case Source2MatHelperClassNamePreset.VideoCapture2MatHelper:
                        multiSourceToMatHelper.requestedSource2MatHelperClassName = MultiSource2MatHelperClassName.VideoCapture2MatHelper;
                        break;
                    case Source2MatHelperClassNamePreset.Image2MatHelper:
                        multiSourceToMatHelper.requestedSource2MatHelperClassName = MultiSource2MatHelperClassName.Image2MatHelper;
                        break;
                    case Source2MatHelperClassNamePreset.AsyncGPUReadback2MatHelper:
                        multiSourceToMatHelper.requestedSource2MatHelperClassName = MultiSource2MatHelperClassName.AsyncGPUReadback2MatHelper;
                        break;
                }

                // Way to perform different processing depending on the interface inherited by the helper class.
                changeCameraBotton.interactable = multiSourceToMatHelper.source2MatHelper is ICameraSource2MatHelper;
            }
        }
    }
}