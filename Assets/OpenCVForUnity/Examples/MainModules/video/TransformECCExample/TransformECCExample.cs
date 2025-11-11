using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using OpenCVForUnity.VideoModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        [Header("UI")]

        /// <summary>
        /// The texture selector (point selection).
        /// </summary>
        public TextureSelector TexturePointSelector;

        /// <summary>
        /// The warp mode dropdown.
        /// </summary>
        public Dropdown WarpModeDropdown;

        [Space(10)]

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
        /// The flag to request point update from selected point.
        /// </summary>
        private bool _shouldUpdatePointFromSelection = false;

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

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {
                if (_shouldUpdatePointFromSelection)
                {
                    var (gameObject, currentSelectionState, currentSelectionPoints) = TexturePointSelector.GetSelectionStatus();
                    var p = TextureSelector.ConvertSelectionPointsToOpenCVPoint(currentSelectionPoints);
                    _x = (float)p.x / SCALING;
                    _y = (float)p.y / SCALING;

                    TexturePointSelector.ResetSelectionStatus();

                    _shouldUpdatePointFromSelection = false;
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

                // Draw current selection overlay
                TexturePointSelector.DrawSelection(rgbaMat, true);

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

            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)_texture.width / _texture.height;

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

            // Reset TexturePointSelector state
            if (TexturePointSelector != null)
            {
                TexturePointSelector.ResetSelectionStatus();
            }

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

        /// <summary>
        /// Handles the texture selection state changed event from TextureSelector.
        /// This should be wired in the Inspector to TextureSelector.OnTextureSelectionStateChanged.
        /// </summary>
        /// <param name="touchedObject">The GameObject that was touched.</param>
        /// <param name="touchState">The touch state.</param>
        /// <param name="texturePoints">The texture coordinates array (OpenCV format: top-left origin).</param>
        public void OnTextureSelectionStateChanged(GameObject touchedObject, TextureSelector.TextureSelectionState touchState, Vector2[] texturePoints)
        {
            switch (touchState)
            {
                case TextureSelector.TextureSelectionState.POINT_SELECTION_COMPLETED:
                    _shouldUpdatePointFromSelection = true;
                    break;
            }
        }

        // Private Methods

    }
}
