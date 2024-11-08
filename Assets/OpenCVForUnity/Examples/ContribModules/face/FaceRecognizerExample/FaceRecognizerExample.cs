using OpenCVForUnity.CoreModule;
using OpenCVForUnity.FaceModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// FaceRecognizer Example
    /// An example of human face recognition using the face (Face Recognition) module.
    /// http://docs.opencv.org/modules/contrib/doc/facerec/facerec_tutorial.html#eigenfaces
    /// </summary>
    public class FaceRecognizerExample : MonoBehaviour
    {
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        /// <summary>
        /// IMAGE_0_FILENAME
        /// </summary>
        protected static readonly string IMAGE_0_FILENAME = "OpenCVForUnity/face/facerec_0.bmp";

        /// <summary>
        /// The image 0 filepath.
        /// </summary>
        string image_0_filepath;

        /// <summary>
        /// IMAGE_1_FILENAME
        /// </summary>
        protected static readonly string IMAGE_1_FILENAME = "OpenCVForUnity/face/facerec_1.bmp";

        /// <summary>
        /// The image 1 filepath.
        /// </summary>
        string image_1_filepath;

        /// <summary>
        /// SAMPLE_IMAGE_FILENAME
        /// </summary>
        protected static readonly string SAMPLE_IMAGE_FILENAME = "OpenCVForUnity/face/facerec_sample.bmp";

        /// <summary>
        /// The sample image filepath.
        /// </summary>
        string sample_image_filepath;

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

            image_0_filepath = await Utils.getFilePathAsyncTask(IMAGE_0_FILENAME, cancellationToken: cts.Token);
            image_1_filepath = await Utils.getFilePathAsyncTask(IMAGE_1_FILENAME, cancellationToken: cts.Token);
            sample_image_filepath = await Utils.getFilePathAsyncTask(SAMPLE_IMAGE_FILENAME, cancellationToken: cts.Token);

            if (fpsMonitor != null)
                fpsMonitor.consoleText = "";

            Run();
        }

        private void Run()
        {
            if (string.IsNullOrEmpty(image_0_filepath) || string.IsNullOrEmpty(image_1_filepath) || string.IsNullOrEmpty(sample_image_filepath))
            {
                Debug.LogError(IMAGE_0_FILENAME + " or " + IMAGE_1_FILENAME + " or " + SAMPLE_IMAGE_FILENAME + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
            }

            List<Mat> images = new List<Mat>();
            List<int> labelsList = new List<int>();
            MatOfInt labels = new MatOfInt();
            images.Add(Imgcodecs.imread(image_0_filepath, Imgcodecs.IMREAD_GRAYSCALE));
            images.Add(Imgcodecs.imread(image_1_filepath, Imgcodecs.IMREAD_GRAYSCALE));
            labelsList.Add(0);
            labelsList.Add(1);
            labels.fromList(labelsList);

            Mat testSampleMat = Imgcodecs.imread(sample_image_filepath, Imgcodecs.IMREAD_GRAYSCALE);
            int testSampleLabel = 0;


            //foreach (Mat item in images)
            //{
            //    Debug.Log("images.ToString " + item.ToString());
            //}
            //foreach (int item in labelsList)
            //{
            //    Debug.Log("labels.ToString " + item.ToString());
            //}


            int[] predictedLabel = new int[1];
            double[] predictedConfidence = new double[1];

            BasicFaceRecognizer faceRecognizer = EigenFaceRecognizer.create();

            faceRecognizer.train(images, labels);
            faceRecognizer.predict(testSampleMat, predictedLabel, predictedConfidence);

            Debug.Log("Predicted class: " + predictedLabel[0] + " / " + "Actual class: " + testSampleLabel);
            Debug.Log("Confidence: " + predictedConfidence[0]);


            int imageSizeW = testSampleMat.cols();
            int imageSizeH = testSampleMat.rows();
            int label = predictedLabel[0];
            double confidence = predictedConfidence[0];

            Mat resultMat = new Mat(imageSizeH * 2, imageSizeW * 2, CvType.CV_8UC1, new Scalar(0));
            testSampleMat.copyTo(resultMat.submat(new Rect(imageSizeW / 2, 0, imageSizeW, imageSizeH)));
            images[0].copyTo(resultMat.submat(new Rect(0, imageSizeH, imageSizeW, imageSizeH)));
            images[1].copyTo(resultMat.submat(new Rect(imageSizeW, imageSizeH, imageSizeW, imageSizeH)));

            Imgproc.putText(resultMat, "TestSample", new Point(imageSizeW / 2 + 5, 15), Imgproc.FONT_HERSHEY_SIMPLEX, 0.4, new Scalar(255), 1, Imgproc.LINE_AA, false);
            Imgproc.rectangle(resultMat, new Rect(imageSizeW * label, imageSizeH, imageSizeW, imageSizeH), new Scalar(255), 2);
            Imgproc.putText(resultMat, "Predicted", new Point(imageSizeW * label + 5, imageSizeH + 15), Imgproc.FONT_HERSHEY_SIMPLEX, 0.4, new Scalar(255), 1, Imgproc.LINE_AA, false);
            Imgproc.putText(resultMat, "Confidence:", new Point(imageSizeW * label + 5, imageSizeH + 25), Imgproc.FONT_HERSHEY_SIMPLEX, 0.2, new Scalar(255), 1, Imgproc.LINE_AA, false);
            Imgproc.putText(resultMat, "   " + confidence, new Point(imageSizeW * label + 5, imageSizeH + 33), Imgproc.FONT_HERSHEY_SIMPLEX, 0.2, new Scalar(255), 1, Imgproc.LINE_AA, false);

            Texture2D texture = new Texture2D(resultMat.cols(), resultMat.rows(), TextureFormat.RGBA32, false);

            Utils.matToTexture2D(resultMat, texture);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
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