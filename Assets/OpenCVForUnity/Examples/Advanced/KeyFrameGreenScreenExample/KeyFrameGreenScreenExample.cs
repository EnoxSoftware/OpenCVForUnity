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
    /// Key Frame Green Screen Example
    /// Simulates a green-screen shot without physical backdrop using background subtraction.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Capturing a still background frame on tap, then differencing each new frame
    /// - Replacing background pixels with solid green (#00FF00) for key-frame video editing
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Scalar"/>, <see cref="Point"/>, <see cref="Size"/>
    /// - <see cref="Core"/>: absdiff, add, bitwise_not
    /// - <see cref="Imgproc"/>: getStructuringElement, threshold, cvtColor, morphologyEx
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://gist.github.com/drscotthawley/2d6bbffce9dda5f3057b4879c3bd4422
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class KeyFrameGreenScreenExample : MonoBehaviour
    {
        // Public Fields
        [Range(0, 255)]
        /// <summary>
        /// The thresh.
        /// </summary>
        public float Thresh = 50.0f;

        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        /// <summary>
        /// The background raw image.
        /// </summary>
        public RawImage BgRawImage;

        /// <summary>
        /// The texture selector (point selection).
        /// </summary>
        public TextureSelector TexturePointSelector;

        // Private Fields
        private Texture2D _texture;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private Mat _bgMat;
        private Mat _fgMaskMat;
        private Mat _bgMaskMat;
        private Mat _greenMat;
        private Texture2D _bgTexture;
        private Mat _kernel;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;
        private bool _shouldUpdateBackgroundFromPoint = false;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            WireSourceToMatControlPanelHooks();

            _multiSourceToMatHelper.Initialize();

            _kernel = Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(3, 3));
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

            if (_shouldUpdateBackgroundFromPoint)
            {
                // Tap captures the current frame as the static background reference.
                SetBackgroundFromCurrentFrame(rgbaMat);
                _shouldUpdateBackgroundFromPoint = false;
            }

            //set fgMaskMat (foreground = pixels that differ from the stored background).
            FindFgMaskMat(rgbaMat, _bgMat, Thresh);

            //set bgMaskMat
            Core.bitwise_not(_fgMaskMat, _bgMaskMat);

            // copyTo with mask: only pixels where bgMaskMat is non-zero receive green.
            _greenMat.copyTo(rgbaMat, _bgMaskMat);

            // Draw current selection overlay
            // TexturePointSelector.DrawSelection(rgbaMat, true, true);

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

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;

            RecreatePreviewTexture();
            CreateOrRecreateProcessingResources(rgbaMat, resetBackground: true);

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
                _fpsMonitor.Toast("Touch the screen to set background image.", 2000);
            }

            // Reset TexturePointSelector state
            TexturePointSelector.ResetSelectionStatus();

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
                CreateOrRecreateProcessingResources(rgbaMat, resetBackground: true);
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
                case TextureSelector.TextureSelectionState.POINT_SELECTION_STARTED:
                    _shouldUpdateBackgroundFromPoint = true;
                    break;
                case TextureSelector.TextureSelectionState.POINT_SELECTION_CANCELLED:
                    ResetBackgroundToWhite();
                    break;
            }
        }

        // Private Methods
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

        private void DisposeFrameProcessingResources()
        {
            _bgMat?.Dispose();
            _bgMat = null;
            _fgMaskMat?.Dispose();
            _fgMaskMat = null;
            _bgMaskMat?.Dispose();
            _bgMaskMat = null;
            _greenMat?.Dispose();
            _greenMat = null;

            if (_bgTexture != null)
            {
                Texture2D.Destroy(_bgTexture);
                _bgTexture = null;
            }
        }

        private void CreateOrRecreateProcessingResources(Mat rgbaMat, bool resetBackground)
        {
            if (rgbaMat == null)
            {
                return;
            }

            DisposeFrameProcessingResources();

            _bgMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC4);
            _fgMaskMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);
            _bgMaskMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);
            _greenMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC4, new Scalar(0, 255, 0, 255));
            _bgTexture = new Texture2D(_bgMat.cols(), _bgMat.rows(), TextureFormat.RGBA32, false);

            if (resetBackground)
            {
                ResetBackgroundToWhite();
            }
        }

        /// <summary>
        /// Finds the foreground mask mat.
        /// </summary>
        /// <param name="fgMat">Fg mat.</param>
        /// <param name="bgMat">Background mat.</param>
        /// <param name="thresh">Thresh.</param>
        private void FindFgMaskMat(Mat fgMat, Mat bgMat, float thresh = 13.0f)
        {
            // absdiff both ways handles signed channel differences after absdiff.
            Mat diff1 = new Mat();
            Core.absdiff(fgMat, bgMat, diff1);
            Mat diff2 = new Mat();
            Core.absdiff(bgMat, fgMat, diff2);
            Mat diff = new Mat();
            Core.add(diff1, diff2, diff);

            Imgproc.threshold(diff, diff, thresh, 0, Imgproc.THRESH_TOZERO);

            Imgproc.cvtColor(diff, _fgMaskMat, Imgproc.COLOR_RGBA2GRAY);

            Imgproc.threshold(_fgMaskMat, _fgMaskMat, 10, 0, Imgproc.THRESH_TOZERO);

            Imgproc.threshold(_fgMaskMat, _fgMaskMat, 0, 255, Imgproc.THRESH_BINARY);

            // Small area removal from binary images by opening
            Imgproc.morphologyEx(_fgMaskMat, _fgMaskMat, Imgproc.MORPH_OPEN, _kernel);

            diff1.Dispose();
            diff2.Dispose();
            diff.Dispose();
        }

        /// <summary>
        /// Sets the background from current frame.
        /// </summary>
        /// <param name="img">The image mat.</param>
        private void SetBackgroundFromCurrentFrame(Mat img)
        {
            img.copyTo(_bgMat);
            SetBgTexture(_bgMat);
        }

        /// <summary>
        /// Resets the background to white.
        /// </summary>
        private void ResetBackgroundToWhite()
        {
            _bgMat.setTo(new Scalar(255, 255, 255, 255));
            SetBgTexture(_bgMat);
        }

        /// <summary>
        /// Sets the background texture.
        /// </summary>
        /// <param name="bgMat">Background mat.</param>
        private void SetBgTexture(Mat bgMat)
        {
            OpenCVMatUnityUtils.MatToTexture2D(bgMat, _bgTexture);

            BgRawImage.texture = _bgTexture;
            BgRawImage.rectTransform.localScale = new Vector3(1.0f, (float)bgMat.height() / (float)bgMat.width(), 1.0f);
        }
    }
}
