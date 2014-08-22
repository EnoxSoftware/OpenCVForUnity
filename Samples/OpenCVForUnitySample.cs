using UnityEngine;
using System.Collections;

namespace OpenCVForUnitySample
{
		public class OpenCVForUnitySample : MonoBehaviour
		{

				// Use this for initialization
				void Start ()
				{
	
				}
	
				// Update is called once per frame
				void Update ()
				{
	
				}

				void OnGUI ()
				{
						float screenScale = Screen.width / 240.0f;
						Matrix4x4 scaledMatrix = Matrix4x4.Scale (new Vector3 (screenScale, screenScale, screenScale));
						GUI.matrix = scaledMatrix;


						GUILayout.BeginVertical ();
				
						if (GUILayout.Button ("Texture2DToMatSample")) {
								Application.LoadLevel ("Texture2DToMatSample");
						}

						if (GUILayout.Button ("ThresholdSample")) {
								Application.LoadLevel ("ThresholdSample");
						}

						if (GUILayout.Button ("DrawingSample")) {
								Application.LoadLevel ("DrawingSample");
						}

						if (GUILayout.Button ("ConvexHullSample")) {
								Application.LoadLevel ("ConvexHullSample");
						}

						if (GUILayout.Button ("HoughLinesPSample")) {
								Application.LoadLevel ("HoughLinesPSample");
						}

						if (GUILayout.Button ("Feature2DSample")) {
								Application.LoadLevel ("Feature2DSample");
						}

						if (GUILayout.Button ("DetectFaceSample")) {
								Application.LoadLevel ("DetectFaceSample");
						}
		
						if (GUILayout.Button ("WebCamTextureToMatSample")) {
								Application.LoadLevel ("WebCamTextureToMatSample");
						}
		
						if (GUILayout.Button ("WebCamTextureDetectFaceSample")) {
								Application.LoadLevel ("WebCamTextureDetectFaceSample");
						}

						if (GUILayout.Button ("ComicFilterSample")) {
								Application.LoadLevel ("ComicFilterSample");
						}

				

						GUILayout.EndVertical ();
				}
		}
}
		