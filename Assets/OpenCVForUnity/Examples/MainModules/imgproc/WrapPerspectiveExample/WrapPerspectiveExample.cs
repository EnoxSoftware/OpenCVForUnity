using OpenCVForUnity.CoreModule;
using OpenCVForUnity.GeometryModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// WrapPerspective Example
    /// Warps an image by a perspective transform defined by four source and destination corners.
    ///
    /// Demonstrates:
    /// - Loading a texture into a <see cref="Mat"/>
    /// - Computing a 3x3 homography with <see cref="Imgproc.getPerspectiveTransform"/>
    /// - Applying <see cref="Imgproc.warpPerspective"/> to distort the image
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Point"/>, <see cref="Size"/>
    /// - <see cref="Imgproc"/>: getPerspectiveTransform, warpPerspective
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    public class WrapPerspectiveExample : MonoBehaviour
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
            Texture2D inputTexture = Resources.Load("face") as Texture2D;

            Mat inputMat = new Mat(inputTexture.height, inputTexture.width, CvType.CV_8UC4);
            Mat outputMat = inputMat.clone();

            OpenCVMatUnityUtils.Texture2DToMat(inputTexture, inputMat);
            Debug.Log("inputMat.ToString() " + inputMat.ToString(), this);

            // Define four corner pairs: full image bounds -> trapezoid destination.
            Mat srcMat = new Mat(4, 1, CvType.CV_32FC2);
            Mat dstMat = new Mat(4, 1, CvType.CV_32FC2);
            srcMat.put(0, 0, 0.0, 0.0, inputMat.cols(), 0.0, 0.0, inputMat.rows(), inputMat.cols(), inputMat.rows());
            dstMat.put(0, 0, 0.0, 0.0, inputMat.cols(), 200.0, 0.0, inputMat.rows(), inputMat.cols(), inputMat.rows() - 200.0);

            // Compute the 3x3 perspective transform matrix.
            Mat perspectiveTransform = Geometry.getPerspectiveTransform(srcMat, dstMat);

            Debug.Log("perspectiveTransform " + perspectiveTransform.dump(), this);

            // Apply the perspective warp to the output buffer.
            Imgproc.warpPerspective(inputMat, outputMat, perspectiveTransform, new Size(inputMat.cols(), inputMat.rows()));

            Texture2D texture = new Texture2D(outputMat.cols(), outputMat.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUnityUtils.MatToTexture2D(outputMat, texture);

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
