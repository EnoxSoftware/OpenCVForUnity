using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.PhotoModule;
using OpenCVForUnity.UnityUtils;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Inpaint Example
    /// An example of image restoration using the Photo.inpaint function.
    /// http://docs.opencv.org/trunk/df/d3d/tutorial_py_inpainting.html
    /// </summary>
    public class InpaintExample : MonoBehaviour
    {
        // Use this for initialization
        void Start()
        {
            Texture2D srcTexture = Resources.Load("face") as Texture2D;

            Mat srcMat = new Mat(srcTexture.height, srcTexture.width, CvType.CV_8UC3);

            Utils.texture2DToMat(srcTexture, srcMat);
            Debug.Log("srcMat.ToString() " + srcMat.ToString());

            Texture2D maskTexture = Resources.Load("face_inpaint_mask") as Texture2D;

            Mat maskMat = new Mat(maskTexture.height, maskTexture.width, CvType.CV_8UC1);

            Utils.texture2DToMat(maskTexture, maskMat);
            Debug.Log("maskMat.ToString() " + maskMat.ToString());

            Mat dstMat = new Mat(srcMat.rows(), srcMat.cols(), CvType.CV_8UC3);


            Photo.inpaint(srcMat, maskMat, dstMat, 5, Photo.INPAINT_NS);


            Texture2D texture = new Texture2D(dstMat.cols(), dstMat.rows(), TextureFormat.RGBA32, false);

            Utils.matToTexture2D(dstMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;
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