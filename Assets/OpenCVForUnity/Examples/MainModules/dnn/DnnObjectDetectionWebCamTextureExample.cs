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
    /// Dnn ObjectDetection Example
    /// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/object_detection.cpp.
    /// </summary>
    [RequireComponent (typeof(WebCamTextureToMatHelper))]
    public class DnnObjectDetectionWebCamTextureExample : MonoBehaviour
    {

        [TooltipAttribute ("Path to a binary file of model contains trained weights. It could be a file with extensions .caffemodel (Caffe), .pb (TensorFlow), .t7 or .net (Torch), .weights (Darknet).")]
        public string model;

        [TooltipAttribute ("Path to a text file of model contains network configuration. It could be a file with extensions .prototxt (Caffe), .pbtxt (TensorFlow), .cfg (Darknet).")]
        public string config;

        [TooltipAttribute ("Optional path to a text file with names of classes to label detected objects.")]
        public string classes;

        [TooltipAttribute ("Optional list of classes to label detected objects.")]
        public List<string> classesList;

        [TooltipAttribute ("Confidence threshold.")]
        public float confThreshold;

        [TooltipAttribute ("Non-maximum suppression threshold.")]
        public float nmsThreshold;

        [TooltipAttribute ("Preprocess input image by multiplying on a scale factor.")]
        public float scale;

        [TooltipAttribute ("Preprocess input image by subtracting mean values. Mean values should be in BGR order and delimited by spaces.")]
        public Scalar mean;

        [TooltipAttribute ("Indicate that model works with RGB input images instead BGR ones.")]
        public bool swapRB;

        [TooltipAttribute ("Preprocess input image by resizing to a specific width.")]
        public int inpWidth;

        [TooltipAttribute ("Preprocess input image by resizing to a specific height.")]
        public int inpHeight;


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
        /// The net.
        /// </summary>
        Net net;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        #if UNITY_ANDROID && !UNITY_EDITOR
        float rearCameraRequestedFPS;
        #endif


        List<string> classNames;
        List<string> outBlobNames;
        List<string> outBlobTypes;

        string classes_filepath;
        string config_filepath;
        string model_filepath;

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
            classes_filepath = Utils.getFilePath ("dnn/" + classes);
            config_filepath = Utils.getFilePath ("dnn/" + config);
            model_filepath = Utils.getFilePath ("dnn/" + model);
            Run ();
            #endif
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator GetFilePath ()
        {
var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync ("dnn/"+classes, (result) => {
classes_filepath = result;
});
coroutines.Push (getFilePathAsync_0_Coroutine);
yield return StartCoroutine (getFilePathAsync_0_Coroutine);

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync ("dnn/"+config, (result) => {
config_filepath = result;
});
coroutines.Push (getFilePathAsync_1_Coroutine);
yield return StartCoroutine (getFilePathAsync_1_Coroutine);

            var getFilePathAsync_2_Coroutine = Utils.getFilePathAsync ("dnn/"+model, (result) => {
model_filepath = result;
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

            if (!string.IsNullOrEmpty (classes)) {
                classNames = readClassNames (classes_filepath);
#if !UNITY_WSA_10_0
                if (classNames == null) {
                    Debug.LogError ("class names list file is not loaded.The model and class names list can be downloaded here: \"https://github.com/pjreddie/darknet/tree/master/data/coco.names\".Please copy to “Assets/StreamingAssets/dnn/” folder. ");
                }
#endif
            } else if (classesList.Count > 0) {
                classNames = classesList;
            }

            if (string.IsNullOrEmpty (config_filepath) || string.IsNullOrEmpty (model_filepath)) {
                Debug.LogError ("model file is not loaded. the cfg-file and weights-file can be downloaded here: https://github.com/pjreddie/darknet/blob/master/cfg/tiny-yolo.cfg and https://pjreddie.com/media/files/tiny-yolo.weights. Please copy to “Assets/StreamingAssets/dnn/” folder. ");
            } else {
                //! [Initialize network]
                net = Dnn.readNet (model_filepath, config_filepath);
                //! [Initialize network]


                outBlobNames = getOutputsNames (net);
                //                for (int i = 0; i < outBlobNames.Count; i++) {
                //                    Debug.Log ("names [" + i + "] " + outBlobNames [i]);
                //                }

                outBlobTypes = getOutputsTypes (net);
                //                for (int i = 0; i < outBlobTypes.Count; i++) {
                //                    Debug.Log ("types [" + i + "] " + outBlobTypes [i]);
                //                }
            }
                

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

            if (fpsMonitor != null) {
                fpsMonitor.Add ("width", webCamTextureMat.width ().ToString ());
                fpsMonitor.Add ("height", webCamTextureMat.height ().ToString ());
                fpsMonitor.Add ("orientation", Screen.orientation.ToString ());
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
                Texture2D.Destroy (texture);
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


                    // Create a 4D blob from a frame.
                    Size inpSize = new Size (inpWidth > 0 ? inpWidth : bgrMat.cols (),
                                       inpHeight > 0 ? inpHeight : bgrMat.rows ());
                    Mat blob = Dnn.blobFromImage (bgrMat, scale, inpSize, mean, swapRB, false);


                    // Run a model.
                    net.setInput (blob);

                    if (net.getLayer (new DictValue (0)).outputNameToIndex ("im_info") != -1) {  // Faster-RCNN or R-FCN
                        Imgproc.resize (bgrMat, bgrMat, inpSize);
                        Mat imInfo = new Mat (1, 3, CvType.CV_32FC1);
                        imInfo.put (0, 0, new float[] {
                            (float)inpSize.height,
                            (float)inpSize.width,
                            1.6f
                        });
                        net.setInput (imInfo, "im_info");
                    }
                        

                    TickMeter tm = new TickMeter ();
                    tm.start ();

                    List<Mat> outs = new List<Mat> ();
                    net.forward (outs, outBlobNames);

                    tm.stop ();
//                    Debug.Log ("Inference time, ms: " + tm.getTimeMilli ());


                    postprocess (rgbaMat, outs, net);

                    for (int i = 0; i < outs.Count; i++) {
                        outs [i].Dispose ();
                    }
                    blob.Dispose ();
                }

                Utils.fastMatToTexture2D (rgbaMat, texture);
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

        /// <summary>
        /// Reads the class names.
        /// </summary>
        /// <returns>The class names.</returns>
        /// <param name="filename">Filename.</param>
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

        /// <summary>
        /// Postprocess the specified frame, outs and net.
        /// </summary>
        /// <param name="frame">Frame.</param>
        /// <param name="outs">Outs.</param>
        /// <param name="net">Net.</param>
        private void postprocess (Mat frame, List<Mat> outs, Net net)
        {
            string outLayerType = outBlobTypes [0];


            List<int> classIdsList = new List<int> ();
            List<float> confidencesList = new List<float> ();
            List<OpenCVForUnity.Rect> boxesList = new List<OpenCVForUnity.Rect> ();
            if (net.getLayer (new DictValue (0)).outputNameToIndex ("im_info") != -1) {  // Faster-RCNN or R-FCN
                // Network produces output blob with a shape 1x1xNx7 where N is a number of
                // detections and an every detection is a vector of values
                // [batchId, classId, confidence, left, top, right, bottom]

                if (outs.Count == 1) {

                    outs [0] = outs [0].reshape (1, (int)outs [0].total () / 7);

                    //                    Debug.Log ("outs[i].ToString() " + outs [0].ToString ());

                    float[] data = new float[7];

                    for (int i = 0; i < outs [0].rows (); i++) {

                        outs [0].get (i, 0, data);

                        float confidence = data [2];

                        if (confidence > confThreshold) {
                            int class_id = (int)(data [1]);


                            int left = (int)(data [3] * frame.cols ());
                            int top = (int)(data [4] * frame.rows ());
                            int right = (int)(data [5] * frame.cols ());
                            int bottom = (int)(data [6] * frame.rows ());
                            int width = right - left + 1;
                            int height = bottom - top + 1;


                            classIdsList.Add ((int)(class_id) - 0);
                            confidencesList.Add ((float)confidence);
                            boxesList.Add (new OpenCVForUnity.Rect (left, top, width, height));
                        }
                    }
                }
            } else if (outLayerType == "DetectionOutput") {
                // Network produces output blob with a shape 1x1xNx7 where N is a number of
                // detections and an every detection is a vector of values
                // [batchId, classId, confidence, left, top, right, bottom]

                if (outs.Count == 1) {

                    outs [0] = outs [0].reshape (1, (int)outs [0].total () / 7);

                    //                    Debug.Log ("outs[i].ToString() " + outs [0].ToString ());

                    float[] data = new float[7];

                    for (int i = 0; i < outs [0].rows (); i++) {

                        outs [0].get (i, 0, data);

                        float confidence = data [2];

                        if (confidence > confThreshold) {
                            int class_id = (int)(data [1]);


                            int left = (int)(data [3] * frame.cols ());
                            int top = (int)(data [4] * frame.rows ());
                            int right = (int)(data [5] * frame.cols ());
                            int bottom = (int)(data [6] * frame.rows ());
                            int width = right - left + 1;
                            int height = bottom - top + 1;


                            classIdsList.Add ((int)(class_id) - 0);
                            confidencesList.Add ((float)confidence);
                            boxesList.Add (new OpenCVForUnity.Rect (left, top, width, height));
                        }
                    }
                }
            } else if (outLayerType == "Region") {
                for (int i = 0; i < outs.Count; ++i) {
                    // Network produces output blob with a shape NxC where N is a number of
                    // detected objects and C is a number of classes + 4 where the first 4
                    // numbers are [center_x, center_y, width, height]

                    //                        Debug.Log ("outs[i].ToString() "+outs[i].ToString());

                    float[] positionData = new float[5];
                    float[] confidenceData = new float[outs [i].cols () - 5];

                    for (int p = 0; p < outs [i].rows (); p++) {



                        outs [i].get (p, 0, positionData);

                        outs [i].get (p, 5, confidenceData);

                        int maxIdx = confidenceData.Select ((val, idx) => new { V = val, I = idx }).Aggregate ((max, working) => (max.V > working.V) ? max : working).I;
                        float confidence = confidenceData [maxIdx];

                        if (confidence > confThreshold) {

                            int centerX = (int)(positionData [0] * frame.cols ());
                            int centerY = (int)(positionData [1] * frame.rows ());
                            int width = (int)(positionData [2] * frame.cols ());
                            int height = (int)(positionData [3] * frame.rows ());
                            int left = centerX - width / 2;
                            int top = centerY - height / 2;

                            classIdsList.Add (maxIdx);
                            confidencesList.Add ((float)confidence);
                            boxesList.Add (new OpenCVForUnity.Rect (left, top, width, height));

                        }
                    }
                }
            } else {
                Debug.Log ("Unknown output layer type: " + outLayerType);
            }


            MatOfRect boxes = new MatOfRect ();
            boxes.fromList (boxesList);

            MatOfFloat confidences = new MatOfFloat ();
            confidences.fromList (confidencesList);


            MatOfInt indices = new MatOfInt ();
            Dnn.NMSBoxes (boxes, confidences, confThreshold, nmsThreshold, indices);

            //            Debug.Log ("indices.dump () "+indices.dump ());
            //            Debug.Log ("indices.ToString () "+indices.ToString());

            for (int i = 0; i < indices.total (); ++i) {
                int idx = (int)indices.get (i, 0) [0];
                OpenCVForUnity.Rect box = boxesList [idx];
                drawPred (classIdsList [idx], confidencesList [idx], box.x, box.y,
                    box.x + box.width, box.y + box.height, frame);
            }

            indices.Dispose ();
            boxes.Dispose ();
            confidences.Dispose ();

        }

        /// <summary>
        /// Draws the pred.
        /// </summary>
        /// <param name="classId">Class identifier.</param>
        /// <param name="conf">Conf.</param>
        /// <param name="left">Left.</param>
        /// <param name="top">Top.</param>
        /// <param name="right">Right.</param>
        /// <param name="bottom">Bottom.</param>
        /// <param name="frame">Frame.</param>
        private void drawPred (int classId, float conf, int left, int top, int right, int bottom, Mat frame)
        {
            Imgproc.rectangle (frame, new Point (left, top), new Point (right, bottom), new Scalar (0, 255, 0, 255), 2);

            string label = conf.ToString ();
            if (classNames != null && classNames.Count != 0) {
                if (classId < (int)classNames.Count) {
                    label = classNames [classId] + ": " + label;
                }
            }

            int[] baseLine = new int[1];
            Size labelSize = Imgproc.getTextSize (label, Core.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

            top = Mathf.Max (top, (int)labelSize.height);
            Imgproc.rectangle (frame, new Point (left, top - labelSize.height),
                new Point (left + labelSize.width, top + baseLine [0]), Scalar.all (255), Core.FILLED);
            Imgproc.putText (frame, label, new Point (left, top), Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (0, 0, 0, 255));
        }

        /// <summary>
        /// Gets the outputs names.
        /// </summary>
        /// <returns>The outputs names.</returns>
        /// <param name="net">Net.</param>
        private List<string> getOutputsNames (Net net)
        {
            List<string> names = new List<string> ();


            MatOfInt outLayers = net.getUnconnectedOutLayers ();
            for (int i = 0; i < outLayers.total (); ++i) {
                names.Add (net.getLayer (new DictValue ((int)outLayers.get (i, 0) [0])).get_name ());
            }
            outLayers.Dispose ();

            return names;
        }

        /// <summary>
        /// Gets the outputs types.
        /// </summary>
        /// <returns>The outputs types.</returns>
        /// <param name="net">Net.</param>
        private List<string> getOutputsTypes (Net net)
        {
            List<string> types = new List<string> ();


            MatOfInt outLayers = net.getUnconnectedOutLayers ();
            for (int i = 0; i < outLayers.total (); ++i) {
                types.Add (net.getLayer (new DictValue ((int)outLayers.get (i, 0) [0])).get_type ());
            }
            outLayers.Dispose ();

            return types;
        }
    }
}
#endif