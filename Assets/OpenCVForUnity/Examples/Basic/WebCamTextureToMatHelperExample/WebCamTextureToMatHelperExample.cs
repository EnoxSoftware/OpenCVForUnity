#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// WebCamTextureToMatHelper Example
    /// Streams live webcam frames into OpenCV <see cref="Mat"/> buffers and displays the result on a Unity UI.
    ///
    /// Demonstrates:
    /// - Configuring <see cref="WebCamTextureToMatHelper"/> via Inspector defaults (resolution, FPS, output color)
    /// - Receiving frames via <see cref="SourceToMatHelperBase.OnFrameMatUpdated"/> (Inspector wiring)
    /// - Calling <see cref="SourceToMatHelperBase.Play"/> in OnInitialized only when not already playing or paused (<c>!IsPlaying &amp;&amp; !IsPaused</c>)
    /// - Recreating the preview texture on <see cref="SourceToMatHelperBase.OnFrameMatLayoutChanged"/>
    /// - Overlaying frame info with <see cref="Imgproc.putText"/>
    /// - Converting Mat back to <see cref="Texture2D"/> for <see cref="RawImage"/> preview
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Scalar"/>, <see cref="Point"/>
    /// - <see cref="Imgproc"/>: putText
    /// - <see cref="WebCamTextureToMatHelper"/>, <see cref="SourceToMatColorFormat"/>
    /// - <see cref="OpenCVMatUnityUtils"/>: MatToTexture2D
    ///
    /// Unity integration:
    /// - The Mat returned by <see cref="SourceToMatHelperBase.FrameMat"/> is owned by the helper; do not dispose it
    /// - <see cref="SourceToMatColorFormat.RGBA"/> matches Unity <see cref="TextureFormat.RGBA32"/> for display
    /// - WebGPU forces AsyncGPU inside the helper; this example does not switch helper types with <c>#if</c>
    /// - Helper lifecycle events (OnInitialized, OnFrameMatUpdated, etc.) are wired in the Inspector on <see cref="WebCamTextureToMatHelper"/>
    /// - <see cref="SourceToMatControlPanel"/> on the same GameObject provides Transport / Transform / Camera UI
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class WebCamTextureToMatHelperExample : MonoBehaviour
    {
        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        // Private Fields
        private Texture2D _texture;
        private WebCamTextureToMatHelper _webCamTextureToMatHelper;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            // Get the WebCamTextureToMatHelper component attached to the current game object.
            _webCamTextureToMatHelper = GetComponent<WebCamTextureToMatHelper>();

            // Resolution, FPS, and device are configured on the helper Inspector.
            // RGBA matches Unity TextureFormat.RGBA32 used for RawImage preview.
            _webCamTextureToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            // Subscribe to SourceToMatControlPanel events (Play/Pause/Stop, Rotate/Flip).
            WireSourceToMatControlPanelHooks();

            // Starts the webcam and prepares the internal Mat buffer for frame conversion.
            // OnSourceToMatHelperInitialized is raised when initialization completes (Inspector wiring).
            _webCamTextureToMatHelper.Initialize();
        }

        private void OnDestroy()
        {
            // Unsubscribe control-panel listeners to avoid dangling callbacks.
            UnwireSourceToMatControlPanelHooks();
        }

        // Public Methods
        /// <summary>
        /// Raises the helper frame mat updated event.
        /// Updates the preview texture when a new frame is available during playback.
        /// </summary>
        public void OnSourceToMatHelperFrameMatUpdated()
        {
            // Invoked by the helper when a new frame is available (replaces Update + DidUpdateThisFrame).
            if (!_webCamTextureToMatHelper.IsPlaying)
            {
                return;
            }

            // Returns the helper's internal Mat (RGBA); owned by the helper — do not dispose.
            Mat rgbaMat = _webCamTextureToMatHelper.FrameMat;
            if (rgbaMat == null || _texture == null)
            {
                return;
            }

            // Draw frame stats in-place before display conversion.
            Imgproc.putText(
                rgbaMat,
                "W:" + rgbaMat.width() + " H:" + rgbaMat.height() + " SO:" + Screen.orientation,
                new Point(5, rgbaMat.rows() - 10),
                Imgproc.FONT_HERSHEY_SIMPLEX,
                0.7,
                new Scalar(255, 255, 255, 255),
                2,
                Imgproc.LINE_AA,
                false);

            // Copy Mat pixels into the reusable Texture2D for UI display.
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

            // First valid FrameMat defines size after rotation/flip are applied.
            RecreatePreviewTexture();

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Clear();
                UpdateFpsMonitorPlaybackState();
                _fpsMonitor.Add("DeviceName", _webCamTextureToMatHelper.DeviceName);
                _fpsMonitor.Add("Width", _webCamTextureToMatHelper.Width.ToString());
                _fpsMonitor.Add("Height", _webCamTextureToMatHelper.Height.ToString());
                _fpsMonitor.Add("FPS", _webCamTextureToMatHelper.FPS.ToString());
                _fpsMonitor.Add("IsFrontFacing", _webCamTextureToMatHelper.IsFrontFacing.ToString());
                _fpsMonitor.Add("RequestedUseAsyncGPUReadback", _webCamTextureToMatHelper.RequestedUseAsyncGPUReadback.ToString());
                _fpsMonitor.Add("EffectiveUseAsyncGPUReadback", _webCamTextureToMatHelper.EffectiveUseAsyncGPUReadback.ToString());
                _fpsMonitor.Add("Rotate90Degree", _webCamTextureToMatHelper.Rotate90Degree.ToString());
                _fpsMonitor.Add("FlipVertical", _webCamTextureToMatHelper.FlipVertical.ToString());
                _fpsMonitor.Add("FlipHorizontal", _webCamTextureToMatHelper.FlipHorizontal.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
                _fpsMonitor.ConsoleText = string.Empty;
            }

            // Call Play only when the helper is not already playing or paused.
            // Re-initialization (e.g. resolution change) may restore the previous playback state.
            if (!_webCamTextureToMatHelper.IsPlaying && !_webCamTextureToMatHelper.IsPaused)
            {
                _webCamTextureToMatHelper.Play();
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

            // Raised when Rotate90 or output size changes; recreate the preview texture to match FrameMat layout.
            RecreatePreviewTexture();

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("Width", _webCamTextureToMatHelper.Width.ToString());
                _fpsMonitor.Add("Height", _webCamTextureToMatHelper.Height.ToString());
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

            // Destroy the preview Texture2D; the helper retains ownership of FrameMat.
            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed", this);

            // Destroy the texture and set it to null.
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
            // Stop and dispose before leaving the scene (required on WebGL to release the camera).
            if (_webCamTextureToMatHelper.IsPlaying || _webCamTextureToMatHelper.IsPaused)
            {
                await _webCamTextureToMatHelper.StopAsync();
            }

            await _webCamTextureToMatHelper.DisposeAsync();

            // Load the main menu scene when the back button is clicked.
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

        // Private Methods
        private void RecreatePreviewTexture()
        {
            Mat imageMat = _webCamTextureToMatHelper.FrameMat;
            if (imageMat == null)
            {
                return;
            }

            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
            }

            // Texture dimensions must match Mat cols()/rows() (width/height may swap when rotated).
            _texture = new Texture2D(imageMat.cols(), imageMat.rows(), TextureFormat.RGBA32, false);

            // Convert the Mat to a Texture2D, effectively transferring the image data.
            OpenCVMatUnityUtils.MatToTexture2D(imageMat, _texture);

            if (ResultPreview != null)
            {
                // Set the Texture2D as the texture of the RawImage for preview.
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
            DestroyPreviewTexture();

            UpdateFpsMonitorPlaybackState();
        }

        private void DestroyPreviewTexture()
        {
            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
                _texture = null;
            }
        }

        private void UpdateFpsMonitorPlaybackState()
        {
            if (_fpsMonitor == null)
            {
                return;
            }

            _fpsMonitor.Add("PlaybackState", GetPlaybackStateText());
            _fpsMonitor.Add("EffectiveUseAsyncGPUReadback", _webCamTextureToMatHelper.EffectiveUseAsyncGPUReadback.ToString());
        }

        private string GetPlaybackStateText()
        {
            if (!_webCamTextureToMatHelper.IsInitialized)
            {
                return "Uninitialized";
            }

            if (_webCamTextureToMatHelper.IsPlaying)
            {
                return "Playing";
            }

            if (_webCamTextureToMatHelper.IsPaused)
            {
                return "Paused";
            }

            return "Ready";
        }

        private void WireSourceToMatControlPanelHooks()
        {
            // Runtime wiring for control-panel callbacks (Inspector does not wire these).
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
            _controlPanel = null;
        }
    }
}

#endif // !OPENCV_DONT_USE_WEBCAMTEXTURE_API
