using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Xml.Serialization;
using System.IO;
using System.Linq;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// ArUco Camera Calibration Example
    /// An example of camera calibration using the aruco module.
    /// Referring to https://github.com/opencv/opencv_contrib/blob/master/modules/aruco/samples/calibrate_camera.cpp.
    /// https://github.com/opencv/opencv/blob/master/samples/cpp/tutorial_code/calib3d/camera_calibration/camera_calibration.cpp
    /// https://docs.opencv.org/3.2.0/da/d13/tutorial_aruco_calibration.html
    /// https://docs.opencv.org/3.4.0/d7/d21/tutorial_interactive_calibration.html
    /// </summary>
    [RequireComponent (typeof(WebCamTextureToMatHelper))]
    public class ArUcoCameraCalibrationExample : MonoBehaviour
    {
        /// <summary>
        /// The marker type.
        /// </summary>
        public MarkerType markerType = MarkerType.ChArUcoBoard;

        /// <summary>
        /// The marker type dropdown.
        /// </summary>
        public Dropdown markerTypeDropdown;

        /// <summary>
        /// The dictionary identifier.
        /// </summary>
        public ArUcoDictionary dictionaryId = ArUcoDictionary.DICT_6X6_250;

        /// <summary>
        /// The dictionary id dropdown.
        /// </summary>
        public Dropdown dictionaryIdDropdown;

        /// <summary>
        /// Number of squares in X direction.
        /// </summary>
        public NumberOfSquaresX squaresX = NumberOfSquaresX.X_5;

        /// <summary>
        /// The squares X dropdown.
        /// </summary>
        public Dropdown squaresXDropdown;

        /// <summary>
        /// Number of squares in X direction.
        /// </summary>
        public NumberOfSquaresY squaresY = NumberOfSquaresY.Y_7;

        /// <summary>
        /// The squares X dropdown.
        /// </summary>
        public Dropdown squaresYDropdown;

        /// <summary>
        /// The save path input field.
        /// </summary>
        public InputField savePathInputField;

        /// <summary>
        /// Determines if refine marker detection. (only valid for ArUco boards)
        /// </summary>
        public bool refineMarkerDetection = true;


        [Header ("Extra Option")]

        /// <summary>
        /// Determines if calibrates camera using the list of calibration images.
        /// </summary>
        [TooltipAttribute ("Determines if calibrates camera using the list of calibration images.")]
        public bool isImagesInputMode = false;

        /// <summary>
        /// The calibration images directory path.
        /// Set a relative directory path from the starting point of the "StreamingAssets" folder.  e.g. "calibration_images/".
        /// </summary>
        [TooltipAttribute ("Set a relative directory path from the starting point of the \"StreamingAssets\" folder.  e.g. \"calibration_images\"")]
        public string calibrationImagesDirectory = "calibration_images";

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;
        
        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMat;

        /// <summary>
        /// The bgr mat.
        /// </summary>
        Mat bgrMat;

        /// <summary>
        /// The rgba mat.
        /// </summary>
        Mat rgbaMat;
        
        /// <summary>
        /// The cameraparam matrix.
        /// </summary>
        Mat camMatrix;
        
        /// <summary>
        /// The distortion coeffs.
        /// </summary>
        MatOfDouble distCoeffs;

        /// <summary>
        /// The identifiers.
        /// </summary>
        Mat ids;
        
        /// <summary>
        /// The corners.
        /// </summary>
        List<Mat> corners;
        
        /// <summary>
        /// The rejected corners.
        /// </summary>
        List<Mat> rejectedCorners;
        
        /// <summary>
        /// The rvecs.
        /// </summary>
        List<Mat> rvecs;
        
        /// <summary>
        /// The tvecs.
        /// </summary>
        List<Mat> tvecs;
        
        /// <summary>
        /// The detector parameters.
        /// </summary>
        DetectorParameters detectorParams;
        
        /// <summary>
        /// The dictionary.
        /// </summary>
        Dictionary dictionary;

        /// <summary>
        /// The recovered identifiers.
        /// </summary>
        Mat recoveredIdxs;

        const int calibrationFlags = 0;
// Calib3d.CALIB_FIX_K3 | Calib3d.CALIB_FIX_K4 | Calib3d.CALIB_FIX_K5
        double repErr = 0;
        bool shouldCaptureFrame = false;

        // for ChArUcoBoard.
        // chessboard square side length (normally in meters)
        const float chArUcoBoradSquareLength = 0.04f;
        // marker side length (same unit than squareLength)
        const float chArUcoBoradMarkerLength = 0.02f;
        const int charucoMinMarkers = 2;
        Mat charucoCorners;
        Mat charucoIds;
        CharucoBoard charucoBoard;
        List<List<Mat>> allCorners;
        List<Mat> allIds;
        List<Mat> allImgs;

        // for OthearMarkers.
        // square size in some user-defined units (1 by default)
        const float squareSize = 1f;
        List<Mat> imagePoints;
        bool isInitialized = false;
        bool isCalibrating = false;

        #if UNITY_ANDROID && !UNITY_EDITOR
        float rearCameraRequestedFPS;
        #endif

        // Use this for initialization
        IEnumerator Start ()
        {
            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();

            // fix the screen orientation.
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            // wait for the screen orientation to change.
            yield return null;

            markerTypeDropdown.value = (int)markerType;
            dictionaryIdDropdown.value = (int)dictionaryId;
            squaresXDropdown.value = (int)squaresX - 1;
            squaresYDropdown.value = (int)squaresY - 1;
            dictionaryIdDropdown.interactable = (markerType == MarkerType.ChArUcoBoard);

            #if UNITY_WEBGL && !UNITY_EDITOR
            isImagesInputMode = false;
            #endif
            if (isImagesInputMode) {
                isImagesInputMode = InitializeImagesInputMode ();
            }

            if (!isImagesInputMode) {                
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
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInitialized");
            
            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();
            
            InitializeCalibraton (webCamTextureMat);

            // if WebCamera is frontFaceing, flip Mat.
            if (webCamTextureToMatHelper.GetWebCamDevice ().isFrontFacing) {
                webCamTextureToMatHelper.flipHorizontal = true;
            }
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

            DisposeCalibraton ();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred (WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log ("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }
        
        // Update is called once per frame
        void Update ()
        {
            if (isImagesInputMode)
                return;

            if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

                Imgproc.cvtColor (rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

                if (shouldCaptureFrame) {
                    shouldCaptureFrame = false;
                    Mat frameMat = grayMat.clone ();
                    double e = CaptureFrame (frameMat);
                    if (e > 0)
                        repErr = e;
                }

                DrawFrame (grayMat, bgrMat);
                Imgproc.cvtColor (bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                Utils.fastMatToTexture2D (rgbaMat, texture);
            }
        }

        private void InitializeCalibraton (Mat frameMat)
        {
            texture = new Texture2D (frameMat.cols (), frameMat.rows (), TextureFormat.RGBA32, false);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3 (frameMat.cols (), frameMat.rows (), 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


            float width = frameMat.width ();
            float height = frameMat.height ();

            float imageSizeScale = 1.0f;
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
                imageSizeScale = (float)Screen.height / (float)Screen.width;
            } else {
                Camera.main.orthographicSize = height / 2;
            }


            // set cameraparam.
            camMatrix = CreateCameraMatrix (width, height);
            Debug.Log ("camMatrix " + camMatrix.dump ());            

            distCoeffs = new MatOfDouble (0, 0, 0, 0, 0);
            Debug.Log ("distCoeffs " + distCoeffs.dump ());            

            // calibration camera.
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


            grayMat = new Mat (frameMat.rows (), frameMat.cols (), CvType.CV_8UC1);
            bgrMat = new Mat (frameMat.rows (), frameMat.cols (), CvType.CV_8UC3);
            rgbaMat = new Mat (frameMat.rows (), frameMat.cols (), CvType.CV_8UC4);
            ids = new Mat ();
            corners = new List<Mat> ();
            rejectedCorners = new List<Mat> ();
            rvecs = new List<Mat> ();
            tvecs = new List<Mat> ();

            detectorParams = DetectorParameters.create ();
            detectorParams.set_cornerRefinementMethod (1);// do cornerSubPix() of OpenCV.
            dictionary = Aruco.getPredefinedDictionary ((int)dictionaryId);

            recoveredIdxs = new Mat ();

            charucoCorners = new Mat ();
            charucoIds = new Mat ();
            charucoBoard = CharucoBoard.create ((int)squaresX, (int)squaresY, chArUcoBoradSquareLength, chArUcoBoradMarkerLength, dictionary);


            allCorners = new List<List<Mat>> ();
            allIds = new List<Mat> ();
            allImgs = new List<Mat> ();

            imagePoints = new List<Mat> ();

            isInitialized = true;
        }

        private void DisposeCalibraton ()
        {
            ResetCalibration ();

            if (grayMat != null)
                grayMat.Dispose ();
            if (bgrMat != null)
                bgrMat.Dispose ();
            if (rgbaMat != null)
                rgbaMat.Dispose ();
            
            if (texture != null) {
                Texture2D.Destroy(texture);
                texture = null;
            }

            if (ids != null)
                ids.Dispose ();
            foreach (var item in corners) {
                item.Dispose ();
            }
            corners.Clear ();
            foreach (var item in rejectedCorners) {
                item.Dispose ();
            }
            rejectedCorners.Clear ();
            foreach (var item in rvecs) {
                item.Dispose ();
            }
            rvecs.Clear ();
            foreach (var item in tvecs) {
                item.Dispose ();
            }
            tvecs.Clear ();

            if (recoveredIdxs != null)
                recoveredIdxs.Dispose (); 

            if (charucoCorners != null)
                charucoCorners.Dispose ();
            if (charucoIds != null)
                charucoIds.Dispose ();
            if (charucoBoard != null)
                charucoBoard.Dispose ();

            isInitialized = false;
        }

        private void DrawFrame (Mat grayMat, Mat bgrMat)
        {
            Imgproc.cvtColor (grayMat, bgrMat, Imgproc.COLOR_GRAY2BGR);

            switch (markerType) {
            default:
            case MarkerType.ChArUcoBoard:
                // detect markers.
                Aruco.detectMarkers (grayMat, dictionary, corners, ids, detectorParams, rejectedCorners, camMatrix, distCoeffs);

                // refine marker detection.
                if (refineMarkerDetection) {
                    Aruco.refineDetectedMarkers (grayMat, charucoBoard, corners, ids, rejectedCorners, camMatrix, distCoeffs, 10f, 3f, true, recoveredIdxs, detectorParams);
                }

                // if at least one marker detected
                if (ids.total () > 0) {
                    Aruco.interpolateCornersCharuco (corners, ids, grayMat, charucoBoard, charucoCorners, charucoIds, camMatrix, distCoeffs, charucoMinMarkers);

                    // draw markers.
                    Aruco.drawDetectedMarkers (bgrMat, corners, ids, new Scalar (0, 255, 0, 255));
                    // if at least one charuco corner detected
                    if (charucoIds.total () > 0) {
                        Aruco.drawDetectedCornersCharuco (bgrMat, charucoCorners, charucoIds, new Scalar (0, 0, 255, 255));
                    }
                }
                break;
            case MarkerType.ChessBoard:
            case MarkerType.CirclesGlid:
            case MarkerType.AsymmetricCirclesGlid:
                // detect markers.
                MatOfPoint2f points = new MatOfPoint2f ();
                bool found = false;

                switch (markerType) {
                default:
                case MarkerType.ChessBoard:
                    found = Calib3d.findChessboardCorners (grayMat, new Size ((int)squaresX, (int)squaresY), points, Calib3d.CALIB_CB_ADAPTIVE_THRESH | Calib3d.CALIB_CB_FAST_CHECK | Calib3d.CALIB_CB_NORMALIZE_IMAGE);
                    break;
                case MarkerType.CirclesGlid:
                    found = Calib3d.findCirclesGrid (grayMat, new Size ((int)squaresX, (int)squaresY), points, Calib3d.CALIB_CB_SYMMETRIC_GRID);
                    break;
                case MarkerType.AsymmetricCirclesGlid:
                    found = Calib3d.findCirclesGrid (grayMat, new Size ((int)squaresX, (int)squaresY), points, Calib3d.CALIB_CB_ASYMMETRIC_GRID);
                    break;
                }

                if (found) {
                    if (markerType == MarkerType.ChessBoard)
                        Imgproc.cornerSubPix (grayMat, points, new Size (5, 5), new Size (-1, -1), new TermCriteria (TermCriteria.EPS + TermCriteria.COUNT, 30, 0.1));

                    // draw markers.
                    Calib3d.drawChessboardCorners (bgrMat, new Size ((int)squaresX, (int)squaresY), points, found);
                }
                break;
            }

            double[] camMatrixArr = new double[(int)camMatrix.total ()];
            camMatrix.get (0, 0, camMatrixArr);
            double[] distCoeffsArr = new double[(int)distCoeffs.total ()];
            distCoeffs.get (0, 0, distCoeffsArr);

            int ff = Core.FONT_HERSHEY_SIMPLEX;
            double fs = 0.4;
            Scalar c = new Scalar (255, 255, 255, 255);
            int t = 0;
            int lt = Imgproc.LINE_AA;
            bool blo = false;
            int frameCount = (markerType == MarkerType.ChArUcoBoard) ? allCorners.Count : imagePoints.Count;
            Imgproc.putText (bgrMat, frameCount + " FRAME CAPTURED", new Point (bgrMat.cols () - 300, 20), ff, fs, c, t, lt, blo);
            Imgproc.putText (bgrMat, "IMAGE_WIDTH: " + bgrMat.width (), new Point (bgrMat.cols () - 300, 40), ff, fs, c, t, lt, blo);
            Imgproc.putText (bgrMat, "IMAGE_HEIGHT: " + bgrMat.height (), new Point (bgrMat.cols () - 300, 60), ff, fs, c, t, lt, blo);
            Imgproc.putText (bgrMat, "CALIBRATION_FLAGS: " + calibrationFlags, new Point (bgrMat.cols () - 300, 80), ff, fs, c, t, lt, blo);

            Imgproc.putText (bgrMat, "CAMERA_MATRIX: ", new Point (bgrMat.cols () - 300, 100), ff, fs, c, t, lt, blo);
            for (int i = 0; i < camMatrixArr.Length; i = i + 3) {
                Imgproc.putText (bgrMat, "   " + camMatrixArr [i] + ", " + camMatrixArr [i + 1] + ", " + camMatrixArr [i + 2] + ",", new Point (bgrMat.cols () - 300, 120 + 20 * i / 3), ff, fs, c, t, lt, blo);
            }
            Imgproc.putText (bgrMat, "DISTORTION_COEFFICIENTS: ", new Point (bgrMat.cols () - 300, 180), ff, fs, c, t, lt, blo);
            for (int i = 0; i < distCoeffsArr.Length; ++i) {
                Imgproc.putText (bgrMat, "   " + distCoeffsArr [i] + ",", new Point (bgrMat.cols () - 300, 200 + 20 * i), ff, fs, c, t, lt, blo);
            }
            Imgproc.putText (bgrMat, "AVG_REPROJECTION_ERROR: " + repErr, new Point (bgrMat.cols () - 300, 300), ff, fs, c, t, lt, blo);

            if (frameCount == 0)
                Imgproc.putText (bgrMat, "Please press the capture button to start!", new Point (5, bgrMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
        }

        private double CaptureFrame (Mat frameMat)
        {
            double repErr = -1;

            switch (markerType) {
            default:
            case MarkerType.ChArUcoBoard:
                List<Mat> corners = new List<Mat> ();
                Mat ids = new Mat ();
                Aruco.detectMarkers (frameMat, dictionary, corners, ids, detectorParams, rejectedCorners, camMatrix, distCoeffs);

                if (refineMarkerDetection) {
                    Aruco.refineDetectedMarkers (frameMat, charucoBoard, corners, ids, rejectedCorners, camMatrix, distCoeffs, 10f, 3f, true, recoveredIdxs, detectorParams);
                }

                if (ids.total () > 0) {
                    Debug.Log ("Frame captured.");

                    allCorners.Add (corners);
                    allIds.Add (ids);
                    allImgs.Add (frameMat);
                } else {

                    Debug.Log ("Invalid frame.");

                    frameMat.Dispose ();
                    if (ids != null)
                        ids.Dispose ();
                    foreach (var item in corners) {
                        item.Dispose ();
                    }
                    corners.Clear ();

                    return -1;
                }

                // calibrate camera using aruco markers
                //double arucoRepErr = CalibrateCameraAruco (allCorners, allIds, charucoBoard, frameMat.size(), camMatrix, distCoeffs, rvecs, tvecs, calibrationFlags);
                //Debug.Log ("arucoRepErr: " + arucoRepErr);

                // calibrate camera using charuco
                repErr = CalibrateCameraCharuco (allCorners, allIds, charucoBoard, frameMat.size (), camMatrix, distCoeffs, rvecs, tvecs, calibrationFlags, calibrationFlags);

                break;
            case MarkerType.ChessBoard:
            case MarkerType.CirclesGlid:
            case MarkerType.AsymmetricCirclesGlid:
                
                MatOfPoint2f points = new MatOfPoint2f ();
                Size patternSize = new Size ((int)squaresX, (int)squaresY);

                bool found = false;
                switch (markerType) {
                default:
                case MarkerType.ChessBoard:
                    found = Calib3d.findChessboardCorners (frameMat, patternSize, points, Calib3d.CALIB_CB_ADAPTIVE_THRESH | Calib3d.CALIB_CB_FAST_CHECK | Calib3d.CALIB_CB_NORMALIZE_IMAGE);
                    break;
                case MarkerType.CirclesGlid:
                    found = Calib3d.findCirclesGrid (frameMat, patternSize, points, Calib3d.CALIB_CB_SYMMETRIC_GRID);
                    break;
                case MarkerType.AsymmetricCirclesGlid:
                    found = Calib3d.findCirclesGrid (frameMat, patternSize, points, Calib3d.CALIB_CB_ASYMMETRIC_GRID);
                    break;
                }

                if (found) {
                    Debug.Log ("Frame captured.");
                    if (markerType == MarkerType.ChessBoard)
                        Imgproc.cornerSubPix (frameMat, points, new Size (5, 5), new Size (-1, -1), new TermCriteria (TermCriteria.EPS + TermCriteria.COUNT, 30, 0.1));
                    
                    imagePoints.Add (points);
                    allImgs.Add (frameMat);
                } else {
                    Debug.Log ("Invalid frame.");
                    frameMat.Dispose ();
                    if (points != null)
                        points.Dispose ();
                    return -1;
                }
                    
                if (imagePoints.Count < 1) {
                    Debug.Log ("Not enough points for calibration.");
                    repErr = -1;
                } else {

                    MatOfPoint3f objectPoint = new MatOfPoint3f (new Mat (imagePoints [0].rows (), 1, CvType.CV_32FC3));
                    CalcChessboardCorners (patternSize, squareSize, objectPoint, markerType);

                    List<Mat> objectPoints = new List<Mat> ();
                    for (int i = 0; i < imagePoints.Count; ++i) {
                        objectPoints.Add (objectPoint);
                    }

                    repErr = Calib3d.calibrateCamera (objectPoints, imagePoints, frameMat.size (), camMatrix, distCoeffs, rvecs, tvecs, calibrationFlags);
                    objectPoint.Dispose ();
                }

                break;
            }

            Debug.Log ("repErr: " + repErr);
            Debug.Log ("camMatrix: " + camMatrix.dump ());
            Debug.Log ("distCoeffs: " + distCoeffs.dump ());
                
            return repErr;
        }

        private double CalibrateCameraAruco (List<List<Mat>> allCorners, List<Mat> allIds, CharucoBoard board, Size imageSize, Mat cameraMatrix, Mat distCoeffs, List<Mat> rvecs = null, List<Mat> tvecs = null, int calibrationFlags = 0)
        {
            // prepare data for calibration
            int nFrames = allCorners.Count;
            int allLen = 0;
            int[] markerCounterPerFrameArr = new int[allCorners.Count];
            for (int i = 0; i < nFrames; ++i) {
                markerCounterPerFrameArr [i] = allCorners [i].Count;
                allLen += allCorners [i].Count;
            }

            int[] allIdsConcatenatedArr = new int[allLen];
            int index = 0;
            for (int j = 0; j < allIds.Count; ++j) {
                int[] idsArr = new int[(int)allIds [j].total ()];
                allIds [j].get (0, 0, idsArr);

                for (int k = 0; k < idsArr.Length; ++k) {
                    allIdsConcatenatedArr [index + k] = (int)idsArr [k];
                }
                index += idsArr.Length;
            }

            using (Mat allIdsConcatenated = new Mat (1, allLen, CvType.CV_32SC1))
            using (Mat markerCounterPerFrame = new Mat (1, nFrames, CvType.CV_32SC1)) {

                List<Mat> allCornersConcatenated = new List<Mat> ();
                foreach (var c in allCorners) {
                    foreach (var m in c) {
                        allCornersConcatenated.Add (m);
                    }
                }

                allIdsConcatenated.put (0, 0, allIdsConcatenatedArr);
                markerCounterPerFrame.put (0, 0, markerCounterPerFrameArr);

                if (rvecs == null)
                    rvecs = new List<Mat> ();
                if (tvecs == null)
                    tvecs = new List<Mat> ();

                return Aruco.calibrateCameraAruco (allCornersConcatenated, allIdsConcatenated, markerCounterPerFrame, board, imageSize, cameraMatrix, distCoeffs, rvecs, tvecs, calibrationFlags);
            }
        }

        private double CalibrateCameraCharuco (List<List<Mat>> allCorners, List<Mat> allIds, CharucoBoard board, Size imageSize, Mat cameraMatrix, Mat distCoeffs, List<Mat> rvecs = null, List<Mat> tvecs = null, int calibrationFlags = 0, int minMarkers = 2)
        {
            // prepare data for charuco calibration
            int nFrames = allCorners.Count;
            List<Mat> allCharucoCorners = new List<Mat> ();
            List<Mat> allCharucoIds = new List<Mat> ();
            List<Mat> filteredImages = new List<Mat> ();

            for (int i = 0; i < nFrames; ++i) {
                // interpolate using camera parameters
                Mat currentCharucoCorners = new Mat ();
                Mat currentCharucoIds = new Mat ();

                Aruco.interpolateCornersCharuco (allCorners [i], allIds [i], allImgs [i], board, currentCharucoCorners, currentCharucoIds, cameraMatrix, distCoeffs, minMarkers);

                if (charucoIds.total () > 0) {
                    allCharucoCorners.Add (currentCharucoCorners);
                    allCharucoIds.Add (currentCharucoIds);
                    filteredImages.Add (allImgs [i]);
                } else {
                    currentCharucoCorners.Dispose ();
                    currentCharucoIds.Dispose ();
                }
            }

            if (allCharucoCorners.Count < 1) {
                Debug.Log ("Not enough corners for calibration.");
                return -1;
            }

            if (rvecs == null)
                rvecs = new List<Mat> ();
            if (tvecs == null)
                tvecs = new List<Mat> ();

            return Aruco.calibrateCameraCharuco (allCharucoCorners, allCharucoIds, board, imageSize, cameraMatrix, distCoeffs, rvecs, tvecs, calibrationFlags);
        }

        private void ResetCalibration ()
        {
            foreach (var corners in allCorners) {
                foreach (var item in corners) {
                    item.Dispose ();
                }
            }
            allCorners.Clear ();

            foreach (var item in allIds) {
                item.Dispose ();
            }
            allIds.Clear ();

            foreach (var item in allImgs) {
                item.Dispose ();
            }
            allImgs.Clear ();

            repErr = 0;
            camMatrix = CreateCameraMatrix (bgrMat.width (), bgrMat.height ());
            distCoeffs = new MatOfDouble (0, 0, 0, 0, 0);

            foreach (var item in imagePoints) {
                item.Dispose ();
            }
            imagePoints.Clear ();
        }

        private Mat CreateCameraMatrix (float width, float height)
        {
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

            return camMatrix;
        }

        private void CalcChessboardCorners (Size patternSize, float squareSize, MatOfPoint3f corners, MarkerType markerType)
        {
            if ((int)(patternSize.width * patternSize.height) != corners.rows ()) {
                Debug.Log ("Invalid corners size.");
                corners.create ((int)(patternSize.width * patternSize.height), 1, CvType.CV_32FC3);
            }

            const int cn = 3;
            float[] cornersArr = new float[corners.rows () * cn];
            int width = (int)patternSize.width;
            int height = (int)patternSize.height;

            switch (markerType) {
            default:
            case MarkerType.ChessBoard:
            case MarkerType.CirclesGlid:
                for (int i = 0; i < height; ++i) {
                    for (int j = 0; j < width; ++j) {
                        cornersArr [(i * width * cn) + (j * cn)] = j * squareSize;
                        cornersArr [(i * width * cn) + (j * cn) + 1] = i * squareSize;
                        cornersArr [(i * width * cn) + (j * cn) + 2] = 0;
                    }
                }
                corners.put (0, 0, cornersArr);

                break;
            case MarkerType.AsymmetricCirclesGlid:
                for (int i = 0; i < height; ++i) {
                    for (int j = 0; j < width; ++j) {
                        cornersArr [(i * width * cn) + (j * cn)] = (2 * j + i % 2) * squareSize;
                        cornersArr [(i * width * cn) + (j * cn) + 1] = i * squareSize;
                        cornersArr [(i * width * cn) + (j * cn) + 2] = 0;
                    }
                }
                corners.put (0, 0, cornersArr);

                break;
            }
        }

        private bool InitializeImagesInputMode ()
        {
            if (isInitialized)
                DisposeCalibraton ();

            if (String.IsNullOrEmpty (calibrationImagesDirectory)) {
                Debug.LogWarning ("When using the images input mode, please set a calibration images directory path.");
                return false;
            }

            string dirPath = Path.Combine (Application.streamingAssetsPath, calibrationImagesDirectory);
            if (!Directory.Exists (dirPath)) {
                Debug.LogWarning ("The directory does not exist.");
                return false;
            }
            string[] imageFiles = GetImageFilesInDirectory (dirPath);
            if (imageFiles.Length < 1) {
                Debug.LogWarning ("The image file does not exist.");
                return false;
            }                
                
            Uri rootPath = new Uri (Application.streamingAssetsPath + System.IO.Path.AltDirectorySeparatorChar);
            Uri fullPath = new Uri (imageFiles [0]);
            string relativePath = rootPath.MakeRelativeUri (fullPath).ToString ();

            using (Mat gray = Imgcodecs.imread (Utils.getFilePath (relativePath), Imgcodecs.IMREAD_GRAYSCALE)) {

                if (gray.total () == 0) {
                    Debug.LogWarning ("Invalid image file.");
                    return false;
                }

                using (Mat bgr = new Mat (gray.size (), CvType.CV_8UC3))
                using (Mat bgra = new Mat (gray.size (), CvType.CV_8UC4)) {
                    InitializeCalibraton (gray);

                    DrawFrame (gray, bgr);
                    Imgproc.cvtColor (bgr, bgra, Imgproc.COLOR_BGR2RGBA);
                    Utils.fastMatToTexture2D (bgra, texture);
                }
            }
            return true;
        }

        private IEnumerator CalibrateCameraUsingImages ()
        {
            string dirPath = Path.Combine (Application.streamingAssetsPath, calibrationImagesDirectory);
            string[] imageFiles = GetImageFilesInDirectory (dirPath);
            if (imageFiles.Length < 1)
                yield break;

            isCalibrating = true;
            markerTypeDropdown.interactable = dictionaryIdDropdown.interactable = squaresXDropdown.interactable = squaresYDropdown.interactable = false;

            Uri rootPath = new Uri (Application.streamingAssetsPath + System.IO.Path.AltDirectorySeparatorChar);

            foreach (var path in imageFiles) {

                Uri fullPath = new Uri (path);
                string relativePath = rootPath.MakeRelativeUri (fullPath).ToString ();

                using (Mat gray = Imgcodecs.imread (Utils.getFilePath (relativePath), Imgcodecs.IMREAD_GRAYSCALE)) {

                    if (gray.width () != bgrMat.width () || gray.height () != bgrMat.height ())
                        continue;

                    Mat frameMat = gray.clone ();
                    double e = CaptureFrame (frameMat);
                    if (e > 0)
                        repErr = e;

                    DrawFrame (gray, bgrMat);
                    Imgproc.cvtColor (bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                    Utils.matToTexture2D (rgbaMat, texture);
                }
                yield return new WaitForSeconds (0.5f);
            }

            isCalibrating = false;
            markerTypeDropdown.interactable = dictionaryIdDropdown.interactable = squaresXDropdown.interactable = squaresYDropdown.interactable = true;
        }

        private string[] GetImageFilesInDirectory (string dirPath)
        {
            if (Directory.Exists (dirPath)) {
                string[] files = Directory.GetFiles (dirPath, "*.jpg");
                files = files.Concat (Directory.GetFiles (dirPath, "*.jpeg")).ToArray ();
                files = files.Concat (Directory.GetFiles (dirPath, "*.png")).ToArray ();
                files = files.Concat (Directory.GetFiles (dirPath, "*.tiff")).ToArray ();
                files = files.Concat (Directory.GetFiles (dirPath, "*.tif")).ToArray ();
                return files;
            }
            return new string[0];
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            if (isImagesInputMode) {
                DisposeCalibraton ();
            } else {
                webCamTextureToMatHelper.Dispose ();
            }
                
            Screen.orientation = ScreenOrientation.AutoRotation;
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
            if (isImagesInputMode)
                return;

            webCamTextureToMatHelper.Play ();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick ()
        {
            if (isImagesInputMode)
                return;

            webCamTextureToMatHelper.Pause ();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick ()
        {
            if (isImagesInputMode)
                return;

            webCamTextureToMatHelper.Stop ();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick ()
        {
            if (isImagesInputMode)
                return;

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

        /// <summary>
        /// Raises the marker type dropdown value changed event.
        /// </summary>
        public void OnMarkerTypeDropdownValueChanged (int result)
        {
            if ((int)markerType != result) {
                markerType = (MarkerType)result;

                dictionaryIdDropdown.interactable = (markerType == MarkerType.ChArUcoBoard);

                if (isImagesInputMode) {
                    InitializeImagesInputMode ();
                } else {
                    if (webCamTextureToMatHelper.IsInitialized ())
                        webCamTextureToMatHelper.Initialize ();
                }
            }
        }

        /// <summary>
        /// Raises the dictionary id dropdown value changed event.
        /// </summary>
        public void OnDictionaryIdDropdownValueChanged (int result)
        {
            if ((int)dictionaryId != result) {
                dictionaryId = (ArUcoDictionary)result;
                dictionary = Aruco.getPredefinedDictionary ((int)dictionaryId);

                if (isImagesInputMode) {
                    InitializeImagesInputMode ();
                } else {
                    if (webCamTextureToMatHelper.IsInitialized ())
                        webCamTextureToMatHelper.Initialize ();
                }
            }
        }

        /// <summary>
        /// Raises the squares X dropdown value changed event.
        /// </summary>
        public void OnSquaresXDropdownValueChanged (int result)
        {
            if ((int)squaresX != result + 1) {
                squaresX = (NumberOfSquaresX)(result + 1);

                if (isImagesInputMode) {
                    InitializeImagesInputMode ();
                } else {
                    if (webCamTextureToMatHelper.IsInitialized ())
                        webCamTextureToMatHelper.Initialize ();
                }
            }
        }

        /// <summary>
        /// Raises the squares Y dropdown value changed event.
        /// </summary>
        public void OnSquaresYDropdownValueChanged (int result)
        {
            if ((int)squaresY != result + 1) {
                squaresY = (NumberOfSquaresY)(result + 1);

                if (isImagesInputMode) {
                    InitializeImagesInputMode ();
                } else {
                    if (webCamTextureToMatHelper.IsInitialized ())
                        webCamTextureToMatHelper.Initialize ();
                }
            }
        }

        /// <summary>
        /// Raises the capture button click event.
        /// </summary>
        public void OnCaptureButtonClick ()
        {
            if (isImagesInputMode) {
                if (!isCalibrating)
                    InitializeImagesInputMode ();
                StartCoroutine ("CalibrateCameraUsingImages");
            } else {
                shouldCaptureFrame = true;
            }
        }

        /// <summary>
        /// Raises the reset button click event.
        /// </summary>
        public void OnResetButtonClick ()
        {
            if (isImagesInputMode) {
                if (!isCalibrating)
                    InitializeImagesInputMode ();
            } else {
                ResetCalibration ();
            }
        }

        /// <summary>
        /// Raises the save button click event.
        /// </summary>
        public void OnSaveButtonClick ()
        {
            string saveDirectoryPath = Path.Combine (Application.persistentDataPath, "ArUcoCameraCalibrationExample");

            if (!Directory.Exists (saveDirectoryPath)) {
                Directory.CreateDirectory (saveDirectoryPath);
            }

            string calibratonDirectoryName = "camera_parameters" + bgrMat.width () + "x" + bgrMat.height ();
            string saveCalibratonFileDirectoryPath = Path.Combine (saveDirectoryPath, calibratonDirectoryName);

            // Clean up old files.
            if (Directory.Exists (saveCalibratonFileDirectoryPath)) {
                DirectoryInfo directoryInfo = new DirectoryInfo (saveCalibratonFileDirectoryPath);
                foreach (FileInfo fileInfo in directoryInfo.GetFiles()) {
                    if ((fileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) {
                        fileInfo.Attributes = FileAttributes.Normal;
                    }
                }
                if ((directoryInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) {
                    directoryInfo.Attributes = FileAttributes.Directory;
                }
                directoryInfo.Delete (true);
            }
            Directory.CreateDirectory (saveCalibratonFileDirectoryPath);

            // save the calibraton file.
            string savePath = Path.Combine (saveCalibratonFileDirectoryPath, calibratonDirectoryName + ".xml");
            int frameCount = (markerType == (int)MarkerType.ChArUcoBoard) ? allCorners.Count : imagePoints.Count;
            CameraParameters param = new CameraParameters (frameCount, bgrMat.width (), bgrMat.height (), calibrationFlags, camMatrix, distCoeffs, repErr);
            XmlSerializer serializer = new XmlSerializer (typeof(CameraParameters));
            using (var stream = new FileStream (savePath, FileMode.Create)) {
                serializer.Serialize (stream, param);
            }

            // save the calibration images.
            #if UNITY_WEBGL && !UNITY_EDITOR
            string format = "jpg";
            MatOfInt compressionParams = new MatOfInt(Imgcodecs.CV_IMWRITE_JPEG_QUALITY, 100);
            #else
            string format = "png";
            MatOfInt compressionParams = new MatOfInt (Imgcodecs.CV_IMWRITE_PNG_COMPRESSION, 0);
            #endif
            for (int i = 0; i < allImgs.Count; ++i) {
                Imgcodecs.imwrite (Path.Combine (saveCalibratonFileDirectoryPath, calibratonDirectoryName + "_" + i.ToString ("00") + "." + format), allImgs [i], compressionParams);
            }

            savePathInputField.text = savePath;
            Debug.Log ("Saved the CameraParameters to disk in XML file format.");
            Debug.Log ("savePath: " + savePath);
        }

        public enum MarkerType
        {
            ChArUcoBoard,
            ChessBoard,
            CirclesGlid,
            AsymmetricCirclesGlid
        }

        public enum ArUcoDictionary
        {
            DICT_4X4_50 = Aruco.DICT_4X4_50,
            DICT_4X4_100 = Aruco.DICT_4X4_100,
            DICT_4X4_250 = Aruco.DICT_4X4_250,
            DICT_4X4_1000 = Aruco.DICT_4X4_1000,
            DICT_5X5_50 = Aruco.DICT_5X5_50,
            DICT_5X5_100 = Aruco.DICT_5X5_100,
            DICT_5X5_250 = Aruco.DICT_5X5_250,
            DICT_5X5_1000 = Aruco.DICT_5X5_1000,
            DICT_6X6_50 = Aruco.DICT_6X6_50,
            DICT_6X6_100 = Aruco.DICT_6X6_100,
            DICT_6X6_250 = Aruco.DICT_6X6_250,
            DICT_6X6_1000 = Aruco.DICT_6X6_1000,
            DICT_7X7_50 = Aruco.DICT_7X7_50,
            DICT_7X7_100 = Aruco.DICT_7X7_100,
            DICT_7X7_250 = Aruco.DICT_7X7_250,
            DICT_7X7_1000 = Aruco.DICT_7X7_1000,
            DICT_ARUCO_ORIGINAL = Aruco.DICT_ARUCO_ORIGINAL,
        }

        public enum NumberOfSquaresX
        {
            X_1 = 1,
            X_2,
            X_3,
            X_4,
            X_5,
            X_6,
            X_7,
            X_8,
            X_9,
            X_10,
            X_11,
            X_12,
            X_13,
            X_14,
            X_15,
        }

        public enum NumberOfSquaresY
        {
            Y_1 = 1,
            Y_2,
            Y_3,
            Y_4,
            Y_5,
            Y_6,
            Y_7,
            Y_8,
            Y_9,
            Y_10,
            Y_11,
            Y_12,
            Y_13,
            Y_14,
            Y_15,
        }
    }
}