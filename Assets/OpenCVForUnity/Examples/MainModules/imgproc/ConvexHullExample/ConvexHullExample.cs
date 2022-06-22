using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// ConvexHull Example
    /// http://docs.opencv.org/trunk/d7/d1d/tutorial_hull.html
    /// </summary>
    public class ConvexHullExample : MonoBehaviour
    {
        // Use this for initialization
        void Start()
        {
            Mat imgMat = new Mat(500, 500, CvType.CV_8UC3, new Scalar(0, 0, 0));
            Debug.Log("imgMat.ToString() " + imgMat.ToString());

            int rand_num = 50;
            MatOfPoint pointsMat = new MatOfPoint();
            pointsMat.alloc(rand_num);

            Core.randu(pointsMat, 100, 400);

            Point[] points = pointsMat.toArray();
            for (int i = 0; i < rand_num; ++i)
            {
                Imgproc.circle(imgMat, points[i], 2, new Scalar(255, 255, 255), -1);
            }


            MatOfInt hullInt = new MatOfInt();
            Imgproc.convexHull(pointsMat, hullInt);


            List<Point> pointMatList = pointsMat.toList();
            List<int> hullIntList = hullInt.toList();
            List<Point> hullPointList = new List<Point>();

            for (int j = 0; j < hullInt.toList().Count; j++)
            {
                hullPointList.Add(pointMatList[hullIntList[j]]);
            }

            MatOfPoint hullPointMat = new MatOfPoint();
            hullPointMat.fromList(hullPointList);

            List<MatOfPoint> hullPoints = new List<MatOfPoint>();
            hullPoints.Add(hullPointMat);


            Imgproc.drawContours(imgMat, hullPoints, -1, new Scalar(0, 255, 0), 2);

            Imgproc.cvtColor(imgMat, imgMat, Imgproc.COLOR_BGR2RGB);

            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(imgMat, texture);

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