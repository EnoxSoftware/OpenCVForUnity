using System.Threading;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.FeaturesModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OpenCVDebug = OpenCVForUnity.Extensions.OpenCVDebug;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// SimpleBlob Example
    /// Detects blob keypoints in a grayscale image using configurable SimpleBlobDetector parameters.
    ///
    /// Demonstrates:
    /// - Configuring <see cref="SimpleBlobDetector_Params"/> thresholds and shape filters
    /// - Blob detection with <see cref="SimpleBlobDetector.detect"/>
    /// - Visualizing keypoints via <see cref="Features.drawKeypoints"/>
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="MatOfKeyPoint"/>
    /// - <see cref="SimpleBlobDetector"/>, <see cref="SimpleBlobDetector_Params"/>
    /// - <see cref="Features"/>: drawKeypoints
    /// - <see cref="OpenCVMatUnityUtils"/>, <see cref="OpenCVForUnityEnv"/>
    /// </summary>
    public class SimpleBlobExample : MonoBehaviour
    {
        // Constants
        private static readonly string BLOBPARAMS_YML_FILEPATH = "OpenCVForUnityExamples/features/blobparams.yml";

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
        private string _blobparamsYmlFilepath;

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

            _blobparamsYmlFilepath = await OpenCVForUnityEnv.GetFilePathAsync(BLOBPARAMS_YML_FILEPATH, cancellationToken: _cts.Token);

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

            Texture2D imgTexture = Resources.Load("detect_blob") as Texture2D;

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC1);

            OpenCVMatUnityUtils.Texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString(), this);

            Mat outImgMat = new Mat();

            // Configure blob detector thresholds and optional shape filters.
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
            Debug.Log("blobDetector.getDefaultName() " + blobDetector.getDefaultName(), this);

            // or

            ////load Params from yml file.
            //SimpleBlobDetector blobDetector = SimpleBlobDetector.create();
            //Debug.Log("blobDetector.getDefaultName() " + blobDetector.getDefaultName());
            //blobDetector.read(_blobparamsYmlFilepath);

            // Detect blob centers and draw them as keypoints.
            MatOfKeyPoint keypoints = new MatOfKeyPoint();
            blobDetector.detect(imgMat, keypoints);
            Features.drawKeypoints(imgMat, keypoints, outImgMat);

            Texture2D texture = new Texture2D(outImgMat.cols(), outImgMat.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUnityUtils.MatToTexture2D(outImgMat, texture);

            ResultPreview.texture = texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;

            OpenCVDebug.SetDebugMode(false);
        }
    }
}
