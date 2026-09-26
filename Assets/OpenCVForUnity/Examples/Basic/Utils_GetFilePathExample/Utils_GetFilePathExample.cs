using System;
using System.Collections;
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
    /// Resolves readable file paths for assets stored under StreamingAssets using <see cref="OpenCVForUnityEnv"/>.
    ///
    /// Demonstrates:
    /// - Synchronous, coroutine, async, and Task-based path resolution APIs
    /// - Resolving single and multiple files with optional refresh and timeout
    /// - Handling missing files and platform-specific limitations (WebGL)
    /// - Cancelling long-running coroutine or async operations
    ///
    /// OpenCV classes and APIs used:
    /// - <see cref="OpenCVForUnityEnv"/>: GetFilePath, GetMultipleFilePaths, GetFilePathCoroutine, GetFilePathAwaitableAsync, GetFilePathAsync
    ///
    /// Unity integration:
    /// - Paths are relative to <c>Assets/StreamingAssets/OpenCVForUnityExamples/</c>
    /// - On Android/iOS, files may be copied to persistent storage on first access; use refresh to force re-copy
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
        /// The get file path awaitable async button.
        /// </summary>
        public Button GetFilePathAwaitableAsyncButton;

        /// <summary>
        /// The get multiple file paths awaitable async button.
        /// </summary>
        public Button GetMultipleFilePathsAwaitableAsyncButton;

        /// <summary>
        /// The get file path async button.
        /// </summary>
        public Button GetFilePathAsyncButton;

        /// <summary>
        /// The get multiple file paths async button.
        /// </summary>
        public Button GetMultipleFilePathsAsyncButton;

        /// <summary>
        /// The abort button.
        /// </summary>
        public Button AbortButton;

        /// <summary>
        /// The file path input field.
        /// </summary>
        public Text FilePathInputField;

        // Private Fields
        // Paths relative to Assets/StreamingAssets/OpenCVForUnityExamples/ (see OpenCVForUnityEnv docs).
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
            GetFilePathAwaitableAsyncButton.interactable = false;
            GetMultipleFilePathsAwaitableAsyncButton.interactable = false;
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
        /// Raises the get file path awaitable async button click event.
        /// </summary>
        public async void OnGetFilePathAwaitableAsyncButtonClick()
        {
#if UNITY_2023_1_OR_NEWER
            bool refresh = RefreshToggle.isOn;
            string[] enumNames = Enum.GetNames(typeof(TimeoutPreset));
            int timeout = (int)System.Enum.Parse(typeof(TimeoutPreset), enumNames[TimeoutDropdown.value], true);

            FilePathInputField.text = "";

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await GetFilePathAwaitableAsync(_filePathPreset[FilePathDropdown.value], refresh, timeout, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("# canceled: " + "The task was canceled externally. OperationCanceledException", this);
                FilePathInputField.text = FilePathInputField.text + "# canceled: " + "The task was canceled externally. OperationCanceledException" + "\n";
            }
#else
            await Task.CompletedTask;
#endif
        }

        /// <summary>
        /// Raises the get multiple file paths awaitable async button click event.
        /// </summary>
        public async void OnGetMultipleFilePathsAwaitableAsyncButtonClick()
        {
#if UNITY_2023_1_OR_NEWER
            bool refresh = RefreshToggle.isOn;
            string[] enumNames = Enum.GetNames(typeof(TimeoutPreset));
            int timeout = (int)System.Enum.Parse(typeof(TimeoutPreset), enumNames[TimeoutDropdown.value], true);

            FilePathInputField.text = "";

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await GetMultipleFilePathsAwaitableAsync(_filePathPreset, refresh, timeout, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("# canceled: " + "The task was canceled externally. OperationCanceledException", this);
                FilePathInputField.text = FilePathInputField.text + "# canceled: " + "The task was canceled externally. OperationCanceledException" + "\n";
            }
#else
            await Task.CompletedTask;
#endif
        }

        /// <summary>
        /// Raises the get file path async button click event.
        /// </summary>
        public async void OnGetFilePathAsyncButtonClick()
        {
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
                Debug.Log("# canceled: " + "The task was canceled externally. OperationCanceledException", this);
                FilePathInputField.text = FilePathInputField.text + "# canceled: " + "The task was canceled externally. OperationCanceledException" + "\n";
            }
        }

        /// <summary>
        /// Raises the get multiple file paths async button click event.
        /// </summary>
        public async void OnGetMultipleFilePathsAsyncButtonClick()
        {
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
                Debug.Log("# canceled: " + "The task was canceled externally. OperationCanceledException", this);
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

                Debug.Log("# canceled: " + "The getFilePath_Coroutine was stoped externally.", this);
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
            // Returns a platform-readable path; empty if the file is missing from StreamingAssets.
            var readableFilePath = OpenCVForUnityEnv.GetFilePath(filePath, refresh, timeout);

#if UNITY_WEBGL
            Debug.Log("The OpenCVForUnityEnv.GetFilePath() method is not supported on WebGL platform.", this);
            FilePathInputField.text = FilePathInputField.text + "The OpenCVForUnityEnv.GetFilePath() method is not supported on WebGL platform." + "\n";
            if (!string.IsNullOrEmpty(readableFilePath))
            {
                Debug.Log("completed: " + "readableFilePath=" + readableFilePath, this);
                FilePathInputField.text = FilePathInputField.text + "completed: " + "readableFilePath=" + readableFilePath;
            }
#else
            if (string.IsNullOrEmpty(readableFilePath))
            {
                Debug.LogWarning("# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                FilePathInputField.text = FilePathInputField.text + "# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder." + "\n";
            }
            else
            {
                Debug.Log("# completed: " + "readableFilePath= " + readableFilePath, this);
                FilePathInputField.text = FilePathInputField.text + "# completed: " + "readableFilePath= " + readableFilePath + "\n";
            }
#endif
        }

        private void GetMultipleFilePaths(string[] filePaths, bool refresh, int timeout)
        {
            // Resolves each path in order; refresh=true forces re-copy from StreamingAssets on supported platforms.
            var readableFilePaths = OpenCVForUnityEnv.GetMultipleFilePaths(filePaths, refresh, timeout);

#if UNITY_WEBGL
            Debug.Log("The OpenCVForUnityEnv.GetMultipleFilePaths() method is not supported on WebGL platform.", this);
            FilePathInputField.text = FilePathInputField.text + "The OpenCVForUnityEnv.GetMultipleFilePaths() method is not supported on WebGL platform." + "\n";
            for (int i = 0; i < readableFilePaths.Count; i++)
            {
                if (!string.IsNullOrEmpty(readableFilePaths[i]))
                {
                    Debug.Log("readableFilePath[" + i + "]=" + readableFilePaths[i], this);
                    FilePathInputField.text = FilePathInputField.text + "readableFilePath[" + i + "]=" + readableFilePaths[i];
                }
            }
#else
            Debug.Log("### allCompleted:" + "\n", this);
            FilePathInputField.text = FilePathInputField.text + "### allCompleted:" + "\n";
            for (int i = 0; i < readableFilePaths.Count; i++)
            {
                if (string.IsNullOrEmpty(readableFilePaths[i]))
                {
                    Debug.LogWarning("readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                    FilePathInputField.text = FilePathInputField.text + "readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder." + "\n";
                }
                else
                {
                    Debug.Log("readableFilePath[" + i + "]= " + readableFilePaths[i], this);
                    FilePathInputField.text = FilePathInputField.text + "readableFilePath[" + i + "]= " + readableFilePaths[i] + "\n";
                }
            }
#endif
        }

        private void GetFilePathCoroutine(string filePath, bool refresh, int timeout)
        {
            HideButton();

            // Coroutine reports progress and errors via callbacks while copying from StreamingAssets.
            _getFilePathCoroutine = OpenCVForUnityEnv.GetFilePathCoroutine(
                filePath,
                (result) =>
                { // completed callback
                    _getFilePathCoroutine = null;
                    ShowButton();

                    string readableFilePath = result;

                    if (string.IsNullOrEmpty(readableFilePath))
                    {
                        Debug.LogWarning("# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                        FilePathInputField.text = FilePathInputField.text + "# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder." + "\n";
                    }

                    Debug.Log("# completed: " + "readableFilePath= " + readableFilePath, this);
                    FilePathInputField.text = FilePathInputField.text + "# completed: " + "readableFilePath= " + readableFilePath + "\n";

                },
                (path, progress) =>
                { // progressChanged callback
                    Debug.Log("# progressChanged: " + "path= " + path + " progress= " + progress, this);
                    FilePathInputField.text = FilePathInputField.text + "# progressChanged: " + "path= " + path + " progress= " + progress + "\n";

                },
                (path, error, responseCode) =>
                { // errorOccurred callback
                    _getFilePathCoroutine = null;
                    ShowButton();

                    Debug.Log("# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode, this);
                    FilePathInputField.text = FilePathInputField.text + "# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode + "\n";

                },
                refresh, timeout);

            StartCoroutine(_getFilePathCoroutine);
        }

        private void GetMultipleFilePathsCoroutine(string[] filePaths, bool refresh, int timeout)
        {
            HideButton();

            _getFilePathCoroutine = OpenCVForUnityEnv.GetMultipleFilePathsCoroutine(
                filePaths,
                (result) =>
                { // allCompleted callback
                    _getFilePathCoroutine = null;
                    ShowButton();

                    var readableFilePaths = result;

                    Debug.Log("### allCompleted:" + "\n", this);
                    FilePathInputField.text = FilePathInputField.text + "### allCompleted:" + "\n";
                    for (int i = 0; i < readableFilePaths.Count; i++)
                    {
                        if (string.IsNullOrEmpty(readableFilePaths[i]))
                        {
                            Debug.LogWarning("readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                            FilePathInputField.text = FilePathInputField.text + "readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder." + "\n";
                        }
                        else
                        {
                            Debug.Log("readableFilePath[" + i + "]= " + readableFilePaths[i], this);
                            FilePathInputField.text = FilePathInputField.text + "readableFilePath[" + i + "]= " + readableFilePaths[i] + "\n";
                        }
                    }
                },
                (path) =>
                { // completed callback
                    Debug.Log("# completed: " + "path= " + path, this);
                    FilePathInputField.text = FilePathInputField.text + "# completed: " + "path= " + path + "\n";

                },
                (path, progress) =>
                { // progressChanged callback
                    Debug.Log("# progressChanged: " + "path= " + path + " progress= " + progress, this);
                    FilePathInputField.text = FilePathInputField.text + "# progressChanged: " + "path= " + path + " progress= " + progress + "\n";

                },
                (path, error, responseCode) =>
                { // errorOccurred callback
                    Debug.Log("# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode, this);
                    FilePathInputField.text = FilePathInputField.text + "# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode + "\n";

                },
                refresh, timeout);

            StartCoroutine(_getFilePathCoroutine);
        }

#if UNITY_2023_1_OR_NEWER
        private async Awaitable GetFilePathAwaitableAsync(string filePath, bool refresh, int timeout, CancellationToken cancellationToken = default)
        {
            HideButton();

            var result = await OpenCVForUnityEnv.GetFilePathAwaitableAsync(
                filePath,
                (path, progress) =>
                { // progressChanged callback
                    Debug.Log("# progressChanged: " + "path= " + path + " progress= " + progress, this);
                    FilePathInputField.text = FilePathInputField.text + "# progressChanged: " + "path= " + path + " progress= " + progress + "\n";

                },
                (path, error, responseCode) =>
                { // errorOccurred callback
                    _getFilePathCoroutine = null;
                    ShowButton();

                    Debug.Log("# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode, this);
                    FilePathInputField.text = FilePathInputField.text + "# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode + "\n";

                },
                refresh, timeout, cancellationToken);

            ShowButton();

            string readableFilePath = result;

            if (string.IsNullOrEmpty(readableFilePath))
            {
                Debug.LogWarning("# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                FilePathInputField.text = FilePathInputField.text + "# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder." + "\n";
            }

            Debug.Log("# completed: " + "readableFilePath= " + readableFilePath, this);
            FilePathInputField.text = FilePathInputField.text + "# completed: " + "readableFilePath= " + readableFilePath + "\n";
        }

        private async Awaitable GetMultipleFilePathsAwaitableAsync(string[] filePaths, bool refresh, int timeout, CancellationToken cancellationToken = default)
        {
            HideButton();

            var result = await OpenCVForUnityEnv.GetMultipleFilePathsAwaitableAsync(
                filePaths,
                (path) =>
                { // completed callback
                    Debug.Log("# completed: " + "path= " + path, this);
                    FilePathInputField.text = FilePathInputField.text + "# completed: " + "path= " + path + "\n";

                },
                (path, progress) =>
                { // progressChanged callback
                    Debug.Log("# progressChanged: " + "path= " + path + " progress= " + progress, this);
                    FilePathInputField.text = FilePathInputField.text + "# progressChanged: " + "path= " + path + " progress= " + progress + "\n";

                },
                (path, error, responseCode) =>
                { // errorOccurred callback
                    Debug.Log("# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode, this);
                    FilePathInputField.text = FilePathInputField.text + "# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode + "\n";

                },
                refresh, timeout, cancellationToken);

            ShowButton();

            var readableFilePaths = result;

            Debug.Log("### allCompleted:" + "\n", this);
            FilePathInputField.text = FilePathInputField.text + "### allCompleted:" + "\n";
            for (int i = 0; i < readableFilePaths.Count; i++)
            {
                if (string.IsNullOrEmpty(readableFilePaths[i]))
                {
                    Debug.LogWarning("readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                    FilePathInputField.text = FilePathInputField.text + "readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder." + "\n";
                }
                else
                {
                    Debug.Log("readableFilePath[" + i + "]= " + readableFilePaths[i], this);
                    FilePathInputField.text = FilePathInputField.text + "readableFilePath[" + i + "]= " + readableFilePaths[i] + "\n";
                }
            }
        }
#endif

        private async Task GetFilePathAsync(string filePath, bool refresh, int timeout, CancellationToken cancellationToken = default)
        {
            HideButton();

            var result = await OpenCVForUnityEnv.GetFilePathAsync(
                filePath,
                (path, progress) =>
                { // progressChanged callback
                    Debug.Log("# progressChanged: " + "path= " + path + " progress= " + progress, this);
                    FilePathInputField.text = FilePathInputField.text + "# progressChanged: " + "path= " + path + " progress= " + progress + "\n";

                },
                (path, error, responseCode) =>
                { // errorOccurred callback
                    _getFilePathCoroutine = null;
                    ShowButton();

                    Debug.Log("# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode, this);
                    FilePathInputField.text = FilePathInputField.text + "# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode + "\n";

                },
                refresh, timeout, cancellationToken);

            ShowButton();

            string readableFilePath = result;

            if (string.IsNullOrEmpty(readableFilePath))
            {
                Debug.LogWarning("# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                FilePathInputField.text = FilePathInputField.text + "# completed: " + "readableFilePath= " + filePath + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder." + "\n";
            }

            Debug.Log("# completed: " + "readableFilePath= " + readableFilePath, this);
            FilePathInputField.text = FilePathInputField.text + "# completed: " + "readableFilePath= " + readableFilePath + "\n";
        }

        private async Task GetMultipleFilePathsAsync(string[] filePaths, bool refresh, int timeout, CancellationToken cancellationToken = default)
        {
            HideButton();

            var result = await OpenCVForUnityEnv.GetMultipleFilePathsAsync(
                filePaths,
                (path) =>
                { // completed callback
                    Debug.Log("# completed: " + "path= " + path, this);
                    FilePathInputField.text = FilePathInputField.text + "# completed: " + "path= " + path + "\n";

                },
                (path, progress) =>
                { // progressChanged callback
                    Debug.Log("# progressChanged: " + "path= " + path + " progress= " + progress, this);
                    FilePathInputField.text = FilePathInputField.text + "# progressChanged: " + "path= " + path + " progress= " + progress + "\n";

                },
                (path, error, responseCode) =>
                { // errorOccurred callback
                    Debug.Log("# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode, this);
                    FilePathInputField.text = FilePathInputField.text + "# errorOccurred: " + "path= " + path + " error= " + error + " responseCode= " + responseCode + "\n";

                },
                refresh, timeout, cancellationToken);

            ShowButton();

            var readableFilePaths = result;

            Debug.Log("### allCompleted:" + "\n", this);
            FilePathInputField.text = FilePathInputField.text + "### allCompleted:" + "\n";
            for (int i = 0; i < readableFilePaths.Count; i++)
            {
                if (string.IsNullOrEmpty(readableFilePaths[i]))
                {
                    Debug.LogWarning("readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder.", this);
                    FilePathInputField.text = FilePathInputField.text + "readableFilePath[" + i + "]= " + filePaths[i] + " is not loaded. Please move from \"OpenCVForUnity/StreamingAssets/OpenCVForUnityExamples/\" to \"Assets/StreamingAssets/OpenCVForUnityExamples/\" folder." + "\n";
                }
                else
                {
                    Debug.Log("readableFilePath[" + i + "]= " + readableFilePaths[i], this);
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
            GetFilePathAwaitableAsyncButton.interactable = true;
            GetMultipleFilePathsAwaitableAsyncButton.interactable = true;
#endif
            GetFilePathAsyncButton.interactable = true;
            GetMultipleFilePathsAsyncButton.interactable = true;
            AbortButton.interactable = false;
        }

        private void HideButton()
        {
            GetFilePathButton.interactable = false;
            GetMultipleFilePathsButton.interactable = false;
            GetFilePathCoroutineButton.interactable = false;
            GetMultipleFilePathsCoroutineButton.interactable = false;
#if UNITY_2023_1_OR_NEWER
            GetFilePathAwaitableAsyncButton.interactable = false;
            GetMultipleFilePathsAwaitableAsyncButton.interactable = false;
#endif
            GetFilePathAsyncButton.interactable = false;
            GetMultipleFilePathsAsyncButton.interactable = false;
            AbortButton.interactable = true;
        }
    }
}
