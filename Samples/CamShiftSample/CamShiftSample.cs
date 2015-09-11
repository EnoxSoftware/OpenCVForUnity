using UnityEngine;
using System.Collections;

using OpenCVForUnity;

using System.Collections.Generic;

namespace OpenCVForUnitySample
{
		/// <summary>
		/// CamShift sample.
		/// referring to the http://www.computervisiononline.com/blog/tutorial-using-camshift-track-objects-video.
		/// </summary>
		public class CamShiftSample : MonoBehaviour
		{
	
				/// <summary>
				/// The web cam texture.
				/// </summary>
				WebCamTexture webCamTexture;

				/// <summary>
				/// The web cam device.
				/// </summary>
				WebCamDevice webCamDevice;

				/// <summary>
				/// The colors.
				/// </summary>
				Color32[] colors;

				/// <summary>
				/// Should use front facing.
				/// </summary>
				public bool shouldUseFrontFacing = false;

				/// <summary>
				/// The width.
				/// </summary>
				int width = 640;

				/// <summary>
				/// The height.
				/// </summary>
				int height = 480;

				/// <summary>
				/// The rgba mat.
				/// </summary>
				Mat rgbaMat;

				/// <summary>
				/// The texture.
				/// </summary>
				Texture2D texture;

				/// <summary>
				/// The init done.
				/// </summary>
				bool initDone = false;

				/// <summary>
				/// The screenOrientation.
				/// </summary>
				ScreenOrientation screenOrientation = ScreenOrientation.Unknown;

				/// <summary>
				/// The roi point list.
				/// </summary>
				List<Point> roiPointList;

				/// <summary>
				/// The roi rect.
				/// </summary>
				OpenCVForUnity.Rect roiRect;

				/// <summary>
				/// The hsv mat.
				/// </summary>
				Mat hsvMat;

				/// <summary>
				/// The roi hist mat.
				/// </summary>
				Mat roiHistMat;

				/// <summary>
				/// The termination.
				/// </summary>
				TermCriteria termination;
	
				// Use this for initialization
				void Start ()
				{
						roiPointList = new List<Point> ();
						termination = new TermCriteria (TermCriteria.EPS | TermCriteria.COUNT, 10, 1);
						
						StartCoroutine (init ());

				}

				private IEnumerator init ()
				{
						if (webCamTexture != null) {
								webCamTexture.Stop ();
								initDone = false;
				
								rgbaMat.Dispose ();
								hsvMat.Dispose ();
								if (roiHistMat != null)
										roiHistMat.Dispose ();
								roiPointList.Clear ();
						}

						// Checks how many and which cameras are available on the device
						for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {
				
				
								if (WebCamTexture.devices [cameraIndex].isFrontFacing == shouldUseFrontFacing) {
					
					
										Debug.Log (cameraIndex + " name " + WebCamTexture.devices [cameraIndex].name + " isFrontFacing " + WebCamTexture.devices [cameraIndex].isFrontFacing);

										webCamDevice = WebCamTexture.devices [cameraIndex];

										webCamTexture = new WebCamTexture (webCamDevice.name, width, height);

										break;
								}
				
				
						}
			
						if (webCamTexture == null) {
								webCamDevice = WebCamTexture.devices [0];
								webCamTexture = new WebCamTexture (webCamDevice.name, width, height);
						}
			
						Debug.Log ("width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);
			
			
			
						// Starts the camera
						webCamTexture.Play ();


						while (true) {
								//If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/)
								#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
				                if (webCamTexture.width > 16 && webCamTexture.height > 16) {
								#else
								if (webCamTexture.didUpdateThisFrame) {
										#endif

										Debug.Log ("width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);
										Debug.Log ("videoRotationAngle " + webCamTexture.videoRotationAngle + " videoVerticallyMirrored " + webCamTexture.videoVerticallyMirrored + " isFrongFacing " + webCamDevice.isFrontFacing);
					
										colors = new Color32[webCamTexture.width * webCamTexture.height];
					
										rgbaMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
										hsvMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC3);
					
										texture = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

										gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

										updateLayout ();

										screenOrientation = Screen.orientation;
										initDone = true;
					
										break;
								} else {
										yield return 0;
								}
						}

				}

				private void updateLayout ()
				{
						gameObject.transform.localRotation = new Quaternion (0, 0, 0, 0);
						gameObject.transform.localScale = new Vector3 (webCamTexture.width, webCamTexture.height, 1);

						if (webCamTexture.videoRotationAngle == 90 || webCamTexture.videoRotationAngle == 270) {
								gameObject.transform.eulerAngles = new Vector3 (0, 0, -90);
						}


						float width = 0;
						float height = 0;
						if (webCamTexture.videoRotationAngle == 90 || webCamTexture.videoRotationAngle == 270) {
								width = gameObject.transform.localScale.y;
								height = gameObject.transform.localScale.x;
						} else if (webCamTexture.videoRotationAngle == 0 || webCamTexture.videoRotationAngle == 180) {
								width = gameObject.transform.localScale.x;
								height = gameObject.transform.localScale.y;
						}

						float widthScale = (float)Screen.width / width;
						float heightScale = (float)Screen.height / height;
						if (widthScale < heightScale) {
								Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
						} else {
								Camera.main.orthographicSize = height / 2;
						}
				}

				// Update is called once per frame
				void Update ()
				{
						if (!initDone)
								return;

						if (screenOrientation != Screen.orientation) {
								screenOrientation = Screen.orientation;
								updateLayout ();
						}

						#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
				        if (webCamTexture.width > 16 && webCamTexture.height > 16) {
						#else
						if (webCamTexture.didUpdateThisFrame) {
								#endif
						
								Utils.webCamTextureToMat (webCamTexture, rgbaMat, colors);


								//flip to correct direction.
								if (webCamDevice.isFrontFacing) {
										if (webCamTexture.videoRotationAngle == 0) {
												Core.flip (rgbaMat, rgbaMat, 1);
										} else if (webCamTexture.videoRotationAngle == 90) {
												Core.flip (rgbaMat, rgbaMat, 0);
										}
										if (webCamTexture.videoRotationAngle == 180) {
												Core.flip (rgbaMat, rgbaMat, 0);
										} else if (webCamTexture.videoRotationAngle == 270) {
												Core.flip (rgbaMat, rgbaMat, 1);
										}
								} else {
										if (webCamTexture.videoRotationAngle == 180) {
												Core.flip (rgbaMat, rgbaMat, -1);
										} else if (webCamTexture.videoRotationAngle == 270) {
												Core.flip (rgbaMat, rgbaMat, -1);
										}
								}

								
								Imgproc.cvtColor (rgbaMat, hsvMat, Imgproc.COLOR_RGBA2RGB);
								Imgproc.cvtColor (hsvMat, hsvMat, Imgproc.COLOR_RGB2HSV);


								Point[] points = roiPointList.ToArray ();

								if (roiPointList.Count == 4) {
										

										using (Mat backProj = new Mat ()) {
												Imgproc.calcBackProject (new List<Mat> (new Mat[]{hsvMat}), new MatOfInt (0), roiHistMat, backProj, new MatOfFloat (0, 180), 1.0);

												RotatedRect r = Video.CamShift (backProj, roiRect, termination);
												r.points (points);
										}

										#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
							//Touch
							int touchCount = Input.touchCount;
							if (touchCount == 1)
							{
								
								if(Input.GetTouch(0).phase == TouchPhase.Ended){
									
									roiPointList.Clear ();
								}
								
							}
										#else
										if (Input.GetMouseButtonUp (0)) {
												roiPointList.Clear ();
										}
#endif
								}


								if (roiPointList.Count < 4) {

										#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
							//Touch
							int touchCount = Input.touchCount;
							if (touchCount == 1)
							{
								Touch t = Input.GetTouch(0);
								if(t.phase == TouchPhase.Ended){
									roiPointList.Add (convertScreenPoint (new Point (t.position.x, t.position.y), gameObject, Camera.main));
//									Debug.Log ("touch X " + t.position.x);
//									Debug.Log ("touch Y " + t.position.y);

									if (!(new OpenCVForUnity.Rect (0, 0, hsvMat.width (), hsvMat.height ()).contains (roiPointList [roiPointList.Count - 1]))) {
										roiPointList.RemoveAt (roiPointList.Count - 1);
									}
								}
								
							}
#else
										//Mouse
										if (Input.GetMouseButtonUp (0)) {
												
												roiPointList.Add (convertScreenPoint (new Point (Input.mousePosition.x, Input.mousePosition.y), gameObject, Camera.main));
//												Debug.Log ("mouse X " + Input.mousePosition.x);
//												Debug.Log ("mouse Y " + Input.mousePosition.y);

												if (!(new OpenCVForUnity.Rect (0, 0, hsvMat.width (), hsvMat.height ()).contains (roiPointList [roiPointList.Count - 1]))) {
														roiPointList.RemoveAt (roiPointList.Count - 1);
												}
										}
#endif

							
										if (roiPointList.Count == 4) {

												using (MatOfPoint roiPointMat = new MatOfPoint (roiPointList.ToArray ())) {
														roiRect = Imgproc.boundingRect (roiPointMat);
												}

								
												if (roiHistMat != null) {
														roiHistMat.Dispose ();
														roiHistMat = null;
												}
												roiHistMat = new Mat ();

												using (Mat roiHSVMat = new Mat(hsvMat, roiRect))
												using (Mat maskMat = new Mat ()) {

														
														Imgproc.calcHist (new List<Mat> (new Mat[]{roiHSVMat}), new MatOfInt (0), maskMat, roiHistMat, new MatOfInt (16), new MatOfFloat (0, 180)); 
														Core.normalize (roiHistMat, roiHistMat, 0, 255, Core.NORM_MINMAX);
								
//														Debug.Log ("roiHist " + roiHistMat.ToString ());
												}
										}
								}

								if (points.Length < 4) {

										for (int i = 0; i < points.Length; i++) {
												Core.circle (rgbaMat, points [i], 6, new Scalar (0, 0, 255, 255), 2);
										}

								} else {

										for (int i = 0; i < 4; i++) {
												Core.line (rgbaMat, points [i], points [(i + 1) % 4], new Scalar (255, 0, 0, 255), 2);
										}

										Core.rectangle (rgbaMat, roiRect.tl (), roiRect.br (), new Scalar (0, 255, 0, 255), 2);
								}

								Core.putText (rgbaMat, "PLEASE TOUCH 4 POINTS", new Point (5, 25), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Core.LINE_AA, false);

		
								Utils.matToTexture2D (rgbaMat, texture, colors);
		
								gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

						}

				}
	
				void OnDisable ()
				{
						webCamTexture.Stop ();
				}
	
				void OnGUI ()
				{
						float screenScale = Screen.height / 240.0f;
						Matrix4x4 scaledMatrix = Matrix4x4.Scale (new Vector3 (screenScale, screenScale, screenScale));
						GUI.matrix = scaledMatrix;
		
		
						GUILayout.BeginVertical ();
						if (GUILayout.Button ("back")) {
								Application.LoadLevel ("OpenCVForUnitySample");
						}
						if (GUILayout.Button ("change camera")) {
								shouldUseFrontFacing = !shouldUseFrontFacing;
								StartCoroutine (init ());
						}
		
		
						GUILayout.EndVertical ();
				}

				/// <summary>
				/// Converts the screen point.
				/// </summary>
				/// <returns>The screen point.</returns>
				/// <param name="screenPoint">Screen point.</param>
				/// <param name="quad">Quad.</param>
				/// <param name="cam">Cam.</param>
				static Point convertScreenPoint (Point screenPoint, GameObject quad, Camera cam)
				{
						Vector2 tl;
						Vector2 tr;
						Vector2 br;
						Vector2 bl;
					
						if (Input.deviceOrientation == DeviceOrientation.Portrait || Input.deviceOrientation == DeviceOrientation.PortraitUpsideDown) {
								tl = cam.WorldToScreenPoint (new Vector3 (quad.transform.localPosition.x + quad.transform.localScale.y / 2, quad.transform.localPosition.y + quad.transform.localScale.x / 2, quad.transform.localPosition.z));
								tr = cam.WorldToScreenPoint (new Vector3 (quad.transform.localPosition.x + quad.transform.localScale.y / 2, quad.transform.localPosition.y - quad.transform.localScale.x / 2, quad.transform.localPosition.z));
								br = cam.WorldToScreenPoint (new Vector3 (quad.transform.localPosition.x - quad.transform.localScale.y / 2, quad.transform.localPosition.y - quad.transform.localScale.x / 2, quad.transform.localPosition.z));
								bl = cam.WorldToScreenPoint (new Vector3 (quad.transform.localPosition.x - quad.transform.localScale.y / 2, quad.transform.localPosition.y + quad.transform.localScale.x / 2, quad.transform.localPosition.z));
						} else {
								tl = cam.WorldToScreenPoint (new Vector3 (quad.transform.localPosition.x - quad.transform.localScale.x / 2, quad.transform.localPosition.y + quad.transform.localScale.y / 2, quad.transform.localPosition.z));
								tr = cam.WorldToScreenPoint (new Vector3 (quad.transform.localPosition.x + quad.transform.localScale.x / 2, quad.transform.localPosition.y + quad.transform.localScale.y / 2, quad.transform.localPosition.z));
								br = cam.WorldToScreenPoint (new Vector3 (quad.transform.localPosition.x + quad.transform.localScale.x / 2, quad.transform.localPosition.y - quad.transform.localScale.y / 2, quad.transform.localPosition.z));
								bl = cam.WorldToScreenPoint (new Vector3 (quad.transform.localPosition.x - quad.transform.localScale.x / 2, quad.transform.localPosition.y - quad.transform.localScale.y / 2, quad.transform.localPosition.z));
						}

						Mat srcRectMat = new Mat (4, 1, CvType.CV_32FC2);
						Mat dstRectMat = new Mat (4, 1, CvType.CV_32FC2);

						
						srcRectMat.put (0, 0, tl.x, tl.y, tr.x, tr.y, br.x, br.y, bl.x, bl.y);
						dstRectMat.put (0, 0, 0.0, 0.0, quad.transform.localScale.x, 0.0, quad.transform.localScale.x, quad.transform.localScale.y, 0.0, quad.transform.localScale.y);

						
						Mat perspectiveTransform = Imgproc.getPerspectiveTransform (srcRectMat, dstRectMat);

//						Debug.Log ("srcRectMat " + srcRectMat.dump ());
//						Debug.Log ("dstRectMat " + dstRectMat.dump ());
//						Debug.Log ("perspectiveTransform " + perspectiveTransform.dump ());

						MatOfPoint2f srcPointMat = new MatOfPoint2f (screenPoint);
						MatOfPoint2f dstPointMat = new MatOfPoint2f ();

						Core.perspectiveTransform (srcPointMat, dstPointMat, perspectiveTransform);

//						Debug.Log ("srcPointMat " + srcPointMat.dump ());
//						Debug.Log ("dstPointMat " + dstPointMat.dump ());

						return dstPointMat.toArray () [0];
				}
		}
}