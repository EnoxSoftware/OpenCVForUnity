using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnitySample
{
    /// <summary>
    /// Texture2D to mat sample.
    /// </summary>
    public class Texture2DToMatSample : MonoBehaviour
    {

        // Use this for initialization
        void Start ()
        {

            Texture2D imgTexture = Resources.Load ("lena") as Texture2D;

            Mat imgMat = new Mat (imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            Utils.texture2DToMat (imgTexture, imgMat);
            Debug.Log ("imgMat.ToString() " + imgMat.ToString ());


            Texture2D texture = new Texture2D (imgMat.cols (), imgMat.rows (), TextureFormat.RGBA32, false);

            Utils.matToTexture2D (imgMat, texture);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

        }
    
        // Update is called once per frame
        void Update ()
        {
    
        }

        public void OnBackButton ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("OpenCVForUnitySample");
            #else
            Application.LoadLevel ("OpenCVForUnitySample");
            #endif
        }
    }
}
