using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
#if !UNITY_WSA_10_0
using OpenCVForUnityExample.DnnModel;
#endif
using OpenCVForUnityExample.MOT.ByteTrack;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Multi Object Tracking (MOT) Example
    /// An example of tracking object detection results using the MOT (Multi Object Tracking) algorithm.
    /// 
    /// ByteTrack: https://github.com/ifzhang/ByteTrack
    /// </summary>
    [RequireComponent(typeof(VideoCaptureToMatHelper))]
    public class MultiObjectTrackingExample : MonoBehaviour
    {
        BYTETracker byteTracker;
        public Toggle trackerByteTrackToggle;

        List<Scalar> palette;

#if !UNITY_WSA_10_0
        YOLOXObjectDetector objectDetector;
#endif

        public Toggle showObjectDetectorResultToggle;

        bool disableObjectDetector = false;

        [TooltipAttribute("Path to a binary file of model contains trained weights. It could be a file with extensions .caffemodel (Caffe), .pb (TensorFlow), .t7 or .net (Torch), .weights (Darknet).")]
        public string model = "yolox_tiny.onnx";

        [TooltipAttribute("Path to a text file of model contains network configuration. It could be a file with extensions .prototxt (Caffe), .pbtxt (TensorFlow), .cfg (Darknet).")]
        public string config = "";

        [TooltipAttribute("Optional path to a text file with names of classes to label detected objects.")]
        public string classes = "coco.names";

        [TooltipAttribute("Confidence threshold.")]
        public float confThreshold = 0.25f;

        [TooltipAttribute("Non-maximum suppression threshold.")]
        public float nmsThreshold = 0.45f;

        [TooltipAttribute("Maximum detections per image.")]
        public int topK = 1000;

        [TooltipAttribute("Preprocess input image by resizing to a specific width.")]
        public int inpWidth = 416;

        [TooltipAttribute("Preprocess input image by resizing to a specific height.")]
        public int inpHeight = 416;

        protected string classes_filepath;
        protected string config_filepath;
        protected string model_filepath;


#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The video capture to mat helper.
        /// </summary>
        VideoCaptureToMatHelper sourceToMatHelper;

        /// <summary>
        /// The bgr mat.
        /// </summary>
        Mat bgrMat;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// VIDEO_FILENAME
        /// </summary>
        protected static readonly string VIDEO_FILENAME = "OpenCVForUnity/768x576_mjpeg.mjpeg";

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            sourceToMatHelper = gameObject.GetComponent<VideoCaptureToMatHelper>();


#if UNITY_WSA_10_0
            
            // Disable the DNN module-dependent Tracker on UWP platforms, as it cannot be used.
            showObjectDetectorResultToggle.isOn = showObjectDetectorResultToggle.interactable = false;
            disableObjectDetector = true;
            Run();

#elif UNITY_WEBGL

            getFilePath_Coroutine = GetFilePath();
            StartCoroutine(getFilePath_Coroutine);

#else

            if (!string.IsNullOrEmpty(classes))
            {
                classes_filepath = Utils.getFilePath("OpenCVForUnity/dnn/" + classes);
                if (string.IsNullOrEmpty(classes_filepath)) Debug.Log("The file:" + classes + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnity/dnn”.");
            }
            if (!string.IsNullOrEmpty(config))
            {
                config_filepath = Utils.getFilePath("OpenCVForUnity/dnn/" + config);
                if (string.IsNullOrEmpty(config_filepath)) Debug.Log("The file:" + config + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnity/dnn”.");
            }
            if (!string.IsNullOrEmpty(model))
            {
                model_filepath = Utils.getFilePath("OpenCVForUnity/dnn/" + model);
                if (string.IsNullOrEmpty(model_filepath)) Debug.Log("The file:" + model + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnity/dnn”.");
            }
            CheckFilePaths();
            Run();

#endif
        }

#if UNITY_WEBGL
        private IEnumerator GetFilePath()
        {
            if (!string.IsNullOrEmpty(classes))
            {
                var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync("OpenCVForUnity/dnn/" + classes, (result) =>
                {
                    classes_filepath = result;
                });
                yield return getFilePathAsync_0_Coroutine;

                if (string.IsNullOrEmpty(classes_filepath)) Debug.Log("The file:" + classes + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnity/dnn”.");
            }

            if (!string.IsNullOrEmpty(config))
            {
                var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync("OpenCVForUnity/dnn/" + config, (result) =>
                {
                    config_filepath = result;
                });
                yield return getFilePathAsync_1_Coroutine;

                if (string.IsNullOrEmpty(config_filepath)) Debug.Log("The file:" + config + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnity/dnn”.");
            }

            if (!string.IsNullOrEmpty(model))
            {
                var getFilePathAsync_2_Coroutine = Utils.getFilePathAsync("OpenCVForUnity/dnn/" + model, (result) =>
                {
                    model_filepath = result;
                });
                yield return getFilePathAsync_2_Coroutine;

                if (string.IsNullOrEmpty(model_filepath)) Debug.Log("The file:" + model + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnity/dnn”.");
            }

            getFilePath_Coroutine = null;

            CheckFilePaths();
            Run();
        }
#endif

        void CheckFilePaths()
        {
            if (string.IsNullOrEmpty(model_filepath))
            {
                showObjectDetectorResultToggle.isOn = showObjectDetectorResultToggle.interactable = false;
                disableObjectDetector = true;
            }
        }

        void Run()
        {
            if (string.IsNullOrEmpty(model_filepath) || string.IsNullOrEmpty(classes_filepath))
            {
                Debug.LogError("model: " + model + " or " + "config: " + config + " or " + "classes: " + classes + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
#if !UNITY_WSA_10_0
                objectDetector = new YOLOXObjectDetector(model_filepath, config_filepath, classes_filepath, new Size(inpWidth, inpHeight), confThreshold, nmsThreshold, topK);
#endif
            }

            if (string.IsNullOrEmpty(sourceToMatHelper.requestedVideoFilePath))
                sourceToMatHelper.requestedVideoFilePath = VIDEO_FILENAME;
            sourceToMatHelper.outputColorFormat = VideoCaptureToMatHelper.ColorFormat.RGB; // Tracking API must handle 3 channels Mat image.
            sourceToMatHelper.Initialize();

            palette = new List<Scalar>();
            palette.Add(new Scalar(255, 56, 56, 255));
            palette.Add(new Scalar(255, 157, 151, 255));
            palette.Add(new Scalar(255, 112, 31, 255));
            palette.Add(new Scalar(255, 178, 29, 255));
            palette.Add(new Scalar(207, 210, 49, 255));
            palette.Add(new Scalar(72, 249, 10, 255));
            palette.Add(new Scalar(146, 204, 23, 255));
            palette.Add(new Scalar(61, 219, 134, 255));
            palette.Add(new Scalar(26, 147, 52, 255));
            palette.Add(new Scalar(0, 212, 187, 255));
            palette.Add(new Scalar(44, 153, 168, 255));
            palette.Add(new Scalar(0, 194, 255, 255));
            palette.Add(new Scalar(52, 69, 147, 255));
            palette.Add(new Scalar(100, 115, 255, 255));
            palette.Add(new Scalar(0, 24, 236, 255));
            palette.Add(new Scalar(132, 56, 255, 255));
            palette.Add(new Scalar(82, 0, 133, 255));
            palette.Add(new Scalar(203, 56, 255, 255));
            palette.Add(new Scalar(255, 149, 200, 255));
            palette.Add(new Scalar(255, 55, 199, 255));
        }

        /// <summary>
        /// Raises the video capture to mat helper initialized event.
        /// </summary>
        public void OnVideoCaptureToMatHelperInitialized()
        {
            Debug.Log("OnVideoCaptureToMatHelperInitialized");

            Mat rgbMat = sourceToMatHelper.GetMat();

            texture = new Texture2D(rgbMat.cols(), rgbMat.rows(), TextureFormat.RGB24, false);
            Utils.matToTexture2D(rgbMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(rgbMat.cols(), rgbMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


            float width = rgbMat.width();
            float height = rgbMat.height();

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }

            int fps = (int)sourceToMatHelper.GetFPS();
            fps = fps > 0 ? fps : 30;
            byteTracker = new BYTETracker(fps, 30);
            //Debug.Log("fps: " + fps);

            bgrMat = new Mat(rgbMat.rows(), rgbMat.cols(), CvType.CV_8UC3);
        }

        /// <summary>
        /// Raises the video capture to mat helper disposed event.
        /// </summary>
        public void OnVideoCaptureToMatHelperDisposed()
        {
            Debug.Log("OnVideoCaptureToMatHelperDisposed");

            ResetTrackers();

            if (bgrMat != null)
                bgrMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the video capture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnVideoCaptureToMatHelperErrorOccurred(VideoCaptureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnVideoCaptureToMatHelperErrorOccurred " + errorCode);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!sourceToMatHelper.IsInitialized())
                return;

            if (!sourceToMatHelper.IsPlaying())
                sourceToMatHelper.Play();

            if (sourceToMatHelper.IsPlaying() && sourceToMatHelper.DidUpdateThisFrame())
            {
                Mat rgbMat = sourceToMatHelper.GetMat();

#if UNITY_WSA_10_0
                Imgproc.putText(rgbMat, "Disable the DNN module-dependent Tracker on UWP platforms.", new Point(5, rgbMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
#else
                if (objectDetector == null)
                {
                    Imgproc.putText(rgbMat, "model file is not loaded.", new Point(5, rgbMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbMat, "Please read console message.", new Point(5, rgbMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    if (!disableObjectDetector)
                    {
                        Imgproc.cvtColor(rgbMat, bgrMat, Imgproc.COLOR_RGB2BGR);

                        //TickMeter tm = new TickMeter();
                        //tm.start();

                        Mat results = objectDetector.infer(bgrMat);

                        //tm.stop();
                        //Debug.Log("ObjectDetector Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                        Imgproc.cvtColor(bgrMat, rgbMat, Imgproc.COLOR_BGR2RGB);

                        if (showObjectDetectorResultToggle.isOn)
                            objectDetector.visualize(rgbMat, results, false, true);

                        if (trackerByteTrackToggle.isOn)
                        {
                            // update trackers.
                            List<Detection> inputs = ConvertToByteTrackDetections(results);
                            List<Track> outputs = byteTracker.Update(inputs);

                            foreach (var output in outputs)
                            {
                                int track_id = output.TrackId;
                                TlwhRect rect = (TlwhRect)output.Detection.Rect;
                                Scalar color = palette[track_id % palette.Count];

                                Imgproc.rectangle(rgbMat, new Point(rect.Left, rect.Top), new Point(rect.Left + rect.Width, rect.Top + rect.Height), color, 2);

                                string label = "ID:" + track_id;
                                int[] baseLine = new int[1];
                                Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

                                float left = rect.Left;
                                float top = Mathf.Max(rect.Top, (float)labelSize.height);
                                Imgproc.rectangle(rgbMat, new Point(left, top - labelSize.height),
                                    new Point(left + labelSize.width, top + baseLine[0]), color, Core.FILLED);
                                Imgproc.putText(rgbMat, label, new Point(left, top), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, Scalar.all(255), 1, Imgproc.LINE_AA);
                            }
                        }
                    }
                }
#endif

                Utils.matToTexture2D(rgbMat, texture);
            }
        }

        private void ResetTrackers()
        {
            if (byteTracker != null)
                byteTracker.Clear();

            if (!disableObjectDetector)
                showObjectDetectorResultToggle.interactable = true;
        }

        private List<Detection> ConvertToByteTrackDetections(Mat results)
        {
            List<Detection> inputs = new List<Detection>();

            if (results.empty() || results.cols() < 6)
                return inputs;

            for (int i = results.rows() - 1; i >= 0; --i)
            {
                float[] box = new float[4];
                results.get(i, 0, box);
                float[] conf = new float[1];
                results.get(i, 4, conf);
                //float[] cls = new float[1];
                //results.get(i, 5, cls);

                float left = box[0];
                float top = box[1];
                float width = box[2] - box[0];
                float height = box[3] - box[1];
                float score = conf[0];

                inputs.Add(new Detection(new TlwhRect(top, left, width, height), score));
            }

            return inputs;
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (sourceToMatHelper != null)
                sourceToMatHelper.Dispose();

#if !UNITY_WSA_10_0
            if (objectDetector != null)
                objectDetector.dispose();
#endif

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

        /// <summary>
        /// Raises the reset trackers button click event.
        /// </summary>
        public void OnResetTrackersButtonClick()
        {
            ResetTrackers();
        }
    }
}