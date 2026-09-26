using System;
using System.Collections.Generic;
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

namespace OpenCVForUnityExample
{
    /// <summary>
    /// CamShift Example
    /// Tracks a color region selected by the user using mean-shift based CamShift.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - HSV color-space histogram from a user-drawn ROI
    /// - Back-projection and Video.CamShift iterative tracking
    /// - TextureSelector rectangle selection on each input frame
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Video"/>: CamShift
    /// - <see cref="Imgproc"/>: cvtColor, calcHist, calcBackProject, line, rectangle
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="TextureSelector"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// http://www.computervisiononline.com/blog/tutorial-using-camshift-track-objects-video
    /// http://docs.opencv.org/3.2.0/db/df8/tutorial_py_meanshift.html
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class CamShiftExample : MonoBehaviour
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
        /// The texture touched point getter component.
        /// </summary>
        public TextureSelector TextureRectangleSelector;

        // Private Fields
        private Texture2D _texture;
        private OpenCVForUnity.CoreModule.Rect _roiRect;
        private Mat _hsvMat;
        private Mat _roiHistMat;
        private TermCriteria _termination;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private bool _shouldStartCamShift = false;
        private bool _isCamShiftStarted = false;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _termination = new TermCriteria(TermCriteria.EPS | TermCriteria.COUNT, 10, 1);

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

            // CamShift tracks in HSV; hue channel (index 0) drives the color histogram.
            Imgproc.cvtColor(rgbaMat, _hsvMat, Imgproc.COLOR_RGBA2RGB);
            Imgproc.cvtColor(_hsvMat, _hsvMat, Imgproc.COLOR_RGB2HSV);

            if (_shouldStartCamShift)
            {

                // Get rectangle selection points from TextureRectangleSelector
                var (gameObject, currentSelectionState, currentSelectionPoints) = TextureRectangleSelector.GetSelectionStatus();
                // Convert rectangle points to OpenCV Rect using TextureSelector utility method
                // Note: currentSelectionPoints is guaranteed to have 2 elements (start and end points) when RECTANGLE_SELECTION_COMPLETED
                _roiRect = TextureSelector.ConvertSelectionPointsToOpenCVRect(currentSelectionPoints);

                _roiHistMat?.Dispose();
                _roiHistMat = null;
                _roiHistMat = new Mat();

                using (Mat roiHSVMat = new Mat(_hsvMat, _roiRect))
                using (Mat maskMat = new Mat())
                {
                    // 16-bin hue histogram from the selected ROI becomes the appearance model.
                    Imgproc.calcHist(new List<Mat>(new Mat[] { roiHSVMat }), new MatOfInt(0), maskMat, _roiHistMat, new MatOfInt(16), new MatOfFloat(0, 180));
                    Core.normalize(_roiHistMat, _roiHistMat, 0, 255, Core.NORM_MINMAX);

                    // Set CamShift started flag
                    _isCamShiftStarted = true;

                    //Debug.Log ("_roiHistMat " + _roiHistMat.ToString ());
                }

                _shouldStartCamShift = false;
            }

            if (_isCamShiftStarted)
            {
                // Create points array for CamShift tracking
                Point[] points = new Point[4];

                using (Mat backProj = new Mat())
                {
                    // Back-project hue channel, then CamShift refines the search window each frame.
                    Imgproc.calcBackProject(new List<Mat>(new Mat[] { _hsvMat }), new MatOfInt(0), _roiHistMat, backProj, new MatOfFloat(0, 180), 1.0);

                    RotatedRect r = Video.CamShift(backProj, _roiRect, _termination);
                    r.points(points);
                }

                // Draw tracking rectangle and lines after CamShift processing
                for (int i = 0; i < 4; i++)
                {
                    Imgproc.line(rgbaMat, points[i], points[(i + 1) % 4], new Scalar(255, 0, 0, 255), 2);
                }

                Imgproc.rectangle(rgbaMat, _roiRect.tl(), _roiRect.br(), new Scalar(0, 255, 0, 255), 2);
            }

            // Draw current selection state on the Mat
            TextureRectangleSelector.DrawSelection(rgbaMat, true);

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

                _fpsMonitor.ConsoleText = "Please touch the screen to select a rectangle for tracking.";
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
        /// Handles the texture selection state changed event.
        /// This method should be connected to the TextureSelector's OnTextureSelectionStateChanged event in the Inspector.
        /// </summary>
        /// <param name="touchedObject">The GameObject that was touched.</param>
        /// <param name="touchState">The touch state.</param>
        /// <param name="texturePoints">The texture coordinates array (OpenCV format: top-left origin).</param>
        public void OnTextureSelectionStateChanged(GameObject touchedObject, TextureSelector.TextureSelectionState touchState, Vector2[] texturePoints)
        {
            if (_isCamShiftStarted)
            {
                switch (touchState)
                {
                    case TextureSelector.TextureSelectionState.RECTANGLE_SELECTION_STARTED:
                        // Reset CamShift when new selection starts
                        _isCamShiftStarted = false;
                        Debug.Log("Resetting CamShift due to new selection.", this);
                        break;
                }
            }
            else
            {
                switch (touchState)
                {
                    case TextureSelector.TextureSelectionState.RECTANGLE_SELECTION_COMPLETED:
                        // Start CamShift when rectangle selection is completed
                        _shouldStartCamShift = true;
                        Debug.Log("Rectangle selection completed, starting CamShift tracking.", this);
                        break;
                }
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
            _hsvMat?.Dispose();
            _hsvMat = null;
            _roiHistMat?.Dispose();
            _roiHistMat = null;
        }

        private void CreateOrRecreateProcessingResources(Mat rgbaMat)
        {
            if (rgbaMat == null)
            {
                return;
            }

            DisposeFrameProcessingResources();

            _hsvMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);

            _isCamShiftStarted = false;
            _shouldStartCamShift = false;

            // Reset TextureRectangleSelector state
            TextureRectangleSelector.ResetSelectionStatus();
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
