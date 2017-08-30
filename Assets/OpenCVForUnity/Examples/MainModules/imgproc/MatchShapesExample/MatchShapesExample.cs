using UnityEngine;
using System.Collections.Generic;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// MatchShapes Example
    /// http://docs.opencv.org/3.1.0/d5/d45/tutorial_py_contours_more_functions.html
    /// </summary>
    public class MatchShapesExample : MonoBehaviour
    {
        // Use this for initialization
        void Start ()
        {
            //srcMat
            Texture2D srcTexture = Resources.Load ("matchshapes") as Texture2D;
            Mat srcMat = new Mat (srcTexture.height, srcTexture.width, CvType.CV_8UC1);
            Utils.texture2DToMat (srcTexture, srcMat);
            Debug.Log ("srcMat.ToString() " + srcMat.ToString ());
            Imgproc.threshold (srcMat, srcMat, 127, 255, Imgproc.THRESH_BINARY);

            //dstMat
            Texture2D dstTexture = Resources.Load ("matchshapes") as Texture2D;
            Mat dstMat = new Mat (dstTexture.height, dstTexture.width, CvType.CV_8UC3);
            Utils.texture2DToMat (dstTexture, dstMat);
            Debug.Log ("dstMat.ToString() " + dstMat.ToString ());


            List<MatOfPoint> srcContours = new List<MatOfPoint> ();
            Mat srcHierarchy = new Mat ();

            /// Find srcContours
            Imgproc.findContours (srcMat, srcContours, srcHierarchy, Imgproc.RETR_CCOMP, Imgproc.CHAIN_APPROX_NONE);

            Debug.Log ("srcContours.Count " + srcContours.Count);
            
            for (int i=0; i<srcContours.Count; i++) {
                Imgproc.drawContours (dstMat, srcContours, i, new Scalar (255, 0, 0), 2, 8, srcHierarchy, 0, new Point ());
            }

            for (int i=0; i<srcContours.Count; i++) {
                double returnVal = Imgproc.matchShapes (srcContours [1], srcContours [i], Imgproc.CV_CONTOURS_MATCH_I1, 0);
                Debug.Log ("returnVal " + i + " " + returnVal);

                Point point = new Point ();
                float[] radius = new float[1];
                Imgproc.minEnclosingCircle (new MatOfPoint2f (srcContours [i].toArray ()), point, radius);
                Debug.Log ("point.ToString() " + point.ToString ());
                Debug.Log ("radius.ToString() " + radius [0]);
                
                Imgproc.circle (dstMat, point, 5, new Scalar (0, 0, 255), -1);
                Imgproc.putText (dstMat, " " + returnVal, point, Core.FONT_HERSHEY_SIMPLEX, 0.4, new Scalar (0, 255, 0), 1, Imgproc.LINE_AA, false);
            }

                        
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