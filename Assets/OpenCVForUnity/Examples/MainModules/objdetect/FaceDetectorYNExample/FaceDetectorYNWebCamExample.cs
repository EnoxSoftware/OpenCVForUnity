#if !UNITY_WSA_10_0

using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// FaceDetectorYN WebCam Example
    /// An example of detecting human face in a image of WebCamTexture using the FaceDetectorYN class.
    /// https://github.com/opencv/opencv/blob/master/samples/dnn/face_detect.cpp
    /// https://docs.opencv.org/4.5.4/d0/dd4/tutorial_dnn_face.html
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class FaceDetectorYNWebCamExample : MonoBehaviour
    {
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
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

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

#if UNITY_WEBGL
        IEnumerator getFilePath_Coroutine;
#endif

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);

#if UNITY_WEBGL
            getFilePath_Coroutine = Utils.getFilePathAsync (MODEL_FILENAME, (result) => {
                getFilePath_Coroutine = null;

                if (string.IsNullOrEmpty(result))
                {
                    Debug.LogError(MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/objdetect/setup_objdetect_module.pdf” to make the necessary setup.");
                }
                else
                {
                    faceDetector = FaceDetectorYN.create(result, "", new Size(inputSizeW, inputSizeH), scoreThreshold, nmsThreshold, topK);
                }

                webCamTextureToMatHelper.Initialize ();
            });
            StartCoroutine (getFilePath_Coroutine);
#else
            string fd_modelPath = Utils.getFilePath(MODEL_FILENAME);
            if (string.IsNullOrEmpty(fd_modelPath))
            {
                Debug.LogError(MODEL_FILENAME + " is not loaded. Please read “StreamingAssets/OpenCVForUnity/objdetect/setup_objdetect_module.pdf” to make the necessary setup.");
            }
            else
            {
                faceDetector = FaceDetectorYN.create(fd_modelPath, "", new Size(inputSizeW, inputSizeH), scoreThreshold, nmsThreshold, topK);
            }



#if UNITY_ANDROID && !UNITY_EDITOR
            // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
            webCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
#endif
            webCamTextureToMatHelper.Initialize();

#endif

        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
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
            inputMat = new Mat(new Size(inputSizeW, inputSizeH), CvType.CV_8UC3);
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

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
        /// Raises the web cam texture to mat helper error occurred event.
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
                    DrawDetection(d, rgbaMat);
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
                faceDetector.Dispose();

            Utils.setDebugMode(false);

#if UNITY_WEBGL
            if (getFilePath_Coroutine != null) {
                StopCoroutine (getFilePath_Coroutine);
                ((IDisposable)getFilePath_Coroutine).Dispose ();
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