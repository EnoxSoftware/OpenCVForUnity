#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.TextModule;
using OpenCVForUnity.UnityUtils;
using System.Collections.Generic;
using System.Threading;
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
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        /// <summary>
        /// IMAGE_FILENAME
        /// </summary>
        protected static readonly string IMAGE_FILENAME = "OpenCVForUnity/text/scenetext01.jpg";

        /// <summary>
        /// The image filepath.
        /// </summary>
        string image_filepath;

        /// <summary>
        /// TRAINED_CLASSIFIER_NM_1_FILENAME
        /// </summary>
        protected static readonly string TRAINED_CLASSIFIER_NM_1_FILENAME = "OpenCVForUnity/text/trained_classifierNM1.xml";

        /// <summary>
        /// The trained_classifierNM1 filepath.
        /// </summary>
        string trained_classifierNM1_filepath;

        /// <summary>
        /// TRAINED_CLASSIFIER_NM_2_FILENAME
        /// </summary>
        protected static readonly string TRAINED_CLASSIFIER_NM_2_FILENAME = "OpenCVForUnity/text/trained_classifierNM2.xml";

        /// <summary>
        /// The trained_classifierNM2 filepath.
        /// </summary>
        string trained_classifierNM2_filepath;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // Use this for initialization
        async void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (fpsMonitor != null)
                fpsMonitor.consoleText = "Preparing file access...";

            image_filepath = await Utils.getFilePathAsyncTask(IMAGE_FILENAME, cancellationToken: cts.Token);
            trained_classifierNM1_filepath = await Utils.getFilePathAsyncTask(TRAINED_CLASSIFIER_NM_1_FILENAME, cancellationToken: cts.Token);
            trained_classifierNM2_filepath = await Utils.getFilePathAsyncTask(TRAINED_CLASSIFIER_NM_2_FILENAME, cancellationToken: cts.Token);

            if (fpsMonitor != null)
                fpsMonitor.consoleText = "";

            Run();
        }

        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            Mat img = Imgcodecs.imread(image_filepath);
            if (img.empty())
            {
                Debug.LogError(IMAGE_FILENAME + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
            }

            if (string.IsNullOrEmpty(trained_classifierNM1_filepath) || string.IsNullOrEmpty(trained_classifierNM2_filepath))
            {
                Debug.LogError(TRAINED_CLASSIFIER_NM_1_FILENAME + " or " + TRAINED_CLASSIFIER_NM_2_FILENAME + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
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

                channels.Add(new Scalar(255) - channels[i]);
            }

            //# Apply the default cascade classifier to each independent channel (could be done in parallel)
            Debug.Log("Extracting Class Specific Extremal Regions from " + channels.Count + " channels ...");
            Debug.Log("    (...) this may take a while (...)");
            foreach (var channel in channels)
            {
                ERFilter er1 = Text.createERFilterNM1(trained_classifierNM1_filepath, 16, 0.00015f, 0.13f, 0.2f, true, 0.1f);

                ERFilter er2 = Text.createERFilterNM2(trained_classifierNM2_filepath, 0.5f);

                List<MatOfPoint> regions = new List<MatOfPoint>();
                Text.detectRegions(channel, er1, er2, regions);

                MatOfRect matOfRects = new MatOfRect();
                Text.erGrouping(img, channel, regions, matOfRects);
                //                Text.erGrouping (img, channel, regions, matOfRects, Text.ERGROUPING_ORIENTATION_ANY, Utils.getFilePath ("text/trained_classifier_erGrouping.xml"), 0.5f);

                List<OpenCVForUnity.CoreModule.Rect> rects = matOfRects.toList();

                //#Visualization
                foreach (var rect in rects)
                {

                    Imgproc.rectangle(vis, new Point(rect.x, rect.y), new Point(rect.x + rect.width, rect.y + rect.height), new Scalar(255, 0, 0), 2);
                    Imgproc.rectangle(vis, new Point(rect.x, rect.y), new Point(rect.x + rect.width, rect.y + rect.height), new Scalar(255, 255, 255), 1);

                }

            }

            Texture2D texture = new Texture2D(vis.cols(), vis.rows(), TextureFormat.RGBA32, false);

            Utils.matToTexture2D(vis, texture);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            Utils.setDebugMode(false);
        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (cts != null)
                cts.Dispose();
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