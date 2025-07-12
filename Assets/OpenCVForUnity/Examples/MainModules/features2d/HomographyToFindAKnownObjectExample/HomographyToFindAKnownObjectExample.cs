using System.Collections.Generic;
using System.Threading;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Features2dModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Homography To Find A Known Object Example
    /// In this tutorial you will learn how to:
    ///  - Use the function cv::findHomography to find the transform between matched keypoints.
    ///  - Use the function cv::perspectiveTransform to map the points.
    /// Referring to https://docs.opencv.org/3.4/d7/dff/tutorial_feature_homography.html
    /// </summary>
    public class HomographyToFindAKnownObjectExample : MonoBehaviour
    {
        // Constants
        private static readonly string IMAGE_0_FILENAME = "OpenCVForUnityExamples/features2d/box.png";
        private static readonly string IMAGE_1_FILENAME = "OpenCVForUnityExamples/features2d/box_in_scene.png";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
        private string _image0Filepath;
        private string _image1Filepath;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _image0Filepath = await OpenCVEnv.GetFilePathTaskAsync(IMAGE_0_FILENAME, cancellationToken: _cts.Token);
            _image1Filepath = await OpenCVEnv.GetFilePathTaskAsync(IMAGE_1_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";


            if (string.IsNullOrEmpty(_image0Filepath) || string.IsNullOrEmpty(_image1Filepath))
            {
                Debug.LogError(IMAGE_0_FILENAME + " or " + IMAGE_1_FILENAME + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
            }



            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);


            Mat imgObject = Imgcodecs.imread(_image0Filepath, Imgcodecs.IMREAD_GRAYSCALE);
            Mat imgScene = Imgcodecs.imread(_image1Filepath, Imgcodecs.IMREAD_GRAYSCALE);

            /// The commercial license SURF feature descriptors are no longer included in OpenCV. The example has been changed to use SIFT feature descriptors instead.

            //-- Step 1: Detect the keypoints using SIFT Detector, compute the descriptors
            SIFT detector = SIFT.create();
            MatOfKeyPoint keypointsObject = new MatOfKeyPoint(), keypointsScene = new MatOfKeyPoint();
            Mat descriptorsObject = new Mat(), descriptorsScene = new Mat();
            detector.detectAndCompute(imgObject, new Mat(), keypointsObject, descriptorsObject);
            detector.detectAndCompute(imgScene, new Mat(), keypointsScene, descriptorsScene);


            //-- Step 2: Matching descriptor vectors with a FLANN based matcher
            // Since SIFT is a floating-point descriptor NORM_L2 is used
            DescriptorMatcher matcher = DescriptorMatcher.create(DescriptorMatcher.FLANNBASED);
            List<MatOfDMatch> knnMatches = new List<MatOfDMatch>();
            matcher.knnMatch(descriptorsObject, descriptorsScene, knnMatches, 2);


            //-- Filter matches using the Lowe's ratio test
            float ratioThresh = 0.75f;
            List<DMatch> listOfGoodMatches = new List<DMatch>();
            for (int i = 0; i < knnMatches.Count; i++)
            {
                if (knnMatches[i].rows() > 1)
                {
                    DMatch[] matches = knnMatches[i].toArray();
                    if (matches[0].distance < ratioThresh * matches[1].distance)
                    {
                        listOfGoodMatches.Add(matches[0]);
                    }
                }
            }
            MatOfDMatch goodMatches = new MatOfDMatch();
            goodMatches.fromList(listOfGoodMatches);


            //-- Draw matches
            Mat imgMatches = new Mat();
            Features2d.drawMatches(imgObject, keypointsObject, imgScene, keypointsScene, goodMatches, imgMatches, Scalar.all(-1),
                Scalar.all(-1), new MatOfByte(), Features2d.DrawMatchesFlags_NOT_DRAW_SINGLE_POINTS);


            //-- Localize the object
            List<Point> objList = new List<Point>();
            List<Point> sceneList = new List<Point>();
            List<KeyPoint> listOfKeypointsObject = keypointsObject.toList();
            List<KeyPoint> listOfKeypointsScene = keypointsScene.toList();
            for (int i = 0; i < listOfGoodMatches.Count; i++)
            {
                //-- Get the keypoints from the good matches
                objList.Add(listOfKeypointsObject[listOfGoodMatches[i].queryIdx].pt);
                sceneList.Add(listOfKeypointsScene[listOfGoodMatches[i].trainIdx].pt);
            }

            MatOfPoint2f objMat = new MatOfPoint2f(objList.ToArray());
            MatOfPoint2f sceneMat = new MatOfPoint2f(sceneList.ToArray());
            double ransacReprojThreshold = 3.0;
            Mat H = Calib3d.findHomography(objMat, sceneMat, Calib3d.RANSAC, ransacReprojThreshold);


            //-- Get the corners from the image_1 ( the object to be "detected" )
            List<Point> objCornersList = new List<Point>(4);
            objCornersList.Add(new Point(0, 0));
            objCornersList.Add(new Point(imgObject.cols(), 0));
            objCornersList.Add(new Point(imgObject.cols(), imgObject.rows()));
            objCornersList.Add(new Point(0, imgObject.rows()));
            List<Point> sceneCornersList = new List<Point>(4);
            MatOfPoint2f objCorners = new MatOfPoint2f(objCornersList.ToArray());
            MatOfPoint2f sceneCorners = new MatOfPoint2f(sceneCornersList.ToArray());

            Core.perspectiveTransform(objCorners, sceneCorners, H);

            sceneCornersList = sceneCorners.toList();


            //-- Draw lines between the corners (the mapped object in the scene - image_2 )
            Imgproc.line(imgMatches, sceneCornersList[0] + new Point(imgObject.cols(), 0), sceneCornersList[1] + new Point(imgObject.cols(), 0), new Scalar(0, 255, 0), 4);
            Imgproc.line(imgMatches, sceneCornersList[1] + new Point(imgObject.cols(), 0), sceneCornersList[2] + new Point(imgObject.cols(), 0), new Scalar(0, 255, 0), 4);
            Imgproc.line(imgMatches, sceneCornersList[2] + new Point(imgObject.cols(), 0), sceneCornersList[3] + new Point(imgObject.cols(), 0), new Scalar(0, 255, 0), 4);
            Imgproc.line(imgMatches, sceneCornersList[3] + new Point(imgObject.cols(), 0), sceneCornersList[0] + new Point(imgObject.cols(), 0), new Scalar(0, 255, 0), 4);

            Imgproc.putText(imgMatches, "SIFT_FLANNBASED Matching + Homography", new Point(5, imgMatches.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);


            //-- Show detected matches
            Texture2D texture = new Texture2D(imgMatches.cols(), imgMatches.rows(), TextureFormat.RGB24, false);
            OpenCVMatUtils.MatToTexture2D(imgMatches, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            OpenCVDebug.SetDebugMode(false);
        }


        private void Update()
        {

        }

        private void OnDestroy()
        {
            _cts?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }
    }
}
