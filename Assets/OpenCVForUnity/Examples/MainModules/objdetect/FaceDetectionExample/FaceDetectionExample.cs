using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Face Detection Example
    /// An example of human face detection using the CascadeClassifier class.
    /// http://docs.opencv.org/3.2.0/db/d28/tutorial_cascade_classifier.html
    /// </summary>
    public class FaceDetectionExample : MonoBehaviour
    {
        CascadeClassifier cascade;

        /// <summary>
        /// HAAR_CASCADE_FILENAME
        /// </summary>
        protected static readonly string HAAR_CASCADE_FILENAME = "haarcascade_frontalface_alt.xml";

        #if UNITY_WEBGL && !UNITY_EDITOR
        IEnumerator getFilePath_Coroutine;
        #endif

        // Use this for initialization
        void Start ()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            getFilePath_Coroutine = Utils.getFilePathAsync (HAAR_CASCADE_FILENAME, 
                (result) => {
                    getFilePath_Coroutine = null;

                    cascade = new CascadeClassifier ();
                    cascade.load (result);
                    if (cascade.empty ()) {
                        Debug.LogError ("cascade file is not loaded. Please copy from “OpenCVForUnity/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
                    }
           
                    Run ();
                }, 
                (result, progress) => {
                    Debug.Log ("getFilePathAsync() progress : " + result + " " + Mathf.CeilToInt (progress * 100) + "%");
                });
            StartCoroutine (getFilePath_Coroutine);
            #else
            //cascade = new CascadeClassifier (Utils.getFilePath ("lbpcascade_frontalface.xml"));
            cascade = new CascadeClassifier ();
            cascade.load (Utils.getFilePath (HAAR_CASCADE_FILENAME));
            if (cascade.empty ()) {
                Debug.LogError ("cascade file is not loaded. Please copy from “OpenCVForUnity/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }
            Run ();
            #endif
        }

        private void Run ()
        {
            Texture2D imgTexture = Resources.Load ("lena") as Texture2D;

            Mat imgMat = new Mat (imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            Utils.texture2DToMat (imgTexture, imgMat);
            Debug.Log ("imgMat.ToString() " + imgMat.ToString ());


            Mat grayMat = new Mat ();
            Imgproc.cvtColor (imgMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
            Imgproc.equalizeHist (grayMat, grayMat);


            MatOfRect faces = new MatOfRect ();

            if (cascade != null)
                cascade.detectMultiScale (grayMat, faces, 1.1, 2, 2, 
                    new Size (20, 20), new Size ());

            OpenCVForUnity.CoreModule.Rect[] rects = faces.toArray ();
            for (int i = 0; i < rects.Length; i++) {
                Debug.Log ("detect faces " + rects [i]);

                Imgproc.rectangle (imgMat, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (255, 0, 0, 255), 2);
            }


            Texture2D texture = new Texture2D (imgMat.cols (), imgMat.rows (), TextureFormat.RGBA32, false);

            Utils.matToTexture2D (imgMat, texture);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
        }

        // Update is called once per frame
        void Update ()
        {

        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
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