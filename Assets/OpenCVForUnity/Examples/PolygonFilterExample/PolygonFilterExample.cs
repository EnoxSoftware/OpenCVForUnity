using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Polygon filter example.
    /// referring to the http://jsdo.it/hedger/tIod.
    /// </summary>
    [RequireComponent(typeof(OptimizationWebCamTextureToMatHelper))]
    public class PolygonFilterExample : MonoBehaviour
    {
        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The web cam texture to mat helper.
        /// </summary>
        OptimizationWebCamTextureToMatHelper webCamTextureToMatHelper;

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

        // Use this for initialization
        void Start ()
        {
            webCamTextureToMatHelper = gameObject.GetComponent<OptimizationWebCamTextureToMatHelper> ();
            webCamTextureToMatHelper.Init ();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper inited event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInited ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInited");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();

            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

                                    
            float width = webCamTextureMat.width ();
            float height = webCamTextureMat.height ();
                                    
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }


            Mat downScaleMat = webCamTextureToMatHelper.GetDownScaleMat (webCamTextureMat);

            gray1Mat = new Mat (downScaleMat.rows (), downScaleMat.cols (), CvType.CV_8UC1);
            gray2Mat = new Mat (downScaleMat.rows (), downScaleMat.cols (), CvType.CV_8UC1);

            int ksize = 7;
            float[] kernelData = new float[ksize * ksize];
            for (int i = 0; i < kernelData.Length; i++) {
                if (i == kernelData.Length / 2) {
                    kernelData [i] = (-(kernelData.Length - 1));
                } else {
                    kernelData [i] = 1;
                }
            }
            kernel = new Mat (ksize, ksize, CvType.CV_32F);
            kernel.put (0, 0, kernelData);

            byteArray = new byte[downScaleMat.width () * downScaleMat.height ()];

            subdiv = new Subdiv2D ();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

            if (gray1Mat != null) {
                gray1Mat.Dispose ();
                gray1Mat = null;
            }

            if (gray2Mat != null) {
                gray2Mat.Dispose ();
                gray2Mat = null;
            }

            if (kernel != null) {
                kernel.Dispose ();
                kernel = null;
            }
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred (WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log ("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update ()
        {
            if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

                //get downScaleMat;
                Mat downScaleRgbaMat = webCamTextureToMatHelper.GetDownScaleMat (rgbaMat);

                //grayscale
                Imgproc.cvtColor (downScaleRgbaMat, gray1Mat, Imgproc.COLOR_RGBA2GRAY);
                
                //blur
                Imgproc.blur (gray1Mat, gray2Mat, new Size (5, 5));

                //edge filter
                Imgproc.filter2D (gray2Mat, gray1Mat, gray1Mat.depth (), kernel); 

                //blur
                Imgproc.blur (gray1Mat, gray2Mat, new Size (3, 3));

                //detect edge
                Imgproc.threshold (gray2Mat, gray2Mat, EDGE_DETECT_VALUE, 255, Imgproc.THRESH_BINARY);

                //copy Mat to byteArray
                Utils.copyFromMat<byte> (gray2Mat, byteArray);

                //set edge pointList
                List<Point> pointList = new List<Point> ();
                int w = gray1Mat.width ();
                int h = gray1Mat.height ();
                for (int y = 0; y < h; y++) {
                    for (int x = 0; x < w; x++) {
                        if (byteArray [x + w * y] == 255)
                            pointList.Add (new Point (x, y));
                    }
                }
                
                int limit = Mathf.RoundToInt ((float)(pointList.Count * POINT_RATE));
                if (limit > POINT_MAX_NUM)
                    limit = POINT_MAX_NUM;
                
                while (pointList.Count > limit) {
                    pointList.RemoveAt (Random.Range (0, pointList.Count));
                }
//              Debug.Log ("pointList.Count " + pointList.Count);


                //init subdiv
                subdiv.initDelaunay (new OpenCVForUnity.Rect (0, 0, downScaleRgbaMat.width (), downScaleRgbaMat.height ()));
                for (int i = 0; i < pointList.Count; i++) {
                    subdiv.insert (pointList [i]);
                }
                subdiv.insert (new Point (0, 0));
                subdiv.insert (new Point (gray1Mat.width () / 2 - 1, 0));
                subdiv.insert (new Point (gray1Mat.width () - 1, 0));
                subdiv.insert (new Point (gray1Mat.width () - 1, gray1Mat.height () / 2 - 1));
                subdiv.insert (new Point (gray1Mat.width () - 1, gray1Mat.height () - 1));
                subdiv.insert (new Point (gray1Mat.width () / 2 - 1, gray1Mat.height () - 1));
                subdiv.insert (new Point (0, gray1Mat.height () - 1));
                subdiv.insert (new Point (0, gray1Mat.height () / 2 - 1));


                using (MatOfFloat6 triangleList = new MatOfFloat6()) {
                    subdiv.getTriangleList (triangleList);
                
                    float[] pointArray = triangleList.toArray ();
                    float downScaleRatio = webCamTextureToMatHelper.DOWNSCALE_RATIO;
                    if (downScaleRatio < 1)
                        downScaleRatio = 1;
                    byte[] color = new byte[4];
                    for (int i = 0; i < pointArray.Length/6; i++) {
                    
                        Point p0 = new Point (pointArray [i * 6 + 0] * downScaleRatio, pointArray [i * 6 + 1] * downScaleRatio);
                        Point p1 = new Point (pointArray [i * 6 + 2] * downScaleRatio, pointArray [i * 6 + 3] * downScaleRatio);
                        Point p2 = new Point (pointArray [i * 6 + 4] * downScaleRatio, pointArray [i * 6 + 5] * downScaleRatio);
                  
                        if (p0.x < 0 || p0.x > rgbaMat.width ())
                            continue;
                        if (p0.y < 0 || p0.y > rgbaMat.height ())
                            continue;
                        if (p1.x < 0 || p1.x > rgbaMat.width ())
                            continue;
                        if (p1.y < 0 || p1.y > rgbaMat.height ())
                            continue;
                        if (p2.x < 0 || p2.x > rgbaMat.width ())
                            continue;
                        if (p2.y < 0 || p2.y > rgbaMat.height ())
                            continue;
                    
                    
                        //get center of gravity
                        int cx = (int)((p0.x + p1.x + p2.x) * 0.33333);
                        int cy = (int)((p0.y + p1.y + p2.y) * 0.33333);
                        //                Debug.Log ("cx " + cx + " cy " + cy );
                    
                        //get center of gravity color
                        rgbaMat.get (cy, cx, color);
                        //                Debug.Log ("r " + color[0] + " g " + color[1] + " b " + color[2] + " a " + color[3]);
                    
                        //fill Polygon
                        Imgproc.fillConvexPoly (rgbaMat, new MatOfPoint (p0, p1, p2), new Scalar (color [0], color [1], color [2], color [3]), Imgproc.LINE_AA, 0);
                       

//                        Imgproc.line (rgbaMat, p0, p1, new Scalar (64, 255, 128, 255));
//                        Imgproc.line (rgbaMat, p1, p2, new Scalar (64, 255, 128, 255));
//                        Imgproc.line (rgbaMat, p2, p0, new Scalar (64, 255, 128, 255));
                    }
                }

                Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " DOWNSCALE W:" + downScaleRgbaMat.width () + " H:" + downScaleRgbaMat.height (), new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Utils.matToTexture2D (rgbaMat, texture, webCamTextureToMatHelper.GetBufferColors ());
            }
        }
    
        /// <summary>
        /// Raises the disable event.
        /// </summary>
        void OnDisable ()
        {
            webCamTextureToMatHelper.Dispose ();
        }

        /// <summary>
        /// Raises the back button event.
        /// </summary>
        public void OnBackButton ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("OpenCVForUnityExample");
            #else
            Application.LoadLevel ("OpenCVForUnityExample");
            #endif
        }

        /// <summary>
        /// Raises the play button event.
        /// </summary>
        public void OnPlayButton ()
        {
            webCamTextureToMatHelper.Play ();
        }

        /// <summary>
        /// Raises the pause button event.
        /// </summary>
        public void OnPauseButton ()
        {
            webCamTextureToMatHelper.Pause ();
        }

        /// <summary>
        /// Raises the stop button event.
        /// </summary>
        public void OnStopButton ()
        {
            webCamTextureToMatHelper.Stop ();
        }

        /// <summary>
        /// Raises the change camera button event.
        /// </summary>
        public void OnChangeCameraButton ()
        {
            webCamTextureToMatHelper.Init (null, webCamTextureToMatHelper.requestWidth, webCamTextureToMatHelper.requestHeight, !webCamTextureToMatHelper.requestIsFrontFacing);
        }
    }
}