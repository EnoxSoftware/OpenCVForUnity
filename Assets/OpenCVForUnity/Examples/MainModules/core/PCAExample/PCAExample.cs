using System.Collections.Generic;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// PCA Example
    /// An example of Principal Component Analysis using the PCA class.
    /// http://docs.opencv.org/3.2.0/d1/dee/tutorial_introduction_to_pca.html
    /// </summary>
    public class PCAExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// IMAGE_FILENAME
        /// </summary>
        protected static readonly string IMAGE_FILENAME = "OpenCVForUnityExamples/core/pca_test1.jpg";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        // Private Fields
        /// <summary>
        /// The image filepath.
        /// </summary>
        private string _imageFilepath;

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

            _imageFilepath = await OpenCVEnv.GetFilePathTaskAsync(IMAGE_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        private void Update()
        {

        }

        private void OnDisable()
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
            Mat src = Imgcodecs.imread(_imageFilepath);
            if (src.empty())
            {
                Debug.LogError(IMAGE_FILENAME + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
            }

            Debug.Log("src.ToString() " + src.ToString());

            // Convert image to grayscale
            Mat gray = new Mat();
            Imgproc.cvtColor(src, gray, Imgproc.COLOR_BGR2GRAY);
            // Convert image to binary
            Mat bw = new Mat();
            Imgproc.threshold(gray, bw, 50, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);
            // Find all the contours in the thresholded image

            Mat hierarchy = new Mat();
            List<MatOfPoint> contours = new List<MatOfPoint>();
            Imgproc.findContours(bw, contours, hierarchy, Imgproc.RETR_LIST, Imgproc.CHAIN_APPROX_NONE);

            for (int i = 0; i < contours.Count; ++i)
            {
                // Calculate the area of each contour
                double area = Imgproc.contourArea(contours[i]);
                // Ignore contours that are too small or too large
                if (area < 1e2 || 1e5 < area)
                    continue;
                // Draw each contour only for visualisation purposes
                Imgproc.drawContours(src, contours, i, new Scalar(0, 0, 255), 2);

                //Construct a buffer used by the pca analysis
                List<Point> pts = contours[i].toList();
                int sz = pts.Count;
                Mat data_pts = new Mat(sz, 2, CvType.CV_64FC1);
                for (int p = 0; p < data_pts.rows(); ++p)
                {
                    data_pts.put(p, 0, pts[p].x);
                    data_pts.put(p, 1, pts[p].y);
                }

                Mat mean = new Mat();
                Mat eigenvectors = new Mat();
                Core.PCACompute(data_pts, mean, eigenvectors, 1);
                Debug.Log("mean.dump() " + mean.dump());
                Debug.Log("eigenvectors.dump() " + eigenvectors.dump());

                Point cntr = new Point(mean.get(0, 0)[0], mean.get(0, 1)[0]);
                Point vec = new Point(eigenvectors.get(0, 0)[0], eigenvectors.get(0, 1)[0]);

                DrawAxis(src, cntr, vec, new Scalar(255, 255, 0), 150);

                data_pts.Dispose();
                mean.Dispose();
                eigenvectors.Dispose();
            }


            Imgproc.cvtColor(src, src, Imgproc.COLOR_BGR2RGB);

            Texture2D texture = new Texture2D(src.cols(), src.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUtils.MatToTexture2D(src, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
        }

        private void DrawAxis(Mat img, Point start_pt, Point vec, Scalar color, double length)
        {
            int CV_AA = 16;

            Point end_pt = new Point(start_pt.x + length * vec.x, start_pt.y + length * vec.y);

            Imgproc.circle(img, start_pt, 5, color, 1);

            Imgproc.line(img, start_pt, end_pt, color, 1, CV_AA, 0);


            double angle = System.Math.Atan2(vec.y, vec.x);

            double qx0 = end_pt.x - 9 * System.Math.Cos(angle + System.Math.PI / 4);
            double qy0 = end_pt.y - 9 * System.Math.Sin(angle + System.Math.PI / 4);
            Imgproc.line(img, end_pt, new Point(qx0, qy0), color, 1, CV_AA, 0);

            double qx1 = end_pt.x - 9 * System.Math.Cos(angle - System.Math.PI / 4);
            double qy1 = end_pt.y - 9 * System.Math.Sin(angle - System.Math.PI / 4);
            Imgproc.line(img, end_pt, new Point(qx1, qy1), color, 1, CV_AA, 0);
        }
    }
}
