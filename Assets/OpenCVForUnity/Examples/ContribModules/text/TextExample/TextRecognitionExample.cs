#if !UNITY_WSA_10_0

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.TextModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Text Detection Example
    /// A demo script of the Extremal Region Filter algorithm described in:Neumann L., Matas J.: Real-Time Scene Text Localization and Recognition, CVPR 2012.
    /// Referring to https://github.com/opencv/opencv_contrib/blob/master/modules/text/samples/textdetection.py.
    /// </summary>
    public class TextRecognitionExample : MonoBehaviour
    {

        /// <summary>
        /// IMAGE_FILENAME
        /// </summary>
        protected static readonly string IMAGE_FILENAME = "OpenCVForUnity/text/test_text.jpg";

        /// <summary>
        /// The image filepath.
        /// </summary>
        string image_filepath;

        /// <summary>
        /// TRAINED_CLASSIFIER_NM_1_FILENAME
        /// </summary>
        protected static readonly string TRAINED_CLASSIFIER_NM_1_FILENAME = "OpenCVForUnity/text/trained_classifierNM1.xml";

        /// <summary>
        /// The trained classifierNM1 filepath.
        /// </summary>
        string trained_classifierNM1_filepath;

        /// <summary>
        /// TRAINED_CLASSIFIER_NM_2_FILENAME
        /// </summary>
        protected static readonly string TRAINED_CLASSIFIER_NM_2_FILENAME = "OpenCVForUnity/text/trained_classifierNM2.xml";

        /// <summary>
        /// The trained classifierNM2 filepath.
        /// </summary>
        string trained_classifierNM2_filepath;

        /// <summary>
        /// CLASSIFIER_NM_2_FILENAME
        /// </summary>
        protected static readonly string OCRHMM_TRANSITIONS_TABLE_FILENAME = "OpenCVForUnity/text/OCRHMM_transitions_table.xml";

        /// <summary>
        /// The OCRHMM transitions table filepath.
        /// </summary>
        string OCRHMM_transitions_table_filepath;

        /// <summary>
        /// CLASSIFIER_NM_2_FILENAME
        /// </summary>
        /// https://stackoverflow.com/questions/4666098/why-does-android-aapt-remove-gz-file-extension-of-assets
#if UNITY_ANDROID && !UNITY_EDITOR
        protected static readonly string OCRHMM_KNN_MODEL_FILENAME = "OpenCVForUnity/text/OCRHMM_knn_model_data.xml";
#else
        protected static readonly string OCRHMM_KNN_MODEL_FILENAME = "OpenCVForUnity/text/OCRHMM_knn_model_data.xml.gz";
#endif

        /// <summary>
        /// The OCRHMM knn model data filepath.
        /// </summary>
        string OCRHMM_knn_model_data_filepath;

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif


        // Use this for initialization
        void Start()
        {
#if UNITY_WEBGL
            getFilePath_Coroutine = GetFilePath ();
            StartCoroutine (getFilePath_Coroutine);
#else
            image_filepath = Utils.getFilePath(IMAGE_FILENAME);
            trained_classifierNM1_filepath = Utils.getFilePath(TRAINED_CLASSIFIER_NM_1_FILENAME);
            trained_classifierNM2_filepath = Utils.getFilePath(TRAINED_CLASSIFIER_NM_2_FILENAME);
            OCRHMM_transitions_table_filepath = Utils.getFilePath(OCRHMM_TRANSITIONS_TABLE_FILENAME);
            OCRHMM_knn_model_data_filepath = Utils.getFilePath(OCRHMM_KNN_MODEL_FILENAME);

            Run();
#endif
        }

#if UNITY_WEBGL
        private IEnumerator GetFilePath ()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync (IMAGE_FILENAME, (result) => {
                image_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync (TRAINED_CLASSIFIER_NM_1_FILENAME, (result) => {
                trained_classifierNM1_filepath = result;
            });
            yield return getFilePathAsync_1_Coroutine;

            var getFilePathAsync_2_Coroutine = Utils.getFilePathAsync (TRAINED_CLASSIFIER_NM_2_FILENAME, (result) => {
                trained_classifierNM2_filepath = result;
            });
            yield return getFilePathAsync_2_Coroutine;

            var getFilePathAsync_3_Coroutine = Utils.getFilePathAsync (OCRHMM_TRANSITIONS_TABLE_FILENAME, (result) => {
                OCRHMM_transitions_table_filepath = result;
            });
            yield return getFilePathAsync_3_Coroutine;

            var getFilePathAsync_4_Coroutine = Utils.getFilePathAsync (OCRHMM_KNN_MODEL_FILENAME, (result) => {
                OCRHMM_knn_model_data_filepath = result;
            });
            yield return getFilePathAsync_4_Coroutine;

            getFilePath_Coroutine = null;

            Run ();
        }
#endif

        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            Mat frame = Imgcodecs.imread(image_filepath);
            if (frame.empty())
            {
                Debug.LogError(IMAGE_FILENAME + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
            }

            if (string.IsNullOrEmpty(trained_classifierNM1_filepath) || string.IsNullOrEmpty(trained_classifierNM2_filepath))
            {
                Debug.LogError(TRAINED_CLASSIFIER_NM_1_FILENAME + " or " + TRAINED_CLASSIFIER_NM_2_FILENAME + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
            }
            if (string.IsNullOrEmpty(OCRHMM_transitions_table_filepath) || string.IsNullOrEmpty(OCRHMM_knn_model_data_filepath))
            {
                Debug.LogError(OCRHMM_TRANSITIONS_TABLE_FILENAME + " or " + OCRHMM_KNN_MODEL_FILENAME + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
            }


            Mat binaryMat = new Mat();
            Mat maskMat = new Mat();


            List<MatOfPoint> regions = new List<MatOfPoint>();

            ERFilter er_filter1 = Text.createERFilterNM1(trained_classifierNM1_filepath, 8, 0.00015f, 0.13f, 0.2f, true, 0.1f);

            ERFilter er_filter2 = Text.createERFilterNM2(trained_classifierNM2_filepath, 0.5f);


            Mat transition_p = new Mat(62, 62, CvType.CV_64FC1);
            //string filename = "OCRHMM_transitions_table.xml";
            //FileStorage fs(filename, FileStorage::READ);
            //fs["transition_probabilities"] >> transition_p;
            //fs.release();

            //Load TransitionProbabilitiesData.
            transition_p.put(0, 0, GetTransitionProbabilitiesData(OCRHMM_transitions_table_filepath));

            Mat emission_p = Mat.eye(62, 62, CvType.CV_64FC1);
            string voc = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            OCRHMMDecoder decoder = OCRHMMDecoder.create(
                                        OCRHMM_knn_model_data_filepath,
                                        voc, transition_p, emission_p);

            //Text Detection
            Imgproc.cvtColor(frame, frame, Imgproc.COLOR_BGR2RGB);
            Imgproc.cvtColor(frame, binaryMat, Imgproc.COLOR_RGB2GRAY);
            Imgproc.threshold(binaryMat, binaryMat, 0, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);
            Core.absdiff(binaryMat, new Scalar(255), maskMat);


            Text.detectRegions(binaryMat, er_filter1, er_filter2, regions);
            Debug.Log("regions.Count " + regions.Count);


            MatOfRect groups_rects = new MatOfRect();
            List<OpenCVForUnity.CoreModule.Rect> rects = new List<OpenCVForUnity.CoreModule.Rect>();
            Text.erGrouping(frame, binaryMat, regions, groups_rects);


            for (int i = 0; i < regions.Count; i++)
            {
                regions[i].Dispose();
            }
            regions.Clear();


            rects.AddRange(groups_rects.toList());

            groups_rects.Dispose();


            //Text Recognition (OCR)

            List<Mat> detections = new List<Mat>();

            for (int i = 0; i < (int)rects.Count; i++)
            {

                Mat group_img = new Mat();
                maskMat.submat(rects[i]).copyTo(group_img);
                Core.copyMakeBorder(group_img, group_img, 15, 15, 15, 15, Core.BORDER_CONSTANT, new Scalar(0));
                detections.Add(group_img);
            }

            Debug.Log("detections.Count " + detections.Count);


            //#Visualization
            for (int i = 0; i < rects.Count; i++)
            {


                Imgproc.rectangle(frame, new Point(rects[i].x, rects[i].y), new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 0, 0), 2);
                Imgproc.rectangle(frame, new Point(rects[i].x, rects[i].y), new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 255, 255), 1);

                string output = decoder.run(detections[i], 0);
                if (!string.IsNullOrEmpty(output))
                {
                    Debug.Log("output " + output);
                    Imgproc.putText(frame, output, new Point(rects[i].x, rects[i].y), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(0, 0, 255), 1, Imgproc.LINE_AA, false);
                }
            }


            Texture2D texture = new Texture2D(frame.cols(), frame.rows(), TextureFormat.RGBA32, false);

            Utils.matToTexture2D(frame, texture);

            //Texture2D texture = new Texture2D (detections [0].cols (), detections [0].rows (), TextureFormat.RGBA32, false);
            //
            //Utils.matToTexture2D (detections [0], texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;


            for (int i = 0; i < detections.Count; i++)
            {
                detections[i].Dispose();
            }
            binaryMat.Dispose();
            maskMat.Dispose();

            Utils.setDebugMode(false);
        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// Gets the transition probabilities data.
        /// </summary>
        /// <returns>The transition probabilities data.</returns>
        /// <param name="filePath">File path.</param>
        double[] GetTransitionProbabilitiesData(string filePath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);


            XmlNode dataNode = xmlDoc.GetElementsByTagName("data").Item(0);
            //Debug.Log ("dataNode.InnerText " + dataNode.InnerText);
            string[] dataString = dataNode.InnerText.Split(new string[] {
                " ",
                "\r\n", "\n"
            }, StringSplitOptions.RemoveEmptyEntries);
            //Debug.Log ("dataString.Length " + dataString.Length);

            double[] data = new double[dataString.Length];
            for (int i = 0; i < data.Length; i++)
            {
                try
                {
                    data[i] = Convert.ToDouble(dataString[i]);
                }
                catch (FormatException)
                {
                    Debug.Log("Unable to convert '{" + dataString[i] + "}' to a Double.");
                }
                catch (OverflowException)
                {
                    Debug.Log("'{" + dataString[i] + "}' is outside the range of a Double.");
                }
            }

            return data;
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
#if UNITY_WEBGL
            if (getFilePath_Coroutine != null) {
                StopCoroutine (getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose ();
            }
#endif
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }
    }
}
#endif