using System;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Interaction;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using OpenCVForUnity.VideoModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenCVDebug = OpenCVForUnity.Extensions.OpenCVDebug;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// TransformECC Example
    /// Tracks a user-selected point across frames using enhanced correlation coefficient (ECC) alignment.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Downscaled grayscale frames for faster Video.findTransformECC
    /// - Translation, Euclidean, affine, and homography motion models
    /// - Applying the warp matrix to keep a screen point aligned frame-to-frame
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Video"/>: findTransformECC, MOTION_*
    /// - <see cref="Imgproc"/>: resize, cvtColor, rectangle
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="TextureSelector"/>
    /// </summary>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class TransformECCExample : MonoBehaviour
    {
        // Enums
        /// <summary>
        /// Warp mode preset enum
        /// </summary>
        public enum WarpModePreset : int
        {
            TRANSLATION = 0,
            EUCLIDEAN,
            AFFINE,
            HOMOGRAPHY
        }

        // Constants
        // downscaling ratio.
        private const float SCALING = 3f;

        // Specify the number of iterations.
        private const int NUMBER_OF_ITERATIONS = 50; // 5000;

        // Specify the threshold of the increment
        // in the correlation coefficient between two iterations
        private const double TERMINATION_EPS = 1e-7; // 1e-10;

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        [Header("UI")]

        /// <summary>
        /// The texture selector (point selection).
        /// </summary>
        public TextureSelector TexturePointSelector;

        /// <summary>
        /// The warp mode dropdown.
        /// </summary>
        public Dropdown WarpModeDropdown;

        [Space(10)]

        /// <summary>
        /// parameter, specifying the type of motion.
        /// </summary>
        public WarpModePreset WarpMode = (WarpModePreset)Video.MOTION_EUCLIDEAN;

        // Private Fields
        private float _x;
        private float _y;
        private Mat _scalingMat;
        private Mat _grayMat;
        private Mat _lastGrayMat;
        private Mat _lastWarpMatrix;

        // Define termination criteria
        private TermCriteria _criteria = new TermCriteria(TermCriteria.EPS | TermCriteria.COUNT, NUMBER_OF_ITERATIONS, TERMINATION_EPS);
        private bool _shouldUpdatePointFromSelection = false;
        private Texture2D _texture;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            WireSourceToMatControlPanelHooks();

            // Update GUI state
            WarpModeDropdown.value = (int)WarpMode;

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Touch the screen to lock the point.";
            }

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

            if (_shouldUpdatePointFromSelection)
            {
                var (gameObject, currentSelectionState, currentSelectionPoints) = TexturePointSelector.GetSelectionStatus();
                var p = TextureSelector.ConvertSelectionPointsToOpenCVPoint(currentSelectionPoints);
                _x = (float)p.x / SCALING;
                _y = (float)p.y / SCALING;

                TexturePointSelector.ResetSelectionStatus();

                _shouldUpdatePointFromSelection = false;
            }

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;

            // ECC runs on downscaled grayscale to reduce iteration cost; SCALING maps coords back to full frame.
            Imgproc.resize(rgbaMat, _scalingMat, new Size(rgbaMat.width() / SCALING, rgbaMat.height() / SCALING));
            Imgproc.cvtColor(_scalingMat, _grayMat, Imgproc.COLOR_RGBA2GRAY);

            if (_lastGrayMat == null)
            {
                _lastGrayMat = _grayMat.clone();
            }

            if (_lastWarpMatrix == null)
            {
                // Define 2x3 or 3x3 matrices and initialize the matrix to identity
                if (WarpMode == (WarpModePreset)Video.MOTION_HOMOGRAPHY)
                {
                    _lastWarpMatrix = Mat.eye(3, 3, CvType.CV_32F);
                }
                else
                {
                    _lastWarpMatrix = Mat.eye(2, 3, CvType.CV_32F);
                }
            }

            try
            {
                // findTransformECC updates _lastWarpMatrix in place; throws CvException when alignment fails to converge.
                double ret = Video.findTransformECC(_lastGrayMat, _grayMat, _lastWarpMatrix, (int)WarpMode, _criteria);
            }
            catch (CvException e)
            {
                Debug.LogWarning("Warning: An error occurred while analyzing frame motion; the transformation did not converge," +
                    " so this frame will be skipped. Error details: " + e, this);

                _lastWarpMatrix?.Dispose();
                _lastWarpMatrix = null;
                _grayMat.copyTo(_lastGrayMat);
                return;
            }

            _grayMat.copyTo(_lastGrayMat);

            // Homogeneous multiply maps the tracked point through the estimated frame-to-frame warp.
            Mat xyz = new Mat(3, 1, CvType.CV_32F);
            xyz.put(0, 0, _x);
            xyz.put(1, 0, _y);
            xyz.put(2, 0, 1.0);
            Mat result = _lastWarpMatrix.matMul(xyz);
            _x = (float)result.get(0, 0)[0];
            _y = (float)result.get(1, 0)[0];

            // Draw a box in the frame at x, y
            Imgproc.rectangle(rgbaMat, new Point((_x * SCALING) - 1, (_y * SCALING) - 1), new Point((_x * SCALING) + 1, (_y * SCALING) + 1), new Scalar(255, 0, 255, 255), 2);
            Imgproc.rectangle(rgbaMat, new Point((_x * SCALING) - 8, (_y * SCALING) - 8), new Point((_x * SCALING) + 8, (_y * SCALING) + 8), new Scalar(0, 255, 0, 255), 2);

            // Draw current selection overlay
            TexturePointSelector.DrawSelection(rgbaMat, true);

            //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

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

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true, true);

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

            DisposeFrameProcessingResources();
            CleanupPreviewResources();

            OpenCVDebug.SetDebugMode(false, false);
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
        /// Raises the warp mode dropdown value changed event.
        /// </summary>
        public void OnWarpModeDropdownValueChanged(int result)
        {
            if ((int)WarpMode != result)
            {
                WarpMode = (WarpModePreset)result;

                _lastWarpMatrix?.Dispose();
                _lastWarpMatrix = null;
            }
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
                    _shouldUpdatePointFromSelection = true;
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
            _scalingMat?.Dispose();
            _scalingMat = null;

            _grayMat?.Dispose();
            _grayMat = null;

            _lastGrayMat?.Dispose();
            _lastGrayMat = null;

            _lastWarpMatrix?.Dispose();
            _lastWarpMatrix = null;
        }

        private void CreateOrRecreateProcessingResources(Mat rgbaMat)
        {
            if (rgbaMat == null)
            {
                return;
            }

            DisposeFrameProcessingResources();

            _scalingMat = new Mat((int)(rgbaMat.rows() / SCALING), (int)(rgbaMat.cols() / SCALING), CvType.CV_8SC4);
            _grayMat = new Mat(_scalingMat.size(), CvType.CV_8SC1);

            _x = _scalingMat.cols() / 2f;
            _y = _scalingMat.rows() / 2f;

            // Reset TexturePointSelector state
            if (TexturePointSelector != null)
            {
                TexturePointSelector.ResetSelectionStatus();
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
    }
}
