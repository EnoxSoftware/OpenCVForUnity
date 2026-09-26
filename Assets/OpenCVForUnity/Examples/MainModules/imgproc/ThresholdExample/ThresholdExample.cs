using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Threshold Example
    /// Binarizes a grayscale chessboard image using Otsu's automatic threshold.
    ///
    /// Demonstrates:
    /// - Loading a grayscale texture into a <see cref="Mat"/>
    /// - Global binarization with <see cref="Imgproc.threshold"/> and THRESH_OTSU
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>
    /// - <see cref="Imgproc"/>: threshold
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// http://docs.opencv.org/trunk/d7/d4d/tutorial_py_thresholding.html
    /// </para>
    /// </remarks>
    public class ThresholdExample : MonoBehaviour
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

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC1);

            OpenCVMatUnityUtils.Texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString(), this);

            // Otsu selects the optimal threshold automatically (THRESH_BINARY | THRESH_OTSU).
            Imgproc.threshold(imgMat, imgMat, 0, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);

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
