using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace OpenCVForUnitySample
{
		/// <summary>
		/// WebCamTexture to mat sample.
		/// </summary>
		[RequireComponent(typeof(WebCamTextureToMatHelper))]
		public class WebCamTextureToMatSample : MonoBehaviour
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
				/// The web cam texture to mat helper.
				/// </summary>
				WebCamTextureToMatHelper webCamTextureToMatHelper;
	

				// Use this for initialization
				void Start ()
				{
						webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper> ();
						webCamTextureToMatHelper.Init ();
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

				}

				// Update is called once per frame
				void Update ()
				{

						if (webCamTextureToMatHelper.isPlaying () && webCamTextureToMatHelper.didUpdateThisFrame ()) {

								Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

								Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

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
						#if UNITY_5_3 || UNITY_5_3_OR_NEWER
			SceneManager.LoadScene ("OpenCVForUnitySample");
						#else
						Application.LoadLevel ("OpenCVForUnitySample");
						#endif
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
						webCamTextureToMatHelper.Init (null, webCamTextureToMatHelper.requestWidth, webCamTextureToMatHelper.requestHeight, !webCamTextureToMatHelper.requestIsFrontFacing);
				}
		}
}