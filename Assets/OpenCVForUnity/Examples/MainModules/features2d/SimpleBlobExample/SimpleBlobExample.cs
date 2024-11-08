using OpenCVForUnity.CoreModule;
using OpenCVForUnity.Features2dModule;
using OpenCVForUnity.UnityUtils;
using System.Threading;
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
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        string blobparams_yml_filepath;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        /// <summary>
        /// The CancellationTokenSource.
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();

        // Use this for initialization
        async void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            // Asynchronously retrieves the readable file path from the StreamingAssets directory.
            if (fpsMonitor != null)
                fpsMonitor.consoleText = "Preparing file access...";

            blobparams_yml_filepath = await Utils.getFilePathAsyncTask("OpenCVForUnity/features2d/blobparams.yml", cancellationToken: cts.Token);

            if (fpsMonitor != null)
                fpsMonitor.consoleText = "";

            Run();
        }

        private void Run()
        {

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            Texture2D imgTexture = Resources.Load("detect_blob") as Texture2D;

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC1);

            Utils.texture2DToMat(imgTexture, imgMat);
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
            //blobDetector.read(blobparams_yml_filepath);


            MatOfKeyPoint keypoints = new MatOfKeyPoint();
            blobDetector.detect(imgMat, keypoints);
            Features2d.drawKeypoints(imgMat, keypoints, outImgMat);


            Texture2D texture = new Texture2D(outImgMat.cols(), outImgMat.rows(), TextureFormat.RGBA32, false);

            Utils.matToTexture2D(outImgMat, texture);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            Utils.setDebugMode(false);
        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
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
    }
}