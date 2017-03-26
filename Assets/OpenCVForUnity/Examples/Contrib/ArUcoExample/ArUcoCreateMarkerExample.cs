using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// ArUco create marker example.
    /// https://github.com/opencv/opencv_contrib/blob/master/modules/aruco/samples/create_marker.cpp
    /// </summary>
    public class ArUcoCreateMarkerExample : MonoBehaviour
    {
        /// <summary>
        /// The dictionary identifier.
        /// </summary>
        public int dictionaryId = Aruco.DICT_6X6_250;

        /// <summary>
        /// The marker identifier.
        /// </summary>
        public int markerId = 1;

        /// <summary>
        /// The border bits.
        /// </summary>
        public int borderBits = 1;

        /// <summary>
        /// The size of the marker.
        /// </summary>
        public int markerSize = 200;

        // Use this for initialization
        void Start ()
        {
            Dictionary dictionary = Aruco.getPredefinedDictionary (dictionaryId);
            
            Mat markerImg = new Mat ();
            Aruco.drawMarker (dictionary, markerId, markerSize, markerImg, borderBits);

            Debug.Log ("markerImg.ToString() " + markerImg.ToString ());

            Texture2D texture = new Texture2D (markerImg.cols (), markerImg.rows (), TextureFormat.RGBA32, false);

            Utils.matToTexture2D (markerImg, texture);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            //save markerImg
//                      string savePath = Application.persistentDataPath + "/marker_id" + markerId + ".jpg";
//                      Debug.Log ("savePath " + savePath);
//                      Imgcodecs.imwrite (savePath, markerImg);
        }

        // Update is called once per frame
        void Update ()
        {
    
        }

        public void OnBackButton ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("OpenCVForUnityExample");
            #else
            Application.LoadLevel ("OpenCVForUnityExample");
            #endif
        }
    }
}