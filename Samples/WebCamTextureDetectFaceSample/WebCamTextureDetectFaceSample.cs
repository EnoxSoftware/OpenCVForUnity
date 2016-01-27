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

		/// <summary>
		/// The colors.
		/// </summary>
		Color32[] colors;

		/// <summary>
		/// The gray mat.
		/// </summary>
		Mat grayMat;

		/// <summary>
		/// The texture.
		/// </summary>
		Texture2D texture;

		/// <summary>
		/// The cascade.
		/// </summary>
		CascadeClassifier cascade;

		/// <summary>
		/// The faces.
		/// </summary>
		MatOfRect faces;

		/// <summary>
		/// The web cam texture to mat helper.
		/// </summary>
		WebCamTextureToMatHelper webCamTextureToMatHelper;


		// Use this for initialization
		void Start ()
		{
						
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

			grayMat = new Mat (webCamTextureMat.rows (), webCamTextureMat.cols (), CvType.CV_8UC1);
			cascade = new CascadeClassifier (Utils.getFilePath ("lbpcascade_frontalface.xml"));
			//cascade = new CascadeClassifier (Utils.getFilePath ("haarcascade_frontalface_alt.xml"));
			faces = new MatOfRect ();

			
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

			grayMat.Dispose ();
			cascade.Dispose ();
			faces.Dispose ();
		}

		// Update is called once per frame
		void Update ()
		{

			if (webCamTextureToMatHelper.isPlaying ()) {
				
				Mat rgbaMat = webCamTextureToMatHelper.GetMat ();

				Imgproc.cvtColor (rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
				Imgproc.equalizeHist (grayMat, grayMat);
				
				
				if (cascade != null)
					cascade.detectMultiScale (grayMat, faces, 1.1, 2, 2, // TODO: objdetect.CV_HAAR_SCALE_IMAGE
					                          new Size (grayMat.cols () * 0.2, grayMat.rows () * 0.2), new Size ());
				
				
				OpenCVForUnity.Rect[] rects = faces.toArray ();
				for (int i = 0; i < rects.Length; i++) {
					//				Debug.Log ("detect faces " + rects [i]);
					
					Core.rectangle (rgbaMat, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (255, 0, 0, 255), 2);
				}
				
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
	}
}
