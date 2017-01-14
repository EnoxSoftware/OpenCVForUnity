using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
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
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ShowLicense");
            #else
            Application.LoadLevel ("ShowLicense");
            #endif
        }

        public void OnTexture2DToMatSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("Texture2DToMatSample");
            #else
            Application.LoadLevel ("Texture2DToMatSample");
            #endif
        }

        public void OnThresholdSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ThresholdSample");
            #else
            Application.LoadLevel ("ThresholdSample");
            #endif
        }
        
        public void OnDrawingSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("DrawingSample");
            #else
            Application.LoadLevel ("DrawingSample");
            #endif
        }
        
        public void OnConvexHullSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ConvexHullSample");
            #else
            Application.LoadLevel ("ConvexHullSample");
            #endif
        }
        
        public void OnHoughLinesPSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("HoughLinesPSample");
            #else
            Application.LoadLevel ("HoughLinesPSample");
            #endif
        }
        
        public void OnFeature2DSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("Feature2DSample");
            #else
            Application.LoadLevel ("Feature2DSample");
            #endif
        }

        public void OnWrapPerspectiveSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WrapPerspectiveSample");
            #else
            Application.LoadLevel ("WrapPerspectiveSample");
            #endif
        }
        
        public void OnFaceRecognizerSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("FaceRecognizerSample");
            #else
            Application.LoadLevel ("FaceRecognizerSample");
            #endif
        }
        
        public void OnDetectFaceSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("DetectFaceSample");
            #else
            Application.LoadLevel ("DetectFaceSample");
            #endif
        }
                
        public void OnWebCamTextureToMatSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureToMatSample");
            #else
            Application.LoadLevel ("WebCamTextureToMatSample");
            #endif
        }

        public void OnWebCamTextureToMatHelperSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureToMatHelperSample");
            #else
            Application.LoadLevel ("WebCamTextureToMatHelperSample");
            #endif
        }
                
        public void OnWebCamTextureDetectFaceSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureDetectFaceSample");
            #else
            Application.LoadLevel ("WebCamTextureDetectFaceSample");
            #endif
        }
        
        public void OnWebCamTextureAsyncDetectFaceSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureAsyncDetectFaceSample");
            #else
            Application.LoadLevel ("WebCamTextureAsyncDetectFaceSample");
            #endif
        }
        
        public void OnOpticalFlowSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("OpticalFlowSample");
            #else
            Application.LoadLevel ("OpticalFlowSample");
            #endif
        }
        
        public void OnComicFilterSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ComicFilterSample");
            #else
            Application.LoadLevel ("ComicFilterSample");
            #endif
        }
        
        public void OnCamShiftSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("CamShiftSample");
            #else
            Application.LoadLevel ("CamShiftSample");
            #endif
        }

        public void OnHandPoseEstimationSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("HandPoseEstimationSample");
            #else
            Application.LoadLevel ("HandPoseEstimationSample");
            #endif
        }

        public void OnMultiObjectTrackingBasedOnColorSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("MultiObjectTrackingBasedOnColorSample");
            #else
            Application.LoadLevel ("MultiObjectTrackingBasedOnColorSample");
            #endif
        }

        public void OnPlotSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("PlotSample");
            #else
            Application.LoadLevel ("PlotSample");
            #endif
        }

        public void OnSimpleBlobSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("SimpleBlobSample");
            #else
            Application.LoadLevel ("SimpleBlobSample");
            #endif
        }

        public void OnBackgroundSubtractorMOG2Sample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("BackgroundSubtractorMOG2Sample");
            #else
            Application.LoadLevel ("BackgroundSubtractorMOG2Sample");
            #endif
        }

        public void OnVideoCaptureSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("VideoCaptureSample");
            #else
            Application.LoadLevel ("VideoCaptureSample");
            #endif
        }

        public void OnMatchTemplateSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("MatchTemplateSample");
            #else
            Application.LoadLevel ("MatchTemplateSample");
            #endif
        }

        public void OnStereoBMSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("StereoBMSample");
            #else
            Application.LoadLevel ("StereoBMSample");
            #endif
        }

        public void OnSeamlessCloneSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("SeamlessCloneSample");
            #else
            Application.LoadLevel ("SeamlessCloneSample");
            #endif
        }

        public void OnWebCamTextureDetectCirclesSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureDetectCirclesSample");
            #else
            Application.LoadLevel ("WebCamTextureDetectCirclesSample");
            #endif
        }

        public void OnSVMSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("SVMSample");
            #else
            Application.LoadLevel ("SVMSample");
            #endif
        }

        public void OnHOGDescriptorSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("HOGDescriptorSample");
            #else
            Application.LoadLevel ("HOGDescriptorSample");
            #endif
        }

        public void OnArUcoCreateMarkerSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ArUcoCreateMarkerSample");
            #else
            Application.LoadLevel ("ArUcoCreateMarkerSample");
            #endif
        }
        
        public void OnArUcoTexture2DSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ArUcoTexture2DSample");
            #else
            Application.LoadLevel ("ArUcoTexture2DSample");
            #endif
        }

        public void OnArUcoWebCamTextureSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ArUcoWebCamTextureSample");
            #else
            Application.LoadLevel ("ArUcoWebCamTextureSample");
            #endif
        }

        public void OnGrabCutSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("GrabCutSample");
            #else
            Application.LoadLevel ("GrabCutSample");
            #endif
        }

        public void OnInpaintSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("InpaintSample");
            #else
            Application.LoadLevel ("InpaintSample");
            #endif
        }
        
        public void OnMatchShapesSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("MatchShapesSample");
            #else
            Application.LoadLevel ("MatchShapesSample");
            #endif
        }
        
        public void OnMSERSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("MSERSample");
            #else
            Application.LoadLevel ("MSERSample");
            #endif
        }

        public void OnPolygonFilterSample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("PolygonFilterSample");
            #else
            Application.LoadLevel ("PolygonFilterSample");
            #endif
        }
    }
}