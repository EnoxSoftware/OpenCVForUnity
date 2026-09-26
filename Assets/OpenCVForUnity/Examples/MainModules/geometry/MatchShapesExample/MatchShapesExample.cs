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
    /// MatchShapes Example
    /// Compares contour shapes using Hu-moment-based distance and visualizes similarity scores.
    ///
    /// Demonstrates:
    /// - Binary thresholding and contour extraction
    /// - Shape comparison with <see cref="Imgproc.matchShapes"/> (CV_CONTOURS_MATCH_I1)
    /// - Annotating each contour with its match distance and centroid
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Point"/>, <see cref="Scalar"/>, <see cref="MatOfPoint"/>, <see cref="MatOfPoint2f"/>
    /// - <see cref="Imgproc"/>: threshold, findContours, drawContours, matchShapes, minEnclosingCircle, circle, putText
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// http://docs.opencv.org/3.1.0/d5/d45/tutorial_py_contours_more_functions.html
    /// </para>
    /// </remarks>
    public class MatchShapesExample : MonoBehaviour
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
            //srcMat
            Texture2D srcTexture = Resources.Load("matchshapes") as Texture2D;
            Mat srcMat = new Mat(srcTexture.height, srcTexture.width, CvType.CV_8UC1);
            OpenCVMatUnityUtils.Texture2DToMat(srcTexture, srcMat);
            Debug.Log("srcMat.ToString() " + srcMat.ToString(), this);
            // Binarize the grayscale source for contour detection.
            Imgproc.threshold(srcMat, srcMat, 127, 255, Imgproc.THRESH_BINARY);

            //dstMat
            Texture2D dstTexture = Resources.Load("matchshapes") as Texture2D;
            Mat dstMat = new Mat(dstTexture.height, dstTexture.width, CvType.CV_8UC3);
            OpenCVMatUnityUtils.Texture2DToMat(dstTexture, dstMat);
            Debug.Log("dstMat.ToString() " + dstMat.ToString(), this);

            List<MatOfPoint> srcContours = new List<MatOfPoint>();
            Mat srcHierarchy = new Mat();

            /// Find srcContours
            Imgproc.findContours(srcMat, srcContours, srcHierarchy, Imgproc.RETR_CCOMP, Imgproc.CHAIN_APPROX_NONE);

            Debug.Log("srcContours.Count " + srcContours.Count, this);

            // Draw all detected contours on the color output image.
            for (int i = 0; i < srcContours.Count; i++)
            {
                Imgproc.drawContours(dstMat, srcContours, i, new Scalar(255, 0, 0), 2, 8, srcHierarchy, 0, new Point());
            }

            for (int i = 0; i < srcContours.Count; i++)
            {
                // Compare each contour against contour[1]; lower distance means more similar shape.
                double returnVal = Geometry.matchShapes(srcContours[1], srcContours[i], Imgproc.CONTOURS_MATCH_I1, 0);
                Debug.Log("returnVal " + i + " " + returnVal, this);

                Point point = new Point();
                float[] radius = new float[1];
                Geometry.minEnclosingCircle(new MatOfPoint2f(srcContours[i].toArray()), point, radius);
                Debug.Log("point.ToString() " + point.ToString(), this);
                Debug.Log("radius.ToString() " + radius[0], this);

                Imgproc.circle(dstMat, point, 5, new Scalar(0, 0, 255), -1);
                Imgproc.putText(dstMat, " " + returnVal, point, Imgproc.FONT_HERSHEY_SIMPLEX, 0.4, new Scalar(0, 255, 0), 1, Imgproc.LINE_AA, false);
            }

            Texture2D texture = new Texture2D(dstMat.cols(), dstMat.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUnityUtils.MatToTexture2D(dstMat, texture);

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
