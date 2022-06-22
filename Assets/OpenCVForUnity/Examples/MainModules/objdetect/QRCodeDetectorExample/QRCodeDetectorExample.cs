using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// QRCodeDetector Example
    /// An example of QRCode detection using the QRCodeDetector class.
    /// https://github.com/opencv/opencv/blob/master/samples/cpp/qrcode.cpp
    /// </summary>
    public class QRCodeDetectorExample : MonoBehaviour
    {
        // Use this for initialization
        void Start()
        {
            Run();
        }

        private void Run()
        {
            Texture2D imgTexture = Resources.Load("link_github_ocv") as Texture2D;

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            Utils.texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString());


            Mat grayMat = new Mat();
            Imgproc.cvtColor(imgMat, grayMat, Imgproc.COLOR_RGBA2GRAY);


            Mat points = new Mat();
            List<string> decodedInfo = new List<string>();
            List<Mat> straightQrcode = new List<Mat>();

            QRCodeDetector detector = new QRCodeDetector();


            bool result = detector.detectAndDecodeMulti(grayMat, decodedInfo, points, straightQrcode);

            if (result)
            {

                //Debug.Log(points.dump());
                //Debug.Log(points.ToString());


                for (int i = 0; i < points.rows(); i++)
                {
                    //Debug.Log(decoded_info[i]);
                    //Debug.Log(straight_qrcode[i].dump());

                    //// draw QRCode contour.
                    float[] points_arr = new float[8];
                    points.get(i, 0, points_arr);
                    Imgproc.line(imgMat, new Point(points_arr[0], points_arr[1]), new Point(points_arr[2], points_arr[3]), new Scalar(255, 0, 0, 255), 2);
                    Imgproc.line(imgMat, new Point(points_arr[2], points_arr[3]), new Point(points_arr[4], points_arr[5]), new Scalar(255, 0, 0, 255), 2);
                    Imgproc.line(imgMat, new Point(points_arr[4], points_arr[5]), new Point(points_arr[6], points_arr[7]), new Scalar(255, 0, 0, 255), 2);
                    Imgproc.line(imgMat, new Point(points_arr[6], points_arr[7]), new Point(points_arr[0], points_arr[1]), new Scalar(255, 0, 0, 255), 2);

                    if (decodedInfo.Count > i && decodedInfo[i] != null)
                        Imgproc.putText(imgMat, decodedInfo[i], new Point(points_arr[0], points_arr[1]), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }

            }
            else
            {
                Imgproc.putText(imgMat, "Decoding failed.", new Point(5, imgMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
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
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
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