using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Utils_GetFilePath Example
    /// An example of how to get the readable path of a file in the "StreamingAssets" folder using the OpenCVEnv class.
    /// </summary>
    public class Utils_GetFilePathExample : MonoBehaviour
    {
        // Enums
        public enum TimeoutPreset : int
        {
            _0 = 0,
            _1 = 1,
            _10 = 10,
        }

        // Public Fields
        /// <summary>
        /// The file path dropdown.
        /// </summary>
        public Dropdown FilePathDropdown;

        /// <summary>
        /// The refresh toggle.
        /// </summary>
        public Toggle RefreshToggle;

        /// <summary>
        /// The timeout dropdown.
        /// </summary>
        public Dropdown TimeoutDropdown;

        /// <summary>
        /// The get file path button.
        /// </summary>
        public Button GetFilePathButton;

        /// <summary>
        /// The get multiple file paths button.
        /// </summary>
        public Button GetMultipleFilePathsButton;

        /// <summary>
        /// The get file path coroutine button.
        /// </summary>
        public Button GetFilePathCoroutineButton;

        /// <summary>
        /// The get multiple file paths coroutine button.
        /// </summary>
        public Button GetMultipleFilePathsCoroutineButton;

        /// <summary>
        /// The get file path async button.
        /// </summary>
        public Button GetFilePathAsyncButton;

        /// <summary>
        /// The get multiple file paths async button.
        /// </summary>
        public Button GetMultipleFilePathsAsyncButton;

        /// <summary>
        /// The get file path task async button.
        /// </summary>
        public Button GetFilePathTaskAsyncButton;

        /// <summary>
        /// The get multiple file paths task async button.
        /// </summary>
        public Button GetMultipleFilePathsTaskAsyncButton;

        /// <summary>
        /// The abort button.
        /// </summary>
        public Button AbortButton;

        /// <summary>
        /// The file path input field.
        /// </summary>
        public Text FilePathInputField;

        // Private Fields
        private string[] _filePathPreset = new string[] {
            "OpenCVForUnityExamples/768x576_mjpeg.mjpeg",
            "/OpenCVForUnityExamples/objdetect/lbpcascade_frontalface.xml",
            "OpenCVForUnityExamples/objdetect/calibration_images/left01.jpg",
            "xxxxxxx.xxx"
        };

        private IEnumerator _getFilePathCoroutine;

        private CancellationTokenSource _cancellationTokenSource = default;

        // Unity Lifecycle Methods
        private void Start()
        {
            AbortButton.interactable = false;

#if !UNITY_2023_1_OR_NEWER
            GetFilePathAsyncButton.interactable = false;
            GetMultipleFilePathsAsyncButton.interactable = false;
#endif
        }

        private void OnDestroy()
        {
            if (_getFilePathCoroutine != null)
            {
                StopCoroutine(_getFilePathCoroutine);
                ((IDisposable)_getFilePathCoroutine).Dispose();
            }

            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
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
        /// Raises the get file path button click event.
        /// </summary>
        public void OnGetFilePathButtonClick()
        {
            bool refresh = RefreshToggle.isOn;
            string[] enumNames = Enum.GetNames(typeof(TimeoutPreset));
            int timeout = (int)System.Enum.Parse(typeof(TimeoutPreset), enumNames[TimeoutDropdown.value], true);

            FilePathInputField.text = "";

            GetFilePath(_filePathPreset[FilePathDropdown.value], refresh, timeout);
        }

        /// <summary>
        /// Raises the get multiple file paths button click event.
        /// </summary>
        public void OnGetMultipleFilePathsButtonClick()
        {
            bool refresh = RefreshToggle.isOn;
            string[] enumNames = Enum.GetNames(typeof(TimeoutPreset));
            int timeout = (int)System.Enum.Parse(typeof(TimeoutPreset), enumNames[TimeoutDropdown.value], true);

            FilePathInputField.text = "";

            GetMultipleFilePaths(_filePathPreset, refresh, timeout);
        }

        /// <summary>
        /// Raises the get file path coroutine button click event.
        /// </summary>
        public void OnGetFilePathCoroutineButtonClick()
        {
            bool refresh = RefreshToggle.isOn;
            string[] enumNames = Enum.GetNames(typeof(TimeoutPreset));
            int timeout = (int)System.Enum.Parse(typeof(TimeoutPreset), enumNames[TimeoutDropdown.value], true);

            FilePathInputField.text = "";

            GetFilePathCoroutine(_filePathPreset[FilePathDropdown.value], refresh, timeout);
        }

        /// <summary>
        /// Raises the get multiple file paths coroutine button click event.
        /// </summary>
        public void OnGetMultipleFilePathsCoroutineButtonClick()
        {
            bool refresh = RefreshToggle.isOn;
            string[] enumNames = Enum.GetNames(typeof(TimeoutPreset));
            int timeout = (int)System.Enum.Parse(typeof(TimeoutPreset), enumNames[TimeoutDropdown.value], true);

            FilePathInputField.text = "";

            GetMultipleFilePathsCoroutine(_filePathPreset, refresh, timeout);
        }

        /// <summary>
        /// Raises the get file path async button click event.
        /// </summary>
        public async void OnGetFilePathAsyncButtonClick()
        {
#if UNITY_2023_1_OR_NEWER
            bool refresh = RefreshToggle.isOn;
            string[] enumNames = Enum.GetNames(typeof(TimeoutPreset));
            int timeout = (int)System.Enum.Parse(typeof(TimeoutPreset), enumNames[TimeoutDropdown.value], true);

            FilePathInputField.text = "";

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await GetFilePathAsync(_filePathPreset[FilePathDropdown.value], refresh, timeout, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("# canceled: " + "The task was canceled externally. OperationCanceledException");
                FilePathInputField.text = FilePathInputField.text + "# canceled: " + "The task was canceled externally. OperationCanceledException" + "\n";
            }
#else
            await Task.CompletedTask;
#endif
        }

        /// <summary>
        /// Raises the get multiple file paths async button click event.
        /// </summary>
        public async void OnGetMultipleFilePathsAsyncButtonClick()
        {
#if UNITY_2023_1_OR_NEWER
            bool refresh = RefreshToggle.isOn;
            string[] enumNames = Enum.GetNames(typeof(TimeoutPreset));
            int timeout = (int)System.Enum.Parse(typeof(TimeoutPreset), enumNames[TimeoutDropdown.value], true);

            FilePathInputField.text = "";

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await GetMultipleFilePathsAsync(_filePathPreset, refresh, timeout, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("# canceled: " + "The task was canceled externally. OperationCanceledException");
                FilePathInputField.text = FilePathInputField.text + "# canceled: " + "The task was canceled externally. OperationCanceledException" + "\n";
            }
#else
            await Task.CompletedTask;
#endif
        }

        /// <summary>
        /// Raises the get file path task async button click event.
        /// </summary>
        public async void OnGetFilePathTaskAsyncButtonClick()
        {
            bool refresh = RefreshToggle.isOn;
            string[] enumNames = Enum.GetNames(typeof(TimeoutPreset));
            int timeout = (int)System.Enum.Parse(typeof(TimeoutPreset), enumNames[TimeoutDropdown.value], true);

            FilePathInputField.text = "";

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await GetFilePathTaskAsync(_filePathPreset[FilePathDropdown.value], refresh, timeout, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("# canceled: " + "The task was canceled externally. OperationCanceledException");
                FilePathInputField.text = FilePathInputField.text + "# canceled: " + "The task was canceled externally. OperationCanceledException" + "\n";
            }
        }

        /// <summary>
        /// Raises the get multiple file paths task async button click event.
        /// </summary>
        public async void OnGetMultipleFilePathsTaskAsyncButtonClick()
        {
            bool refresh = RefreshToggle.isOn;
            string[] enumNames = Enum.GetNames(typeof(TimeoutPreset));
            int timeout = (int)System.Enum.Parse(typeof(TimeoutPreset), enumNames[TimeoutDropdown.value], true);

            FilePathInputField.text = "";

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await GetMultipleFilePathsTaskAsync(_filePathPreset, refresh, timeout, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("# canceled: " + "The task was canceled externally. OperationCanceledException");
                FilePathInputField.text = FilePathInputField.text + "# canceled: " + "The task was canceled externally. OperationCanceledException" + "\n";
            }
        }

        /// <summary>
        /// Raises the abort button click event.
        /// </summary>
        public void OnAbortButtonClick()
        {
            if (_getFilePathCoroutine != null)
            {
                StopCoroutine(_getFilePathCoroutine);
                ((IDisposable)_getFilePathCoroutine).Dispose();

                Debug.Log("# canceled: " + "The getFilePath_Coroutine was stoped externally.");
                FilePathInputField.text = FilePathInputField.text + "# canceled: " + "The getFilePath_Coroutine was stoped externally." + "\n";
            }

            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            ShowButton();
        }

        /// <summary>
        /// Raises the on scroll rect value changed event.
        /// </summary>
        public void OnScrollRectValueChanged()
        {
            if (FilePathInputField.text.Length > 10000)
            {
                FilePathInputField.text = FilePathInputField.text.Substring(FilePathInputField.text.Length - 10000);
            }
        }

        // Private Methods
        private void GetFilePath(string filePath, bool refresh, int timeout)
        {
            var readableFilePath = OpenCVEnv.GetFilePath(filePath, refresh, timeout);

#if UNITY_WEBGL
            Debug.Log("The OpenCVEnv.GetFilePath() method is not supported on WebGL platform.");
            FilePathInputField.text = FilePathInputField.text + "The OpenCVEnv.GetFilePath() method is not supported on WebGL platform." + "\n";
            if (!string.IsNullOrEmpty(readableFilePath))
            {
                Debug.Log("completed: " + "readableFilePath=" + readableFilePath);
                FilePathInputField.text = FilePathInputField.text + "completed: " + "readableFilePath=" + readableFilePath;
            }
#else
            if (string.IsNullOrEmpty(readableFilePath))
            {
                Debug.LogWarning("# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
                FilePathInputField.text = FilePathInputField.text + "# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder." + "\n";
            }
            else
            {
                Debug.Log("# completed: " + "readableFilePath= " + readableFilePath);
                FilePathInputField.text = FilePathInputField.text + "# completed: " + "readableFilePath= " + readableFilePath + "\n";
            }
#endif
        }

        private void GetMultipleFilePaths(string[] filePaths, bool refresh, int timeout)
        {
            var readableFilePaths = OpenCVEnv.GetMultipleFilePaths(filePaths, refresh, timeout);

#if UNITY_WEBGL
            Debug.Log("The OpenCVEnv.GetMultipleFilePaths() method is not supported on WebGL platform.");
            FilePathInputField.text = FilePathInputField.text + "The OpenCVEnv.GetMultipleFilePaths() method is not supported on WebGL platform." + "\n";
            for (int i = 0; i < readableFilePaths.Count; i++)
            {
                if (!string.IsNullOrEmpty(readableFilePaths[i]))
                {
                    Debug.Log("readableFilePath[" + i + "]=" + readableFilePaths[i]);
                    FilePathInputField.text = FilePathInputField.text + "readableFilePath[" + i + "]=" + readableFilePaths[i];
                }
            }
#else
            Debug.Log("### allCompleted:" + "\n");
            FilePathInputField.text = FilePathInputField.text + "### allCompleted:" + "\n";
            for (int i = 0; i < readableFilePaths.Count; i++)
            {
                if (string.IsNullOrEmpty(readableFilePaths[i]))
                {
                    Debug.LogWarning("readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
                    FilePathInputField.text = FilePathInputField.text + "readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder." + "\n";
                }
                else
                {
                    Debug.Log("readableFilePath[" + i + "]= " + readableFilePaths[i]);
                    FilePathInputField.text = FilePathInputField.text + "readableFilePath[" + i + "]= " + readableFilePaths[i] + "\n";
                }
            }
#endif
        }

        private void GetFilePathCoroutine(string filePath, bool refresh, int timeout)
        {
            HideButton();

            _getFilePathCoroutine = OpenCVEnv.GetFilePathCoroutine(
                filePath,
                (result) =>
                { // completed callback
                    _getFilePathCoroutine = null;
                    ShowButton();


                    string readableFilePath = result;

                    if (string.IsNullOrEmpty(readableFilePath))
                    {
                        Debug.LogWarning("# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
                        FilePathInputField.text = FilePathInputField.text + "# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder." + "\n";
                    }

                    Debug.Log("# completed: " + "readableFilePath= " + readableFilePath);
                    FilePathInputField.text = FilePathInputField.text + "# completed: " + "readableFilePath= " + readableFilePath + "\n";

                },
                (path, progress) =>
                { // progressChanged callback
                    Debug.Log("# progressChanged: " + "path= " + path + " progress= " + progress);
                    FilePathInputField.text = FilePathInputField.text + "# progressChanged: " + "path= " + path + " progress= " + progress + "\n";

                },
                (path, error, responseCode) =>
                { // errorOccurred callback
                    _getFilePathCoroutine = null;
                    ShowButton();

                    Debug.Log("# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode);
                    FilePathInputField.text = FilePathInputField.text + "# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode + "\n";

                },
                refresh, timeout);

            StartCoroutine(_getFilePathCoroutine);
        }

        private void GetMultipleFilePathsCoroutine(string[] filePaths, bool refresh, int timeout)
        {
            HideButton();

            _getFilePathCoroutine = OpenCVEnv.GetMultipleFilePathsCoroutine(
                filePaths,
                (result) =>
                { // allCompleted callback
                    _getFilePathCoroutine = null;
                    ShowButton();

                    var readableFilePaths = result;

                    Debug.Log("### allCompleted:" + "\n");
                    FilePathInputField.text = FilePathInputField.text + "### allCompleted:" + "\n";
                    for (int i = 0; i < readableFilePaths.Count; i++)
                    {
                        if (string.IsNullOrEmpty(readableFilePaths[i]))
                        {
                            Debug.LogWarning("readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
                            FilePathInputField.text = FilePathInputField.text + "readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder." + "\n";
                        }
                        else
                        {
                            Debug.Log("readableFilePath[" + i + "]= " + readableFilePaths[i]);
                            FilePathInputField.text = FilePathInputField.text + "readableFilePath[" + i + "]= " + readableFilePaths[i] + "\n";
                        }
                    }

                },
                (path) =>
                { // completed callback
                    Debug.Log("# completed: " + "path= " + path);
                    FilePathInputField.text = FilePathInputField.text + "# completed: " + "path= " + path + "\n";

                },
                (path, progress) =>
                { // progressChanged callback
                    Debug.Log("# progressChanged: " + "path= " + path + " progress= " + progress);
                    FilePathInputField.text = FilePathInputField.text + "# progressChanged: " + "path= " + path + " progress= " + progress + "\n";

                },
                (path, error, responseCode) =>
                { // errorOccurred callback
                    Debug.Log("# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode);
                    FilePathInputField.text = FilePathInputField.text + "# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode + "\n";

                },
                refresh, timeout);

            StartCoroutine(_getFilePathCoroutine);
        }

#if UNITY_2023_1_OR_NEWER
        private async Awaitable GetFilePathAsync(string filePath, bool refresh, int timeout, CancellationToken cancellationToken = default)
        {
            HideButton();

            var result = await OpenCVEnv.GetFilePathAsync(
                filePath,
                (path, progress) =>
                { // progressChanged callback
                    Debug.Log("# progressChanged: " + "path= " + path + " progress= " + progress);
                    FilePathInputField.text = FilePathInputField.text + "# progressChanged: " + "path= " + path + " progress= " + progress + "\n";

                },
                (path, error, responseCode) =>
                { // errorOccurred callback
                    _getFilePathCoroutine = null;
                    ShowButton();

                    Debug.Log("# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode);
                    FilePathInputField.text = FilePathInputField.text + "# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode + "\n";

                },
                refresh, timeout, cancellationToken);


            ShowButton();

            string readableFilePath = result;

            if (string.IsNullOrEmpty(readableFilePath))
            {
                Debug.LogWarning("# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
                FilePathInputField.text = FilePathInputField.text + "# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder." + "\n";
            }

            Debug.Log("# completed: " + "readableFilePath= " + readableFilePath);
            FilePathInputField.text = FilePathInputField.text + "# completed: " + "readableFilePath= " + readableFilePath + "\n";
        }

        private async Awaitable GetMultipleFilePathsAsync(string[] filePaths, bool refresh, int timeout, CancellationToken cancellationToken = default)
        {
            HideButton();

            var result = await OpenCVEnv.GetMultipleFilePathsAsync(
                filePaths,
                (path) =>
                { // completed callback
                    Debug.Log("# completed: " + "path= " + path);
                    FilePathInputField.text = FilePathInputField.text + "# completed: " + "path= " + path + "\n";

                },
                (path, progress) =>
                { // progressChanged callback
                    Debug.Log("# progressChanged: " + "path= " + path + " progress= " + progress);
                    FilePathInputField.text = FilePathInputField.text + "# progressChanged: " + "path= " + path + " progress= " + progress + "\n";

                },
                (path, error, responseCode) =>
                { // errorOccurred callback
                    Debug.Log("# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode);
                    FilePathInputField.text = FilePathInputField.text + "# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode + "\n";

                },
                refresh, timeout, cancellationToken);


            ShowButton();

            var readableFilePaths = result;

            Debug.Log("### allCompleted:" + "\n");
            FilePathInputField.text = FilePathInputField.text + "### allCompleted:" + "\n";
            for (int i = 0; i < readableFilePaths.Count; i++)
            {
                if (string.IsNullOrEmpty(readableFilePaths[i]))
                {
                    Debug.LogWarning("readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
                    FilePathInputField.text = FilePathInputField.text + "readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder." + "\n";
                }
                else
                {
                    Debug.Log("readableFilePath[" + i + "]= " + readableFilePaths[i]);
                    FilePathInputField.text = FilePathInputField.text + "readableFilePath[" + i + "]= " + readableFilePaths[i] + "\n";
                }
            }
        }
#endif

        private async Task GetFilePathTaskAsync(string filePath, bool refresh, int timeout, CancellationToken cancellationToken = default)
        {
            HideButton();

            var result = await OpenCVEnv.GetFilePathTaskAsync(
                filePath,
                (path, progress) =>
                { // progressChanged callback
                    Debug.Log("# progressChanged: " + "path= " + path + " progress= " + progress);
                    FilePathInputField.text = FilePathInputField.text + "# progressChanged: " + "path= " + path + " progress= " + progress + "\n";

                },
                (path, error, responseCode) =>
                { // errorOccurred callback
                    _getFilePathCoroutine = null;
                    ShowButton();

                    Debug.Log("# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode);
                    FilePathInputField.text = FilePathInputField.text + "# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode + "\n";

                },
                refresh, timeout, cancellationToken);


            ShowButton();

            string readableFilePath = result;

            if (string.IsNullOrEmpty(readableFilePath))
            {
                Debug.LogWarning("# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
                FilePathInputField.text = FilePathInputField.text + "# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder." + "\n";
            }

            Debug.Log("# completed: " + "readableFilePath= " + readableFilePath);
            FilePathInputField.text = FilePathInputField.text + "# completed: " + "readableFilePath= " + readableFilePath + "\n";
        }

        private async Task GetMultipleFilePathsTaskAsync(string[] filePaths, bool refresh, int timeout, CancellationToken cancellationToken = default)
        {
            HideButton();

            var result = await OpenCVEnv.GetMultipleFilePathsTaskAsync(
                filePaths,
                (path) =>
                { // completed callback
                    Debug.Log("# completed: " + "path= " + path);
                    FilePathInputField.text = FilePathInputField.text + "# completed: " + "path= " + path + "\n";

                },
                (path, progress) =>
                { // progressChanged callback
                    Debug.Log("# progressChanged: " + "path= " + path + " progress= " + progress);
                    FilePathInputField.text = FilePathInputField.text + "# progressChanged: " + "path= " + path + " progress= " + progress + "\n";

                },
                (path, error, responseCode) =>
                { // errorOccurred callback
                    Debug.Log("# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode);
                    FilePathInputField.text = FilePathInputField.text + "# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode + "\n";

                },
                refresh, timeout, cancellationToken);


            ShowButton();

            var readableFilePaths = result;

            Debug.Log("### allCompleted:" + "\n");
            FilePathInputField.text = FilePathInputField.text + "### allCompleted:" + "\n";
            for (int i = 0; i < readableFilePaths.Count; i++)
            {
                if (string.IsNullOrEmpty(readableFilePaths[i]))
                {
                    Debug.LogWarning("readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.");
                    FilePathInputField.text = FilePathInputField.text + "readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder." + "\n";
                }
                else
                {
                    Debug.Log("readableFilePath[" + i + "]= " + readableFilePaths[i]);
                    FilePathInputField.text = FilePathInputField.text + "readableFilePath[" + i + "]= " + readableFilePaths[i] + "\n";
                }
            }
        }

        private void ShowButton()
        {
            GetFilePathButton.interactable = true;
            GetMultipleFilePathsButton.interactable = true;
            GetFilePathCoroutineButton.interactable = true;
            GetMultipleFilePathsCoroutineButton.interactable = true;
#if UNITY_2023_1_OR_NEWER
            GetFilePathAsyncButton.interactable = true;
            GetMultipleFilePathsAsyncButton.interactable = true;
#endif
            GetFilePathTaskAsyncButton.interactable = true;
            GetMultipleFilePathsTaskAsyncButton.interactable = true;
            AbortButton.interactable = false;
        }

        private void HideButton()
        {
            GetFilePathButton.interactable = false;
            GetMultipleFilePathsButton.interactable = false;
            GetFilePathCoroutineButton.interactable = false;
            GetMultipleFilePathsCoroutineButton.interactable = false;
#if UNITY_2023_1_OR_NEWER
            GetFilePathAsyncButton.interactable = false;
            GetMultipleFilePathsAsyncButton.interactable = false;
#endif
            GetFilePathTaskAsyncButton.interactable = false;
            GetMultipleFilePathsTaskAsyncButton.interactable = false;
            AbortButton.interactable = true;
        }
    }
}
