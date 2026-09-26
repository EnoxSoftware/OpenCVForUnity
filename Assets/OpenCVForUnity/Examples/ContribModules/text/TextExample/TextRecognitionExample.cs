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
using OpenCVForUnity.UnityIntegration.Helper.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenCVDebug = OpenCVForUnity.Extensions.OpenCVDebug;
using Text = OpenCVForUnity.TextModule.Text;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Text Recognition Example
    /// Detects text regions and recognizes characters using the OCR HMM decoder.
    ///
    /// Demonstrates:
    /// - ER-based text detection with NM1/NM2 filters and erGrouping
    /// - Building per-region binary patches for OCR input
    /// - OCRHMMDecoder with transition/emission matrices for character recognition
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Scalar"/>, <see cref="Point"/>, <see cref="Core"/>
    /// - <see cref="ERFilter"/>, <see cref="OCRHMMDecoder"/>
    /// - <see cref="Text"/>: computeNMChannels, createERFilterNM1, createERFilterNM2, detectRegions, erGrouping
    /// - <see cref="Imgcodecs"/>: imread
    /// - <see cref="Imgproc"/>: cvtColor, threshold, rectangle, putText
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// Neumann L., Matas J.: Real-Time Scene Text Localization and Recognition, CVPR 2012
    /// https://github.com/opencv/opencv_contrib/blob/master/modules/text/samples/textdetection.py
    /// </para>
    /// </remarks>
    public class TextRecognitionExample : MonoBehaviour
    {
        // Constants
        private static readonly string IMAGE_FILEPATH = "OpenCVForUnityExamples/text/test_text.jpg";

        private static readonly string TRAINED_CLASSIFIER_NM_1_FILEPATH = "OpenCVForUnityExamples/text/trained_classifierNM1.xml";

        private static readonly string TRAINED_CLASSIFIER_NM_2_FILEPATH = "OpenCVForUnityExamples/text/trained_classifierNM2.xml";

        private static readonly string OCRHMM_TRANSITIONS_TABLE_FILEPATH = "OpenCVForUnityExamples/text/OCRHMM_transitions_table.xml";

        // https://stackoverflow.com/questions/4666098/why-does-android-aapt-remove-gz-file-extension-of-assets
#if UNITY_ANDROID && !UNITY_EDITOR
        private static readonly string OCRHMM_KNN_MODEL_FILEPATH = "OpenCVForUnityExamples/text/OCRHMM_knn_model_data.xml";
#else
        private static readonly string OCRHMM_KNN_MODEL_FILEPATH = "OpenCVForUnityExamples/text/OCRHMM_knn_model_data.xml.gz";
#endif

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
        private string _imageFilepath;

        private string _trainedClassifierNM1Filepath;

        private string _trainedClassifierNM2Filepath;

        private string _ocrmmTransitionsTableFilepath;

        private string _ocrmmKnnModelDataFilepath;

        private FpsMonitor _fpsMonitor;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            _imageFilepath = await OpenCVForUnityEnv.GetFilePathAsync(IMAGE_FILEPATH, cancellationToken: _cts.Token);
            _trainedClassifierNM1Filepath = await OpenCVForUnityEnv.GetFilePathAsync(TRAINED_CLASSIFIER_NM_1_FILEPATH, cancellationToken: _cts.Token);
            _trainedClassifierNM2Filepath = await OpenCVForUnityEnv.GetFilePathAsync(TRAINED_CLASSIFIER_NM_2_FILEPATH, cancellationToken: _cts.Token);
            _ocrmmTransitionsTableFilepath = await OpenCVForUnityEnv.GetFilePathAsync(OCRHMM_TRANSITIONS_TABLE_FILEPATH, cancellationToken: _cts.Token);
            _ocrmmKnnModelDataFilepath = await OpenCVForUnityEnv.GetFilePathAsync(OCRHMM_KNN_MODEL_FILEPATH, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            Run();
        }

        private void Update()
        {

        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
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

            if (string.IsNullOrEmpty(_imageFilepath))
            {
                Debug.LogError(IMAGE_FILEPATH + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("image file is not loaded.\nPlease read console message.", 20000);
                }

                OpenCVDebug.SetDebugMode(false);
                return;
            }

            Mat frame = Imgcodecs.imread(_imageFilepath);
            if (frame.empty())
            {
                Debug.LogError(IMAGE_FILEPATH + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("image file is not loaded.\nPlease read console message.", 20000);
                }

                frame.Dispose();
                OpenCVDebug.SetDebugMode(false);
                return;
            }

            bool classifierMissing = string.IsNullOrEmpty(_trainedClassifierNM1Filepath) || string.IsNullOrEmpty(_trainedClassifierNM2Filepath);
            bool ocrMissing = string.IsNullOrEmpty(_ocrmmTransitionsTableFilepath) || string.IsNullOrEmpty(_ocrmmKnnModelDataFilepath);
            if (classifierMissing)
            {
                Debug.LogError(TRAINED_CLASSIFIER_NM_1_FILEPATH + " or " + TRAINED_CLASSIFIER_NM_2_FILEPATH + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("trained classifier file is not loaded.\nPlease read console message.", 20000);
                }
            }
            if (ocrMissing)
            {
                Debug.LogError(OCRHMM_TRANSITIONS_TABLE_FILEPATH + " or " + OCRHMM_KNN_MODEL_FILEPATH + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("OCR data file is not loaded.\nPlease read console message.", 20000);
                }
            }
            if (classifierMissing || ocrMissing)
            {
                frame.Dispose();
                OpenCVDebug.SetDebugMode(false);
                return;
            }

            Mat binaryMat = new Mat();
            Mat maskMat = new Mat();

            List<MatOfPoint> regions = new List<MatOfPoint>();

            // NM1/NM2 ER filters detect character-like extremal regions in the binary image.
            ERFilter er_filter1 = Text.createERFilterNM1(_trainedClassifierNM1Filepath, 8, 0.00015f, 0.13f, 0.2f, true, 0.1f);

            ERFilter er_filter2 = Text.createERFilterNM2(_trainedClassifierNM2Filepath, 0.5f);

            // 62x62 transition matrix loaded from XML (character-to-character probabilities).
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

            // Binarize image; inverted mask highlights dark text on light background.
            Imgproc.cvtColor(frame, frame, Imgproc.COLOR_BGR2RGB);
            Imgproc.cvtColor(frame, binaryMat, Imgproc.COLOR_RGB2GRAY);
            Imgproc.threshold(binaryMat, binaryMat, 0, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);
            Core.absdiff(binaryMat, new Scalar(255), maskMat);

            Text.detectRegions(binaryMat, er_filter1, er_filter2, regions);
            Debug.Log("regions.Count " + regions.Count, this);

            // Group detected regions into word/line bounding boxes.
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

            //Text Recognition (OCR) — crop each grouped region with padding for decoder input.

            List<Mat> detections = new List<Mat>();

            for (int i = 0; i < (int)rects.Count; i++)
            {

                Mat group_img = new Mat();
                maskMat.submat(rects[i]).copyTo(group_img);
                // Border padding improves OCR accuracy on tight crops.
                Core.copyMakeBorder(group_img, group_img, 15, 15, 15, 15, Core.BORDER_CONSTANT, new Scalar(0));
                detections.Add(group_img);
            }

            Debug.Log("detections.Count " + detections.Count, this);

            //#Visualization
            for (int i = 0; i < rects.Count; i++)
            {

                Imgproc.rectangle(frame, new Point(rects[i].x, rects[i].y), new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 0, 0), 2);
                Imgproc.rectangle(frame, new Point(rects[i].x, rects[i].y), new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 255, 255), 1);

                string output = decoder.run(detections[i], 0);
                if (!string.IsNullOrEmpty(output))
                {
                    Debug.Log("output " + output, this);
                    // Draw recognized text above each detected bounding box.
                    Imgproc.putText(frame, output, new Point(rects[i].x, rects[i].y), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(0, 0, 255), 1, Imgproc.LINE_AA, false);
                }
            }

            Texture2D texture = new Texture2D(frame.cols(), frame.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUnityUtils.MatToTexture2D(frame, texture);

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
            frame.Dispose();

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
                    Debug.Log("Unable to convert '{" + dataString[i] + "}' to a Double.", this);
                }
                catch (OverflowException)
                {
                    Debug.Log("'{" + dataString[i] + "}' is outside the range of a Double.", this);
                }
            }

            return data;
        }
    }
}
#endif
