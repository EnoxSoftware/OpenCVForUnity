using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

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
        void Start ()
        {
            Texture2D srcTexture = Resources.Load ("lena") as Texture2D;

            Mat srcMat = new Mat (srcTexture.height, srcTexture.width, CvType.CV_8UC3);

            Utils.texture2DToMat (srcTexture, srcMat);
            Debug.Log ("srcMat.ToString() " + srcMat.ToString ());

            Texture2D maskTexture = Resources.Load ("lena_inpaint_mask") as Texture2D;
            
            Mat maskMat = new Mat (maskTexture.height, maskTexture.width, CvType.CV_8UC1);
            
            Utils.texture2DToMat (maskTexture, maskMat);
            Debug.Log ("maskMat.ToString() " + maskMat.ToString ());

            Mat dstMat = new Mat (srcMat.rows (), srcMat.cols (), CvType.CV_8UC3);


            Photo.inpaint (srcMat, maskMat, dstMat, 5, Photo.INPAINT_NS);


            Texture2D texture = new Texture2D (dstMat.cols (), dstMat.rows (), TextureFormat.RGBA32, false);

            Utils.matToTexture2D (dstMat, texture);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
        }
    
        // Update is called once per frame
        void Update ()
        {
    
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("OpenCVForUnityExample");
            #else
            Application.LoadLevel ("OpenCVForUnityExample");
            #endif
        }
    }
}