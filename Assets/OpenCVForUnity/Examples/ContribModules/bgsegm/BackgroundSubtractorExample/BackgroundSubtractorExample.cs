using System;
using OpenCVForUnity.BgsegmModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using OpenCVForUnity.VideoModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Background Subtractor Example
    /// Compares foreground masks produced by different background subtraction algorithms on video.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Switching between KNN, MOG2, CNT, GMG, GSOC, LSBP, and MOG subtractors at runtime
    /// - Optional morphological opening to reduce foreground noise
    /// - Toggling between foreground mask and estimated background image display
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Scalar"/>, <see cref="Size"/>
    /// - <see cref="BackgroundSubtractor"/>: apply, getBackgroundImage, clear
    /// - <see cref="Video"/>: createBackgroundSubtractorKNN, createBackgroundSubtractorMOG2
    /// - <see cref="Bgsegm"/>: createBackgroundSubtractorCNT, createBackgroundSubtractorGMG, createBackgroundSubtractorGSOC, createBackgroundSubtractorLSBP, createBackgroundSubtractorMOG
    /// - <see cref="Imgproc"/>: morphologyEx, cvtColor, getStructuringElement
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class BackgroundSubtractorExample : MonoBehaviour
    {
        // Enums
        /// <summary>
        /// Background subtractor algorithm preset enum
        /// </summary>
        public enum BackgroundSubtractorAlgorithmPreset : byte
        {
            KNN = 0,
            MOG2,
            CNT,
            GMG,
            GSOC,
            LSBP,
            MOG,
        }

        // Constants
        private static readonly string VIDEO_FILEPATH = "OpenCVForUnityExamples/768x576_mjpeg.mjpeg";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        /// <summary>
        /// The background subtractor algorithm dropdown.
        /// </summary>
        public Dropdown BackgroundSubtractorAlgorithmDropdown;

        /// <summary>
        /// The background subtractor algorithm.
        /// </summary>
        public BackgroundSubtractorAlgorithmPreset BackgroundSubtractorAlgorithm = BackgroundSubtractorAlgorithmPreset.KNN;

        /// <summary>
        /// The enable MorphologyEx toggle.
        /// </summary>
        public Toggle EnableMorphologyExToggle;

        /// <summary>
        /// The show background image toggle.
        /// </summary>
        public Toggle ShowBackgroundImageToggle;

        // Private Fields
        private Texture2D _texture;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private BackgroundSubtractor _backgroundSubstractor;
        private Mat _fgmaskMat;
        private Mat _kernel;
        private System.Diagnostics.Stopwatch _watch;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGB; // Background subtractor expects a 3-channel BGR/RGB Mat.

            WireSourceToMatControlPanelHooks();

            // Update GUI state
            BackgroundSubtractorAlgorithmDropdown.value = Array.IndexOf(System.Enum.GetNames(typeof(BackgroundSubtractorAlgorithmPreset)), BackgroundSubtractorAlgorithm.ToString());
            EnableMorphologyExToggle.isOn = false;
            ShowBackgroundImageToggle.isOn = false;

            CreateBackgroundSubstractor(BackgroundSubtractorAlgorithm);

            _kernel = Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(3, 3));

            _watch = new System.Diagnostics.Stopwatch();

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("time: ", "");
            }

            if (string.IsNullOrEmpty(_multiSourceToMatHelper.PerKindSettings.VideoCapture.RequestedVideoFilePath))
            {
                _multiSourceToMatHelper.PerKindSettings.VideoCapture.RequestedVideoFilePath = VIDEO_FILEPATH;
            }

            _multiSourceToMatHelper.Initialize();
        }

        private void OnDestroy()
        {
            UnwireSourceToMatControlPanelHooks();

            _backgroundSubstractor?.Dispose();
            _backgroundSubstractor = null;
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

            Mat rgbMat = _multiSourceToMatHelper.FrameMat;

            _watch.Reset();
            _watch.Start();

            // apply() writes a single-channel foreground mask into _fgmaskMat.
            _backgroundSubstractor.apply(rgbMat, _fgmaskMat);

            if (EnableMorphologyExToggle.isOn)
            {
                // MORPH_OPEN removes small noise blobs from the binary mask.
                Imgproc.morphologyEx(_fgmaskMat, _fgmaskMat, Imgproc.MORPH_OPEN, _kernel);
            }

            _watch.Stop();

            if (ShowBackgroundImageToggle.isOn && BackgroundSubtractorAlgorithm != BackgroundSubtractorAlgorithmPreset.GMG)
            {
                // GMG does not support getBackgroundImage(); CNT writes into the mask Mat.
                if (BackgroundSubtractorAlgorithm == BackgroundSubtractorAlgorithmPreset.CNT)
                {
                    _backgroundSubstractor.getBackgroundImage(_fgmaskMat);
                    Imgproc.cvtColor(_fgmaskMat, rgbMat, Imgproc.COLOR_GRAY2RGB);
                }
                else
                {
                    _backgroundSubstractor.getBackgroundImage(rgbMat);
                }
            }
            else
            {
                // Convert binary mask to RGB for display on the RawImage.
                Imgproc.cvtColor(_fgmaskMat, rgbMat, Imgproc.COLOR_GRAY2RGB);
            }

            OpenCVMatUnityUtils.MatToTexture2D(rgbMat, _texture);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("time: ", _watch.ElapsedMilliseconds + " ms");
            }
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
            CreateOrRecreateProcessingResources(_multiSourceToMatHelper.FrameMat);

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
            CreateOrRecreateProcessingResources(_multiSourceToMatHelper.FrameMat);

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

            _backgroundSubstractor?.clear();

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
        /// Raises the background subtractor algorithm dropdown value changed event.
        /// </summary>
        public void OnBackgroundSubtractorAlgorithmDropdownValueChanged(int result)
        {
            string[] enumNames = Enum.GetNames(typeof(BackgroundSubtractorAlgorithmPreset));
            byte value = (byte)System.Enum.Parse(typeof(BackgroundSubtractorAlgorithmPreset), enumNames[result], true);

            if ((byte)BackgroundSubtractorAlgorithm != value)
            {
                BackgroundSubtractorAlgorithm = (BackgroundSubtractorAlgorithmPreset)value;
            }

            //Debug.Log((int)backgroundSubtractorAlgorithm);

            CreateBackgroundSubstractor(BackgroundSubtractorAlgorithm);

            _multiSourceToMatHelper.Initialize();
        }

        /// <summary>
        /// Raises the enable MorphologyEx toggle value changed event.
        /// </summary>
        public void OnEnableMorphologyExToggleValueChanged()
        {
            //
            //Debug.Log(enableMorphologyExToggleToggle.isOn);
        }

        /// <summary>
        /// Raises the show background image toggle value changed event.
        /// </summary>
        public void OnShowBackgroundImageToggleValueChanged()
        {
            //
            //Debug.Log(showBackgroundImageToggle.isOn);
        }

        protected void CreateBackgroundSubstractor(BackgroundSubtractorAlgorithmPreset algorithm)
        {
            if (_backgroundSubstractor != null)
            {
                _backgroundSubstractor.Dispose();
                _backgroundSubstractor = null;
            }

            // Each preset maps to a different OpenCV background-subtraction implementation.
            switch (algorithm)
            {
                case BackgroundSubtractorAlgorithmPreset.KNN:
                    _backgroundSubstractor = Video.createBackgroundSubtractorKNN();

                    //BackgroundSubtractorKNN subtractorKNN = (BackgroundSubtractorKNN)backgroundSubstractor;
                    //subtractorKNN.setDetectShadows(true);
                    //subtractorKNN.setDist2Threshold(400);
                    //subtractorKNN.setHistory(500);
                    //subtractorKNN.setkNNSamples(2);
                    //subtractorKNN.setNSamples(7);
                    //subtractorKNN.setShadowThreshold(0.5);
                    //subtractorKNN.setShadowValue(127);

                    break;
                case BackgroundSubtractorAlgorithmPreset.MOG2:
                    _backgroundSubstractor = Video.createBackgroundSubtractorMOG2();

                    //BackgroundSubtractorMOG2 subtractorMOG2 = (BackgroundSubtractorMOG2)backgroundSubstractor;
                    //subtractorMOG2.setBackgroundRatio(0.899999976158142);
                    //subtractorMOG2.setComplexityReductionThreshold(0.0500000007450581);
                    //subtractorMOG2.setDetectShadows(true);
                    //subtractorMOG2.setHistory(500);
                    //subtractorMOG2.setNMixtures(5);
                    //subtractorMOG2.setShadowThreshold(0.5);
                    //subtractorMOG2.setShadowValue(127);
                    //subtractorMOG2.setVarInit(15);
                    //subtractorMOG2.setVarMax(75);
                    //subtractorMOG2.setVarMin(4);
                    //subtractorMOG2.setVarThreshold(16);
                    //subtractorMOG2.setVarThresholdGen(9);

                    break;
                case BackgroundSubtractorAlgorithmPreset.CNT:
                    _backgroundSubstractor = Bgsegm.createBackgroundSubtractorCNT();

                    //BackgroundSubtractorCNT subtractorCNT = (BackgroundSubtractorCNT)backgroundSubstractor;
                    //subtractorCNT.setIsParallel(true);
                    //subtractorCNT.setMaxPixelStability(900)
                    //subtractorCNT.setMinPixelStability(15);
                    //subtractorCNT.setUseHistory(true);

                    break;
                case BackgroundSubtractorAlgorithmPreset.GMG:
                    _backgroundSubstractor = Bgsegm.createBackgroundSubtractorGMG();
                    break;
                case BackgroundSubtractorAlgorithmPreset.GSOC:
                    _backgroundSubstractor = Bgsegm.createBackgroundSubtractorGSOC();
                    break;
                case BackgroundSubtractorAlgorithmPreset.LSBP:
                    _backgroundSubstractor = Bgsegm.createBackgroundSubtractorLSBP();
                    break;
                case BackgroundSubtractorAlgorithmPreset.MOG:
                    _backgroundSubstractor = Bgsegm.createBackgroundSubtractorMOG();
                    break;
                default:

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

        private void DisposeFrameProcessingResources()
        {
            _fgmaskMat?.Dispose();
            _fgmaskMat = null;
        }

        private void CreateOrRecreateProcessingResources(Mat rgbMat)
        {
            if (rgbMat == null)
            {
                return;
            }

            DisposeFrameProcessingResources();

            // Single-channel mask written by BackgroundSubtractor.apply() each frame.
            _fgmaskMat = new Mat(rgbMat.rows(), rgbMat.cols(), CvType.CV_8UC1);
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
    }
}
