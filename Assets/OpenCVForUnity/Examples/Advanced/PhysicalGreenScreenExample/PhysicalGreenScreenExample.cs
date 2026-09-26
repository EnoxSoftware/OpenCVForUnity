using System;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Interaction;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Physical Green Screen Example
    /// Composites a background image over a physical green-screen subject using HSV chroma keying.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - HSV_FULL range masking with Core.inRange
    /// - Tap-to-pick key color and slider-adjusted H/S/V tolerance
    /// - copyTo with mask to replace keyed pixels with a background Mat
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Scalar"/>, <see cref="Point"/>
    /// - <see cref="Core"/>: inRange, sumElems
    /// - <see cref="Imgproc"/>: cvtColor, resize
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
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
        private Mat _hsvMat;
        private Mat _chromaKeyMaskMat;
        private Mat _backGroundImageMat;

        // Lower and Upper bounds for range checking in HSV color space
        private Scalar _lowerBound = new Scalar(0);
        private Scalar _upperBound = new Scalar(0);

        // Color radius for range checking in HSV color space
        private Scalar _colorRadiusRange = new Scalar(25, 50, 50, 0);
        private Scalar _blobColorHsv = new Scalar(99, 255, 177, 255);
        private Mat _spectrumMat;
        private Texture2D _spectrumTexture;
        private Texture2D _texture;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;
        private bool _shouldUpdateChromakeyFromPoint = false;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();

            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            WireSourceToMatControlPanelHooks();

            _multiSourceToMatHelper.Initialize();
        }

        private void OnDestroy()
        {
            UnwireSourceToMatControlPanelHooks();
        }

        // Public Methods
        /// <summary>
        /// Raises the helper frame mat updated event.
        /// Updates the preview texture when a new frame is available during playback.
        /// </summary>
        public void OnSourceToMatHelperFrameMatUpdated()
        {
            if (!_multiSourceToMatHelper.IsPlaying)
            {
                return;
            }

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;

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

            // copyTo with mask: pixels inside the key range are replaced by the background image.
            _backGroundImageMat.copyTo(rgbaMat, _chromaKeyMaskMat);

            // Draw current selection overlay
            TexturePointSelector.DrawSelection(rgbaMat, true);

            OpenCVMatUnityUtils.MatToTexture2D(rgbaMat, _texture);
        }

        /// <summary>
        /// Raises the helper initialized event.
        /// Recreates the preview texture and starts playback on first initialization.
        /// Skips Play when re-initialization has already restored Playing or Paused.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized", this);

            RecreatePreviewTexture();

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;
            CreateOrRecreateProcessingResources(rgbaMat);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Clear();
                UpdateFpsMonitorPlaybackState();
                _fpsMonitor.Add("HelperKind", _multiSourceToMatHelper.RequestedHelperKind.ToString());
                _fpsMonitor.Add("Width", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("Height", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("Rotate90Degree", _multiSourceToMatHelper.Rotate90Degree.ToString());
                _fpsMonitor.Add("FlipVertical", _multiSourceToMatHelper.FlipVertical.ToString());
                _fpsMonitor.Add("FlipHorizontal", _multiSourceToMatHelper.FlipHorizontal.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());

                _fpsMonitor.Add("blobColorHsv", "\n" + _blobColorHsv.ToString());
                _fpsMonitor.Add("colorRadiusRange", "\n" + _colorRadiusRange.ToString());

                _fpsMonitor.Toast("Touch the screen to specify the chromakey color.", 4000);
            }

            // Set default chromakey color.
            _blobColorHsv = new Scalar(99, 255, 177, 255); // = R:39 G:255 B:86 (Green screen)
            SetHsvColor(_blobColorHsv);

            // Reset TexturePointSelector state
            if (TexturePointSelector != null)
            {
                TexturePointSelector.ResetSelectionStatus();
            }

            if (!_multiSourceToMatHelper.IsPlaying && !_multiSourceToMatHelper.IsPaused)
            {
                _multiSourceToMatHelper.Play();
                UpdateFpsMonitorPlaybackState();
            }
        }

        /// <summary>
        /// Raises the helper frame mat layout changed event.
        /// Recreates the preview texture when rotation or output size changes.
        /// </summary>
        public void OnSourceToMatHelperFrameMatLayoutChanged()
        {
            Debug.Log("OnSourceToMatHelperFrameMatLayoutChanged", this);

            RecreatePreviewTexture();

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;
            if (rgbaMat != null)
            {
                CreateOrRecreateProcessingResources(rgbaMat);
            }

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("Width", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("Height", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
            }
        }

        /// <summary>
        /// Raises the helper released event.
        /// </summary>
        public void OnSourceToMatHelperReleased()
        {
            Debug.Log("OnSourceToMatHelperReleased", this);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Clear();
            }

            DisposeFrameProcessingResources();
            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed", this);

            DisposeFrameProcessingResources();
            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(SourceToMatErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message, this);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        /// <summary>
        /// Raises the back button click event.
        /// Stops playback and disposes the helper before scene transition (required on WebGL).
        /// </summary>
        public async void OnBackButtonClick()
        {
            if (_multiSourceToMatHelper.IsPlaying || _multiSourceToMatHelper.IsPaused)
            {
                await _multiSourceToMatHelper.StopAsync();
            }

            await _multiSourceToMatHelper.DisposeAsync();

            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterPlay"/>.
        /// </summary>
        public void OnControlPanelAfterPlay()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterPause"/>.
        /// </summary>
        public void OnControlPanelAfterPause()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterStop"/>.
        /// </summary>
        public void OnControlPanelAfterStop()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnRotate90Changed"/>.
        /// </summary>
        /// <param name="isOn">New Rotate90Degree value applied by the panel.</param>
        public void OnControlPanelRotate90Changed(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("Rotate90Degree", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnFlipVerticalChanged"/>.
        /// </summary>
        /// <param name="isOn">New FlipVertical value applied by the panel.</param>
        public void OnControlPanelFlipVerticalChanged(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("FlipVertical", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnFlipHorizontalChanged"/>.
        /// </summary>
        /// <param name="isOn">New FlipHorizontal value applied by the panel.</param>
        public void OnControlPanelFlipHorizontalChanged(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("FlipHorizontal", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnHelperKindChanged"/>.
        /// </summary>
        /// <param name="kindIndex">Dropdown index matching <see cref="MultiSourceHelperKind"/>.</param>
        public void OnControlPanelHelperKindChanged(int kindIndex)
        {
            if (_fpsMonitor == null || !Enum.IsDefined(typeof(MultiSourceHelperKind), kindIndex))
            {
                return;
            }

            _fpsMonitor.Add("HelperKind", ((MultiSourceHelperKind)kindIndex).ToString());
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
                OpenCVMatUnityUtils.MatToTexture2D(_spectrumMat, _spectrumTexture);

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
        private void DisposeFrameProcessingResources()
        {
            _hsvMat?.Dispose();
            _hsvMat = null;
            _chromaKeyMaskMat?.Dispose();
            _chromaKeyMaskMat = null;
            _backGroundImageMat?.Dispose();
            _backGroundImageMat = null;
            _spectrumMat?.Dispose();
            _spectrumMat = null;

            if (_spectrumTexture != null)
            {
                Texture2D.Destroy(_spectrumTexture);
                _spectrumTexture = null;
            }
        }

        private void CreateOrRecreateProcessingResources(Mat rgbaMat)
        {
            if (rgbaMat == null)
            {
                return;
            }

            DisposeFrameProcessingResources();

            _hsvMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
            _chromaKeyMaskMat = new Mat(_hsvMat.size(), CvType.CV_8UC1);
            _backGroundImageMat = new Mat(_hsvMat.size(), CvType.CV_8UC4, new Scalar(39, 255, 86, 255));

            if (BackGroundImageTexture != null)
            {
                using (Mat bgMat = new Mat(BackGroundImageTexture.height, BackGroundImageTexture.width, CvType.CV_8UC4))
                {
                    OpenCVMatUnityUtils.Texture2DToMat(BackGroundImageTexture, bgMat);
                    Imgproc.resize(bgMat, _backGroundImageMat, _backGroundImageMat.size());
                }
            }

            _spectrumMat = new Mat(100, 100, CvType.CV_8UC4, new Scalar(255, 255, 255, 255));
            _spectrumTexture = new Texture2D(_spectrumMat.cols(), _spectrumMat.rows(), TextureFormat.RGBA32, false);
        }

        private void RecreatePreviewTexture()
        {
            Mat frameMat = _multiSourceToMatHelper.FrameMat;
            if (frameMat == null)
            {
                return;
            }

            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
                _texture = null;
            }

            bool isRgb = _multiSourceToMatHelper.OutputColorFormat == SourceToMatColorFormat.RGB;
            _texture = new Texture2D(frameMat.cols(), frameMat.rows(), isRgb ? TextureFormat.RGB24 : TextureFormat.RGBA32, false);
            OpenCVMatUnityUtils.MatToTexture2D(frameMat, _texture);

            if (ResultPreview != null)
            {
                ResultPreview.texture = _texture;
                AspectRatioFitter aspectRatioFitter = ResultPreview.GetComponent<AspectRatioFitter>();
                if (aspectRatioFitter != null)
                {
                    aspectRatioFitter.aspectRatio = (float)_texture.width / _texture.height;
                }
            }
        }

        private void CleanupPreviewResources()
        {
            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
                _texture = null;
            }

            UpdateFpsMonitorPlaybackState();
        }

        private void UpdateFpsMonitorPlaybackState()
        {
            if (_fpsMonitor == null || _multiSourceToMatHelper == null)
            {
                return;
            }

            _fpsMonitor.Add("PlaybackState", GetPlaybackStateText());
        }

        private string GetPlaybackStateText()
        {
            if (!_multiSourceToMatHelper.IsInitialized)
            {
                return "Uninitialized";
            }

            if (_multiSourceToMatHelper.IsPlaying)
            {
                return "Playing";
            }

            if (_multiSourceToMatHelper.IsPaused)
            {
                return "Paused";
            }

            return "Ready";
        }

        private void WireSourceToMatControlPanelHooks()
        {
            _controlPanel = GetComponent<SourceToMatControlPanel>();
            if (_controlPanel == null)
            {
                return;
            }

            _controlPanel.OnAfterPlay.AddListener(OnControlPanelAfterPlay);
            _controlPanel.OnAfterPause.AddListener(OnControlPanelAfterPause);
            _controlPanel.OnAfterStop.AddListener(OnControlPanelAfterStop);
            _controlPanel.OnRotate90Changed.AddListener(OnControlPanelRotate90Changed);
            _controlPanel.OnFlipVerticalChanged.AddListener(OnControlPanelFlipVerticalChanged);
            _controlPanel.OnFlipHorizontalChanged.AddListener(OnControlPanelFlipHorizontalChanged);
            _controlPanel.OnHelperKindChanged.AddListener(OnControlPanelHelperKindChanged);
        }

        private void UnwireSourceToMatControlPanelHooks()
        {
            if (_controlPanel == null)
            {
                return;
            }

            _controlPanel.OnAfterPlay.RemoveListener(OnControlPanelAfterPlay);
            _controlPanel.OnAfterPause.RemoveListener(OnControlPanelAfterPause);
            _controlPanel.OnAfterStop.RemoveListener(OnControlPanelAfterStop);
            _controlPanel.OnRotate90Changed.RemoveListener(OnControlPanelRotate90Changed);
            _controlPanel.OnFlipVerticalChanged.RemoveListener(OnControlPanelFlipVerticalChanged);
            _controlPanel.OnFlipHorizontalChanged.RemoveListener(OnControlPanelFlipHorizontalChanged);
            _controlPanel.OnHelperKindChanged.RemoveListener(OnControlPanelHelperKindChanged);
            _controlPanel = null;
        }

        private void UpdateChromaKeyFromPoint(Mat img, Point touchPoint)
        {
            int cols = img.cols();
            int rows = img.rows();

            int x = (int)touchPoint.x;
            int y = (int)touchPoint.y;

            //Debug.Log ("Touch image coordinates: (" + x + ", " + y + ")");

            if ((x < 0) || (y < 0) || (x > cols) || (y > rows))
            {
                return;
            }

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
                {
                    _blobColorHsv.val[i] /= pointCount;
                }

                SetHsvColor(_blobColorHsv);
            }
        }
    }
}
