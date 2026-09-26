using System.Collections.Generic;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.FeaturesModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using OpenCVForUnity.Xfeatures2dModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenCVDebug = OpenCVForUnity.Extensions.OpenCVDebug;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Feature Matching Example
    /// Matches descriptors between two images using SIFT+FLANN or AKAZE+BruteForce-Hamming pipelines.
    ///
    /// Demonstrates:
    /// - Batch keypoint detection and descriptor computation on multiple images
    /// - k-NN matching with Lowe's ratio test for outlier rejection
    /// - Side-by-side match visualization with <see cref="Features.drawMatches"/>
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="Point"/>, <see cref="Scalar"/>, <see cref="MatOfKeyPoint"/>, <see cref="MatOfDMatch"/>
    /// - <see cref="SIFT"/>, <see cref="AKAZE"/>, <see cref="DescriptorMatcher"/>, <see cref="Features"/>, <see cref="DMatch"/>
    /// - <see cref="Imgcodecs"/>: imread
    /// - <see cref="Imgproc"/>: putText
    /// - <see cref="OpenCVMatUnityUtils"/>, <see cref="OpenCVForUnityEnv"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://docs.opencv.org/4.8.0/d5/d6f/tutorial_feature_flann_matcher.html
    /// https://docs.opencv.org/4.8.0/db/d70/tutorial_akaze_matching.html
    /// </para>
    /// </remarks>
    public class FeatureMatchingExample : MonoBehaviour
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

            RunSiftFlannBasedMatching();
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
        // The commercial license SURF feature descriptors are no longer included in OpenCV. The example has been changed to use SIFT feature descriptors instead.
        /// <summary>
        /// Runs SIFT detection and FLANN-based k-NN matching between two images.
        /// </summary>
        private void RunSiftFlannBasedMatching()
        {
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

            Mat img1Mat = Imgcodecs.imread(_image0Filepath, Imgcodecs.IMREAD_GRAYSCALE);
            Mat img2Mat = Imgcodecs.imread(_image1Filepath, Imgcodecs.IMREAD_GRAYSCALE);
            Mat img3Mat = img2Mat.clone();

            // Matcher compatibility: BruteForce works for any descriptor; Hamming for binary (ORB/AKAZE); FLANN for float (SIFT/SURF).
            ///
            // Not all matching algorithms can be applied to all features. Some can be used and some cannot, as follows
            //
            // BruteForce (BRUTEFORCE, BRUTEFORCE_SL2, BRUTEFORCE_L1): can be used for anything
            // BruteForce-Hamming (BRUTEFORCE_HAMMING, BRUTEFORCE_HAMMINGLUT): can be used when the features are represented in binary code (ORB, AKAZE, etc)
            // FLANNBASED: can be used when features are represented as real vectors (SIFT, SURF, etc)
            ///

            // Step 1: Detect keypoints and compute SIFT descriptors on three images.
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

            // Select image pair and descriptors for matching (image 0 vs cloned image 2).
            Mat img1 = images[0];
            Mat img2 = images[2];
            MatOfKeyPoint keypoints1 = keypoints[0];
            MatOfKeyPoint keypoints2 = keypoints[2];
            Mat descriptors1 = descriptors[0];
            Mat descriptors2 = descriptors[2];

            // Step 2: Match floating-point descriptors with a FLANN-based k-NN matcher.
            DescriptorMatcher matcher = DescriptorMatcher.create(DescriptorMatcher.FLANNBASED);
            List<MatOfDMatch> knnMatches = new List<MatOfDMatch>();
            matcher.knnMatch(descriptors1, descriptors2, knnMatches, 2);

            // Apply Lowe's ratio test to keep reliable matches.
            float ratioThresh = 0.7f;
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
            Mat resultImg = new Mat();
            Features.drawMatches(img1, keypoints1, img2, keypoints2, goodMatches, resultImg);

            Imgproc.putText(resultImg, "SIFT_FLANNBASED Matching", new Point(5, resultImg.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

            Texture2D texture = new Texture2D(resultImg.cols(), resultImg.rows(), TextureFormat.RGB24, false);
            OpenCVMatUnityUtils.MatToTexture2D(resultImg, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;

            OpenCVDebug.SetDebugMode(false);
        }

        /// <summary>
        /// Runs AKAZE detection and BruteForce-Hamming k-NN matching between two images.
        /// </summary>
        private void RunAkazeBruteForceMatching()
        {
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

            Mat img1Mat = Imgcodecs.imread(_image0Filepath, Imgcodecs.IMREAD_GRAYSCALE);
            Mat img2Mat = Imgcodecs.imread(_image1Filepath, Imgcodecs.IMREAD_GRAYSCALE);
            Mat img3Mat = img2Mat.clone();

            // Matcher compatibility: BruteForce works for any descriptor; Hamming for binary (ORB/AKAZE); FLANN for float (SIFT/SURF).
            ///
            // Not all matching algorithms can be applied to all features. Some can be used and some cannot, as follows
            //
            // BruteForce (BRUTEFORCE, BRUTEFORCE_SL2, BRUTEFORCE_L1): can be used for anything
            // BruteForce-Hamming (BRUTEFORCE_HAMMING, BRUTEFORCE_HAMMINGLUT): can be used when the features are represented in binary code (ORB, AKAZE, etc)
            // FLANNBASED: can be used when features are represented as real vectors (SIFT, SURF, etc)
            ///

            // Detect keypoints and compute binary AKAZE descriptors on three images.
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

            // Select image pair and descriptors for matching (image 0 vs cloned image 2).
            Mat img1 = images[0];
            Mat img2 = images[2];
            MatOfKeyPoint keypoints1 = keypoints[0];
            MatOfKeyPoint keypoints2 = keypoints[2];
            Mat descriptors1 = descriptors[0];
            Mat descriptors2 = descriptors[2];

            // Match binary descriptors with Hamming distance (suitable for AKAZE/ORB).
            DescriptorMatcher matcher = DescriptorMatcher.create(DescriptorMatcher.BRUTEFORCE_HAMMING);
            List<MatOfDMatch> knnMatches = new List<MatOfDMatch>();
            matcher.knnMatch(descriptors1, descriptors2, knnMatches, 2);

            // Apply Lowe's ratio test to keep reliable matches.
            float ratioThresh = 0.7f;
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
            Mat resultImg = new Mat();
            Features.drawMatches(img1, keypoints1, img2, keypoints2, goodMatches, resultImg);

            Imgproc.putText(resultImg, "AKAZE_BRUTEFORCE Matching", new Point(5, resultImg.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

            Texture2D texture = new Texture2D(resultImg.cols(), resultImg.rows(), TextureFormat.RGB24, false);
            OpenCVMatUnityUtils.MatToTexture2D(resultImg, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;

            OpenCVDebug.SetDebugMode(false);
        }
    }
}
