using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// MatchTemplate Example
    /// Locates a template patch within a source image using normalized cross-correlation.
    ///
    /// Demonstrates:
    /// - Sliding-window template matching with <see cref="Imgproc.matchTemplate"/> (TM_CCOEFF_NORMED)
    /// - Thresholding the correlation map to keep strong matches
    /// - Drawing match bounding rectangles on the source image
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Point"/>, <see cref="Scalar"/>
    /// - <see cref="Imgproc"/>: matchTemplate, threshold, rectangle, TM_CCOEFF_NORMED, THRESH_TOZERO
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// http://docs.opencv.org/3.2.0/de/da9/tutorial_template_matching.html
    /// </para>
    /// </remarks>
    public class MatchTemplateExample : MonoBehaviour
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
            Texture2D imgTexture = Resources.Load("face") as Texture2D;
            Texture2D tempTexture = Resources.Load("template") as Texture2D;
            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);
            Mat tempMat = new Mat(tempTexture.height, tempTexture.width, CvType.CV_8UC4);
            OpenCVMatUnityUtils.Texture2DToMat(imgTexture, imgMat);
            OpenCVMatUnityUtils.Texture2DToMat(tempTexture, tempMat);

            //Create the result mat
            int result_cols = imgMat.cols() - tempMat.cols() + 1;
            int result_rows = imgMat.rows() - tempMat.rows() + 1;
            Mat result = new Mat(result_rows, result_cols, CvType.CV_32FC1);

            int match_method = Imgproc.TM_CCOEFF_NORMED;

            // Compute normalized correlation coefficients between template and every window.
            Imgproc.matchTemplate(imgMat, tempMat, result, match_method);

            // Keep only locations with correlation >= 0.8.
            Imgproc.threshold(result, result, 0.8, 1.0, Imgproc.THRESH_TOZERO);//threshold = 0.8

            // Draw a rectangle at each location that exceeds the threshold.
            for (int i = 0; i < result.rows(); i++)
            {
                for (int j = 0; j < result.cols(); j++)
                {
                    if (result.get(i, j)[0] > 0)
                    {

                        Imgproc.rectangle(imgMat, new Point(j, i), new Point(j + tempMat.cols(), i + tempMat.rows()), new Scalar(255, 0, 0, 255), 2);
                        Debug.Log("value" + result.get(i, j)[0], this);
                    }
                }
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
