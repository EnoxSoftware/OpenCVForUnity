using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Features2dModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// SimpleBlob Example
    /// An example of feature detection using the SIMPLEBLOB algorithm.
    /// </summary>
    public class SimpleBlobExample : MonoBehaviour
    {
        // Constants
        private static readonly string BLOBPARAMS_YML_FILENAME = "OpenCVForUnityExamples/features2d/blobparams.yml";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
        private string _blobparamsYmlFilepath;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Unity Lifecycle Methods
        private async void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "Preparing file access...";

            _blobparamsYmlFilepath = await OpenCVEnv.GetFilePathTaskAsync(BLOBPARAMS_YML_FILENAME, cancellationToken: _cts.Token);

            if (_fpsMonitor != null)
                _fpsMonitor.ConsoleText = "";

            Run();
        }

        private void Update()
        {

        }

        private void OnDestroy()
        {
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

        // Private Methods
        private void Run()
        {

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);


            Texture2D imgTexture = Resources.Load("detect_blob") as Texture2D;

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC1);

            OpenCVMatUtils.Texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString());

            Mat outImgMat = new Mat();


            //load Params from SimpleBlobDetector_Params class.
            SimpleBlobDetector_Params param = new SimpleBlobDetector_Params();
            param.set_thresholdStep(20.0f);
            param.set_minThreshold(10.0f);
            param.set_maxThreshold(200.0f);
            param.set_minRepeatability(2);
            param.set_minDistBetweenBlobs(10);
            param.set_filterByColor(false);
            param.set_filterByArea(false);
            param.set_minArea(1);
            param.set_maxArea(100000);
            param.set_filterByCircularity(false);
            param.set_minCircularity(1);
            param.set_maxCircularity(100000);
            param.set_filterByInertia(false);
            param.set_minInertiaRatio(1);
            param.set_maxInertiaRatio(100000);
            param.set_filterByConvexity(false);
            param.set_minConvexity(1);
            param.set_maxConvexity(100000);

            SimpleBlobDetector blobDetector = SimpleBlobDetector.create(param);
            Debug.Log("blobDetector.getDefaultName() " + blobDetector.getDefaultName());

            // or

            ////load Params from yml file.
            //SimpleBlobDetector blobDetector = SimpleBlobDetector.create();
            //Debug.Log("blobDetector.getDefaultName() " + blobDetector.getDefaultName());
            //blobDetector.read(_blobparamsYmlFilepath);


            MatOfKeyPoint keypoints = new MatOfKeyPoint();
            blobDetector.detect(imgMat, keypoints);
            Features2d.drawKeypoints(imgMat, keypoints, outImgMat);


            Texture2D texture = new Texture2D(outImgMat.cols(), outImgMat.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUtils.MatToTexture2D(outImgMat, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            OpenCVDebug.SetDebugMode(false);
        }
    }
}
