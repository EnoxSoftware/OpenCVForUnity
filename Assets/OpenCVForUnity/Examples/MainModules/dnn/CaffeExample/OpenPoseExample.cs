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
    /// OpenPose Example
    /// This example uses OpenPose human pose estimation network.
    /// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/openpose.py.
    /// </summary>
    public class OpenPoseExample : MonoBehaviour
    {
        const float inWidth = 368;
        const float inHeight = 368;

        //COCO
        //        Dictionary<string, int> BODY_PARTS
        //        = new Dictionary<string, int> () {
        //            { "Nose", 0 }, { "Neck", 1 }, { "RShoulder", 2 }, { "RElbow", 3 }, {
        //                "RWrist",
        //                4
        //            },
        //            { "LShoulder",5 }, { "LElbow", 6 }, { "LWrist", 7 }, { "RHip", 8 }, {
        //                "RKnee",
        //                9
        //            },
        //            { "RAnkle", 10 }, { "LHip", 11 }, { "LKnee", 12 }, { "LAnkle", 13 }, {
        //                "REye",
        //                14
        //            },
        //            { "LEye", 15 }, { "REar", 16 }, { "LEar", 17 }, {
        //                "Background",
        //                18
        //            }
        //        };
        //
        //        string[,] POSE_PAIRS
        //        = new string[,] {
        //            { "Neck", "RShoulder" }, { "Neck", "LShoulder" }, {
        //                "RShoulder",
        //                "RElbow"
        //            },
        //            { "RElbow", "RWrist" }, { "LShoulder", "LElbow" }, {
        //                "LElbow",
        //                "LWrist"
        //            },
        //            { "Neck", "RHip" }, { "RHip", "RKnee" }, { "RKnee", "RAnkle" }, {
        //                "Neck",
        //                "LHip"
        //            },
        //            { "LHip", "LKnee" }, { "LKnee", "LAnkle" }, { "Neck", "Nose" }, {
        //                "Nose",
        //                "REye"
        //            },
        //            { "REye", "REar" }, { "Nose", "LEye" }, { "LEye", "LEar" }
        //        };

        //MPI
        Dictionary<string, int> BODY_PARTS
        = new Dictionary<string, int> () { { "Head", 0 }, { "Neck", 1 }, {
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

        string[,] POSE_PAIRS = new string[,] {
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

        string COCO_val2014_000000000589_jpg_filepath;
        string pose_iter_160000_caffemodel_filepath;
        string openpose_pose_mpi_faster_4_stages_prototxt_filepath;

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
            COCO_val2014_000000000589_jpg_filepath = Utils.getFilePath ("dnn/COCO_val2014_000000000589.jpg");
            pose_iter_160000_caffemodel_filepath = Utils.getFilePath ("dnn/pose_iter_160000.caffemodel");
            openpose_pose_mpi_faster_4_stages_prototxt_filepath = Utils.getFilePath ("dnn/openpose_pose_mpi_faster_4_stages.prototxt");
            Run ();
#endif
        }

        #if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator GetFilePath ()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync ("dnn/COCO_val2014_000000000589.jpg", (result) => {
                COCO_val2014_000000000589_jpg_filepath = result;
            });
            coroutines.Push (getFilePathAsync_0_Coroutine);
            yield return StartCoroutine (getFilePathAsync_0_Coroutine);

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync ("dnn/pose_iter_160000.caffemodel", (result) => {
                pose_iter_160000_caffemodel_filepath = result;
            });
            coroutines.Push (getFilePathAsync_1_Coroutine);
            yield return StartCoroutine (getFilePathAsync_1_Coroutine);

            var getFilePathAsync_2_Coroutine = Utils.getFilePathAsync ("dnn/openpose_pose_mpi_faster_4_stages.prototxt", (result) => {
                openpose_pose_mpi_faster_4_stages_prototxt_filepath = result;
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

            Mat img = Imgcodecs.imread (COCO_val2014_000000000589_jpg_filepath);
            #if !UNITY_WSA_10_0
            if (img.empty ()) {
                Debug.LogError ("dnn/COCO_val2014_000000000589.jpg is not loaded.The image file can be downloaded here: \"https://github.com/CMU-Perceptual-Computing-Lab/openpose/blob/master/examples/media/COCO_val2014_000000000589.jpg\" folder. ");
                img = new Mat (368, 368, CvType.CV_8UC3, new Scalar (0, 0, 0));

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
           
            if (string.IsNullOrEmpty (pose_iter_160000_caffemodel_filepath) || string.IsNullOrEmpty (openpose_pose_mpi_faster_4_stages_prototxt_filepath)) {
                Debug.LogError ("model file is not loaded.The model and prototxt file can be downloaded here: \"http://posefs1.perception.cs.cmu.edu/OpenPose/models/pose/mpi/pose_iter_160000.caffemodel\",\"https://github.com/opencv/opencv_extra/blob/master/testdata/dnn/openpose_pose_mpi_faster_4_stages.prototxt\".Please copy to “Assets/StreamingAssets/dnn/” folder. ");
            } else {
                net = Dnn.readNetFromCaffe (openpose_pose_mpi_faster_4_stages_prototxt_filepath, pose_iter_160000_caffemodel_filepath);

                //Intel's Deep Learning Inference Engine backend is supported on Windows 64bit platform only. Please refer to ReadMe.pdf for the setup procedure.
                //net.setPreferableBackend (Dnn.DNN_BACKEND_INFERENCE_ENGINE);
            }

            if (net == null) {

                Imgproc.putText (img, "model file is not loaded.", new Point (5, img.rows () - 30), Core.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar (255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText (img, "Please read console message.", new Point (5, img.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar (255, 255, 255), 2, Imgproc.LINE_AA, false);

            } else {

                float frameWidth = img.cols ();
                float frameHeight = img.rows ();

                Mat input = Dnn.blobFromImage (img, 1.0 / 255, new Size (inWidth, inHeight), new Scalar (0, 0, 0), false, false);
               
                net.setInput (input);

//                TickMeter tm = new TickMeter ();
//                tm.start ();

                Mat output = net.forward ();

//                tm.stop ();
//                Debug.Log ("Inference time, ms: " + tm.getTimeMilli ());


                output = output.reshape (1, 16);


                float[] data = new float[46 * 46];
                List<Point> points = new List<Point> ();
                for (int i = 0; i < BODY_PARTS.Count; i++) {

                    output.get (i, 0, data);

                    Mat heatMap = new Mat (1, data.Length, CvType.CV_32FC1);
                    heatMap.put (0, 0, data);

    
                    //Originally, we try to find all the local maximums. To simplify a sample
                    //we just find a global one. However only a single pose at the same time
                    //could be detected this way.
                    Core.MinMaxLocResult result = Core.minMaxLoc (heatMap);

                    heatMap.Dispose ();


                    double x = (frameWidth * (result.maxLoc.x % 46)) / 46;
                    double y = (frameHeight * (result.maxLoc.x / 46)) / 46;

                    if (result.maxVal > 0.1) {
                        points.Add (new Point (x, y));
                    } else {
                        points.Add (null);
                    }

                }

                for (int i = 0; i < POSE_PAIRS.GetLength (0); i++) {
                    string partFrom = POSE_PAIRS [i, 0];
                    string partTo = POSE_PAIRS [i, 1];

                    int idFrom = BODY_PARTS [partFrom];
                    int idTo = BODY_PARTS [partTo];

                    if (points [idFrom] != null && points [idTo] != null) {
                        Imgproc.line (img, points [idFrom], points [idTo], new Scalar (0, 255, 0), 3);
                        Imgproc.ellipse (img, points [idFrom], new Size (3, 3), 0, 0, 360, new Scalar (0, 0, 255), Core.FILLED);
                        Imgproc.ellipse (img, points [idTo], new Size (3, 3), 0, 0, 360, new Scalar (0, 0, 255), Core.FILLED);
                    }
                }
                    


                MatOfDouble timings = new MatOfDouble ();
                long t = net.getPerfProfile (timings);
                Debug.Log ("t: " + t);
                Debug.Log ("timings.dump(): " + timings.dump ());

                double freq = Core.getTickFrequency () / 1000;
                Debug.Log ("freq: " + freq);

                Imgproc.putText (img, (t / freq) + "ms", new Point (10, img.height () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.6, new Scalar (0, 0, 255), 2);
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