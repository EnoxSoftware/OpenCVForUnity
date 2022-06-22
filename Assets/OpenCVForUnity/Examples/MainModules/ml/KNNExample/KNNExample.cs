using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.MlModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// KNN Example
    /// An example to understand the concepts of the k-Nearest Neighbour (kNN) algorithm.
    /// https://docs.opencv.org/4.x/d5/d26/tutorial_py_knn_understanding.html
    /// </summary>
    public class KNNExample : MonoBehaviour
    {
        // Use this for initialization
        void Start()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            // Feature set containing (x,y) values of 25 known/training data
            Mat trainData = new Mat(25, 2, CvType.CV_32FC1);
            using (Mat trainDataInt = new Mat(25, 2, CvType.CV_16SC1))
            {
                Core.randu(trainDataInt, 0, 100); // random values
                trainDataInt.convertTo(trainData, CvType.CV_32FC1);
            }
            //Debug.Log(trainData.dump());

            // Label each one either Red or Blue with numbers 0 and 1
            Mat responses = new Mat(25, 1, CvType.CV_32FC1);
            using (Mat responsesInt = new Mat(25, 1, CvType.CV_16SC1))
            {
                Core.randu(responsesInt, 0, 2); // random values
                responsesInt.convertTo(responses, CvType.CV_32FC1);
            }
            //Debug.Log(responses.dump());


            KNearest knn = KNearest.create();
            knn.train(trainData, Ml.ROW_SAMPLE, responses);

            Mat newcomer = new Mat(1, 2, CvType.CV_32FC1, new Scalar(50, 50));
            Mat results = new Mat();
            Mat neighbours = new Mat();
            Mat dist = new Mat();
            knn.findNearest(newcomer, 3, results, neighbours, dist);


            Mat plotMat = new Mat(500, 500, CvType.CV_8UC4, new Scalar(255, 255, 255, 255));

            // Take Red neighbours and plot them
            // Take Blue neighbours and plot them
            for (int i = 0; i < trainData.rows(); i++)
            {
                bool red = ((int)responses.get(i, 0)[0] == 0);

                double x = trainData.get(i, 0)[0];
                double y = trainData.get(i, 1)[0];

                Imgproc.circle(plotMat, new Point(x * 5f, y * 5f), 5, red ? new Scalar(255, 0, 0, 255) : new Scalar(0, 0, 255, 255), -1);
            }
            // Plot newcomer and the neighbours distance circle
            Imgproc.circle(plotMat, new Point(50f * 5f, 50f * 5f), 5, new Scalar(0, 255, 0, 255), -1);
            Imgproc.circle(plotMat, new Point(50f * 5f, 50f * 5f), (int)(Mathf.Sqrt((float)dist.get(0, 2)[0]) * 5f), new Scalar(0, 255, 0, 255), 1);


            Debug.Log("0:Red / 1:Blue");
            Debug.Log("result: " + results.dump());
            Debug.Log("neighbours: " + neighbours.dump());
            Debug.Log("distance: " + dist.dump());

            Imgproc.putText(plotMat, "0:Red / 1:Blue", new Point(5, 30), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(0, 0, 0, 255));
            Imgproc.putText(plotMat, "result: " + results.dump(), new Point(5, 65), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(0, 0, 0, 255));
            Imgproc.putText(plotMat, "neighbours: " + neighbours.dump(), new Point(5, 100), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(0, 0, 0, 255));
            Imgproc.putText(plotMat, "distance: " + dist.dump(), new Point(5, 135), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(0, 0, 0, 255));


            Texture2D texture = new Texture2D(plotMat.cols(), plotMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(plotMat, texture);
            gameObject.GetComponent<Renderer>().material.mainTexture = texture;


            Utils.setDebugMode(false);
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