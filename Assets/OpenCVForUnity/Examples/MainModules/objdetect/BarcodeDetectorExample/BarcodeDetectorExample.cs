#if !UNITY_WSA_10_0

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
    /// BarcodeDetector Example
    /// An example of Barcode detection using the BarcodeDetector class.
    /// </summary>
    public class BarcodeDetectorExample : MonoBehaviour
    {
        // Use this for initialization
        void Start()
        {
            Run();
        }

        private void Run()
        {
            Texture2D imgTexture = Resources.Load("book") as Texture2D;

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            Utils.texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString());


            BarcodeDetector detector = new BarcodeDetector();

            // When using super resolution.
            // Please, download 'sr.*' from https://github.com/WeChatCV/opencv_3rdparty/tree/wechat_qrcode and put them into the StreamingAssets/OpenCVForUnity/barcode directory.
            //BarcodeDetector detector = new BarcodeDetector(Utils.getFilePath("OpenCVForUnity/barcode/sr.prototxt"), Utils.getFilePath("OpenCVForUnity/barcode/sr.caffemodel"));

            List<string> decoded_info = new List<string>();
            List<string> decoded_type = new List<string>();
            Mat points = new Mat();

            bool result = detector.detectAndDecodeWithType(imgMat, decoded_info, decoded_type, points);

            if (result)
            {
                for (int i = 0; i < decoded_info.Count; i++)
                {
                    Debug.Log("decoded_info[" + i + "] " + decoded_info[i]);
                }
                for (int i = 0; i < decoded_type.Count; i++)
                {
                    Debug.Log("decoded_type[" + i + "] " + decoded_type[i]);
                }
                Debug.Log("points.dump() " + points.dump());


                // draw Barcode contour.
                float[] points_arr = new float[8];
                points.get(0, 0, points_arr);
                Imgproc.line(imgMat, new Point(points_arr[0], points_arr[1]), new Point(points_arr[2], points_arr[3]), new Scalar(255, 0, 0, 255), 2);
                Imgproc.line(imgMat, new Point(points_arr[2], points_arr[3]), new Point(points_arr[4], points_arr[5]), new Scalar(255, 0, 0, 255), 2);
                Imgproc.line(imgMat, new Point(points_arr[4], points_arr[5]), new Point(points_arr[6], points_arr[7]), new Scalar(255, 0, 0, 255), 2);
                Imgproc.line(imgMat, new Point(points_arr[6], points_arr[7]), new Point(points_arr[0], points_arr[1]), new Scalar(255, 0, 0, 255), 2);

                Imgproc.putText(imgMat, "DECODED INFO[0]: " + decoded_info[0], new Point(5, imgMat.rows() - 35), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(imgMat, "DECODED TYPE[0]: " + decoded_type[0], new Point(5, imgMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
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

#endif