using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.PhotoModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
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
        /// <summary>
        /// The thresh.
        /// </summary>
        [Range(0, 255)]
        public float thresh = 50.0f;

        /// <summary>
        /// The background raw image.
        /// </summary>
        public RawImage bgRawImage;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        MultiSource2MatHelper multiSource2MatHelper;

        /// <summary>
        /// The background mat.
        /// </summary>
        Mat bgMat;

        /// <summary>
        /// The foreground mask mat.
        /// </summary>
        Mat fgMaskMat;

        /// <summary>
        /// The background mask mat.
        /// </summary>
        Mat bgMaskMat;

        /// <summary>
        /// The green mat.
        /// </summary>
        Mat greenMat;

        /// <summary>
        /// The background texture.
        /// </summary>
        Texture2D bgTexture;

        /// <summary>
        /// The kernel for morphologyEx method.
        /// </summary>
        Mat kernel;

        bool isTouched;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        // Use this for initialization
        void Start()
        {
            fpsMonitor = GetComponent<FpsMonitor>();

            multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            multiSource2MatHelper.outputColorFormat = Source2MatHelperColorFormat.RGBA;

            multiSource2MatHelper.Initialize();

            kernel = Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(3, 3));
        }

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = multiSource2MatHelper.GetMat();

            texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(rgbaMat, texture);

            // Set the Texture2D as the main texture of the Renderer component attached to the game object
            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

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


            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", rgbaMat.width().ToString());
                fpsMonitor.Add("height", rgbaMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
                fpsMonitor.consoleText = "SPACE KEY or TOUCH SCREEN: Reset backgroud image.";
            }

            bgMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC4);
            fgMaskMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);
            bgMaskMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC1);
            greenMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC4, new Scalar(0, 255, 0, 255));

            bgTexture = new Texture2D(bgMat.cols(), bgMat.rows(), TextureFormat.RGBA32, false);
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (bgMat != null)
            {
                bgMat.Dispose();
                bgMat = null;
            }
            if (fgMaskMat != null)
            {
                fgMaskMat.Dispose();
                fgMaskMat = null;
            }
            if (bgMaskMat != null)
            {
                bgMaskMat.Dispose();
                bgMaskMat = null;
            }
            if (greenMat != null)
            {
                greenMat.Dispose();
                greenMat = null;
            }
            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode + ":" + message;
            }
        }

        // Update is called once per frame
        void Update()
        {

#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            //Touch
            if (Input.touchCount == 1) {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Ended && !EventSystem.current.IsPointerOverGameObject (t.fingerId)) {
                    isTouched = true;
                }
            }
#else
            //Mouse
            if (Input.GetKeyUp(KeyCode.Space))
            {
                isTouched = true;
            }
#endif

            if (multiSource2MatHelper.IsPlaying() && multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = multiSource2MatHelper.GetMat();

                if (isTouched)
                {
                    rgbaMat.copyTo(bgMat);

                    setBgTexture(bgMat);

                    isTouched = false;
                }

                //set fgMaskMat
                findFgMaskMat(rgbaMat, bgMat, thresh);

                //set bgMaskMat
                Core.bitwise_not(fgMaskMat, bgMaskMat);

                //copy greenMat using bgMaskMat
                greenMat.copyTo(rgbaMat, bgMaskMat);

                //Imgproc.putText (rgbaMat, "SPACE KEY or TOUCH SCREEN: Reset backgroud image.", new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.6, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Utils.matToTexture2D(rgbaMat, texture);
            }
        }

        /// <summary>
        /// Finds the foreground mask mat.
        /// </summary>
        /// <param name="fgMat">Fg mat.</param>
        /// <param name="bgMat">Background mat.</param>
        /// <param name="thresh">Thresh.</param>
        private void findFgMaskMat(Mat fgMat, Mat bgMat, float thresh = 13.0f)
        {
            Mat diff1 = new Mat();
            Core.absdiff(fgMat, bgMat, diff1);
            Mat diff2 = new Mat();
            Core.absdiff(bgMat, fgMat, diff2);
            Mat diff = diff1 + diff2;

            Imgproc.threshold(diff, diff, thresh, 0, Imgproc.THRESH_TOZERO);

            Imgproc.cvtColor(diff, fgMaskMat, Imgproc.COLOR_RGBA2GRAY);

            Imgproc.threshold(fgMaskMat, fgMaskMat, 10, 0, Imgproc.THRESH_TOZERO);

            Imgproc.threshold(fgMaskMat, fgMaskMat, 0, 255, Imgproc.THRESH_BINARY);

            // Small area removal from binary images by opening
            Imgproc.morphologyEx(fgMaskMat, fgMaskMat, Imgproc.MORPH_OPEN, kernel);

            diff1.Dispose();
            diff2.Dispose();
            diff.Dispose();
        }

        /// <summary>
        /// Sets the background texture.
        /// </summary>
        /// <param name="bgMat">Background mat.</param>
        private void setBgTexture(Mat bgMat)
        {
            Utils.matToTexture2D(bgMat, bgTexture);

            bgRawImage.texture = bgTexture;
            bgRawImage.rectTransform.localScale = new Vector3(1.0f, (float)bgMat.height() / (float)bgMat.width(), 1.0f);
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            multiSource2MatHelper.Dispose();
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
            multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            multiSource2MatHelper.requestedIsFrontFacing = !multiSource2MatHelper.requestedIsFrontFacing;
        }
    }
}