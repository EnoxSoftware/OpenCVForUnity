using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine;
using UnityEngine.Rendering;
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
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/BarcodeDetectorImageExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/BarcodeDetectorExampleButton").GetComponent<Button>().interactable = false;

            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FaceDetectorYNExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FaceRecognizerSFExampleButton").GetComponent<Button>().interactable = false;

            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ColorizationExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ObjectTrackingDaSiamRPNExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FastNeuralStyleTransferExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FaceDetectionResnetSSDExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FaceDetectionYuNetExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FaceDetectionYuNetV2ExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/FacialExpressionRecognitionExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/PoseEstimationMediaPipeExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/HandPoseEstimationMediaPipeExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/HumanSegmentationPPHumanSegExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ImageClassificationMobilenetExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ImageClassificationPPResnetExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ObjectDetectionMobileNetSSDExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ObjectDetectionYOLOv4ExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ObjectDetectionYOLOXExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/ObjectDetectionNanoDetPlusExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/TextRecognitionCRNNImageExampleButton").GetComponent<Button>().interactable = false;
            GameObject.Find("Canvas/Panel/SceneList/ScrollView/List/MainModulesGroup/TextRecognitionCRNNExampleButton").GetComponent<Button>().interactable = false;

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

        public void OnDebugMatUtilsExampleButtonClick()
        {
            SceneManager.LoadScene("DebugMatUtilsExample");
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

        public void OnFaceDetectionResnetSSDExampleButtonClick()
        {
            SceneManager.LoadScene("FaceDetectionResnetSSDExample");
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

        public void OnObjectDetectionMobileNetSSDExampleButtonClick()
        {
            SceneManager.LoadScene("ObjectDetectionMobileNetSSDExample");
        }

        public void OnObjectDetectionYOLOv4ExampleButtonClick()
        {
            SceneManager.LoadScene("ObjectDetectionYOLOv4Example");
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

        #endregion
    }
}