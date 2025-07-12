using System.Collections.Generic;
using System.Threading;
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
    /// Feature Matching Example
    /// An example of descriptor extraction and descriptor matching.
    /// https://docs.opencv.org/4.8.0/d5/d6f/tutorial_feature_flann_matcher.html
    /// https://docs.opencv.org/4.8.0/db/d70/tutorial_akaze_matching.html
    /// </summary>
    public class FeatureMatchingExample : MonoBehaviour
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

            RunSiftFlannBasedMatching();
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

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnRunSiftFlannBasedMatchingButtonClick()
        {
            RunSiftFlannBasedMatching();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnRunAkazeBruteForceMatchingButtonClick()
        {
            RunAkazeBruteForceMatching();
        }

        // Private Methods
        /// The commercial license SURF feature descriptors are no longer included in OpenCV. The example has been changed to use SIFT feature descriptors instead.
        private void RunSiftFlannBasedMatching()
        {
            if (string.IsNullOrEmpty(_image0Filepath) || string.IsNullOrEmpty(_image1Filepath))
            {
                Debug.LogError(IMAGE_0_FILENAME + " or " + IMAGE_1_FILENAME + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
            }


            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);


            Mat img1Mat = Imgcodecs.imread(_image0Filepath, Imgcodecs.IMREAD_GRAYSCALE);
            Mat img2Mat = Imgcodecs.imread(_image1Filepath, Imgcodecs.IMREAD_GRAYSCALE);
            Mat img3Mat = img2Mat.clone();

            ///
            // Not all matching algorithms can be applied to all features. Some can be used and some cannot, as follows
            //
            // BruteForce (BRUTEFORCE, BRUTEFORCE_SL2, BRUTEFORCE_L1): can be used for anything
            // BruteForce-Hamming (BRUTEFORCE_HAMMING, BRUTEFORCE_HAMMINGLUT): can be used when the features are represented in binary code (ORB, AKAZE, etc)
            // FLANNBASED: can be used when features are represented as real vectors (SIFT, SURF, etc)
            ///

            //-- Step 1: Detect the keypoints using SIFT Detector, compute the descriptors
            List<Mat> images = new List<Mat>();
            List<MatOfKeyPoint> keypoints = new List<MatOfKeyPoint>();
            List<Mat> descriptors = new List<Mat>();

            // Test the input processing of multiple images.
            images.Add(img1Mat);
            images.Add(img2Mat);
            images.Add(img3Mat);

            SIFT detector = SIFT.create();

            detector.detect(images, keypoints);
            detector.compute(images, keypoints, descriptors);

            // Select image, keypoints, and descriptor for matching process.
            Mat img1 = images[0];
            Mat img2 = images[2];
            MatOfKeyPoint keypoints1 = keypoints[0];
            MatOfKeyPoint keypoints2 = keypoints[2];
            Mat descriptors1 = descriptors[0];
            Mat descriptors2 = descriptors[2];


            //-- Step 2: Matching descriptor vectors with a FLANN based matcher
            // Since SIFT is a floating-point descriptor NORM_L2 is used
            DescriptorMatcher matcher = DescriptorMatcher.create(DescriptorMatcher.FLANNBASED);
            List<MatOfDMatch> knnMatches = new List<MatOfDMatch>();
            matcher.knnMatch(descriptors1, descriptors2, knnMatches, 2);


            //-- Filter matches using the Lowe's ratio test
            float ratioThresh = 0.7f;
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
            Mat resultImg = new Mat();
            Features2d.drawMatches(img1, keypoints1, img2, keypoints2, goodMatches, resultImg);

            Imgproc.putText(resultImg, "SIFT_FLANNBASED Matching", new Point(5, resultImg.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

            Texture2D texture = new Texture2D(resultImg.cols(), resultImg.rows(), TextureFormat.RGB24, false);
            OpenCVMatUtils.MatToTexture2D(resultImg, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            OpenCVDebug.SetDebugMode(false);
        }

        private void RunAkazeBruteForceMatching()
        {
            if (string.IsNullOrEmpty(_image0Filepath) || string.IsNullOrEmpty(_image1Filepath))
            {
                Debug.LogError(IMAGE_0_FILENAME + " or " + IMAGE_1_FILENAME + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
            }


            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);


            Mat img1Mat = Imgcodecs.imread(_image0Filepath, Imgcodecs.IMREAD_GRAYSCALE);
            Mat img2Mat = Imgcodecs.imread(_image1Filepath, Imgcodecs.IMREAD_GRAYSCALE);
            Mat img3Mat = img2Mat.clone();

            ///
            // Not all matching algorithms can be applied to all features. Some can be used and some cannot, as follows
            //
            // BruteForce (BRUTEFORCE, BRUTEFORCE_SL2, BRUTEFORCE_L1): can be used for anything
            // BruteForce-Hamming (BRUTEFORCE_HAMMING, BRUTEFORCE_HAMMINGLUT): can be used when the features are represented in binary code (ORB, AKAZE, etc)
            // FLANNBASED: can be used when features are represented as real vectors (SIFT, SURF, etc)
            ///

            // Detect keypoints and compute descriptors using AKAZE.
            List<Mat> images = new List<Mat>();
            List<MatOfKeyPoint> keypoints = new List<MatOfKeyPoint>();
            List<Mat> descriptors = new List<Mat>();

            // Test the input processing of multiple images.
            images.Add(img1Mat);
            images.Add(img2Mat);
            images.Add(img3Mat);

            AKAZE detector = AKAZE.create();

            detector.detect(images, keypoints);
            detector.compute(images, keypoints, descriptors);

            // Select image, keypoints, and descriptor for matching process.
            Mat img1 = images[0];
            Mat img2 = images[2];
            MatOfKeyPoint keypoints1 = keypoints[0];
            MatOfKeyPoint keypoints2 = keypoints[2];
            Mat descriptors1 = descriptors[0];
            Mat descriptors2 = descriptors[2];


            //-- Use brute-force matcher to find 2-nn matches
            // We use Hamming distance, because AKAZE uses binary descriptor by default.
            DescriptorMatcher matcher = DescriptorMatcher.create(DescriptorMatcher.BRUTEFORCE_HAMMING);
            List<MatOfDMatch> knnMatches = new List<MatOfDMatch>();
            matcher.knnMatch(descriptors1, descriptors2, knnMatches, 2);


            //-- Filter matches using the Lowe's ratio test
            float ratioThresh = 0.7f;
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
            Mat resultImg = new Mat();
            Features2d.drawMatches(img1, keypoints1, img2, keypoints2, goodMatches, resultImg);

            Imgproc.putText(resultImg, "AKAZE_BRUTEFORCE Matching", new Point(5, resultImg.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

            Texture2D texture = new Texture2D(resultImg.cols(), resultImg.rows(), TextureFormat.RGB24, false);
            OpenCVMatUtils.MatToTexture2D(resultImg, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            OpenCVDebug.SetDebugMode(false);
        }
    }
}
