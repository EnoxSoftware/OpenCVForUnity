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
    /// MobileNet SSD WebCamTexture Example
    /// This example uses Single-Shot Detector (https://arxiv.org/abs/1512.02325) to detect objects in a WebCamTexture image.
    /// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/mobilenet_ssd_python.py.
    /// </summary>
    [RequireComponent (typeof(WebCamTextureToMatHelper))]
    public class MobileNetSSDWebCamTextureExample : MonoBehaviour
    {
        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The colors.
        /// </summary>
        Color32[] colors;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        const float inWidth = 300;
        const float inHeight = 300;
        float WHRatio = inWidth / inHeight;
        float inScaleFactor = 0.007843f;
        float meanVal = 127.5f;
        
        string[] classNames = {"background",
            "aeroplane", "bicycle", "bird", "boat",
            "bottle", "bus", "car", "cat", "chair",
            "cow", "diningtable", "dog", "horse",
            "motorbike", "person", "pottedplant",
            "sheep", "sofa", "train", "tvmonitor"
        };

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
        /// The crop.
        /// </summary>
        OpenCVForUnity.Rect crop;

        // Use this for initialization
        void Start ()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode (true);

            string model_filepath = Utils.getFilePath ("dnn/MobileNetSSD_deploy.caffemodel");
            string prototxt_filepath = Utils.getFilePath ("dnn/MobileNetSSD_deploy.prototxt");

            if (string.IsNullOrEmpty (model_filepath) || string.IsNullOrEmpty (prototxt_filepath)) {
                Debug.LogError ("model file is not loaded.The model and prototxt file can be downloaded here: \"https://github.com/chuanqi305/MobileNet-SSD\".Please copy to “Assets/StreamingAssets/dnn/” folder. ");
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


            float width = webCamTextureMat.width ();
            float height = webCamTextureMat.height ();

            Size inVideoSize = new Size (width, height);
            Size cropSize;
            if (inVideoSize.width / (float)inVideoSize.height > WHRatio) {
                cropSize = new Size (inVideoSize.height * WHRatio, inVideoSize.height);
            } else {
                cropSize = new Size (inVideoSize.width, inVideoSize.width / WHRatio);
            }
            crop = new OpenCVForUnity.Rect (new Point ((inVideoSize.width - cropSize.width) / 2, (inVideoSize.height - cropSize.height) / 2), cropSize);


            float widthScale = (float)Screen.width / crop.width;
            float heightScale = (float)Screen.height / crop.height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (crop.width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = crop.height / 2;
            }


            texture = new Texture2D (crop.width, crop.height, TextureFormat.RGBA32, false);
            colors = new Color32[crop.width * crop.height];
            
            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
            
            gameObject.transform.localScale = new Vector3 (crop.width, crop.height, 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            bgrMat = new Mat (crop.height, crop.width, CvType.CV_8UC3);
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed ()
        {
            Debug.Log ("OnWebCamTextureToMatHelperDisposed");

        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred (WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log ("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);

            if (bgrMat != null)
                bgrMat.Dispose ();
        }

        // Update is called once per frame
        void Update ()
        {
            if (webCamTextureToMatHelper.IsPlaying () && webCamTextureToMatHelper.DidUpdateThisFrame ()) {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

                if (net == null) {

                    rgbaMat = new Mat (rgbaMat, crop);

                    Imgproc.putText (rgbaMat, "model file is not loaded.", new Point (5, rgbaMat.rows () - 30), Core.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText (rgbaMat, "Please read console message.", new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                } else {

                    Imgproc.cvtColor (rgbaMat, bgrMat, Imgproc.COLOR_RGBA2BGR);

                    blob = Dnn.blobFromImage (bgrMat, inScaleFactor, new Size (inWidth, inHeight), new Scalar (meanVal), false, true);
                    net.setInput (blob);

                    Mat prob = net.forward ();
                    prob = prob.reshape (1, (int)prob.total () / 7);

                    rgbaMat = new Mat (rgbaMat, crop);
                
                
                    float[] data = new float[7];

                    float confidenceThreshold = 0.2f;
                    for (int i = 0; i < prob.rows (); i++) {

                        prob.get (i, 0, data);

                        float confidence = data [2];
                    
                        if (confidence > confidenceThreshold) {
                            int class_id = (int)(data [1]);
                        
                            float xLeftBottom = data [3] * rgbaMat.cols ();
                            float yLeftBottom = data [4] * rgbaMat.rows ();
                            float xRightTop = data [5] * rgbaMat.cols ();
                            float yRightTop = data [6] * rgbaMat.rows ();

                            Imgproc.rectangle (rgbaMat, new Point (xLeftBottom, yLeftBottom), new Point (xRightTop, yRightTop),
                                new Scalar (0, 255, 0, 255), 2);
                            string label = classNames [class_id] + ": " + confidence;
                            int[] baseLine = new int[1];
                            Size labelSize = Imgproc.getTextSize (label, Core.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

                            Imgproc.rectangle (rgbaMat, new Point (xLeftBottom, yLeftBottom),
                                new Point (xLeftBottom + labelSize.width, yLeftBottom + labelSize.height + baseLine [0]),
                                new Scalar (255, 255, 255, 255), Core.FILLED);
                            Imgproc.putText (rgbaMat, label, new Point (xLeftBottom, yLeftBottom + labelSize.height),
                                Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (0, 0, 0, 255));
                        }
                    }

                    prob.Dispose ();
                }

                Utils.matToTexture2D (rgbaMat, texture, colors);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
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
            webCamTextureToMatHelper.Initialize (null, webCamTextureToMatHelper.requestedWidth, webCamTextureToMatHelper.requestedHeight, !webCamTextureToMatHelper.requestedIsFrontFacing);
        }
    }
}
#endif