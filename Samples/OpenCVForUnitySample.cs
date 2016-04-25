using UnityEngine;
using System.Collections;

#if UNITY_5_3
using UnityEngine.SceneManagement;
#endif

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

				public void OnShowLicenseButton ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("ShowLicense");
#else
						Application.LoadLevel ("ShowLicense");
#endif
				}

				public void OnTexture2DToMatSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("Texture2DToMatSample");
						#else
						Application.LoadLevel ("Texture2DToMatSample");
						#endif
				}

				public void OnThresholdSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("ThresholdSample");
						#else
						Application.LoadLevel ("ThresholdSample");
						#endif
				}
		
				public void OnDrawingSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("DrawingSample");
						#else
						Application.LoadLevel ("DrawingSample");
						#endif
				}
		
				public void OnConvexHullSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("ConvexHullSample");
						#else
						Application.LoadLevel ("ConvexHullSample");
						#endif
				}
		
				public void OnHoughLinesPSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("HoughLinesPSample");
						#else
						Application.LoadLevel ("HoughLinesPSample");
						#endif
				}
		
				public void OnFeature2DSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("Feature2DSample");
						#else
						Application.LoadLevel ("Feature2DSample");
						#endif
				}

				public void OnWrapPerspectiveSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("WrapPerspectiveSample");
						#else
						Application.LoadLevel ("WrapPerspectiveSample");
						#endif
				}
		
				public void OnFaceRecognizerSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("FaceRecognizerSample");
						#else
						Application.LoadLevel ("FaceRecognizerSample");
						#endif
				}
		
				public void OnDetectFaceSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("DetectFaceSample");
						#else
						Application.LoadLevel ("DetectFaceSample");
						#endif
				}
				
				public void OnWebCamTextureToMatSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("WebCamTextureToMatSample");
						#else
						Application.LoadLevel ("WebCamTextureToMatSample");
						#endif
				}
				
				public void OnWebCamTextureDetectFaceSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("WebCamTextureDetectFaceSample");
						#else
						Application.LoadLevel ("WebCamTextureDetectFaceSample");
						#endif
				}
		
				public void OnWebCamTextureAsyncDetectFaceSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("WebCamTextureAsyncDetectFaceSample");
						#else
						Application.LoadLevel ("WebCamTextureAsyncDetectFaceSample");
						#endif
				}
		
				public void OnOpticalFlowSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("OpticalFlowSample");
						#else
						Application.LoadLevel ("OpticalFlowSample");
						#endif
				}
		
				public void OnComicFilterSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("ComicFilterSample");
						#else
						Application.LoadLevel ("ComicFilterSample");
						#endif
				}
		
				public void OnCamShiftSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("CamShiftSample");
						#else
						Application.LoadLevel ("CamShiftSample");
						#endif
				}

				public void OnHandPoseEstimationSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("HandPoseEstimationSample");
						#else
						Application.LoadLevel ("HandPoseEstimationSample");
						#endif
				}

				public void OnMultiObjectTrackingBasedOnColorSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("MultiObjectTrackingBasedOnColorSample");
						#else
						Application.LoadLevel ("MultiObjectTrackingBasedOnColorSample");
						#endif
				}

				public void OnPlotSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("PlotSample");
						#else
						Application.LoadLevel ("PlotSample");
						#endif
				}

				public void OnSimpleBlobSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("SimpleBlobSample");
						#else
						Application.LoadLevel ("SimpleBlobSample");
						#endif
				}

				public void OnBackgroundSubtractorMOG2Sample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("BackgroundSubtractorMOG2Sample");
						#else
						Application.LoadLevel ("BackgroundSubtractorMOG2Sample");
						#endif
				}

				public void OnVideoCaptureSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("VideoCaptureSample");
						#else
						Application.LoadLevel ("VideoCaptureSample");
						#endif
				}

				public void OnMatchTemplateSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("MatchTemplateSample");
						#else
						Application.LoadLevel ("MatchTemplateSample");
						#endif
				}

				public void OnStereoBMSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("StereoBMSample");
						#else
						Application.LoadLevel ("StereoBMSample");
						#endif
				}

				public void OnSeamlessCloneSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("SeamlessCloneSample");
						#else
						Application.LoadLevel ("SeamlessCloneSample");
						#endif
				}

				public void OnWebCamTextureDetectCirclesSample ()
				{
						#if UNITY_5_3
			SceneManager.LoadScene ("WebCamTextureDetectCirclesSample");
						#else
						Application.LoadLevel ("WebCamTextureDetectCirclesSample");
						#endif

				}
		}
}
		