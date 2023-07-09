using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using System.Collections.Generic;
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
    /// </summary>
    public class ArUcoExample : MonoBehaviour
    {
        /// <summary>
        /// The image texture.
        /// </summary>
        public Texture2D imgTexture;

        [Space(10)]

        /// <summary>
        /// The dictionary identifier.
        /// </summary>
        public ArUcoDictionary dictionaryId = ArUcoDictionary.DICT_6X6_250;

        /// <summary>
        /// The dictionary id dropdown.
        /// </summary>
        public Dropdown dictionaryIdDropdown;

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

        /// <summary>
        /// The rgb mat.
        /// </summary>
        Mat rgbMat;

        /// <summary>
        /// The undistorted rgb mat.
        /// </summary>
        Mat undistortedRgbMat;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        // Use this for initialization
        void Start()
        {
            rgbMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC3);
            texture = new Texture2D(rgbMat.cols(), rgbMat.rows(), TextureFormat.RGBA32, false);
            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            dictionaryIdDropdown.value = (int)dictionaryId;
            showRejectedCornersToggle.isOn = showRejectedCorners;

            undistortedRgbMat = new Mat();

            DetectMarkers();
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void DetectMarkers()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            Utils.texture2DToMat(imgTexture, rgbMat);
            Debug.Log("imgMat dst ToString " + rgbMat.ToString());

            gameObject.transform.localScale = new Vector3(imgTexture.width, imgTexture.height, 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            float width = rgbMat.width();
            float height = rgbMat.height();

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
            Debug.Log("camMatrix " + camMatrix.dump());


            MatOfDouble distCoeffs = new MatOfDouble(0, 0, 0, 0);
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



            Mat ids = new Mat();
            List<Mat> corners = new List<Mat>();
            List<Mat> rejectedCorners = new List<Mat>();
            Mat rotMat = new Mat(3, 3, CvType.CV_64FC1);

            MatOfPoint3f objPoints = new MatOfPoint3f(
                new Point3(-markerLength / 2f, markerLength / 2f, 0),
                new Point3(markerLength / 2f, markerLength / 2f, 0),
                new Point3(markerLength / 2f, -markerLength / 2f, 0),
                new Point3(-markerLength / 2f, -markerLength / 2f, 0)
                );

            Dictionary dictionary = Objdetect.getPredefinedDictionary((int)dictionaryId);
            DetectorParameters detectorParams = new DetectorParameters();
            detectorParams.set_useAruco3Detection(true);
            detectorParams.set_cornerRefinementMethod(Objdetect.CORNER_REFINE_SUBPIX);
            RefineParameters refineParameters = new RefineParameters(10f, 3f, true);
            ArucoDetector arucoDetector = new ArucoDetector(dictionary, detectorParams, refineParameters);


            // undistort image.
            Calib3d.undistort(rgbMat, undistortedRgbMat, camMatrix, distCoeffs);
            // detect markers.
            arucoDetector.detectMarkers(undistortedRgbMat, corners, ids, rejectedCorners);

            if (corners.Count == ids.total() || ids.total() == 0)
                Objdetect.drawDetectedMarkers(undistortedRgbMat, corners, ids, new Scalar(0, 255, 0));

            // if at least one marker detected
            if (ids.total() > 0)
            {
                // estimate pose.
                if (applyEstimationPose)
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
                            Calib3d.drawFrameAxes(undistortedRgbMat, camMatrix, distCoeffs, rvec, tvec, markerLength * 0.5f);


                            // This example can display the ARObject on only first detected marker.
                            if (i == 0)
                            {
                                // Get translation vector
                                double[] tvecArr = new double[3];
                                tvec.get(0, 0, tvecArr);

                                // Get rotation vector
                                Mat rvec_3x1 = rvec.reshape(1, 3);

                                // Convert rotation vector to rotation matrix.
                                Calib3d.Rodrigues(rvec_3x1, rotMat);
                                double[] rotMatArr = new double[rotMat.total()];
                                rotMat.get(0, 0, rotMatArr);

                                // Convert OpenCV camera extrinsic parameters to Unity Matrix4x4.
                                Matrix4x4 transformationM = new Matrix4x4(); // from OpenCV
                                transformationM.SetRow(0, new Vector4((float)rotMatArr[0], (float)rotMatArr[1], (float)rotMatArr[2], (float)tvecArr[0]));
                                transformationM.SetRow(1, new Vector4((float)rotMatArr[3], (float)rotMatArr[4], (float)rotMatArr[5], (float)tvecArr[1]));
                                transformationM.SetRow(2, new Vector4((float)rotMatArr[6], (float)rotMatArr[7], (float)rotMatArr[8], (float)tvecArr[2]));
                                transformationM.SetRow(3, new Vector4(0, 0, 0, 1));
                                Debug.Log("transformationM " + transformationM.ToString());

                                Matrix4x4 invertYM = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1));
                                Debug.Log("invertYM " + invertYM.ToString());

                                // right-handed coordinates system (OpenCV) to left-handed one (Unity)
                                // https://stackoverflow.com/questions/30234945/change-handedness-of-a-row-major-4x4-transformation-matrix
                                Matrix4x4 ARM = invertYM * transformationM * invertYM;

                                if (shouldMoveARCamera)
                                {

                                    ARM = arGameObject.transform.localToWorldMatrix * ARM.inverse;

                                    Debug.Log("ARM " + ARM.ToString());

                                    ARUtils.SetTransformFromMatrix(arCamera.transform, ref ARM);

                                }
                                else
                                {

                                    ARM = arCamera.transform.localToWorldMatrix * ARM;

                                    Debug.Log("ARM " + ARM.ToString());

                                    ARUtils.SetTransformFromMatrix(arGameObject.transform, ref ARM);
                                }

                            }
                        }
                    }
                }
            }

            if (showRejectedCorners && rejectedCorners.Count > 0)
                Objdetect.drawDetectedMarkers(undistortedRgbMat, rejectedCorners, new Mat(), new Scalar(255, 0, 0));

            Utils.matToTexture2D(undistortedRgbMat, texture);


            Utils.setDebugMode(false, false);
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
            if (rgbMat != null)
                rgbMat.Dispose();

            if (undistortedRgbMat != null)
                undistortedRgbMat.Dispose();
        }

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
            if ((int)dictionaryId != result)
            {
                dictionaryId = (ArUcoDictionary)result;

                ResetObjectTransform();

                DetectMarkers();
            }
        }

        /// <summary>
        /// Raises the show rejected corners toggle value changed event.
        /// </summary>
        public void OnShowRejectedCornersToggleValueChanged()
        {
            if (showRejectedCorners != showRejectedCornersToggle.isOn)
            {
                showRejectedCorners = showRejectedCornersToggle.isOn;

                ResetObjectTransform();

                DetectMarkers();
            }
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