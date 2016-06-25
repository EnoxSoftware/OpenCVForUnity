using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnitySample
{
		/// <summary>
		/// VideoCapture sample.
		/// </summary>
		public class VideoCaptureSample : MonoBehaviour
		{

				/// <summary>
				/// The width of the frame.
				/// </summary>
				private double frameWidth = 768;

				/// <summary>
				/// The height of the frame.
				/// </summary>
				private double frameHeight = 576;

				/// <summary>
				/// The capture.
				/// </summary>
				VideoCapture capture;

				/// <summary>
				/// The rgb mat.
				/// </summary>
				Mat rgbMat;

				/// <summary>
				/// The texture.
				/// </summary>
				Texture2D texture;
		
				// Use this for initialization
				void Start ()
				{
			
						rgbMat = new Mat ();
			
						capture = new VideoCapture ();
						capture.open (Utils.getFilePath ("768x576_mjpeg.mjpeg"));
			
						if (capture.isOpened ()) {
								Debug.Log ("capture.isOpened() true");
						} else {
								Debug.Log ("capture.isOpened() false");
						}


						Debug.Log ("CAP_PROP_FORMAT: " + capture.get (Videoio.CAP_PROP_FORMAT));
						Debug.Log ("CV_CAP_PROP_PREVIEW_FORMAT: " + capture.get (Videoio.CV_CAP_PROP_PREVIEW_FORMAT));
						Debug.Log ("CAP_PROP_POS_MSEC: " + capture.get (Videoio.CAP_PROP_POS_MSEC));
						Debug.Log ("CAP_PROP_POS_FRAMES: " + capture.get (Videoio.CAP_PROP_POS_FRAMES));
						Debug.Log ("CAP_PROP_POS_AVI_RATIO: " + capture.get (Videoio.CAP_PROP_POS_AVI_RATIO));
						Debug.Log ("CAP_PROP_FRAME_COUNT: " + capture.get (Videoio.CAP_PROP_FRAME_COUNT));
						Debug.Log ("CAP_PROP_FPS: " + capture.get (Videoio.CAP_PROP_FPS));
						Debug.Log ("CAP_PROP_FRAME_WIDTH: " + capture.get (Videoio.CAP_PROP_FRAME_WIDTH));
						Debug.Log ("CAP_PROP_FRAME_HEIGHT: " + capture.get (Videoio.CAP_PROP_FRAME_HEIGHT));


						texture = new Texture2D ((int)(frameWidth), (int)(frameHeight), TextureFormat.RGBA32, false);
						gameObject.transform.localScale = new Vector3 ((float)frameWidth, (float)frameHeight, 1);
						float widthScale = (float)Screen.width / (float)frameWidth;
						float heightScale = (float)Screen.height / (float)frameHeight;
						if (widthScale < heightScale) {
								Camera.main.orthographicSize = ((float)frameWidth * (float)Screen.height / (float)Screen.width) / 2;
						} else {
								Camera.main.orthographicSize = (float)frameHeight / 2;
						}
			
						gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
			
				}
		
				// Update is called once per frame
				void Update ()
				{
						//Loop play
						if (capture.get (Videoio.CAP_PROP_POS_FRAMES) >= capture.get (Videoio.CAP_PROP_FRAME_COUNT))
								capture.set (Videoio.CAP_PROP_POS_FRAMES, 0);

						//error PlayerLoop called recursively! on iOS.reccomend WebCamTexture.
						if (capture.grab ()) {

								capture.retrieve (rgbMat, 0);

								Imgproc.cvtColor (rgbMat, rgbMat, Imgproc.COLOR_BGR2RGB);
				
								//Debug.Log ("Mat toString " + rgbMat.ToString ());
				
								Utils.matToTexture2D (rgbMat, texture);
				
								gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
				
						}
				}
		
				void OnDestroy ()
				{
						capture.release ();

						if (rgbMat != null)
								rgbMat.Dispose ();
				}
		
				public void OnBackButton ()
				{
						#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			SceneManager.LoadScene ("OpenCVForUnitySample");
						#else
						Application.LoadLevel ("OpenCVForUnitySample");
						#endif
				}
		}
	
}