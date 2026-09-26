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
    /// Blends a source patch into a destination image at a given center using Poisson editing.
    ///
    /// Demonstrates:
    /// - Preparing a full-opacity mask for the source region
    /// - Seamless cloning with <see cref="Photo.seamlessClone"/> (NORMAL_CLONE)
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Point"/>, <see cref="Scalar"/>
    /// - <see cref="Photo"/>: seamlessClone, NORMAL_CLONE
    /// - <see cref="OpenCVMatUnityUtils"/>
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
            OpenCVMatUnityUtils.Texture2DToMat(srcTexture, src);
            OpenCVMatUnityUtils.Texture2DToMat(dstTexture, dst);

            // Full-opacity mask selects the entire source patch for cloning.
            Mat mask = new Mat(src.rows(), src.cols(), CvType.CV_8UC1, new Scalar(255));
            Mat result = new Mat();

            // Blend src into dst centered at the given point.
            Point point = new Point(250, 160);
            Photo.seamlessClone(src, dst, mask, point, result, Photo.NORMAL_CLONE);

            Debug.Log("result ToString " + result.ToString(), this);

            Texture2D texture = new Texture2D(result.cols(), result.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUnityUtils.MatToTexture2D(result, texture);

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
