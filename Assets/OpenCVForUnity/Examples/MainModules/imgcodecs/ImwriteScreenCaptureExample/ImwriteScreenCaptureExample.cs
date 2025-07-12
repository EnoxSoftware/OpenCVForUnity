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
    [RequireComponent(typeof(Camera))]
    public class ImwriteScreenCaptureExample : MonoBehaviour
    {
        // Constants
        private static readonly string DEFAULT_OUTPUT_FILENAME = "/ImwriteScreenCaptureExample_output.jpg";

        // Public Fields
        /// <summary>
        /// The cube.
        /// </summary>
        public GameObject Cube;

        /// <summary>
        /// The save path input field.
        /// </summary>
        public InputField SavePathInputField;

        // Private Fields
        private bool _captureFlag = false;
        private string _savePath;

        // Unity Lifecycle Methods
        private void Start()
        {
            _savePath = Application.persistentDataPath + DEFAULT_OUTPUT_FILENAME;

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            Texture2D imgTexture = Resources.Load("face") as Texture2D;

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            OpenCVMatUtils.Texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString());

            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);

            OpenCVMatUtils.MatToTexture2D(imgMat, texture);

            Cube.GetComponent<Renderer>().material.mainTexture = texture;

            OpenCVDebug.SetDebugMode(false);
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_captureFlag)
            {
                //Debug.Log ("source.width " + source.width + "source.height " + source.height);

                Mat cameraMat = new Mat(source.height, source.width, CvType.CV_8UC4);
                Texture2D texture = new Texture2D(cameraMat.width(), cameraMat.height(), TextureFormat.ARGB32, false);

                OpenCVMatUtils.TextureToTexture2D(source, texture);
                OpenCVMatUtils.Texture2DToMat(texture, cameraMat);

                Imgproc.cvtColor(cameraMat, cameraMat, Imgproc.COLOR_RGBA2BGRA);

                Imgproc.rectangle(cameraMat, new Point(0, 0), new Point(cameraMat.width(), cameraMat.height()), new Scalar(0, 0, 255, 255), 3);
                Imgproc.putText(cameraMat, "SavePath:", new Point(5, cameraMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar(0, 0, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(cameraMat, _savePath, new Point(5, cameraMat.rows() - 8), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 255, 255), 0, Imgproc.LINE_AA, false);

                Imgcodecs.imwrite(_savePath, cameraMat);

                SavePathInputField.text = _savePath;
                Debug.Log("savePath: " + _savePath);

                _captureFlag = false;
            }

            Graphics.Blit(source, destination);
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
            _captureFlag = true;
        }

        /// <summary>
        /// Raises the load screen button click event.
        /// </summary>
        public void OnLoadScreenButtonClick()
        {
            Mat loadMat = Imgcodecs.imread(_savePath);
            Debug.Log("loadMat.ToString() " + loadMat.ToString());

            if (loadMat.width() != 0 && loadMat.height() != 0)
            {
                Texture2D texture = new Texture2D(loadMat.width(), loadMat.height(), TextureFormat.RGBA32, false);

                Imgproc.cvtColor(loadMat, loadMat, Imgproc.COLOR_BGR2RGB);

                OpenCVMatUtils.MatToTexture2D(loadMat, texture);

                Cube.GetComponent<Renderer>().material.mainTexture = texture;
            }
        }
    }
}
