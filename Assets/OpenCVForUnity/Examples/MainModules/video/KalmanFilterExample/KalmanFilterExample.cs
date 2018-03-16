using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// KalmanFilter Example
    /// An example of tracking cursor position using the Video.KalmanFilter class.
    /// </summary>
    public class KalmanFilterExample : MonoBehaviour
    {
        /// <summary>
        /// The rgba mat.
        /// </summary>
        Mat rgbaMat;

        /// <summary>
        /// The colors.
        /// </summary>
        Color32[] colors;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The kalman filter.
        /// </summary>
        KalmanFilter KF;

        /// <summary>
        /// The cursor pos.
        /// </summary>
        Point cursorPos;

        /// <summary>
        /// The measurement.
        /// </summary>
        Mat measurement;

        /// <summary>
        /// The predicted trajectory points.
        /// </summary>
        List<Point> predictedTrajectoryPoints = new List<Point>();

        /// <summary>
        /// The cursor trajectory points.
        /// </summary>
        List<Point> cursorTrajectoryPoints = new List<Point>();

        /// <summary>
        /// The estimated trajectory points.
        /// </summary>
        List<Point> estimatedTrajectoryPoints = new List<Point>();

        // Use this for initialization
        void Start ()
        {
            rgbaMat = new Mat (480, 640, CvType.CV_8UC4);

            int frameWidth = rgbaMat.cols ();
            int frameHeight = rgbaMat.rows ();
            colors = new Color32[frameWidth * frameHeight];
            texture = new Texture2D (frameWidth, frameHeight, TextureFormat.RGBA32, false);
            gameObject.transform.localScale = new Vector3 ((float)frameWidth, (float)frameHeight, 1);
            float widthScale = (float)Screen.width / (float)frameWidth;
            float heightScale = (float)Screen.height / (float)frameHeight;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = ((float)frameWidth * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = (float)frameHeight / 2;
            }

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;


            KF = new KalmanFilter (4, 2, 0, CvType.CV_32FC1);

            // intialization of KF...
            Mat transitionMat = new Mat (4, 4, CvType.CV_32F);
            transitionMat.put (0, 0, new float[] {1,0,1,0,   0,1,0,1,  0,0,1,0,  0,0,0,1});
            KF.set_transitionMatrix (transitionMat);

            measurement = new Mat (2, 1, CvType.CV_32FC1); measurement.setTo (Scalar.all(0));

            Mat statePreMat = new Mat (1, 4, CvType.CV_32FC1);
            cursorPos = new Point ();
            GetCursorPos(cursorPos);
            statePreMat.put (0, 0, new float[] {(float)cursorPos.x,(float)cursorPos.y,0,0});
            KF.set_statePre (statePreMat);

            Mat measurementMat = new Mat (2, 4, CvType.CV_32FC1);
            Core.setIdentity (measurementMat);
            KF.set_measurementMatrix (measurementMat);

            Mat processNoiseCovMat = new Mat (4, 4, CvType.CV_32FC1);
            Core.setIdentity (processNoiseCovMat, Scalar.all(1e-4));
            KF.set_processNoiseCov (processNoiseCovMat);

            Mat measurementNoiseCovMat = new Mat (2, 2, CvType.CV_32FC1);
            Core.setIdentity (measurementNoiseCovMat, Scalar.all(10));
            KF.set_measurementNoiseCov (measurementNoiseCovMat);

            Mat errorCovPostMat = new Mat (4, 4, CvType.CV_32FC1);
            Core.setIdentity (errorCovPostMat, Scalar.all(.1));
            KF.set_errorCovPost (errorCovPostMat);
        }

        // Update is called once per frame
        void Update ()
        {
            // fill all black.
            Imgproc.rectangle (rgbaMat, new Point (0, 0), new Point (rgbaMat.width (), rgbaMat.height ()), new Scalar (0, 0, 0, 255), -1);

            Point predictedPt;
            Point estimatedPt;

            // First predict, to update the internal statePre variable.
            using (Mat prediction = KF.predict ()) {
                predictedPt = new Point (prediction.get (0, 0) [0], prediction.get (1, 0) [0]);
            }

            // Get cursor point.
            GetCursorPos(cursorPos);
            // Noise addition (measurements/detections simulation )
            cursorPos.x += UnityEngine.Random.Range(-2.0f, 2.0f);
            cursorPos.y += UnityEngine.Random.Range(-2.0f, 2.0f);
            measurement.put (0, 0, new float[] {(float)cursorPos.x,(float)cursorPos.y});
            Point measurementPt = new Point(measurement.get (0, 0)[0], measurement.get (1, 0)[0]);

            // The update phase.
            using (Mat estimated = KF.correct (measurement)) {
                estimatedPt = new Point (estimated.get (0, 0) [0], estimated.get (1, 0) [0]);
            }

            predictedTrajectoryPoints.Add (predictedPt);
            cursorTrajectoryPoints.Add (measurementPt);
            estimatedTrajectoryPoints.Add (estimatedPt);

            DrawCross(rgbaMat, predictedPt, new Scalar(0,0,255,255), 20 );
            DrawCross(rgbaMat, measurementPt, new Scalar(0,255,0,255), 20 );
            DrawCross(rgbaMat, estimatedPt, new Scalar(255,0,0,255), 20 );

            for (int i = 0; i < predictedTrajectoryPoints.Count-1; i++) {
                Imgproc.line(rgbaMat, predictedTrajectoryPoints[i], predictedTrajectoryPoints[i+1], new Scalar(0,255,255,i), 1);
            }

            for (int i = 0; i < cursorTrajectoryPoints.Count; i++) {
                Imgproc.circle(rgbaMat, cursorTrajectoryPoints[i], 1, new Scalar(0,255,0,i), -1);
            }

            for (int i = 0; i < estimatedTrajectoryPoints.Count-1; i++) {
                Imgproc.line(rgbaMat, estimatedTrajectoryPoints[i], estimatedTrajectoryPoints[i+1], new Scalar(255,0,0,i), 2);
            }

            if (predictedTrajectoryPoints.Count > 255) predictedTrajectoryPoints.RemoveAt (0);
            if (cursorTrajectoryPoints.Count > 255) cursorTrajectoryPoints.RemoveAt (0);
            if (estimatedTrajectoryPoints.Count > 255) estimatedTrajectoryPoints.RemoveAt (0);

            Imgproc.putText (rgbaMat, "Kalman predicton", new Point (rgbaMat.cols() - 170, 20), Core.FONT_HERSHEY_SIMPLEX, 0.4, new Scalar (255, 255, 255, 255), 0, Imgproc.LINE_AA, false);
            Imgproc.putText (rgbaMat, "measurement (cursor)", new Point (rgbaMat.cols() - 170, 40), Core.FONT_HERSHEY_SIMPLEX, 0.4, new Scalar (255, 255, 255, 255), 0, Imgproc.LINE_AA, false);
            Imgproc.putText (rgbaMat, "Kalman correction", new Point (rgbaMat.cols() - 170, 60), Core.FONT_HERSHEY_SIMPLEX, 0.4, new Scalar (255, 255, 255, 255), 0, Imgproc.LINE_AA, false);
            DrawCross(rgbaMat, new Point(rgbaMat.cols() - 15, 15), new Scalar(0,0,255,255), 20 );
            DrawCross(rgbaMat, new Point(rgbaMat.cols() - 15, 35), new Scalar(0,255,0,255), 20 );
            DrawCross(rgbaMat, new Point(rgbaMat.cols() - 15, 55), new Scalar(255,0,0,255), 20 );

            Imgproc.putText (rgbaMat, "Please move the cursor on the screen.", new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 0.8, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

            Utils.matToTexture2D (rgbaMat, texture, colors);
        }

        /// <summary>
        /// Gets cursor pos.
        /// </summary>
        /// <returns>The cursor point.</returns>
        private void GetCursorPos (Point pos)
        {
            #if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
            //Touch
            int touchCount = Input.touchCount;
            if (touchCount >= 1)
            {
                Touch t = Input.GetTouch(0);
                ConvertScreenPointToTexturePoint (new Point (t.position.x, t.position.y), pos, gameObject, rgbaMat.cols(), rgbaMat.rows());
            }
            #else
            //Mouse
            ConvertScreenPointToTexturePoint (new Point (Input.mousePosition.x, Input.mousePosition.y), pos, gameObject, rgbaMat.cols(), rgbaMat.rows());
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
        private void ConvertScreenPointToTexturePoint (Point screenPoint, Point dstPoint, GameObject textureQuad, int textureWidth = -1, int textureHeight = -1, Camera camera = null)
        {
            if (textureWidth < 0 || textureHeight < 0) {
                Renderer r = textureQuad.GetComponent<Renderer> ();
                if (r != null && r.material != null && r.material.mainTexture != null) {
                    textureWidth = r.material.mainTexture.width;
                    textureHeight = r.material.mainTexture.height;
                } else {
                    textureWidth = (int)textureQuad.transform.localScale.x;
                    textureHeight = (int)textureQuad.transform.localScale.y;
                }
            }

            if (camera == null)
                camera = Camera.main;

            Vector3 quadPosition = textureQuad.transform.localPosition;
            Vector3 quadScale = textureQuad.transform.localScale;

            Vector2 tl = camera.WorldToScreenPoint (new Vector3 (quadPosition.x - quadScale.x / 2, quadPosition.y + quadScale.y / 2, quadPosition.z));
            Vector2 tr = camera.WorldToScreenPoint (new Vector3 (quadPosition.x + quadScale.x / 2, quadPosition.y + quadScale.y / 2, quadPosition.z));
            Vector2 br = camera.WorldToScreenPoint (new Vector3 (quadPosition.x + quadScale.x / 2, quadPosition.y - quadScale.y / 2, quadPosition.z));
            Vector2 bl = camera.WorldToScreenPoint (new Vector3 (quadPosition.x - quadScale.x / 2, quadPosition.y - quadScale.y / 2, quadPosition.z));                       

            using(Mat srcRectMat = new Mat (4, 1, CvType.CV_32FC2))
            using(Mat dstRectMat = new Mat (4, 1, CvType.CV_32FC2)) {
                srcRectMat.put (0, 0, tl.x, tl.y, tr.x, tr.y, br.x, br.y, bl.x, bl.y);
                dstRectMat.put (0, 0, 0, 0, quadScale.x, 0, quadScale.x, quadScale.y, 0, quadScale.y);            

                using(Mat perspectiveTransform = Imgproc.getPerspectiveTransform (srcRectMat, dstRectMat))
                using(MatOfPoint2f srcPointMat = new MatOfPoint2f (screenPoint))
                using(MatOfPoint2f dstPointMat = new MatOfPoint2f ()) {
                    Core.perspectiveTransform (srcPointMat, dstPointMat, perspectiveTransform);

                    dstPoint.x = dstPointMat.get(0,0)[0] * textureWidth / quadScale.x;
                    dstPoint.y = dstPointMat.get(0,0)[1] * textureHeight / quadScale.y;
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
        private void DrawCross (Mat img, Point center, Scalar color, int radius)
        {
            float d = Mathf.Sqrt (radius);
            Imgproc.line(img, new Point( center.x - d, center.y - d ), new Point( center.x + d, center.y + d ), color, 2);
            Imgproc.line(img, new Point( center.x + d, center.y - d ), new Point( center.x - d, center.y + d ), color, 2);
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            if (rgbaMat != null)
                rgbaMat.Dispose ();

            if (measurement != null)
                measurement.Dispose ();

            KF.Dispose ();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("OpenCVForUnityExample");
            #else
            Application.LoadLevel ("OpenCVForUnityExample");
            #endif
        }
    }
}