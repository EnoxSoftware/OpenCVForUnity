using System.IO;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.VideoioModule;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// VideoWriter Example
    /// An example of saving a video file using the VideoWriter class.
    /// http://docs.opencv.org/3.2.0/dd/d43/tutorial_py_video_display.html
    /// </summary>
    public class VideoWriterExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// The max frame count.
        /// </summary>
        private const int MAX_FRAME_COUNT = 300;

        // Public Fields
        /// <summary>
        /// The full screen capture
        /// </summary>
        [HeaderAttribute("Capture Settings")]
        [TooltipAttribute("When checked, the entire screen screen is captured.")]
        public bool FullScreenCapture = true;

        /// <summary>
        /// The capture rect
        /// </summary>
        [TooltipAttribute("The four values indicate which area of the screen screen is to be captured (value 0-1).")]
        public UnityEngine.Rect CaptureRect = new UnityEngine.Rect(0.05f, 0.05f, 0.95f, 0.95f);

        /// <summary>
        /// The cube.
        /// </summary>
        [Space(10)]
        public GameObject Cube;

        /// <summary>
        /// The canvas.
        /// </summary>
        public Canvas Canvas;

        /// <summary>
        /// The capture rect panel.
        /// </summary>
        public RawImage CaptureRectPanel;

        /// <summary>
        /// The preview panel.
        /// </summary>
        public RawImage PreviewPanel;

        /// <summary>
        /// The rec button.
        /// </summary>
        public Button RecButton;

        /// <summary>
        /// The play button.
        /// </summary>
        public Button PlayButton;

        /// <summary>
        /// The save path input field.
        /// </summary>
        public InputField SavePathInputField;

        // Private Fields
        /// <summary>
        /// The frame count.
        /// </summary>
        private int _frameCount;

        /// <summary>
        /// The videowriter.
        /// </summary>
        private VideoWriter _writer;

        /// <summary>
        /// The videocapture.
        /// </summary>
        private VideoCapture _capture;

        /// <summary>
        /// The screen capture.
        /// </summary>
        private Texture2D _screenCapture;

        /// <summary>
        /// The recording frame rgb mat.
        /// </summary>
        private Mat _recordingFrameRgbMat;

        /// <summary>
        /// The preview rgb mat.
        /// </summary>
        private Mat _previewRgbMat;

        /// <summary>
        /// The preview texture.
        /// </summary>
        private Texture2D _previewTexture;

        /// <summary>
        /// Indicates whether videowriter is recording.
        /// </summary>
        private bool _isRecording;

        /// <summary>
        /// Indicates whether videocapture is playing.
        /// </summary>
        private bool _isPlaying;

        /// <summary>
        /// The save path.
        /// </summary>
        private string _savePath;

        /// <summary>
        /// The capture rect pixel
        /// </summary>
        private UnityEngine.Rect _captureRectPixel;

#if UNITY_EDITOR
        private RectTransform _canvasRectTransform;

        private Vector2 _canvasSizeDelta;

        private void Awake()
        {
            _canvasRectTransform = Canvas.GetComponent<RectTransform>();
            _canvasSizeDelta = _canvasRectTransform.sizeDelta;
        }

        private void OnValidate()
        {
            UnityEditor.EditorApplication.update += OnValidateImpl;
        }

        private void OnValidateImpl()
        {
            UnityEditor.EditorApplication.update -= OnValidateImpl;
            if (this == null) return;

            CaptureRect = new UnityEngine.Rect(Mathf.Clamp(CaptureRect.x, 0, 1), Mathf.Clamp(CaptureRect.y, 0, 1), Mathf.Clamp(CaptureRect.width, 0, 1), Mathf.Clamp(CaptureRect.height, 0, 1));

            SetCaptureRectPanel();
        }

        private void SetCaptureRectPanel()
        {
            if (_canvasRectTransform == null) _canvasRectTransform = Canvas.GetComponent<RectTransform>();

            Vector2 uGuiScreenSize = _canvasRectTransform.sizeDelta;
            if (FullScreenCapture)
            {
                CaptureRectPanel.rectTransform.offsetMin = new Vector2(0, 0);
                CaptureRectPanel.rectTransform.offsetMax = new Vector2(0, 0);
            }
            else
            {
                CaptureRectPanel.rectTransform.offsetMin = new Vector2(uGuiScreenSize.x * CaptureRect.x, uGuiScreenSize.y * (1.0f - CaptureRect.height));
                CaptureRectPanel.rectTransform.offsetMax = new Vector2(-uGuiScreenSize.x * (1.0f - CaptureRect.width), -uGuiScreenSize.y * CaptureRect.y);
            }
        }
#endif

        // Unity Lifecycle Methods
        private void Start()
        {
#if UNITY_EDITOR
            SetCaptureRectPanel();
#endif

            PlayButton.interactable = false;
            CaptureRectPanel.gameObject.SetActive(true);
            PreviewPanel.gameObject.SetActive(false);

            Initialize();

            // for URP and HDRP
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (_canvasSizeDelta.x != _canvasRectTransform.sizeDelta.x || _canvasSizeDelta.y != _canvasRectTransform.sizeDelta.y)
            {
                _canvasSizeDelta = _canvasRectTransform.sizeDelta;
                SetCaptureRectPanel();
            }
#endif

            if (!_isPlaying)
            {
                Cube.transform.Rotate(new Vector3(90, 90, 0) * Time.deltaTime, Space.Self);
            }

            if (_isPlaying)
            {
                //Loop play
                if (_capture.get(Videoio.CAP_PROP_POS_FRAMES) >= _capture.get(Videoio.CAP_PROP_FRAME_COUNT))
                    _capture.set(Videoio.CAP_PROP_POS_FRAMES, 0);

                if (_capture.grab())
                {
                    _capture.retrieve(_previewRgbMat);

                    Imgproc.rectangle(_previewRgbMat, new Point(0, 0), new Point(_previewRgbMat.cols(), _previewRgbMat.rows()), new Scalar(0, 0, 255), 3);

                    Imgproc.cvtColor(_previewRgbMat, _previewRgbMat, Imgproc.COLOR_BGR2RGB);
                    OpenCVMatUtils.MatToTexture2D(_previewRgbMat, _previewTexture);
                }
            }
        }

        // for URP and HDRP
        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            OnPostRender();
        }

        private void OnPostRender()
        {
            if (_isRecording)
            {
                if (_frameCount >= MAX_FRAME_COUNT)
                {
                    Debug.LogError("Recording was stopped because the maxframeCount was exceeded.");
                    OnRecButtonClick();
                    return;
                }
                if (_recordingFrameRgbMat.width() > Screen.width || _recordingFrameRgbMat.height() > Screen.height)
                {
                    Debug.LogError("Recording was stopped because the screen size was larger than the recording area.");
                    OnRecButtonClick();
                    return;
                }

                // Take screen shot.
                _screenCapture.ReadPixels(_captureRectPixel, 0, 0);
                _screenCapture.Apply();

                OpenCVMatUtils.Texture2DToMat(_screenCapture, _recordingFrameRgbMat);
                Imgproc.cvtColor(_recordingFrameRgbMat, _recordingFrameRgbMat, Imgproc.COLOR_RGB2BGR);

                Imgproc.putText(_recordingFrameRgbMat, _frameCount.ToString(), new Point(_recordingFrameRgbMat.cols() - 70, 30), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(_recordingFrameRgbMat, "SavePath:", new Point(5, _recordingFrameRgbMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar(0, 0, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(_recordingFrameRgbMat, _savePath, new Point(5, _recordingFrameRgbMat.rows() - 8), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 255, 255), 0, Imgproc.LINE_AA, false);

                _writer.write(_recordingFrameRgbMat);

                _frameCount++;
            }
        }

        private void OnDestroy()
        {
            // for URP and HDRP
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

            StopRecording();
            StopVideo();
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
        /// Raises the rec button click event.
        /// </summary>
        public void OnRecButtonClick()
        {
            if (_isRecording)
            {
                RecButton.GetComponentInChildren<UnityEngine.UI.Text>().color = Color.black;
                StopRecording();
                PlayButton.interactable = true;
                CaptureRectPanel.gameObject.SetActive(true);
                PreviewPanel.gameObject.SetActive(false);
                OnPlayButtonClick();
            }
            else
            {
                StartRecording(Application.persistentDataPath + "/VideoWriterExample_output.avi");

                if (_isRecording)
                {
                    RecButton.GetComponentInChildren<UnityEngine.UI.Text>().color = Color.red;
                    CaptureRectPanel.gameObject.SetActive(false);
                    PlayButton.interactable = false;
                }
            }
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            if (_isPlaying)
            {
                StopVideo();
                PlayButton.GetComponentInChildren<UnityEngine.UI.Text>().text = "Play";
                RecButton.interactable = true;
                CaptureRectPanel.gameObject.SetActive(true);
                PreviewPanel.gameObject.SetActive(false);
            }
            else
            {
                if (string.IsNullOrEmpty(_savePath))
                    return;

                PlayVideo(_savePath);
                PlayButton.GetComponentInChildren<UnityEngine.UI.Text>().text = "Stop";
                RecButton.interactable = false;
                CaptureRectPanel.gameObject.SetActive(false);
                PreviewPanel.gameObject.SetActive(true);
            }
        }

        // Private Methods
        private void Initialize()
        {
            Texture2D imgTexture = Resources.Load("face") as Texture2D;

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            OpenCVMatUtils.Texture2DToMat(imgTexture, imgMat);

            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUtils.MatToTexture2D(imgMat, texture);

            Cube.GetComponent<Renderer>().material.mainTexture = texture;
        }

        private void StartRecording(string savePath)
        {
            if (_isRecording || _isPlaying)
                return;

            _savePath = savePath;

            if (File.Exists(savePath))
            {
                Debug.Log("Delete " + savePath);
                File.Delete(savePath);
            }

            if (FullScreenCapture)
            {
                _captureRectPixel = new UnityEngine.Rect(0, 0, Screen.width, Screen.height);
            }
            else
            {
                _captureRectPixel = new UnityEngine.Rect(Screen.width * CaptureRect.x, Screen.height * CaptureRect.y, Screen.width * (CaptureRect.width - CaptureRect.x), Screen.height * (CaptureRect.height - CaptureRect.y));

                if (_captureRectPixel.x < 0 || _captureRectPixel.y < 0 || _captureRectPixel.width > Screen.width || _captureRectPixel.height > Screen.height || _captureRectPixel.width < 1 || _captureRectPixel.height < 1)
                {
                    Debug.LogError("Since the captureRect is larger than the screen size, the value of captureRect is changed to the screen size.");
                    _captureRectPixel = new UnityEngine.Rect(0, 0, Screen.width, Screen.height);
                }
            }
            //Debug.Log("captureRectPixel " + captureRectPixel);

            _writer = new VideoWriter();

            _writer.open(savePath, Videoio.CAP_OPENCV_MJPEG, VideoWriter.fourcc('M', 'J', 'P', 'G'), 30, new Size((int)_captureRectPixel.width, (int)_captureRectPixel.height));

            if (!_writer.isOpened())
            {
                Debug.LogError("writer.isOpened() false");
                _writer.release();
                return;
            }

            _screenCapture = new Texture2D((int)_captureRectPixel.width, (int)_captureRectPixel.height, TextureFormat.RGB24, false);
            _recordingFrameRgbMat = new Mat((int)_captureRectPixel.height, (int)_captureRectPixel.width, CvType.CV_8UC3);
            _frameCount = 0;

            _isRecording = true;
        }

        private void StopRecording()
        {
            if (!_isRecording || _isPlaying)
                return;

            _writer?.release();

            if (_screenCapture != null) Texture2D.Destroy(_screenCapture); _screenCapture = null;

            _recordingFrameRgbMat?.Dispose();

            SavePathInputField.text = _savePath;

            _isRecording = false;
        }

        private void PlayVideo(string filePath)
        {
            if (_isPlaying || _isRecording)
                return;

            _capture = new VideoCapture();
            _capture.open(filePath, Videoio.CAP_OPENCV_MJPEG);

            if (!_capture.isOpened())
            {
                Debug.LogError("capture.isOpened() is false. ");
                _capture.release();
                return;
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

            _previewRgbMat = new Mat();
            _capture.read(_previewRgbMat);

            int frameWidth = _previewRgbMat.cols();
            int frameHeight = _previewRgbMat.rows();
            _previewTexture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGB24, false);

            _capture.set(Videoio.CAP_PROP_POS_FRAMES, 0);

            PreviewPanel.texture = _previewTexture;
            PreviewPanel.rectTransform.localScale = new Vector3((_captureRectPixel.width / (float)Screen.width) * 0.8f, (_captureRectPixel.height / (float)Screen.height) * 0.8f, 1f);

            _isPlaying = true;
        }

        private void StopVideo()
        {
            if (!_isPlaying || _isRecording)
                return;

            _capture?.release();

            _previewRgbMat?.Dispose();

            _isPlaying = false;
        }
    }
}
