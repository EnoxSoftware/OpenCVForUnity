using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Features2dModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// MSER Example
    /// An example of region detection using the MSER extraction algorithm.
    /// </summary>
    public class MSERExample : MonoBehaviour
    {
        // Use this for initialization
        void Start()
        {
            Texture2D imgTexture = Resources.Load("chessboard") as Texture2D;

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC3);

            Utils.texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString());

            MSER mserExtractor = MSER.create();
            mserExtractor.setDelta(5);
            mserExtractor.setMinArea(60);
            mserExtractor.setMaxArea(14400);

            List<MatOfPoint> mserContours = new List<MatOfPoint>();
            MatOfRect mserBbox = new MatOfRect();
            mserExtractor.detectRegions(imgMat, mserContours, mserBbox);


            for (int i = 0; i < mserContours.Count; i++)
            {
                Imgproc.drawContours(imgMat, mserContours, i, new Scalar(Random.Range(0, 255), Random.Range(0, 255), Random.Range(0, 255)), 4);
            }


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