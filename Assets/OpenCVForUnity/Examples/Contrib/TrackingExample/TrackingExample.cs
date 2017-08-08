using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Tracking example.
    /// An example of object tracking using the tracking (Tracking API) module.
    /// http://docs.opencv.org/trunk/d5/d07/tutorial_multitracker.html
    /// </summary>
    public class TrackingExample : MonoBehaviour
    {
        /// <summary>
        /// The capture.
        /// </summary>
        VideoCapture capture;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        Mat rgbMat;

        /// <summary>
        /// The colors.
        /// </summary>
        Color32[] colors;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The trackers.
        /// </summary>
        MultiTracker trackers;

        /// <summary>
        /// The objects.
        /// </summary>
        MatOfRect2d objects;

        /// <summary>
        /// The tracking color list.
        /// </summary>
        List<Scalar> trackingColorList;

        /// <summary>
        /// The selected point list.
        /// </summary>
        List<Point> selectedPointList;

        #if UNITY_WEBGL && !UNITY_EDITOR
        Stack<IEnumerator> coroutines = new Stack<IEnumerator> ();
        #endif
        
        // Use this for initialization
        void Start ()
        {
            capture = new VideoCapture ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            var getFilePath_Coroutine = Utils.getFilePathAsync("768x576_mjpeg.mjpeg", (result) => {
                coroutines.Clear ();
            
                capture.open (result);
                Init ();
            });
            coroutines.Push (getFilePath_Coroutine);
            StartCoroutine (getFilePath_Coroutine);
            #else
            capture.open (Utils.getFilePath ("768x576_mjpeg.mjpeg"));
            Init ();
            #endif
        }

        private void Init ()
        {
            rgbMat = new Mat ();

            if (capture.isOpened ()) {
                Debug.Log ("capture.isOpened() true");
            } else {
                Debug.Log ("capture.isOpened() false");
            }


            Debug.Log ("CAP_PROP_FORMAT: " + capture.get (Videoio.CAP_PROP_FORMAT));
            Debug.Log ("CV_CAP_PROP_PREVIEW_FORMAT: " + capture.get (Videoio.CV_CAP_PROP_PREVIEW_FORMAT));
            Debug.Log ("CAP_PROP_POS_MSEC: " + capture.get (Videoio.CAP_PROP_POS_MSEC));
            Debug.Log ("CAP_PROP_POS_FRAMES: " + capture.get (Videoio.CAP_PROP_POS_FRAMES));
            Debug.Log ("CAP_PROP_POS_AVI_RATIO: " + capture.get (Videoio.CAP_PROP_POS_AVI_RATIO));
            Debug.Log ("CAP_PROP_FRAME_COUNT: " + capture.get (Videoio.CAP_PROP_FRAME_COUNT));
            Debug.Log ("CAP_PROP_FPS: " + capture.get (Videoio.CAP_PROP_FPS));
            Debug.Log ("CAP_PROP_FRAME_WIDTH: " + capture.get (Videoio.CAP_PROP_FRAME_WIDTH));
            Debug.Log ("CAP_PROP_FRAME_HEIGHT: " + capture.get (Videoio.CAP_PROP_FRAME_HEIGHT));

            capture.grab ();
            capture.retrieve (rgbMat, 0);
            int frameWidth = rgbMat.cols ();
            int frameHeight = rgbMat.rows ();
            colors = new Color32[frameWidth * frameHeight];
            texture = new Texture2D (frameWidth, frameHeight, TextureFormat.RGBA32, false);
            gameObject.transform.localScale = new Vector3 ((float)frameWidth, (float)frameHeight, 1);
            float widthScale = (float)Screen.width / (float)frameWidth;
            float heightScale = (float)Screen.height / (float)frameHeight;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = ((float)frameWidth * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = (float)frameHeight / 2;
            }
            capture.set (Videoio.CAP_PROP_POS_FRAMES, 0);


            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;


            trackers = new MultiTracker ("KCF");
            objects = new MatOfRect2d ();

            trackingColorList = new List<Scalar> ();
            selectedPointList = new List<Point> ();
        }

        // Update is called once per frame
        void Update ()
        {
            //Loop play
            if (capture.get (Videoio.CAP_PROP_POS_FRAMES) >= capture.get (Videoio.CAP_PROP_FRAME_COUNT))
                capture.set (Videoio.CAP_PROP_POS_FRAMES, 0);

            //error PlayerLoop called recursively! on iOS.reccomend WebCamTexture.
            if (capture.grab ()) {

                capture.retrieve (rgbMat, 0);
                Imgproc.cvtColor (rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);


                selectPoint (rgbMat);
                if (selectedPointList.Count < 2) {
                    foreach (var point in selectedPointList) {
                        Imgproc.circle (rgbMat, point, 6, new Scalar (0, 0, 255), 2);
                    }
                } else {
                    trackers.add (rgbMat, new Rect2d (selectedPointList [0].x, selectedPointList [0].y, Math.Abs (selectedPointList [1].x - selectedPointList [0].x), Math.Abs (selectedPointList [1].y - selectedPointList [0].y)));
                    selectedPointList.Clear ();
                    trackingColorList.Add (new Scalar (UnityEngine.Random.Range (0, 255), UnityEngine.Random.Range (0, 255), UnityEngine.Random.Range (0, 255)));
                }

                trackers.update (rgbMat, objects);

                Rect2d[] objectsArray = objects.toArray ();
                for (int i = 0; i < objectsArray.Length; i++) {
                    Imgproc.rectangle (rgbMat, objectsArray [i].tl (), objectsArray [i].br (), trackingColorList [i], 2, 1, 0);
                }
                
                //Debug.Log ("Mat toString " + rgbMat.ToString ());
                
                Utils.matToTexture2D (rgbMat, texture, colors);
            }
        }

        /// <summary>
        /// Selects the point.
        /// </summary>
        /// <param name="mat">Mat.</param>
        private void selectPoint (Mat mat)
        {
            if (selectedPointList.Count < 2) {
                
                #if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
                //Touch
                int touchCount = Input.touchCount;
                if (touchCount == 1)
                {
                    Touch t = Input.GetTouch(0);
                    if(EventSystem.current.IsPointerOverGameObject(t.fingerId))return;

                    if(t.phase == TouchPhase.Ended){
                        selectedPointList.Add (convertScreenPointToMatPoint (new Point (t.position.x, t.position.y), gameObject, Camera.main));
                        //                                  Debug.Log ("touch X " + t.position.x);
                        //                                  Debug.Log ("touch Y " + t.position.y);
                        
                        if (!(new OpenCVForUnity.Rect (0, 0, mat.width (), mat.height ()).contains (selectedPointList [selectedPointList.Count - 1]))) {
                            selectedPointList.RemoveAt (selectedPointList.Count - 1);
                        }
                    }
                }
                #else
                //Mouse
                if (Input.GetMouseButtonUp (0)) {
                    if (EventSystem.current.IsPointerOverGameObject ())
                        return;
                    
                    selectedPointList.Add (convertScreenPointToMatPoint (new Point (Input.mousePosition.x, Input.mousePosition.y), gameObject, Camera.main));
                    //                                              Debug.Log ("mouse X " + Input.mousePosition.x);
                    //                                              Debug.Log ("mouse Y " + Input.mousePosition.y);
                    
                    if (!(new OpenCVForUnity.Rect (0, 0, mat.width (), mat.height ()).contains (selectedPointList [selectedPointList.Count - 1]))) {
                        selectedPointList.RemoveAt (selectedPointList.Count - 1);
                    }
                }
                #endif
            }
        }
        
        /// <summary>
        /// Converts the screen point to texture point.
        /// </summary>
        /// <returns>The screen point to texture point.</returns>
        /// <param name="screenPoint">Screen point.</param>
        /// <param name="quad">Quad.</param>
        /// <param name="cam">Cam.</param>
        private Point convertScreenPointToMatPoint (Point screenPoint, GameObject quad, Camera cam)
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

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            capture.release ();

            if (rgbMat != null)
                rgbMat.Dispose ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            foreach (var coroutine in coroutines) {
                StopCoroutine (coroutine);
                ((IDisposable)coroutine).Dispose ();
            }
            #endif
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
        /// Raises the reset tracker button click event.
        /// </summary>
        public void OnResetTrackerButtonClick ()
        {
            if (trackers != null) {
                trackers.Dispose ();
                trackers = null;
            }
            if (objects != null) {
                objects.Dispose ();
                objects = null;
            }

            trackers = new MultiTracker ("KCF");
            objects = new MatOfRect2d ();
            trackingColorList.Clear ();
            selectedPointList.Clear ();
        }
    }
}