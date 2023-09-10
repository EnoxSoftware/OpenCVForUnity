using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.VideoioModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System.IO;
using UnityEngine.Rendering;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// VideoWriter Example
    /// An example of saving a video file using the VideoWriter class.
    /// http://docs.opencv.org/3.2.0/dd/d43/tutorial_py_video_display.html
    /// </summary>
    public class VideoWriterExample : MonoBehaviour
    {
        /// <summary>
        /// The cube.
        /// </summary>
        public GameObject cube;

        /// <summary>
        /// The preview panel.
        /// </summary>
        public RawImage previewPanel;

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
        public InputField savePathInputField;

        /// <summary>
        /// The max frame count.
        /// </summary>
        const int maxframeCount = 300;

        /// <summary>
        /// The frame count.
        /// </summary>
        int frameCount;

        /// <summary>
        /// The videowriter.
        /// </summary>
        VideoWriter writer;

        /// <summary>
        /// The videocapture.
        /// </summary>
        VideoCapture capture;

        /// <summary>
        /// The screen capture.
        /// </summary>
        Texture2D screenCapture;

        /// <summary>
        /// The recording frame rgb mat.
        /// </summary>
        Mat recordingFrameRgbMat;

        /// <summary>
        /// The preview rgb mat.
        /// </summary>
        Mat previewRgbMat;

        /// <summary>
        /// The preview texture.
        /// </summary>
        Texture2D previrwTexture;

        /// <summary>
        /// Indicates whether videowriter is recording.
        /// </summary>
        bool isRecording;

        /// <summary>
        /// Indicates whether videocapture is playing.
        /// </summary>
        bool isPlaying;

        /// <summary>
        /// The save path.
        /// </summary>
        string savePath;

        // Use this for initialization
        void Start()
        {
            PlayButton.interactable = false;
            previewPanel.gameObject.SetActive(false);

            Initialize();

            // for URP and HDRP
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void Initialize()
        {
            Texture2D imgTexture = Resources.Load("face") as Texture2D;

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            Utils.texture2DToMat(imgTexture, imgMat);

            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);

            Utils.matToTexture2D(imgMat, texture);

            cube.GetComponent<Renderer>().material.mainTexture = texture;
        }

        // Update is called once per frame
        void Update()
        {
            if (!isPlaying)
            {
                cube.transform.Rotate(new Vector3(90, 90, 0) * Time.deltaTime, Space.Self);
            }

            if (isPlaying)
            {
                //Loop play
                if (capture.get(Videoio.CAP_PROP_POS_FRAMES) >= capture.get(Videoio.CAP_PROP_FRAME_COUNT))
                    capture.set(Videoio.CAP_PROP_POS_FRAMES, 0);

                if (capture.grab())
                {
                    capture.retrieve(previewRgbMat);

                    Imgproc.rectangle(previewRgbMat, new Point(0, 0), new Point(previewRgbMat.cols(), previewRgbMat.rows()), new Scalar(0, 0, 255), 3);

                    Imgproc.cvtColor(previewRgbMat, previewRgbMat, Imgproc.COLOR_BGR2RGB);
                    Utils.matToTexture2D(previewRgbMat, previrwTexture);
                }
            }
        }

        // for URP and HDRP
        void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            OnPostRender();
        }

        void OnPostRender()
        {
            if (isRecording)
            {
                if (frameCount >= maxframeCount)
                {
                    Debug.LogError("Recording was stopped because the maxframeCount was exceeded.");
                    OnRecButtonClick();
                    return;
                }
                if (recordingFrameRgbMat.width() != Screen.width || recordingFrameRgbMat.height() != Screen.height)
                {
                    Debug.LogError("Please fix the screen ratio of the Game View to recognize the recording area.");
                    OnRecButtonClick();
                    return;
                }

                // Take screen shot.
                screenCapture.ReadPixels(new UnityEngine.Rect(0, 0, Screen.width, Screen.height), 0, 0);
                screenCapture.Apply();

                Utils.texture2DToMat(screenCapture, recordingFrameRgbMat);
                Imgproc.cvtColor(recordingFrameRgbMat, recordingFrameRgbMat, Imgproc.COLOR_RGB2BGR);

                Imgproc.putText(recordingFrameRgbMat, frameCount.ToString(), new Point(recordingFrameRgbMat.cols() - 70, 30), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(recordingFrameRgbMat, "SavePath:", new Point(5, recordingFrameRgbMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar(0, 0, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(recordingFrameRgbMat, savePath, new Point(5, recordingFrameRgbMat.rows() - 8), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 255, 255), 0, Imgproc.LINE_AA, false);

                writer.write(recordingFrameRgbMat);

                frameCount++;
            }
        }

        private void StartRecording(string savePath)
        {
            if (isRecording || isPlaying)
                return;

            this.savePath = savePath;

            if (File.Exists(savePath))
            {
                Debug.Log("Delete " + savePath);
                File.Delete(savePath);
            }

            writer = new VideoWriter();
#if !UNITY_IOS
            writer.open(savePath, VideoWriter.fourcc('M', 'J', 'P', 'G'), 30, new Size(Screen.width, Screen.height));
#else
            writer.open(savePath, VideoWriter.fourcc('D', 'V', 'I', 'X'), 30, new Size(Screen.width, Screen.height));
#endif

            if (!writer.isOpened())
            {
                Debug.LogError("writer.isOpened() false");
                writer.release();
                return;
            }

            screenCapture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            recordingFrameRgbMat = new Mat(Screen.height, Screen.width, CvType.CV_8UC3);
            frameCount = 0;

            isRecording = true;
        }

        private void StopRecording()
        {
            if (!isRecording || isPlaying)
                return;

            if (writer != null && !writer.IsDisposed)
                writer.release();

            if (screenCapture != null)
            {
                Texture2D.Destroy(screenCapture);
                screenCapture = null;
            }

            if (recordingFrameRgbMat != null && !recordingFrameRgbMat.IsDisposed)
                recordingFrameRgbMat.Dispose();

            savePathInputField.text = savePath;

            isRecording = false;
        }

        private void PlayVideo(string filePath)
        {
            if (isPlaying || isRecording)
                return;

            capture = new VideoCapture();
            capture.open(filePath);

            if (!capture.isOpened())
            {
                Debug.LogError("capture.isOpened() is false. ");
                capture.release();
                return;
            }

            Debug.Log("CAP_PROP_FORMAT: " + capture.get(Videoio.CAP_PROP_FORMAT));
            Debug.Log("CAP_PROP_POS_MSEC: " + capture.get(Videoio.CAP_PROP_POS_MSEC));
            Debug.Log("CAP_PROP_POS_FRAMES: " + capture.get(Videoio.CAP_PROP_POS_FRAMES));
            Debug.Log("CAP_PROP_POS_AVI_RATIO: " + capture.get(Videoio.CAP_PROP_POS_AVI_RATIO));
            Debug.Log("CAP_PROP_FRAME_COUNT: " + capture.get(Videoio.CAP_PROP_FRAME_COUNT));
            Debug.Log("CAP_PROP_FPS: " + capture.get(Videoio.CAP_PROP_FPS));
            Debug.Log("CAP_PROP_FRAME_WIDTH: " + capture.get(Videoio.CAP_PROP_FRAME_WIDTH));
            Debug.Log("CAP_PROP_FRAME_HEIGHT: " + capture.get(Videoio.CAP_PROP_FRAME_HEIGHT));
            double ext = capture.get(Videoio.CAP_PROP_FOURCC);
            Debug.Log("CAP_PROP_FOURCC: " + (char)((int)ext & 0XFF) + (char)(((int)ext & 0XFF00) >> 8) + (char)(((int)ext & 0XFF0000) >> 16) + (char)(((int)ext & 0XFF000000) >> 24));


            previewRgbMat = new Mat();
            capture.read(previewRgbMat);

            int frameWidth = previewRgbMat.cols();
            int frameHeight = previewRgbMat.rows();
            previrwTexture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGB24, false);

            capture.set(Videoio.CAP_PROP_POS_FRAMES, 0);

            previewPanel.texture = previrwTexture;

            isPlaying = true;
        }

        private void StopVideo()
        {
            if (!isPlaying || isRecording)
                return;

            if (capture != null && !capture.IsDisposed)
                capture.release();

            if (previewRgbMat != null && !previewRgbMat.IsDisposed)
                previewRgbMat.Dispose();

            isPlaying = false;
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            // for URP and HDRP
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

            StopRecording();
            StopVideo();
        }

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
            if (isRecording)
            {
                RecButton.GetComponentInChildren<UnityEngine.UI.Text>().color = Color.black;
                StopRecording();
                PlayButton.interactable = true;
                previewPanel.gameObject.SetActive(false);
            }
            else
            {
#if !UNITY_IOS
                StartRecording(Application.persistentDataPath + "/VideoWriterExample_output.avi");
#else
                StartRecording(Application.persistentDataPath + "/VideoWriterExample_output.m4v");
#endif

                if (isRecording)
                {
                    RecButton.GetComponentInChildren<UnityEngine.UI.Text>().color = Color.red;
                    PlayButton.interactable = false;
                }
            }
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            if (isPlaying)
            {
                StopVideo();
                PlayButton.GetComponentInChildren<UnityEngine.UI.Text>().text = "Play";
                RecButton.interactable = true;
                previewPanel.gameObject.SetActive(false);
            }
            else
            {
                if (string.IsNullOrEmpty(savePath))
                    return;

                PlayVideo(savePath);
                PlayButton.GetComponentInChildren<UnityEngine.UI.Text>().text = "Stop";
                RecButton.interactable = false;
                previewPanel.gameObject.SetActive(true);
            }
        }
    }
}