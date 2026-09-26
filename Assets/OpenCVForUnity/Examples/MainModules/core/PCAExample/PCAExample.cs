using System.Collections.Generic;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.GeometryModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// PCA Example
    /// Finds contour orientations by applying Principal Component Analysis to each contour point set.
    ///
    /// Demonstrates:
    /// - Grayscale conversion and Otsu binarization
    /// - Contour detection and area filtering
    /// - <see cref="Core"/>: PCACompute on contour points to obtain mean and principal eigenvector
    /// - Drawing the principal axis on each qualifying contour
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Point"/>, <see cref="Scalar"/>, <see cref="MatOfPoint"/>
    /// - <see cref="Imgcodecs"/>: imread
    /// - <see cref="Imgproc"/>: cvtColor, threshold, findContours, contourArea, drawContours, circle, line
    /// - <see cref="Core"/>: PCACompute
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// http://docs.opencv.org/3.2.0/d1/dee/tutorial_introduction_to_pca.html
    /// </para>
    /// </remarks>
    public class PCAExample : MonoBehaviour
    {
        // Constants
        private static readonly string IMAGE_FILEPATH = "OpenCVForUnityExamples/core/pca_test1.jpg";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        // Private Fields
        private string _imageFilepath;

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

            _imageFilepath = await OpenCVForUnityEnv.GetFilePathAsync(IMAGE_FILEPATH, cancellationToken: _cts.Token);

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
            Mat src = Imgcodecs.imread(_imageFilepath);
            if (src.empty())
            {
                Debug.LogError(IMAGE_FILEPATH + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("image file is not loaded.\nPlease read console message.", 20000);
                }

                src.Dispose();
                return;
            }

            Debug.Log("src.ToString() " + src.ToString(), this);

            // Convert to grayscale for contour extraction.
            Mat gray = new Mat();
            Imgproc.cvtColor(src, gray, Imgproc.COLOR_BGR2GRAY);
            // Binarize with Otsu's automatic threshold.
            Mat bw = new Mat();
            Imgproc.threshold(gray, bw, 50, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);
            // Extract all contours from the binary image.
            Mat hierarchy = new Mat();
            List<MatOfPoint> contours = new List<MatOfPoint>();
            Imgproc.findContours(bw, contours, hierarchy, Imgproc.RETR_LIST, Imgproc.CHAIN_APPROX_NONE);

            for (int i = 0; i < contours.Count; ++i)
            {
                // Skip contours outside a reasonable area range.
                double area = Geometry.contourArea(contours[i]);
                if (area < 1e2 || 1e5 < area)
                {
                    continue;
                }
                // Overlay contour outline for visualization.
                Imgproc.drawContours(src, contours, i, new Scalar(0, 0, 255), 2);

                // Pack contour (x, y) coordinates into a 2-column matrix for PCA.
                List<Point> pts = contours[i].toList();
                int sz = pts.Count;
                Mat data_pts = new Mat(sz, 2, CvType.CV_64FC1);
                for (int p = 0; p < data_pts.rows(); ++p)
                {
                    data_pts.put(p, 0, pts[p].x);
                    data_pts.put(p, 1, pts[p].y);
                }

                // Compute centroid (mean) and the first principal eigenvector.
                Mat mean = new Mat();
                Mat eigenvectors = new Mat();
                Core.PCACompute(data_pts, mean, eigenvectors, 1);
                Debug.Log("mean.dump() " + mean.dump(), this);
                Debug.Log("eigenvectors.dump() " + eigenvectors.dump(), this);

                Point cntr = new Point(mean.get(0, 0)[0], mean.get(0, 1)[0]);
                Point vec = new Point(eigenvectors.get(0, 0)[0], eigenvectors.get(0, 1)[0]);

                DrawAxis(src, cntr, vec, new Scalar(255, 255, 0), 150);

                data_pts.Dispose();
                mean.Dispose();
                eigenvectors.Dispose();
            }

            // Convert BGR to RGB for Unity texture display.
            Imgproc.cvtColor(src, src, Imgproc.COLOR_BGR2RGB);

            Texture2D texture = new Texture2D(src.cols(), src.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUnityUtils.MatToTexture2D(src, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
        }

        /// <summary>
        /// Draws an arrowed axis from a centroid along the principal eigenvector.
        /// </summary>
        private void DrawAxis(Mat img, Point start_pt, Point vec, Scalar color, double length)
        {
            int cv_AA = 16;

            // Scale the unit eigenvector to the desired axis length.
            Point end_pt = new Point(start_pt.x + length * vec.x, start_pt.y + length * vec.y);

            Imgproc.circle(img, start_pt, 5, color, 1);

            Imgproc.line(img, start_pt, end_pt, color, 1, cv_AA, 0);

            double angle = System.Math.Atan2(vec.y, vec.x);

            double qx0 = end_pt.x - 9 * System.Math.Cos(angle + System.Math.PI / 4);
            double qy0 = end_pt.y - 9 * System.Math.Sin(angle + System.Math.PI / 4);
            Imgproc.line(img, end_pt, new Point(qx0, qy0), color, 1, cv_AA, 0);

            double qx1 = end_pt.x - 9 * System.Math.Cos(angle - System.Math.PI / 4);
            double qy1 = end_pt.y - 9 * System.Math.Sin(angle - System.Math.PI / 4);
            Imgproc.line(img, end_pt, new Point(qx1, qy1), color, 1, cv_AA, 0);
        }
    }
}
