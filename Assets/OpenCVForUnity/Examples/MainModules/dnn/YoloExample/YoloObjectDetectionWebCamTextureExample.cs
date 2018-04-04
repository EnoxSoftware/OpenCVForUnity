#if !UNITY_WSA_10_0

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Yolo ObjectDetection Example
    /// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/yolo_object_detection.cpp.
    /// </summary>
    [RequireComponent (typeof(WebCamTextureToMatHelper))]
    public class YoloObjectDetectionWebCamTextureExample : MonoBehaviour
    {
        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        const int network_width = 416;
        const int network_height = 416;

        /// <summary>
        /// The bgr mat.
        /// </summary>
        Mat bgrMat;

        /// <summary>
        /// The net.
        /// </summary>
        Net net;

        /// <summary>
        /// The resized.
        /// </summary>
        Mat resized;

        /// <summary>
        /// The input BLOB.
        /// </summary>
        Mat inputBlob;

        /// <summary>
        /// The detection mat.
        /// </summary>
        Mat detectionMat;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        #if UNITY_ANDROID && !UNITY_EDITOR
        float rearCameraRequestedFPS;
        #endif

        /// <summary>
        /// The class names.
        /// </summary>
        List<string> classNames;

        string coco_names_filepath;
        string tiny_yolo_cfg_filepath;
        string tiny_yolo_weights_filepath;

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
            coco_names_filepath = Utils.getFilePath ("dnn/coco.names");
            tiny_yolo_cfg_filepath = Utils.getFilePath ("dnn/tiny-yolo.cfg");
            tiny_yolo_weights_filepath = Utils.getFilePath ("dnn/tiny-yolo.weights");
            Run ();
            #endif
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator GetFilePath ()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync ("dnn/coco.names", (result) => {
                coco_names_filepath = result;
            });
            coroutines.Push (getFilePathAsync_0_Coroutine);
            yield return StartCoroutine (getFilePathAsync_0_Coroutine);

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync ("dnn/tiny-yolo.cfg", (result) => {
                tiny_yolo_cfg_filepath = result;
            });
            coroutines.Push (getFilePathAsync_1_Coroutine);
            yield return StartCoroutine (getFilePathAsync_1_Coroutine);

            var getFilePathAsync_2_Coroutine = Utils.getFilePathAsync ("dnn/tiny-yolo.weights", (result) => {
                tiny_yolo_weights_filepath = result;
            });
            coroutines.Push (getFilePathAsync_2_Coroutine);
            yield return StartCoroutine (getFilePathAsync_2_Coroutine);

            coroutines.Clear ();

            Run ();
        }
        #endif

        // Use this for initialization
        void Run ()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode (true);

            classNames = readClassNames (coco_names_filepath);
            #if !UNITY_WSA_10_0
            if (classNames == null) {
                Debug.LogError ("class names list file is not loaded.The model and class names list can be downloaded here: \"https://github.com/pjreddie/darknet/tree/master/data/coco.names\".Please copy to “Assets/StreamingAssets/dnn/” folder. ");
            }
            #endif

            if (string.IsNullOrEmpty (tiny_yolo_cfg_filepath) || string.IsNullOrEmpty (tiny_yolo_weights_filepath)) {
                Debug.LogError ("model file is not loaded. the cfg-file and weights-file can be downloaded here: https://github.com/pjreddie/darknet/blob/master/cfg/tiny-yolo.cfg and https://pjreddie.com/media/files/tiny-yolo.weights. Please copy to “Assets/StreamingAssets/dnn/” folder. ");
            } else {
                //! [Initialize network]
                net = Dnn.readNetFromDarknet (tiny_yolo_cfg_filepath, tiny_yolo_weights_filepath);
                //! [Initialize network]
            }

            resized = new Mat ();

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

                if (net == null) {

                    Imgproc.putText (rgbaMat, "model file is not loaded.", new Point (5, rgbaMat.rows () - 30), Core.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText (rgbaMat, "Please read console message.", new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                } else {
                    
                    Imgproc.cvtColor (rgbaMat, bgrMat, Imgproc.COLOR_RGBA2BGR);

                    //! [Resizing without keeping aspect ratio]

                    Imgproc.resize (bgrMat, resized, new Size (network_width, network_height));
                    //! [Resizing without keeping aspect ratio]


                    //! [Prepare blob]
                    inputBlob = Dnn.blobFromImage (resized, 1 / 255.0, new Size (), new Scalar (0), true, true); //Convert Mat to batch of images
                    //! [Prepare blob]

                    //! [Set input blob]
                    net.setInput (inputBlob, "data");                   //set the network input
                    //! [Set input blob]


//                    TickMeter tm = new TickMeter ();
//                    tm.start ();

                    //! [Make forward pass]
                    Mat detectionMat = net.forward ("detection_out");   //compute output
                    //! [Make forward pass]

//                    tm.stop ();
//                    Debug.Log ("Inference time, ms: " + tm.getTimeMilli ());

//                    Debug.Log ("detectionMat.ToString(): " + detectionMat.ToString ());

                    float[] position = new float[5];
                    float[] confidences = new float[80];

                    float confidenceThreshold = 0.24f;
                    for (int i = 0; i < detectionMat.rows (); i++) {

                        detectionMat.get (i, 0, position);

                        detectionMat.get (i, 5, confidences);


                        int maxIdx = confidences.Select ((val, idx) => new { V = val, I = idx }).Aggregate ((max, working) => (max.V > working.V) ? max : working).I;
                        float confidence = confidences [maxIdx];


                        if (confidence > confidenceThreshold) {

                            float x = position [0];
                            float y = position [1];
                            float width = position [2];
                            float height = position [3];
                            int xLeftBottom = (int)((x - width / 2) * rgbaMat.cols ());
                            int yLeftBottom = (int)((y - height / 2) * rgbaMat.rows ());
                            int xRightTop = (int)((x + width / 2) * rgbaMat.cols ());
                            int yRightTop = (int)((y + height / 2) * rgbaMat.rows ());

//                            Debug.Log ("confidence: " + confidence);
//
//                            Debug.Log (" " + xLeftBottom
//                            + " " + yLeftBottom
//                            + " " + xRightTop
//                            + " " + yRightTop);


                            Imgproc.rectangle (rgbaMat, new Point (xLeftBottom, yLeftBottom), new Point (xRightTop, yRightTop),
                                new Scalar (0, 255, 0, 255), 2);

                            if (maxIdx < classNames.Count) {

                                string label = classNames [maxIdx] + ": " + confidence;
                                int[] baseLine = new int[1];
                                Size labelSize = Imgproc.getTextSize (label, Core.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

                                Imgproc.rectangle (rgbaMat, new Point (xLeftBottom, yLeftBottom),
                                    new Point (xLeftBottom + labelSize.width, yLeftBottom + labelSize.height + baseLine [0]),
                                    new Scalar (255, 255, 255, 255), Core.FILLED);
                                Imgproc.putText (rgbaMat, label, new Point (xLeftBottom, yLeftBottom + labelSize.height),
                                    Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (0, 0, 0, 255));

                            }
                                
                        }
                    }

                    detectionMat.Dispose ();
                }

                Utils.matToTexture2D (rgbaMat, texture, webCamTextureToMatHelper.GetBufferColors ());
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            webCamTextureToMatHelper.Dispose ();

            if (resized != null)
                resized.Dispose ();
            if (inputBlob != null)
                inputBlob.Dispose ();
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