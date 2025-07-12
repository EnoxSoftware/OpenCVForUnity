using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenCVForUnityExample
{
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
