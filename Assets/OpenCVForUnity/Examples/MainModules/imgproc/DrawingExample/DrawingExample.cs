using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Drawing Example
    /// Demonstrates basic vector drawing primitives and Hershey font variants on an image.
    ///
    /// Demonstrates:
    /// - Drawing lines, rectangles, circles, arrowed lines, and ellipses
    /// - Rendering text with multiple <see cref="Imgproc"/> font faces and italic flag
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Point"/>, <see cref="Scalar"/>, <see cref="Size"/>
    /// - <see cref="Imgproc"/>: line, rectangle, circle, arrowedLine, ellipse, putText, FONT_HERSHEY_*, LINE_AA, LINE_8
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    public class DrawingExample : MonoBehaviour
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
            Texture2D imgTexture = Resources.Load("chessboard") as Texture2D;

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC3);

            OpenCVMatUnityUtils.Texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString(), this);

            // Basic drawing primitives.
            Imgproc.line(imgMat, new Point(50, 50), new Point(400, 105), new Scalar(0, 0, 200), 3);

            Imgproc.rectangle(imgMat, new Point(150, 200), new Point(300, 300), new Scalar(0, 200, 0), 5);

            Imgproc.circle(imgMat, new Point(500, 300), 80, new Scalar(200, 0, 0), 1);

            Imgproc.arrowedLine(imgMat, new Point(100, 500), new Point(550, 350), new Scalar(255, 255, 0), 4, Imgproc.LINE_8, 0, 0.1);

            double angle = 100;
            Imgproc.ellipse(imgMat, new Point(200, 400), new Size(80, 150), angle, angle - 200, angle + 100, new Scalar(255, 255, 255), -1);

            // Hershey font face identifiers for putText samples.
            int[] face = {Imgproc.FONT_HERSHEY_SIMPLEX, Imgproc.FONT_HERSHEY_PLAIN, Imgproc.FONT_HERSHEY_DUPLEX, Imgproc.FONT_HERSHEY_COMPLEX,
                Imgproc.FONT_HERSHEY_TRIPLEX, Imgproc.FONT_HERSHEY_COMPLEX_SMALL, Imgproc.FONT_HERSHEY_SCRIPT_SIMPLEX,
                Imgproc.FONT_HERSHEY_SCRIPT_COMPLEX, Imgproc.FONT_ITALIC
            };

            // Render each font face (left column) and the italic variant (right column).
            Imgproc.putText(imgMat, "OpenCV", new Point(50, 50), face[0], 1.2, new Scalar(0, 0, 200), 2, Imgproc.LINE_AA, false);
            Imgproc.putText(imgMat, "OpenCV", new Point(50, 100), face[1], 1.2, new Scalar(0, 200, 0), 2, Imgproc.LINE_AA, false);
            Imgproc.putText(imgMat, "OpenCV", new Point(50, 150), face[2], 1.2, new Scalar(200, 0, 0), 2, Imgproc.LINE_AA, false);
            Imgproc.putText(imgMat, "OpenCV", new Point(50, 200), face[3], 1.2, new Scalar(0, 100, 100), 2, Imgproc.LINE_AA, false);
            Imgproc.putText(imgMat, "OpenCV", new Point(50, 250), face[4], 1.2, new Scalar(100, 100, 0), 2, Imgproc.LINE_AA, false);
            Imgproc.putText(imgMat, "OpenCV", new Point(50, 300), face[5], 1.2, new Scalar(100, 0, 100), 2, Imgproc.LINE_AA, false);
            Imgproc.putText(imgMat, "OpenCV", new Point(50, 350), face[6], 1.2, new Scalar(100, 100, 100), 2, Imgproc.LINE_AA, false);
            Imgproc.putText(imgMat, "OpenCV", new Point(50, 400), face[7], 1.2, new Scalar(100, 100, 200), 2, Imgproc.LINE_AA, false);
            Imgproc.putText(imgMat, "OpenCV", new Point(300, 50), face[0] | face[8], 1.2, new Scalar(100, 200, 100), 2, Imgproc.LINE_AA, false);
            Imgproc.putText(imgMat, "OpenCV", new Point(300, 100), face[1] | face[8], 1.2, new Scalar(200, 100, 100), 2, Imgproc.LINE_AA, false);
            Imgproc.putText(imgMat, "OpenCV", new Point(300, 150), face[2] | face[8], 1.2, new Scalar(200, 200, 100), 2, Imgproc.LINE_AA, false);
            Imgproc.putText(imgMat, "OpenCV", new Point(300, 200), face[3] | face[8], 1.2, new Scalar(200, 100, 200), 2, Imgproc.LINE_AA, false);
            Imgproc.putText(imgMat, "OpenCV", new Point(300, 250), face[4] | face[8], 1.2, new Scalar(100, 200, 200), 2, Imgproc.LINE_AA, false);
            Imgproc.putText(imgMat, "OpenCV", new Point(300, 300), face[5] | face[8], 1.2, new Scalar(100, 200, 255), 2, Imgproc.LINE_AA, false);
            Imgproc.putText(imgMat, "OpenCV", new Point(300, 350), face[6] | face[8], 1.2, new Scalar(100, 255, 200), 2, Imgproc.LINE_AA, false);
            Imgproc.putText(imgMat, "OpenCV", new Point(300, 400), face[7] | face[8], 1.2, new Scalar(255, 200, 100), 2, Imgproc.LINE_AA, false);

            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUnityUtils.MatToTexture2D(imgMat, texture);

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
