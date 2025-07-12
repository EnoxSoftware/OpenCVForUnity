#if !UNITY_WSA_10_0

using System.Collections.Generic;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Range = OpenCVForUnity.CoreModule.Range;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Colorization Example
    /// This sample demonstrates recoloring grayscale images with dnn.
    /// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/colorization.cpp
    ///
    /// [Tested Models]
    /// https://github.com/EnoxSoftware/OpenCVForUnityExampleAssets/releases/download/dnn%2FColorizationExample/colorization_release_v2.caffemodel
    /// https://github.com/richzhang/colorization/raw/caffe/models/colorization_deploy_v2.prototxt
    /// </summary>
    public class ColorizationExample : MonoBehaviour
    {
        // Constants
        private const float IN_WIDTH = 224;
        private const float IN_HEIGHT = 224;

        // the 313 ab cluster centers from pts_in_hull.npy (already transposed)
        private static readonly float[] HULL_PTS = new float[]{
            -90f, -90f, -90f, -90f, -90f, -80f, -80f, -80f, -80f, -80f, -80f, -80f, -80f, -70f, -70f, -70f, -70f, -70f, -70f, -70f, -70f,
            -70f, -70f, -60f, -60f, -60f, -60f, -60f, -60f, -60f, -60f, -60f, -60f, -60f, -60f, -50f, -50f, -50f, -50f, -50f, -50f, -50f, -50f,
            -50f, -50f, -50f, -50f, -50f, -50f, -40f, -40f, -40f, -40f, -40f, -40f, -40f, -40f, -40f, -40f, -40f, -40f, -40f, -40f, -40f, -30f,
            -30f, -30f, -30f, -30f, -30f, -30f, -30f, -30f, -30f, -30f, -30f, -30f, -30f, -30f, -30f, -20f, -20f, -20f, -20f, -20f, -20f, -20f,
            -20f, -20f, -20f, -20f, -20f, -20f, -20f, -20f, -20f, -10f, -10f, -10f, -10f, -10f, -10f, -10f, -10f, -10f, -10f, -10f, -10f, -10f,
            -10f, -10f, -10f, -10f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 10f, 10f, 10f, 10f, 10f, 10f, 10f,
            10f, 10f, 10f, 10f, 10f, 10f, 10f, 10f, 10f, 10f, 10f, 20f, 20f, 20f, 20f, 20f, 20f, 20f, 20f, 20f, 20f, 20f, 20f, 20f, 20f, 20f,
            20f, 20f, 20f, 30f, 30f, 30f, 30f, 30f, 30f, 30f, 30f, 30f, 30f, 30f, 30f, 30f, 30f, 30f, 30f, 30f, 30f, 30f, 40f, 40f, 40f, 40f,
            40f, 40f, 40f, 40f, 40f, 40f, 40f, 40f, 40f, 40f, 40f, 40f, 40f, 40f, 40f, 40f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f,
            50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 60f, 60f, 60f, 60f, 60f, 60f, 60f, 60f, 60f, 60f, 60f, 60f, 60f, 60f, 60f, 60f, 60f,
            60f, 60f, 60f, 70f, 70f, 70f, 70f, 70f, 70f, 70f, 70f, 70f, 70f, 70f, 70f, 70f, 70f, 70f, 70f, 70f, 70f, 70f, 70f, 80f, 80f, 80f,
            80f, 80f, 80f, 80f, 80f, 80f, 80f, 80f, 80f, 80f, 80f, 80f, 80f, 80f, 80f, 80f, 90f, 90f, 90f, 90f, 90f, 90f, 90f, 90f, 90f, 90f,
            90f, 90f, 90f, 90f, 90f, 90f, 90f, 90f, 90f, 100f, 100f, 100f, 100f, 100f, 100f, 100f, 100f, 100f, 100f, 50f, 60f, 70f, 80f, 90f,
            20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f, 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f, -20f, -10f, 0f, 10f, 20f, 30f, 40f, 50f,
            60f, 70f, 80f, 90f, -30f, -20f, -10f, 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f, 100f, -40f, -30f, -20f, -10f, 0f, 10f, 20f,
            30f, 40f, 50f, 60f, 70f, 80f, 90f, 100f, -50f, -40f, -30f, -20f, -10f, 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f, 100f, -50f,
            -40f, -30f, -20f, -10f, 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f, 100f, -60f, -50f, -40f, -30f, -20f, -10f, 0f, 10f, 20f,
            30f, 40f, 50f, 60f, 70f, 80f, 90f, 100f, -70f, -60f, -50f, -40f, -30f, -20f, -10f, 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f,
            100f, -80f, -70f, -60f, -50f, -40f, -30f, -20f, -10f, 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f, -80f, -70f, -60f, -50f,
            -40f, -30f, -20f, -10f, 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f, -90f, -80f, -70f, -60f, -50f, -40f, -30f, -20f, -10f,
            0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f, -100f, -90f, -80f, -70f, -60f, -50f, -40f, -30f, -20f, -10f, 0f, 10f, 20f, 30f,
            40f, 50f, 60f, 70f, 80f, 90f, -100f, -90f, -80f, -70f, -60f, -50f, -40f, -30f, -20f, -10f, 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f,
            80f, -110f, -100f, -90f, -80f, -70f, -60f, -50f, -40f, -30f, -20f, -10f, 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, -110f, -100f,
            -90f, -80f, -70f, -60f, -50f, -40f, -30f, -20f, -10f, 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, -110f, -100f, -90f, -80f, -70f,
            -60f, -50f, -40f, -30f, -20f, -10f, 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, -110f, -100f, -90f, -80f, -70f, -60f, -50f, -40f, -30f,
            -20f, -10f, 0f, 10f, 20f, 30f, 40f, 50f, 60f, 70f, -90f, -80f, -70f, -60f, -50f, -40f, -30f, -20f, -10f, 0f
        };

        /// <summary>
        /// IMAGE_FILENAME
        /// </summary>
        private static readonly string IMAGE_FILENAME = "OpenCVForUnityExamples/dnn/ansel_adams3.jpg";

        /// <summary>
        /// CAFFEMODEL_FILENAME
        /// </summary>
        private static readonly string CAFFEMODEL_FILENAME = "OpenCVForUnityExamples/dnn/colorization_release_v2.caffemodel";

        /// <summary>
        /// PROTOTXT_FILENAME
        /// </summary>
        private static readonly string PROTOTXT_FILENAME = "OpenCVForUnityExamples/dnn/colorization_deploy_v2.prototxt";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        // Private Fields
        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// The image filepath.
        /// </summary>
        private string _imageFilepath;

        /// <summary>
        /// The caffemodel filepath.
        /// </summary>
        private string _caffemodelFilepath;

        /// <summary>
        /// The prototxt filepath.
        /// </summary>
        private string _prototxtFilepath;

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
            _caffemodelFilepath = await OpenCVEnv.GetFilePathTaskAsync(CAFFEMODEL_FILENAME, cancellationToken: _cts.Token);
            _prototxtFilepath = await OpenCVEnv.GetFilePathTaskAsync(PROTOTXT_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        private void Update()
        {

        }

        private void OnDisable()
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

            Mat img = Imgcodecs.imread(_imageFilepath, Imgcodecs.IMREAD_COLOR);
            Mat colorized = new Mat(img.rows(), img.cols(), img.type());
            if (img.empty())
            {
                Debug.LogError(IMAGE_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
                img = new Mat(368, 368, CvType.CV_8UC3, new Scalar(0, 0, 0));
            }

            Net net = null;

            if (string.IsNullOrEmpty(_caffemodelFilepath) || string.IsNullOrEmpty(_prototxtFilepath))
            {
                Debug.LogError(CAFFEMODEL_FILENAME + " or " + PROTOTXT_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
            }
            else
            {
                net = Dnn.readNetFromCaffe(_prototxtFilepath, _caffemodelFilepath);
            }

            if (net == null)
            {
                Imgproc.putText(img, "model file is not loaded.", new Point(5, img.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(img, "Please read console message.", new Point(5, img.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
            }
            else
            {
                // setup additional layers:
                int[] sz = new int[] { 2, 313, 1, 1 };
                Mat pts_in_hull = new Mat(sz, CvType.CV_32F);
                pts_in_hull.put(new int[] { 0, 0, 0, 0 }, HULL_PTS);

                Layer class8_ab = net.getLayer(net.getLayerId("class8_ab"));
                List<Mat> blobs = class8_ab.get_blobs();
                blobs.Add(pts_in_hull);
                class8_ab.set_blobs(blobs);

                Layer conv8_313_rh = net.getLayer(net.getLayerId("conv8_313_rh"));
                blobs = conv8_313_rh.get_blobs();
                blobs.Add(new Mat(1, 313, CvType.CV_32F, new Scalar(2.606)));
                conv8_313_rh.set_blobs(blobs);

                // extract L channel and subtract mean
                Mat img_32F = new Mat();
                Mat lab = new Mat();
                Mat L = new Mat();
                Mat input = new Mat();
                img.convertTo(img_32F, CvType.CV_32F, 1.0 / 255);
                Imgproc.cvtColor(img_32F, lab, Imgproc.COLOR_BGR2Lab);
                Core.extractChannel(lab, L, 0);
                Imgproc.resize(L, input, new Size(IN_WIDTH, IN_HEIGHT));
                Core.subtract(input, new Scalar(50.0), input);

                // run the L channel through the network
                Mat inputBlob = Dnn.blobFromImage(input);
                net.setInput(inputBlob);
                Mat result = net.forward();

                // retrieve the calculated a,b channels from the network output
                Mat result_a = new Mat(result, new Range[] { new Range(0, 1), new Range(0, 1), new Range(0, result.size(2)), new Range(0, result.size(3)) });
                Mat result_b = new Mat(result, new Range[] { new Range(0, 1), new Range(1, 2), new Range(0, result.size(2)), new Range(0, result.size(3)) });
                result_a = result_a.reshape(1, result.size(2));
                result_b = result_b.reshape(1, result.size(2));
                Mat a = new Mat(img.size(), CvType.CV_32F);
                Mat b = new Mat(img.size(), CvType.CV_32F);
                Imgproc.resize(result_a, a, img.size());
                Imgproc.resize(result_b, b, img.size());

                // merge, and convert back to BGR
                List<Mat> chn = new List<Mat>();
                chn.Add(L); chn.Add(a); chn.Add(b);
                Core.merge(chn, lab);
                Imgproc.cvtColor(lab, img_32F, Imgproc.COLOR_Lab2BGR);
                img_32F.convertTo(colorized, CvType.CV_8U, 255.0);

                MatOfDouble timings = new MatOfDouble();
                long t = net.getPerfProfile(timings);
                double freq = Core.getTickFrequency() / 1000;
                Debug.Log("Inference time " + (t / freq) + "ms");
                Imgproc.putText(colorized, (t / freq) + "ms", new Point(10, img.height() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2);

                Imgproc.putText(img, "gray", new Point(10, 20), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2);
                Imgproc.putText(colorized, "colorized", new Point(10, 20), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2);

                // Dispose temporary Mats
                pts_in_hull.Dispose();
                img_32F.Dispose();
                lab.Dispose();
                L.Dispose();
                input.Dispose();
                inputBlob.Dispose();
                result.Dispose();
                result_a.Dispose();
                result_b.Dispose();
                a.Dispose();
                b.Dispose();
                timings.Dispose();
                net.Dispose();
            }

            Imgproc.cvtColor(colorized, colorized, Imgproc.COLOR_BGR2RGB);

            Mat display = new Mat(img.rows() * 2, img.cols(), img.type());

            using (Mat gray = new Mat(img.rows(), img.cols(), CvType.CV_8UC1))
            using (Mat displayUpperHalf = new Mat(display, new Range(0, img.rows())))
            using (Mat displayLowerHalf = new Mat(display, new Range(img.rows(), display.rows())))
            {
                Imgproc.cvtColor(img, gray, Imgproc.COLOR_BGR2GRAY);
                Imgproc.cvtColor(gray, img, Imgproc.COLOR_GRAY2RGB);

                img.copyTo(displayUpperHalf);
                colorized.copyTo(displayLowerHalf);
            }

            Texture2D texture = new Texture2D(display.cols(), display.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUtils.MatToTexture2D(display, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;

            // Dispose remaining Mats
            display.Dispose();
            img.Dispose();
            colorized.Dispose();

            OpenCVDebug.SetDebugMode(false);
        }
    }
}
#endif
