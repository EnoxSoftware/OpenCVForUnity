using UnityEngine;
using System.Collections.Generic;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// ConnectedComponents Example
    /// An example of Connected-component labeling using the Imgproc.connectedComponentsWithStats function.
    /// Referring to http://qiita.com/wakaba130/items/9d921b8b3eb812e4f197.
    /// </summary>
    public class ConnectedComponentsExample : MonoBehaviour
    {
        // Use this for initialization
        void Start ()
        {
            Texture2D imgTexture = Resources.Load ("matchshapes") as Texture2D;
            
            Mat srcMat = new Mat (imgTexture.height, imgTexture.width, CvType.CV_8UC1);
            
            Utils.texture2DToMat (imgTexture, srcMat);
            Debug.Log ("srcMat.ToString() " + srcMat.ToString ());

            Mat dstMat = new Mat (srcMat.size (), CvType.CV_8UC3);

            //labeling
            Mat labels = new Mat ();
            Mat stats = new Mat ();
            Mat centroids = new Mat ();
            int total = Imgproc.connectedComponentsWithStats (srcMat, labels, stats, centroids);

            Debug.Log ("labels.ToString() " + labels.ToString ());
            Debug.Log ("stats.ToString() " + stats.ToString ());
            Debug.Log ("centroids.ToString() " + centroids.ToString ());
            Debug.Log ("total " + total);
            
            // determine drawing color
            List<Scalar> colors = new List<Scalar> (total);
            colors.Add (new Scalar (0, 0, 0));
            for (int i = 1; i < total; ++i) {
                colors.Add (new Scalar (Random.Range (0, 255), Random.Range (0, 255), Random.Range (0, 255)));
            }
            
            // draw labels
            for (int i = 0; i < dstMat.rows(); ++i) {
                for (int j = 0; j < dstMat.cols(); ++j) {
                    Scalar color = colors [(int)labels.get (i, j) [0]];
                    dstMat.put (i, j, color.val [0], color.val [1], color.val [2]);
                }
            }
            
            // draw rectangle
            for (int i = 1; i < total; ++i) {

                int x = (int)stats.get (i, Imgproc.CC_STAT_LEFT) [0];
                int y = (int)stats.get (i, Imgproc.CC_STAT_TOP) [0];
                int height = (int)stats.get (i, Imgproc.CC_STAT_HEIGHT) [0];
                int width = (int)stats.get (i, Imgproc.CC_STAT_WIDTH) [0];

                OpenCVForUnity.Rect rect = new OpenCVForUnity.Rect (x, y, width, height);

                Imgproc.rectangle (dstMat, rect.tl (), rect.br (), new Scalar (0, 255, 0), 2);
            }
            
            // draw centroids
            for (int i = 1; i < total; ++i) {

                int x = (int)centroids.get (i, 0) [0];
                int y = (int)centroids.get (i, 1) [0];

                Imgproc.circle (dstMat, new Point (x, y), 3, new Scalar (255, 0, 0), -1);
            }
            
            // draw index of label
            for (int i = 1; i < total; ++i) {

                int x = (int)stats.get (i, Imgproc.CC_STAT_LEFT) [0];
                int y = (int)stats.get (i, Imgproc.CC_STAT_TOP) [0];

                Imgproc.putText (dstMat, "" + i, new Point (x + 5, y + 15), Core.FONT_HERSHEY_COMPLEX, 0.5, new Scalar (255, 255, 0), 2);
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