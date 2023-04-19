using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

using UnityEngine.SceneManagement;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Features2dModule;
using OpenCVForUnity.UnityUtils;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// SimpleBlob Example
    /// An example of feature detection using the SIMPLEBLOB algorithm.
    /// </summary>
    public class SimpleBlobExample : MonoBehaviour
    {
        string blobparams_yml_filepath;

#if UNITY_WEBGL
        Stack<IEnumerator> coroutines = new Stack<IEnumerator> ();
#endif

        // Use this for initialization
        void Start()
        {
#if UNITY_WEBGL
            var getFilePath_Coroutine = Utils.getFilePathAsync("OpenCVForUnity/features2d/blobparams.yml", (result) => {
                coroutines.Clear ();

                blobparams_yml_filepath = result;
                Run ();
            });
            coroutines.Push (getFilePath_Coroutine);
            StartCoroutine (getFilePath_Coroutine);
#else
            blobparams_yml_filepath = Utils.getFilePath("OpenCVForUnity/features2d/blobparams.yml");
            Run();
#endif
        }

        private void Run()
        {

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            Texture2D imgTexture = Resources.Load("detect_blob") as Texture2D;

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC1);

            Utils.texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString());

            Mat outImgMat = new Mat();


            //load Params from SimpleBlobDetector_Params class.
            SimpleBlobDetector_Params param = new SimpleBlobDetector_Params();
            param.set_thresholdStep(20.0f);
            param.set_minThreshold(10.0f);
            param.set_maxThreshold(200.0f);
            param.set_minRepeatability(2);
            param.set_minDistBetweenBlobs(10);
            param.set_filterByColor(false);
            param.set_filterByArea(false);
            param.set_minArea(1);
            param.set_maxArea(100000);
            param.set_filterByCircularity(false);
            param.set_minCircularity(1);
            param.set_maxCircularity(100000);
            param.set_filterByInertia(false);
            param.set_minInertiaRatio(1);
            param.set_maxInertiaRatio(100000);
            param.set_filterByConvexity(false);
            param.set_minConvexity(1);
            param.set_maxConvexity(100000);

            SimpleBlobDetector blobDetector = SimpleBlobDetector.create(param);
            Debug.Log("blobDetector.getDefaultName() " + blobDetector.getDefaultName());

            // or

            ////load Params from yml file.
            //SimpleBlobDetector blobDetector = SimpleBlobDetector.create();
            //Debug.Log("blobDetector.getDefaultName() " + blobDetector.getDefaultName());
            //blobDetector.read(blobparams_yml_filepath);


            MatOfKeyPoint keypoints = new MatOfKeyPoint();
            blobDetector.detect(imgMat, keypoints);
            Features2d.drawKeypoints(imgMat, keypoints, outImgMat);


            Texture2D texture = new Texture2D(outImgMat.cols(), outImgMat.rows(), TextureFormat.RGBA32, false);

            Utils.matToTexture2D(outImgMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;


            Utils.setDebugMode(false);
        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
#if UNITY_WEBGL
            foreach (var coroutine in coroutines) {
                StopCoroutine (coroutine);
                ((IDisposable)coroutine).Dispose ();
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
    }
}