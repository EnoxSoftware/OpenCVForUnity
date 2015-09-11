using UnityEngine;
using System.Collections;

using OpenCVForUnity;

namespace OpenCVForUnitySample
{
		/// <summary>
		/// WebCamTexture to mat sample.
		/// </summary>
		public class WebCamTextureToMatSample : MonoBehaviour
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
                                        texture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

                                        gameObject.GetComponent<Renderer>().material.mainTexture = texture;

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
								
		
								Utils.matToTexture2D (rgbaMat, texture, colors);
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