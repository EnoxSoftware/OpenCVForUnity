using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    // v1.0.3
    public class FpsMonitor : MonoBehaviour
    {
        int tick = 0;
        float elapsed = 0;
        float fps = 0;

        public enum Alignment
        {
            LeftTop,
            RightTop,
            LeftBottom,
            RightBottom,
        }

        public Alignment alignment = Alignment.RightTop;

        const float GUI_WIDTH = 95f;
        const float GUI_HEIGHT = 35f;
        const float MARGIN_X = 10f;
        const float MARGIN_Y = 10f;
        const float INNER_X = 10f;
        const float INNER_Y = 10f;
        const float GUI_CONSOLE_HEIGHT = 100f;

        public Vector2 offset = new Vector2(MARGIN_X, MARGIN_Y);
        public bool boxVisible = true;
        public float boxWidth = GUI_WIDTH;
        public float boxHeight = GUI_HEIGHT;
        public Vector2 padding = new Vector2(INNER_X, INNER_Y);
        public float consoleHeight = GUI_CONSOLE_HEIGHT;

        private Text fpsTextComponent;
        private Text consoleTextComponent;
        private GameObject fpsPanel;
        private GameObject consolePanel;
        private Canvas canvas;

        Dictionary<string, string> outputDict = new Dictionary<string, string>();

        protected string _consoleText = null;
        public virtual string consoleText
        {
            get { return _consoleText; }
            set
            {
                _consoleText = value;
                toast_time = -1;
                UpdateConsoleText();
            }
        }

        float toast_time = -1;  // Managed in milliseconds

        // Additional: Reference to the background Image GameObject
        private GameObject fpsBackgroundObj;
        private GameObject consoleBackgroundObj;

        private bool _needsUpdate = false;
        private bool _isInitialized = false;

        // Path to the prefab
        private const string CANVAS_PREFAB_PATH = "FpsMonitorCanvas_103";

        // Lifecycle methods
        void Awake()
        {
            LoadCanvasFromPrefab();
            LocateUI();
            _isInitialized = true;
            _needsUpdate = true;
        }

        void LoadCanvasFromPrefab()
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
            canvas = canvasObj.GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("FpsMonitorCanvas prefab does not have a Canvas component");
                return;
            }

            // Get references to required components
            fpsPanel = canvasObj.transform.Find("FpsPanel")?.gameObject;
            consolePanel = canvasObj.transform.Find("ConsolePanel")?.gameObject;

            if (fpsPanel != null)
            {
                fpsTextComponent = fpsPanel.transform.Find("Mask/FpsText")?.GetComponent<Text>();
                fpsBackgroundObj = fpsPanel.transform.Find("Mask/Background")?.gameObject;
            }

            if (consolePanel != null)
            {
                consoleTextComponent = consolePanel.transform.Find("Mask/ConsoleText")?.GetComponent<Text>();
                consoleBackgroundObj = consolePanel.transform.Find("Mask/Background")?.gameObject;
            }

            // Initial display settings
            if (fpsPanel != null) fpsPanel.SetActive(true);
            if (consolePanel != null) consolePanel.SetActive(false);

            // Show/hide background
            if (fpsBackgroundObj != null) fpsBackgroundObj.SetActive(boxVisible);
            if (consoleBackgroundObj != null) consoleBackgroundObj.SetActive(boxVisible);
        }

        void OnDestroy()
        {
            if (canvas != null)
            {
                Destroy(canvas.gameObject);
            }
        }

        // Update method
        void Update()
        {
            tick++;
            elapsed += Time.deltaTime;
            if (elapsed >= 1f)
            {
                fps = tick / elapsed;
                tick = 0;
                elapsed = 0;
                UpdateFpsText();
            }

            if (toast_time > 0)
            {
                toast_time -= Time.deltaTime * 1000; // Decrement in milliseconds
                if (toast_time <= 0)
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

        void OnValidate()
        {
            if (Application.isPlaying)
            {
                _needsUpdate = true;
            }
        }

        // UI-related methods
        void LocateUI()
        {
            if (fpsPanel == null || consolePanel == null) return;

            // Set FPS panel position
            var fpsRect = fpsPanel.GetComponent<RectTransform>();
            switch (alignment)
            {
                case Alignment.LeftTop:
                    fpsRect.anchorMin = new Vector2(0, 1);
                    fpsRect.anchorMax = new Vector2(0, 1);
                    fpsRect.pivot = new Vector2(0, 1);
                    fpsRect.anchoredPosition = new Vector2(offset.x, -offset.y);
                    break;
                case Alignment.RightTop:
                    fpsRect.anchorMin = new Vector2(1, 1);
                    fpsRect.anchorMax = new Vector2(1, 1);
                    fpsRect.pivot = new Vector2(1, 1);
                    fpsRect.anchoredPosition = new Vector2(-offset.x, -offset.y);
                    break;
                case Alignment.LeftBottom:
                    fpsRect.anchorMin = new Vector2(0, 0);
                    fpsRect.anchorMax = new Vector2(0, 0);
                    fpsRect.pivot = new Vector2(0, 0);
                    fpsRect.anchoredPosition = new Vector2(offset.x, offset.y);
                    break;
                case Alignment.RightBottom:
                    fpsRect.anchorMin = new Vector2(1, 0);
                    fpsRect.anchorMax = new Vector2(1, 0);
                    fpsRect.pivot = new Vector2(1, 0);
                    fpsRect.anchoredPosition = new Vector2(-offset.x, offset.y);
                    break;
            }

            // Set console panel position
            var consoleRect = consolePanel.GetComponent<RectTransform>();
            consoleRect.anchorMin = new Vector2(0, 0);
            consoleRect.anchorMax = new Vector2(1, 0);
            consoleRect.pivot = new Vector2(0.5f, 0);
            consoleRect.offsetMin = new Vector2(offset.x, offset.y);
            consoleRect.offsetMax = new Vector2(-offset.x, offset.y + consoleHeight);

            // Show/hide background
            if (fpsBackgroundObj != null) fpsBackgroundObj.SetActive(boxVisible);
            if (consoleBackgroundObj != null) consoleBackgroundObj.SetActive(boxVisible);
        }

        void UpdateUI()
        {
            if (fpsPanel != null)
            {
                var rectTransform = fpsPanel.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(boxWidth, boxHeight);

                // Update FPS text padding
                var fpsTextTransform = fpsTextComponent?.GetComponent<RectTransform>();
                if (fpsTextTransform != null)
                {
                    fpsTextTransform.offsetMin = new Vector2(padding.x, padding.y);
                    fpsTextTransform.offsetMax = new Vector2(-padding.x, -padding.y);
                }
            }

            if (consolePanel != null)
            {
                var rectTransform = consolePanel.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(Screen.width - offset.x * 2, consoleHeight);

                // Update console text padding
                var consoleTextTransform = consoleTextComponent?.GetComponent<RectTransform>();
                if (consoleTextTransform != null)
                {
                    consoleTextTransform.offsetMin = new Vector2(padding.x, padding.y);
                    consoleTextTransform.offsetMax = new Vector2(-padding.x, -padding.y);
                }
            }

            // Update show/hide background
            if (fpsBackgroundObj != null) fpsBackgroundObj.SetActive(boxVisible);
            if (consoleBackgroundObj != null) consoleBackgroundObj.SetActive(boxVisible);

            // Update panel position
            LocateUI();
        }

        // Text update methods
        void UpdateFpsText()
        {
            if (fpsTextComponent == null) return;

            // Display FPS value directly
            string text = $"fps : {fps:F1}";

            // Add only if there is additional information
            if (outputDict.Count > 0)
            {
                foreach (KeyValuePair<string, string> pair in outputDict)
                {
                    text += $"\n{pair.Key} : {pair.Value}";
                }
            }

            fpsTextComponent.text = text;
            fpsTextComponent.enabled = true;
            fpsTextComponent.gameObject.SetActive(true);
        }

        void UpdateConsoleText()
        {
            if (consoleTextComponent == null || consolePanel == null)
            {
                Debug.LogWarning("UpdateConsoleText: consoleTextComponent or consolePanel is null");
                return;
            }

            if (!string.IsNullOrEmpty(_consoleText) && toast_time != 0)
            {
                consoleTextComponent.text = _consoleText;
                consolePanel.SetActive(true);
                consoleTextComponent.gameObject.SetActive(true);
            }
            else
            {
                consoleTextComponent.text = "";
                consolePanel.SetActive(false);
                consoleTextComponent.gameObject.SetActive(false);
            }
        }

        // Public methods
        public void Add(string key, string value)
        {
            if (outputDict.ContainsKey(key))
            {
                outputDict[key] = value;
            }
            else
            {
                outputDict.Add(key, value);
            }
            UpdateFpsText();
        }

        public void Remove(string key)
        {
            outputDict.Remove(key);
            UpdateFpsText();
        }

        public void Clear()
        {
            outputDict.Clear();
            UpdateFpsText();
        }

        public void LocateGUI()
        {
            _needsUpdate = true;
        }

        public void Toast(string message, float timeMs = 2000f)  // Default is 2 seconds
        {
            _consoleText = message;
            toast_time = Mathf.Max(timeMs, 1000f);  // Minimum 1 second

            if (!_isInitialized)
            {
                Debug.LogError("Toast: FpsMonitor is not initialized. Make sure FpsMonitor is properly set up in the scene.");
                return;
            }

            UpdateConsoleText();
        }
    }
}
