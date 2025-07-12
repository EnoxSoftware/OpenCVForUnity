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
    /// Image Correction Example
    /// An example of image correction including contrast, brightness, gamma, and thresholding.
    /// Referring to https://docs.opencv.org/3.4/d3/dc1/tutorial_basic_linear_transform.html.
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class ImageCorrectionExample : MonoBehaviour
    {
        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        // Private Fields
        private float _contrast = 1f;
        private float _brightness = 0f;
        private float _gamma = 1f;
        private bool _thresholdEnabled = false;
        private float _threshold = 127f;

        private Mat _lut;

        /// <summary>
        /// The gray1 mat.
        /// </summary>
        private Mat _grayMat;

        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        // Unity Lifecycle Methods
        private void Start()
        {
            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGB;
            _multiSource2MatHelper.Initialize();

            // Create the initial LUT
            CreateLUT();
        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {
                Mat rgbMat = _multiSource2MatHelper.GetMat();

                // Adjust brightness and contrast
                Core.convertScaleAbs(rgbMat, rgbMat, _contrast, _brightness);

                // Adjust gamma value if it has changed
                if (_lut == null || Mathf.Abs(_gamma - (float)_lut.get(0, 0)[0]) > float.Epsilon)
                {
                    _gamma = Mathf.Max(_gamma, 0.01f); // Ensure gamma is non-zero
                    CreateLUT();
                }

                // Apply gamma correction using the LUT
                Core.LUT(rgbMat, _lut, rgbMat);

                // Apply threshold
                if (_thresholdEnabled)
                {
                    // Convert the image to grayscale
                    Imgproc.cvtColor(rgbMat, _grayMat, Imgproc.COLOR_RGB2GRAY);

                    // Apply thresholding
                    Imgproc.threshold(_grayMat, _grayMat, _threshold, 255, Imgproc.THRESH_BINARY);

                    // Convert the image to RGB
                    Imgproc.cvtColor(_grayMat, rgbMat, Imgproc.COLOR_GRAY2RGB);
                }

                Imgproc.putText(rgbMat, "contrast:" + _contrast.ToString("F2") + " brightness:" + _brightness.ToString("F2") + " gamma:" + _gamma.ToString("F2") + " threshold:" + _threshold.ToString("F2"), new Point(5, rgbMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.6, new Scalar(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);

                OpenCVMatUtils.MatToTexture2D(rgbMat, _texture);
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbMat = _multiSource2MatHelper.GetMat();

            // Fill in the image so that the unprocessed image is not displayed.
            rgbMat.setTo(new Scalar(0, 0, 0, 255));

            _texture = new Texture2D(rgbMat.cols(), rgbMat.rows(), TextureFormat.RGB24, false);
            OpenCVMatUtils.MatToTexture2D(rgbMat, _texture);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;

            _grayMat = new Mat(rgbMat.rows(), rgbMat.cols(), CvType.CV_8UC1);
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _grayMat?.Dispose(); _grayMat = null;
            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
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
        /// Raises the contrast slider value changed event.
        /// </summary>
        /// <param name="value">Slider value.</param>
        public void OnContrastSliderValueChanged(float value)
        {
            _contrast = value;
        }

        /// <summary>
        /// Raises the brightness slider value changed event.
        /// </summary>
        /// <param name="value">Slider value.</param>
        public void OnBrightnessSliderValueChanged(float value)
        {
            _brightness = value;
        }

        /// <summary>
        /// Raises the gamma slider value changed event.
        /// </summary>
        /// <param name="value">Slider value.</param>
        public void OnGammaSliderValueChanged(float value)
        {
            _gamma = value;
        }

        /// <summary>
        /// Raises the apply threshold toggle value changed event.
        /// </summary>
        /// <param name="value">Toggle value.</param>
        public void OnApplayThresholdToggleValueChanged(bool value)
        {
            _thresholdEnabled = value;
        }

        /// <summary>
        /// Raises the threshold slider value changed event.
        /// </summary>
        /// <param name="value">Slider value.</param>
        public void OnThresholdSliderValueChanged(float value)
        {
            _threshold = value;
        }

        // Private Methods
        private void CreateLUT()
        {
            _lut = new Mat(1, 256, CvType.CV_8UC1);

            for (int i = 0; i < 256; i++)
            {
                double gammaCorrection = Mathf.Pow((float)i / 255f, 1f / _gamma) * 255f;
                _lut.put(0, i, gammaCorrection);
            }
        }
    }
}
