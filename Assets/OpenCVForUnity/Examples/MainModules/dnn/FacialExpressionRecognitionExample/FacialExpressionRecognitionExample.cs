#if !(PLATFORM_LUMIN && !UNITY_EDITOR)

#if !UNITY_WSA_10_0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVRect = OpenCVForUnity.CoreModule.Rect;
using OpenCVRange = OpenCVForUnity.CoreModule.Range;
using System.Runtime.InteropServices;
using System.Linq;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Facial Expression Recognition Example
    /// An example of using OpenCV dnn module with Facial Expression Recognition.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/master/models/facial_expression_recognition.
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class FacialExpressionRecognitionExample : MonoBehaviour
    {
        [Header("TEST")]

        [TooltipAttribute("Path to test input image.")]
        public string testInputImage;

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
        /// The facial expression recognizer.
        /// </summary>
        FacialExpressionRecognizer facialExpressionRecognizer;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// FACIAL_EXPRESSION_RECOGNITION_MODEL_FILENAME
        /// </summary>
        protected static readonly string FACIAL_EXPRESSION_RECOGNITION_MODEL_FILENAME = "OpenCVForUnity/dnn/facial_expression_recognition_mobilefacenet_2022july.onnx";

        /// <summary>
        /// The facial expression recognition model filepath.
        /// </summary>
        string facial_expression_recognition_model_filepath;

        /// <summary>
        /// FACE_RECOGNITION_MODEL_FILENAME
        /// </summary>
        protected static readonly string FACE_RECOGNITION_MODEL_FILENAME = "OpenCVForUnity/dnn/face_recognition_sface_2021dec.onnx";

        /// <summary>
        /// The face recognition model filepath.
        /// </summary>
        string face_recognition_model_filepath;


        /// <summary>
        /// The FaceDetectorYN Model.
        /// </summary>
        FaceDetectorYNModel faceDetector;

        int inputSizeW = 320;
        int inputSizeH = 320;
        float scoreThreshold = 0.9f;
        float nmsThreshold = 0.3f;
        int topK = 5000;

        /// <summary>
        /// FACE_DETECTION_MODEL_FILENAME
        /// </summary>
        protected static readonly string FACE_DETECTION_MODEL_FILENAME = "OpenCVForUnity/dnn/face_detection_yunet_2022mar.onnx";

        /// <summary>
        /// The face detection model filepath.
        /// </summary>
        string face_detection_model_filepath;


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
            face_detection_model_filepath = Utils.getFilePath(FACE_DETECTION_MODEL_FILENAME);
            facial_expression_recognition_model_filepath = Utils.getFilePath(FACIAL_EXPRESSION_RECOGNITION_MODEL_FILENAME);
            face_recognition_model_filepath = Utils.getFilePath(FACE_RECOGNITION_MODEL_FILENAME);
            Run();
#endif
        }

#if UNITY_WEBGL
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync(FACE_DETECTION_MODEL_FILENAME, (result) =>
            {
                face_detection_model_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync(FACIAL_EXPRESSION_RECOGNITION_MODEL_FILENAME, (result) =>
            {
                facial_expression_recognition_model_filepath = result;
            });
            yield return getFilePathAsync_1_Coroutine;

            var getFilePathAsync_2_Coroutine = Utils.getFilePathAsync(FACE_RECOGNITION_MODEL_FILENAME, (result) =>
            {
                face_recognition_model_filepath = result;
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


            if (string.IsNullOrEmpty(face_detection_model_filepath))
            {
                Debug.LogError(FACE_DETECTION_MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                faceDetector = new FaceDetectorYNModel(face_detection_model_filepath, "", new Size(inputSizeW, inputSizeH), scoreThreshold, nmsThreshold, topK);
            }

            if (string.IsNullOrEmpty(facial_expression_recognition_model_filepath) || string.IsNullOrEmpty(face_recognition_model_filepath))
            {
                Debug.LogError(FACIAL_EXPRESSION_RECOGNITION_MODEL_FILENAME + " or " + FACE_RECOGNITION_MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                facialExpressionRecognizer = new FacialExpressionRecognizer(facial_expression_recognition_model_filepath, face_recognition_model_filepath, "");
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

                        Mat faces = faceDetector.infer(img);

                        tm.stop();
                        Debug.Log("FaceDetectorYNModel Inference time, ms: " + tm.getTimeMilli());

                        List<Mat> expressions = new List<Mat>();

                        // Estimate the expression of each face
                        for (int i = 0; i < faces.rows(); ++i)
                        {
                            tm.reset();
                            tm.start();

                            // Facial expression recognizer inference
                            Mat facialExpression = facialExpressionRecognizer.infer(img, faces.row(i));

                            tm.stop();
                            Debug.Log("FacialExpressionRecognizer Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                            if (!facialExpression.empty())
                                expressions.Add(facialExpression);
                        }
                        faceDetector.visualize(img, faces, true, false);
                        facialExpressionRecognizer.visualize(img, expressions, faces, true, false);
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

                if (faceDetector == null || facialExpressionRecognizer == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    Imgproc.cvtColor(rgbaMat, bgrMat, Imgproc.COLOR_RGBA2BGR);

                    //TickMeter tm = new TickMeter();
                    //tm.start();

                    Mat faces = faceDetector.infer(bgrMat);

                    //tm.stop();
                    //Debug.Log("FaceDetectorYNModel Inference time, ms: " + tm.getTimeMilli());

                    List<Mat> expressions = new List<Mat>();

                    // Estimate the expression of each face
                    for (int i = 0; i < faces.rows(); ++i)
                    {
                        //tm.reset();
                        //tm.start();

                        // Facial expression recognizer inference
                        Mat facialExpression = facialExpressionRecognizer.infer(bgrMat, faces.row(i));

                        //tm.stop();
                        //Debug.Log("FacialExpressionRecognizer Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                        if (!facialExpression.empty())
                            expressions.Add(facialExpression);
                    }

                    Imgproc.cvtColor(bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                    //faceDetector.visualize(rgbaMat, faces, false, true);
                    facialExpressionRecognizer.visualize(rgbaMat, expressions, faces, false, true);
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

            if (faceDetector != null)
                //faceDetector.Dispose();
                faceDetector.dispose();

            if (facialExpressionRecognizer != null)
                facialExpressionRecognizer.dispose();

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

        private class FaceDetectorYNModel
        {
            Size input_size;
            float conf_threshold;
            float nms_threshold;
            int topK;
            int backend;
            int target;

            protected Scalar bBoxColor = new Scalar(0, 255, 0, 255);

            protected Scalar[] keyPointsColors = new Scalar[] {
            new Scalar(0, 0, 255, 255), // # right eye
            new Scalar(255, 0, 0, 255), // # left eye
            new Scalar(255, 255, 0, 255), // # nose tip
            new Scalar(0, 255, 255, 255), // # mouth right
            new Scalar(0, 255, 0, 255), // # mouth left
            new Scalar(255, 255, 255, 255) };

            FaceDetectorYN detection_model;

            Mat input_sizeMat;

            public FaceDetectorYNModel(string modelFilepath, string configFilepath, Size inputSize, float confThreshold = 0.6f, float nmsThreshold = 0.3f, int topK = 5000, int backend = Dnn.DNN_BACKEND_OPENCV, int target = Dnn.DNN_TARGET_CPU)
            {
                // initialize
                if (!string.IsNullOrEmpty(modelFilepath))
                {
                    detection_model = FaceDetectorYN.create(modelFilepath, configFilepath, inputSize, confThreshold, nmsThreshold, topK, backend, target);
                }

                input_size = new Size(inputSize.width > 0 ? inputSize.width : 320, inputSize.height > 0 ? inputSize.height : 320);
                conf_threshold = Mathf.Clamp01(confThreshold);
                nms_threshold = Mathf.Clamp01(nmsThreshold);
                this.topK = topK;
                this.backend = backend;
                this.target = target;
            }

            protected virtual Mat preprocess(Mat image)
            {
                int h = (int)input_size.height;
                int w = (int)input_size.width;

                if (input_sizeMat == null)
                    input_sizeMat = new Mat(new Size(w, h), CvType.CV_8UC3);// [h, w]

                Imgproc.resize(image, input_sizeMat, new Size(w, h));

                return input_sizeMat;// [h, w, 3]
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
                Mat results = new Mat();
                detection_model.detect(input_blob, results);

                // Postprocess
                // scale_boxes
                float x_factor = image.width() / (float)input_size.width;
                float y_factor = image.height() / (float)input_size.height;

                for (int i = 0; i < results.rows(); ++i)
                {
                    float[] results_arr = new float[14];
                    results.get(i, 0, results_arr);
                    for (int j = 0; j < 14; ++j)
                    {
                        if (j % 2 == 0)
                        {
                            results_arr[j] = results_arr[j] * x_factor;
                        }
                        else
                        {
                            results_arr[j] = results_arr[j] * y_factor;
                        }
                    }

                    results.put(i, 0, results_arr);
                }

                return results;
            }

            protected virtual Mat postprocess(Mat output_blob)
            {
                return output_blob;
            }

            public virtual void visualize(Mat image, Mat results, bool print_results = false, bool isRGB = false)
            {
                if (image.IsDisposed)
                    return;

                if (results.empty() || results.cols() < 15)
                    return;

                for (int i = results.rows() - 1; i >= 0; --i)
                {
                    float[] box = new float[4];
                    results.get(i, 0, box);
                    float[] conf = new float[1];
                    results.get(i, 14, conf);
                    float[] landmarks = new float[10];
                    results.get(i, 4, landmarks);

                    float left = box[0];
                    float top = box[1];
                    float right = box[0] + box[2];
                    float bottom = box[1] + box[3];

                    Scalar bbc = bBoxColor;
                    Scalar bbcolor = isRGB ? bbc : new Scalar(bbc.val[2], bbc.val[1], bbc.val[0], bbc.val[3]);

                    Imgproc.rectangle(image, new Point(left, top), new Point(right, bottom), bbcolor, 2);

                    string label = String.Format("{0:0.0000}", conf[0]);
                    int[] baseLine = new int[1];
                    Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

                    top = Mathf.Max((float)top, (float)labelSize.height);
                    Imgproc.rectangle(image, new Point(left, top - labelSize.height),
                        new Point(left + labelSize.width, top + baseLine[0]), bbcolor, Core.FILLED);
                    Imgproc.putText(image, label, new Point(left, top), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(0, 0, 0, 255), 1, Imgproc.LINE_AA);

                    // draw landmark points
                    for (int j = 0; j < 10; j += 2)
                    {
                        Scalar c = keyPointsColors[(j / 2) % keyPointsColors.Length];
                        Scalar color = isRGB ? c : new Scalar(c.val[2], c.val[1], c.val[0], c.val[3]);

                        Imgproc.circle(image, new Point(landmarks[j], landmarks[j + 1]), 2, color, 2);
                    }
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
                        results.get(i, 14, conf);
                        float[] landmarks = new float[10];
                        results.get(i, 4, landmarks);

                        sb.AppendLine(String.Format("-----------face {0}-----------", i + 1));
                        sb.AppendLine(String.Format("conf: {0:0.0000}", conf[0]));
                        sb.AppendLine(String.Format("box: {0:0} {1:0} {2:0} {3:0}", box[0], box[1], box[2], box[3]));
                        sb.Append("landmarks: ");
                        foreach (var p in landmarks)
                        {
                            sb.Append(String.Format("{0:0} ", p));
                        }
                        sb.AppendLine();
                    }

                    Debug.Log(sb);
                }
            }

            public virtual void dispose()
            {
                if (detection_model != null)
                    detection_model.Dispose();

                if (input_sizeMat != null)
                    input_sizeMat.Dispose();

                input_sizeMat = null;
            }
        }

        private class FacialExpressionRecognizer
        {
            int backend;
            int target;

            string inputName = "data";
            string outputName = "label";
            Size input_size = new Size(112, 112);
            Scalar mean = new Scalar(0.5, 0.5, 0.5);
            Scalar std = new Scalar(0.5, 0.5, 0.5);

            Net facial_expression_recognition_net;

            List<string> classNames;

            List<Scalar> palette;

            Mat input_sizeMat;

            Mat getDataMat;

            FaceRecognizerSF faceRecognizer;

            public FacialExpressionRecognizer(string modelFilepath, string SF_modelFilepath, string SF_configFilepath, int backend = Dnn.DNN_BACKEND_OPENCV, int target = Dnn.DNN_TARGET_CPU)
            {
                // initialize
                if (!string.IsNullOrEmpty(modelFilepath))
                {
                    facial_expression_recognition_net = Dnn.readNet(modelFilepath);
                }

                if (!string.IsNullOrEmpty(SF_modelFilepath))
                {
                    faceRecognizer = FaceRecognizerSF.create(SF_modelFilepath, SF_configFilepath, backend, target);
                }

                this.backend = backend;
                this.target = target;

                facial_expression_recognition_net.setPreferableBackend(this.backend);
                facial_expression_recognition_net.setPreferableTarget(this.target);

                classNames = new List<string>();
                classNames.Add("angry");
                classNames.Add("disgust");
                classNames.Add("fearful");
                classNames.Add("happy");
                classNames.Add("neutral");
                classNames.Add("sad");
                classNames.Add("surprised");

                palette = new List<Scalar>();
                palette.Add(new Scalar(255, 56, 56, 255));
                palette.Add(new Scalar(82, 0, 133, 255));
                palette.Add(new Scalar(52, 69, 147, 255));
                palette.Add(new Scalar(255, 178, 29, 255));
                palette.Add(new Scalar(55, 55, 55, 255));
                palette.Add(new Scalar(100, 115, 255, 255));
                palette.Add(new Scalar(255, 112, 31, 255));
            }

            protected virtual Mat preprocess(Mat image, Mat bbox = null)
            {
                if (input_sizeMat == null)
                    input_sizeMat = new Mat(input_size, CvType.CV_8UC3);

                if (bbox != null && faceRecognizer != null)
                {
                    alignCrop(image, bbox, input_sizeMat);
                }
                else
                {
                    Imgproc.resize(image, input_sizeMat, input_size);
                }

                // Create a 4D blob from a frame.
                Mat blob;

                blob = Dnn.blobFromImage(input_sizeMat, 1.0 / 255.0, input_sizeMat.size(), Scalar.all(0), true, false, CvType.CV_32F); // HWC to NCHW, BGR to RGB

                int c = input_sizeMat.channels();
                int h = input_sizeMat.height();
                int w = input_sizeMat.width();

                Mat blob_cxhxw = blob.reshape(1, new int[] { c, h, w });// [c, h, w]

                for (int i = 0; i < c; ++i)
                {
                    Mat blob_1xhw = blob_cxhxw.row(i).reshape(1, 1);// [1, h, w] => [1, h * w]

                    // Subtract blob by mean.
                    Core.subtract(blob_1xhw, new Scalar(mean.val[i]), blob_1xhw);
                    // Divide blob by std.
                    Core.divide(blob_1xhw, new Scalar(std.val[i]), blob_1xhw);
                }

                return blob;// [1, 112, 112, 3]
            }

            public virtual Mat infer(Mat image, Mat bbox = null)
            {
                // cheack
                if (image.channels() != 3)
                {
                    Debug.Log("The input image must be in BGR format.");
                    return new Mat();
                }

                // Preprocess
                Mat input_blob = preprocess(image, bbox);

                // Forward
                facial_expression_recognition_net.setInput(input_blob, inputName);

                Mat output_blob = facial_expression_recognition_net.forward(outputName);

                // Postprocess
                Mat results = postprocess(output_blob);

                input_blob.Dispose();

                return results;
            }

            protected virtual Mat postprocess(Mat output_blob)
            {
                Mat results = softmax(output_blob);

                return results;// [1, 7]
            }

            protected virtual Mat softmax(Mat src)
            {
                Mat dst = src.clone();

                Core.MinMaxLocResult result = Core.minMaxLoc(src);
                Scalar max = new Scalar(result.maxVal);
                Core.subtract(src, max, dst);
                Core.exp(dst, dst);
                Scalar sum = Core.sumElems(dst);
                Core.divide(dst, sum, dst);

                return dst;
            }

            public virtual void visualize(Mat image, List<Mat> results, Mat faces, bool print_results = false, bool isRGB = false)
            {
                if (image.IsDisposed)
                    return;

                if (results.Count != faces.rows())
                    return;

                StringBuilder sb = null;

                if (print_results)
                    sb = new StringBuilder();

                for (int i = 0; i < results.Count; ++i)
                {
                    float[] face_box = new float[4];
                    faces.get(i, 0, face_box);

                    float left = face_box[0] + 2;
                    float top = face_box[1] + 2;
                    float right = face_box[0] + face_box[2] - 2;
                    float bottom = face_box[1] + face_box[3] - 2;

                    ClassificationData bmData = getBestMatchData(results[i]);
                    int classId = (int)bmData.cls;
                    string label = getClassLabel(bmData.cls) + ", " + String.Format("{0:0.0000}", bmData.conf);

                    Scalar c = palette[classId % palette.Count];
                    Scalar color = isRGB ? c : new Scalar(c.val[2], c.val[1], c.val[0], c.val[3]);

                    // draw box
                    Imgproc.rectangle(image, new Point(left, top), new Point(right, bottom), color, 2);

                    // draw label
                    int[] baseLine = new int[1];
                    Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

                    top = Mathf.Max((float)top, (float)labelSize.height);
                    Imgproc.rectangle(image, new Point(left, top + 2),
                        new Point(left + labelSize.width, top + labelSize.height + baseLine[0] + 2), color, Core.FILLED);
                    Imgproc.putText(image, label, new Point(left, top + labelSize.height + 2), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, Scalar.all(255), 1, Imgproc.LINE_AA);

                    // Print results
                    if (print_results)
                    {
                        sb.AppendLine(String.Format("-----------expression {0}-----------", i + 1));
                        sb.AppendLine(String.Format("Best match: " + getClassLabel(bmData.cls) + ", " + bmData));
                    }
                }

                if (print_results)
                    Debug.Log(sb);
            }

            public virtual void dispose()
            {
                if (facial_expression_recognition_net != null)
                    facial_expression_recognition_net.Dispose();

                if (input_sizeMat != null)
                    input_sizeMat.Dispose();

                input_sizeMat = null;

                if (getDataMat != null)
                    getDataMat.Dispose();

                getDataMat = null;

                if (faceRecognizer != null)
                    faceRecognizer.Dispose();
            }

            private void alignCrop(Mat src_img, Mat face_box, Mat aligned_img)
            {
                // The alignCrop method of FaceRecognizerSF is used here, because the implementation of the alignment and crop process is cumbersome.
                // This method returns an image of 112x112 pixels, the same as the Facial Expression Recognition model input.
                faceRecognizer.alignCrop(src_img, face_box, aligned_img);
            }

            [StructLayout(LayoutKind.Sequential)]
            public readonly struct ClassificationData
            {
                public readonly float cls;
                public readonly float conf;

                // sizeof(ClassificationData)
                public const int Size = 2 * sizeof(float);

                public ClassificationData(int cls, float conf)
                {
                    this.cls = cls;
                    this.conf = conf;
                }

                public override string ToString()
                {
                    return "cls:" + cls + " conf:" + conf;
                }
            };

            public virtual ClassificationData[] getData(Mat results)
            {
                if (results.empty())
                    return new ClassificationData[0];

                int num = results.cols();

                if (getDataMat == null)
                {
                    getDataMat = new Mat(num, 2, CvType.CV_32FC1);
                    float[] arange = Enumerable.Range(0, num).Select(i => (float)i).ToArray();
                    getDataMat.col(0).put(0, 0, arange);
                }

                Mat results_numx1 = results.reshape(1, num);
                results_numx1.copyTo(getDataMat.col(1));

                var dst = new ClassificationData[num];
                OpenCVForUnity.UtilsModule.MatUtils.copyFromMat(getDataMat, dst);

                return dst;
            }

            public virtual ClassificationData[] getSortedData(Mat results, int topK = 5)
            {
                if (results.empty())
                    return new ClassificationData[0];

                int num = results.cols();

                if (topK < 1 || topK > num) topK = num;
                var sortedData = getData(results).OrderByDescending(x => x.conf).Take(topK).ToArray();

                return sortedData;
            }

            public virtual ClassificationData getBestMatchData(Mat results)
            {
                if (results.empty())
                    return new ClassificationData();

                Core.MinMaxLocResult minmax = Core.minMaxLoc(results);

                return new ClassificationData((int)minmax.maxLoc.x, (float)minmax.maxVal);
            }

            public virtual string getClassLabel(float id)
            {
                int classId = (int)id;
                string className = string.Empty;
                if (classNames != null && classNames.Count != 0)
                {
                    if (classId >= 0 && classId < (int)classNames.Count)
                    {
                        className = classNames[classId];
                    }
                }
                if (string.IsNullOrEmpty(className))
                    className = classId.ToString();

                return className;
            }
        }
    }
}
#endif

#endif