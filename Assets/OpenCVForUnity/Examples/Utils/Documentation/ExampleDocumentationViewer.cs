using UnityEngine;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Displays example documentation in the Inspector and in a runtime overlay (Doc button + scrollable panel).
    /// Documentation text is baked into each scene by the Editor setup tool.
    /// UI is created at runtime from a Canvas prefab in Resources.
    /// </summary>
    public class ExampleDocumentationViewer : MonoBehaviour
    {
        // Enums
        /// <summary>
        /// Corner alignment of the Doc button on the screen.
        /// </summary>
        public enum Alignment
        {
            LeftTop,
            RightTop,
            LeftBottom,
            RightBottom,
        }

        // Constants
        private const string CANVAS_PREFAB_PATH = "ExampleDocumentationCanvas_OpenCV";
        private const string CANVAS_OBJECT_NAME = "ExampleDocumentationCanvas";
        private const string PATH_DOC_BUTTON = "DocButton";
        private const string PATH_DOC_PANEL = "DocPanel";
        private const string PATH_SCROLL_VIEW = "ScrollView";
        private const string PATH_VIEWPORT = "ScrollView/Viewport";
        private const string PATH_DOC_TEXT = "ScrollView/Viewport/Content/DocText";
        private const string PATH_CLOSE_BUTTON = "CloseButton";
        private const float MARGIN_X = 10f;
        private const float MARGIN_Y = 10f;
        private const float DOC_FONT_SIZE = 14f;
        private const float DOC_LINE_SPACING = 1.2f;
        private const float SCROLLBAR_WIDTH = 20f;
        private const float DOC_TEXT_HORIZONTAL_PADDING = 10f;
        private const float DOC_TEXT_BOTTOM_PADDING = 8f;
        private const int CANVAS_SORTING_ORDER = 32760;

        // Public Fields
        /// <summary>
        /// Baked documentation text assigned in the scene by the setup editor tool.
        /// </summary>
        [SerializeField]
        [HideInInspector]
        private string _documentationText;

        /// <summary>
        /// Corner alignment of the Doc button.
        /// </summary>
        public Alignment AlignmentSetting = Alignment.LeftTop;

        // Private Fields
        private Canvas _canvas;
        private RectTransform _canvasRectTransform;
        private GameObject _docPanel;
        private RectTransform _scrollViewRect;
        private RectTransform _viewportRect;
        private ScrollRect _scrollRect;
        private InputField _docInputField;
        private Button _docButton;
        private Button _closeButton;
        private bool _isPanelVisible;
        private Vector2 _lastCanvasSize;
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private bool _needsLayoutUpdate;

        // Public Properties
        /// <summary>
        /// Plain-text documentation for Inspector preview and runtime display.
        /// </summary>
        public string DocumentationText
        {
            get { return _documentationText ?? string.Empty; }
        }

        private Text _docDisplayText
        {
            get { return _docInputField != null ? _docInputField.textComponent : null; }
        }

        // Unity Lifecycle Methods
        private void Awake()
        {
            if (string.IsNullOrEmpty(_documentationText))
            {
                Debug.LogWarning($"{nameof(ExampleDocumentationViewer)}: Documentation text is not assigned on '{name}'.", this);
                return;
            }

            LoadCanvas();
            ApplyDocumentationText();
            SetPanelVisible(false);
            MarkLayoutDirty();
        }

        private void Start()
        {
            BringDocumentationCanvasToFront();
        }

        private void Update()
        {
            if (_canvas == null)
            {
                return;
            }

            if (HasScreenOrCanvasSizeChanged())
            {
                MarkLayoutDirty();
            }

            if (_needsLayoutUpdate)
            {
                _needsLayoutUpdate = false;
                UpdateLayout();
            }
        }

        private void OnDestroy()
        {
            if (_canvas != null)
            {
                Destroy(_canvas.gameObject);
            }
        }

        // Public Methods
        /// <summary>
        /// Toggles the documentation panel visibility.
        /// </summary>
        public void ToggleDocumentationPanel()
        {
            SetPanelVisible(!_isPanelVisible);
        }

        /// <summary>
        /// Shows or hides the documentation panel.
        /// </summary>
        /// <param name="visible">Whether the panel should be visible.</param>
        public void SetPanelVisible(bool visible)
        {
            _isPanelVisible = visible;
            if (_docPanel != null)
            {
                _docPanel.SetActive(visible);
            }

            if (visible)
            {
                BringDocumentationCanvasToFront();
                MarkLayoutDirty();
            }
        }

        // Private Methods
        private void MarkLayoutDirty()
        {
            _needsLayoutUpdate = true;
        }

        private void ApplyDocumentationText()
        {
            if (_docInputField == null)
            {
                return;
            }

            _docInputField.text = _documentationText;
            MarkLayoutDirty();
        }

        private void LoadCanvas()
        {
            GameObject prefab = Resources.Load<GameObject>(CANVAS_PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError(
                    $"{nameof(ExampleDocumentationViewer)}: Failed to load '{CANVAS_PREFAB_PATH}' from Resources.",
                    this);
                return;
            }

            GameObject canvasObj = Instantiate(prefab);
            canvasObj.name = CANVAS_OBJECT_NAME;

            _canvas = canvasObj.GetComponent<Canvas>();
            if (_canvas == null)
            {
                Debug.LogError($"{nameof(ExampleDocumentationViewer)}: Canvas is missing on documentation UI.", this);
                return;
            }

            _canvas.overrideSorting = true;
            _canvas.sortingOrder = CANVAS_SORTING_ORDER;
            _canvasRectTransform = canvasObj.GetComponent<RectTransform>();

            Transform root = canvasObj.transform;
            Transform docButtonTransform = root.Find(PATH_DOC_BUTTON);
            Transform docPanelTransform = root.Find(PATH_DOC_PANEL);

            _docButton = docButtonTransform != null ? docButtonTransform.GetComponent<Button>() : null;
            _docPanel = docPanelTransform != null ? docPanelTransform.gameObject : null;

            if (_docPanel != null)
            {
                Transform scrollViewTransform = _docPanel.transform.Find(PATH_SCROLL_VIEW);
                Transform docTextTransform = _docPanel.transform.Find(PATH_DOC_TEXT);
                Transform viewportTransform = _docPanel.transform.Find(PATH_VIEWPORT);

                _scrollViewRect = scrollViewTransform != null ? scrollViewTransform.GetComponent<RectTransform>() : null;
                _viewportRect = viewportTransform != null ? viewportTransform.GetComponent<RectTransform>() : null;
                _scrollRect = scrollViewTransform != null ? scrollViewTransform.GetComponent<ScrollRect>() : null;
                _docInputField = docTextTransform != null ? docTextTransform.GetComponent<InputField>() : null;

                if (_scrollRect == null)
                {
                    Debug.LogWarning(
                        $"{nameof(ExampleDocumentationViewer)}: Prefab is missing ScrollRect at '{PATH_SCROLL_VIEW}'.",
                        this);
                }

                if (_docInputField == null)
                {
                    Debug.LogWarning(
                        $"{nameof(ExampleDocumentationViewer)}: Prefab is missing InputField at '{PATH_DOC_TEXT}'.",
                        this);
                }

                Transform closeButtonTransform = _docPanel.transform.Find(PATH_CLOSE_BUTTON);
                _closeButton = closeButtonTransform != null ? closeButtonTransform.GetComponent<Button>() : null;
            }

            if (_docButton != null)
            {
                _docButton.onClick.AddListener(ToggleDocumentationPanel);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(() => SetPanelVisible(false));
            }

            LocateDocButton();
            BringDocumentationCanvasToFront();
            CacheScreenAndCanvasSize();
        }

        private void BringDocumentationCanvasToFront()
        {
            if (_canvas == null)
            {
                return;
            }

            _canvas.overrideSorting = true;
            _canvas.sortingOrder = CANVAS_SORTING_ORDER;
            _canvas.transform.SetAsLastSibling();
        }

        private void UpdateLayout()
        {
            if (!TryGetCanvasSize(out _))
            {
                return;
            }

            ApplyDocTextStyle();
            UpdateDocTextLayout();

            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 1f;
            }

            CacheScreenAndCanvasSize();
        }

        private void UpdateDocTextLayout()
        {
            Text docText = _docDisplayText;
            if (_docInputField == null || docText == null || _viewportRect == null)
            {
                return;
            }

            float viewportWidth = _viewportRect.rect.width;
            if (viewportWidth <= 1f)
            {
                MarkLayoutDirty();
                return;
            }

            float horizontalPadding = DOC_TEXT_HORIZONTAL_PADDING * 2f;
            float textViewportWidth = GetDocumentationTextViewportWidth();
            float textWidth = Mathf.Max(1f, textViewportWidth - horizontalPadding);
            float preferredHeight = MeasureDocumentationTextHeight(docText, _docInputField.text, textWidth);
            preferredHeight += DOC_TEXT_BOTTOM_PADDING;
            preferredHeight = Mathf.Max(preferredHeight, docText.fontSize);

            RectTransform docTextRect = _docInputField.GetComponent<RectTransform>();
            docTextRect.sizeDelta = new Vector2(-horizontalPadding, preferredHeight);

            RectTransform contentRect = docTextRect.parent as RectTransform;
            if (contentRect != null)
            {
                contentRect.sizeDelta = new Vector2(0f, preferredHeight);
            }

            Canvas.ForceUpdateCanvases();
            if (_scrollRect != null)
            {
                _scrollRect.enabled = false;
                _scrollRect.enabled = true;
            }

            float layoutTextViewportWidth = GetDocumentationTextViewportWidth();
            if (Mathf.Abs(layoutTextViewportWidth - textViewportWidth) > 0.5f)
            {
                MarkLayoutDirty();
            }
        }

        private float GetDocumentationTextViewportWidth()
        {
            float viewportWidth = _viewportRect.rect.width;
            if (_scrollViewRect == null || _scrollRect == null || _scrollRect.verticalScrollbar == null)
            {
                return viewportWidth;
            }

            float scrollbarOccupiedWidth = _scrollViewRect.rect.width - viewportWidth;
            if (scrollbarOccupiedWidth < 1f)
            {
                return Mathf.Max(1f, viewportWidth - SCROLLBAR_WIDTH);
            }

            return viewportWidth;
        }

        private static float MeasureDocumentationTextHeight(Text docText, string text, float textWidth)
        {
            TextGenerationSettings settings = docText.GetGenerationSettings(new Vector2(textWidth, 0f));
            settings.fontSize = docText.fontSize;
            settings.resizeTextForBestFit = false;
            settings.horizontalOverflow = HorizontalWrapMode.Wrap;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            settings.updateBounds = true;
            settings.generateOutOfBounds = true;

            TextGenerator generator = docText.cachedTextGeneratorForLayout;
            generator.Populate(text, settings);
            return generator.rectExtents.height / docText.pixelsPerUnit;
        }

        private void ApplyDocTextStyle()
        {
            Text docText = _docDisplayText;
            if (docText == null)
            {
                return;
            }

            docText.fontSize = Mathf.RoundToInt(DOC_FONT_SIZE);
            docText.lineSpacing = DOC_LINE_SPACING;
            docText.horizontalOverflow = HorizontalWrapMode.Wrap;
            docText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private bool HasScreenOrCanvasSizeChanged()
        {
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                return true;
            }

            if (_canvasRectTransform == null)
            {
                return false;
            }

            return _canvasRectTransform.rect.size != _lastCanvasSize;
        }

        private void CacheScreenAndCanvasSize()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            if (_canvasRectTransform != null)
            {
                _lastCanvasSize = _canvasRectTransform.rect.size;
            }
        }

        private bool TryGetCanvasSize(out Vector2 canvasSize)
        {
            canvasSize = Vector2.zero;
            if (_canvasRectTransform == null)
            {
                return false;
            }

            canvasSize = _canvasRectTransform.rect.size;
            return canvasSize.x > 0f && canvasSize.y > 0f;
        }

        private void LocateDocButton()
        {
            if (_docButton == null)
            {
                return;
            }

            RectTransform buttonRect = _docButton.GetComponent<RectTransform>();
            switch (AlignmentSetting)
            {
                case Alignment.LeftTop:
                    buttonRect.anchorMin = new Vector2(0f, 1f);
                    buttonRect.anchorMax = new Vector2(0f, 1f);
                    buttonRect.pivot = new Vector2(0f, 1f);
                    buttonRect.anchoredPosition = new Vector2(MARGIN_X, -MARGIN_Y);
                    break;
                case Alignment.RightTop:
                    buttonRect.anchorMin = new Vector2(1f, 1f);
                    buttonRect.anchorMax = new Vector2(1f, 1f);
                    buttonRect.pivot = new Vector2(1f, 1f);
                    buttonRect.anchoredPosition = new Vector2(-MARGIN_X, -MARGIN_Y);
                    break;
                case Alignment.LeftBottom:
                    buttonRect.anchorMin = new Vector2(0f, 0f);
                    buttonRect.anchorMax = new Vector2(0f, 0f);
                    buttonRect.pivot = new Vector2(0f, 0f);
                    buttonRect.anchoredPosition = new Vector2(MARGIN_X, MARGIN_Y);
                    break;
                case Alignment.RightBottom:
                    buttonRect.anchorMin = new Vector2(1f, 0f);
                    buttonRect.anchorMax = new Vector2(1f, 0f);
                    buttonRect.pivot = new Vector2(1f, 0f);
                    buttonRect.anchoredPosition = new Vector2(-MARGIN_X, MARGIN_Y);
                    break;
            }
        }
    }
}
