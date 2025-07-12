using System;
using OpenCVForUnity.BgsegmModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.VideoModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Background Subtractor Example
    /// This example is intended for comparison of different background subtraction algorithms presented in OpenCV.
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class BackgroundSubtractorExample : MonoBehaviour
    {
        // Enums
        /// <summary>
        /// Background subtractor algorithm preset enum
        /// </summary>
        public enum BackgroundSubtractorAlgorithmPreset : byte
        {
            KNN = 0,
            MOG2,
            CNT,
            GMG,
            GSOC,
            LSBP,
            MOG,
        }

        // Constants
        /// <summary>
        /// VIDEO_FILENAME
        /// </summary>
        protected static readonly string VIDEO_FILENAME = "OpenCVForUnityExamples/768x576_mjpeg.mjpeg";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        /// <summary>
        /// The background subtractor algorithm dropdown.
        /// </summary>
        public Dropdown BackgroundSubtractorAlgorithmDropdown;

        /// <summary>
        /// The background subtractor algorithm.
        /// </summary>
        public BackgroundSubtractorAlgorithmPreset BackgroundSubtractorAlgorithm = BackgroundSubtractorAlgorithmPreset.KNN;

        /// <summary>
        /// The enable MorphologyEx toggle.
        /// </summary>
        public Toggle EnableMorphologyExToggle;

        /// <summary>
        /// The show background image toggle.
        /// </summary>
        public Toggle ShowBackgroundImageToggle;

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
        /// The background substractor.
        /// </summary>
        private BackgroundSubtractor _backgroundSubstractor;

        /// <summary>
        /// The fgmask mat.
        /// </summary>
        private Mat _fgmaskMat;

        /// <summary>
        /// The kernel for morphologyEx method.
        /// </summary>
        private Mat _kernel;

        /// <summary>
        /// The stopwatch.
        /// </summary>
        private System.Diagnostics.Stopwatch _watch;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            if (string.IsNullOrEmpty(_multiSource2MatHelper.RequestedVideoFilePath))
                _multiSource2MatHelper.RequestedVideoFilePath = VIDEO_FILENAME;
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGB; // Background Subtractor API must handle 3 channels Mat image.
            _multiSource2MatHelper.Initialize();

            // Update GUI state
            BackgroundSubtractorAlgorithmDropdown.value = Array.IndexOf(System.Enum.GetNames(typeof(BackgroundSubtractorAlgorithmPreset)), BackgroundSubtractorAlgorithm.ToString());
            EnableMorphologyExToggle.isOn = false;
            ShowBackgroundImageToggle.isOn = false;


            CreateBackgroundSubstractor(BackgroundSubtractorAlgorithm);

            _kernel = Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(3, 3));

            _watch = new System.Diagnostics.Stopwatch();

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("time: ", "");
            }
        }

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {
                Mat rgbMat = _multiSource2MatHelper.GetMat();

                _watch.Reset();
                _watch.Start();

                _backgroundSubstractor.apply(rgbMat, _fgmaskMat);

                if (EnableMorphologyExToggle.isOn)
                    Imgproc.morphologyEx(_fgmaskMat, _fgmaskMat, Imgproc.MORPH_OPEN, _kernel);

                _watch.Stop();


                if (ShowBackgroundImageToggle.isOn && BackgroundSubtractorAlgorithm != BackgroundSubtractorAlgorithmPreset.GMG)
                {
                    if (BackgroundSubtractorAlgorithm == BackgroundSubtractorAlgorithmPreset.CNT)
                    {
                        _backgroundSubstractor.getBackgroundImage(_fgmaskMat);
                        Imgproc.cvtColor(_fgmaskMat, rgbMat, Imgproc.COLOR_GRAY2RGB);
                    }
                    else
                    {
                        _backgroundSubstractor.getBackgroundImage(rgbMat);
                    }
                }
                else
                {
                    Imgproc.cvtColor(_fgmaskMat, rgbMat, Imgproc.COLOR_GRAY2RGB);
                }

                OpenCVMatUtils.MatToTexture2D(rgbMat, _texture);


                if (_fpsMonitor != null)
                {
                    _fpsMonitor.Add("time: ", _watch.ElapsedMilliseconds + " ms");
                }
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();

            _backgroundSubstractor?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbMat = _multiSource2MatHelper.GetMat();

            _texture = new Texture2D(rgbMat.cols(), rgbMat.rows(), TextureFormat.RGB24, false);
            OpenCVMatUtils.MatToTexture2D(rgbMat, _texture);

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;


            _fgmaskMat = new Mat(rgbMat.rows(), rgbMat.cols(), CvType.CV_8UC1);
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _backgroundSubstractor?.clear();

            _fgmaskMat?.Dispose();

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
        /// Raises the background subtractor algorithm dropdown value changed event.
        /// </summary>
        public void OnBackgroundSubtractorAlgorithmDropdownValueChanged(int result)
        {
            string[] enumNames = Enum.GetNames(typeof(BackgroundSubtractorAlgorithmPreset));
            byte value = (byte)System.Enum.Parse(typeof(BackgroundSubtractorAlgorithmPreset), enumNames[result], true);

            if ((byte)BackgroundSubtractorAlgorithm != value)
            {
                BackgroundSubtractorAlgorithm = (BackgroundSubtractorAlgorithmPreset)value;
            }

            //Debug.Log((int)backgroundSubtractorAlgorithm);

            CreateBackgroundSubstractor(BackgroundSubtractorAlgorithm);

            _multiSource2MatHelper.Initialize();
        }

        /// <summary>
        /// Raises the enable MorphologyEx toggle value changed event.
        /// </summary>
        public void OnEnableMorphologyExToggleValueChanged()
        {
            //
            //Debug.Log(enableMorphologyExToggleToggle.isOn);
        }

        /// <summary>
        /// Raises the show background image toggle value changed event.
        /// </summary>
        public void OnShowBackgroundImageToggleValueChanged()
        {
            //
            //Debug.Log(showBackgroundImageToggle.isOn);
        }

        // Private Methods
        protected void CreateBackgroundSubstractor(BackgroundSubtractorAlgorithmPreset algorithm)
        {
            if (_backgroundSubstractor != null)
                _backgroundSubstractor.Dispose();

            switch (algorithm)
            {
                case BackgroundSubtractorAlgorithmPreset.KNN:
                    _backgroundSubstractor = Video.createBackgroundSubtractorKNN();

                    //BackgroundSubtractorKNN subtractorKNN = (BackgroundSubtractorKNN)backgroundSubstractor;
                    //subtractorKNN.setDetectShadows(true);
                    //subtractorKNN.setDist2Threshold(400);
                    //subtractorKNN.setHistory(500);
                    //subtractorKNN.setkNNSamples(2);
                    //subtractorKNN.setNSamples(7);
                    //subtractorKNN.setShadowThreshold(0.5);
                    //subtractorKNN.setShadowValue(127);

                    break;
                case BackgroundSubtractorAlgorithmPreset.MOG2:
                    _backgroundSubstractor = Video.createBackgroundSubtractorMOG2();

                    //BackgroundSubtractorMOG2 subtractorMOG2 = (BackgroundSubtractorMOG2)backgroundSubstractor;
                    //subtractorMOG2.setBackgroundRatio(0.899999976158142);
                    //subtractorMOG2.setComplexityReductionThreshold(0.0500000007450581);
                    //subtractorMOG2.setDetectShadows(true);
                    //subtractorMOG2.setHistory(500);
                    //subtractorMOG2.setNMixtures(5);
                    //subtractorMOG2.setShadowThreshold(0.5);
                    //subtractorMOG2.setShadowValue(127);
                    //subtractorMOG2.setVarInit(15);
                    //subtractorMOG2.setVarMax(75);
                    //subtractorMOG2.setVarMin(4);
                    //subtractorMOG2.setVarThreshold(16);
                    //subtractorMOG2.setVarThresholdGen(9);

                    break;
                case BackgroundSubtractorAlgorithmPreset.CNT:
                    _backgroundSubstractor = Bgsegm.createBackgroundSubtractorCNT();

                    //BackgroundSubtractorCNT subtractorCNT = (BackgroundSubtractorCNT)backgroundSubstractor;
                    //subtractorCNT.setIsParallel(true);
                    //subtractorCNT.setMaxPixelStability(900)
                    //subtractorCNT.setMinPixelStability(15);
                    //subtractorCNT.setUseHistory(true);

                    break;
                case BackgroundSubtractorAlgorithmPreset.GMG:
                    _backgroundSubstractor = Bgsegm.createBackgroundSubtractorGMG();
                    break;
                case BackgroundSubtractorAlgorithmPreset.GSOC:
                    _backgroundSubstractor = Bgsegm.createBackgroundSubtractorGSOC();
                    break;
                case BackgroundSubtractorAlgorithmPreset.LSBP:
                    _backgroundSubstractor = Bgsegm.createBackgroundSubtractorLSBP();
                    break;
                case BackgroundSubtractorAlgorithmPreset.MOG:
                    _backgroundSubstractor = Bgsegm.createBackgroundSubtractorMOG();
                    break;
                default:

                    break;
            }
        }
    }
}
