using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// OpenCVForUnity Example
    /// The main menu scene that lists all sample scenes and displays OpenCVForUnity and Unity version information.
    /// Disables example buttons that are not supported on the current platform or graphics device.
    /// </summary>
    public class OpenCVForUnityExample : MonoBehaviour
    {
        // Constants
#if UNITY_6000_5_OR_NEWER
        [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
        private static float _verticalNormalizedPosition = 1f;

        // Public Fields
        public Text VersionInfo;
        public ScrollRect ScrollRect;

        // Unity Lifecycle Methods
        private void Start()
        {
            VersionInfo.text = Core.NATIVE_LIBRARY_NAME + " " + OpenCVForUnityEnv.GetVersion() + " (" + Core.VERSION + ")";
            VersionInfo.text += " / UnityEditor " + Application.unityVersion;
            VersionInfo.text += " / ";

#if UNITY_EDITOR
            VersionInfo.text += "Editor";
#elif UNITY_STANDALONE_WIN
            VersionInfo.text += "Windows";
#elif UNITY_STANDALONE_OSX
            VersionInfo.text += "Mac OSX";
#elif UNITY_STANDALONE_LINUX
            VersionInfo.text += "Linux";
#elif UNITY_ANDROID
            VersionInfo.text += "Android";
#elif UNITY_IOS
            VersionInfo.text += "iOS";
#elif UNITY_VISIONOS
            VersionInfo.text += "VisionOS";
#elif UNITY_WSA
            VersionInfo.text += "WSA";
#elif UNITY_WEBGL
            VersionInfo.text += "WebGL";
#endif
            VersionInfo.text += " ";
#if ENABLE_MONO
            VersionInfo.text += "Mono";
#elif ENABLE_IL2CPP
            VersionInfo.text += "IL2CPP";
#elif ENABLE_DOTNET
            VersionInfo.text += ".NET";
#endif

            ScrollRect.verticalNormalizedPosition = _verticalNormalizedPosition;

#if UNITY_WSA_10_0
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/AdvancedGroup/MultiObjectTrackingExampleButton").GetComponent<Button>().interactable = false;

            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/BarcodeDetectorExampleButton").GetComponent<Button>().interactable = false;

            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FaceDetectorYNExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FaceRecognizerSFExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FaceIdentificationEstimatorExampleButton").GetComponent<Button>().interactable = false;

            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ColorizationExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ObjectTrackingDaSiamRPNExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FastNeuralStyleTransferExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FaceDetectionYuNetExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FaceDetectionYuNetV2ExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FacialExpressionRecognitionExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/MediaPipeFaceLandmarkerExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/MediaPipeHandLandmarkerExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/MediaPipePoseLandmarkerExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/MediaPipeHolisticLandmarkerExampleButton").GetComponent<Button>().interactable = false;

            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ObjectDetectionDAMOYOLOExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ObjectDetectionYOLOXExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ObjectDetectionNanoDetPlusExampleButton").GetComponent<Button>().interactable = false;

            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/YOLOv5ObjectDetectionExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/YOLOv5InstanceSegmentationExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/YOLOv5ImageClassificationExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/YOLOv8ObjectDetectionExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/YOLOv8InstanceSegmentationExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/YOLOv8ImageClassificationExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/YOLOv8PoseEstimationExampleButton").GetComponent<Button>().interactable = false;

            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/TextRecognitionCRNNExampleButton").GetComponent<Button>().interactable = false;

            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/ContribModulesGroup/TextDetectionExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/ContribModulesGroup/TextRecognitionExampleButton").GetComponent<Button>().interactable = false;

            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/ContribModulesGroup/WeChatQRCodeDetectorExampleButton").GetComponent<Button>().interactable = false;
#endif

#if UNITY_6000_0_OR_NEWER
            // WebCamTextureToMatExample does not work on WebGPU (no helper AutoGPU path).
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.WebGPU)
            {
                GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/BasicGroup/WebCamTextureToMatExampleButton").GetComponent<Button>().interactable = false;
            }
#endif

#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN && !UNITY_STANDALONE_OSX && !UNITY_LINUX && !UNITY_IOS && !UNITY_ANDROID
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/VideoCaptureCameraInputExampleButton").GetComponent<Button>().interactable = false;
#endif

            // for Demo Build
            // GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/YOLOv5ObjectDetectionExampleButton").GetComponent<Button>().interactable = false;
            // GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/YOLOv5InstanceSegmentationExampleButton").GetComponent<Button>().interactable = false;
            // GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/YOLOv5ImageClassificationExampleButton").GetComponent<Button>().interactable = false;
            // GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/YOLOv8ObjectDetectionExampleButton").GetComponent<Button>().interactable = false;
            // GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/YOLOv8InstanceSegmentationExampleButton").GetComponent<Button>().interactable = false;
            // GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/YOLOv8ImageClassificationExampleButton").GetComponent<Button>().interactable = false;
            // GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/YOLOv8PoseEstimationExampleButton").GetComponent<Button>().interactable = false;

            // for WebGL Demo Build
            //GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ColorizationExampleButton").GetComponent<Button>().interactable = false;
        }

        private void Update()
        {

        }

        // Public Methods
        public void OnScrollRectValueChanged()
        {
            _verticalNormalizedPosition = ScrollRect.verticalNormalizedPosition;
        }

        public void OnShowSystemInfoButtonClick()
        {
            SceneManager.LoadScene("ShowSystemInfo");
        }

        public void OnShowLicenseButtonClick()
        {
            SceneManager.LoadScene("ShowLicense");
        }

        #region Basic

        public void OnTexture2DToMatExampleButtonClick()
        {
            SceneManager.LoadScene("Texture2DToMatExample");
        }

        public void OnWebCamTextureToMatExampleButtonClick()
        {
            SceneManager.LoadScene("WebCamTextureToMatExample");
        }

        public void OnWebCamTextureToMatHelperExampleButtonClick()
        {
            SceneManager.LoadScene("WebCamTextureToMatHelperExample");
        }

        public void OnMultiSourceToMatHelperExampleButtonClick()
        {
            SceneManager.LoadScene("MultiSourceToMatHelperExample");
        }

        public void OnMatBasicProcessingExampleButtonClick()
        {
            SceneManager.LoadScene("MatBasicProcessingExample");
        }

        public void OnUtils_GetFilePathExampleButtonClick()
        {
            SceneManager.LoadScene("Utils_GetFilePathExample");
        }

        public void OnDebugMatExampleButtonClick()
        {
            SceneManager.LoadScene("DebugMatExample");
        }

        #endregion

        #region Advanced

        public void OnImageCorrectionExampleButtonClick()
        {
            SceneManager.LoadScene("ImageCorrectionExample");
        }

        public void OnComicFilterExampleButtonClick()
        {
            SceneManager.LoadScene("ComicFilterExample");
        }

        public void OnDocumentScannerExampleButtonClick()
        {
            SceneManager.LoadScene("DocumentScannerExample");
        }

        public void OnPhysicalGreenScreenExampleButtonClick()
        {
            SceneManager.LoadScene("PhysicalGreenScreenExample");
        }

        public void OnKeyFrameGreenScreenExampleButtonClick()
        {
            SceneManager.LoadScene("KeyFrameGreenScreenExample");
        }

        public void OnBallTrackingBasedOnColorExampleButtonClick()
        {
            SceneManager.LoadScene("BallTrackingBasedOnColorExample");
        }

        public void OnCountFingersExampleButtonClick()
        {
            SceneManager.LoadScene("CountFingersExample");
        }

        public void OnMultiObjectTrackingBasedOnColorExampleButtonClick()
        {
            SceneManager.LoadScene("MultiObjectTrackingBasedOnColorExample");
        }

        public void OnMultiObjectTrackingExampleButtonClick()
        {
            SceneManager.LoadScene("MultiObjectTrackingExample");
        }

        public void OnPolygonFilterExampleButtonClick()
        {
            SceneManager.LoadScene("PolygonFilterExample");
        }

        public void OnAlphaBlendingExampleButtonClick()
        {
            SceneManager.LoadScene("AlphaBlendingExample");
        }

        #endregion

        #region Main modules

        #region core

        public void OnKMeansClusteringExampleButtonClick()
        {
            SceneManager.LoadScene("KMeansClusteringExample");
        }

        public void OnPCAExampleButtonClick()
        {
            SceneManager.LoadScene("PCAExample");
        }

        #endregion

        #region imgproc

        public void OnCircleDetectionExampleButtonClick()
        {
            SceneManager.LoadScene("CircleDetectionExample");
        }

        public void OnConnectedComponentsExampleButtonClick()
        {
            SceneManager.LoadScene("ConnectedComponentsExample");
        }

        public void OnDrawingExampleButtonClick()
        {
            SceneManager.LoadScene("DrawingExample");
        }

        public void OnGrabCutExampleButtonClick()
        {
            SceneManager.LoadScene("GrabCutExample");
        }

        public void OnHoughLinesPExampleButtonClick()
        {
            SceneManager.LoadScene("HoughLinesPExample");
        }

        public void OnMatchTemplateExampleButtonClick()
        {
            SceneManager.LoadScene("MatchTemplateExample");
        }

        public void OnThresholdExampleButtonClick()
        {
            SceneManager.LoadScene("ThresholdExample");
        }

        public void OnWrapPerspectiveExampleButtonClick()
        {
            SceneManager.LoadScene("WrapPerspectiveExample");
        }

        #endregion

        #region geometry

        public void OnConvexHullExampleButtonClick()
        {
            SceneManager.LoadScene("ConvexHullExample");
        }

        public void OnMatchShapesExampleButtonClick()
        {
            SceneManager.LoadScene("MatchShapesExample");
        }

        #endregion

        #region videoio

        public void OnVideoCaptureExampleButtonClick()
        {
            SceneManager.LoadScene("VideoCaptureExample");
        }

        public void OnVideoCaptureCameraInputExampleButtonClick()
        {
            SceneManager.LoadScene("VideoCaptureCameraInputExample");
        }

        public void OnVideoWriterExampleButtonClick()
        {
            SceneManager.LoadScene("VideoWriterExample");
        }

        public void OnVideoWriterAsyncExampleButtonClick()
        {
            SceneManager.LoadScene("VideoWriterAsyncExample");
        }

        #endregion

        #region video

        public void OnCamShiftExampleButtonClick()
        {
            SceneManager.LoadScene("CamShiftExample");
        }

        public void OnKalmanFilterExampleButtonClick()
        {
            SceneManager.LoadScene("KalmanFilterExample");
        }

        public void OnOpticalFlowExampleButtonClick()
        {
            SceneManager.LoadScene("OpticalFlowExample");
        }

        public void OnTransformECCExampleButtonClick()
        {
            SceneManager.LoadScene("TransformECCExample");
        }

        #endregion

        #region stereo

        public void OnStereoBMExampleButtonClick()
        {
            SceneManager.LoadScene("StereoBMExample");
        }

        #endregion

        #region features

        public void OnFeatureMatchingExampleButtonClick()
        {
            SceneManager.LoadScene("FeatureMatchingExample");
        }

        public void OnHomographyToFindAKnownObjectExampleButtonClick()
        {
            SceneManager.LoadScene("HomographyToFindAKnownObjectExample");
        }

        public void OnMSERExampleButtonClick()
        {
            SceneManager.LoadScene("MSERExample");
        }

        public void OnSimpleBlobExampleButtonClick()
        {
            SceneManager.LoadScene("SimpleBlobExample");
        }

        #endregion

        #region imgcodecs

        public void OnImwriteScreenCaptureExampleButtonClick()
        {
            SceneManager.LoadScene("ImwriteScreenCaptureExample");
        }

        #endregion

        #region objdetect

        public void OnArUcoImageExampleButtonClick()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                SceneManager.LoadScene("ArUcoImageExample_Built-in");
            }
            else
            {
                SceneManager.LoadScene("ArUcoImageExample_SRP");
            }
        }

        public void OnArUcoExampleButtonClick()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                SceneManager.LoadScene("ArUcoExample_Built-in");
            }
            else
            {
                SceneManager.LoadScene("ArUcoExample_SRP");
            }
        }

        public void OnArUcoCreateMarkerExampleButtonClick()
        {
            SceneManager.LoadScene("ArUcoCreateMarkerExample");
        }

        public void OnArUcoCameraCalibrationExampleButtonClick()
        {
            SceneManager.LoadScene("ArUcoCameraCalibrationExample");
        }

        public void OnBarcodeDetectorExampleButtonClick()
        {
            SceneManager.LoadScene("BarcodeDetectorExample");
        }

        public void OnFaceDetectionImageExampleButtonClick()
        {
            SceneManager.LoadScene("FaceDetectionImageExample");
        }

        public void OnFaceDetectionExampleButtonClick()
        {
            SceneManager.LoadScene("FaceDetectionExample");
        }

        public void OnAsynchronousFaceDetectionExampleButtonClick()
        {
            SceneManager.LoadScene("AsynchronousFaceDetectionExample");
        }

        public void OnFaceDetectorYNExampleButtonClick()
        {
            SceneManager.LoadScene("FaceDetectorYNExample");
        }

        public void OnFaceRecognizerSFExampleButtonClick()
        {
            SceneManager.LoadScene("FaceRecognizerSFExample");
        }

        public void OnFaceIdentificationEstimatorExampleButtonClick()
        {
            SceneManager.LoadScene("FaceIdentificationEstimatorExample");
        }

        public void OnHOGDescriptorExampleButtonClick()
        {
            SceneManager.LoadScene("HOGDescriptorExample");
        }

        public void OnQRCodeDetectorExampleButtonClick()
        {
            SceneManager.LoadScene("QRCodeDetectorExample");
        }

        public void OnQRCodeEncoderExampleButtonClick()
        {
            SceneManager.LoadScene("QRCodeEncoderExample");
        }

        #endregion

        #region dnn

        public void OnColorizationExampleButtonClick()
        {
            SceneManager.LoadScene("ColorizationExample");
        }

        public void OnObjectTrackingDaSiamRPNExampleButtonClick()
        {
            SceneManager.LoadScene("ObjectTrackingDaSiamRPNExample");
        }

        public void OnFastNeuralStyleTransferExampleButtonClick()
        {
            SceneManager.LoadScene("FastNeuralStyleTransferExample");
        }

        public void OnFaceDetectionYuNetExampleButtonClick()
        {
            SceneManager.LoadScene("FaceDetectionYuNetExample");
        }

        public void OnFaceDetectionYuNetV2ExampleButtonClick()
        {
            SceneManager.LoadScene("FaceDetectionYuNetV2Example");
        }

        public void OnFacialExpressionRecognitionExampleButtonClick()
        {
            SceneManager.LoadScene("FacialExpressionRecognitionExample");
        }

        public void OnMediaPipeFaceLandmarkerExampleButtonClick()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                SceneManager.LoadScene("MediaPipeFaceLandmarkerExample_Built-in");
            }
            else
            {
                SceneManager.LoadScene("MediaPipeFaceLandmarkerExample_SRP");
            }
        }

        public void OnMediaPipeHandLandmarkerExampleButtonClick()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                SceneManager.LoadScene("MediaPipeHandLandmarkerExample_Built-in");
            }
            else
            {
                SceneManager.LoadScene("MediaPipeHandLandmarkerExample_SRP");
            }
        }

        public void OnMediaPipeHolisticLandmarkerExampleButtonClick()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                SceneManager.LoadScene("MediaPipeHolisticLandmarkerExample_Built-in");
            }
            else
            {
                SceneManager.LoadScene("MediaPipeHolisticLandmarkerExample_SRP");
            }
        }

        public void OnMediaPipePoseLandmarkerExampleButtonClick()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                SceneManager.LoadScene("MediaPipePoseLandmarkerExample_Built-in");
            }
            else
            {
                SceneManager.LoadScene("MediaPipePoseLandmarkerExample_SRP");
            }
        }

        public void OnHumanSegmentationPPHumanSegExampleButtonClick()
        {
            SceneManager.LoadScene("HumanSegmentationPPHumanSegExample");
        }

        public void OnImageClassificationMobilenetExampleButtonClick()
        {
            SceneManager.LoadScene("ImageClassificationMobilenetExample");
        }

        public void OnImageClassificationPPResnetExampleButtonClick()
        {
            SceneManager.LoadScene("ImageClassificationPPResnetExample");
        }

        public void OnObjectDetectionDAMOYOLOExampleButtonClick()
        {
            SceneManager.LoadScene("ObjectDetectionDAMOYOLOExample");
        }

        public void OnObjectDetectionYOLOXExampleButtonClick()
        {
            SceneManager.LoadScene("ObjectDetectionYOLOXExample");
        }

        public void OnObjectDetectionNanoDetPlusExampleButtonClick()
        {
            SceneManager.LoadScene("ObjectDetectionNanoDetPlusExample");
        }

        public void OnYOLOv5ObjectDetectionExampleButtonClick()
        {
            SceneManager.LoadScene("YOLOv5ObjectDetectionExample");
        }

        public void OnYOLOv5InstanceSegmentationExampleButtonClick()
        {
            SceneManager.LoadScene("YOLOv5InstanceSegmentationExample");
        }

        public void OnYOLOv5ImageClassificationExampleButtonClick()
        {
            SceneManager.LoadScene("YOLOv5ImageClassificationExample");
        }

        public void OnYOLOv8ObjectDetectionExampleButtonClick()
        {
            SceneManager.LoadScene("YOLOv8ObjectDetectionExample");
        }

        public void OnYOLOv8InstanceSegmentationExampleButtonClick()
        {
            SceneManager.LoadScene("YOLOv8InstanceSegmentationExample");
        }

        public void OnYOLOv8ImageClassificationExampleButtonClick()
        {
            SceneManager.LoadScene("YOLOv8ImageClassificationExample");
        }

        public void OnYOLOv8PoseEstimationExampleButtonClick()
        {
            SceneManager.LoadScene("YOLOv8PoseEstimationExample");
        }

        public void OnTextRecognitionCRNNExampleButtonClick()
        {
            SceneManager.LoadScene("TextRecognitionCRNNExample");
        }

        #endregion

        #region photo

        public void OnInpaintExampleButtonClick()
        {
            SceneManager.LoadScene("InpaintExample");
        }

        public void OnSeamlessCloneExampleButtonClick()
        {
            SceneManager.LoadScene("SeamlessCloneExample");
        }

        #endregion

        #endregion

        #region Contrib modules

        #region ml

        public void OnKNNExampleButtonClick()
        {
            SceneManager.LoadScene("KNNExample");
        }

        public void OnSVMExampleButtonClick()
        {
            SceneManager.LoadScene("SVMExample");
        }

        #endregion

        #region bgsegm

        public void OnBackgroundSubtractorExampleButtonClick()
        {
            SceneManager.LoadScene("BackgroundSubtractorExample");
        }

        #endregion

        #region face

        // public void OnFaceMarkExampleButtonClick()
        // {
        //     SceneManager.LoadScene("FaceMarkExample");
        // }

        public void OnFaceRecognizerExampleButtonClick()
        {
            SceneManager.LoadScene("FaceRecognizerExample");
        }

        #endregion

        #region plot

        public void OnPlotExampleButtonClick()
        {
            SceneManager.LoadScene("PlotExample");
        }

        #endregion

        #region text

        public void OnTextDetectionExampleButtonClick()
        {
            SceneManager.LoadScene("TextDetectionExample");
        }

        public void OnTextRecognitionExampleButtonClick()
        {
            SceneManager.LoadScene("TextRecognitionExample");
        }

        #endregion

        #region tracking

        public void OnLegacyTrackingExampleButtonClick()
        {
            SceneManager.LoadScene("LegacyTrackingExample");
        }

        public void OnTrackingExampleButtonClick()
        {
            SceneManager.LoadScene("TrackingExample");
        }

        #endregion

        #region wechat_qrcode

        public void OnWeChatQRCodeDetectorExampleButtonClick()
        {
            SceneManager.LoadScene("WeChatQRCodeDetectorExample");
        }

        #endregion

        #endregion
    }
}
