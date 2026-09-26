using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using OpenCVDebug = OpenCVForUnity.Extensions.OpenCVDebug;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Texture2DToMat Example
    /// Loads a Unity <see cref="Texture2D"/> and converts it to and from an OpenCV <see cref="Mat"/>.
    ///
    /// Demonstrates:
    /// - Loading a sample image from <see cref="Resources"/>
    /// - Creating a Mat with a matching element type (<see cref="CvType.CV_8UC4"/>)
    /// - Round-trip conversion with <see cref="OpenCVMatUnityUtils.Texture2DToMat"/> and MatToTexture2D
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="Mat"/>, <see cref="CvType"/>
    /// - <see cref="OpenCVMatUnityUtils"/>: Texture2DToMat, MatToTexture2D
    ///
    /// Unity integration:
    /// - <see cref="CvType.CV_8UC4"/> aligns with Unity <see cref="TextureFormat.RGBA32"/> channel layout
    /// - Enable <see cref="OpenCVDebug.SetDebugMode"/> to surface native OpenCV errors in the Editor console
    /// </summary>
    public class Texture2DToMatExample : MonoBehaviour
    {
        // Unity Lifecycle Methods
        private void Start()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);

            // Load the image texture from the Resources folder (path is relative to any Resources/ folder).
            Texture2D imgTexture = Resources.Load("face") as Texture2D;

            // CV_8UC4 = 8-bit unsigned, 4 channels; matches TextureFormat.RGBA32.
            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            // Copies Texture2D pixel data into the Mat buffer (RGBA layout).
            OpenCVMatUnityUtils.Texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString(), this);

            // Create a display Texture2D sized to Mat cols()/rows() (width/height).
            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);

            // Copy processed Mat data back to Unity texture memory for rendering.
            OpenCVMatUnityUtils.MatToTexture2D(imgMat, texture);

            // Assign the created texture to the mainTexture of the game object's material
            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            OpenCVDebug.SetDebugMode(false);
        }

        private void Update()
        {
            // Update logic (not used in this example)
        }

        // Public Methods
        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            // Load the specified scene when the back button is clicked
            SceneManager.LoadScene("OpenCVForUnityExample");
        }
    }
}
