using System.Collections.Generic;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.FaceModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
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
        // Constants
        /// <summary>
        /// IMAGE_0_FILENAME
        /// </summary>
        protected static readonly string IMAGE_0_FILENAME = "OpenCVForUnityExamples/face/facerec_0.bmp";

        /// <summary>
        /// IMAGE_1_FILENAME
        /// </summary>
        protected static readonly string IMAGE_1_FILENAME = "OpenCVForUnityExamples/face/facerec_1.bmp";

        /// <summary>
        /// SAMPLE_IMAGE_FILENAME
        /// </summary>
        protected static readonly string SAMPLE_IMAGE_FILENAME = "OpenCVForUnityExamples/face/facerec_sample.bmp";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
        /// <summary>
        /// The image 0 filepath.
        /// </summary>
        private string _image0Filepath;

        /// <summary>
        /// The image 1 filepath.
        /// </summary>
        private string _image1Filepath;

        /// <summary>
        /// The sample image filepath.
        /// </summary>
        private string _sampleImageFilepath;

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

            _image0Filepath = await OpenCVEnv.GetFilePathTaskAsync(IMAGE_0_FILENAME, cancellationToken: _cts.Token);
            _image1Filepath = await OpenCVEnv.GetFilePathTaskAsync(IMAGE_1_FILENAME, cancellationToken: _cts.Token);
            _sampleImageFilepath = await OpenCVEnv.GetFilePathTaskAsync(SAMPLE_IMAGE_FILENAME, cancellationToken: _cts.Token);

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
            if (string.IsNullOrEmpty(_image0Filepath) || string.IsNullOrEmpty(_image1Filepath) || string.IsNullOrEmpty(_sampleImageFilepath))
            {
                Debug.LogError(IMAGE_0_FILENAME + " or " + IMAGE_1_FILENAME + " or " + SAMPLE_IMAGE_FILENAME + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
            }

            List<Mat> images = new List<Mat>();
            List<int> labelsList = new List<int>();
            MatOfInt labels = new MatOfInt();
            images.Add(Imgcodecs.imread(_image0Filepath, Imgcodecs.IMREAD_GRAYSCALE));
            images.Add(Imgcodecs.imread(_image1Filepath, Imgcodecs.IMREAD_GRAYSCALE));
            labelsList.Add(0);
            labelsList.Add(1);
            labels.fromList(labelsList);

            Mat testSampleMat = Imgcodecs.imread(_sampleImageFilepath, Imgcodecs.IMREAD_GRAYSCALE);
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

            OpenCVMatUtils.MatToTexture2D(resultMat, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
        }
    }
}
