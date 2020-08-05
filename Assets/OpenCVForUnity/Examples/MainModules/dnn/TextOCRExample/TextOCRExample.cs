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
    /// Test OCR Example
    /// This example demonstrates text detection and recognition with dnn.
    /// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/text_detection.cpp.
    /// </summary>
    public class TextOCRExample : MonoBehaviour
    {

        [TooltipAttribute("Preprocess input image by resizing to a specific width. It should be multiple by 32.")]
        public float inpWidth = 320f;

        [TooltipAttribute("Preprocess input image by resizing to a specific height. It should be multiple by 32.")]
        public float inpHeight = 320f;

        [TooltipAttribute("Confidence threshold.")]
        public float confThreshold = 0.5f;

        [TooltipAttribute("Confidence threshold.")]
        public float nmsThreshold = 0.4f;

        /// <summary>
        /// IMAGE_FILENAME
        /// </summary>
        string IMAGE_FILENAME = "text/test_text.jpg";

        /// <summary>
        /// The image filepath.
        /// </summary>
        string image_filepath;

        /// <summary>
        /// Path to a binary .pb file contains trained detector network.
        /// </summary>
        string DETECTIONMODEL_FILENAME = "dnn/frozen_east_text_detection.pb";

        /// <summary>
        /// The detection model filepath.
        /// </summary>
        string detectionmodel_filepath;

        /// <summary>
        /// Path to a binary .pb or .onnx file contains trained recognition network.
        /// </summary>
        string RECOGNTIONMODEL_FILENAME = "dnn/crnn.onnx";

        /// <summary>
        /// The recognition model filepath.
        /// </summary>
        string recognitionmodel_filepath;

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
            image_filepath = Utils.getFilePath(IMAGE_FILENAME);
            detectionmodel_filepath = Utils.getFilePath(DETECTIONMODEL_FILENAME);
            recognitionmodel_filepath = Utils.getFilePath(RECOGNTIONMODEL_FILENAME);
            Run();
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator GetFilePath()
        {
            var getFilePathAsync_0_Coroutine = Utils.getFilePathAsync(IMAGE_FILENAME, (result) =>
            {
                image_filepath = result;
            });
            yield return getFilePathAsync_0_Coroutine;

            var getFilePathAsync_1_Coroutine = Utils.getFilePathAsync(DETECTIONMODEL_FILENAME, (result) =>
            {
                detectionmodel_filepath = result;
            });
            yield return getFilePathAsync_1_Coroutine;

            var getFilePathAsync_2_Coroutine = Utils.getFilePathAsync(RECOGNTIONMODEL_FILENAME, (result) =>
            {
                recognitionmodel_filepath = result;
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

            Mat img = Imgcodecs.imread(image_filepath, Imgcodecs.IMREAD_COLOR);
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


            Net detector = null;
            Net recognizer = null;

            if (string.IsNullOrEmpty(detectionmodel_filepath) || string.IsNullOrEmpty(recognitionmodel_filepath))
            {
                Debug.LogError(detectionmodel_filepath + " or " + recognitionmodel_filepath + " is not loaded. Please see \"StreamingAssets/dnn/setup_dnn_module.pdf\". ");
            }
            else
            {
                detector = Dnn.readNet(detectionmodel_filepath);
                recognizer = Dnn.readNet(recognitionmodel_filepath);
            }

            if (detector == null || recognizer == null)
            {
                Imgproc.putText(img, "model file is not loaded.", new Point(5, img.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(img, "Please read console message.", new Point(5, img.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255), 2, Imgproc.LINE_AA, false);
            }
            else
            {
                TickMeter tickMeter = new TickMeter();

                List<Mat> outs = new List<Mat>();
                List<string> outNames = new List<string>();
                outNames.Add("feature_fusion/Conv_7/Sigmoid");
                outNames.Add("feature_fusion/concat_3");

                // Create a 4D blob from a frame.
                Size inpSize = new Size(inpWidth > 0 ? inpWidth : img.cols(), inpHeight > 0 ? inpHeight : img.rows());
                Mat blob = Dnn.blobFromImage(img, 1.0, inpSize, new Scalar(123.68, 116.78, 103.94), true, false); // blobFromImage(frame, blob, 1.0, Size(inpWidth, inpHeight), Scalar(123.68, 116.78, 103.94), true, false);

                // Run detection model.
                detector.setInput(blob);
                tickMeter.start();
                detector.forward(outs, outNames);
                tickMeter.stop();

                Mat scores = outs[0];
                Mat geometry = outs[1];

                // Decode predicted bounding boxes.
                List<RotatedRect> boxes = new List<RotatedRect>();
                List<float> confidences = new List<float>();
                decodeBoundingBoxes(scores, geometry, confThreshold, boxes, confidences);


                // Apply non-maximum suppression procedure.
                MatOfRotatedRect boxesMat = new MatOfRotatedRect(boxes.ToArray());
                MatOfFloat confidencesMat = new MatOfFloat(confidences.ToArray());
                MatOfInt indicesMat = new MatOfInt();
                Dnn.NMSBoxesRotated(boxesMat, confidencesMat, confThreshold, nmsThreshold, indicesMat);

                List<int> indices = indicesMat.toList();
                Point ratio = new Point(img.cols() / inpWidth, img.rows() / inpHeight);

                // Render text.
                for (int i = 0; i < indices.Count; ++i)
                {
                    RotatedRect box = boxes[indices[i]];

                    Point[] vertices = new Point[4];
                    box.points(vertices);

                    for (int j = 0; j < 4; ++j)
                    {
                        vertices[j].x *= ratio.x;
                        vertices[j].y *= ratio.y;
                    }

                    for (int j = 0; j < 4; ++j)
                        Imgproc.line(img, vertices[j], vertices[(j + 1) % 4], new Scalar(0, 255, 0), 1);

                    if (recognizer != null)
                    {
                        Mat cropped = new Mat();
                        fourPointsTransform(img, vertices, cropped);

                        //Debug.Log(cropped);

                        Imgproc.cvtColor(cropped, cropped, Imgproc.COLOR_BGR2GRAY);

                        Mat blobCrop = Dnn.blobFromImage(cropped, 1.0 / 127.5, new Size(), Scalar.all(127.5));
                        recognizer.setInput(blobCrop);

                        //Debug.Log(blobCrop);

                        tickMeter.start();
                        Mat result = recognizer.forward();
                        tickMeter.stop();

                        string wordRecognized;
                        decodeText(result, out wordRecognized);
                        Imgproc.putText(img, wordRecognized, vertices[1], Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 0, 0), 1, Imgproc.LINE_AA, false);

                        Debug.Log(wordRecognized);


                        cropped.Dispose();
                        blobCrop.Dispose();
                        result.Dispose();
                    }
                }

                Debug.Log("Inference time, ms: " + tickMeter.getTimeMilli());

                for (int i = 0; i < outs.Count; i++)
                {
                    outs[i].Dispose();
                }
                blob.Dispose();
                detector.Dispose();
                recognizer.Dispose();
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

        protected void decodeBoundingBoxes(Mat scores, Mat geometry, float scoreThresh, List<RotatedRect> detections, List<float> confidences)
        {
            detections.Clear();

            //Debug.Log("scores.dims() = 4 : " + scores.dims());
            //Debug.Log("geometry.dims() = 4 : " + geometry.dims());
            //Debug.Log("scores.size(0) = 1 : " + scores.size(0));
            //Debug.Log("geometry..size(0) = 1 : " + geometry.size(0));
            //Debug.Log("scores.size(1) = 1 : " + scores.size(1));
            //Debug.Log("geometry..size(1) = 5 : " + geometry.size(1));
            //Debug.Log("scores.size(2) = geometry.size(2) : " + (scores.size(2) == geometry.size(2)));
            //Debug.Log("scores.size(3) = geometry.size(3) : " + (scores.size(3) == geometry.size(3)));

            int height = scores.size(2);
            int width = scores.size(3);
            for (int y = 0; y < height; ++y)
            {
                float[] scoresData = new float[scores.size(3)];
                scores.get(new int[] { 0, 0, y, 0 }, scoresData);
                float[] x0_data = new float[geometry.size(3)];
                geometry.get(new int[] { 0, 0, y, 0 }, x0_data);
                float[] x1_data = new float[geometry.size(3)];
                geometry.get(new int[] { 0, 1, y, 0 }, x1_data);
                float[] x2_data = new float[geometry.size(3)];
                geometry.get(new int[] { 0, 2, y, 0 }, x2_data);
                float[] x3_data = new float[geometry.size(3)];
                geometry.get(new int[] { 0, 3, y, 0 }, x3_data);
                float[] anglesData = new float[geometry.size(3)];
                geometry.get(new int[] { 0, 4, y, 0 }, anglesData);

                for (int x = 0; x < width; ++x)
                {
                    float score = scoresData[x];
                    if (score < scoreThresh)
                        continue;

                    // Decode a prediction.
                    // Multiple by 4 because feature maps are 4 time less than input image.
                    float offsetX = x * 4.0f, offsetY = y * 4.0f;
                    float angle = anglesData[x];
                    float cosA = Mathf.Cos(angle);
                    float sinA = Mathf.Sin(angle);
                    float h = x0_data[x] + x2_data[x];
                    float w = x1_data[x] + x3_data[x];

                    Point offset = new Point(offsetX + cosA * x1_data[x] + sinA * x2_data[x], offsetY - sinA * x1_data[x] + cosA * x2_data[x]);
                    Point p1 = new Point(-sinA * h, -cosA * h) + offset;
                    Point p3 = new Point(-cosA * w, sinA * w) + offset;
                    RotatedRect r = new RotatedRect(0.5f * (p1 + p3), new Size(w, h), -angle * 180.0f / Mathf.PI);
                    detections.Add(r);
                    confidences.Add(score);
                }
            }
        }

        protected void fourPointsTransform(Mat frame, Point[] vertices, Mat result)
        {
            Size outputSize = new Size(100, 32);

            Point[] targetVertices = new Point[] { new Point(0, outputSize.height - 1),
                new Point(0, 0), new Point(outputSize.width - 1, 0),
                new Point(outputSize.width - 1, outputSize.height - 1),
            };

            MatOfPoint2f verticesMat = new MatOfPoint2f(vertices);
            MatOfPoint2f targetVerticesMat = new MatOfPoint2f(targetVertices);
            Mat rotationMatrix = Imgproc.getPerspectiveTransform(verticesMat, targetVerticesMat);

            Imgproc.warpPerspective(frame, result, rotationMatrix, outputSize);
        }

        protected void decodeText(Mat scores, out string text)
        {
            text = "";

            const string alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
            char[] alphabetCharArr = alphabet.ToCharArray();
            Mat scoresMat = scores.reshape(1, scores.size(0));
            List<char> elements = new List<char>();

            for (int rowIndex = 0; rowIndex < scoresMat.rows(); ++rowIndex)
            {
                
                Core.MinMaxLocResult result = Core.minMaxLoc(scoresMat.row(rowIndex));
                Point p = result.maxLoc;
                if ((int)p.x > 0 && (int)p.x <= alphabet.Length)
                {
                    elements.Add(alphabetCharArr[(int)p.x - 1]);
                }
                else
                {
                    elements.Add('-');
                }
            }

            if (elements.Count > 0 && elements[0] != '-')
                text += elements[0];

            for (int elementIndex = 1; elementIndex < elements.Count; ++elementIndex)
            {
                if (elementIndex > 0 && elements[elementIndex] != '-' && elements[elementIndex - 1] != elements[elementIndex])
                {
                    text += elements[elementIndex];
                }
            }
        }
    }
}
#endif