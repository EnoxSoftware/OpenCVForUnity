using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.MlModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenCVDebug = OpenCVForUnity.Extensions.OpenCVDebug;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// KNN Example
    /// Classifies a test point by majority vote among its k nearest training neighbours.
    ///
    /// Demonstrates:
    /// - Generating random 2D training data and binary class labels
    /// - Training <see cref="KNearest"/> and querying with <see cref="KNearest.findNearest"/>
    /// - Visualizing training points, the query point, and the k-th neighbour distance
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Point"/>, <see cref="Scalar"/>
    /// - <see cref="Core"/>: randu
    /// - <see cref="KNearest"/>: create, train, findNearest
    /// - <see cref="Ml"/>: ROW_SAMPLE
    /// - <see cref="Imgproc"/>: circle, putText
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://docs.opencv.org/4.x/d5/d26/tutorial_py_knn_understanding.html
    /// </para>
    /// </remarks>
    public class KNNExample : MonoBehaviour
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
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            // Feature set containing (x,y) values of 25 known/training data
            Mat trainData = new Mat(25, 2, CvType.CV_32FC1);
            using (Mat trainDataInt = new Mat(25, 2, CvType.CV_16SC1))
            {
                Core.randu(trainDataInt, 0, 100); // random values
                trainDataInt.convertTo(trainData, CvType.CV_32FC1);
            }
            //Debug.Log(trainData.dump());

            // Label each point as class 0 (Red) or 1 (Blue).
            Mat responses = new Mat(25, 1, CvType.CV_32FC1);
            using (Mat responsesInt = new Mat(25, 1, CvType.CV_16SC1))
            {
                Core.randu(responsesInt, 0, 2); // random values
                responsesInt.convertTo(responses, CvType.CV_32FC1);
            }
            //Debug.Log(responses.dump());

            KNearest knn = KNearest.create();
            knn.train(trainData, Ml.ROW_SAMPLE, responses);

            // Classify the newcomer using its 3 nearest neighbours.
            Mat newcomer = new Mat(1, 2, CvType.CV_32FC1, new Scalar(50, 50));
            Mat results = new Mat();
            Mat neighbours = new Mat();
            Mat dist = new Mat();
            knn.findNearest(newcomer, 3, results, neighbours, dist);

            Mat plotMat = new Mat(500, 500, CvType.CV_8UC4, new Scalar(255, 255, 255, 255));

            // Plot training points colored by class label.
            for (int i = 0; i < trainData.rows(); i++)
            {
                bool isRed = ((int)responses.get(i, 0)[0] == 0);

                double x = trainData.get(i, 0)[0];
                double y = trainData.get(i, 1)[0];

                Imgproc.circle(plotMat, new Point(x * 5f, y * 5f), 5, isRed ? new Scalar(255, 0, 0, 255) : new Scalar(0, 0, 255, 255), -1);
            }
            // Plot the query point (green) and a circle showing the 3rd-neighbour distance.
            Imgproc.circle(plotMat, new Point(50f * 5f, 50f * 5f), 5, new Scalar(0, 255, 0, 255), -1);
            Imgproc.circle(plotMat, new Point(50f * 5f, 50f * 5f), (int)(Mathf.Sqrt((float)dist.get(0, 2)[0]) * 5f), new Scalar(0, 255, 0, 255), 1);

            Debug.Log("0:Red / 1:Blue", this);
            Debug.Log("result: " + results.dump(), this);
            Debug.Log("neighbours: " + neighbours.dump(), this);
            Debug.Log("distance: " + dist.dump(), this);

            Imgproc.putText(plotMat, "0:Red / 1:Blue", new Point(5, 30), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(0, 0, 0, 255));
            Imgproc.putText(plotMat, "result: " + results.dump(), new Point(5, 65), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(0, 0, 0, 255));
            Imgproc.putText(plotMat, "neighbours: " + neighbours.dump(), new Point(5, 100), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(0, 0, 0, 255));
            Imgproc.putText(plotMat, "distance: " + dist.dump(), new Point(5, 135), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(0, 0, 0, 255));

            Texture2D texture = new Texture2D(plotMat.cols(), plotMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUnityUtils.MatToTexture2D(plotMat, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;

            OpenCVDebug.SetDebugMode(false);
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
