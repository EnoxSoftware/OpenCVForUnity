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
    /// OpenPose Example
    /// This example uses OpenPose human pose estimation network.
    /// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/openpose.py.
    /// </summary>
    public class OpenPoseExample : MonoBehaviour
    {

        public enum DATASET_TYPE
        {
            COCO,
            MPI,
            HAND
        }
        public DATASET_TYPE dataset = DATASET_TYPE.MPI;

        const float inWidth = 368;
        const float inHeight = 368;

        const float inScale = 1.0f / 255f;


        Dictionary<string, int> BODY_PARTS;
        string[,] POSE_PAIRS;


        /// <summary>
        /// IMAGE_FILENAME
        /// </summary>
        string IMAGE_FILENAME;

        /// <summary>
        /// The image filepath.
        /// </summary>
        string image_filepath;

        /// <summary>
        /// CAFFEMODEL_FILENAME
        /// </summary>
        string CAFFEMODEL_FILENAME;

        /// <summary>
        /// The caffemodel filepath.
        /// </summary>
        string caffemodel_filepath;

        /// <summary>
        /// PROTOTXT_FILENAME
        /// </summary>
        string PROTOTXT_FILENAME;

        /// <summary>
        /// The prototxt filepath.
        /// </summary>
        string prototxt_filepath;

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        void Start()
        {
            if (dataset == DATASET_TYPE.COCO)
            {
                //COCO
                BODY_PARTS = new Dictionary<string, int>() {
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

                POSE_PAIRS
                = new string[,] {
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

                IMAGE_FILENAME = "dnn/COCO_val2014_000000000589.jpg";
                CAFFEMODEL_FILENAME = "dnn/pose_iter_440000.caffemodel";
                PROTOTXT_FILENAME = "dnn/openpose_pose_coco.prototxt";
            }
            else if (dataset == DATASET_TYPE.MPI)
            {

                //MPI
                BODY_PARTS
                = new Dictionary<string, int>() { { "Head", 0 }, { "Neck", 1 }, {
                        "RShoulder",
                        2
                    }, {
                        "RElbow",
                        3
                    }, {
                        "RWrist",
                        4
                    },
                    { "LShoulder", 5 }, { "LElbow", 6 }, { "LWrist", 7 }, { "RHip", 8 }, {
                        "RKnee",
                        9
                    },
                    { "RAnkle", 10 }, { "LHip", 11 }, { "LKnee", 12 }, { "LAnkle", 13 }, {
                        "Chest",
                        14
                    },
                    { "Background", 15 }
                };

                POSE_PAIRS = new string[,] {
                    { "Head", "Neck" }, {
                        "Neck",
                        "RShoulder"
                    }, {
                        "RShoulder",
                        "RElbow"
                    },
                    { "RElbow", "RWrist" },
                    { "Neck", "LShoulder" }, {
                        "LShoulder",
                        "LElbow"
                    },
                    { "LElbow", "LWrist" },
                    { "Neck", "Chest" },
                    { "Chest", "RHip" }, {
                        "RHip",
                        "RKnee"
                    },
                    { "RKnee", "RAnkle" },
                    { "Chest", "LHip" },
                    { "LHip", "LKnee" }, {
                        "LKnee",
                        "LAnkle"
                    }
                };

                IMAGE_FILENAME = "dnn/COCO_val2014_000000000589.jpg";
                CAFFEMODEL_FILENAME = "dnn/pose_iter_160000.caffemodel";
                PROTOTXT_FILENAME = "dnn/openpose_pose_mpi_faster_4_stages.prototxt";

            }
            else if (dataset == DATASET_TYPE.HAND)
            {
                //HAND
                BODY_PARTS = new Dictionary<string, int>() {{ "Wrist", 0 },
                    { "ThumbMetacarpal", 1 },{ "ThumbProximal", 2 },{ "ThumbMiddle", 3 },{ "ThumbDistal", 4 },
                    { "IndexFingerMetacarpal", 5 }, {"IndexFingerProximal", 6 },{ "IndexFingerMiddle", 7 },{ "IndexFingerDistal", 8 },
                    { "MiddleFingerMetacarpal", 9 },{ "MiddleFingerProximal", 10 },{ "MiddleFingerMiddle", 11 },{ "MiddleFingerDistal", 12 },
                    { "RingFingerMetacarpal", 13 },{ "RingFingerProximal", 14 },{ "RingFingerMiddle", 15 },{ "RingFingerDistal", 16 },
                    { "LittleFingerMetacarpal", 17 }, {"LittleFingerProximal", 18 }, {"LittleFingerMiddle", 19 },{ "LittleFingerDistal", 20 }
                };

                POSE_PAIRS = new string[,] { {"Wrist", "ThumbMetacarpal"}, {"ThumbMetacarpal", "ThumbProximal"},
                   {"ThumbProximal", "ThumbMiddle"}, {"ThumbMiddle", "ThumbDistal"},
                   {"Wrist", "IndexFingerMetacarpal"}, {"IndexFingerMetacarpal", "IndexFingerProximal"},
                   {"IndexFingerProximal", "IndexFingerMiddle"}, {"IndexFingerMiddle", "IndexFingerDistal"},
                   {"Wrist", "MiddleFingerMetacarpal"}, {"MiddleFingerMetacarpal", "MiddleFingerProximal"},
                   {"MiddleFingerProximal", "MiddleFingerMiddle"}, {"MiddleFingerMiddle", "MiddleFingerDistal"},
                   {"Wrist", "RingFingerMetacarpal"}, {"RingFingerMetacarpal", "RingFingerProximal"},
                   {"RingFingerProximal", "RingFingerMiddle"}, {"RingFingerMiddle", "RingFingerDistal"},
                   {"Wrist", "LittleFingerMetacarpal"}, {"LittleFingerMetacarpal", "LittleFingerProximal"},
                   {"LittleFingerProximal", "LittleFingerMiddle"}, {"LittleFingerMiddle", "LittleFingerDistal"} };


                IMAGE_FILENAME = "dnn/person-s-right-hand-1257770.jpg";
                CAFFEMODEL_FILENAME = "dnn/pose_iter_102000.caffemodel";
                PROTOTXT_FILENAME = "dnn/pose_deploy.prototxt";

            }

#if UNITY_WEBGL
            getFilePath_Coroutine = GetFilePath();
            StartCoroutine(getFilePath_Coroutine);
#else
            image_filepath = Utils.getFilePath(IMAGE_FILENAME);
            caffemodel_filepath = Utils.getFilePath(CAFFEMODEL_FILENAME);
            prototxt_filepath = Utils.getFilePath(PROTOTXT_FILENAME);
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

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync(CAFFEMODEL_FILENAME, (result) =>
            {
                caffemodel_filepath = result;
            });
            yield return getFilePathAsync_1_Coroutine;

            var getFilePathAsync_2_Coroutine = Utils.getFilePathAsync(PROTOTXT_FILENAME, (result) =>
            {
                prototxt_filepath = result;
            });
            yield return getFilePathAsync_2_Coroutine;

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
                Debug.LogError(image_filepath + " is not loaded. Please see \"StreamingAssets/dnn/setup_dnn_module.pdf\". ");
                img = new Mat(368, 368, CvType.CV_8UC3, new Scalar(0, 0, 0));
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

            if (string.IsNullOrEmpty(caffemodel_filepath) || string.IsNullOrEmpty(prototxt_filepath))
            {
                Debug.LogError(caffemodel_filepath + " or " + prototxt_filepath + " is not loaded. Please see \"StreamingAssets/dnn/setup_dnn_module.pdf\". ");
            }
            else
            {
                net = Dnn.readNet(prototxt_filepath, caffemodel_filepath);
            }

            if (net == null)
            {
                Imgproc.putText(img, "model file is not loaded.", new Point(5, img.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(img, "Please read console message.", new Point(5, img.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
            }
            else
            {

                float frameWidth = img.cols();
                float frameHeight = img.rows();

                Mat input = Dnn.blobFromImage(img, inScale, new Size(inWidth, inHeight), new Scalar(0, 0, 0), false, false);

                net.setInput(input);

                //TickMeter tm = new TickMeter ();
                //tm.start ();

                Mat output = net.forward();

                //tm.stop ();
                //Debug.Log ("Inference time, ms: " + tm.getTimeMilli ());

                //Debug.Log("output.size(0) " + output.size(0));
                //Debug.Log("output.size(1) " + output.size(1));
                //Debug.Log("output.size(2) " + output.size(2));
                //Debug.Log("output.size(3) " + output.size(3));

                float[] data = new float[output.size(2) * output.size(3)];

                output = output.reshape(1, output.size(1));

                List<Point> points = new List<Point>();
                for (int i = 0; i < BODY_PARTS.Count; i++)
                {

                    output.get(i, 0, data);

                    Mat heatMap = new Mat(1, data.Length, CvType.CV_32FC1);
                    heatMap.put(0, 0, data);


                    //Originally, we try to find all the local maximums. To simplify a sample
                    //we just find a global one. However only a single pose at the same time
                    //could be detected this way.
                    Core.MinMaxLocResult result = Core.minMaxLoc(heatMap);

                    heatMap.Dispose();


                    double x = (frameWidth * (result.maxLoc.x % 46)) / 46;
                    double y = (frameHeight * (result.maxLoc.x / 46)) / 46;

                    if (result.maxVal > 0.1)
                    {
                        points.Add(new Point(x, y));
                    }
                    else
                    {
                        points.Add(null);
                    }

                }

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