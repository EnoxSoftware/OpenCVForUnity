using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// HoughLinesP Example
    /// Detects line segments in a chessboard image using the probabilistic Hough transform.
    ///
    /// Demonstrates:
    /// - Canny edge detection as a preprocessing step
    /// - Line segment detection with <see cref="Imgproc.HoughLinesP"/>
    /// - Overlaying detected segments on the source image
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Point"/>, <see cref="Scalar"/>
    /// - <see cref="Imgproc"/>: cvtColor, Canny, HoughLinesP, line, COLOR_RGB2GRAY
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// http://docs.opencv.org/3.1.0/d9/db0/tutorial_hough_lines.html
    /// </para>
    /// </remarks>
    public class HoughLinesPExample : MonoBehaviour
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

            Mat grayMat = new Mat();
            Imgproc.cvtColor(imgMat, grayMat, Imgproc.COLOR_RGB2GRAY);

            // Extract edges for the Hough accumulator.
            Imgproc.Canny(grayMat, grayMat, 50, 200);

            Mat lines = new Mat();

            // Detect line segments; output columns are (x1, y1, x2, y2).
            Imgproc.HoughLinesP(grayMat, lines, 1, Mathf.PI / 180, 50, 50, 10);

            //Debug.Log ("lines.toStirng() " + lines.ToString ());
            //Debug.Log ("lines.dump()" + lines.dump ());

            int[] linesArray = new int[lines.cols() * lines.rows() * lines.channels()];
            lines.get(0, 0, linesArray);

            // Draw each detected segment on the color image.
            for (int i = 0; i < linesArray.Length; i = i + 4)
            {
                Imgproc.line(imgMat, new Point(linesArray[i + 0], linesArray[i + 1]), new Point(linesArray[i + 2], linesArray[i + 3]), new Scalar(255, 0, 0), 2);
            }

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
