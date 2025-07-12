using System.Collections;
using System.Collections.Generic;
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
    /// OpticalFlow Example
    /// http://stackoverflow.com/questions/6505779/android-optical-flow-with-opencv?rq=1
    /// http://docs.opencv.org/3.2.0/d7/d8b/tutorial_py_lucas_kanade.html
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class OpticalFlowExample : MonoBehaviour
    {
        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
        /// <summary>
        /// The mat op flow this.
        /// </summary>
        private Mat _matOpFlowThis;

        /// <summary>
        /// The mat op flow previous.
        /// </summary>
        private Mat _matOpFlowPrev;

        /// <summary>
        /// The i GFFT max.
        /// </summary>
        private int _iGfftMax = 40;

        /// <summary>
        /// The MO pcorners.
        /// </summary>
        private MatOfPoint _mopCorners;

        /// <summary>
        /// The m MO p2fpts this.
        /// </summary>
        private MatOfPoint2f _mMop2fptsThis;

        /// <summary>
        /// The m MO p2fpts previous.
        /// </summary>
        private MatOfPoint2f _mMop2fptsPrev;

        /// <summary>
        /// The m MO p2fpts safe.
        /// </summary>
        private MatOfPoint2f _mMop2fptsSafe;

        /// <summary>
        /// The m MOB status.
        /// </summary>
        private MatOfByte _mMobStatus;

        /// <summary>
        /// The m MO ferr.
        /// </summary>
        private MatOfFloat _mMofErr;

        /// <summary>
        /// The color red.
        /// </summary>
        private Scalar _colorRed = new Scalar(255, 0, 0, 255);

        /// <summary>
        /// The i line thickness.
        /// </summary>
        private int _iLineThickness = 3;

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

        private void Update()
        {
            if (_multiSource2MatHelper.IsPlaying() && _multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = _multiSource2MatHelper.GetMat();

                if (_mMop2fptsPrev.rows() == 0)
                {

                    // first time through the loop so we need prev and this mats
                    // plus prev points
                    // get this mat
                    Imgproc.cvtColor(rgbaMat, _matOpFlowThis, Imgproc.COLOR_RGBA2GRAY);

                    // copy that to prev mat
                    _matOpFlowThis.copyTo(_matOpFlowPrev);

                    // get prev corners
                    Imgproc.goodFeaturesToTrack(_matOpFlowPrev, _mopCorners, _iGfftMax, 0.05, 20);
                    _mMop2fptsPrev.fromArray(_mopCorners.toArray());

                    // get safe copy of this corners
                    _mMop2fptsPrev.copyTo(_mMop2fptsSafe);
                }
                else
                {
                    // we've been through before so
                    // this mat is valid. Copy it to prev mat
                    _matOpFlowThis.copyTo(_matOpFlowPrev);

                    // get this mat
                    Imgproc.cvtColor(rgbaMat, _matOpFlowThis, Imgproc.COLOR_RGBA2GRAY);

                    // get the corners for this mat
                    Imgproc.goodFeaturesToTrack(_matOpFlowThis, _mopCorners, _iGfftMax, 0.05, 20);
                    _mMop2fptsThis.fromArray(_mopCorners.toArray());

                    // retrieve the corners from the prev mat
                    // (saves calculating them again)
                    _mMop2fptsSafe.copyTo(_mMop2fptsPrev);

                    // and save this corners for next time through

                    _mMop2fptsThis.copyTo(_mMop2fptsSafe);
                }

                /*
                    Parameters:
                        prevImg first 8-bit input image
                        nextImg second input image
                        prevPts vector of 2D points for which the flow needs to be found; point coordinates must be single-precision floating-point numbers.
                        nextPts output vector of 2D points (with single-precision floating-point coordinates) containing the calculated new positions of input features in the second image; when OPTFLOW_USE_INITIAL_FLOW flag is passed, the vector must have the same size as in the input.
                        status output status vector (of unsigned chars); each element of the vector is set to 1 if the flow for the corresponding features has been found, otherwise, it is set to 0.
                        err output vector of errors; each element of the vector is set to an error for the corresponding feature, type of the error measure can be set in flags parameter; if the flow wasn't found then the error is not defined (use the status parameter to find such cases).
                */
                Video.calcOpticalFlowPyrLK(_matOpFlowPrev, _matOpFlowThis, _mMop2fptsPrev, _mMop2fptsThis, _mMobStatus, _mMofErr);

                if (_mMobStatus.rows() > 0)
                {
                    List<Point> cornersPrev = _mMop2fptsPrev.toList();
                    List<Point> cornersThis = _mMop2fptsThis.toList();
                    List<byte> byteStatus = _mMobStatus.toList();

                    int x = 0;
                    int y = byteStatus.Count - 1;

                    for (x = 0; x < y; x++)
                    {
                        if (byteStatus[x] == 1)
                        {
                            Point pt = cornersThis[x];
                            Point pt2 = cornersPrev[x];

                            Imgproc.circle(rgbaMat, pt, 5, _colorRed, _iLineThickness - 1);

                            Imgproc.line(rgbaMat, pt, pt2, _colorRed, _iLineThickness);
                        }
                    }
                }

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

            _matOpFlowThis = new Mat();
            _matOpFlowPrev = new Mat();
            _mopCorners = new MatOfPoint();
            _mMop2fptsThis = new MatOfPoint2f();
            _mMop2fptsPrev = new MatOfPoint2f();
            _mMop2fptsSafe = new MatOfPoint2f();
            _mMobStatus = new MatOfByte();
            _mMofErr = new MatOfFloat();
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;

            _matOpFlowThis?.Dispose();
            _matOpFlowPrev?.Dispose();
            _mopCorners?.Dispose();
            _mMop2fptsThis?.Dispose();
            _mMop2fptsPrev?.Dispose();
            _mMop2fptsSafe?.Dispose();
            _mMobStatus?.Dispose();
            _mMofErr?.Dispose();
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
    }
}
