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
    /// This example demonstrates how to track specific points between consecutive frames in a video sequence using OpenCV’s findTransformECC method.
    /// By applying the calculated transformation matrix to each frame, the program ensures that designated points remain aligned,
    /// enabling smooth tracking across a series of frames. This technique is especially useful for stabilizing video or analyzing object motion in changing scenes.
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
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

        Mat last_Warp_matrix;

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
        /// The multi source to mat helper.
        /// </summary>
        MultiSource2MatHelper multiSource2MatHelper;

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

            // Update GUI state
            warpModeDropdown.value = (int)warpMode;

            if (fpsMonitor != null)
            {
                fpsMonitor.consoleText = "Touch the screen to lock the point.";
            }
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
            }

            scalingMat = new Mat((int)(rgbaMat.rows() / scaling), (int)(rgbaMat.cols() / scaling), CvType.CV_8SC4);
            grayMat = new Mat(scalingMat.size(), CvType.CV_8SC1);

            x = scalingMat.cols() / 2f;
            y = scalingMat.rows() / 2f;

            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true, true);
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

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

            if (multiSource2MatHelper.IsPlaying() && multiSource2MatHelper.DidUpdateThisFrame())
            {
                if (storedTouchPoint != null)
                {
                    ConvertScreenPointToTexturePoint(storedTouchPoint, storedTouchPoint, gameObject, texture.width, texture.height);
                    x = (float)storedTouchPoint.x / scaling;
                    y = (float)storedTouchPoint.y / scaling;
                    storedTouchPoint = null;
                }


                Mat rgbaMat = multiSource2MatHelper.GetMat();

                Imgproc.resize(rgbaMat, scalingMat, new Size(rgbaMat.width() / scaling, rgbaMat.height() / scaling));
                Imgproc.cvtColor(scalingMat, grayMat, Imgproc.COLOR_RGBA2GRAY);


                if (lastGrayMat == null)
                    lastGrayMat = grayMat.clone();

                if (last_Warp_matrix == null)
                {
                    // Define 2x3 or 3x3 matrices and initialize the matrix to identity
                    if (warpMode == (WarpModePreset)Video.MOTION_HOMOGRAPHY)
                    {
                        last_Warp_matrix = Mat.eye(3, 3, CvType.CV_32F);
                    }
                    else
                    {
                        last_Warp_matrix = Mat.eye(2, 3, CvType.CV_32F);
                    }
                }

                try
                {
                    // Run the ECC algorithm. The results are stored in warp_matrix.
                    double ret = Video.findTransformECC(lastGrayMat, grayMat, last_Warp_matrix, (int)warpMode, criteria);
                }
                catch (CvException e)
                {
                    Debug.LogWarning("Warning: An error occurred while analyzing frame motion; the transformation did not converge," +
                        " so this frame will be skipped. Error details: " + e);

                    if (last_Warp_matrix != null)
                    {
                        last_Warp_matrix.Dispose();
                        last_Warp_matrix = null;
                    }
                    grayMat.copyTo(lastGrayMat);
                    return;
                }

                grayMat.copyTo(lastGrayMat);

                // Apply warp_matrix to x, y.
                Mat xyz = new Mat(3, 1, CvType.CV_32F);
                xyz.put(0, 0, x);
                xyz.put(1, 0, y);
                xyz.put(2, 0, 1.0);
                Mat result = last_Warp_matrix.matMul(xyz);
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

        /// <summary>
        /// Raises the warp mode dropdown value changed event.
        /// </summary>
        public void OnWarpModeDropdownValueChanged(int result)
        {
            if ((int)warpMode != result)
            {
                warpMode = (WarpModePreset)result;

                if (last_Warp_matrix != null)
                {
                    last_Warp_matrix.Dispose();
                    last_Warp_matrix = null;
                }
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