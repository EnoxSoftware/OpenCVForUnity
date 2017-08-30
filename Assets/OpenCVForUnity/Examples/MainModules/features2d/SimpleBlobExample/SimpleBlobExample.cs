using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// SimpleBlob Example
    /// An example of feature detection using the SIMPLEBLOB algorithm.
    /// </summary>
    public class SimpleBlobExample : MonoBehaviour
    {
        string blobparams_yml_filepath;

        #if UNITY_WEBGL && !UNITY_EDITOR
        Stack<IEnumerator> coroutines = new Stack<IEnumerator> ();
        #endif

        // Use this for initialization
        void Start ()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            var getFilePath_Coroutine = Utils.getFilePathAsync("blobparams.yml", (result) => {
                coroutines.Clear ();

                blobparams_yml_filepath = result;
                Run ();
            });
            coroutines.Push (getFilePath_Coroutine);
            StartCoroutine (getFilePath_Coroutine);
            #else
            blobparams_yml_filepath = Utils.getFilePath ("blobparams.yml");
            Run ();
            #endif
        }

        private void Run ()
        {
            Texture2D imgTexture = Resources.Load ("detect_blob") as Texture2D;

            Mat imgMat = new Mat (imgTexture.height, imgTexture.width, CvType.CV_8UC1);

            Utils.texture2DToMat (imgTexture, imgMat);
            Debug.Log ("imgMat.ToString() " + imgMat.ToString ());

            Mat outImgMat = new Mat ();

            FeatureDetector blobDetector = FeatureDetector.create (FeatureDetector.SIMPLEBLOB);

            blobDetector.read (blobparams_yml_filepath);



            MatOfKeyPoint keypoints = new MatOfKeyPoint ();
            blobDetector.detect (imgMat, keypoints);
            Features2d.drawKeypoints (imgMat, keypoints, outImgMat);


            Texture2D texture = new Texture2D (outImgMat.cols (), outImgMat.rows (), TextureFormat.RGBA32, false);

            Utils.matToTexture2D (outImgMat, texture);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
        }

        // Update is called once per frame
        void Update ()
        {

        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            foreach (var coroutine in coroutines) {
                StopCoroutine (coroutine);
                ((IDisposable)coroutine).Dispose ();
            }
            #endif
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
    }
}