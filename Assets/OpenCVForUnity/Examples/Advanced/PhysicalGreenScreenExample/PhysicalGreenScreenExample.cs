using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
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
    /// Physical Green Screen Example
    /// An example of creating a chromakey mask and compositing background image. (aka green-screen compositing)
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class PhysicalGreenScreenExample : MonoBehaviour
    {
        // Public Fields
        /// <summary>
        /// The background image texture.
        /// </summary>
        public Texture2D BackGroundImageTexture;

        /// <summary>
        /// The radius range sliders.
        /// </summary>
        public Slider HRadiusRangeSlider;
        public Slider SRadiusRangeSlider;
        public Slider VRadiusRangeSlider;

        /// <summary>
        /// The spectrum image UI.
        /// </summary>
        public RawImage SpectrumImage;

        // Private Fields
        /// <summary>
        /// The hsv mat.
        /// </summary>
        private Mat _hsvMat;

        /// <summary>
        /// The chroma key mask mat.
        /// </summary>
        private Mat _chromaKeyMaskMat;

        /// <summary>
        /// The background image mat.
        /// </summary>
        private Mat _backGroundImageMat;

        // Lower and Upper bounds for range checking in HSV color space
        private Scalar _lowerBound = new Scalar(0);
        private Scalar _upperBound = new Scalar(0);

        // Color radius for range checking in HSV color space
        private Scalar _colorRadiusRange = new Scalar(25, 50, 50, 0);

        /// <summary>
        /// The BLOB color hsv.
        /// </summary>
        private Scalar _blobColorHsv = new Scalar(99, 255, 177, 255);

        /// <summary>
        /// The spectrum mat.
        /// </summary>
        private Mat _spectrumMat;

        /// <summary>
        /// The spectrum texture.
        /// </summary>
        private Texture2D _spectrumTexture;

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
                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_storedTouchPoint != null)
                {
                    ConvertScreenPointToTexturePoint(_storedTouchPoint, _storedTouchPoint, gameObject, rgbaMat.cols(), rgbaMat.rows());
                    OnTouch(rgbaMat, _storedTouchPoint);
                    _storedTouchPoint = null;
                }

                // Convert the color space from RGBA to HSV_FULL.
                // HSV_FULL is HSV with H elements scaled from 0 to 255.
                Imgproc.cvtColor(rgbaMat, _hsvMat, Imgproc.COLOR_RGB2HSV_FULL);

                // Create a chromakey mask from extracting the lower and upper limits range of values in the HSV color space.
                Core.inRange(_hsvMat, _lowerBound, _upperBound, _chromaKeyMaskMat);

                // Compose the background image.
                _backGroundImageMat.copyTo(rgbaMat, _chromaKeyMaskMat);

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

                _fpsMonitor.Add("blobColorHsv", "\n" + _blobColorHsv.ToString());
                _fpsMonitor.Add("colorRadiusRange", "\n" + _colorRadiusRange.ToString());

                _fpsMonitor.Toast("Touch the screen to specify the chromakey color.", 2000);
            }

            _hsvMat = new Mat((int)height, (int)width, CvType.CV_8UC3);
            _chromaKeyMaskMat = new Mat(_hsvMat.size(), CvType.CV_8UC1);
            _backGroundImageMat = new Mat(_hsvMat.size(), CvType.CV_8UC4, new Scalar(39, 255, 86, 255));

            if (BackGroundImageTexture != null)
            {
                using (Mat bgMat = new Mat(BackGroundImageTexture.height, BackGroundImageTexture.width, CvType.CV_8UC4))
                {
                    OpenCVMatUtils.Texture2DToMat(BackGroundImageTexture, bgMat);
                    Imgproc.resize(_backGroundImageMat, _backGroundImageMat, _backGroundImageMat.size());
                }
            }

            _spectrumMat = new Mat(100, 100, CvType.CV_8UC4, new Scalar(255, 255, 255, 255));
            _spectrumTexture = new Texture2D(_spectrumMat.cols(), _spectrumMat.rows(), TextureFormat.RGBA32, false);

            // Set default chromakey color.
            _blobColorHsv = new Scalar(99, 255, 177, 255); // = R:39 G:255 B:86 (Green screen)
            SetHsvColor(_blobColorHsv);
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _hsvMat?.Dispose(); _hsvMat = null;
            _chromaKeyMaskMat?.Dispose(); _chromaKeyMaskMat = null;
            _backGroundImageMat?.Dispose(); _backGroundImageMat = null;
            _spectrumMat?.Dispose(); _spectrumMat = null;
            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
            if (_spectrumTexture != null) Texture2D.Destroy(_spectrumTexture); _spectrumTexture = null;
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
        /// Sets the HSV color.
        /// </summary>
        /// <param name="hsvColor">The HSV color.</param>
        public void SetHsvColor(Scalar hsvColor)
        {
            // Calculate lower and Upper bounds.
            double minH = (hsvColor.val[0] >= _colorRadiusRange.val[0]) ? hsvColor.val[0] - _colorRadiusRange.val[0] : 0;
            double maxH = (hsvColor.val[0] + _colorRadiusRange.val[0] <= 255) ? hsvColor.val[0] + _colorRadiusRange.val[0] : 255;

            _lowerBound.val[0] = minH;
            _upperBound.val[0] = maxH;

            _lowerBound.val[1] = hsvColor.val[1] - _colorRadiusRange.val[1];
            _lowerBound.val[1] = (_lowerBound.val[1] >= 0) ? _lowerBound.val[1] : 0;
            _upperBound.val[1] = hsvColor.val[1] + _colorRadiusRange.val[1];
            _upperBound.val[1] = (_upperBound.val[1] <= 255) ? _upperBound.val[1] : 255;

            _lowerBound.val[2] = hsvColor.val[2] - _colorRadiusRange.val[2];
            _lowerBound.val[2] = (_lowerBound.val[2] >= 0) ? _lowerBound.val[2] : 0;
            _upperBound.val[2] = hsvColor.val[2] + _colorRadiusRange.val[2];
            _upperBound.val[2] = (_upperBound.val[2] <= 255) ? _upperBound.val[2] : 255;

            _lowerBound.val[3] = 0;
            _upperBound.val[3] = 255;

            // Generate a spectrum chart.
            using (Mat spectrumHsv = new Mat((int)(_upperBound.val[1] - _lowerBound.val[1]), (int)(maxH - minH), CvType.CV_8UC3))
            using (Mat spectrumRgba = new Mat((int)(_upperBound.val[1] - _lowerBound.val[1]), (int)(maxH - minH), CvType.CV_8UC4))
            {
                for (int i = 0; i < _upperBound.val[1] - _lowerBound.val[1]; i++)
                {
                    for (int j = 0; j < maxH - minH; j++)
                    {
                        byte[] tmp = { (byte)(minH + j), (byte)(_lowerBound.val[1] + i), (byte)hsvColor.val[2] };
                        spectrumHsv.put(i, j, tmp);
                    }
                }

                Imgproc.cvtColor(spectrumHsv, spectrumRgba, Imgproc.COLOR_HSV2RGB_FULL, 4);

                Imgproc.resize(spectrumRgba, _spectrumMat, _spectrumMat.size());
                OpenCVMatUtils.MatToTexture2D(_spectrumMat, _spectrumTexture);

                SpectrumImage.texture = _spectrumTexture;
            }

            if (_fpsMonitor != null)
            {
                _fpsMonitor.Add("blobColorHsv", "\n" + _blobColorHsv.ToString());
                _fpsMonitor.Add("colorRadiusRange", "\n" + _colorRadiusRange.ToString());
            }

            //Debug.Log("blobColorHsv: " + _blobColorHsv);
            //Debug.Log("lowerBound: " + _lowerBound);
            //Debug.Log("upperBound: " + _upperBound);
            //Debug.Log("blobColorRgba: " + ConverScalarHsv2Rgba(_blobColorHsv));
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
        /// Raises the radius range slider value changed event.
        /// </summary>
        public void OnRadiusRangeSliderValueChanged()
        {
            _colorRadiusRange = new Scalar(HRadiusRangeSlider.value, SRadiusRangeSlider.value, VRadiusRangeSlider.value, 255);

            SetHsvColor(_blobColorHsv);
        }

        // Private Methods
        private void OnTouch(Mat img, Point touchPoint)
        {
            int cols = img.cols();
            int rows = img.rows();

            int x = (int)touchPoint.x;
            int y = (int)touchPoint.y;

            //Debug.Log ("Touch image coordinates: (" + x + ", " + y + ")");

            if ((x < 0) || (y < 0) || (x > cols) || (y > rows))
                return;

            OpenCVForUnity.CoreModule.Rect touchedRect = new OpenCVForUnity.CoreModule.Rect();

            touchedRect.x = (x > 5) ? x - 5 : 0;
            touchedRect.y = (y > 5) ? y - 5 : 0;

            touchedRect.width = (x + 5 < cols) ? x + 5 - touchedRect.x : cols - touchedRect.x;
            touchedRect.height = (y + 5 < rows) ? y + 5 - touchedRect.y : rows - touchedRect.y;

            using (Mat touchedRegionRgba = img.submat(touchedRect))
            using (Mat touchedRegionHsv = new Mat())
            {
                Imgproc.cvtColor(touchedRegionRgba, touchedRegionHsv, Imgproc.COLOR_RGB2HSV_FULL);

                // Calculate average color of touched region.
                _blobColorHsv = Core.sumElems(touchedRegionHsv);
                int pointCount = touchedRect.width * touchedRect.height;
                for (int i = 0; i < _blobColorHsv.val.Length; i++)
                    _blobColorHsv.val[i] /= pointCount;

                SetHsvColor(_blobColorHsv);
            }
        }

        /// <summary>
        /// Converts the screen point to texture point.
        /// </summary>
        /// <param name="screenPoint">Screen point.</param>
        /// <param name="dstPoint">Dst point.</param>
        /// <param name="texturQuad">Texture quad.</param>
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

        private Scalar ConverScalarHsv2Rgba(Scalar hsvColor)
        {
            Scalar rgbaColor;
            using (Mat pointMatRgba = new Mat())
            using (Mat pointMatHsv = new Mat(1, 1, CvType.CV_8UC3, hsvColor))
            {
                Imgproc.cvtColor(pointMatHsv, pointMatRgba, Imgproc.COLOR_HSV2RGB_FULL, 4);
                rgbaColor = new Scalar(pointMatRgba.get(0, 0));
            }

            return rgbaColor;
        }
    }
}
