using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    public class OpenCVForUnityExample : MonoBehaviour
    {
        // Constants
        private static float VERTICAL_NORMALIZED_POSITION = 1f;

        // Public Fields
        public Text VersionInfo;
        public ScrollRect ScrollRect;

        // Unity Lifecycle Methods
        private void Start()
        {
            VersionInfo.text = Core.NATIVE_LIBRARY_NAME + " " + OpenCVEnv.GetVersion() + " (" + Core.VERSION + ")";
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

            ScrollRect.verticalNormalizedPosition = VERTICAL_NORMALIZED_POSITION;

#if UNITY_WSA_10_0
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/BarcodeDetectorImageExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/BarcodeDetectorExampleButton").GetComponent<Button>().interactable = false;

            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FaceDetectorYNExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FaceRecognizerSFExampleButton").GetComponent<Button>().interactable = false;

            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ColorizationExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ObjectTrackingDaSiamRPNExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FastNeuralStyleTransferExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FaceDetectionYuNetExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FaceDetectionYuNetV2ExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FacialExpressionRecognitionExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/PoseEstimationMediaPipeExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/HandPoseEstimationMediaPipeExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/HumanSegmentationPPHumanSegExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ImageClassificationMobilenetExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ImageClassificationPPResnetExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ObjectDetectionDAMOYOLOExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ObjectDetectionYOLOXExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ObjectDetectionNanoDetPlusExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/TextRecognitionCRNNImageExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/TextRecognitionCRNNExampleButton").GetComponent<Button>().interactable = false;

            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/ContribModulesGroup/TextDetectionExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/ContribModulesGroup/TextRecognitionExampleButton").GetComponent<Button>().interactable = false;

            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/ContribModulesGroup/WeChatQRCodeDetectorImageExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/ContribModulesGroup/WeChatQRCodeDetectorExampleButton").GetComponent<Button>().interactable = false;
#endif


#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN && !UNITY_STANDALONE_OSX && !UNITY_LINUX && !UNITY_IOS && !UNITY_ANDROID
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/VideoCaptureCameraInputExampleButton").GetComponent<Button>().interactable = false;
#endif
        }

        private void Update()
        {

        }

        // Public Methods
        public void OnScrollRectValueChanged()
        {
            VERTICAL_NORMALIZED_POSITION = ScrollRect.verticalNormalizedPosition;
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

        public void OnWebCamTexture2MatHelperExampleButtonClick()
        {
            SceneManager.LoadScene("WebCamTexture2MatHelperExample");
        }

        public void OnMultiSource2MatHelperExampleButtonClick()
        {
            SceneManager.LoadScene("MultiSource2MatHelperExample");
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

        public void OnConvexHullExampleButtonClick()
        {
            SceneManager.LoadScene("ConvexHullExample");
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

        public void OnMatchShapesExampleButtonClick()
        {
            SceneManager.LoadScene("MatchShapesExample");
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

        #region calib3d

        public void OnStereoBMExampleButtonClick()
        {
            SceneManager.LoadScene("StereoBMExample");
        }

        #endregion

        #region features2d

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
            if (GraphicsSettings.defaultRenderPipeline == null)
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
            if (GraphicsSettings.defaultRenderPipeline == null)
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

        public void OnBarcodeDetectorImageExampleButtonClick()
        {
            SceneManager.LoadScene("BarcodeDetectorImageExample");
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

        public void OnHOGDescriptorExampleButtonClick()
        {
            SceneManager.LoadScene("HOGDescriptorExample");
        }

        public void OnQRCodeDetectorImageExampleButtonClick()
        {
            SceneManager.LoadScene("QRCodeDetectorImageExample");
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

        public void OnPoseEstimationMediaPipeExampleButtonClick()
        {
            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                SceneManager.LoadScene("PoseEstimationMediaPipeExample_Built-in");
            }
            else
            {
                SceneManager.LoadScene("PoseEstimationMediaPipeExample_SRP");
            }
        }

        public void OnHandPoseEstimationMediaPipeExampleButtonClick()
        {
            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                SceneManager.LoadScene("HandPoseEstimationMediaPipeExample_Built-in");
            }
            else
            {
                SceneManager.LoadScene("HandPoseEstimationMediaPipeExample_SRP");
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

        public void OnTextRecognitionCRNNImageExampleButtonClick()
        {
            SceneManager.LoadScene("TextRecognitionCRNNImageExample");
        }

        public void OnTextRecognitionCRNNExampleButtonClick()
        {
            SceneManager.LoadScene("TextRecognitionCRNNExample");
        }

        #endregion

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

        #region bgsegm

        public void OnBackgroundSubtractorExampleButtonClick()
        {
            SceneManager.LoadScene("BackgroundSubtractorExample");
        }

        #endregion

        #region face

        public void OnFaceMarkExampleButtonClick()
        {
            SceneManager.LoadScene("FaceMarkExample");
        }

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

        public void OnWeChatQRCodeDetectorImageExampleButtonClick()
        {
            SceneManager.LoadScene("WeChatQRCodeDetectorImageExample");
        }

        public void OnWeChatQRCodeDetectorExampleButtonClick()
        {
            SceneManager.LoadScene("WeChatQRCodeDetectorExample");
        }

        #endregion

        #endregion
    }
}
