using System.Collections;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// StereoBM Example
    /// An example of stereo correspondence using the block matching algorithm.
    /// Referring to http://docs.opencv.org/trunk/tutorial_py_depthmap.html#gsc.tab=0.
    /// </summary>
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
            OpenCVMatUtils.Texture2DToMat(texLeft, imgLeft);
            OpenCVMatUtils.Texture2DToMat(texRight, imgRight);
            //or
            //Mat imgLeft = Imgcodecs.imread (Utils.getFilePath ("tsukuba_l.png"), Imgcodecs.IMREAD_GRAYSCALE);
            //Mat imgRight = Imgcodecs.imread (Utils.getFilePath ("tsukuba_r.png"), Imgcodecs.IMREAD_GRAYSCALE);


            Mat imgDisparity16S = new Mat(imgLeft.rows(), imgLeft.cols(), CvType.CV_16S);
            Mat imgDisparity8U = new Mat(imgLeft.rows(), imgLeft.cols(), CvType.CV_8UC1);

            //if (imgLeft.empty () || imgRight.empty ()) {
            //   Debug.Log ("Error reading images ");
            //}

            StereoBM sbm = StereoBM.create(16, 15);

            sbm.compute(imgLeft, imgRight, imgDisparity16S);

            //normalize to CvType.CV_8U
            Core.normalize(imgDisparity16S, imgDisparity8U, 0, 255, Core.NORM_MINMAX, CvType.CV_8U);


            Texture2D texture = new Texture2D(imgDisparity8U.cols(), imgDisparity8U.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUtils.MatToTexture2D(imgDisparity8U, texture);

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
