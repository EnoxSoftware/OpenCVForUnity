using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.UnityUtils;

namespace OpenCVForUnityExample
{
    [RequireComponent(typeof(Camera))]
    public class ImwriteScreenCaptureExample : MonoBehaviour
    {
        /// <summary>
        /// The cube.
        /// </summary>
        public GameObject cube;

        /// <summary>
        /// The save path input field.
        /// </summary>
        public InputField savePathInputField;

        /// <summary>
        /// The capture flag.
        /// </summary>
        bool captureFlag = false;

        /// <summary>
        /// The save path.
        /// </summary>
        string savePath;

        // Use this for initialization
        void Start()
        {
            savePath = Application.persistentDataPath + "/ImwriteScreenCaptureExample_output.jpg";

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            Texture2D imgTexture = Resources.Load("face") as Texture2D;

            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            Utils.texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString());

            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);

            Utils.matToTexture2D(imgMat, texture);

            cube.GetComponent<Renderer>().material.mainTexture = texture;


            Utils.setDebugMode(false);
        }

        /// <summary>
        /// Raises the render image event.
        /// </summary>
        /// <param name="source">Source.</param>
        /// <param name="destination">Destination.</param>
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (captureFlag)
            {
                //Debug.Log ("source.width " + source.width + "source.height " + source.height);

                Mat cameraMat = new Mat(source.height, source.width, CvType.CV_8UC4);
                Texture2D texture = new Texture2D(cameraMat.width(), cameraMat.height(), TextureFormat.ARGB32, false);

                Utils.textureToTexture2D(source, texture);
                Utils.texture2DToMat(texture, cameraMat);

                Imgproc.cvtColor(cameraMat, cameraMat, Imgproc.COLOR_RGBA2BGRA);

                Imgproc.rectangle(cameraMat, new Point(0, 0), new Point(cameraMat.width(), cameraMat.height()), new Scalar(0, 0, 255, 255), 3);
                Imgproc.putText(cameraMat, "SavePath:", new Point(5, cameraMat.rows() - 30), Imgproc.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar(0, 0, 255), 2, Imgproc.LINE_AA, false);
                Imgproc.putText(cameraMat, savePath, new Point(5, cameraMat.rows() - 8), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 255, 255), 0, Imgproc.LINE_AA, false);

                Imgcodecs.imwrite(savePath, cameraMat);

                savePathInputField.text = savePath;
                Debug.Log("savePath: " + savePath);

                captureFlag = false;
            }

            Graphics.Blit(source, destination);
        }

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
            captureFlag = true;
        }

        /// <summary>
        /// Raises the load screen button click event.
        /// </summary>
        public void OnLoadScreenButtonClick()
        {
            Mat loadMat = Imgcodecs.imread(savePath);
            Debug.Log("loadMat.ToString() " + loadMat.ToString());

            if (loadMat.width() != 0 && loadMat.height() != 0)
            {

                Texture2D texture = new Texture2D(loadMat.width(), loadMat.height(), TextureFormat.RGBA32, false);

                Imgproc.cvtColor(loadMat, loadMat, Imgproc.COLOR_BGR2RGB);

                Utils.matToTexture2D(loadMat, texture);

                cube.GetComponent<Renderer>().material.mainTexture = texture;
            }
        }
    }
}
