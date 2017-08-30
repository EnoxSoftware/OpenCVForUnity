using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Threshold Example
    /// An example of image binarization using the Imgproc.threshold function.
    /// http://docs.opencv.org/trunk/d7/d4d/tutorial_py_thresholding.html
    /// </summary>
    public class ThresholdExample : MonoBehaviour
    {
        // Use this for initialization
        void Start ()
        {
            Texture2D imgTexture = Resources.Load ("chessboard") as Texture2D;
            
            Mat imgMat = new Mat (imgTexture.height, imgTexture.width, CvType.CV_8UC1);
            
            Utils.texture2DToMat (imgTexture, imgMat);
            Debug.Log ("imgMat.ToString() " + imgMat.ToString ());


            Imgproc.threshold (imgMat, imgMat, 0, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);


            Texture2D texture = new Texture2D (imgMat.cols (), imgMat.rows (), TextureFormat.RGBA32, false);
            Utils.matToTexture2D (imgMat, texture);
        
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