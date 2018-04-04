using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using OpenCVForUnity;
using UnityEngine.UI;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace OpenCVForUnitySample
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
        /// The preview colors.
        /// </summary>
        Color32[] previewColors;

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
        void Start ()
        {
            PlayButton.interactable = false;
            previewPanel.gameObject.SetActive (false);

            Initialize ();
        }

        private void Initialize ()
        {
            Texture2D imgTexture = Resources.Load ("lena") as Texture2D;

            Mat imgMat = new Mat (imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            Utils.texture2DToMat (imgTexture, imgMat);

            Texture2D texture = new Texture2D (imgMat.cols (), imgMat.rows (), TextureFormat.RGBA32, false);

            Utils.matToTexture2D (imgMat, texture);

            cube.GetComponent<Renderer> ().material.mainTexture = texture;
        }
        
        // Update is called once per frame
        void Update ()
        {
            if (!isPlaying) {
                cube.transform.Rotate (new Vector3 (90, 90, 0) * Time.deltaTime, Space.Self);
            }

            if (isPlaying) {
                //Loop play
                if (capture.get (Videoio.CAP_PROP_POS_FRAMES) >= capture.get (Videoio.CAP_PROP_FRAME_COUNT))
                    capture.set (Videoio.CAP_PROP_POS_FRAMES, 0);

                if (capture.grab ())
                {
                    capture.retrieve (previewRgbMat, 0);

                    Imgproc.rectangle (previewRgbMat, new Point(0,0), new Point(previewRgbMat.cols(),previewRgbMat.rows()), new Scalar (0, 0, 255), 3);

                    Imgproc.cvtColor (previewRgbMat, previewRgbMat, Imgproc.COLOR_BGR2RGB);
                    Utils.matToTexture2D (previewRgbMat, previrwTexture, previewColors);
                }
            }
        }

        void OnPostRender()
        {
            if (isRecording)
            {
                if (frameCount >= maxframeCount ||
                    recordingFrameRgbMat.width() != Screen.width || recordingFrameRgbMat.height() != Screen.height)
                {
                    OnRecButtonClick ();
                    return;
                }

                frameCount++;

                // Take screen shot.
                screenCapture.ReadPixels(new UnityEngine.Rect(0, 0, Screen.width, Screen.height), 0, 0);
                screenCapture.Apply();

                Utils.texture2DToMat (screenCapture, recordingFrameRgbMat);
                Imgproc.cvtColor (recordingFrameRgbMat, recordingFrameRgbMat, Imgproc.COLOR_RGB2BGR);

                Imgproc.putText (recordingFrameRgbMat, frameCount.ToString(), new Point (recordingFrameRgbMat.cols() - 70, 30), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText (recordingFrameRgbMat, "SavePath:",new Point (5, recordingFrameRgbMat.rows () - 30), Core.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar (0, 0, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText (recordingFrameRgbMat, savePath, new Point (5, recordingFrameRgbMat.rows () - 8), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255), 0, Imgproc.LINE_AA, false);

                writer.write (recordingFrameRgbMat);
            }
        }

        private void StartRecording (string savePath)
        {
            if (isRecording || isPlaying)
                return;

            this.savePath = savePath;

            writer = new VideoWriter ();
            writer.open (savePath, VideoWriter.fourcc ('M', 'J', 'P', 'G'), 30, new OpenCVForUnity.Size (Screen.width, Screen.height));

            if (!writer.isOpened ()) {
                Debug.LogError ("writer.isOpened() false");
                writer.release ();
                return;
            }

            screenCapture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            recordingFrameRgbMat = new Mat (Screen.height, Screen.width, CvType.CV_8UC3);
            frameCount = 0;

            isRecording = true;
        }

        private void StopRecording ()
        {
            if (!isRecording || isPlaying)
                return;

            if (writer != null && !writer.IsDisposed)
                writer.release ();

            if (recordingFrameRgbMat != null && !recordingFrameRgbMat.IsDisposed)
                recordingFrameRgbMat.Dispose ();

            savePathInputField.text = savePath;

            isRecording = false;
        }

        private void PlayVideo (string filePath)
        {
            if (isPlaying || isRecording)
                return;

            capture = new VideoCapture ();
            capture.open (filePath);

            if (!capture.isOpened ()) {
                Debug.LogError ("capture.isOpened() is false. ");
                capture.release ();
                return;
            }

            Debug.Log ("CAP_PROP_FORMAT: " + capture.get (Videoio.CAP_PROP_FORMAT));
            Debug.Log ("CV_CAP_PROP_PREVIEW_FORMAT: " + capture.get (Videoio.CV_CAP_PROP_PREVIEW_FORMAT));
            Debug.Log ("CAP_PROP_POS_MSEC: " + capture.get (Videoio.CAP_PROP_POS_MSEC));
            Debug.Log ("CAP_PROP_POS_FRAMES: " + capture.get (Videoio.CAP_PROP_POS_FRAMES));
            Debug.Log ("CAP_PROP_POS_AVI_RATIO: " + capture.get (Videoio.CAP_PROP_POS_AVI_RATIO));
            Debug.Log ("CAP_PROP_FRAME_COUNT: " + capture.get (Videoio.CAP_PROP_FRAME_COUNT));
            Debug.Log ("CAP_PROP_FPS: " + capture.get (Videoio.CAP_PROP_FPS));
            Debug.Log ("CAP_PROP_FRAME_WIDTH: " + capture.get (Videoio.CAP_PROP_FRAME_WIDTH));
            Debug.Log ("CAP_PROP_FRAME_HEIGHT: " + capture.get (Videoio.CAP_PROP_FRAME_HEIGHT));
            double ext = capture.get (Videoio.CAP_PROP_FOURCC);
            Debug.Log ("CAP_PROP_FOURCC: " + (char)((int)ext & 0XFF) + (char)(((int)ext & 0XFF00) >> 8) + (char)(((int)ext & 0XFF0000) >> 16) + (char)(((int)ext & 0XFF000000) >> 24));


            previewRgbMat = new Mat ();
            capture.grab ();

            // OpenCV => 3.4.1
            // The AVI container implementation in the videoio module appears, to me, to have a bug. 
            // https://github.com/opencv/opencv/issues/11126
            // https://github.com/opencv/opencv/pull/11146
            Utils.setDebugMode (true);
            capture.retrieve (previewRgbMat, 0);
            Utils.setDebugMode (false);

            return;
            //

            int frameWidth = previewRgbMat.cols ();
            int frameHeight = previewRgbMat.rows ();
            previewColors = new Color32[frameWidth * frameHeight];
            previrwTexture = new Texture2D (frameWidth, frameHeight, TextureFormat.RGB24, false);

            capture.set (Videoio.CAP_PROP_POS_FRAMES, 0);

            previewPanel.texture = previrwTexture;

            isPlaying = true;
        }

        private void StopVideo ()
        {
            if (!isPlaying || isRecording)
                return;

            if (capture != null && !capture.IsDisposed)
                capture.release ();

            if (previewRgbMat != null && !previewRgbMat.IsDisposed)
                previewRgbMat.Dispose ();

            isPlaying = false;
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            StopRecording ();
            StopVideo ();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("OpenCVForUnityExample");
            #else
            Application.LoadLevel ("OpenCVForUnityExample");
            #endif
        }

        /// <summary>
        /// Raises the rec button click event.
        /// </summary>
        public void OnRecButtonClick ()
        {
            if (isRecording) {
                RecButton.GetComponentInChildren<UnityEngine.UI.Text>().color = Color.black;
                StopRecording ();
                PlayButton.interactable = true;
                previewPanel.gameObject.SetActive (false);
            } else {
                RecButton.GetComponentInChildren<UnityEngine.UI.Text>().color = Color.red;
                StartRecording (Application.persistentDataPath + "/VideoWriterExample_output.avi");
                PlayButton.interactable = false;
            }
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick ()
        {
            if (isPlaying) {
                StopVideo ();
                PlayButton.GetComponentInChildren<UnityEngine.UI.Text>().text = "Play";
                RecButton.interactable = true;
                previewPanel.gameObject.SetActive (false);
            } else {
                if (string.IsNullOrEmpty (savePath))
                    return;
                
                PlayVideo (savePath);
                PlayButton.GetComponentInChildren<UnityEngine.UI.Text>().text = "Stop";
                RecButton.interactable = false;
                previewPanel.gameObject.SetActive (true);
            }
        }
    }
}