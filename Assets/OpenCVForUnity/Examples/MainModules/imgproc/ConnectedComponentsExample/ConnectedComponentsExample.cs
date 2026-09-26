using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// ConnectedComponents Example
    /// Labels connected regions in a binary image and visualizes stats, bounding boxes, and centroids.
    ///
    /// Demonstrates:
    /// - Connected-component labeling with <see cref="Imgproc.connectedComponentsWithStats"/>
    /// - Per-label colorization from the label map
    /// - Drawing bounding rectangles, centroids, and label indices
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Point"/>, <see cref="Scalar"/>, <see cref="OpenCVForUnity.CoreModule.Rect"/>
    /// - <see cref="Imgproc"/>: connectedComponentsWithStats, rectangle, circle, putText, CC_STAT_LEFT, CC_STAT_TOP, CC_STAT_WIDTH, CC_STAT_HEIGHT
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// http://qiita.com/wakaba130/items/9d921b8b3eb812e4f197
    /// </para>
    /// </remarks>
    public class ConnectedComponentsExample : MonoBehaviour
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
            Texture2D imgTexture = Resources.Load("matchshapes") as Texture2D;

            Mat srcMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC1);

            OpenCVMatUnityUtils.Texture2DToMat(imgTexture, srcMat);
            Debug.Log("srcMat.ToString() " + srcMat.ToString(), this);

            Mat dstMat = new Mat(srcMat.size(), CvType.CV_8UC3);

            // Label each connected component and collect per-label statistics.
            Mat labels = new Mat();
            Mat stats = new Mat();
            Mat centroids = new Mat();
            int total = Imgproc.connectedComponentsWithStats(srcMat, labels, stats, centroids);

            Debug.Log("labels.ToString() " + labels.ToString(), this);
            Debug.Log("stats.ToString() " + stats.ToString(), this);
            Debug.Log("centroids.ToString() " + centroids.ToString(), this);
            Debug.Log("total " + total, this);

            // Assign a random color to each label (background label 0 is black).
            List<Scalar> colors = new List<Scalar>(total);
            colors.Add(new Scalar(0, 0, 0));
            for (int i = 1; i < total; ++i)
            {
                colors.Add(new Scalar(Random.Range(0, 255), Random.Range(0, 255), Random.Range(0, 255)));
            }

            // Paint each pixel with its label color.
            for (int i = 0; i < dstMat.rows(); ++i)
            {
                for (int j = 0; j < dstMat.cols(); ++j)
                {
                    Scalar color = colors[(int)labels.get(i, j)[0]];
                    dstMat.put(i, j, color.val[0], color.val[1], color.val[2]);
                }
            }

            // Draw a green bounding box per component from CC_STAT_* columns.
            for (int i = 1; i < total; ++i)
            {

                int x = (int)stats.get(i, Imgproc.CC_STAT_LEFT)[0];
                int y = (int)stats.get(i, Imgproc.CC_STAT_TOP)[0];
                int height = (int)stats.get(i, Imgproc.CC_STAT_HEIGHT)[0];
                int width = (int)stats.get(i, Imgproc.CC_STAT_WIDTH)[0];

                OpenCVForUnity.CoreModule.Rect rect = new OpenCVForUnity.CoreModule.Rect(x, y, width, height);

                Imgproc.rectangle(dstMat, rect.tl(), rect.br(), new Scalar(0, 255, 0), 2);
            }

            // Mark each component centroid with a filled circle.
            for (int i = 1; i < total; ++i)
            {

                int x = (int)centroids.get(i, 0)[0];
                int y = (int)centroids.get(i, 1)[0];

                Imgproc.circle(dstMat, new Point(x, y), 3, new Scalar(255, 0, 0), -1);
            }

            // Annotate each component with its label index.
            for (int i = 1; i < total; ++i)
            {

                int x = (int)stats.get(i, Imgproc.CC_STAT_LEFT)[0];
                int y = (int)stats.get(i, Imgproc.CC_STAT_TOP)[0];

                Imgproc.putText(dstMat, "" + i, new Point(x + 5, y + 15), Imgproc.FONT_HERSHEY_COMPLEX, 0.5, new Scalar(255, 255, 0), 2);
            }

            Texture2D texture = new Texture2D(dstMat.cols(), dstMat.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUnityUtils.MatToTexture2D(dstMat, texture);

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
