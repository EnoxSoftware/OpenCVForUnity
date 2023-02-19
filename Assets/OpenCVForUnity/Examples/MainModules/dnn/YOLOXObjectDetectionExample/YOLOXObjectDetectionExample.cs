#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

#if !UNITY_WSA_10_0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVRect = OpenCVForUnity.CoreModule.Rect;
using OpenCVRange = OpenCVForUnity.CoreModule.Range;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// YOLOX ObjectDetection Example
    /// An example of using OpenCV dnn module with YOLOX Object Detection.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/master/models/object_detection_yolox.
    /// https://github.com/Megvii-BaseDetection/YOLOX
    /// https://github.com/Megvii-BaseDetection/YOLOX/tree/main/demo/ONNXRuntime
    /// 
    /// [Tested Models]
    /// yolox_nano.onnx https://github.com/Megvii-BaseDetection/YOLOX/releases/download/0.1.1rc0/yolox_nano.onnx
    /// yolox_tiny.onnx https://github.com/Megvii-BaseDetection/YOLOX/releases/download/0.1.1rc0/yolox_tiny.onnx
    /// yolox_s.onnx https://github.com/Megvii-BaseDetection/YOLOX/releases/download/0.1.1rc0/yolox_s.onnx
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class YOLOXObjectDetectionExample : MonoBehaviour
    {
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


        [Header("TEST")]

        [TooltipAttribute("Path to test input image.")]
        public string testInputImage;

        protected string classes_filepath;
        protected string config_filepath;
        protected string model_filepath;


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
        /// The YOLOX ObjectDetector.
        /// </summary>
        YOLOXObjectDetector objectDetector;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;


#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

#if UNITY_WEBGL
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

            Run();
        }
#endif

        // Use this for initialization
        void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            if (string.IsNullOrEmpty(model_filepath) || string.IsNullOrEmpty(classes_filepath))
            {
                Debug.LogError("model: " + model + " or " + "config: " + config + " or " + "classes: " + classes + " is not loaded.");
            }
            else
            {
                objectDetector = new YOLOXObjectDetector(model_filepath, config_filepath, classes_filepath, new Size(inpWidth, inpHeight), confThreshold, nmsThreshold, topK);
            }

            if (string.IsNullOrEmpty(testInputImage))
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
                webCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
#endif
                webCamTextureToMatHelper.Initialize();
            }
            else
            {
                /////////////////////
                // TEST

                var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync("OpenCVForUnity/dnn/" + testInputImage, (result) =>
                {
                    string test_input_image_filepath = result;
                    if (string.IsNullOrEmpty(test_input_image_filepath)) Debug.Log("The file:" + testInputImage + " did not exist in the folder “Assets/StreamingAssets/OpenCVForUnity/dnn”.");

                    Mat img = Imgcodecs.imread(test_input_image_filepath);
                    if (img.empty())
                    {
                        img = new Mat(424, 640, CvType.CV_8UC3, new Scalar(0, 0, 0));
                        Imgproc.putText(img, testInputImage + " is not loaded.", new Point(5, img.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                        Imgproc.putText(img, "Please read console message.", new Point(5, img.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    }
                    else
                    {
                        TickMeter tm = new TickMeter();
                        tm.start();

                        Mat results = objectDetector.infer(img);

                        tm.stop();
                        Debug.Log("YOLOXObjectDetector Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                        objectDetector.visualize(img, results, true, false);
                    }

                    gameObject.transform.localScale = new Vector3(img.width(), img.height(), 1);
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

                    Imgproc.cvtColor(img, img, Imgproc.COLOR_BGR2RGB);
                    Texture2D texture = new Texture2D(img.cols(), img.rows(), TextureFormat.RGB24, false);
                    Utils.matToTexture2D(img, texture);
                    gameObject.GetComponent<Renderer>().material.mainTexture = texture;

                });
                StartCoroutine(getFilePathAsync_0_Coroutine);

                /////////////////////
            }
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(webCamTextureMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", webCamTextureMat.width().ToString());
                fpsMonitor.Add("height", webCamTextureMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }


            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();

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

            bgrMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC3);
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (bgrMat != null)
                bgrMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update()
        {

            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                if (objectDetector == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    Imgproc.cvtColor(rgbaMat, bgrMat, Imgproc.COLOR_RGBA2BGR);

                    //TickMeter tm = new TickMeter();
                    //tm.start();

                    Mat results = objectDetector.infer(bgrMat);

                    //tm.stop();
                    //Debug.Log("YOLOXObjectDetector Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                    Imgproc.cvtColor(bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                    objectDetector.visualize(rgbaMat, results, false, true);
                }

                Utils.matToTexture2D(rgbaMat, texture);
            }

        }


        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            webCamTextureToMatHelper.Dispose();

            if (objectDetector != null)
                objectDetector.dispose();

            Utils.setDebugMode(false);

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
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.requestedIsFrontFacing;
        }

        private class YOLOXObjectDetector
        {
            Size input_size;
            float conf_threshold;
            float nms_threshold;
            int topK;
            int backend;
            int target;

            int num_classes = 80;
            bool class_agnostic = false;// Non-use of multi-class NMS

            Net object_detection_net;
            Mat grids;
            Mat expanded_strides;
            int[] strides = new int[] { 8, 16, 32 };

            List<string> classNames;

            List<Scalar> palette;

            Mat maxSizeImg;

            Mat pickup_blob_numx6;
            Mat boxesMat;

            Mat boxes_m_c4;
            Mat confidences_m;
            Mat class_ids_m;
            MatOfRect2d boxes;
            MatOfFloat confidences;
            MatOfInt class_ids;

            public YOLOXObjectDetector(string modelFilepath, string configFilepath, string classesFilepath, Size inputSize, float confThreshold = 0.25f, float nmsThreshold = 0.45f, int topK = 1000, int backend = Dnn.DNN_BACKEND_OPENCV, int target = Dnn.DNN_TARGET_CPU)
            {
                // initialize
                if (!string.IsNullOrEmpty(modelFilepath))
                {
                    object_detection_net = Dnn.readNet(modelFilepath, configFilepath);
                }

                if (!string.IsNullOrEmpty(classesFilepath))
                {
                    classNames = readClassNames(classesFilepath);
                    num_classes = classNames.Count;
                }

                input_size = new Size(inputSize.width > 0 ? inputSize.width : 416, inputSize.height > 0 ? inputSize.height : 416);
                conf_threshold = Mathf.Clamp01(confThreshold);
                nms_threshold = Mathf.Clamp01(nmsThreshold);
                this.topK = topK;
                this.backend = backend;
                this.target = target;

                object_detection_net.setPreferableBackend(this.backend);
                object_detection_net.setPreferableTarget(this.target);

                generateAnchors(out grids, out expanded_strides);

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


            protected virtual Mat preprocess(Mat image)
            {
                // Add padding to make it square.
                // (padding on the bottom and right side)
                int max = Mathf.Max(image.cols(), image.rows());

                if (maxSizeImg == null)
                    maxSizeImg = new Mat(max, max, image.type(), Scalar.all(114));
                if (maxSizeImg.width() != max || maxSizeImg.height() != max)
                {
                    maxSizeImg.create(max, max, image.type());
                    Imgproc.rectangle(maxSizeImg, new OpenCVRect(0, 0, maxSizeImg.width(), maxSizeImg.height()), Scalar.all(114), -1);
                }

                Mat _maxSizeImg_roi = new Mat(maxSizeImg, new OpenCVRect(0, 0, image.cols(), image.rows()));
                image.copyTo(_maxSizeImg_roi);

                // Create a 4D blob from a frame.
                Mat blob = Dnn.blobFromImage(maxSizeImg, 1.0, input_size, Scalar.all(0), true, false, CvType.CV_32F); // HWC to NCHW, BGR to RGB

                return blob;// [1, 3, h, w]
            }

            public virtual Mat infer(Mat image)
            {
                // cheack
                if (image.channels() != 3)
                {
                    Debug.Log("The input image must be in BGR format.");
                    return new Mat();
                }

                // Preprocess
                Mat input_blob = preprocess(image);

                // Forward
                object_detection_net.setInput(input_blob);

                List<Mat> output_blob = new List<Mat>();
                object_detection_net.forward(output_blob, object_detection_net.getUnconnectedOutLayersNames());

                // Postprocess
                Mat results = postprocess(output_blob[0], image.size());

                // scale_boxes
                float maxSize = Mathf.Max((float)image.size().width, (float)image.size().height);
                float x_factor = maxSize / (float)input_size.width;
                float y_factor = maxSize / (float)input_size.height;

                for (int i = 0; i < results.rows(); ++i)
                {
                    float[] results_arr = new float[4];
                    results.get(i, 0, results_arr);
                    float x1 = Mathf.Round(results_arr[0] * x_factor);
                    float y1 = Mathf.Round(results_arr[1] * y_factor);
                    float x2 = Mathf.Round(results_arr[2] * x_factor);
                    float y2 = Mathf.Round(results_arr[3] * y_factor);

                    results.put(i, 0, new float[] { x1, y1, x2, y2 });
                }

                input_blob.Dispose();
                for (int i = 0; i < output_blob.Count; i++)
                {
                    output_blob[i].Dispose();
                }

                return results;
            }

            protected virtual Mat postprocess(Mat output_blob, Size original_shape)
            {
                Mat output_blob_0 = output_blob;

                if (output_blob_0.size(2) < 5 + num_classes)
                    return new Mat();

                int num = output_blob_0.size(1);
                Mat output_blob_numx85 = output_blob_0.reshape(1, num);
                Mat box_delta = output_blob_numx85.colRange(new OpenCVRange(0, 4));
                Mat confidence = output_blob_numx85.colRange(new OpenCVRange(4, 5));
                Mat classes_scores_delta = output_blob_numx85.colRange(new OpenCVRange(5, 5 + num_classes));


                Mat cxy_delta = box_delta.colRange(new OpenCVRange(0, 2));
                Mat wh_delta = box_delta.colRange(new OpenCVRange(2, 4));

                Mat grids_numx2 = grids.reshape(1, num);//num*2*CV_32FC1
                Mat expanded_strides_numx2 = expanded_strides.reshape(1, num);//num*2*CV_32FC1
                Core.add(cxy_delta, grids_numx2, cxy_delta);
                Core.multiply(cxy_delta, expanded_strides_numx2, cxy_delta);
                Core.exp(wh_delta, wh_delta);
                Core.multiply(wh_delta, expanded_strides_numx2, wh_delta);


                // pre-NMS
                // Pick up rows to process by conf_threshold value and calculate scores and class_ids.
                if (pickup_blob_numx6 == null)
                    pickup_blob_numx6 = new Mat(300, 6, CvType.CV_32FC1, new Scalar(0));

                Imgproc.rectangle(pickup_blob_numx6, new OpenCVRect(4, 0, 1, pickup_blob_numx6.rows()), Scalar.all(0), -1);

                float[] conf_arr = new float[num];
                confidence.get(0, 0, conf_arr);
                int ind = 0;
                for (int i = 0; i < num; ++i)
                {
                    float conf = conf_arr[i];
                    if (conf > conf_threshold)
                    {
                        if (ind > pickup_blob_numx6.rows())
                        {
                            Mat _conf_blob_numx6 = new Mat(pickup_blob_numx6.rows() * 2, pickup_blob_numx6.cols(), pickup_blob_numx6.type(), new Scalar(0));
                            pickup_blob_numx6.copyTo(_conf_blob_numx6.rowRange(0, pickup_blob_numx6.rows()));
                            pickup_blob_numx6 = _conf_blob_numx6;
                        }

                        float[] box_arr = new float[4];
                        box_delta.get(i, 0, box_arr);

                        Mat cls_scores = classes_scores_delta.row(i);
                        Core.MinMaxLocResult minmax = Core.minMaxLoc(cls_scores);

                        pickup_blob_numx6.put(ind, 0, new float[] { box_arr[0], box_arr[1], box_arr[2], box_arr[3], ((float)minmax.maxVal * conf), (float)minmax.maxLoc.x });
                        ind++;
                    }
                }

                int num_pickup = pickup_blob_numx6.rows();
                Mat pickup_box_delta = pickup_blob_numx6.colRange(new OpenCVRange(0, 4));
                Mat pickup_confidence = pickup_blob_numx6.colRange(new OpenCVRange(4, 5));

                // Convert boxes from [cx, cy, w, h] to [x, y, w, h] where Rect2d data style.
                if (boxesMat == null || boxesMat.rows() != num_pickup)
                    boxesMat = new Mat(num_pickup, 4, CvType.CV_32FC1);
                Mat pickup_cxy_delta = pickup_box_delta.colRange(new OpenCVRange(0, 2));
                Mat pickup_wh_delta = pickup_box_delta.colRange(new OpenCVRange(2, 4));
                Mat pickup_xy1 = boxesMat.colRange(new OpenCVRange(0, 2));
                Mat pickup_xy2 = boxesMat.colRange(new OpenCVRange(2, 4));
                pickup_wh_delta.copyTo(pickup_xy2);
                Core.divide(pickup_wh_delta, new Scalar(2.0), pickup_wh_delta);
                Core.subtract(pickup_cxy_delta, pickup_wh_delta, pickup_xy1);


                if (boxes_m_c4 == null || boxes_m_c4.rows() != num_pickup)
                    boxes_m_c4 = new Mat(num_pickup, 1, CvType.CV_64FC4);
                if (confidences_m == null || confidences_m.rows() != num_pickup)
                    confidences_m = new Mat(num_pickup, 1, CvType.CV_32FC1);

                if (boxes == null || boxes.rows() != num_pickup)
                    boxes = new MatOfRect2d(boxes_m_c4);
                if (confidences == null || confidences.rows() != num_pickup)
                    confidences = new MatOfFloat(confidences_m);


                // non-maximum suppression
                Mat boxes_m_c1 = boxes_m_c4.reshape(1, num_pickup);
                boxesMat.convertTo(boxes_m_c1, CvType.CV_64F);
                pickup_confidence.copyTo(confidences_m);

                MatOfInt indices = new MatOfInt();

                if (class_agnostic)
                {
                    // NMS
                    Dnn.NMSBoxes(boxes, confidences, conf_threshold, nms_threshold, indices, 1f, topK);
                }
                else
                {
                    Mat pickup_class_ids = pickup_blob_numx6.colRange(new OpenCVRange(5, 6));

                    if (class_ids_m == null || class_ids_m.rows() != num_pickup)
                        class_ids_m = new Mat(num_pickup, 1, CvType.CV_32SC1);
                    if (class_ids == null || class_ids.rows() != num_pickup)
                        class_ids = new MatOfInt(class_ids_m);

                    pickup_class_ids.convertTo(class_ids_m, CvType.CV_32S);

                    // multi-class NMS
                    Dnn.NMSBoxesBatched(boxes, confidences, class_ids, conf_threshold, nms_threshold, indices, 1f, topK);
                }

                Mat results = new Mat(indices.rows(), 6, CvType.CV_32FC1);

                for (int i = 0; i < indices.rows(); ++i)
                {
                    int idx = (int)indices.get(i, 0)[0];

                    pickup_blob_numx6.row(idx).copyTo(results.row(i));

                    float[] bbox_arr = new float[4];
                    boxesMat.get(idx, 0, bbox_arr);
                    float x = bbox_arr[0];
                    float y = bbox_arr[1];
                    float w = bbox_arr[2];
                    float h = bbox_arr[3];
                    results.put(i, 0, new float[] { x, y, x + w, y + h });
                }

                indices.Dispose();

                // [
                //   [xyxy, conf, cls]
                //   ...
                //   [xyxy, conf, cls]
                // ]
                return results;
            }

            public virtual void visualize(Mat image, Mat results, bool print_results = false, bool isRGB = false)
            {
                if (image.IsDisposed)
                    return;

                if (results.empty() || results.cols() < 6)
                    return;

                for (int i = results.rows() - 1; i >= 0; --i)
                {
                    float[] box = new float[4];
                    results.get(i, 0, box);
                    float[] conf = new float[1];
                    results.get(i, 4, conf);
                    float[] cls = new float[1];
                    results.get(i, 5, cls);

                    float left = box[0];
                    float top = box[1];
                    float right = box[2];
                    float bottom = box[3];
                    int classId = (int)cls[0];

                    Scalar c = palette[classId % palette.Count];
                    Scalar color = isRGB ? c : new Scalar(c.val[2], c.val[1], c.val[0], c.val[3]);

                    Imgproc.rectangle(image, new Point(left, top), new Point(right, bottom), color, 2);

                    string label = String.Format("{0:0.00}", conf[0]);
                    if (classNames != null && classNames.Count != 0)
                    {
                        if (classId < (int)classNames.Count)
                        {
                            label = classNames[classId] + " " + label;
                        }
                    }

                    int[] baseLine = new int[1];
                    Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

                    top = Mathf.Max((float)top, (float)labelSize.height);
                    Imgproc.rectangle(image, new Point(left, top - labelSize.height),
                        new Point(left + labelSize.width, top + baseLine[0]), color, Core.FILLED);
                    Imgproc.putText(image, label, new Point(left, top), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, Scalar.all(255), 1, Imgproc.LINE_AA);
                }

                // Print results
                if (print_results)
                {
                    StringBuilder sb = new StringBuilder();

                    for (int i = 0; i < results.rows(); ++i)
                    {
                        float[] box = new float[4];
                        results.get(i, 0, box);
                        float[] conf = new float[1];
                        results.get(i, 4, conf);
                        float[] cls = new float[1];
                        results.get(i, 5, cls);

                        int classId = (int)cls[0];
                        string label = String.Format("{0:0}", cls[0]);
                        if (classNames != null && classNames.Count != 0)
                        {
                            if (classId < (int)classNames.Count)
                            {
                                label = classNames[classId] + " " + label;
                            }
                        }

                        sb.AppendLine(String.Format("-----------object {0}-----------", i + 1));
                        sb.AppendLine(String.Format("conf: {0:0.0000}", conf[0]));
                        sb.AppendLine(String.Format("cls: {0:0}", label));
                        sb.AppendLine(String.Format("box: {0:0} {1:0} {2:0} {3:0}", box[0], box[1], box[2], box[3]));
                    }

                    Debug.Log(sb);
                }
            }

            public virtual void dispose()
            {
                if (object_detection_net != null)
                    object_detection_net.Dispose();

                if (maxSizeImg != null)
                    maxSizeImg.Dispose();

                maxSizeImg = null;

                if (pickup_blob_numx6 != null)
                    pickup_blob_numx6.Dispose();
                if (boxesMat != null)
                    boxesMat.Dispose();

                pickup_blob_numx6 = null;
                boxesMat = null;

                if (boxes_m_c4 != null)
                    boxes_m_c4.Dispose();
                if (confidences_m != null)
                    confidences_m.Dispose();
                if (class_ids_m != null)
                    class_ids_m.Dispose();
                if (boxes != null)
                    boxes.Dispose();
                if (confidences != null)
                    confidences.Dispose();
                if (class_ids != null)
                    class_ids.Dispose();

                boxes_m_c4 = null;
                confidences_m = null;
                class_ids_m = null;
                boxes = null;
                confidences = null;
                class_ids = null;
            }

            protected virtual void generateAnchors(out Mat grids, out Mat expanded_strides)
            {
                int num = 0;

                int[] hsizes = new int[strides.Length];// stride for stride in self.strides
                int[] wsizes = new int[strides.Length];// stride for stride in self.strides
                for (int i = 0; i < strides.Length; i++)
                {
                    hsizes[i] = (int)(input_size.height / strides[i]);
                    wsizes[i] = (int)(input_size.width / strides[i]);

                    num += hsizes[i] * wsizes[i];
                }

                grids = new Mat(new int[] { 1, num, 2 }, CvType.CV_32FC1);
                expanded_strides = new Mat(new int[] { 1, num, 2 }, CvType.CV_32FC1);

                Mat grids_numx2 = grids.reshape(1, num);//num*2*CV_32FC1
                Mat expanded_strides_numx2 = expanded_strides.reshape(1, num);//num*2*CV_32FC1
                int index = 0;

                for (int i = 0; i < strides.Length; i++)
                {
                    int hsize = hsizes[i];
                    int wsize = wsizes[i];
                    int stride = strides[i];

                    // #xv, yv = np.meshgrid(np.arange(hsize), np.arange(wsize))
                    Mat h_arange = arange(0, hsize);
                    Mat w_arange = arange(0, wsize).t();
                    Mat xv = new Mat(hsize, hsize, CvType.CV_32FC1);
                    tile(h_arange, hsize, 1, xv);
                    Mat yv = new Mat(wsize, wsize, CvType.CV_32FC1);
                    tile(w_arange, 1, wsize, yv);

                    // #grid = np.stack((xv, yv), 2).reshape(1, -1, 2)
                    // #self.grids.append(grid)
                    Mat xv_totalx1 = xv.reshape(1, (int)xv.total());//total*1*CV_32FC1
                    Mat grid_roi = new Mat(grids_numx2, new OpenCVRect(0, index, 1, (int)xv.total()));//total*1*CV_32FC1
                    xv_totalx1.copyTo(grid_roi);
                    Mat yv_totalx1 = yv.reshape(1, (int)yv.total());//total*1*CV_32FC1
                    grid_roi = new Mat(grids_numx2, new OpenCVRect(1, index, 1, (int)yv.total()));//total*1*CV_32FC1
                    yv_totalx1.copyTo(grid_roi);

                    // #shap = e = grid.shape[:2]
                    // #self.expanded_strides.append(np.full((*shape, 1), stride))
                    int shape = hsize * wsize;
                    Mat expanded_strides_roi = expanded_strides_numx2.rowRange(index, index + shape);
                    Imgproc.rectangle(expanded_strides_roi, new OpenCVRect(0, 0, 2, shape), Scalar.all(stride));

                    index += hsize * wsize;
                }
            }

            private Mat arange(int start, int stop)
            {
                if (start < 0 || stop < 0 || stop < start || stop == start)
                    throw new ArgumentException("start < 0 || stop < 0 || stop < start || stop == start");

                float[] data = Enumerable.Range(start, stop).Select(i => (float)i).ToArray();
                Mat dst = new Mat(1, stop - start, CvType.CV_32FC1);
                dst.put(0, 0, data);

                return dst;
            }

            private void tile(Mat a, int ny, int nx, Mat dst)
            {
                if (a == null)
                    throw new ArgumentNullException("a");
                if (a != null)
                    a.ThrowIfDisposed();

                if (dst == null)
                    throw new ArgumentNullException("dst");
                if (dst != null)
                    dst.ThrowIfDisposed();
                if (dst.rows() != a.rows() * ny || dst.cols() != a.cols() * nx || dst.type() != a.type())
                    throw new ArgumentException("dst.rows() != a.rows() * ny || dst.cols() != a.cols() * nx || dst.type() != a.type()");

                Core.repeat(a, ny, nx, dst);
            }

            protected virtual List<string> readClassNames(string filename)
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
}
#endif

#endif