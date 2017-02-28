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
    /// ArUco texture2D example.
    /// https://github.com/opencv/opencv_contrib/blob/master/modules/aruco/samples/detect_markers.cpp
    /// </summary>
    public class ArUcoTexture2DExample : MonoBehaviour
    {
        
        /// <summary>
        /// The image texture.
        /// </summary>
        public Texture2D imgTexture;
        
        /// <summary>
        /// The AR camera.
        /// </summary>
        public Camera ARCamera;
        
        /// <summary>
        /// The AR game object.
        /// </summary>
        public GameObject ARGameObject;
        
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
        
        // Use this for initialization
        void Start ()
        {
            Mat rgbMat = new Mat (imgTexture.height, imgTexture.width, CvType.CV_8UC3);
            
            Utils.texture2DToMat (imgTexture, rgbMat);
            Debug.Log ("imgMat dst ToString " + rgbMat.ToString ());
            
            
            gameObject.transform.localScale = new Vector3 (imgTexture.width, imgTexture.height, 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);
            
            float width = rgbMat.width ();
            float height = rgbMat.height ();
            
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
            Mat camMatrix = new Mat (3, 3, CvType.CV_64FC1);
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
            
            
            MatOfDouble distCoeffs = new MatOfDouble (0, 0, 0, 0);
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
            
            
            Mat ids = new Mat ();
            List<Mat> corners = new List<Mat> ();
            List<Mat> rejected = new List<Mat> ();
            Mat rvecs = new Mat ();
            Mat tvecs = new Mat ();
            Mat rotMat = new Mat (3, 3, CvType.CV_64FC1);
            
            DetectorParameters detectorParams = DetectorParameters.create ();
            Dictionary dictionary = Aruco.getPredefinedDictionary (dictionaryId);
            
            
            // detect markers and estimate pose
            Aruco.detectMarkers (rgbMat, dictionary, corners, ids, detectorParams, rejected);
            
            if (estimatePose && ids.total () > 0)
                Aruco.estimatePoseSingleMarkers (corners, markerLength, camMatrix, distCoeffs, rvecs, tvecs);
            
            
            // draw results
            if (ids.total () > 0) {
                Aruco.drawDetectedMarkers (rgbMat, corners, ids, new Scalar (255, 0, 0));
                
                if (estimatePose) {
                    for (int i = 0; i < ids.total(); i++) {
                        //                      Debug.Log ("ids.dump() " + ids.dump ());
                        
                        Aruco.drawAxis (rgbMat, camMatrix, distCoeffs, rvecs, tvecs, markerLength * 0.5f);
                        
                        //This example can display ARObject on only first detected marker.
                        if (i == 0) {
                            
                            // position
                            double[] tvec = tvecs.get (i, 0);
                            Vector4 pos =  new Vector4((float)tvec [0], (float)tvec [1], (float)tvec [2], 0); // from OpenCV
                            // right-handed coordinates system (OpenCV) to left-handed one (Unity)
                            ARGameObject.transform.localPosition = new Vector3(pos.x, -pos.y, pos.z);
                            
                            
                            // rotation
                            double[] rv = rvecs.get (i, 0);
                            Mat rvec = new Mat(3,1, CvType.CV_64FC1);
                            rvec.put(0,0, rv[0]);
                            rvec.put(1,0, rv[1]);
                            rvec.put(2,0, rv[2]);
                            
                            Calib3d.Rodrigues (rvec, rotMat);
                            
                            Vector3 forward = new Vector3((float)rotMat.get (0, 2) [0],(float)rotMat.get (1, 2) [0],(float)rotMat.get (2, 2) [0]); // from OpenCV
                            Vector3 up = new Vector3((float)rotMat.get (0, 1) [0],(float)rotMat.get (1, 1) [0],(float)rotMat.get (2, 1) [0]); // from OpenCV
                            // right-handed coordinates system (OpenCV) to left-handed one (Unity)
                            Quaternion rot = Quaternion.LookRotation(new Vector3(forward.x, -forward.y, forward.z), new Vector3(up.x, -up.y, up.z));
                            ARGameObject.transform.localRotation = rot;
                            
                            // Apply Z axis inverted matrix.
                            Matrix4x4 invertZM = Matrix4x4.TRS (Vector3.zero, Quaternion.identity, new Vector3 (1, 1, -1));
                            Matrix4x4 transformationM = ARGameObject.transform.localToWorldMatrix * invertZM;
                            
                            // Apply camera transform matrix.
                            transformationM = ARCamera.transform.localToWorldMatrix * transformationM ;
                            
                            ARUtils.SetTransformFromMatrix (ARGameObject.transform, ref transformationM);
                        }
                    }
                }
            }
            
            if (showRejected && rejected.Count > 0)
                Aruco.drawDetectedMarkers (rgbMat, rejected, new Mat (), new Scalar (0, 0, 255));
            
            
            Texture2D texture = new Texture2D (rgbMat.cols (), rgbMat.rows (), TextureFormat.RGBA32, false);
            
            Utils.matToTexture2D (rgbMat, texture);
            
            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
        }
        
        // Update is called once per frame
        void Update ()
        {
            
        }
        
        public void OnBackButton ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("OpenCVForUnityExample");
            #else
            Application.LoadLevel ("OpenCVForUnityExample");
            #endif
        }
    }
}