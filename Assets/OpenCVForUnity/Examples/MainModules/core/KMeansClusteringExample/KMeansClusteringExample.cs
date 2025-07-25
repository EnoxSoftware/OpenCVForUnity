using System.Collections;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// KMeansClustering Example
    /// An example of K-Means clustering.
    /// </summary>
    public class KMeansClusteringExample : MonoBehaviour
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


            int k = 5;

            Texture2D imgTexture = Resources.Load("face") as Texture2D;

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            OpenCVMatUtils.Texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString());



            // convert to 3-channel color image (RGBA to RGB).
            Mat imgMatRGB = new Mat(imgMat.rows(), imgMat.cols(), CvType.CV_8UC3);
            Imgproc.cvtColor(imgMat, imgMatRGB, Imgproc.COLOR_RGBA2RGB);

            // reshape the image to be a 1 column matrix.
            Mat samples = imgMatRGB.reshape(3, imgMatRGB.cols() * imgMatRGB.rows());
            Mat samples32f = new Mat();
            samples.convertTo(samples32f, CvType.CV_32F, 1.0 / 255.0);

            // run k-means clustering algorithm to segment pixels in RGB color space.
            Mat labels = new Mat();
            TermCriteria criteria = new TermCriteria(TermCriteria.COUNT, 100, 1);
            Mat centers = new Mat();
            Core.kmeans(samples32f, k, labels, criteria, 1, Core.KMEANS_PP_CENTERS, centers);

            // make a each centroid represent all pixels in the cluster.
            centers.convertTo(centers, CvType.CV_8U, 255.0);
            int rows = 0;
            for (int y = 0; y < imgMatRGB.rows(); y++)
            {
                for (int x = 0; x < imgMatRGB.cols(); x++)
                {
                    int label = (int)labels.get(rows, 0)[0];
                    int r = (int)centers.get(label, 0)[0];
                    int g = (int)centers.get(label, 1)[0];
                    int b = (int)centers.get(label, 2)[0];
                    imgMatRGB.put(y, x, r, g, b);
                    rows++;
                }
            }

            // convert to 4-channel color image (RGB to RGBA).
            Imgproc.cvtColor(imgMatRGB, imgMat, Imgproc.COLOR_RGB2RGBA);



            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUtils.MatToTexture2D(imgMat, texture);

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
