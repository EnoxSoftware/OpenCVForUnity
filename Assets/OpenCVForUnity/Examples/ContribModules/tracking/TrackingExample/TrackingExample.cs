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
    /// Tracking Example
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

        /// <summary>
        /// The stored touch point.
        /// </summary>
        Point storedTouchPoint;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        #if UNITY_WEBGL && !UNITY_EDITOR
        Stack<IEnumerator> coroutines = new Stack<IEnumerator> ();
        #endif
        
        // Use this for initialization
        void Start ()
        {
            fpsMonitor = GetComponent<FpsMonitor> ();

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


            trackers = MultiTracker.create ();
            objects = new MatOfRect2d ();

            trackingColorList = new List<Scalar> ();
            selectedPointList = new List<Point> ();
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

            if (selectedPointList.Count == 1) {
                if(storedTouchPoint != null) {
                    ConvertScreenPointToTexturePoint (storedTouchPoint, storedTouchPoint, gameObject, rgbMat.cols (), rgbMat.rows ());
                    OnTouch (rgbMat, storedTouchPoint);
                    storedTouchPoint = null;
                }
            }

            //Loop play
            if (capture.get (Videoio.CAP_PROP_POS_FRAMES) >= capture.get (Videoio.CAP_PROP_FRAME_COUNT))
                capture.set (Videoio.CAP_PROP_POS_FRAMES, 0);

            //error PlayerLoop called recursively! on iOS.reccomend WebCamTexture.
            if (selectedPointList.Count != 1 && capture.grab ()) {

                capture.retrieve (rgbMat, 0);
                Imgproc.cvtColor (rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);

                if(storedTouchPoint != null) {
                    ConvertScreenPointToTexturePoint (storedTouchPoint, storedTouchPoint, gameObject, rgbMat.cols (), rgbMat.rows ());
                    OnTouch (rgbMat, storedTouchPoint);
                    storedTouchPoint = null;
                }

                if (selectedPointList.Count < 2) {
                    foreach (var point in selectedPointList) {
                        Imgproc.circle (rgbMat, point, 6, new Scalar (0, 0, 255), 2);
                    }
                } else {
                    using (MatOfPoint selectedPointMat = new MatOfPoint (selectedPointList.ToArray ())) {
                        OpenCVForUnity.Rect region = Imgproc.boundingRect (selectedPointMat);
                        trackers.add (TrackerKCF.create (), rgbMat, new Rect2d(region.x, region.y, region.width, region.height));
                    }

                    selectedPointList.Clear ();
                    trackingColorList.Add (new Scalar (UnityEngine.Random.Range (0, 255), UnityEngine.Random.Range (0, 255), UnityEngine.Random.Range (0, 255)));
                }

                bool updated = trackers.update (rgbMat, objects);
                Debug.Log ("updated " + updated);
//                if (!updated && objects.rows () > 0) {
//                    OnResetTrackerButtonClick ();
//                }

                Rect2d[] objectsArray = objects.toArray ();
                for (int i = 0; i < objectsArray.Length; i++) {
                    Imgproc.rectangle (rgbMat, objectsArray [i].tl (), objectsArray [i].br (), trackingColorList [i], 2, 1, 0);
                }

                if (selectedPointList.Count != 1) {
                    //Imgproc.putText (rgbMat, "Please touch the screen, and select tracking regions.", new Point (5, rgbMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    if (fpsMonitor != null) {
                        fpsMonitor.consoleText = "Please touch the screen, and select tracking regions.";
                    }
                } else {
                    //Imgproc.putText (rgbMat, "Please select the end point of the new tracking region.", new Point (5, rgbMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    if (fpsMonitor != null) {
                        fpsMonitor.consoleText = "Please select the end point of the new tracking region.";
                    }
                }

                Utils.matToTexture2D (rgbMat, texture, colors);
            }
        }

        private void OnTouch (Mat img, Point touchPoint)
        {
            if (selectedPointList.Count < 2) {
                selectedPointList.Add (touchPoint);
                if (!(new OpenCVForUnity.Rect (0, 0, img.cols(), img.rows()).contains (selectedPointList [selectedPointList.Count - 1]))) {
                    selectedPointList.RemoveAt (selectedPointList.Count - 1);
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
            capture.release ();

            if (rgbMat != null)
                rgbMat.Dispose ();

            if (texture != null) {
                Texture2D.Destroy(texture);
                texture = null;
            }

            if (trackers != null)
                trackers.Dispose ();

            if (objects != null)
                objects.Dispose ();

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

            trackers = MultiTracker.create ();
            objects = new MatOfRect2d ();
            trackingColorList.Clear ();
            selectedPointList.Clear ();
        }
    }
}