#if !UNITY_WSA_10_0

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Xml;

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
    public class TextRecognitionExample : MonoBehaviour
    {
        string scenetext01_jpg_filepath;
        string trained_classifierNM1_xml_filepath;
        string trained_classifierNM2_xml_filepath;
        string OCRHMM_transitions_table_xml_filepath;
        string OCRHMM_knn_model_data_xml_gz_filepath;

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
            scenetext01_jpg_filepath = Utils.getFilePath ("text/test_text.jpg");
            trained_classifierNM1_xml_filepath = Utils.getFilePath ("text/trained_classifierNM1.xml");
            trained_classifierNM2_xml_filepath = Utils.getFilePath ("text/trained_classifierNM2.xml");
            OCRHMM_transitions_table_xml_filepath = Utils.getFilePath ("text/OCRHMM_transitions_table.xml");
            #if UNITY_ANDROID && !UNITY_EDITOR
            OCRHMM_knn_model_data_xml_gz_filepath = Utils.getFilePath ("text/OCRHMM_knn_model_data.xml");
            #else
            OCRHMM_knn_model_data_xml_gz_filepath = Utils.getFilePath ("text/OCRHMM_knn_model_data.xml.gz");
            #endif
            Run ();
            #endif
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator GetFilePath ()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync ("text/test_text.jpg", (result) => {
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
            var getFilePathAsync_3_Coroutine = Utils.getFilePathAsync ("text/OCRHMM_transitions_table.xml", (result) => {
                OCRHMM_transitions_table_xml_filepath = result;
            });
            coroutines.Push (getFilePathAsync_3_Coroutine);
            yield return StartCoroutine (getFilePathAsync_3_Coroutine);
            //Please strip ".gz" when using ".gz" file on WebGL platform.
            var getFilePathAsync_4_Coroutine = Utils.getFilePathAsync ("text/OCRHMM_knn_model_data.xml", (result) => {
                OCRHMM_knn_model_data_xml_gz_filepath = result;
            });
            coroutines.Push (getFilePathAsync_4_Coroutine);
            yield return StartCoroutine (getFilePathAsync_4_Coroutine);

            coroutines.Clear ();

            Run ();
        }
        #endif

        private void Run ()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode (true);


            Mat frame = Imgcodecs.imread (scenetext01_jpg_filepath);
            #if !UNITY_WSA_10_0
            if (frame.empty ()) {
                Debug.LogError ("text/scenetext01.jpg is not loaded.Please copy from “OpenCVForUnity/StreamingAssets/text/” to “Assets/StreamingAssets/” folder. ");
            }
            #endif

            Mat binaryMat = new Mat ();
            Mat maskMat = new Mat ();


            List<MatOfPoint> regions = new List<MatOfPoint> ();

            ERFilter er_filter1 = Text.createERFilterNM1 (trained_classifierNM1_xml_filepath, 8, 0.00015f, 0.13f, 0.2f, true, 0.1f);

            ERFilter er_filter2 = Text.createERFilterNM2 (trained_classifierNM2_xml_filepath, 0.5f);


            Mat transition_p = new Mat (62, 62, CvType.CV_64FC1);
            //            string filename = "OCRHMM_transitions_table.xml";
            //            FileStorage fs(filename, FileStorage::READ);
            //            fs["transition_probabilities"] >> transition_p;
            //            fs.release();

            //Load TransitionProbabilitiesData.
            transition_p.put (0, 0, GetTransitionProbabilitiesData (OCRHMM_transitions_table_xml_filepath));

            Mat emission_p = Mat.eye (62, 62, CvType.CV_64FC1);
            string voc = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            OCRHMMDecoder decoder = OCRHMMDecoder.create (
                                        OCRHMM_knn_model_data_xml_gz_filepath,
                                        voc, transition_p, emission_p);

            //Text Detection
            Imgproc.cvtColor (frame, frame, Imgproc.COLOR_BGR2RGB);
            Imgproc.cvtColor (frame, binaryMat, Imgproc.COLOR_RGB2GRAY);
            Imgproc.threshold (binaryMat, binaryMat, 0, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);
            Core.absdiff (binaryMat, new Scalar (255), maskMat);


            Text.detectRegions (binaryMat, er_filter1, er_filter2, regions);
            Debug.Log ("regions.Count " + regions.Count);


            MatOfRect groups_rects = new MatOfRect ();
            List<OpenCVForUnity.Rect> rects = new List<OpenCVForUnity.Rect> ();
            Text.erGrouping (frame, binaryMat, regions, groups_rects);


            for (int i = 0; i < regions.Count; i++) {
                regions [i].Dispose ();
            }
            regions.Clear ();


            rects.AddRange (groups_rects.toList ());

            groups_rects.Dispose ();


            //Text Recognition (OCR)

            List<Mat> detections = new List<Mat> ();

            for (int i = 0; i < (int)rects.Count; i++) {

                Mat group_img = new Mat ();
                maskMat.submat (rects [i]).copyTo (group_img);
                Core.copyMakeBorder (group_img, group_img, 15, 15, 15, 15, Core.BORDER_CONSTANT, new Scalar (0));
                detections.Add (group_img);
            }

            Debug.Log ("detections.Count " + detections.Count);


            //#Visualization
            for (int i = 0; i < rects.Count; i++) {


                Imgproc.rectangle (frame, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (255, 0, 0), 2);
                Imgproc.rectangle (frame, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (255, 255, 255), 1);

                string output = decoder.run (detections [i], 0);
                if (!string.IsNullOrEmpty (output)) {
                    Debug.Log ("output " + output);
                    Imgproc.putText (frame, output, new Point (rects [i].x, rects [i].y), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (0, 0, 255), 1, Imgproc.LINE_AA, false);
                }
            }


            Texture2D texture = new Texture2D (frame.cols (), frame.rows (), TextureFormat.RGBA32, false);

            Utils.matToTexture2D (frame, texture);

//            Texture2D texture = new Texture2D (detections [0].cols (), detections [0].rows (), TextureFormat.RGBA32, false);
//
//            Utils.matToTexture2D (detections [0], texture);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;


            for (int i = 0; i < detections.Count; i++) {
                detections [i].Dispose ();
            }
            binaryMat.Dispose ();
            maskMat.Dispose ();

            Utils.setDebugMode (false);
        }
    
        // Update is called once per frame
        void Update ()
        {

        }

        /// <summary>
        /// Gets the transition probabilities data.
        /// </summary>
        /// <returns>The transition probabilities data.</returns>
        /// <param name="filePath">File path.</param>
        double[] GetTransitionProbabilitiesData (string filePath)
        {
            XmlDocument xmlDoc = new XmlDocument ();
            xmlDoc.Load (filePath);


            XmlNode dataNode = xmlDoc.GetElementsByTagName ("data").Item (0);
//            Debug.Log ("dataNode.InnerText " + dataNode.InnerText);
            string[] dataString = dataNode.InnerText.Split (new string[] {
                " ",
                "\r\n", "\n"
            }, StringSplitOptions.RemoveEmptyEntries);
//            Debug.Log ("dataString.Length " + dataString.Length);

            double[] data = new double[dataString.Length];
            for (int i = 0; i < data.Length; i++) {
                try {
                    data [i] = Convert.ToDouble (dataString [i]);
                } catch (FormatException) {
                    Debug.Log ("Unable to convert '{" + dataString [i] + "}' to a Double.");
                } catch (OverflowException) {
                    Debug.Log ("'{" + dataString [i] + "}' is outside the range of a Double.");
                }
            }       

            return data;
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