using System.Collections.Generic;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.FaceModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Face Recognizer Example
    /// Trains a small Eigenfaces model on two labeled faces and predicts identity of a test sample.
    ///
    /// Demonstrates:
    /// - Loading grayscale face images and assigning integer labels
    /// - Training BasicFaceRecognizer (Eigenfaces) on a tiny dataset
    /// - Predicting label and confidence, then visualizing the result grid
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Scalar"/>, <see cref="Point"/>, <see cref="Rect"/>
    /// - <see cref="BasicFaceRecognizer"/>, EigenFaceRecognizer.create, train, predict
    /// - <see cref="Imgcodecs"/>: imread
    /// - <see cref="Imgproc"/>: putText, rectangle
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// http://docs.opencv.org/modules/contrib/doc/facerec/facerec_tutorial.html#eigenfaces
    /// </para>
    /// </remarks>
    public class FaceRecognizerExample : MonoBehaviour
    {
        // Constants
        private static readonly string IMAGE_0_FILEPATH = "OpenCVForUnityExamples/face/facerec_0.bmp";

        private static readonly string IMAGE_1_FILEPATH = "OpenCVForUnityExamples/face/facerec_1.bmp";

        private static readonly string SAMPLE_IMAGE_FILEPATH = "OpenCVForUnityExamples/face/facerec_sample.bmp";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
        private string _image0Filepath;

        private string _image1Filepath;

        private string _sampleImageFilepath;

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

            _image0Filepath = await OpenCVForUnityEnv.GetFilePathAsync(IMAGE_0_FILEPATH, cancellationToken: _cts.Token);
            _image1Filepath = await OpenCVForUnityEnv.GetFilePathAsync(IMAGE_1_FILEPATH, cancellationToken: _cts.Token);
            _sampleImageFilepath = await OpenCVForUnityEnv.GetFilePathAsync(SAMPLE_IMAGE_FILEPATH, cancellationToken: _cts.Token);

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
            if (string.IsNullOrEmpty(_image0Filepath) || string.IsNullOrEmpty(_image1Filepath) || string.IsNullOrEmpty(_sampleImageFilepath))
            {
                Debug.LogError(IMAGE_0_FILEPATH + " or " + IMAGE_1_FILEPATH + " or " + SAMPLE_IMAGE_FILEPATH + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("image file is not loaded.\nPlease read console message.", 20000);
                }

                return;
            }

            // Load training and test images as single-channel (grayscale) Mats.
            Mat image0Mat = Imgcodecs.imread(_image0Filepath, Imgcodecs.IMREAD_GRAYSCALE);
            Mat image1Mat = Imgcodecs.imread(_image1Filepath, Imgcodecs.IMREAD_GRAYSCALE);
            Mat testSampleMat = Imgcodecs.imread(_sampleImageFilepath, Imgcodecs.IMREAD_GRAYSCALE);

            if (image0Mat.empty() || image1Mat.empty() || testSampleMat.empty())
            {
                Debug.LogError(IMAGE_0_FILEPATH + " or " + IMAGE_1_FILEPATH + " or " + SAMPLE_IMAGE_FILEPATH + " could not be read or is empty. Please move valid image files from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("image file is not loaded.\nPlease read console message.", 20000);
                }

                image0Mat.Dispose();
                image1Mat.Dispose();
                testSampleMat.Dispose();
                return;
            }

            List<Mat> images = new List<Mat>();
            List<int> labelsList = new List<int>();
            MatOfInt labels = new MatOfInt();
            images.Add(image0Mat);
            images.Add(image1Mat);
            labelsList.Add(0);
            labelsList.Add(1);
            labels.fromList(labelsList);

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

            // Eigenfaces: train on labeled images, then predict nearest class for the test face.
            BasicFaceRecognizer faceRecognizer = EigenFaceRecognizer.create();

            faceRecognizer.train(images, labels);
            faceRecognizer.predict(testSampleMat, predictedLabel, predictedConfidence);

            Debug.Log("Predicted class: " + predictedLabel[0] + " / " + "Actual class: " + testSampleLabel, this);
            Debug.Log("Confidence: " + predictedConfidence[0], this);

            int imageSizeW = testSampleMat.cols();
            int imageSizeH = testSampleMat.rows();
            int label = predictedLabel[0];
            double confidence = predictedConfidence[0];

            // Build a 2x2 grid: test sample on top, training images below; highlight predicted match.
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

            OpenCVMatUnityUtils.MatToTexture2D(resultMat, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;

            image0Mat.Dispose();
            image1Mat.Dispose();
            testSampleMat.Dispose();
            resultMat.Dispose();
        }
    }
}
