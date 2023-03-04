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
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVRect = OpenCVForUnity.CoreModule.Rect;
using OpenCVRange = OpenCVForUnity.CoreModule.Range;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Hand Pose Estimation Example
    /// An example of using OpenCV dnn module with Hand Pose Estimation.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/master/models/handpose_estimation_mediapipe.
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class HandPoseEstimationExample : MonoBehaviour
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
        /// The palm detector.
        /// </summary>
        PalmDetector palmDetector;

        /// <summary>
        /// The handpose estimator.
        /// </summary>
        HandPoseEstimator handPoseEstimator;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// PALM_DETECTION_MODEL_FILENAME
        /// </summary>
        protected static readonly string PALM_DETECTION_MODEL_FILENAME = "OpenCVForUnity/dnn/palm_detection_mediapipe_2023feb.onnx";

        /// <summary>
        /// The palm detection model filepath.
        /// </summary>
        string palm_detection_model_filepath;

        /// <summary>
        /// HANDPOSE_ESTIMATION_MODEL_FILENAME
        /// </summary>
        protected static readonly string HANDPOSE_ESTIMATION_MODEL_FILENAME = "OpenCVForUnity/dnn/handpose_estimation_mediapipe_2023feb.onnx";

        /// <summary>
        /// The handpose estimation model filepath.
        /// </summary>
        string handpose_estimation_model_filepath;



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
            palm_detection_model_filepath = Utils.getFilePath(PALM_DETECTION_MODEL_FILENAME);
            handpose_estimation_model_filepath = Utils.getFilePath(HANDPOSE_ESTIMATION_MODEL_FILENAME);
            Run();
#endif
        }

#if UNITY_WEBGL
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync(PALM_DETECTION_MODEL_FILENAME, (result) =>
            {
                palm_detection_model_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync(HANDPOSE_ESTIMATION_MODEL_FILENAME, (result) =>
            {
                handpose_estimation_model_filepath = result;
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


            if (string.IsNullOrEmpty(palm_detection_model_filepath))
            {
                Debug.LogError(PALM_DETECTION_MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                palmDetector = new PalmDetector(palm_detection_model_filepath, 0.3f, 0.6f);
            }

            if (string.IsNullOrEmpty(handpose_estimation_model_filepath))
            {
                Debug.LogError(HANDPOSE_ESTIMATION_MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/dnn/setup_dnn_module.pdf” to make the necessary setup.");
            }
            else
            {
                handPoseEstimator = new HandPoseEstimator(handpose_estimation_model_filepath, 0.9f);
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

                        Mat palms = palmDetector.infer(img);

                        tm.stop();
                        Debug.Log("PalmDetector Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                        List<Mat> hands = new List<Mat>();

                        // Estimate the pose of each hand
                        for (int i = 0; i < palms.rows(); ++i)
                        {
                            tm.reset();
                            tm.start();

                            // Handpose estimator inference
                            Mat hodpose = handPoseEstimator.infer(img, palms.row(i));

                            tm.stop();
                            Debug.Log("HandPoseEstimator Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                            if (!hodpose.empty())
                                hands.Add(hodpose);
                        }
                        //palmDetector.visualize(img, palms, true, false);
                        handPoseEstimator.visualize(img, hands, true, false);
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

                if (palmDetector == null || handPoseEstimator == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    Imgproc.cvtColor(rgbaMat, bgrMat, Imgproc.COLOR_RGBA2BGR);

                    //TickMeter tm = new TickMeter();
                    //tm.start();

                    Mat palms = palmDetector.infer(bgrMat);

                    //tm.stop();
                    //Debug.Log("PalmDetector Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                    List<Mat> hands = new List<Mat>();

                    // Estimate the pose of each hand
                    for (int i = 0; i < palms.rows(); ++i)
                    {
                        //tm.reset();
                        //tm.start();

                        // Handpose estimator inference
                        Mat hodpose = handPoseEstimator.infer(bgrMat, palms.row(i));

                        //tm.stop();
                        //Debug.Log("HandPoseEstimator Inference time (preprocess + infer + postprocess), ms: " + tm.getTimeMilli());

                        if (!hodpose.empty())
                            hands.Add(hodpose);
                    }

                    Imgproc.cvtColor(bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                    //palmDetector.visualize(rgbaMat, palms, false, true);
                    handPoseEstimator.visualize(rgbaMat, hands, false, true);
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

            if (palmDetector != null)
                palmDetector.dispose();

            if (handPoseEstimator != null)
                handPoseEstimator.dispose();

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

        private class PalmDetector
        {
            float nms_threshold;
            float score_threshold;
            int topK;
            int backend;
            int target;

            Size input_size = new Size(192, 192);

            Net palm_detection_net;
            Mat anchors;

            Mat input_sizeMat;
            Mat maxSizeImg;

            Mat boxesMat;
            Mat boxes_m_c4;
            Mat confidences_m;
            MatOfRect2d boxes;
            MatOfFloat confidences;

            public PalmDetector(string modelFilepath, float nmsThreshold = 0.3f, float scoreThreshold = 0.5f, int topK = 5000, int backend = Dnn.DNN_BACKEND_OPENCV, int target = Dnn.DNN_TARGET_CPU)
            {
                // initialize
                if (!string.IsNullOrEmpty(modelFilepath))
                {
                    palm_detection_net = Dnn.readNet(modelFilepath);
                }

                nms_threshold = Mathf.Clamp01(nmsThreshold);
                score_threshold = Mathf.Clamp01(scoreThreshold);
                this.topK = topK;
                this.backend = backend;
                this.target = target;

                palm_detection_net.setPreferableBackend(this.backend);
                palm_detection_net.setPreferableTarget(this.target);

                anchors = load_anchors();
            }

            protected virtual Mat preprocess(Mat image)
            {
                // Add padding to make it square.
                int max = Mathf.Max(image.cols(), image.rows());

                if (maxSizeImg == null)
                    maxSizeImg = new Mat(max, max, image.type(), Scalar.all(0));
                if (maxSizeImg.width() != max || maxSizeImg.height() != max)
                {
                    maxSizeImg.create(max, max, image.type());
                    Imgproc.rectangle(maxSizeImg, new OpenCVRect(0, 0, maxSizeImg.width(), maxSizeImg.height()), Scalar.all(0), -1);
                }

                Mat _maxSizeImg_roi = new Mat(maxSizeImg, new OpenCVRect((max - image.cols()) / 2, (max - image.rows()) / 2, image.cols(), image.rows()));
                image.copyTo(_maxSizeImg_roi);

                // Create a 4D blob from a frame.
                int c = image.channels();
                int h = (int)input_size.height;
                int w = (int)input_size.width;

                // HWC to NHWC, BGR to RGB
                Mat blob = new Mat(new int[] { 1, h, w, c }, CvType.CV_32FC1);
                Mat blob_hxw = blob.reshape(c, new int[] { h, w });// [h, w]

                if (input_sizeMat == null)
                    input_sizeMat = new Mat(new Size(w, h), CvType.CV_8UC3);// [h, w]

                Imgproc.resize(maxSizeImg, input_sizeMat, new Size(w, h));
                Imgproc.cvtColor(input_sizeMat, input_sizeMat, Imgproc.COLOR_BGR2RGB);

                input_sizeMat.convertTo(blob_hxw, CvType.CV_32F, 1.0 / 255.0);

                return blob;// [1, 192, 192, 3]
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
                palm_detection_net.setInput(input_blob);

                List<Mat> output_blob = new List<Mat>();
                palm_detection_net.forward(output_blob, palm_detection_net.getUnconnectedOutLayersNames());

                // Postprocess
                Mat results = postprocess(output_blob);

                // scale_boxes
                float maxSize = Mathf.Max((float)image.size().width, (float)image.size().height);
                float x_factor = maxSize;
                float y_factor = maxSize;
                float x_shift = (maxSize - (float)image.size().width) / 2f;
                float y_shift = (maxSize - (float)image.size().height) / 2f;

                for (int i = 0; i < results.rows(); ++i)
                {
                    float[] results_arr = new float[4];
                    results.get(i, 0, results_arr);
                    float x1 = Mathf.Floor(results_arr[0] * x_factor - x_shift);
                    float y1 = Mathf.Floor(results_arr[1] * y_factor - y_shift);
                    float x2 = Mathf.Floor(results_arr[2] * x_factor - x_shift);
                    float y2 = Mathf.Floor(results_arr[3] * y_factor - y_shift);

                    results.put(i, 0, new float[] { x1, y1, x2, y2 });

                    float[] landmarks_arr = new float[14];
                    results.get(i, 4, landmarks_arr);
                    for (int j = 0; j < 14; ++j)
                    {
                        if (j % 2 == 0)
                        {
                            landmarks_arr[j] = Mathf.Floor(landmarks_arr[j] * x_factor - x_shift);
                        }
                        else
                        {
                            landmarks_arr[j] = Mathf.Floor(landmarks_arr[j] * y_factor - y_shift);
                        }
                    }

                    results.put(i, 4, landmarks_arr);
                }

                input_blob.Dispose();
                for (int i = 0; i < output_blob.Count; i++)
                {
                    output_blob[i].Dispose();
                }

                return results;
            }

            protected virtual Mat postprocess(List<Mat> output_blob)
            {
                int num = output_blob[0].size(1);
                Mat output_blob_1_numx1 = output_blob[1].reshape(1, num);
                Mat score = output_blob_1_numx1.colRange(new OpenCVRange(0, 1));

                Mat output_blob_0_numx18 = output_blob[0].reshape(1, num);
                Mat box_delta = output_blob_0_numx18.colRange(new OpenCVRange(0, 4));
                Mat landmark_delta = output_blob_0_numx18.colRange(new OpenCVRange(4, 18));

                // get scores
                Core.multiply(score, Scalar.all(1.0), score, -1.0);
                Core.exp(score, score);
                Core.add(score, Scalar.all(1.0), score);
                Core.divide(1.0, score, score);

                // get boxes
                Mat cxy_delta = box_delta.colRange(new OpenCVRange(0, 2));
                Mat _cxy_delta_numx1_c2 = cxy_delta.reshape(2, cxy_delta.rows());
                Core.divide(_cxy_delta_numx1_c2, new Scalar(input_size.width, input_size.height), _cxy_delta_numx1_c2);
                Mat wh_delta = box_delta.colRange(new OpenCVRange(2, 4));
                Mat _wh_delta_numx1_c2 = wh_delta.reshape(2, wh_delta.rows());
                Core.divide(_wh_delta_numx1_c2, new Scalar(input_size.width, input_size.height), _wh_delta_numx1_c2);

                if (boxesMat == null)
                    boxesMat = new Mat(num, 4, CvType.CV_32FC1);

                Mat xy1 = boxesMat.colRange(new OpenCVRange(0, 2));
                Mat xy2 = boxesMat.colRange(new OpenCVRange(2, 4));

                Core.divide(wh_delta, new Scalar(2.0), wh_delta);

                Core.subtract(cxy_delta, wh_delta, xy1);
                Core.add(xy1, anchors, xy1);

                Core.add(cxy_delta, wh_delta, xy2);
                Core.add(xy2, anchors, xy2);

                // NMS
                if (boxes_m_c4 == null)
                    boxes_m_c4 = new Mat(num, 1, CvType.CV_64FC4);
                if (confidences_m == null)
                    confidences_m = new Mat(num, 1, CvType.CV_32FC1);

                if (boxes == null)
                    boxes = new MatOfRect2d(boxes_m_c4);
                if (confidences == null)
                    confidences = new MatOfFloat(confidences_m);

                Mat boxes_m_c1 = boxes_m_c4.reshape(1, num);
                boxesMat.convertTo(boxes_m_c1, CvType.CV_64F);
                score.copyTo(confidences_m);
                MatOfInt indices = new MatOfInt();
                Dnn.NMSBoxes(boxes, confidences, score_threshold, nms_threshold, indices, 1f, topK);

                // get landmarks
                Mat results = new Mat(indices.rows(), 19, CvType.CV_32FC1);

                for (int i = 0; i < indices.total(); ++i)
                {
                    int idx = (int)indices.get(i, 0)[0];

                    float[] bbox_arr = new float[4];
                    boxesMat.get(idx, 0, bbox_arr);
                    results.put(i, 0, bbox_arr);

                    float[] confidence_arr = new float[1];
                    confidences.get(idx, 0, confidence_arr);
                    results.put(i, 18, confidence_arr);

                    float[] landmarks_arr = new float[14];
                    landmark_delta.get(idx, 0, landmarks_arr);
                    float[] anchors_arr = new float[2];
                    anchors.get(idx, 0, anchors_arr);

                    for (int j = 0; j < 14; ++j)
                    {
                        if (j % 2 == 0)
                        {
                            landmarks_arr[j] /= (float)input_size.width;
                            landmarks_arr[j] += anchors_arr[0];
                        }
                        else
                        {
                            landmarks_arr[j] /= (float)input_size.height;
                            landmarks_arr[j] += anchors_arr[1];
                        }
                    }

                    results.put(i, 4, landmarks_arr);
                }

                indices.Dispose();

                // [
                //   [bbox_coords, landmarks_coords, score]
                //   ...
                //   [bbox_coords, landmarks_coords, score]
                // ]
                return results;
            }

            public virtual void visualize(Mat image, Mat results, bool print_results = false, bool isRGB = false)
            {
                if (image.IsDisposed)
                    return;

                if (results.empty() || results.cols() < 19)
                    return;

                StringBuilder sb = null;

                if (print_results)
                    sb = new StringBuilder();

                for (int i = 0; i < results.rows(); ++i)
                {
                    float[] score = new float[1];
                    results.get(i, 18, score);
                    float[] palm_box = new float[4];
                    results.get(i, 0, palm_box);
                    float[] palm_landmarks = new float[14];
                    results.get(i, 4, palm_landmarks);

                    // put score
                    Imgproc.putText(image, String.Format("{0:0.0000}", score[0]), new Point(palm_box[0], palm_box[1] + 12), Imgproc.FONT_HERSHEY_DUPLEX, 0.5, new Scalar(0, 255, 0, 255));

                    // draw box
                    Imgproc.rectangle(image, new Point(palm_box[0], palm_box[1]), new Point(palm_box[2], palm_box[3]), new Scalar(0, 255, 0, 255), 2);

                    // draw points
                    for (int j = 0; j < 14; j += 2)
                    {
                        Imgproc.circle(image, new Point(palm_landmarks[j], palm_landmarks[j + 1]), 2, (isRGB) ? new Scalar(255, 0, 0, 255) : new Scalar(0, 0, 255, 255), 2);
                    }

                    // Print results
                    if (print_results)
                    {
                        sb.AppendLine(String.Format("-----------palm {0}-----------", i + 1));
                        sb.AppendLine(String.Format("score: {0:0.00}", score[0]));
                        sb.AppendLine(String.Format("palm box: {0:0} {1:0} {2:0} {3:0}", palm_box[0], palm_box[1], palm_box[2], palm_box[3]));
                        sb.Append("palm landmarks: ");
                        foreach (var p in palm_landmarks)
                        {
                            sb.Append(String.Format("{0:0} ", p));
                        }
                        sb.AppendLine();
                    }
                }

                if (print_results)
                    Debug.Log(sb);
            }

            public virtual void dispose()
            {
                if (palm_detection_net != null)
                    palm_detection_net.Dispose();

                if (input_sizeMat != null)
                    input_sizeMat.Dispose();

                input_sizeMat = null;

                if (maxSizeImg != null)
                    maxSizeImg.Dispose();

                maxSizeImg = null;

                if (boxesMat != null)
                    boxesMat.Dispose();

                boxesMat = null;

                if (boxes_m_c4 != null)
                    boxes_m_c4.Dispose();
                if (confidences_m != null)
                    confidences_m.Dispose();
                if (boxes != null)
                    boxes.Dispose();
                if (confidences != null)
                    confidences.Dispose();

                boxes_m_c4 = null;
                confidences_m = null;
                boxes = null;
                confidences = null;
            }

            protected virtual Mat load_anchors()
            {
                Mat anchors = new Mat(2016, 2, CvType.CV_32FC1);

                float[] anchors_arr = new float[] {
                  0.02083333f, 0.02083333f,
                      0.02083333f, 0.02083333f,
                      0.0625f, 0.02083333f,
                      0.0625f, 0.02083333f,
                      0.10416666f, 0.02083333f,
                      0.10416666f, 0.02083333f,
                      0.14583333f, 0.02083333f,
                      0.14583333f, 0.02083333f,
                      0.1875f, 0.02083333f,
                      0.1875f, 0.02083333f,
                      0.22916667f, 0.02083333f,
                      0.22916667f, 0.02083333f,
                      0.27083334f, 0.02083333f,
                      0.27083334f, 0.02083333f,
                      0.3125f, 0.02083333f,
                      0.3125f, 0.02083333f,
                      0.35416666f, 0.02083333f,
                      0.35416666f, 0.02083333f,
                      0.39583334f, 0.02083333f,
                      0.39583334f, 0.02083333f,
                      0.4375f, 0.02083333f,
                      0.4375f, 0.02083333f,
                      0.47916666f, 0.02083333f,
                      0.47916666f, 0.02083333f,
                      0.5208333f, 0.02083333f,
                      0.5208333f, 0.02083333f,
                      0.5625f, 0.02083333f,
                      0.5625f, 0.02083333f,
                      0.6041667f, 0.02083333f,
                      0.6041667f, 0.02083333f,
                      0.6458333f, 0.02083333f,
                      0.6458333f, 0.02083333f,
                      0.6875f, 0.02083333f,
                      0.6875f, 0.02083333f,
                      0.7291667f, 0.02083333f,
                      0.7291667f, 0.02083333f,
                      0.7708333f, 0.02083333f,
                      0.7708333f, 0.02083333f,
                      0.8125f, 0.02083333f,
                      0.8125f, 0.02083333f,
                      0.8541667f, 0.02083333f,
                      0.8541667f, 0.02083333f,
                      0.8958333f, 0.02083333f,
                      0.8958333f, 0.02083333f,
                      0.9375f, 0.02083333f,
                      0.9375f, 0.02083333f,
                      0.9791667f, 0.02083333f,
                      0.9791667f, 0.02083333f,
                      0.02083333f, 0.0625f,
                      0.02083333f, 0.0625f,
                      0.0625f, 0.0625f,
                      0.0625f, 0.0625f,
                      0.10416666f, 0.0625f,
                      0.10416666f, 0.0625f,
                      0.14583333f, 0.0625f,
                      0.14583333f, 0.0625f,
                      0.1875f, 0.0625f,
                      0.1875f, 0.0625f,
                      0.22916667f, 0.0625f,
                      0.22916667f, 0.0625f,
                      0.27083334f, 0.0625f,
                      0.27083334f, 0.0625f,
                      0.3125f, 0.0625f,
                      0.3125f, 0.0625f,
                      0.35416666f, 0.0625f,
                      0.35416666f, 0.0625f,
                      0.39583334f, 0.0625f,
                      0.39583334f, 0.0625f,
                      0.4375f, 0.0625f,
                      0.4375f, 0.0625f,
                      0.47916666f, 0.0625f,
                      0.47916666f, 0.0625f,
                      0.5208333f, 0.0625f,
                      0.5208333f, 0.0625f,
                      0.5625f, 0.0625f,
                      0.5625f, 0.0625f,
                      0.6041667f, 0.0625f,
                      0.6041667f, 0.0625f,
                      0.6458333f, 0.0625f,
                      0.6458333f, 0.0625f,
                      0.6875f, 0.0625f,
                      0.6875f, 0.0625f,
                      0.7291667f, 0.0625f,
                      0.7291667f, 0.0625f,
                      0.7708333f, 0.0625f,
                      0.7708333f, 0.0625f,
                      0.8125f, 0.0625f,
                      0.8125f, 0.0625f,
                      0.8541667f, 0.0625f,
                      0.8541667f, 0.0625f,
                      0.8958333f, 0.0625f,
                      0.8958333f, 0.0625f,
                      0.9375f, 0.0625f,
                      0.9375f, 0.0625f,
                      0.9791667f, 0.0625f,
                      0.9791667f, 0.0625f,
                      0.02083333f, 0.10416666f,
                      0.02083333f, 0.10416666f,
                      0.0625f, 0.10416666f,
                      0.0625f, 0.10416666f,
                      0.10416666f, 0.10416666f,
                      0.10416666f, 0.10416666f,
                      0.14583333f, 0.10416666f,
                      0.14583333f, 0.10416666f,
                      0.1875f, 0.10416666f,
                      0.1875f, 0.10416666f,
                      0.22916667f, 0.10416666f,
                      0.22916667f, 0.10416666f,
                      0.27083334f, 0.10416666f,
                      0.27083334f, 0.10416666f,
                      0.3125f, 0.10416666f,
                      0.3125f, 0.10416666f,
                      0.35416666f, 0.10416666f,
                      0.35416666f, 0.10416666f,
                      0.39583334f, 0.10416666f,
                      0.39583334f, 0.10416666f,
                      0.4375f, 0.10416666f,
                      0.4375f, 0.10416666f,
                      0.47916666f, 0.10416666f,
                      0.47916666f, 0.10416666f,
                      0.5208333f, 0.10416666f,
                      0.5208333f, 0.10416666f,
                      0.5625f, 0.10416666f,
                      0.5625f, 0.10416666f,
                      0.6041667f, 0.10416666f,
                      0.6041667f, 0.10416666f,
                      0.6458333f, 0.10416666f,
                      0.6458333f, 0.10416666f,
                      0.6875f, 0.10416666f,
                      0.6875f, 0.10416666f,
                      0.7291667f, 0.10416666f,
                      0.7291667f, 0.10416666f,
                      0.7708333f, 0.10416666f,
                      0.7708333f, 0.10416666f,
                      0.8125f, 0.10416666f,
                      0.8125f, 0.10416666f,
                      0.8541667f, 0.10416666f,
                      0.8541667f, 0.10416666f,
                      0.8958333f, 0.10416666f,
                      0.8958333f, 0.10416666f,
                      0.9375f, 0.10416666f,
                      0.9375f, 0.10416666f,
                      0.9791667f, 0.10416666f,
                      0.9791667f, 0.10416666f,
                      0.02083333f, 0.14583333f,
                      0.02083333f, 0.14583333f,
                      0.0625f, 0.14583333f,
                      0.0625f, 0.14583333f,
                      0.10416666f, 0.14583333f,
                      0.10416666f, 0.14583333f,
                      0.14583333f, 0.14583333f,
                      0.14583333f, 0.14583333f,
                      0.1875f, 0.14583333f,
                      0.1875f, 0.14583333f,
                      0.22916667f, 0.14583333f,
                      0.22916667f, 0.14583333f,
                      0.27083334f, 0.14583333f,
                      0.27083334f, 0.14583333f,
                      0.3125f, 0.14583333f,
                      0.3125f, 0.14583333f,
                      0.35416666f, 0.14583333f,
                      0.35416666f, 0.14583333f,
                      0.39583334f, 0.14583333f,
                      0.39583334f, 0.14583333f,
                      0.4375f, 0.14583333f,
                      0.4375f, 0.14583333f,
                      0.47916666f, 0.14583333f,
                      0.47916666f, 0.14583333f,
                      0.5208333f, 0.14583333f,
                      0.5208333f, 0.14583333f,
                      0.5625f, 0.14583333f,
                      0.5625f, 0.14583333f,
                      0.6041667f, 0.14583333f,
                      0.6041667f, 0.14583333f,
                      0.6458333f, 0.14583333f,
                      0.6458333f, 0.14583333f,
                      0.6875f, 0.14583333f,
                      0.6875f, 0.14583333f,
                      0.7291667f, 0.14583333f,
                      0.7291667f, 0.14583333f,
                      0.7708333f, 0.14583333f,
                      0.7708333f, 0.14583333f,
                      0.8125f, 0.14583333f,
                      0.8125f, 0.14583333f,
                      0.8541667f, 0.14583333f,
                      0.8541667f, 0.14583333f,
                      0.8958333f, 0.14583333f,
                      0.8958333f, 0.14583333f,
                      0.9375f, 0.14583333f,
                      0.9375f, 0.14583333f,
                      0.9791667f, 0.14583333f,
                      0.9791667f, 0.14583333f,
                      0.02083333f, 0.1875f,
                      0.02083333f, 0.1875f,
                      0.0625f, 0.1875f,
                      0.0625f, 0.1875f,
                      0.10416666f, 0.1875f,
                      0.10416666f, 0.1875f,
                      0.14583333f, 0.1875f,
                      0.14583333f, 0.1875f,
                      0.1875f, 0.1875f,
                      0.1875f, 0.1875f,
                      0.22916667f, 0.1875f,
                      0.22916667f, 0.1875f,
                      0.27083334f, 0.1875f,
                      0.27083334f, 0.1875f,
                      0.3125f, 0.1875f,
                      0.3125f, 0.1875f,
                      0.35416666f, 0.1875f,
                      0.35416666f, 0.1875f,
                      0.39583334f, 0.1875f,
                      0.39583334f, 0.1875f,
                      0.4375f, 0.1875f,
                      0.4375f, 0.1875f,
                      0.47916666f, 0.1875f,
                      0.47916666f, 0.1875f,
                      0.5208333f, 0.1875f,
                      0.5208333f, 0.1875f,
                      0.5625f, 0.1875f,
                      0.5625f, 0.1875f,
                      0.6041667f, 0.1875f,
                      0.6041667f, 0.1875f,
                      0.6458333f, 0.1875f,
                      0.6458333f, 0.1875f,
                      0.6875f, 0.1875f,
                      0.6875f, 0.1875f,
                      0.7291667f, 0.1875f,
                      0.7291667f, 0.1875f,
                      0.7708333f, 0.1875f,
                      0.7708333f, 0.1875f,
                      0.8125f, 0.1875f,
                      0.8125f, 0.1875f,
                      0.8541667f, 0.1875f,
                      0.8541667f, 0.1875f,
                      0.8958333f, 0.1875f,
                      0.8958333f, 0.1875f,
                      0.9375f, 0.1875f,
                      0.9375f, 0.1875f,
                      0.9791667f, 0.1875f,
                      0.9791667f, 0.1875f,
                      0.02083333f, 0.22916667f,
                      0.02083333f, 0.22916667f,
                      0.0625f, 0.22916667f,
                      0.0625f, 0.22916667f,
                      0.10416666f, 0.22916667f,
                      0.10416666f, 0.22916667f,
                      0.14583333f, 0.22916667f,
                      0.14583333f, 0.22916667f,
                      0.1875f, 0.22916667f,
                      0.1875f, 0.22916667f,
                      0.22916667f, 0.22916667f,
                      0.22916667f, 0.22916667f,
                      0.27083334f, 0.22916667f,
                      0.27083334f, 0.22916667f,
                      0.3125f, 0.22916667f,
                      0.3125f, 0.22916667f,
                      0.35416666f, 0.22916667f,
                      0.35416666f, 0.22916667f,
                      0.39583334f, 0.22916667f,
                      0.39583334f, 0.22916667f,
                      0.4375f, 0.22916667f,
                      0.4375f, 0.22916667f,
                      0.47916666f, 0.22916667f,
                      0.47916666f, 0.22916667f,
                      0.5208333f, 0.22916667f,
                      0.5208333f, 0.22916667f,
                      0.5625f, 0.22916667f,
                      0.5625f, 0.22916667f,
                      0.6041667f, 0.22916667f,
                      0.6041667f, 0.22916667f,
                      0.6458333f, 0.22916667f,
                      0.6458333f, 0.22916667f,
                      0.6875f, 0.22916667f,
                      0.6875f, 0.22916667f,
                      0.7291667f, 0.22916667f,
                      0.7291667f, 0.22916667f,
                      0.7708333f, 0.22916667f,
                      0.7708333f, 0.22916667f,
                      0.8125f, 0.22916667f,
                      0.8125f, 0.22916667f,
                      0.8541667f, 0.22916667f,
                      0.8541667f, 0.22916667f,
                      0.8958333f, 0.22916667f,
                      0.8958333f, 0.22916667f,
                      0.9375f, 0.22916667f,
                      0.9375f, 0.22916667f,
                      0.9791667f, 0.22916667f,
                      0.9791667f, 0.22916667f,
                      0.02083333f, 0.27083334f,
                      0.02083333f, 0.27083334f,
                      0.0625f, 0.27083334f,
                      0.0625f, 0.27083334f,
                      0.10416666f, 0.27083334f,
                      0.10416666f, 0.27083334f,
                      0.14583333f, 0.27083334f,
                      0.14583333f, 0.27083334f,
                      0.1875f, 0.27083334f,
                      0.1875f, 0.27083334f,
                      0.22916667f, 0.27083334f,
                      0.22916667f, 0.27083334f,
                      0.27083334f, 0.27083334f,
                      0.27083334f, 0.27083334f,
                      0.3125f, 0.27083334f,
                      0.3125f, 0.27083334f,
                      0.35416666f, 0.27083334f,
                      0.35416666f, 0.27083334f,
                      0.39583334f, 0.27083334f,
                      0.39583334f, 0.27083334f,
                      0.4375f, 0.27083334f,
                      0.4375f, 0.27083334f,
                      0.47916666f, 0.27083334f,
                      0.47916666f, 0.27083334f,
                      0.5208333f, 0.27083334f,
                      0.5208333f, 0.27083334f,
                      0.5625f, 0.27083334f,
                      0.5625f, 0.27083334f,
                      0.6041667f, 0.27083334f,
                      0.6041667f, 0.27083334f,
                      0.6458333f, 0.27083334f,
                      0.6458333f, 0.27083334f,
                      0.6875f, 0.27083334f,
                      0.6875f, 0.27083334f,
                      0.7291667f, 0.27083334f,
                      0.7291667f, 0.27083334f,
                      0.7708333f, 0.27083334f,
                      0.7708333f, 0.27083334f,
                      0.8125f, 0.27083334f,
                      0.8125f, 0.27083334f,
                      0.8541667f, 0.27083334f,
                      0.8541667f, 0.27083334f,
                      0.8958333f, 0.27083334f,
                      0.8958333f, 0.27083334f,
                      0.9375f, 0.27083334f,
                      0.9375f, 0.27083334f,
                      0.9791667f, 0.27083334f,
                      0.9791667f, 0.27083334f,
                      0.02083333f, 0.3125f,
                      0.02083333f, 0.3125f,
                      0.0625f, 0.3125f,
                      0.0625f, 0.3125f,
                      0.10416666f, 0.3125f,
                      0.10416666f, 0.3125f,
                      0.14583333f, 0.3125f,
                      0.14583333f, 0.3125f,
                      0.1875f, 0.3125f,
                      0.1875f, 0.3125f,
                      0.22916667f, 0.3125f,
                      0.22916667f, 0.3125f,
                      0.27083334f, 0.3125f,
                      0.27083334f, 0.3125f,
                      0.3125f, 0.3125f,
                      0.3125f, 0.3125f,
                      0.35416666f, 0.3125f,
                      0.35416666f, 0.3125f,
                      0.39583334f, 0.3125f,
                      0.39583334f, 0.3125f,
                      0.4375f, 0.3125f,
                      0.4375f, 0.3125f,
                      0.47916666f, 0.3125f,
                      0.47916666f, 0.3125f,
                      0.5208333f, 0.3125f,
                      0.5208333f, 0.3125f,
                      0.5625f, 0.3125f,
                      0.5625f, 0.3125f,
                      0.6041667f, 0.3125f,
                      0.6041667f, 0.3125f,
                      0.6458333f, 0.3125f,
                      0.6458333f, 0.3125f,
                      0.6875f, 0.3125f,
                      0.6875f, 0.3125f,
                      0.7291667f, 0.3125f,
                      0.7291667f, 0.3125f,
                      0.7708333f, 0.3125f,
                      0.7708333f, 0.3125f,
                      0.8125f, 0.3125f,
                      0.8125f, 0.3125f,
                      0.8541667f, 0.3125f,
                      0.8541667f, 0.3125f,
                      0.8958333f, 0.3125f,
                      0.8958333f, 0.3125f,
                      0.9375f, 0.3125f,
                      0.9375f, 0.3125f,
                      0.9791667f, 0.3125f,
                      0.9791667f, 0.3125f,
                      0.02083333f, 0.35416666f,
                      0.02083333f, 0.35416666f,
                      0.0625f, 0.35416666f,
                      0.0625f, 0.35416666f,
                      0.10416666f, 0.35416666f,
                      0.10416666f, 0.35416666f,
                      0.14583333f, 0.35416666f,
                      0.14583333f, 0.35416666f,
                      0.1875f, 0.35416666f,
                      0.1875f, 0.35416666f,
                      0.22916667f, 0.35416666f,
                      0.22916667f, 0.35416666f,
                      0.27083334f, 0.35416666f,
                      0.27083334f, 0.35416666f,
                      0.3125f, 0.35416666f,
                      0.3125f, 0.35416666f,
                      0.35416666f, 0.35416666f,
                      0.35416666f, 0.35416666f,
                      0.39583334f, 0.35416666f,
                      0.39583334f, 0.35416666f,
                      0.4375f, 0.35416666f,
                      0.4375f, 0.35416666f,
                      0.47916666f, 0.35416666f,
                      0.47916666f, 0.35416666f,
                      0.5208333f, 0.35416666f,
                      0.5208333f, 0.35416666f,
                      0.5625f, 0.35416666f,
                      0.5625f, 0.35416666f,
                      0.6041667f, 0.35416666f,
                      0.6041667f, 0.35416666f,
                      0.6458333f, 0.35416666f,
                      0.6458333f, 0.35416666f,
                      0.6875f, 0.35416666f,
                      0.6875f, 0.35416666f,
                      0.7291667f, 0.35416666f,
                      0.7291667f, 0.35416666f,
                      0.7708333f, 0.35416666f,
                      0.7708333f, 0.35416666f,
                      0.8125f, 0.35416666f,
                      0.8125f, 0.35416666f,
                      0.8541667f, 0.35416666f,
                      0.8541667f, 0.35416666f,
                      0.8958333f, 0.35416666f,
                      0.8958333f, 0.35416666f,
                      0.9375f, 0.35416666f,
                      0.9375f, 0.35416666f,
                      0.9791667f, 0.35416666f,
                      0.9791667f, 0.35416666f,
                      0.02083333f, 0.39583334f,
                      0.02083333f, 0.39583334f,
                      0.0625f, 0.39583334f,
                      0.0625f, 0.39583334f,
                      0.10416666f, 0.39583334f,
                      0.10416666f, 0.39583334f,
                      0.14583333f, 0.39583334f,
                      0.14583333f, 0.39583334f,
                      0.1875f, 0.39583334f,
                      0.1875f, 0.39583334f,
                      0.22916667f, 0.39583334f,
                      0.22916667f, 0.39583334f,
                      0.27083334f, 0.39583334f,
                      0.27083334f, 0.39583334f,
                      0.3125f, 0.39583334f,
                      0.3125f, 0.39583334f,
                      0.35416666f, 0.39583334f,
                      0.35416666f, 0.39583334f,
                      0.39583334f, 0.39583334f,
                      0.39583334f, 0.39583334f,
                      0.4375f, 0.39583334f,
                      0.4375f, 0.39583334f,
                      0.47916666f, 0.39583334f,
                      0.47916666f, 0.39583334f,
                      0.5208333f, 0.39583334f,
                      0.5208333f, 0.39583334f,
                      0.5625f, 0.39583334f,
                      0.5625f, 0.39583334f,
                      0.6041667f, 0.39583334f,
                      0.6041667f, 0.39583334f,
                      0.6458333f, 0.39583334f,
                      0.6458333f, 0.39583334f,
                      0.6875f, 0.39583334f,
                      0.6875f, 0.39583334f,
                      0.7291667f, 0.39583334f,
                      0.7291667f, 0.39583334f,
                      0.7708333f, 0.39583334f,
                      0.7708333f, 0.39583334f,
                      0.8125f, 0.39583334f,
                      0.8125f, 0.39583334f,
                      0.8541667f, 0.39583334f,
                      0.8541667f, 0.39583334f,
                      0.8958333f, 0.39583334f,
                      0.8958333f, 0.39583334f,
                      0.9375f, 0.39583334f,
                      0.9375f, 0.39583334f,
                      0.9791667f, 0.39583334f,
                      0.9791667f, 0.39583334f,
                      0.02083333f, 0.4375f,
                      0.02083333f, 0.4375f,
                      0.0625f, 0.4375f,
                      0.0625f, 0.4375f,
                      0.10416666f, 0.4375f,
                      0.10416666f, 0.4375f,
                      0.14583333f, 0.4375f,
                      0.14583333f, 0.4375f,
                      0.1875f, 0.4375f,
                      0.1875f, 0.4375f,
                      0.22916667f, 0.4375f,
                      0.22916667f, 0.4375f,
                      0.27083334f, 0.4375f,
                      0.27083334f, 0.4375f,
                      0.3125f, 0.4375f,
                      0.3125f, 0.4375f,
                      0.35416666f, 0.4375f,
                      0.35416666f, 0.4375f,
                      0.39583334f, 0.4375f,
                      0.39583334f, 0.4375f,
                      0.4375f, 0.4375f,
                      0.4375f, 0.4375f,
                      0.47916666f, 0.4375f,
                      0.47916666f, 0.4375f,
                      0.5208333f, 0.4375f,
                      0.5208333f, 0.4375f,
                      0.5625f, 0.4375f,
                      0.5625f, 0.4375f,
                      0.6041667f, 0.4375f,
                      0.6041667f, 0.4375f,
                      0.6458333f, 0.4375f,
                      0.6458333f, 0.4375f,
                      0.6875f, 0.4375f,
                      0.6875f, 0.4375f,
                      0.7291667f, 0.4375f,
                      0.7291667f, 0.4375f,
                      0.7708333f, 0.4375f,
                      0.7708333f, 0.4375f,
                      0.8125f, 0.4375f,
                      0.8125f, 0.4375f,
                      0.8541667f, 0.4375f,
                      0.8541667f, 0.4375f,
                      0.8958333f, 0.4375f,
                      0.8958333f, 0.4375f,
                      0.9375f, 0.4375f,
                      0.9375f, 0.4375f,
                      0.9791667f, 0.4375f,
                      0.9791667f, 0.4375f,
                      0.02083333f, 0.47916666f,
                      0.02083333f, 0.47916666f,
                      0.0625f, 0.47916666f,
                      0.0625f, 0.47916666f,
                      0.10416666f, 0.47916666f,
                      0.10416666f, 0.47916666f,
                      0.14583333f, 0.47916666f,
                      0.14583333f, 0.47916666f,
                      0.1875f, 0.47916666f,
                      0.1875f, 0.47916666f,
                      0.22916667f, 0.47916666f,
                      0.22916667f, 0.47916666f,
                      0.27083334f, 0.47916666f,
                      0.27083334f, 0.47916666f,
                      0.3125f, 0.47916666f,
                      0.3125f, 0.47916666f,
                      0.35416666f, 0.47916666f,
                      0.35416666f, 0.47916666f,
                      0.39583334f, 0.47916666f,
                      0.39583334f, 0.47916666f,
                      0.4375f, 0.47916666f,
                      0.4375f, 0.47916666f,
                      0.47916666f, 0.47916666f,
                      0.47916666f, 0.47916666f,
                      0.5208333f, 0.47916666f,
                      0.5208333f, 0.47916666f,
                      0.5625f, 0.47916666f,
                      0.5625f, 0.47916666f,
                      0.6041667f, 0.47916666f,
                      0.6041667f, 0.47916666f,
                      0.6458333f, 0.47916666f,
                      0.6458333f, 0.47916666f,
                      0.6875f, 0.47916666f,
                      0.6875f, 0.47916666f,
                      0.7291667f, 0.47916666f,
                      0.7291667f, 0.47916666f,
                      0.7708333f, 0.47916666f,
                      0.7708333f, 0.47916666f,
                      0.8125f, 0.47916666f,
                      0.8125f, 0.47916666f,
                      0.8541667f, 0.47916666f,
                      0.8541667f, 0.47916666f,
                      0.8958333f, 0.47916666f,
                      0.8958333f, 0.47916666f,
                      0.9375f, 0.47916666f,
                      0.9375f, 0.47916666f,
                      0.9791667f, 0.47916666f,
                      0.9791667f, 0.47916666f,
                      0.02083333f, 0.5208333f,
                      0.02083333f, 0.5208333f,
                      0.0625f, 0.5208333f,
                      0.0625f, 0.5208333f,
                      0.10416666f, 0.5208333f,
                      0.10416666f, 0.5208333f,
                      0.14583333f, 0.5208333f,
                      0.14583333f, 0.5208333f,
                      0.1875f, 0.5208333f,
                      0.1875f, 0.5208333f,
                      0.22916667f, 0.5208333f,
                      0.22916667f, 0.5208333f,
                      0.27083334f, 0.5208333f,
                      0.27083334f, 0.5208333f,
                      0.3125f, 0.5208333f,
                      0.3125f, 0.5208333f,
                      0.35416666f, 0.5208333f,
                      0.35416666f, 0.5208333f,
                      0.39583334f, 0.5208333f,
                      0.39583334f, 0.5208333f,
                      0.4375f, 0.5208333f,
                      0.4375f, 0.5208333f,
                      0.47916666f, 0.5208333f,
                      0.47916666f, 0.5208333f,
                      0.5208333f, 0.5208333f,
                      0.5208333f, 0.5208333f,
                      0.5625f, 0.5208333f,
                      0.5625f, 0.5208333f,
                      0.6041667f, 0.5208333f,
                      0.6041667f, 0.5208333f,
                      0.6458333f, 0.5208333f,
                      0.6458333f, 0.5208333f,
                      0.6875f, 0.5208333f,
                      0.6875f, 0.5208333f,
                      0.7291667f, 0.5208333f,
                      0.7291667f, 0.5208333f,
                      0.7708333f, 0.5208333f,
                      0.7708333f, 0.5208333f,
                      0.8125f, 0.5208333f,
                      0.8125f, 0.5208333f,
                      0.8541667f, 0.5208333f,
                      0.8541667f, 0.5208333f,
                      0.8958333f, 0.5208333f,
                      0.8958333f, 0.5208333f,
                      0.9375f, 0.5208333f,
                      0.9375f, 0.5208333f,
                      0.9791667f, 0.5208333f,
                      0.9791667f, 0.5208333f,
                      0.02083333f, 0.5625f,
                      0.02083333f, 0.5625f,
                      0.0625f, 0.5625f,
                      0.0625f, 0.5625f,
                      0.10416666f, 0.5625f,
                      0.10416666f, 0.5625f,
                      0.14583333f, 0.5625f,
                      0.14583333f, 0.5625f,
                      0.1875f, 0.5625f,
                      0.1875f, 0.5625f,
                      0.22916667f, 0.5625f,
                      0.22916667f, 0.5625f,
                      0.27083334f, 0.5625f,
                      0.27083334f, 0.5625f,
                      0.3125f, 0.5625f,
                      0.3125f, 0.5625f,
                      0.35416666f, 0.5625f,
                      0.35416666f, 0.5625f,
                      0.39583334f, 0.5625f,
                      0.39583334f, 0.5625f,
                      0.4375f, 0.5625f,
                      0.4375f, 0.5625f,
                      0.47916666f, 0.5625f,
                      0.47916666f, 0.5625f,
                      0.5208333f, 0.5625f,
                      0.5208333f, 0.5625f,
                      0.5625f, 0.5625f,
                      0.5625f, 0.5625f,
                      0.6041667f, 0.5625f,
                      0.6041667f, 0.5625f,
                      0.6458333f, 0.5625f,
                      0.6458333f, 0.5625f,
                      0.6875f, 0.5625f,
                      0.6875f, 0.5625f,
                      0.7291667f, 0.5625f,
                      0.7291667f, 0.5625f,
                      0.7708333f, 0.5625f,
                      0.7708333f, 0.5625f,
                      0.8125f, 0.5625f,
                      0.8125f, 0.5625f,
                      0.8541667f, 0.5625f,
                      0.8541667f, 0.5625f,
                      0.8958333f, 0.5625f,
                      0.8958333f, 0.5625f,
                      0.9375f, 0.5625f,
                      0.9375f, 0.5625f,
                      0.9791667f, 0.5625f,
                      0.9791667f, 0.5625f,
                      0.02083333f, 0.6041667f,
                      0.02083333f, 0.6041667f,
                      0.0625f, 0.6041667f,
                      0.0625f, 0.6041667f,
                      0.10416666f, 0.6041667f,
                      0.10416666f, 0.6041667f,
                      0.14583333f, 0.6041667f,
                      0.14583333f, 0.6041667f,
                      0.1875f, 0.6041667f,
                      0.1875f, 0.6041667f,
                      0.22916667f, 0.6041667f,
                      0.22916667f, 0.6041667f,
                      0.27083334f, 0.6041667f,
                      0.27083334f, 0.6041667f,
                      0.3125f, 0.6041667f,
                      0.3125f, 0.6041667f,
                      0.35416666f, 0.6041667f,
                      0.35416666f, 0.6041667f,
                      0.39583334f, 0.6041667f,
                      0.39583334f, 0.6041667f,
                      0.4375f, 0.6041667f,
                      0.4375f, 0.6041667f,
                      0.47916666f, 0.6041667f,
                      0.47916666f, 0.6041667f,
                      0.5208333f, 0.6041667f,
                      0.5208333f, 0.6041667f,
                      0.5625f, 0.6041667f,
                      0.5625f, 0.6041667f,
                      0.6041667f, 0.6041667f,
                      0.6041667f, 0.6041667f,
                      0.6458333f, 0.6041667f,
                      0.6458333f, 0.6041667f,
                      0.6875f, 0.6041667f,
                      0.6875f, 0.6041667f,
                      0.7291667f, 0.6041667f,
                      0.7291667f, 0.6041667f,
                      0.7708333f, 0.6041667f,
                      0.7708333f, 0.6041667f,
                      0.8125f, 0.6041667f,
                      0.8125f, 0.6041667f,
                      0.8541667f, 0.6041667f,
                      0.8541667f, 0.6041667f,
                      0.8958333f, 0.6041667f,
                      0.8958333f, 0.6041667f,
                      0.9375f, 0.6041667f,
                      0.9375f, 0.6041667f,
                      0.9791667f, 0.6041667f,
                      0.9791667f, 0.6041667f,
                      0.02083333f, 0.6458333f,
                      0.02083333f, 0.6458333f,
                      0.0625f, 0.6458333f,
                      0.0625f, 0.6458333f,
                      0.10416666f, 0.6458333f,
                      0.10416666f, 0.6458333f,
                      0.14583333f, 0.6458333f,
                      0.14583333f, 0.6458333f,
                      0.1875f, 0.6458333f,
                      0.1875f, 0.6458333f,
                      0.22916667f, 0.6458333f,
                      0.22916667f, 0.6458333f,
                      0.27083334f, 0.6458333f,
                      0.27083334f, 0.6458333f,
                      0.3125f, 0.6458333f,
                      0.3125f, 0.6458333f,
                      0.35416666f, 0.6458333f,
                      0.35416666f, 0.6458333f,
                      0.39583334f, 0.6458333f,
                      0.39583334f, 0.6458333f,
                      0.4375f, 0.6458333f,
                      0.4375f, 0.6458333f,
                      0.47916666f, 0.6458333f,
                      0.47916666f, 0.6458333f,
                      0.5208333f, 0.6458333f,
                      0.5208333f, 0.6458333f,
                      0.5625f, 0.6458333f,
                      0.5625f, 0.6458333f,
                      0.6041667f, 0.6458333f,
                      0.6041667f, 0.6458333f,
                      0.6458333f, 0.6458333f,
                      0.6458333f, 0.6458333f,
                      0.6875f, 0.6458333f,
                      0.6875f, 0.6458333f,
                      0.7291667f, 0.6458333f,
                      0.7291667f, 0.6458333f,
                      0.7708333f, 0.6458333f,
                      0.7708333f, 0.6458333f,
                      0.8125f, 0.6458333f,
                      0.8125f, 0.6458333f,
                      0.8541667f, 0.6458333f,
                      0.8541667f, 0.6458333f,
                      0.8958333f, 0.6458333f,
                      0.8958333f, 0.6458333f,
                      0.9375f, 0.6458333f,
                      0.9375f, 0.6458333f,
                      0.9791667f, 0.6458333f,
                      0.9791667f, 0.6458333f,
                      0.02083333f, 0.6875f,
                      0.02083333f, 0.6875f,
                      0.0625f, 0.6875f,
                      0.0625f, 0.6875f,
                      0.10416666f, 0.6875f,
                      0.10416666f, 0.6875f,
                      0.14583333f, 0.6875f,
                      0.14583333f, 0.6875f,
                      0.1875f, 0.6875f,
                      0.1875f, 0.6875f,
                      0.22916667f, 0.6875f,
                      0.22916667f, 0.6875f,
                      0.27083334f, 0.6875f,
                      0.27083334f, 0.6875f,
                      0.3125f, 0.6875f,
                      0.3125f, 0.6875f,
                      0.35416666f, 0.6875f,
                      0.35416666f, 0.6875f,
                      0.39583334f, 0.6875f,
                      0.39583334f, 0.6875f,
                      0.4375f, 0.6875f,
                      0.4375f, 0.6875f,
                      0.47916666f, 0.6875f,
                      0.47916666f, 0.6875f,
                      0.5208333f, 0.6875f,
                      0.5208333f, 0.6875f,
                      0.5625f, 0.6875f,
                      0.5625f, 0.6875f,
                      0.6041667f, 0.6875f,
                      0.6041667f, 0.6875f,
                      0.6458333f, 0.6875f,
                      0.6458333f, 0.6875f,
                      0.6875f, 0.6875f,
                      0.6875f, 0.6875f,
                      0.7291667f, 0.6875f,
                      0.7291667f, 0.6875f,
                      0.7708333f, 0.6875f,
                      0.7708333f, 0.6875f,
                      0.8125f, 0.6875f,
                      0.8125f, 0.6875f,
                      0.8541667f, 0.6875f,
                      0.8541667f, 0.6875f,
                      0.8958333f, 0.6875f,
                      0.8958333f, 0.6875f,
                      0.9375f, 0.6875f,
                      0.9375f, 0.6875f,
                      0.9791667f, 0.6875f,
                      0.9791667f, 0.6875f,
                      0.02083333f, 0.7291667f,
                      0.02083333f, 0.7291667f,
                      0.0625f, 0.7291667f,
                      0.0625f, 0.7291667f,
                      0.10416666f, 0.7291667f,
                      0.10416666f, 0.7291667f,
                      0.14583333f, 0.7291667f,
                      0.14583333f, 0.7291667f,
                      0.1875f, 0.7291667f,
                      0.1875f, 0.7291667f,
                      0.22916667f, 0.7291667f,
                      0.22916667f, 0.7291667f,
                      0.27083334f, 0.7291667f,
                      0.27083334f, 0.7291667f,
                      0.3125f, 0.7291667f,
                      0.3125f, 0.7291667f,
                      0.35416666f, 0.7291667f,
                      0.35416666f, 0.7291667f,
                      0.39583334f, 0.7291667f,
                      0.39583334f, 0.7291667f,
                      0.4375f, 0.7291667f,
                      0.4375f, 0.7291667f,
                      0.47916666f, 0.7291667f,
                      0.47916666f, 0.7291667f,
                      0.5208333f, 0.7291667f,
                      0.5208333f, 0.7291667f,
                      0.5625f, 0.7291667f,
                      0.5625f, 0.7291667f,
                      0.6041667f, 0.7291667f,
                      0.6041667f, 0.7291667f,
                      0.6458333f, 0.7291667f,
                      0.6458333f, 0.7291667f,
                      0.6875f, 0.7291667f,
                      0.6875f, 0.7291667f,
                      0.7291667f, 0.7291667f,
                      0.7291667f, 0.7291667f,
                      0.7708333f, 0.7291667f,
                      0.7708333f, 0.7291667f,
                      0.8125f, 0.7291667f,
                      0.8125f, 0.7291667f,
                      0.8541667f, 0.7291667f,
                      0.8541667f, 0.7291667f,
                      0.8958333f, 0.7291667f,
                      0.8958333f, 0.7291667f,
                      0.9375f, 0.7291667f,
                      0.9375f, 0.7291667f,
                      0.9791667f, 0.7291667f,
                      0.9791667f, 0.7291667f,
                      0.02083333f, 0.7708333f,
                      0.02083333f, 0.7708333f,
                      0.0625f, 0.7708333f,
                      0.0625f, 0.7708333f,
                      0.10416666f, 0.7708333f,
                      0.10416666f, 0.7708333f,
                      0.14583333f, 0.7708333f,
                      0.14583333f, 0.7708333f,
                      0.1875f, 0.7708333f,
                      0.1875f, 0.7708333f,
                      0.22916667f, 0.7708333f,
                      0.22916667f, 0.7708333f,
                      0.27083334f, 0.7708333f,
                      0.27083334f, 0.7708333f,
                      0.3125f, 0.7708333f,
                      0.3125f, 0.7708333f,
                      0.35416666f, 0.7708333f,
                      0.35416666f, 0.7708333f,
                      0.39583334f, 0.7708333f,
                      0.39583334f, 0.7708333f,
                      0.4375f, 0.7708333f,
                      0.4375f, 0.7708333f,
                      0.47916666f, 0.7708333f,
                      0.47916666f, 0.7708333f,
                      0.5208333f, 0.7708333f,
                      0.5208333f, 0.7708333f,
                      0.5625f, 0.7708333f,
                      0.5625f, 0.7708333f,
                      0.6041667f, 0.7708333f,
                      0.6041667f, 0.7708333f,
                      0.6458333f, 0.7708333f,
                      0.6458333f, 0.7708333f,
                      0.6875f, 0.7708333f,
                      0.6875f, 0.7708333f,
                      0.7291667f, 0.7708333f,
                      0.7291667f, 0.7708333f,
                      0.7708333f, 0.7708333f,
                      0.7708333f, 0.7708333f,
                      0.8125f, 0.7708333f,
                      0.8125f, 0.7708333f,
                      0.8541667f, 0.7708333f,
                      0.8541667f, 0.7708333f,
                      0.8958333f, 0.7708333f,
                      0.8958333f, 0.7708333f,
                      0.9375f, 0.7708333f,
                      0.9375f, 0.7708333f,
                      0.9791667f, 0.7708333f,
                      0.9791667f, 0.7708333f,
                      0.02083333f, 0.8125f,
                      0.02083333f, 0.8125f,
                      0.0625f, 0.8125f,
                      0.0625f, 0.8125f,
                      0.10416666f, 0.8125f,
                      0.10416666f, 0.8125f,
                      0.14583333f, 0.8125f,
                      0.14583333f, 0.8125f,
                      0.1875f, 0.8125f,
                      0.1875f, 0.8125f,
                      0.22916667f, 0.8125f,
                      0.22916667f, 0.8125f,
                      0.27083334f, 0.8125f,
                      0.27083334f, 0.8125f,
                      0.3125f, 0.8125f,
                      0.3125f, 0.8125f,
                      0.35416666f, 0.8125f,
                      0.35416666f, 0.8125f,
                      0.39583334f, 0.8125f,
                      0.39583334f, 0.8125f,
                      0.4375f, 0.8125f,
                      0.4375f, 0.8125f,
                      0.47916666f, 0.8125f,
                      0.47916666f, 0.8125f,
                      0.5208333f, 0.8125f,
                      0.5208333f, 0.8125f,
                      0.5625f, 0.8125f,
                      0.5625f, 0.8125f,
                      0.6041667f, 0.8125f,
                      0.6041667f, 0.8125f,
                      0.6458333f, 0.8125f,
                      0.6458333f, 0.8125f,
                      0.6875f, 0.8125f,
                      0.6875f, 0.8125f,
                      0.7291667f, 0.8125f,
                      0.7291667f, 0.8125f,
                      0.7708333f, 0.8125f,
                      0.7708333f, 0.8125f,
                      0.8125f, 0.8125f,
                      0.8125f, 0.8125f,
                      0.8541667f, 0.8125f,
                      0.8541667f, 0.8125f,
                      0.8958333f, 0.8125f,
                      0.8958333f, 0.8125f,
                      0.9375f, 0.8125f,
                      0.9375f, 0.8125f,
                      0.9791667f, 0.8125f,
                      0.9791667f, 0.8125f,
                      0.02083333f, 0.8541667f,
                      0.02083333f, 0.8541667f,
                      0.0625f, 0.8541667f,
                      0.0625f, 0.8541667f,
                      0.10416666f, 0.8541667f,
                      0.10416666f, 0.8541667f,
                      0.14583333f, 0.8541667f,
                      0.14583333f, 0.8541667f,
                      0.1875f, 0.8541667f,
                      0.1875f, 0.8541667f,
                      0.22916667f, 0.8541667f,
                      0.22916667f, 0.8541667f,
                      0.27083334f, 0.8541667f,
                      0.27083334f, 0.8541667f,
                      0.3125f, 0.8541667f,
                      0.3125f, 0.8541667f,
                      0.35416666f, 0.8541667f,
                      0.35416666f, 0.8541667f,
                      0.39583334f, 0.8541667f,
                      0.39583334f, 0.8541667f,
                      0.4375f, 0.8541667f,
                      0.4375f, 0.8541667f,
                      0.47916666f, 0.8541667f,
                      0.47916666f, 0.8541667f,
                      0.5208333f, 0.8541667f,
                      0.5208333f, 0.8541667f,
                      0.5625f, 0.8541667f,
                      0.5625f, 0.8541667f,
                      0.6041667f, 0.8541667f,
                      0.6041667f, 0.8541667f,
                      0.6458333f, 0.8541667f,
                      0.6458333f, 0.8541667f,
                      0.6875f, 0.8541667f,
                      0.6875f, 0.8541667f,
                      0.7291667f, 0.8541667f,
                      0.7291667f, 0.8541667f,
                      0.7708333f, 0.8541667f,
                      0.7708333f, 0.8541667f,
                      0.8125f, 0.8541667f,
                      0.8125f, 0.8541667f,
                      0.8541667f, 0.8541667f,
                      0.8541667f, 0.8541667f,
                      0.8958333f, 0.8541667f,
                      0.8958333f, 0.8541667f,
                      0.9375f, 0.8541667f,
                      0.9375f, 0.8541667f,
                      0.9791667f, 0.8541667f,
                      0.9791667f, 0.8541667f,
                      0.02083333f, 0.8958333f,
                      0.02083333f, 0.8958333f,
                      0.0625f, 0.8958333f,
                      0.0625f, 0.8958333f,
                      0.10416666f, 0.8958333f,
                      0.10416666f, 0.8958333f,
                      0.14583333f, 0.8958333f,
                      0.14583333f, 0.8958333f,
                      0.1875f, 0.8958333f,
                      0.1875f, 0.8958333f,
                      0.22916667f, 0.8958333f,
                      0.22916667f, 0.8958333f,
                      0.27083334f, 0.8958333f,
                      0.27083334f, 0.8958333f,
                      0.3125f, 0.8958333f,
                      0.3125f, 0.8958333f,
                      0.35416666f, 0.8958333f,
                      0.35416666f, 0.8958333f,
                      0.39583334f, 0.8958333f,
                      0.39583334f, 0.8958333f,
                      0.4375f, 0.8958333f,
                      0.4375f, 0.8958333f,
                      0.47916666f, 0.8958333f,
                      0.47916666f, 0.8958333f,
                      0.5208333f, 0.8958333f,
                      0.5208333f, 0.8958333f,
                      0.5625f, 0.8958333f,
                      0.5625f, 0.8958333f,
                      0.6041667f, 0.8958333f,
                      0.6041667f, 0.8958333f,
                      0.6458333f, 0.8958333f,
                      0.6458333f, 0.8958333f,
                      0.6875f, 0.8958333f,
                      0.6875f, 0.8958333f,
                      0.7291667f, 0.8958333f,
                      0.7291667f, 0.8958333f,
                      0.7708333f, 0.8958333f,
                      0.7708333f, 0.8958333f,
                      0.8125f, 0.8958333f,
                      0.8125f, 0.8958333f,
                      0.8541667f, 0.8958333f,
                      0.8541667f, 0.8958333f,
                      0.8958333f, 0.8958333f,
                      0.8958333f, 0.8958333f,
                      0.9375f, 0.8958333f,
                      0.9375f, 0.8958333f,
                      0.9791667f, 0.8958333f,
                      0.9791667f, 0.8958333f,
                      0.02083333f, 0.9375f,
                      0.02083333f, 0.9375f,
                      0.0625f, 0.9375f,
                      0.0625f, 0.9375f,
                      0.10416666f, 0.9375f,
                      0.10416666f, 0.9375f,
                      0.14583333f, 0.9375f,
                      0.14583333f, 0.9375f,
                      0.1875f, 0.9375f,
                      0.1875f, 0.9375f,
                      0.22916667f, 0.9375f,
                      0.22916667f, 0.9375f,
                      0.27083334f, 0.9375f,
                      0.27083334f, 0.9375f,
                      0.3125f, 0.9375f,
                      0.3125f, 0.9375f,
                      0.35416666f, 0.9375f,
                      0.35416666f, 0.9375f,
                      0.39583334f, 0.9375f,
                      0.39583334f, 0.9375f,
                      0.4375f, 0.9375f,
                      0.4375f, 0.9375f,
                      0.47916666f, 0.9375f,
                      0.47916666f, 0.9375f,
                      0.5208333f, 0.9375f,
                      0.5208333f, 0.9375f,
                      0.5625f, 0.9375f,
                      0.5625f, 0.9375f,
                      0.6041667f, 0.9375f,
                      0.6041667f, 0.9375f,
                      0.6458333f, 0.9375f,
                      0.6458333f, 0.9375f,
                      0.6875f, 0.9375f,
                      0.6875f, 0.9375f,
                      0.7291667f, 0.9375f,
                      0.7291667f, 0.9375f,
                      0.7708333f, 0.9375f,
                      0.7708333f, 0.9375f,
                      0.8125f, 0.9375f,
                      0.8125f, 0.9375f,
                      0.8541667f, 0.9375f,
                      0.8541667f, 0.9375f,
                      0.8958333f, 0.9375f,
                      0.8958333f, 0.9375f,
                      0.9375f, 0.9375f,
                      0.9375f, 0.9375f,
                      0.9791667f, 0.9375f,
                      0.9791667f, 0.9375f,
                      0.02083333f, 0.9791667f,
                      0.02083333f, 0.9791667f,
                      0.0625f, 0.9791667f,
                      0.0625f, 0.9791667f,
                      0.10416666f, 0.9791667f,
                      0.10416666f, 0.9791667f,
                      0.14583333f, 0.9791667f,
                      0.14583333f, 0.9791667f,
                      0.1875f, 0.9791667f,
                      0.1875f, 0.9791667f,
                      0.22916667f, 0.9791667f,
                      0.22916667f, 0.9791667f,
                      0.27083334f, 0.9791667f,
                      0.27083334f, 0.9791667f,
                      0.3125f, 0.9791667f,
                      0.3125f, 0.9791667f,
                      0.35416666f, 0.9791667f,
                      0.35416666f, 0.9791667f,
                      0.39583334f, 0.9791667f,
                      0.39583334f, 0.9791667f,
                      0.4375f, 0.9791667f,
                      0.4375f, 0.9791667f,
                      0.47916666f, 0.9791667f,
                      0.47916666f, 0.9791667f,
                      0.5208333f, 0.9791667f,
                      0.5208333f, 0.9791667f,
                      0.5625f, 0.9791667f,
                      0.5625f, 0.9791667f,
                      0.6041667f, 0.9791667f,
                      0.6041667f, 0.9791667f,
                      0.6458333f, 0.9791667f,
                      0.6458333f, 0.9791667f,
                      0.6875f, 0.9791667f,
                      0.6875f, 0.9791667f,
                      0.7291667f, 0.9791667f,
                      0.7291667f, 0.9791667f,
                      0.7708333f, 0.9791667f,
                      0.7708333f, 0.9791667f,
                      0.8125f, 0.9791667f,
                      0.8125f, 0.9791667f,
                      0.8541667f, 0.9791667f,
                      0.8541667f, 0.9791667f,
                      0.8958333f, 0.9791667f,
                      0.8958333f, 0.9791667f,
                      0.9375f, 0.9791667f,
                      0.9375f, 0.9791667f,
                      0.9791667f, 0.9791667f,
                      0.9791667f, 0.9791667f,
                      0.04166667f, 0.04166667f,
                      0.04166667f, 0.04166667f,
                      0.04166667f, 0.04166667f,
                      0.04166667f, 0.04166667f,
                      0.04166667f, 0.04166667f,
                      0.04166667f, 0.04166667f,
                      0.125f, 0.04166667f,
                      0.125f, 0.04166667f,
                      0.125f, 0.04166667f,
                      0.125f, 0.04166667f,
                      0.125f, 0.04166667f,
                      0.125f, 0.04166667f,
                      0.20833333f, 0.04166667f,
                      0.20833333f, 0.04166667f,
                      0.20833333f, 0.04166667f,
                      0.20833333f, 0.04166667f,
                      0.20833333f, 0.04166667f,
                      0.20833333f, 0.04166667f,
                      0.29166666f, 0.04166667f,
                      0.29166666f, 0.04166667f,
                      0.29166666f, 0.04166667f,
                      0.29166666f, 0.04166667f,
                      0.29166666f, 0.04166667f,
                      0.29166666f, 0.04166667f,
                      0.375f, 0.04166667f,
                      0.375f, 0.04166667f,
                      0.375f, 0.04166667f,
                      0.375f, 0.04166667f,
                      0.375f, 0.04166667f,
                      0.375f, 0.04166667f,
                      0.45833334f, 0.04166667f,
                      0.45833334f, 0.04166667f,
                      0.45833334f, 0.04166667f,
                      0.45833334f, 0.04166667f,
                      0.45833334f, 0.04166667f,
                      0.45833334f, 0.04166667f,
                      0.5416667f, 0.04166667f,
                      0.5416667f, 0.04166667f,
                      0.5416667f, 0.04166667f,
                      0.5416667f, 0.04166667f,
                      0.5416667f, 0.04166667f,
                      0.5416667f, 0.04166667f,
                      0.625f, 0.04166667f,
                      0.625f, 0.04166667f,
                      0.625f, 0.04166667f,
                      0.625f, 0.04166667f,
                      0.625f, 0.04166667f,
                      0.625f, 0.04166667f,
                      0.7083333f, 0.04166667f,
                      0.7083333f, 0.04166667f,
                      0.7083333f, 0.04166667f,
                      0.7083333f, 0.04166667f,
                      0.7083333f, 0.04166667f,
                      0.7083333f, 0.04166667f,
                      0.7916667f, 0.04166667f,
                      0.7916667f, 0.04166667f,
                      0.7916667f, 0.04166667f,
                      0.7916667f, 0.04166667f,
                      0.7916667f, 0.04166667f,
                      0.7916667f, 0.04166667f,
                      0.875f, 0.04166667f,
                      0.875f, 0.04166667f,
                      0.875f, 0.04166667f,
                      0.875f, 0.04166667f,
                      0.875f, 0.04166667f,
                      0.875f, 0.04166667f,
                      0.9583333f, 0.04166667f,
                      0.9583333f, 0.04166667f,
                      0.9583333f, 0.04166667f,
                      0.9583333f, 0.04166667f,
                      0.9583333f, 0.04166667f,
                      0.9583333f, 0.04166667f,
                      0.04166667f, 0.125f,
                      0.04166667f, 0.125f,
                      0.04166667f, 0.125f,
                      0.04166667f, 0.125f,
                      0.04166667f, 0.125f,
                      0.04166667f, 0.125f,
                      0.125f, 0.125f,
                      0.125f, 0.125f,
                      0.125f, 0.125f,
                      0.125f, 0.125f,
                      0.125f, 0.125f,
                      0.125f, 0.125f,
                      0.20833333f, 0.125f,
                      0.20833333f, 0.125f,
                      0.20833333f, 0.125f,
                      0.20833333f, 0.125f,
                      0.20833333f, 0.125f,
                      0.20833333f, 0.125f,
                      0.29166666f, 0.125f,
                      0.29166666f, 0.125f,
                      0.29166666f, 0.125f,
                      0.29166666f, 0.125f,
                      0.29166666f, 0.125f,
                      0.29166666f, 0.125f,
                      0.375f, 0.125f,
                      0.375f, 0.125f,
                      0.375f, 0.125f,
                      0.375f, 0.125f,
                      0.375f, 0.125f,
                      0.375f, 0.125f,
                      0.45833334f, 0.125f,
                      0.45833334f, 0.125f,
                      0.45833334f, 0.125f,
                      0.45833334f, 0.125f,
                      0.45833334f, 0.125f,
                      0.45833334f, 0.125f,
                      0.5416667f, 0.125f,
                      0.5416667f, 0.125f,
                      0.5416667f, 0.125f,
                      0.5416667f, 0.125f,
                      0.5416667f, 0.125f,
                      0.5416667f, 0.125f,
                      0.625f, 0.125f,
                      0.625f, 0.125f,
                      0.625f, 0.125f,
                      0.625f, 0.125f,
                      0.625f, 0.125f,
                      0.625f, 0.125f,
                      0.7083333f, 0.125f,
                      0.7083333f, 0.125f,
                      0.7083333f, 0.125f,
                      0.7083333f, 0.125f,
                      0.7083333f, 0.125f,
                      0.7083333f, 0.125f,
                      0.7916667f, 0.125f,
                      0.7916667f, 0.125f,
                      0.7916667f, 0.125f,
                      0.7916667f, 0.125f,
                      0.7916667f, 0.125f,
                      0.7916667f, 0.125f,
                      0.875f, 0.125f,
                      0.875f, 0.125f,
                      0.875f, 0.125f,
                      0.875f, 0.125f,
                      0.875f, 0.125f,
                      0.875f, 0.125f,
                      0.9583333f, 0.125f,
                      0.9583333f, 0.125f,
                      0.9583333f, 0.125f,
                      0.9583333f, 0.125f,
                      0.9583333f, 0.125f,
                      0.9583333f, 0.125f,
                      0.04166667f, 0.20833333f,
                      0.04166667f, 0.20833333f,
                      0.04166667f, 0.20833333f,
                      0.04166667f, 0.20833333f,
                      0.04166667f, 0.20833333f,
                      0.04166667f, 0.20833333f,
                      0.125f, 0.20833333f,
                      0.125f, 0.20833333f,
                      0.125f, 0.20833333f,
                      0.125f, 0.20833333f,
                      0.125f, 0.20833333f,
                      0.125f, 0.20833333f,
                      0.20833333f, 0.20833333f,
                      0.20833333f, 0.20833333f,
                      0.20833333f, 0.20833333f,
                      0.20833333f, 0.20833333f,
                      0.20833333f, 0.20833333f,
                      0.20833333f, 0.20833333f,
                      0.29166666f, 0.20833333f,
                      0.29166666f, 0.20833333f,
                      0.29166666f, 0.20833333f,
                      0.29166666f, 0.20833333f,
                      0.29166666f, 0.20833333f,
                      0.29166666f, 0.20833333f,
                      0.375f, 0.20833333f,
                      0.375f, 0.20833333f,
                      0.375f, 0.20833333f,
                      0.375f, 0.20833333f,
                      0.375f, 0.20833333f,
                      0.375f, 0.20833333f,
                      0.45833334f, 0.20833333f,
                      0.45833334f, 0.20833333f,
                      0.45833334f, 0.20833333f,
                      0.45833334f, 0.20833333f,
                      0.45833334f, 0.20833333f,
                      0.45833334f, 0.20833333f,
                      0.5416667f, 0.20833333f,
                      0.5416667f, 0.20833333f,
                      0.5416667f, 0.20833333f,
                      0.5416667f, 0.20833333f,
                      0.5416667f, 0.20833333f,
                      0.5416667f, 0.20833333f,
                      0.625f, 0.20833333f,
                      0.625f, 0.20833333f,
                      0.625f, 0.20833333f,
                      0.625f, 0.20833333f,
                      0.625f, 0.20833333f,
                      0.625f, 0.20833333f,
                      0.7083333f, 0.20833333f,
                      0.7083333f, 0.20833333f,
                      0.7083333f, 0.20833333f,
                      0.7083333f, 0.20833333f,
                      0.7083333f, 0.20833333f,
                      0.7083333f, 0.20833333f,
                      0.7916667f, 0.20833333f,
                      0.7916667f, 0.20833333f,
                      0.7916667f, 0.20833333f,
                      0.7916667f, 0.20833333f,
                      0.7916667f, 0.20833333f,
                      0.7916667f, 0.20833333f,
                      0.875f, 0.20833333f,
                      0.875f, 0.20833333f,
                      0.875f, 0.20833333f,
                      0.875f, 0.20833333f,
                      0.875f, 0.20833333f,
                      0.875f, 0.20833333f,
                      0.9583333f, 0.20833333f,
                      0.9583333f, 0.20833333f,
                      0.9583333f, 0.20833333f,
                      0.9583333f, 0.20833333f,
                      0.9583333f, 0.20833333f,
                      0.9583333f, 0.20833333f,
                      0.04166667f, 0.29166666f,
                      0.04166667f, 0.29166666f,
                      0.04166667f, 0.29166666f,
                      0.04166667f, 0.29166666f,
                      0.04166667f, 0.29166666f,
                      0.04166667f, 0.29166666f,
                      0.125f, 0.29166666f,
                      0.125f, 0.29166666f,
                      0.125f, 0.29166666f,
                      0.125f, 0.29166666f,
                      0.125f, 0.29166666f,
                      0.125f, 0.29166666f,
                      0.20833333f, 0.29166666f,
                      0.20833333f, 0.29166666f,
                      0.20833333f, 0.29166666f,
                      0.20833333f, 0.29166666f,
                      0.20833333f, 0.29166666f,
                      0.20833333f, 0.29166666f,
                      0.29166666f, 0.29166666f,
                      0.29166666f, 0.29166666f,
                      0.29166666f, 0.29166666f,
                      0.29166666f, 0.29166666f,
                      0.29166666f, 0.29166666f,
                      0.29166666f, 0.29166666f,
                      0.375f, 0.29166666f,
                      0.375f, 0.29166666f,
                      0.375f, 0.29166666f,
                      0.375f, 0.29166666f,
                      0.375f, 0.29166666f,
                      0.375f, 0.29166666f,
                      0.45833334f, 0.29166666f,
                      0.45833334f, 0.29166666f,
                      0.45833334f, 0.29166666f,
                      0.45833334f, 0.29166666f,
                      0.45833334f, 0.29166666f,
                      0.45833334f, 0.29166666f,
                      0.5416667f, 0.29166666f,
                      0.5416667f, 0.29166666f,
                      0.5416667f, 0.29166666f,
                      0.5416667f, 0.29166666f,
                      0.5416667f, 0.29166666f,
                      0.5416667f, 0.29166666f,
                      0.625f, 0.29166666f,
                      0.625f, 0.29166666f,
                      0.625f, 0.29166666f,
                      0.625f, 0.29166666f,
                      0.625f, 0.29166666f,
                      0.625f, 0.29166666f,
                      0.7083333f, 0.29166666f,
                      0.7083333f, 0.29166666f,
                      0.7083333f, 0.29166666f,
                      0.7083333f, 0.29166666f,
                      0.7083333f, 0.29166666f,
                      0.7083333f, 0.29166666f,
                      0.7916667f, 0.29166666f,
                      0.7916667f, 0.29166666f,
                      0.7916667f, 0.29166666f,
                      0.7916667f, 0.29166666f,
                      0.7916667f, 0.29166666f,
                      0.7916667f, 0.29166666f,
                      0.875f, 0.29166666f,
                      0.875f, 0.29166666f,
                      0.875f, 0.29166666f,
                      0.875f, 0.29166666f,
                      0.875f, 0.29166666f,
                      0.875f, 0.29166666f,
                      0.9583333f, 0.29166666f,
                      0.9583333f, 0.29166666f,
                      0.9583333f, 0.29166666f,
                      0.9583333f, 0.29166666f,
                      0.9583333f, 0.29166666f,
                      0.9583333f, 0.29166666f,
                      0.04166667f, 0.375f,
                      0.04166667f, 0.375f,
                      0.04166667f, 0.375f,
                      0.04166667f, 0.375f,
                      0.04166667f, 0.375f,
                      0.04166667f, 0.375f,
                      0.125f, 0.375f,
                      0.125f, 0.375f,
                      0.125f, 0.375f,
                      0.125f, 0.375f,
                      0.125f, 0.375f,
                      0.125f, 0.375f,
                      0.20833333f, 0.375f,
                      0.20833333f, 0.375f,
                      0.20833333f, 0.375f,
                      0.20833333f, 0.375f,
                      0.20833333f, 0.375f,
                      0.20833333f, 0.375f,
                      0.29166666f, 0.375f,
                      0.29166666f, 0.375f,
                      0.29166666f, 0.375f,
                      0.29166666f, 0.375f,
                      0.29166666f, 0.375f,
                      0.29166666f, 0.375f,
                      0.375f, 0.375f,
                      0.375f, 0.375f,
                      0.375f, 0.375f,
                      0.375f, 0.375f,
                      0.375f, 0.375f,
                      0.375f, 0.375f,
                      0.45833334f, 0.375f,
                      0.45833334f, 0.375f,
                      0.45833334f, 0.375f,
                      0.45833334f, 0.375f,
                      0.45833334f, 0.375f,
                      0.45833334f, 0.375f,
                      0.5416667f, 0.375f,
                      0.5416667f, 0.375f,
                      0.5416667f, 0.375f,
                      0.5416667f, 0.375f,
                      0.5416667f, 0.375f,
                      0.5416667f, 0.375f,
                      0.625f, 0.375f,
                      0.625f, 0.375f,
                      0.625f, 0.375f,
                      0.625f, 0.375f,
                      0.625f, 0.375f,
                      0.625f, 0.375f,
                      0.7083333f, 0.375f,
                      0.7083333f, 0.375f,
                      0.7083333f, 0.375f,
                      0.7083333f, 0.375f,
                      0.7083333f, 0.375f,
                      0.7083333f, 0.375f,
                      0.7916667f, 0.375f,
                      0.7916667f, 0.375f,
                      0.7916667f, 0.375f,
                      0.7916667f, 0.375f,
                      0.7916667f, 0.375f,
                      0.7916667f, 0.375f,
                      0.875f, 0.375f,
                      0.875f, 0.375f,
                      0.875f, 0.375f,
                      0.875f, 0.375f,
                      0.875f, 0.375f,
                      0.875f, 0.375f,
                      0.9583333f, 0.375f,
                      0.9583333f, 0.375f,
                      0.9583333f, 0.375f,
                      0.9583333f, 0.375f,
                      0.9583333f, 0.375f,
                      0.9583333f, 0.375f,
                      0.04166667f, 0.45833334f,
                      0.04166667f, 0.45833334f,
                      0.04166667f, 0.45833334f,
                      0.04166667f, 0.45833334f,
                      0.04166667f, 0.45833334f,
                      0.04166667f, 0.45833334f,
                      0.125f, 0.45833334f,
                      0.125f, 0.45833334f,
                      0.125f, 0.45833334f,
                      0.125f, 0.45833334f,
                      0.125f, 0.45833334f,
                      0.125f, 0.45833334f,
                      0.20833333f, 0.45833334f,
                      0.20833333f, 0.45833334f,
                      0.20833333f, 0.45833334f,
                      0.20833333f, 0.45833334f,
                      0.20833333f, 0.45833334f,
                      0.20833333f, 0.45833334f,
                      0.29166666f, 0.45833334f,
                      0.29166666f, 0.45833334f,
                      0.29166666f, 0.45833334f,
                      0.29166666f, 0.45833334f,
                      0.29166666f, 0.45833334f,
                      0.29166666f, 0.45833334f,
                      0.375f, 0.45833334f,
                      0.375f, 0.45833334f,
                      0.375f, 0.45833334f,
                      0.375f, 0.45833334f,
                      0.375f, 0.45833334f,
                      0.375f, 0.45833334f,
                      0.45833334f, 0.45833334f,
                      0.45833334f, 0.45833334f,
                      0.45833334f, 0.45833334f,
                      0.45833334f, 0.45833334f,
                      0.45833334f, 0.45833334f,
                      0.45833334f, 0.45833334f,
                      0.5416667f, 0.45833334f,
                      0.5416667f, 0.45833334f,
                      0.5416667f, 0.45833334f,
                      0.5416667f, 0.45833334f,
                      0.5416667f, 0.45833334f,
                      0.5416667f, 0.45833334f,
                      0.625f, 0.45833334f,
                      0.625f, 0.45833334f,
                      0.625f, 0.45833334f,
                      0.625f, 0.45833334f,
                      0.625f, 0.45833334f,
                      0.625f, 0.45833334f,
                      0.7083333f, 0.45833334f,
                      0.7083333f, 0.45833334f,
                      0.7083333f, 0.45833334f,
                      0.7083333f, 0.45833334f,
                      0.7083333f, 0.45833334f,
                      0.7083333f, 0.45833334f,
                      0.7916667f, 0.45833334f,
                      0.7916667f, 0.45833334f,
                      0.7916667f, 0.45833334f,
                      0.7916667f, 0.45833334f,
                      0.7916667f, 0.45833334f,
                      0.7916667f, 0.45833334f,
                      0.875f, 0.45833334f,
                      0.875f, 0.45833334f,
                      0.875f, 0.45833334f,
                      0.875f, 0.45833334f,
                      0.875f, 0.45833334f,
                      0.875f, 0.45833334f,
                      0.9583333f, 0.45833334f,
                      0.9583333f, 0.45833334f,
                      0.9583333f, 0.45833334f,
                      0.9583333f, 0.45833334f,
                      0.9583333f, 0.45833334f,
                      0.9583333f, 0.45833334f,
                      0.04166667f, 0.5416667f,
                      0.04166667f, 0.5416667f,
                      0.04166667f, 0.5416667f,
                      0.04166667f, 0.5416667f,
                      0.04166667f, 0.5416667f,
                      0.04166667f, 0.5416667f,
                      0.125f, 0.5416667f,
                      0.125f, 0.5416667f,
                      0.125f, 0.5416667f,
                      0.125f, 0.5416667f,
                      0.125f, 0.5416667f,
                      0.125f, 0.5416667f,
                      0.20833333f, 0.5416667f,
                      0.20833333f, 0.5416667f,
                      0.20833333f, 0.5416667f,
                      0.20833333f, 0.5416667f,
                      0.20833333f, 0.5416667f,
                      0.20833333f, 0.5416667f,
                      0.29166666f, 0.5416667f,
                      0.29166666f, 0.5416667f,
                      0.29166666f, 0.5416667f,
                      0.29166666f, 0.5416667f,
                      0.29166666f, 0.5416667f,
                      0.29166666f, 0.5416667f,
                      0.375f, 0.5416667f,
                      0.375f, 0.5416667f,
                      0.375f, 0.5416667f,
                      0.375f, 0.5416667f,
                      0.375f, 0.5416667f,
                      0.375f, 0.5416667f,
                      0.45833334f, 0.5416667f,
                      0.45833334f, 0.5416667f,
                      0.45833334f, 0.5416667f,
                      0.45833334f, 0.5416667f,
                      0.45833334f, 0.5416667f,
                      0.45833334f, 0.5416667f,
                      0.5416667f, 0.5416667f,
                      0.5416667f, 0.5416667f,
                      0.5416667f, 0.5416667f,
                      0.5416667f, 0.5416667f,
                      0.5416667f, 0.5416667f,
                      0.5416667f, 0.5416667f,
                      0.625f, 0.5416667f,
                      0.625f, 0.5416667f,
                      0.625f, 0.5416667f,
                      0.625f, 0.5416667f,
                      0.625f, 0.5416667f,
                      0.625f, 0.5416667f,
                      0.7083333f, 0.5416667f,
                      0.7083333f, 0.5416667f,
                      0.7083333f, 0.5416667f,
                      0.7083333f, 0.5416667f,
                      0.7083333f, 0.5416667f,
                      0.7083333f, 0.5416667f,
                      0.7916667f, 0.5416667f,
                      0.7916667f, 0.5416667f,
                      0.7916667f, 0.5416667f,
                      0.7916667f, 0.5416667f,
                      0.7916667f, 0.5416667f,
                      0.7916667f, 0.5416667f,
                      0.875f, 0.5416667f,
                      0.875f, 0.5416667f,
                      0.875f, 0.5416667f,
                      0.875f, 0.5416667f,
                      0.875f, 0.5416667f,
                      0.875f, 0.5416667f,
                      0.9583333f, 0.5416667f,
                      0.9583333f, 0.5416667f,
                      0.9583333f, 0.5416667f,
                      0.9583333f, 0.5416667f,
                      0.9583333f, 0.5416667f,
                      0.9583333f, 0.5416667f,
                      0.04166667f, 0.625f,
                      0.04166667f, 0.625f,
                      0.04166667f, 0.625f,
                      0.04166667f, 0.625f,
                      0.04166667f, 0.625f,
                      0.04166667f, 0.625f,
                      0.125f, 0.625f,
                      0.125f, 0.625f,
                      0.125f, 0.625f,
                      0.125f, 0.625f,
                      0.125f, 0.625f,
                      0.125f, 0.625f,
                      0.20833333f, 0.625f,
                      0.20833333f, 0.625f,
                      0.20833333f, 0.625f,
                      0.20833333f, 0.625f,
                      0.20833333f, 0.625f,
                      0.20833333f, 0.625f,
                      0.29166666f, 0.625f,
                      0.29166666f, 0.625f,
                      0.29166666f, 0.625f,
                      0.29166666f, 0.625f,
                      0.29166666f, 0.625f,
                      0.29166666f, 0.625f,
                      0.375f, 0.625f,
                      0.375f, 0.625f,
                      0.375f, 0.625f,
                      0.375f, 0.625f,
                      0.375f, 0.625f,
                      0.375f, 0.625f,
                      0.45833334f, 0.625f,
                      0.45833334f, 0.625f,
                      0.45833334f, 0.625f,
                      0.45833334f, 0.625f,
                      0.45833334f, 0.625f,
                      0.45833334f, 0.625f,
                      0.5416667f, 0.625f,
                      0.5416667f, 0.625f,
                      0.5416667f, 0.625f,
                      0.5416667f, 0.625f,
                      0.5416667f, 0.625f,
                      0.5416667f, 0.625f,
                      0.625f, 0.625f,
                      0.625f, 0.625f,
                      0.625f, 0.625f,
                      0.625f, 0.625f,
                      0.625f, 0.625f,
                      0.625f, 0.625f,
                      0.7083333f, 0.625f,
                      0.7083333f, 0.625f,
                      0.7083333f, 0.625f,
                      0.7083333f, 0.625f,
                      0.7083333f, 0.625f,
                      0.7083333f, 0.625f,
                      0.7916667f, 0.625f,
                      0.7916667f, 0.625f,
                      0.7916667f, 0.625f,
                      0.7916667f, 0.625f,
                      0.7916667f, 0.625f,
                      0.7916667f, 0.625f,
                      0.875f, 0.625f,
                      0.875f, 0.625f,
                      0.875f, 0.625f,
                      0.875f, 0.625f,
                      0.875f, 0.625f,
                      0.875f, 0.625f,
                      0.9583333f, 0.625f,
                      0.9583333f, 0.625f,
                      0.9583333f, 0.625f,
                      0.9583333f, 0.625f,
                      0.9583333f, 0.625f,
                      0.9583333f, 0.625f,
                      0.04166667f, 0.7083333f,
                      0.04166667f, 0.7083333f,
                      0.04166667f, 0.7083333f,
                      0.04166667f, 0.7083333f,
                      0.04166667f, 0.7083333f,
                      0.04166667f, 0.7083333f,
                      0.125f, 0.7083333f,
                      0.125f, 0.7083333f,
                      0.125f, 0.7083333f,
                      0.125f, 0.7083333f,
                      0.125f, 0.7083333f,
                      0.125f, 0.7083333f,
                      0.20833333f, 0.7083333f,
                      0.20833333f, 0.7083333f,
                      0.20833333f, 0.7083333f,
                      0.20833333f, 0.7083333f,
                      0.20833333f, 0.7083333f,
                      0.20833333f, 0.7083333f,
                      0.29166666f, 0.7083333f,
                      0.29166666f, 0.7083333f,
                      0.29166666f, 0.7083333f,
                      0.29166666f, 0.7083333f,
                      0.29166666f, 0.7083333f,
                      0.29166666f, 0.7083333f,
                      0.375f, 0.7083333f,
                      0.375f, 0.7083333f,
                      0.375f, 0.7083333f,
                      0.375f, 0.7083333f,
                      0.375f, 0.7083333f,
                      0.375f, 0.7083333f,
                      0.45833334f, 0.7083333f,
                      0.45833334f, 0.7083333f,
                      0.45833334f, 0.7083333f,
                      0.45833334f, 0.7083333f,
                      0.45833334f, 0.7083333f,
                      0.45833334f, 0.7083333f,
                      0.5416667f, 0.7083333f,
                      0.5416667f, 0.7083333f,
                      0.5416667f, 0.7083333f,
                      0.5416667f, 0.7083333f,
                      0.5416667f, 0.7083333f,
                      0.5416667f, 0.7083333f,
                      0.625f, 0.7083333f,
                      0.625f, 0.7083333f,
                      0.625f, 0.7083333f,
                      0.625f, 0.7083333f,
                      0.625f, 0.7083333f,
                      0.625f, 0.7083333f,
                      0.7083333f, 0.7083333f,
                      0.7083333f, 0.7083333f,
                      0.7083333f, 0.7083333f,
                      0.7083333f, 0.7083333f,
                      0.7083333f, 0.7083333f,
                      0.7083333f, 0.7083333f,
                      0.7916667f, 0.7083333f,
                      0.7916667f, 0.7083333f,
                      0.7916667f, 0.7083333f,
                      0.7916667f, 0.7083333f,
                      0.7916667f, 0.7083333f,
                      0.7916667f, 0.7083333f,
                      0.875f, 0.7083333f,
                      0.875f, 0.7083333f,
                      0.875f, 0.7083333f,
                      0.875f, 0.7083333f,
                      0.875f, 0.7083333f,
                      0.875f, 0.7083333f,
                      0.9583333f, 0.7083333f,
                      0.9583333f, 0.7083333f,
                      0.9583333f, 0.7083333f,
                      0.9583333f, 0.7083333f,
                      0.9583333f, 0.7083333f,
                      0.9583333f, 0.7083333f,
                      0.04166667f, 0.7916667f,
                      0.04166667f, 0.7916667f,
                      0.04166667f, 0.7916667f,
                      0.04166667f, 0.7916667f,
                      0.04166667f, 0.7916667f,
                      0.04166667f, 0.7916667f,
                      0.125f, 0.7916667f,
                      0.125f, 0.7916667f,
                      0.125f, 0.7916667f,
                      0.125f, 0.7916667f,
                      0.125f, 0.7916667f,
                      0.125f, 0.7916667f,
                      0.20833333f, 0.7916667f,
                      0.20833333f, 0.7916667f,
                      0.20833333f, 0.7916667f,
                      0.20833333f, 0.7916667f,
                      0.20833333f, 0.7916667f,
                      0.20833333f, 0.7916667f,
                      0.29166666f, 0.7916667f,
                      0.29166666f, 0.7916667f,
                      0.29166666f, 0.7916667f,
                      0.29166666f, 0.7916667f,
                      0.29166666f, 0.7916667f,
                      0.29166666f, 0.7916667f,
                      0.375f, 0.7916667f,
                      0.375f, 0.7916667f,
                      0.375f, 0.7916667f,
                      0.375f, 0.7916667f,
                      0.375f, 0.7916667f,
                      0.375f, 0.7916667f,
                      0.45833334f, 0.7916667f,
                      0.45833334f, 0.7916667f,
                      0.45833334f, 0.7916667f,
                      0.45833334f, 0.7916667f,
                      0.45833334f, 0.7916667f,
                      0.45833334f, 0.7916667f,
                      0.5416667f, 0.7916667f,
                      0.5416667f, 0.7916667f,
                      0.5416667f, 0.7916667f,
                      0.5416667f, 0.7916667f,
                      0.5416667f, 0.7916667f,
                      0.5416667f, 0.7916667f,
                      0.625f, 0.7916667f,
                      0.625f, 0.7916667f,
                      0.625f, 0.7916667f,
                      0.625f, 0.7916667f,
                      0.625f, 0.7916667f,
                      0.625f, 0.7916667f,
                      0.7083333f, 0.7916667f,
                      0.7083333f, 0.7916667f,
                      0.7083333f, 0.7916667f,
                      0.7083333f, 0.7916667f,
                      0.7083333f, 0.7916667f,
                      0.7083333f, 0.7916667f,
                      0.7916667f, 0.7916667f,
                      0.7916667f, 0.7916667f,
                      0.7916667f, 0.7916667f,
                      0.7916667f, 0.7916667f,
                      0.7916667f, 0.7916667f,
                      0.7916667f, 0.7916667f,
                      0.875f, 0.7916667f,
                      0.875f, 0.7916667f,
                      0.875f, 0.7916667f,
                      0.875f, 0.7916667f,
                      0.875f, 0.7916667f,
                      0.875f, 0.7916667f,
                      0.9583333f, 0.7916667f,
                      0.9583333f, 0.7916667f,
                      0.9583333f, 0.7916667f,
                      0.9583333f, 0.7916667f,
                      0.9583333f, 0.7916667f,
                      0.9583333f, 0.7916667f,
                      0.04166667f, 0.875f,
                      0.04166667f, 0.875f,
                      0.04166667f, 0.875f,
                      0.04166667f, 0.875f,
                      0.04166667f, 0.875f,
                      0.04166667f, 0.875f,
                      0.125f, 0.875f,
                      0.125f, 0.875f,
                      0.125f, 0.875f,
                      0.125f, 0.875f,
                      0.125f, 0.875f,
                      0.125f, 0.875f,
                      0.20833333f, 0.875f,
                      0.20833333f, 0.875f,
                      0.20833333f, 0.875f,
                      0.20833333f, 0.875f,
                      0.20833333f, 0.875f,
                      0.20833333f, 0.875f,
                      0.29166666f, 0.875f,
                      0.29166666f, 0.875f,
                      0.29166666f, 0.875f,
                      0.29166666f, 0.875f,
                      0.29166666f, 0.875f,
                      0.29166666f, 0.875f,
                      0.375f, 0.875f,
                      0.375f, 0.875f,
                      0.375f, 0.875f,
                      0.375f, 0.875f,
                      0.375f, 0.875f,
                      0.375f, 0.875f,
                      0.45833334f, 0.875f,
                      0.45833334f, 0.875f,
                      0.45833334f, 0.875f,
                      0.45833334f, 0.875f,
                      0.45833334f, 0.875f,
                      0.45833334f, 0.875f,
                      0.5416667f, 0.875f,
                      0.5416667f, 0.875f,
                      0.5416667f, 0.875f,
                      0.5416667f, 0.875f,
                      0.5416667f, 0.875f,
                      0.5416667f, 0.875f,
                      0.625f, 0.875f,
                      0.625f, 0.875f,
                      0.625f, 0.875f,
                      0.625f, 0.875f,
                      0.625f, 0.875f,
                      0.625f, 0.875f,
                      0.7083333f, 0.875f,
                      0.7083333f, 0.875f,
                      0.7083333f, 0.875f,
                      0.7083333f, 0.875f,
                      0.7083333f, 0.875f,
                      0.7083333f, 0.875f,
                      0.7916667f, 0.875f,
                      0.7916667f, 0.875f,
                      0.7916667f, 0.875f,
                      0.7916667f, 0.875f,
                      0.7916667f, 0.875f,
                      0.7916667f, 0.875f,
                      0.875f, 0.875f,
                      0.875f, 0.875f,
                      0.875f, 0.875f,
                      0.875f, 0.875f,
                      0.875f, 0.875f,
                      0.875f, 0.875f,
                      0.9583333f, 0.875f,
                      0.9583333f, 0.875f,
                      0.9583333f, 0.875f,
                      0.9583333f, 0.875f,
                      0.9583333f, 0.875f,
                      0.9583333f, 0.875f,
                      0.04166667f, 0.9583333f,
                      0.04166667f, 0.9583333f,
                      0.04166667f, 0.9583333f,
                      0.04166667f, 0.9583333f,
                      0.04166667f, 0.9583333f,
                      0.04166667f, 0.9583333f,
                      0.125f, 0.9583333f,
                      0.125f, 0.9583333f,
                      0.125f, 0.9583333f,
                      0.125f, 0.9583333f,
                      0.125f, 0.9583333f,
                      0.125f, 0.9583333f,
                      0.20833333f, 0.9583333f,
                      0.20833333f, 0.9583333f,
                      0.20833333f, 0.9583333f,
                      0.20833333f, 0.9583333f,
                      0.20833333f, 0.9583333f,
                      0.20833333f, 0.9583333f,
                      0.29166666f, 0.9583333f,
                      0.29166666f, 0.9583333f,
                      0.29166666f, 0.9583333f,
                      0.29166666f, 0.9583333f,
                      0.29166666f, 0.9583333f,
                      0.29166666f, 0.9583333f,
                      0.375f, 0.9583333f,
                      0.375f, 0.9583333f,
                      0.375f, 0.9583333f,
                      0.375f, 0.9583333f,
                      0.375f, 0.9583333f,
                      0.375f, 0.9583333f,
                      0.45833334f, 0.9583333f,
                      0.45833334f, 0.9583333f,
                      0.45833334f, 0.9583333f,
                      0.45833334f, 0.9583333f,
                      0.45833334f, 0.9583333f,
                      0.45833334f, 0.9583333f,
                      0.5416667f, 0.9583333f,
                      0.5416667f, 0.9583333f,
                      0.5416667f, 0.9583333f,
                      0.5416667f, 0.9583333f,
                      0.5416667f, 0.9583333f,
                      0.5416667f, 0.9583333f,
                      0.625f, 0.9583333f,
                      0.625f, 0.9583333f,
                      0.625f, 0.9583333f,
                      0.625f, 0.9583333f,
                      0.625f, 0.9583333f,
                      0.625f, 0.9583333f,
                      0.7083333f, 0.9583333f,
                      0.7083333f, 0.9583333f,
                      0.7083333f, 0.9583333f,
                      0.7083333f, 0.9583333f,
                      0.7083333f, 0.9583333f,
                      0.7083333f, 0.9583333f,
                      0.7916667f, 0.9583333f,
                      0.7916667f, 0.9583333f,
                      0.7916667f, 0.9583333f,
                      0.7916667f, 0.9583333f,
                      0.7916667f, 0.9583333f,
                      0.7916667f, 0.9583333f,
                      0.875f, 0.9583333f,
                      0.875f, 0.9583333f,
                      0.875f, 0.9583333f,
                      0.875f, 0.9583333f,
                      0.875f, 0.9583333f,
                      0.875f, 0.9583333f,
                      0.9583333f, 0.9583333f,
                      0.9583333f, 0.9583333f,
                      0.9583333f, 0.9583333f,
                      0.9583333f, 0.9583333f,
                      0.9583333f, 0.9583333f,
                      0.9583333f, 0.9583333f
                 };

                anchors.put(0, 0, anchors_arr);

                return anchors;
            }
        }

        private class HandPoseEstimator
        {
            float conf_threshold;
            int backend;
            int target;

            Net handpose_estimation_net;

            Size input_size = new Size(224, 224);
            //int[] PALM_LANDMARK_IDS = new int[] { 0, 5, 9, 13, 17, 1, 2 };
            int PALM_LANDMARKS_INDEX_OF_PALM_BASE = 0;
            int PALM_LANDMARKS_INDEX_OF_MIDDLE_FINGER_BASE = 2;
            Point PALM_BOX_SHIFT_VECTOR = new Point(0, -0.4);
            double PALM_BOX_ENLARGE_FACTOR = 3.0;
            Point HAND_BOX_SHIFT_VECTOR = new Point(0, -0.1);
            double HAND_BOX_ENLARGE_FACTOR = 1.65;

            Mat tmpImage;

            public HandPoseEstimator(string modelFilepath, float confThreshold = 0.8f, int backend = Dnn.DNN_BACKEND_OPENCV, int target = Dnn.DNN_TARGET_CPU)
            {
                // initialize
                if (!string.IsNullOrEmpty(modelFilepath))
                {
                    handpose_estimation_net = Dnn.readNet(modelFilepath);
                }

                conf_threshold = Mathf.Clamp01(confThreshold);
                this.backend = backend;
                this.target = target;

                handpose_estimation_net.setPreferableBackend(this.backend);
                handpose_estimation_net.setPreferableTarget(this.target);
            }

            protected virtual Mat preprocess(Mat image, Mat palm, out Mat rotated_palm_bbox, out double angle, out Mat rotation_matrix)
            {
                // Rotate input to have vertically oriented hand image
                // compute rotation
                Mat palm_bbox = palm.colRange(new OpenCVRange(0, 4)).reshape(1, 2);
                Mat palm_landmarks = palm.colRange(new OpenCVRange(4, 18)).reshape(1, 7);

                Mat p1 = palm_landmarks.row(PALM_LANDMARKS_INDEX_OF_PALM_BASE);
                Mat p2 = palm_landmarks.row(PALM_LANDMARKS_INDEX_OF_MIDDLE_FINGER_BASE);
                float[] p1_arr = new float[2];
                p1.get(0, 0, p1_arr);
                float[] p2_arr = new float[2];
                p2.get(0, 0, p2_arr);
                double radians = Math.PI / 2 - Math.Atan2(-(p2_arr[1] - p1_arr[1]), p2_arr[0] - p1_arr[0]);
                radians = radians - 2 * Math.PI * Math.Floor((radians + Math.PI) / (2 * Math.PI));
                angle = Mathf.Rad2Deg * radians;

                // get bbox center
                float[] palm_bbox_arr = new float[4];
                palm_bbox.get(0, 0, palm_bbox_arr);
                Point center_palm_bbox = new Point((palm_bbox_arr[0] + palm_bbox_arr[2]) / 2, (palm_bbox_arr[1] + palm_bbox_arr[3]) / 2);

                // get rotation matrix
                rotation_matrix = Imgproc.getRotationMatrix2D(center_palm_bbox, angle, 1.0);

                // get rotated image
                Mat rotated_image = new Mat();
                Imgproc.warpAffine(image, rotated_image, rotation_matrix, image.size());

                // get bounding boxes from rotated palm landmarks
                Mat rotated_palm_landmarks = new Mat(2, 7, CvType.CV_32FC1);
                Mat _a = new Mat(1, 3, CvType.CV_64FC1);
                Mat _b = new Mat(1, 3, CvType.CV_64FC1);
                float[] _a_arr = new float[2];
                double[] _b_arr = new double[3];

                Point[] rotated_palm_landmarks_points = new Point[7];

                for (int i = 0; i < 7; ++i)
                {
                    palm_landmarks.get(i, 0, _a_arr);
                    _a.put(0, 0, new double[] { _a_arr[0], _a_arr[1], 1f });
                    rotation_matrix.get(0, 0, _b_arr);
                    _b.put(0, 0, new double[] { _b_arr[0], _b_arr[1], _b_arr[2] });
                    double x = _a.dot(_b);
                    rotated_palm_landmarks.put(0, i, new float[] { (float)x });

                    rotation_matrix.get(1, 0, _b_arr);
                    _b.put(0, 0, new double[] { _b_arr[0], _b_arr[1], _b_arr[2] });
                    double y = _a.dot(_b);
                    rotated_palm_landmarks.put(1, i, new float[] { (float)y });

                    rotated_palm_landmarks_points[i] = new Point(x, y);
                }

                // get landmark bounding box
                MatOfPoint points = new MatOfPoint(rotated_palm_landmarks_points);
                OpenCVRect _rotated_palm_bbox = Imgproc.boundingRect(points);
                rotated_palm_bbox = new Mat(2, 2, CvType.CV_64FC1);

                // shift bounding box
                Point _rotated_palm_bbox_tl = _rotated_palm_bbox.tl();
                Point _rotated_palm_bbox_br = _rotated_palm_bbox.br();
                Point wh_rotated_palm_bbox = _rotated_palm_bbox_br - _rotated_palm_bbox_tl;
                Point shift_vector = new Point(PALM_BOX_SHIFT_VECTOR.x * wh_rotated_palm_bbox.x, PALM_BOX_SHIFT_VECTOR.y * wh_rotated_palm_bbox.y);

                _rotated_palm_bbox_tl = _rotated_palm_bbox_tl + shift_vector;
                _rotated_palm_bbox_br = _rotated_palm_bbox_br + shift_vector;

                // squarify bounding boxx
                Point center_rotated_plam_bbox = new Point((_rotated_palm_bbox_tl.x + _rotated_palm_bbox_br.x) / 2, (_rotated_palm_bbox_tl.y + _rotated_palm_bbox_br.y) / 2);
                wh_rotated_palm_bbox = _rotated_palm_bbox_br - _rotated_palm_bbox_tl;
                double new_half_size = Math.Max(wh_rotated_palm_bbox.x, wh_rotated_palm_bbox.y) / 2.0;
                _rotated_palm_bbox_tl = new Point(center_rotated_plam_bbox.x - new_half_size, center_rotated_plam_bbox.y - new_half_size);
                _rotated_palm_bbox_br = new Point(center_rotated_plam_bbox.x + new_half_size, center_rotated_plam_bbox.y + new_half_size);

                // enlarge bounding box
                center_rotated_plam_bbox = new Point((_rotated_palm_bbox_tl.x + _rotated_palm_bbox_br.x) / 2, (_rotated_palm_bbox_tl.y + _rotated_palm_bbox_br.y) / 2);
                wh_rotated_palm_bbox = _rotated_palm_bbox_br - _rotated_palm_bbox_tl;
                Point new_half_size2 = new Point(wh_rotated_palm_bbox.x * PALM_BOX_ENLARGE_FACTOR / 2.0, wh_rotated_palm_bbox.y * PALM_BOX_ENLARGE_FACTOR / 2.0);
                _rotated_palm_bbox_tl = new Point(center_rotated_plam_bbox.x - new_half_size2.x, center_rotated_plam_bbox.y - new_half_size2.x);
                _rotated_palm_bbox_br = new Point(center_rotated_plam_bbox.x + new_half_size2.x, center_rotated_plam_bbox.y + new_half_size2.x);
                rotated_palm_bbox.put(0, 0, new double[] { _rotated_palm_bbox_tl.x, _rotated_palm_bbox_tl.y, _rotated_palm_bbox_br.x, _rotated_palm_bbox_br.y });

                // Crop and resize the rotated image by the bounding box
                int[] diff = new int[] {
                    Math.Max((int)-_rotated_palm_bbox_tl.x, 0),
                    Math.Max((int)-_rotated_palm_bbox_tl.y, 0),
                    Math.Max((int)_rotated_palm_bbox_br.x - rotated_image.width(), 0),
                    Math.Max((int)_rotated_palm_bbox_br.y - rotated_image.height(), 0)
                };
                Point tl = new Point(_rotated_palm_bbox_tl.x + diff[0], _rotated_palm_bbox_tl.y + diff[1]);
                Point br = new Point(_rotated_palm_bbox_br.x + diff[2], _rotated_palm_bbox_br.y + diff[3]);
                OpenCVRect rotated_palm_bbox_rect = new OpenCVRect(tl, br);
                OpenCVRect rotated_image_rect = new OpenCVRect(0, 0, rotated_image.width(), rotated_image.height());
                Mat crop = new Mat(rotated_image, rotated_image_rect.intersect(rotated_palm_bbox_rect));

                //
                //Core.copyMakeBorder(crop, crop, diff[1], diff[3], diff[0], diff[2], Core.BORDER_CONSTANT, Scalar.all(0));
                //Mat blob = Dnn.blobFromImage(crop, 1.0 / 255.0, input_size, new Scalar(0, 0, 0), true, false, CvType.CV_32F);
                //
                // or
                //
                int tmpImageSize = (int)(Math.Max(image.width(), image.height()) * 1.5);
                if (tmpImage != null && (tmpImage.width() != tmpImageSize || tmpImage.height() != tmpImageSize))
                {
                    tmpImage.Dispose();
                    tmpImage = null;
                }
                if (tmpImage == null)
                {
                    tmpImage = new Mat(tmpImageSize, tmpImageSize, image.type(), Scalar.all(0));
                }
                Mat _tmpImage_crop = new Mat(tmpImage, new OpenCVRect(0, 0, diff[0] + crop.width() + diff[2], diff[1] + crop.height() + diff[3]));
                Imgproc.rectangle(_tmpImage_crop, new OpenCVRect(0, 0, _tmpImage_crop.width(), _tmpImage_crop.height()), Scalar.all(0));
                Mat _tmpImage_crop2 = new Mat(tmpImage, new OpenCVRect(diff[0], diff[1], crop.width(), crop.height()));
                crop.copyTo(_tmpImage_crop2);

                Mat blob = Dnn.blobFromImage(_tmpImage_crop, 1.0 / 255.0, input_size, new Scalar(0, 0, 0), true, false, CvType.CV_32F);
                //

                // NCHW => NHWC
                Core.transposeND(blob, new MatOfInt(0, 2, 3, 1), blob);

                rotated_image.Dispose();

                return blob;
            }

            public virtual Mat infer(Mat image, Mat palm)
            {
                // Preprocess
                Mat rotated_palm_bbox;
                double angle;
                Mat rotation_matrix;
                Mat input_blob = preprocess(image, palm, out rotated_palm_bbox, out angle, out rotation_matrix);

                // Forward
                handpose_estimation_net.setInput(input_blob);
                List<Mat> output_blob = new List<Mat>();
                handpose_estimation_net.forward(output_blob, handpose_estimation_net.getUnconnectedOutLayersNames());

                // Postprocess
                Mat results = postprocess(output_blob, rotated_palm_bbox, angle, rotation_matrix);

                input_blob.Dispose();
                for (int i = 0; i < output_blob.Count; i++)
                {
                    output_blob[i].Dispose();
                }

                return results;// [bbox_coords, landmarks_coords, landmarks_coords_world, handedness, conf]
            }

            protected virtual Mat postprocess(List<Mat> output_blob, Mat rotated_palm_bbox, double angle, Mat rotation_matrix)
            {
                Mat landmarks = output_blob[0];
                float conf = (float)output_blob[1].get(0, 0)[0];
                float handedness = (float)output_blob[2].get(0, 0)[0];
                Mat landmarks_world = output_blob[3];

                if (conf < conf_threshold)
                    return new Mat();

                landmarks = landmarks.reshape(1, 21); // shape: (1, 63) -> (21, 3)
                landmarks_world = landmarks_world.reshape(1, 21); // shape: (1, 63) -> (21, 3)

                // transform coords back to the input coords
                double[] rotated_palm_bbox_arr = new double[4];
                rotated_palm_bbox.get(0, 0, rotated_palm_bbox_arr);
                Point _rotated_palm_bbox_tl = new Point(rotated_palm_bbox_arr[0], rotated_palm_bbox_arr[1]);
                Point _rotated_palm_bbox_br = new Point(rotated_palm_bbox_arr[2], rotated_palm_bbox_arr[3]);
                Point wh_rotated_palm_bbox = _rotated_palm_bbox_br - _rotated_palm_bbox_tl;
                Point scale_factor = new Point(wh_rotated_palm_bbox.x / input_size.width, wh_rotated_palm_bbox.y / input_size.height);

                Mat _landmarks_21x1_c3 = landmarks.reshape(3, 21);
                Core.subtract(_landmarks_21x1_c3, new Scalar(input_size.width / 2.0, input_size.height / 2.0, 0.0), _landmarks_21x1_c3);
                double max_scale_factor = Math.Max(scale_factor.x, scale_factor.y);
                Core.multiply(_landmarks_21x1_c3, new Scalar(scale_factor.x, scale_factor.y, max_scale_factor), _landmarks_21x1_c3); //  # depth scaling

                Mat coords_rotation_matrix = Imgproc.getRotationMatrix2D(new Point(0, 0), angle, 1.0);

                Mat rotated_landmarks = landmarks.clone();
                Mat _a = new Mat(1, 2, CvType.CV_64FC1);
                Mat _b = new Mat(1, 2, CvType.CV_64FC1);
                float[] _a_arr = new float[2];
                double[] _b_arr = new double[6];
                coords_rotation_matrix.get(0, 0, _b_arr);

                for (int i = 0; i < 21; ++i)
                {
                    landmarks.get(i, 0, _a_arr);
                    _a.put(0, 0, new double[] { _a_arr[0], _a_arr[1] });

                    _b.put(0, 0, new double[] { _b_arr[0], _b_arr[3] });
                    rotated_landmarks.put(i, 0, new float[] { (float)_a.dot(_b) });
                    _b.put(0, 0, new double[] { _b_arr[1], _b_arr[4] });
                    rotated_landmarks.put(i, 1, new float[] { (float)_a.dot(_b) });
                }

                Mat rotated_landmarks_world = landmarks_world.clone();
                for (int i = 0; i < 21; ++i)
                {
                    landmarks_world.get(i, 0, _a_arr);
                    _a.put(0, 0, new double[] { _a_arr[0], _a_arr[1] });

                    _b.put(0, 0, new double[] { _b_arr[0], _b_arr[3] });
                    rotated_landmarks_world.put(i, 0, new float[] { (float)_a.dot(_b) });
                    _b.put(0, 0, new double[] { _b_arr[1], _b_arr[4] });
                    rotated_landmarks_world.put(i, 1, new float[] { (float)_a.dot(_b) });
                }

                // invert rotation
                double[] rotation_matrix_arr = new double[6];
                rotation_matrix.get(0, 0, rotation_matrix_arr);
                Mat rotation_component = new Mat(2, 2, CvType.CV_64FC1);
                rotation_component.put(0, 0, new double[] { rotation_matrix_arr[0], rotation_matrix_arr[3], rotation_matrix_arr[1], rotation_matrix_arr[4] });
                Mat translation_component = new Mat(2, 1, CvType.CV_64FC1);
                translation_component.put(0, 0, new double[] { rotation_matrix_arr[2], rotation_matrix_arr[5] });
                Mat inverted_translation = new Mat(2, 1, CvType.CV_64FC1);
                inverted_translation.put(0, 0, new double[] { -rotation_component.row(0).dot(translation_component.reshape(1, 1)), -rotation_component.row(1).dot(translation_component.reshape(1, 1)) });

                Mat inverse_rotation_matrix = new Mat(2, 3, CvType.CV_64FC1);
                rotation_component.copyTo(inverse_rotation_matrix.colRange(new OpenCVRange(0, 2)));
                inverted_translation.copyTo(inverse_rotation_matrix.colRange(new OpenCVRange(2, 3)));

                // get box center
                Mat center = new Mat(3, 1, CvType.CV_64FC1);
                center.put(0, 0, new double[] { (rotated_palm_bbox_arr[0] + rotated_palm_bbox_arr[2]) / 2.0, (rotated_palm_bbox_arr[1] + rotated_palm_bbox_arr[3]) / 2.0, 1.0 });
                Mat original_center = new Mat(2, 1, CvType.CV_64FC1);
                original_center.put(0, 0, new double[] { inverse_rotation_matrix.row(0).dot(center.reshape(1, 1)), inverse_rotation_matrix.row(1).dot(center.reshape(1, 1)) });
                Core.add(rotated_landmarks.reshape(3, 21), new Scalar(original_center.get(0, 0)[0], original_center.get(1, 0)[0], 0.0), landmarks.reshape(3, 21));

                // get bounding box from rotated_landmarks
                Point[] landmarks_points = new Point[21];
                for (int i = 0; i < 21; ++i)
                {
                    landmarks.get(i, 0, _a_arr);
                    landmarks_points[i] = new Point(_a_arr[0], _a_arr[1]);
                }
                MatOfPoint points = new MatOfPoint(landmarks_points);
                OpenCVRect bbox = Imgproc.boundingRect(points);

                // shift bounding box
                Point wh_bbox = bbox.br() - bbox.tl();
                Point shift_vector = new Point(HAND_BOX_SHIFT_VECTOR.x * wh_bbox.x, HAND_BOX_SHIFT_VECTOR.y * wh_bbox.y);
                bbox = bbox + shift_vector;

                // enlarge bounding box
                Point center_bbox = new Point((bbox.tl().x + bbox.br().x) / 2, (bbox.tl().y + bbox.br().y) / 2);
                wh_bbox = bbox.br() - bbox.tl();
                Point new_half_size = new Point(wh_bbox.x * HAND_BOX_ENLARGE_FACTOR / 2.0, wh_bbox.y * HAND_BOX_ENLARGE_FACTOR / 2.0);
                bbox = new OpenCVRect(new Point(center_bbox.x - new_half_size.x, center_bbox.y - new_half_size.y), new Point(center_bbox.x + new_half_size.x, center_bbox.y + new_half_size.y));


                Mat results = new Mat(132, 1, CvType.CV_32FC1);
                results.put(0, 0, new float[] { (float)bbox.tl().x, (float)bbox.tl().y, (float)bbox.br().x, (float)bbox.br().y });
                Mat results_col4_67_21x3 = results.rowRange(new OpenCVRange(4, 67)).reshape(1, 21);
                landmarks.colRange(new OpenCVRange(0, 3)).copyTo(results_col4_67_21x3);
                Mat results_col67_130_21x3 = results.rowRange(new OpenCVRange(67, 130)).reshape(1, 21);
                rotated_landmarks_world.colRange(new OpenCVRange(0, 3)).copyTo(results_col67_130_21x3);
                results.put(130, 0, new float[] { handedness });
                results.put(131, 0, new float[] { conf });

                // # [0: 4]: hand bounding box found in image of format [x1, y1, x2, y2] (top-left and bottom-right points)
                // # [4: 67]: screen landmarks with format [x1, y1, z1, x2, y2 ... x21, y21, z21], z value is relative to WRIST
                // # [67: 130]: world landmarks with format [x1, y1, z1, x2, y2 ... x21, y21, z21], 3D metric x, y, z coordinate
                // # [130]: handedness, (left)[0, 1](right) hand
                // # [131]: confidence
                return results;//np.r_[bbox.reshape(-1), landmarks.reshape(-1), rotated_landmarks_world.reshape(-1), handedness[0][0], conf]
            }

            public virtual void visualize(Mat image, List<Mat> results, bool print_results = false, bool isRGB = false)
            {
                if (image.IsDisposed)
                    return;

                if (results.Count < 1)
                    return;

                StringBuilder sb = null;

                if (print_results)
                    sb = new StringBuilder();

                Scalar line_color = new Scalar(255, 255, 255, 255);
                Scalar point_color = (isRGB) ? new Scalar(255, 0, 0, 255) : new Scalar(0, 0, 255, 255);
                for (int i = 0; i < results.Count; ++i)
                {
                    Mat result = results[i];

                    if (result.empty() || result.rows() < 132)
                        continue;

                    float[] conf = new float[1];
                    result.get(131, 0, conf);
                    float[] handedness = new float[1];
                    result.get(130, 0, handedness);
                    string handedness_text = (handedness[0] <= 0.5f) ? "Left" : "Right";
                    float[] bbox = new float[4];
                    result.get(0, 0, bbox);

                    Mat results_col4_67_21x3 = result.rowRange(new OpenCVRange(4, 67)).reshape(1, 21);
                    float[] landmarks_screen_xy = new float[42];
                    results_col4_67_21x3.colRange(new OpenCVRange(0, 2)).get(0, 0, landmarks_screen_xy);

                    float[] landmarks_screen_xyz = new float[63];
                    results_col4_67_21x3.get(0, 0, landmarks_screen_xyz);

                    Mat results_col67_130_21x3 = result.rowRange(new OpenCVRange(67, 130)).reshape(1, 21);
                    float[] landmarks_world = new float[63];
                    results_col67_130_21x3.get(0, 0, landmarks_world);

                    // # draw box
                    Imgproc.rectangle(image, new Point(bbox[0], bbox[1]), new Point(bbox[2], bbox[3]), new Scalar(0, 255, 0, 255), 2);

                    // # draw handedness
                    Imgproc.putText(image, handedness_text, new Point(bbox[0], bbox[1] + 12), Imgproc.FONT_HERSHEY_DUPLEX, 0.5, point_color);

                    // # Draw line between each key points
                    draw_lines(landmarks_screen_xy, false);

                    // # z value is relative to WRIST
                    for (int j = 0; j < 63; j += 3)
                    {
                        int r = Mathf.Max((int)(5 - landmarks_screen_xyz[j + 2] / 5), 0);
                        r = Mathf.Min(r, 14);
                        Imgproc.circle(image, new Point(landmarks_screen_xyz[j], landmarks_screen_xyz[j + 1]), r, point_color, -1);
                    }


                    // Print results
                    if (print_results)
                    {
                        sb.AppendLine(String.Format("-----------hand {0}-----------", i + 1));
                        sb.AppendLine(String.Format("conf: {0:0.00}", conf[0]));
                        sb.AppendLine("handedness: " + handedness_text);
                        sb.AppendLine(String.Format("hand box: {0:0} {1:0} {2:0} {3:0}", bbox[0], bbox[1], bbox[2], bbox[3]));
                        sb.AppendLine("hand landmarks: ");
                        foreach (var p in landmarks_screen_xyz)
                        {
                            sb.Append(String.Format("{0:0} ", p));
                        }
                        sb.AppendLine();
                        sb.AppendLine("hand world landmarks: ");
                        foreach (var p in landmarks_world)
                        {
                            sb.Append(String.Format("{0:0.000000} ", p));
                        }
                    }
                }

                if (print_results)
                    Debug.Log(sb);


                void draw_lines(float[] landmarks, bool is_draw_point = true, int thickness = 2)
                {
                    // Draw line between each key points
                    Imgproc.line(image, new Point(landmarks[0], landmarks[1]), new Point(landmarks[2], landmarks[3]), line_color, thickness);
                    Imgproc.line(image, new Point(landmarks[2], landmarks[3]), new Point(landmarks[4], landmarks[5]), line_color, thickness);
                    Imgproc.line(image, new Point(landmarks[4], landmarks[5]), new Point(landmarks[6], landmarks[7]), line_color, thickness);
                    Imgproc.line(image, new Point(landmarks[6], landmarks[7]), new Point(landmarks[8], landmarks[9]), line_color, thickness);

                    Imgproc.line(image, new Point(landmarks[0], landmarks[1]), new Point(landmarks[10], landmarks[11]), line_color, thickness);
                    Imgproc.line(image, new Point(landmarks[10], landmarks[11]), new Point(landmarks[12], landmarks[13]), line_color, thickness);
                    Imgproc.line(image, new Point(landmarks[12], landmarks[13]), new Point(landmarks[14], landmarks[15]), line_color, thickness);
                    Imgproc.line(image, new Point(landmarks[14], landmarks[15]), new Point(landmarks[16], landmarks[17]), line_color, thickness);

                    Imgproc.line(image, new Point(landmarks[0], landmarks[1]), new Point(landmarks[18], landmarks[19]), line_color, thickness);
                    Imgproc.line(image, new Point(landmarks[18], landmarks[19]), new Point(landmarks[20], landmarks[21]), line_color, thickness);
                    Imgproc.line(image, new Point(landmarks[20], landmarks[21]), new Point(landmarks[22], landmarks[23]), line_color, thickness);
                    Imgproc.line(image, new Point(landmarks[22], landmarks[23]), new Point(landmarks[24], landmarks[25]), line_color, thickness);

                    Imgproc.line(image, new Point(landmarks[0], landmarks[1]), new Point(landmarks[26], landmarks[27]), line_color, thickness);
                    Imgproc.line(image, new Point(landmarks[26], landmarks[27]), new Point(landmarks[28], landmarks[29]), line_color, thickness);
                    Imgproc.line(image, new Point(landmarks[28], landmarks[29]), new Point(landmarks[30], landmarks[31]), line_color, thickness);
                    Imgproc.line(image, new Point(landmarks[30], landmarks[31]), new Point(landmarks[32], landmarks[33]), line_color, thickness);

                    Imgproc.line(image, new Point(landmarks[0], landmarks[1]), new Point(landmarks[34], landmarks[35]), line_color, thickness);
                    Imgproc.line(image, new Point(landmarks[34], landmarks[35]), new Point(landmarks[36], landmarks[37]), line_color, thickness);
                    Imgproc.line(image, new Point(landmarks[36], landmarks[37]), new Point(landmarks[38], landmarks[39]), line_color, thickness);
                    Imgproc.line(image, new Point(landmarks[38], landmarks[39]), new Point(landmarks[40], landmarks[41]), line_color, thickness);

                    if (is_draw_point)
                    {
                        for (int j = 0; j < 42; j += 2)
                        {
                            Imgproc.circle(image, new Point(landmarks[j], landmarks[j + 1]), 2, point_color, -1);
                        }
                    }
                }
            }

            public virtual void dispose()
            {
                if (handpose_estimation_net != null)
                    handpose_estimation_net.Dispose();

                if (tmpImage != null)
                    tmpImage.Dispose();
            }
        }
    }
}
#endif

#endif