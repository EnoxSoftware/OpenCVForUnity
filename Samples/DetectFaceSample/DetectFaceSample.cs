using UnityEngine;
using System.Collections;

using OpenCVForUnity;

namespace OpenCVForUnitySample
{
		/// <summary>
		/// DetectFace sample.
		/// </summary>
		public class DetectFaceSample : MonoBehaviour
		{

				// Use this for initialization
				void Start ()
				{
	
						Texture2D imgTexture = Resources.Load ("lena") as Texture2D;
			
						Mat imgMat = new Mat (imgTexture.height, imgTexture.width, CvType.CV_8UC4);
			
						Utils.texture2DToMat (imgTexture, imgMat);
						Debug.Log ("imgMat dst ToString " + imgMat.ToString ());


						//CascadeClassifier cascade = new CascadeClassifier (Utils.getFilePath ("lbpcascade_frontalface.xml"));
						CascadeClassifier cascade = new CascadeClassifier (Utils.getFilePath ("haarcascade_frontalface_alt.xml"));


						Mat grayMat = new Mat ();
						Imgproc.cvtColor (imgMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
						Imgproc.equalizeHist (grayMat, grayMat);


						MatOfRect faces = new MatOfRect ();
		
						if (cascade != null)
								cascade.detectMultiScale (grayMat, faces, 1.1, 2, 2, 
			                               new Size (20, 20), new Size ());

						OpenCVForUnity.Rect[] rects = faces.toArray ();
						for (int i = 0; i < rects.Length; i++) {
								Debug.Log ("detect faces " + rects [i]);

								Core.rectangle (imgMat, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (255, 0, 0, 255), 2);
						}



						Texture2D texture = new Texture2D (imgMat.cols (), imgMat.rows (), TextureFormat.RGBA32, false);

						Utils.matToTexture2D (imgMat, texture);

						gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
	

				}
	
				// Update is called once per frame
				void Update ()
				{

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
		
		
						GUILayout.EndVertical ();
				}
		}
}
