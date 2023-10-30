using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Image Correction Example
    /// An example of image correction including contrast, brightness, gamma, and thresholding.
    /// Referring to https://docs.opencv.org/3.4/d3/dc1/tutorial_basic_linear_transform.html.
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class ImageCorrectionExample : MonoBehaviour
    {
        private float contrast = 1f;
        private float brightness = 0f;
        private float gamma = 1f;
        private bool thresholdEnabled = false;
        private float threshold = 127f;

        private Mat lut;

        /// <summary>
        /// The gray1 mat.
        /// </summary>
        Mat grayMat;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        // Use this for initialization
        void Start()
        {
            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();
            webCamTextureToMatHelper.outputColorFormat = WebCamTextureToMatHelper.ColorFormat.RGB;
            webCamTextureToMatHelper.Initialize();

            // Create the initial LUT
            CreateLUT();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGB24, false);
            Utils.matToTexture2D(webCamTextureMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }

            grayMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC1);
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (grayMat != null)
            {
                grayMat.Dispose();
                grayMat = null;
            }

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
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update()
        {
            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {

                Mat rgbMat = webCamTextureToMatHelper.GetMat();

                // Adjust brightness and contrast
                Core.convertScaleAbs(rgbMat, rgbMat, contrast, brightness);

                // Adjust gamma value if it has changed
                if (lut == null || Mathf.Abs(gamma - (float)lut.get(0, 0)[0]) > float.Epsilon)
                {
                    gamma = Mathf.Max(gamma, 0.01f); // Ensure gamma is non-zero
                    CreateLUT();
                }

                // Apply gamma correction using the LUT
                Core.LUT(rgbMat, lut, rgbMat);

                // Apply threshold
                if (thresholdEnabled)
                {
                    // Convert the image to grayscale
                    Imgproc.cvtColor(rgbMat, grayMat, Imgproc.COLOR_RGB2GRAY);

                    // Apply thresholding
                    Imgproc.threshold(grayMat, grayMat, threshold, 255, Imgproc.THRESH_BINARY);

                    // Convert the image to RGB
                    Imgproc.cvtColor(grayMat, rgbMat, Imgproc.COLOR_GRAY2RGB);
                }

                Imgproc.putText(rgbMat, "contrast:" + contrast.ToString("F2") + " brightness:" + brightness.ToString("F2") + " gamma:" + gamma.ToString("F2") + " threshold:" + threshold.ToString("F2"), new Point(5, rgbMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.6, new Scalar(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);

                Utils.matToTexture2D(rgbMat, texture);
            }
        }

        private void CreateLUT()
        {
            lut = new Mat(1, 256, CvType.CV_8UC1);

            for (int i = 0; i < 256; i++)
            {
                double gammaCorrection = Mathf.Pow((float)i / 255f, 1f / gamma) * 255f;
                lut.put(0, i, gammaCorrection);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            webCamTextureToMatHelper.Dispose();
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
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.requestedIsFrontFacing;
        }

        public void OnContrastSliderValueChanged(float value)
        {
            contrast = value;
        }

        public void OnBrightnessSliderValueChanged(float value)
        {
            brightness = value;
        }

        public void OnGammaSliderValueChanged(float value)
        {
            gamma = value;
        }

        public void OnApplayThresholdToggleValueChanged(bool value)
        {
            thresholdEnabled = value;
        }

        public void OnThresholdSliderValueChanged(float value)
        {
            threshold = value;
        }
    }
}