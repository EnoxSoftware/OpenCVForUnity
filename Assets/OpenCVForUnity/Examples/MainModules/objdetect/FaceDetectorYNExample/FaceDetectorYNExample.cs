#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// FaceDetectorYN Example
    /// An example of detecting human face using the FaceDetectorYN class.
    /// https://github.com/opencv/opencv/blob/master/samples/dnn/face_detect.cpp
    /// https://docs.opencv.org/4.5.4/d0/dd4/tutorial_dnn_face.html
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class FaceDetectorYNExample : MonoBehaviour
    {
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        /// <summary>
        /// The apply face blurring Toggle.
        /// </summary>
        public Toggle applyFaceBlurringToggle;

        /// <summary>
        /// The FaceDetectorYN.
        /// </summary>
        FaceDetectorYN faceDetector;

        /// <summary>
        /// The size for the network input.
        /// </summary>
        int inputSizeW = 320;
        int inputSizeH = 320;

        /// <summary>
        /// Filter out faces of score < score_threshold.
        /// </summary>
        float scoreThreshold = 0.9f;

        /// <summary>
        /// Suppress bounding boxes of iou >= nms_threshold
        /// </summary>
        float nmsThreshold = 0.3f;

        /// <summary>
        /// Keep top_k bounding boxes before NMS.
        /// </summary>
        int topK = 5000;

        /// <summary>
        /// The bgr mat.
        /// </summary>
        Mat bgrMat;

        /// <summary>
        /// The input mat.
        /// </summary>
        Mat inputMat;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        MultiSource2MatHelper multiSource2MatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// MODEL_FILENAME
        /// </summary>
        protected static readonly string MODEL_FILENAME = "OpenCVForUnity/objdetect/face_detection_yunet_2023mar.onnx";

        protected Scalar bBoxColor = new Scalar(255, 255, 0, 255);

        protected Scalar[] keyPointsColors = new Scalar[] {
            new Scalar(0, 0, 255, 255), // # right eye
            new Scalar(255, 0, 0, 255), // # left eye
            new Scalar(255, 255, 0, 255), // # nose tip
            new Scalar(0, 255, 255, 255), // # mouth right
            new Scalar(0, 255, 0, 255), // # mouth left
            new Scalar(255, 255, 255, 255) };

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // Use this for initialization
        async void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            multiSource2MatHelper.outputColorFormat = Source2MatHelperColorFormat.RGBA;

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (fpsMonitor != null)
                fpsMonitor.consoleText = "Preparing file access...";

            string fd_modelPath = await Utils.getFilePathAsyncTask(MODEL_FILENAME, cancellationToken: cts.Token);

            if (fpsMonitor != null)
                fpsMonitor.consoleText = "";


            if (string.IsNullOrEmpty(fd_modelPath))
            {
                Debug.LogError(MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/objdetect/setup_objdetect_module.pdf” to make the necessary setup.");
            }
            else
            {
                faceDetector = FaceDetectorYN.create(fd_modelPath, "", new Size(inputSizeW, inputSizeH), scoreThreshold, nmsThreshold, topK);
            }

            multiSource2MatHelper.Initialize();
        }

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = multiSource2MatHelper.GetMat();

            texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(rgbaMat, texture);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", rgbaMat.width().ToString());
                fpsMonitor.Add("height", rgbaMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
            inputMat = new Mat(new Size(inputSizeW, inputSizeH), CvType.CV_8UC3);
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

            if (bgrMat != null)
                bgrMat.Dispose();

            if (inputMat != null)
                inputMat.Dispose();
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (multiSource2MatHelper.IsPlaying() && multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = multiSource2MatHelper.GetMat();

                if (faceDetector == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                    Utils.matToTexture2D(rgbaMat, texture);
                    return;
                }

                Imgproc.cvtColor(rgbaMat, bgrMat, Imgproc.COLOR_RGBA2BGR);

                Detection[] detections = Detect(bgrMat);

                foreach (var d in detections)
                {
                    if (applyFaceBlurringToggle.isOn)
                    {
                        BlurDetection(d, rgbaMat);
                    }
                    else
                    {
                        DrawDetection(d, rgbaMat);
                    }
                }

                Utils.matToTexture2D(rgbaMat, texture);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            multiSource2MatHelper.Dispose();

            if (faceDetector != null)
                faceDetector.Dispose();

            Utils.setDebugMode(false);

            if (cts != null)
                cts.Dispose();
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
            multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            multiSource2MatHelper.requestedIsFrontFacing = !multiSource2MatHelper.requestedIsFrontFacing;
        }

        protected virtual Detection[] Detect(Mat image)
        {
            Imgproc.resize(image, inputMat, inputMat.size());

            float scaleRatioX = (float)image.width() / inputMat.width();
            float scaleRatioY = (float)image.height() / inputMat.height();

            Detection[] detections;

            using (Mat faces = new Mat())
            {
                // The detection output faces is a two - dimension array of type CV_32F, whose rows are the detected face instances, columns are the location of a face and 5 facial landmarks.
                // The format of each row is as follows:
                // x1, y1, w, h, x_re, y_re, x_le, y_le, x_nt, y_nt, x_rcm, y_rcm, x_lcm, y_lcm
                // ,  where x1, y1, w, h are the top - left coordinates, width and height of the face bounding box, { x, y}_{ re, le, nt, rcm, lcm}
                // stands for the coordinates of right eye, left eye, nose tip, the right corner and left corner of the mouth respectively.
                faceDetector.detect(inputMat, faces);

                detections = new Detection[faces.rows()];

                for (int i = 0; i < faces.rows(); i++)
                {
                    float[] buf = new float[Detection.Size];
                    faces.get(i, 0, buf);

                    for (int x = 0; x < 14; x++)
                    {
                        if (x % 2 == 0)
                        {
                            buf[x] *= scaleRatioX;
                        }
                        else
                        {
                            buf[x] *= scaleRatioY;
                        }
                    }

                    GCHandle gch = GCHandle.Alloc(buf, GCHandleType.Pinned);
                    detections[i] = (Detection)Marshal.PtrToStructure(gch.AddrOfPinnedObject(), typeof(Detection));
                    gch.Free();
                }
            }

            return detections;
        }

        protected virtual void DrawDetection(Detection d, Mat frame)
        {
            Imgproc.rectangle(frame, new Point(d.xy.x, d.xy.y), new Point(d.xy.x + d.wh.x, d.xy.y + d.wh.y), bBoxColor, 2);
            Imgproc.circle(frame, new Point(d.rightEye.x, d.rightEye.y), 2, keyPointsColors[0], 2);
            Imgproc.circle(frame, new Point(d.leftEye.x, d.leftEye.y), 2, keyPointsColors[1], 2);
            Imgproc.circle(frame, new Point(d.nose.x, d.nose.y), 2, keyPointsColors[2], 2);
            Imgproc.circle(frame, new Point(d.rightMouth.x, d.rightMouth.y), 2, keyPointsColors[3], 2);
            Imgproc.circle(frame, new Point(d.leftMouth.x, d.leftMouth.y), 2, keyPointsColors[4], 2);

            string label = d.score.ToString();
            int[] baseLine = new int[1];
            Size labelSize = Imgproc.getTextSize(label, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, 1, baseLine);

            float top = Mathf.Max(d.xy.y, (float)labelSize.height);
            float left = d.xy.x;
            Imgproc.rectangle(frame, new Point(left, top - labelSize.height),
                new Point(left + labelSize.width, top + baseLine[0]), Scalar.all(255), Core.FILLED);
            Imgproc.putText(frame, label, new Point(left, top), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(0, 0, 0, 255));
        }

        protected virtual void BlurDetection(Detection d, Mat frame)
        {
            int x = (int)d.xy.x;
            int y = (int)d.xy.y;
            int width = (int)d.wh.x;
            int height = (int)d.wh.y;

            Rect faceRect = new Rect(x, y, width, height);
            Rect frameRect = new Rect(0, 0, frame.cols(), frame.rows());
            Rect intersectRect = faceRect.intersect(frameRect);

            if (intersectRect.width > 0 && intersectRect.height > 0)
            {
                Mat faceRegion = new Mat(frame, intersectRect);
                Imgproc.GaussianBlur(faceRegion, faceRegion, new Size(45, 45), 0);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Detection
        {
            // Bounding box
            public readonly Vector2 xy;
            public readonly Vector2 wh;

            // Key points
            public readonly Vector2 rightEye;
            public readonly Vector2 leftEye;
            public readonly Vector2 nose;
            public readonly Vector2 rightMouth;
            public readonly Vector2 leftMouth;

            // Confidence score [0, 1]
            public readonly float score;

            // sizeof(Detection)
            public const int Size = 15 * sizeof(float);
        };
    }
}

#endif