using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityIntegration;
using OpenCVForUnity.UnityIntegration.Helper.Source2Mat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Multi Object Tracking Based on Color Example
    /// Referring to https://www.youtube.com/watch?v=hQ-bpfdWQh8.
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class MultiObjectTrackingBasedOnColorExample : MonoBehaviour
    {
        // Constants
        /// <summary>
        /// max number of objects to be detected in frame
        /// </summary>
        private const int MAX_NUM_OBJECTS = 50;

        /// <summary>
        /// minimum and maximum object area
        /// </summary>
        private const int MIN_OBJECT_AREA = 20 * 20;

        // Public Fields
        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage ResultPreview;

        [Space(10)]

        // Private Fields
        /// <summary>
        /// The texture.
        /// </summary>
        private Texture2D _texture;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        private Mat _rgbMat;

        /// <summary>
        /// The threshold mat.
        /// </summary>
        private Mat _thresholdMat;

        /// <summary>
        /// The hsv mat.
        /// </summary>
        private Mat _hsvMat;

        private ColorObject _blue = new ColorObject("blue");
        private ColorObject _yellow = new ColorObject("yellow");
        private ColorObject _red = new ColorObject("red");
        private ColorObject _green = new ColorObject("green");

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

                Imgproc.cvtColor(rgbaMat, _rgbMat, Imgproc.COLOR_RGBA2RGB);

                //first find blue objects
                Imgproc.cvtColor(_rgbMat, _hsvMat, Imgproc.COLOR_RGB2HSV);
                Core.inRange(_hsvMat, _blue.GetHSVmin(), _blue.GetHSVmax(), _thresholdMat);
                MorphOps(_thresholdMat);
                TrackFilteredObject(_blue, _thresholdMat, _hsvMat, _rgbMat);
                //then yellows
                Imgproc.cvtColor(_rgbMat, _hsvMat, Imgproc.COLOR_RGB2HSV);
                Core.inRange(_hsvMat, _yellow.GetHSVmin(), _yellow.GetHSVmax(), _thresholdMat);
                MorphOps(_thresholdMat);
                TrackFilteredObject(_yellow, _thresholdMat, _hsvMat, _rgbMat);
                //then reds
                Imgproc.cvtColor(_rgbMat, _hsvMat, Imgproc.COLOR_RGB2HSV);
                Core.inRange(_hsvMat, _red.GetHSVmin(), _red.GetHSVmax(), _thresholdMat);
                MorphOps(_thresholdMat);
                TrackFilteredObject(_red, _thresholdMat, _hsvMat, _rgbMat);
                //then greens
                Imgproc.cvtColor(_rgbMat, _hsvMat, Imgproc.COLOR_RGB2HSV);
                Core.inRange(_hsvMat, _green.GetHSVmin(), _green.GetHSVmax(), _thresholdMat);
                MorphOps(_thresholdMat);
                TrackFilteredObject(_green, _thresholdMat, _hsvMat, _rgbMat);

                //Imgproc.putText (_rgbMat, "W:" + _rgbMat.width () + " H:" + _rgbMat.height () + " SO:" + Screen.orientation, new Point (5, _rgbMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Imgproc.cvtColor(_rgbMat, rgbaMat, Imgproc.COLOR_RGB2RGBA);

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

            _rgbMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
            _thresholdMat = new Mat();
            _hsvMat = new Mat();
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            _rgbMat?.Dispose();
            _thresholdMat?.Dispose();
            _hsvMat?.Dispose();
            if (_texture != null) Texture2D.Destroy(_texture); _texture = null;
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

        // Private Methods
        /// <summary>
        /// Draws the object.
        /// </summary>
        /// <param name="theColorObjects">The color objects.</param>
        /// <param name="frame">Frame.</param>
        /// <param name="temp">Temp.</param>
        /// <param name="contours">Contours.</param>
        /// <param name="hierarchy">Hierarchy.</param>
        private void DrawObject(List<ColorObject> theColorObjects, Mat frame, Mat temp, List<MatOfPoint> contours, Mat hierarchy)
        {
            for (int i = 0; i < theColorObjects.Count; i++)
            {
                Imgproc.drawContours(frame, contours, i, theColorObjects[i].GetColor(), 3, 8, hierarchy, int.MaxValue, new Point());
                Imgproc.circle(frame, new Point(theColorObjects[i].GetXPos(), theColorObjects[i].GetYPos()), 5, theColorObjects[i].GetColor());
                Imgproc.putText(frame, theColorObjects[i].GetXPos() + " , " + theColorObjects[i].GetYPos(), new Point(theColorObjects[i].GetXPos(), theColorObjects[i].GetYPos() + 20), 1, 1, theColorObjects[i].GetColor(), 2);
                Imgproc.putText(frame, theColorObjects[i].GetObjectType(), new Point(theColorObjects[i].GetXPos(), theColorObjects[i].GetYPos() - 20), 1, 2, theColorObjects[i].GetColor(), 2);
            }
        }

        /// <summary>
        /// Morphs the ops.
        /// </summary>
        /// <param name="thresh">Thresh.</param>
        private void MorphOps(Mat thresh)
        {
            //create structuring element that will be used to "dilate" and "erode" image.
            //the element chosen here is a 3px by 3px rectangle
            Mat erodeElement = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(3, 3));
            //dilate with larger element so make sure object is nicely visible
            Mat dilateElement = Imgproc.getStructuringElement(Imgproc.MORPH_RECT, new Size(8, 8));

            Imgproc.erode(thresh, thresh, erodeElement);
            Imgproc.erode(thresh, thresh, erodeElement);

            Imgproc.dilate(thresh, thresh, dilateElement);
            Imgproc.dilate(thresh, thresh, dilateElement);
        }

        /// <summary>
        /// Tracks the filtered object.
        /// </summary>
        /// <param name="theColorObject">The color object.</param>
        /// <param name="threshold">Threshold.</param>
        /// <param name="HSV">HS.</param>
        /// <param name="cameraFeed">Camera feed.</param>
        private void TrackFilteredObject(ColorObject theColorObject, Mat threshold, Mat HSV, Mat cameraFeed)
        {

            List<ColorObject> colorObjects = new List<ColorObject>();
            Mat temp = new Mat();
            threshold.copyTo(temp);
            //these two vectors needed for output of findContours
            List<MatOfPoint> contours = new List<MatOfPoint>();
            Mat hierarchy = new Mat();
            //find contours of filtered image using openCV findContours function
            Imgproc.findContours(temp, contours, hierarchy, Imgproc.RETR_CCOMP, Imgproc.CHAIN_APPROX_SIMPLE);

            //use moments method to find our filtered object
            bool colorObjectFound = false;
            if (hierarchy.rows() > 0)
            {
                int numObjects = hierarchy.rows();

                //                      Debug.Log("hierarchy " + hierarchy.ToString());

                //if number of objects greater than MAX_NUM_OBJECTS we have a noisy filter
                if (numObjects < MAX_NUM_OBJECTS)
                {
                    for (int index = 0; index >= 0; index = (int)hierarchy.get(0, index)[0])
                    {

                        Moments moment = Imgproc.moments(contours[index]);
                        double area = moment.get_m00();

                        //if the area is less than 20 px by 20px then it is probably just noise
                        //if the area is the same as the 3/2 of the image size, probably just a bad filter
                        //we only want the object with the largest area so we safe a reference area each
                        //iteration and compare it to the area in the next iteration.
                        if (area > MIN_OBJECT_AREA)
                        {

                            ColorObject colorObject = new ColorObject();

                            colorObject.SetXPos((int)(moment.get_m10() / area));
                            colorObject.SetYPos((int)(moment.get_m01() / area));
                            colorObject.SetType(theColorObject.GetObjectType());
                            colorObject.SetColor(theColorObject.GetColor());

                            colorObjects.Add(colorObject);

                            colorObjectFound = true;

                        }
                        else
                        {
                            colorObjectFound = false;
                        }
                    }
                    //let user know you found an object
                    if (colorObjectFound == true)
                    {
                        //draw object location on screen
                        DrawObject(colorObjects, cameraFeed, temp, contours, hierarchy);
                    }

                }
                else
                {
                    Imgproc.putText(cameraFeed, "TOO MUCH NOISE!", new Point(5, cameraFeed.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                }
            }
        }
    }
}
