using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OpenCVForUnityExample
{
    /// <summary>
    /// TextureSelector
    /// A component that detects touch/click on GameObject and converts screen coordinates to texture coordinates.
    /// Supports Quad mesh with Renderer component and RawImage UI elements.
    /// Provides both point selection and rectangle selection modes with OpenCV coordinate system support.
    /// </summary>
    public class TextureSelector : MonoBehaviour
    {
        /// <summary>
        /// Selection mode for touch interaction.
        /// Determines how the user can interact with the texture.
        /// </summary>
        public enum SelectionMode
        {
            /// <summary>
            /// Point selection mode - single point selection.
            /// </summary>
            POINT,
            /// <summary>
            /// Rectangle selection mode - drag to select rectangular area.
            /// </summary>
            RECTANGLE
        }

        /// <summary>
        /// Texture selection states.
        /// </summary>
        public enum TextureSelectionState
        {
            /// <summary>
            /// No event state (default state).
            /// </summary>
            NONE,
            /// <summary>
            /// Selection outside texture area.
            /// </summary>
            OUTSIDE_TEXTURE_SELECTED,
            /// <summary>
            /// Point selection started.
            /// </summary>
            POINT_SELECTION_STARTED,
            /// <summary>
            /// Point selection in progress (dragging).
            /// </summary>
            POINT_SELECTION_IN_PROGRESS,
            /// <summary>
            /// Point selection completed.
            /// </summary>
            POINT_SELECTION_COMPLETED,
            /// <summary>
            /// Point selection cancelled.
            /// </summary>
            POINT_SELECTION_CANCELLED,
            /// <summary>
            /// Rectangle selection started.
            /// </summary>
            RECTANGLE_SELECTION_STARTED,
            /// <summary>
            /// Rectangle selection in progress (dragging).
            /// </summary>
            RECTANGLE_SELECTION_IN_PROGRESS,
            /// <summary>
            /// Rectangle selection completed.
            /// </summary>
            RECTANGLE_SELECTION_COMPLETED,
            /// <summary>
            /// Rectangle selection cancelled.
            /// </summary>
            RECTANGLE_SELECTION_CANCELLED
        }

        #region Public Fields

        /// <summary>
        /// Selection mode for touch interaction.
        /// Controls whether the component operates in point selection or rectangle selection mode.
        /// </summary>
        [SerializeField, Tooltip("Selection mode for touch interaction.\nPOINT: Single point selection\nRECTANGLE: Drag to select rectangular area")]
        protected SelectionMode _selectionMode = SelectionMode.POINT;

        /// <summary>
        /// Selection mode for touch interaction.
        /// </summary>
        public SelectionMode selectionMode
        {
            get { return _selectionMode; }
            set
            {
                if (_selectionMode != value)
                {
                    _selectionMode = value;
                    ResetCurrentState(); // Reset selection state when mode changes
                }
            }
        }

        /// <summary>
        /// Target camera for Quad objects (Quad objects only).
        /// Used for raycast-based touch detection on 3D Quad objects.
        /// If null, MainCamera will be used as fallback.
        /// Not used for RawImage UI elements.
        /// </summary>
        [SerializeField, Tooltip("Target camera for Quad objects.\nIf null, MainCamera will be used as fallback.\nNot used for RawImage elements.")]
        protected Camera _targetCamera;

        /// <summary>
        /// Target camera for Quad objects (Quad objects only).
        /// If null, MainCamera will be used as fallback.
        /// </summary>
        public Camera targetCamera
        {
            get { return _targetCamera; }
            set
            {
                if (_targetCamera != value)
                {
                    _targetCamera = value;
                    ResetCurrentState(); // Reset selection state when camera changes
                }
            }
        }

        /// <summary>
        /// Use OpenCV Mat coordinates (top-left origin) instead of Unity texture coordinates (bottom-left origin).
        /// When true, coordinates follow OpenCV convention with (0,0) at top-left.
        /// When false, coordinates follow Unity convention with (0,0) at bottom-left.
        /// </summary>
        [SerializeField, Tooltip("Use OpenCV Mat coordinates (top-left origin) instead of Unity texture coordinates (bottom-left origin).\nWhen enabled: (0,0) is at top-left\nWhen disabled: (0,0) is at bottom-left")]
        protected bool _useOpenCVMatCoordinates = true;

        /// <summary>
        /// Use OpenCV Mat coordinates (top-left origin) instead of Unity texture coordinates (bottom-left origin).
        /// </summary>
        public bool useOpenCVMatCoordinates
        {
            get { return _useOpenCVMatCoordinates; }
            set
            {
                if (_useOpenCVMatCoordinates != value)
                {
                    _useOpenCVMatCoordinates = value;
                    ResetCurrentState(); // Reset selection state when coordinate system changes
                }
            }
        }

        /// <summary>
        /// Unity event fired when texture selection state has changed.
        /// Can be configured in the Inspector.
        /// Parameters: GameObject (target object), TextureSelectionState (current state), Vector2[] (texture coordinates).
        /// </summary>
        /// <remarks>
        /// Vector2[] array contents by mode:
        /// - NONE: [(-1, -1)] (default invalid state)
        /// - OUTSIDE_TEXTURE_SELECTED: [(-1, -1)] (invalid coordinates)
        /// - POINT_SELECTION_*: [point] (single point coordinates)
        /// - RECTANGLE_SELECTION_*: [startPoint, endPoint] (two corner points of rectangle)
        /// - POINT_SELECTION_CANCELLED: [point] (cancelled point coordinates from before cancellation)
        /// - RECTANGLE_SELECTION_CANCELLED: [startPoint, endPoint] (cancelled rectangle coordinates from before cancellation)
        /// </remarks>
        [System.Serializable]
        public class TextureSelectionEvent : UnityEvent<GameObject, TextureSelectionState, Vector2[]> { }

        /// <summary>
        /// Event fired when texture selection state has changed.
        /// Parameters: GameObject (target object), TextureSelectionState (current state), Vector2[] (texture coordinates array)
        /// </summary>
        [Tooltip("Event fired when texture selection state has changed.\nParameters:\n- GameObject: Target object\n- TextureSelectionState: Current state\n- Vector2[]: Texture coordinates array")]
        public TextureSelectionEvent OnTextureSelectionStateChanged = new TextureSelectionEvent();

        /// <summary>
        /// Whether to fire event with invalid coordinates (-1, -1) when selecting outside the GameObject.
        /// When enabled, OUTSIDE_TEXTURE_SELECTED events are fired even when selection is outside the texture area.
        /// </summary>
        [SerializeField, Tooltip("Whether to fire event with invalid coordinates (-1, -1) when selecting outside the GameObject.\nWhen enabled: OUTSIDE_TEXTURE_SELECTED events are fired even when selection is outside the texture area.")]
        protected bool _fireEventOnOutsideSelect = false;

        /// <summary>
        /// Whether to fire event with invalid coordinates (-1, -1) when selecting outside the GameObject.
        /// </summary>
        public bool fireEventOnOutsideSelect
        {
            get { return _fireEventOnOutsideSelect; }
            set
            {
                if (_fireEventOnOutsideSelect != value)
                {
                    _fireEventOnOutsideSelect = value;
                    ResetCurrentState(); // Reset selection state when event behavior changes
                }
            }
        }

        #endregion

        #region Private Fields

        /// <summary>
        /// The target camera for raycast (internal field).
        /// Used internally for raycast-based touch detection on Quad objects.
        /// </summary>
        private Camera _internalTargetCamera;

        /// <summary>
        /// The RawImage component (for RawImage UI elements).
        /// Used for UI-based texture display and touch detection on RawImage elements.
        /// </summary>
        private RawImage _rawImage;

        /// <summary>
        /// The Renderer component (for Quad objects).
        /// Used for 3D Quad mesh rendering and texture display on Quad objects.
        /// </summary>
        private Renderer _renderer;


        /// <summary>
        /// Flag to track if this is a Quad object or RawImage element.
        /// True for Quad objects with Renderer component, false for RawImage UI elements.
        /// </summary>
        private bool _isQuadObject;



        /// <summary>
        /// Current selection state.
        /// Tracks the current state of selection interaction (NONE, POINT_SELECTION_*, RECTANGLE_SELECTION_*, etc.).
        /// </summary>
        private TextureSelectionState _currentState = TextureSelectionState.NONE;

        /// <summary>
        /// Current texture points (for point selection or rectangle selection).
        /// Contains the current texture coordinates for the active selection.
        /// </summary>
        private Vector2[] _currentTexturePoints = new Vector2[] { new Vector2(-1, -1) };

        /// <summary>
        /// Rectangle selection start point in texture coordinates.
        /// Stores the initial touch point for rectangle selection drag operations.
        /// </summary>
        private Vector2 _currentRectangleStartPoint = new Vector2(-1, -1);


        #endregion

        #region Public Methods

        /// <summary>
        /// Convert single point (first element) to UnityEngine.Vector2.
        /// Extracts the first valid point from the points array.
        /// </summary>
        /// <param name="points">Vector2 array expected to contain at least one point.</param>
        /// <returns>UnityEngine.Vector2 for the first point. Returns (-1, -1) when invalid.</returns>
        public static Vector2 ConvertSelectionPointsToUnityVector2(Vector2[] points)
        {
            // Input validation
            if (points == null)
            {
                Debug.LogError("TextureSelector: ConvertSelectionPointsToUnityVector2 - points array is null.");
                return new Vector2(-1, -1);
            }

            if (points.Length < 1)
            {
                Debug.LogError("TextureSelector: ConvertSelectionPointsToUnityVector2 - points array must contain at least 1 point.");
                return new Vector2(-1, -1);
            }

            return points[0];
        }

        /// <summary>
        /// Convert single point (first element) to OpenCVForUnity.CoreModule.Point.
        /// Extracts the first valid point from the points array.
        /// </summary>
        /// <param name="points">Vector2 array expected to contain at least one point.</param>
        /// <returns>OpenCVForUnity.CoreModule.Point for the first point. Returns (-1, -1) when invalid.</returns>
        public static OpenCVForUnity.CoreModule.Point ConvertSelectionPointsToOpenCVPoint(Vector2[] points)
        {
            // Input validation
            if (points == null)
            {
                Debug.LogError("TextureSelector: ConvertSelectionPointsToOpenCVPoint - points array is null.");
                return new OpenCVForUnity.CoreModule.Point(-1, -1);
            }

            if (points.Length < 1)
            {
                Debug.LogError("TextureSelector: ConvertSelectionPointsToOpenCVPoint - points array must contain at least 1 point.");
                return new OpenCVForUnity.CoreModule.Point(-1, -1);
            }

            return new OpenCVForUnity.CoreModule.Point(
                (int)points[0].x,
                (int)points[0].y
            );
        }

        /// <summary>
        /// Convert single point (first element) to ValueTuple (int x, int y).
        /// Extracts the first valid point from the points array and casts to integers.
        /// </summary>
        /// <param name="points">Vector2 array expected to contain at least one point.</param>
        /// <returns>Tuple (x, y). Returns (-1, -1) when invalid.</returns>
        public static (int x, int y) ConvertSelectionPointsToValueTuple(Vector2[] points)
        {
            // Input validation
            if (points == null)
            {
                Debug.LogError("TextureSelector: ConvertSelectionPointsToValueTuple - points array is null.");
                return (-1, -1);
            }

            if (points.Length < 1)
            {
                Debug.LogError("TextureSelector: ConvertSelectionPointsToValueTuple - points array must contain at least 1 point.");
                return (-1, -1);
            }

            return (
                (int)points[0].x,
                (int)points[0].y
            );
        }

        /// <summary>
        /// Convert rectangle corner points to UnityEngine.Rect.
        /// Creates a Unity Rect from two corner points, handling coordinate validation and bounds calculation.
        /// </summary>
        /// <param name="points">Vector2 array containing start and end points of rectangle selection.</param>
        /// <returns>UnityEngine.Rect created from the two corner points. Returns Rect.zero if input is invalid.</returns>
        public static UnityEngine.Rect ConvertSelectionPointsToUnityRect(Vector2[] points)
        {
            // Input validation
            if (points == null)
            {
                Debug.LogError("TextureSelector: ConvertSelectionPointsToUnityRect - points array is null.");
                return UnityEngine.Rect.zero;
            }

            if (points.Length < 2)
            {
                Debug.LogError("TextureSelector: ConvertSelectionPointsToUnityRect - points array must contain at least 2 points (start and end).");
                return UnityEngine.Rect.zero;
            }

            // Note: no coordinate range validation; caller is responsible for ensuring correctness

            // Get the two corner points
            Vector2 point1 = points[0];
            Vector2 point2 = points[1];

            // Calculate the rectangle bounds
            float minX = Mathf.Min(point1.x, point2.x);
            float maxX = Mathf.Max(point1.x, point2.x);
            float minY = Mathf.Min(point1.y, point2.y);
            float maxY = Mathf.Max(point1.y, point2.y);

            // Create UnityEngine.Rect from the bounds
            float width = maxX - minX;
            float height = maxY - minY;

            // Validate that the rectangle has positive dimensions
            if (width <= 0 || height <= 0)
            {
                Debug.LogWarning("TextureSelector: ConvertSelectionPointsToUnityRect - resulting rectangle has zero or negative dimensions.");
                return UnityEngine.Rect.zero;
            }

            return new UnityEngine.Rect(minX, minY, width, height);
        }

        /// <summary>
        /// Convert rectangle corner points to OpenCVForUnity.CoreModule.Rect.
        /// Creates an OpenCV Rect from two corner points, handling coordinate validation and bounds calculation.
        /// </summary>
        /// <param name="points">Vector2 array containing start and end points of rectangle selection.</param>
        /// <returns>OpenCVForUnity.CoreModule.Rect created from the two corner points. Returns invalid rect if input is invalid.</returns>
        public static OpenCVForUnity.CoreModule.Rect ConvertSelectionPointsToOpenCVRect(Vector2[] points)
        {
            // Input validation
            if (points == null)
            {
                Debug.LogError("TextureSelector: ConvertSelectionPointsToOpenCVRect - points array is null.");
                return new OpenCVForUnity.CoreModule.Rect(-1, -1, -1, -1);
            }

            if (points.Length < 2)
            {
                Debug.LogError("TextureSelector: ConvertSelectionPointsToOpenCVRect - points array must contain at least 2 points (start and end).");
                return new OpenCVForUnity.CoreModule.Rect(-1, -1, -1, -1);
            }

            // Note: no coordinate range validation; caller is responsible for ensuring correctness

            // Get the two corner points
            Vector2 point1 = points[0];
            Vector2 point2 = points[1];

            // Calculate the rectangle bounds
            float minX = Mathf.Min(point1.x, point2.x);
            float maxX = Mathf.Max(point1.x, point2.x);
            float minY = Mathf.Min(point1.y, point2.y);
            float maxY = Mathf.Max(point1.y, point2.y);

            // Create OpenCVForUnity.CoreModule.Rect from the bounds
            int width = (int)(maxX - minX);
            int height = (int)(maxY - minY);

            // Validate that the rectangle has positive dimensions
            if (width <= 0 || height <= 0)
            {
                Debug.LogWarning("TextureSelector: ConvertSelectionPointsToOpenCVRect - resulting rectangle has zero or negative dimensions.");
                return new OpenCVForUnity.CoreModule.Rect(-1, -1, -1, -1);
            }

            return new OpenCVForUnity.CoreModule.Rect((int)minX, (int)minY, width, height);
        }

        /// <summary>
        /// Convert rectangle corner points to OpenCV Rect coordinates as ValueTuple.
        /// Creates OpenCV Rect coordinates from two corner points, handling coordinate validation and bounds calculation.
        /// </summary>
        /// <param name="points">Vector2 array containing start and end points of rectangle selection.</param>
        /// <returns>ValueTuple containing (int x, int y, int width, int height). Returns (-1, -1, -1, -1) if input is invalid.</returns>
        public static (int x, int y, int width, int height) ConvertSelectionPointsToOpenCVValueTuple(Vector2[] points)
        {
            // Input validation
            if (points == null)
            {
                Debug.LogError("TextureSelector: ConvertSelectionPointsToOpenCVValueTuple - points array is null.");
                return (-1, -1, -1, -1);
            }

            if (points.Length < 2)
            {
                Debug.LogError("TextureSelector: ConvertSelectionPointsToOpenCVValueTuple - points array must contain at least 2 points (start and end).");
                return (-1, -1, -1, -1);
            }

            // Note: no coordinate range validation; caller is responsible for ensuring correctness

            // Get the two corner points
            Vector2 point1 = points[0];
            Vector2 point2 = points[1];

            // Calculate the rectangle bounds
            float minX = Mathf.Min(point1.x, point2.x);
            float maxX = Mathf.Max(point1.x, point2.x);
            float minY = Mathf.Min(point1.y, point2.y);
            float maxY = Mathf.Max(point1.y, point2.y);

            // Create OpenCV Rect coordinates from the bounds
            int width = (int)(maxX - minX);
            int height = (int)(maxY - minY);

            // Validate that the rectangle has positive dimensions
            if (width <= 0 || height <= 0)
            {
                Debug.LogWarning("TextureSelector: ConvertSelectionPointsToOpenCVValueTuple - resulting rectangle has zero or negative dimensions.");
                return (-1, -1, -1, -1);
            }

            return ((int)minX, (int)minY, width, height);
        }

        /// <summary>
        /// Get current selection status.
        /// Returns the current state and coordinates of the active selection.
        /// </summary>
        /// <returns>Tuple containing (GameObject, TextureSelectionState, Vector2[]).</returns>
        /// <remarks>
        /// Vector2[] array contents by mode:
        /// - NONE: [(-1, -1)] (default invalid state)
        /// - OUTSIDE_TEXTURE_SELECTED: [(-1, -1)] (invalid coordinates)
        /// - POINT_SELECTION_*: [point] (single point coordinates)
        /// - RECTANGLE_SELECTION_*: [startPoint, endPoint] (two corner points of rectangle)
        /// - POINT_SELECTION_CANCELLED: [point] (cancelled point coordinates from before cancellation)
        /// - RECTANGLE_SELECTION_CANCELLED: [startPoint, endPoint] (cancelled rectangle coordinates from before cancellation)
        /// </remarks>
        public (GameObject, TextureSelectionState, Vector2[]) GetSelectionStatus()
        {
            return (gameObject, _currentState, _currentTexturePoints);
        }

        /// <summary>
        /// Reset the current selection status to NONE.
        /// This method can be called externally to cancel any ongoing selection.
        /// Clears all current selection state and coordinates.
        /// </summary>
        public void ResetSelectionStatus()
        {
            ResetCurrentState();
        }

        /// <summary>
        /// Draw the current selection state on the provided Mat.
        /// Renders visual indicators for the current touch selection state on the OpenCV Mat.
        /// </summary>
        /// <param name="mat">The Mat to draw the selection on.</param>
        /// <param name="showCoordinates">If true, displays coordinates near the points.</param>
        /// <param name="debugMode">If true, draws current state in the top-left corner of the Mat.</param>
        public void DrawSelection(Mat mat, bool showCoordinates = false, bool debugMode = false)
        {
            if (mat == null || _currentTexturePoints == null || _currentTexturePoints.Length == 0)
                return;

            // Base resolution for scaling calculations
            const int baseWidth = 640;
            const int baseHeight = 480;

            // Calculate scaling factors based on current Mat size
            float scaleX = (float)mat.cols() / baseWidth;
            float scaleY = (float)mat.rows() / baseHeight;
            float scale = Mathf.Min(scaleX, scaleY); // Use minimum scale to maintain aspect ratio

            // Adjust drawing parameters based on scale
            int lineThickness = Mathf.Max(1, Mathf.RoundToInt(2 * scale));
            int circleRadius = Mathf.Max(3, Mathf.RoundToInt(8 * scale));
            int cornerRadius = Mathf.Max(2, Mathf.RoundToInt(5 * scale));
            float fontSize = Mathf.Max(0.1f, 0.4f * scale);
            int textThickness = Mathf.Max(1, Mathf.RoundToInt(1 * scale));
            float debugFontSize = Mathf.Max(0.1f, 0.6f * scale);
            int debugThickness = Mathf.Max(1, Mathf.RoundToInt(2 * scale));

            // Note: textOffset and textSpacing are calculated in CalculateTextPosition method

            // Define colors for different touch states using ValueTuple
            var pointColor = (0.0, 255.0, 0.0, 255.0); // Green for point selection
            var rectangleColor = (255.0, 0.0, 0.0, 255.0); // Red for rectangle selection
            var progressColor = (255.0, 255.0, 0.0, 255.0); // Yellow for rectangle in progress
            var coordinateColor = (255.0, 255.0, 255.0, 255.0); // White for coordinate text
            var debugColor = (255.0, 255.0, 255.0, 255.0); // White for debug text

            switch (_currentState)
            {
                case TextureSelectionState.POINT_SELECTION_STARTED:
                case TextureSelectionState.POINT_SELECTION_IN_PROGRESS:
                case TextureSelectionState.POINT_SELECTION_COMPLETED:
                    // Draw a circle at the touched point
                    if (_currentTexturePoints.Length >= 1)
                    {
                        var center = (_currentTexturePoints[0].x, _currentTexturePoints[0].y);

                        // Choose color based on state
                        var currentColor = _currentState == TextureSelectionState.POINT_SELECTION_COMPLETED ?
                            pointColor : progressColor;

                        Imgproc.circle(mat, center, circleRadius, currentColor, lineThickness);

                        // Draw coordinates if enabled
                        if (showCoordinates)
                        {
                            var coordinateText = $"({(int)_currentTexturePoints[0].x},{(int)_currentTexturePoints[0].y})";
                            var (textX, textY) = CalculateTextPosition((_currentTexturePoints[0].x, _currentTexturePoints[0].y), mat.cols(), mat.rows(), scale);
                            Imgproc.putText(mat, coordinateText, (textX, textY),
                                Imgproc.FONT_HERSHEY_SIMPLEX, fontSize, coordinateColor, textThickness, Imgproc.LINE_AA, false);
                        }
                    }
                    break;

                case TextureSelectionState.RECTANGLE_SELECTION_STARTED:
                case TextureSelectionState.RECTANGLE_SELECTION_IN_PROGRESS:
                case TextureSelectionState.RECTANGLE_SELECTION_COMPLETED:
                    // Draw rectangle selection
                    if (_currentTexturePoints.Length >= 2)
                    {
                        var point1 = (_currentTexturePoints[0].x, _currentTexturePoints[0].y);
                        var point2 = (_currentTexturePoints[1].x, _currentTexturePoints[1].y);

                        // Create rectangle from two points using ValueTuple
                        var (minX, minY, maxX, maxY) = (
                            Mathf.Min(point1.x, point2.x),
                            Mathf.Min(point1.y, point2.y),
                            Mathf.Max(point1.x, point2.x),
                            Mathf.Max(point1.y, point2.y)
                        );

                        // Calculate dimensions
                        var width = maxX - minX;
                        var height = maxY - minY;

                        // Set minimum size to ensure rectangle is visible
                        const float minSize = 2.0f;
                        if (width < minSize) width = minSize;
                        if (height < minSize) height = minSize;

                        // Choose color based on state
                        var currentColor = _currentState == TextureSelectionState.RECTANGLE_SELECTION_COMPLETED ?
                            rectangleColor : progressColor;

                        // Draw rectangle
                        Imgproc.rectangle(mat, (minX, minY), (maxX, maxY), currentColor, lineThickness);

                        // Draw corner markers
                        Imgproc.circle(mat, point1, cornerRadius, currentColor, -1);
                        Imgproc.circle(mat, point2, cornerRadius, currentColor, -1);

                        // Draw coordinates if enabled
                        if (showCoordinates)
                        {
                            // Draw coordinates for both corner points
                            var coord1Text = $"({(int)point1.x},{(int)point1.y})";
                            var coord2Text = $"({(int)point2.x},{(int)point2.y})";

                            var (textX1, textY1) = CalculateTextPosition(point1, mat.cols(), mat.rows(), scale);
                            var (textX2, textY2) = CalculateTextPosition(point2, mat.cols(), mat.rows(), scale);

                            Imgproc.putText(mat, coord1Text, (textX1, textY1),
                                Imgproc.FONT_HERSHEY_SIMPLEX, fontSize, coordinateColor, textThickness, Imgproc.LINE_AA, false);
                            Imgproc.putText(mat, coord2Text, (textX2, textY2),
                                Imgproc.FONT_HERSHEY_SIMPLEX, fontSize, coordinateColor, textThickness, Imgproc.LINE_AA, false);
                        }
                    }
                    break;

                case TextureSelectionState.OUTSIDE_TEXTURE_SELECTED:
                    // Remove warning message - no longer drawing "Touch Outside Texture"
                    break;

                case TextureSelectionState.POINT_SELECTION_CANCELLED:
                case TextureSelectionState.RECTANGLE_SELECTION_CANCELLED:
                    // Clear any previous drawing by not drawing anything
                    break;

                case TextureSelectionState.NONE:
                default:
                    // No drawing for NONE state
                    break;
            }

            // Draw debug information in top-left corner if debug mode is enabled
            if (debugMode)
            {
                var stateText = _currentState.ToString();
                int debugX = Mathf.Max(5, Mathf.RoundToInt(10 * scale));
                int debugY = Mathf.Max(15, Mathf.RoundToInt(30 * scale));
                Imgproc.putText(mat, stateText, (debugX, debugY),
                    Imgproc.FONT_HERSHEY_SIMPLEX, debugFontSize, debugColor, debugThickness, Imgproc.LINE_AA, false);
            }
        }

        #endregion

        #region Unity Lifecycle Methods

        private void Start()
        {
            InitializeComponents();
            ResetCurrentState(); // Initialize selection state to NONE
        }

        /// <summary>
        /// Called when the script is loaded or a value is changed in the Inspector.
        /// </summary>
        protected virtual void OnValidate()
        {
            //Debug.Log("OnValidate");

#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
#endif
                ResetCurrentState();
#if UNITY_EDITOR
            }
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }
#endif

        private void Update()
        {
            if (_selectionMode == SelectionMode.POINT)
            {
                DetectPointSelection();
            }
            else if (_selectionMode == SelectionMode.RECTANGLE)
            {
                DetectRectangleSelection();
            }
        }

        private void OnDestroy()
        {
            OnTextureSelectionStateChanged?.RemoveAllListeners();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Check if rectangle selection is in progress based on current state.
        /// Determines if the user is currently dragging to create a rectangle selection.
        /// </summary>
        /// <returns>True if rectangle selection is in progress, false otherwise.</returns>
        private bool IsRectangleSelectionInProgress()
        {
            return _currentState == TextureSelectionState.RECTANGLE_SELECTION_STARTED ||
                   _currentState == TextureSelectionState.RECTANGLE_SELECTION_IN_PROGRESS;
        }

        /// <summary>
        /// Check if point selection is in progress based on current state.
        /// Determines if the user is currently performing a point selection operation.
        /// </summary>
        /// <returns>True if point selection is in progress, false otherwise.</returns>
        private bool IsPointSelectionInProgress()
        {
            return _currentState == TextureSelectionState.POINT_SELECTION_STARTED ||
                   _currentState == TextureSelectionState.POINT_SELECTION_IN_PROGRESS;
        }

        /// <summary>
        /// Check if touch/mouse is currently pressed based on current state.
        /// Determines if any selection operation is currently active.
        /// </summary>
        /// <returns>True if touch/mouse is pressed, false otherwise.</returns>
        private bool IsPressed()
        {
            return _currentState == TextureSelectionState.RECTANGLE_SELECTION_STARTED ||
                   _currentState == TextureSelectionState.RECTANGLE_SELECTION_IN_PROGRESS ||
                   _currentState == TextureSelectionState.POINT_SELECTION_STARTED ||
                   _currentState == TextureSelectionState.POINT_SELECTION_IN_PROGRESS;
        }

        /// <summary>
        /// Update current state and texture points.
        /// Updates the internal state and coordinates for the current selection interaction.
        /// </summary>
        /// <param name="state">New state.</param>
        /// <param name="texturePoints">Texture points.</param>
        private void UpdateCurrentState(TextureSelectionState state, Vector2[] texturePoints = null)
        {
            _currentState = state;
            if (texturePoints != null)
            {
                // Reuse existing array if same length, otherwise create new one
                if (_currentTexturePoints == null || _currentTexturePoints.Length != texturePoints.Length)
                {
                    _currentTexturePoints = new Vector2[texturePoints.Length];
                }
                System.Array.Copy(texturePoints, _currentTexturePoints, texturePoints.Length);
            }
        }

        /// <summary>
        /// Reset current selection state to NONE (internal method).
        /// Clears all internal selection state variables and resets to initial state.
        /// </summary>
        private void ResetCurrentState()
        {
            _currentState = TextureSelectionState.NONE;
            _currentTexturePoints = new Vector2[] { new Vector2(-1, -1) };
            _currentRectangleStartPoint = new Vector2(-1, -1);
        }

        /// <summary>
        /// Detect point selection input.
        /// Handles input detection for point selection mode using appropriate input system.
        /// </summary>
        private void DetectPointSelection()
        {
#if ENABLE_INPUT_SYSTEM
            DetectPointSelectionNewSystem();
#else
            DetectPointSelectionLegacy();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// Detect point selection using the new input system.
        /// Handles touch and mouse input using Unity's new Input System.
        /// </summary>
        private void DetectPointSelectionNewSystem()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            // Touch input for mobile platforms
            if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count == 1)
            {
                var touch = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];

                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    // Unified processing for both Quad objects and RawImage elements
                    // Exclude all UI elements (unified)
                    if (!ShouldIgnoreInput(touch.screenPosition))
                    {
                        StartPointSelection(touch.screenPosition);
                    }
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved && IsPointSelectionInProgress())
                {
                    // Unified processing for both Quad objects and RawImage elements
                    // Exclude all UI elements (unified)
                    if (!ShouldIgnoreInput(touch.screenPosition))
                    {
                        UpdatePointSelection(touch.screenPosition);
                    }
                    else
                    {
                        // Cancel if moved over UI element
                        CancelPointSelection();
                    }
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended && IsPointSelectionInProgress())
                {
                    // Unified processing for both Quad objects and RawImage elements
                    // Exclude all UI elements (unified)
                    if (!ShouldIgnoreInput(touch.screenPosition))
                    {
                        EndPointSelection(touch.screenPosition);
                    }
                    else
                    {
                        // Cancel if ended over UI element
                        CancelPointSelection();
                    }
                }
            }
#else
            // Mouse input for non-mobile platforms
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame && !IsPressed())
                {
                    // Unified processing for both Quad objects and RawImage elements
                    // Exclude all UI elements (unified)
                    if (!ShouldIgnoreInput(mouse.position.ReadValue()))
                    {
                        StartPointSelection(mouse.position.ReadValue());
                    }
                }
                else if (mouse.leftButton.isPressed && IsPressed() && IsPointSelectionInProgress())
                {
                    // Unified processing for both Quad objects and RawImage elements
                    // Exclude all UI elements (unified)
                    if (!ShouldIgnoreInput(mouse.position.ReadValue()))
                    {
                        UpdatePointSelection(mouse.position.ReadValue());
                    }
                    else
                    {
                        // Cancel if moved over UI element
                        CancelPointSelection();
                    }
                }
                else if (mouse.leftButton.wasReleasedThisFrame && IsPressed() && IsPointSelectionInProgress())
                {
                    // Unified processing for both Quad objects and RawImage elements
                    // Exclude all UI elements (unified)
                    if (!ShouldIgnoreInput(mouse.position.ReadValue()))
                    {
                        EndPointSelection(mouse.position.ReadValue());
                        // State is updated in EndPointSelection method
                    }
                    else
                    {
                        // Cancel if ended over UI element
                        CancelPointSelection();
                    }
                }
            }
#endif
        }
#endif

        /// <summary>
        /// Detect point selection using the legacy input system.
        /// Handles touch and mouse input using Unity's legacy Input class.
        /// </summary>
        private void DetectPointSelectionLegacy()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            // Touch input for mobile platforms
            int touchCount = Input.touchCount;
            if (touchCount == 1)
            {
                UnityEngine.Touch t = Input.GetTouch(0);

                if (t.phase == UnityEngine.TouchPhase.Began)
                {
                    // Unified processing for both Quad objects and RawImage elements
                    // Exclude all UI elements (unified)
                    if (!ShouldIgnoreInput(t.position))
                    {
                        StartPointSelection(t.position);
                    }
                }
                else if (t.phase == UnityEngine.TouchPhase.Moved && IsPointSelectionInProgress())
                {
                    // Unified processing for both Quad objects and RawImage elements
                    // Exclude all UI elements (unified)
                    if (!ShouldIgnoreInput(t.position))
                    {
                        UpdatePointSelection(t.position);
                    }
                    else
                    {
                        // Cancel if moved over UI element
                        CancelPointSelection();
                    }
                }
                else if (t.phase == UnityEngine.TouchPhase.Ended && IsPointSelectionInProgress())
                {
                    // Unified processing for both Quad objects and RawImage elements
                    // Exclude all UI elements (unified)
                    if (!ShouldIgnoreInput(t.position))
                    {
                        EndPointSelection(t.position);
                    }
                    else
                    {
                        // Cancel if ended over UI element
                        CancelPointSelection();
                    }
                }
            }
#else
            // Mouse input for non-mobile platforms
            if (Input.GetMouseButtonDown(0) && !IsPressed())
            {
                // Unified processing for both Quad objects and RawImage elements
                // Exclude all UI elements (unified)
                if (!ShouldIgnoreInput(Input.mousePosition))
                {
                    StartPointSelection(Input.mousePosition);
                }
            }
            else if (Input.GetMouseButton(0) && IsPressed() && IsPointSelectionInProgress())
            {
                // Unified processing for both Quad objects and RawImage elements
                // Exclude all UI elements (unified)
                if (!ShouldIgnoreInput(Input.mousePosition))
                {
                    UpdatePointSelection(Input.mousePosition);
                }
                else
                {
                    // Cancel if moved over UI element
                    CancelPointSelection();
                }
            }
            else if (Input.GetMouseButtonUp(0) && IsPressed() && IsPointSelectionInProgress())
            {
                // Unified processing for both Quad objects and RawImage elements
                // Exclude all UI elements (unified)
                if (!ShouldIgnoreInput(Input.mousePosition))
                {
                    EndPointSelection(Input.mousePosition);
                    // State is updated in EndPointSelection method
                }
                else
                {
                    // Cancel if ended over UI element
                    CancelPointSelection();
                }
            }
#endif
        }

        /// <summary>
        /// Start point selection.
        /// Initiates point selection with the given screen coordinates.
        /// </summary>
        /// <param name="screenPoint">Screen point where selection started.</param>
        private void StartPointSelection(Vector2 screenPoint)
        {
            // Convert to texture coordinates and fire event with start point
            Vector2[] texturePoints = ConvertScreenToTexturePoints(new Vector2[] { screenPoint });
            if (texturePoints != null && texturePoints.Length >= 1)
            {
                UpdateCurrentState(TextureSelectionState.POINT_SELECTION_STARTED, texturePoints);
                OnTextureSelectionStateChanged?.Invoke(gameObject, TextureSelectionState.POINT_SELECTION_STARTED, texturePoints);
            }
            else if (_fireEventOnOutsideSelect)
            {
                // Fire event with invalid coordinates when touching outside
                FireOutsideTextureTouchedEvent();
            }
        }

        /// <summary>
        /// Update point selection during drag.
        /// Updates the point selection with new screen coordinates during drag operation.
        /// </summary>
        /// <param name="screenPoint">Current screen point.</param>
        private void UpdatePointSelection(Vector2 screenPoint)
        {
            // Convert to texture coordinates and check for validity
            Vector2[] texturePoints = ConvertScreenToTexturePoints(new Vector2[] { screenPoint });

            if (texturePoints == null)
            {
                // Cancel point selection if any point is invalid
                CancelPointSelection();
                return;
            }

            // Update state to in progress
            UpdateCurrentState(TextureSelectionState.POINT_SELECTION_IN_PROGRESS, texturePoints);

            // Fire event with valid points
            OnTextureSelectionStateChanged?.Invoke(gameObject, TextureSelectionState.POINT_SELECTION_IN_PROGRESS, texturePoints);
        }

        /// <summary>
        /// End point selection.
        /// Completes the point selection with the final screen coordinates.
        /// </summary>
        /// <param name="screenPoint">Screen point where selection ended.</param>
        private void EndPointSelection(Vector2 screenPoint)
        {
            // Convert to texture coordinates and check for validity
            Vector2[] texturePoints = ConvertScreenToTexturePoints(new Vector2[] { screenPoint });

            if (texturePoints == null)
            {
                // Cancel if any point is invalid
                CancelPointSelection();
                return;
            }

            // Update state to completed
            UpdateCurrentState(TextureSelectionState.POINT_SELECTION_COMPLETED, texturePoints);

            // Fire event with valid points
            OnTextureSelectionStateChanged?.Invoke(gameObject, TextureSelectionState.POINT_SELECTION_COMPLETED, texturePoints);
        }

        /// <summary>
        /// Cancel point selection.
        /// Cancels the current point selection and resets state.
        /// </summary>
        private void CancelPointSelection()
        {
            // Create a copy of current points for the event
            Vector2[] eventPoints;
            if (_currentTexturePoints != null)
            {
                eventPoints = new Vector2[_currentTexturePoints.Length];
                System.Array.Copy(_currentTexturePoints, eventPoints, _currentTexturePoints.Length);
            }
            else
            {
                eventPoints = new Vector2[] { new Vector2(-1, -1) };
            }

            // Update state to cancelled before firing event
            UpdateCurrentState(TextureSelectionState.POINT_SELECTION_CANCELLED, eventPoints);

            // Fire cancelled event
            OnTextureSelectionStateChanged?.Invoke(gameObject, TextureSelectionState.POINT_SELECTION_CANCELLED, eventPoints);
        }

        /// <summary>
        /// Detect rectangle selection input.
        /// Handles input detection for rectangle selection mode using appropriate input system.
        /// </summary>
        private void DetectRectangleSelection()
        {
#if ENABLE_INPUT_SYSTEM
            DetectRectangleSelectionNewSystem();
#else
            DetectRectangleSelectionLegacy();
#endif
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// Detect rectangle selection using the new input system.
        /// Handles touch and mouse input for rectangle selection using Unity's new Input System.
        /// </summary>
        private void DetectRectangleSelectionNewSystem()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            // Touch input for mobile platforms
            if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count == 1)
            {
                var touch = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];

                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    // Unified processing for both Quad objects and RawImage elements
                    // Exclude all UI elements (unified)
                    if (!ShouldIgnoreInput(touch.screenPosition))
                    {
                        StartRectangleSelection(touch.screenPosition);
                    }
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved && IsRectangleSelectionInProgress())
                {
                    // Unified processing for both Quad objects and RawImage elements
                    // Exclude all UI elements (unified)
                    if (!ShouldIgnoreInput(touch.screenPosition))
                    {
                        UpdateRectangleSelection(touch.screenPosition);
                    }
                    else
                    {
                        // Cancel if moved over UI element
                        CancelRectangleSelection();
                    }
                }
                else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended && IsRectangleSelectionInProgress())
                {
                    // Unified processing for both Quad objects and RawImage elements
                    // Exclude all UI elements (unified)
                    if (!ShouldIgnoreInput(touch.screenPosition))
                    {
                        EndRectangleSelection(touch.screenPosition);
                    }
                    else
                    {
                        // Cancel if ended over UI element
                        CancelRectangleSelection();
                    }
                }
            }
#else
            // Mouse input for non-mobile platforms
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame && !IsPressed())
                {
                    // Unified processing for both Quad objects and RawImage elements
                    // Exclude all UI elements (unified)
                    if (!ShouldIgnoreInput(mouse.position.ReadValue()))
                    {
                        StartRectangleSelection(mouse.position.ReadValue());
                    }
                }
                else if (mouse.leftButton.isPressed && IsPressed() && IsRectangleSelectionInProgress())
                {
                    // Unified processing for both Quad objects and RawImage elements
                    // Exclude all UI elements (unified)
                    if (!ShouldIgnoreInput(mouse.position.ReadValue()))
                    {
                        UpdateRectangleSelection(mouse.position.ReadValue());
                    }
                    else
                    {
                        // Cancel if moved over UI element
                        CancelRectangleSelection();
                    }
                }
                else if (mouse.leftButton.wasReleasedThisFrame && IsPressed() && IsRectangleSelectionInProgress())
                {
                    // Unified processing for both Quad objects and RawImage elements
                    // Exclude all UI elements (unified)
                    if (!ShouldIgnoreInput(mouse.position.ReadValue()))
                    {
                        EndRectangleSelection(mouse.position.ReadValue());
                        // State is updated in EndRectangleSelection method
                    }
                    else
                    {
                        // Cancel if ended over UI element
                        CancelRectangleSelection();
                    }
                }
            }
#endif
        }
#endif

        /// <summary>
        /// Detect rectangle selection using the legacy input system.
        /// Handles touch and mouse input for rectangle selection using Unity's legacy Input class.
        /// </summary>
        private void DetectRectangleSelectionLegacy()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            // Touch input for mobile platforms
            int touchCount = Input.touchCount;
            if (touchCount == 1)
            {
                UnityEngine.Touch t = Input.GetTouch(0);

                if (t.phase == UnityEngine.TouchPhase.Began)
                {
                    // Unified processing for both Quad objects and RawImage elements
                    // Exclude all UI elements (unified)
                    if (!ShouldIgnoreInput(t.position))
                    {
                        StartRectangleSelection(t.position);
                    }
                }
                else if (t.phase == UnityEngine.TouchPhase.Moved && IsRectangleSelectionInProgress())
                {
                    // Unified processing for both Quad objects and RawImage elements
                    // Exclude all UI elements (unified)
                    if (!ShouldIgnoreInput(t.position))
                    {
                        UpdateRectangleSelection(t.position);
                    }
                    else
                    {
                        // Cancel if moved over UI element
                        CancelRectangleSelection();
                    }
                }
                else if (t.phase == UnityEngine.TouchPhase.Ended && IsRectangleSelectionInProgress())
                {
                    // Unified processing for both Quad objects and RawImage elements
                    // Exclude all UI elements (unified)
                    if (!ShouldIgnoreInput(t.position))
                    {
                        EndRectangleSelection(t.position);
                    }
                    else
                    {
                        // Cancel if ended over UI element
                        CancelRectangleSelection();
                    }
                }
            }
#else
            // Mouse input for non-mobile platforms
            if (Input.GetMouseButtonDown(0) && !IsPressed())
            {
                // Unified processing for both Quad objects and RawImage elements
                // Exclude all UI elements (unified)
                if (!ShouldIgnoreInput(Input.mousePosition))
                {
                    StartRectangleSelection(Input.mousePosition);
                }
            }
            else if (Input.GetMouseButton(0) && IsPressed() && IsRectangleSelectionInProgress())
            {
                // Unified processing for both Quad objects and RawImage elements
                // Exclude all UI elements (unified)
                if (!ShouldIgnoreInput(Input.mousePosition))
                {
                    UpdateRectangleSelection(Input.mousePosition);
                }
                else
                {
                    // Cancel if moved over UI element
                    CancelRectangleSelection();
                }
            }
            else if (Input.GetMouseButtonUp(0) && IsPressed() && IsRectangleSelectionInProgress())
            {
                // Unified processing for both Quad objects and RawImage elements
                // Exclude all UI elements (unified)
                if (!ShouldIgnoreInput(Input.mousePosition))
                {
                    EndRectangleSelection(Input.mousePosition);
                    // State is updated in EndRectangleSelection method
                }
                else
                {
                    // Cancel if ended over UI element
                    CancelRectangleSelection();
                }
            }
#endif
        }

        /// <summary>
        /// Check if input should be ignored due to UI overlap.
        /// For Quad objects, checks if any UI element is overlapping.
        /// For RawImage elements, checks if other UI elements (excluding this RawImage) are overlapping.
        /// </summary>
        /// <param name="position">Input position</param>
        /// <returns>True if input should be ignored</returns>
        private bool ShouldIgnoreInput(Vector2 position)
        {
            if (_isQuadObject)
            {
                // For Quad objects, exclude all UI elements
                return EventSystem.current.IsPointerOverGameObject();
            }
            else
            {
                // For RawImage elements, check if other UI elements (excluding this RawImage) are overlapping

                // Stage 1: Check if any UI element is overlapping
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    // No UI elements overlapping - safe to proceed
                    return false;
                }

                // Stage 2: Check if it's this specific RawImage
                if (IsPointerOverThisRawImageElement(position))
                {
                    // This RawImage is being touched - check if other UI elements are overlapping this RawImage
                    return IsOtherUIElementOnTop(position);
                }

                // Touching other UI elements but not this RawImage - ignore input
                return true;
            }
        }

        /// <summary>
        /// Fire OUTSIDE_TEXTURE_SELECTED event with invalid coordinates.
        /// Triggers the event when touch occurs outside the texture area.
        /// </summary>
        private void FireOutsideTextureTouchedEvent()
        {
            Vector2[] invalidPoints = new Vector2[] { new Vector2(-1, -1) };
            UpdateCurrentState(TextureSelectionState.OUTSIDE_TEXTURE_SELECTED, invalidPoints);
            OnTextureSelectionStateChanged?.Invoke(gameObject, TextureSelectionState.OUTSIDE_TEXTURE_SELECTED, invalidPoints);
        }

        /// <summary>
        /// Cancel rectangle selection.
        /// Cancels the current rectangle selection and resets state.
        /// </summary>
        private void CancelRectangleSelection()
        {
            // Create a copy of current points for the event
            Vector2[] eventPoints;
            if (_currentTexturePoints != null)
            {
                eventPoints = new Vector2[_currentTexturePoints.Length];
                System.Array.Copy(_currentTexturePoints, eventPoints, _currentTexturePoints.Length);
            }
            else
            {
                eventPoints = new Vector2[] { new Vector2(-1, -1) };
            }

            // Update state to cancelled before firing event
            UpdateCurrentState(TextureSelectionState.RECTANGLE_SELECTION_CANCELLED, eventPoints);

            // Fire cancelled event
            OnTextureSelectionStateChanged?.Invoke(gameObject, TextureSelectionState.RECTANGLE_SELECTION_CANCELLED, eventPoints);
        }

        /// <summary>
        /// Start rectangle selection.
        /// Initiates rectangle selection with the given screen coordinates.
        /// </summary>
        /// <param name="screenPoint">Screen point where selection started.</param>
        private void StartRectangleSelection(Vector2 screenPoint)
        {
            _currentRectangleStartPoint = screenPoint;

            // Convert to texture coordinates and fire event with start point duplicated
            Vector2[] texturePoints = ConvertScreenToTexturePoints(new Vector2[] { screenPoint, screenPoint });
            if (texturePoints != null && texturePoints.Length >= 2)
            {
                UpdateCurrentState(TextureSelectionState.RECTANGLE_SELECTION_STARTED, texturePoints);
                OnTextureSelectionStateChanged?.Invoke(gameObject, TextureSelectionState.RECTANGLE_SELECTION_STARTED, texturePoints);
            }
            else if (_fireEventOnOutsideSelect)
            {
                // Fire event with invalid coordinates when touching outside
                FireOutsideTextureTouchedEvent();
            }
        }

        /// <summary>
        /// Update rectangle selection during drag.
        /// Updates the rectangle selection with new screen coordinates during drag operation.
        /// </summary>
        /// <param name="screenPoint">Current screen point.</param>
        private void UpdateRectangleSelection(Vector2 screenPoint)
        {
            // Convert to texture coordinates and check for validity
            Vector2[] texturePoints = ConvertScreenToTexturePoints(new Vector2[] { _currentRectangleStartPoint, screenPoint });

            if (texturePoints == null)
            {
                // Cancel rectangle selection if any point is invalid
                CancelRectangleSelection();
                return;
            }

            // Update state to in progress
            UpdateCurrentState(TextureSelectionState.RECTANGLE_SELECTION_IN_PROGRESS, texturePoints);

            // Fire event with valid points
            OnTextureSelectionStateChanged?.Invoke(gameObject, TextureSelectionState.RECTANGLE_SELECTION_IN_PROGRESS, texturePoints);
        }

        /// <summary>
        /// End rectangle selection.
        /// Completes the rectangle selection with the final screen coordinates.
        /// </summary>
        /// <param name="screenPoint">Screen point where selection ended.</param>
        private void EndRectangleSelection(Vector2 screenPoint)
        {
            // Convert to texture coordinates and check for validity
            Vector2[] texturePoints = ConvertScreenToTexturePoints(new Vector2[] { _currentRectangleStartPoint, screenPoint });

            if (texturePoints == null)
            {
                // Cancel if any point is invalid
                CancelRectangleSelection();
                return;
            }

            // Check if the distance between start and end points is too small (less than 1% of texture size)
            if (IsRectangleSelectionTooSmall(texturePoints[0], texturePoints[1]))
            {
                // Cancel rectangle selection if the selection area is too small
                CancelRectangleSelection();
                return;
            }

            // Update state to completed
            UpdateCurrentState(TextureSelectionState.RECTANGLE_SELECTION_COMPLETED, texturePoints);

            // Fire event with valid points
            OnTextureSelectionStateChanged?.Invoke(gameObject, TextureSelectionState.RECTANGLE_SELECTION_COMPLETED, texturePoints);
        }

        /// <summary>
        /// Check if rectangle selection is too small (less than 1% of texture size).
        /// Prevents accidental selections by checking if the rectangle area is too small.
        /// </summary>
        /// <param name="startPoint">Rectangle selection start point in texture coordinates.</param>
        /// <param name="endPoint">Rectangle selection end point in texture coordinates.</param>
        /// <returns>True if the selection is too small and should be cancelled.</returns>
        private bool IsRectangleSelectionTooSmall(Vector2 startPoint, Vector2 endPoint)
        {
            // Check if x or y coordinates are the same (no area selection)
            if (Mathf.Approximately(startPoint.x, endPoint.x) || Mathf.Approximately(startPoint.y, endPoint.y))
            {
                return true; // Selection is too small if coordinates are the same
            }

            // Calculate the distance between start and end points
            float distance = Vector2.Distance(startPoint, endPoint);

            // Get current texture dimensions
            int textureWidth = GetCurrentTextureWidth();
            int textureHeight = GetCurrentTextureHeight();

            // Validate texture dimensions
            if (textureWidth <= 0 || textureHeight <= 0)
            {
                Debug.LogWarning("TextureSelector: Invalid texture dimensions for distance calculation");
                return false; // Don't cancel if we can't determine texture size
            }

            // Calculate 1% of the texture diagonal (most conservative approach)
            float textureDiagonal = Mathf.Sqrt(textureWidth * textureWidth + textureHeight * textureHeight);
            float threshold = textureDiagonal * 0.01f; // 1% of texture diagonal

            // Check if distance is below threshold
            bool isTooSmall = distance < threshold;

            return isTooSmall;
        }

        /// <summary>
        /// Convert screen points to texture points.
        /// Converts screen coordinates to texture coordinates using appropriate method based on object type.
        /// </summary>
        /// <param name="screenPoints">Screen points to convert.</param>
        /// <returns>Converted texture points. Returns null if any point is invalid.</returns>
        private Vector2[] ConvertScreenToTexturePoints(Vector2[] screenPoints)
        {
            Vector2[] texturePoints = new Vector2[screenPoints.Length];
            bool hasInvalidPoint = false;

            for (int i = 0; i < screenPoints.Length; i++)
            {
                Vector2 texturePoint;
                bool isValid = false;

                if (_isQuadObject)
                {
                    isValid = ProcessQuadTouch(screenPoints[i], out texturePoint);
                }
                else
                {
                    isValid = ProcessRawImageTouch(screenPoints[i], out texturePoint);
                }

                if (isValid)
                {
                    // Convert coordinates if needed
                    if (!_useOpenCVMatCoordinates)
                    {
                        texturePoint = ConvertToUnityCoordinates(texturePoint);
                    }
                    texturePoints[i] = texturePoint;
                }
                else
                {
                    texturePoints[i] = new Vector2(-1, -1); // Invalid point
                    hasInvalidPoint = true;
                }
            }

            // If any point is invalid, return null to indicate cancellation
            if (hasInvalidPoint)
            {
                return null;
            }

            // Convert coordinates to integers
            for (int i = 0; i < texturePoints.Length; i++)
            {
                texturePoints[i] = new Vector2(
                    Mathf.RoundToInt(texturePoints[i].x),
                    Mathf.RoundToInt(texturePoints[i].y)
                );
            }

            return texturePoints;
        }

        /// <summary>
        /// Initialize components and detect object type (Quad or RawImage).
        /// Sets up the component based on the detected object type and validates required components.
        /// </summary>
        private void InitializeComponents()
        {
            _renderer = GetComponent<Renderer>();
            _rawImage = GetComponent<RawImage>();

            if (_renderer != null && _rawImage != null)
            {
                Debug.LogWarning("TextureSelector: Both Renderer and RawImage components found. Using Renderer for Quad.");
                _isQuadObject = true;
            }
            else if (_renderer != null)
            {
                _isQuadObject = true;
            }
            else if (_rawImage != null)
            {
                _isQuadObject = false;
            }
            else
            {
                Debug.LogError("TextureSelector: Neither Renderer nor RawImage component found! This component only supports Quad mesh with Renderer or RawImage.");
                return;
            }

            // Validate collider type for Quad objects
            if (_isQuadObject)
            {
                ValidateColliderType();
            }

            // Set appropriate camera based on object type
            if (_isQuadObject)
            {
                // Use inspector-assigned camera for Quad objects, fallback to MainCamera
                _internalTargetCamera = _targetCamera;
                if (_internalTargetCamera == null)
                {
                    _internalTargetCamera = Camera.main;
                    if (_internalTargetCamera == null)
                    {
                        Debug.LogError("TextureSelector: No camera assigned and MainCamera not found for Quad object!");
                        return;
                    }
                    //Debug.Log("TextureSelector: Using MainCamera as fallback for Quad object");
                }
                else
                {
                    //Debug.Log($"TextureSelector: Using assigned camera for Quad object: {_internalTargetCamera.name}");
                }
            }
            else
            {
                // For RawImage elements, get camera from Canvas
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        _internalTargetCamera = null; // ScreenSpaceOverlay doesn't need camera
                        //Debug.Log("TextureSelector: Using ScreenSpaceOverlay mode (no camera needed)");
                    }
                    else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    {
                        _internalTargetCamera = canvas.worldCamera;
                        if (_internalTargetCamera == null)
                        {
                            _internalTargetCamera = Camera.main;
                        }
                        //Debug.Log($"TextureSelector: Using ScreenSpaceCamera mode with camera: {_internalTargetCamera.name}");
                    }
                    else if (canvas.renderMode == RenderMode.WorldSpace)
                    {
                        _internalTargetCamera = Camera.main;
                        //Debug.Log("TextureSelector: Using WorldSpace mode with main camera");
                    }
                }
                else
                {
                    Debug.LogError("TextureSelector: Canvas not found for RawImage element!");
                    return;
                }
            }

        }

        /// <summary>
        /// Validate that the collider type is supported (MeshCollider with Quad mesh only for Quad objects).
        /// Ensures the collider is appropriate for touch detection on Quad objects.
        /// </summary>
        /// <returns>True if collider type is valid, false otherwise.</returns>
        private bool ValidateColliderType()
        {
            Collider collider = GetComponent<Collider>();
            if (collider == null)
            {
                Debug.LogError("TextureSelector: No Collider component found! This component requires a MeshCollider for Quad objects.");
                return false;
            }

            // Check if it's a MeshCollider
            if (collider is MeshCollider)
            {
                MeshCollider meshCollider = collider as MeshCollider;

                // Check if it's a Quad mesh
                if (IsQuadMesh(meshCollider.sharedMesh))
                {
                    //Debug.Log("TextureSelector: Valid MeshCollider with Quad mesh detected for Quad object.");
                    return true;
                }
                else
                {
                    Debug.LogError("TextureSelector: MeshCollider detected but it's not a Quad mesh! This component only supports MeshCollider with Quad mesh for Quad objects.");
                    return false;
                }
            }
            else
            {
                string colliderTypeName = collider.GetType().Name;
                Debug.LogError($"TextureSelector: Unsupported collider type '{colliderTypeName}' detected! This component only supports MeshCollider with Quad mesh for Quad objects.");
                return false;
            }
        }

        /// <summary>
        /// Check if the mesh is a Quad mesh (required for Quad objects).
        /// Validates that the mesh has the correct structure for Quad objects.
        /// </summary>
        /// <param name="mesh">The mesh to check.</param>
        /// <returns>True if the mesh is a Quad, false otherwise.</returns>
        private bool IsQuadMesh(Mesh mesh)
        {
            if (mesh == null)
            {
                return false;
            }

            // A Quad mesh should have (required for Quad objects):
            // - 4 vertices
            // - 2 triangles (6 indices)
            // - 4 UV coordinates
            return mesh.vertexCount == 4 &&
                   mesh.triangles.Length == 6 &&
                   mesh.uv.Length == 4;
        }



        /// <summary>
        /// Process touch for Quad objects using raycast.
        /// Implementation: Screen coordinates → Raycast → Quad collider position → Texture UV coordinates
        /// Uses raycast to determine if touch hits the Quad and converts to texture coordinates.
        /// </summary>
        /// <param name="screenPoint">Screen point.</param>
        /// <param name="texturePoint">Output texture point.</param>
        /// <returns>True if valid touch, false otherwise.</returns>
        private bool ProcessQuadTouch(Vector2 screenPoint, out Vector2 texturePoint)
        {
            texturePoint = Vector2.zero;

            // Input validation
            if (_internalTargetCamera == null)
            {
                Debug.LogError("TextureSelector: Target camera is null!");
                return false;
            }

            if (_renderer == null)
            {
                Debug.LogError("TextureSelector: Renderer component is null!");
                return false;
            }

            // Step 1: Create ray from screen coordinates
            Ray ray = _internalTargetCamera.ScreenPointToRay(screenPoint);
            RaycastHit hit;

            // Step 2: Perform raycast to get intersection with Quad collider
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    // Validate hit point and texture coordinates
                    if (float.IsNaN(hit.point.x) || float.IsNaN(hit.point.y) || float.IsNaN(hit.point.z))
                    {
                        Debug.LogError("TextureSelector: Invalid hit point detected!");
                        return false;
                    }

                    if (float.IsNaN(hit.textureCoord.x) || float.IsNaN(hit.textureCoord.y))
                    {
                        Debug.LogError("TextureSelector: Invalid texture coordinates detected!");
                        return false;
                    }

                    // Step 3: Convert UV coordinates to texture coordinates
                    texturePoint = ConvertUVToTextureCoordinates(hit.textureCoord, "MeshCollider");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Process touch for RawImage elements using RectTransform approach.
        /// Implementation: Screen coordinates → RectTransform → RawImage position → Texture UV coordinates
        /// Always uses RectTransform method for all Canvas render modes (ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace).
        /// </summary>
        /// <param name="screenPoint">Screen point.</param>
        /// <param name="texturePoint">Output texture point.</param>
        /// <returns>True if valid touch, false otherwise.</returns>
        private bool ProcessRawImageTouch(Vector2 screenPoint, out Vector2 texturePoint)
        {
            //Debug.Log($"TextureSelector: ProcessRawImageTouch called - screenPoint: {screenPoint}");
            texturePoint = Vector2.zero;

            if (_rawImage == null)
            {
                Debug.LogError("TextureSelector: RawImage is null in ProcessRawImageTouch!");
                return false;
            }

            // Always use RectTransform-based approach for all Canvas render modes
            return ProcessRawImageTouchWithRectTransform(screenPoint, out texturePoint);
        }

        /// <summary>
        /// Process RawImage touch using raycast (when camera is available).
        /// Uses raycast method for RawImage elements when camera is available.
        /// </summary>
        /// <param name="screenPoint">Screen point.</param>
        /// <param name="texturePoint">Output texture point.</param>
        /// <returns>True if valid touch, false otherwise.</returns>
        private bool ProcessRawImageTouchWithRaycast(Vector2 screenPoint, out Vector2 texturePoint)
        {
            texturePoint = Vector2.zero;

            // Step 1: Create ray from screen coordinates
            Ray ray = _internalTargetCamera.ScreenPointToRay(screenPoint);
            RaycastHit hit;

            // Step 2: Perform raycast to get intersection with RawImage collider
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    // Validate hit point and texture coordinates
                    if (float.IsNaN(hit.point.x) || float.IsNaN(hit.point.y) || float.IsNaN(hit.point.z))
                    {
                        Debug.LogError("TextureSelector: Invalid hit point detected in RawImage!");
                        return false;
                    }

                    if (float.IsNaN(hit.textureCoord.x) || float.IsNaN(hit.textureCoord.y))
                    {
                        Debug.LogError("TextureSelector: Invalid texture coordinates detected in RawImage!");
                        return false;
                    }

                    // Step 3: Convert UV coordinates to texture coordinates (same as Quad)
                    texturePoint = ConvertUVToTextureCoordinates(hit.textureCoord, "RawImage-Raycast");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Process RawImage touch using RectTransform (for ScreenSpaceOverlay and WorldSpace).
        /// Uses RectTransform method for RawImage elements in ScreenSpaceOverlay and WorldSpace modes.
        /// </summary>
        /// <param name="screenPoint">Screen point.</param>
        /// <param name="texturePoint">Output texture point.</param>
        /// <returns>True if valid touch, false otherwise.</returns>
        private bool ProcessRawImageTouchWithRectTransform(Vector2 screenPoint, out Vector2 texturePoint)
        {
            texturePoint = Vector2.zero;

            RectTransform rectTransform = _rawImage.rectTransform;
            Vector2 localPoint;

            // Use appropriate camera based on Canvas render mode
            // ScreenSpaceOverlay: null camera
            // ScreenSpaceCamera/WorldSpace: use assigned camera
            Camera cameraToUse = _internalTargetCamera;

            //Debug.Log($"TextureSelector: Using RectTransform approach with camera: {cameraToUse?.name ?? "null"}");

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPoint, cameraToUse, out localPoint))
            {
                //Debug.Log($"TextureSelector: localPoint: {localPoint}");
                // Check if point is within the rect
                if (rectTransform.rect.Contains(localPoint))
                {
                    //Debug.Log("TextureSelector: Point is within rect, converting to texture point");
                    // Convert local point to UV coordinates (0-1 range)
                    Vector2 uvCoordinates = ConvertLocalPointToUV(localPoint, rectTransform);
                    texturePoint = ConvertUVToTextureCoordinates(uvCoordinates, "RawImage-RectTransform");
                    //Debug.Log($"TextureSelector: texturePoint: {texturePoint}");
                    return true;
                }
                else
                {
                    //Debug.Log("TextureSelector: Point is not within rect");
                }
            }
            else
            {
                //Debug.Log("TextureSelector: RectTransformUtility.ScreenPointToLocalPointInRectangle failed");
            }

            return false;
        }

        /// <summary>
        /// Convert UV coordinates to texture coordinates (unified for both Quad and RawImage).
        /// Implementation: UV coordinates (0-1) → Texture pixel coordinates
        /// Converts normalized UV coordinates to actual texture pixel coordinates.
        /// </summary>
        /// <param name="uvCoordinates">UV coordinates in 0-1 range.</param>
        /// <param name="sourceType">Source type for debug information.</param>
        /// <returns>Texture coordinates.</returns>
        private Vector2 ConvertUVToTextureCoordinates(Vector2 uvCoordinates, string sourceType = "Unknown")
        {
            // Input validation
            if (float.IsNaN(uvCoordinates.x) || float.IsNaN(uvCoordinates.y))
            {
                Debug.LogError($"TextureSelector: Invalid UV coordinates in ConvertUVToTextureCoordinates from {sourceType}!");
                return Vector2.zero;
            }

            // Validate UV coordinates are within valid range (0-1)
            if (uvCoordinates.x < 0f || uvCoordinates.x > 1f || uvCoordinates.y < 0f || uvCoordinates.y > 1f)
            {
                Debug.LogWarning($"TextureSelector: UV coordinates out of range (0-1) from {sourceType}: {uvCoordinates}");
                // Clamp to valid range
                uvCoordinates.x = Mathf.Clamp01(uvCoordinates.x);
                uvCoordinates.y = Mathf.Clamp01(uvCoordinates.y);
            }

            // Get current texture size (may have changed since initialization)
            int currentTextureWidth = GetCurrentTextureWidth();
            int currentTextureHeight = GetCurrentTextureHeight();

            // Validate texture dimensions
            if (currentTextureWidth <= 0 || currentTextureHeight <= 0)
            {
                Debug.LogError($"TextureSelector: Invalid texture dimensions - Width: {currentTextureWidth}, Height: {currentTextureHeight}");
                return Vector2.zero;
            }

            // Convert from normalized UV coordinates to texture pixel coordinates
            float textureX = uvCoordinates.x * currentTextureWidth;
            float textureY = uvCoordinates.y * currentTextureHeight;

            // Validate calculated coordinates
            if (float.IsNaN(textureX) || float.IsNaN(textureY))
            {
                Debug.LogError("TextureSelector: Calculated texture coordinates are NaN!");
                return Vector2.zero;
            }

            // Convert from Unity coordinates (bottom-left origin) to OpenCV coordinates (top-left origin)
            float openCVY = currentTextureHeight - textureY;

            // Final validation
            if (float.IsNaN(openCVY))
            {
                Debug.LogError("TextureSelector: Final OpenCV Y coordinate is NaN!");
                return Vector2.zero;
            }

            return new Vector2(textureX, openCVY);
        }




        /// <summary>
        /// Convert local point to UV coordinates for RawImage elements (RectTransform approach).
        /// Converts RectTransform local coordinates to normalized UV coordinates.
        /// </summary>
        /// <param name="localPoint">Local point in RectTransform.</param>
        /// <param name="rectTransform">RectTransform.</param>
        /// <returns>UV coordinates (0-1 range).</returns>
        private Vector2 ConvertLocalPointToUV(Vector2 localPoint, RectTransform rectTransform)
        {
            // Convert from RectTransform local coordinates to UV coordinates (0-1 range)
            UnityEngine.Rect rect = rectTransform.rect;

            // Normalize to 0-1 range (RectTransform uses bottom-left origin)
            float normalizedX = (localPoint.x - rect.xMin) / rect.width;
            float normalizedY = (localPoint.y - rect.yMin) / rect.height;

            // Clamp to valid range
            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedY = Mathf.Clamp01(normalizedY);

            //Debug.Log($"TextureSelector RawImage UV Conversion:");
            //Debug.Log($"  localPoint: ({localPoint.x}, {localPoint.y})");
            //Debug.Log($"  rect: {rect}");
            //Debug.Log($"  UV coordinates: ({normalizedX}, {normalizedY})");

            return new Vector2(normalizedX, normalizedY);
        }



        /// <summary>
        /// Get current texture width.
        /// Retrieves the current texture width from the appropriate source (Renderer or RawImage).
        /// </summary>
        /// <returns>Current texture width. Returns 0 if texture is not available.</returns>
        private int GetCurrentTextureWidth()
        {
            if (_isQuadObject && _renderer?.material?.mainTexture != null)
            {
                return _renderer.material.mainTexture.width;
            }
            else if (!_isQuadObject && _rawImage?.texture != null)
            {
                return _rawImage.texture.width;
            }
            return 0; // No texture available
        }

        /// <summary>
        /// Get current texture height.
        /// Retrieves the current texture height from the appropriate source (Renderer or RawImage).
        /// </summary>
        /// <returns>Current texture height. Returns 0 if texture is not available.</returns>
        private int GetCurrentTextureHeight()
        {
            if (_isQuadObject && _renderer?.material?.mainTexture != null)
            {
                return _renderer.material.mainTexture.height;
            }
            else if (!_isQuadObject && _rawImage?.texture != null)
            {
                return _rawImage.texture.height;
            }
            return 0; // No texture available
        }

        /// <summary>
        /// Check if there are other UI elements on top of this RawImage.
        /// Determines if other UI elements are overlapping this RawImage at the given position.
        /// </summary>
        /// <param name="screenPosition">Screen position to check for overlapping UI elements.</param>
        /// <returns>True if other UI elements are overlapping this RawImage, false otherwise.</returns>
        private bool IsOtherUIElementOnTop(Vector2 screenPosition)
        {
            // Get all UI elements at the specified screen position
            var eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            // Filter out this RawImage and its children
            foreach (var result in results)
            {
                if (result.gameObject != gameObject && !result.gameObject.transform.IsChildOf(transform))
                {
                    // Found another UI element on top - early exit for better performance
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Check if pointer is over this specific RawImage element.
        /// Determines if the pointer is positioned over this specific RawImage element.
        /// </summary>
        /// <param name="screenPoint">Screen point.</param>
        /// <returns>True if pointer is over this RawImage element, false otherwise.</returns>
        private bool IsPointerOverThisRawImageElement(Vector2 screenPoint)
        {
            if (_rawImage == null)
            {
                Debug.LogWarning("TextureSelector: RawImage is null!");
                return false;
            }

            RectTransform rectTransform = _rawImage.rectTransform;
            Vector2 localPoint;

            //Debug.Log($"TextureSelector RawImage Debug - IsPointerOverThisRawImageElement:");
            //Debug.Log($"  screenPoint: {screenPoint}");
            //Debug.Log($"  rectTransform: {rectTransform}");
            //Debug.Log($"  rectTransform.rect: {rectTransform.rect}");
            //Debug.Log($"  _internalTargetCamera: {_internalTargetCamera}");

            // For ScreenSpaceOverlay, use null camera
            Camera cameraToUse = _internalTargetCamera;
            if (_internalTargetCamera == null)
            {
                //Debug.Log("  Using null camera for ScreenSpaceOverlay");
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, screenPoint, cameraToUse, out localPoint))
            {
                //Debug.Log($"  localPoint: {localPoint}");
                //Debug.Log($"  rectContains: {rectTransform.rect.Contains(localPoint)}");

                // Check if point is within the rect
                bool isOver = rectTransform.rect.Contains(localPoint);
                //Debug.Log($"  IsPointerOverThisRawImageElement result: {isOver}");
                return isOver;
            }

            //Debug.Log("  RectTransformUtility.ScreenPointToLocalPointInRectangle failed");
            return false;
        }

        /// <summary>
        /// Convert OpenCV coordinates to Unity coordinates.
        /// Converts from OpenCV coordinate system (top-left origin) to Unity coordinate system (bottom-left origin).
        /// </summary>
        /// <param name="openCVPoint">OpenCV coordinates (top-left origin).</param>
        /// <returns>Unity coordinates (bottom-left origin).</returns>
        private Vector2 ConvertToUnityCoordinates(Vector2 openCVPoint)
        {
            // Get current texture height (may have changed since initialization)
            int currentTextureHeight = GetCurrentTextureHeight();
            return new Vector2(openCVPoint.x, currentTextureHeight - openCVPoint.y);
        }

        /// <summary>
        /// Calculate text position to ensure it stays within the Mat bounds.
        /// Calculates appropriate text position to avoid clipping at Mat boundaries.
        /// </summary>
        /// <param name="point">The point near which to display text as ValueTuple (x, y).</param>
        /// <param name="matWidth">Width of the Mat.</param>
        /// <param name="matHeight">Height of the Mat.</param>
        /// <param name="scale">Scaling factor for text positioning.</param>
        /// <returns>Adjusted text position as ValueTuple (x, y).</returns>
        private (double x, double y) CalculateTextPosition((double x, double y) point, int matWidth, int matHeight, float scale)
        {
            // Adjust padding and margins based on scale
            int margin = Mathf.Max(20, Mathf.RoundToInt(40 * scale));
            int textOffset = Mathf.Max(4, Mathf.RoundToInt(8 * scale));
            int textSpacing = Mathf.Max(30, Mathf.RoundToInt(65 * scale));

            double textX = point.x + textOffset;
            double textY = point.y - textOffset;

            // Adjust X position if text would go off the right edge
            if (textX > matWidth - margin)
            {
                textX = point.x - textOffset - textSpacing; // Move even further to the left of the point
            }

            // Adjust Y position if text would go off the top edge
            if (textY < margin)
            {
                int belowOffset = Mathf.Max(2, Mathf.RoundToInt(5 * scale));
                textY = point.y + textOffset + belowOffset; // Move further below the point
            }

            return (textX, textY);
        }

        #endregion
    }
}
