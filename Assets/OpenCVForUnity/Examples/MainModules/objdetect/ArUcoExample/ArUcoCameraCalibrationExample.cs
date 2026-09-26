using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using OpenCVForUnity.CalibModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Extensions.SourceToMat;
using OpenCVForUnity.GeometryModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.SourceToMat;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenCVDebug = OpenCVForUnity.Extensions.OpenCVDebug;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// ArUco Camera Calibration Example
    /// Interactive camera calibration using chessboard, circle grid, or ChArUco patterns.
    ///
    /// Demonstrates:
    /// - Reading input frames via <see cref="MultiSourceToMatHelper"/> (webcam, video file, image file, or GPU readback)
    /// - Collecting calibration frames from input sources (webcam or video file)
    /// - findChessboardCorners, findCirclesGrid, and CharucoDetector corner detection
    /// - calibrateCamera / calibrateCameraCharuco and saving CameraParameters to XML
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Calib3d"/>: findChessboardCorners, calibrateCamera, projectPoints
    /// - <see cref="CharucoDetector"/>, <see cref="Aruco"/>, <see cref="Dictionary"/>
    /// - <see cref="MultiSourceToMatHelper"/>, <see cref="CameraParameters"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://docs.opencv.org/master/d4/d94/tutorial_camera_calibration.html
    /// https://github.com/opencv/opencv/blob/master/samples/cpp/tutorial_code/calib3d/camera_calibration/camera_calibration.cpp
    /// https://docs.opencv.org/3.4.0/d7/d21/tutorial_interactive_calibration.html
    /// https://github.com/opencv/opencv/tree/master/apps/interactive-calibration
    /// https://docs.opencv.org/3.2.0/da/d13/tutorial_aruco_calibration.html
    /// https://github.com/opencv/opencv_contrib/blob/master/modules/aruco/samples/calibrate_camera_charuco.cpp
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(MultiSourceToMatHelper))]
    public class ArUcoCameraCalibrationExample : MonoBehaviour
    {
        // Enums
        /// <summary>
        /// Marker type enum
        /// </summary>
        public enum MarkerType
        {
            ChessBoard,
            CirclesGlid,
            AsymmetricCirclesGlid,
            ChArUcoBoard,
        }

        /// <summary>
        /// Number of board size width enum
        /// </summary>
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

        /// <summary>
        /// Number of board size height enum
        /// </summary>
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

        // Constants
        /*
        // for ChArUcoBoard.
        // chessboard square side length (normally in meters)
        private const float _chArUcoBoradSquareLength = 0.04f;
        // marker side length (same unit than squareLength)
        private const float _chArUcoBoradMarkerLength = 0.02f;
        private const int _charucoMinMarkers = 2;
        private Mat _ids;
        private List<Mat> _corners;
        private List<Mat> _rejectedCorners;
        private Mat _recoveredIdxs;
        private Mat _charucoCorners;
        private Mat _charucoIds;
        private CharucoBoard _charucoBoard;
        private ArucoDetector _arucoDetector;
        private CharucoDetector _charucoDetector;
        */

        private const int FIND_CHESSBOARD_CORNERS_FLAGS =
            Objdetect.CALIB_CB_ADAPTIVE_THRESH |
            Objdetect.CALIB_CB_NORMALIZE_IMAGE |
            //Calib3d.CALIB_CB_FILTER_QUADS |

            Objdetect.CALIB_CB_FAST_CHECK |
            0;
        private const int FIND_CHESSBOARD_CORNERS_SB_FLAGS =
            Objdetect.CALIB_CB_NORMALIZE_IMAGE |
            Objdetect.CALIB_CB_EXHAUSTIVE |
            Objdetect.CALIB_CB_ACCURACY |
            0;
        private const int FIND_CIRCLES_GRID_FLAGS =
            //Calib3d.CALIB_CB_CLUSTERING |

            0;
        private const int CALIBRATION_FLAGS =
            //Calib3d.CALIB_USE_INTRINSIC_GUESS |
            //Calib3d.CALIB_FIX_PRINCIPAL_POINT |
            //Calib3d.CALIB_FIX_ASPECT_RATIO |
            //Calib3d.CALIB_ZERO_TANGENT_DIST |
            //Calib3d.CALIB_FIX_K1 |
            //Calib3d.CALIB_FIX_K2 |
            //Calib3d.CALIB_FIX_K3 |
            //Calib3d.CALIB_FIX_K4 |
            //Calib3d.CALIB_FIX_K5 |

            Calib.CALIB_USE_LU |
            0;

        // Public Fields
        [Header("Output")]
        [Tooltip("The RawImage for previewing the result.")]
        public RawImage ResultPreview;
        [Space(10)]
        public InputField SavePathInputField;
        public Toggle ShowUndistortImageToggle;
        public bool ShowUndistortImage = true;
        [Space(10)]
        public Dropdown MarkerTypeDropdown;
        [Tooltip("The marker type used for calibration.")]
        public MarkerType MarkerTypeValue = MarkerType.ChessBoard;
        public Dropdown BoardSizeWDropdown;
        [Tooltip("Number of inner corners per a item column. (square, circle)")]
        public NumberOfBoardSizeWidth BoardSizeW = NumberOfBoardSizeWidth.W_7;
        public Dropdown BoardSizeHDropdown;
        [Tooltip("Number of inner corners per a item row. (square, circle)")]
        public NumberOfBoardSizeHeight BoardSizeH = NumberOfBoardSizeHeight.H_5;
        [Header("Normal Calibration Option")]
        public GameObject NormalCalibrationOptionsGroup;
        public InputField SquareSizeInputField;
        [Tooltip("The size of a square in some user defined metric system (pixel, millimeter)")]
        public float SquareSize = 50f;
        public Toggle UseNewCalibrationMethodToggle;
        [Tooltip("If your calibration board is inaccurate, unmeasured, roughly planar targets (Checkerboard patterns on paper using off-the-shelf printers are the most convenient calibration targets and most of them are not accurate enough.), a method from [219] can be utilized to dramatically improve the accuracies of the estimated camera intrinsic parameters. Need to set the measured values from the actual chess board to \"squareSize\" and \"gridWidth\".")]
        public bool UseNewCalibrationMethod = true;
        public InputField GridWidthInputField;
        [Tooltip("The measured distance between top-left (0, 0, 0) and top-right (squareSize*(boardSizeW - 1), 0, 0) corners of the pattern grid points.")]
        public float GridWidth = 400f;
        [Tooltip("Determines if use findChessboardCornersSB method. (More accurate than the findChessboardCorners and cornerSubPix methods)")]
        public bool UseFindChessboardCornersSBMethod = true;
        [Tooltip("Determines if enable CornerSubPix method. (Improve the found corners' coordinate accuracy for chessboard)")]
        public bool EnableCornerSubPix = true;
        [Header("ArUco Calibration Option")]
        public GameObject ArUcoCalibrationOptionsGroup;
        public Dropdown DictionaryIdDropdown;
        [Tooltip("The dictionary identifier used for ArUco marker detection.")]
        public ArUcoDictionary DictionaryId = ArUcoDictionary.DICT_6X6_250;
        [Tooltip("Determines if refine marker detection. (only valid for ArUco boards)")]
        public bool RefineMarkerDetection = true;

        [Header("[Debug] Image Input Option")]
        [Tooltip("Determines if calibrates camera using the list of calibration images.")]
        public bool IsImagesInputMode = false;
        [Tooltip("Set a relative directory path from the starting point of the \"StreamingAssets\" folder.  e.g. \"OpenCVForUnityExamples/objdetect/calibration_images\"")]
        public string CalibrationImagesDirectory = "OpenCVForUnityExamples/objdetect/calibration_images"; // W_9 x H_6

        // Private Fields
        private Texture2D _texture;
        private MultiSourceToMatHelper _multiSourceToMatHelper;
        private Mat _grayMat;
        private Mat _bgrMat;
        private Mat _undistortedBgrMat;
        private Mat _rgbaMat;
        private Mat _camMatrix;
        private MatOfDouble _distCoeffs;
        private List<Mat> _rvecs;
        private List<Mat> _tvecs;
        private List<Mat> _imagePoints;
        private List<Mat> _allImgs;
        private bool _isInitialized = false;
        private bool _isCalibrating = false;
        private double _repErr = 0;
        private bool _shouldCaptureFrame = false;
        private Dictionary _dictionary;
        private List<List<Mat>> _allCorners;
        private List<Mat> _allIds;
        private FpsMonitor _fpsMonitor;
        private SourceToMatControlPanel _controlPanel;

        // Unity Lifecycle Methods
        private IEnumerator Start()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSourceToMatHelper = gameObject.GetComponent<MultiSourceToMatHelper>();
            _multiSourceToMatHelper.OutputColorFormat = SourceToMatColorFormat.RGBA;

            // fix the screen orientation.
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            // wait for the screen orientation to change.
            yield return null;

            // Update GUI state
            MarkerTypeDropdown.value = (int)MarkerTypeValue;
            BoardSizeWDropdown.value = (int)BoardSizeW - 1;
            BoardSizeHDropdown.value = (int)BoardSizeH - 1;

            ShowUndistortImageToggle.isOn = ShowUndistortImage;

            SquareSizeInputField.text = SquareSize.ToString();
            UseNewCalibrationMethodToggle.isOn = UseNewCalibrationMethod;
            GridWidthInputField.text = GridWidth.ToString();

            DictionaryIdDropdown.value = (int)DictionaryId;

            bool arUcoCalibMode = MarkerTypeValue == MarkerType.ChArUcoBoard;
            NormalCalibrationOptionsGroup.gameObject.SetActive(!arUcoCalibMode);
            ArUcoCalibrationOptionsGroup.gameObject.SetActive(arUcoCalibMode);

            if (IsImagesInputMode)
            {
                IsImagesInputMode = InitializeImagesInputMode();
            }

            if (IsImagesInputMode)
            {
                HideSourceToMatControlPanel();
            }
            else
            {
                WireSourceToMatControlPanelHooks();
                _multiSourceToMatHelper.Initialize();
            }
        }

        private void OnDestroy()
        {
            UnwireSourceToMatControlPanelHooks();
            if (IsImagesInputMode)
            {
                DisposeCalibraton();
            }
            else
            {
                _multiSourceToMatHelper.Dispose();
                _multiSourceToMatHelper = null;
            }

            Screen.orientation = ScreenOrientation.AutoRotation;

            OpenCVDebug.SetDebugMode(false);
        }

        // Public Methods
        /// <summary>
        /// Raises the helper frame mat updated event.
        /// Updates the preview texture when a new frame is available during playback.
        /// </summary>
        public void OnSourceToMatHelperFrameMatUpdated()
        {
            if (IsImagesInputMode)
            {
                return;
            }

            if (!_multiSourceToMatHelper.IsPlaying)
            {
                return;
            }

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;

            Imgproc.cvtColor(rgbaMat, _grayMat, Imgproc.COLOR_RGBA2GRAY);

            // Capture only on button press; clone keeps the calibration frame independent of live preview.
            if (_shouldCaptureFrame)
            {
                _shouldCaptureFrame = false;
                Mat frameMat = _grayMat.clone();

                double e = 0;
                if (MarkerTypeValue != MarkerType.ChArUcoBoard)
                {
                    e = CaptureFrame(frameMat);
                }
                // else
                //     e = CaptureFrame_Charuco(frameMat);

                if (e > 0)
                {
                    _repErr = e;
                }
            }

            DrawFrame(_grayMat, _bgrMat);

            if (ShowUndistortImage)
            {
                Imgproc.undistort(_bgrMat, _undistortedBgrMat, _camMatrix, _distCoeffs);
                DrawCalibrationResult(_undistortedBgrMat);
                Imgproc.cvtColor(_undistortedBgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);
            }
            else
            {
                DrawCalibrationResult(_bgrMat);
                Imgproc.cvtColor(_bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);
            }

            OpenCVMatUnityUtils.MatToTexture2D(rgbaMat, _texture);
        }

        /// <summary>
        /// Raises the helper initialized event.
        /// Recreates the preview texture and starts playback on first initialization.
        /// Skips Play when re-initialization has already restored Playing or Paused.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized", this);

            Mat rgbaMat = _multiSourceToMatHelper.FrameMat;

            RecreatePreviewTexture();
            CreateOrRecreateProcessingResources(rgbaMat);

#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API
            // If the WebCam is front facing, flip the Mat horizontally. Required for successful detection.
            if (_multiSourceToMatHelper.ActiveHelper is WebCamTextureToMatHelper webCamHelper)
            {
                _multiSourceToMatHelper.FlipHorizontal = webCamHelper.IsFrontFacing;
            }
#endif

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Clear();
                UpdateFpsMonitorPlaybackState();
                _fpsMonitor.Add("HelperKind", _multiSourceToMatHelper.RequestedHelperKind.ToString());
                _fpsMonitor.Add("Width", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("Height", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("Rotate90Degree", _multiSourceToMatHelper.Rotate90Degree.ToString());
                _fpsMonitor.Add("FlipVertical", _multiSourceToMatHelper.FlipVertical.ToString());
                _fpsMonitor.Add("FlipHorizontal", _multiSourceToMatHelper.FlipHorizontal.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
            }

            if (!_multiSourceToMatHelper.IsPlaying && !_multiSourceToMatHelper.IsPaused)
            {
                _multiSourceToMatHelper.Play();
                UpdateFpsMonitorPlaybackState();
            }
        }

        /// <summary>
        /// Raises the helper frame mat layout changed event.
        /// Recreates the preview texture when rotation or output size changes.
        /// </summary>
        public void OnSourceToMatHelperFrameMatLayoutChanged()
        {
            Debug.Log("OnSourceToMatHelperFrameMatLayoutChanged", this);

            RecreatePreviewTexture();
            CreateOrRecreateProcessingResources(_multiSourceToMatHelper.FrameMat);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("Width", _multiSourceToMatHelper.Width.ToString());
                _fpsMonitor.Add("Height", _multiSourceToMatHelper.Height.ToString());
                _fpsMonitor.Add("Orientation", Screen.orientation.ToString());
            }
        }

        /// <summary>
        /// Raises the helper released event.
        /// </summary>
        public void OnSourceToMatHelperReleased()
        {
            Debug.Log("OnSourceToMatHelperReleased", this);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Clear();
            }

            DisposeFrameProcessingResources();
            CleanupPreviewResources();
        }

        /// <summary>
        /// Raises the helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed", this);

            DisposeCalibraton();
        }

        /// <summary>
        /// Raises the helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(SourceToMatErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message, this);
        }

        /// <summary>
        /// Raises the back button click event.
        /// Stops playback and disposes the helper before scene transition (required on WebGL).
        /// </summary>
        public async void OnBackButtonClick()
        {
            if (_multiSourceToMatHelper.IsPlaying || _multiSourceToMatHelper.IsPaused)
            {
                await _multiSourceToMatHelper.StopAsync();
            }

            await _multiSourceToMatHelper.DisposeAsync();

            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterPlay"/>.
        /// </summary>
        public void OnControlPanelAfterPlay()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterPause"/>.
        /// </summary>
        public void OnControlPanelAfterPause()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnAfterStop"/>.
        /// </summary>
        public void OnControlPanelAfterStop()
        {
            UpdateFpsMonitorPlaybackState();
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnRotate90Changed"/>.
        /// </summary>
        /// <param name="isOn">New Rotate90Degree value applied by the panel.</param>
        public void OnControlPanelRotate90Changed(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("Rotate90Degree", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnFlipVerticalChanged"/>.
        /// </summary>
        /// <param name="isOn">New FlipVertical value applied by the panel.</param>
        public void OnControlPanelFlipVerticalChanged(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("FlipVertical", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnFlipHorizontalChanged"/>.
        /// </summary>
        /// <param name="isOn">New FlipHorizontal value applied by the panel.</param>
        public void OnControlPanelFlipHorizontalChanged(bool isOn)
        {
            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("FlipHorizontal", isOn.ToString());
            }
        }

        /// <summary>
        /// Invoked by <see cref="SourceToMatControlPanel.OnHelperKindChanged"/>.
        /// </summary>
        /// <param name="kindIndex">Dropdown index matching <see cref="MultiSourceHelperKind"/>.</param>
        public void OnControlPanelHelperKindChanged(int kindIndex)
        {
            if (_fpsMonitor == null || !Enum.IsDefined(typeof(MultiSourceHelperKind), kindIndex))
            {
                return;
            }

            _fpsMonitor.Add("HelperKind", ((MultiSourceHelperKind)kindIndex).ToString());
        }

        public void OnMarkerTypeDropdownValueChanged(int result)
        {
            if ((int)MarkerTypeValue != result)
            {
                MarkerTypeValue = (MarkerType)result;

                bool arUcoCalibMode = MarkerTypeValue == MarkerType.ChArUcoBoard;

                NormalCalibrationOptionsGroup.gameObject.SetActive(!arUcoCalibMode);
                ArUcoCalibrationOptionsGroup.gameObject.SetActive(arUcoCalibMode);

                if (IsImagesInputMode)
                {
                    InitializeImagesInputMode();
                }
                else
                {
                    if (_multiSourceToMatHelper.IsInitialized)
                    {
                        _multiSourceToMatHelper.Initialize();
                    }
                }
            }
        }

        public void OnBoardSizeWDropdownValueChanged(int result)
        {
            if ((int)BoardSizeW != result + 1)
            {
                BoardSizeW = (NumberOfBoardSizeWidth)(result + 1);

                GridWidth = SquareSize * ((int)BoardSizeW - 1);
                GridWidthInputField.text = GridWidth.ToString();

                if (IsImagesInputMode)
                {
                    InitializeImagesInputMode();
                }
                else
                {
                    if (_multiSourceToMatHelper.IsInitialized)
                    {
                        _multiSourceToMatHelper.Initialize();
                    }
                }
            }
        }

        public void OnBoardSizeHDropdownValueChanged(int result)
        {
            if ((int)BoardSizeH != result + 1)
            {
                BoardSizeH = (NumberOfBoardSizeHeight)(result + 1);

                if (IsImagesInputMode)
                {
                    InitializeImagesInputMode();
                }
                else
                {
                    if (_multiSourceToMatHelper.IsInitialized)
                    {
                        _multiSourceToMatHelper.Initialize();
                    }
                }
            }
        }

        public void OnShowUndistortImageToggleValueChanged()
        {
            if (ShowUndistortImage != ShowUndistortImageToggle.isOn)
            {
                ShowUndistortImage = ShowUndistortImageToggle.isOn;
            }
        }

        public void OnSquareSizeInputFieldValueChanged()
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android)
            {
                return;
            }

            bool result = float.TryParse(SquareSizeInputField.text, out float f);

            if (result)
            {
                SquareSize = f;
                SquareSizeInputField.text = f.ToString();
            }
            else
            {
                SquareSize = 1f;
                SquareSizeInputField.text = SquareSize.ToString();
            }
        }

        public void OnSquareSizeInputFieldEndEdit()
        {
            bool result = float.TryParse(SquareSizeInputField.text, out float f);

            if (result)
            {
                SquareSize = f;
                SquareSizeInputField.text = f.ToString();
            }
            else
            {
                SquareSize = 1f;
                SquareSizeInputField.text = SquareSize.ToString();
            }
        }

        public void OnUseNewCalibrationMethodToggleValueChanged()
        {
            if (UseNewCalibrationMethod != UseNewCalibrationMethodToggle.isOn)
            {
                UseNewCalibrationMethod = UseNewCalibrationMethodToggle.isOn;
            }
        }

        public void OnGridWidthInputFieldValueChanged()
        {
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android)
            {
                return;
            }

            bool result = float.TryParse(GridWidthInputField.text, out float f);

            if (result)
            {
                GridWidth = f;
                GridWidthInputField.text = f.ToString();
            }
            else
            {
                GridWidth = SquareSize * ((int)BoardSizeW - 1);
                GridWidthInputField.text = GridWidth.ToString();
            }
        }

        public void OnGridWidthInputFieldEndEdit()
        {
            bool result = float.TryParse(GridWidthInputField.text, out float f);

            if (result)
            {
                GridWidth = f;
                GridWidthInputField.text = f.ToString();
            }
            else
            {
                GridWidth = SquareSize * ((int)BoardSizeW - 1);
                GridWidthInputField.text = GridWidth.ToString();
            }
        }

        public void OnDictionaryIdDropdownValueChanged(int result)
        {
            if ((int)DictionaryId != result)
            {
                DictionaryId = (ArUcoDictionary)result;
                _dictionary = Objdetect.getPredefinedDictionary((int)DictionaryId);

                if (IsImagesInputMode)
                {
                    InitializeImagesInputMode();
                }
                else
                {
                    if (_multiSourceToMatHelper.IsInitialized)
                    {
                        _multiSourceToMatHelper.Initialize();
                    }
                }
            }
        }

        public void OnCaptureButtonClick()
        {
            if (IsImagesInputMode)
            {
                if (!_isCalibrating)
                {
                    InitializeImagesInputMode();
                }

                StartCoroutine("CalibrateCameraUsingImages");
            }
            else
            {
                _shouldCaptureFrame = true;
            }
        }

        public void OnResetButtonClick()
        {
            if (IsImagesInputMode)
            {
                if (!_isCalibrating)
                {
                    InitializeImagesInputMode();
                }
            }
            else
            {
                ResetCalibration();
            }
        }

        public void OnSaveButtonClick()
        {
            string saveDirectoryPath = Path.Combine(Application.persistentDataPath, "ArUcoCameraCalibrationExample");

            if (!Directory.Exists(saveDirectoryPath))
            {
                Directory.CreateDirectory(saveDirectoryPath);
            }

            string calibratonDirectoryName = "camera_parameters" + _bgrMat.width() + "x" + _bgrMat.height();
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
            int frameCount = (MarkerTypeValue == MarkerType.ChArUcoBoard) ? _allCorners.Count : _imagePoints.Count;
            CameraParameters param = new CameraParameters(frameCount, _bgrMat.width(), _bgrMat.height(), CALIBRATION_FLAGS, _camMatrix, _distCoeffs, _repErr);
            XmlSerializer serializer = new XmlSerializer(typeof(CameraParameters));
            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                serializer.Serialize(stream, param);
            }

            // save the calibration images.
            string format = "png";
            MatOfInt compressionParams = new MatOfInt(Imgcodecs.IMWRITE_PNG_COMPRESSION, 0);
            for (int i = 0; i < _allImgs.Count; ++i)
            {
                Imgcodecs.imwrite(Path.Combine(saveCalibratonFileDirectoryPath, calibratonDirectoryName + "_" + i.ToString("00") + "." + format), _allImgs[i], compressionParams);
            }

            SavePathInputField.text = savePath;
            Debug.Log("Saved the CameraParameters to disk in XML file format.", this);
            Debug.Log("savePath: " + savePath, this);
        }

        // Private Methods
        private void RecreatePreviewTexture()
        {
            Mat frameMat = _multiSourceToMatHelper.FrameMat;
            if (frameMat == null)
            {
                return;
            }

            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
                _texture = null;
            }

            bool isRgb = _multiSourceToMatHelper.OutputColorFormat == SourceToMatColorFormat.RGB;
            _texture = new Texture2D(frameMat.cols(), frameMat.rows(), isRgb ? TextureFormat.RGB24 : TextureFormat.RGBA32, false);
            OpenCVMatUnityUtils.MatToTexture2D(frameMat, _texture);

            if (ResultPreview != null)
            {
                ResultPreview.texture = _texture;
                AspectRatioFitter aspectRatioFitter = ResultPreview.GetComponent<AspectRatioFitter>();
                if (aspectRatioFitter != null)
                {
                    aspectRatioFitter.aspectRatio = (float)_texture.width / _texture.height;
                }
            }
        }

        private void DisposeFrameProcessingResources()
        {
            // ResetCalibration recreates the camera matrix from _bgrMat size; skip until processing Mats exist.
            if (_bgrMat != null)
            {
                ResetCalibration();
            }

            _grayMat?.Dispose();
            _grayMat = null;
            _bgrMat?.Dispose();
            _bgrMat = null;
            _undistortedBgrMat?.Dispose();
            _undistortedBgrMat = null;
            _rgbaMat?.Dispose();
            _rgbaMat = null;

            if (_rvecs != null)
            {
                foreach (var item in _rvecs)
                {
                    item.Dispose();
                }
            }

            _rvecs?.Clear();
            if (_tvecs != null)
            {
                foreach (var item in _tvecs)
                {
                    item.Dispose();
                }
            }

            _tvecs?.Clear();

            /*
                        // for ChArUcoBoard.
                        _ids?.Dispose();
                        _ids = null;
                        if (_corners != null) foreach (var item in _corners) item.Dispose();
                        _corners.Clear()
                        if (_rejectedCorners != null) foreach (var item in _rejectedCorners) item.Dispose();
                        _rejectedCorners.Clear()
                        _recoveredIdxs?.Dispose();
                        _recoveredIdxs = null;
                        _charucoCorners?.Dispose();
                        _charucoCorners = null;
                        _charucoIds?.Dispose();
                        _charucoIds = null;
                        _charucoBoard?.Dispose();
                        _charucoBoard = null;
                        _arucoDetector?.Dispose();
                        _arucoDetector = null;
                        _charucoDetector?.Dispose();
                        _charucoDetector = null;
                        _dictionary?.Dispose();
                        _dictionary = null;
            */

            _isInitialized = false;
        }

        private void CreateOrRecreateProcessingResources(Mat frameMat)
        {
            if (frameMat == null)
            {
                return;
            }

            DisposeFrameProcessingResources();

            float width = frameMat.width();
            float height = frameMat.height();
            float imageSizeScale = 1.0f;
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                imageSizeScale = (float)Screen.height / (float)Screen.width;
            }

            // set cameraparam.
            _camMatrix = CreateCameraMatrix(width, height);
            Debug.Log("camMatrix " + _camMatrix.dump(), this);

            _distCoeffs = new MatOfDouble(0, 0, 0, 0, 0);
            Debug.Log("distCoeffs " + _distCoeffs.dump(), this);

            // calibration camera.
            Size imageSize = new Size(width * imageSizeScale, height * imageSizeScale);
            double apertureWidth = 0;
            double apertureHeight = 0;
            double[] fovx = new double[1];
            double[] fovy = new double[1];
            double[] focalLength = new double[1];
            Point principalPoint = new Point(0, 0);
            double[] aspectratio = new double[1];

            Geometry.calibrationMatrixValues(_camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);

            Debug.Log("imageSize " + imageSize.ToString(), this);
            Debug.Log("apertureWidth " + apertureWidth, this);
            Debug.Log("apertureHeight " + apertureHeight, this);
            Debug.Log("fovx " + fovx[0], this);
            Debug.Log("fovy " + fovy[0], this);
            Debug.Log("focalLength " + focalLength[0], this);
            Debug.Log("principalPoint " + principalPoint.ToString(), this);
            Debug.Log("aspectratio " + aspectratio[0], this);

            _grayMat = new Mat(frameMat.rows(), frameMat.cols(), CvType.CV_8UC1);
            _bgrMat = new Mat(frameMat.rows(), frameMat.cols(), CvType.CV_8UC3);
            _undistortedBgrMat = new Mat();
            _rgbaMat = new Mat(frameMat.rows(), frameMat.cols(), CvType.CV_8UC4);
            _rvecs = new List<Mat>();
            _tvecs = new List<Mat>();

            _imagePoints = new List<Mat>();
            _allImgs = new List<Mat>();

            /*
                        // for ChArUcoBoard.
                        _ids = new Mat();
                        _corners = new List<Mat>();
                        _rejectedCorners = new List<Mat>();
                        _recoveredIdxs = new Mat();
                        DetectorParameters detectorParams = new DetectorParameters();
                        detectorParams.set_minDistanceToBorder(3);
                        detectorParams.set_useAruco3Detection(true);
                        detectorParams.set_cornerRefinementMethod(Objdetect.CORNER_REFINE_SUBPIX);
                        detectorParams.set_minSideLengthCanonicalImg(16);
                        detectorParams.set_errorCorrectionRate(0.8);
                        _dictionary = Objdetect.getPredefinedDictionary((int)_dictionaryId);
                        RefineParameters refineParameters = new RefineParameters(10f, 3f, true);
                        _arucoDetector = new ArucoDetector(_dictionary, detectorParams, refineParameters);

                        _charucoCorners = new Mat();
                        _charucoIds = new Mat();
                        _charucoBoard = new CharucoBoard(new Size((int)_boardSizeW, (int)_boardSizeH), _chArUcoBoradSquareLength, _chArUcoBoradMarkerLength, _dictionary);

                        _charucoDetector = new CharucoDetector(_charucoBoard);
                        CharucoParameters charucoParameters = _charucoDetector.getCharucoParameters();
                        charucoParameters.set_cameraMatrix(_camMatrix);
                        charucoParameters.set_distCoeffs(_distCoeffs);
                        charucoParameters.set_minMarkers(_charucoMinMarkers);
                        _charucoDetector.setCharucoParameters(charucoParameters);
                        _charucoDetector.setDetectorParameters(detectorParams);
                        _charucoDetector.setRefineParameters(refineParameters);
            */

            _allIds = new List<Mat>();
            _allCorners = new List<List<Mat>>();

            _isInitialized = true;
        }

        private void CleanupPreviewResources()
        {
            if (_texture != null)
            {
                Texture2D.Destroy(_texture);
                _texture = null;
            }

            UpdateFpsMonitorPlaybackState();
        }

        private void UpdateFpsMonitorPlaybackState()
        {
            if (_fpsMonitor == null || _multiSourceToMatHelper == null)
            {
                return;
            }

            _fpsMonitor.Add("PlaybackState", GetPlaybackStateText());
        }

        private string GetPlaybackStateText()
        {
            if (!_multiSourceToMatHelper.IsInitialized)
            {
                return "Uninitialized";
            }

            if (_multiSourceToMatHelper.IsPlaying)
            {
                return "Playing";
            }

            if (_multiSourceToMatHelper.IsPaused)
            {
                return "Paused";
            }

            return "Ready";
        }

        private void WireSourceToMatControlPanelHooks()
        {
            _controlPanel = GetComponent<SourceToMatControlPanel>();
            if (_controlPanel == null)
            {
                return;
            }

            _controlPanel.OnAfterPlay.AddListener(OnControlPanelAfterPlay);
            _controlPanel.OnAfterPause.AddListener(OnControlPanelAfterPause);
            _controlPanel.OnAfterStop.AddListener(OnControlPanelAfterStop);
            _controlPanel.OnRotate90Changed.AddListener(OnControlPanelRotate90Changed);
            _controlPanel.OnFlipVerticalChanged.AddListener(OnControlPanelFlipVerticalChanged);
            _controlPanel.OnFlipHorizontalChanged.AddListener(OnControlPanelFlipHorizontalChanged);
            _controlPanel.OnHelperKindChanged.AddListener(OnControlPanelHelperKindChanged);
        }

        private void UnwireSourceToMatControlPanelHooks()
        {
            if (_controlPanel == null)
            {
                return;
            }

            _controlPanel.OnAfterPlay.RemoveListener(OnControlPanelAfterPlay);
            _controlPanel.OnAfterPause.RemoveListener(OnControlPanelAfterPause);
            _controlPanel.OnAfterStop.RemoveListener(OnControlPanelAfterStop);
            _controlPanel.OnRotate90Changed.RemoveListener(OnControlPanelRotate90Changed);
            _controlPanel.OnFlipVerticalChanged.RemoveListener(OnControlPanelFlipVerticalChanged);
            _controlPanel.OnFlipHorizontalChanged.RemoveListener(OnControlPanelFlipHorizontalChanged);
            _controlPanel.OnHelperKindChanged.RemoveListener(OnControlPanelHelperKindChanged);
            _controlPanel = null;
        }

        /// <summary>
        /// Hides the SourceToMat control panel. Image-list calibration does not use the live helper UI.
        /// The panel instantiates Canvas <c>SourceToMatControlPanelCanvas</c> in Awake, so disable the component and the canvas.
        /// </summary>
        private void HideSourceToMatControlPanel()
        {
            SourceToMatControlPanel panel = GetComponent<SourceToMatControlPanel>();
            if (panel != null)
            {
                panel.enabled = false;
            }

            GameObject canvas = GameObject.Find("SourceToMatControlPanelCanvas");
            if (canvas != null)
            {
                canvas.SetActive(false);
            }
        }

        private void InitializeCalibraton(Mat frameMat)
        {
            _texture = new Texture2D(frameMat.cols(), frameMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUnityUtils.MatToTexture2D(frameMat, _texture);

            if (ResultPreview != null)
            {
                ResultPreview.texture = _texture;
                AspectRatioFitter aspectRatioFitter = ResultPreview.GetComponent<AspectRatioFitter>();
                if (aspectRatioFitter != null)
                {
                    aspectRatioFitter.aspectRatio = (float)_texture.width / _texture.height;
                }
            }

            CreateOrRecreateProcessingResources(frameMat);
        }

        private void DisposeCalibraton()
        {
            DisposeFrameProcessingResources();
            CleanupPreviewResources();
        }

        private void DrawFrame(Mat grayMat, Mat bgrMat)
        {
            Imgproc.cvtColor(grayMat, bgrMat, Imgproc.COLOR_GRAY2BGR);

            switch (MarkerTypeValue)
            {
                default:
                case MarkerType.ChessBoard:
                case MarkerType.CirclesGlid:
                case MarkerType.AsymmetricCirclesGlid:
                    // detect markers.
                    MatOfPoint2f points = new MatOfPoint2f();
                    bool found = false;

                    switch (MarkerTypeValue)
                    {
                        default:
                        case MarkerType.ChessBoard:
                            if (UseFindChessboardCornersSBMethod)
                            {
                                found = Objdetect.findChessboardCornersSB(grayMat, new Size((int)BoardSizeW, (int)BoardSizeH), points, FIND_CHESSBOARD_CORNERS_SB_FLAGS);
                            }
                            else
                            {
                                found = Objdetect.findChessboardCorners(grayMat, new Size((int)BoardSizeW, (int)BoardSizeH), points, FIND_CHESSBOARD_CORNERS_FLAGS);
                            }
                            break;
                        case MarkerType.CirclesGlid:
                            found = Objdetect.findCirclesGrid(grayMat, new Size((int)BoardSizeW, (int)BoardSizeH), points, FIND_CIRCLES_GRID_FLAGS | Objdetect.CALIB_CB_SYMMETRIC_GRID);
                            Debug.Log("CirclesGlid found: " + found, this);
                            break;
                        case MarkerType.AsymmetricCirclesGlid:
                            found = Objdetect.findCirclesGrid(grayMat, new Size((int)BoardSizeW, (int)BoardSizeH), points, FIND_CIRCLES_GRID_FLAGS | Objdetect.CALIB_CB_ASYMMETRIC_GRID);
                            Debug.Log("AsymmetricCirclesGlid found: " + found, this);
                            break;
                    }

                    if (found)
                    {
                        // draw markers.
                        Objdetect.drawChessboardCorners(bgrMat, new Size((int)BoardSizeW, (int)BoardSizeH), points, found);
                    }
                    break;
                case MarkerType.ChArUcoBoard:

                    Imgproc.putText(bgrMat, "Calibration of MarkerType.ChArUcoBoard is not yet supported.", new Point(5, bgrMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(0, 0, 255, 255), 1, Imgproc.LINE_AA, false);

                    /*
                                        // detect markers.
                                        _arucoDetector.detectMarkers(grayMat, _corners, _ids, _rejectedCorners);

                                        // refine marker detection.
                                        if (_refineMarkerDetection)
                                        {
                                            // https://github.com/opencv/opencv/blob/377be68d923e40900ac5526242bcf221e3f355e5/modules/objdetect/src/aruco/charuco_detector.cpp#L310
                                            _arucoDetector.refineDetectedMarkers(grayMat, _charucoBoard, _corners, _ids, _rejectedCorners);
                                        }

                                        // if at least one marker detected
                                        if (_ids.total() > 0)
                                        {
                                            _charucoDetector.detectBoard(grayMat, _charucoCorners, _charucoIds, _corners, _ids);

                                            // draw markers.
                                            if (_corners.Count == _ids.total() || _ids.total() == 0)
                                                Objdetect.drawDetectedMarkers(bgrMat, _corners, _ids, new Scalar(0, 255, 0, 255));

                                            // if at least one charuco corner detected
                                            if (_charucoCorners.total() == _charucoIds.total() || _charucoIds.total() == 0)
                                            {
                                                Objdetect.drawDetectedCornersCharuco(bgrMat, _charucoCorners, _charucoIds, new Scalar(0, 0, 255, 255));
                                            }
                                        }
                    */
                    break;
            }
        }

        private void DrawCalibrationResult(Mat bgrMat)
        {
            double[] camMatrixArr = new double[(int)_camMatrix.total()];
            _camMatrix.get(0, 0, camMatrixArr);
            double[] distCoeffsArr = new double[(int)_distCoeffs.total()];
            _distCoeffs.get(0, 0, distCoeffsArr);

            int textLeft = 320;
            int ff = Imgproc.FONT_HERSHEY_SIMPLEX;
            double fs = 0.4;
            Scalar c = new Scalar(255, 255, 255, 255);
            int t = 0;
            int lt = Imgproc.LINE_AA;
            bool blo = false;
            int frameCount = (MarkerTypeValue == MarkerType.ChArUcoBoard) ? _allCorners.Count : _imagePoints.Count;
            Imgproc.putText(bgrMat, frameCount + " FRAME CAPTURED", new Point(bgrMat.cols() - textLeft, 20), ff, fs, c, t, lt, blo);
            Imgproc.putText(bgrMat, "IMAGE_WIDTH: " + bgrMat.width(), new Point(bgrMat.cols() - textLeft, 40), ff, fs, c, t, lt, blo);
            Imgproc.putText(bgrMat, "IMAGE_HEIGHT: " + bgrMat.height(), new Point(bgrMat.cols() - textLeft, 60), ff, fs, c, t, lt, blo);
            Imgproc.putText(bgrMat, "CALIBRATION_FLAGS: " + CALIBRATION_FLAGS, new Point(bgrMat.cols() - textLeft, 80), ff, fs, c, t, lt, blo);

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
            Imgproc.putText(bgrMat, "AVG_REPROJECTION_ERROR: " + _repErr, new Point(bgrMat.cols() - textLeft, 300), ff, fs, c, t, lt, blo);

            if (frameCount == 0)
            {
                Imgproc.putText(bgrMat, "Please press the capture button to start!", new Point(5, bgrMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
            }
        }

        private double CaptureFrame(Mat frameMat)
        {
            double repErr = -1;

            MatOfPoint2f points = new MatOfPoint2f();
            Size patternSize = new Size((int)BoardSizeW, (int)BoardSizeH);

            bool found = false;
            switch (MarkerTypeValue)
            {
                default:
                case MarkerType.ChessBoard:
                    if (UseFindChessboardCornersSBMethod)
                    {
                        found = Objdetect.findChessboardCornersSB(frameMat, patternSize, points, FIND_CHESSBOARD_CORNERS_SB_FLAGS);
                    }
                    else
                    {
                        found = Objdetect.findChessboardCorners(frameMat, patternSize, points, FIND_CHESSBOARD_CORNERS_FLAGS);
                    }
                    break;
                case MarkerType.CirclesGlid:
                    found = Objdetect.findCirclesGrid(frameMat, patternSize, points, FIND_CIRCLES_GRID_FLAGS | Objdetect.CALIB_CB_SYMMETRIC_GRID);
                    break;
                case MarkerType.AsymmetricCirclesGlid:
                    found = Objdetect.findCirclesGrid(frameMat, patternSize, points, FIND_CIRCLES_GRID_FLAGS | Objdetect.CALIB_CB_ASYMMETRIC_GRID);
                    break;
            }

            if (found)
            {
                if (MarkerTypeValue == MarkerType.ChessBoard && !UseFindChessboardCornersSBMethod && EnableCornerSubPix)
                {
                    int winSize = 11;
                    Imgproc.cornerSubPix(frameMat, points, new Size(winSize, winSize), new Size(-1, -1), new TermCriteria(TermCriteria.EPS + TermCriteria.COUNT, 30, 0.0001));
                }

                _imagePoints.Add(points);
                _allImgs.Add(frameMat);

                Debug.Log(_imagePoints.Count + " Frame captured.", this);
            }
            else
            {
                Debug.Log("Invalid frame.", this);

                frameMat?.Dispose();
                points?.Dispose();

                return -1;
            }

            if (_imagePoints.Count < 1)
            {
                Debug.Log("Not enough points for calibration.", this);
                repErr = -1;
            }
            else
            {
                MatOfPoint3f objectPoint = new MatOfPoint3f();
                objectPoint.alloc((int)_imagePoints[0].total());
                CalcChessboardCorners(patternSize, SquareSize, objectPoint, MarkerTypeValue);

                // Adjust the grid width based on the calibration method:
                // - Traditional method: Calculate from square size
                // - New method: Use actual measured grid width for more accurate calibration
                // This is particularly useful when using printed calibration patterns
                // where the actual dimensions may differ from theoretical values
                float grid_width = SquareSize * ((int)patternSize.width - 1);
                bool release_object = false;
                if (UseNewCalibrationMethod)
                {
                    grid_width = GridWidth;
                    release_object = true;
                }
                Point3[] objectPointsArray = objectPoint.toArray();
                int topRightIndex = (int)patternSize.width - 1;
                objectPointsArray[topRightIndex].x = objectPointsArray[0].x + grid_width;
                objectPoint.fromArray(objectPointsArray);

                Mat newObjPoints = objectPoint.clone();

                List<Mat> objectPoints = new List<Mat>();
                for (int i = 0; i < _imagePoints.Count; ++i)
                {
                    objectPoints.Add(objectPoint.clone());
                }

                int iFixedPoint = -1;
                if (release_object)
                {
                    iFixedPoint = (int)patternSize.width - 1;
                }

                try
                {
                    repErr = Calib.calibrateCameraRO(
                                objectPoints,
                                _imagePoints,
                                frameMat.size(),
                                iFixedPoint,
                                _camMatrix,
                                _distCoeffs,
                                _rvecs,
                                _tvecs,
                                newObjPoints,
                                CALIBRATION_FLAGS
                            );
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Calib3d.calibrateCameraRO: " + e.Message, this);
                }

                //if (release_object)
                //{
                //    Debug.Log("New board corners: ");
                //    Point3[] newPoints = new MatOfPoint3f(newObjPoints).toArray();
                //    Debug.Log(newPoints[0]);
                //    Debug.Log(newPoints[(int)patternSize.width - 1]);
                //    Debug.Log(newPoints[(int)patternSize.width * ((int)patternSize.height - 1)]);
                //    Debug.Log(newPoints[newPoints.Length - 1]);
                //}

                objectPoint?.Dispose();
                if (objectPoints != null)
                {
                    foreach (var item in objectPoints)
                    {
                        item.Dispose();
                    }
                }

                objectPoints.Clear();
            }

            Debug.Log("repErr: " + repErr, this);
            Debug.Log("camMatrix: " + _camMatrix.dump(), this);
            Debug.Log("distCoeffs: " + _distCoeffs.dump(), this);

            return repErr;
        }

        /*
                private double CaptureFrame_Charuco(Mat frameMat)
                {
                    double repErr = -1;

                    List<Mat> corners = new List<Mat>();
                    Mat ids = new Mat();

                    _arucoDetector.detectMarkers(frameMat, corners, ids, _rejectedCorners);

                    if (_refineMarkerDetection)
                    {
                        // https://github.com/opencv/opencv/blob/377be68d923e40900ac5526242bcf221e3f355e5/modules/objdetect/src/aruco/charuco_detector.cpp#L310
                        _arucoDetector.refineDetectedMarkers(frameMat, _charucoBoard, corners, ids, _rejectedCorners);
                    }

                    if (ids.total() > 0)
                    {
                        _allCorners.Add(corners);
                        _allIds.Add(ids);
                        _allImgs.Add(frameMat);

                        Debug.Log(_allCorners.Count + " Frame captured.");
                    }
                    else
                    {
                        Debug.Log("Invalid frame.");

                        frameMat?.Dispose();
                        ids?.Dispose();
                        if (corners != null) foreach (var item in corners) item.Dispose();
                        corners.Clear()

                        return -1;
                    }

                    // calibrate camera using charuco boards
                    repErr = CalibrateCameraCharuco(_allCorners, _allIds, _charucoBoard, frameMat.size(), _camMatrix, _distCoeffs, _rvecs, _tvecs, _calibrationFlags, _calibrationFlags);

                    Debug.Log("repErr: " + repErr);
                    Debug.Log("camMatrix: " + _camMatrix.dump());
                    Debug.Log("distCoeffs: " + _distCoeffs.dump());

                    return repErr;
                }

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

                        _charucoDetector.detectBoard(_allImgs[i], currentCharucoCorners, currentCharucoIds, allCorners[i], allIds[i]);

                        //if (currentCharucoIds.total() > 0)
                        if (currentCharucoIds.total() > 0 && currentCharucoCorners.total() == currentCharucoIds.total())
                        {
                            allCharucoCorners.Add(currentCharucoCorners);
                            allCharucoIds.Add(currentCharucoIds);
                            filteredImages.Add(_allImgs[i]);
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
            _repErr = 0;

            if (_bgrMat == null)
            {
                return;
            }

            _camMatrix?.Dispose();
            _camMatrix = CreateCameraMatrix(_bgrMat.width(), _bgrMat.height());
            _distCoeffs?.Dispose();
            _distCoeffs = new MatOfDouble(0, 0, 0, 0, 0);

            if (_imagePoints != null)
            {
                foreach (var item in _imagePoints)
                {
                    item.Dispose();
                }

                _imagePoints.Clear();
            }

            if (_allImgs != null)
            {
                foreach (var item in _allImgs)
                {
                    item.Dispose();
                }

                _allImgs.Clear();
            }

            /*
                        // for ChArUcoBoard.
                        if (_allCorners != null)
                        {
                            foreach (var corners in _allCorners)
                            {
                                if (corners != null) foreach (var item in corners) item.Dispose();
                                corners.Clear()
                            }
                        }
                        _allCorners.Clear();

                        if (_allIds != null) foreach (var item in _allIds) item.Dispose();
                        _allIds.Clear()
            */
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
            int expectedCount = (int)(patternSize.width * patternSize.height);
            if (expectedCount != (int)corners.total())
            {
                Debug.Log("Invalid corners size.", this);
                corners.alloc(expectedCount);
            }

            int width = (int)patternSize.width;
            int height = (int)patternSize.height;
            int pointCount = width * height;
            Point3[] cornersArray = new Point3[pointCount];

            switch (markerType)
            {
                default:
                case MarkerType.ChessBoard:
                case MarkerType.CirclesGlid:
                    for (int i = 0; i < height; ++i)
                    {
                        for (int j = 0; j < width; ++j)
                        {
                            int index = width * i + j;
                            cornersArray[index] = new Point3(j * squareSize, i * squareSize, 0f);
                        }
                    }
                    break;
                case MarkerType.AsymmetricCirclesGlid:
                    for (int i = 0; i < height; ++i)
                    {
                        for (int j = 0; j < width; ++j)
                        {
                            int index = width * i + j;
                            cornersArray[index] = new Point3((2 * j + i % 2) * squareSize, i * squareSize, 0f);
                        }
                    }
                    break;
            }

            corners.fromArray(cornersArray);
        }

        private bool InitializeImagesInputMode()
        {
            if (_isInitialized)
            {
                DisposeCalibraton();
            }

            if (string.IsNullOrEmpty(CalibrationImagesDirectory))
            {
                Debug.LogWarning("When using the images input mode, please set a calibration images directory path.", this);
                return false;
            }

            string dirPath = Path.Combine(Application.streamingAssetsPath, CalibrationImagesDirectory);
            if (!Directory.Exists(dirPath))
            {
                Debug.LogWarning("The directory does not exist.", this);
                return false;
            }
            string[] imageFiles = GetImageFilesInDirectory(dirPath);
            if (imageFiles.Length < 1)
            {
                Debug.LogWarning("The image file does not exist.", this);
                return false;
            }

            Uri rootPath = new Uri(Application.streamingAssetsPath + System.IO.Path.AltDirectorySeparatorChar);
            Uri fullPath = new Uri(imageFiles[0]);
            string relativePath = rootPath.MakeRelativeUri(fullPath).ToString();

            using (Mat gray = Imgcodecs.imread(OpenCVForUnityEnv.GetFilePath(relativePath), Imgcodecs.IMREAD_GRAYSCALE))
            {

                if (gray.total() == 0)
                {
                    Debug.LogWarning("Invalid image file.", this);
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
                    OpenCVMatUnityUtils.MatToTexture2D(rgba, _texture);
                }
            }
            return true;
        }

        private IEnumerator CalibrateCameraUsingImages()
        {
            string dirPath = Path.Combine(Application.streamingAssetsPath, CalibrationImagesDirectory);
            string[] imageFiles = GetImageFilesInDirectory(dirPath);
            if (imageFiles.Length < 1)
            {
                yield break;
            }

            _isCalibrating = true;
            MarkerTypeDropdown.interactable = BoardSizeWDropdown.interactable = BoardSizeHDropdown.interactable = false;
            NormalCalibrationOptionsGroup.gameObject.SetActive(false);
            ArUcoCalibrationOptionsGroup.gameObject.SetActive(false);

            Uri rootPath = new Uri(Application.streamingAssetsPath + System.IO.Path.AltDirectorySeparatorChar);

            foreach (var path in imageFiles)
            {

                Uri fullPath = new Uri(path);
                string relativePath = rootPath.MakeRelativeUri(fullPath).ToString();

                using (Mat gray = Imgcodecs.imread(OpenCVForUnityEnv.GetFilePath(relativePath), Imgcodecs.IMREAD_GRAYSCALE))
                {
                    if (gray.width() != _bgrMat.width() || gray.height() != _bgrMat.height())
                    {
                        continue;
                    }

                    Mat frameMat = gray.clone();

                    double e = 0;
                    if (MarkerTypeValue != MarkerType.ChArUcoBoard)
                    {
                        e = CaptureFrame(frameMat);
                    }
                    // else
                    //     e = CaptureFrame_Charuco(frameMat);

                    if (e > 0)
                    {
                        _repErr = e;
                    }

                    DrawFrame(gray, _bgrMat);
                    DrawCalibrationResult(_bgrMat);
                    Imgproc.cvtColor(_bgrMat, _rgbaMat, Imgproc.COLOR_BGR2RGBA);

                    OpenCVMatUnityUtils.MatToTexture2D(_rgbaMat, _texture);
                }
                yield return new WaitForSeconds(0.5f);
            }

            _isCalibrating = false;
            MarkerTypeDropdown.interactable = BoardSizeWDropdown.interactable = BoardSizeHDropdown.interactable = true;
            bool arUcoCalibMode = MarkerTypeValue == MarkerType.ChArUcoBoard;
            NormalCalibrationOptionsGroup.gameObject.SetActive(!arUcoCalibMode);
            ArUcoCalibrationOptionsGroup.gameObject.SetActive(arUcoCalibMode);
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
    }
}
