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
		[RequireComponent(typeof(WebCamTextureToMatHelper))]
		public class CamShiftSample : MonoBehaviour
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

				/// <summary>
				/// The web cam texture to mat helper.
				/// </summary>
				WebCamTextureToMatHelper webCamTextureToMatHelper;
	
				// Use this for initialization
				void Start ()
				{
						roiPointList = new List<Point> ();
						termination = new TermCriteria (TermCriteria.EPS | TermCriteria.COUNT, 10, 1);
						
						webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();
						webCamTextureToMatHelper.Init (OnWebCamTextureToMatHelperInited, OnWebCamTextureToMatHelperDisposed);

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

						hsvMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC3);
			
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

						hsvMat.Dispose ();
						if (roiHistMat != null)
								roiHistMat.Dispose ();
						roiPointList.Clear ();
				}

				// Update is called once per frame
				void Update ()
				{

						if (webCamTextureToMatHelper.isPlaying ()) {
				
								Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

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
				
								Core.putText (rgbaMat, "PLEASE TOUCH 4 POINTS", new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Core.LINE_AA, false);

				
//				Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
				
								Utils.matToTexture2D (rgbaMat, texture, colors);
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
						Application.LoadLevel ("OpenCVForUnitySample");
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
						webCamTextureToMatHelper.Init (null, webCamTextureToMatHelper.requestWidth, webCamTextureToMatHelper.requestHeight, !webCamTextureToMatHelper.requestIsFrontFacing, OnWebCamTextureToMatHelperInited, OnWebCamTextureToMatHelperDisposed);
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
					

						tl = cam.WorldToScreenPoint (new Vector3 (quad.transform.localPosition.x - quad.transform.localScale.x / 2, quad.transform.localPosition.y + quad.transform.localScale.y / 2, quad.transform.localPosition.z));
						tr = cam.WorldToScreenPoint (new Vector3 (quad.transform.localPosition.x + quad.transform.localScale.x / 2, quad.transform.localPosition.y + quad.transform.localScale.y / 2, quad.transform.localPosition.z));
						br = cam.WorldToScreenPoint (new Vector3 (quad.transform.localPosition.x + quad.transform.localScale.x / 2, quad.transform.localPosition.y - quad.transform.localScale.y / 2, quad.transform.localPosition.z));
						bl = cam.WorldToScreenPoint (new Vector3 (quad.transform.localPosition.x - quad.transform.localScale.x / 2, quad.transform.localPosition.y - quad.transform.localScale.y / 2, quad.transform.localPosition.z));


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