#if !UNITY_WSA_10_0

using System.Collections.Generic;
using System.Threading;
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
    /// Text Detection Example
    /// Locates text regions in a scene image using the Extremal Region (ER) filter pipeline.
    ///
    /// Demonstrates:
    /// - Computing NM (Natural Mode) channels and their inverted counterparts
    /// - Running ERFilter NM1/NM2 classifiers per channel to find text candidates
    /// - Grouping detected regions into bounding boxes and visualizing results
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Scalar"/>, <see cref="Point"/>, <see cref="Core"/>
    /// - <see cref="ERFilter"/>
    /// - <see cref="Text"/>: computeNMChannels, createERFilterNM1, createERFilterNM2, detectRegions, erGrouping
    /// - <see cref="Imgcodecs"/>: imread
    /// - <see cref="Imgproc"/>: cvtColor, rectangle
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// Neumann L., Matas J.: Real-Time Scene Text Localization and Recognition, CVPR 2012
    /// https://github.com/opencv/opencv_contrib/blob/master/modules/text/samples/textdetection.py
    /// </para>
    /// </remarks>
    public class TextDetectionExample : MonoBehaviour
    {
        // Constants
        private static readonly string IMAGE_FILEPATH = "OpenCVForUnityExamples/text/scenetext01.jpg";

        private static readonly string TRAINED_CLASSIFIER_NM_1_FILEPATH = "OpenCVForUnityExamples/text/trained_classifierNM1.xml";

        private static readonly string TRAINED_CLASSIFIER_NM_2_FILEPATH = "OpenCVForUnityExamples/text/trained_classifierNM2.xml";

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

            // Load scene image (BGR); Imgcodecs returns an empty Mat if the path is invalid.
            Mat img = Imgcodecs.imread(_imageFilepath);
            if (img.empty())
            {
                Debug.LogError(IMAGE_FILEPATH + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("image file is not loaded.\nPlease read console message.", 20000);
                }

                img.Dispose();
                OpenCVDebug.SetDebugMode(false);
                return;
            }

            if (string.IsNullOrEmpty(_trainedClassifierNM1Filepath) || string.IsNullOrEmpty(_trainedClassifierNM2Filepath))
            {
                Debug.LogError(TRAINED_CLASSIFIER_NM_1_FILEPATH + " or " + TRAINED_CLASSIFIER_NM_2_FILEPATH + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("trained classifier file is not loaded.\nPlease read console message.", 20000);
                }

                img.Dispose();
                OpenCVDebug.SetDebugMode(false);
                return;
            }

            //# for visualization —convert BGR input to RGB for Unity display
            Mat vis = new Mat();
            img.copyTo(vis);
            Imgproc.cvtColor(vis, vis, Imgproc.COLOR_BGR2RGB);

            //# Extract channels to be processed individually (one ER pass per channel)
            List<Mat> channels = new List<Mat>();
            Text.computeNMChannels(img, channels);

            //# Append negative channels to detect ER- (bright text on dark background)
            int cn = channels.Count;
            for (int i = 0; i < cn; i++)
            {
                Mat a = channels[i];
                Mat negativeChannel = new Mat();
                using (Mat b = new Mat(a.size(), a.type(), new Scalar(255)))
                {
                    Core.subtract(b, a, negativeChannel);
                }
                channels.Add(negativeChannel);
            }

            //# Apply the default cascade classifier to each independent channel (could be done in parallel)
            Debug.Log("Extracting Class Specific Extremal Regions from " + channels.Count + " channels ...", this);
            Debug.Log("    (...) this may take a while (...)", this);
            foreach (var channel in channels)
            {
                // NM1 filters raw ER candidates; NM2 refines them before grouping.
                ERFilter er1 = Text.createERFilterNM1(_trainedClassifierNM1Filepath, 16, 0.00015f, 0.13f, 0.2f, true, 0.1f);

                ERFilter er2 = Text.createERFilterNM2(_trainedClassifierNM2Filepath, 0.5f);

                List<MatOfPoint> regions = new List<MatOfPoint>();
                Text.detectRegions(channel, er1, er2, regions);

                // Merge nearby regions into text-line bounding boxes.
                MatOfRect matOfRects = new MatOfRect();
                Text.erGrouping(img, channel, regions, matOfRects);
                //Text.erGrouping (img, channel, regions, matOfRects, Text.ERGROUPING_ORIENTATION_ANY, OpenCVForUnityEnv.GetFilePath ("text/trained_classifier_erGrouping.xml"), 0.5f);

                List<OpenCVForUnity.CoreModule.Rect> rects = matOfRects.toList();

                //#Visualization
                foreach (var rect in rects)
                {

                    Imgproc.rectangle(vis, new Point(rect.x, rect.y), new Point(rect.x + rect.width, rect.y + rect.height), new Scalar(255, 0, 0), 2);
                    Imgproc.rectangle(vis, new Point(rect.x, rect.y), new Point(rect.x + rect.width, rect.y + rect.height), new Scalar(255, 255, 255), 1);

                }
            }

            Texture2D texture = new Texture2D(vis.cols(), vis.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUnityUtils.MatToTexture2D(vis, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;

            OpenCVDebug.SetDebugMode(false);

            img.Dispose();
            vis.Dispose();
            foreach (var ch in channels)
            {
                ch.Dispose();
            }
        }
    }
}
#endif
