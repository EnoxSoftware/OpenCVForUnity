using System;
using System.Collections;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.VideoioModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// VideoCapture Example
    /// An example of playing a video file using the VideoCapture class.
    /// http://docs.opencv.org/3.2.0/dd/d43/tutorial_py_video_display.html
    /// </summary>
    public class VideoCaptureExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// VIDEO_FILENAME
        /// </summary>
        protected static readonly string VIDEO_FILENAME = "OpenCVForUnityExamples/768x576_mjpeg.mjpeg";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        /// <summary>
        /// The seek bar slider.
        /// </summary>
        public Slider SeekBarSlider;

        // Private Fields
        private Slider.SliderEvent _defaultSliderEvent = new Slider.SliderEvent();

        /// <summary>
        /// The videocapture.
        /// </summary>
        private VideoCapture _capture;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        private Mat _rgbMat;

        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// Indicates whether the video is playing.
        /// </summary>
        private bool _isPlaying = false;

        /// <summary>
        /// Indicates whether the video frame needs updating.
        /// </summary>
        private bool _shouldUpdateVideoFrame = false;

        /// <summary>
        /// The prev frame tick count.
        /// </summary>
        private long _prevFrameTickCount;

        /// <summary>
        /// The current frame tick count.
        /// </summary>
        private long _currentFrameTickCount;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _capture = new VideoCapture();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            var video_filepath = await OpenCVEnv.GetFilePathTaskAsync(VIDEO_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            _capture.open(video_filepath);
            Initialize();
        }

        private void Update()
        {
            if (_isPlaying && _shouldUpdateVideoFrame)
            {
                _shouldUpdateVideoFrame = false;

                //Loop play
                if (_capture.get(Videoio.CAP_PROP_POS_FRAMES) >= _capture.get(Videoio.CAP_PROP_FRAME_COUNT))
                    _capture.set(Videoio.CAP_PROP_POS_FRAMES, 0);

                if (_capture.grab())
                {
                    _capture.retrieve(_rgbMat);

                    Imgproc.cvtColor(_rgbMat, _rgbMat, Imgproc.COLOR_BGR2RGB);

                    if (_fpsMonitor != null)
                    {
                        _fpsMonitor.Add("CAP_PROP_POS_MSEC", _capture.get(Videoio.CAP_PROP_POS_MSEC).ToString());
                        _fpsMonitor.Add("CAP_PROP_POS_FRAMES", _capture.get(Videoio.CAP_PROP_POS_FRAMES).ToString());
                        _fpsMonitor.Add("CAP_PROP_POS_AVI_RATIO", _capture.get(Videoio.CAP_PROP_POS_AVI_RATIO).ToString());
                        _fpsMonitor.Add("CAP_PROP_FRAME_COUNT", _capture.get(Videoio.CAP_PROP_FRAME_COUNT).ToString());
                        int msec = (int)Math.Round(1000.0 * (_currentFrameTickCount - _prevFrameTickCount) / Core.getTickFrequency());
                        int fps = (int)Math.Round(1000.0 / msec);
                        _fpsMonitor.Add("STATE", msec + "ms (" + fps + "fps)");
                    }

                    OpenCVMatUtils.MatToTexture2D(_rgbMat, _texture);

                    var tmp = SeekBarSlider.onValueChanged;
                    SeekBarSlider.onValueChanged = _defaultSliderEvent;
                    SeekBarSlider.value = (float)_capture.get(Videoio.CAP_PROP_POS_AVI_RATIO);
                    SeekBarSlider.onValueChanged = tmp;
                }
            }
        }

        private void OnDestroy()
        {
            StopCoroutine("WaitFrameTime");

            _capture?.release();

            _rgbMat?.Dispose();

            _cts?.Dispose();
        }

        // Public Methods
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
            _isPlaying = true;
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            _isPlaying = false;
        }

        /// <summary>
        /// Raises the jump ahead button click event.
        /// </summary>
        public void OnJumpAheadButtonClick()
        {
            int courentFrame = (int)_capture.get(Videoio.CAP_PROP_POS_FRAMES) + 50;
            if (courentFrame >= _capture.get(Videoio.CAP_PROP_FRAME_COUNT))
            {
                courentFrame = (int)_capture.get(Videoio.CAP_PROP_FRAME_COUNT);
            }

            _capture.set(Videoio.CAP_PROP_POS_FRAMES, courentFrame);
        }

        /// <summary>
        /// Raises the jump back button click event.
        /// </summary>
        public void OnJumpBackButtonClick()
        {
            int courentFrame = (int)_capture.get(Videoio.CAP_PROP_POS_FRAMES) - 50;
            if (courentFrame <= 0)
            {
                courentFrame = 0;
            }

            _capture.set(Videoio.CAP_PROP_POS_FRAMES, courentFrame);
        }

        /// <summary>
        /// Raises the seek bar slider value changed event.
        /// </summary>
        public void OnSeekBarSliderValueChanged()
        {
            bool supported = _capture.set(Videoio.CAP_PROP_POS_AVI_RATIO, SeekBarSlider.value);

            if (!supported)
            {
                _capture.set(Videoio.CAP_PROP_POS_FRAMES, (int)(SeekBarSlider.value * _capture.get(Videoio.CAP_PROP_FRAME_COUNT)));
            }
        }

        // Private Methods
        private void Initialize()
        {
            _rgbMat = new Mat();

            if (!_capture.isOpened())
            {
                Debug.LogError(VIDEO_FILENAME + " is not opened. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
            }

            Debug.Log("CAP_PROP_FORMAT: " + _capture.get(Videoio.CAP_PROP_FORMAT));
            Debug.Log("CAP_PROP_POS_MSEC: " + _capture.get(Videoio.CAP_PROP_POS_MSEC));
            Debug.Log("CAP_PROP_POS_FRAMES: " + _capture.get(Videoio.CAP_PROP_POS_FRAMES));
            Debug.Log("CAP_PROP_POS_AVI_RATIO: " + _capture.get(Videoio.CAP_PROP_POS_AVI_RATIO));
            Debug.Log("CAP_PROP_FRAME_COUNT: " + _capture.get(Videoio.CAP_PROP_FRAME_COUNT));
            Debug.Log("CAP_PROP_FPS: " + _capture.get(Videoio.CAP_PROP_FPS));
            Debug.Log("CAP_PROP_FRAME_WIDTH: " + _capture.get(Videoio.CAP_PROP_FRAME_WIDTH));
            Debug.Log("CAP_PROP_FRAME_HEIGHT: " + _capture.get(Videoio.CAP_PROP_FRAME_HEIGHT));
            double ext = _capture.get(Videoio.CAP_PROP_FOURCC);
            Debug.Log("CAP_PROP_FOURCC: " + (char)((int)ext & 0XFF) + (char)(((int)ext & 0XFF00) >> 8) + (char)(((int)ext & 0XFF0000) >> 16) + (char)(((int)ext & 0XFF000000) >> 24));

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("CAP_PROP_FORMAT", _capture.get(Videoio.CAP_PROP_FORMAT).ToString());
                _fpsMonitor.Add("CAP_PROP_POS_MSEC", _capture.get(Videoio.CAP_PROP_POS_MSEC).ToString());
                _fpsMonitor.Add("CAP_PROP_POS_FRAMES", _capture.get(Videoio.CAP_PROP_POS_FRAMES).ToString());
                _fpsMonitor.Add("CAP_PROP_POS_AVI_RATIO", _capture.get(Videoio.CAP_PROP_POS_AVI_RATIO).ToString());
                _fpsMonitor.Add("CAP_PROP_FRAME_COUNT", _capture.get(Videoio.CAP_PROP_FRAME_COUNT).ToString());
                _fpsMonitor.Add("CAP_PROP_FPS", _capture.get(Videoio.CAP_PROP_FPS).ToString());
                _fpsMonitor.Add("CAP_PROP_FRAME_WIDTH", _capture.get(Videoio.CAP_PROP_FRAME_WIDTH).ToString());
                _fpsMonitor.Add("CAP_PROP_FRAME_HEIGHT", _capture.get(Videoio.CAP_PROP_FRAME_HEIGHT).ToString());
                _fpsMonitor.Add("CAP_PROP_FOURCC", "" + (char)((int)ext & 0XFF) + (char)(((int)ext & 0XFF00) >> 8) + (char)(((int)ext & 0XFF0000) >> 16) + (char)(((int)ext & 0XFF000000) >> 24));
                _fpsMonitor.Add("STATE", "");
            }

            _capture.grab();
            _capture.retrieve(_rgbMat);

            _texture = new Texture2D(_rgbMat.cols(), _rgbMat.rows(), TextureFormat.RGB24, false);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;

            _capture.set(Videoio.CAP_PROP_POS_FRAMES, 0);

            StartCoroutine("WaitFrameTime");

            _isPlaying = true;
        }

        private IEnumerator WaitFrameTime()
        {
            double videoFPS = (_capture.get(Videoio.CAP_PROP_FPS) <= 0) ? 10.0 : _capture.get(Videoio.CAP_PROP_FPS);
            float frameTime_sec = (float)(1000.0 / videoFPS / 1000.0);
            WaitForSeconds wait = new WaitForSeconds(frameTime_sec);
            _prevFrameTickCount = _currentFrameTickCount = Core.getTickCount();

            _capture.grab();

            while (true)
            {
                if (_isPlaying)
                {
                    _shouldUpdateVideoFrame = true;

                    _prevFrameTickCount = _currentFrameTickCount;
                    _currentFrameTickCount = Core.getTickCount();

                    yield return wait;
                }
                else
                {
                    yield return null;
                }
            }
        }
    }
}
