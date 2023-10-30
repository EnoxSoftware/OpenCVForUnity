using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.UnityUtils;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Physical Green Screen Example
    /// An example of creating a chromakey mask and compositing background image. (aka green-screen compositing) 
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class PhysicalGreenScreenExample : MonoBehaviour
    {
        /// <summary>
        /// The background image texture.
        /// </summary>
        public Texture2D backGroundImageTexture;

        /// <summary>
        /// The radius range sliders.
        /// </summary>
        public Slider hRadiusRangeSlider;
        public Slider sRadiusRangeSlider;
        public Slider vRadiusRangeSlider;

        /// <summary>
        /// The spectrum image UI.
        /// </summary>
        public RawImage spectrumImage;

        /// <summary>
        /// The hsv mat.
        /// </summary>
        Mat hsvMat;

        /// <summary>
        /// The chroma key mask mat.
        /// </summary>
        Mat chromaKeyMaskMat;

        /// <summary>
        /// The background image mat.
        /// </summary>
        Mat backGroundImageMat;

        // Lower and Upper bounds for range checking in HSV color space
        Scalar lowerBound = new Scalar(0);
        Scalar upperBound = new Scalar(0);

        // Color radius for range checking in HSV color space
        Scalar colorRadiusRange = new Scalar(25, 50, 50, 0);

        /// <summary>
        /// The BLOB color hsv.
        /// </summary>
        Scalar blobColorHsv = new Scalar(99, 255, 177, 255);

        /// <summary>
        /// The spectrum mat.
        /// </summary>
        Mat spectrumMat;

        /// <summary>
        /// The spectrum texture.
        /// </summary>
        Texture2D spectrumTexture;

        /// <summary>
        /// The stored touch point.
        /// </summary>
        Point storedTouchPoint;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        WebCamTextureToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();

#if UNITY_ANDROID && !UNITY_EDITOR
            // Avoids the front camera low light issue that occurs in only some Android devices (e.g. Google Pixel, Pixel2).
            webCamTextureToMatHelper.avoidAndroidFrontCameraLowLightIssue = true;
#endif
            webCamTextureToMatHelper.Initialize();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

            texture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(webCamTextureMat, texture);

            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            gameObject.transform.localScale = new Vector3(webCamTextureMat.cols(), webCamTextureMat.rows(), 1);

            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", webCamTextureMat.width().ToString());
                fpsMonitor.Add("height", webCamTextureMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());

                fpsMonitor.Add("blobColorHsv", "\n" + blobColorHsv.ToString());
                fpsMonitor.Add("colorRadiusRange", "\n" + colorRadiusRange.ToString());

                fpsMonitor.Toast("Touch the screen to specify the chromakey color.", 360);
            }


            float width = webCamTextureMat.width();
            float height = webCamTextureMat.height();

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


            hsvMat = new Mat((int)height, (int)width, CvType.CV_8UC3);
            chromaKeyMaskMat = new Mat(hsvMat.size(), CvType.CV_8UC1);
            backGroundImageMat = new Mat(hsvMat.size(), CvType.CV_8UC4, new Scalar(39, 255, 86, 255));

            if (backGroundImageTexture != null)
            {
                using (Mat bgMat = new Mat(backGroundImageTexture.height, backGroundImageTexture.width, CvType.CV_8UC4))
                {
                    Utils.texture2DToMat(backGroundImageTexture, bgMat);
                    Imgproc.resize(bgMat, backGroundImageMat, backGroundImageMat.size());
                }
            }

            spectrumMat = new Mat(100, 100, CvType.CV_8UC4, new Scalar(255, 255, 255, 255));
            spectrumTexture = new Texture2D(spectrumMat.cols(), spectrumMat.rows(), TextureFormat.RGBA32, false);

            // Set default chromakey color.
            blobColorHsv = new Scalar(99, 255, 177, 255); // = R:39 G:255 B:86 (Green screen)
            SetHsvColor(blobColorHsv);
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (hsvMat != null)
            {
                hsvMat.Dispose();
                hsvMat = null;
            }
            if (chromaKeyMaskMat != null)
            {
                chromaKeyMaskMat.Dispose();
                chromaKeyMaskMat = null;
            }
            if (backGroundImageMat != null)
            {
                backGroundImageMat.Dispose();
                backGroundImageMat = null;
            }
            if (spectrumMat != null)
            {
                spectrumMat.Dispose();
                spectrumMat = null;
            }
            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
            if (spectrumTexture != null)
            {
                Texture2D.Destroy(spectrumTexture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

        // Update is called once per frame
        void Update()
        {
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            //Touch
            int touchCount = Input.touchCount;
            if (touchCount == 1)
            {
                Touch t = Input.GetTouch (0);
                if(t.phase == TouchPhase.Ended && !EventSystem.current.IsPointerOverGameObject (t.fingerId)) {
                    storedTouchPoint = new Point (t.position.x, t.position.y);
                    //Debug.Log ("touch X " + t.position.x);
                    //Debug.Log ("touch Y " + t.position.y);
                }
            }
#else
            //Mouse
            if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                storedTouchPoint = new Point(Input.mousePosition.x, Input.mousePosition.y);
                //Debug.Log ("mouse X " + Input.mousePosition.x);
                //Debug.Log ("mouse Y " + Input.mousePosition.y);
            }
#endif

            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {
                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                if (storedTouchPoint != null)
                {
                    ConvertScreenPointToTexturePoint(storedTouchPoint, storedTouchPoint, gameObject, rgbaMat.cols(), rgbaMat.rows());
                    OnTouch(rgbaMat, storedTouchPoint);
                    storedTouchPoint = null;
                }

                // Convert the color space from RGBA to HSV_FULL.
                // HSV_FULL is HSV with H elements scaled from 0 to 255.
                Imgproc.cvtColor(rgbaMat, hsvMat, Imgproc.COLOR_RGB2HSV_FULL);

                // Create a chromakey mask from extracting the lower and upper limits range of values in the HSV color space.
                Core.inRange(hsvMat, lowerBound, upperBound, chromaKeyMaskMat);

                // Compose the background image.
                backGroundImageMat.copyTo(rgbaMat, chromaKeyMaskMat);

                Utils.matToTexture2D(rgbaMat, texture);
            }
        }

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
                blobColorHsv = Core.sumElems(touchedRegionHsv);
                int pointCount = touchedRect.width * touchedRect.height;
                for (int i = 0; i < blobColorHsv.val.Length; i++)
                    blobColorHsv.val[i] /= pointCount;

                SetHsvColor(blobColorHsv);
            }
        }

        public void SetHsvColor(Scalar hsvColor)
        {
            // Calculate lower and Upper bounds.
            double minH = (hsvColor.val[0] >= colorRadiusRange.val[0]) ? hsvColor.val[0] - colorRadiusRange.val[0] : 0;
            double maxH = (hsvColor.val[0] + colorRadiusRange.val[0] <= 255) ? hsvColor.val[0] + colorRadiusRange.val[0] : 255;

            lowerBound.val[0] = minH;
            upperBound.val[0] = maxH;

            lowerBound.val[1] = hsvColor.val[1] - colorRadiusRange.val[1];
            lowerBound.val[1] = (lowerBound.val[1] >= 0) ? lowerBound.val[1] : 0;
            upperBound.val[1] = hsvColor.val[1] + colorRadiusRange.val[1];
            upperBound.val[1] = (upperBound.val[1] <= 255) ? upperBound.val[1] : 255;

            lowerBound.val[2] = hsvColor.val[2] - colorRadiusRange.val[2];
            lowerBound.val[2] = (lowerBound.val[2] >= 0) ? lowerBound.val[2] : 0;
            upperBound.val[2] = hsvColor.val[2] + colorRadiusRange.val[2];
            upperBound.val[2] = (upperBound.val[2] <= 255) ? upperBound.val[2] : 255;

            lowerBound.val[3] = 0;
            upperBound.val[3] = 255;

            // Generate a spectrum chart.
            using (Mat spectrumHsv = new Mat((int)(upperBound.val[1] - lowerBound.val[1]), (int)(maxH - minH), CvType.CV_8UC3))
            using (Mat spectrumRgba = new Mat((int)(upperBound.val[1] - lowerBound.val[1]), (int)(maxH - minH), CvType.CV_8UC4))
            {
                for (int i = 0; i < upperBound.val[1] - lowerBound.val[1]; i++)
                {
                    for (int j = 0; j < maxH - minH; j++)
                    {
                        byte[] tmp = { (byte)(minH + j), (byte)(lowerBound.val[1] + i), (byte)hsvColor.val[2] };
                        spectrumHsv.put(i, j, tmp);
                    }
                }

                Imgproc.cvtColor(spectrumHsv, spectrumRgba, Imgproc.COLOR_HSV2RGB_FULL, 4);

                Imgproc.resize(spectrumRgba, spectrumMat, spectrumMat.size());
                Utils.matToTexture2D(spectrumMat, spectrumTexture);

                spectrumImage.texture = spectrumTexture;
            }

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("blobColorHsv", "\n" + blobColorHsv.ToString());
                fpsMonitor.Add("colorRadiusRange", "\n" + colorRadiusRange.ToString());
            }

            //Debug.Log("blobColorHsv: " + blobColorHsv);
            //Debug.Log("lowerBound: " + lowerBound);
            //Debug.Log("upperBound: " + upperBound);
            //Debug.Log("blobColorRgba: " + ConverScalarHsv2Rgba(blobColorHsv));
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

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            webCamTextureToMatHelper.Dispose();
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
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.requestedIsFrontFacing;
        }

        /// <summary>
        /// Raises the radius range slider value changed event.
        /// </summary>
        public void OnRadiusRangeSliderValueChanged()
        {
            colorRadiusRange = new Scalar(hRadiusRangeSlider.value, sRadiusRangeSlider.value, vRadiusRangeSlider.value, 255);

            SetHsvColor(blobColorHsv);
        }
    }
}