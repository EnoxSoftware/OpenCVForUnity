using System;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.Extensions.SourceToMat.DerivedFrame;
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
    /// MultiSourceToMatHelper Example
    /// Switches between multiple input sources (webcam, video file, image file, or GPU readback) and processes each frame as an OpenCV <see cref="Mat"/>.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Configuring <see cref="MultiSourceToMatHelper"/> per-kind settings via the helper Inspector and <see cref="SourceToMatControlPanel"/>
    /// - Runtime kind switching via the control panel MultiSection
    /// - Receiving frames via <see cref="SourceToMatHelperBase.OnFrameMatUpdated"/> (Inspector wiring)
    /// - Calling <see cref="SourceToMatHelperBase.Play"/> in OnInitialized only when not already playing or paused (<c>!IsPlaying &amp;&amp; !IsPaused</c>)
    /// - Recreating the preview texture on <see cref="SourceToMatHelperBase.OnFrameMatLayoutChanged"/>
    /// - Registering a derived frame via <see cref="MultiSourceToMatHelper.DerivedFrameSettings"/> and
    ///   updating its preview in the same <see cref="SourceToMatHelperBase.OnFrameMatUpdated"/> handler
    /// - Hiding <see cref="DerivedPreview"/> when the derived frame is not registered
    /// - Overlaying frame info with <see cref="Imgproc.putText"/> and measuring Mat update rate with <see cref="FPSCounter"/>
    /// - Displaying output via <see cref="Texture2D"/> or <see cref="RenderTexture"/>
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Scalar"/>, <see cref="Point"/>
    /// - <see cref="Imgproc"/>: putText
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="MultiSourceHelperKind"/>, <see cref="SourceToMatColorFormat"/>
    /// - <see cref="OpenCVMatUnityUtils"/>: MatToTexture2D, MatToRenderTexture
    ///
    /// Unity integration:
    /// - The Mat returned by <see cref="SourceToMatHelperBase.FrameMat"/> is owned by the active helper; do not dispose it
    /// - <see cref="SourceToMatColorFormat.RGBA"/> matches Unity <see cref="TextureFormat.RGBA32"/> for display
    /// - WebGPU forces AsyncGPU inside WebCamTextureToMatHelper; this example does not switch helper types with <c>#if</c>
    /// - Reuse output textures across helper switches to avoid preview flicker
    /// - Helper lifecycle events (OnInitialized, OnFrameMatUpdated, etc.) are wired in the Inspector on <see cref="MultiSourceToMatHelper"/>
    /// - <see cref="SourceToMatControlPanel"/> on the same GameObject provides Transport / Transform / Kind / capability UI
    /// </summary>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class MultiSourceToMatHelperExample : MonoBehaviour
    {
        private const string DERIVED_FRAME_NAME = "gray-downsample";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        /// <summary>
        /// The RawImage for previewing the registered derived frame (half-scale GRAY, every 3rd source frame).
        /// </summary>
        public RawImage DerivedPreview;

        [Space(10)]

        /// <summary>
        /// Whether RenderTexture is used when displaying rgbaMat in the scene; if Off, Texture2D is used.
        /// </summary>
        public Toggle OutputRenderTextureToggle;

        /// <summary>
        /// The cube rotated each frame to show the scene Update loop is running independently of Mat processing.
        /// </summary>
        public GameObject Cube;

        // Private Fields
        private Texture2D _outputTexture2D;
        private Texture2D _derivedTexture2D;
        private RenderTexture _outputRenderTexture;
        private GraphicsBuffer _graphicsBuffer;
        private IDerivedFrame _derivedFrame;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;

        /// <summary>
        /// The FPS counter.
        /// Measure how frequently DidUpdateThisFrame() is actually updated.
        /// </summary>
        private FPSCounter _fpsCounter;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            // Get the MultiSourceToMatHelper component attached to the current game object.
            _multiSourceToMatHelper = GetComponent<MultiSourceToMatHelper>();

            // Per-kind paths and camera settings are configured on the helper Inspector and control panel.
            // RGBA matches Unity TextureFormat.RGBA32 used for RawImage preview.
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            if (OutputRenderTextureToggle != null && !SystemInfo.supportsComputeShaders)
            {
                OutputRenderTextureToggle.interactable = false;
            }

            // Subscribe to SourceToMatControlPanel events (Play/Pause/Stop, Rotate/Flip, HelperKind).
            WireSourceToMatControlPanelHooks();

            // Creates the active child helper and opens the requested input source.
            // OnSourceToMatHelperInitialized is raised when initialization completes (Inspector wiring).
            _multiSourceToMatHelper.Initialize();
        }

        private void Update()
        {
            // Count Mat updates while playing; MatUpdateFPS is shown on FpsMonitor and in the putText overlay.
            if (_fpsCounter != null
                && _multiSourceToMatHelper.IsInitialized
                && _multiSourceToMatHelper.IsPlaying
                && _multiSourceToMatHelper.DidUpdateThisFrame)
            {
                _fpsCounter.MeasureFPS();
            }

            if (_fpsMonitor != null && _fpsCounter != null)
            {
                _fpsMonitor.Add("MatUpdateFPS", _fpsCounter.GetCurrentFPS().ToString("F1"));
            }

            if (Cube != null)
            {
                Cube.transform.Rotate(new Vector3(90, 90, 0) * Time.deltaTime * 0.5f, Space.Self);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe control-panel listeners to avoid dangling callbacks.
            UnwireSourceToMatControlPanelHooks();
        }
        // Public Methods
        /// <summary>
        /// Raises the helper frame mat updated event.
        /// Draws overlays and updates the preview texture when a new frame is available during playback.
        /// </summary>
        public void OnSourceToMatHelperFrameMatUpdated()
        {
            // Invoked by the helper when a new frame is available (replaces Update + DidUpdateThisFrame for Mat processing).
            if (!_multiSourceToMatHelper.IsPlaying)
            {
                return;
            }

            // Returns the helper's internal Mat (RGBA); owned by the active child helper 窶・do not dispose.
            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;
            if (rgbaMat == null)
            {
                return;
            }

            // Draw helper-kind label and frame stats in-place before display conversion.
            Imgproc.putText(
                rgbaMat,
                GetHelperKindOverlayText(),
                new Point(5, 30),
                Imgproc.FONT_HERSHEY_SIMPLEX,
                0.7,
                new Scalar(255, 255, 255, 255),
                2,
                Imgproc.LINE_AA,
                false);

            Imgproc.putText(
                rgbaMat,
                "W:" + rgbaMat.width() + " H:" + rgbaMat.height() + " SO:" + Screen.orientation
                + " MatUpdateFPS:" + (_fpsCounter != null ? _fpsCounter.GetCurrentFPS() : 0f),
                new Point(5, rgbaMat.rows() - 10),
                Imgproc.FONT_HERSHEY_SIMPLEX,
                0.7,
                new Scalar(255, 255, 255, 255),
                2,
                Imgproc.LINE_AA,
                false);

            UpdatePreviewFromFrameMat(rgbaMat);

            if (_derivedFrame != null && _derivedFrame.DidUpdateThisFrame)
            {
                UpdateDerivedPreviewFromFrameMat();
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

            // Retain output textures across helper switches to prevent RawImage flicker.
            ReleasePreviewResources();
            RecreatePreviewTexture();
            TryCacheDerivedFrame();
            RecreateDerivedPreviewTexture();

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Clear();
                UpdateFpsMonitorPlaybackState();
                UpdateFpsMonitorSourceInfo();
                UpdateFpsMonitorDerivedFrame();
            }

            _fpsCounter = new FPSCounter(1.0f);

            // Call Play only when the helper is not already playing or paused.
            // Re-initialization (e.g. kind switch or RenderTexture toggle) may restore the previous playback state.
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

            // Raised when Rotate90 or output size changes; recreate the preview texture to match FrameMat layout.
            RecreatePreviewTexture();
            RecreateDerivedPreviewTexture();

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("Width", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("Height", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
            }
        }

        /// <summary>
        /// Raises the helper derived frame layout changed event.
        /// </summary>
        /// <param name="derivedFrameName">Derived frame name whose output layout changed.</param>
        public void OnSourceToMatHelperDerivedFrameLayoutChanged(string derivedFrameName)
        {
            if (derivedFrameName != DERIVED_FRAME_NAME)
            {
                return;
            }

            Debug.Log("OnSourceToMatHelperDerivedFrameLayoutChanged " + derivedFrameName, this);

            TryCacheDerivedFrame();
            RecreateDerivedPreviewTexture();

            if (_fpsMonitor != null)
            {
                UpdateFpsMonitorDerivedFrame();
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

            _fpsCounter = null;

            // Destroy preview textures; the helper retains ownership of FrameMat.
            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed", this);

            // Destroy preview textures and set references to null.
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
            // Stop and dispose before leaving the scene (required on WebGL to release the active source).
            if (_multiSourceToMatHelper.IsPlaying || _multiSourceToMatHelper.IsPaused)
            {
                await _multiSourceToMatHelper.StopAsync();
            }

            await _multiSourceToMatHelper.DisposeAsync();

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
            _fpsMonitor.Add("ActiveHelper", GetActiveHelperTypeName());
        }

        /// <summary>
        /// Raises the output RenderTexture toggle value changed event.
        /// Re-initializes the helper so the preview path switches between Texture2D and RenderTexture.
        /// </summary>
        public void OnOutputRenderTextureToggleValueChanged()
        {
            if (_multiSourceToMatHelper.IsInitialized)
            {
                _multiSourceToMatHelper.Initialize();
            }
        }
        // Private Methods
        private void RecreatePreviewTexture()
        {
            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;
            if (rgbaMat == null)
            {
                return;
            }

            ReleasePreviewResources();

            if (OutputRenderTextureToggle == null || !OutputRenderTextureToggle.isOn)
            {
                // Texture dimensions must match Mat cols()/rows() (width/height may swap when rotated).
                _outputTexture2D = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);

                // CPU path: convert the Mat to a Texture2D for RawImage preview.
                OpenCVMatUnityUtils.MatToTexture2D(rgbaMat, _outputTexture2D);
                ApplyPreviewTexture(_outputTexture2D);
                return;
            }

            _graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)rgbaMat.total(), (int)rgbaMat.elemSize());
            _outputRenderTexture = new RenderTexture(rgbaMat.width(), rgbaMat.height(), 0);
            _outputRenderTexture.enableRandomWrite = true;
            _outputRenderTexture.Create();

            try
            {
                // GPU path: upload Mat data into a RenderTexture via GraphicsBuffer.
                OpenCVMatUnityUtils.MatToRenderTexture(rgbaMat, _outputRenderTexture, _graphicsBuffer);
            }
            catch (Exception ex)
            {
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.ConsoleText = ex.Message;
                }
            }

            ApplyPreviewTexture(_outputRenderTexture);
        }

        private void CleanupPreviewResources()
        {
            ReleasePreviewResources();

            _derivedFrame = null;
            UpdateFpsMonitorPlaybackState();
        }

        private void ReleasePreviewResources()
        {
            if (_outputTexture2D != null)
            {
                Texture2D.Destroy(_outputTexture2D);
                _outputTexture2D = null;
            }

            if (_outputRenderTexture != null)
            {
                RenderTexture.Destroy(_outputRenderTexture);
                _outputRenderTexture = null;
            }

            if (_graphicsBuffer != null)
            {
                _graphicsBuffer.Dispose();
                _graphicsBuffer = null;
            }

            if (_derivedTexture2D != null)
            {
                Texture2D.Destroy(_derivedTexture2D);
                _derivedTexture2D = null;
            }
        }

        private void UpdateFpsMonitorPlaybackState()
        {
            if (_fpsMonitor == null)
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

        private void UpdatePreviewFromFrameMat(Mat rgbaMat)
        {
            if (OutputRenderTextureToggle == null || !OutputRenderTextureToggle.isOn)
            {
                if (_outputTexture2D != null)
                {
                    // CPU path: copy Mat into a reusable Texture2D for RawImage.
                    OpenCVMatUnityUtils.MatToTexture2D(rgbaMat, _outputTexture2D);
                }

                return;
            }

            if (_outputRenderTexture != null && _graphicsBuffer != null)
            {
                // GPU path: upload Mat data into a reusable RenderTexture.
                OpenCVMatUnityUtils.MatToRenderTexture(rgbaMat, _outputRenderTexture, _graphicsBuffer);
            }
        }

        private void ApplyPreviewTexture(Texture texture)
        {
            if (ResultPreview == null)
            {
                return;
            }

            // Set the texture as the RawImage preview and keep aspect ratio in sync.
            ResultPreview.texture = texture;
            ApplyPreviewAspectRatio(ResultPreview, texture);
        }

        private void RecreateDerivedPreviewTexture()
        {
            if (_derivedFrame?.FrameMat == null)
            {
                return;
            }

            Mat derivedMat = _derivedFrame.FrameMat;

            if (_derivedTexture2D != null)
            {
                Texture2D.Destroy(_derivedTexture2D);
            }

            _derivedTexture2D = new Texture2D(derivedMat.cols(), derivedMat.rows(), TextureFormat.Alpha8, false);
            DrawDerivedFrameOverlay(derivedMat);
            OpenCVMatUnityUtils.MatToTexture2D(derivedMat, _derivedTexture2D);

            if (DerivedPreview != null)
            {
                DerivedPreview.texture = _derivedTexture2D;
                ApplyPreviewAspectRatio(DerivedPreview, _derivedTexture2D);
            }
        }

        private void UpdateDerivedPreviewFromFrameMat()
        {
            Mat derivedMat = _derivedFrame?.FrameMat;
            if (derivedMat != null && _derivedTexture2D != null)
            {
                DrawDerivedFrameOverlay(derivedMat);
                OpenCVMatUnityUtils.MatToTexture2D(derivedMat, _derivedTexture2D);
            }
        }

        /// <summary>
        /// Draws a derived frame label on <paramref name="derivedMat"/> before preview upload.
        /// </summary>
        private void DrawDerivedFrameOverlay(Mat derivedMat)
        {
            if (_derivedFrame == null)
            {
                return;
            }

            Scalar textColor = derivedMat.channels() == 1
                ? new Scalar(0)
                : new Scalar(0, 0, 0, 255);

            double fontScale = Mathf.Max(0.35f, derivedMat.rows() / 480f * 0.45f) * 3;
            int thickness = 5;
            int lineHeight = Mathf.Max(12, (int)(derivedMat.rows() / 480f * 18f)) * 3;

            Imgproc.putText(
                derivedMat,
                "Derived Frame",
                new Point(5, lineHeight),
                Imgproc.FONT_HERSHEY_SIMPLEX,
                fontScale,
                textColor,
                thickness,
                Imgproc.LINE_AA,
                false);
        }

        private void TryCacheDerivedFrame()
        {
            _derivedFrame = null;

            if (_multiSourceToMatHelper.MatSource is IMatSourceDerivedFrames derivedFramesHost)
            {
                try
                {
                    _derivedFrame = derivedFramesHost.DerivedFrames.Get(DERIVED_FRAME_NAME);
                }
                catch (KeyNotFoundException)
                {
                    // Derived frame is not registered in helper settings.
                }
            }
        }

        private static void ApplyPreviewAspectRatio(RawImage preview, Texture texture)
        {
            AspectRatioFitter aspectRatioFitter = preview.GetComponent<AspectRatioFitter>();
            if (aspectRatioFitter != null)
            {
                aspectRatioFitter.aspectRatio = (float)texture.width / texture.height;
            }
        }

        private void UpdateFpsMonitorDerivedFrame()
        {
            if (_fpsMonitor == null)
            {
                return;
            }

            if (_derivedFrame == null)
            {
                _fpsMonitor.Add("[Derived Frame Settings]", "");
                _fpsMonitor.Add("DerivedName", "-");
                _fpsMonitor.Add("DerivedScaleRatio", "-");
                _fpsMonitor.Add("DerivedColorFormat", "-");
                _fpsMonitor.Add("DerivedProcessEveryNFrames", "-");
                _fpsMonitor.Add("DerivedWidth", "-");
                _fpsMonitor.Add("DerivedHeight", "-");
                return;
            }

            float scaleRatio = 0f;
            int processEveryNFrames = 0;
            if (_derivedFrame is IDerivedFrameProcessor processor)
            {
                DerivedFrameSettings settings = processor.Settings;
                scaleRatio = settings.ScaleRatio;
                processEveryNFrames = settings.ProcessEveryNFrames;
            }

            _fpsMonitor.Add("[Derived Frame Settings]", "");
            _fpsMonitor.Add("DerivedName", _derivedFrame.Name);
            _fpsMonitor.Add("DerivedScaleRatio", scaleRatio.ToString("F2"));
            _fpsMonitor.Add("DerivedColorFormat", _derivedFrame.OutputColorFormat.ToString());
            _fpsMonitor.Add("DerivedProcessEveryNFrames", processEveryNFrames.ToString());
            _fpsMonitor.Add("DerivedWidth", _derivedFrame.Width.ToString());
            _fpsMonitor.Add("DerivedHeight", _derivedFrame.Height.ToString());
        }

        private void UpdateFpsMonitorSourceInfo()
        {
            _fpsMonitor.Add("HelperKind", _multiSourceToMatHelper.RequestedHelperKind.ToString());
            _fpsMonitor.Add("ActiveHelper", GetActiveHelperTypeName());
            _fpsMonitor.Add("Width", _multiSourceToMatHelper.Width.ToString());
            _fpsMonitor.Add("Height", _multiSourceToMatHelper.Height.ToString());
            _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
            _fpsMonitor.Add("Rotate90Degree", _multiSourceToMatHelper.Rotate90Degree.ToString());
            _fpsMonitor.Add("FlipVertical", _multiSourceToMatHelper.FlipVertical.ToString());
            _fpsMonitor.Add("FlipHorizontal", _multiSourceToMatHelper.FlipHorizontal.ToString());

            IMatSource matSource = _multiSourceToMatHelper.MatSource;
            if (matSource is ICameraMatSource camera)
            {
                _fpsMonitor.Add("DeviceName", camera.DeviceName);
                _fpsMonitor.Add("FPS", camera.FPS.ToString());
            }

            if (matSource is ICameraFacingControllable facing)
            {
                _fpsMonitor.Add("IsFrontFacing", facing.IsFrontFacing.ToString());
            }

            if (matSource is IVideoFileMatSource video)
            {
                _fpsMonitor.Add("VideoPath", video.RequestedVideoFilePath);
                _fpsMonitor.Add("VideoFPS", video.FPS.ToString());
                _fpsMonitor.Add("Loop", video.Loop.ToString());
            }

            if (matSource is IImageFileMatSource image)
            {
                _fpsMonitor.Add("ImagePath", image.RequestedImageFilePath);
                _fpsMonitor.Add("Repeat", image.Repeat.ToString());
            }

#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API
            if (_multiSourceToMatHelper.ActiveHelper is WebCamTextureToMatHelper webCamHelper)
            {
                _fpsMonitor.Add("RequestedUseAsyncGPUReadback", webCamHelper.RequestedUseAsyncGPUReadback.ToString());
                _fpsMonitor.Add("EffectiveUseAsyncGPUReadback", webCamHelper.EffectiveUseAsyncGPUReadback.ToString());
            }
#endif
        }

        private string GetActiveHelperTypeName()
        {
            SourceToMatHelperBase activeHelper = _multiSourceToMatHelper.ActiveHelper;
            return activeHelper != null ? activeHelper.GetType().Name : "(none)";
        }

        private string GetHelperKindOverlayText()
        {
            switch (_multiSourceToMatHelper.RequestedHelperKind)
            {
                case MultiSourceHelperKind.WebCamTexture:
#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API
                    if (_multiSourceToMatHelper.ActiveHelper is WebCamTextureToMatHelper webCamHelper
                        && webCamHelper.EffectiveUseAsyncGPUReadback)
                    {
                        return "WebCamTexture -> RenderTexture => Mat";
                    }
#endif
                    return "WebCamTexture => Mat";
                case MultiSourceHelperKind.VideoCapture:
                case MultiSourceHelperKind.UnityVideoPlayer:
                    return "Video File => Mat";
                case MultiSourceHelperKind.ImageFile:
                    return "Image File => Mat";
                case MultiSourceHelperKind.AsyncGPUReadback:
                    return "Camera => RenderTexture => Mat";
                default:
                    return _multiSourceToMatHelper.RequestedHelperKind.ToString() + " => Mat";
            }
        }
    }
}
