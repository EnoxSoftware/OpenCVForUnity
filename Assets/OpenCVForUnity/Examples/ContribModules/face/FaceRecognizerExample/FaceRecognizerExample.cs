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
    /// FaceRecognizer Example
    /// An example of human face recognition using the face (Face Recognition) module.
    /// http://docs.opencv.org/modules/contrib/doc/facerec/facerec_tutorial.html#eigenfaces
    /// </summary>
    public class FaceRecognizerExample : MonoBehaviour
    {
        string facerec_0_bmp_filepath;
        string facerec_1_bmp_filepath;
        string facerec_sample_bmp_filepath;

        #if UNITY_WEBGL && !UNITY_EDITOR
        Stack<IEnumerator> coroutines = new Stack<IEnumerator> ();
        #endif

        // Use this for initialization
        void Start ()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            var getFilePath_Coroutine = GetFilePath ();
            coroutines.Push (getFilePath_Coroutine);
            StartCoroutine (getFilePath_Coroutine);
            #else
            facerec_0_bmp_filepath = Utils.getFilePath ("facerec/facerec_0.bmp");
            facerec_1_bmp_filepath = Utils.getFilePath ("facerec/facerec_1.bmp");
            facerec_sample_bmp_filepath = Utils.getFilePath ("facerec/facerec_sample.bmp");
            Run ();
            #endif
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync ("facerec/facerec_0.bmp", (result) => {
                facerec_0_bmp_filepath = result;
            });
            coroutines.Push (getFilePathAsync_0_Coroutine);
            yield return StartCoroutine (getFilePathAsync_0_Coroutine);

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync ("facerec/facerec_1.bmp", (result) => {
                facerec_1_bmp_filepath = result;
            });
            coroutines.Push (getFilePathAsync_1_Coroutine);
            yield return StartCoroutine (getFilePathAsync_1_Coroutine);

            var getFilePathAsync_sample_Coroutine = Utils.getFilePathAsync ("facerec/facerec_sample.bmp", (result) => {
                facerec_sample_bmp_filepath = result;
            });
            coroutines.Push (getFilePathAsync_sample_Coroutine);
            yield return StartCoroutine (getFilePathAsync_sample_Coroutine);

            coroutines.Clear ();

            Run ();
        }
        #endif

        private void Run ()
        {
            List<Mat> images = new List<Mat> ();
            List<int> labelsList = new List<int> ();
            MatOfInt labels = new MatOfInt ();
            images.Add (Imgcodecs.imread (facerec_0_bmp_filepath, 0));
            images.Add (Imgcodecs.imread (facerec_1_bmp_filepath, 0));
            labelsList.Add (0);
            labelsList.Add (1);
            labels.fromList (labelsList);

            Mat testSampleMat = Imgcodecs.imread (facerec_sample_bmp_filepath, 0);
            int testSampleLabel = 0;


            //                      foreach (Mat item in images) {
            //                              Debug.Log ("images.ToString " + item.ToString ());
            //                      }
            //                      foreach (int item in labelsList) {
            //                              Debug.Log ("labels.ToString " + item.ToString ());
            //                      }

            int[] predictedLabel = new int[1];
            double[] predictedConfidence = new double[1];


            BasicFaceRecognizer faceRecognizer = EigenFaceRecognizer.create ();

            faceRecognizer.train (images, labels);

            faceRecognizer.predict (testSampleMat, predictedLabel, predictedConfidence);


            Debug.Log ("Predicted class: " + predictedLabel [0] + " / " + "Actual class: " + testSampleLabel);
            Debug.Log ("Confidence: " + predictedConfidence [0]);


            Mat predictedMat = images [predictedLabel [0]];

            Mat baseMat = new Mat (testSampleMat.rows (), predictedMat.cols () + testSampleMat.cols (), CvType.CV_8UC1);
            predictedMat.copyTo (baseMat.submat (new OpenCVForUnity.Rect (0, 0, predictedMat.cols (), predictedMat.rows ())));
            testSampleMat.copyTo (baseMat.submat (new OpenCVForUnity.Rect (predictedMat.cols (), 0, testSampleMat.cols (), testSampleMat.rows ())));

            Imgproc.putText (baseMat, "Predicted", new Point (10, 15), Core.FONT_HERSHEY_SIMPLEX, 0.4, new Scalar (255), 1, Imgproc.LINE_AA, false);
            Imgproc.putText (baseMat, "Confidence:", new Point (5, 25), Core.FONT_HERSHEY_SIMPLEX, 0.2, new Scalar (255), 1, Imgproc.LINE_AA, false);
            Imgproc.putText (baseMat, "   " + predictedConfidence [0], new Point (5, 33), Core.FONT_HERSHEY_SIMPLEX, 0.2, new Scalar (255), 1, Imgproc.LINE_AA, false);
            Imgproc.putText (baseMat, "TestSample", new Point (predictedMat.cols () + 10, 15), Core.FONT_HERSHEY_SIMPLEX, 0.4, new Scalar (255), 1, Imgproc.LINE_AA, false);


            Texture2D texture = new Texture2D (baseMat.cols (), baseMat.rows (), TextureFormat.RGBA32, false);

            Utils.matToTexture2D (baseMat, texture);

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