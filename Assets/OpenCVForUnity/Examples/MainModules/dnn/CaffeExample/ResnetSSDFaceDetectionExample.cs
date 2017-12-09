#if !UNITY_WEBGL && !UNITY_WSA_10_0

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
    /// Resnet SSD FaceDetection Example
    /// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/resnet_ssd_face_python.py.
    /// </summary>
    [RequireComponent (typeof(WebCamTextureToMatHelper))]
    public class ResnetSSDFaceDetectionExample : MonoBehaviour
    {
        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        const float inWidth = 300;
        const float inHeight = 300;
        float confThreshold = 0.5f;

        /// <summary>
        /// The bgr mat.
        /// </summary>
        Mat bgrMat;

        /// <summary>
        /// The net.
        /// </summary>
        Net net;

        // Use this for initialization
        void Start ()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode (true);

            string model_filepath = Utils.getFilePath ("dnn/res10_300x300_ssd_iter_140000.caffemodel");
            string prototxt_filepath = Utils.getFilePath ("dnn/deploy.prototxt");

            if (string.IsNullOrEmpty (model_filepath) || string.IsNullOrEmpty (prototxt_filepath)) {
                Debug.LogError ("model file is not loaded.The model and prototxt file can be downloaded here: \"https://raw.githubusercontent.com/opencv/opencv_3rdparty/b2bfc75f6aea5b1f834ff0f0b865a7c18ff1459f/res10_300x300_ssd_iter_140000.caffemodel\" and \"https://github.com/opencv/opencv/blob/master/samples/dnn/face_detector/deploy.prototxt\".Please copy to “Assets/StreamingAssets/dnn/” folder. ");
            } else {
                net = Dnn.readNetFromCaffe (prototxt_filepath, model_filepath);

            }

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();
            webCamTextureToMatHelper.Initialize ();
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();

            bgrMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC3);

            float width = webCamTextureMat.width ();
            float height = webCamTextureMat.height ();


            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }


            texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);
            
            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
            
            gameObject.transform.localScale = new Vector3 (width, height, 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

            if (bgrMat != null)
                bgrMat.Dispose ();
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


                    net.setInput (Dnn.blobFromImage (bgrMat, 1.0, new Size (inWidth, inHeight), new Scalar (104, 177, 123), false, false), "data");

                    Mat detections = net.forward ("detection_out");


                    detections = detections.reshape (1, (int)detections.total () / 7);


                    float[] data = new float[7];

                    for (int i = 0; i < detections.rows (); i++) {

                        detections.get (i, 0, data);

                        float confidence = data [2];
                                        
                        if (confidence > confThreshold) {
                                            
                            float xLeftBottom = data [3] * bgrMat.cols ();
                            float yLeftBottom = data [4] * bgrMat.rows ();
                            float xRightTop = data [5] * bgrMat.cols ();
                            float yRightTop = data [6] * bgrMat.rows ();
                    
                            Imgproc.rectangle (rgbaMat, new Point (xLeftBottom, yLeftBottom), new Point (xRightTop, yRightTop),
                                new Scalar (0, 255, 0, 255), 2);
                                                
                            string label = "face: " + confidence;
                            int[] baseLine = new int[1];
                    
                            Size labelSize = Imgproc.getTextSize (label, Core.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);
                    
                            Imgproc.rectangle (rgbaMat, new Point (xLeftBottom, yLeftBottom),
                                new Point (xLeftBottom + labelSize.width, yLeftBottom + labelSize.height + baseLine [0]),
                                new Scalar (255, 255, 255, 255), Core.FILLED);
                            Imgproc.putText (rgbaMat, label, new Point (xLeftBottom, yLeftBottom + labelSize.height),
                                Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (0, 0, 0, 255));
                        }
                    }
                

                    detections.Dispose ();
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
            webCamTextureToMatHelper.Initialize (null, webCamTextureToMatHelper.requestedWidth, webCamTextureToMatHelper.requestedHeight, !webCamTextureToMatHelper.requestedIsFrontFacing);
        }
    }
}
#endif