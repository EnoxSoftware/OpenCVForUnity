using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.AR;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// ArUco Example
    /// An example of marker-based AR view and camera pose estimation using the objdetect and aruco module.
    /// Referring to https://github.com/opencv/opencv_contrib/blob/4.x/modules/aruco/samples/detect_markers.cpp
    /// http://docs.opencv.org/3.1.0/d5/dae/tutorial_aruco_detection.html
    /// https://github.com/opencv/opencv/blob/4.x/modules/objdetect/test/test_arucodetection.cpp
    /// https://github.com/opencv/opencv/blob/4.x/modules/objdetect/test/test_boarddetection.cpp
    /// https://github.com/opencv/opencv/blob/4.x/modules/objdetect/test/test_charucodetection.cpp
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class ArUcoExample : MonoBehaviour
    {
        // Enums
        /// <summary>
        /// Marker type enum
        /// </summary>
        public enum MarkerType
        {
            CanonicalMarker,
            GridBoard,
            ChArUcoBoard,
            ChArUcoDiamondMarker
        }

        /// <summary>
        /// ArUco dictionary enum
        /// </summary>
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

        // Public Fields
        public Dropdown MarkerTypeDropdown;
        public MarkerType SelectedMarkerType = MarkerType.CanonicalMarker;
        public Dropdown DictionaryIdDropdown;
        public ArUcoDictionary DictionaryId = ArUcoDictionary.DICT_6X6_250;
        public Toggle UseStoredCameraParametersToggle;
        public bool UseStoredCameraParameters = false;
        public Toggle ShowDetectedMarkersToggle;
        public bool ShowDetectedMarkers = false;
        public Toggle ShowRejectedCornersToggle;
        public bool ShowRejectedCorners = false;
        public Toggle RefineMarkerDetectionToggle;
        public bool RefineMarkerDetection = true;
        public Toggle EnableLowPassFilterToggle;
        public bool EnableLowPassFilter = false;
        public Toggle DebugDrawFrameAxesToggle;
        public bool IsDebugDrawFrameAxes = false;

        public bool ApplyEstimationPose = true;

        [Space(10)]

        [Tooltip("The length of the markers' side. Normally, unit is meters.")]
        public float MarkerLength = 0.1f;

        public ARHelper ArHelper;
        public GameObject ArCubePrefab;

        // Private Fields
        private Texture2D _texture;
        private MultiSource2MatHelper _multiSource2MatHelper;
        private Mat _rgbMat;
        private Mat _undistortedRgbMat;
        private Mat _camMatrix;
        private MatOfDouble _distCoeffs;
        private FpsMonitor _fpsMonitor;

        // for CanonicalMarker.
        private Mat _ids;
        private List<Mat> _corners;
        private List<Mat> _rejectedCorners;
        private Dictionary _dictionary;
        private Mat _recoveredIdxs;
        private ArucoDetector _arucoDetector;

        // for GridBoard.
        // Constants
        // number of markers in X direction
        private const int GRID_BOARD_MARKERS_X = 5;
        // number of markers in Y direction
        private const int GRID_BOARD_MARKERS_Y = 7;
        // marker side length (normally in meters)
        private const float GRID_BOARD_MARKER_LENGTH = 0.04f;
        // separation between two markers (same unit as markerLength)
        private const float GRID_BOARD_MARKER_SEPARATION = 0.01f;
        // Private Fields
        private GridBoard _gridBoard;

        // for ChArUcoBoard.
        // Constants
        //  number of chessboard squares in X direction
        private const int CHARUCO_BOARD_SQUARES_X = 5;
        //  number of chessboard squares in Y direction
        private const int CHARUCO_BOARD_SQUARES_Y = 7;
        // chessboard square side length (normally in meters)
        private const float CHARUCO_BOARD_SQUARE_LENGTH = 0.04f;
        // marker side length (same unit than squareLength)
        private const float CHARUCO_BOARD_MARKER_LENGTH = 0.02f;
        private const int CHARUCO_MIN_MARKERS = 2;
        // Private Fields
        private Mat _charucoCorners;
        private Mat _charucoIds;
        private CharucoBoard _charucoBoard;
        private CharucoDetector _charucoDetector;

        // for ChArUcoDiamondMarker.
        // Constants
        // size of the diamond squares in pixels
        private const float DIAMOND_SQUARE_LENGTH = 0.1f;
        // size of the markers in pixels.
        private const float DIAMOND_MARKER_LENGTH = 0.06f;
        // Private Fields
        private List<Mat> _diamondCorners;
        private Mat _diamondIds;
        private CharucoBoard _charucoDiamondBoard;
        private CharucoDetector _charucoDiamondDetector;

        private Dictionary<ArUcoIdentifier, ARGameObject> _arGameObjectCache = new Dictionary<ArUcoIdentifier, ARGameObject>();

        // Unity Lifecycle Methods
        private void Start()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;
            _multiSource2MatHelper.Initialize();

            // Update GUI state
            MarkerTypeDropdown.value = (int)SelectedMarkerType;
            DictionaryIdDropdown.value = (int)DictionaryId;
            UseStoredCameraParametersToggle.isOn = UseStoredCameraParameters;
            ShowRejectedCornersToggle.isOn = ShowRejectedCorners;
            RefineMarkerDetectionToggle.isOn = RefineMarkerDetection;
            RefineMarkerDetectionToggle.interactable = (SelectedMarkerType == MarkerType.GridBoard || SelectedMarkerType == MarkerType.ChArUcoBoard);
            EnableLowPassFilterToggle.isOn = EnableLowPassFilter;
            DebugDrawFrameAxesToggle.isOn = IsDebugDrawFrameAxes;
        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                Imgproc.cvtColor(rgbaMat, _rgbMat, Imgproc.COLOR_RGBA2RGB);

                // Reset ARGameObjects ImagePoints and ObjectPoints.
                if (ApplyEstimationPose)
                    ArHelper.ResetARGameObjectsImagePointsAndObjectPoints();

                switch (SelectedMarkerType)
                {
                    default:
                    case MarkerType.CanonicalMarker:

                        Calib3d.undistort(_rgbMat, _undistortedRgbMat, _camMatrix, _distCoeffs);
                        _arucoDetector.detectMarkers(_undistortedRgbMat, _corners, _ids, _rejectedCorners);

                        if (ShowDetectedMarkers && _corners.Count == _ids.total() || _ids.total() == 0)
                            Objdetect.drawDetectedMarkers(_undistortedRgbMat, _corners, _ids, new Scalar(0, 255, 0));

                        if (ApplyEstimationPose)
                        {
                            // If at least one marker detected
                            if (_ids.total() > 0)
                                EstimatePoseCanonicalMarker(_undistortedRgbMat, _corners, _ids);
                        }

                        break;

                    case MarkerType.GridBoard:

                        Calib3d.undistort(_rgbMat, _undistortedRgbMat, _camMatrix, _distCoeffs);
                        _arucoDetector.detectMarkers(_undistortedRgbMat, _corners, _ids, _rejectedCorners);

                        if (RefineMarkerDetection)
                            _arucoDetector.refineDetectedMarkers(_undistortedRgbMat, _gridBoard, _corners, _ids, _rejectedCorners, _camMatrix, _distCoeffs, _recoveredIdxs);

                        if (ShowDetectedMarkers && _corners.Count == _ids.total() || _ids.total() == 0)
                            Objdetect.drawDetectedMarkers(_undistortedRgbMat, _corners, _ids, new Scalar(0, 255, 0));

                        if (ApplyEstimationPose)
                        {
                            // If at least one marker detected
                            if (_ids.total() > 0)
                                EstimatePoseGridBoard(_undistortedRgbMat, _corners, _ids);
                        }

                        break;

                    case MarkerType.ChArUcoBoard:

                        Calib3d.undistort(_rgbMat, _undistortedRgbMat, _camMatrix, _distCoeffs);

                        _ids = new Mat();
                        _corners = new List<Mat>();

                        _charucoDetector.detectBoard(_undistortedRgbMat, _charucoCorners, _charucoIds, _corners, _ids); // error

                        if (ShowDetectedMarkers && _corners.Count == _ids.total() || _ids.total() == 0)
                            Objdetect.drawDetectedMarkers(_undistortedRgbMat, _corners, _ids, new Scalar(0, 255, 0));

                        if ((_charucoCorners.total() == _charucoIds.total() || _charucoIds.total() != 0) && !_charucoCorners.empty())
                            Objdetect.drawDetectedCornersCharuco(_undistortedRgbMat, _charucoCorners, _charucoIds, new Scalar(0, 0, 255));

                        if (ApplyEstimationPose)
                        {
                            // if at least one charuco corner detected
                            if (_charucoIds.total() > 0)
                                EstimatePoseChArUcoBoard(_undistortedRgbMat, _charucoCorners, _charucoIds);
                        }

                        break;

                    case MarkerType.ChArUcoDiamondMarker:

                        Calib3d.undistort(_rgbMat, _undistortedRgbMat, _camMatrix, _distCoeffs);

                        _ids = new Mat();
                        _corners = new List<Mat>();
                        _charucoDiamondDetector.detectDiamonds(_undistortedRgbMat, _diamondCorners, _diamondIds, _corners, _ids);

                        if (ShowDetectedMarkers && _corners.Count == _ids.total() || _ids.total() == 0)
                            Objdetect.drawDetectedMarkers(_undistortedRgbMat, _corners, _ids, new Scalar(0, 255, 0));

                        if (_diamondCorners.Count == _diamondIds.total() || _diamondIds.total() == 0)
                            Objdetect.drawDetectedDiamonds(_undistortedRgbMat, _diamondCorners, _diamondIds, new Scalar(0, 0, 255));

                        if (ApplyEstimationPose)
                        {
                            // If at least one diamonds detected
                            if (_diamondIds.total() > 0)
                                EstimatePoseChArUcoDiamondMarker(_undistortedRgbMat, _diamondCorners, _diamondIds);
                        }

                        break;
                }

                if (ShowRejectedCorners && _rejectedCorners.Count > 0)
                    Objdetect.drawDetectedMarkers(_undistortedRgbMat, _rejectedCorners, new Mat(), new Scalar(255, 0, 0));

                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Imgproc.cvtColor(_undistortedRgbMat, rgbaMat, Imgproc.COLOR_RGB2RGBA);

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);

                foreach (var item in _corners) item.Dispose(); _corners.Clear();
                foreach (var item in _rejectedCorners) item.Dispose(); _rejectedCorners.Clear();
                foreach (var item in _diamondCorners) item.Dispose(); _diamondCorners.Clear();
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            OpenCVDebug.SetDebugMode(false);
        }

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = _multiSource2MatHelper.GetMat();

            _texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);

            // Set the Texture2D as the main texture of the Renderer component attached to the game object
            gameObject.GetComponent<Renderer>().material.mainTexture = _texture;

            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


            // Set the camera's orthographicSize to half of the texture height
            Camera.main.orthographicSize = _texture.height / 2f;

            // Get the camera's aspect ratio
            float cameraAspect = Camera.main.aspect;

            // Get the texture's aspect ratio
            float textureAspect = (float)_texture.width / _texture.height;

            // Calculate imageSizeScale
            float imageSizeScale;
            if (textureAspect > cameraAspect)
            {
                // Calculate the camera width (height is already fixed)
                float cameraWidth = Camera.main.orthographicSize * 2f * cameraAspect;

                // Scale so that the texture width fits within the camera width
                imageSizeScale = cameraWidth / _texture.width;
            }
            else
            {
                // Scale so that the texture height fits within the camera height
                imageSizeScale = 1f; // No scaling needed since height is already fixed
            }
            Debug.Log("imageSizeScale " + imageSizeScale);

            // The calculated imageSizeScale is used to set the scale of the game object on which the texture is displayed.
            transform.localScale = new Vector3(_texture.width * imageSizeScale, _texture.height * imageSizeScale, 1);


            // set camera parameters.
            double fx;
            double fy;
            double cx;
            double cy;

            string loadDirectoryPath = Path.Combine(Application.persistentDataPath, "ArUcoCameraCalibrationExample");
            string calibratonDirectoryName = "camera_parameters" + rgbaMat.width() + "x" + rgbaMat.height();
            string loadCalibratonFileDirectoryPath = Path.Combine(loadDirectoryPath, calibratonDirectoryName);
            string loadPath = Path.Combine(loadCalibratonFileDirectoryPath, calibratonDirectoryName + ".xml");
            if (UseStoredCameraParameters && File.Exists(loadPath))
            {
                CameraParameters param;
                XmlSerializer serializer = new XmlSerializer(typeof(CameraParameters));
                using (var stream = new FileStream(loadPath, FileMode.Open))
                {
                    param = (CameraParameters)serializer.Deserialize(stream);
                }

                _camMatrix = param.GetCameraMatrix();
                _distCoeffs = new MatOfDouble(param.GetDistortionCoefficients());

                fx = param.CameraMatrix[0];
                fy = param.CameraMatrix[4];
                cx = param.CameraMatrix[2];
                cy = param.CameraMatrix[5];

                Debug.Log("Loaded CameraParameters from a stored XML file.");
                Debug.Log("loadPath: " + loadPath);

            }
            else
            {
                int max_d = (int)Mathf.Max(rgbaMat.width(), rgbaMat.height());
                fx = max_d;
                fy = max_d;
                cx = rgbaMat.width() / 2.0f;
                cy = rgbaMat.height() / 2.0f;

                _camMatrix = new Mat(3, 3, CvType.CV_64FC1);
                _camMatrix.put(0, 0, fx);
                _camMatrix.put(0, 1, 0);
                _camMatrix.put(0, 2, cx);
                _camMatrix.put(1, 0, 0);
                _camMatrix.put(1, 1, fy);
                _camMatrix.put(1, 2, cy);
                _camMatrix.put(2, 0, 0);
                _camMatrix.put(2, 1, 0);
                _camMatrix.put(2, 2, 1.0f);

                _distCoeffs = new MatOfDouble(0, 0, 0, 0);

                Debug.Log("Created a dummy CameraParameters.");
            }

            Debug.Log("camMatrix " + _camMatrix.dump());
            Debug.Log("distCoeffs " + _distCoeffs.dump());


            _rgbMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
            _undistortedRgbMat = new Mat();
            _ids = new Mat();
            _corners = new List<Mat>();
            _rejectedCorners = new List<Mat>();
            _dictionary = Objdetect.getPredefinedDictionary((int)DictionaryId);
            _recoveredIdxs = new Mat();

            DetectorParameters detectorParams = new DetectorParameters();
            detectorParams.set_minDistanceToBorder(3);
            detectorParams.set_useAruco3Detection(true);
            detectorParams.set_cornerRefinementMethod(Objdetect.CORNER_REFINE_SUBPIX);
            detectorParams.set_minSideLengthCanonicalImg(16);
            detectorParams.set_errorCorrectionRate(0.8);
            RefineParameters refineParameters = new RefineParameters(10f, 3f, true);
            _arucoDetector = new ArucoDetector(_dictionary, detectorParams, refineParameters);


            _gridBoard = new GridBoard(new Size(GRID_BOARD_MARKERS_X, GRID_BOARD_MARKERS_Y), GRID_BOARD_MARKER_LENGTH, GRID_BOARD_MARKER_SEPARATION, _dictionary);


            _charucoCorners = new Mat();
            _charucoIds = new Mat();
            _charucoBoard = new CharucoBoard(new Size(CHARUCO_BOARD_SQUARES_X, CHARUCO_BOARD_SQUARES_Y), CHARUCO_BOARD_SQUARE_LENGTH, CHARUCO_BOARD_MARKER_LENGTH, _dictionary);
            CharucoParameters charucoParameters = new CharucoParameters();
            charucoParameters.set_cameraMatrix(_camMatrix);
            charucoParameters.set_distCoeffs(_distCoeffs);
            charucoParameters.set_minMarkers(CHARUCO_MIN_MARKERS);
            _charucoDetector = new CharucoDetector(_charucoBoard, charucoParameters, detectorParams, refineParameters);


            _diamondCorners = new List<Mat>();
            _diamondIds = new Mat(1, 1, CvType.CV_32SC4);
            _charucoDiamondBoard = new CharucoBoard(new Size(3, 3), DIAMOND_SQUARE_LENGTH, DIAMOND_MARKER_LENGTH, _dictionary);
            CharucoParameters charucoDiamondParameters = new CharucoParameters();
            charucoDiamondParameters.set_cameraMatrix(_camMatrix);
            charucoDiamondParameters.set_distCoeffs(_distCoeffs);
            charucoDiamondParameters.set_tryRefineMarkers(true);
            _charucoDiamondDetector = new CharucoDetector(_charucoDiamondBoard, charucoDiamondParameters, detectorParams, refineParameters);

#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API
            // If the WebCam is front facing, flip the Mat horizontally. Required for successful detection.
            if (_multiSource2MatHelper.Source2MatHelper is WebCamTexture2MatHelper webCamHelper)
                webCamHelper.FlipHorizontal = webCamHelper.IsFrontFacing();
#endif

            // Initialize ARHelper.
            ArHelper.Initialize();
            // Set ARCamera parameters.
            ArHelper.ARCamera.SetCamMatrix(_camMatrix);
            ArHelper.ARCamera.SetDistCoeffs(_distCoeffs);
            ArHelper.ARCamera.SetARCameraParameters(Screen.width, Screen.height, _rgbMat.width(), _rgbMat.height(), Vector2.zero, new Vector2(imageSizeScale, imageSizeScale));

        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _rgbMat?.Dispose(); _rgbMat = null;
            _undistortedRgbMat?.Dispose(); _undistortedRgbMat = null;
            _camMatrix?.Dispose(); _camMatrix = null;
            _distCoeffs?.Dispose(); _distCoeffs = null;

            _ids?.Dispose(); _ids = null;
            if (_corners != null) foreach (var item in _corners) item.Dispose(); _corners.Clear(); _corners = null;
            if (_rejectedCorners != null) foreach (var item in _rejectedCorners) item.Dispose(); _rejectedCorners.Clear(); _rejectedCorners = null;
            _dictionary?.Dispose(); _dictionary = null;
            _recoveredIdxs?.Dispose(); _recoveredIdxs = null;
            _arucoDetector?.Dispose(); _arucoDetector = null;

            _gridBoard?.Dispose(); _gridBoard = null;

            _charucoCorners?.Dispose(); _charucoCorners = null;
            _charucoIds?.Dispose(); _charucoIds = null;
            _charucoBoard?.Dispose(); _charucoBoard = null;
            _charucoDetector?.Dispose(); _charucoDetector = null;

            if (_diamondCorners != null) foreach (var item in _diamondCorners) item.Dispose(); _diamondCorners.Clear(); _diamondCorners = null;
            _diamondIds?.Dispose(); _diamondIds = null;
            _charucoDiamondBoard?.Dispose(); _charucoDiamondBoard = null;
            _charucoDiamondDetector?.Dispose(); _charucoDiamondDetector = null;

            if (ArHelper != null)
            {
                RemoveAllARGameObject(ArHelper.ARGameObjects);
                ArHelper.Dispose();
            }

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message);
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
            _multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            _multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            _multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            _multiSource2MatHelper.RequestedIsFrontFacing = !_multiSource2MatHelper.RequestedIsFrontFacing;
        }

        /// <summary>
        /// Raises the marker type dropdown value changed event.
        /// </summary>
        public void OnMarkerTypeDropdownValueChanged(int result)
        {
            if ((int)SelectedMarkerType != result)
            {
                SelectedMarkerType = (MarkerType)result;

                RefineMarkerDetectionToggle.interactable = (SelectedMarkerType == MarkerType.GridBoard || SelectedMarkerType == MarkerType.ChArUcoBoard);

                if (_multiSource2MatHelper.IsInitialized())
                    _multiSource2MatHelper.Initialize();
            }
        }

        /// <summary>
        /// Raises the dictionary id dropdown value changed event.
        /// </summary>
        public void OnDictionaryIdDropdownValueChanged(int result)
        {
            if ((int)DictionaryId != result)
            {
                DictionaryId = (ArUcoDictionary)result;
                _dictionary = Objdetect.getPredefinedDictionary((int)DictionaryId);

                if (_multiSource2MatHelper.IsInitialized())
                    _multiSource2MatHelper.Initialize();
            }
        }

        /// <summary>
        /// Raises the use stored camera parameters toggle value changed event.
        /// </summary>
        public void OnUseStoredCameraParametersToggleValueChanged()
        {
            if (UseStoredCameraParameters != UseStoredCameraParametersToggle.isOn)
            {
                UseStoredCameraParameters = UseStoredCameraParametersToggle.isOn;

                if (_multiSource2MatHelper != null && _multiSource2MatHelper.IsInitialized())
                    _multiSource2MatHelper.Initialize();
            }
        }

        /// <summary>
        /// Raises the show detected markers toggle value changed event.
        /// </summary>
        public void OnShowDetectedMarkersToggleValueChanged()
        {
            ShowDetectedMarkers = ShowDetectedMarkersToggle.isOn;
        }

        /// <summary>
        /// Raises the show rejected corners toggle value changed event.
        /// </summary>
        public void OnShowRejectedCornersToggleValueChanged()
        {
            ShowRejectedCorners = ShowRejectedCornersToggle.isOn;
        }

        /// <summary>
        /// Raises the refine marker detection toggle value changed event.
        /// </summary>
        public void OnRefineMarkerDetectionToggleValueChanged()
        {
            RefineMarkerDetection = RefineMarkerDetectionToggle.isOn;
        }

        /// <summary>
        /// Raises the enable low pass filter toggle value changed event.
        /// </summary>
        public void OnEnableLowPassFilterToggleValueChanged()
        {
            EnableLowPassFilter = EnableLowPassFilterToggle.isOn;

            foreach (ARGameObject arGameObject in ArHelper.ARGameObjects)
            {
                arGameObject.UseLowPassFilter = EnableLowPassFilter;
            }
        }

        /// <summary>
        /// Raises the debug draw frame axes toggle value changed event.
        /// </summary>
        public void OnDebugDrawFrameAxesToggleValueChanged()
        {
            IsDebugDrawFrameAxes = DebugDrawFrameAxesToggle.isOn;
        }

        /// <summary>
        /// Called when an ARGameObject enters the ARCamera viewport.
        /// </summary>
        /// <param name="aRHelper"></param>
        /// <param name="arCamera"></param>
        /// <param name="arGameObject"></param>
        public void OnEnterARCameraViewport(ARHelper aRHelper, ARCamera arCamera, ARGameObject arGameObject)
        {
            Debug.Log("OnEnterARCamera arCamera.name " + arCamera.name + " arGameObject.name " + arGameObject.name);

            StartCoroutine(arGameObject.GetComponent<ARCube>().EnterAnimation(arGameObject.gameObject, 0f, 1f, 0.5f));
        }

        /// <summary>
        /// Called when an ARGameObject exits the ARCamera viewport.
        /// </summary>
        /// <param name="aRHelper"></param>
        /// <param name="arCamera"></param>
        /// <param name="arGameObject"></param>
        public void OnExitARCameraViewport(ARHelper aRHelper, ARCamera arCamera, ARGameObject arGameObject)
        {
            Debug.Log("OnExitARCamera arCamera.name " + arCamera.name + " arGameObject.name " + arGameObject.name);

            StartCoroutine(arGameObject.GetComponent<ARCube>().ExitAnimation(arGameObject.gameObject, 1f, 0f, 0.2f));
        }

        /// <summary>
        /// Finds or creates an ARGameObject with the specified AR marker identifier.
        /// </summary>
        /// <param name="arGameObjects"></param>
        /// <param name="arUcoId"></param>
        /// <param name="parentTransform"></param>
        /// <returns></returns>
        private ARGameObject FindOrCreateARGameObject(List<ARGameObject> arGameObjects, ArUcoIdentifier arUcoId, Transform parentTransform)
        {
            ARGameObject FindARGameObjectById(List<ARGameObject> arGameObjects, ArUcoIdentifier id)
            {
                if (_arGameObjectCache.TryGetValue(id, out var cachedObject))
                {
                    return cachedObject;
                }
                return null;
            }

            ARGameObject arGameObject = FindARGameObjectById(arGameObjects, arUcoId);
            if (arGameObject == null)
            {
                arGameObject = Instantiate(ArCubePrefab, parentTransform).GetComponent<ARGameObject>();

                string markerIdsStr = arUcoId.MarkerIds != null ? string.Join(",", arUcoId.MarkerIds) : null;
                string arUcoIdNameStr;
                if (markerIdsStr != null)
                    arUcoIdNameStr = (MarkerType)arUcoId.MarkerType + " " + (ArUcoDictionary)arUcoId.DictionaryId + " [" + markerIdsStr + "]";
                else
                    arUcoIdNameStr = (MarkerType)arUcoId.MarkerType + " " + (ArUcoDictionary)arUcoId.DictionaryId;

                arGameObject.name = arUcoIdNameStr;
                arGameObject.GetComponent<ARCube>().SetInfoPlateTexture(arUcoIdNameStr);
                arGameObject.UseLowPassFilter = EnableLowPassFilter;
                arGameObject.OnEnterARCameraViewport.AddListener(OnEnterARCameraViewport);
                arGameObject.OnExitARCameraViewport.AddListener(OnExitARCameraViewport);
                arGameObject.gameObject.SetActive(false);
                arGameObjects.Add(arGameObject);
                _arGameObjectCache[arUcoId] = arGameObject;
            }
            return arGameObject;
        }

        /// <summary>
        /// Removes all ARGameObjects from the list and destroys them.
        /// </summary>
        /// <param name="arGameObjects"></param>
        private void RemoveAllARGameObject(List<ARGameObject> arGameObjects)
        {
            foreach (ARGameObject arGameObject in arGameObjects)
            {
                Destroy(arGameObject.gameObject);
            }
            arGameObjects.Clear();

            _arGameObjectCache.Clear();
        }

        private void DebugDrawFrameAxes(Mat image, MatOfPoint3f objectPoints, MatOfPoint2f imagePoints, Mat cameraMatrix, MatOfDouble distCoeffs,
                                         float length, int thickness = 3)
        {
            // Calculate rvec and tvec for debug display and draw with Calib3d.drawFrameAxes()
            using (Mat rvec = new Mat(3, 1, CvType.CV_64FC1))
            using (Mat tvec = new Mat(3, 1, CvType.CV_64FC1))
            {
                // Calculate pose
                Calib3d.solvePnP(objectPoints, imagePoints, cameraMatrix, distCoeffs, rvec, tvec);

                // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                OpenCVARUtils.SafeDrawFrameAxes(image, cameraMatrix, distCoeffs, rvec, tvec, length * 0.5f, thickness);
            }
        }

        private struct ArUcoIdentifier : IEquatable<ArUcoIdentifier>
        {
            public int MarkerType;    // enum value
            public int DictionaryId;  // enum value
            public int[] MarkerIds;   // marker ID array

            public ArUcoIdentifier(int markerType, int dictionaryId, int[] markerIds)
            {
                MarkerType = markerType;
                DictionaryId = dictionaryId;
                MarkerIds = markerIds;
            }

            public override string ToString()
            {
                string markerIdsStr = MarkerIds != null ? string.Join(",", MarkerIds) : null;
                if (markerIdsStr != null)
                    return $"{MarkerType} {DictionaryId} [{markerIdsStr}]";
                else
                    return $"{MarkerType} {DictionaryId}";
            }

            public override int GetHashCode()
            {
                // fast hash calculation
                int hash = MarkerType;
                hash = hash * 31 + DictionaryId;
                if (MarkerIds != null)
                {
                    foreach (int id in MarkerIds)
                    {
                        hash = hash * 31 + id;
                    }
                }
                return hash;
            }

            public bool Equals(ArUcoIdentifier other)
            {
                if (MarkerType != other.MarkerType || DictionaryId != other.DictionaryId)
                    return false;

                if (MarkerIds == null) return other.MarkerIds == null;
                if (other.MarkerIds == null) return false;

                if (MarkerIds.Length != other.MarkerIds.Length)
                    return false;

                for (int i = 0; i < MarkerIds.Length; i++)
                {
                    if (MarkerIds[i] != other.MarkerIds[i])
                        return false;
                }
                return true;
            }
        }

        // Private Methods
        private void EstimatePoseCanonicalMarker(Mat rgbMat, List<Mat> corners, Mat ids)
        {
            using (MatOfPoint3f objectPoints = new MatOfPoint3f(
                new Point3(-MarkerLength / 2f, MarkerLength / 2f, 0),
                new Point3(MarkerLength / 2f, MarkerLength / 2f, 0),
                new Point3(MarkerLength / 2f, -MarkerLength / 2f, 0),
                new Point3(-MarkerLength / 2f, -MarkerLength / 2f, 0)
                ))
            {

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
                Span<int> idsValues = ids.AsSpan<int>();
#else
                int[] idsArray = new int[ids.total() * ids.channels()];
                ids.get(0, 0, idsArray);
                int[] idsValues = idsArray;
#endif

                for (int i = 0; i < idsValues.Length; i++)
                {
                    using (Mat corner_4x1 = corners[i].reshape(2, 4)) // 1*4*CV_32FC2 => 4*1*CV_32FC2
                    using (MatOfPoint2f imagePoints = new MatOfPoint2f(corner_4x1))
                    {
                        var arUcoId = new ArUcoIdentifier((int)MarkerType.CanonicalMarker, (int)DictionaryId, new[] { idsValues[i] });
                        ARGameObject aRGameObject = FindOrCreateARGameObject(ArHelper.ARGameObjects, arUcoId, ArHelper.transform);

                        aRGameObject.ImagePoints = imagePoints.toVector2Array();
                        aRGameObject.ObjectPoints = objectPoints.toVector3Array();

                        if (IsDebugDrawFrameAxes)
                            DebugDrawFrameAxes(_undistortedRgbMat, objectPoints, imagePoints, _camMatrix, _distCoeffs, MarkerLength * 0.5f);
                    }
                }
            }
        }

        private void EstimatePoseGridBoard(Mat rgbMat, List<Mat> corners, Mat ids)
        {
            if (ids.total() == 0)
                return;

            // https://github.com/opencv/opencv_contrib/blob/f10c84d48b0714f2b408c9e5cccfac1277c8e6cc/modules/aruco/src/aruco.cpp#L43
            if (corners.Count != ids.total())
                return;

            using (Mat objectPoints = new Mat())
            using (Mat imagePoints = new Mat())
            {
                // Get object and image points for the solvePnP function
                _gridBoard.matchImagePoints(corners, ids, objectPoints, imagePoints);

                if (imagePoints.total() != objectPoints.total())
                    return;

                if (objectPoints.total() == 0) // 0 of the detected markers in board
                    return;

                // Find pose
                using (MatOfPoint3f objectPoints_p3f = new MatOfPoint3f(objectPoints))
                using (MatOfPoint2f imagePoints_p3f = new MatOfPoint2f(imagePoints))
                {
                    // If at least one board marker detected
                    int markersOfBoardDetected = (int)objectPoints.total() / 4;
                    if (markersOfBoardDetected > 0)
                    {
                        var arUcoId = new ArUcoIdentifier((int)MarkerType.GridBoard, (int)DictionaryId, null);
                        ARGameObject aRGameObject = FindOrCreateARGameObject(ArHelper.ARGameObjects, arUcoId, ArHelper.transform);
                        aRGameObject.SolvePnPFlagsMode = ARGameObject.Calib3dSolvePnPFlagsMode.SOLVEPNP_ITERATIVE;

                        aRGameObject.ImagePoints = imagePoints_p3f.toVector2Array();
                        aRGameObject.ObjectPoints = objectPoints_p3f.toVector3Array();
                    }

                    if (IsDebugDrawFrameAxes)
                        DebugDrawFrameAxes(_undistortedRgbMat, objectPoints_p3f, imagePoints_p3f, _camMatrix, _distCoeffs, MarkerLength * 0.5f);
                }
            }
        }

        private void EstimatePoseChArUcoBoard(Mat rgbMat, Mat charucoCorners, Mat charucoIds)
        {
            // https://github.com/opencv/opencv_contrib/blob/f10c84d48b0714f2b408c9e5cccfac1277c8e6cc/modules/aruco/src/aruco.cpp#L63
            if (charucoCorners.total() != charucoIds.total())
                return;
            if (charucoIds.total() < 4)
                return;

            using (Mat objectPoints = new Mat())
            using (Mat imagePoints = new Mat())
            {
                // Get object and image points for the solvePnP function
                List<Mat> charucoCorners_list = new List<Mat>();
                for (int i = 0; i < charucoCorners.rows(); i++)
                {
                    charucoCorners_list.Add(charucoCorners.row(i));
                }
                _charucoBoard.matchImagePoints(charucoCorners_list, charucoIds, objectPoints, imagePoints);

                // Find pose
                using (MatOfPoint3f objectPoints_p3f = new MatOfPoint3f(objectPoints))
                using (MatOfPoint2f imagePoints_p3f = new MatOfPoint2f(imagePoints))
                {
                    if (objectPoints_p3f.rows() < 6 || imagePoints_p3f.rows() < 6)
                        return;

                    var arUcoId = new ArUcoIdentifier((int)MarkerType.ChArUcoBoard, (int)DictionaryId, null);
                    ARGameObject aRGameObject = FindOrCreateARGameObject(ArHelper.ARGameObjects, arUcoId, ArHelper.transform);
                    aRGameObject.SolvePnPFlagsMode = ARGameObject.Calib3dSolvePnPFlagsMode.SOLVEPNP_ITERATIVE;

                    aRGameObject.ImagePoints = imagePoints_p3f.toVector2Array();
                    aRGameObject.ObjectPoints = objectPoints_p3f.toVector3Array();

                    if (IsDebugDrawFrameAxes)
                        DebugDrawFrameAxes(_undistortedRgbMat, objectPoints_p3f, imagePoints_p3f, _camMatrix, _distCoeffs, MarkerLength * 0.5f);
                }
            }
        }

        private void EstimatePoseChArUcoDiamondMarker(Mat rgbMat, List<Mat> diamondCorners, Mat diamondIds)
        {
            using (MatOfPoint3f objectPoints = new MatOfPoint3f(
                new Point3(-MarkerLength / 2f, MarkerLength / 2f, 0),
                new Point3(MarkerLength / 2f, MarkerLength / 2f, 0),
                new Point3(MarkerLength / 2f, -MarkerLength / 2f, 0),
                new Point3(-MarkerLength / 2f, -MarkerLength / 2f, 0)
                ))
            {

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
                Span<int> diamondIdsValues = diamondIds.AsSpan<int>();
#else
                int[] diamondIdsArray = new int[diamondIds.total() * diamondIds.channels()];
                diamondIds.get(0, 0, diamondIdsArray);
                int[] diamondIdsValues = diamondIdsArray;
#endif

                for (int i = 0; i < diamondCorners.Count; i++)
                {
                    using (Mat corner_4x1 = diamondCorners[i].reshape(2, 4)) // 1*4*CV_32FC2 => 4*1*CV_32FC2
                    using (MatOfPoint2f imagePoints = new MatOfPoint2f(corner_4x1))
                    {
                        int markerIdsIndex = 4 * i;
                        var arUcoId = new ArUcoIdentifier(
                            (int)MarkerType.ChArUcoDiamondMarker,
                            (int)DictionaryId,
                            new[] {
                                diamondIdsValues[markerIdsIndex],
                                diamondIdsValues[markerIdsIndex + 1],
                                diamondIdsValues[markerIdsIndex + 2],
                                diamondIdsValues[markerIdsIndex + 3]
                            });
                        ARGameObject aRGameObject = FindOrCreateARGameObject(ArHelper.ARGameObjects, arUcoId, ArHelper.transform);
                        aRGameObject.SolvePnPFlagsMode = ARGameObject.Calib3dSolvePnPFlagsMode.SOLVEPNP_IPPE_SQUARE;

                        aRGameObject.ImagePoints = imagePoints.toVector2Array();
                        aRGameObject.ObjectPoints = objectPoints.toVector3Array();

                        if (IsDebugDrawFrameAxes)
                            DebugDrawFrameAxes(_undistortedRgbMat, objectPoints, imagePoints, _camMatrix, _distCoeffs, MarkerLength * 0.5f);
                    }
                }
            }
        }
    }
}
