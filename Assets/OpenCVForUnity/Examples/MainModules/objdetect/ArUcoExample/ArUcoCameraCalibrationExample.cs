using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Linq;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.ArucoModule;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// ArUco Camera Calibration Example
    /// An example of camera calibration using the objdetect module. (ChessBoard, CirclesGlid, AsymmetricCirclesGlid and ChArUcoBoard)
    /// Referring to https://docs.opencv.org/master/d4/d94/tutorial_camera_calibration.html.
    /// https://github.com/opencv/opencv/blob/master/samples/cpp/tutorial_code/calib3d/camera_calibration/camera_calibration.cpp
    /// https://docs.opencv.org/3.4.0/d7/d21/tutorial_interactive_calibration.html
    /// https://github.com/opencv/opencv/tree/master/apps/interactive-calibration
    /// https://docs.opencv.org/3.2.0/da/d13/tutorial_aruco_calibration.html
    /// https://github.com/opencv/opencv_contrib/blob/master/modules/aruco/samples/calibrate_camera_charuco.cpp
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class ArUcoCameraCalibrationExample : MonoBehaviour
    {
        /// <summary>
        /// The marker type used for calibration.
        /// </summary>
        [Tooltip("The marker type used for calibration.")]
        public MarkerType markerType = MarkerType.ChessBoard;

        /// <summary>
        /// The marker type dropdown.
        /// </summary>
        public Dropdown markerTypeDropdown;

        /// <summary>
        /// Number of inner corners per a item column. (square, circle)
        /// </summary>
        [Tooltip("Number of inner corners per a item column. (square, circle)")]
        public NumberOfBoardSizeWidth boardSizeW = NumberOfBoardSizeWidth.W_9;

        /// <summary>
        /// The board size W dropdown.
        /// </summary>
        public Dropdown boardSizeWDropdown;

        /// <summary>
        /// Number of inner corners per a item row. (square, circle)
        /// </summary>
        [Tooltip("Number of inner corners per a item row. (square, circle)")]
        public NumberOfBoardSizeHeight boardSizeH = NumberOfBoardSizeHeight.H_6;

        /// <summary>
        /// The board size H dropdown.
        /// </summary>
        public Dropdown boardSizeHDropdown;

        /// <summary>
        /// The save path input field.
        /// </summary>
        public InputField savePathInputField;

        /// <summary>
        /// The show undistort image.
        /// </summary>
        public bool showUndistortImage = true;

        /// <summary>
        /// The show undistort image toggle.
        /// </summary>
        public Toggle showUndistortImageToggle;



        [Header("Normal Calibration Option")]

        /// <summary>
        /// The normal calibration options group.
        /// </summary>
        public GameObject normalCalibrationOptionsGroup;

        /// <summary>
        /// The size of a square in some user defined metric system (pixel, millimeter)
        /// </summary>
        [Tooltip("The size of a square in some user defined metric system (pixel, millimeter)")]
        public float squareSize = 50f;

        /// <summary>
        /// The square size input field.
        /// </summary>
        public InputField squareSizeInputField;

        /// <summary>
        /// If your calibration board is inaccurate, unmeasured, roughly planar targets
        /// (Checkerboard patterns on paper using off-the-shelf printers are the most convenient calibration targets and most of them are not accurate enough.),
        /// a method from [219] can be utilized to dramatically improve the accuracies of the estimated camera intrinsic parameters.
        /// Need to set the measured values from the actual chess board to "squareSize" and "gridWidth".
        ///  https://docs.opencv.org/4.2.0/d9/d0c/group__calib3d.html#ga11eeb16e5a458e1ed382fb27f585b753
        /// </summary>
        [Tooltip("If your calibration board is inaccurate, unmeasured, roughly planar targets (Checkerboard patterns on paper using off-the-shelf printers are the most convenient calibration targets and most of them are not accurate enough.), a method from [219] can be utilized to dramatically improve the accuracies of the estimated camera intrinsic parameters. Need to set the measured values from the actual chess board to \"squareSize\" and \"gridWidth\".")]
        public bool useNewCalibrationMethod = true;

        /// <summary>
        /// The use new calibration method toggle.
        /// </summary>
        public Toggle useNewCalibrationMethodToggle;

        /// <summary>
        /// The measured distance between top-left (0, 0, 0) and top-right (squareSize*(boardSizeW - 1), 0, 0) corners of the pattern grid points.
        /// </summary>
        [Tooltip("The measured distance between top-left (0, 0, 0) and top-right (squareSize*(boardSizeW - 1), 0, 0) corners of the pattern grid points.")]
        public float gridWidth = 400f;

        /// <summary>
        /// The glid width input field.
        /// </summary>
        public InputField gridWidthInputField;

        /// <summary>
        /// Determines if use findChessboardCornersSB method. (More accurate than the findChessboardCorners and cornerSubPix methods)
        /// https://docs.opencv.org/4.2.0/d9/d0c/group__calib3d.html#gad0e88e13cd3d410870a99927510d7f91
        /// </summary>
        [Tooltip("Determines if use findChessboardCornersSB method. (More accurate than the findChessboardCorners and cornerSubPix methods)")]
        public bool useFindChessboardCornersSBMethod = true;

        /// <summary>
        /// Determines if enable CornerSubPix method. (Improve the found corners' coordinate accuracy for chessboard)
        /// </summary>
        [Tooltip("Determines if enable CornerSubPix method. (Improve the found corners' coordinate accuracy for chessboard)")]
        public bool enableCornerSubPix = true;



        [Header("ArUco Calibration Option")]

        /// <summary>
        /// The arUco calibration options group.
        /// </summary>
        public GameObject arUcoCalibrationOptionsGroup;

        /// <summary>
        /// The dictionary identifier used for ArUco marker detection.
        /// </summary>
        [Tooltip("The dictionary identifier used for ArUco marker detection.")]
        public ArUcoDictionary dictionaryId = ArUcoDictionary.DICT_6X6_250;

        /// <summary>
        /// The dictionary id dropdown.
        /// </summary>
        public Dropdown dictionaryIdDropdown;

        /// <summary>
        /// Determines if refine marker detection. (only valid for ArUco boards)
        /// </summary>
        [Tooltip("Determines if refine marker detection. (only valid for ArUco boards)")]
        public bool refineMarkerDetection = true;



        [Header("Image Input Option")]

        /// <summary>
        /// Determines if calibrates camera using the list of calibration images.
        /// </summary>
        [Tooltip("Determines if calibrates camera using the list of calibration images.")]
        public bool isImagesInputMode = false;

        /// <summary>
        /// The calibration images directory path.
        /// Set a relative directory path from the starting point of the "StreamingAssets" folder.  e.g. "objdetect/calibration_images/".
        /// </summary>
        [Tooltip("Set a relative directory path from the starting point of the \"StreamingAssets\" folder.  e.g. \"OpenCVForUnity/objdetect/calibration_images\"")]
        public string calibrationImagesDirectory = "OpenCVForUnity/objdetect/calibration_images";

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
        /// The undistorted bgr mat.
        /// </summary>
        Mat undistortedBgrMat;

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
        /// The rvecs.
        /// </summary>
        List<Mat> rvecs;

        /// <summary>
        /// The tvecs.
        /// </summary>
        List<Mat> tvecs;


        List<Mat> imagePoints;
        List<Mat> allImgs;
        bool isInitialized = false;
        bool isCalibrating = false;
        double repErr = 0;
        bool shouldCaptureFrame = false;

        const int findChessboardCornersFlags =
            Calib3d.CALIB_CB_ADAPTIVE_THRESH |
            Calib3d.CALIB_CB_NORMALIZE_IMAGE |
            //Calib3d.CALIB_CB_FILTER_QUADS |
            Calib3d.CALIB_CB_FAST_CHECK |
            0;

        const int findChessboardCornersSBFlags =
            Calib3d.CALIB_CB_NORMALIZE_IMAGE |
            Calib3d.CALIB_CB_EXHAUSTIVE |
            Calib3d.CALIB_CB_ACCURACY |
            0;

        const int findCirclesGridFlags =
            //Calib3d.CALIB_CB_CLUSTERING |
            0;

        const int calibrationFlags =
            //Calib3d.CALIB_USE_INTRINSIC_GUESS |
            //Calib3d.CALIB_FIX_PRINCIPAL_POINT |
            //Calib3d.CALIB_FIX_ASPECT_RATIO |
            //Calib3d.CALIB_ZERO_TANGENT_DIST |
            //Calib3d.CALIB_FIX_K1 |
            //Calib3d.CALIB_FIX_K2 |
            //Calib3d.CALIB_FIX_K3 |
            //Calib3d.CALIB_FIX_K4 |
            //Calib3d.CALIB_FIX_K5 |
            Calib3d.CALIB_USE_LU |
            0;


        /*
        // for ChArUcoBoard.
        // chessboard square side length (normally in meters)
        const float chArUcoBoradSquareLength = 0.04f;
        // marker side length (same unit than squareLength)
        const float chArUcoBoradMarkerLength = 0.02f;
        const int charucoMinMarkers = 2;

        Mat ids;
        List<Mat> corners;
        List<Mat> rejectedCorners;
        Mat recoveredIdxs;
        Mat charucoCorners;
        Mat charucoIds;
        CharucoBoard charucoBoard;
        ArucoDetector arucoDetector;
        CharucoDetector charucoDetector;
        */


        Dictionary dictionary;
        List<List<Mat>> allCorners;
        List<Mat> allIds;


        // Use this for initialization
        IEnumerator Start()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

            // fix the screen orientation.
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            // wait for the screen orientation to change.
            yield return null;

            markerTypeDropdown.value = (int)markerType;
            boardSizeWDropdown.value = (int)boardSizeW - 1;
            boardSizeHDropdown.value = (int)boardSizeH - 1;

            showUndistortImageToggle.isOn = showUndistortImage;

            squareSizeInputField.text = squareSize.ToString();
            useNewCalibrationMethodToggle.isOn = useNewCalibrationMethod;
            gridWidthInputField.text = gridWidth.ToString();

            dictionaryIdDropdown.value = (int)dictionaryId;

            bool arUcoCalibMode = markerType == MarkerType.ChArUcoBoard;
            normalCalibrationOptionsGroup.gameObject.SetActive(!arUcoCalibMode);
            arUcoCalibrationOptionsGroup.gameObject.SetActive(arUcoCalibMode);

#if UNITY_WEBGL && !UNITY_EDITOR
            isImagesInputMode = false;
#endif
            if (isImagesInputMode)
            {
                isImagesInputMode = InitializeImagesInputMode();
            }

            if (!isImagesInputMode)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
                webCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
#endif
                webCamTextureToMatHelper.Initialize();
            }
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            InitializeCalibraton(webCamTextureMat);

            // If the WebCam is front facing, flip the Mat horizontally. Required for successful detection of AR markers.
            if (webCamTextureToMatHelper.IsFrontFacing() && !webCamTextureToMatHelper.flipHorizontal)
            {
                webCamTextureToMatHelper.flipHorizontal = true;
            }
            else if (!webCamTextureToMatHelper.IsFrontFacing() && webCamTextureToMatHelper.flipHorizontal)
            {
                webCamTextureToMatHelper.flipHorizontal = false;
            }
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            DisposeCalibraton();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update()
        {
            if (isImagesInputMode)
                return;

            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

                if (shouldCaptureFrame)
                {
                    shouldCaptureFrame = false;
                    Mat frameMat = grayMat.clone();
                    double e = CaptureFrame(frameMat);
                    if (e > 0)
                        repErr = e;
                }

                DrawFrame(grayMat, bgrMat);

                if (showUndistortImage)
                {
                    Calib3d.undistort(bgrMat, undistortedBgrMat, camMatrix, distCoeffs);
                    DrawCalibrationResult(undistortedBgrMat);
                    Imgproc.cvtColor(undistortedBgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);
                }
                else
                {
                    DrawCalibrationResult(bgrMat);
                    Imgproc.cvtColor(bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);
                }

                Utils.matToTexture2D(rgbaMat, texture);
            }
        }

        private void InitializeCalibraton(Mat frameMat)
        {
            texture = new Texture2D(frameMat.cols(), frameMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(frameMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(frameMat.cols(), frameMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


            float width = frameMat.width();
            float height = frameMat.height();

            float imageSizeScale = 1.0f;
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
                imageSizeScale = (float)Screen.height / (float)Screen.width;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }


            // set cameraparam.
            camMatrix = CreateCameraMatrix(width, height);
            Debug.Log("camMatrix " + camMatrix.dump());

            distCoeffs = new MatOfDouble(0, 0, 0, 0, 0);
            Debug.Log("distCoeffs " + distCoeffs.dump());

            // calibration camera.
            Size imageSize = new Size(width * imageSizeScale, height * imageSizeScale);
            double apertureWidth = 0;
            double apertureHeight = 0;
            double[] fovx = new double[1];
            double[] fovy = new double[1];
            double[] focalLength = new double[1];
            Point principalPoint = new Point(0, 0);
            double[] aspectratio = new double[1];

            Calib3d.calibrationMatrixValues(camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);

            Debug.Log("imageSize " + imageSize.ToString());
            Debug.Log("apertureWidth " + apertureWidth);
            Debug.Log("apertureHeight " + apertureHeight);
            Debug.Log("fovx " + fovx[0]);
            Debug.Log("fovy " + fovy[0]);
            Debug.Log("focalLength " + focalLength[0]);
            Debug.Log("principalPoint " + principalPoint.ToString());
            Debug.Log("aspectratio " + aspectratio[0]);


            grayMat = new Mat(frameMat.rows(), frameMat.cols(), CvType.CV_8UC1);
            bgrMat = new Mat(frameMat.rows(), frameMat.cols(), CvType.CV_8UC3);
            undistortedBgrMat = new Mat();
            rgbaMat = new Mat(frameMat.rows(), frameMat.cols(), CvType.CV_8UC4);
            rvecs = new List<Mat>();
            tvecs = new List<Mat>();

            imagePoints = new List<Mat>();
            allImgs = new List<Mat>();


            /*
            ids = new Mat();
            corners = new List<Mat>();
            rejectedCorners = new List<Mat>();
            recoveredIdxs = new Mat();
            DetectorParameters detectorParams = new DetectorParameters();
            detectorParams.set_minDistanceToBorder(3);
            detectorParams.set_useAruco3Detection(true);
            detectorParams.set_cornerRefinementMethod(Objdetect.CORNER_REFINE_SUBPIX);
            detectorParams.set_minSideLengthCanonicalImg(16);
            detectorParams.set_errorCorrectionRate(0.8);
            dictionary = Objdetect.getPredefinedDictionary((int)dictionaryId);
            RefineParameters refineParameters = new RefineParameters(10f, 3f, true);
            arucoDetector = new ArucoDetector(dictionary, detectorParams, refineParameters);

            charucoCorners = new Mat();
            charucoIds = new Mat();
            charucoBoard = new CharucoBoard( new Size((int)boardSizeW, (int)boardSizeH), chArUcoBoradSquareLength, chArUcoBoradMarkerLength, dictionary);

            charucoDetector = new CharucoDetector(charucoBoard);
            CharucoParameters charucoParameters = charucoDetector.getCharucoParameters();
            charucoParameters.set_cameraMatrix(camMatrix);
            charucoParameters.set_distCoeffs(distCoeffs);
            charucoParameters.set_minMarkers(charucoMinMarkers);
            charucoDetector.setCharucoParameters(charucoParameters);
            charucoDetector.setDetectorParameters(detectorParams);
            charucoDetector.setRefineParameters(refineParameters);
            */
            allIds = new List<Mat>();
            allCorners = new List<List<Mat>>();


            isInitialized = true;
        }

        private void DisposeCalibraton()
        {
            ResetCalibration();

            if (grayMat != null)
                grayMat.Dispose();
            if (bgrMat != null)
                bgrMat.Dispose();
            if (undistortedBgrMat != null)
                undistortedBgrMat.Dispose();
            if (rgbaMat != null)
                rgbaMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

            foreach (var item in rvecs)
            {
                item.Dispose();
            }
            rvecs.Clear();
            foreach (var item in tvecs)
            {
                item.Dispose();
            }
            tvecs.Clear();

            /*
            if (ids != null)
                ids.Dispose();
            foreach (var item in corners)
            {
                item.Dispose();
            }
            corners.Clear();
            foreach (var item in rejectedCorners)
            {
                item.Dispose();
            }
            rejectedCorners.Clear();
            if (recoveredIdxs != null)
                recoveredIdxs.Dispose();

            if (charucoCorners != null)
                charucoCorners.Dispose();
            if (charucoIds != null)
                charucoIds.Dispose();
            if (charucoBoard != null)
                charucoBoard.Dispose();

            if (arucoDetector != null)
                arucoDetector.Dispose();
            if (charucoDetector != null)
                charucoDetector.Dispose();
            */

            isInitialized = false;
        }

        private void DrawFrame(Mat grayMat, Mat bgrMat)
        {
            Imgproc.cvtColor(grayMat, bgrMat, Imgproc.COLOR_GRAY2BGR);

            switch (markerType)
            {
                default:
                case MarkerType.ChessBoard:
                case MarkerType.CirclesGlid:
                case MarkerType.AsymmetricCirclesGlid:
                    // detect markers.
                    MatOfPoint2f points = new MatOfPoint2f();
                    bool found = false;

                    switch (markerType)
                    {
                        default:
                        case MarkerType.ChessBoard:
                            if (useFindChessboardCornersSBMethod)
                            {
                                found = Calib3d.findChessboardCornersSB(grayMat, new Size((int)boardSizeW, (int)boardSizeH), points, findChessboardCornersSBFlags);
                            }
                            else
                            {
                                found = Calib3d.findChessboardCorners(grayMat, new Size((int)boardSizeW, (int)boardSizeH), points, findChessboardCornersFlags);
                            }
                            break;
                        case MarkerType.CirclesGlid:
                            found = Calib3d.findCirclesGrid(grayMat, new Size((int)boardSizeW, (int)boardSizeH), points, findCirclesGridFlags | Calib3d.CALIB_CB_SYMMETRIC_GRID);
                            break;
                        case MarkerType.AsymmetricCirclesGlid:
                            found = Calib3d.findCirclesGrid(grayMat, new Size((int)boardSizeW, (int)boardSizeH), points, findCirclesGridFlags | Calib3d.CALIB_CB_ASYMMETRIC_GRID);
                            break;
                    }

                    if (found)
                    {
                        // draw markers.
                        Calib3d.drawChessboardCorners(bgrMat, new Size((int)boardSizeW, (int)boardSizeH), points, found);
                    }
                    break;
                case MarkerType.ChArUcoBoard:
                    /*
                    // detect markers.
                    arucoDetector.detectMarkers(grayMat, corners, ids, rejectedCorners);

                    // refine marker detection.
                    if (refineMarkerDetection)
                    {
                        // https://github.com/opencv/opencv/blob/377be68d923e40900ac5526242bcf221e3f355e5/modules/objdetect/src/aruco/charuco_detector.cpp#L310
                        arucoDetector.refineDetectedMarkers(grayMat, charucoBoard, corners, ids, rejectedCorners);
                    }

                    // if at least one marker detected
                    if (ids.total() > 0)
                    {
                        charucoDetector.detectBoard(grayMat, charucoCorners, charucoIds, corners, ids);

                        // draw markers.
                        if (corners.Count == ids.total() || ids.total() == 0)
                            Objdetect.drawDetectedMarkers(bgrMat, corners, ids, new Scalar(0, 255, 0, 255));

                        // if at least one charuco corner detected
                        if (charucoCorners.total() == charucoIds.total() || charucoIds.total() == 0)
                        {
                            Objdetect.drawDetectedCornersCharuco(bgrMat, charucoCorners, charucoIds, new Scalar(0, 0, 255, 255));
                        }
                    }
                    */
                    break;
            }
        }

        private void DrawCalibrationResult(Mat bgrMat)
        {
            double[] camMatrixArr = new double[(int)camMatrix.total()];
            camMatrix.get(0, 0, camMatrixArr);
            double[] distCoeffsArr = new double[(int)distCoeffs.total()];
            distCoeffs.get(0, 0, distCoeffsArr);

            int textLeft = 320;
            int ff = Imgproc.FONT_HERSHEY_SIMPLEX;
            double fs = 0.4;
            Scalar c = new Scalar(255, 255, 255, 255);
            int t = 0;
            int lt = Imgproc.LINE_AA;
            bool blo = false;
            int frameCount = (markerType == MarkerType.ChArUcoBoard) ? allCorners.Count : imagePoints.Count;
            Imgproc.putText(bgrMat, frameCount + " FRAME CAPTURED", new Point(bgrMat.cols() - textLeft, 20), ff, fs, c, t, lt, blo);
            Imgproc.putText(bgrMat, "IMAGE_WIDTH: " + bgrMat.width(), new Point(bgrMat.cols() - textLeft, 40), ff, fs, c, t, lt, blo);
            Imgproc.putText(bgrMat, "IMAGE_HEIGHT: " + bgrMat.height(), new Point(bgrMat.cols() - textLeft, 60), ff, fs, c, t, lt, blo);
            Imgproc.putText(bgrMat, "CALIBRATION_FLAGS: " + calibrationFlags, new Point(bgrMat.cols() - textLeft, 80), ff, fs, c, t, lt, blo);

            Imgproc.putText(bgrMat, "CAMERA_MATRIX: ", new Point(bgrMat.cols() - 310, 100), ff, fs, c, t, lt, blo);
            for (int i = 0; i < camMatrixArr.Length; i = i + 3)
            {
                Imgproc.putText(bgrMat, "   " + camMatrixArr[i] + ", " + camMatrixArr[i + 1] + ", " + camMatrixArr[i + 2] + ",", new Point(bgrMat.cols() - textLeft, 120 + 20 * i / 3), ff, fs, c, t, lt, blo);
            }
            Imgproc.putText(bgrMat, "DISTORTION_COEFFICIENTS: ", new Point(bgrMat.cols() - textLeft, 180), ff, fs, c, t, lt, blo);
            for (int i = 0; i < distCoeffsArr.Length; ++i)
            {
                Imgproc.putText(bgrMat, "   " + distCoeffsArr[i] + ",", new Point(bgrMat.cols() - textLeft, 200 + 20 * i), ff, fs, c, t, lt, blo);
            }
            Imgproc.putText(bgrMat, "AVG_REPROJECTION_ERROR: " + repErr, new Point(bgrMat.cols() - textLeft, 300), ff, fs, c, t, lt, blo);

            if (frameCount == 0)
                Imgproc.putText(bgrMat, "Please press the capture button to start!", new Point(5, bgrMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
        }

        private double CaptureFrame(Mat frameMat)
        {
            double repErr = -1;

            switch (markerType)
            {
                default:
                case MarkerType.ChessBoard:
                case MarkerType.CirclesGlid:
                case MarkerType.AsymmetricCirclesGlid:

                    MatOfPoint2f points = new MatOfPoint2f();
                    Size patternSize = new Size((int)boardSizeW, (int)boardSizeH);

                    bool found = false;
                    switch (markerType)
                    {
                        default:
                        case MarkerType.ChessBoard:
                            if (useFindChessboardCornersSBMethod)
                            {
                                found = Calib3d.findChessboardCornersSB(frameMat, patternSize, points, findChessboardCornersSBFlags);
                            }
                            else
                            {
                                found = Calib3d.findChessboardCorners(frameMat, patternSize, points, findChessboardCornersFlags);
                            }
                            break;
                        case MarkerType.CirclesGlid:
                            found = Calib3d.findCirclesGrid(frameMat, patternSize, points, findCirclesGridFlags | Calib3d.CALIB_CB_SYMMETRIC_GRID);
                            break;
                        case MarkerType.AsymmetricCirclesGlid:
                            found = Calib3d.findCirclesGrid(frameMat, patternSize, points, findCirclesGridFlags | Calib3d.CALIB_CB_ASYMMETRIC_GRID);
                            break;
                    }

                    if (found)
                    {
                        if (markerType == MarkerType.ChessBoard && !useFindChessboardCornersSBMethod && enableCornerSubPix)
                        {
                            int winSize = 11;
                            Imgproc.cornerSubPix(frameMat, points, new Size(winSize, winSize), new Size(-1, -1), new TermCriteria(TermCriteria.EPS + TermCriteria.COUNT, 30, 0.0001));
                        }

                        imagePoints.Add(points);
                        allImgs.Add(frameMat);

                        Debug.Log(imagePoints.Count + " Frame captured.");
                    }
                    else
                    {
                        Debug.Log("Invalid frame.");
                        frameMat.Dispose();
                        if (points != null)
                            points.Dispose();
                        return -1;
                    }

                    if (imagePoints.Count < 1)
                    {
                        Debug.Log("Not enough points for calibration.");
                        repErr = -1;
                    }
                    else
                    {
                        MatOfPoint3f objectPoint = new MatOfPoint3f(new Mat(imagePoints[0].rows(), 1, CvType.CV_32FC3));
                        CalcChessboardCorners(patternSize, squareSize, objectPoint, markerType);

                        float grid_width = squareSize * ((int)patternSize.width - 1);
                        bool release_object = false;
                        if (useNewCalibrationMethod)
                        {
                            grid_width = gridWidth;
                            release_object = true;
                        }
                        float[] tlPt = new float[3]; // top-left point
                        objectPoint.get(0, 0, tlPt);
                        float[] trPt = new float[3]; // top-right point
                        objectPoint.get((int)patternSize.width - 1, 0, trPt);
                        trPt[0] = tlPt[0] + grid_width;
                        objectPoint.put((int)patternSize.width - 1, 0, trPt);
                        Mat newObjPoints = objectPoint.clone();

                        List<Mat> objectPoints = new List<Mat>();
                        for (int i = 0; i < imagePoints.Count; ++i)
                        {
                            objectPoints.Add(objectPoint.clone());
                        }

                        int iFixedPoint = -1;
                        if (release_object)
                            iFixedPoint = (int)patternSize.width - 1;
                        repErr = Calib3d.calibrateCameraRO(
                            objectPoints,
                            imagePoints,
                            frameMat.size(),
                            iFixedPoint,
                            camMatrix,
                            distCoeffs,
                            rvecs,
                            tvecs,
                            newObjPoints,
                            calibrationFlags
                            );

                        //if (release_object)
                        //{
                        //    Debug.Log("New board corners: ");
                        //    Point3[] newPoints = new MatOfPoint3f(newObjPoints).toArray();
                        //    Debug.Log(newPoints[0]);
                        //    Debug.Log(newPoints[(int)patternSize.width - 1]);
                        //    Debug.Log(newPoints[(int)patternSize.width * ((int)patternSize.height - 1)]);
                        //    Debug.Log(newPoints[newPoints.Length - 1]);
                        //}

                        objectPoint.Dispose();
                    }

                    break;
                case MarkerType.ChArUcoBoard:
                    /*
                    List<Mat> corners = new List<Mat>();
                    Mat ids = new Mat();
                    
                    arucoDetector.detectMarkers(frameMat, corners, ids, rejectedCorners);

                    if (refineMarkerDetection)
                    {
                        // https://github.com/opencv/opencv/blob/377be68d923e40900ac5526242bcf221e3f355e5/modules/objdetect/src/aruco/charuco_detector.cpp#L310
                        arucoDetector.refineDetectedMarkers(frameMat, charucoBoard, corners, ids, rejectedCorners);
                    }

                    if (ids.total() > 0)
                    {
                        allCorners.Add(corners);
                        allIds.Add(ids);
                        allImgs.Add(frameMat);

                        Debug.Log(allCorners.Count + " Frame captured.");
                    }
                    else
                    {
                        Debug.Log("Invalid frame.");

                        frameMat.Dispose();
                        if (ids != null)
                            ids.Dispose();
                        foreach (var item in corners)
                        {
                            item.Dispose();
                        }
                        corners.Clear();

                        return -1;
                    }

                    // calibrate camera using charuco boards
                    repErr = CalibrateCameraCharuco(allCorners, allIds, charucoBoard, frameMat.size(), camMatrix, distCoeffs, rvecs, tvecs, calibrationFlags, calibrationFlags);
                    */
                    break;
            }

            Debug.Log("repErr: " + repErr);
            Debug.Log("camMatrix: " + camMatrix.dump());
            Debug.Log("distCoeffs: " + distCoeffs.dump());

            return repErr;
        }

        /*
        private double CalibrateCameraCharuco(List<List<Mat>> allCorners, List<Mat> allIds, CharucoBoard board, Size imageSize, Mat cameraMatrix, Mat distCoeffs, List<Mat> rvecs = null, List<Mat> tvecs = null, int calibrationFlags = 0, int minMarkers = 2)
        {
            // prepare data for charuco calibration
            int nFrames = allCorners.Count;
            List<Mat> allCharucoCorners = new List<Mat>();
            List<Mat> allCharucoIds = new List<Mat>();
            List<Mat> filteredImages = new List<Mat>();

            for (int i = 0; i < nFrames; ++i)
            {
                // interpolate using camera parameters
                Mat currentCharucoCorners = new Mat();
                Mat currentCharucoIds = new Mat();

                charucoDetector.detectBoard(allImgs[i], currentCharucoCorners, currentCharucoIds, allCorners[i], allIds[i]);

                //if (currentCharucoIds.total() > 0)
                if (currentCharucoIds.total() > 0 && currentCharucoCorners.total() == currentCharucoIds.total())
                {
                    allCharucoCorners.Add(currentCharucoCorners);
                    allCharucoIds.Add(currentCharucoIds);
                    filteredImages.Add(allImgs[i]);
                }
                else
                {
                    currentCharucoCorners.Dispose();
                    currentCharucoIds.Dispose();
                }
            }

            if (allCharucoCorners.Count < 1)
            {
                Debug.Log("Not enough corners for calibration.");
                return -1;
            }

            if (rvecs == null)
                rvecs = new List<Mat>();
            if (tvecs == null)
                tvecs = new List<Mat>();

            return Aruco.calibrateCameraCharuco(allCharucoCorners, allCharucoIds, board, imageSize, cameraMatrix, distCoeffs, rvecs, tvecs, calibrationFlags); // error
        }
        */

        private void ResetCalibration()
        {
            foreach (var item in allImgs)
            {
                item.Dispose();
            }
            allImgs.Clear();

            repErr = 0;
            camMatrix = CreateCameraMatrix(bgrMat.width(), bgrMat.height());
            distCoeffs = new MatOfDouble(0, 0, 0, 0, 0);

            foreach (var item in imagePoints)
            {
                item.Dispose();
            }
            imagePoints.Clear();

            foreach (var corners in allCorners)
            {
                foreach (var item in corners)
                {
                    item.Dispose();
                }
            }
            allCorners.Clear();

            
            foreach (var item in allIds)
            {
                item.Dispose();
            }
            allIds.Clear();
            
        }

        private Mat CreateCameraMatrix(float width, float height)
        {
            int max_d = (int)Mathf.Max(width, height);
            double fx = max_d;
            double fy = max_d;
            double cx = width / 2.0f;
            double cy = height / 2.0f;

            Mat camMatrix = new Mat(3, 3, CvType.CV_64FC1);
            camMatrix.put(0, 0, fx);
            camMatrix.put(0, 1, 0);
            camMatrix.put(0, 2, cx);
            camMatrix.put(1, 0, 0);
            camMatrix.put(1, 1, fy);
            camMatrix.put(1, 2, cy);
            camMatrix.put(2, 0, 0);
            camMatrix.put(2, 1, 0);
            camMatrix.put(2, 2, 1.0f);

            return camMatrix;
        }

        private void CalcChessboardCorners(Size patternSize, float squareSize, MatOfPoint3f corners, MarkerType markerType)
        {
            if ((int)(patternSize.width * patternSize.height) != corners.rows())
            {
                Debug.Log("Invalid corners size.");
                corners.create((int)(patternSize.width * patternSize.height), 1, CvType.CV_32FC3);
            }

            int width = (int)patternSize.width;
            int height = (int)patternSize.height;

            switch (markerType)
            {
                default:
                case MarkerType.ChessBoard:
                case MarkerType.CirclesGlid:
                    for (int i = 0; i < height; ++i)
                    {
                        for (int j = 0; j < width; ++j)
                        {
                            corners.put(width * i + j, 0, new float[] { j * squareSize, i * squareSize, 0f });
                        }
                    }
                    break;
                case MarkerType.AsymmetricCirclesGlid:
                    for (int i = 0; i < height; ++i)
                    {
                        for (int j = 0; j < width; ++j)
                        {
                            corners.put(width * i + j, 0, new float[] { (2 * j + i % 2) * squareSize, i * squareSize, 0f });
                        }
                    }
                    break;
            }
        }

        private bool InitializeImagesInputMode()
        {
            if (isInitialized)
                DisposeCalibraton();

            if (String.IsNullOrEmpty(calibrationImagesDirectory))
            {
                Debug.LogWarning("When using the images input mode, please set a calibration images directory path.");
                return false;
            }

            string dirPath = Path.Combine(Application.streamingAssetsPath, calibrationImagesDirectory);
            if (!Directory.Exists(dirPath))
            {
                Debug.LogWarning("The directory does not exist.");
                return false;
            }
            string[] imageFiles = GetImageFilesInDirectory(dirPath);
            if (imageFiles.Length < 1)
            {
                Debug.LogWarning("The image file does not exist.");
                return false;
            }

            Uri rootPath = new Uri(Application.streamingAssetsPath + System.IO.Path.AltDirectorySeparatorChar);
            Uri fullPath = new Uri(imageFiles[0]);
            string relativePath = rootPath.MakeRelativeUri(fullPath).ToString();

            using (Mat gray = Imgcodecs.imread(Utils.getFilePath(relativePath), Imgcodecs.IMREAD_GRAYSCALE))
            {

                if (gray.total() == 0)
                {
                    Debug.LogWarning("Invalid image file.");
                    return false;
                }

                using (Mat bgr = new Mat(gray.size(), CvType.CV_8UC3))
                using (Mat rgba = new Mat(gray.size(), CvType.CV_8UC4))
                {
                    Imgproc.cvtColor(gray, rgba, Imgproc.COLOR_GRAY2RGBA);
                    InitializeCalibraton(rgba);

                    DrawFrame(gray, bgr);
                    DrawCalibrationResult(bgr);
                    Imgproc.cvtColor(bgr, rgba, Imgproc.COLOR_BGR2RGBA);
                    Utils.matToTexture2D(rgba, texture);
                }
            }
            return true;
        }

        private IEnumerator CalibrateCameraUsingImages()
        {
            string dirPath = Path.Combine(Application.streamingAssetsPath, calibrationImagesDirectory);
            string[] imageFiles = GetImageFilesInDirectory(dirPath);
            if (imageFiles.Length < 1)
                yield break;

            isCalibrating = true;
            markerTypeDropdown.interactable = boardSizeWDropdown.interactable = boardSizeHDropdown.interactable = false;
            normalCalibrationOptionsGroup.gameObject.SetActive(false);
            arUcoCalibrationOptionsGroup.gameObject.SetActive(false);

            Uri rootPath = new Uri(Application.streamingAssetsPath + System.IO.Path.AltDirectorySeparatorChar);

            foreach (var path in imageFiles)
            {

                Uri fullPath = new Uri(path);
                string relativePath = rootPath.MakeRelativeUri(fullPath).ToString();

                using (Mat gray = Imgcodecs.imread(Utils.getFilePath(relativePath), Imgcodecs.IMREAD_GRAYSCALE))
                {
                    if (gray.width() != bgrMat.width() || gray.height() != bgrMat.height())
                        continue;

                    Mat frameMat = gray.clone();
                    double e = CaptureFrame(frameMat);
                    if (e > 0)
                        repErr = e;

                    DrawFrame(gray, bgrMat);
                    DrawCalibrationResult(bgrMat);
                    Imgproc.cvtColor(bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                    Utils.matToTexture2D(rgbaMat, texture);
                }
                yield return new WaitForSeconds(0.5f);
            }

            isCalibrating = false;
            markerTypeDropdown.interactable = boardSizeWDropdown.interactable = boardSizeHDropdown.interactable = true;
            bool arUcoCalibMode = markerType == MarkerType.ChArUcoBoard;
            normalCalibrationOptionsGroup.gameObject.SetActive(!arUcoCalibMode);
            arUcoCalibrationOptionsGroup.gameObject.SetActive(arUcoCalibMode);
        }

        private string[] GetImageFilesInDirectory(string dirPath)
        {
            if (Directory.Exists(dirPath))
            {
                string[] files = Directory.GetFiles(dirPath, "*.jpg");
                files = files.Concat(Directory.GetFiles(dirPath, "*.jpeg")).ToArray();
                files = files.Concat(Directory.GetFiles(dirPath, "*.png")).ToArray();
                files = files.Concat(Directory.GetFiles(dirPath, "*.tiff")).ToArray();
                files = files.Concat(Directory.GetFiles(dirPath, "*.tif")).ToArray();
                return files;
            }
            return new string[0];
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (isImagesInputMode)
            {
                DisposeCalibraton();
            }
            else
            {
                webCamTextureToMatHelper.Dispose();
            }

            Screen.orientation = ScreenOrientation.AutoRotation;


            Utils.setDebugMode(false);
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
            if (isImagesInputMode)
                return;

            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            if (isImagesInputMode)
                return;

            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            if (isImagesInputMode)
                return;

            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            if (isImagesInputMode)
                return;

            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.requestedIsFrontFacing;
        }

        /// <summary>
        /// Raises the marker type dropdown value changed event.
        /// </summary>
        public void OnMarkerTypeDropdownValueChanged(int result)
        {
            if ((int)markerType != result)
            {
                markerType = (MarkerType)result;

                bool arUcoCalibMode = markerType == MarkerType.ChArUcoBoard;

                normalCalibrationOptionsGroup.gameObject.SetActive(!arUcoCalibMode);
                arUcoCalibrationOptionsGroup.gameObject.SetActive(arUcoCalibMode);

                if (isImagesInputMode)
                {
                    InitializeImagesInputMode();
                }
                else
                {
                    if (webCamTextureToMatHelper.IsInitialized())
                        webCamTextureToMatHelper.Initialize();
                }
            }
        }

        /// <summary>
        /// Raises the board size W dropdown value changed event.
        /// </summary>
        public void OnBoardSizeWDropdownValueChanged(int result)
        {
            if ((int)boardSizeW != result + 1)
            {
                boardSizeW = (NumberOfBoardSizeWidth)(result + 1);

                gridWidth = squareSize * ((int)boardSizeW - 1);
                gridWidthInputField.text = gridWidth.ToString();

                if (isImagesInputMode)
                {
                    InitializeImagesInputMode();
                }
                else
                {
                    if (webCamTextureToMatHelper.IsInitialized())
                        webCamTextureToMatHelper.Initialize();
                }
            }
        }

        /// <summary>
        /// Raises the board size H dropdown value changed event.
        /// </summary>
        public void OnBoardSizeHDropdownValueChanged(int result)
        {
            if ((int)boardSizeH != result + 1)
            {
                boardSizeH = (NumberOfBoardSizeHeight)(result + 1);

                if (isImagesInputMode)
                {
                    InitializeImagesInputMode();
                }
                else
                {
                    if (webCamTextureToMatHelper.IsInitialized())
                        webCamTextureToMatHelper.Initialize();
                }
            }
        }


        /// <summary>
        /// Raises the show undistort image toggle value changed event.
        /// </summary>
        public void OnShowUndistortImageToggleValueChanged()
        {
            if (showUndistortImage != showUndistortImageToggle.isOn)
            {
                showUndistortImage = showUndistortImageToggle.isOn;
            }
        }



        /// <summary>
        /// Raises the square size input field end edit event.
        /// </summary>
        public void OnSquareSizeInputFieldEndEdit()
        {
            float f;
            bool result = float.TryParse(squareSizeInputField.text, out f);

            if (result)
            {
                squareSize = f;
                squareSizeInputField.text = f.ToString();
            }
            else
            {
                squareSize = 1f;
                squareSizeInputField.text = squareSize.ToString();
            }
        }

        /// <summary>
        /// Raises the use new calibration method toggle value changed event.
        /// </summary>
        public void OnUseNewCalibrationMethodToggleValueChanged()
        {
            if (useNewCalibrationMethod != useNewCalibrationMethodToggle.isOn)
            {
                useNewCalibrationMethod = useNewCalibrationMethodToggle.isOn;
            }
        }

        /// <summary>
        /// Raises the grid width input field end edit event.
        /// </summary>
        public void OnGridWidthInputFieldEndEdit()
        {
            float f;
            bool result = float.TryParse(gridWidthInputField.text, out f);

            if (result)
            {
                gridWidth = f;
                gridWidthInputField.text = f.ToString();
            }
            else
            {
                gridWidth = squareSize * ((int)boardSizeW - 1);
                gridWidthInputField.text = gridWidth.ToString();
            }
        }



        /// <summary>
        /// Raises the dictionary id dropdown value changed event.
        /// </summary>
        public void OnDictionaryIdDropdownValueChanged(int result)
        {
            if ((int)dictionaryId != result)
            {
                dictionaryId = (ArUcoDictionary)result;
                dictionary = Objdetect.getPredefinedDictionary((int)dictionaryId);

                if (isImagesInputMode)
                {
                    InitializeImagesInputMode();
                }
                else
                {
                    if (webCamTextureToMatHelper.IsInitialized())
                        webCamTextureToMatHelper.Initialize();
                }
            }
        }

        /// <summary>
        /// Raises the capture button click event.
        /// </summary>
        public void OnCaptureButtonClick()
        {
            if (isImagesInputMode)
            {
                if (!isCalibrating)
                    InitializeImagesInputMode();
                StartCoroutine("CalibrateCameraUsingImages");
            }
            else
            {
                shouldCaptureFrame = true;
            }
        }

        /// <summary>
        /// Raises the reset button click event.
        /// </summary>
        public void OnResetButtonClick()
        {
            if (isImagesInputMode)
            {
                if (!isCalibrating)
                    InitializeImagesInputMode();
            }
            else
            {
                ResetCalibration();
            }
        }

        /// <summary>
        /// Raises the save button click event.
        /// </summary>
        public void OnSaveButtonClick()
        {
            string saveDirectoryPath = Path.Combine(Application.persistentDataPath, "ArUcoCameraCalibrationExample");

            if (!Directory.Exists(saveDirectoryPath))
            {
                Directory.CreateDirectory(saveDirectoryPath);
            }

            string calibratonDirectoryName = "camera_parameters" + bgrMat.width() + "x" + bgrMat.height();
            string saveCalibratonFileDirectoryPath = Path.Combine(saveDirectoryPath, calibratonDirectoryName);

            // Clean up old files.
            if (Directory.Exists(saveCalibratonFileDirectoryPath))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(saveCalibratonFileDirectoryPath);
                foreach (FileInfo fileInfo in directoryInfo.GetFiles())
                {
                    if ((fileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        fileInfo.Attributes = FileAttributes.Normal;
                    }
                }
                if ((directoryInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    directoryInfo.Attributes = FileAttributes.Directory;
                }
                directoryInfo.Delete(true);
            }
            Directory.CreateDirectory(saveCalibratonFileDirectoryPath);

            // save the calibraton file.
            string savePath = Path.Combine(saveCalibratonFileDirectoryPath, calibratonDirectoryName + ".xml");
            int frameCount = (markerType == MarkerType.ChArUcoBoard) ? allCorners.Count : imagePoints.Count;
            CameraParameters param = new CameraParameters(frameCount, bgrMat.width(), bgrMat.height(), calibrationFlags, camMatrix, distCoeffs, repErr);
            XmlSerializer serializer = new XmlSerializer(typeof(CameraParameters));
            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                serializer.Serialize(stream, param);
            }

            // save the calibration images.
#if UNITY_WEBGL && !UNITY_EDITOR
            string format = "jpg";
            MatOfInt compressionParams = new MatOfInt(Imgcodecs.IMWRITE_JPEG_QUALITY, 100);
#else
            string format = "png";
            MatOfInt compressionParams = new MatOfInt(Imgcodecs.IMWRITE_PNG_COMPRESSION, 0);
#endif
            for (int i = 0; i < allImgs.Count; ++i)
            {
                Imgcodecs.imwrite(Path.Combine(saveCalibratonFileDirectoryPath, calibratonDirectoryName + "_" + i.ToString("00") + "." + format), allImgs[i], compressionParams);
            }

            savePathInputField.text = savePath;
            Debug.Log("Saved the CameraParameters to disk in XML file format.");
            Debug.Log("savePath: " + savePath);
        }

        public enum MarkerType
        {
            ChessBoard,
            CirclesGlid,
            AsymmetricCirclesGlid,
            ChArUcoBoard,
        }

        public enum NumberOfBoardSizeWidth
        {
            W_1 = 1,
            W_2,
            W_3,
            W_4,
            W_5,
            W_6,
            W_7,
            W_8,
            W_9,
            W_10,
            W_11,
            W_12,
            W_13,
            W_14,
            W_15,
        }

        public enum NumberOfBoardSizeHeight
        {
            H_1 = 1,
            H_2,
            H_3,
            H_4,
            H_5,
            H_6,
            H_7,
            H_8,
            H_9,
            H_10,
            H_11,
            H_12,
            H_13,
            H_14,
            H_15,
        }

        public enum ArUcoDictionary
        {
            DICT_4X4_50 = Objdetect.DICT_4X4_50,
            DICT_4X4_100 = Objdetect.DICT_4X4_100,
            DICT_4X4_250 = Objdetect.DICT_4X4_250,
            DICT_4X4_1000 = Objdetect.DICT_4X4_1000,
            DICT_5X5_50 = Objdetect.DICT_5X5_50,
            DICT_5X5_100 = Objdetect.DICT_5X5_100,
            DICT_5X5_250 = Objdetect.DICT_5X5_250,
            DICT_5X5_1000 = Objdetect.DICT_5X5_1000,
            DICT_6X6_50 = Objdetect.DICT_6X6_50,
            DICT_6X6_100 = Objdetect.DICT_6X6_100,
            DICT_6X6_250 = Objdetect.DICT_6X6_250,
            DICT_6X6_1000 = Objdetect.DICT_6X6_1000,
            DICT_7X7_50 = Objdetect.DICT_7X7_50,
            DICT_7X7_100 = Objdetect.DICT_7X7_100,
            DICT_7X7_250 = Objdetect.DICT_7X7_250,
            DICT_7X7_1000 = Objdetect.DICT_7X7_1000,
            DICT_ARUCO_ORIGINAL = Objdetect.DICT_ARUCO_ORIGINAL,
        }
    }
}