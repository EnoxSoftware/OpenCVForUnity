using System;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.UnityIntegration.MOT;
using OpenCVForUnity.UnityIntegration.MOT.ByteTrack;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if !UNITY_WSA_10_0
using OpenCVForUnity.UnityIntegration.Worker.DnnModule;
using OpenCVForUnity.UnityIntegration.Worker.DataStruct;
#endif

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Multi Object Tracking (MOT) Example
    /// An example of tracking object detection results using the MOT (Multi Object Tracking) algorithm.
    ///
    /// ByteTrack: https://github.com/ifzhang/ByteTrack
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class MultiObjectTrackingExample : MonoBehaviour
    {
        // Public Fields
        [Header("Output")]
        [Tooltip("The RawImage for previewing the result.")]
        public RawImage ResultPreview;

        [Header("UI")]
        public Toggle ShowObjectDetectorResultToggle;
        public bool ShowObjectDetectorResult;
        public Toggle EnableByteTrackToggle;
        public bool EnableByteTrack;

        [Header("Model Settings")]
        [Tooltip("Path to a binary file of model contains trained weights.")]
        public string Model = "OpenCVForUnityExamples/dnn/yolox_tiny.onnx";

        [Tooltip("Optional path to a text file with names of classes to label detected objects.")]
        public string Classes = "OpenCVForUnityExamples/dnn/coco.names";

        [Tooltip("Confidence threshold.")]
        public float ConfThreshold = 0.25f;

        [Tooltip("Non-maximum suppression threshold.")]
        public float NmsThreshold = 0.45f;

        [Tooltip("Maximum detections per image.")]
        public int TopK = 300;

        [Tooltip("Preprocess input image by resizing to a specific width.")]
        public int InpWidth = 416;

        [Tooltip("Preprocess input image by resizing to a specific height.")]
        public int InpHeight = 416;

        // Private Fields
#if !UNITY_WSA_10_0
        private YOLOXObjectDetector _objectDetector;
#endif

        private BYTETracker _byteTracker;
        private BYTETrackInfoVisualizer _byteTrackInfoVisualizer;
        private bool _disableObjectDetector = false;
        private string _classesFilepath;
        private string _modelFilepath;

        private Texture2D _texture;
        private MultiSource2MatHelper _multiSource2MatHelper;
        private Mat _bgrMat;

        private FpsMonitor _fpsMonitor;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();

            // Update GUI state
            ShowObjectDetectorResultToggle.isOn = ShowObjectDetectorResult;
            EnableByteTrackToggle.isOn = EnableByteTrack;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            if (!string.IsNullOrEmpty(Classes))
            {
                _classesFilepath = await OpenCVEnv.GetFilePathTaskAsync(Classes, cancellationToken: _cts.Token);
                if (string.IsNullOrEmpty(_classesFilepath)) Debug.Log("The file:" + Classes + " did not exist.");
            }
            if (!string.IsNullOrEmpty(Model))
            {
                _modelFilepath = await OpenCVEnv.GetFilePathTaskAsync(Model, cancellationToken: _cts.Token);
                if (string.IsNullOrEmpty(_modelFilepath)) Debug.Log("The file:" + Model + " did not exist.");
            }

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            CheckFilePaths();
            Run();
        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = _multiSource2MatHelper.GetMat();

#if UNITY_WSA_10_0
                Imgproc.putText(rgbaMat, "Disable the DNN module-dependent Object Detector on UWP platforms.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
#else

                if (_objectDetector == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {
                    if (!_disableObjectDetector)
                    {
                        Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

                        using (Mat results = _objectDetector.Detect(_bgrMat))
                        {
                            Imgproc.cvtColor(_bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGB);

                            if (ShowObjectDetectorResult)
                                _objectDetector.Visualize(rgbaMat, results, false, true);

                            if (EnableByteTrack)
                            {
                                BBox[] inputs = ConvertToBBoxes(results);
                                _byteTracker.Update(inputs);
                                BYTETrackInfo[] outputs = _byteTracker.GetActiveTrackInfos();
                                _byteTrackInfoVisualizer.Visualize(rgbaMat, outputs, false, true);
                            }
                        }
                    }
                }
#endif

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

#if !UNITY_WSA_10_0
            _objectDetector?.Dispose();
#endif
            _byteTracker?.Dispose();
            _byteTrackInfoVisualizer?.Dispose();

            _cts?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = _multiSource2MatHelper.GetMat();

            _texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGB24, false);
            OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;


            int fps = 30;
            if (_multiSource2MatHelper.Source2MatHelper is ICameraSource2MatHelper cameraHelper)
            {
                fps = (int)cameraHelper.GetFPS();
            }
            else if (_multiSource2MatHelper.Source2MatHelper is IVideoSource2MatHelper videoHelper)
            {
                fps = (int)videoHelper.GetFPS();
            }

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("width", rgbaMat.width().ToString());
                _fpsMonitor.Add("height", rgbaMat.height().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
                _fpsMonitor.Add("source fps", fps.ToString());
            }

            _byteTracker = new BYTETracker(fps, 30, mot20: false);

            _bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _byteTracker?.Dispose(); _byteTracker = null;
            _bgrMat?.Dispose(); _bgrMat = null;
            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "ErrorCode: " + errorCode + ":" + message;
            }
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
            _multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            _multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            _multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            _multiSource2MatHelper.RequestedIsFrontFacing = !_multiSource2MatHelper.RequestedIsFrontFacing;
        }

        /// <summary>
        /// Raises the reset trackers button click event.
        /// </summary>
        public void OnResetTrackersButtonClick()
        {
            ResetTrackers();
        }

        /// <summary>
        /// Raises the show object detector result toggle value changed event.
        /// </summary>
        public void OnShowObjectDetectorResultToggleValueChanged()
        {
            if (ShowObjectDetectorResultToggle.isOn != ShowObjectDetectorResult)
            {
                ShowObjectDetectorResult = ShowObjectDetectorResultToggle.isOn;
            }
        }

        /// <summary>
        /// Raises the enable byte track toggle value changed event.
        /// </summary>
        public void OnEnableByteTrackToggleValueChanged()
        {
            if (EnableByteTrackToggle.isOn != EnableByteTrack)
            {
                EnableByteTrack = EnableByteTrackToggle.isOn;
            }
        }

        // Private Methods
        private void CheckFilePaths()
        {
            if (string.IsNullOrEmpty(_modelFilepath))
            {
                ShowObjectDetectorResultToggle.isOn = ShowObjectDetectorResultToggle.interactable = false;
                _disableObjectDetector = true;
            }
        }

        private void Run()
        {
            if (string.IsNullOrEmpty(_modelFilepath))
            {
                Debug.LogError("model: " + Model + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
            }
            else
            {

#if !UNITY_WSA_10_0
                _objectDetector = new YOLOXObjectDetector(_modelFilepath, _classesFilepath, new Size(InpWidth, InpHeight), ConfThreshold, NmsThreshold, TopK);
#endif

                _byteTrackInfoVisualizer = new BYTETrackInfoVisualizer(_classesFilepath);
            }

            _multiSource2MatHelper.Initialize();
        }

        private void ResetTrackers()
        {
            _byteTracker?.Reset();

            if (!_disableObjectDetector)
                ShowObjectDetectorResultToggle.interactable = true;
        }

#if !UNITY_WSA_10_0
        private BBox[] ConvertToBBoxes(Mat result)
        {
            if (result.empty() || result.cols() < 6)
                return new BBox[0];

#if NET_STANDARD_2_1 && !OPENCV_DONT_USE_UNSAFE_CODE
            Span<ObjectDetectionData> data = _objectDetector.ToStructuredDataAsSpan(result);
#else
            ObjectDetectionData[] data = _objectDetector.ToStructuredData(result);
#endif

            BBox[] inputs = new BBox[data.Length];
            for (int i = 0; i < data.Length; ++i)
            {
                ref readonly var d = ref data[i];
                inputs[i] = new BBox(d);
            }

            return inputs;
        }
#endif
    }
}
