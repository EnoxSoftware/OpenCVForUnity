using OpenCVForUnity.BgsegmModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.VideoModule;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Background Subtractor Example
    /// This example is intended for comparison of different background subtraction algorithms presented in OpenCV.
    /// </summary>
    [RequireComponent(typeof(VideoCaptureToMatHelper))]
    public class BackgroundSubtractorExample : MonoBehaviour
    {
        /// <summary>
        /// The background subtractor algorithm dropdown.
        /// </summary>
        public Dropdown backgroundSubtractorAlgorithmDropdown;

        /// <summary>
        /// The background subtractor algorithm.
        /// </summary>
        public BackgroundSubtractorAlgorithmPreset backgroundSubtractorAlgorithm = BackgroundSubtractorAlgorithmPreset.KNN;

        /// <summary>
        /// The enable MorphologyEx toggle.
        /// </summary>
        public Toggle enableMorphologyExToggle;

        /// <summary>
        /// The show background image toggle.
        /// </summary>
        public Toggle showBackgroundImageToggle;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The video capture to mat helper.
        /// </summary>
        VideoCaptureToMatHelper sourceToMatHelper;

        /// <summary>
        /// The background substractor.
        /// </summary>
        BackgroundSubtractor backgroundSubstractor;

        /// <summary>
        /// The fgmask mat.
        /// </summary>
        Mat fgmaskMat;

        /// <summary>
        /// The kernel for morphologyEx method.
        /// </summary>
        Mat kernel;

        /// <summary>
        /// The stopwatch.
        /// </summary>
        System.Diagnostics.Stopwatch watch;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// VIDEO_FILENAME
        /// </summary>
        protected static readonly string VIDEO_FILENAME = "OpenCVForUnity/768x576_mjpeg.mjpeg";

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            sourceToMatHelper = gameObject.GetComponent<VideoCaptureToMatHelper>();
            if (string.IsNullOrEmpty(sourceToMatHelper.requestedVideoFilePath))
                sourceToMatHelper.requestedVideoFilePath = VIDEO_FILENAME;
            sourceToMatHelper.outputColorFormat = VideoCaptureToMatHelper.ColorFormat.RGB; // Background Subtractor API must handle 3 channels Mat image.
            sourceToMatHelper.Initialize();

            // Update GUI state
            backgroundSubtractorAlgorithmDropdown.value = Array.IndexOf(System.Enum.GetNames(typeof(BackgroundSubtractorAlgorithmPreset)), backgroundSubtractorAlgorithm.ToString());
            enableMorphologyExToggle.isOn = false;
            showBackgroundImageToggle.isOn = false;


            CreateBackgroundSubstractor(backgroundSubtractorAlgorithm);

            kernel = Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(3, 3));

            watch = new System.Diagnostics.Stopwatch();

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("time: ", "");
            }
        }

        protected void CreateBackgroundSubstractor(BackgroundSubtractorAlgorithmPreset algorithm)
        {
            if (backgroundSubstractor != null)
                backgroundSubstractor.Dispose();

            switch (algorithm)
            {
                case BackgroundSubtractorAlgorithmPreset.KNN:
                    backgroundSubstractor = Video.createBackgroundSubtractorKNN();

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
                    backgroundSubstractor = Video.createBackgroundSubtractorMOG2();

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
                    backgroundSubstractor = Bgsegm.createBackgroundSubtractorCNT();

                    //BackgroundSubtractorCNT subtractorCNT = (BackgroundSubtractorCNT)backgroundSubstractor;
                    //subtractorCNT.setIsParallel(true);
                    //subtractorCNT.setMaxPixelStability(900)
                    //subtractorCNT.setMinPixelStability(15);
                    //subtractorCNT.setUseHistory(true);

                    break;
                case BackgroundSubtractorAlgorithmPreset.GMG:
                    backgroundSubstractor = Bgsegm.createBackgroundSubtractorGMG();
                    break;
                case BackgroundSubtractorAlgorithmPreset.GSOC:
                    backgroundSubstractor = Bgsegm.createBackgroundSubtractorGSOC();
                    break;
                case BackgroundSubtractorAlgorithmPreset.LSBP:
                    backgroundSubstractor = Bgsegm.createBackgroundSubtractorLSBP();
                    break;
                case BackgroundSubtractorAlgorithmPreset.MOG:
                    backgroundSubstractor = Bgsegm.createBackgroundSubtractorMOG();
                    break;
                default:

                    break;
            }
        }

        /// <summary>
        /// Raises the video capture to mat helper initialized event.
        /// </summary>
        public void OnVideoCaptureToMatHelperInitialized()
        {
            Debug.Log("OnVideoCaptureToMatHelperInitialized");

            Mat rgbMat = sourceToMatHelper.GetMat();

            texture = new Texture2D(rgbMat.cols(), rgbMat.rows(), TextureFormat.RGB24, false);
            Utils.matToTexture2D(rgbMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(rgbMat.cols(), rgbMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


            float width = rgbMat.width();
            float height = rgbMat.height();

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


            fgmaskMat = new Mat(rgbMat.rows(), rgbMat.cols(), CvType.CV_8UC1);
        }

        /// <summary>
        /// Raises the video capture to mat helper disposed event.
        /// </summary>
        public void OnVideoCaptureToMatHelperDisposed()
        {
            Debug.Log("OnVideoCaptureToMatHelperDisposed");

            if (backgroundSubstractor != null)
                backgroundSubstractor.clear();

            if (fgmaskMat != null)
                fgmaskMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the video capture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnVideoCaptureToMatHelperErrorOccurred(VideoCaptureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnVideoCaptureToMatHelperErrorOccurred " + errorCode);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (sourceToMatHelper.IsPlaying() && sourceToMatHelper.DidUpdateThisFrame())
            {
                Mat rgbMat = sourceToMatHelper.GetMat();

                watch.Reset();
                watch.Start();

                backgroundSubstractor.apply(rgbMat, fgmaskMat);

                if (enableMorphologyExToggle.isOn)
                    Imgproc.morphologyEx(fgmaskMat, fgmaskMat, Imgproc.MORPH_OPEN, kernel);

                watch.Stop();


                if (showBackgroundImageToggle.isOn && backgroundSubtractorAlgorithm != BackgroundSubtractorAlgorithmPreset.GMG)
                {
                    if (backgroundSubtractorAlgorithm == BackgroundSubtractorAlgorithmPreset.CNT)
                    {
                        backgroundSubstractor.getBackgroundImage(fgmaskMat);
                        Imgproc.cvtColor(fgmaskMat, rgbMat, Imgproc.COLOR_GRAY2RGB);
                    }
                    else
                    {
                        backgroundSubstractor.getBackgroundImage(rgbMat);
                    }
                }
                else
                {
                    Imgproc.cvtColor(fgmaskMat, rgbMat, Imgproc.COLOR_GRAY2RGB);
                }

                Utils.matToTexture2D(rgbMat, texture);


                if (fpsMonitor != null)
                {
                    fpsMonitor.Add("time: ", watch.ElapsedMilliseconds + " ms");
                }
            }
        }


        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            if (backgroundSubstractor != null)
                backgroundSubstractor.Dispose();

            if (sourceToMatHelper != null)
                sourceToMatHelper.Dispose();
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

            if ((byte)backgroundSubtractorAlgorithm != value)
            {
                backgroundSubtractorAlgorithm = (BackgroundSubtractorAlgorithmPreset)value;
            }

            //Debug.Log((int)backgroundSubtractorAlgorithm);

            CreateBackgroundSubstractor(backgroundSubtractorAlgorithm);

            sourceToMatHelper.Initialize();
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
    }
}