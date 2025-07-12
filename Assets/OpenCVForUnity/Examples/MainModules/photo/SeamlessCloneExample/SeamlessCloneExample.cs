using System.Collections;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.PhotoModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// SeamlessClone Example
    /// An example of seamless photo synthesis using the Photo.seamlessClone function.
    /// </summary>
    public class SeamlessCloneExample : MonoBehaviour
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
            Texture2D srcTexture = Resources.Load("template") as Texture2D;
            Texture2D dstTexture = Resources.Load("face") as Texture2D;
            Mat src = new Mat(srcTexture.height, srcTexture.width, CvType.CV_8UC3);
            Mat dst = new Mat(dstTexture.height, dstTexture.width, CvType.CV_8UC3);
            OpenCVMatUtils.Texture2DToMat(srcTexture, src);
            OpenCVMatUtils.Texture2DToMat(dstTexture, dst);

            Mat mask = new Mat(src.rows(), src.cols(), CvType.CV_8UC1, new Scalar(255));
            Mat result = new Mat();

            Point point = new Point(250, 160);
            Photo.seamlessClone(src, dst, mask, point, result, Photo.NORMAL_CLONE);

            Debug.Log("result ToString " + result.ToString());

            Texture2D texture = new Texture2D(result.cols(), result.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUtils.MatToTexture2D(result, texture);

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
