using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.IO;
using System.Linq;
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
        /// <summary>
        /// The size of the output marker image (px).
        /// </summary>
        public int markerSize = 1000;

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
        /// The marker identifier.
        /// </summary>
        public MarkerID markerId = MarkerID.MarkerID_1;

        /// <summary>
        /// The marker id dropdown.
        /// </summary>
        public Dropdown markerIdDropdown;

        /// <summary>
        /// The save path input field.
        /// </summary>
        public InputField savePathInputField;

        /// <summary>
        /// The marker img mat.
        /// </summary>
        Mat markerImg;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        // width of the marker borders.
        const int borderBits = 1;

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
        // minimum margins (in pixels) of the board in the output image
        const int gridBoradMarginSize = 10;

        // for ChArUcoBoard.
        //  number of chessboard squares in X direction
        const int chArUcoBoradMarkersX = 5;
        //  number of chessboard squares in Y direction
        const int chArUcoBoradMarkersY = 7;
        // chessboard square side length (normally in meters)
        const float chArUcoBoradSquareLength = 0.04f;
        // marker side length (same unit than squareLength)
        const float chArUcoBoradMarkerLength = 0.02f;
        // id of first marker in dictionary to use on board.
        const int chArUcoMarkerFirstMarker = 0;
        // minimum margins (in pixels) of the board in the output image
        const int chArUcoBoradMarginSize = 10;
        // set pre4.6.0 chessboard pattern behavior (even row count patterns have a white box in the upper left corner)
        const bool useLegacyPattern = false;

        // for ChArUcoDiamondMarker.
        //  number of diamond squares in X direction
        const int diamondMarkersX = 3;
        //  number of diamond squares in Y direction
        const int diamondMarkersY = 3;
        // size of the diamond squares in pixels
        const float diamondSquareLength = 0.1f;
        // size of the markers in pixels.
        const float diamondMarkerLength = 0.06f;
        // identifiers for diamonds in diamond corners.
        const int diamondId1 = 45;
        const int diamondId2 = 68;
        const int diamondId3 = 28;
        const int diamondId4 = 74;
        const int diamondMarginSize = 0;


        // Use this for initialization
        void Start()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            markerImg = new Mat(markerSize, markerSize, CvType.CV_8UC3);
            texture = new Texture2D(markerImg.cols(), markerImg.rows(), TextureFormat.RGB24, false);
            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            markerTypeDropdown.value = (int)markerType;
            markerIdDropdown.value = (int)markerId;
            dictionaryIdDropdown.value = (int)dictionaryId;

            CreateMarkerImg();
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void CreateMarkerImg()
        {
            if (markerImg.cols() != markerSize)
            {
                markerImg.Dispose();
                markerImg = new Mat(markerSize, markerSize, CvType.CV_8UC3);
                texture = new Texture2D(markerImg.cols(), markerImg.rows(), TextureFormat.RGB24, false);
            }
            else
            {
                markerImg.setTo(Scalar.all(255));
            }

            gameObject.transform.localScale = new Vector3(markerImg.cols(), markerImg.rows(), 1);

            float width = markerImg.width() / 0.7f;
            float height = markerImg.height() / 0.7f;

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
                gameObject.transform.localPosition = new Vector3(0, -height * 0.1f, 0);
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
                gameObject.transform.localPosition = new Vector3(width * 0.1f, 0, 0);
            }

            // create dictinary.
            Dictionary dictionary = Objdetect.getPredefinedDictionary((int)dictionaryId);

            // draw marker.
            switch (markerType)
            {
                default:
                case MarkerType.CanonicalMarker:
                    Objdetect.generateImageMarker(dictionary, (int)markerId, markerSize, markerImg, borderBits);
                    Debug.Log("draw CanonicalMarker: " + "dictionaryId " + (int)dictionaryId + " markerId " + (int)markerId + " sidePixels " + markerSize + " borderBits " + borderBits);
                    break;
                case MarkerType.GridBoard:
                    int gridBoardTotalMarkers = gridBoradMarkersX * gridBoradMarkersY;
                    Mat gridBoardIds = new Mat(gridBoardTotalMarkers, 1, CvType.CV_32SC1);
                    gridBoardIds.put(0, 0, Enumerable.Range(gridBoradMarkerFirstMarker, gridBoradMarkerFirstMarker + gridBoardTotalMarkers).ToArray());

                    GridBoard gridBoard = new GridBoard(new Size(gridBoradMarkersX, gridBoradMarkersY), gridBoradMarkerLength, gridBoradMarkerSeparation, dictionary, gridBoardIds);
                    gridBoard.generateImage(new Size(markerSize, markerSize), markerImg, gridBoradMarginSize, borderBits);
                    gridBoard.Dispose();
                    Debug.Log("draw GridBoard: " + "markersX " + gridBoradMarkersX + " markersY " + gridBoradMarkersY + " markerLength " + gridBoradMarkerLength +
                    " markerSeparation " + gridBoradMarkerSeparation + " dictionaryId " + (int)dictionaryId + " firstMarkerId " + gridBoradMarkerFirstMarker + 
                    " outSize " + markerSize + " marginSize " + gridBoradMarginSize + " borderBits " + borderBits);
                    break;
                case MarkerType.ChArUcoBoard:
                    int charucoBoardTotalMarkers = (int)Math.Truncate(chArUcoBoradMarkersX * chArUcoBoradMarkersY / 2.0);
                    Mat charucoBoardIds = new Mat(charucoBoardTotalMarkers, 1, CvType.CV_32SC1);
                    charucoBoardIds.put(0, 0, Enumerable.Range(chArUcoMarkerFirstMarker, chArUcoMarkerFirstMarker + charucoBoardTotalMarkers).ToArray());

                    CharucoBoard charucoBoard = new CharucoBoard(new Size(chArUcoBoradMarkersX, chArUcoBoradMarkersY), chArUcoBoradSquareLength, chArUcoBoradMarkerLength, dictionary, charucoBoardIds);
                    charucoBoard.setLegacyPattern(useLegacyPattern);
                    charucoBoard.generateImage(new Size(markerSize, markerSize), markerImg, chArUcoBoradMarginSize, borderBits);
                    charucoBoard.Dispose();
                    Debug.Log("draw ChArUcoBoard: " + "markersX " + chArUcoBoradMarkersX + " markersY " + chArUcoBoradMarkersY + " squareLength " + chArUcoBoradSquareLength +
                    " markerLength " + chArUcoBoradMarkerLength + " dictionaryId " + (int)dictionaryId + " firstMarkerId " + chArUcoMarkerFirstMarker + " outSize " + markerSize + 
                    " marginSize " + chArUcoBoradMarginSize + " borderBits " + borderBits);
                    break;
                case MarkerType.ChArUcoDiamondMarker:
                    Mat diamondIds = new Mat(4, 1, CvType.CV_32SC1);
                    diamondIds.put(0, 0, new int[] { diamondId1, diamondId2, diamondId3, diamondId4 });
                    CharucoBoard charucoDiamondBoard = new CharucoBoard(new Size(diamondMarkersX, diamondMarkersY), diamondSquareLength, diamondMarkerLength, dictionary, diamondIds);
                    charucoDiamondBoard.generateImage(new Size(markerSize, markerSize), markerImg, diamondMarginSize, borderBits);
                    charucoDiamondBoard.Dispose();
                    Debug.Log("draw ChArUcoDiamondMarker: " + "markersX " + diamondMarkersX + " markersY " + diamondMarkersY + " squareLength " + diamondSquareLength + 
                        " markerLength " + diamondMarkerLength +" dictionaryId " + (int)dictionaryId + 
                        " markerIds " + diamondId1 + ", " + diamondId2 + ", " + diamondId3 + ", " + diamondId4 + " outSize " + markerSize + " marginSize " + diamondMarginSize + " borderBits " + borderBits);
                    break;
            }

            Utils.matToTexture2D(markerImg, texture, true, 0, true);
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
            switch (markerType)
            {
                default:
                case MarkerType.CanonicalMarker:
                    savePath = Path.Combine(saveDirectoryPath, "CanonicalMarker-d" + (int)dictionaryId + "-i" + (int)markerId + "-sp" + markerSize + "-bb" + borderBits + "." + format);
                    break;
                case MarkerType.GridBoard:
                    savePath = Path.Combine(saveDirectoryPath, "GridBoard-mx" + gridBoradMarkersX + "-my" + gridBoradMarkersY + "-ml" + gridBoradMarkerLength + "-ms" + 
                        gridBoradMarkerSeparation + "-d" + (int)dictionaryId + "-fi" + gridBoradMarkerFirstMarker + "-os" + markerSize + "-ms" + gridBoradMarginSize + "-bb" + borderBits + "." + format);
                    break;
                case MarkerType.ChArUcoBoard:
                    savePath = Path.Combine(saveDirectoryPath, "ChArUcoBoard-mx" + chArUcoBoradMarkersX + "-my" + chArUcoBoradMarkersY + "-sl" + chArUcoBoradSquareLength + 
                        "-ml" + chArUcoBoradMarkerLength + "-d" + (int)dictionaryId + "-fi" + chArUcoMarkerFirstMarker + "-os" + markerSize + "-ms" + chArUcoBoradMarginSize + 
                        "-bb" + borderBits + "." + format);
                    break;
                case MarkerType.ChArUcoDiamondMarker:
                    savePath = Path.Combine(saveDirectoryPath, "ChArUcoDiamondMarker-mx" + diamondMarkersX + "-my" + diamondMarkersY + "-sl" + diamondSquareLength + "-ml" + 
                        diamondMarkerLength + "-d" + (int)dictionaryId + "-i" + diamondId1 + "_" + diamondId2 + "_" + diamondId3 + "_" + diamondId4 + "-os" + markerSize + 
                        "-ms" + diamondMarginSize + "-bb" + borderBits + "." + format);
                    break;
            }

            if (!Directory.Exists(saveDirectoryPath))
            {
                Directory.CreateDirectory(saveDirectoryPath);
            }

            Imgcodecs.imwrite(savePath, markerImg, compressionParams);

            savePathInputField.text = savePath;
            Debug.Log("savePath: " + savePath);
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (markerImg != null)
                markerImg.Dispose();

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
        /// Raises the marker type dropdown value changed event.
        /// </summary>
        public void OnMarkerTypeDropdownValueChanged(int result)
        {
            if ((int)markerType != result)
            {
                markerType = (MarkerType)result;

                markerIdDropdown.interactable = (markerType == MarkerType.CanonicalMarker);

                CreateMarkerImg();
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
                CreateMarkerImg();
            }
        }

        /// <summary>
        /// Raises the marker id dropdown value changed event.
        /// </summary>
        public void OnMarkerIdDropdownValueChanged(int result)
        {
            if ((int)markerId != result)
            {
                markerId = (MarkerID)result;
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
    }
}