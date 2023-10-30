using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Features2dModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        /// <summary>
        /// IMAGE_0_FILENAME
        /// </summary>
        protected static readonly string IMAGE_0_FILENAME = "OpenCVForUnity/features2d/box.png";

        /// <summary>
        /// The image 0 filepath.
        /// </summary>
        string image_0_filepath;

        /// <summary>
        /// IMAGE_1_FILENAME
        /// </summary>
        protected static readonly string IMAGE_1_FILENAME = "OpenCVForUnity/features2d/box_in_scene.png";

        /// <summary>
        /// The image 1 filepath.
        /// </summary>
        string image_1_filepath;
#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        void Start()
        {
#if UNITY_WEBGL
            getFilePath_Coroutine = GetFilePath ();
            StartCoroutine (getFilePath_Coroutine);
#else
            image_0_filepath = Utils.getFilePath(IMAGE_0_FILENAME);
            image_1_filepath = Utils.getFilePath(IMAGE_1_FILENAME);
            Run_SIFT_FLANNBASEDMatching();
#endif
        }

#if UNITY_WEBGL
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync (IMAGE_0_FILENAME, (result) => {
                image_0_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync (IMAGE_1_FILENAME, (result) => {
                image_1_filepath = result;
            });
            yield return getFilePathAsync_1_Coroutine;


            getFilePath_Coroutine = null;

            Run_SIFT_FLANNBASEDMatching();
        }
#endif

        /// The commercial license SURF feature descriptors are no longer included in OpenCV. The example has been changed to use SIFT feature descriptors instead.
        private void Run_SIFT_FLANNBASEDMatching()
        {
            if (string.IsNullOrEmpty(image_0_filepath) || string.IsNullOrEmpty(image_1_filepath))
            {
                Debug.LogError(IMAGE_0_FILENAME + " or " + IMAGE_1_FILENAME + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
            }


            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            Mat img1Mat = Imgcodecs.imread(image_0_filepath, Imgcodecs.IMREAD_GRAYSCALE);
            Mat img2Mat = Imgcodecs.imread(image_1_filepath, Imgcodecs.IMREAD_GRAYSCALE);
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
            Utils.matToTexture2D(resultImg, texture);
            gameObject.GetComponent<Renderer>().material.mainTexture = texture;            


            Utils.setDebugMode(false, false);
        }

        private void Run_AKAZE_BRUTEFORCEMatching()
        {
            if (string.IsNullOrEmpty(image_0_filepath) || string.IsNullOrEmpty(image_1_filepath))
            {
                Debug.LogError(IMAGE_0_FILENAME + " or " + IMAGE_1_FILENAME + " is not loaded. Please move from “OpenCVForUnity/StreamingAssets/OpenCVForUnity/” to “Assets/StreamingAssets/OpenCVForUnity/” folder.");
            }


            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            Mat img1Mat = Imgcodecs.imread(image_0_filepath, Imgcodecs.IMREAD_GRAYSCALE);
            Mat img2Mat = Imgcodecs.imread(image_1_filepath, Imgcodecs.IMREAD_GRAYSCALE);
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
            Utils.matToTexture2D(resultImg, texture);
            gameObject.GetComponent<Renderer>().material.mainTexture = texture;


            Utils.setDebugMode(false, false);
        }

        // Update is called once per frame
        void Update()
        {

        }

        void OnDestroy()
        {
#if UNITY_WEBGL
            if (getFilePath_Coroutine != null) {
                StopCoroutine (getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose ();
            }
#endif
        }

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
        public void OnRun_SIFT_FLANNBASEDMatchingButtonClick()
        {
            Run_SIFT_FLANNBASEDMatching();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnRun_AKAZE_BRUTEFORCEMatchingButtonClick()
        {
            Run_AKAZE_BRUTEFORCEMatching();
        }
    }
}