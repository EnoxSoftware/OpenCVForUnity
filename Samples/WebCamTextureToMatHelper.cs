using UnityEngine;
using System.Collections;

using System;
using OpenCVForUnity;

namespace OpenCVForUnitySample
{

/// <summary>
/// Web cam texture to mat helper.
/// </summary>
		public class WebCamTextureToMatHelper  : MonoBehaviour
		{
				/// <summary>
				/// The name of the device.
				/// </summary>
				public string requestDeviceName = null;

				/// <summary>
				/// The width.
				/// </summary>
				public int requestWidth = 640;
	
				/// <summary>
				/// The height.
				/// </summary>
				public int requestHeight = 480;

				/// <summary>
				/// Should use front facing.
				/// </summary>
				public bool requestIsFrontFacing = false;

				/// <summary>
				/// The on inited.
				/// </summary>
				public Action OnInited;

				/// <summary>
				/// The on disposed.
				/// </summary>
				public Action OnDisposed;

				/// <summary>
				/// The web cam texture.
				/// </summary>
				WebCamTexture webCamTexture;
	
				/// <summary>
				/// The web cam device.
				/// </summary>
				WebCamDevice webCamDevice;

				/// <summary>
				/// The rgba mat.
				/// </summary>
				Mat rgbaMat;

				/// <summary>
				/// The rotated rgba mat
				/// </summary>
				Mat rotatedRgbaMat;

				/// <summary>
				/// The colors.
				/// </summary>
				Color32[] colors;

				/// <summary>
				/// The init done.
				/// </summary>
				bool initDone = false;
	
				/// <summary>
				/// The screenOrientation.
				/// </summary>
				ScreenOrientation screenOrientation = ScreenOrientation.Unknown;
	

				// Use this for initialization
				void Start ()
				{
		
				}

				// Update is called once per frame
				void Update ()
				{
						if (initDone) {
								if (screenOrientation != Screen.orientation) {
										StartCoroutine (init ());
								}
						}
				}

				/// <summary>
				/// Init the specified OnInited.
				/// </summary>
				/// <param name="OnInited">On inited.</param>
				public void Init (Action OnInited, Action OnDisposed)
				{
						this.OnInited = OnInited;
						this.OnDisposed = OnDisposed;
			
						StartCoroutine (init ());
				}

				/// <summary>
				/// Init the specified deviceName, requestWidth, requestHeight, requestIsFrontFacing and OnInited.
				/// </summary>
				/// <param name="deviceName">Device name.</param>
				/// <param name="requestWidth">Request width.</param>
				/// <param name="requestHeight">Request height.</param>
				/// <param name="requestIsFrontFacing">If set to <c>true</c> request is front facing.</param>
				/// <param name="OnInited">On inited.</param>
				public void Init (string deviceName, int requestWidth, int requestHeight, bool requestIsFrontFacing, Action OnInited, Action OnDisposed)
				{
						this.requestDeviceName = deviceName;
						this.requestWidth = requestWidth;
						this.requestHeight = requestHeight;
						this.requestIsFrontFacing = requestIsFrontFacing;
						this.OnInited = OnInited;
						this.OnDisposed = OnDisposed;

						StartCoroutine (init ());
				}

				/// <summary>
				/// Init this instance.
				/// </summary>
				private IEnumerator init ()
				{
						if (initDone)
								Dispose ();

						if (!String.IsNullOrEmpty (requestDeviceName)) {
//			Debug.Log ("deviceName is "+requestDeviceName);
								webCamTexture = new WebCamTexture (requestDeviceName, requestWidth, requestHeight);
						} else {
//			Debug.Log ("deviceName is null");
								// Checks how many and which cameras are available on the device
								for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {
			
										if (WebCamTexture.devices [cameraIndex].isFrontFacing == requestIsFrontFacing) {
				
//					Debug.Log (cameraIndex + " name " + WebCamTexture.devices [cameraIndex].name + " isFrontFacing " + WebCamTexture.devices [cameraIndex].isFrontFacing);
				
												webCamDevice = WebCamTexture.devices [cameraIndex];
				
												webCamTexture = new WebCamTexture (webCamDevice.name, requestWidth, requestHeight);
				
												break;
										}
			
			
								}
						}
		
						if (webCamTexture == null) {
//			Debug.Log ("webCamTexture is null");

								webCamDevice = WebCamTexture.devices [0];
								webCamTexture = new WebCamTexture (webCamDevice.name, requestWidth, requestHeight);
						}
	
//		Debug.Log ("name " + webCamTexture.name + " width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);
		
		
		
						// Starts the camera
						webCamTexture.Play ();
		
		
						while (true) {
								//If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/)
								#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
			if (webCamTexture.width > 16 && webCamTexture.height > 16) {
								#else
								if (webCamTexture.didUpdateThisFrame) {
										#if UNITY_IOS && !UNITY_EDITOR && UNITY_5_2                                    
					while (webCamTexture.width <= 16) {
						webCamTexture.GetPixels32 ();
						yield return new WaitForEndOfFrame ();
					} 
										#endif
										#endif
					
										Debug.Log ("name " + webCamTexture.name + " width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);
										Debug.Log ("videoRotationAngle " + webCamTexture.videoRotationAngle + " videoVerticallyMirrored " + webCamTexture.videoVerticallyMirrored + " isFrongFacing " + webCamDevice.isFrontFacing);

										colors = new Color32[webCamTexture.width * webCamTexture.height];
										rgbaMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);

//				Debug.Log ("Screen.orientation " + Screen.orientation);
										screenOrientation = Screen.orientation;
//					screenOrientation = ScreenOrientation.PortraitUpsideDown;

										#if !UNITY_EDITOR && !UNITY_STANDALONE 
										if (screenOrientation == ScreenOrientation.Portrait || screenOrientation == ScreenOrientation.PortraitUpsideDown) {
												rotatedRgbaMat = new Mat (webCamTexture.width, webCamTexture.height, CvType.CV_8UC4);
										}
#endif

//				webCamTexture.Stop ();

										initDone = true;

										if (OnInited != null)
												OnInited ();


										break;
								} else {
										yield return 0;
								}
						}
				}

				/// <summary>
				/// Ises the inited.
				/// </summary>
				/// <returns><c>true</c>, if inited was ised, <c>false</c> otherwise.</returns>
				public bool isInited ()
				{

						return initDone;
				}

				/// <summary>
				/// Play this instance.
				/// </summary>
				public void Play ()
				{
						if (initDone)
								webCamTexture.Play ();
				}

				/// <summary>
				/// Pause this instance.
				/// </summary>
				public void Pause ()
				{
						if (initDone)
								webCamTexture.Pause ();
				}

				/// <summary>
				/// Stop this instance.
				/// </summary>
				public void Stop ()
				{
						if (initDone)
								webCamTexture.Stop ();
				}
	
				/// <summary>
				/// Ises the playing.
				/// </summary>
				/// <returns><c>true</c>, if playing was ised, <c>false</c> otherwise.</returns>
				public bool isPlaying ()
				{
						if (!initDone)
								return false;
						return webCamTexture.isPlaying;
				}

				/// <summary>
				/// Gets the web cam texture.
				/// </summary>
				/// <returns>The web cam texture.</returns>
				public WebCamTexture GetWebCamTexture ()
				{
						return webCamTexture;
				}

				/// <summary>
				/// Gets the mat.
				/// </summary>
				/// <returns>The mat.</returns>
				public Mat GetMat ()
				{
						if (!initDone || !webCamTexture.isPlaying) {
								if (rotatedRgbaMat != null) {
										return rotatedRgbaMat;
								} else {
										return rgbaMat;
								}
						}

			
			
						#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
			if (webCamTexture.width > 16 && webCamTexture.height > 16) {
						#else
						if (webCamTexture.didUpdateThisFrame) {
								#endif
					
								Utils.webCamTextureToMat (webCamTexture, rgbaMat, colors);
					
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
					
						}

						if (rotatedRgbaMat != null) {

								using (Mat transposeRgbaMat = rgbaMat.t()) {
										Core.flip (transposeRgbaMat, rotatedRgbaMat, 1);
								}

								return rotatedRgbaMat;
						} else {
								return rgbaMat;
						}
				}

				/// <summary>
				/// Releases all resource used by the <see cref="WebCamTextureToMatHelper"/> object.
				/// </summary>
				/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="WebCamTextureToMatHelper"/>. The
				/// <see cref="Dispose"/> method leaves the <see cref="WebCamTextureToMatHelper"/> in an unusable state. After
				/// calling <see cref="Dispose"/>, you must release all references to the <see cref="WebCamTextureToMatHelper"/> so
				/// the garbage collector can reclaim the memory that the <see cref="WebCamTextureToMatHelper"/> was occupying.</remarks>
				public void Dispose ()
				{
						initDone = false;

						if (webCamTexture != null) {
								webCamTexture.Stop ();
								webCamTexture = null;
						}
						if (rgbaMat != null) {
								rgbaMat.Dispose ();
								rgbaMat = null;
						}
						if (rotatedRgbaMat != null) {
								rotatedRgbaMat.Dispose ();
								rotatedRgbaMat = null;
						}
						colors = null;

						if (OnDisposed != null)
								OnDisposed ();
				}
		}
}
