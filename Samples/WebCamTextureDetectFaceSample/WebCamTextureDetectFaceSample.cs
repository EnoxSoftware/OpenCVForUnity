using UnityEngine;
using System.Collections;

using OpenCVForUnity;

namespace OpenCVForUnitySample
{
		/// <summary>
		/// WebCamTexture detect face sample.
		/// </summary>
		public class WebCamTextureDetectFaceSample : MonoBehaviour
		{

				WebCamTexture webCamTexture;
				Color32[] colors;
		        #if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR
				bool isFront = false;
		        #endif
				int width = 640;
				int height = 480;
				Mat rgbaMat;
				Mat grayMat;
				Texture2D texture;
				CascadeClassifier cascade;
				MatOfRect faces;
				bool initDone = false;

				// Use this for initialization
				void Start ()
				{
						// Checks how many and which cameras are available on the device
						for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {
							
								#if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR
								if (WebCamTexture.devices [cameraIndex].isFrontFacing == isFront) {
								#endif
				
								Debug.Log (cameraIndex + " name " + WebCamTexture.devices [cameraIndex].name + " isFrontFacing " + WebCamTexture.devices [cameraIndex].isFrontFacing);
				
								//Set the appropriate fps
								webCamTexture = new WebCamTexture (WebCamTexture.devices [cameraIndex].name, width, height, 3);
	
								#if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR
										break;
								}
								#endif
			
						}

						Debug.Log ("width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);


						// Starts the camera
						webCamTexture.Play ();


						StartCoroutine (init ());

						
				}

				private IEnumerator init ()
				{
						while (true) {
								//If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/)
								if (webCamTexture.didUpdateThisFrame) {
										Debug.Log ("width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);
					
										colors = new Color32[webCamTexture.width * webCamTexture.height];
					
										rgbaMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
										grayMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
					
										texture = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
					
										gameObject.transform.eulerAngles = new Vector3 (0, 0, -90);
										gameObject.transform.localScale = new Vector3 (webCamTexture.width, webCamTexture.height, 1);
					
					
										cascade = new CascadeClassifier (Utils.getFilePath ("haarcascade_frontalface_alt.xml"));
										faces = new MatOfRect ();

										gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
					
										initDone = true;
					
										break;
								} else {
										yield return 0;
								}
						}
				}
	
				// Update is called once per frame
				void Update ()
				{
						if (!initDone)
								return;
	
						if (webCamTexture.didUpdateThisFrame) {

								Utils.WebCamTextureToMat (webCamTexture, rgbaMat, colors);



								Imgproc.cvtColor (rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
								Imgproc.equalizeHist (grayMat, grayMat);

								if (cascade != null)
										cascade.detectMultiScale (grayMat, faces, 1.1, 2, 2, // TODO: objdetect.CV_HAAR_SCALE_IMAGE
			                          new Size (50, 50), new Size ());
		
								OpenCVForUnity.Rect[] rects = faces.toArray ();
								for (int i = 0; i < rects.Length; i++) {
										//				Debug.Log ("detect faces " + rects [i]);
			
										Core.rectangle (rgbaMat, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (255, 0, 0, 255), 2);
								}

								Utils.matToTexture2D (rgbaMat, texture);


						}
				}

				void OnDisable ()
				{
						webCamTexture.Stop ();
				}

				void OnGUI ()
				{
						float screenScale = Screen.width / 240.0f;
						Matrix4x4 scaledMatrix = Matrix4x4.Scale (new Vector3 (screenScale, screenScale, screenScale));
						GUI.matrix = scaledMatrix;
		
		
						GUILayout.BeginVertical ();
						if (GUILayout.Button ("back")) {
								Application.LoadLevel ("OpenCVForUnitySample");
						}
		
		
						GUILayout.EndVertical ();
				}
		}
}
