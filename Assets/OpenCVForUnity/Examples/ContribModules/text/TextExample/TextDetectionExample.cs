#if !UNITY_WSA_10_0

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
    /// Text Detection Example
    /// A demo script of the Extremal Region Filter algorithm described in:Neumann L., Matas J.: Real-Time Scene Text Localization and Recognition, CVPR 2012.
    /// Referring to https://github.com/opencv/opencv_contrib/blob/master/modules/text/samples/textdetection.py.
    /// </summary>
    public class TextDetectionExample : MonoBehaviour
    {
        string scenetext01_jpg_filepath;
        string trained_classifierNM1_xml_filepath;
        string trained_classifierNM2_xml_filepath;

        #if UNITY_WEBGL && !UNITY_EDITOR
        Stack<IEnumerator> coroutines = new Stack<IEnumerator> ();
        #endif

        // Use this for initialization
        void Start ()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            var getFilePath_Coroutine = GetFilePath ();
            coroutines.Push (getFilePath_Coroutine);
            StartCoroutine (getFilePath_Coroutine);
            #else
            scenetext01_jpg_filepath = Utils.getFilePath ("text/scenetext01.jpg");
            trained_classifierNM1_xml_filepath = Utils.getFilePath ("text/trained_classifierNM1.xml");
            trained_classifierNM2_xml_filepath = Utils.getFilePath ("text/trained_classifierNM2.xml");
            Run ();
            #endif
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync ("text/scenetext01.jpg", (result) => {
                scenetext01_jpg_filepath = result;
        });
        coroutines.Push (getFilePathAsync_0_Coroutine);
        yield return StartCoroutine (getFilePathAsync_0_Coroutine);

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync ("text/trained_classifierNM1.xml", (result) => {
                trained_classifierNM1_xml_filepath = result;
        });
        coroutines.Push (getFilePathAsync_1_Coroutine);
        yield return StartCoroutine (getFilePathAsync_1_Coroutine);

            var getFilePathAsync_2_Coroutine = Utils.getFilePathAsync ("text/trained_classifierNM2.xml", (result) => {
                trained_classifierNM2_xml_filepath = result;
        });
        coroutines.Push (getFilePathAsync_2_Coroutine);
        yield return StartCoroutine (getFilePathAsync_2_Coroutine);

        coroutines.Clear ();

        Run ();
        }
        #endif

        private void Run ()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode (true);


            Mat img = Imgcodecs.imread (scenetext01_jpg_filepath);
            #if !UNITY_WSA_10_0
            if (img.empty ()) {
                Debug.LogError ("text/scenetext01.jpg is not loaded.Please copy from “OpenCVForUnity/StreamingAssets/text/” to “Assets/StreamingAssets/” folder. ");
            }
            #endif

            //# for visualization
            Mat vis = new Mat ();
            img.copyTo (vis);
            Imgproc.cvtColor (vis, vis, Imgproc.COLOR_BGR2RGB);
                    
                    
            //# Extract channels to be processed individually
            List<Mat> channels = new List<Mat> ();
            Text.computeNMChannels (img, channels);

            //# Append negative channels to detect ER- (bright regions over dark background)
            int cn = channels.Count;
            for (int i = 0; i < cn; i++) {

                channels.Add (new Scalar (255) - channels [i]);
            }

            //# Apply the default cascade classifier to each independent channel (could be done in parallel)
            Debug.Log ("Extracting Class Specific Extremal Regions from " + channels.Count + " channels ...");
            Debug.Log ("    (...) this may take a while (...)");
            foreach (var channel in channels) {
                ERFilter er1 = Text.createERFilterNM1 (trained_classifierNM1_xml_filepath, 16, 0.00015f, 0.13f, 0.2f, true, 0.1f);

                ERFilter er2 = Text.createERFilterNM2 (trained_classifierNM2_xml_filepath, 0.5f);

                List<MatOfPoint> regions = new List<MatOfPoint> ();
                Text.detectRegions (channel, er1, er2, regions);

                MatOfRect matOfRects = new MatOfRect ();
                Text.erGrouping (img, channel, regions, matOfRects);
//                Text.erGrouping (img, channel, regions, matOfRects, Text.ERGROUPING_ORIENTATION_ANY, Utils.getFilePath ("text/trained_classifier_erGrouping.xml"), 0.5f);
               
                List<OpenCVForUnity.Rect> rects = matOfRects.toList ();

                //#Visualization
                foreach (var rect in rects) {

                    Imgproc.rectangle (vis, new Point (rect.x, rect.y), new Point (rect.x + rect.width, rect.y + rect.height), new Scalar (255, 0, 0), 2);
                    Imgproc.rectangle (vis, new Point (rect.x, rect.y), new Point (rect.x + rect.width, rect.y + rect.height), new Scalar (255, 255, 255), 1);

                }
                  
            }

            Texture2D texture = new Texture2D (vis.cols (), vis.rows (), TextureFormat.RGBA32, false);

            Utils.matToTexture2D (vis, texture);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;


            Utils.setDebugMode (false);
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
#endif