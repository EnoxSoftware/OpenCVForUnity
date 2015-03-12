using UnityEngine;
using System.Collections;

using OpenCVForUnity;

/// <summary>
/// ComicFilter sample.
/// referring to the http://dev.classmethod.jp/smartphone/opencv-manga-2/.
/// </summary>
public class ComicFilterSample : MonoBehaviour
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
		Mat lineMat;
		Mat maskMat;
		Mat bgMat;
		Mat dstMat;
		byte[] grayPixels;
		byte[] maskPixels;
		Texture2D texture;
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
						webCamTexture = new WebCamTexture (WebCamTexture.devices [cameraIndex].name, width, height, 10);

						#if (UNITY_ANDROID || UNITY_IPHONE) && !UNITY_EDITOR
								break;
						}
						#endif
			
				}
		
		
		
				// Starts the camera
				webCamTexture.Play ();


				Debug.Log ("width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);



				StartCoroutine (init ());


		}

		private IEnumerator init ()
		{
				while (true) {

						//If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/)
						if (webCamTexture.width > 16 && webCamTexture.height > 16) {
								Debug.Log ("width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);
				
								colors = new Color32[webCamTexture.width * webCamTexture.height];
				
								rgbaMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
								grayMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
								lineMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
								maskMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);

								//create a striped background.
								bgMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1, new Scalar (255));
								for (int i = 0; i < bgMat.rows ()*2.5f; i=i+4) {
										Core.line (bgMat, new Point (0, 0 + i), new Point (bgMat.cols (), -bgMat.cols () + i), new Scalar (0), 1);
								}
				
								dstMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC1);
				
								grayPixels = new byte[grayMat.cols () * grayMat.rows () * grayMat.channels ()];
								maskPixels = new byte[maskMat.cols () * maskMat.rows () * maskMat.channels ()];
				
								texture = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
				
								gameObject.transform.eulerAngles = new Vector3 (0, 0, -90);
								gameObject.transform.localScale = new Vector3 (webCamTexture.width, webCamTexture.height, 1);

								bool _videoVerticallyMirrored = webCamTexture.videoVerticallyMirrored;
								float scaleX = 1;
								float scaleY = _videoVerticallyMirrored ? -1.0f : 1.0f;
								gameObject.transform.localScale = new Vector3 (scaleX * gameObject.transform.localScale.x, scaleY * gameObject.transform.localScale.y, 1);


								gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

								Camera.main.orthographicSize = webCamTexture.width / 2;

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

				if (webCamTexture.width > 16 && webCamTexture.height > 16) {
		
						Utils.webCamTextureToMat (webCamTexture, rgbaMat, colors);
						Imgproc.cvtColor (rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);

//						Utils.webCamTextureToMat (webCamTexture, grayMat, colors);

				
						bgMat.copyTo (dstMat);


						Imgproc.GaussianBlur (grayMat, lineMat, new Size (3, 3), 0);
				



						grayMat.get (0, 0, grayPixels);

						for (int i = 0; i < grayPixels.Length; i++) {

								maskPixels [i] = 0;
			
								if (grayPixels [i] < 70) {
										grayPixels [i] = 0;

										maskPixels [i] = 1;
								} else if (70 <= grayPixels [i] && grayPixels [i] < 120) {
										grayPixels [i] = 100;

								
								} else {
										grayPixels [i] = 255;

										maskPixels [i] = 1;
								}
						}
		
						grayMat.put (0, 0, grayPixels);
	
						maskMat.put (0, 0, maskPixels);

						grayMat.copyTo (dstMat, maskMat);




				
						Imgproc.Canny (lineMat, lineMat, 20, 120);
		
						lineMat.copyTo (maskMat);
		
						Core.bitwise_not (lineMat, lineMat);

						lineMat.copyTo (dstMat, maskMat);




//		Imgproc.cvtColor(dstMat,rgbaMat,Imgproc.COLOR_GRAY2RGBA);
//				Utils.matToTexture2D (rgbaMat, texture);

						Utils.matToTexture2D (dstMat, texture, colors);
		
				

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
