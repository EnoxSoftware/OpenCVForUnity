using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System.Collections.Generic;
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
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        // Use this for initialization
        void Start()
        {
            Texture2D inputTexture = Resources.Load("face") as Texture2D;

            Mat inputMat = new Mat(inputTexture.height, inputTexture.width, CvType.CV_8UC4);
            Mat outputMat = inputMat.clone();

            Utils.texture2DToMat(inputTexture, inputMat);
            Debug.Log("inputMat.ToString() " + inputMat.ToString());


            Mat src_mat = new Mat(4, 1, CvType.CV_32FC2);
            Mat dst_mat = new Mat(4, 1, CvType.CV_32FC2);
            src_mat.put(0, 0, 0.0, 0.0, inputMat.cols(), 0.0, 0.0, inputMat.rows(), inputMat.cols(), inputMat.rows());
            dst_mat.put(0, 0, 0.0, 0.0, inputMat.cols(), 200.0, 0.0, inputMat.rows(), inputMat.cols(), inputMat.rows() - 200.0);

            Mat perspectiveTransform = Imgproc.getPerspectiveTransform(src_mat, dst_mat);

            Debug.Log("perspectiveTransform " + perspectiveTransform.dump());

            Imgproc.warpPerspective(inputMat, outputMat, perspectiveTransform, new Size(inputMat.cols(), inputMat.rows()));


            Texture2D texture = new Texture2D(outputMat.cols(), outputMat.rows(), TextureFormat.RGBA32, false);

            Utils.matToTexture2D(outputMat, texture);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }
    }
}