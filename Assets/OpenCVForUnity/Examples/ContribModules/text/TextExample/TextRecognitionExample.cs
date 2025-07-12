#if !UNITY_WSA_10_0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.TextModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Text = OpenCVForUnity.TextModule.Text;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Text Detection Example
    /// A demo script of the Extremal Region Filter algorithm described in:Neumann L., Matas J.: Real-Time Scene Text Localization and Recognition, CVPR 2012.
    /// Referring to https://github.com/opencv/opencv_contrib/blob/master/modules/text/samples/textdetection.py.
    /// </summary>
    public class TextRecognitionExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// IMAGE_FILENAME
        /// </summary>
        protected static readonly string IMAGE_FILENAME = "OpenCVForUnityExamples/text/test_text.jpg";

        /// <summary>
        /// TRAINED_CLASSIFIER_NM_1_FILENAME
        /// </summary>
        protected static readonly string TRAINED_CLASSIFIER_NM_1_FILENAME = "OpenCVForUnityExamples/text/trained_classifierNM1.xml";

        /// <summary>
        /// TRAINED_CLASSIFIER_NM_2_FILENAME
        /// </summary>
        protected static readonly string TRAINED_CLASSIFIER_NM_2_FILENAME = "OpenCVForUnityExamples/text/trained_classifierNM2.xml";

        /// <summary>
        /// CLASSIFIER_NM_2_FILENAME
        /// </summary>
        protected static readonly string OCRHMM_TRANSITIONS_TABLE_FILENAME = "OpenCVForUnityExamples/text/OCRHMM_transitions_table.xml";

        /// <summary>
        /// CLASSIFIER_NM_2_FILENAME
        /// </summary>
        /// https://stackoverflow.com/questions/4666098/why-does-android-aapt-remove-gz-file-extension-of-assets
#if UNITY_ANDROID && !UNITY_EDITOR
        protected static readonly string OCRHMM_KNN_MODEL_FILENAME = "OpenCVForUnityExamples/text/OCRHMM_knn_model_data.xml";
#else
        protected static readonly string OCRHMM_KNN_MODEL_FILENAME = "OpenCVForUnityExamples/text/OCRHMM_knn_model_data.xml.gz";
#endif

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
        /// <summary>
        /// The image filepath.
        /// </summary>
        private string _imageFilepath;

        /// <summary>
        /// The trained classifierNM1 filepath.
        /// </summary>
        private string _trainedClassifierNM1Filepath;

        /// <summary>
        /// The trained classifierNM2 filepath.
        /// </summary>
        private string _trainedClassifierNM2Filepath;

        /// <summary>
        /// The OCRHMM transitions table filepath.
        /// </summary>
        private string _ocrmmTransitionsTableFilepath;

        /// <summary>
        /// The OCRHMM knn model data filepath.
        /// </summary>
        private string _ocrmmKnnModelDataFilepath;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _imageFilepath = await OpenCVEnv.GetFilePathTaskAsync(IMAGE_FILENAME, cancellationToken: _cts.Token);
            _trainedClassifierNM1Filepath = await OpenCVEnv.GetFilePathTaskAsync(TRAINED_CLASSIFIER_NM_1_FILENAME, cancellationToken: _cts.Token);
            _trainedClassifierNM2Filepath = await OpenCVEnv.GetFilePathTaskAsync(TRAINED_CLASSIFIER_NM_2_FILENAME, cancellationToken: _cts.Token);
            _ocrmmTransitionsTableFilepath = await OpenCVEnv.GetFilePathTaskAsync(OCRHMM_TRANSITIONS_TABLE_FILENAME, cancellationToken: _cts.Token);
            _ocrmmKnnModelDataFilepath = await OpenCVEnv.GetFilePathTaskAsync(OCRHMM_KNN_MODEL_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        private void Update()
        {

        }

        private void OnDestroy()
        {
            _cts?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        // Private Methods
        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);


            Mat frame = Imgcodecs.imread(_imageFilepath);
            if (frame.empty())
            {
                Debug.LogError(IMAGE_FILENAME + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
            }

            if (string.IsNullOrEmpty(_trainedClassifierNM1Filepath) || string.IsNullOrEmpty(_trainedClassifierNM2Filepath))
            {
                Debug.LogError(TRAINED_CLASSIFIER_NM_1_FILENAME + " or " + TRAINED_CLASSIFIER_NM_2_FILENAME + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
            }
            if (string.IsNullOrEmpty(_ocrmmTransitionsTableFilepath) || string.IsNullOrEmpty(_ocrmmKnnModelDataFilepath))
            {
                Debug.LogError(OCRHMM_TRANSITIONS_TABLE_FILENAME + " or " + OCRHMM_KNN_MODEL_FILENAME + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
            }


            Mat binaryMat = new Mat();
            Mat maskMat = new Mat();


            List<MatOfPoint> regions = new List<MatOfPoint>();

            ERFilter er_filter1 = Text.createERFilterNM1(_trainedClassifierNM1Filepath, 8, 0.00015f, 0.13f, 0.2f, true, 0.1f);

            ERFilter er_filter2 = Text.createERFilterNM2(_trainedClassifierNM2Filepath, 0.5f);


            Mat transition_p = new Mat(62, 62, CvType.CV_64FC1);
            //string filename = "OCRHMM_transitions_table.xml";
            //FileStorage fs(filename, FileStorage::READ);
            //fs["transition_probabilities"] >> transition_p;
            //fs.release();

            //Load TransitionProbabilitiesData.
            transition_p.put(0, 0, GetTransitionProbabilitiesData(_ocrmmTransitionsTableFilepath));

            Mat emission_p = Mat.eye(62, 62, CvType.CV_64FC1);
            string voc = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            OCRHMMDecoder decoder = OCRHMMDecoder.create(
                                        _ocrmmKnnModelDataFilepath,
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

            OpenCVMatUtils.MatToTexture2D(frame, texture);

            //Texture2D texture = new Texture2D (detections [0].cols (), detections [0].rows (), TextureFormat.RGBA32, false);
            //
            //Utils.matToTexture2D (detections [0], texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            for (int i = 0; i < detections.Count; i++)
            {
                detections[i].Dispose();
            }
            binaryMat.Dispose();
            maskMat.Dispose();

            OpenCVDebug.SetDebugMode(false);
        }

        /// <summary>
        /// Gets the transition probabilities data.
        /// </summary>
        /// <returns>The transition probabilities data.</returns>
        /// <param name="filePath">File path.</param>
        private double[] GetTransitionProbabilitiesData(string filePath)
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
    }
}
#endif
