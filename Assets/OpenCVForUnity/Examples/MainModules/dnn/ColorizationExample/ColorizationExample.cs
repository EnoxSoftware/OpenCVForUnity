#if !UNITY_WSA_10_0

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using Range = OpenCVForUnity.CoreModule.Range;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Colorization Example
    /// This sample demonstrates recoloring grayscale images with dnn.
    /// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/colorization.cpp
    /// </summary>
    public class ColorizationExample : MonoBehaviour
    {

        const float inWidth = 224;
        const float inHeight = 224;

        // the 313 ab cluster centers from pts_in_hull.npy (already transposed)
        readonly float[] hull_pts = new float[]{
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
        string IMAGE_FILENAME = "OpenCVForUnity/dnn/ansel_adams3.jpg";

        /// <summary>
        /// The image filepath.
        /// </summary>
        string image_filepath;

        /// <summary>
        /// CAFFEMODEL_FILENAME
        /// </summary>
        string CAFFEMODEL_FILENAME = "OpenCVForUnity/dnn/colorization_release_v2.caffemodel";

        /// <summary>
        /// The caffemodel filepath.
        /// </summary>
        string caffemodel_filepath;

        /// <summary>
        /// PROTOTXT_FILENAME
        /// </summary>
        string PROTOTXT_FILENAME = "OpenCVForUnity/dnn/colorization_deploy_v2.prototxt";

        /// <summary>
        /// The prototxt filepath.
        /// </summary>
        string prototxt_filepath;

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        void Start()
        {

#if UNITY_WEBGL
            getFilePath_Coroutine = GetFilePath();
            StartCoroutine(getFilePath_Coroutine);
#else
            image_filepath = Utils.getFilePath(IMAGE_FILENAME);
            caffemodel_filepath = Utils.getFilePath(CAFFEMODEL_FILENAME);
            prototxt_filepath = Utils.getFilePath(PROTOTXT_FILENAME);
            Run();
#endif
        }

#if UNITY_WEBGL
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync(IMAGE_FILENAME, (result) =>
            {
                image_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync(CAFFEMODEL_FILENAME, (result) =>
            {
                caffemodel_filepath = result;
            });
            yield return getFilePathAsync_1_Coroutine;

            var getFilePathAsync_2_Coroutine = Utils.getFilePathAsync(PROTOTXT_FILENAME, (result) =>
            {
                prototxt_filepath = result;
            });
            yield return getFilePathAsync_2_Coroutine;

            getFilePath_Coroutine = null;

            Run();
        }
#endif

        // Use this for initialization
        void Run()
        {

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);

            Mat img = Imgcodecs.imread(image_filepath, Imgcodecs.IMREAD_COLOR);
            Mat colorized = new Mat(img.rows(), img.cols(), img.type());
            if (img.empty())
            {
                Debug.LogError(IMAGE_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
                img = new Mat(368, 368, CvType.CV_8UC3, new Scalar(0, 0, 0));
            }

            Net net = null;

            if (string.IsNullOrEmpty(caffemodel_filepath) || string.IsNullOrEmpty(prototxt_filepath))
            {
                Debug.LogError(CAFFEMODEL_FILENAME + " or " + PROTOTXT_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                net = Dnn.readNetFromCaffe(prototxt_filepath, caffemodel_filepath);
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
                pts_in_hull.put(new int[] { 0, 0, 0, 0 }, hull_pts);

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
                Imgproc.resize(L, input, new Size(inWidth, inHeight));
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

            Utils.matToTexture2D(display, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            //Adust Quad.transform.localScale.
            gameObject.transform.localScale = new Vector3(display.width(), display.height(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            float imageWidth = display.width();
            float imageHeight = display.height();

            float widthScale = (float)Screen.width / imageWidth;
            float heightScale = (float)Screen.height / imageHeight;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (imageWidth * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = imageHeight / 2;
            }


            Utils.setDebugMode(false);
        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// Raises the disable event.
        /// </summary>
        void OnDisable()
        {
#if UNITY_WEBGL
            if (getFilePath_Coroutine != null)
            {
                StopCoroutine(getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose();
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