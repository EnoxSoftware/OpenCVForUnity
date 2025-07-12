using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.VideoModule;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif

namespace OpenCVForUnityExample
{
    /// <summary>
    /// TransformECC Example
    /// This example demonstrates how to track specific points between consecutive frames in a video sequence using OpenCV's findTransformECC method.
    /// By applying the calculated transformation matrix to each frame, the program ensures that designated points remain aligned,
    /// enabling smooth tracking across a series of frames. This technique is especially useful for stabilizing video or analyzing object motion in changing scenes.
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class TransformECCExample : MonoBehaviour
    {
        // Enums
        /// <summary>
        /// Warp mode preset enum
        /// </summary>
        public enum WarpModePreset : int
        {
            TRANSLATION = 0,
            EUCLIDEAN,
            AFFINE,
            HOMOGRAPHY
        }

        // Constants
        // downscaling ratio.
        private const float SCALING = 3f;

        // Specify the number of iterations.
        private const int NUMBER_OF_ITERATIONS = 50; // 5000;

        // Specify the threshold of the increment
        // in the correlation coefficient between two iterations
        private const double TERMINATION_EPS = 1e-7; // 1e-10;

        // Public Fields
        /// <summary>
        /// The warp mode dropdown.
        /// </summary>
        public Dropdown WarpModeDropdown;

        /// <summary>
        /// parameter, specifying the type of motion.
        /// </summary>
        public WarpModePreset WarpMode = (WarpModePreset)Video.MOTION_EUCLIDEAN;

        // Private Fields
        private float _x;
        private float _y;

        private Mat _scalingMat;
        private Mat _grayMat;
        private Mat _lastGrayMat;

        private Mat _lastWarpMatrix;

        // Define termination criteria
        private TermCriteria _criteria = new TermCriteria(TermCriteria.EPS | TermCriteria.COUNT, NUMBER_OF_ITERATIONS, TERMINATION_EPS);

        /// <summary>
        /// The stored touch point.
        /// </summary>
        private Point _storedTouchPoint;

        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;
            _multiSource2MatHelper.Initialize();

            // Update GUI state
            WarpModeDropdown.value = (int)WarpMode;

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "Touch the screen to lock the point.";
            }
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
#if ENABLE_INPUT_SYSTEM
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            // Touch input for mobile platforms
            if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count == 1)
            {
                foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
                {
                    if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended)
                    {
                        if (!EventSystem.current.IsPointerOverGameObject(touch.finger.index))
                        {
                            _storedTouchPoint = new Point(touch.screenPosition.x, touch.screenPosition.y);
                        }
                    }
                }
            }
#else
            // Mouse input for non-mobile platforms
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasReleasedThisFrame)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                _storedTouchPoint = new Point(mouse.position.ReadValue().x, mouse.position.ReadValue().y);
            }
#endif
#else
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            //Touch
            int touchCount = Input.touchCount;
            if (touchCount == 1)
            {
                Touch t = Input.GetTouch(0);
                if(t.phase == TouchPhase.Ended && !EventSystem.current.IsPointerOverGameObject (t.fingerId)) {
                    _storedTouchPoint = new Point (t.position.x, t.position.y);
                }
            }
#else
            //Mouse
            if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                _storedTouchPoint = new Point(Input.mousePosition.x, Input.mousePosition.y);
            }
#endif
#endif

            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {
                if (_storedTouchPoint != null)
                {
                    ConvertScreenPointToTexturePoint(_storedTouchPoint, _storedTouchPoint, gameObject, _texture.width, _texture.height);
                    _x = (float)_storedTouchPoint.x / SCALING;
                    _y = (float)_storedTouchPoint.y / SCALING;
                    _storedTouchPoint = null;
                }

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                Imgproc.resize(rgbaMat, _scalingMat, new Size(rgbaMat.width() / SCALING, rgbaMat.height() / SCALING));
                Imgproc.cvtColor(_scalingMat, _grayMat, Imgproc.COLOR_RGBA2GRAY);

                if (_lastGrayMat == null)
                    _lastGrayMat = _grayMat.clone();

                if (_lastWarpMatrix == null)
                {
                    // Define 2x3 or 3x3 matrices and initialize the matrix to identity
                    if (WarpMode == (WarpModePreset)Video.MOTION_HOMOGRAPHY)
                    {
                        _lastWarpMatrix = Mat.eye(3, 3, CvType.CV_32F);
                    }
                    else
                    {
                        _lastWarpMatrix = Mat.eye(2, 3, CvType.CV_32F);
                    }
                }

                try
                {
                    // Run the ECC algorithm. The results are stored in warp_matrix.
                    double ret = Video.findTransformECC(_lastGrayMat, _grayMat, _lastWarpMatrix, (int)WarpMode, _criteria);
                }
                catch (CvException e)
                {
                    Debug.LogWarning("Warning: An error occurred while analyzing frame motion; the transformation did not converge," +
                        " so this frame will be skipped. Error details: " + e);

                    _lastWarpMatrix?.Dispose(); _lastWarpMatrix = null;
                    _grayMat.copyTo(_lastGrayMat);
                    return;
                }

                _grayMat.copyTo(_lastGrayMat);

                // Apply warp_matrix to x, y.
                Mat xyz = new Mat(3, 1, CvType.CV_32F);
                xyz.put(0, 0, _x);
                xyz.put(1, 0, _y);
                xyz.put(2, 0, 1.0);
                Mat result = _lastWarpMatrix.matMul(xyz);
                _x = (float)result.get(0, 0)[0];
                _y = (float)result.get(1, 0)[0];

                // Draw a box in the frame at x, y
                Imgproc.rectangle(rgbaMat, new Point((_x * SCALING) - 1, (_y * SCALING) - 1), new Point((_x * SCALING) + 1, (_y * SCALING) + 1), new Scalar(255, 0, 255, 255), 2);
                Imgproc.rectangle(rgbaMat, new Point((_x * SCALING) - 8, (_y * SCALING) - 8), new Point((_x * SCALING) + 8, (_y * SCALING) + 8), new Scalar(0, 255, 0, 255), 2);

                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);
            }
        }

        private void OnDestroy()
        {
            _multiSource2MatHelper?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = _multiSource2MatHelper.GetMat();

            _texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            OpenCVMatUtils.MatToTexture2D(rgbaMat, _texture);

            // Set the Texture2D as the main texture of the Renderer component attached to the game object
            gameObject.GetComponent<Renderer>().material.mainTexture = _texture;

            // Adjust the scale of the game object to match the dimensions of the texture
            gameObject.transform.localScale = new Vector3(rgbaMat.cols(), rgbaMat.rows(), 1);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            // Adjust the orthographic size of the main Camera to fit the aspect ratio of the image
            float width = rgbaMat.width();
            float height = rgbaMat.height();
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("width", rgbaMat.width().ToString());
                _fpsMonitor.Add("height", rgbaMat.height().ToString());
                _fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            _scalingMat = new Mat((int)(rgbaMat.rows() / SCALING), (int)(rgbaMat.cols() / SCALING), CvType.CV_8SC4);
            _grayMat = new Mat(_scalingMat.size(), CvType.CV_8SC1);

            _x = _scalingMat.cols() / 2f;
            _y = _scalingMat.rows() / 2f;

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            OpenCVDebug.SetDebugMode(true, true);
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _scalingMat?.Dispose(); _scalingMat = null;

            _grayMat?.Dispose(); _grayMat = null;

            _lastGrayMat?.Dispose(); _lastGrayMat = null;

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;

            OpenCVDebug.SetDebugMode(false, false);
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message);

            if (_fpsMonitor != null)
            {
                _fpsMonitor.ConsoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            _multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            _multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            _multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            _multiSource2MatHelper.RequestedIsFrontFacing = !_multiSource2MatHelper.RequestedIsFrontFacing;
        }

        /// <summary>
        /// Raises the warp mode dropdown value changed event.
        /// </summary>
        public void OnWarpModeDropdownValueChanged(int result)
        {
            if ((int)WarpMode != result)
            {
                WarpMode = (WarpModePreset)result;

                _lastWarpMatrix?.Dispose(); _lastWarpMatrix = null;
            }
        }

        // Private Methods
        /// <summary>
        /// Converts the screen point to texture point.
        /// </summary>
        /// <param name="screenPoint">Screen point.</param>
        /// <param name="dstPoint">Dst point.</param>
        /// <param name="textureQuad">Texture quad.</param>
        /// <param name="textureWidth">Texture width.</param>
        /// <param name="textureHeight">Texture height.</param>
        /// <param name="camera">Camera.</param>
        private void ConvertScreenPointToTexturePoint(Point screenPoint, Point dstPoint, GameObject textureQuad, int textureWidth = -1, int textureHeight = -1, Camera camera = null)
        {
            if (textureWidth < 0 || textureHeight < 0)
            {
                Renderer r = textureQuad.GetComponent<Renderer>();
                if (r != null && r.material != null && r.material.mainTexture != null)
                {
                    textureWidth = r.material.mainTexture.width;
                    textureHeight = r.material.mainTexture.height;
                }
                else
                {
                    textureWidth = (int)textureQuad.transform.localScale.x;
                    textureHeight = (int)textureQuad.transform.localScale.y;
                }
            }

            if (camera == null)
                camera = Camera.main;

            Vector3 quadPosition = textureQuad.transform.localPosition;
            Vector3 quadScale = textureQuad.transform.localScale;

            Vector2 tl = camera.WorldToScreenPoint(new Vector3(quadPosition.x - quadScale.x / 2, quadPosition.y + quadScale.y / 2, quadPosition.z));
            Vector2 tr = camera.WorldToScreenPoint(new Vector3(quadPosition.x + quadScale.x / 2, quadPosition.y + quadScale.y / 2, quadPosition.z));
            Vector2 br = camera.WorldToScreenPoint(new Vector3(quadPosition.x + quadScale.x / 2, quadPosition.y - quadScale.y / 2, quadPosition.z));
            Vector2 bl = camera.WorldToScreenPoint(new Vector3(quadPosition.x - quadScale.x / 2, quadPosition.y - quadScale.y / 2, quadPosition.z));

            using (Mat srcRectMat = new Mat(4, 1, CvType.CV_32FC2))
            using (Mat dstRectMat = new Mat(4, 1, CvType.CV_32FC2))
            {
                srcRectMat.put(0, 0, tl.x, tl.y, tr.x, tr.y, br.x, br.y, bl.x, bl.y);
                dstRectMat.put(0, 0, 0, 0, quadScale.x, 0, quadScale.x, quadScale.y, 0, quadScale.y);

                using (Mat perspectiveTransform = Imgproc.getPerspectiveTransform(srcRectMat, dstRectMat))
                using (MatOfPoint2f srcPointMat = new MatOfPoint2f(screenPoint))
                using (MatOfPoint2f dstPointMat = new MatOfPoint2f())
                {
                    Core.perspectiveTransform(srcPointMat, dstPointMat, perspectiveTransform);

                    dstPoint.x = dstPointMat.get(0, 0)[0] * textureWidth / quadScale.x;
                    dstPoint.y = dstPointMat.get(0, 0)[1] * textureHeight / quadScale.y;
                }
            }
        }

    }
}
