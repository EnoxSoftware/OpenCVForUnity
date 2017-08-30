using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnitySample
{
    /// <summary>
    /// ThinPlateSplineShapeTransformer Example
    /// An example of Thin Plate Spline (TPS) Warping using the ThinPlateSplineShapeTransformer class.
    /// </summary>
    public class ThinPlateSplineShapeTransformerExample : MonoBehaviour
    {
        // Use this for initialization
        void Start ()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode (true);

            Texture2D imgTexture = Resources.Load ("lena") as Texture2D;

            Mat img = new Mat (imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            Utils.texture2DToMat (imgTexture, img);
            Debug.Log ("imgMat.ToString() " + img.ToString ());


            OpenCVForUnity.ThinPlateSplineShapeTransformer tps = Shape.createThinPlateSplineShapeTransformer (0);
            MatOfPoint2f sourcePoints = new MatOfPoint2f (
                                            new Point (0, 0),
                                            new Point (512, 0),
                                            new Point (0, 512),

                                            new Point (250, 200),
                                            new Point (400, 400),
                                            new Point (200, 400),

                                            new Point (512, 512)
                                        );
            MatOfPoint2f targetPoints = new MatOfPoint2f (
                                            new Point (0, 0),
                                            new Point (512, 0),
                                            new Point (0, 599),

                                            new Point (250, 120),
                                            new Point (450, 450),
                                            new Point (100, 450),

                                            new Point (512, 512)
                                        );
            MatOfDMatch matches = new MatOfDMatch (
                                      new DMatch (0, 0, 0),
                                      new DMatch (1, 1, 0),
                                      new DMatch (2, 2, 0),
                                      new DMatch (3, 3, 0),
                                      new DMatch (4, 4, 0),
                                      new DMatch (5, 5, 0),
                                      new DMatch (6, 6, 0)
                                  );


            //http://stackoverflow.com/questions/32207085/shape-transformers-and-interfaces-opencv3-0
            Core.transpose (sourcePoints, sourcePoints);
            Core.transpose (targetPoints, targetPoints);

            Debug.Log ("sourcePoints " + sourcePoints.ToString ());
            Debug.Log ("targetPoints " + targetPoints.ToString ());

            tps.estimateTransformation (targetPoints, sourcePoints, matches);

            MatOfPoint2f transPoints = new MatOfPoint2f ();
            tps.applyTransformation (sourcePoints, transPoints);

            Debug.Log ("sourcePoints " + sourcePoints.dump ());
            Debug.Log ("targetPoints " + targetPoints.dump ());
            Debug.Log ("transPoints " + transPoints.dump ());


            Mat res = new Mat ();

            tps.warpImage (img, res);

            //plot points
            Point[] sourcePointsArray = sourcePoints.toArray ();
            Point[] targetPointsArray = targetPoints.toArray ();
            for (int i = 0; i < sourcePointsArray.Length; i++) {
                Imgproc.arrowedLine (res, sourcePointsArray [i], targetPointsArray [i], new Scalar (255, 255, 0, 255), 3, Imgproc.LINE_AA, 0, 0.2);

                Imgproc.circle (res, sourcePointsArray [i], 10, new Scalar (255, 0, 0, 255), -1);
                Imgproc.circle (res, targetPointsArray [i], 10, new Scalar (0, 0, 255, 255), -1);
            }


            Texture2D texture = new Texture2D (res.cols (), res.rows (), TextureFormat.RGBA32, false);

            Utils.matToTexture2D (res, texture);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;


            Utils.setDebugMode (false);
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
