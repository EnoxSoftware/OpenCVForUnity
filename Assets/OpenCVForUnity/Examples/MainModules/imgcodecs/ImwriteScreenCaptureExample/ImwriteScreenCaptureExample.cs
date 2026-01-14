using System.Collections;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    public class ImwriteScreenCaptureExample : MonoBehaviour
    {
        // Constants
        private static readonly string DEFAULT_OUTPUT_FILENAME = "/ImwriteScreenCaptureExample_output.jpg";

        // Public Fields
        public GameObject Cube;
        public InputField SavePathInputField;

        // Private Fields
        private string _savePath;
        private bool _isCapturing;

        private void Start()
        {
            _savePath = Application.persistentDataPath + DEFAULT_OUTPUT_FILENAME;

            OpenCVDebug.SetDebugMode(true);

            Texture2D imgTexture = Resources.Load<Texture2D>("face");
            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            OpenCVMatUtils.Texture2DToMat(imgTexture, imgMat);

            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(imgMat, texture);

            Cube.GetComponent<Renderer>().material.mainTexture = texture;

            OpenCVDebug.SetDebugMode(false);
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
        /// Raises the capture screen button click event.
        /// </summary>
        public void OnCaptureScreenButtonClick()
        {
            StartCoroutine(CaptureScreenCoroutine());
        }

        /// <summary>
        /// Raises the load screen button click event.
        /// </summary>
        public void OnLoadScreenButtonClick()
        {
            Mat loadMat = Imgcodecs.imread(_savePath);
            Debug.Log("loadMat.ToString() " + loadMat.ToString());

            if (loadMat.width() > 0 && loadMat.height() > 0)
            {
                Imgproc.cvtColor(loadMat, loadMat, Imgproc.COLOR_BGR2RGB);

                Texture2D texture = new Texture2D(loadMat.width(), loadMat.height(), TextureFormat.RGBA32, false);
                OpenCVMatUtils.MatToTexture2D(loadMat, texture);

                Cube.GetComponent<Renderer>().material.mainTexture = texture;
            }
        }

        // Private Methods
        private IEnumerator CaptureScreenCoroutine()
        {
            if (_isCapturing)
                yield break;

            _isCapturing = true;

            // Wait for the frame to finish rendering (including UI / PostProcess, this is essential)
            yield return new WaitForEndOfFrame();

            Texture2D screenTex = ScreenCapture.CaptureScreenshotAsTexture();

            try
            {
                Debug.Log($"Capture size : {screenTex.width} x {screenTex.height}");

                Mat cameraMat = new Mat(screenTex.height, screenTex.width, CvType.CV_8UC4);
                OpenCVMatUtils.Texture2DToMat(screenTex, cameraMat);

                // OpenCV assumes BGRA format
                Imgproc.cvtColor(cameraMat, cameraMat, Imgproc.COLOR_RGBA2BGRA);

                // Debug drawing
                Imgproc.rectangle(cameraMat, new Point(0, 0), new Point(cameraMat.width(), cameraMat.height()), new Scalar(0, 0, 255, 255), 3);

                Imgproc.putText(cameraMat, "SavePath:", new Point(5, cameraMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar(0, 0, 255), 2, Imgproc.LINE_AA, false);

                Imgproc.putText(cameraMat, _savePath, new Point(5, cameraMat.rows() - 8), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 255, 255), 1, Imgproc.LINE_AA, false);

                Imgcodecs.imwrite(_savePath, cameraMat);

                SavePathInputField.text = _savePath;
                Debug.Log("savePath: " + _savePath);
            }
            finally
            {
                // Always destroy Texture2D
                Destroy(screenTex);
                _isCapturing = false;
            }
        }

    }
}

