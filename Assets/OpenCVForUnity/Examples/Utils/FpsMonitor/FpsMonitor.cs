using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    // v1.0.4
    public class FpsMonitor : MonoBehaviour
    {
        // Enums
        /// <summary>
        /// Alignment enum
        /// </summary>
        public enum Alignment
        {
            LeftTop,
            RightTop,
            LeftBottom,
            RightBottom,
        }

        // Constants
        private const float GUI_WIDTH = 95f;
        private const float GUI_HEIGHT = 35f;
        private const float MARGIN_X = 10f;
        private const float MARGIN_Y = 10f;
        private const float INNER_X = 10f;
        private const float INNER_Y = 10f;
        private const float GUI_CONSOLE_HEIGHT = 100f;
        private const string CANVAS_PREFAB_PATH = "FpsMonitorCanvas_104";

        // Public Fields
        public Alignment AlignmentSetting = Alignment.RightTop;
        public Vector2 Offset = new Vector2(MARGIN_X, MARGIN_Y);
        public bool BoxVisible = true;
        public float BoxWidth = GUI_WIDTH;
        public float BoxHeight = GUI_HEIGHT;
        public Vector2 Padding = new Vector2(INNER_X, INNER_Y);
        public float ConsoleHeight = GUI_CONSOLE_HEIGHT;

        // Private Fields
        private int _tick = 0;
        private float _elapsed = 0;
        private float _fps = 0;
        private Text _fpsTextComponent;
        private Text _consoleTextComponent;
        private GameObject _fpsPanel;
        private GameObject _consolePanel;
        private Canvas _canvas;
        private Dictionary<string, string> _outputDict = new Dictionary<string, string>();
        protected string _consoleText = null;
        private float _toastTime = -1;
        private GameObject _fpsBackgroundObj;
        private GameObject _consoleBackgroundObj;
        private bool _needsUpdate = false;
        private bool _isInitialized = false;
        private RectTransform _canvasRectTransform;
        private Vector2 _canvasSizeDelta;

        // Public Properties
        public virtual string ConsoleText
        {
            get { return _consoleText; }
            set
            {
                _consoleText = value;
                _toastTime = -1;
                UpdateConsoleText();
            }
        }

        // Unity Lifecycle Methods
        private void Awake()
        {
            LoadCanvasFromPrefab();
            LocateUI();
            _isInitialized = true;
            _needsUpdate = true;
        }

        private void OnDestroy()
        {
            if (_canvas != null)
            {
                Destroy(_canvas.gameObject);
            }
        }

        private void Update()
        {
            _tick++;
            _elapsed += Time.deltaTime;
            if (_elapsed >= 1f)
            {
                _fps = _tick / _elapsed;
                _tick = 0;
                _elapsed = 0;
                UpdateFpsText();
            }

            if (_toastTime > 0)
            {
                _toastTime -= Time.deltaTime * 1000; // Decrement in milliseconds
                if (_toastTime <= 0)
                {
                    _consoleText = "";
                    UpdateConsoleText();
                }
            }

            if (_needsUpdate)
            {
                _needsUpdate = false;
                UpdateUI();
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                _needsUpdate = true;
            }
        }

        // Public Methods
        public void Add(string key, string value)
        {
            if (_outputDict.ContainsKey(key))
            {
                _outputDict[key] = value;
            }
            else
            {
                _outputDict.Add(key, value);
            }
            UpdateFpsText();
        }

        public void Remove(string key)
        {
            _outputDict.Remove(key);
            UpdateFpsText();
        }

        public void Clear()
        {
            _outputDict.Clear();
            UpdateFpsText();
        }

        public void LocateGUI()
        {
            _needsUpdate = true;
        }

        public void Toast(string message, float timeMs = 2000f)  // Default is 2 seconds
        {
            _consoleText = message;
            _toastTime = Mathf.Max(timeMs, 1000f);  // Minimum 1 second

            if (!_isInitialized)
            {
                Debug.LogError("Toast: FpsMonitor is not initialized. Make sure FpsMonitor is properly set up in the scene.");
                return;
            }

            UpdateConsoleText();
        }

        // Private Methods
        private void LoadCanvasFromPrefab()
        {
            // Load Canvas from Prefab
            GameObject prefab = Resources.Load<GameObject>(CANVAS_PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError($"Failed to load FpsMonitorCanvas prefab from {CANVAS_PREFAB_PATH}");
                return;
            }

            // Instantiate Canvas
            GameObject canvasObj = Instantiate(prefab);
            // Set the name to the part before the first underscore (if any)
            int underscoreIndex = prefab.name.IndexOf('_');
            if (underscoreIndex > 0)
                canvasObj.name = prefab.name.Substring(0, underscoreIndex);
            else
                canvasObj.name = prefab.name;
            _canvas = canvasObj.GetComponent<Canvas>();
            if (_canvas == null)
            {
                Debug.LogError("FpsMonitorCanvas prefab does not have a Canvas component");
                return;
            }

            // Get references to required components
            _fpsPanel = canvasObj.transform.Find("FpsPanel")?.gameObject;
            _consolePanel = canvasObj.transform.Find("ConsolePanel")?.gameObject;

            if (_fpsPanel != null)
            {
                _fpsTextComponent = _fpsPanel.transform.Find("Mask/FpsText")?.GetComponent<Text>();
                _fpsBackgroundObj = _fpsPanel.transform.Find("Mask/Background")?.gameObject;
            }

            if (_consolePanel != null)
            {
                _consoleTextComponent = _consolePanel.transform.Find("Mask/ConsoleText")?.GetComponent<Text>();
                _consoleBackgroundObj = _consolePanel.transform.Find("Mask/Background")?.gameObject;
            }

            // Initial display settings
            if (_fpsPanel != null) _fpsPanel.SetActive(true);
            if (_consolePanel != null) _consolePanel.SetActive(false);

            // Show/hide background
            if (_fpsBackgroundObj != null) _fpsBackgroundObj.SetActive(BoxVisible);
            if (_consoleBackgroundObj != null) _consoleBackgroundObj.SetActive(BoxVisible);
        }

        private void LocateUI()
        {
            if (_fpsPanel == null || _consolePanel == null) return;

            // Set FPS panel position
            var fpsRect = _fpsPanel.GetComponent<RectTransform>();
            switch (AlignmentSetting)
            {
                case Alignment.LeftTop:
                    fpsRect.anchorMin = new Vector2(0, 1);
                    fpsRect.anchorMax = new Vector2(0, 1);
                    fpsRect.pivot = new Vector2(0, 1);
                    fpsRect.anchoredPosition = new Vector2(Offset.x, -Offset.y);
                    break;
                case Alignment.RightTop:
                    fpsRect.anchorMin = new Vector2(1, 1);
                    fpsRect.anchorMax = new Vector2(1, 1);
                    fpsRect.pivot = new Vector2(1, 1);
                    fpsRect.anchoredPosition = new Vector2(-Offset.x, -Offset.y);
                    break;
                case Alignment.LeftBottom:
                    fpsRect.anchorMin = new Vector2(0, 0);
                    fpsRect.anchorMax = new Vector2(0, 0);
                    fpsRect.pivot = new Vector2(0, 0);
                    fpsRect.anchoredPosition = new Vector2(Offset.x, Offset.y);
                    break;
                case Alignment.RightBottom:
                    fpsRect.anchorMin = new Vector2(1, 0);
                    fpsRect.anchorMax = new Vector2(1, 0);
                    fpsRect.pivot = new Vector2(1, 0);
                    fpsRect.anchoredPosition = new Vector2(-Offset.x, Offset.y);
                    break;
            }

            // Set console panel position
            var consoleRect = _consolePanel.GetComponent<RectTransform>();
            consoleRect.anchorMin = new Vector2(0, 0);
            consoleRect.anchorMax = new Vector2(1, 0);
            consoleRect.pivot = new Vector2(0.5f, 0);
            consoleRect.offsetMin = new Vector2(Offset.x, Offset.y);
            consoleRect.offsetMax = new Vector2(-Offset.x, Offset.y + ConsoleHeight);

            // Show/hide background
            if (_fpsBackgroundObj != null) _fpsBackgroundObj.SetActive(BoxVisible);
            if (_consoleBackgroundObj != null) _consoleBackgroundObj.SetActive(BoxVisible);
        }

        private void UpdateUI()
        {
            if (_fpsPanel != null)
            {
                var rectTransform = _fpsPanel.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(BoxWidth, BoxHeight);

                // Update FPS text padding
                var fpsTextTransform = _fpsTextComponent?.GetComponent<RectTransform>();
                if (fpsTextTransform != null)
                {
                    fpsTextTransform.offsetMin = new Vector2(Padding.x, Padding.y);
                    fpsTextTransform.offsetMax = new Vector2(-Padding.x, -Padding.y);
                }
            }

            if (_consolePanel != null)
            {
                var rectTransform = _consolePanel.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(Screen.width - Offset.x * 2, ConsoleHeight);

                // Update console text padding
                var consoleTextTransform = _consoleTextComponent?.GetComponent<RectTransform>();
                if (consoleTextTransform != null)
                {
                    consoleTextTransform.offsetMin = new Vector2(Padding.x, Padding.y);
                    consoleTextTransform.offsetMax = new Vector2(-Padding.x, -Padding.y);
                }
            }

            // Update show/hide background
            if (_fpsBackgroundObj != null) _fpsBackgroundObj.SetActive(BoxVisible);
            if (_consoleBackgroundObj != null) _consoleBackgroundObj.SetActive(BoxVisible);

            // Update panel position
            LocateUI();
        }

        private void UpdateFpsText()
        {
            if (_fpsTextComponent == null) return;

            // Display FPS value directly
            string text = $"fps : {_fps:F1}";

            // Add only if there is additional information
            if (_outputDict.Count > 0)
            {
                foreach (KeyValuePair<string, string> pair in _outputDict)
                {
                    text += $"\n{pair.Key} : {pair.Value}";
                }
            }

            _fpsTextComponent.text = text;
            _fpsTextComponent.enabled = true;
            _fpsTextComponent.gameObject.SetActive(true);
        }

        private void UpdateConsoleText()
        {
            if (_consoleTextComponent == null || _consolePanel == null)
            {
                Debug.LogWarning("UpdateConsoleText: consoleTextComponent or consolePanel is null");
                return;
            }

            if (!string.IsNullOrEmpty(_consoleText) && _toastTime != 0)
            {
                _consoleTextComponent.text = _consoleText;
                _consolePanel.SetActive(true);
                _consoleTextComponent.gameObject.SetActive(true);
            }
            else
            {
                _consoleTextComponent.text = "";
                _consolePanel.SetActive(false);
                _consoleTextComponent.gameObject.SetActive(false);
            }
        }
    }
}
