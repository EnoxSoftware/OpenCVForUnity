using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// StereoBM Example
    /// An example of stereo correspondence using the block matching algorithm.
    /// Referring to http://docs.opencv.org/trunk/tutorial_py_depthmap.html#gsc.tab=0.
    /// </summary>
    public class StereoBMExample : MonoBehaviour
    {
        // Use this for initialization
        void Start ()
        {
            //Read the left and right images
            Texture2D texLeft = Resources.Load ("tsukuba_l") as Texture2D;
            Texture2D texRight = Resources.Load ("tsukuba_r") as Texture2D;
            Mat imgLeft = new Mat (texLeft.height, texLeft.width, CvType.CV_8UC1);
            Mat imgRight = new Mat (texRight.height, texRight.width, CvType.CV_8UC1);
            Utils.texture2DToMat (texLeft, imgLeft);
            Utils.texture2DToMat (texRight, imgRight);
            //or
            //Mat imgLeft = Imgcodecs.imread (Utils.getFilePath ("tsukuba_l.png"), Imgcodecs.IMREAD_GRAYSCALE);
            //Mat imgRight = Imgcodecs.imread (Utils.getFilePath ("tsukuba_r.png"), Imgcodecs.IMREAD_GRAYSCALE);
            
            
            Mat imgDisparity16S = new Mat (imgLeft.rows (), imgLeft.cols (), CvType.CV_16S);
            Mat imgDisparity8U = new Mat (imgLeft.rows (), imgLeft.cols (), CvType.CV_8UC1);
            
//            if (imgLeft.empty () || imgRight.empty ()) {
//                Debug.Log ("Error reading images ");
//            }
            
            StereoBM sbm = StereoBM.create (16, 15);
            
            sbm.compute (imgLeft, imgRight, imgDisparity16S);
            
            //normalize to CvType.CV_8U
            Core.normalize (imgDisparity16S, imgDisparity8U, 0, 255, Core.NORM_MINMAX, CvType.CV_8U);
            
            
            Texture2D texture = new Texture2D (imgDisparity8U.cols (), imgDisparity8U.rows (), TextureFormat.RGBA32, false);
            
            Utils.matToTexture2D (imgDisparity8U, texture);
            
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