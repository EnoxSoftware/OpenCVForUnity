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
	
				WebCamTexture webCamTexture;
				Color32[] colors;
				bool isFront = false;
				int width = 640;
				int height = 480;
				Mat rgbaMat;
				Texture2D texture;
				bool initDone = false;

	
				// Use this for initialization
				void Start ()
				{
						// Checks how many and which cameras are available on the device
						for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {

								if (WebCamTexture.devices [cameraIndex].isFrontFacing == isFront) {
				
										Debug.Log (cameraIndex + " name " + WebCamTexture.devices [cameraIndex].name + " isFrontFacing " + WebCamTexture.devices [cameraIndex].isFrontFacing);
				
										//Set the appropriate fps
										webCamTexture = new WebCamTexture (WebCamTexture.devices [cameraIndex].name, width, height, 30);
				
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
					
										texture = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
					
										gameObject.transform.eulerAngles = new Vector3 (0, 0, -90);
										gameObject.transform.localScale = new Vector3 (webCamTexture.width, webCamTexture.height, 1);


										gameObject.renderer.material.mainTexture = texture;
					
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

		
								Utils.matToTexture2D (rgbaMat, texture);
		
								gameObject.renderer.material.mainTexture = texture;


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