#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.VideoioModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// HOGDescriptor Example
    /// An example of people detection using the HOGDescriptor class.
    /// </summary>
    public class HOGDescriptorExample : MonoBehaviour
    {
        /// <summary>
        /// The videoCapture.
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
        /// The HOGDescriptor.
        /// </summary>
        HOGDescriptor des;

        /// <summary>
        /// VIDEO_FILENAME
        /// </summary>
        protected static readonly string VIDEO_FILENAME = "768x576_mjpeg.mjpeg";

        #if UNITY_WEBGL && !UNITY_EDITOR
        IEnumerator getFilePath_Coroutine;
        #endif

        // Use this for initialization
        void Start ()
        {
            capture = new VideoCapture ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            getFilePath_Coroutine = Utils.getFilePathAsync(VIDEO_FILENAME, (result) => {
                getFilePath_Coroutine = null;

                capture.open (result);
                Init();
            });
            StartCoroutine (getFilePath_Coroutine);
            #else
            capture.open (Utils.getFilePath (VIDEO_FILENAME));
            Init ();
            #endif
        }

        private void Init ()
        {
            rgbMat = new Mat ();
            
            if (!capture.isOpened ()) {
                Debug.LogError ("capture.isOpened() is true. Please copy from “OpenCVForUnity/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }

            Debug.Log ("CAP_PROP_FORMAT: " + capture.get (Videoio.CAP_PROP_FORMAT));
            Debug.Log ("CAP_PROP_POS_MSEC: " + capture.get (Videoio.CAP_PROP_POS_MSEC));
            Debug.Log ("CAP_PROP_POS_FRAMES: " + capture.get (Videoio.CAP_PROP_POS_FRAMES));
            Debug.Log ("CAP_PROP_POS_AVI_RATIO: " + capture.get (Videoio.CAP_PROP_POS_AVI_RATIO));
            Debug.Log ("CAP_PROP_FRAME_COUNT: " + capture.get (Videoio.CAP_PROP_FRAME_COUNT));
            Debug.Log ("CAP_PROP_FPS: " + capture.get (Videoio.CAP_PROP_FPS));
            Debug.Log ("CAP_PROP_FRAME_WIDTH: " + capture.get (Videoio.CAP_PROP_FRAME_WIDTH));
            Debug.Log ("CAP_PROP_FRAME_HEIGHT: " + capture.get (Videoio.CAP_PROP_FRAME_HEIGHT));


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

            des = new HOGDescriptor ();
        }
        
        // Update is called once per frame
        void Update ()
        {
            //Loop play
            if (capture.get (Videoio.CAP_PROP_POS_FRAMES) >= capture.get (Videoio.CAP_PROP_FRAME_COUNT))
                capture.set (Videoio.CAP_PROP_POS_FRAMES, 0);

            //error PlayerLoop called recursively! on iOS.reccomend WebCamTexture.
            if (capture.grab ()) {

                capture.retrieve (rgbMat, 0);

                Imgproc.cvtColor (rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);
                
                //Debug.Log ("Mat toString " + rgbMat.ToString ());
                                
                using (MatOfRect locations = new MatOfRect ())
                using (MatOfDouble weights = new MatOfDouble ()) {
                    des.setSVMDetector (HOGDescriptor.getDefaultPeopleDetector ());
                    des.detectMultiScale (rgbMat, locations, weights);

                    OpenCVForUnity.CoreModule.Rect[] rects = locations.toArray ();
                    for (int i = 0; i < rects.Length; i++) {
                        //Debug.Log ("detected person " + rects [i]);
                        Imgproc.rectangle (rgbMat, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (255, 0, 0), 2);
                    }
                    //Debug.Log (locations.ToString ());
                    //Debug.Log (weights.ToString ());
                }

                
                Utils.fastMatToTexture2D (rgbMat, texture);
                
                gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            capture.release ();

            if (rgbMat != null)
                rgbMat.Dispose ();

            if (texture != null) {
                Texture2D.Destroy (texture);
                texture = null;
            }

            if (des != null)
                des.Dispose ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            if (getFilePath_Coroutine != null) {
                StopCoroutine (getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose ();
            }
            #endif
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick ()
        {
            SceneManager.LoadScene ("OpenCVForUnityExample");
        }
    }
}

#endif