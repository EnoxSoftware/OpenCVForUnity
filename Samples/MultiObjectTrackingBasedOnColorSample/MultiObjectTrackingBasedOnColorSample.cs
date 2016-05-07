using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;
using System.Collections.Generic;

namespace OpenCVForUnitySample
{
		/// <summary>
		/// Multi object tracking based on color sample.
		/// referring to the https://www.youtube.com/watch?v=hQ-bpfdWQh8.
		/// </summary>
		[RequireComponent(typeof(WebCamTextureToMatHelper))]
		public class MultiObjectTrackingBasedOnColorSample : MonoBehaviour
		{

				/// <summary>
				/// The colors.
				/// </summary>
				Color32[] colors;

				/// <summary>
				/// The texture.
				/// </summary>
				Texture2D texture;
	
				/// <summary>
				/// max number of objects to be detected in frame
				/// </summary>
				const int MAX_NUM_OBJECTS = 50;
		
				/// <summary>
				/// minimum and maximum object area
				/// </summary>
				const int MIN_OBJECT_AREA = 20 * 20;

//				/// <summary>
//				/// max object area
//				/// </summary>
//				int MAX_OBJECT_AREA;

				/// <summary>
				/// The rgb mat.
				/// </summary>
				Mat rgbMat;

				/// <summary>
				/// The threshold mat.
				/// </summary>
				Mat thresholdMat;

				/// <summary>
				/// The hsv mat.
				/// </summary>
				Mat hsvMat;
				ColorObject blue = new ColorObject ("blue");
				ColorObject yellow = new ColorObject ("yellow");
				ColorObject red = new ColorObject ("red");
				ColorObject green = new ColorObject ("green");

				/// <summary>
				/// The web cam texture to mat helper.
				/// </summary>
				WebCamTextureToMatHelper webCamTextureToMatHelper;

				// Use this for initialization
				void Start ()
				{
						
						webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();
						webCamTextureToMatHelper.Init ();

				}

				/// <summary>
				/// Raises the web cam texture to mat helper inited event.
				/// </summary>
				public void OnWebCamTextureToMatHelperInited ()
				{
						Debug.Log ("OnWebCamTextureToMatHelperInited");
			
						Mat webCamTextureMat = webCamTextureToMatHelper.GetMat ();
			
						colors = new Color32[webCamTextureMat.cols () * webCamTextureMat.rows ()];
						texture = new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.RGBA32, false);

						rgbMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC3);
						thresholdMat = new Mat ();
						hsvMat = new Mat ();
			
						//										MAX_OBJECT_AREA = (int)(webCamTexture.height * webCamTexture.width / 1.5);


						gameObject.transform.localScale = new Vector3 (webCamTextureMat.cols (), webCamTextureMat.rows (), 1);
			
						Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);
			
						float width = 0;
						float height = 0;
			
						width = gameObject.transform.localScale.x;
						height = gameObject.transform.localScale.y;
			
						float widthScale = (float)Screen.width / width;
						float heightScale = (float)Screen.height / height;
						if (widthScale < heightScale) {
								Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
						} else {
								Camera.main.orthographicSize = height / 2;
						}
			
						gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
			
				}
		
				/// <summary>
				/// Raises the web cam texture to mat helper disposed event.
				/// </summary>
				public void OnWebCamTextureToMatHelperDisposed ()
				{
						Debug.Log ("OnWebCamTextureToMatHelperDisposed");

						if (rgbMat != null)
								rgbMat.Dispose ();
						if (thresholdMat != null)
								thresholdMat.Dispose ();
						if (hsvMat != null)
								hsvMat.Dispose ();
				}


				// Update is called once per frame
				void Update ()
				{

						if (webCamTextureToMatHelper.isPlaying () && webCamTextureToMatHelper.didUpdateThisFrame ()) {
				
								Mat rgbaMat = webCamTextureToMatHelper.GetMat ();
								

								Imgproc.cvtColor (rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);
										
								//first find blue objects
								Imgproc.cvtColor (rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);
								Core.inRange (hsvMat, blue.getHSVmin (), blue.getHSVmax (), thresholdMat);
								morphOps (thresholdMat);
								trackFilteredObject (blue, thresholdMat, hsvMat, rgbMat);
								//then yellows
								Imgproc.cvtColor (rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);
								Core.inRange (hsvMat, yellow.getHSVmin (), yellow.getHSVmax (), thresholdMat);
								morphOps (thresholdMat);
								trackFilteredObject (yellow, thresholdMat, hsvMat, rgbMat);
								//then reds
								Imgproc.cvtColor (rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);
								Core.inRange (hsvMat, red.getHSVmin (), red.getHSVmax (), thresholdMat);
								morphOps (thresholdMat);
								trackFilteredObject (red, thresholdMat, hsvMat, rgbMat);
								//then greens
								Imgproc.cvtColor (rgbMat, hsvMat, Imgproc.COLOR_RGB2HSV);
								Core.inRange (hsvMat, green.getHSVmin (), green.getHSVmax (), thresholdMat);
								morphOps (thresholdMat);
								trackFilteredObject (green, thresholdMat, hsvMat, rgbMat);
				
								Imgproc.putText (rgbMat, "W:" + rgbMat.width () + " H:" + rgbMat.height () + " SO:" + Screen.orientation, new Point (5, rgbMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
				
								Utils.matToTexture2D (rgbMat, texture, colors);
						}
				}
	
				/// <summary>
				/// Raises the disable event.
				/// </summary>
				void OnDisable ()
				{
						webCamTextureToMatHelper.Dispose ();
				}
		
				/// <summary>
				/// Raises the back button event.
				/// </summary>
				public void OnBackButton ()
				{
						#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			SceneManager.LoadScene ("OpenCVForUnitySample");
						#else
						Application.LoadLevel ("OpenCVForUnitySample");
						#endif
				}
		
				/// <summary>
				/// Raises the play button event.
				/// </summary>
				public void OnPlayButton ()
				{
						webCamTextureToMatHelper.Play ();
				}
		
				/// <summary>
				/// Raises the pause button event.
				/// </summary>
				public void OnPauseButton ()
				{
						webCamTextureToMatHelper.Pause ();
				}
		
				/// <summary>
				/// Raises the stop button event.
				/// </summary>
				public void OnStopButton ()
				{
						webCamTextureToMatHelper.Stop ();
				}
		
				/// <summary>
				/// Raises the change camera button event.
				/// </summary>
				public void OnChangeCameraButton ()
				{
						webCamTextureToMatHelper.Init (null, webCamTextureToMatHelper.requestWidth, webCamTextureToMatHelper.requestHeight, !webCamTextureToMatHelper.requestIsFrontFacing);
				}

				/// <summary>
				/// Draws the object.
				/// </summary>
				/// <param name="theColorObjects">The color objects.</param>
				/// <param name="frame">Frame.</param>
				/// <param name="temp">Temp.</param>
				/// <param name="contours">Contours.</param>
				/// <param name="hierarchy">Hierarchy.</param>
				void drawObject (List<ColorObject> theColorObjects, Mat frame, Mat temp, List<MatOfPoint> contours, Mat hierarchy)
				{

						for (int i = 0; i < theColorObjects.Count; i++) {
								Imgproc.drawContours (frame, contours, i, theColorObjects [i].getColor (), 3, 8, hierarchy, int.MaxValue, new Point ());
								Imgproc.circle (frame, new Point (theColorObjects [i].getXPos (), theColorObjects [i].getYPos ()), 5, theColorObjects [i].getColor ());
								Imgproc.putText (frame, theColorObjects [i].getXPos () + " , " + theColorObjects [i].getYPos (), new Point (theColorObjects [i].getXPos (), theColorObjects [i].getYPos () + 20), 1, 1, theColorObjects [i].getColor (), 2);
								Imgproc.putText (frame, theColorObjects [i].getType (), new Point (theColorObjects [i].getXPos (), theColorObjects [i].getYPos () - 20), 1, 2, theColorObjects [i].getColor (), 2);
						}
				}

				/// <summary>
				/// Morphs the ops.
				/// </summary>
				/// <param name="thresh">Thresh.</param>
				void morphOps (Mat thresh)
				{
					
						//create structuring element that will be used to "dilate" and "erode" image.
						//the element chosen here is a 3px by 3px rectangle
						Mat erodeElement = Imgproc.getStructuringElement (Imgproc.MORPH_RECT, new Size (3, 3));
						//dilate with larger element so make sure object is nicely visible
						Mat dilateElement = Imgproc.getStructuringElement (Imgproc.MORPH_RECT, new Size (8, 8));
					
						Imgproc.erode (thresh, thresh, erodeElement);
						Imgproc.erode (thresh, thresh, erodeElement);
					
						Imgproc.dilate (thresh, thresh, dilateElement);
						Imgproc.dilate (thresh, thresh, dilateElement);
				}

				/// <summary>
				/// Tracks the filtered object.
				/// </summary>
				/// <param name="theColorObject">The color object.</param>
				/// <param name="threshold">Threshold.</param>
				/// <param name="HSV">HS.</param>
				/// <param name="cameraFeed">Camera feed.</param>
				void trackFilteredObject (ColorObject theColorObject, Mat threshold, Mat HSV, Mat cameraFeed)
				{
					
						List<ColorObject> colorObjects = new List<ColorObject> ();
						Mat temp = new Mat ();
						threshold.copyTo (temp);
						//these two vectors needed for output of findContours
						List<MatOfPoint> contours = new List<MatOfPoint> ();
						Mat hierarchy = new Mat ();
						//find contours of filtered image using openCV findContours function
						Imgproc.findContours (temp, contours, hierarchy, Imgproc.RETR_CCOMP, Imgproc.CHAIN_APPROX_SIMPLE);
						
						//use moments method to find our filtered object
						bool colorObjectFound = false;
						if (hierarchy.rows () > 0) {
								int numObjects = hierarchy.rows ();

//						Debug.Log("hierarchy " + hierarchy.ToString());

								//if number of objects greater than MAX_NUM_OBJECTS we have a noisy filter
								if (numObjects < MAX_NUM_OBJECTS) {
										for (int index = 0; index >= 0; index = (int)hierarchy.get(0, index)[0]) {

												Moments moment = Imgproc.moments (contours [index]);
												double area = moment.get_m00 ();
								
												//if the area is less than 20 px by 20px then it is probably just noise
												//if the area is the same as the 3/2 of the image size, probably just a bad filter
												//we only want the object with the largest area so we safe a reference area each
												//iteration and compare it to the area in the next iteration.
												if (area > MIN_OBJECT_AREA) {
									
														ColorObject colorObject = new ColorObject ();
									
														colorObject.setXPos ((int)(moment.get_m10 () / area));
														colorObject.setYPos ((int)(moment.get_m01 () / area));
														colorObject.setType (theColorObject.getType ());
														colorObject.setColor (theColorObject.getColor ());
									
														colorObjects.Add (colorObject);
									
														colorObjectFound = true;
									
												} else {
														colorObjectFound = false;
												}
										}
										//let user know you found an object
										if (colorObjectFound == true) {
												//draw object location on screen
												drawObject (colorObjects, cameraFeed, temp, contours, hierarchy);
										}
							
								} else {
										Imgproc.putText (cameraFeed, "TOO MUCH NOISE!", new Point (5, cameraFeed.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
								}
						}
				}

		}
}