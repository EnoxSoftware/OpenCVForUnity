using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Show License
    /// A scene that displays the OpenCVForUnity license text.
    /// </summary>
    public class ShowLicense : MonoBehaviour
    {
        // Unity Lifecycle Methods
        private void Start()
        {

        }

        private void Update()
        {

        }

        // Public Methods
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }
    }
}
