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
    /// Lightweight PoseEstimation Example
    /// An example of DNN inference with a lightweight person pose estimation model using the KeypointsModel class.
    /// </summary>
    public class LightweightPoseEstimationExample : MonoBehaviour
    {

        const float inWidth = 256;
        const float inHeight = 256;

        const float inScale = 1.0f / 255f;

        double[] inMean = new double[] { 128.0, 128.0, 128.0 };

        double threshold = 0.1;

        Dictionary<string, int> BODY_PARTS = new Dictionary<string, int>() {
                        { "Nose", 0 }, { "Neck", 1 }, { "RShoulder", 2 }, { "RElbow", 3 }, {
                            "RWrist",
                            4
                        },
                        { "LShoulder",5 }, { "LElbow", 6 }, { "LWrist", 7 }, { "RHip", 8 }, {
                            "RKnee",
                            9
                        },
                        { "RAnkle", 10 }, { "LHip", 11 }, { "LKnee", 12 }, { "LAnkle", 13 }, {
                            "REye",
                            14
                        },
                        { "LEye", 15 }, { "REar", 16 }, { "LEar", 17 }, {
                            "Background",
                            18
                        }
                    };

        string[,] POSE_PAIRS = new string[,] {
                        { "Neck", "RShoulder" }, { "Neck", "LShoulder" }, {
                            "RShoulder",
                            "RElbow"
                        },
                        { "RElbow", "RWrist" }, { "LShoulder", "LElbow" }, {
                            "LElbow",
                            "LWrist"
                        },
                        { "Neck", "RHip" }, { "RHip", "RKnee" }, { "RKnee", "RAnkle" }, {
                            "Neck",
                            "LHip"
                        },
                        { "LHip", "LKnee" }, { "LKnee", "LAnkle" }, { "Neck", "Nose" }, {
                            "Nose",
                            "REye"
                        },
                        { "REye", "REar" }, { "Nose", "LEye" }, { "LEye", "LEar" }
            };


        /// <summary>
        /// IMAGE_FILENAME
        /// </summary>
        string IMAGE_FILENAME = "dnn/COCO_val2014_000000000589.jpg";

        /// <summary>
        /// The image filepath.
        /// </summary>
        string image_filepath;

        /// <summary>
        /// MODEL_FILENAME
        /// </summary>
        string MODEL_FILENAME = "dnn/lightweight_pose_estimation_201912.onnx";

        /// <summary>
        /// The model filepath.
        /// </summary>
        string model_filepath;

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        void Start()
        {

#if UNITY_WEBGL
            getFilePath_Coroutine = GetFilePath();
            StartCoroutine(getFilePath_Coroutine);
#else
            image_filepath = Utils.getFilePath(IMAGE_FILENAME);
            model_filepath = Utils.getFilePath(MODEL_FILENAME);
            Run();
#endif
        }

#if UNITY_WEBGL
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync(IMAGE_FILENAME, (result) =>
            {
                image_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync(MODEL_FILENAME, (result) =>
            {
                model_filepath = result;
            });
            yield return getFilePathAsync_1_Coroutine;

            getFilePath_Coroutine = null;

            Run();
        }
#endif

        // Use this for initialization
        void Run()
        {

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            Mat img = Imgcodecs.imread(image_filepath);
            if (img.empty())
            {
                Debug.LogError(IMAGE_FILENAME + " is not loaded. Please read “StreamingAssets/dnn/setup_dnn_module.pdf” to make the necessary setup.");
                img = new Mat((int)inWidth, (int)inHeight, CvType.CV_8UC3, new Scalar(0, 0, 0));
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

            if (string.IsNullOrEmpty(model_filepath))
            {
                Debug.LogError(MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                net = Dnn.readNet(model_filepath);
            }

            if (net == null)
            {
                Imgproc.putText(img, "model file is not loaded.", new Point(5, img.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(img, "Please read console message.", new Point(5, img.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
            }
            else
            {

                KeypointsModel keypointsModel = new KeypointsModel(net);
                keypointsModel.setInputScale(inScale);
                keypointsModel.setInputSize(new Size(inWidth, inHeight));
                keypointsModel.setInputMean(new Scalar(inMean));
                keypointsModel.setInputSwapRB(false);
                keypointsModel.setInputCrop(false);

                List<Point> points = keypointsModel.estimate(img, (float)threshold).toList();
                
                for (int i = 0; i < POSE_PAIRS.GetLength(0); i++)
                {
                    string partFrom = POSE_PAIRS[i, 0];
                    string partTo = POSE_PAIRS[i, 1];

                    int idFrom = BODY_PARTS[partFrom];
                    int idTo = BODY_PARTS[partTo];

                    if (points[idFrom] != null && points[idTo] != null)
                    {
                        Imgproc.line(img, points[idFrom], points[idTo], new Scalar(0, 255, 0), 3);
                        Imgproc.ellipse(img, points[idFrom], new Size(3, 3), 0, 0, 360, new Scalar(0, 0, 255), Core.FILLED);
                        Imgproc.ellipse(img, points[idTo], new Size(3, 3), 0, 0, 360, new Scalar(0, 0, 255), Core.FILLED);
                    }
                }

                MatOfDouble timings = new MatOfDouble();
                long t = net.getPerfProfile(timings);
                Debug.Log("t: " + t);
                Debug.Log("timings.dump(): " + timings.dump());

                double freq = Core.getTickFrequency() / 1000;
                Debug.Log("freq: " + freq);

                Imgproc.putText(img, (t / freq) + "ms", new Point(10, img.height() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.6, new Scalar(0, 0, 255), 2);

                net.Dispose();
            }

            Imgproc.cvtColor(img, img, Imgproc.COLOR_BGR2RGB);

            Texture2D texture = new Texture2D(img.cols(), img.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(img, texture);
            gameObject.GetComponent<Renderer>().material.mainTexture = texture;


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
#if UNITY_WEBGL
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
    }
}
#endif