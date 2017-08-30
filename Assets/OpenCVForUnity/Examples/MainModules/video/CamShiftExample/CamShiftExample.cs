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
        /// The flag for requesting the start of the CamShift Method.
        /// </summary>
        bool shouldStartCamShift = false;

        // Use this for initialization
        void Start ()
        {
            roiPointList = new List<Point> ();
            termination = new TermCriteria (TermCriteria.EPS | TermCriteria.COUNT, 10, 1);
                        
            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();
            webCamTextureToMatHelper.Initialize ();
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
            if (roiPointList.Count == 4) {

                #if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
                //Touch
                int touchCount = Input.touchCount;
                if (touchCount == 1)
                {
                    Touch t = Input.GetTouch(0);
                    if(t.phase == TouchPhase.Ended && !EventSystem.current.IsPointerOverGameObject(t.fingerId)){
                        roiPointList.Clear ();
                    }
                }
                #else
                if (Input.GetMouseButtonUp (0) && !EventSystem.current.IsPointerOverGameObject()) {
                    roiPointList.Clear ();
                }
                #endif
            }

            if (roiPointList.Count < 4) {

                #if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
                //Touch
                int touchCount = Input.touchCount;
                if (touchCount == 1)
                {
                    Touch t = Input.GetTouch(0);
                    if(t.phase == TouchPhase.Ended && !EventSystem.current.IsPointerOverGameObject(t.fingerId)){
                        roiPointList.Add (convertScreenPoint (new Point (t.position.x, t.position.y), gameObject, Camera.main));
                        //Debug.Log ("touch X " + t.position.x);
                        //Debug.Log ("touch Y " + t.position.y);

                        if (!(new OpenCVForUnity.Rect (0, 0, hsvMat.width (), hsvMat.height ()).contains (roiPointList [roiPointList.Count - 1]))) {
                            roiPointList.RemoveAt (roiPointList.Count - 1);
                        }
                    }
                }
                #else
                //Mouse
                if (Input.GetMouseButtonUp (0) && !EventSystem.current.IsPointerOverGameObject()) {

                    roiPointList.Add (convertScreenPoint (new Point (Input.mousePosition.x, Input.mousePosition.y), gameObject, Camera.main));
                    //                                              Debug.Log ("mouse X " + Input.mousePosition.x);
                    //                                              Debug.Log ("mouse Y " + Input.mousePosition.y);

                    if (!(new OpenCVForUnity.Rect (0, 0, hsvMat.width (), hsvMat.height ()).contains (roiPointList [roiPointList.Count - 1]))) {
                        roiPointList.RemoveAt (roiPointList.Count - 1);
                    }
                }
                #endif

                if (roiPointList.Count == 4) {
                    shouldStartCamShift = true;
                }
            }

            if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {
                
                Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

                Imgproc.cvtColor (rgbaMat, hsvMat, Imgproc.COLOR_RGBA2RGB);
                Imgproc.cvtColor (hsvMat, hsvMat, Imgproc.COLOR_RGB2HSV);
                
                
                Point[] points = roiPointList.ToArray ();

                if (shouldStartCamShift) {
                    shouldStartCamShift = false;

                    using (MatOfPoint roiPointMat = new MatOfPoint (roiPointList.ToArray ())) {
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
                }else if (roiPointList.Count == 4) {
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
            webCamTextureToMatHelper.Initialize (null, webCamTextureToMatHelper.requestedWidth, webCamTextureToMatHelper.requestedHeight, !webCamTextureToMatHelper.requestedIsFrontFacing);
        }

        /// <summary>
        /// Converts the screen point.
        /// </summary>
        /// <returns>The converted point.</returns>
        /// <param name="screenPoint">Screen point.</param>
        /// <param name="quad">Quad.</param>
        /// <param name="cam">Cam.</param>
        private Point convertScreenPoint (Point screenPoint, GameObject quad, Camera cam)
        {
            Vector2 tl;
            Vector2 tr;
            Vector2 br;
            Vector2 bl;

            tl = cam.WorldToScreenPoint (new Vector3 (quad.transform.localPosition.x - quad.transform.localScale.x / 2, quad.transform.localPosition.y + quad.transform.localScale.y / 2, quad.transform.localPosition.z));
            tr = cam.WorldToScreenPoint (new Vector3 (quad.transform.localPosition.x + quad.transform.localScale.x / 2, quad.transform.localPosition.y + quad.transform.localScale.y / 2, quad.transform.localPosition.z));
            br = cam.WorldToScreenPoint (new Vector3 (quad.transform.localPosition.x + quad.transform.localScale.x / 2, quad.transform.localPosition.y - quad.transform.localScale.y / 2, quad.transform.localPosition.z));
            bl = cam.WorldToScreenPoint (new Vector3 (quad.transform.localPosition.x - quad.transform.localScale.x / 2, quad.transform.localPosition.y - quad.transform.localScale.y / 2, quad.transform.localPosition.z));


            Mat srcRectMat = new Mat (4, 1, CvType.CV_32FC2);
            Mat dstRectMat = new Mat (4, 1, CvType.CV_32FC2);

                        
            srcRectMat.put (0, 0, tl.x, tl.y, tr.x, tr.y, br.x, br.y, bl.x, bl.y);
            dstRectMat.put (0, 0, 0.0, 0.0, quad.transform.localScale.x, 0.0, quad.transform.localScale.x, quad.transform.localScale.y, 0.0, quad.transform.localScale.y);

                        
            Mat perspectiveTransform = Imgproc.getPerspectiveTransform (srcRectMat, dstRectMat);

//                      Debug.Log ("srcRectMat " + srcRectMat.dump ());
//                      Debug.Log ("dstRectMat " + dstRectMat.dump ());
//                      Debug.Log ("perspectiveTransform " + perspectiveTransform.dump ());

            MatOfPoint2f srcPointMat = new MatOfPoint2f (screenPoint);
            MatOfPoint2f dstPointMat = new MatOfPoint2f ();

            Core.perspectiveTransform (srcPointMat, dstPointMat, perspectiveTransform);

//                      Debug.Log ("srcPointMat " + srcPointMat.dump ());
//                      Debug.Log ("dstPointMat " + dstPointMat.dump ());

            return dstPointMat.toArray () [0];
        }
    }
}