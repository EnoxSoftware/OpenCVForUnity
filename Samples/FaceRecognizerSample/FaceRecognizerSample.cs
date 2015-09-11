using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using OpenCVForUnity;

namespace OpenCVForUnitySample
{
		/// <summary>
		/// FaceRecognizer sample.
		/// http://docs.opencv.org/modules/contrib/doc/facerec/facerec_tutorial.html#eigenfaces
		/// </summary>
		public class FaceRecognizerSample : MonoBehaviour
		{

				// Use this for initialization
				void Start ()
				{

						List<Mat> images = new List<Mat> ();
						List<int> labelsList = new List<int> ();
						MatOfInt labels = new MatOfInt ();
						images.Add (Highgui.imread (Utils.getFilePath ("facerec/facerec_0.bmp"), 0));
						images.Add (Highgui.imread (Utils.getFilePath ("facerec/facerec_1.bmp"), 0));
						labelsList.Add (0);
						labelsList.Add (0);
						labels.fromList (labelsList);

						Mat testSampleMat = Highgui.imread (Utils.getFilePath ("facerec/facerec_sample.bmp"), 0);
						int testSampleLabel = 0;


//						foreach (Mat item in images) {
//								Debug.Log ("images.ToString " + item.ToString ());
//						}
//						foreach (int item in labelsList) {
//								Debug.Log ("labels.ToString " + item.ToString ());
//						}

						int[] predictedLabel = new int[1];
						double[] predictedConfidence = new double[1];

			
						FaceRecognizer faceRecognizer = FaceRecognizer.createEigenFaceRecognizer ();

						faceRecognizer.train (images, labels);

						faceRecognizer.predict (testSampleMat, predictedLabel, predictedConfidence);


						Debug.Log ("Predicted class: " + predictedLabel [0] + " / " + "Actual class: " + testSampleLabel);
						Debug.Log ("Confidence: " + predictedConfidence [0]);


						Mat predictedMat = images [predictedLabel[0]];

						Mat baseMat = new Mat (testSampleMat.rows (), predictedMat.cols () + testSampleMat.cols (), CvType.CV_8UC1);
						predictedMat.copyTo (baseMat.submat (new OpenCVForUnity.Rect (0, 0, predictedMat.cols (), predictedMat.rows ())));
						testSampleMat.copyTo (baseMat.submat (new OpenCVForUnity.Rect (predictedMat.cols (), 0, testSampleMat.cols (), testSampleMat.rows ())));

						Core.putText (baseMat, "Predicted", new Point (10, baseMat.rows () - 5), Core.FONT_HERSHEY_SIMPLEX, 0.4, new Scalar (255), 1, Core.LINE_AA, false);
						Core.putText (baseMat, "TestSample", new Point (predictedMat.cols () + 10, baseMat.rows () - 5), Core.FONT_HERSHEY_SIMPLEX, 0.4, new Scalar (255), 1, Core.LINE_AA, false);

			
						Texture2D texture = new Texture2D (baseMat.cols (), baseMat.rows (), TextureFormat.RGBA32, false);

						Utils.matToTexture2D (baseMat, texture);

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
