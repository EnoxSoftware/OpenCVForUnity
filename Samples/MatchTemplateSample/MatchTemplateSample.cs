using UnityEngine;
using System.Collections;

using OpenCVForUnity;

namespace OpenCVForUnitySample
{
		/// <summary>
		/// Match template sample.
		/// </summary>
		public class MatchTemplateSample : MonoBehaviour
		{
		
				// Use this for initialization
				void Start ()
				{

						Texture2D imgTexture = Resources.Load ("lena") as Texture2D;
						Texture2D tempTexture = Resources.Load ("template") as Texture2D;
						Mat imgMat = new Mat (imgTexture.height, imgTexture.width, CvType.CV_8UC4);
						Mat tempMat = new Mat (tempTexture.height, tempTexture.width, CvType.CV_8UC4);
						Utils.texture2DToMat (imgTexture, imgMat);
						Utils.texture2DToMat (tempTexture, tempMat);

			
						//Create the result mat
						int result_cols = imgMat.cols () - tempMat.cols () + 1;
						int result_rows = imgMat.rows () - tempMat.rows () + 1;
						Mat result = new Mat (result_rows, result_cols, CvType.CV_32FC1);
			
						int match_method = Imgproc.TM_CCOEFF_NORMED;
			

						Imgproc.matchTemplate (imgMat, tempMat, result, match_method);
			
						Imgproc.threshold (result, result, 0.8, 1.0, Imgproc.THRESH_TOZERO);//threshold = 0.8
			
						for (int i=0; i<result.rows(); i++) {
								for (int j=0; j<result.cols(); j++) {
										if (result.get (i, j) [0] > 0) {
						
												Imgproc.rectangle (imgMat, new Point (j, i), new Point (j + tempMat.cols (), i + tempMat.rows ()), new Scalar (255, 0, 0, 255), 2);
												Debug.Log ("value" + result.get (i, j) [0]);
										}
								}
						}

			
			
						Texture2D texture = new Texture2D (imgMat.cols (), imgMat.rows (), TextureFormat.RGBA32, false);
			
						Utils.matToTexture2D (imgMat, texture);
			
						gameObject.GetComponent<Renderer> ().material.mainTexture = texture;
			
				}
		
				// Update is called once per frame
				void Update ()
				{
			
				}
		
				public void OnBackButton ()
				{
						Application.LoadLevel ("OpenCVForUnitySample");
				}
		}
}

