#if !UNITY_WSA_10_0

using System.Collections.Generic;
using System.Threading;
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
    public class TextDetectionExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// IMAGE_FILENAME
        /// </summary>
        protected static readonly string IMAGE_FILENAME = "OpenCVForUnityExamples/text/scenetext01.jpg";

        /// <summary>
        /// TRAINED_CLASSIFIER_NM_1_FILENAME
        /// </summary>
        protected static readonly string TRAINED_CLASSIFIER_NM_1_FILENAME = "OpenCVForUnityExamples/text/trained_classifierNM1.xml";

        /// <summary>
        /// TRAINED_CLASSIFIER_NM_2_FILENAME
        /// </summary>
        protected static readonly string TRAINED_CLASSIFIER_NM_2_FILENAME = "OpenCVForUnityExamples/text/trained_classifierNM2.xml";

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
        /// The trained_classifierNM1 filepath.
        /// </summary>
        private string _trainedClassifierNM1Filepath;

        /// <summary>
        /// The trained_classifierNM2 filepath.
        /// </summary>
        private string _trainedClassifierNM2Filepath;

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


            Mat img = Imgcodecs.imread(_imageFilepath);
            if (img.empty())
            {
                Debug.LogError(IMAGE_FILENAME + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
            }

            if (string.IsNullOrEmpty(_trainedClassifierNM1Filepath) || string.IsNullOrEmpty(_trainedClassifierNM2Filepath))
            {
                Debug.LogError(TRAINED_CLASSIFIER_NM_1_FILENAME + " or " + TRAINED_CLASSIFIER_NM_2_FILENAME + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
            }

            //# for visualization
            Mat vis = new Mat();
            img.copyTo(vis);
            Imgproc.cvtColor(vis, vis, Imgproc.COLOR_BGR2RGB);


            //# Extract channels to be processed individually
            List<Mat> channels = new List<Mat>();
            Text.computeNMChannels(img, channels);

            //# Append negative channels to detect ER- (bright regions over dark background)
            int cn = channels.Count;
            for (int i = 0; i < cn; i++)
            {
                Mat A = channels[i];
                Mat negativeChannel = new Mat();
                using (Mat B = new Mat(A.size(), A.type(), new Scalar(255)))
                {
                    Core.subtract(B, A, negativeChannel);
                }
                channels.Add(negativeChannel);
            }

            //# Apply the default cascade classifier to each independent channel (could be done in parallel)
            Debug.Log("Extracting Class Specific Extremal Regions from " + channels.Count + " channels ...");
            Debug.Log("    (...) this may take a while (...)");
            foreach (var channel in channels)
            {
                ERFilter er1 = Text.createERFilterNM1(_trainedClassifierNM1Filepath, 16, 0.00015f, 0.13f, 0.2f, true, 0.1f);

                ERFilter er2 = Text.createERFilterNM2(_trainedClassifierNM2Filepath, 0.5f);

                List<MatOfPoint> regions = new List<MatOfPoint>();
                Text.detectRegions(channel, er1, er2, regions);

                MatOfRect matOfRects = new MatOfRect();
                Text.erGrouping(img, channel, regions, matOfRects);
                //Text.erGrouping (img, channel, regions, matOfRects, Text.ERGROUPING_ORIENTATION_ANY, OpenCVEnv.GetFilePath ("text/trained_classifier_erGrouping.xml"), 0.5f);

                List<OpenCVForUnity.CoreModule.Rect> rects = matOfRects.toList();

                //#Visualization
                foreach (var rect in rects)
                {

                    Imgproc.rectangle(vis, new Point(rect.x, rect.y), new Point(rect.x + rect.width, rect.y + rect.height), new Scalar(255, 0, 0), 2);
                    Imgproc.rectangle(vis, new Point(rect.x, rect.y), new Point(rect.x + rect.width, rect.y + rect.height), new Scalar(255, 255, 255), 1);

                }

            }

            Texture2D texture = new Texture2D(vis.cols(), vis.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUtils.MatToTexture2D(vis, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            OpenCVDebug.SetDebugMode(false);
        }
    }
}
#endif
