using System;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.Extensions.SourceToMat.DerivedFrame;
using OpenCVForUnity.GeometryModule;
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
    /// Polygon Filter Example
    /// Applies a low-poly effect by Delaunay triangulation and flat color fills per triangle on each input frame.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Edge detection on a downscaled GRAY <see cref="DerivedFrame"/> output for performance
    /// - Edge detection and random point sampling for triangulation vertices
    /// - Subdiv2D Delaunay triangulation with boundary anchor points
    /// - fillConvexPoly using the source color at each triangle centroid
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Point"/>, <see cref="Scalar"/>, <see cref="Size"/>, <see cref="Rect"/>
    /// - <see cref="Subdiv2D"/>, <see cref="MatOfFloat6"/>, <see cref="MatOfPoint"/>
    /// - <see cref="Core"/>: split, merge
    /// - <see cref="Imgproc"/>: blur, filter2D, threshold, fillConvexPoly
    /// - <see cref="MatBufferUtils"/>, <see cref="MultiSourceToMatHelper"/>, <see cref="IDerivedFrame"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// http://jsdo.it/hedger/tIod
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class PolygonFilterExample : MonoBehaviour
    {
        // Constants
        private const int EDGE_DETECT_VALUE = 70;

        private const double POINT_RATE = 0.075;

        private const int POINT_MAX_NUM = 2500;

        private const string DERIVED_FRAME_NAME = "edge-gray";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Header("Derived Frame (edge detection)")]
        [SerializeField]
        [Tooltip("Spatial scale for the edge-detection derived frame (1/3 matches the former DownscaleRatio of 3).")]
        private float _derivedScaleRatio = 1f / 3f;

        [SerializeField]
        [Tooltip("Recompute the derived frame every N source updates (2 matches the former FrameSkippingRatio of 2).")]
        private int _derivedProcessEveryNFrames = 2;

        [Space(10)]

        // Private Fields
        private Texture2D _texture;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private IDerivedFrame _derivedFrame;
        private Mat _gray1Mat;
        private Mat _gray2Mat;
        private Mat _kernel;
        private byte[] _byteArray;
        private Subdiv2D _subdiv;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();

            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            ApplyDerivedFrameSettings();

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

            if (_derivedFrame == null || !_derivedFrame.DidUpdateThisFrame)
            {
                return;
            }

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;
            Mat derivedGrayMat = _derivedFrame.FrameMat;
            if (rgbaMat == null || derivedGrayMat == null || _gray1Mat == null)
            {
                return;
            }

            // Edge detection runs on the downscaled GRAY derived frame (see DerivedFrameSettings).
            derivedGrayMat.copyTo(_gray1Mat);

            //blur
            Imgproc.blur(_gray1Mat, _gray2Mat, new Size(5, 5));

            //edge filter
            Imgproc.filter2D(_gray2Mat, _gray1Mat, _gray1Mat.depth(), _kernel);

            //blur
            Imgproc.blur(_gray1Mat, _gray2Mat, new Size(3, 3));

            //detect edge
            Imgproc.threshold(_gray2Mat, _gray2Mat, EDGE_DETECT_VALUE, 255, Imgproc.THRESH_BINARY);

            //copy Mat to byteArray
            MatBufferUtils.CopyFromMat<byte>(_gray2Mat, _byteArray);

            //set edge pointList
            List<Point> pointList = new List<Point>();
            int w = _gray1Mat.width();
            int h = _gray1Mat.height();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (_byteArray[x + w * y] == 255)
                    {
                        pointList.Add(new Point(x, y));
                    }
                }
            }

            int limit = Mathf.RoundToInt((float)(pointList.Count * POINT_RATE));
            if (limit > POINT_MAX_NUM)
            {
                limit = POINT_MAX_NUM;
            }

            while (pointList.Count > limit)
            {
                pointList.RemoveAt(UnityEngine.Random.Range(0, pointList.Count));
            }
            //Debug.Log ("pointList.Count " + pointList.Count);

            //init subdiv (Subdiv2D requires boundary points for stable triangles at image edges).
            _subdiv.initDelaunay(new OpenCVForUnity.CoreModule.Rect(0, 0, w, h));
            for (int i = 0; i < pointList.Count; i++)
            {
                _subdiv.insert(pointList[i]);
            }
            _subdiv.insert(new Point(0, 0));
            _subdiv.insert(new Point(w / 2 - 1, 0));
            _subdiv.insert(new Point(w - 1, 0));
            _subdiv.insert(new Point(w - 1, h / 2 - 1));
            _subdiv.insert(new Point(w - 1, h - 1));
            _subdiv.insert(new Point(w / 2 - 1, h - 1));
            _subdiv.insert(new Point(0, h - 1));
            _subdiv.insert(new Point(0, h / 2 - 1));

            using (MatOfFloat6 triangleList = new MatOfFloat6())
            {
                _subdiv.getTriangleList(triangleList);

                float[] pointArray = triangleList.toArray();
                float downScaleRatio = GetDownScaleRatio(rgbaMat);

                byte[] color = new byte[4];
                for (int i = 0; i < pointArray.Length / 6; i++)
                {

                    Point p0 = new Point(pointArray[i * 6 + 0] * downScaleRatio, pointArray[i * 6 + 1] * downScaleRatio);
                    Point p1 = new Point(pointArray[i * 6 + 2] * downScaleRatio, pointArray[i * 6 + 3] * downScaleRatio);
                    Point p2 = new Point(pointArray[i * 6 + 4] * downScaleRatio, pointArray[i * 6 + 5] * downScaleRatio);

                    if (p0.x < 0 || p0.x > rgbaMat.width())
                    {
                        continue;
                    }

                    if (p0.y < 0 || p0.y > rgbaMat.height())
                    {
                        continue;
                    }

                    if (p1.x < 0 || p1.x > rgbaMat.width())
                    {
                        continue;
                    }

                    if (p1.y < 0 || p1.y > rgbaMat.height())
                    {
                        continue;
                    }

                    if (p2.x < 0 || p2.x > rgbaMat.width())
                    {
                        continue;
                    }

                    if (p2.y < 0 || p2.y > rgbaMat.height())
                    {
                        continue;
                    }

                    //get center of gravity
                    int cx = (int)((p0.x + p1.x + p2.x) * 0.33333);
                    int cy = (int)((p0.y + p1.y + p2.y) * 0.33333);
                    //                Debug.Log ("cx " + cx + " cy " + cy );

                    //get center of gravity color from the full-resolution source Mat.
                    rgbaMat.get(cy, cx, color);
                    //Debug.Log ("r " + color[0] + " g " + color[1] + " b " + color[2] + " a " + color[3]);

                    //fill Polygon
                    Imgproc.fillConvexPoly(rgbaMat, new MatOfPoint(p0, p1, p2), new Scalar(color[0], color[1], color[2], color[3]), Imgproc.LINE_AA, 0);

                    //Imgproc.line (rgbaMat, p0, p1, new Scalar (64, 255, 128, 255));
                    //Imgproc.line (rgbaMat, p1, p2, new Scalar (64, 255, 128, 255));
                    //Imgproc.line (rgbaMat, p2, p0, new Scalar (64, 255, 128, 255));
                }
            }

            //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " DOWNSCALE W:" + downScaleRgbaMat.width () + " H:" + downScaleRgbaMat.height (), new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

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

            // Fill in the image so that the unprocessed image is not displayed.
            rgbaMat.setTo(new Scalar(0, 0, 0, 255));

            TryCacheDerivedFrame();
            RecreatePreviewTexture();
            CreateOrRecreateProcessingResources();

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
            TryCacheDerivedFrame();
            CreateOrRecreateProcessingResources();

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
            CreateOrRecreateProcessingResources();
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
            _derivedFrame = null;
        }

        /// <summary>
        /// Raises the helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed", this);

            DisposeFrameProcessingResources();
            CleanupPreviewResources();
            _derivedFrame = null;
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

        // Private Methods
        private void ApplyDerivedFrameSettings()
        {
            _multiSourceToMatHelper.DerivedFrameSettings = new[]
            {
                new DerivedFrameInspectorSettings
                {
                    Enabled = true,
                    Name = DERIVED_FRAME_NAME,
                    ScaleRatio = _derivedScaleRatio,
                    UseSourceOutputColorFormat = false,
                    OutputColorFormat = SourceToMatColorFormat.GRAY,
                    ProcessEveryNFrames = _derivedProcessEveryNFrames,
                },
            };
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

        private float GetDownScaleRatio(Mat rgbaMat)
        {
            if (_derivedFrame == null || _derivedFrame.Width <= 0)
            {
                return 1f;
            }

            float downScaleRatio = (float)rgbaMat.width() / _derivedFrame.Width;
            if (downScaleRatio < 1f)
            {
                downScaleRatio = 1f;
            }

            return downScaleRatio;
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

        private void DisposeFrameProcessingResources()
        {
            _gray1Mat?.Dispose();
            _gray1Mat = null;
            _gray2Mat?.Dispose();
            _gray2Mat = null;
            _kernel?.Dispose();
            _kernel = null;
            _subdiv?.Dispose();
            _subdiv = null;
            _byteArray = null;
        }

        private void CreateOrRecreateProcessingResources()
        {
            if (_derivedFrame?.FrameMat == null)
            {
                return;
            }

            DisposeFrameProcessingResources();

            Mat derivedGrayMat = _derivedFrame.FrameMat;
            int downWidth = derivedGrayMat.width();
            int downHeight = derivedGrayMat.height();

            _gray1Mat = new Mat(downHeight, downWidth, CvType.CV_8UC1);
            _gray2Mat = new Mat(downHeight, downWidth, CvType.CV_8UC1);

            int ksize = 7;
            float[] kernelData = new float[ksize * ksize];
            for (int i = 0; i < kernelData.Length; i++)
            {
                if (i == kernelData.Length / 2)
                {
                    kernelData[i] = (-(kernelData.Length - 1));
                }
                else
                {
                    kernelData[i] = 1;
                }
            }

            _kernel = new Mat(ksize, ksize, CvType.CV_32F);
            _kernel.put(0, 0, kernelData);

            _byteArray = new byte[downWidth * downHeight];

            _subdiv = new Subdiv2D();
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
