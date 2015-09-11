using UnityEngine;
using System.Collections;

using OpenCVForUnity;

namespace OpenCVForUnitySample
{
		/// <summary>
		/// Threshold sample.
		/// </summary>
		public class ThresholdSample : MonoBehaviour
		{

				// Use this for initialization
				void Start ()
				{

						Texture2D imgTexture = Resources.Load ("chessboard") as Texture2D;
			
						Mat imgMat = new Mat (imgTexture.height, imgTexture.width, CvType.CV_8UC1);
			
						Utils.texture2DToMat (imgTexture, imgMat);
						Debug.Log ("imgMat dst ToString " + imgMat.ToString ());


						Imgproc.threshold (imgMat, imgMat, 0, 255, Imgproc.THRESH_BINARY | Imgproc.THRESH_OTSU);


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
