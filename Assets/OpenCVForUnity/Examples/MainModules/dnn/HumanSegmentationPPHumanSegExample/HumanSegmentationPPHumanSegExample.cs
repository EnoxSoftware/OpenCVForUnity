#if !UNITY_WSA_10_0

using System;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Human Segmentation PPHumanSeg Example
    /// An example of using OpenCV dnn module with Human Segmentation model.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/master/models/human_segmentation_pphumanseg
    ///
    /// [Tested Models]
    /// https://github.com/opencv/opencv_zoo/raw/2027dd2f5a8a5746b5d4964900a0465afc6d3a53/models/human_segmentation_pphumanseg/human_segmentation_pphumanseg_2023mar.onnx
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class HumanSegmentationPPHumanSegExample : MonoBehaviour
    {
        // Constants
        protected static readonly string MODEL_FILENAME = "OpenCVForUnityExamples/dnn/human_segmentation_pphumanseg_2023mar.onnx";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        /// <summary>
        /// The compose bg image toggle.
        /// </summary>
        public Toggle ComposeBGImageToggle;

        public bool ComposeBGImage;

        /// <summary>
        /// The hide person toggle.
        /// </summary>
        public Toggle HidePersonToggle;

        public bool HidePerson;

        /// <summary>
        /// The background image texture.
        /// </summary>
        public Texture2D BackGroundImageTexture;

        // Private Fields
        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        private Mat _rgbMat;

        /// <summary>
        /// The mask mat.
        /// </summary>
        private Mat _maskMat;

        /// <summary>
        /// The background mask mat.
        /// </summary>
        private Mat _bgMaskMat;

        /// <summary>
        /// The background image mat.
        /// </summary>
        private Mat _backGroundImageMat;

        /// <summary>
        /// The net.
        /// </summary>
        private Net _net;

        private Size _inputSize = new Size(192, 192);
        private Scalar _mean = new Scalar(127.5, 127.5, 127.5); //  = 0.5, 0.5, 0.5
        private double _std = 1.0 / 127.5; // = 0.5, 0.5, 0.5

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// The model filepath.
        /// </summary>
        private string _modelFilepath;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;

            // Update GUI state
            ComposeBGImageToggle.isOn = ComposeBGImage;
            HidePersonToggle.isOn = HidePerson;

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _modelFilepath = await OpenCVEnv.GetFilePathTaskAsync(MODEL_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            if (string.IsNullOrEmpty(_modelFilepath))
            {
                Debug.LogError(MODEL_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
            }
            else
            {
                _net = Dnn.readNet(_modelFilepath);
            }

            _multiSource2MatHelper.Initialize();
        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_net == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {

                    Imgproc.cvtColor(rgbaMat, _rgbMat, Imgproc.COLOR_RGBA2RGB);

                    Mat blob = Dnn.blobFromImage(_rgbMat, _std, _inputSize, _mean, false, false, CvType.CV_32F); // HWC to NCHW, RGB

                    // How to display the contents of the blob for debugging purposes
                    //DebugMat.imshowDNNBlob("blob", blob);

                    _net.setInput(blob);

                    Mat prob = _net.forward("save_infer_model/scale_0.tmp_1");

                    Mat result = new Mat();
                    Core.reduceArgMax(prob, result, 1);
                    result.convertTo(result, CvType.CV_8U, 255.0);

                    Mat mask192x192 = new Mat(_inputSize, CvType.CV_8UC1, (IntPtr)result.dataAddr());
                    Imgproc.resize(mask192x192, _maskMat, rgbaMat.size(), Imgproc.INTER_NEAREST);

                    if (ComposeBGImageToggle.isOn)
                    {
                        // Compose the background image.
                        Core.bitwise_not(_maskMat, _bgMaskMat);
                        _backGroundImageMat.copyTo(rgbaMat, _bgMaskMat);
                    }

                    if (HidePersonToggle.isOn)
                    {
                        rgbaMat.setTo(new Scalar(255, 255, 255, 255), _maskMat);
                    }

                    mask192x192.Dispose();
                    result.Dispose();

                    prob.Dispose();
                    blob.Dispose();

                }

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            _net?.Dispose();

            OpenCVDebug.SetDebugMode(false);

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

            // Fill in the image so that the unprocessed image is not displayed.
            rgbaMat.setTo(new Scalar(0, 0, 0, 255));

            _texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;


            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("width", rgbaMat.width().ToString());
                _fpsMonitor.Add("height", rgbaMat.height().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            _rgbMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
            _maskMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);

            _bgMaskMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);
            _backGroundImageMat = new Mat(rgbaMat.size(), CvType.CV_8UC4, new Scalar(39, 255, 86, 255));
            if (BackGroundImageTexture != null)
            {
                using (Mat bgMat = new Mat(BackGroundImageTexture.height, BackGroundImageTexture.width, CvType.CV_8UC4))
                {
                    OpenCVMatUtils.Texture2DToMat(BackGroundImageTexture, bgMat);
                    Imgproc.resize(_backGroundImageMat, _backGroundImageMat, _backGroundImageMat.size());
                }
            }
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _rgbMat?.Dispose();
            _maskMat?.Dispose();
            _bgMaskMat?.Dispose();
            _backGroundImageMat?.Dispose();

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
    }
}

#endif
