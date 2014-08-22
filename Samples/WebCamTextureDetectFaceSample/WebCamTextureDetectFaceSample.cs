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
				bool isFront = false;
				int width = 640;
				int height = 480;
				Mat rgbaMat;
				Mat grayMat;
//				Texture2D texture;
				CascadeClassifier cascade;
				MatOfRect faces;
				Size minSize;
				Size maxSize;
				public GameObject[] facesQuad;
				Texture2D faceTexture;
				bool initDone = false;

				// Use this for initialization
				void Start ()
				{
						// Checks how many and which cameras are available on the device
						for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {
							
								if (WebCamTexture.devices [cameraIndex].isFrontFacing == isFront) {
				
										Debug.Log (cameraIndex + " name " + WebCamTexture.devices [cameraIndex].name + " isFrontFacing " + WebCamTexture.devices [cameraIndex].isFrontFacing);
				
										//Set the appropriate fps
										webCamTexture = new WebCamTexture (WebCamTexture.devices [cameraIndex].name, width, height, 10);
	
										break;
								}
			
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
					
//										texture = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
					
//										gameObject.transform.eulerAngles = new Vector3 (0, 0, -90);
//										gameObject.transform.localScale = new Vector3 (webCamTexture.width, webCamTexture.height, 1);
										gameObject.transform.localPosition = new Vector3 (webCamTexture.width / 2, -webCamTexture.height / 2, 0);
										gameObject.transform.eulerAngles = new Vector3 (0, 0, 0);
										gameObject.transform.localScale = new Vector3 (webCamTexture.width, webCamTexture.height, 1);
					
					
										cascade = new CascadeClassifier (Utils.getFilePath ("haarcascade_frontalface_alt.xml"));
										faces = new MatOfRect ();
										minSize = new Size (60, 60);
										maxSize = new Size ();

//										gameObject.renderer.material.mainTexture = texture;
										gameObject.renderer.material.mainTexture = webCamTexture;


										//make faceTexture
										Mat faceMat = new Mat (100, 100, CvType.CV_8UC4, new Scalar (0, 0, 0, 0));
										Core.rectangle (faceMat, new Point (1, 1), new Point (100 - 2, 100 - 2), new Scalar (255, 0, 0, 127), 3);
										faceTexture = new Texture2D (faceMat.cols (), faceMat.rows (), TextureFormat.RGBA32, false);
										Utils.matToTexture2D (faceMat, faceTexture);
										for (int i = 0; i < facesQuad.Length; i++) {
												facesQuad [i].renderer.material.mainTexture = faceTexture;
										}

					
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
			                         minSize, maxSize);
		
								OpenCVForUnity.Rect[] rects = faces.toArray ();


								for (int i = 0; i < facesQuad.Length; i++) {
										facesQuad [i].SetActive (false);
								}


								for (int i = 0; i < rects.Length; i++) {
										//				Debug.Log ("detect faces " + rects [i]);
			
//										Core.rectangle (rgbaMat, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (255, 0, 0, 255), 2);

										if (i < facesQuad.Length) {
												facesQuad [i].transform.localPosition = new Vector3 (rects [i].x + rects [i].width / 2, -(rects [i].y + rects [i].height / 2), -1);
												facesQuad [i].transform.localScale = new Vector3 (rects [i].width, rects [i].height, 1);
												facesQuad [i].SetActive (true);
										}
								}

//								Utils.matToTexture2D (rgbaMat, texture);


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
