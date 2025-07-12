#if !UNITY_WSA_10_0

using System.Collections.Generic;
using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.DnnModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Range = OpenCVForUnity.CoreModule.Range;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Fast Neural Style Transfer Example
    /// Referring to https://github.com/opencv/opencv/blob/master/samples/dnn/fast_neural_style.py and https://github.com/jcjohnson/fast-neural-style
    ///
    /// [Tested Models]
    /// https://cs.stanford.edu/people/jcjohns/fast-neural-style/models/instance_norm/mosaic.t7
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class FastNeuralStyleTransferExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// The model filename.
        /// </summary>
        protected static readonly string MODEL_FILENAME = "OpenCVForUnityExamples/dnn/mosaic.t7";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

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
        /// The bgr mat.
        /// </summary>
        private Mat _bgrMat;
        /// <summary>
        /// The net.
        /// </summary>
        private Net _net;
        /// <summary>
        /// The out mat.
        /// </summary>
        private Mat _outMat;
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

            _multiSource2MatHelper.Initialize();
        }

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


            _net = Dnn.readNetFromTorch(_modelFilepath);

            _bgrMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
            _outMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_32FC3);
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _net?.Dispose();

            _bgrMat?.Dispose();

            _outMat?.Dispose();

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

                    Imgproc.cvtColor(rgbaMat, _bgrMat, Imgproc.COLOR_RGBA2BGR);

                    Scalar mean = new Scalar(103.939, 116.779, 123.68);
                    Mat blob = Dnn.blobFromImage(_bgrMat, 1.0, new Size(_bgrMat.width(), _bgrMat.height()), mean, false, false);

                    // How to display the contents of the blob for debugging purposes
                    //DebugMat.imshowDNNBlob("blob", blob, 1.0 / 255.0);

                    _net.setInput(blob);

                    Mat prob = _net.forward();

                    int[] newshape = new int[] { prob.size(2), prob.size(3) };

                    using (Mat B_channel = new Mat(prob, new Range[] { new Range(0, 1), new Range(0, 1), Range.all(), Range.all() }).reshape(1, newshape))
                    using (Mat G_channel = new Mat(prob, new Range[] { new Range(0, 1), new Range(1, 2), Range.all(), Range.all() }).reshape(1, newshape))
                    using (Mat R_channel = new Mat(prob, new Range[] { new Range(0, 1), new Range(2, 3), Range.all(), Range.all() }).reshape(1, newshape))
                    {
                        Core.merge(new List<Mat>() { B_channel, G_channel, R_channel }, _outMat);
                    }

                    Core.add(_outMat, mean, _outMat);

                    // The DNN model outputs images in multiples of 8. If the input and output mat sizes are different, only the overlapping area is transcribed.
                    if (_outMat.width() != rgbaMat.width() || _outMat.height() != rgbaMat.height())
                    {
                        Rect intersectRect = new Rect(0, 0, _outMat.width(), _outMat.height()).intersect(new Rect(0, 0, rgbaMat.width(), rgbaMat.height()));
                        Mat outMat_roi = new Mat(_outMat, intersectRect);
                        Mat bgrMat_roi = new Mat(_bgrMat, intersectRect);

                        outMat_roi.convertTo(bgrMat_roi, CvType.CV_8U);
                    }
                    else
                    {
                        _outMat.convertTo(_bgrMat, CvType.CV_8U);
                    }

                    Imgproc.cvtColor(_bgrMat, rgbaMat, Imgproc.COLOR_BGR2RGBA);

                    prob.Dispose();
                    blob.Dispose();

                }

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper.Dispose();

            OpenCVDebug.SetDebugMode(false);

            if (_cts != null)
                _cts.Dispose();
        }

        // Public Methods
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
