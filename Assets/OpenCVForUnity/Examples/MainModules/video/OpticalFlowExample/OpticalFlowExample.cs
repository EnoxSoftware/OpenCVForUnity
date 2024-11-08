using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.VideoModule;
using System.Collections;
using System.Collections.Generic;
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
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]

        /// <summary>
        /// The mat op flow this.
        /// </summary>
        Mat matOpFlowThis;

        /// <summary>
        /// The mat op flow previous.
        /// </summary>
        Mat matOpFlowPrev;

        /// <summary>
        /// The i GFFT max.
        /// </summary>
        int iGFFTMax = 40;

        /// <summary>
        /// The MO pcorners.
        /// </summary>
        MatOfPoint MOPcorners;

        /// <summary>
        /// The m MO p2fpts this.
        /// </summary>
        MatOfPoint2f mMOP2fptsThis;

        /// <summary>
        /// The m MO p2fpts previous.
        /// </summary>
        MatOfPoint2f mMOP2fptsPrev;

        /// <summary>
        /// The m MO p2fpts safe.
        /// </summary>
        MatOfPoint2f mMOP2fptsSafe;

        /// <summary>
        /// The m MOB status.
        /// </summary>
        MatOfByte mMOBStatus;

        /// <summary>
        /// The m MO ferr.
        /// </summary>
        MatOfFloat mMOFerr;

        /// <summary>
        /// The color red.
        /// </summary>
        Scalar colorRed = new Scalar(255, 0, 0, 255);

        /// <summary>
        /// The i line thickness.
        /// </summary>
        int iLineThickness = 3;

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

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;


            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", rgbaMat.width().ToString());
                fpsMonitor.Add("height", rgbaMat.height().ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }

            matOpFlowThis = new Mat();
            matOpFlowPrev = new Mat();
            MOPcorners = new MatOfPoint();
            mMOP2fptsThis = new MatOfPoint2f();
            mMOP2fptsPrev = new MatOfPoint2f();
            mMOP2fptsSafe = new MatOfPoint2f();
            mMOBStatus = new MatOfByte();
            mMOFerr = new MatOfFloat();
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

            if (matOpFlowThis != null)
                matOpFlowThis.Dispose();
            if (matOpFlowPrev != null)
                matOpFlowPrev.Dispose();
            if (MOPcorners != null)
                MOPcorners.Dispose();
            if (mMOP2fptsThis != null)
                mMOP2fptsThis.Dispose();
            if (mMOP2fptsPrev != null)
                mMOP2fptsPrev.Dispose();
            if (mMOP2fptsSafe != null)
                mMOP2fptsSafe.Dispose();
            if (mMOBStatus != null)
                mMOBStatus.Dispose();
            if (mMOFerr != null)
                mMOFerr.Dispose();
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
            if (multiSource2MatHelper.IsPlaying() && multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = multiSource2MatHelper.GetMat();

                if (mMOP2fptsPrev.rows() == 0)
                {

                    // first time through the loop so we need prev and this mats
                    // plus prev points
                    // get this mat
                    Imgproc.cvtColor(rgbaMat, matOpFlowThis, Imgproc.COLOR_RGBA2GRAY);

                    // copy that to prev mat
                    matOpFlowThis.copyTo(matOpFlowPrev);

                    // get prev corners
                    Imgproc.goodFeaturesToTrack(matOpFlowPrev, MOPcorners, iGFFTMax, 0.05, 20);
                    mMOP2fptsPrev.fromArray(MOPcorners.toArray());

                    // get safe copy of this corners
                    mMOP2fptsPrev.copyTo(mMOP2fptsSafe);
                }
                else
                {
                    // we've been through before so
                    // this mat is valid. Copy it to prev mat
                    matOpFlowThis.copyTo(matOpFlowPrev);

                    // get this mat
                    Imgproc.cvtColor(rgbaMat, matOpFlowThis, Imgproc.COLOR_RGBA2GRAY);

                    // get the corners for this mat
                    Imgproc.goodFeaturesToTrack(matOpFlowThis, MOPcorners, iGFFTMax, 0.05, 20);
                    mMOP2fptsThis.fromArray(MOPcorners.toArray());

                    // retrieve the corners from the prev mat
                    // (saves calculating them again)
                    mMOP2fptsSafe.copyTo(mMOP2fptsPrev);

                    // and save this corners for next time through

                    mMOP2fptsThis.copyTo(mMOP2fptsSafe);
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
                Video.calcOpticalFlowPyrLK(matOpFlowPrev, matOpFlowThis, mMOP2fptsPrev, mMOP2fptsThis, mMOBStatus, mMOFerr);

                if (mMOBStatus.rows() > 0)
                {
                    List<Point> cornersPrev = mMOP2fptsPrev.toList();
                    List<Point> cornersThis = mMOP2fptsThis.toList();
                    List<byte> byteStatus = mMOBStatus.toList();

                    int x = 0;
                    int y = byteStatus.Count - 1;

                    for (x = 0; x < y; x++)
                    {
                        if (byteStatus[x] == 1)
                        {
                            Point pt = cornersThis[x];
                            Point pt2 = cornersPrev[x];

                            Imgproc.circle(rgbaMat, pt, 5, colorRed, iLineThickness - 1);

                            Imgproc.line(rgbaMat, pt, pt2, colorRed, iLineThickness);
                        }
                    }
                }

                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Utils.matToTexture2D(rgbaMat, texture);
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
    }
}