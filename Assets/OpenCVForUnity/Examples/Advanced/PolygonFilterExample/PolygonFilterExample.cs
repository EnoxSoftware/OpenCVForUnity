using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Polygon Filter Example
    /// Referring to http://jsdo.it/hedger/tIod.
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper), typeof(ImageOptimizationHelper))]
    public class PolygonFilterExample : MonoBehaviour
    {
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        MultiSource2MatHelper multiSource2MatHelper;

        /// <summary>
        /// The image optimization helper.
        /// </summary>
        ImageOptimizationHelper imageOptimizationHelper;

        /// <summary>
        /// EDGE_DETECT_VALUE
        /// </summary>
        int EDGE_DETECT_VALUE = 70;

        /// <summary>
        /// POINT_RATE
        /// </summary>
        double POINT_RATE = 0.075;

        /// <summary>
        /// POINT_MAX_NUM
        /// </summary>
        int POINT_MAX_NUM = 2500;

        /// <summary>
        /// The gray1 mat.
        /// </summary>
        Mat gray1Mat;

        /// <summary>
        /// The gray2 mat.
        /// </summary>
        Mat gray2Mat;

        /// <summary>
        /// The kernel.
        /// </summary>
        Mat kernel;

        /// <summary>
        /// The byte array.
        /// </summary>
        byte[] byteArray;

        /// <summary>
        /// The subdiv.
        /// </summary>
        Subdiv2D subdiv;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();

            multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            multiSource2MatHelper.outputColorFormat = Source2MatHelperColorFormat.RGBA;
            multiSource2MatHelper.Initialize();
        }

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = multiSource2MatHelper.GetMat();

            // Fill in the image so that the unprocessed image is not displayed.
            rgbaMat.setTo(new Scalar(0, 0, 0, 255));

            texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(rgbaMat, texture);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", rgbaMat.width().ToString());
                fpsMonitor.Add("height", rgbaMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            Mat downScaleMat = imageOptimizationHelper.GetDownScaleMat((rgbaMat));

            gray1Mat = new Mat(downScaleMat.rows(), downScaleMat.cols(), CvType.CV_8UC1);
            gray2Mat = new Mat(downScaleMat.rows(), downScaleMat.cols(), CvType.CV_8UC1);

            int ksize = 7;
            float[] kernelData = new float[ksize * ksize];
            for (int i = 0; i < kernelData.Length; i++)
            {
                if (i == kernelData.Length / 2)
                {
                    kernelData[i] = (-(kernelData.Length - 1));
                }
                else
                {
                    kernelData[i] = 1;
                }
            }
            kernel = new Mat(ksize, ksize, CvType.CV_32F);
            kernel.put(0, 0, kernelData);

            byteArray = new byte[downScaleMat.width() * downScaleMat.height()];

            subdiv = new Subdiv2D();
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (gray1Mat != null)
            {
                gray1Mat.Dispose();
                gray1Mat = null;
            }

            if (gray2Mat != null)
            {
                gray2Mat.Dispose();
                gray2Mat = null;
            }

            if (kernel != null)
            {
                kernel.Dispose();
                kernel = null;
            }

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (multiSource2MatHelper.IsPlaying() && multiSource2MatHelper.DidUpdateThisFrame() && !imageOptimizationHelper.IsCurrentFrameSkipped())
            {

                Mat rgbaMat = multiSource2MatHelper.GetMat();

                //get downScaleMat;
                Mat downScaleRgbaMat = imageOptimizationHelper.GetDownScaleMat((rgbaMat));

                //grayscale
                Imgproc.cvtColor(downScaleRgbaMat, gray1Mat, Imgproc.COLOR_RGBA2GRAY);

                //blur
                Imgproc.blur(gray1Mat, gray2Mat, new Size(5, 5));

                //edge filter
                Imgproc.filter2D(gray2Mat, gray1Mat, gray1Mat.depth(), kernel);

                //blur
                Imgproc.blur(gray1Mat, gray2Mat, new Size(3, 3));

                //detect edge
                Imgproc.threshold(gray2Mat, gray2Mat, EDGE_DETECT_VALUE, 255, Imgproc.THRESH_BINARY);

                //copy Mat to byteArray
                MatUtils.copyFromMat<byte>(gray2Mat, byteArray);

                //set edge pointList
                List<Point> pointList = new List<Point>();
                int w = gray1Mat.width();
                int h = gray1Mat.height();
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (byteArray[x + w * y] == 255)
                            pointList.Add(new Point(x, y));
                    }
                }

                int limit = Mathf.RoundToInt((float)(pointList.Count * POINT_RATE));
                if (limit > POINT_MAX_NUM)
                    limit = POINT_MAX_NUM;

                while (pointList.Count > limit)
                {
                    pointList.RemoveAt(Random.Range(0, pointList.Count));
                }
                //Debug.Log ("pointList.Count " + pointList.Count);


                //init subdiv
                subdiv.initDelaunay(new OpenCVForUnity.CoreModule.Rect(0, 0, downScaleRgbaMat.width(), downScaleRgbaMat.height()));
                for (int i = 0; i < pointList.Count; i++)
                {
                    subdiv.insert(pointList[i]);
                }
                subdiv.insert(new Point(0, 0));
                subdiv.insert(new Point(gray1Mat.width() / 2 - 1, 0));
                subdiv.insert(new Point(gray1Mat.width() - 1, 0));
                subdiv.insert(new Point(gray1Mat.width() - 1, gray1Mat.height() / 2 - 1));
                subdiv.insert(new Point(gray1Mat.width() - 1, gray1Mat.height() - 1));
                subdiv.insert(new Point(gray1Mat.width() / 2 - 1, gray1Mat.height() - 1));
                subdiv.insert(new Point(0, gray1Mat.height() - 1));
                subdiv.insert(new Point(0, gray1Mat.height() / 2 - 1));


                using (MatOfFloat6 triangleList = new MatOfFloat6())
                {
                    subdiv.getTriangleList(triangleList);

                    float[] pointArray = triangleList.toArray();
                    float downScaleRatio = imageOptimizationHelper.downscaleRatio;
                    if (downScaleRatio < 1)
                        downScaleRatio = 1;
                    byte[] color = new byte[4];
                    for (int i = 0; i < pointArray.Length / 6; i++)
                    {

                        Point p0 = new Point(pointArray[i * 6 + 0] * downScaleRatio, pointArray[i * 6 + 1] * downScaleRatio);
                        Point p1 = new Point(pointArray[i * 6 + 2] * downScaleRatio, pointArray[i * 6 + 3] * downScaleRatio);
                        Point p2 = new Point(pointArray[i * 6 + 4] * downScaleRatio, pointArray[i * 6 + 5] * downScaleRatio);

                        if (p0.x < 0 || p0.x > rgbaMat.width())
                            continue;
                        if (p0.y < 0 || p0.y > rgbaMat.height())
                            continue;
                        if (p1.x < 0 || p1.x > rgbaMat.width())
                            continue;
                        if (p1.y < 0 || p1.y > rgbaMat.height())
                            continue;
                        if (p2.x < 0 || p2.x > rgbaMat.width())
                            continue;
                        if (p2.y < 0 || p2.y > rgbaMat.height())
                            continue;


                        //get center of gravity
                        int cx = (int)((p0.x + p1.x + p2.x) * 0.33333);
                        int cy = (int)((p0.y + p1.y + p2.y) * 0.33333);
                        //                Debug.Log ("cx " + cx + " cy " + cy );

                        //get center of gravity color
                        rgbaMat.get(cy, cx, color);
                        //Debug.Log ("r " + color[0] + " g " + color[1] + " b " + color[2] + " a " + color[3]);

                        //fill Polygon
                        Imgproc.fillConvexPoly(rgbaMat, new MatOfPoint(p0, p1, p2), new Scalar(color[0], color[1], color[2], color[3]), Imgproc.LINE_AA, 0);


                        //Imgproc.line (rgbaMat, p0, p1, new Scalar (64, 255, 128, 255));
                        //Imgproc.line (rgbaMat, p1, p2, new Scalar (64, 255, 128, 255));
                        //Imgproc.line (rgbaMat, p2, p0, new Scalar (64, 255, 128, 255));
                    }
                }

                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " DOWNSCALE W:" + downScaleRgbaMat.width () + " H:" + downScaleRgbaMat.height (), new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Utils.matToTexture2D(rgbaMat, texture);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            multiSource2MatHelper.Dispose();
            imageOptimizationHelper.Dispose();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            multiSource2MatHelper.requestedIsFrontFacing = !multiSource2MatHelper.requestedIsFrontFacing;
        }
    }
}