#if !UNITY_WEBGL && !UNITY_WSA_10_0

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

using System.Linq;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Yolo ObjectDetection Example
    /// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/yolo_object_detection.cpp.
    /// </summary>
    public class YoloObjectDetectionExample : MonoBehaviour
    {

        const int network_width = 416;
        const int network_height = 416;

        // Use this for initialization
        void Start ()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode (true);


            List<string> classNames = readClassNames (Utils.getFilePath ("dnn/coco.names"));
#if !UNITY_WSA_10_0
            if (classNames == null) {
                Debug.LogError ("class names list file is not loaded.The model and class names list can be downloaded here: \"https://github.com/pjreddie/darknet/tree/master/data/coco.names\".Please copy to “Assets/StreamingAssets/dnn/” folder. ");
            }
#endif


            Mat img = Imgcodecs.imread (Utils.getFilePath ("dnn/person.jpg"));
            #if !UNITY_WSA_10_0
            if (img.empty ()) {
                Debug.LogError ("dnn/person.jpg is not loaded.The image file can be downloaded here: \"https://github.com/pjreddie/darknet/blob/master/data/person.jpg\".Please copy to \"Assets/StreamingAssets/dnn/\" folder. ");
                img = new Mat (424, 640, CvType.CV_8UC3, new Scalar (0, 0, 0));
            }
            #endif
            

            string modelConfiguration = Utils.getFilePath ("dnn/tiny-yolo.cfg");
            string modelBinary = Utils.getFilePath ("dnn/tiny-yolo.weights");

            Net net = null;


            if (string.IsNullOrEmpty (modelConfiguration) || string.IsNullOrEmpty (modelBinary)) {
                Debug.LogError ("model file is not loaded. the cfg-file and weights-file can be downloaded here: https://github.com/pjreddie/darknet/blob/master/cfg/tiny-yolo.cfg and https://pjreddie.com/media/files/tiny-yolo.weights. Please copy to “Assets/StreamingAssets/dnn/” folder. ");
            } else {
                //! [Initialize network]
                net = Dnn.readNetFromDarknet (modelConfiguration, modelBinary);
                //! [Initialize network]
            }


            if (net == null) {
               
                Imgproc.putText (img, "model file is not loaded.", new Point (5, img.rows () - 30), Core.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar (255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText (img, "Please read console message.", new Point (5, img.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar (255, 255, 255), 2, Imgproc.LINE_AA, false);

            } else {

                //! [Resizing without keeping aspect ratio]
                Mat resized = new Mat ();
                Imgproc.resize (img, resized, new Size (network_width, network_height));
                //! [Resizing without keeping aspect ratio]

                //! [Prepare blob]
                Mat inputBlob = Dnn.blobFromImage (resized, 1 / 255.0, new Size (), new Scalar (0), true, true); //Convert Mat to batch of images
                //! [Prepare blob]

                //! [Set input blob]
                net.setInput (inputBlob, "data");                   //set the network input
                //! [Set input blob]


                TickMeter tm = new TickMeter ();
                tm.start ();

                //! [Make forward pass]
                Mat detectionMat = net.forward ("detection_out");   //compute output
                //! [Make forward pass]

                tm.stop ();
                Debug.Log ("Inference time, ms: " + tm.getTimeMilli ());

                Debug.Log ("detectionMat.ToString(): " + detectionMat.ToString ());

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
                        int xLeftBottom = (int)((x - width / 2) * img.cols ());
                        int yLeftBottom = (int)((y - height / 2) * img.rows ());
                        int xRightTop = (int)((x + width / 2) * img.cols ());
                        int yRightTop = (int)((y + height / 2) * img.rows ());

                        Debug.Log ("confidence: " + confidence);

                        Debug.Log (" " + xLeftBottom
                        + " " + yLeftBottom
                        + " " + xRightTop
                        + " " + yRightTop);

                        Imgproc.rectangle (img, new Point (xLeftBottom, yLeftBottom), new Point (xRightTop, yRightTop),
                            new Scalar (0, 255, 0), 2);

                        if (maxIdx < classNames.Count) {

                            string label = classNames [maxIdx] + ": " + confidence;
                            int[] baseLine = new int[1];
                            Size labelSize = Imgproc.getTextSize (label, Core.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

                            Imgproc.rectangle (img, new Point (xLeftBottom, yLeftBottom),
                                new Point (xLeftBottom + labelSize.width, yLeftBottom + labelSize.height + baseLine [0]),
                                new Scalar (255, 255, 255), Core.FILLED);
                            Imgproc.putText (img, label, new Point (xLeftBottom, yLeftBottom + labelSize.height),
                                Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (0, 0, 0));

                        }
                        
                    }
                }
            }
            
            Imgproc.cvtColor (img, img, Imgproc.COLOR_BGR2RGB);

            Texture2D texture = new Texture2D (img.cols (), img.rows (), TextureFormat.RGBA32, false);

            Utils.matToTexture2D (img, texture);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;


            Utils.setDebugMode (false);
        }
    
        // Update is called once per frame
        void Update ()
        {

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
    }
}
#endif