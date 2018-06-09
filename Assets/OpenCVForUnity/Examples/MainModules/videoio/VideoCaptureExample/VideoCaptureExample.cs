using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// VideoCapture Example
    /// An example of playing a video file using the VideoCapture class.
    /// http://docs.opencv.org/3.2.0/dd/d43/tutorial_py_video_display.html
    /// </summary>
    public class VideoCaptureExample : MonoBehaviour
    {
        /// <summary>
        /// The videocapture.
        /// </summary>
        VideoCapture capture;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        Mat rgbMat;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// Indicates whether the video frame needs updating.
        /// </summary>
        bool shouldUpdateVideoFrame = false;

        /// <summary>
        /// The prev frame tick count.
        /// </summary>
        long prevFrameTickCount;

        /// <summary>
        /// The current frame tick count.
        /// </summary>
        long currentFrameTickCount;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        #if UNITY_WEBGL && !UNITY_EDITOR
        Stack<IEnumerator> coroutines = new Stack<IEnumerator> ();
        #endif
        
        // Use this for initialization
        void Start ()
        {
            fpsMonitor = GetComponent<FpsMonitor> ();

            capture = new VideoCapture ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            var getFilePath_Coroutine = Utils.getFilePathAsync("768x576_mjpeg.mjpeg", (result) => {
                coroutines.Clear ();

                capture.open (result);
                Initialize ();
            });
            coroutines.Push (getFilePath_Coroutine);
            StartCoroutine (getFilePath_Coroutine);
            #else
            capture.open (Utils.getFilePath ("768x576_mjpeg.mjpeg"));

            Initialize ();
            #endif
        }

        private void Initialize ()
        {
            rgbMat = new Mat ();

            if (!capture.isOpened ()) {
                Debug.LogError ("capture.isOpened() is false. Please copy from “OpenCVForUnity/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
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

            if (fpsMonitor != null) {
                fpsMonitor.Add ("CAP_PROP_FORMAT", capture.get (Videoio.CAP_PROP_FORMAT).ToString ());
                fpsMonitor.Add ("CV_CAP_PROP_PREVIEW_FORMAT", capture.get (Videoio.CV_CAP_PROP_PREVIEW_FORMAT).ToString ());
                fpsMonitor.Add ("CAP_PROP_POS_MSEC", capture.get (Videoio.CAP_PROP_POS_MSEC).ToString ());
                fpsMonitor.Add ("CAP_PROP_POS_FRAMES", capture.get (Videoio.CAP_PROP_POS_FRAMES).ToString ());
                fpsMonitor.Add ("CAP_PROP_POS_AVI_RATIO", capture.get (Videoio.CAP_PROP_POS_AVI_RATIO).ToString ());
                fpsMonitor.Add ("CAP_PROP_FRAME_COUNT", capture.get (Videoio.CAP_PROP_FRAME_COUNT).ToString ());
                fpsMonitor.Add ("CAP_PROP_FPS", capture.get (Videoio.CAP_PROP_FPS).ToString ());
                fpsMonitor.Add ("CAP_PROP_FRAME_WIDTH", capture.get (Videoio.CAP_PROP_FRAME_WIDTH).ToString ());
                fpsMonitor.Add ("CAP_PROP_FRAME_HEIGHT", capture.get (Videoio.CAP_PROP_FRAME_HEIGHT).ToString ());
                fpsMonitor.Add ("CAP_PROP_FOURCC", ((char)((int)ext & 0XFF) + (char)(((int)ext & 0XFF00) >> 8) + (char)(((int)ext & 0XFF0000) >> 16) + (char)(((int)ext & 0XFF000000) >> 24)).ToString ());
                fpsMonitor.Add ("STATE", "");
            }

            capture.grab ();
            capture.retrieve (rgbMat, 0);
            int frameWidth = rgbMat.cols ();
            int frameHeight = rgbMat.rows ();
            texture = new Texture2D (frameWidth, frameHeight, TextureFormat.RGB24, false);
            gameObject.transform.localScale = new Vector3 ((float)frameWidth, (float)frameHeight, 1);
            float widthScale = (float)Screen.width / (float)frameWidth;
            float heightScale = (float)Screen.height / (float)frameHeight;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = ((float)frameWidth * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = (float)frameHeight / 2;
            }
            capture.set (Videoio.CAP_PROP_POS_FRAMES, 0);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            StartCoroutine ("WaitFrameTime");
        }

        // Update is called once per frame
        void Update ()
        {            
            if (shouldUpdateVideoFrame) {
                shouldUpdateVideoFrame = false;

                //Loop play
                if (capture.get (Videoio.CAP_PROP_POS_FRAMES) >= capture.get (Videoio.CAP_PROP_FRAME_COUNT))
                    capture.set (Videoio.CAP_PROP_POS_FRAMES, 0);

                if (capture.grab ()) {

                    capture.retrieve (rgbMat, 0);

                    Imgproc.cvtColor (rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);

                    if (fpsMonitor != null) {
                        fpsMonitor.Add ("CAP_PROP_POS_MSEC", capture.get (Videoio.CAP_PROP_POS_MSEC).ToString ());
                        fpsMonitor.Add ("CAP_PROP_POS_FRAMES", capture.get (Videoio.CAP_PROP_POS_FRAMES).ToString ());
                        fpsMonitor.Add ("CAP_PROP_POS_AVI_RATIO", capture.get (Videoio.CAP_PROP_POS_AVI_RATIO).ToString ());
                        fpsMonitor.Add ("CAP_PROP_FRAME_COUNT", capture.get (Videoio.CAP_PROP_FRAME_COUNT).ToString ());
                        int msec = (int)Math.Round (1000.0 * (currentFrameTickCount - prevFrameTickCount) / Core.getTickFrequency ());
                        int fps = (int)Math.Round (1000.0 / msec);
                        fpsMonitor.Add ("STATE", msec + "ms " + " (" + fps + "fps)");
                    }
                
                    Utils.fastMatToTexture2D (rgbMat, texture);
                }
            }
        }

        private IEnumerator WaitFrameTime ()
        {
            double videoFPS = (capture.get (Videoio.CAP_PROP_FPS) <= 0) ? 10.0 : capture.get (Videoio.CAP_PROP_FPS);
            int frameTime_msec = (int)Math.Round (1000.0 / videoFPS);

            while (true) {
                shouldUpdateVideoFrame = true;

                prevFrameTickCount = currentFrameTickCount;
                currentFrameTickCount = Core.getTickCount ();

                yield return new WaitForSeconds (frameTime_msec / 1000f);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            StopCoroutine ("WaitFrameTime");

            capture.release ();

            if (rgbMat != null)
                rgbMat.Dispose ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            foreach (var coroutine in coroutines) {
                StopCoroutine (coroutine);
                ((IDisposable)coroutine).Dispose ();
            }
            #endif
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
    }
}