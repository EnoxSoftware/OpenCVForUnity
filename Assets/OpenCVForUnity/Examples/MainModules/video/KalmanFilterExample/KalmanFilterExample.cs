using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.VideoModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// KalmanFilter Example
    /// An example of tracking cursor position using the Video.KalmanFilter class.
    /// </summary>
    public class KalmanFilterExample : MonoBehaviour
    {
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

            // Set the Texture2D as the main texture of the RawImage
            ResultPreview.texture = _texture;
            ResultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)frameWidth / frameHeight;

            _kf = new KalmanFilter(4, 2, 0, CvType.CV_32FC1);

            // intialization of KF...
            Mat transitionMat = new Mat(4, 4, CvType.CV_32F);
            transitionMat.put(0, 0, new float[] { 1, 0, 1, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1 });
            _kf.set_transitionMatrix(transitionMat);

            _measurement = new Mat(2, 1, CvType.CV_32FC1);
            _measurement.setTo(Scalar.all(0));

            _cursorPos = new Point(_rgbaMat.cols() / 2f, _rgbaMat.rows() / 2f);

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

            // Reset TexturePointSelector state
            TexturePointSelector.ResetSelectionStatus();
        }

        private void Update()
        {
            // Check if touch is active and update cursor position
            var (gameObject, currentSelectionState, currentSelectionPoints) = TexturePointSelector.GetSelectionStatus();
            if (currentSelectionState == TextureSelector.TextureSelectionState.POINT_SELECTION_IN_PROGRESS)
            {
                var p = TextureSelector.ConvertSelectionPointsToUnityVector2(currentSelectionPoints);
                _cursorPos.x = p.x;
                _cursorPos.y = p.y;
            }

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

            Imgproc.putText(_rgbaMat, "Touch and drag to start Kalman filter tracking.", new Point(5, _rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

            // Draw current selection overlay
            // TexturePointSelector.DrawSelection(_rgbaMat, true);

            OpenCVMatUtils.MatToTexture2D(_rgbaMat, _texture, _colors);
        }

        private void OnDestroy()
        {
            _rgbaMat?.Dispose(); _rgbaMat = null;

            _measurement?.Dispose(); _measurement = null;

            _kf?.Dispose(); _kf = null;

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
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
