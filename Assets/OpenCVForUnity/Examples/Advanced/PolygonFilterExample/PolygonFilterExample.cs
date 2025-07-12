using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Optimization;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
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
        // Constants
        /// <summary>
        /// EDGE_DETECT_VALUE
        /// </summary>
        private const int EDGE_DETECT_VALUE = 70;

        /// <summary>
        /// POINT_RATE
        /// </summary>
        private const double POINT_RATE = 0.075;

        /// <summary>
        /// POINT_MAX_NUM
        /// </summary>
        private const int POINT_MAX_NUM = 2500;

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The image optimization helper.
        /// </summary>
        private ImageOptimizationHelper _imageOptimizationHelper;

        /// <summary>
        /// The gray1 mat.
        /// </summary>
        private Mat _gray1Mat;

        /// <summary>
        /// The gray2 mat.
        /// </summary>
        private Mat _gray2Mat;

        /// <summary>
        /// The kernel.
        /// </summary>
        private Mat _kernel;

        /// <summary>
        /// The byte array.
        /// </summary>
        private byte[] _byteArray;

        /// <summary>
        /// The subdiv.
        /// </summary>
        private Subdiv2D _subdiv;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _imageOptimizationHelper = gameObject.GetComponent<ImageOptimizationHelper>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;
            _multiSource2MatHelper.Initialize();
        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame() && !_imageOptimizationHelper.IsCurrentFrameSkipped())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                //get downScaleMat;
                Mat downScaleRgbaMat = _imageOptimizationHelper.GetDownScaleMat((rgbaMat));

                //grayscale
                Imgproc.cvtColor(downScaleRgbaMat, _gray1Mat, Imgproc.COLOR_RGBA2GRAY);

                //blur
                Imgproc.blur(_gray1Mat, _gray2Mat, new Size(5, 5));

                //edge filter
                Imgproc.filter2D(_gray2Mat, _gray1Mat, _gray1Mat.depth(), _kernel);

                //blur
                Imgproc.blur(_gray1Mat, _gray2Mat, new Size(3, 3));

                //detect edge
                Imgproc.threshold(_gray2Mat, _gray2Mat, EDGE_DETECT_VALUE, 255, Imgproc.THRESH_BINARY);

                //copy Mat to byteArray
                OpenCVMatUtils.CopyFromMat<byte>(_gray2Mat, _byteArray);

                //set edge pointList
                List<Point> pointList = new List<Point>();
                int w = _gray1Mat.width();
                int h = _gray1Mat.height();
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (_byteArray[x + w * y] == 255)
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
                _subdiv.initDelaunay(new OpenCVForUnity.CoreModule.Rect(0, 0, downScaleRgbaMat.width(), downScaleRgbaMat.height()));
                for (int i = 0; i < pointList.Count; i++)
                {
                    _subdiv.insert(pointList[i]);
                }
                _subdiv.insert(new Point(0, 0));
                _subdiv.insert(new Point(_gray1Mat.width() / 2 - 1, 0));
                _subdiv.insert(new Point(_gray1Mat.width() - 1, 0));
                _subdiv.insert(new Point(_gray1Mat.width() - 1, _gray1Mat.height() / 2 - 1));
                _subdiv.insert(new Point(_gray1Mat.width() - 1, _gray1Mat.height() - 1));
                _subdiv.insert(new Point(_gray1Mat.width() / 2 - 1, _gray1Mat.height() - 1));
                _subdiv.insert(new Point(0, _gray1Mat.height() - 1));
                _subdiv.insert(new Point(0, _gray1Mat.height() / 2 - 1));


                using (MatOfFloat6 triangleList = new MatOfFloat6())
                {
                    _subdiv.getTriangleList(triangleList);

                    float[] pointArray = triangleList.toArray();
                    float downScaleRatio = _imageOptimizationHelper.DownscaleRatio;
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

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();
            _imageOptimizationHelper?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = _multiSource2MatHelper.GetMat();

            // Fill in the image so that the unprocessed image is not displayed.
            rgbaMat.setTo(new Scalar(0, 0, 0, 255));

            _texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;


            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("width", rgbaMat.width().ToString());
                _fpsMonitor.Add("height", rgbaMat.height().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            Mat downScaleMat = _imageOptimizationHelper.GetDownScaleMat((rgbaMat));

            _gray1Mat = new Mat(downScaleMat.rows(), downScaleMat.cols(), CvType.CV_8UC1);
            _gray2Mat = new Mat(downScaleMat.rows(), downScaleMat.cols(), CvType.CV_8UC1);

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
            _kernel = new Mat(ksize, ksize, CvType.CV_32F);
            _kernel.put(0, 0, kernelData);

            _byteArray = new byte[downScaleMat.width() * downScaleMat.height()];

            _subdiv = new Subdiv2D();
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _gray1Mat?.Dispose(); _gray1Mat = null;
            _gray2Mat?.Dispose(); _gray2Mat = null;
            _kernel?.Dispose(); _kernel = null;

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "ErrorCode: " + errorCode + ":" + message;
            }
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
            _multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            _multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            _multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            _multiSource2MatHelper.RequestedIsFrontFacing = !_multiSource2MatHelper.RequestedIsFrontFacing;
        }
    }
}
