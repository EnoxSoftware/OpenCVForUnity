using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.VideoModule;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif

namespace OpenCVForUnityExample
{
    /// <summary>
    /// KalmanFilter Example
    /// An example of tracking cursor position using the Video.KalmanFilter class.
    /// </summary>
    public class KalmanFilterExample : MonoBehaviour
    {
        // Private Fields
        /// <summary>
        /// The rgba mat.
        /// </summary>
        private Mat _rgbaMat;

        /// <summary>
        /// The colors.
        /// </summary>
        private Color32[] _colors;

        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The kalman filter.
        /// </summary>
        private KalmanFilter _kf;

        /// <summary>
        /// The cursor pos.
        /// </summary>
        private Point _cursorPos;

        /// <summary>
        /// The measurement.
        /// </summary>
        private Mat _measurement;

        /// <summary>
        /// The predicted trajectory points.
        /// </summary>
        private List<Point> _predictedTrajectoryPoints = new List<Point>();

        /// <summary>
        /// The cursor trajectory points.
        /// </summary>
        private List<Point> _cursorTrajectoryPoints = new List<Point>();

        /// <summary>
        /// The estimated trajectory points.
        /// </summary>
        private List<Point> _estimatedTrajectoryPoints = new List<Point>();

        // Unity Lifecycle Methods
        private void Start()
        {
            _rgbaMat = new Mat(480, 640, CvType.CV_8UC4);

            int frameWidth = _rgbaMat.cols();
            int frameHeight = _rgbaMat.rows();
            _colors = new Color32[frameWidth * frameHeight];
            _texture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGBA32, false);

            // Set the Texture2D as the main texture of the Renderer component attached to the game object
            gameObject.GetComponent<Renderer>().material.mainTexture = _texture;

            // Set the Texture2D as the main texture of the Renderer component attached to the game object
            gameObject.transform.localScale = new Vector3((float)frameWidth, (float)frameHeight, 1);

            // Adjust the scale of the game object to match the dimensions of the texture
            float widthScale = (float)Screen.width / (float)frameWidth;
            float heightScale = (float)Screen.height / (float)frameHeight;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = ((float)frameWidth * (float)Screen.height / (float)Screen.width) / 2;
            }
            else
            {
                Camera.main.orthographicSize = (float)frameHeight / 2;
            }

            _kf = new KalmanFilter(4, 2, 0, CvType.CV_32FC1);

            // intialization of KF...
            Mat transitionMat = new Mat(4, 4, CvType.CV_32F);
            transitionMat.put(0, 0, new float[] { 1, 0, 1, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1 });
            _kf.set_transitionMatrix(transitionMat);

            _measurement = new Mat(2, 1, CvType.CV_32FC1);
            _measurement.setTo(Scalar.all(0));

            _cursorPos = new Point();
            GetCursorPos(_cursorPos);

            // Set initial state estimate.
            Mat statePreMat = _kf.get_statePre();
            statePreMat.put(0, 0, new float[] { (float)_cursorPos.x, (float)_cursorPos.y, 0, 0 });
            Mat statePostMat = _kf.get_statePost();
            statePostMat.put(0, 0, new float[] { (float)_cursorPos.x, (float)_cursorPos.y, 0, 0 });

            Mat measurementMat = new Mat(2, 4, CvType.CV_32FC1);
            Core.setIdentity(measurementMat);
            _kf.set_measurementMatrix(measurementMat);

            Mat processNoiseCovMat = new Mat(4, 4, CvType.CV_32FC1);
            Core.setIdentity(processNoiseCovMat, Scalar.all(1e-4));
            _kf.set_processNoiseCov(processNoiseCovMat);

            Mat measurementNoiseCovMat = new Mat(2, 2, CvType.CV_32FC1);
            Core.setIdentity(measurementNoiseCovMat, Scalar.all(10));
            _kf.set_measurementNoiseCov(measurementNoiseCovMat);

            Mat errorCovPostMat = new Mat(4, 4, CvType.CV_32FC1);
            Core.setIdentity(errorCovPostMat, Scalar.all(.1));
            _kf.set_errorCovPost(errorCovPostMat);
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
            // fill all black.
            Imgproc.rectangle(_rgbaMat, new Point(0, 0), new Point(_rgbaMat.width(), _rgbaMat.height()), new Scalar(0, 0, 0, 255), -1);

            Point predictedPt;
            Point estimatedPt;

            // First predict, to update the internal statePre variable.
            using (Mat prediction = _kf.predict())
            {
                predictedPt = new Point(prediction.get(0, 0)[0], prediction.get(1, 0)[0]);
            }

            // Get cursor point.
            GetCursorPos(_cursorPos);
            // Noise addition (measurements/detections simulation )
            _cursorPos.x += UnityEngine.Random.Range(-2.0f, 2.0f);
            _cursorPos.y += UnityEngine.Random.Range(-2.0f, 2.0f);
            _measurement.put(0, 0, new float[] { (float)_cursorPos.x, (float)_cursorPos.y });
            Point measurementPt = new Point(_measurement.get(0, 0)[0], _measurement.get(1, 0)[0]);

            // The update phase.
            using (Mat estimated = _kf.correct(_measurement))
            {
                estimatedPt = new Point(estimated.get(0, 0)[0], estimated.get(1, 0)[0]);
            }

            _predictedTrajectoryPoints.Add(predictedPt);
            _cursorTrajectoryPoints.Add(measurementPt);
            _estimatedTrajectoryPoints.Add(estimatedPt);

            DrawCross(_rgbaMat, predictedPt, new Scalar(0, 0, 255, 255), 20);
            DrawCross(_rgbaMat, measurementPt, new Scalar(0, 255, 0, 255), 20);
            DrawCross(_rgbaMat, estimatedPt, new Scalar(255, 0, 0, 255), 20);

            for (int i = 0; i < _predictedTrajectoryPoints.Count - 1; i++)
            {
                Imgproc.line(_rgbaMat, _predictedTrajectoryPoints[i], _predictedTrajectoryPoints[i + 1], new Scalar(0, 255, 255, i), 1);
            }

            for (int i = 0; i < _cursorTrajectoryPoints.Count; i++)
            {
                Imgproc.circle(_rgbaMat, _cursorTrajectoryPoints[i], 1, new Scalar(0, 255, 0, i), -1);
            }

            for (int i = 0; i < _estimatedTrajectoryPoints.Count - 1; i++)
            {
                Imgproc.line(_rgbaMat, _estimatedTrajectoryPoints[i], _estimatedTrajectoryPoints[i + 1], new Scalar(255, 0, 0, i), 2);
            }

            if (_predictedTrajectoryPoints.Count > 255)
                _predictedTrajectoryPoints.RemoveAt(0);
            if (_cursorTrajectoryPoints.Count > 255)
                _cursorTrajectoryPoints.RemoveAt(0);
            if (_estimatedTrajectoryPoints.Count > 255)
                _estimatedTrajectoryPoints.RemoveAt(0);

            Imgproc.putText(_rgbaMat, "Kalman predicton", new Point(_rgbaMat.cols() - 170, 20), Imgproc.FONT_HERSHEY_SIMPLEX, 0.4, new Scalar(255, 255, 255, 255), 0, Imgproc.LINE_AA, false);
            Imgproc.putText(_rgbaMat, "measurement (cursor)", new Point(_rgbaMat.cols() - 170, 40), Imgproc.FONT_HERSHEY_SIMPLEX, 0.4, new Scalar(255, 255, 255, 255), 0, Imgproc.LINE_AA, false);
            Imgproc.putText(_rgbaMat, "Kalman correction", new Point(_rgbaMat.cols() - 170, 60), Imgproc.FONT_HERSHEY_SIMPLEX, 0.4, new Scalar(255, 255, 255, 255), 0, Imgproc.LINE_AA, false);
            DrawCross(_rgbaMat, new Point(_rgbaMat.cols() - 15, 15), new Scalar(0, 0, 255, 255), 20);
            DrawCross(_rgbaMat, new Point(_rgbaMat.cols() - 15, 35), new Scalar(0, 255, 0, 255), 20);
            DrawCross(_rgbaMat, new Point(_rgbaMat.cols() - 15, 55), new Scalar(255, 0, 0, 255), 20);

            Imgproc.putText(_rgbaMat, "Please move the cursor on the screen.", new Point(5, _rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

            OpenCVMatUtils.MatToTexture2D(_rgbaMat, _texture, _colors);
        }

        private void OnDestroy()
        {
            _rgbaMat?.Dispose();

            _measurement?.Dispose();

            _kf?.Dispose();
        }

        // Public Methods
        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        // Private Methods
        /// <summary>
        /// Gets cursor pos.
        /// </summary>
        /// <returns>The cursor point.</returns>
        private void GetCursorPos(Point pos)
        {
#if ENABLE_INPUT_SYSTEM
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            // Touch input for mobile platforms
            if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count >= 1)
            {
                var touch = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];
                ConvertScreenPointToTexturePoint(new Point(touch.screenPosition.x, touch.screenPosition.y), pos, gameObject, _rgbaMat.cols(), _rgbaMat.rows());
            }
#else
            // Mouse input for non-mobile platforms
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                ConvertScreenPointToTexturePoint(new Point(mouse.position.ReadValue().x, mouse.position.ReadValue().y), pos, gameObject, _rgbaMat.cols(), _rgbaMat.rows());
            }
#endif
#else
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            //Touch
            int touchCount = Input.touchCount;
            if (touchCount >= 1)
            {
                Touch t = Input.GetTouch(0);
                ConvertScreenPointToTexturePoint (new Point (t.position.x, t.position.y), pos, gameObject, _rgbaMat.cols(), _rgbaMat.rows());
            }
#else
            //Mouse
            ConvertScreenPointToTexturePoint(new Point(Input.mousePosition.x, Input.mousePosition.y), pos, gameObject, _rgbaMat.cols(), _rgbaMat.rows());
#endif
#endif
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
        /// Draws Cross.
        /// </summary>
        /// <param name="img">Img.</param>
        /// <param name="center">Center.</param>
        /// <param name="color">Color.</param>
        /// <param name="radius">Radius.</param>
        private void DrawCross(Mat img, Point center, Scalar color, int radius)
        {
            float d = Mathf.Sqrt(radius);
            Imgproc.line(img, new Point(center.x - d, center.y - d), new Point(center.x + d, center.y + d), color, 2);
            Imgproc.line(img, new Point(center.x + d, center.y - d), new Point(center.x - d, center.y + d), color, 2);
        }
    }
}
