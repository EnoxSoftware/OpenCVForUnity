using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.MlModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// SVM Example
    /// Trains a linear SVM on 2D points and visualizes the decision boundary and support vectors.
    ///
    /// Demonstrates:
    /// - Preparing labeled training data and training <see cref="SVM"/> (C_SVC, LINEAR kernel)
    /// - Classifying every pixel to render decision regions
    /// - Overlaying training points and uncompressed support vectors
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Point"/>, <see cref="Scalar"/>, <see cref="TermCriteria"/>
    /// - <see cref="SVM"/>: create, setType, setKernel, setTermCriteria, train, predict, getUncompressedSupportVectors, C_SVC, LINEAR
    /// - <see cref="Ml"/>: ROW_SAMPLE
    /// - <see cref="Imgproc"/>: circle, cvtColor, LINE_8, COLOR_BGR2RGB
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://docs.opencv.org/4.12.0/d1/d73/tutorial_introduction_to_svm.html#gsc.tab=0
    /// </para>
    /// </remarks>
    public class SVMExample : MonoBehaviour
    {
        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        // Unity Lifecycle Methods
        private void Start()
        {
            // Set up training data
            int[] labels = { 1, -1, -1, -1 };
            float[] trainingData = { 501, 10, 255, 10, 501, 255, 10, 501 };
            Mat trainingDataMat = new Mat(4, 2, CvType.CV_32FC1);
            trainingDataMat.put(0, 0, trainingData);
            Mat labelsMat = new Mat(4, 1, CvType.CV_32SC1);
            labelsMat.put(0, 0, labels);

            // Train a linear C-SVC classifier.
            SVM svm = SVM.create();
            svm.setType(SVM.C_SVC);
            svm.setKernel(SVM.LINEAR);
            svm.setTermCriteria(new TermCriteria(TermCriteria.MAX_ITER, 100, 1e-6));
            svm.train(trainingDataMat, Ml.ROW_SAMPLE, labelsMat);

            // Data for visual representation
            int width = 512, height = 512;
            Mat image = Mat.zeros(height, width, CvType.CV_8UC3);

            // Classify every pixel to color the +1 (green) and -1 (red) decision regions.
            byte[] imageData = new byte[(int)(image.total() * image.channels())];
            Mat sampleMat = new Mat(1, 2, CvType.CV_32F);
            float[] sampleMatData = new float[(int)(sampleMat.total() * sampleMat.channels())];
            for (int i = 0; i < image.rows(); i++)
            {
                for (int j = 0; j < image.cols(); j++)
                {
                    sampleMatData[0] = j;
                    sampleMatData[1] = i;
                    sampleMat.put(0, 0, sampleMatData);

                    float response = svm.predict(sampleMat);

                    if (response == 1)
                    {
                        imageData[(i * image.cols() + j) * image.channels()] = 0;
                        imageData[(i * image.cols() + j) * image.channels() + 1] = (byte)255;
                        imageData[(i * image.cols() + j) * image.channels() + 2] = 0;
                    }
                    else if (response == -1)
                    {
                        imageData[(i * image.cols() + j) * image.channels()] = (byte)255;
                        imageData[(i * image.cols() + j) * image.channels() + 1] = 0;
                        imageData[(i * image.cols() + j) * image.channels() + 2] = 0;
                    }
                }
            }
            image.put(0, 0, imageData);

            // Overlay the four training samples.
            int thickness = -1;
            int lineType = Imgproc.LINE_8;
            Imgproc.circle(image, new Point(501, 10), 5, new Scalar(0, 0, 0), thickness, lineType, 0);
            Imgproc.circle(image, new Point(255, 10), 5, new Scalar(255, 255, 255), thickness, lineType, 0);
            Imgproc.circle(image, new Point(501, 255), 5, new Scalar(255, 255, 255), thickness, lineType, 0);
            Imgproc.circle(image, new Point(10, 501), 5, new Scalar(255, 255, 255), thickness, lineType, 0);

            // Highlight support vectors in gray.
            thickness = 2;
            Mat sv = svm.getUncompressedSupportVectors();
            float[] svData = new float[(int)(sv.total() * sv.channels())];
            sv.get(0, 0, svData);
            for (int i = 0; i < sv.rows(); ++i)
            {
                Imgproc.circle(image, new Point(svData[i * sv.cols()], svData[i * sv.cols() + 1]), 6,
                        new Scalar(128, 128, 128), thickness, lineType, 0);
            }

            // Convert BGR to RGB for Unity texture display.
            Imgproc.cvtColor(image, image, Imgproc.COLOR_BGR2RGB);

            Texture2D texture = new Texture2D(image.width(), image.height(), TextureFormat.RGBA32, false);
            OpenCVMatUnityUtils.MatToTexture2D(image, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
        }

        private void Update()
        {

        }

        // Public Methods
        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }
    }
}
