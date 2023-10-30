using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.VideoModule;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// TransformECC Example
    /// An example of background movement tracking using ECC algorithm.
    /// </summary>
    [RequireComponent(typeof(WebCamTextureToMatHelper))]
    public class TransformECCExample : MonoBehaviour
    {
        /// <summary>
        /// The warp mode dropdown.
        /// </summary>
        public Dropdown warpModeDropdown;

        /// <summary>
        /// parameter, specifying the type of motion.
        /// </summary>
        public WarpModePreset warpMode = (WarpModePreset)Video.MOTION_EUCLIDEAN;

        float x;
        float y;

        Mat scalingMat;
        Mat grayMat;
        Mat lastGrayMat;

        // downscaling ratio.
        const float scaling = 3f;

        // Specify the number of iterations.
        const int number_of_iterations = 50; // 5000;

        // Specify the threshold of the increment
        // in the correlation coefficient between two iterations
        const double termination_eps = 1e-7; // 1e-10;

        // Define termination criteria
        TermCriteria criteria = new TermCriteria(TermCriteria.EPS | TermCriteria.COUNT, number_of_iterations, termination_eps);

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
            webCamTextureToMatHelper.Initialize();

            // Update GUI state
            warpModeDropdown.value = (int)warpMode;

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "Touch the screen to lock the point.";
            }
        }

        /// <summary>
        /// Raises the webcam texture to mat helper initialized event.
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


            scalingMat = new Mat((int)(webCamTextureMat.rows() / scaling), (int)(webCamTextureMat.cols() / scaling), CvType.CV_8SC4);
            grayMat = new Mat(scalingMat.size(), CvType.CV_8SC1);

            x = scalingMat.cols() / 2f;
            y = scalingMat.rows() / 2f;

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);
        }

        /// <summary>
        /// Raises the webcam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            if (scalingMat != null)
            {
                scalingMat.Dispose();
                scalingMat = null;
            }

            if (grayMat != null)
            {
                grayMat.Dispose();
                grayMat = null;
            }

            if (lastGrayMat != null)
            {
                lastGrayMat.Dispose();
                lastGrayMat = null;
            }

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }


            Utils.setDebugMode(false, false);
        }

        /// <summary>
        /// Raises the webcam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "ErrorCode: " + errorCode;
            }
        }

        // Update is called once per frame
        void Update()
        {
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            //Touch
            int touchCount = Input.touchCount;
            if (touchCount == 1)
            {
                Touch t = Input.GetTouch(0);
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
                if (storedTouchPoint != null)
                {
                    ConvertScreenPointToTexturePoint(storedTouchPoint, storedTouchPoint, gameObject, texture.width, texture.height);
                    x = (float)storedTouchPoint.x / scaling;
                    y = (float)storedTouchPoint.y / scaling;
                    storedTouchPoint = null;
                }


                Mat rgbaMat = webCamTextureToMatHelper.GetMat();

                Imgproc.resize(rgbaMat, scalingMat, new Size(rgbaMat.width() / scaling, rgbaMat.height() / scaling));
                Imgproc.cvtColor(scalingMat, grayMat, Imgproc.COLOR_RGBA2GRAY);


                if (lastGrayMat == null)
                    lastGrayMat = grayMat.clone();

                // Define 2x3 or 3x3 matrices and initialize the matrix to identity
                Mat warp_matrix;
                if (warpMode == (WarpModePreset)Video.MOTION_HOMOGRAPHY)
                {
                    warp_matrix = Mat.eye(3, 3, CvType.CV_32F);
                }
                else
                {
                    warp_matrix = Mat.eye(2, 3, CvType.CV_32F);
                }

                // Run the ECC algorithm. The results are stored in warp_matrix.
                double ret = Video.findTransformECC(lastGrayMat, grayMat, warp_matrix, (int)warpMode, criteria);

                grayMat.copyTo(lastGrayMat);

                // Apply warp_matrix to x, y.
                Mat xyz = new Mat(3, 1, CvType.CV_32F);
                xyz.put(0, 0, x);
                xyz.put(1, 0, y);
                xyz.put(2, 0, 1.0);
                Mat result = warp_matrix.matMul(xyz);
                x = (float)result.get(0, 0)[0];
                y = (float)result.get(1, 0)[0];

                // Draw a box in the frame at x, y
                Imgproc.rectangle(rgbaMat, new Point((x * scaling) - 1, (y * scaling) - 1), new Point((x * scaling) + 1, (y * scaling) + 1), new Scalar(255, 0, 255, 255), 2);
                Imgproc.rectangle(rgbaMat, new Point((x * scaling) - 8, (y * scaling) - 8), new Point((x * scaling) + 8, (y * scaling) + 8), new Scalar(0, 255, 0, 255), 2);


                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Utils.matToTexture2D(rgbaMat, texture);
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
        /// Raises the warp mode dropdown value changed event.
        /// </summary>
        public void OnWarpModeDropdownValueChanged(int result)
        {
            if ((int)warpMode != result)
            {
                warpMode = (WarpModePreset)result;
            }
        }

        public enum WarpModePreset : int
        {
            TRANSLATION = 0,
            EUCLIDEAN,
            AFFINE,
            HOMOGRAPHY
        }
    }
}