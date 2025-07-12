using System.Collections.Generic;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.AR;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// ArUco Image Example
    /// An example of marker-based AR view and camera pose estimation using the objdetect and aruco module.
    /// Referring to https://github.com/opencv/opencv_contrib/blob/4.x/modules/aruco/samples/detect_markers.cpp
    /// http://docs.opencv.org/3.1.0/d5/dae/tutorial_aruco_detection.html
    /// https://github.com/opencv/opencv/blob/4.x/modules/objdetect/test/test_arucodetection.cpp
    /// </summary>
    public class ArUcoImageExample : MonoBehaviour
    {
        // Enums
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

        // Public Fields
        public Texture2D ImgTexture;

        [Space(10)]

        public Dropdown DictionaryIdDropdown;
        public ArUcoDictionary DictionaryId = ArUcoDictionary.DICT_6X6_250;
        public Toggle ShowRejectedCornersToggle;
        public bool ShowRejectedCorners = false;

        public bool ApplyEstimationPose = true;

        [Tooltip("The length of the markers' side. Normally, unit is meters.")]
        public float MarkerLength = 0.1f;

        public ARHelper ArHelper;
        public GameObject ArCubePrefab;

        // Private Fields
        private Mat _rgbMat;
        private Mat _undistortedRgbMat;
        private Texture2D _texture;


        // Unity Lifecycle Methods
        private void Start()
        {
            _rgbMat = new Mat(ImgTexture.height, ImgTexture.width, CvType.CV_8UC3);
            _texture = new Texture2D(_rgbMat.cols(), _rgbMat.rows(), TextureFormat.RGBA32, false);

            // Update GUI state
            DictionaryIdDropdown.value = (int)DictionaryId;
            ShowRejectedCornersToggle.isOn = ShowRejectedCorners;

            _undistortedRgbMat = new Mat();

            DetectMarkers();
        }

        private void Update()
        {

        }

        private void OnDestroy()
        {
            _rgbMat?.Dispose(); _rgbMat = null;
            _undistortedRgbMat?.Dispose(); _undistortedRgbMat = null;

            ArHelper?.Dispose(); ArHelper = null;

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
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
        /// Raises the dictionary id dropdown value changed event.
        /// </summary>
        public void OnDictionaryIdDropdownValueChanged(int result)
        {
            if ((int)DictionaryId != result)
            {
                DictionaryId = (ArUcoDictionary)result;

                DetectMarkers();
            }
        }

        /// <summary>
        /// Raises the show rejected corners toggle value changed event.
        /// </summary>
        public void OnShowRejectedCornersToggleValueChanged()
        {
            if (ShowRejectedCorners != ShowRejectedCornersToggle.isOn)
            {
                ShowRejectedCorners = ShowRejectedCornersToggle.isOn;

                DetectMarkers();
            }
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

        // Private Methods
        private void DetectMarkers()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            OpenCVMatUtils.Texture2DToMat(ImgTexture, _rgbMat);
            Debug.Log("imgMat dst ToString " + _rgbMat.ToString());

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
            int max_d = (int)Mathf.Max(_rgbMat.width(), _rgbMat.height());
            double fx = max_d;
            double fy = max_d;
            double cx = _rgbMat.width() / 2.0f;
            double cy = _rgbMat.height() / 2.0f;
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
            Debug.Log("camMatrix " + camMatrix.dump());

            MatOfDouble distCoeffs = new MatOfDouble(0, 0, 0, 0);
            Debug.Log("distCoeffs " + distCoeffs.dump());

            // Initialize ARHelper.
            ArHelper.Initialize();
            // Set ARCamera parameters.
            ArHelper.ARCamera.SetCamMatrix(camMatrix);
            ArHelper.ARCamera.SetDistCoeffs(distCoeffs);
            ArHelper.ARCamera.SetARCameraParameters(Screen.width, Screen.height, _rgbMat.width(), _rgbMat.height(), Vector2.zero, new Vector2(imageSizeScale, imageSizeScale));

            Mat ids = new Mat();
            List<Mat> corners = new List<Mat>();
            List<Mat> rejectedCorners = new List<Mat>();

            MatOfPoint3f objectPoints = new MatOfPoint3f(
                new Point3(-MarkerLength / 2f, MarkerLength / 2f, 0),
                new Point3(MarkerLength / 2f, MarkerLength / 2f, 0),
                new Point3(MarkerLength / 2f, -MarkerLength / 2f, 0),
                new Point3(-MarkerLength / 2f, -MarkerLength / 2f, 0)
                );

            Dictionary dictionary = Objdetect.getPredefinedDictionary((int)DictionaryId);
            DetectorParameters detectorParams = new DetectorParameters();
            detectorParams.set_useAruco3Detection(true);
            detectorParams.set_cornerRefinementMethod(Objdetect.CORNER_REFINE_SUBPIX);
            RefineParameters refineParameters = new RefineParameters(10f, 3f, true);
            ArucoDetector arucoDetector = new ArucoDetector(dictionary, detectorParams, refineParameters);

            // undistort image.
            Calib3d.undistort(_rgbMat, _undistortedRgbMat, camMatrix, distCoeffs);
            // detect markers.
            arucoDetector.detectMarkers(_undistortedRgbMat, corners, ids, rejectedCorners);

            if (corners.Count == ids.total() || ids.total() == 0)
                Objdetect.drawDetectedMarkers(_undistortedRgbMat, corners, ids, new Scalar(0, 255, 0));

            // Reset ARGameObjects ImagePoints and ObjectPoints.
            ArHelper.ResetARGameObjectsImagePointsAndObjectPoints();

            // if at least one marker detected
            if (ids.total() > 0)
            {
                // estimate pose.
                if (ApplyEstimationPose)
                {
                    //Debug.Log("ids.dump() " + ids.dump());

                    int[] idsValues = new int[ids.total()];
                    ids.get(0, 0, idsValues);

                    for (int i = 0; i < idsValues.Length; i++)
                    {
                        using (Mat corner_4x1 = corners[i].reshape(2, 4)) // 1*4*CV_32FC2 => 4*1*CV_32FC2
                        using (MatOfPoint2f imagePoints = new MatOfPoint2f(corner_4x1))
                        {
                            string arUcoId = GetArUcoMarkerName("CanonicalMarker", DictionaryId.ToString(), idsValues[i]);
                            ARGameObject aRGameObject = FindOrCreateARGameObject(ArHelper.ARGameObjects, arUcoId, ArHelper.transform);

                            aRGameObject.ImagePoints = imagePoints.toVector2Array();
                            aRGameObject.ObjectPoints = objectPoints.toVector3Array();

                            // Calculate rvec and tvec for debug display and draw with Calib3d.drawFrameAxes()
                            using (Mat rvec = new Mat(3, 1, CvType.CV_64FC1))
                            using (Mat tvec = new Mat(3, 1, CvType.CV_64FC1))
                            {
                                // Calculate pose
                                Calib3d.solvePnP(objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);

                                // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                                OpenCVARUtils.SafeDrawFrameAxes(_undistortedRgbMat, camMatrix, distCoeffs, rvec, tvec, MarkerLength * 0.5f);
                            }
                        }
                    }
                }
            }

            if (ShowRejectedCorners && rejectedCorners.Count > 0)
                Objdetect.drawDetectedMarkers(_undistortedRgbMat, rejectedCorners, new Mat(), new Scalar(255, 0, 0));

            OpenCVMatUtils.MatToTexture2D(_undistortedRgbMat, _texture);

            camMatrix?.Dispose();
            distCoeffs?.Dispose();
            ids?.Dispose();
            if (rejectedCorners != null) foreach (var item in rejectedCorners) item.Dispose(); rejectedCorners.Clear();
            if (corners != null) foreach (var item in corners) item.Dispose(); corners.Clear();
            objectPoints?.Dispose();
            dictionary?.Dispose();
            detectorParams?.Dispose();
            refineParameters?.Dispose();
            arucoDetector?.Dispose();

            OpenCVDebug.SetDebugMode(false, false);
        }

        /// <summary>
        /// Returns the name of the ArUco marker.
        /// </summary>
        /// <param name="markerType"></param>
        /// <param name="dictionaryId"></param>
        /// <param name="markerIds"></param>
        /// <returns></returns>
        private string GetArUcoMarkerName(string markerType, string dictionaryId, params int[] markerIds)
        {
            if (markerIds.Length == 0)
                return markerType + " " + dictionaryId;

            return markerType + " " + dictionaryId + " " + string.Join(",", markerIds);
        }

        /// <summary>
        /// Finds or creates an ARGameObject with the specified AR marker name.
        /// </summary>
        /// <param name="arGameObjects"></param>
        /// <param name="arUcoId"></param>
        /// <param name="parentTransform"></param>
        /// <returns></returns>
        private ARGameObject FindOrCreateARGameObject(List<ARGameObject> arGameObjects, string arUcoId, Transform parentTransform)
        {
            ARGameObject FindARGameObjectByName(List<ARGameObject> arGameObjects, string targetName)
            {
                foreach (ARGameObject obj in arGameObjects)
                {
                    if (obj != null && obj.name == targetName)
                    {
                        return obj;
                    }
                }
                return null;
            }

            ARGameObject arGameObject = FindARGameObjectByName(arGameObjects, arUcoId);
            if (arGameObject == null)
            {
                arGameObject = Instantiate(ArCubePrefab, parentTransform).GetComponent<ARGameObject>();
                arGameObject.name = arUcoId;

                arGameObject.GetComponent<ARCube>().SetInfoPlateTexture(arUcoId);

                arGameObject.OnEnterARCameraViewport.AddListener(OnEnterARCameraViewport);
                arGameObject.OnExitARCameraViewport.AddListener(OnExitARCameraViewport);
                arGameObject.gameObject.SetActive(false);
                arGameObjects.Add(arGameObject);
            }
            return arGameObject;
        }
    }
}
