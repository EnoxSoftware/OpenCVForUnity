using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using OpenCVForUnity;

namespace OpenCVForUnitySample
{
		/// <summary>
		/// Optical flow sample.
		/// http://stackoverflow.com/questions/6505779/android-optical-flow-with-opencv?rq=1
		/// </summary>
		public class OpticalFlowSample : MonoBehaviour
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
				Scalar colorRed = new Scalar (255, 0, 0, 255);

				/// <summary>
				/// The i line thickness.
				/// </summary>
				int iLineThickness = 3;

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

	
				// Use this for initialization
				void Start ()
				{

						StartCoroutine (init ());
	
				}

				private IEnumerator init ()
				{
						if (webCamTexture != null) {
								webCamTexture.Stop ();
								initDone = false;
				
								rgbaMat.Dispose ();
				
								matOpFlowThis.Dispose ();
								matOpFlowPrev.Dispose ();
								MOPcorners.Dispose ();
								mMOP2fptsThis.Dispose ();
								mMOP2fptsPrev.Dispose ();
								mMOP2fptsSafe.Dispose ();
								mMOBStatus.Dispose ();
								mMOFerr.Dispose ();
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

										matOpFlowThis = new Mat ();
										matOpFlowPrev = new Mat ();
										MOPcorners = new MatOfPoint ();
										mMOP2fptsThis = new MatOfPoint2f ();
										mMOP2fptsPrev = new MatOfPoint2f ();
										mMOP2fptsSafe = new MatOfPoint2f ();
										mMOBStatus = new MatOfByte ();
										mMOFerr = new MatOfFloat ();
					
										texture = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

										gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

                                        updateLayout();

                                        screenOrientation = Screen.orientation;
										initDone = true;
					
										break;
								} else {
										yield return 0;
								}
						}
				}

                private void updateLayout()
                {
                    gameObject.transform.localRotation = new Quaternion(0, 0, 0, 0);
                    gameObject.transform.localScale = new Vector3(webCamTexture.width, webCamTexture.height, 1);

                    if (webCamTexture.videoRotationAngle == 90 || webCamTexture.videoRotationAngle == 270)
                    {
                        gameObject.transform.eulerAngles = new Vector3(0, 0, -90);
                    }


                    float width = 0;
                    float height = 0;
                    if (webCamTexture.videoRotationAngle == 90 || webCamTexture.videoRotationAngle == 270)
                    {
                        width = gameObject.transform.localScale.y;
                        height = gameObject.transform.localScale.x;
                    }
                    else if (webCamTexture.videoRotationAngle == 0 || webCamTexture.videoRotationAngle == 180)
                    {
                        width = gameObject.transform.localScale.x;
                        height = gameObject.transform.localScale.y;
                    }

                    float widthScale = (float)Screen.width / width;
                    float heightScale = (float)Screen.height / height;
                    if (widthScale < heightScale)
                    {
                        Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
                    }
                    else
                    {
                        Camera.main.orthographicSize = height / 2;
                    }
                }

				// Update is called once per frame
				void Update ()
				{
						if (!initDone)
								return;

                        if (screenOrientation != Screen.orientation)
                        {
                            screenOrientation = Screen.orientation;
                            updateLayout();
                        }

						#if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
				if (webCamTexture.width > 16 && webCamTexture.height > 16) {
						#else
						if (webCamTexture.didUpdateThisFrame) {
								#endif
		
								Utils.webCamTextureToMat (webCamTexture, rgbaMat, colors);

                                //flip to correct direction.
                                if (webCamDevice.isFrontFacing)
                                {
                                    if (webCamTexture.videoRotationAngle == 0)
                                    {
                                        Core.flip(rgbaMat, rgbaMat, 1);
                                    }
                                    else if (webCamTexture.videoRotationAngle == 90)
                                    {
                                        Core.flip(rgbaMat, rgbaMat, 0);
                                    }
                                    if (webCamTexture.videoRotationAngle == 180)
                                    {
                                        Core.flip(rgbaMat, rgbaMat, 0);
                                    }
                                    else if (webCamTexture.videoRotationAngle == 270)
                                    {
                                        Core.flip(rgbaMat, rgbaMat, 1);
                                    }
                                }
                                else
                                {
                                    if (webCamTexture.videoRotationAngle == 180)
                                    {
                                        Core.flip(rgbaMat, rgbaMat, -1);
                                    }
                                    else if (webCamTexture.videoRotationAngle == 270)
                                    {
                                        Core.flip(rgbaMat, rgbaMat, -1);
                                    }
                                }

								if (mMOP2fptsPrev.rows () == 0) {

										// first time through the loop so we need prev and this mats
										// plus prev points
										// get this mat
										Imgproc.cvtColor (rgbaMat, matOpFlowThis, Imgproc.COLOR_RGBA2GRAY);
					
										// copy that to prev mat
										matOpFlowThis.copyTo (matOpFlowPrev);
					
										// get prev corners
										Imgproc.goodFeaturesToTrack (matOpFlowPrev, MOPcorners, iGFFTMax, 0.05, 20);
										mMOP2fptsPrev.fromArray (MOPcorners.toArray ());
					
										// get safe copy of this corners
										mMOP2fptsPrev.copyTo (mMOP2fptsSafe);
								} else {
										// we've been through before so
										// this mat is valid. Copy it to prev mat
										matOpFlowThis.copyTo (matOpFlowPrev);
					
										// get this mat
										Imgproc.cvtColor (rgbaMat, matOpFlowThis, Imgproc.COLOR_RGBA2GRAY);
					
										// get the corners for this mat
										Imgproc.goodFeaturesToTrack (matOpFlowThis, MOPcorners, iGFFTMax, 0.05, 20);
										mMOP2fptsThis.fromArray (MOPcorners.toArray ());
					
										// retrieve the corners from the prev mat
										// (saves calculating them again)
										mMOP2fptsSafe.copyTo (mMOP2fptsPrev);
					
										// and save this corners for next time through
					
										mMOP2fptsThis.copyTo (mMOP2fptsSafe);
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
								Video.calcOpticalFlowPyrLK (matOpFlowPrev, matOpFlowThis, mMOP2fptsPrev, mMOP2fptsThis, mMOBStatus, mMOFerr);

								if (!mMOBStatus.empty ()) {
										List<Point> cornersPrev = mMOP2fptsPrev.toList ();
										List<Point> cornersThis = mMOP2fptsThis.toList ();
										List<byte> byteStatus = mMOBStatus.toList ();

										int x = 0;
										int y = byteStatus.Count - 1;
									
										for (x = 0; x < y; x++) {
												if (byteStatus [x] == 1) {
														Point pt = cornersThis [x];
														Point pt2 = cornersPrev [x];
						
														Core.circle (rgbaMat, pt, 5, colorRed, iLineThickness - 1);
						
														Core.line (rgbaMat, pt, pt2, colorRed, iLineThickness);
												}
										}
								}


		
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
		}
}