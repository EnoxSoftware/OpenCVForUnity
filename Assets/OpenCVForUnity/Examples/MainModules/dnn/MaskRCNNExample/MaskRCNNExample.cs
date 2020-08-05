#if !UNITY_WSA_10_0

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Mask-RCNN Example
    /// Use this script to run Mask-RCNN object detection and semantic segmentation network from TensorFlow Object Detection API.
    /// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/mask_rcnn.py.
    /// </summary>
    public class MaskRCNNExample : MonoBehaviour
    {
        const int width = 800;
        const int height = 800;
        const float thr = 0.6f;
        //const float mask_thr = 0.5f;

        List<string> classNames;
        List<Scalar> classColors;

        /// <summary>
        /// Path to input image file.
        /// </summary>
        string classes_filepath;

        /// <summary>
        /// IMAGE_FILENAME
        /// </summary>
        protected static readonly string IMAGE_FILENAME = "dnn/004545.jpg";

        /// <summary>
        /// The image filepath.
        /// </summary>
        string image_filepath;

        /// <summary>
        /// Path to a .pb file with weights.
        /// </summary>
        protected static readonly string MODEL_FILENAME = "dnn/mask_rcnn_inception_v2_coco_2018_01_28.pb";

        /// <summary>
        /// The model filepath.
        /// </summary>
        string model_filepath;

        /// <summary>
        /// Path to a .pxtxt file contains network configuration.
        /// </summary>
        protected static readonly string CONFIG_FILENAME = "dnn/mask_rcnn_inception_v2_coco_2018_01_28.pbtxt";

        /// <summary>
        /// The config filepath.
        /// </summary>
        string config_filepath;

        /// <summary>
        /// Path to input image file.
        /// </summary>
        protected static readonly string CLASSES_FILENAME = "dnn/coco-labels-paper.txt";



#if UNITY_WEBGL && !UNITY_EDITOR
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        void Start()
        {

#if UNITY_WEBGL && !UNITY_EDITOR
            getFilePath_Coroutine = GetFilePath();
            StartCoroutine(getFilePath_Coroutine);
#else

            classes_filepath = Utils.getFilePath(CLASSES_FILENAME);
            image_filepath = Utils.getFilePath(IMAGE_FILENAME);
            model_filepath = Utils.getFilePath(MODEL_FILENAME);
            config_filepath = Utils.getFilePath(CONFIG_FILENAME);
            Run();
#endif

        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync(CLASSES_FILENAME, (result) =>
            {
                classes_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync(IMAGE_FILENAME, (result) =>
            {
                image_filepath = result;
            });
            yield return getFilePathAsync_1_Coroutine;

            var getFilePathAsync_2_Coroutine = Utils.getFilePathAsync(MODEL_FILENAME, (result) =>
            {
                model_filepath = result;
            });
            yield return getFilePathAsync_2_Coroutine;

            var getFilePathAsync_3_Coroutine = Utils.getFilePathAsync(CONFIG_FILENAME, (result) =>
            {
                config_filepath = result;
            });
            yield return getFilePathAsync_3_Coroutine;

            getFilePath_Coroutine = null;

            Run();
        }
#endif


        // Use this for initialization
        void Run()
        {

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            classNames = readClassNames(classes_filepath);
            if (classNames == null)
            {
                Debug.LogError(classes_filepath + " is not loaded. Please see \"StreamingAssets/dnn/setup_dnn_module.pdf\". ");
            }

            classColors = new List<Scalar>();
            for (int i = 0; i < classNames.Count; i++)
            {
                classColors.Add(new Scalar(UnityEngine.Random.Range(0, 255), UnityEngine.Random.Range(0, 255), UnityEngine.Random.Range(0, 255)));
            }


            Mat img = Imgcodecs.imread(image_filepath);
            if (img.empty())
            {
                Debug.LogError(image_filepath + " is not loaded. Please see \"StreamingAssets/dnn/setup_dnn_module.pdf\". ");
                img = new Mat(height, width, CvType.CV_8UC3, new Scalar(0, 0, 0));
            }



            //Adust Quad.transform.localScale.
            gameObject.transform.localScale = new Vector3(img.width(), img.height(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            float imageWidth = img.width();
            float imageHeight = img.height();

            float widthScale = (float)Screen.width / imageWidth;
            float heightScale = (float)Screen.height / imageHeight;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (imageWidth * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = imageHeight / 2;
            }


            Net net = null;

            if (string.IsNullOrEmpty(model_filepath) || string.IsNullOrEmpty(config_filepath))
            {
                Debug.LogError(model_filepath + " or " + config_filepath + " is not loaded. Please see \"StreamingAssets/dnn/setup_dnn_module.pdf\". ");
            }
            else
            {
                net = Dnn.readNetFromTensorflow(model_filepath, config_filepath);
            }

            if (net == null)
            {
                Imgproc.putText(img, "model file is not loaded.", new Point(5, img.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(img, "Please read console message.", new Point(5, img.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
            }
            else
            {

                float frameW = img.cols();
                float frameH = img.rows();

                // Create a 4D blob from a frame.
                Mat blob = Dnn.blobFromImage(img, 1.0, new Size(width, height), new Scalar(0, 0, 0), true, false);

                //Run a model
                net.setInput(blob);

                List<Mat> outputBlobs = new List<Mat>();
                List<string> outputName = new List<string>();
                outputName.Add("detection_out_final");
                outputName.Add("detection_masks");

                TickMeter tm = new TickMeter();
                tm.start();

                net.forward(outputBlobs, outputName);

                tm.stop();
                Debug.Log("Inference time, ms: " + tm.getTimeMilli());

                Mat boxes = outputBlobs[0];
                Mat masks = outputBlobs[1];

                int numClasses = masks.size(1);
                int numDetections = boxes.size(2);
                int mask_sizeH = masks.size(2);
                int mask_sizeW = masks.size(3);

                float[] box_data = new float[boxes.size(3)];
                float[] mask_data = new float[masks.size(2) * masks.size(3)];

                for (int i = 0; i < numDetections; i++)
                {
                    boxes.get(new int[] { 0, 0, i, 0 }, box_data);

                    float score = box_data[2];

                    if (score > thr)
                    {
                        int classId = (int)box_data[1];

                        float left = (int)frameW * box_data[3];
                        float top = (int)frameH * box_data[4];
                        float right = (int)frameW * box_data[5];
                        float bottom = (int)frameH * box_data[6];

                        left = (int)Mathf.Max(0, Mathf.Min(left, frameW - 1));
                        top = (int)Mathf.Max(0, Mathf.Min(top, frameH - 1));
                        right = (int)Mathf.Max(0, Mathf.Min(right, frameW - 1));
                        bottom = (int)Mathf.Max(0, Mathf.Min(bottom, frameH - 1));


                        masks.get(new int[] { i, classId, 0, 0 }, mask_data);

                        Mat classMask = new Mat(mask_sizeH, mask_sizeW, CvType.CV_32F);
                        classMask.put(0, 0, mask_data);
                        Imgproc.resize(classMask, classMask, new Size(right - left + 1, bottom - top + 1));
                        Core.compare(classMask, new Scalar(0.5), classMask, Core.CMP_GT);

                        Mat roi = new Mat(img, new OpenCVForUnity.CoreModule.Rect(new Point(left, top), new Point(right + 1, bottom + 1)));
                        Mat coloredRoi = new Mat(roi.size(), CvType.CV_8UC3);
                        Imgproc.rectangle(coloredRoi, new Point(0, 0), new Point(coloredRoi.width(), coloredRoi.height()), classColors[classId], -1);
                        Core.addWeighted(coloredRoi, 0.7, roi, 0.3, 0, coloredRoi);

                        coloredRoi.copyTo(roi, classMask);
                        coloredRoi.Dispose();
                        classMask.Dispose();


                        drawPred(classId, score, left, top, right, bottom, img);

                        Debug.Log("classId:" + classId + " cnof:" + score + " l:" + left + " t:" + top + " r:" + right + " b:" + bottom);
                    }
                }

                boxes.Dispose();
                masks.Dispose();
                blob.Dispose();
            }

            Imgproc.cvtColor(img, img, Imgproc.COLOR_BGR2RGB);

            Texture2D texture = new Texture2D(img.cols(), img.rows(), TextureFormat.RGBA32, false);

            Utils.matToTexture2D(img, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            net.Dispose();


            Utils.setDebugMode(false);
        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// Raises the disable event.
        /// </summary>
        void OnDisable()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (getFilePath_Coroutine != null)
            {
                StopCoroutine(getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose();
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
        protected virtual void drawPred(int classId, float conf, double left, double top, double right, double bottom, Mat frame)
        {
            Imgproc.rectangle(frame, new Point(left, top), new Point(right, bottom), new Scalar(0, 255, 0, 255), 2);

            string label = conf.ToString();
            if (classNames != null && classNames.Count != 0)
            {
                if (classId < (int)classNames.Count)
                {
                    label = classNames[classId] + ": " + label;
                }
            }

            int[] baseLine = new int[1];
            Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

            top = Mathf.Max((float)top, (float)labelSize.height);
            Imgproc.rectangle(frame, new Point(left, top - labelSize.height),
                new Point(left + labelSize.width, top + baseLine[0]), Scalar.all(255), Core.FILLED);
            Imgproc.putText(frame, label, new Point(left, top), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(0, 0, 0, 255));
        }

        /// <summary>
        /// Reads the class names.
        /// </summary>
        /// <returns>The class names.</returns>
        /// <param name="filename">Filename.</param>
        private List<string> readClassNames(string filename)
        {
            List<string> classNames = new List<string>();

            System.IO.StreamReader cReader = null;
            try
            {
                cReader = new System.IO.StreamReader(filename, System.Text.Encoding.Default);

                while (cReader.Peek() >= 0)
                {
                    string name = cReader.ReadLine();
                    classNames.Add(name);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
                return null;
            }
            finally
            {
                if (cReader != null)
                    cReader.Close();
            }

            return classNames;
        }
    }
}
#endif