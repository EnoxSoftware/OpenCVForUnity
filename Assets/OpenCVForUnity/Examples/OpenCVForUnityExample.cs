using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    public class OpenCVForUnityExample : MonoBehaviour
    {
        public Text versionInfo;
        public ScrollRect scrollRect;
        static float verticalNormalizedPosition = 1f;

        // Use this for initialization
        void Start()
        {
            versionInfo.text = Core.NATIVE_LIBRARY_NAME + " " + Utils.getVersion() + " (" + Core.VERSION + ")";
            versionInfo.text += " / UnityEditor " + Application.unityVersion;
            versionInfo.text += " / ";

#if UNITY_EDITOR
            versionInfo.text += "Editor";
#elif UNITY_STANDALONE_WIN
            versionInfo.text += "Windows";
#elif UNITY_STANDALONE_OSX
            versionInfo.text += "Mac OSX";
#elif UNITY_STANDALONE_LINUX
            versionInfo.text += "Linux";
#elif UNITY_ANDROID
            versionInfo.text += "Android";
#elif UNITY_IOS
            versionInfo.text += "iOS";
#elif UNITY_WSA
            versionInfo.text += "WSA";
#elif UNITY_WEBGL
            versionInfo.text += "WebGL";
#endif
            versionInfo.text += " ";
#if ENABLE_MONO
            versionInfo.text += "Mono";
#elif ENABLE_IL2CPP
            versionInfo.text += "IL2CPP";
#elif ENABLE_DOTNET
            versionInfo.text += ".NET";
#endif

            scrollRect.verticalNormalizedPosition = verticalNormalizedPosition;

#if UNITY_WSA_10_0
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FaceDetectorYNWebCamExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FaceRecognizerSFExampleButton").GetComponent<Button>().interactable = false;

            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ColorizationExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/DaSiamRPNTrackerExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FastNeuralStyleTransferExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/LibFaceDetectionV2ExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/LibFaceDetectionV3ExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ResnetSSDFaceDetectionExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/LightweightPoseEstimationExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/LightweightPoseEstimationWebCamExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/HandPoseEstimationExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/HumanSegmentationExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/MobileNetSSDExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/MobileNetSSDWebCamExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ImageClassificationMobilenetExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ImageClassificationPPResnetExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/TextOCRExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/TextOCRWebCamExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/YoloObjectDetectionExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/YoloObjectDetectionWebCamExampleButton").GetComponent<Button>().interactable = false;

            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/ContribModulesGroup/BarcodeDetectorExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/ContribModulesGroup/BarcodeDetectorWebCamExampleButton").GetComponent<Button>().interactable = false;

            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/ContribModulesGroup/TextDetectionExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/ContribModulesGroup/TextRecognitionExampleButton").GetComponent<Button>().interactable = false;
#endif


#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN && !UNITY_STANDALONE_OSX && !UNITY_LINUX && !UNITY_IOS && !UNITY_ANDROID
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/VideoCaptureCameraInputExampleButton").GetComponent<Button>().interactable = false;
#endif
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void OnScrollRectValueChanged()
        {
            verticalNormalizedPosition = scrollRect.verticalNormalizedPosition;
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

        public void OnMatBasicProcessingExampleButtonClick()
        {
            SceneManager.LoadScene("MatBasicProcessingExample");
        }

        public void OnUtils_GetFilePathExampleButtonClick()
        {
            SceneManager.LoadScene("Utils_GetFilePathExample");
        }

        #endregion

        #region Advanced

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

        public void OnPolygonFilterExampleButtonClick()
        {
            SceneManager.LoadScene("PolygonFilterExample");
        }

        public void OnAlphaBlendingExampleButtonClick()
        {
            SceneManager.LoadScene("AlphaBlendingExample");
        }

        public void OnMatToTextureInRenderThreadExampleButtonClick()
        {
            SceneManager.LoadScene("MatToTextureInRenderThreadExample");
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

        public void OnFeature2DExampleButtonClick()
        {
            SceneManager.LoadScene("Feature2DExample");
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

        public void OnArUcoExampleButtonClick()
        {
            SceneManager.LoadScene("ArUcoExample");
        }

        public void OnArUcoWebCamExampleButtonClick()
        {
            SceneManager.LoadScene("ArUcoWebCamExample");
        }

        public void OnArUcoCreateMarkerExampleButtonClick()
        {
            SceneManager.LoadScene("ArUcoCreateMarkerExample");
        }

        public void OnArUcoCameraCalibrationExampleButtonClick()
        {
            SceneManager.LoadScene("ArUcoCameraCalibrationExample");
        }

        public void OnFaceDetectionExampleButtonClick()
        {
            SceneManager.LoadScene("FaceDetectionExample");
        }

        public void OnFaceDetectionWebCamExampleButtonClick()
        {
            SceneManager.LoadScene("FaceDetectionWebCamExample");
        }

        public void OnAsynchronousFaceDetectionWebCamExampleButtonClick()
        {
            SceneManager.LoadScene("AsynchronousFaceDetectionWebCamExample");
        }

        public void OnFaceDetectorYNWebCamExampleButtonClick()
        {
            SceneManager.LoadScene("FaceDetectorYNWebCamExample");
        }

        public void OnFaceRecognizerSFExampleButtonClick()
        {
            SceneManager.LoadScene("FaceRecognizerSFExample");
        }

        public void OnHOGDescriptorExampleButtonClick()
        {
            SceneManager.LoadScene("HOGDescriptorExample");
        }

        public void OnQRCodeDetectorExampleButtonClick()
        {
            SceneManager.LoadScene("QRCodeDetectorExample");
        }

        public void OnQRCodeDetectorWebCamExampleButtonClick()
        {
            SceneManager.LoadScene("QRCodeDetectorWebCamExample");
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

        public void OnDaSiamRPNTrackerExampleButtonClick()
        {
            SceneManager.LoadScene("DaSiamRPNTrackerExample");
        }

        public void OnFastNeuralStyleTransferExampleButtonClick()
        {
            SceneManager.LoadScene("FastNeuralStyleTransferExample");
        }

        public void OnLibFaceDetectionV2ExampleButtonClick()
        {
            SceneManager.LoadScene("LibFaceDetectionV2Example");
        }

        public void OnLibFaceDetectionV3ExampleButtonClick()
        {
            SceneManager.LoadScene("LibFaceDetectionV3Example");
        }

        public void OnResnetSSDFaceDetectionExampleButtonClick()
        {
            SceneManager.LoadScene("ResnetSSDFaceDetectionExample");
        }

        public void OnLightweightPoseEstimationExampleButtonClick()
        {
            SceneManager.LoadScene("LightweightPoseEstimationExample");
        }

        public void OnLightweightPoseEstimationWebCamExampleButtonClick()
        {
            SceneManager.LoadScene("LightweightPoseEstimationWebCamExample");
        }

        public void OnHandPoseEstimationExampleButtonClick()
        {
            SceneManager.LoadScene("HandPoseEstimationExample");
        }

        public void OnHumanSegmentationExampleButtonClick()
        {
            SceneManager.LoadScene("HumanSegmentationExample");
        }

        public void OnMobileNetSSDExampleButtonClick()
        {
            SceneManager.LoadScene("MobileNetSSDExample");
        }

        public void OnMobileNetSSDWebCamExampleButtonClick()
        {
            SceneManager.LoadScene("MobileNetSSDWebCamExample");
        }

        public void OnImageClassificationMobilenetExampleButtonClick()
        {
            SceneManager.LoadScene("ImageClassificationMobilenetExample");
        }

        public void OnImageClassificationPPResnetExampleButtonClick()
        {
            SceneManager.LoadScene("ImageClassificationPPResnetExample");
        }

        public void OnTextOCRExampleButtonClick()
        {
            SceneManager.LoadScene("TextOCRExample");
        }

        public void OnTextOCRWebCamExampleButtonClick()
        {
            SceneManager.LoadScene("TextOCRWebCamExample");
        }

        public void OnYoloObjectDetectionExampleButtonClick()
        {
            SceneManager.LoadScene("YoloObjectDetectionExample");
        }

        public void OnYoloObjectDetectionWebCamExampleButtonClick()
        {
            SceneManager.LoadScene("YoloObjectDetectionWebCamExample");
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

        #region barcode

        public void OnBarcodeDetectorExampleButtonClick()
        {
            SceneManager.LoadScene("BarcodeDetectorExample");
        }

        public void OnBarcodeDetectorWebCamExampleButtonClick()
        {
            SceneManager.LoadScene("BarcodeDetectorWebCamExample");
        }

        #endregion

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

        #endregion
    }
}