using UnityEngine;
using System.Collections.Generic;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// WrapPerspective Example
    /// An example of perspective transformation of a image using the Imgproc.warpPerspective function.
    /// </summary>
    public class WrapPerspectiveExample : MonoBehaviour
    {
        // Use this for initialization
        void Start ()
        {
            Texture2D inputTexture = Resources.Load ("lena") as Texture2D;
            
            Mat inputMat = new Mat (inputTexture.height, inputTexture.width, CvType.CV_8UC4);

            Utils.texture2DToMat (inputTexture, inputMat);
            Debug.Log ("inputMat.ToString() " + inputMat.ToString ());


            Mat src_mat = new Mat (4, 1, CvType.CV_32FC2);
            Mat dst_mat = new Mat (4, 1, CvType.CV_32FC2);
            
            
            src_mat.put (0, 0, 0.0, 0.0, inputMat.rows (), 0.0, 0.0, inputMat.cols (), inputMat.rows (), inputMat.cols ());
            dst_mat.put (0, 0, 0.0, 0.0, inputMat.rows (), 200.0, 0.0, inputMat.cols (), inputMat.rows (), inputMat.cols () - 200.0);
            Mat perspectiveTransform = Imgproc.getPerspectiveTransform (src_mat, dst_mat);


            Mat outputMat = inputMat.clone ();


            Imgproc.warpPerspective (inputMat, outputMat, perspectiveTransform, new Size (inputMat.rows (), inputMat.cols ()));

            
            Texture2D outputTexture = new Texture2D (outputMat.cols (), outputMat.rows (), TextureFormat.RGBA32, false);
            
            
            Utils.matToTexture2D (outputMat, outputTexture);
            
            gameObject.GetComponent<Renderer> ().material.mainTexture = outputTexture;
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