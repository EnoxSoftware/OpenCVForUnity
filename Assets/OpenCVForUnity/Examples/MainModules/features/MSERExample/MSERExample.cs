using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.FeaturesModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// MSER Example
    /// Detects stable extremal regions (MSER) in a still image and draws each region contour.
    ///
    /// Demonstrates:
    /// - MSER.create() parameter tuning (delta, min/max area)
    /// - detectRegions output as contour list and bounding boxes
    /// - Overlaying detected regions with drawContours
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="CvType"/>, <see cref="Scalar"/>, <see cref="MatOfPoint"/>, <see cref="MatOfRect"/>
    /// - <see cref="MSER"/>: create, setDelta, setMinArea, setMaxArea, detectRegions
    /// - <see cref="Imgproc"/>: drawContours
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://docs.opencv.org/4.x/d3/d39/classcv_1_1MSER.html
    /// </para>
    /// </remarks>
    public class MSERExample : MonoBehaviour
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
            Texture2D imgTexture = Resources.Load("chessboard") as Texture2D;

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC3);

            OpenCVMatUnityUtils.Texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString(), this);

            MSER mserExtractor = MSER.create();
            // delta: step between successive threshold levels; area limits filter noise.
            mserExtractor.setDelta(5);
            mserExtractor.setMinArea(60);
            mserExtractor.setMaxArea(14400);

            List<MatOfPoint> mserContours = new List<MatOfPoint>();
            MatOfRect mserBbox = new MatOfRect();
            // detectRegions fills contour list and optional bounding boxes.
            mserExtractor.detectRegions(imgMat, mserContours, mserBbox);

            for (int i = 0; i < mserContours.Count; i++)
            {
                Imgproc.drawContours(imgMat, mserContours, i, new Scalar(Random.Range(0, 255), Random.Range(0, 255), Random.Range(0, 255)), 4);
            }

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
