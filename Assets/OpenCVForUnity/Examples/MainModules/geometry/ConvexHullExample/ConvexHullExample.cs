using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.GeometryModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// ConvexHull Example
    /// Computes and draws the convex hull enclosing a set of random 2D points.
    ///
    /// Demonstrates:
    /// - Generating random points with <see cref="Core.randu"/>
    /// - Computing a convex hull with <see cref="Imgproc.convexHull"/>
    /// - Drawing the hull polygon with <see cref="Imgproc.drawContours"/>
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Point"/>, <see cref="Scalar"/>, <see cref="MatOfPoint"/>, <see cref="MatOfInt"/>
    /// - <see cref="Core"/>: randu
    /// - <see cref="Imgproc"/>: circle, convexHull, drawContours, cvtColor
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// http://docs.opencv.org/trunk/d7/d1d/tutorial_hull.html
    /// </para>
    /// </remarks>
    public class ConvexHullExample : MonoBehaviour
    {
        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        // Unity Lifecycle Methods
        private void Start()
        {
            Mat imgMat = new Mat(500, 500, CvType.CV_8UC3, new Scalar(0, 0, 0));
            Debug.Log("imgMat.ToString() " + imgMat.ToString(), this);

            int rand_num = 50;
            MatOfPoint pointsMat = new MatOfPoint();
            pointsMat.alloc(rand_num);

            // Generate random (x, y) coordinates within the canvas.
            Core.randu(pointsMat, 100, 400);

            Point[] points = pointsMat.toArray();
            for (int i = 0; i < rand_num; ++i)
            {
                Imgproc.circle(imgMat, points[i], 2, new Scalar(255, 255, 255), -1);
            }

            // Compute convex hull indices for the point set.
            MatOfInt hullInt = new MatOfInt();
            Geometry.convexHull(pointsMat, hullInt);

            // Map hull indices back to point coordinates.
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

            // Draw the closed hull polygon in green.
            Imgproc.drawContours(imgMat, hullPoints, -1, new Scalar(0, 255, 0), 2);

            // Convert BGR to RGB for Unity texture display.
            Imgproc.cvtColor(imgMat, imgMat, Imgproc.COLOR_BGR2RGB);

            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUnityUtils.MatToTexture2D(imgMat, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;
        }

        private void Update()
        {

        }

        // Public Methods
        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }
    }
}
