using System;
using System.IO;
using System.Linq;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// ArUco Create Marker Example
    /// Referring to https://github.com/opencv/opencv_contrib/blob/4.x/modules/aruco/samples/create_marker.cpp
    /// https://github.com/opencv/opencv_contrib/blob/4.x/modules/aruco/samples/create_board.cpp
    /// https://github.com/opencv/opencv_contrib/blob/4.x/modules/aruco/samples/create_board_charuco.cpp
    /// https://github.com/opencv/opencv_contrib/blob/4.x/modules/aruco/samples/create_diamond.cpp
    /// </summary>
    public class ArUcoCreateMarkerExample : MonoBehaviour
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

        /// <summary>
        /// Marker ID enum
        /// </summary>
        public enum MarkerID
        {
            MarkerID_0,
            MarkerID_1,
            MarkerID_2,
            MarkerID_3,
            MarkerID_4,
            MarkerID_5,
            MarkerID_6,
            MarkerID_7,
            MarkerID_8,
            MarkerID_9,
        }

        // Constants
        // width of the marker borders.
        private const int BORDER_BITS = 1;

        // for GridBoard.
        // number of markers in X direction
        private const int GRID_BOARD_MARKERS_X = 5;
        // number of markers in Y direction
        private const int GRID_BOARD_MARKERS_Y = 7;
        // marker side length (normally in meters)
        private const float GRID_BOARD_MARKER_LENGTH = 0.04f;
        // separation between two markers (same unit as markerLength)
        private const float GRID_BOARD_MARKER_SEPARATION = 0.01f;
        // id of first marker in dictionary to use on board.
        private const int GRID_BOARD_MARKER_FIRST_MARKER = 0;
        // minimum margins (in pixels) of the board in the output image
        private const int GRID_BOARD_MARGIN_SIZE = 10;

        // for ChArUcoBoard.
        //  number of chessboard squares in X direction
        private const int CHARUCO_BOARD_MARKERS_X = 5;
        //  number of chessboard squares in Y direction
        private const int CHARUCO_BOARD_MARKERS_Y = 7;
        // chessboard square side length (normally in meters)
        private const float CHARUCO_BOARD_SQUARE_LENGTH = 0.04f;
        // marker side length (same unit than squareLength)
        private const float CHARUCO_BOARD_MARKER_LENGTH = 0.02f;
        // id of first marker in dictionary to use on board.
        private const int CHARUCO_MARKER_FIRST_MARKER = 0;
        // minimum margins (in pixels) of the board in the output image
        private const int CHARUCO_BOARD_MARGIN_SIZE = 10;
        // set pre4.6.0 chessboard pattern behavior (even row count patterns have a white box in the upper left corner)
        private const bool USE_LEGACY_PATTERN = false;

        // for ChArUcoDiamondMarker.
        //  number of diamond squares in X direction
        private const int DIAMOND_MARKERS_X = 3;
        //  number of diamond squares in Y direction
        private const int DIAMOND_MARKERS_Y = 3;
        // size of the diamond squares in pixels
        private const float DIAMOND_SQUARE_LENGTH = 0.1f;
        // size of the markers in pixels.
        private const float DIAMOND_MARKER_LENGTH = 0.06f;
        // identifiers for diamonds in diamond corners.
        private const int DIAMOND_ID_1 = 45;
        private const int DIAMOND_ID_2 = 68;
        private const int DIAMOND_ID_3 = 28;
        private const int DIAMOND_ID_4 = 74;
        private const int DIAMOND_MARGIN_SIZE = 0;

        // Public Fields
        [Header("Output")]
        [Tooltip("The RawImage for previewing the result.")]
        public RawImage ResultPreview;

        [Space(10)]

        [Tooltip("The size of the output marker image (px).")]
        public int MarkerSize = 1000;

        public Dropdown MarkerTypeDropdown;
        public MarkerType SelectedMarkerType = MarkerType.CanonicalMarker;
        public Dropdown DictionaryIdDropdown;
        public ArUcoDictionary DictionaryId = ArUcoDictionary.DICT_6X6_250;
        public Dropdown MarkerIdDropdown;
        public MarkerID MarkerId = MarkerID.MarkerID_1;
        public InputField SavePathInputField;

        // Private Fields
        private Mat _markerImg;
        private Texture2D _texture;

        // Unity Lifecycle Methods
        private void Start()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            _markerImg = new Mat(MarkerSize, MarkerSize, CvType.CV_8UC3);
            _texture = new Texture2D(_markerImg.cols(), _markerImg.rows(), TextureFormat.RGB24, false);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;

            // Update GUI state
            MarkerTypeDropdown.value = (int)SelectedMarkerType;
            MarkerIdDropdown.value = (int)MarkerId;
            DictionaryIdDropdown.value = (int)DictionaryId;

            CreateMarkerImg();
        }

        private void Update()
        {

        }

        private void OnDestroy()
        {
            _markerImg?.Dispose(); _markerImg = null;
            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;

            OpenCVDebug.SetDebugMode(false);
        }

        // Public Methods
        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the marker type dropdown value changed event.
        /// </summary>
        public void OnMarkerTypeDropdownValueChanged(int result)
        {
            if ((int)SelectedMarkerType != result)
            {
                SelectedMarkerType = (MarkerType)result;

                MarkerIdDropdown.interactable = (SelectedMarkerType == MarkerType.CanonicalMarker);

                CreateMarkerImg();
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
                CreateMarkerImg();
            }
        }

        /// <summary>
        /// Raises the marker id dropdown value changed event.
        /// </summary>
        public void OnMarkerIdDropdownValueChanged(int result)
        {
            if ((int)MarkerId != result)
            {
                MarkerId = (MarkerID)result;
                CreateMarkerImg();
            }
        }

        /// <summary>
        /// Raises the save marker img button click event.
        /// </summary>
        public void OnSaveMarkerImgButtonClick()
        {
            SaveMarkerImg();
        }

        // Private Methods
        private void CreateMarkerImg()
        {
            if (_markerImg.cols() != MarkerSize)
            {
                _markerImg.Dispose();
                _markerImg = new Mat(MarkerSize, MarkerSize, CvType.CV_8UC3);
                _texture = new Texture2D(_markerImg.cols(), _markerImg.rows(), TextureFormat.RGB24, false);
            }
            else
            {
                _markerImg.setTo(Scalar.all(255));
            }

            // create dictinary.
            Dictionary dictionary = Objdetect.getPredefinedDictionary((int)DictionaryId);

            // draw marker.
            switch (SelectedMarkerType)
            {
                default:
                case MarkerType.CanonicalMarker:
                    Objdetect.generateImageMarker(dictionary, (int)MarkerId, MarkerSize, _markerImg, BORDER_BITS);
                    Debug.Log("draw CanonicalMarker: " + "dictionaryId " + (int)DictionaryId + " markerId " + (int)MarkerId + " sidePixels " + MarkerSize + " borderBits " + BORDER_BITS);
                    break;
                case MarkerType.GridBoard:

                    int gridBoardTotalMarkers = GRID_BOARD_MARKERS_X * GRID_BOARD_MARKERS_Y;
                    Mat gridBoardIds = new Mat(gridBoardTotalMarkers, 1, CvType.CV_32SC1);
                    gridBoardIds.put(0, 0, Enumerable.Range(GRID_BOARD_MARKER_FIRST_MARKER, GRID_BOARD_MARKER_FIRST_MARKER + gridBoardTotalMarkers).ToArray());

                    GridBoard gridBoard = new GridBoard(new Size(GRID_BOARD_MARKERS_X, GRID_BOARD_MARKERS_Y), GRID_BOARD_MARKER_LENGTH, GRID_BOARD_MARKER_SEPARATION, dictionary, gridBoardIds);

                    //
                    // This code includes adjustments to address an issue in the OpenCV GridBoard::generateImage method,
                    // where the vertical and horizontal margins between AR markers in the grid are not evenly spaced.

                    // Calculate the aspect ratio of the grid (width/height)
                    // Calculate the total aspect ratio of the marker grid including markers and separations
                    double gridAspectRatio = (GRID_BOARD_MARKERS_X * GRID_BOARD_MARKER_LENGTH + (GRID_BOARD_MARKERS_X - 1) * GRID_BOARD_MARKER_SEPARATION) /
                                             (GRID_BOARD_MARKERS_Y * GRID_BOARD_MARKER_LENGTH + (GRID_BOARD_MARKERS_Y - 1) * GRID_BOARD_MARKER_SEPARATION);

                    // Adjust the output size to fit within the specified markerSize
                    int adjustedWidth, adjustedHeight;
                    if (gridAspectRatio >= 1.0)
                    {
                        // If the grid is wider than tall, fix the width to markerSize and scale the height proportionally
                        adjustedWidth = MarkerSize;
                        adjustedHeight = (int)(MarkerSize / gridAspectRatio);
                    }
                    else
                    {
                        // If the grid is taller than wide, fix the height to markerSize and scale the width proportionally
                        adjustedHeight = MarkerSize;
                        adjustedWidth = (int)(MarkerSize * gridAspectRatio);
                    }

                    Mat adjustedMarkerImg = new Mat(_markerImg,
                        new OpenCVForUnity.CoreModule.Rect((MarkerSize - adjustedWidth) / 2, (MarkerSize - adjustedHeight) / 2, adjustedWidth, adjustedHeight));

                    gridBoard.generateImage(new Size(adjustedWidth, adjustedHeight), adjustedMarkerImg, GRID_BOARD_MARGIN_SIZE, BORDER_BITS);
                    //
                    // or
                    //gridBoard.generateImage(new Size(MarkerSize, MarkerSize), _markerImg, GRID_BOARD_MARGIN_SIZE, BORDER_BITS);
                    //

                    gridBoard.Dispose();
                    Debug.Log("draw GridBoard: " + "markersX " + GRID_BOARD_MARKERS_X + " markersY " + GRID_BOARD_MARKERS_Y + " markerLength " + GRID_BOARD_MARKER_LENGTH +
                    " markerSeparation " + GRID_BOARD_MARKER_SEPARATION + " dictionaryId " + (int)DictionaryId + " firstMarkerId " + GRID_BOARD_MARKER_FIRST_MARKER +
                    " outSize " + MarkerSize + " marginSize " + GRID_BOARD_MARGIN_SIZE + " borderBits " + BORDER_BITS);
                    break;
                case MarkerType.ChArUcoBoard:
                    int charucoBoardTotalMarkers = (int)Math.Truncate(CHARUCO_BOARD_MARKERS_X * CHARUCO_BOARD_MARKERS_Y / 2.0);
                    Mat charucoBoardIds = new Mat(charucoBoardTotalMarkers, 1, CvType.CV_32SC1);
                    charucoBoardIds.put(0, 0, Enumerable.Range(CHARUCO_MARKER_FIRST_MARKER, CHARUCO_MARKER_FIRST_MARKER + charucoBoardTotalMarkers).ToArray());

                    CharucoBoard charucoBoard = new CharucoBoard(new Size(CHARUCO_BOARD_MARKERS_X, CHARUCO_BOARD_MARKERS_Y), CHARUCO_BOARD_SQUARE_LENGTH, CHARUCO_BOARD_MARKER_LENGTH, dictionary, charucoBoardIds);
                    charucoBoard.setLegacyPattern(USE_LEGACY_PATTERN);
                    charucoBoard.generateImage(new Size(MarkerSize, MarkerSize), _markerImg, CHARUCO_BOARD_MARGIN_SIZE, BORDER_BITS);
                    charucoBoard.Dispose();
                    Debug.Log("draw ChArUcoBoard: " + "markersX " + CHARUCO_BOARD_MARKERS_X + " markersY " + CHARUCO_BOARD_MARKERS_Y + " squareLength " + CHARUCO_BOARD_SQUARE_LENGTH +
                    " markerLength " + CHARUCO_BOARD_MARKER_LENGTH + " dictionaryId " + (int)DictionaryId + " firstMarkerId " + CHARUCO_MARKER_FIRST_MARKER + " outSize " + MarkerSize +
                    " marginSize " + CHARUCO_BOARD_MARGIN_SIZE + " borderBits " + BORDER_BITS);
                    break;
                case MarkerType.ChArUcoDiamondMarker:
                    Mat diamondIds = new Mat(4, 1, CvType.CV_32SC1);
                    diamondIds.put(0, 0, new int[] { DIAMOND_ID_1, DIAMOND_ID_2, DIAMOND_ID_3, DIAMOND_ID_4 });
                    CharucoBoard charucoDiamondBoard = new CharucoBoard(new Size(DIAMOND_MARKERS_X, DIAMOND_MARKERS_Y), DIAMOND_SQUARE_LENGTH, DIAMOND_MARKER_LENGTH, dictionary, diamondIds);
                    charucoDiamondBoard.generateImage(new Size(MarkerSize, MarkerSize), _markerImg, DIAMOND_MARGIN_SIZE, BORDER_BITS);
                    charucoDiamondBoard.Dispose();
                    Debug.Log("draw ChArUcoDiamondMarker: " + "markersX " + DIAMOND_MARKERS_X + " markersY " + DIAMOND_MARKERS_Y + " squareLength " + DIAMOND_SQUARE_LENGTH +
                        " markerLength " + DIAMOND_MARKER_LENGTH + " dictionaryId " + (int)DictionaryId +
                        " markerIds " + DIAMOND_ID_1 + ", " + DIAMOND_ID_2 + ", " + DIAMOND_ID_3 + ", " + DIAMOND_ID_4 + " outSize " + MarkerSize + " marginSize " + DIAMOND_MARGIN_SIZE + " borderBits " + BORDER_BITS);
                    break;
            }

            OpenCVMatUtils.MatToTexture2D(_markerImg, _texture);
        }

        private void SaveMarkerImg()
        {
            // save the markerImg.
            string saveDirectoryPath = Path.Combine(Application.persistentDataPath, "ArUcoCreateMarkerExample");
            string savePath = "";
#if UNITY_WEBGL && !UNITY_EDITOR
            string format = "jpg";
            MatOfInt compressionParams = new MatOfInt(Imgcodecs.IMWRITE_JPEG_QUALITY, 100);
#else
            string format = "png";
            MatOfInt compressionParams = new MatOfInt(Imgcodecs.IMWRITE_PNG_COMPRESSION, 0);
#endif
            switch (SelectedMarkerType)
            {
                default:
                case MarkerType.CanonicalMarker:
                    savePath = Path.Combine(saveDirectoryPath, "CanonicalMarker-d" + (int)DictionaryId + "-i" + (int)MarkerId + "-sp" + MarkerSize + "-bb" + BORDER_BITS + "." + format);
                    break;
                case MarkerType.GridBoard:
                    savePath = Path.Combine(saveDirectoryPath, "GridBoard-mx" + GRID_BOARD_MARKERS_X + "-my" + GRID_BOARD_MARKERS_Y + "-ml" + GRID_BOARD_MARKER_LENGTH + "-ms" +
                        GRID_BOARD_MARKER_SEPARATION + "-d" + (int)DictionaryId + "-fi" + GRID_BOARD_MARKER_FIRST_MARKER + "-os" + MarkerSize + "-ms" + GRID_BOARD_MARGIN_SIZE + "-bb" + BORDER_BITS + "." + format);
                    break;
                case MarkerType.ChArUcoBoard:
                    savePath = Path.Combine(saveDirectoryPath, "ChArUcoBoard-mx" + CHARUCO_BOARD_MARKERS_X + "-my" + CHARUCO_BOARD_MARKERS_Y + "-sl" + CHARUCO_BOARD_SQUARE_LENGTH +
                        "-ml" + CHARUCO_BOARD_MARKER_LENGTH + "-d" + (int)DictionaryId + "-fi" + CHARUCO_MARKER_FIRST_MARKER + "-os" + MarkerSize + "-ms" + CHARUCO_BOARD_MARGIN_SIZE +
                        "-bb" + BORDER_BITS + "." + format);
                    break;
                case MarkerType.ChArUcoDiamondMarker:
                    savePath = Path.Combine(saveDirectoryPath, "ChArUcoDiamondMarker-mx" + DIAMOND_MARKERS_X + "-my" + DIAMOND_MARKERS_Y + "-sl" + DIAMOND_SQUARE_LENGTH + "-ml" +
                        DIAMOND_MARKER_LENGTH + "-d" + (int)DictionaryId + "-i" + DIAMOND_ID_1 + "_" + DIAMOND_ID_2 + "_" + DIAMOND_ID_3 + "_" + DIAMOND_ID_4 + "-os" + MarkerSize +
                        "-ms" + DIAMOND_MARGIN_SIZE + "-bb" + BORDER_BITS + "." + format);
                    break;
            }

            if (!Directory.Exists(saveDirectoryPath))
            {
                Directory.CreateDirectory(saveDirectoryPath);
            }

            Imgcodecs.imwrite(savePath, _markerImg, compressionParams);

            SavePathInputField.text = savePath;
            Debug.Log("savePath: " + savePath);
        }
    }
}
