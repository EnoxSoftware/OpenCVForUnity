using System.Collections.Generic;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.FeaturesModule;
using OpenCVForUnity.GeometryModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenCVDebug = OpenCVForUnity.Extensions.OpenCVDebug;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Homography To Find A Known Object Example
    /// Locates a known planar object in a scene image using feature matching and homography.
    ///
    /// Demonstrates:
    /// - SIFT keypoint detection and descriptor extraction
    /// - FLANN-based k-NN matching with Lowe's ratio test
    /// - Homography estimation with <see cref="Geometry.findHomography"/> (RANSAC)
    /// - Mapping object corners via <see cref="Core.perspectiveTransform"/>
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Point"/>, <see cref="Scalar"/>, <see cref="MatOfKeyPoint"/>, <see cref="MatOfDMatch"/>, <see cref="MatOfPoint2f"/>
    /// - <see cref="SIFT"/>, <see cref="DescriptorMatcher"/>, <see cref="Features"/>, <see cref="DMatch"/>
    /// - <see cref="Imgcodecs"/>: imread
    /// - <see cref="Imgproc"/>: line, putText
    /// - <see cref="Geometry"/>: findHomography, RANSAC
    /// - <see cref="Core"/>: perspectiveTransform
    /// - <see cref="OpenCVMatUnityUtils"/>, <see cref="OpenCVForUnityEnv"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://docs.opencv.org/3.4/d7/dff/tutorial_feature_homography.html
    /// </para>
    /// </remarks>
    public class HomographyToFindAKnownObjectExample : MonoBehaviour
    {
        // Constants
        private static readonly string IMAGE_0_FILEPATH = "OpenCVForUnityExamples/features/box.png";
        private static readonly string IMAGE_1_FILEPATH = "OpenCVForUnityExamples/features/box_in_scene.png";

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

        private FpsMonitor _fpsMonitor;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            _image0Filepath = await OpenCVForUnityEnv.GetFilePathAsync(IMAGE_0_FILEPATH, cancellationToken: _cts.Token);
            _image1Filepath = await OpenCVForUnityEnv.GetFilePathAsync(IMAGE_1_FILEPATH, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            if (string.IsNullOrEmpty(_image0Filepath) || string.IsNullOrEmpty(_image1Filepath))
            {
                Debug.LogError(IMAGE_0_FILEPATH + " or " + IMAGE_1_FILEPATH + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("image file is not loaded.\nPlease read console message.", 20000);
                }

                return;
            }

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            Mat imgObject = Imgcodecs.imread(_image0Filepath, Imgcodecs.IMREAD_GRAYSCALE);
            Mat imgScene = Imgcodecs.imread(_image1Filepath, Imgcodecs.IMREAD_GRAYSCALE);

            /// The commercial license SURF feature descriptors are no longer included in OpenCV. The example has been changed to use SIFT feature descriptors instead.

            // Step 1: Detect keypoints and compute SIFT descriptors for object and scene.
            SIFT detector = SIFT.create();
            MatOfKeyPoint keypointsObject = new MatOfKeyPoint(), keypointsScene = new MatOfKeyPoint();
            Mat descriptorsObject = new Mat(), descriptorsScene = new Mat();
            detector.detectAndCompute(imgObject, new Mat(), keypointsObject, descriptorsObject);
            detector.detectAndCompute(imgScene, new Mat(), keypointsScene, descriptorsScene);

            // Step 2: Match descriptors with a FLANN-based k-NN matcher (NORM_L2 for SIFT).
            DescriptorMatcher matcher = DescriptorMatcher.create(DescriptorMatcher.FLANNBASED);
            List<MatOfDMatch> knnMatches = new List<MatOfDMatch>();
            matcher.knnMatch(descriptorsObject, descriptorsScene, knnMatches, 2);

            // Filter matches using Lowe's ratio test (keep best match if clearly better than second).
            float ratioThresh = 0.75f;
            List<DMatch> listOfGoodMatches = new List<DMatch>();
            for (int i = 0; i < knnMatches.Count; i++)
            {
                if (knnMatches[i].total() > 1)
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

            // Draw side-by-side match visualization.
            Mat imgMatches = new Mat();
            Features.drawMatches(imgObject, keypointsObject, imgScene, keypointsScene, goodMatches, imgMatches, Scalar.all(-1),
                Scalar.all(-1), new MatOfByte(), Features.DrawMatchesFlags_NOT_DRAW_SINGLE_POINTS);

            // Collect matched point pairs for homography estimation.
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
            // Estimate the perspective transform from object to scene coordinates.
            Mat homography = Geometry.findHomography(objMat, sceneMat, Geometry.RANSAC, ransacReprojThreshold);

            // Define the four corners of the object image in its local coordinates.
            List<Point> objCornersList = new List<Point>(4);
            objCornersList.Add(new Point(0, 0));
            objCornersList.Add(new Point(imgObject.cols(), 0));
            objCornersList.Add(new Point(imgObject.cols(), imgObject.rows()));
            objCornersList.Add(new Point(0, imgObject.rows()));
            List<Point> sceneCornersList = new List<Point>(4);
            MatOfPoint2f objCorners = new MatOfPoint2f(objCornersList.ToArray());
            MatOfPoint2f sceneCorners = new MatOfPoint2f(sceneCornersList.ToArray());

            // Map object corners into the scene image via the homography.
            Core.perspectiveTransform(objCorners, sceneCorners, homography);

            sceneCornersList = sceneCorners.toList();

            // Draw the projected object boundary on the scene side of the match image.
            Imgproc.line(imgMatches, sceneCornersList[0] + new Point(imgObject.cols(), 0), sceneCornersList[1] + new Point(imgObject.cols(), 0), new Scalar(0, 255, 0), 4);
            Imgproc.line(imgMatches, sceneCornersList[1] + new Point(imgObject.cols(), 0), sceneCornersList[2] + new Point(imgObject.cols(), 0), new Scalar(0, 255, 0), 4);
            Imgproc.line(imgMatches, sceneCornersList[2] + new Point(imgObject.cols(), 0), sceneCornersList[3] + new Point(imgObject.cols(), 0), new Scalar(0, 255, 0), 4);
            Imgproc.line(imgMatches, sceneCornersList[3] + new Point(imgObject.cols(), 0), sceneCornersList[0] + new Point(imgObject.cols(), 0), new Scalar(0, 255, 0), 4);

            Imgproc.putText(imgMatches, "SIFT_FLANNBASED Matching + Homography", new Point(5, imgMatches.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

            //-- Show detected matches
            Texture2D texture = new Texture2D(imgMatches.cols(), imgMatches.rows(), TextureFormat.RGB24, false);
            OpenCVMatUnityUtils.MatToTexture2D(imgMatches, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;

            OpenCVDebug.SetDebugMode(false);
        }

        private void Update()
        {

        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
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
