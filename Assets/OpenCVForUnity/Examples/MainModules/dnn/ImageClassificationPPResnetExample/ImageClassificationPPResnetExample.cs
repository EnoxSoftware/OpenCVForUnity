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

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Image Classification PPResnet Example
    /// An example of using OpenCV dnn module with Image Classification PPResnet model.
    /// Referring to https://github.com/opencv/opencv_zoo/tree/master/models/image_classification_ppresnet
    ///
    /// [Tested Models]
    /// https://github.com/opencv/opencv_zoo/raw/c8812a7668ea3f285797c0c450d0912add9248f2/models/image_classification_ppresnet/image_classification_ppresnet50_2022jan.onnx
    /// https://raw.githubusercontent.com/opencv/opencv_zoo/326e15b31a70812eb6d616406d6e6a17ceaddb6f/models/image_classification_ppresnet/imagenet_labels.txt
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class ImageClassificationPPResnetExample : MonoBehaviour
    {
        // Constants
        protected static readonly string MODEL_FILENAME = "OpenCVForUnityExamples/dnn/image_classification_ppresnet50_2022jan.onnx";
        protected static readonly string CLASSES_FILENAME = "OpenCVForUnityExamples/dnn/imagenet_labels.txt";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

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
        /// The net.
        /// </summary>
        private Net _net;

        private Size _inputSize = new Size(224, 224);
        private Scalar _mean = new Scalar(0.485, 0.456, 0.406);
        private Scalar _std = new Scalar(0.229, 0.224, 0.225);

        /// <summary>
        /// The classes.
        /// </summary>
        private List<string> _classes;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// The model filepath.
        /// </summary>
        private string _modelFilepath;

        /// <summary>
        /// The classes filepath.
        /// </summary>
        private string _classesFilepath;

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
            _classesFilepath = await OpenCVEnv.GetFilePathTaskAsync(CLASSES_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        private void Run()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);


            _classes = ReadClassNames(_classesFilepath);
            if (_classes == null)
            {
                Debug.LogError(CLASSES_FILENAME + " is not loaded. Please use [Tools] > [OpenCV for Unity] > [Setup Tools] > [Example Assets Downloader]to download the asset files required for this example scene, and then move them to the \"Assets/StreamingAssets\" folder.");
            }

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

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = _multiSource2MatHelper.GetMat();

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
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _rgbMat?.Dispose();

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

        // Update is called once per frame
        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_net == null || _classes == null)
                {
                    Imgproc.putText(rgbaMat, "model file is not loaded.", new Point(5, rgbaMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText(rgbaMat, "Please read console message.", new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.7, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
                else
                {

                    Imgproc.cvtColor(rgbaMat, _rgbMat, Imgproc.COLOR_RGBA2RGB);

                    Mat blob = Dnn.blobFromImage(_rgbMat, 1.0 / 255.0, _inputSize, Scalar.all(0), false, true, CvType.CV_32F); // HWC to NCHW, RGB

                    int c = 3;
                    int h = (int)_inputSize.height;
                    int w = (int)_inputSize.width;

                    using (Mat blob_CxHxW = blob.reshape(1, new int[] { c, h, w })) // [c, h, w]
                    {
                        for (int i = 0; i < c; ++i)
                        {
                            using (Mat blob_CxHxW_row = blob_CxHxW.row(i))
                            using (Mat blob_1xHW = blob_CxHxW_row.reshape(1, 1)) // [1, h, w] => [1, h * w]
                            {
                                // Subtract blob by mean.
                                Core.subtract(blob_1xHW, (_mean.val[i], 0, 0, 0), blob_1xHW);
                                // Divide blob by std.
                                Core.divide(blob_1xHW, (_std.val[i], 0, 0, 0), blob_1xHW);
                            }
                        }
                    }

                    // How to display the contents of the blob for debugging purposes
                    //DebugMat.imshowDNNBlob("blob", blob);

                    _net.setInput(blob);

                    Mat prob = _net.forward("save_infer_model/scale_0.tmp_0");
                    //Debug.Log("prob.ToString(): " + prob.ToString());


                    Core.MinMaxLocResult minmax = Core.minMaxLoc(prob.reshape(1, 1));
                    //Debug.Log ("Best match " + (int)minmax.maxLoc.x);
                    //Debug.Log ("Best match class " + classes [(int)minmax.maxLoc.x]);
                    //Debug.Log ("Probability: " + minmax.maxVal * 100 + "%");

                    prob.Dispose();
                    blob.Dispose();

                    //Imgproc.putText (rgbaMat, "Best match class " + classes [(int)minmax.maxLoc.x], new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    if (_fpsMonitor != null)
                    {
                        _fpsMonitor.ConsoleText = "Best match class " + _classes[(int)minmax.maxLoc.x];
                    }
                }

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        // Unity Lifecycle Methods
        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            _net?.Dispose();

            OpenCVDebug.SetDebugMode(false);

            _cts?.Dispose();
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

        // Private Methods
        private List<string> ReadClassNames(string filename)
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

#endif
