using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Xml;


#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// WebCamTextureTextRecognitionExample Example
    /// </summary>
    [RequireComponent (typeof(WebCamTextureToMatHelper))]
    public class WebCamTextureTextRecognitionExample : MonoBehaviour
    {
        /// <summary>
        /// The flip vertical toggle.
        /// </summary>
        public Toggle flipVerticalToggle;
        
        /// <summary>
        /// The flip horizontal toggle.
        /// </summary>
        public Toggle flipHorizontalToggle;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The binary mat.
        /// </summary>
        Mat binaryMat;

        /// <summary>
        /// The mask mat.
        /// </summary>
        Mat maskMat;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        Mat rgbMat;

        /// <summary>
        /// The er filter1.
        /// </summary>
        ERFilter er_filter1;

        /// <summary>
        /// The er filter2.
        /// </summary>
        ERFilter er_filter2;

        /// <summary>
        /// The decoder.
        /// </summary>
        OCRHMMDecoder decoder;


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
            Utils.setDebugMode (true);


            er_filter1 = OpenCVForUnity.Text.createERFilterNM1 (trained_classifierNM1_xml_filepath, 8, 0.00015f, 0.13f, 0.2f, true, 0.1f);

            er_filter2 = OpenCVForUnity.Text.createERFilterNM2 (trained_classifierNM2_xml_filepath, 0.5f);


            Mat transition_p = new Mat (62, 62, CvType.CV_64FC1);
            //            string filename = "OCRHMM_transitions_table.xml";
            //            FileStorage fs(filename, FileStorage::READ);
            //            fs["transition_probabilities"] >> transition_p;
            //            fs.release();

            //Load TransitionProbabilitiesData.
            transition_p.put (0, 0, GetTransitionProbabilitiesData (OCRHMM_transitions_table_xml_filepath));


            Mat emission_p = Mat.eye (62, 62, CvType.CV_64FC1);
            string voc = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            decoder = OCRHMMDecoder.create (OCRHMM_knn_model_data_xml_gz_filepath,
                voc, transition_p, emission_p);


            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();
            webCamTextureToMatHelper.Initialize ();

            flipVerticalToggle.isOn = webCamTextureToMatHelper.flipVertical;
            flipHorizontalToggle.isOn = webCamTextureToMatHelper.flipHorizontal;
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();

            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

                                    
            float width = webCamTextureMat.width ();
            float height = webCamTextureMat.height ();
                                    
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }


            binaryMat = new Mat ();
            maskMat = new Mat ();
            rgbMat = new Mat ();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

            if (binaryMat != null)
                binaryMat.Dispose ();
            if (maskMat != null)
                maskMat.Dispose ();
            if (rgbMat != null)
                rgbMat.Dispose ();

        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred (WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log ("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);

        }

        // Update is called once per frame
        void Update ()
        {

            if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

                Imgproc.cvtColor (rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);


                /*Text Detection*/
                Imgproc.cvtColor (rgbMat, binaryMat, Imgproc.COLOR_RGB2GRAY);
                Imgproc.threshold (binaryMat, binaryMat, 0, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);
                Core.absdiff (binaryMat, new Scalar (255), maskMat);


                List<MatOfPoint> regions = new List<MatOfPoint> ();


                OpenCVForUnity.Text.detectRegions (binaryMat, er_filter1, er_filter2, regions);
                               
//                Debug.Log ("regions.Count " + regions.Count);

                MatOfRect groups_rects = new MatOfRect ();
                List<OpenCVForUnity.Rect> rects = new List<OpenCVForUnity.Rect> ();
                if (regions.Count > 0) {
                    OpenCVForUnity.Text.erGrouping (rgbMat, binaryMat, regions, groups_rects);
                }


                for (int i = 0; i < regions.Count; i++) {
                    regions [i].Dispose ();
                }
                regions.Clear ();

                rects.AddRange (groups_rects.toList ());

                groups_rects.Dispose ();



                /*Text Recognition (OCR)*/

                List<Mat> detections = new List<Mat> ();

                for (int i = 0; i < (int)rects.Count; i++) {

                    Mat group_img = new Mat ();

                    maskMat.submat (rects [i]).copyTo (group_img);
                    Core.copyMakeBorder (group_img, group_img, 15, 15, 15, 15, Core.BORDER_CONSTANT, new Scalar (0));
                    detections.Add (group_img);

                }

//                Debug.Log ("detections.Count " + detections.Count);

//                Debug.Log ("rects.Count " + rects.Count);

                //#Visualization
                for (int i = 0; i < rects.Count; i++) {

                    Imgproc.rectangle (rgbaMat, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (255, 0, 0, 255), 2);
                    Imgproc.rectangle (rgbaMat, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (255, 255, 255, 255), 1);

                    Imgproc.putText (rgbaMat, "" + i, new Point (rects [i].x, rects [i].y), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 0, 0, 255), 1, Imgproc.LINE_AA, false);
                                      
                }
                    

                for (int i = 0; i < detections.Count; i++) {

                    string output = decoder.run (detections [i], 0);
                    Debug.Log ("output " + output);
                    if (string.IsNullOrEmpty (output)) {
                        Debug.LogError ("IsNullOrEmpty output " + output);
                    } else {
                        Imgproc.putText (rgbaMat, "  " + output, new Point (rects [i].x, rects [i].y), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 0, 0, 255), 1, Imgproc.LINE_AA, false);
                    }
                }

                for (int i = 0; i < detections.Count; i++) {
                    detections [i].Dispose ();
                }
                detections.Clear ();


                Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Utils.matToTexture2D (rgbaMat, texture, webCamTextureToMatHelper.GetBufferColors ());
            }
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
            webCamTextureToMatHelper.Dispose ();

            if (er_filter1 != null)
                er_filter1.Dispose ();
            if (er_filter2 != null)
                er_filter2.Dispose ();
            if (decoder != null)
                decoder.Dispose ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            foreach (var coroutine in coroutines) {
            StopCoroutine (coroutine);
            ((IDisposable)coroutine).Dispose ();
            }
            #endif

            Utils.setDebugMode (false);
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
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick ()
        {
            webCamTextureToMatHelper.Play ();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick ()
        {
            webCamTextureToMatHelper.Pause ();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick ()
        {
            webCamTextureToMatHelper.Stop ();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick ()
        {
            webCamTextureToMatHelper.Initialize (null, webCamTextureToMatHelper.requestedWidth, webCamTextureToMatHelper.requestedHeight, !webCamTextureToMatHelper.requestedIsFrontFacing);
        }

        /// <summary>
        /// Raises the flip vertical toggle value changed event.
        /// </summary>
        public void OnFlipVerticalToggleValueChanged ()
        {
            if (flipVerticalToggle.isOn) {
                webCamTextureToMatHelper.flipVertical = true;
            } else {
                webCamTextureToMatHelper.flipVertical = false;
            }
        }

        /// <summary>
        /// Raises the flip horizontal toggle value changed event.
        /// </summary>
        public void OnFlipHorizontalToggleValueChanged ()
        {
            if (flipHorizontalToggle.isOn) {
                webCamTextureToMatHelper.flipHorizontal = true;
            } else {
                webCamTextureToMatHelper.flipHorizontal = false;
            }
        }
    }
}