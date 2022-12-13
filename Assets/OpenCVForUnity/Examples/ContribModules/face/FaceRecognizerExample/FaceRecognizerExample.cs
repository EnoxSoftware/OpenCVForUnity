using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.FaceModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
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

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        void Start()
        {
#if UNITY_WEBGL
            getFilePath_Coroutine = GetFilePath ();
            StartCoroutine (getFilePath_Coroutine);
#else
            image_0_filepath = Utils.getFilePath(IMAGE_0_FILENAME);
            image_1_filepath = Utils.getFilePath(IMAGE_1_FILENAME);
            sample_image_filepath = Utils.getFilePath(SAMPLE_IMAGE_FILENAME);
            Run();
#endif
        }

#if UNITY_WEBGL
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync (IMAGE_0_FILENAME, (result) => {
                image_0_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync (IMAGE_1_FILENAME, (result) => {
                image_1_filepath = result;
            });
            yield return getFilePathAsync_1_Coroutine;

            var getFilePathAsync_sample_Coroutine = Utils.getFilePathAsync (SAMPLE_IMAGE_FILENAME, (result) => {
                sample_image_filepath = result;
            });
            yield return getFilePathAsync_sample_Coroutine;

            getFilePath_Coroutine = null;

            Run ();
        }
#endif

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

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;
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
#if UNITY_WEBGL
            if (getFilePath_Coroutine != null) {
                StopCoroutine (getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose ();
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