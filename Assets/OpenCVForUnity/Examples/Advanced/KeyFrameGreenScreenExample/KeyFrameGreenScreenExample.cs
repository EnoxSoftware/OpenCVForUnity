using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Key Frame Green Screen Example
    /// Greenscreen effect without a physical green screen, via OpenCV.
    /// This performs background subtraction, and sets the background to "green" for use with "key frame" video editing software.
    /// Referring to https://gist.github.com/drscotthawley/2d6bbffce9dda5f3057b4879c3bd4422.
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class KeyFrameGreenScreenExample : MonoBehaviour
    {
        // Public Fields
        /// <summary>
        /// The thresh.
        /// </summary>
        [Range(0, 255)]
        public float Thresh = 50.0f;

        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        /// <summary>
        /// The background raw image.
        /// </summary>
        public RawImage BgRawImage;

        /// <summary>
        /// The texture selector (point selection).
        /// </summary>
        public TextureSelector TexturePointSelector;

        // Private Fields
        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        private MultiSource2MatHelper _multiSource2MatHelper;

        /// <summary>
        /// The background mat.
        /// </summary>
        private Mat _bgMat;

        /// <summary>
        /// The foreground mask mat.
        /// </summary>
        private Mat _fgMaskMat;

        /// <summary>
        /// The background mask mat.
        /// </summary>
        private Mat _bgMaskMat;

        /// <summary>
        /// The green mat.
        /// </summary>
        private Mat _greenMat;

        /// <summary>
        /// The background texture.
        /// </summary>
        private Texture2D _bgTexture;

        /// <summary>
        /// The kernel for morphologyEx method.
        /// </summary>
        private Mat _kernel;


        /// <summary>
        /// The FPS monitor.
        /// </summary>
        private FpsMonitor _fpsMonitor;

        /// <summary>
        /// The flag to request background update from selected point.
        /// </summary>
        private bool _shouldUpdateBackgroundFromPoint = false;

        // Unity Lifecycle Methods
        private void Start()
        {
            _fpsMonitor = GetComponent<FpsMonitor>();

            _multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            _multiSource2MatHelper.OutputColorFormat = Source2MatHelperColorFormat.RGBA;

            _multiSource2MatHelper.Initialize();

            _kernel = Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(3, 3));
        }


        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_shouldUpdateBackgroundFromPoint)
                {
                    SetBackgroundFromCurrentFrame(rgbaMat);
                    _shouldUpdateBackgroundFromPoint = false;
                }

                //set fgMaskMat
                FindFgMaskMat(rgbaMat, _bgMat, Thresh);

                //set bgMaskMat
                Core.bitwise_not(_fgMaskMat, _bgMaskMat);

                //copy greenMat using bgMaskMat
                _greenMat.copyTo(rgbaMat, _bgMaskMat);

                // Draw current selection overlay
                // TexturePointSelector.DrawSelection(rgbaMat, true, true);

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
                _fpsMonitor.Toast("Touch the screen to set background image.", 2000);
            }

            _bgMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC4);
            _fgMaskMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);
            _bgMaskMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);
            _greenMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC4, new Scalar(0, 255, 0, 255));

            _bgTexture = new Texture2D(_bgMat.cols(), _bgMat.rows(), TextureFormat.RGBA32, false);

            // Initialize background to white
            ResetBackgroundToWhite();

            // Reset TexturePointSelector state
            TexturePointSelector.ResetSelectionStatus();
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _bgMat?.Dispose(); _bgMat = null;
            _fgMaskMat?.Dispose(); _fgMaskMat = null;
            _bgMaskMat?.Dispose(); _bgMaskMat = null;
            _greenMat?.Dispose(); _greenMat = null;
            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
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
                case TextureSelector.TextureSelectionState.POINT_SELECTION_STARTED:
                    _shouldUpdateBackgroundFromPoint = true;
                    break;
                case TextureSelector.TextureSelectionState.OUTSIDE_TEXTURE_SELECTED:
                    ResetBackgroundToWhite();
                    break;
            }
        }

        // Private Methods
        /// <summary>
        /// Finds the foreground mask mat.
        /// </summary>
        /// <param name="fgMat">Fg mat.</param>
        /// <param name="bgMat">Background mat.</param>
        /// <param name="thresh">Thresh.</param>
        private void FindFgMaskMat(Mat fgMat, Mat bgMat, float thresh = 13.0f)
        {
            Mat diff1 = new Mat();
            Core.absdiff(fgMat, bgMat, diff1);
            Mat diff2 = new Mat();
            Core.absdiff(bgMat, fgMat, diff2);
            Mat diff = new Mat();
            Core.add(diff1, diff2, diff);

            Imgproc.threshold(diff, diff, thresh, 0, Imgproc.THRESH_TOZERO);

            Imgproc.cvtColor(diff, _fgMaskMat, Imgproc.COLOR_RGBA2GRAY);

            Imgproc.threshold(_fgMaskMat, _fgMaskMat, 10, 0, Imgproc.THRESH_TOZERO);

            Imgproc.threshold(_fgMaskMat, _fgMaskMat, 0, 255, Imgproc.THRESH_BINARY);

            // Small area removal from binary images by opening
            Imgproc.morphologyEx(_fgMaskMat, _fgMaskMat, Imgproc.MORPH_OPEN, _kernel);

            diff1.Dispose();
            diff2.Dispose();
            diff.Dispose();
        }

        /// <summary>
        /// Sets the background from current frame.
        /// </summary>
        /// <param name="img">The image mat.</param>
        private void SetBackgroundFromCurrentFrame(Mat img)
        {
            img.copyTo(_bgMat);
            SetBgTexture(_bgMat);
        }

        /// <summary>
        /// Resets the background to white.
        /// </summary>
        private void ResetBackgroundToWhite()
        {
            _bgMat.setTo(new Scalar(255, 255, 255, 255));
            SetBgTexture(_bgMat);
        }

        /// <summary>
        /// Sets the background texture.
        /// </summary>
        /// <param name="bgMat">Background mat.</param>
        private void SetBgTexture(Mat bgMat)
        {
            OpenCVMatUtils.MatToTexture2D(bgMat, _bgTexture);

            BgRawImage.texture = _bgTexture;
            BgRawImage.rectTransform.localScale = new Vector3(1.0f, (float)bgMat.height() / (float)bgMat.width(), 1.0f);
        }
    }
}
