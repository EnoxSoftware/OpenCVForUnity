using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.StereoModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// StereoBM Example
    /// Computes a disparity map from a stereo image pair using block matching.
    ///
    /// Demonstrates:
    /// - Loading left/right grayscale stereo images
    /// - Disparity computation with <see cref="StereoBM.compute"/>
    /// - Normalizing 16-bit disparity to 8-bit for display
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>
    /// - <see cref="StereoBM"/>: create, compute
    /// - <see cref="Core"/>: normalize, NORM_MINMAX
    /// - <see cref="Imgcodecs"/>: imread
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// http://docs.opencv.org/trunk/tutorial_py_depthmap.html#gsc.tab=0
    /// </para>
    /// </remarks>
    public class StereoBMExample : MonoBehaviour
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
            //Read the left and right images
            Texture2D texLeft = Resources.Load("tsukuba_l") as Texture2D;
            Texture2D texRight = Resources.Load("tsukuba_r") as Texture2D;
            Mat imgLeft = new Mat(texLeft.height, texLeft.width, CvType.CV_8UC1);
            Mat imgRight = new Mat(texRight.height, texRight.width, CvType.CV_8UC1);
            OpenCVMatUnityUtils.Texture2DToMat(texLeft, imgLeft);
            OpenCVMatUnityUtils.Texture2DToMat(texRight, imgRight);
            //or
            //Mat imgLeft = Imgcodecs.imread (Utils.getFilePath ("tsukuba_l.png"), Imgcodecs.IMREAD_GRAYSCALE);
            //Mat imgRight = Imgcodecs.imread (Utils.getFilePath ("tsukuba_r.png"), Imgcodecs.IMREAD_GRAYSCALE);

            Mat imgDisparity16S = new Mat(imgLeft.rows(), imgLeft.cols(), CvType.CV_16S);
            Mat imgDisparity8U = new Mat(imgLeft.rows(), imgLeft.cols(), CvType.CV_8UC1);

            //if (imgLeft.empty () || imgRight.empty ()) {
            //   Debug.Log ("Error reading images ");
            //}

            // numDisparities=16, blockSize=15
            StereoBM sbm = StereoBM.create(16, 15);

            // Compute raw 16-bit signed disparity from the stereo pair.
            sbm.compute(imgLeft, imgRight, imgDisparity16S);

            // Scale disparity values to 0-255 for visualization.
            Core.normalize(imgDisparity16S, imgDisparity8U, 0, 255, Core.NORM_MINMAX, CvType.CV_8U);

            Texture2D texture = new Texture2D(imgDisparity8U.cols(), imgDisparity8U.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUnityUtils.MatToTexture2D(imgDisparity8U, texture);

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
