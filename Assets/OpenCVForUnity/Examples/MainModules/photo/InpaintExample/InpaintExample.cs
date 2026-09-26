using OpenCVForUnity.CoreModule;
using OpenCVForUnity.PhotoModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Inpaint Example
    /// Restores damaged image regions by propagating information from surrounding pixels.
    ///
    /// Demonstrates:
    /// - Loading a source image and a single-channel inpaint mask
    /// - Region filling with <see cref="Photo.inpaint"/> (INPAINT_NS algorithm)
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>
    /// - <see cref="Photo"/>: inpaint, INPAINT_NS
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// http://docs.opencv.org/trunk/df/d3d/tutorial_py_inpainting.html
    /// </para>
    /// </remarks>
    public class InpaintExample : MonoBehaviour
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
            Texture2D srcTexture = Resources.Load("face") as Texture2D;

            Mat srcMat = new Mat(srcTexture.height, srcTexture.width, CvType.CV_8UC3);

            OpenCVMatUnityUtils.Texture2DToMat(srcTexture, srcMat);
            Debug.Log("srcMat.ToString() " + srcMat.ToString(), this);

            Texture2D maskTexture = Resources.Load("face_inpaint_mask") as Texture2D;

            Mat maskMat = new Mat(maskTexture.height, maskTexture.width, CvType.CV_8UC1);

            OpenCVMatUnityUtils.Texture2DToMat(maskTexture, maskMat);
            Debug.Log("maskMat.ToString() " + maskMat.ToString(), this);

            Mat dstMat = new Mat(srcMat.rows(), srcMat.cols(), CvType.CV_8UC3);

            // Fill masked pixels using the Navier-Stokes inpainting method (inpaintRadius=5).
            Photo.inpaint(srcMat, maskMat, dstMat, 5, Photo.INPAINT_NS);

            Texture2D texture = new Texture2D(dstMat.cols(), dstMat.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUnityUtils.MatToTexture2D(dstMat, texture);

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
