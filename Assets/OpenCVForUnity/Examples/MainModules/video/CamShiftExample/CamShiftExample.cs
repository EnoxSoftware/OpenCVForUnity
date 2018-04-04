using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// CamShift Example
    /// An example of object tracking using the Video.Camshift function.
    /// Referring to http://www.computervisiononline.com/blog/tutorial-using-camshift-track-objects-video.
    /// http://docs.opencv.org/3.2.0/db/df8/tutorial_py_meanshift.html
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class CamShiftExample : MonoBehaviour
    {
        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The roi point list.
        /// </summary>
        List<Point> roiPointList;

        /// <summary>
        /// The roi rect.
        /// </summary>
        OpenCVForUnity.Rect roiRect;

        /// <summary>
        /// The hsv mat.
        /// </summary>
        Mat hsvMat;

        /// <summary>
        /// The roi hist mat.
        /// </summary>
        Mat roiHistMat;

        /// <summary>
        /// The termination.
        /// </summary>
        TermCriteria termination;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The stored touch point.
        /// </summary>
        Point storedTouchPoint;

        /// <summary>
        /// The flag for requesting the start of the CamShift Method.
        /// </summary>
        bool shouldStartCamShift = false;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        #if UNITY_ANDROID && !UNITY_EDITOR
        float rearCameraRequestedFPS;
        #endif

        // Use this for initialization
        void Start ()
        {
            fpsMonitor = GetComponent<FpsMonitor> ();

            roiPointList = new List<Point> ();
            termination = new TermCriteria (TermCriteria.EPS | TermCriteria.COUNT, 10, 1);
                        
            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();

            #if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices. (Pixel, pixel 2)
            // https://forum.unity.com/threads/android-webcamtexture-in-low-light-only-some-models.520656/
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            rearCameraRequestedFPS = webCamTextureToMatHelper.requestedFPS;
            if (webCamTextureToMatHelper.requestedIsFrontFacing) {                
                webCamTextureToMatHelper.requestedFPS = 15;
                webCamTextureToMatHelper.Initialize ();
            } else {
                webCamTextureToMatHelper.Initialize ();
            }
            #else
            webCamTextureToMatHelper.Initialize ();
            #endif
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInitialized");
            
            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();

            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
            
            gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
            
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null){
                fpsMonitor.Add ("width", webCamTextureMat.width ().ToString());
                fpsMonitor.Add ("height", webCamTextureMat.height ().ToString());
                fpsMonitor.Add ("orientation", Screen.orientation.ToString());
            }
           
            
            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();
            
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }

            hsvMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC3);
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

            hsvMat.Dispose ();
            if (roiHistMat != null)
                roiHistMat.Dispose ();
            roiPointList.Clear ();

            if (texture != null) {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode){
            Debug.Log ("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update ()
        {
            #if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            //Touch
            int touchCount = Input.touchCount;
            if (touchCount == 1)
            {
                Touch t = Input.GetTouch(0);
                if(t.phase == TouchPhase.Ended && !EventSystem.current.IsPointerOverGameObject(t.fingerId)){
                    storedTouchPoint = new Point (t.position.x, t.position.y);
                    //Debug.Log ("touch X " + t.position.x);
                    //Debug.Log ("touch Y " + t.position.y);
                }
            }
            #else
            //Mouse
            if (Input.GetMouseButtonUp (0) && !EventSystem.current.IsPointerOverGameObject()) {
                storedTouchPoint = new Point (Input.mousePosition.x, Input.mousePosition.y);
                //Debug.Log ("mouse X " + Input.mousePosition.x);
                //Debug.Log ("mouse Y " + Input.mousePosition.y);
            }
            #endif

            if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {
                
                Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

                Imgproc.cvtColor (rgbaMat, hsvMat, Imgproc.COLOR_RGBA2RGB);
                Imgproc.cvtColor (hsvMat, hsvMat, Imgproc.COLOR_RGB2HSV);

                if(storedTouchPoint != null) {
                    ConvertScreenPointToTexturePoint (storedTouchPoint, storedTouchPoint, gameObject, rgbaMat.cols (), rgbaMat.rows ());
                    OnTouch (rgbaMat, storedTouchPoint);
                    storedTouchPoint = null;
                }
                
                Point[] points = roiPointList.ToArray ();

                if (shouldStartCamShift) {
                    shouldStartCamShift = false;

                    using (MatOfPoint roiPointMat = new MatOfPoint (points)) {
                        roiRect = Imgproc.boundingRect (roiPointMat);
                    }

                    if (roiHistMat != null) {
                        roiHistMat.Dispose ();
                        roiHistMat = null;
                    }
                    roiHistMat = new Mat ();

                    using (Mat roiHSVMat = new Mat(hsvMat, roiRect))
                    using (Mat maskMat = new Mat ()) {
                        Imgproc.calcHist (new List<Mat> (new Mat[]{roiHSVMat}), new MatOfInt (0), maskMat, roiHistMat, new MatOfInt (16), new MatOfFloat (0, 180)); 
                        Core.normalize (roiHistMat, roiHistMat, 0, 255, Core.NORM_MINMAX);

                        //Debug.Log ("roiHist " + roiHistMat.ToString ());
                    }
                }else if (points.Length == 4) {
                    using (Mat backProj = new Mat ()) {
                        Imgproc.calcBackProject (new List<Mat> (new Mat[]{hsvMat}), new MatOfInt (0), roiHistMat, backProj, new MatOfFloat (0, 180), 1.0);
                
                        RotatedRect r = Video.CamShift (backProj, roiRect, termination);
                        r.points (points);
                    }
                }
                
                if (points.Length < 4) {
                    for (int i = 0; i < points.Length; i++) {
                        Imgproc.circle (rgbaMat, points [i], 6, new Scalar (0, 0, 255, 255), 2);
                    }
                
                } else {
                    for (int i = 0; i < 4; i++) {
                        Imgproc.line (rgbaMat, points [i], points [(i + 1) % 4], new Scalar (255, 0, 0, 255), 2);
                    }
                
                    Imgproc.rectangle (rgbaMat, roiRect.tl (), roiRect.br (), new Scalar (0, 255, 0, 255), 2);
                }
                
                Imgproc.putText (rgbaMat, "Please touch the 4 points surrounding the tracking object.", new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                
//              Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                
                Utils.matToTexture2D (rgbaMat, texture, webCamTextureToMatHelper.GetBufferColors());
            }
        }
            
        private void OnTouch (Mat img, Point touchPoint)
        {
            if (roiPointList.Count == 4) {
                    roiPointList.Clear ();
            }

            if (roiPointList.Count < 4) {
                roiPointList.Add (touchPoint);

                if (!(new OpenCVForUnity.Rect (0, 0, img.width (), img.height ()).contains (roiPointList [roiPointList.Count - 1]))) {
                    roiPointList.RemoveAt (roiPointList.Count - 1);
                }

                if (roiPointList.Count == 4) {
                    shouldStartCamShift = true;
                }
            }
        }

        /// <summary>
        /// Converts the screen point to texture point.
        /// </summary>
        /// <param name="screenPoint">Screen point.</param>
        /// <param name="dstPoint">Dst point.</param>
        /// <param name="texturQuad">Texture quad.</param>
        /// <param name="textureWidth">Texture width.</param>
        /// <param name="textureHeight">Texture height.</param>
        /// <param name="camera">Camera.</param>
        private void ConvertScreenPointToTexturePoint (Point screenPoint, Point dstPoint, GameObject textureQuad, int textureWidth = -1, int textureHeight = -1, Camera camera = null)
        {
            if (textureWidth < 0 || textureHeight < 0) {
                Renderer r = textureQuad.GetComponent<Renderer> ();
                if (r != null && r.material != null && r.material.mainTexture != null) {
                    textureWidth = r.material.mainTexture.width;
                    textureHeight = r.material.mainTexture.height;
                } else {
                    textureWidth = (int)textureQuad.transform.localScale.x;
                    textureHeight = (int)textureQuad.transform.localScale.y;
                }
            }

            if (camera == null)
                camera = Camera.main;

            Vector3 quadPosition = textureQuad.transform.localPosition;
            Vector3 quadScale = textureQuad.transform.localScale;

            Vector2 tl = camera.WorldToScreenPoint (new Vector3 (quadPosition.x - quadScale.x / 2, quadPosition.y + quadScale.y / 2, quadPosition.z));
            Vector2 tr = camera.WorldToScreenPoint (new Vector3 (quadPosition.x + quadScale.x / 2, quadPosition.y + quadScale.y / 2, quadPosition.z));
            Vector2 br = camera.WorldToScreenPoint (new Vector3 (quadPosition.x + quadScale.x / 2, quadPosition.y - quadScale.y / 2, quadPosition.z));
            Vector2 bl = camera.WorldToScreenPoint (new Vector3 (quadPosition.x - quadScale.x / 2, quadPosition.y - quadScale.y / 2, quadPosition.z));                       

            using(Mat srcRectMat = new Mat (4, 1, CvType.CV_32FC2))
            using(Mat dstRectMat = new Mat (4, 1, CvType.CV_32FC2)) {
                srcRectMat.put (0, 0, tl.x, tl.y, tr.x, tr.y, br.x, br.y, bl.x, bl.y);
                dstRectMat.put (0, 0, 0, 0, quadScale.x, 0, quadScale.x, quadScale.y, 0, quadScale.y);            

                using(Mat perspectiveTransform = Imgproc.getPerspectiveTransform (srcRectMat, dstRectMat))
                using(MatOfPoint2f srcPointMat = new MatOfPoint2f (screenPoint))
                using(MatOfPoint2f dstPointMat = new MatOfPoint2f ()) {
                    Core.perspectiveTransform (srcPointMat, dstPointMat, perspectiveTransform);

                    dstPoint.x = dstPointMat.get(0,0)[0] * textureWidth / quadScale.x;
                    dstPoint.y = dstPointMat.get(0,0)[1] * textureHeight / quadScale.y;
                }
            }
        }
    
        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            webCamTextureToMatHelper.Dispose ();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("OpenCVForUnityExample");
            #else
            Application.LoadLevel ("OpenCVForUnityExample");
            #endif
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick ()
        {
            webCamTextureToMatHelper.Play ();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick ()
        {
            webCamTextureToMatHelper.Pause ();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick ()
        {
            webCamTextureToMatHelper.Stop ();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick ()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (!webCamTextureToMatHelper.IsFrontFacing ()) {
                rearCameraRequestedFPS = webCamTextureToMatHelper.requestedFPS;
                webCamTextureToMatHelper.Initialize (!webCamTextureToMatHelper.IsFrontFacing (), 15, webCamTextureToMatHelper.rotate90Degree);
            } else {                
                webCamTextureToMatHelper.Initialize (!webCamTextureToMatHelper.IsFrontFacing (), rearCameraRequestedFPS, webCamTextureToMatHelper.rotate90Degree);
            }
            #else
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing ();
            #endif
        }
    }
}