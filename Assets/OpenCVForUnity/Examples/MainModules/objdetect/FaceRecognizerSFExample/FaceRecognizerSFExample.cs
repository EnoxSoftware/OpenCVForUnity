#if !UNITY_WSA_10_0

using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenCVDebug = OpenCVForUnity.Extensions.OpenCVDebug;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// FaceRecognizerSF Example
    /// Compares face identity between a sample image and two reference images using SFace features.
    ///
    /// Demonstrates:
    /// - Face detection with FaceDetectorYN on static images
    /// - Face alignment and 112x112 crop via FaceRecognizerSF.alignCrop
    /// - Feature extraction and cosine/L2 matching thresholds
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="FaceDetectorYN"/>, <see cref="FaceRecognizerSF"/>
    /// - <see cref="Imgcodecs"/>: imread
    /// - <see cref="Imgproc"/>: putText, rectangle
    /// - <see cref="OpenCVMatUnityUtils"/>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Referring to:
    /// https://github.com/opencv/opencv/blob/master/samples/dnn/face_detect.cpp
    /// https://docs.opencv.org/4.5.4/d0/dd4/tutorial_dnn_face.html
    /// </para>
    /// <para>
    /// [Tested Models]
    /// https://github.com/opencv/opencv_zoo/raw/bc04b1b1c2199fe041723122aa0b4b71032c398c/models/face_recognition_sface/face_recognition_sface_2021dec.onnx
    /// </para>
    /// </remarks>
    public class FaceRecognizerSFExample : MonoBehaviour
    {
        // Constants
        private static readonly string FD_MODEL_FILEPATH = "OpenCVForUnityExamples/objdetect/face_detection_yunet_2023mar.onnx";

        private static readonly string SF_MODEL_FILEPATH = "OpenCVForUnityExamples/objdetect/face_recognition_sface_2021dec.onnx";

        private static readonly string IMAGE_0_FILEPATH = "OpenCVForUnityExamples/face/facerec_0.bmp";

        private static readonly string IMAGE_1_FILEPATH = "OpenCVForUnityExamples/face/facerec_1.bmp";

        private static readonly string SAMPLE_IMAGE_FILEPATH = "OpenCVForUnityExamples/face/facerec_sample.bmp";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
        /// <summary>
        /// Filter out faces of score < score_threshold.
        /// </summary>
        private float _scoreThreshold = 0.9f;

        /// <summary>
        /// Suppress bounding boxes of iou >= nms_threshold
        /// </summary>
        private float _nmsThreshold = 0.3f;

        /// <summary>
        /// Keep top_k bounding boxes before NMS.
        /// </summary>
        private int _topK = 5000;

        private string _fdModelFilepath;

        private double _cosineSimilarThresh = 0.363;

        private double _l2normSimilarThresh = 1.128;

        private string _sfModelFilepath;

        private string _image0Filepath;

        private string _image1Filepath;

        private string _sampleImageFilepath;

        private FpsMonitor _fpsMonitor;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Preparing file access...";
            }

            _fdModelFilepath = await OpenCVForUnityEnv.GetFilePathAsync(FD_MODEL_FILEPATH, cancellationToken: _cts.Token);
            _sfModelFilepath = await OpenCVForUnityEnv.GetFilePathAsync(SF_MODEL_FILEPATH, cancellationToken: _cts.Token);
            _image0Filepath = await OpenCVForUnityEnv.GetFilePathAsync(IMAGE_0_FILEPATH, cancellationToken: _cts.Token);
            _image1Filepath = await OpenCVForUnityEnv.GetFilePathAsync(IMAGE_1_FILEPATH, cancellationToken: _cts.Token);
            _sampleImageFilepath = await OpenCVForUnityEnv.GetFilePathAsync(SAMPLE_IMAGE_FILEPATH, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "";
            }

            Run();
        }

        private void Update()
        {

        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        // Public Methods
        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        // Private Methods
        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            if (string.IsNullOrEmpty(_image0Filepath) || string.IsNullOrEmpty(_image1Filepath) || string.IsNullOrEmpty(_sampleImageFilepath))
            {
                Debug.LogError(IMAGE_0_FILEPATH + " or " + IMAGE_1_FILEPATH + " or " + SAMPLE_IMAGE_FILEPATH + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("image file is not loaded.\nPlease read console message.", 20000);
                }

                OpenCVDebug.SetDebugMode(false);
                return;
            }

            Mat testSampleMat = Imgcodecs.imread(_sampleImageFilepath, Imgcodecs.IMREAD_COLOR);
            Mat image0Mat = Imgcodecs.imread(_image0Filepath, Imgcodecs.IMREAD_COLOR);
            Mat image1Mat = Imgcodecs.imread(_image1Filepath, Imgcodecs.IMREAD_COLOR);

            int imageSizeW = 112;
            int imageSizeH = 112;
            Mat resultMat = new Mat(imageSizeH * 2, imageSizeW * 2, CvType.CV_8UC3, new Scalar(127, 127, 127, 255));

            if (string.IsNullOrEmpty(_fdModelFilepath) || string.IsNullOrEmpty(_sfModelFilepath))
            {
                Debug.LogError(FD_MODEL_FILEPATH + " or " + SF_MODEL_FILEPATH + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.", this);
                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Toast("model file is not loaded.\nPlease read console message.", 20000);
                }
            }
            else
            {

                FaceDetectorYN faceDetector = FaceDetectorYN.create(_fdModelFilepath, "", new Size(imageSizeW, imageSizeH), _scoreThreshold, _nmsThreshold, _topK);
                FaceRecognizerSF faceRecognizer = FaceRecognizerSF.create(_sfModelFilepath, "");

                // setInputSize must match the image passed to detect(); one face per row in the output Mat.
                faceDetector.setInputSize(testSampleMat.size());
                Mat faces_sample = new Mat();
                faceDetector.detect(testSampleMat, faces_sample);

                if (faces_sample.rows() < 1)
                {
                    Debug.Log("Cannot find a face in " + SAMPLE_IMAGE_FILEPATH + ".", this);
                }

                faceDetector.setInputSize(image0Mat.size());
                Mat faces_0 = new Mat();
                faceDetector.detect(image0Mat, faces_0);

                if (faces_0.rows() < 1)
                {
                    Debug.Log("Cannot find a face in " + IMAGE_0_FILEPATH + ".", this);
                }

                faceDetector.setInputSize(image1Mat.size());
                Mat faces_1 = new Mat();
                faceDetector.detect(image1Mat, faces_1);

                if (faces_1.rows() < 1)
                {
                    Debug.Log("Cannot find a face in " + IMAGE_1_FILEPATH + ".", this);
                }

                /*
                // Draw results on the input image.
                DrawDetection(faces_sample.row(0),testSampleMat);
                DrawDetection(faces_0.row(0), image0Mat);
                DrawDetection(faces_1.row(0), image1Mat);
                */

                // alignCrop uses the 5 landmarks in row(0) to produce a normalized 112x112 face patch.
                Mat aligned_face_sample = new Mat();
                faceRecognizer.alignCrop(testSampleMat, faces_sample.row(0), aligned_face_sample);
                Mat aligned_face_0 = new Mat();
                faceRecognizer.alignCrop(image0Mat, faces_0.row(0), aligned_face_0);
                Mat aligned_face_1 = new Mat();
                faceRecognizer.alignCrop(image1Mat, faces_1.row(0), aligned_face_1);

                // clone() keeps feature Mats valid after faceRecognizer is disposed at scope end.
                Mat feature_sample = new Mat();
                faceRecognizer.feature(aligned_face_sample, feature_sample);
                feature_sample = feature_sample.clone();
                Mat feature_0 = new Mat();
                faceRecognizer.feature(aligned_face_0, feature_0);
                feature_0 = feature_0.clone();
                Mat feature_1 = new Mat();
                faceRecognizer.feature(aligned_face_1, feature_1);
                feature_1 = feature_1.clone();

                // Match the face features.
                bool sample_0_match = Match(faceRecognizer, feature_sample, feature_0);
                bool sample_1_match = Match(faceRecognizer, feature_sample, feature_1);

                Debug.Log("Match(" + SAMPLE_IMAGE_FILEPATH + ", " + IMAGE_0_FILEPATH + "): " + sample_0_match, this);
                Debug.Log("Match(" + SAMPLE_IMAGE_FILEPATH + ", " + IMAGE_1_FILEPATH + "): " + sample_1_match, this);

                // Draw the recognition result.
                aligned_face_sample.copyTo(resultMat.submat(new Rect(new Point(imageSizeW / 2, 0), aligned_face_sample.size())));
                aligned_face_0.copyTo(resultMat.submat(new Rect(new Point(0, imageSizeH), aligned_face_0.size())));
                aligned_face_1.copyTo(resultMat.submat(new Rect(new Point(imageSizeW, imageSizeH), aligned_face_1.size())));

                Imgproc.putText(resultMat, "TestSample", new Point(imageSizeW / 2 + 5, 15), Imgproc.FONT_HERSHEY_SIMPLEX, 0.4, new Scalar(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
                Imgproc.putText(resultMat, "Image0", new Point(5, imageSizeH + 15), Imgproc.FONT_HERSHEY_SIMPLEX, 0.4, new Scalar(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
                Imgproc.putText(resultMat, "Image1", new Point(imageSizeW + 5, imageSizeH + 15), Imgproc.FONT_HERSHEY_SIMPLEX, 0.4, new Scalar(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);

                if (sample_0_match)
                {
                    Imgproc.rectangle(resultMat, new Rect(0, imageSizeH, imageSizeW, imageSizeH), new Scalar(255, 0, 0, 255), 2);
                }

                if (sample_1_match)
                {
                    Imgproc.rectangle(resultMat, new Rect(imageSizeW, imageSizeH, imageSizeW, imageSizeH), new Scalar(255, 0, 0, 255), 2);
                }
            }

            Texture2D texture = new Texture2D(resultMat.cols(), resultMat.rows(), TextureFormat.RGB24, false);

            OpenCVMatUnityUtils.MatToTexture2D(resultMat, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;

            OpenCVDebug.SetDebugMode(false);
        }

        protected void DrawDetection(Mat d, Mat frame)
        {
            float[] buf = new float[15];
            d.get(0, 0, buf);

            Scalar color = new Scalar(255, 255, 0, 255);

            Imgproc.rectangle(frame, new Point(buf[0], buf[1]), new Point(buf[0] + buf[2], buf[1] + buf[3]), color, 2);
            Imgproc.circle(frame, new Point(buf[4], buf[5]), 2, color, 2);
            Imgproc.circle(frame, new Point(buf[6], buf[7]), 2, color, 2);
            Imgproc.circle(frame, new Point(buf[8], buf[9]), 2, color, 2);
            Imgproc.circle(frame, new Point(buf[10], buf[11]), 2, color, 2);
            Imgproc.circle(frame, new Point(buf[12], buf[13]), 2, color, 2);
        }

        protected bool Match(FaceRecognizerSF faceRecognizer, Mat feature1, Mat feature2)
        {
            // Match requires both metrics to agree; thresholds come from the SFace model documentation.
            double cos_score = faceRecognizer.match(feature1, feature2, FaceRecognizerSF.FR_COSINE);
            bool cos_match = (cos_score >= _cosineSimilarThresh);

            Debug.Log((cos_match ? "They have the same identity;" : "They have different identities;") + "\n"
                + " Cosine Similarity: " + cos_score + ", threshold: " + _cosineSimilarThresh + ". (higher value means higher similarity, max 1.0)", this);

            double l2_score = faceRecognizer.match(feature1, feature2, FaceRecognizerSF.FR_NORM_L2);
            bool l2_match = (l2_score <= _l2normSimilarThresh);

            Debug.Log((l2_match ? "They have the same identity;" : "They have different identities;") + "\n"
                + " NormL2 Distance: " + l2_score + ", threshold: " + _l2normSimilarThresh + ". (lower value means higher similarity, min 0.0)", this);

            return cos_match && l2_match;
        }
    }
}

#endif
