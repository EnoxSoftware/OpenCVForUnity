using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Image Correction Example
    /// An example of image correction including contrast, brightness, gamma, and thresholding.
    /// Referring to https://docs.opencv.org/3.4/d3/dc1/tutorial_basic_linear_transform.html.
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class ImageCorrectionExample : MonoBehaviour
    {
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

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
        /// The multi source to mat helper.
        /// </summary>
        MultiSource2MatHelper multiSource2MatHelper;

        // Use this for initialization
        void Start()
        {
            multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            multiSource2MatHelper.outputColorFormat = Source2MatHelperColorFormat.RGB;
            multiSource2MatHelper.Initialize();

            // Create the initial LUT
            CreateLUT();
        }

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbMat = multiSource2MatHelper.GetMat();

            // Fill in the image so that the unprocessed image is not displayed.
            rgbMat.setTo(new Scalar(0, 0, 0, 255));

            texture = new Texture2D(rgbMat.cols(), rgbMat.rows(), TextureFormat.RGB24, false);
            Utils.matToTexture2D(rgbMat, texture);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            grayMat = new Mat(rgbMat.rows(), rgbMat.cols(), CvType.CV_8UC1);
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

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
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message);
        }

        // Update is called once per frame
        void Update()
        {
            if (multiSource2MatHelper.IsPlaying() && multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbMat = multiSource2MatHelper.GetMat();

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
            multiSource2MatHelper.Dispose();
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