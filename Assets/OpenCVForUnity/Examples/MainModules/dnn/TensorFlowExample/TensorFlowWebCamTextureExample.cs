#if !UNITY_WSA_10_0

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// TensorFlow WebCamTexture Example
    /// An example of using OpenCV dnn module with Tensorflow Inception model.
    /// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/tf_inception.cpp.
    /// </summary>
    [RequireComponent (typeof(WebCamTextureToMatHelper))]
    public class TensorFlowWebCamTextureExample : MonoBehaviour
    {
        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The bgr mat.
        /// </summary>
        Mat bgrMat;

        /// <summary>
        /// The BLOB.
        /// </summary>
        Mat blob;

        /// <summary>
        /// The net.
        /// </summary>
        Net net;

        /// <summary>
        /// The classes.
        /// </summary>
        List<string> classes;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        #if UNITY_ANDROID && !UNITY_EDITOR
        float rearCameraRequestedFPS;
        #endif

        string tensorflow_inception_graph_pb_filepath;
        string imagenet_comp_graph_label_strings_txt_filepath;

        #if UNITY_WEBGL && !UNITY_EDITOR
        Stack<IEnumerator> coroutines = new Stack<IEnumerator> ();
        #endif

        // Use this for initialization
        void Start ()
        {
            fpsMonitor = GetComponent<FpsMonitor> ();

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();

            #if UNITY_WEBGL && !UNITY_EDITOR
            var getFilePath_Coroutine = GetFilePath ();
            coroutines.Push (getFilePath_Coroutine);
            StartCoroutine (getFilePath_Coroutine);
            #else
            tensorflow_inception_graph_pb_filepath = Utils.getFilePath ("dnn/tensorflow_inception_graph.pb");
            imagenet_comp_graph_label_strings_txt_filepath = Utils.getFilePath ("dnn/imagenet_comp_graph_label_strings.txt");
            Run ();
            #endif
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator GetFilePath()
        {

            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync ("dnn/tensorflow_inception_graph.pb", (result) => {
                tensorflow_inception_graph_pb_filepath = result;
            });
            coroutines.Push (getFilePathAsync_0_Coroutine);
            yield return StartCoroutine (getFilePathAsync_0_Coroutine);

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync ("dnn/imagenet_comp_graph_label_strings.txt", (result) => {
                imagenet_comp_graph_label_strings_txt_filepath = result;
            });
            coroutines.Push (getFilePathAsync_1_Coroutine);
            yield return StartCoroutine (getFilePathAsync_1_Coroutine);

            coroutines.Clear ();

            Run ();
        }
        #endif

        // Use this for initialization
        void Run ()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode (true);


            net = Dnn.readNetFromTensorflow (tensorflow_inception_graph_pb_filepath);
            #if !UNITY_WSA_10_0
            if (net.empty ()) {
                Debug.LogError ("model file is not loaded.The model and class names list can be downloaded here: \"https://storage.googleapis.com/download.tensorflow.org/models/inception5h.zip\".Please copy to “Assets/StreamingAssets/dnn/” folder. ");
            }
            #endif
            classes = readClassNames (imagenet_comp_graph_label_strings_txt_filepath);
            #if !UNITY_WSA_10_0
            if (classes == null) {
                Debug.LogError ("class names list file is not loaded.The model and class names list can be downloaded here: \"https://storage.googleapis.com/download.tensorflow.org/models/inception5h.zip\".Please copy to “Assets/StreamingAssets/dnn/” folder. ");
            }
            #endif
            
            #if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices. (Pixel, pixel 2)
            // https://forum.unity.com/threads/android-webcamtexture-in-low-light-only-some-models.520656/
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            rearCameraRequestedFPS = webCamTextureToMatHelper.requestedFPS;
            if (webCamTextureToMatHelper.requestedIsFrontFacing) {                
                webCamTextureToMatHelper.requestedFPS = 15;
                webCamTextureToMatHelper.Initialize ();
            } else {
                webCamTextureToMatHelper.Initialize ();
            }
            #else
            webCamTextureToMatHelper.Initialize ();
            #endif
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();

            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null){
                fpsMonitor.Add ("width", webCamTextureMat.width ().ToString());
                fpsMonitor.Add ("height", webCamTextureMat.height ().ToString());
                fpsMonitor.Add ("orientation", Screen.orientation.ToString());
            }

                                    
            float width = webCamTextureMat.width ();
            float height = webCamTextureMat.height ();
                                    
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }

            bgrMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC3);
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

            if (bgrMat != null)
                bgrMat.Dispose ();

            if (texture != null) {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred (WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log ("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update ()
        {
            if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

                if (net.empty () || classes == null) {
                    Imgproc.putText (rgbaMat, "model file is not loaded.", new Point (5, rgbaMat.rows () - 30), Core.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText (rgbaMat, "Please read console message.", new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                } else {

                    Imgproc.cvtColor (rgbaMat, bgrMat, Imgproc.COLOR_RGBA2BGR);

                    blob = Dnn.blobFromImage (bgrMat, 1, new Size (224, 224), new Scalar (104, 117, 123), false, true);
                    net.setInput (blob);

                    Mat prob = net.forward ();

                    Core.MinMaxLocResult minmax = Core.minMaxLoc (prob.reshape (1, 1));
//                Debug.Log ("Best match " + (int)minmax.maxLoc.x);
//                Debug.Log ("Best match class " + classes [(int)minmax.maxLoc.x]);
//                Debug.Log ("Probability: " + minmax.maxVal * 100 + "%");

                    prob.Dispose ();

                    //Imgproc.putText (rgbaMat, "Best match class " + classes [(int)minmax.maxLoc.x], new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    if (fpsMonitor != null) {
                        fpsMonitor.consoleText = "Best match class " + classes [(int)minmax.maxLoc.x];
                    }
                }

                Utils.matToTexture2D (rgbaMat, texture, webCamTextureToMatHelper.GetBufferColors ());
            }
        }

        /// <summary>
        /// Raises the disable event.
        /// </summary>
        void OnDisable ()
        {
            webCamTextureToMatHelper.Dispose ();

            if (blob != null)
                blob.Dispose ();
            if (net != null)
                net.Dispose ();

            Utils.setDebugMode (false);
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

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick ()
        {
            webCamTextureToMatHelper.Play ();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick ()
        {
            webCamTextureToMatHelper.Pause ();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick ()
        {
            webCamTextureToMatHelper.Stop ();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick ()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (!webCamTextureToMatHelper.IsFrontFacing ()) {
                rearCameraRequestedFPS = webCamTextureToMatHelper.requestedFPS;
                webCamTextureToMatHelper.Initialize (!webCamTextureToMatHelper.IsFrontFacing (), 15, webCamTextureToMatHelper.rotate90Degree);
            } else {                
                webCamTextureToMatHelper.Initialize (!webCamTextureToMatHelper.IsFrontFacing (), rearCameraRequestedFPS, webCamTextureToMatHelper.rotate90Degree);
            }
            #else
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing ();
            #endif
        }

        private List<string> readClassNames (string filename)
        {
            List<string> classNames = new List<string> ();

            System.IO.StreamReader cReader = null;
            try {
                cReader = new System.IO.StreamReader (filename, System.Text.Encoding.Default);

                while (cReader.Peek () >= 0) {
                    string name = cReader.ReadLine ();
                    classNames.Add (name);
                }
            } catch (System.Exception ex) {
                Debug.LogError (ex.Message);
                return null;
            } finally {
                if (cReader != null)
                    cReader.Close ();
            }

            return classNames;
        }
    }
}
#endif