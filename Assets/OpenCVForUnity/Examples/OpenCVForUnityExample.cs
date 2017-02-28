using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace OpenCVForUnityExample
{
    public class OpenCVForUnityExample : MonoBehaviour
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

        public void OnTexture2DToMatExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("Texture2DToMatExample");
            #else
            Application.LoadLevel ("Texture2DToMatExample");
            #endif
        }

        public void OnThresholdExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ThresholdExample");
            #else
            Application.LoadLevel ("ThresholdExample");
            #endif
        }
        
        public void OnDrawingExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("DrawingExample");
            #else
            Application.LoadLevel ("DrawingExample");
            #endif
        }
        
        public void OnConvexHullExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ConvexHullExample");
            #else
            Application.LoadLevel ("ConvexHullExample");
            #endif
        }
        
        public void OnHoughLinesPExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("HoughLinesPExample");
            #else
            Application.LoadLevel ("HoughLinesPExample");
            #endif
        }
        
        public void OnFeature2DExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("Feature2DExample");
            #else
            Application.LoadLevel ("Feature2DExample");
            #endif
        }

        public void OnWrapPerspectiveExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WrapPerspectiveExample");
            #else
            Application.LoadLevel ("WrapPerspectiveExample");
            #endif
        }
        
        public void OnFaceRecognizerExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("FaceRecognizerExample");
            #else
            Application.LoadLevel ("FaceRecognizerExample");
            #endif
        }
        
        public void OnDetectFaceExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("DetectFaceExample");
            #else
            Application.LoadLevel ("DetectFaceExample");
            #endif
        }
                
        public void OnWebCamTextureToMatExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureToMatExample");
            #else
            Application.LoadLevel ("WebCamTextureToMatExample");
            #endif
        }

        public void OnWebCamTextureToMatHelperExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureToMatHelperExample");
            #else
            Application.LoadLevel ("WebCamTextureToMatHelperExample");
            #endif
        }
                
        public void OnWebCamTextureDetectFaceExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureDetectFaceExample");
            #else
            Application.LoadLevel ("WebCamTextureDetectFaceExample");
            #endif
        }
        
        public void OnWebCamTextureAsyncDetectFaceExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureAsyncDetectFaceExample");
            #else
            Application.LoadLevel ("WebCamTextureAsyncDetectFaceExample");
            #endif
        }
        
        public void OnOpticalFlowExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("OpticalFlowExample");
            #else
            Application.LoadLevel ("OpticalFlowExample");
            #endif
        }
        
        public void OnComicFilterExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ComicFilterExample");
            #else
            Application.LoadLevel ("ComicFilterExample");
            #endif
        }
        
        public void OnCamShiftExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("CamShiftExample");
            #else
            Application.LoadLevel ("CamShiftExample");
            #endif
        }

        public void OnHandPoseEstimationExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("HandPoseEstimationExample");
            #else
            Application.LoadLevel ("HandPoseEstimationExample");
            #endif
        }

        public void OnMultiObjectTrackingBasedOnColorExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("MultiObjectTrackingBasedOnColorExample");
            #else
            Application.LoadLevel ("MultiObjectTrackingBasedOnColorExample");
            #endif
        }

        public void OnPlotExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("PlotExample");
            #else
            Application.LoadLevel ("PlotExample");
            #endif
        }

        public void OnSimpleBlobExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("SimpleBlobExample");
            #else
            Application.LoadLevel ("SimpleBlobExample");
            #endif
        }

        public void OnBackgroundSubtractorMOG2Example ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("BackgroundSubtractorMOG2Example");
            #else
            Application.LoadLevel ("BackgroundSubtractorMOG2Example");
            #endif
        }

        public void OnVideoCaptureExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("VideoCaptureExample");
            #else
            Application.LoadLevel ("VideoCaptureExample");
            #endif
        }

        public void OnMatchTemplateExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("MatchTemplateExample");
            #else
            Application.LoadLevel ("MatchTemplateExample");
            #endif
        }

        public void OnStereoBMExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("StereoBMExample");
            #else
            Application.LoadLevel ("StereoBMExample");
            #endif
        }

        public void OnSeamlessCloneExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("SeamlessCloneExample");
            #else
            Application.LoadLevel ("SeamlessCloneExample");
            #endif
        }

        public void OnWebCamTextureDetectCirclesExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureDetectCirclesExample");
            #else
            Application.LoadLevel ("WebCamTextureDetectCirclesExample");
            #endif
        }

        public void OnSVMExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("SVMExample");
            #else
            Application.LoadLevel ("SVMExample");
            #endif
        }

        public void OnHOGDescriptorExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("HOGDescriptorExample");
            #else
            Application.LoadLevel ("HOGDescriptorExample");
            #endif
        }

        public void OnArUcoCreateMarkerExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ArUcoCreateMarkerExample");
            #else
            Application.LoadLevel ("ArUcoCreateMarkerExample");
            #endif
        }
        
        public void OnArUcoTexture2DExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ArUcoTexture2DExample");
            #else
            Application.LoadLevel ("ArUcoTexture2DExample");
            #endif
        }

        public void OnArUcoWebCamTextureExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ArUcoWebCamTextureExample");
            #else
            Application.LoadLevel ("ArUcoWebCamTextureExample");
            #endif
        }

        public void OnGrabCutExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("GrabCutExample");
            #else
            Application.LoadLevel ("GrabCutExample");
            #endif
        }

        public void OnInpaintExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("InpaintExample");
            #else
            Application.LoadLevel ("InpaintExample");
            #endif
        }
        
        public void OnMatchShapesExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("MatchShapesExample");
            #else
            Application.LoadLevel ("MatchShapesExample");
            #endif
        }
        
        public void OnMSERExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("MSERExample");
            #else
            Application.LoadLevel ("MSERExample");
            #endif
        }

        public void OnPolygonFilterExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("PolygonFilterExample");
            #else
            Application.LoadLevel ("PolygonFilterExample");
            #endif
        }

        public void OnConnectedComponentsExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ConnectedComponentsExample");
            #else
            Application.LoadLevel ("ConnectedComponentsExample");
            #endif
        }

        public void OnGreenScreenExample ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("GreenScreenExample");
            #else
            Application.LoadLevel ("GreenScreenExample");
            #endif
        }
    }
}