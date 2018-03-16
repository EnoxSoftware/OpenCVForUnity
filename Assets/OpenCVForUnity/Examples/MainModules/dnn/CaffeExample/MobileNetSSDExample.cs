#if !UNITY_WSA_10_0

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// MobileNet SSD Example
    /// This example uses Single-Shot Detector (https://arxiv.org/abs/1512.02325) to detect objects.
    /// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/mobilenet_ssd_python.py.
    /// </summary>
    public class MobileNetSSDExample : MonoBehaviour
    {
        const float inWidth = 300;
        const float inHeight = 300;
        float inScaleFactor = 0.007843f;
        float meanVal = 127.5f;
                
        string[] classNames = {"background",
            "aeroplane", "bicycle", "bird", "boat",
            "bottle", "bus", "car", "cat", "chair",
            "cow", "diningtable", "dog", "horse",
            "motorbike", "person", "pottedplant",
            "sheep", "sofa", "train", "tvmonitor"
        };

        string dnn004545_jpg_filepath;
        string MobileNetSSD_deploy_caffemodel_filepath;
        string MobileNetSSD_deploy_prototxt_filepath;

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
            dnn004545_jpg_filepath = Utils.getFilePath ("dnn/004545.jpg");
            MobileNetSSD_deploy_caffemodel_filepath = Utils.getFilePath ("dnn/MobileNetSSD_deploy.caffemodel");
            MobileNetSSD_deploy_prototxt_filepath = Utils.getFilePath ("dnn/MobileNetSSD_deploy.prototxt");
            Run ();
#endif
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator GetFilePath()
{
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync ("dnn/004545.jpg", (result) => {
dnn004545_jpg_filepath = result;
});
coroutines.Push (getFilePathAsync_0_Coroutine);
yield return StartCoroutine (getFilePathAsync_0_Coroutine);

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync ("dnn/MobileNetSSD_deploy.caffemodel", (result) => {
MobileNetSSD_deploy_caffemodel_filepath = result;
});
coroutines.Push (getFilePathAsync_1_Coroutine);
yield return StartCoroutine (getFilePathAsync_1_Coroutine);

            var getFilePathAsync_2_Coroutine = Utils.getFilePathAsync ("dnn/MobileNetSSD_deploy.prototxt", (result) => {
MobileNetSSD_deploy_prototxt_filepath = result;
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


            Mat img = Imgcodecs.imread (dnn004545_jpg_filepath);
            #if !UNITY_WSA_10_0
            if (img.empty ()) {
                Debug.LogError ("dnn/004545.jpg is not loaded.The image file can be downloaded here: \"https://github.com/chuanqi305/MobileNet-SSD/blob/master/images/004545.jpg\".Please copy to \"Assets/StreamingAssets/dnn/\" folder. ");
                img = new Mat (375, 500, CvType.CV_8UC3, new Scalar (0, 0, 0));

            }
            #endif


            //Adust Quad.transform.localScale.
            gameObject.transform.localScale = new Vector3 (img.width (), img.height (), 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            float imageWidth = img.width ();
            float imageHeight = img.height ();

            float widthScale = (float)Screen.width / imageWidth;
            float heightScale = (float)Screen.height / imageHeight;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (imageWidth * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = imageHeight / 2;
            }


            Net net = null;
           
            if (string.IsNullOrEmpty (MobileNetSSD_deploy_caffemodel_filepath) || string.IsNullOrEmpty (MobileNetSSD_deploy_prototxt_filepath)) {
                Debug.LogError ("model file is not loaded.The model and prototxt file can be downloaded here: \"https://github.com/chuanqi305/MobileNet-SSD\".Please copy to “Assets/StreamingAssets/dnn/” folder. ");
            } else {
                net = Dnn.readNetFromCaffe (MobileNetSSD_deploy_prototxt_filepath, MobileNetSSD_deploy_caffemodel_filepath);

            }

            if (net == null) {

                Imgproc.putText (img, "model file is not loaded.", new Point (5, img.rows () - 30), Core.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar (255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText (img, "Please read console message.", new Point (5, img.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar (255, 255, 255), 2, Imgproc.LINE_AA, false);

            } else {

                Mat blob = Dnn.blobFromImage (img, inScaleFactor, new Size (inWidth, inHeight), new Scalar (meanVal, meanVal, meanVal), false, false);

                net.setInput (blob);


                TickMeter tm = new TickMeter ();
                tm.start ();

                Mat prob = net.forward ();
                prob = prob.reshape (1, (int)prob.total () / 7);

                tm.stop ();
                Debug.Log ("Inference time, ms: " + tm.getTimeMilli ());



                float[] data = new float[7];

                float confidenceThreshold = 0.2f;
                for (int i = 0; i < prob.rows (); i++) {

                    prob.get (i, 0, data);

                    float confidence = data [2];

                    if (confidence > confidenceThreshold) {
                        int class_id = (int)(data [1]);

                        float left = data [3] * img.cols ();
                        float top = data [4] * img.rows ();
                        float right = data [5] * img.cols ();
                        float bottom = data [6] * img.rows ();

                        Debug.Log ("class_id: " + class_id);
                        Debug.Log ("Confidence: " + confidence);

                        Debug.Log (" " + left
                        + " " + top
                        + " " + right
                        + " " + bottom);

                        Imgproc.rectangle (img, new Point (left, top), new Point (right, bottom),
                            new Scalar (0, 255, 0), 2);
                        string label = classNames [class_id] + ": " + confidence;
                        int[] baseLine = new int[1];
                        Size labelSize = Imgproc.getTextSize (label, Core.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

                        top = Mathf.Max (top, (float)labelSize.height);

                        Imgproc.rectangle (img, new Point (left, top),
                            new Point (left + labelSize.width, top + labelSize.height + baseLine [0]),
                            new Scalar (255, 255, 255), Core.FILLED);
                        Imgproc.putText (img, label, new Point (left, top + labelSize.height),
                            Core.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar (0, 0, 0));
                    }
                }

                prob.Dispose ();

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
    }
}
#endif