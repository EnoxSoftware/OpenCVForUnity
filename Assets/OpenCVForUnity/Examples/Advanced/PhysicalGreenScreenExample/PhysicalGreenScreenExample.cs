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
    /// Physical Green Screen Example
    /// An example of creating a chromakey mask and compositing background image. (aka green-screen compositing)
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class PhysicalGreenScreenExample : MonoBehaviour
    {
        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        [Header("UI")]
        /// <summary>
        /// The background image texture.
        /// </summary>
        public Texture2D BackGroundImageTexture;

        /// <summary>
        /// The radius range sliders.
        /// </summary>
        public Slider HRadiusRangeSlider;
        public Slider SRadiusRangeSlider;
        public Slider VRadiusRangeSlider;

        /// <summary>
        /// The spectrum image UI.
        /// </summary>
        public RawImage SpectrumImage;

        /// <summary>
        /// The texture selector (point selection).
        /// </summary>
        public TextureSelector TexturePointSelector;

        // Private Fields
        /// <summary>
        /// The hsv mat.
        /// </summary>
        private Mat _hsvMat;

        /// <summary>
        /// The chroma key mask mat.
        /// </summary>
        private Mat _chromaKeyMaskMat;

        /// <summary>
        /// The background image mat.
        /// </summary>
        private Mat _backGroundImageMat;

        // Lower and Upper bounds for range checking in HSV color space
        private Scalar _lowerBound = new Scalar(0);
        private Scalar _upperBound = new Scalar(0);

        // Color radius for range checking in HSV color space
        private Scalar _colorRadiusRange = new Scalar(25, 50, 50, 0);

        /// <summary>
        /// The BLOB color hsv.
        /// </summary>
        private Scalar _blobColorHsv = new Scalar(99, 255, 177, 255);

        /// <summary>
        /// The spectrum mat.
        /// </summary>
        private Mat _spectrumMat;

        /// <summary>
        /// The spectrum texture.
        /// </summary>
        private Texture2D _spectrumTexture;

        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// The flag to request chromakey update from selected point.
        /// </summary>
        private bool _shouldUpdateChromakeyFromPoint = false;



        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;
            _multiSource2MatHelper.Initialize();
        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_shouldUpdateChromakeyFromPoint)
                {

                    var (gameObject, currentSelectionState, currentSelectionPoints) = TexturePointSelector.GetSelectionStatus();
                    var p = TextureSelector.ConvertSelectionPointsToOpenCVPoint(currentSelectionPoints);
                    UpdateChromaKeyFromPoint(rgbaMat, p);

                    TexturePointSelector.ResetSelectionStatus();

                    _shouldUpdateChromakeyFromPoint = false;
                }

                // Convert the color space from RGBA to HSV_FULL.
                // HSV_FULL is HSV with H elements scaled from 0 to 255.
                Imgproc.cvtColor(rgbaMat, _hsvMat, Imgproc.COLOR_RGB2HSV_FULL);

                // Create a chromakey mask from extracting the lower and upper limits range of values in the HSV color space.
                Core.inRange(_hsvMat, _lowerBound, _upperBound, _chromaKeyMaskMat);

                // Compose the background image.
                _backGroundImageMat.copyTo(rgbaMat, _chromaKeyMaskMat);

                // Draw current selection overlay
                TexturePointSelector.DrawSelection(rgbaMat, true);

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
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

            Mat rgbaMat = _multiSource2MatHelper.GetMat();

            _texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;


            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("width", rgbaMat.width().ToString());
                _fpsMonitor.Add("height", rgbaMat.height().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());

                _fpsMonitor.Add("blobColorHsv", "\n" + _blobColorHsv.ToString());
                _fpsMonitor.Add("colorRadiusRange", "\n" + _colorRadiusRange.ToString());

                _fpsMonitor.Toast("Touch the screen to specify the chromakey color.", 2000);
            }

            _hsvMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
            _chromaKeyMaskMat = new Mat(_hsvMat.size(), CvType.CV_8UC1);
            _backGroundImageMat = new Mat(_hsvMat.size(), CvType.CV_8UC4, new Scalar(39, 255, 86, 255));

            if (BackGroundImageTexture != null)
            {
                using (Mat bgMat = new Mat(BackGroundImageTexture.height, BackGroundImageTexture.width, CvType.CV_8UC4))
                {
                    OpenCVMatUtils.Texture2DToMat(BackGroundImageTexture, bgMat);
                    Imgproc.resize(_backGroundImageMat, _backGroundImageMat, _backGroundImageMat.size());
                }
            }

            _spectrumMat = new Mat(100, 100, CvType.CV_8UC4, new Scalar(255, 255, 255, 255));
            _spectrumTexture = new Texture2D(_spectrumMat.cols(), _spectrumMat.rows(), TextureFormat.RGBA32, false);

            // Set default chromakey color.
            _blobColorHsv = new Scalar(99, 255, 177, 255); // = R:39 G:255 B:86 (Green screen)
            SetHsvColor(_blobColorHsv);

            // Reset TexturePointSelector state
            if (TexturePointSelector != null)
            {
                TexturePointSelector.ResetSelectionStatus();
            }
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _hsvMat?.Dispose(); _hsvMat = null;
            _chromaKeyMaskMat?.Dispose(); _chromaKeyMaskMat = null;
            _backGroundImageMat?.Dispose(); _backGroundImageMat = null;
            _spectrumMat?.Dispose(); _spectrumMat = null;
            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
            if (_spectrumTexture != null) Texture2D.Destroy(_spectrumTexture); _spectrumTexture = null;
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
        /// Sets the HSV color.
        /// </summary>
        /// <param name="hsvColor">The HSV color.</param>
        public void SetHsvColor(Scalar hsvColor)
        {
            // Calculate lower and Upper bounds.
            double minH = (hsvColor.val[0] >= _colorRadiusRange.val[0]) ? hsvColor.val[0] - _colorRadiusRange.val[0] : 0;
            double maxH = (hsvColor.val[0] + _colorRadiusRange.val[0] <= 255) ? hsvColor.val[0] + _colorRadiusRange.val[0] : 255;

            _lowerBound.val[0] = minH;
            _upperBound.val[0] = maxH;

            _lowerBound.val[1] = hsvColor.val[1] - _colorRadiusRange.val[1];
            _lowerBound.val[1] = (_lowerBound.val[1] >= 0) ? _lowerBound.val[1] : 0;
            _upperBound.val[1] = hsvColor.val[1] + _colorRadiusRange.val[1];
            _upperBound.val[1] = (_upperBound.val[1] <= 255) ? _upperBound.val[1] : 255;

            _lowerBound.val[2] = hsvColor.val[2] - _colorRadiusRange.val[2];
            _lowerBound.val[2] = (_lowerBound.val[2] >= 0) ? _lowerBound.val[2] : 0;
            _upperBound.val[2] = hsvColor.val[2] + _colorRadiusRange.val[2];
            _upperBound.val[2] = (_upperBound.val[2] <= 255) ? _upperBound.val[2] : 255;

            _lowerBound.val[3] = 0;
            _upperBound.val[3] = 255;

            // Generate a spectrum chart.
            using (Mat spectrumHsv = new Mat((int)(_upperBound.val[1] - _lowerBound.val[1]), (int)(maxH - minH), CvType.CV_8UC3))
            using (Mat spectrumRgba = new Mat((int)(_upperBound.val[1] - _lowerBound.val[1]), (int)(maxH - minH), CvType.CV_8UC4))
            {
                for (int i = 0; i < _upperBound.val[1] - _lowerBound.val[1]; i++)
                {
                    for (int j = 0; j < maxH - minH; j++)
                    {
                        byte[] tmp = { (byte)(minH + j), (byte)(_lowerBound.val[1] + i), (byte)hsvColor.val[2] };
                        spectrumHsv.put(i, j, tmp);
                    }
                }

                Imgproc.cvtColor(spectrumHsv, spectrumRgba, Imgproc.COLOR_HSV2RGB_FULL, 4);

                Imgproc.resize(spectrumRgba, _spectrumMat, _spectrumMat.size());
                OpenCVMatUtils.MatToTexture2D(_spectrumMat, _spectrumTexture);

                SpectrumImage.texture = _spectrumTexture;
            }

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("blobColorHsv", "\n" + _blobColorHsv.ToString());
                _fpsMonitor.Add("colorRadiusRange", "\n" + _colorRadiusRange.ToString());
            }

            //Debug.Log("blobColorHsv: " + _blobColorHsv);
            //Debug.Log("lowerBound: " + _lowerBound);
            //Debug.Log("upperBound: " + _upperBound);
            //Debug.Log("blobColorRgba: " + ConverScalarHsv2Rgba(_blobColorHsv));
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
        /// Raises the radius range slider value changed event.
        /// </summary>
        public void OnRadiusRangeSliderValueChanged()
        {
            _colorRadiusRange = new Scalar(HRadiusRangeSlider.value, SRadiusRangeSlider.value, VRadiusRangeSlider.value, 255);

            SetHsvColor(_blobColorHsv);
        }

        /// <summary>
        /// Handles the texture selection state changed event from TextureSelector.
        /// This should be wired in the Inspector to TextureSelector.OnTextureSelectionStateChanged.
        /// </summary>
        /// <param name="touchedObject">The GameObject that was touched.</param>
        /// <param name="touchState">The touch state.</param>
        /// <param name="texturePoints">The texture coordinates array (OpenCV format: top-left origin).</param>
        public void OnTextureSelectionStateChanged(GameObject touchedObject, TextureSelector.TextureSelectionState touchState, Vector2[] texturePoints)
        {
            switch (touchState)
            {
                case TextureSelector.TextureSelectionState.POINT_SELECTION_COMPLETED:
                    _shouldUpdateChromakeyFromPoint = true;
                    break;
            }
        }

        // Private Methods
        private void UpdateChromaKeyFromPoint(Mat img, Point touchPoint)
        {
            int cols = img.cols();
            int rows = img.rows();

            int x = (int)touchPoint.x;
            int y = (int)touchPoint.y;

            //Debug.Log ("Touch image coordinates: (" + x + ", " + y + ")");

            if ((x < 0) || (y < 0) || (x > cols) || (y > rows))
                return;

            OpenCVForUnity.CoreModule.Rect touchedRect = new OpenCVForUnity.CoreModule.Rect();

            touchedRect.x = (x > 5) ? x - 5 : 0;
            touchedRect.y = (y > 5) ? y - 5 : 0;

            touchedRect.width = (x + 5 < cols) ? x + 5 - touchedRect.x : cols - touchedRect.x;
            touchedRect.height = (y + 5 < rows) ? y + 5 - touchedRect.y : rows - touchedRect.y;

            using (Mat touchedRegionRgba = img.submat(touchedRect))
            using (Mat touchedRegionHsv = new Mat())
            {
                Imgproc.cvtColor(touchedRegionRgba, touchedRegionHsv, Imgproc.COLOR_RGB2HSV_FULL);

                // Calculate average color of touched region.
                _blobColorHsv = Core.sumElems(touchedRegionHsv);
                int pointCount = touchedRect.width * touchedRect.height;
                for (int i = 0; i < _blobColorHsv.val.Length; i++)
                    _blobColorHsv.val[i] /= pointCount;

                SetHsvColor(_blobColorHsv);
            }
        }
    }
}
