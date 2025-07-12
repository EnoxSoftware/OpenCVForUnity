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
    /// WrapPerspective Example
    /// An example of perspective transformation of a image using the Imgproc.warpPerspective function.
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

            OpenCVMatUtils.Texture2DToMat(inputTexture, inputMat);
            Debug.Log("inputMat.ToString() " + inputMat.ToString());


            Mat srcMat = new Mat(4, 1, CvType.CV_32FC2);
            Mat dstMat = new Mat(4, 1, CvType.CV_32FC2);
            srcMat.put(0, 0, 0.0, 0.0, inputMat.cols(), 0.0, 0.0, inputMat.rows(), inputMat.cols(), inputMat.rows());
            dstMat.put(0, 0, 0.0, 0.0, inputMat.cols(), 200.0, 0.0, inputMat.rows(), inputMat.cols(), inputMat.rows() - 200.0);

            Mat perspectiveTransform = Imgproc.getPerspectiveTransform(srcMat, dstMat);

            Debug.Log("perspectiveTransform " + perspectiveTransform.dump());

            Imgproc.warpPerspective(inputMat, outputMat, perspectiveTransform, new Size(inputMat.cols(), inputMat.rows()));


            Texture2D texture = new Texture2D(outputMat.cols(), outputMat.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUtils.MatToTexture2D(outputMat, texture);

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
