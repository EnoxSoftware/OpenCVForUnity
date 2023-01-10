#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.ObjdetectModule;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// ArUco WebCam Example
    /// An example of marker-based AR view and camera pose estimation using the aruco (ArUco Marker Detection) module.
    /// Referring to https://github.com/opencv/opencv_contrib/blob/master/modules/aruco/samples/detect_markers.cpp.
    /// http://docs.opencv.org/3.1.0/d5/dae/tutorial_aruco_detection.html
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class ArUcoWebCamExample : MonoBehaviour
    {

        /// <summary>
        /// The marker type.
        /// </summary>
        public MarkerType markerType = MarkerType.CanonicalMarker;

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
        /// Determines if restores the camera parameters when the file exists.
        /// </summary>
        public bool useStoredCameraParameters = false;

        /// <summary>
        /// The toggle for switching to use the stored camera parameters.
        /// </summary>
        public Toggle useStoredCameraParametersToggle;

        /// <summary>
        /// Determines if shows rejected corners.
        /// </summary>
        public bool showRejectedCorners = false;

        /// <summary>
        /// The shows rejected corners toggle.
        /// </summary>
        public Toggle showRejectedCornersToggle;

        /// <summary>
        /// Determines if applied the pose estimation.
        /// </summary>
        public bool applyEstimationPose = true;

        /// <summary>
        /// Determines if refine marker detection. (only valid for ArUco boards)
        /// </summary>
        public bool refineMarkerDetection = true;

        /// <summary>
        /// The shows refine marker detection toggle.
        /// </summary>
        public Toggle refineMarkerDetectionToggle;

        [Space(10)]

        /// <summary>
        /// The length of the markers' side. Normally, unit is meters.
        /// </summary>
        public float markerLength = 0.1f;

        /// <summary>
        /// The AR game object.
        /// </summary>
        public GameObject arGameObject;

        /// <summary>
        /// The AR camera.
        /// </summary>
        public Camera arCamera;

        [Space(10)]

        /// <summary>
        /// Determines if request the AR camera moving.
        /// </summary>
        public bool shouldMoveARCamera = false;

        [Space(10)]

        /// <summary>
        /// Determines if enable low pass filter.
        /// </summary>
        public bool enableLowPassFilter;

        /// <summary>
        /// The enable low pass filter toggle.
        /// </summary>
        public Toggle enableLowPassFilterToggle;

        /// <summary>
        /// The position low pass. (Value in meters)
        /// </summary>
        public float positionLowPass = 0.005f;

        /// <summary>
        /// The rotation low pass. (Value in degrees)
        /// </summary>
        public float rotationLowPass = 2f;

        /// <summary>
        /// The old pose data.
        /// </summary>
        PoseData oldPoseData;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        Mat rgbMat;

        /// <summary>
        /// The cameraparam matrix.
        /// </summary>
        Mat camMatrix;

        /// <summary>
        /// The distortion coeffs.
        /// </summary>
        MatOfDouble distCoeffs;

        /// <summary>
        /// The transformation matrix for AR.
        /// </summary>
        Matrix4x4 ARM;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        // for CanonicalMarker.
        Mat ids;
        List<Mat> corners;
        List<Mat> rejectedCorners;
        Mat rotMat;
        Dictionary dictionary;
        Mat recoveredIdxs;
        ArucoDetector arucoDetector;

        // for GridBoard.
        // number of markers in X direction
        const int gridBoradMarkersX = 5;
        // number of markers in Y direction
        const int gridBoradMarkersY = 7;
        // marker side length (normally in meters)
        const float gridBoradMarkerLength = 0.04f;
        // separation between two markers (same unit as markerLength)
        const float gridBoradMarkerSeparation = 0.01f;
        // id of first marker in dictionary to use on board.
        const int gridBoradMarkerFirstMarker = 0;
        GridBoard gridBoard;

        // for ChArUcoBoard.
        //  number of chessboard squares in X direction
        const int chArUcoBoradSquaresX = 5;
        //  number of chessboard squares in Y direction
        const int chArUcoBoradSquaresY = 7;
        // chessboard square side length (normally in meters)
        const float chArUcoBoradSquareLength = 0.04f;
        // marker side length (same unit than squareLength)
        const float chArUcoBoradMarkerLength = 0.02f;
        const int charucoMinMarkers = 2;
        Mat charucoCorners;
        Mat charucoIds;
        CharucoBoard charucoBoard;
        CharucoDetector charucoDetector;

        // for ChArUcoDiamondMarker.
        // size of the chessboard squares in pixels
        const float diamondSquareLength = 0.1f;
        // size of the markers in pixels.
        const float diamondMarkerLength = 0.06f;
        // identifiers for diamonds in diamond corners.
        const int diamondId1 = 45;
        const int diamondId2 = 68;
        const int diamondId3 = 28;
        const int diamondId4 = 74;
        List<Mat> diamondCorners;
        Mat diamondIds;
        CharucoBoard charucoDiamondBoard;
        CharucoDetector charucoDiamondDetector;


        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            markerTypeDropdown.value = (int)markerType;
            dictionaryIdDropdown.value = (int)dictionaryId;
            useStoredCameraParametersToggle.isOn = useStoredCameraParameters;
            showRejectedCornersToggle.isOn = showRejectedCorners;
            refineMarkerDetectionToggle.isOn = refineMarkerDetection;
            refineMarkerDetectionToggle.interactable = (markerType == MarkerType.GridBoard || markerType == MarkerType.ChArUcoBoard);
            enableLowPassFilterToggle.isOn = enableLowPassFilter;

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

#if UNITY_ANDROID && !UNITY_EDITOR
            // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
            webCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
#endif
            webCamTextureToMatHelper.Initialize();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(webCamTextureMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", webCamTextureMat.width().ToString());
                fpsMonitor.Add("height", webCamTextureMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }


            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();

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


            // set camera parameters.
            double fx;
            double fy;
            double cx;
            double cy;

            string loadDirectoryPath = Path.Combine(Application.persistentDataPath, "ArUcoCameraCalibrationExample");
            string calibratonDirectoryName = "camera_parameters" + width + "x" + height;
            string loadCalibratonFileDirectoryPath = Path.Combine(loadDirectoryPath, calibratonDirectoryName);
            string loadPath = Path.Combine(loadCalibratonFileDirectoryPath, calibratonDirectoryName + ".xml");
            if (useStoredCameraParameters && File.Exists(loadPath))
            {
                CameraParameters param;
                XmlSerializer serializer = new XmlSerializer(typeof(CameraParameters));
                using (var stream = new FileStream(loadPath, FileMode.Open))
                {
                    param = (CameraParameters)serializer.Deserialize(stream);
                }

                camMatrix = param.GetCameraMatrix();
                distCoeffs = new MatOfDouble(param.GetDistortionCoefficients());

                fx = param.camera_matrix[0];
                fy = param.camera_matrix[4];
                cx = param.camera_matrix[2];
                cy = param.camera_matrix[5];

                Debug.Log("Loaded CameraParameters from a stored XML file.");
                Debug.Log("loadPath: " + loadPath);

            }
            else
            {
                int max_d = (int)Mathf.Max(width, height);
                fx = max_d;
                fy = max_d;
                cx = width / 2.0f;
                cy = height / 2.0f;

                camMatrix = new Mat(3, 3, CvType.CV_64FC1);
                camMatrix.put(0, 0, fx);
                camMatrix.put(0, 1, 0);
                camMatrix.put(0, 2, cx);
                camMatrix.put(1, 0, 0);
                camMatrix.put(1, 1, fy);
                camMatrix.put(1, 2, cy);
                camMatrix.put(2, 0, 0);
                camMatrix.put(2, 1, 0);
                camMatrix.put(2, 2, 1.0f);

                distCoeffs = new MatOfDouble(0, 0, 0, 0);

                Debug.Log("Created a dummy CameraParameters.");
            }

            Debug.Log("camMatrix " + camMatrix.dump());
            Debug.Log("distCoeffs " + distCoeffs.dump());


            // calibration camera matrix values.
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


            // To convert the difference of the FOV value of the OpenCV and Unity. 
            double fovXScale = (2.0 * Mathf.Atan((float)(imageSize.width / (2.0 * fx)))) / (Mathf.Atan2((float)cx, (float)fx) + Mathf.Atan2((float)(imageSize.width - cx), (float)fx));
            double fovYScale = (2.0 * Mathf.Atan((float)(imageSize.height / (2.0 * fy)))) / (Mathf.Atan2((float)cy, (float)fy) + Mathf.Atan2((float)(imageSize.height - cy), (float)fy));

            Debug.Log("fovXScale " + fovXScale);
            Debug.Log("fovYScale " + fovYScale);


            // Adjust Unity Camera FOV https://github.com/opencv/opencv/commit/8ed1945ccd52501f5ab22bdec6aa1f91f1e2cfd4
            if (widthScale < heightScale)
            {
                arCamera.fieldOfView = (float)(fovx[0] * fovXScale);
            }
            else
            {
                arCamera.fieldOfView = (float)(fovy[0] * fovYScale);
            }
            // Display objects near the camera.
            arCamera.nearClipPlane = 0.01f;


            rgbMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC3);
            ids = new Mat();
            corners = new List<Mat>();
            rejectedCorners = new List<Mat>();
            rotMat = new Mat(3, 3, CvType.CV_64FC1);
            dictionary = Objdetect.getPredefinedDictionary((int)dictionaryId);
            recoveredIdxs = new Mat();

            DetectorParameters detectorParams = new DetectorParameters();
            detectorParams.set_useAruco3Detection(true);
            RefineParameters refineParameters = new RefineParameters(10f, 3f, true);
            arucoDetector = new ArucoDetector(dictionary, detectorParams, refineParameters);


            gridBoard = new GridBoard(new Size(gridBoradMarkersX, gridBoradMarkersY), gridBoradMarkerLength, gridBoradMarkerSeparation, dictionary);


            charucoCorners = new Mat();
            charucoIds = new Mat();
            charucoBoard = new CharucoBoard(new Size(chArUcoBoradSquaresX, chArUcoBoradSquaresY), chArUcoBoradSquareLength, chArUcoBoradMarkerLength, dictionary);

            charucoDetector = new CharucoDetector(charucoBoard);
            CharucoParameters charucoParameters = charucoDetector.getCharucoParameters();
            charucoParameters.set_cameraMatrix(camMatrix);
            charucoParameters.set_distCoeffs(distCoeffs);
            charucoParameters.set_minMarkers(charucoMinMarkers);
            charucoDetector.setCharucoParameters(charucoParameters);
            charucoDetector.setDetectorParameters(detectorParams);
            charucoDetector.setRefineParameters(refineParameters);


            diamondCorners = new List<Mat>();
            diamondIds = new Mat(1, 1, CvType.CV_32SC4);
            diamondIds.put(0, 0, new int[] { diamondId1, diamondId2, diamondId3, diamondId4 });
            charucoDiamondBoard = new CharucoBoard(new Size(chArUcoBoradSquaresX, chArUcoBoradSquaresY), diamondSquareLength, diamondMarkerLength, dictionary);

            charucoDiamondDetector = new CharucoDetector(charucoDiamondBoard);
            CharucoParameters charucoDiamondParameters = charucoDiamondDetector.getCharucoParameters();
            charucoDiamondParameters.set_cameraMatrix(camMatrix);
            charucoDiamondParameters.set_distCoeffs(distCoeffs);
            charucoDiamondParameters.set_minMarkers(charucoMinMarkers);
            charucoDiamondDetector.setCharucoParameters(charucoDiamondParameters);
            charucoDiamondDetector.setDetectorParameters(detectorParams);
            charucoDiamondDetector.setRefineParameters(refineParameters);


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

            if (rgbMat != null)
                rgbMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

            if (arucoDetector != null)
                arucoDetector.Dispose();
            if (charucoDetector != null)
                charucoDetector.Dispose();
            if (charucoDiamondDetector != null)
                charucoDiamondDetector.Dispose();

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

            if (rotMat != null)
                rotMat.Dispose();

            if (recoveredIdxs != null)
                recoveredIdxs.Dispose();

            if (gridBoard != null)
                gridBoard.Dispose();

            if (charucoCorners != null)
                charucoCorners.Dispose();
            if (charucoIds != null)
                charucoIds.Dispose();
            if (charucoBoard != null)
                charucoBoard.Dispose();

            foreach (var item in diamondCorners)
            {
                item.Dispose();
            }
            diamondCorners.Clear();
            if (diamondIds != null)
                diamondIds.Dispose();

            if (charucoDiamondBoard != null)
                charucoDiamondBoard.Dispose();
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
            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);


                Calib3d.undistort(rgbMat, rgbMat, camMatrix, distCoeffs);
                arucoDetector.detectMarkers(rgbMat, corners, ids, rejectedCorners);

                switch (markerType)
                {
                    default:
                    case MarkerType.CanonicalMarker:

                        // If at least one marker detected
                        if (ids.total() > 0)
                        {
                            Objdetect.drawDetectedMarkers(rgbMat, corners, ids, new Scalar(0, 255, 0));

                            if (applyEstimationPose)
                                EstimatePoseCanonicalMarker(rgbMat);
                        }
                        break;

                    case MarkerType.GridBoard:

                        if (refineMarkerDetection)
                            arucoDetector.refineDetectedMarkers(rgbMat, gridBoard, corners, ids, rejectedCorners, camMatrix, distCoeffs, recoveredIdxs);

                        // If at least one marker detected
                        if (ids.total() > 0)
                        {
                            Objdetect.drawDetectedMarkers(rgbMat, corners, ids, new Scalar(0, 255, 0));

                            if (applyEstimationPose)
                                EstimatePoseGridBoard(rgbMat);
                        }
                        break;

                    case MarkerType.ChArUcoBoard:

                        if (refineMarkerDetection)
                            arucoDetector.refineDetectedMarkers(rgbMat, charucoBoard, corners, ids, rejectedCorners, camMatrix, distCoeffs, recoveredIdxs);

                        // If at least one marker detected
                        if (ids.total() > 0)
                        {
                            //charucoDetector.detectBoard(rgbMat, charucoCorners, charucoIds, corners, ids); // error
                            charucoDetector.detectBoard(rgbMat, charucoCorners, charucoIds);

                            Objdetect.drawDetectedMarkers(rgbMat, corners, ids, new Scalar(0, 255, 0));

                            // if at least one charuco corner detected
                            if (charucoIds.total() > 0)
                            {
                                Objdetect.drawDetectedCornersCharuco(rgbMat, charucoCorners, charucoIds, new Scalar(0, 0, 255));

                                if (applyEstimationPose)
                                    EstimatePoseChArUcoBoard(rgbMat);
                            }
                        }
                        break;

                    case MarkerType.ChArUcoDiamondMarker:
                        /*

                        // If at least one marker detected
                        if (ids.total() > 0)
                        {
                            //charucoDiamondDetector.detectDiamonds(rgbMat, diamondCorners, diamondIds, corners, ids); // error
                            //charucoDiamondDetector.detectDiamonds(rgbMat, diamondCorners, diamondIds, corners); // error
                            //charucoDiamondDetector.detectDiamonds(rgbMat, diamondCorners, diamondIds); // error

                            Objdetect.drawDetectedMarkers(rgbMat, corners, ids, new Scalar(0, 255, 0));

                            // If at least one diamonds detected
                            if (diamondIds.total() > 0)
                            {
                                Objdetect.drawDetectedDiamonds(rgbMat, diamondCorners, diamondIds, new Scalar(0, 0, 255));

                                if (applyEstimationPose)
                                    EstimatePoseChArUcoDiamondMarker(rgbMat);
                            }
                        }
                        */
                        break;
                }

                //

                if (showRejectedCorners && rejectedCorners.Count > 0)
                    Objdetect.drawDetectedMarkers(rgbMat, rejectedCorners, new Mat(), new Scalar(255, 0, 0));


                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Imgproc.cvtColor(rgbMat, rgbaMat, Imgproc.COLOR_RGB2RGBA);

                Utils.matToTexture2D(rgbaMat, texture);
            }
        }

        private void EstimatePoseCanonicalMarker(Mat rgbMat)
        {
            using (MatOfPoint3f objPoints = new MatOfPoint3f(
                new Point3(-markerLength / 2f, markerLength / 2f, 0),
                new Point3(markerLength / 2f, markerLength / 2f, 0),
                new Point3(markerLength / 2f, -markerLength / 2f, 0),
                new Point3(-markerLength / 2f, -markerLength / 2f, 0)
                ))
            {
                for (int i = 0; i < ids.total(); i++)
                {
                    using (Mat rvec = new Mat(1, 1, CvType.CV_64FC3))
                    using (Mat tvec = new Mat(1, 1, CvType.CV_64FC3))
                    using (Mat corner_4x1 = corners[i].reshape(2, 4)) // 1*4*CV_32FC2 => 4*1*CV_32FC2
                    using (MatOfPoint2f imagePoints = new MatOfPoint2f(corner_4x1))
                    {
                        // Calculate pose for each marker
                        Calib3d.solvePnP(objPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);

                        // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                        Calib3d.drawFrameAxes(rgbMat, camMatrix, distCoeffs, rvec, tvec, markerLength * 0.5f);

                        // This example can display the ARObject on only first detected marker.
                        if (i == 0)
                        {
                            UpdateARObjectTransform(rvec, tvec);
                        }
                    }
                }
            }
        }

        private void EstimatePoseGridBoard(Mat rgbMat)
        {
            using (Mat rvec = new Mat(1, 1, CvType.CV_64FC3))
            using (Mat tvec = new Mat(1, 1, CvType.CV_64FC3))
            using (Mat objPoints = new Mat())
            using (Mat imgPoints = new Mat())
            {
                // Get object and image points for the solvePnP function
                gridBoard.matchImagePoints(corners, ids, objPoints, imgPoints);

                // Find pose
                MatOfPoint3f objPoints_p3f = new MatOfPoint3f(objPoints);
                MatOfPoint2f imgPoints_p3f = new MatOfPoint2f(imgPoints);
                Calib3d.solvePnP(objPoints_p3f, imgPoints_p3f, camMatrix, distCoeffs, rvec, tvec);

                // If at least one board marker detected
                int markersOfBoardDetected = (int)objPoints.total() / 4;
                if (markersOfBoardDetected > 0)
                {
                    // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                    Calib3d.drawFrameAxes(rgbMat, camMatrix, distCoeffs, rvec, tvec, markerLength * 0.5f);

                    UpdateARObjectTransform(rvec, tvec);
                }
            }
        }

        private void EstimatePoseChArUcoBoard(Mat rgbMat)
        {
            /*
            // if at least one charuco corner detected
            if (charucoIds.total() > 0)
            {
                using (Mat rvec = new Mat(1, 1, CvType.CV_64FC3))
                using (Mat tvec = new Mat(1, 1, CvType.CV_64FC3))
                {
                    bool valid = Aruco.estimatePoseCharucoBoard(charucoCorners, charucoIds, charucoBoard, camMatrix, distCoeffs, rvec, tvec); // error

                    // if at least one board marker detected
                    if (valid)
                    {
                        // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                        Calib3d.drawFrameAxes(rgbMat, camMatrix, distCoeffs, rvec, tvec, markerLength * 0.5f);

                        UpdateARObjectTransform(rvec, tvec);
                    }
                }
            }
            */

            using (Mat rvec = new Mat(1, 1, CvType.CV_64FC3))
            using (Mat tvec = new Mat(1, 1, CvType.CV_64FC3))
            using (Mat objPoints = new Mat())
            using (Mat imgPoints = new Mat())
            {
                // Get object and image points for the solvePnP function
                charucoBoard.matchImagePoints(corners, ids, objPoints, imgPoints);

                // Find pose
                MatOfPoint3f objPoints_p3f = new MatOfPoint3f(objPoints);
                MatOfPoint2f imgPoints_p3f = new MatOfPoint2f(imgPoints);
                Calib3d.solvePnP(objPoints_p3f, imgPoints_p3f, camMatrix, distCoeffs, rvec, tvec);

                // If at least one board marker detected
                int markersOfBoardDetected = (int)objPoints.total() / 4;
                if (markersOfBoardDetected > 0)
                {
                    // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                    Calib3d.drawFrameAxes(rgbMat, camMatrix, distCoeffs, rvec, tvec, markerLength * 0.5f);

                    UpdateARObjectTransform(rvec, tvec);
                }
            }
        }

        /*
        private void EstimatePoseChArUcoDiamondMarker(Mat rgbMat)
        {
            for (int i = 0; i < diamondIds.total(); i++)
            {
                using (Mat rvec = new Mat(1, 1, CvType.CV_64FC3))
                using (Mat tvec = new Mat(1, 1, CvType.CV_64FC3))
                using (Mat objPoints = new Mat())
                using (Mat imgPoints = new Mat())
                {
                    // Get object and image points for the solvePnP function
                    charucoDiamondBoard.matchImagePoints(diamondCorners, diamondIds, objPoints, imgPoints);

                    // Find pose
                    MatOfPoint3f objPoints_p3f = new MatOfPoint3f(objPoints);
                    MatOfPoint2f imgPoints_p3f = new MatOfPoint2f(imgPoints);
                    Calib3d.solvePnP(objPoints_p3f, imgPoints_p3f, camMatrix, distCoeffs, rvec, tvec);

                    // If at least one board marker detected
                    int markersOfBoardDetected = (int)objPoints.total() / 4;
                    if (markersOfBoardDetected > 0)
                    {
                        // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                        Calib3d.drawFrameAxes(rgbMat, camMatrix, distCoeffs, rvec, tvec, diamondSquareLength * 0.5f);

                        // This example can display the ARObject on only first detected marker.
                        if (i == 0)
                        {
                            UpdateARObjectTransform(rvec, tvec);
                        }
                    }
                }
            }
        }
        */

        private void UpdateARObjectTransform(Mat rvec, Mat tvec)
        {
            // Convert to unity pose data.
            double[] rvecArr = new double[3];
            rvec.get(0, 0, rvecArr);
            double[] tvecArr = new double[3];
            tvec.get(0, 0, tvecArr);
            PoseData poseData = ARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr);

            // Changes in pos/rot below these thresholds are ignored.
            if (enableLowPassFilter)
            {
                ARUtils.LowpassPoseData(ref oldPoseData, ref poseData, positionLowPass, rotationLowPass);
            }
            oldPoseData = poseData;

            // Convert to transform matrix.
            ARM = ARUtils.ConvertPoseDataToMatrix(ref poseData, true);

            if (shouldMoveARCamera)
            {

                ARM = arGameObject.transform.localToWorldMatrix * ARM.inverse;

                ARUtils.SetTransformFromMatrix(arCamera.transform, ref ARM);

            }
            else
            {

                ARM = arCamera.transform.localToWorldMatrix * ARM;

                ARUtils.SetTransformFromMatrix(arGameObject.transform, ref ARM);
            }
        }

        private void ResetObjectTransform()
        {
            // reset AR object transform.
            Matrix4x4 i = Matrix4x4.identity;
            ARUtils.SetTransformFromMatrix(arCamera.transform, ref i);
            ARUtils.SetTransformFromMatrix(arGameObject.transform, ref i);
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            webCamTextureToMatHelper.Dispose();
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
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
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

                refineMarkerDetectionToggle.interactable = (markerType == MarkerType.GridBoard || markerType == MarkerType.ChArUcoBoard);

                ResetObjectTransform();

                if (webCamTextureToMatHelper.IsInitialized())
                    webCamTextureToMatHelper.Initialize();
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

                ResetObjectTransform();

                if (webCamTextureToMatHelper.IsInitialized())
                    webCamTextureToMatHelper.Initialize();
            }
        }

        /// <summary>
        /// Raises the use stored camera parameters toggle value changed event.
        /// </summary>
        public void OnUseStoredCameraParametersToggleValueChanged()
        {
            if (useStoredCameraParameters != useStoredCameraParametersToggle.isOn)
            {
                useStoredCameraParameters = useStoredCameraParametersToggle.isOn;

                if (webCamTextureToMatHelper != null && webCamTextureToMatHelper.IsInitialized())
                    webCamTextureToMatHelper.Initialize();
            }
        }

        /// <summary>
        /// Raises the show rejected corners toggle value changed event.
        /// </summary>
        public void OnShowRejectedCornersToggleValueChanged()
        {
            showRejectedCorners = showRejectedCornersToggle.isOn;
        }

        /// <summary>
        /// Raises the refine marker detection toggle value changed event.
        /// </summary>
        public void OnRefineMarkerDetectionToggleValueChanged()
        {
            refineMarkerDetection = refineMarkerDetectionToggle.isOn;
        }


        /// <summary>
        /// Raises the enable low pass filter toggle value changed event.
        /// </summary>
        public void OnEnableLowPassFilterToggleValueChanged()
        {
            enableLowPassFilter = enableLowPassFilterToggle.isOn;
        }

        public enum MarkerType
        {
            CanonicalMarker,
            GridBoard,
            ChArUcoBoard,
            ChArUcoDiamondMarker
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

#endif