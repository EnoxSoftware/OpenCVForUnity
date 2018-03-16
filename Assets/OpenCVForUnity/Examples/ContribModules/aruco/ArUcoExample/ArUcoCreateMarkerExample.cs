using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.IO;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// ArUco Create Marker Example
    /// Referring to https://github.com/opencv/opencv_contrib/blob/master/modules/aruco/samples/create_marker.cpp.
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
        public int markerType = (int)MarkerType.CanonicalMarker;

        /// <summary>
        /// The marker type dropdown.
        /// </summary>
        public Dropdown markerTypeDropdown;

        /// <summary>
        /// The dictionary identifier.
        /// </summary>
        public int dictionaryId = Aruco.DICT_6X6_250;

        /// <summary>
        /// The dictionary id dropdown.
        /// </summary>
        public Dropdown dictionaryIdDropdown;

        /// <summary>
        /// The marker identifier.
        /// </summary>
        public int markerId = 1;

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
        // minimum margins (in pixels) of the board in the output image
        const int chArUcoBoradMarginSize = 10;

        // Use this for initialization
        void Start ()
        {
            markerImg = new Mat (markerSize, markerSize, CvType.CV_8UC3);
            texture = new Texture2D (markerImg.cols (), markerImg.rows (), TextureFormat.RGB24, false);
            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            if (markerTypeDropdown.value != markerType || markerIdDropdown.value != markerId || dictionaryIdDropdown.value != dictionaryId) {
                markerTypeDropdown.value = markerType;
                markerIdDropdown.value = markerId;
                dictionaryIdDropdown.value = dictionaryId;

                CreateMaeker ();
            } else {
                CreateMaeker ();
            }
        }

        // Update is called once per frame
        void Update ()
        {
    
        }

        private void CreateMaeker ()
        {
            if (markerImg.cols () != markerSize) {
                markerImg.Dispose ();
                markerImg = new Mat (markerSize, markerSize, CvType.CV_8UC3);
                texture = new Texture2D (markerImg.cols (), markerImg.rows (), TextureFormat.RGB24, false);
            } else {
                markerImg.setTo (Scalar.all(255));
            }

            gameObject.transform.localScale = new Vector3 (markerImg.cols (), markerImg.rows (), 1);

            float width = markerImg.width () / 0.7f;
            float height = markerImg.height () / 0.7f;

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
                gameObject.transform.localPosition = new Vector3 (0, -height * 0.1f, 0);
            } else {
                Camera.main.orthographicSize = height / 2;
                gameObject.transform.localPosition = new Vector3 (width * 0.1f, 0, 0);
            }

            // create dictinary.
            Dictionary dictionary = Aruco.getPredefinedDictionary (dictionaryId);

            // draw marker.
            switch (markerType)
            {
            case (int)MarkerType.CanonicalMarker:
                Aruco.drawMarker (dictionary, markerId, markerSize, markerImg, borderBits);
                Debug.Log ("draw CanonicalMarker: " + "dictionaryId " + dictionaryId + " markerId " + markerId + " sidePixels " + markerSize + " borderBits " + borderBits);
                break;
            case (int)MarkerType.GridBoard:
                GridBoard gridBoard = GridBoard.create (gridBoradMarkersX, gridBoradMarkersY, gridBoradMarkerLength, gridBoradMarkerSeparation, dictionary, gridBoradMarkerFirstMarker);
                gridBoard.draw (new Size(markerSize, markerSize), markerImg, gridBoradMarginSize, borderBits);
                gridBoard.Dispose ();
                Debug.Log ("draw GridBoard: " + "markersX " + gridBoradMarkersX + " markersY " + gridBoradMarkersY + " markerLength " + gridBoradMarkerLength + 
                    " markerSeparation "+ gridBoradMarkerSeparation + "dictionaryId " + dictionaryId + " outSize " + markerSize + " marginSize " + gridBoradMarginSize + " borderBits " + borderBits);
                break;
            case (int)MarkerType.ChArUcoBoard:
                CharucoBoard charucoBoard = CharucoBoard.create (chArUcoBoradMarkersX, chArUcoBoradMarkersY, chArUcoBoradSquareLength, chArUcoBoradMarkerLength, dictionary);
                charucoBoard.draw (new Size(markerSize, markerSize), markerImg, chArUcoBoradMarginSize, borderBits);
                charucoBoard.Dispose ();
                Debug.Log ("draw ChArUcoBoard: " + "markersX " + chArUcoBoradMarkersX + " markersY " + chArUcoBoradMarkersY + " markerLength " + chArUcoBoradSquareLength + 
                    " markerSeparation "+ chArUcoBoradMarkerLength + "dictionaryId " + dictionaryId + " outSize " + markerSize + " marginSize " + chArUcoBoradMarginSize + " borderBits " + borderBits);
                break;
            }

            Utils.matToTexture2D (markerImg, texture);
        }

        private void SaveMarkerImg()
        {
            // save the markerImg.
            string saveDirectoryPath = Path.Combine (Application.persistentDataPath, "ArUcoCreateMarkerExample");
            string savePath = "";
            #if UNITY_WEBGL && !UNITY_EDITOR
            string format = "jpg";
            MatOfInt compressionParams = new MatOfInt(Imgcodecs.CV_IMWRITE_JPEG_QUALITY, 100);
            #else
            string format = "png";
            MatOfInt compressionParams = new MatOfInt(Imgcodecs.CV_IMWRITE_PNG_COMPRESSION, 0);
            #endif
            switch (markerType)
            {
            case (int)MarkerType.CanonicalMarker:
                savePath = Path.Combine (saveDirectoryPath, "CanonicalMarker-d" + dictionaryId + "-i" + markerId + "-sp" + markerSize + "-bb" + borderBits + "." + format);
                break;
            case (int)MarkerType.GridBoard:
                savePath = Path.Combine (saveDirectoryPath, "GridBoard-mx" + gridBoradMarkersX + "-my" + gridBoradMarkersY + "-d" + dictionaryId + "-os" + markerSize + "-bb" + borderBits + "." + format);
                break;
            case (int)MarkerType.ChArUcoBoard:
                savePath = Path.Combine (saveDirectoryPath, "ChArUcoBoard-mx" + chArUcoBoradMarkersX + "-my" + chArUcoBoradMarkersY + "-d" + dictionaryId + "-os" + markerSize + "-bb" + borderBits + "." + format);
                break;
            }
                
            if (!Directory.Exists (saveDirectoryPath)) {
                Directory.CreateDirectory (saveDirectoryPath);
            }

            Imgcodecs.imwrite (savePath, markerImg, compressionParams);

            savePathInputField.text = savePath;
            Debug.Log ("savePath: " + savePath);
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            if (markerImg != null)
                markerImg.Dispose ();
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
        /// Raises the marker type dropdown value changed event.
        /// </summary>
        public void OnMarkerTypeDropdownValueChanged(int result)
        {
            if (markerType != result) {
                markerType = result;

                markerIdDropdown.interactable = (markerType == (int)MarkerType.CanonicalMarker);

                CreateMaeker ();
            }
        }

        /// <summary>
        /// Raises the dictionary id dropdown value changed event.
        /// </summary>
        public void OnDictionaryIdDropdownValueChanged(int result)
        {
            if (dictionaryId != result) {
                dictionaryId = result;
                CreateMaeker ();
            }
        }

        /// <summary>
        /// Raises the marker id dropdown value changed event.
        /// </summary>
        public void OnMarkerIdDropdownValueChanged(int result)
        {
            if (markerId != result) {
                markerId = result;
                CreateMaeker ();
            }
        }

        /// <summary>
        /// Raises the save marker img button click event.
        /// </summary>
        public void OnSaveMarkerImgButtonClick ()
        {
            SaveMarkerImg ();
        }

        private enum MarkerType
        {
            CanonicalMarker,
            GridBoard,
            ChArUcoBoard,
            //ChArUcoDiamondMarker //Not yet implemented.
        }
    }
}