using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.MlModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// SVM Example
    /// An example of  find a separating straight line using the Support Vector Machines (SVM).
    /// Referring to http://docs.opencv.org/3.1.0/d1/d73/tutorial_introduction_to_svm.html#gsc.tab=0.
    /// </summary>
    public class SVMExample : MonoBehaviour
    {
        // Use this for initialization
        void Start()
        {
            // Data for visual representation
            int width = 512, height = 512;
            Mat image = Mat.zeros(height, width, CvType.CV_8UC4);

            // Set up training data
            int[] labels = { 1, -1, -1, -1 };
            float[] trainingData = { 501, 10, 255, 10, 501, 255, 10, 501 };
            Mat trainingDataMat = new Mat(4, 2, CvType.CV_32FC1);
            trainingDataMat.put(0, 0, trainingData);
            Mat labelsMat = new Mat(4, 1, CvType.CV_32SC1);
            labelsMat.put(0, 0, labels);

            // Train the SVM
            SVM svm = SVM.create();
            svm.setType(SVM.C_SVC);
            svm.setKernel(SVM.LINEAR);
            svm.setTermCriteria(new TermCriteria(TermCriteria.MAX_ITER, 100, 1e-6));
            svm.train(trainingDataMat, Ml.ROW_SAMPLE, labelsMat);

            // Show the decision regions given by the SVM
            byte[] green = { 0, 255, 0, 255 };
            byte[] blue = { 0, 0, 255, 255 };
            for (int i = 0; i < image.rows(); ++i)
                for (int j = 0; j < image.cols(); ++j)
                {
                    Mat sampleMat = new Mat(1, 2, CvType.CV_32FC1);
                    sampleMat.put(0, 0, j, i);

                    float response = svm.predict(sampleMat);
                    if (response == 1)
                        image.put(i, j, green);
                    else if (response == -1)
                        image.put(i, j, blue);
                }

            // Show the training data
            int thickness = -1;
            int lineType = 8;

            Imgproc.circle(image, new Point(501, 10), 5, new Scalar(0, 0, 0, 255), thickness, lineType, 0);
            Imgproc.circle(image, new Point(255, 10), 5, new Scalar(255, 255, 255, 255), thickness, lineType, 0);
            Imgproc.circle(image, new Point(501, 255), 5, new Scalar(255, 255, 255, 255), thickness, lineType, 0);
            Imgproc.circle(image, new Point(10, 501), 5, new Scalar(255, 255, 255, 255), thickness, lineType, 0);

            // Show support vectors
            thickness = 2;
            lineType = 8;
            Mat sv = svm.getUncompressedSupportVectors();
            //Debug.Log ("sv.ToString() " + sv.ToString ());
            //Debug.Log ("sv.dump() " + sv.dump ());
            for (int i = 0; i < sv.rows(); ++i)
            {
                Imgproc.circle(image, new Point((int)sv.get(i, 0)[0], (int)sv.get(i, 1)[0]), 6, new Scalar(128, 128, 128, 255), thickness, lineType, 0);
            }


            Texture2D texture = new Texture2D(image.width(), image.height(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(image, texture);
            gameObject.GetComponent<Renderer>().material.mainTexture = texture;
        }

        // Update is called once per frame
        void Update()
        {

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