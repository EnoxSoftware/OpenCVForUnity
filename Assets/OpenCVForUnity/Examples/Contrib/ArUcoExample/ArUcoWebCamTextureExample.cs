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
    /// ArUco WebCamTexture example.
    /// https://github.com/opencv/opencv_contrib/blob/master/modules/aruco/samples/detect_markers.cpp
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class ArUcoWebCamTextureExample : MonoBehaviour
    {
        
        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;
        
        /// <summary>
        /// The web cam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;
        
        /// <summary>
        /// The rgb mat.
        /// </summary>
        Mat rgbMat;
        
        /// <summary>
        /// The dictionary identifier.
        /// </summary>
        public int dictionaryId = 10;
        
        /// <summary>
        /// The show rejected.
        /// </summary>
        public bool showRejected = true;
        
        /// <summary>
        /// The estimate pose.
        /// </summary>
        public bool estimatePose = true;
        
        /// <summary>
        /// The length of the marker.
        /// </summary>
        public float markerLength = 100;
        
        /// <summary>
        /// The AR game object.
        /// </summary>
        public GameObject ARGameObject;
        
        /// <summary>
        /// The AR camera.
        /// </summary>
        public Camera ARCamera;
        
        /// <summary>
        /// The cam matrix.
        /// </summary>
        Mat camMatrix;
        
        /// <summary>
        /// The dist coeffs.
        /// </summary>
        MatOfDouble distCoeffs;
        
        /// <summary>
        /// The invert Y.
        /// </summary>
        Matrix4x4 invertYM;
        
        /// <summary>
        /// The transformation m.
        /// </summary>
        Matrix4x4 transformationM;
        
        /// <summary>
        /// The invert Z.
        /// </summary>
        Matrix4x4 invertZM;
        
        /// <summary>
        /// The ar m.
        /// </summary>
        Matrix4x4 ARM;
        
        /// <summary>
        /// The should move AR camera.
        /// </summary>
        public bool shouldMoveARCamera;
        
        /// <summary>
        /// The identifiers.
        /// </summary>
        Mat ids ;
        
        /// <summary>
        /// The corners.
        /// </summary>
        List<Mat> corners;
        
        /// <summary>
        /// The rejected.
        /// </summary>
        List<Mat> rejected;
        
        /// <summary>
        /// The rvecs.
        /// </summary>
        Mat rvecs;
        
        /// <summary>
        /// The tvecs.
        /// </summary>
        Mat tvecs;
        
        /// <summary>
        /// The rot mat.
        /// </summary>
        Mat rotMat;
        
        /// <summary>
        /// The detector parameters.
        /// </summary>
        DetectorParameters detectorParams;
        
        /// <summary>
        /// The dictionary.
        /// </summary>
        Dictionary dictionary;
        
        
        // Use this for initialization
        void Start ()
        {
            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();
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
            
            float imageSizeScale = 1.0f;
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
                imageSizeScale = (float)Screen.height / (float)Screen.width;
            } else {
                Camera.main.orthographicSize = height / 2;
            }
            
            
            //set cameraparam
            int max_d = (int)Mathf.Max (width, height);
            double fx = max_d;
            double fy = max_d;
            double cx = width / 2.0f;
            double cy = height / 2.0f;
            camMatrix = new Mat (3, 3, CvType.CV_64FC1);
            camMatrix.put (0, 0, fx);
            camMatrix.put (0, 1, 0);
            camMatrix.put (0, 2, cx);
            camMatrix.put (1, 0, 0);
            camMatrix.put (1, 1, fy);
            camMatrix.put (1, 2, cy);
            camMatrix.put (2, 0, 0);
            camMatrix.put (2, 1, 0);
            camMatrix.put (2, 2, 1.0f);
            Debug.Log ("camMatrix " + camMatrix.dump ());
            
            
            distCoeffs = new MatOfDouble (0, 0, 0, 0);
            Debug.Log ("distCoeffs " + distCoeffs.dump ());
            
            
            //calibration camera
            Size imageSize = new Size (width * imageSizeScale, height * imageSizeScale);
            double apertureWidth = 0;
            double apertureHeight = 0;
            double[] fovx = new double[1];
            double[] fovy = new double[1];
            double[] focalLength = new double[1];
            Point principalPoint = new Point (0, 0);
            double[] aspectratio = new double[1];
            
            Calib3d.calibrationMatrixValues (camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);
            
            Debug.Log ("imageSize " + imageSize.ToString ());
            Debug.Log ("apertureWidth " + apertureWidth);
            Debug.Log ("apertureHeight " + apertureHeight);
            Debug.Log ("fovx " + fovx [0]);
            Debug.Log ("fovy " + fovy [0]);
            Debug.Log ("focalLength " + focalLength [0]);
            Debug.Log ("principalPoint " + principalPoint.ToString ());
            Debug.Log ("aspectratio " + aspectratio [0]);
            
            
            //To convert the difference of the FOV value of the OpenCV and Unity. 
            double fovXScale = (2.0 * Mathf.Atan ((float)(imageSize.width / (2.0 * fx)))) / (Mathf.Atan2 ((float)cx, (float)fx) + Mathf.Atan2 ((float)(imageSize.width - cx), (float)fx));
            double fovYScale = (2.0 * Mathf.Atan ((float)(imageSize.height / (2.0 * fy)))) / (Mathf.Atan2 ((float)cy, (float)fy) + Mathf.Atan2 ((float)(imageSize.height - cy), (float)fy));
            
            Debug.Log ("fovXScale " + fovXScale);
            Debug.Log ("fovYScale " + fovYScale);
            
            
            //Adjust Unity Camera FOV https://github.com/opencv/opencv/commit/8ed1945ccd52501f5ab22bdec6aa1f91f1e2cfd4
            if (widthScale < heightScale) {
                ARCamera.fieldOfView = (float)(fovx [0] * fovXScale);
            } else {
                ARCamera.fieldOfView = (float)(fovy [0] * fovYScale);
            }
            
            
            
            rgbMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC3);
            ids = new Mat ();
            corners = new List<Mat> ();
            rejected = new List<Mat> ();
            rvecs = new Mat ();
            tvecs = new Mat ();
            rotMat = new Mat (3, 3, CvType.CV_64FC1);
            
            
            transformationM = new Matrix4x4 ();
            
            invertYM = Matrix4x4.TRS (Vector3.zero, Quaternion.identity, new Vector3 (1, -1, 1));
            Debug.Log ("invertYM " + invertYM.ToString ());
            
            invertZM = Matrix4x4.TRS (Vector3.zero, Quaternion.identity, new Vector3 (1, 1, -1));
            Debug.Log ("invertZM " + invertZM.ToString ());
            
            detectorParams = DetectorParameters.create ();
            dictionary = Aruco.getPredefinedDictionary (Aruco.DICT_6X6_250);
            
            
            //if WebCamera is frontFaceing,flip Mat.
            if (webCamTextureToMatHelper.GetWebCamDevice ().isFrontFacing) {
                webCamTextureToMatHelper.flipHorizontal = true;
            }
        }
        
        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");
            
            if (rgbMat != null)
                rgbMat.Dispose ();
            if (ids != null)
                ids.Dispose ();
            foreach (var item in corners) {
                item.Dispose ();
            }
            corners.Clear ();
            foreach (var item in rejected) {
                item.Dispose ();
            }
            rejected.Clear ();
            if (rvecs != null)
                rvecs.Dispose ();
            if (tvecs != null)
                tvecs.Dispose ();
            if (rotMat != null)
                rotMat.Dispose ();
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
                
                Imgproc.cvtColor (rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
                
                // detect markers and estimate pose
                Aruco.detectMarkers (rgbMat, dictionary, corners, ids, detectorParams, rejected);

                if (estimatePose && ids.total () > 0)
                    Aruco.estimatePoseSingleMarkers (corners, markerLength, camMatrix, distCoeffs, rvecs, tvecs);
                
                
                // draw results
                if (ids.total () > 0) {
                    Aruco.drawDetectedMarkers (rgbMat, corners, ids, new Scalar (255, 0, 0));
                    
                    if (estimatePose) {
                        for (int i = 0; i < ids.total(); i++) {
                            Aruco.drawAxis (rgbMat, camMatrix, distCoeffs, rvecs, tvecs, markerLength * 0.5f);
                            
                            //This example can display ARObject on only first detected marker.
                            if (i == 0) {

                                // position
                                double[] tvec = tvecs.get (i, 0);
                                
                                // rotation
                                double[] rv = rvecs.get (i, 0);
                                Mat rvec = new Mat (3, 1, CvType.CV_64FC1);
                                rvec.put (0, 0, rv [0]);
                                rvec.put (1, 0, rv [1]);
                                rvec.put (2, 0, rv [2]);
                                Calib3d.Rodrigues (rvec, rotMat);

                                transformationM.SetRow (0, new Vector4 ((float)rotMat.get (0, 0) [0], (float)rotMat.get (0, 1) [0], (float)rotMat.get (0, 2) [0], (float)tvec [0]));
                                transformationM.SetRow (1, new Vector4 ((float)rotMat.get (1, 0) [0], (float)rotMat.get (1, 1) [0], (float)rotMat.get (1, 2) [0], (float)tvec [1]));
                                transformationM.SetRow (2, new Vector4 ((float)rotMat.get (2, 0) [0], (float)rotMat.get (2, 1) [0], (float)rotMat.get (2, 2) [0], (float)tvec [2]));
                                transformationM.SetRow (3, new Vector4 (0, 0, 0, 1));
                                
                                // right-handed coordinates system (OpenCV) to left-handed one (Unity)
                                ARM = invertYM * transformationM;
                                
                                // Apply Z axis inverted matrix.
                                ARM = ARM * invertZM;
                                
                                if (shouldMoveARCamera) {
                                    
                                    // Apply ARObject transform matrix.
                                    ARM = ARGameObject.transform.localToWorldMatrix * ARM.inverse;

                                    ARUtils.SetTransformFromMatrix (ARCamera.transform, ref ARM);
                                    
                                } else {
                                    
                                    // Apply camera transform matrix.
                                    ARM = ARCamera.transform.localToWorldMatrix * ARM;

                                    ARUtils.SetTransformFromMatrix (ARGameObject.transform, ref ARM);
                                }
                            }
                        }
                    }
                }
                
                if (showRejected && rejected.Count > 0)
                    Aruco.drawDetectedMarkers (rgbMat, rejected, new Mat (), new Scalar (0, 0, 255));
                
                
                Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                
                Utils.matToTexture2D (rgbMat, texture, webCamTextureToMatHelper.GetBufferColors ());
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