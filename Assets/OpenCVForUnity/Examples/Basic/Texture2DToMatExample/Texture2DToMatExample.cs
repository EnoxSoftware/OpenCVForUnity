using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Texture2DToMat Example
    /// An example of converting a Texture2D image to OpenCV's Mat format.
    /// </summary>
    public class Texture2DToMatExample : MonoBehaviour
    {
        // Unity Lifecycle Methods
        private void Start()
        {
            // if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true);


            // Load the image texture from the Resources folder
            Texture2D imgTexture = Resources.Load("face") as Texture2D;

            // Create a new Mat object with the same dimensions and color format as the texture
            Mat imgMat = new Mat(imgTexture.height, imgTexture.width, CvType.CV_8UC4);

            // Convert the Texture2D image to Mat format
            OpenCVMatUtils.Texture2DToMat(imgTexture, imgMat);
            Debug.Log("imgMat.ToString() " + imgMat.ToString());

            // Create a new Texture2D with the same dimensions and color format as the Mat
            Texture2D texture = new Texture2D(imgMat.cols(), imgMat.rows(), TextureFormat.RGBA32, false);

            // Convert the Mat back to Texture2D format
            OpenCVMatUtils.MatToTexture2D(imgMat, texture);

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
