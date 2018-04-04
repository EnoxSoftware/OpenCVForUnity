using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace OpenCVForUnityExample
{
    public class ShowSystemInfo : MonoBehaviour
    {
        public Text systemInfoText;
        public InputField systemInfoInputField;
        Dictionary<string, string> dicSystemInfo;

        // Use this for initialization
        void Start ()
        {
            dicSystemInfo = GetSystemInfo ();

            systemInfoText.text = systemInfoInputField.text = "### System Info ###" + "\n";
            Debug.Log ("### System Info ###");

            foreach (string key in dicSystemInfo.Keys) {
                systemInfoText.text = systemInfoInputField.text += key + " = " + dicSystemInfo [key] + "\n";
                Debug.Log (key + "=" + dicSystemInfo [key]);
            }

            systemInfoText.text = systemInfoInputField.text += "###################" + "\n";
            Debug.Log ("###################");
        }

        // Update is called once per frame
        void Update ()
        {

        }

        public Dictionary<string, string> GetSystemInfo ()
        {
            Dictionary<string, string> dicSystemInfo = new Dictionary<string, string> ();

            dicSystemInfo.Add ("OpenCVForUnity version", OpenCVForUnity.Core.NATIVE_LIBRARY_NAME + " " + OpenCVForUnity.Utils.getVersion () + " (" + OpenCVForUnity.Core.VERSION + ")");
            dicSystemInfo.Add ("Build Unity version", Application.unityVersion);

            #if UNITY_EDITOR
            dicSystemInfo.Add ("Build target", "Editor");
            #elif UNITY_STANDALONE_WIN
            dicSystemInfo.Add("Build target", "Windows");
            #elif UNITY_STANDALONE_OSX
            dicSystemInfo.Add("Build target", "Mac OSX");
            #elif UNITY_STANDALONE_LINUX
            dicSystemInfo.Add("Build target", "Linux");
            #elif UNITY_ANDROID
            dicSystemInfo.Add("Build target", "Android");
            #elif UNITY_IOS
            dicSystemInfo.Add("Build target", "iOS");
            #elif UNITY_WSA
            dicSystemInfo.Add("Build target", "WSA");
            #elif UNITY_WEBGL
            dicSystemInfo.Add("Build target", "WebGL");
            #else
            dicSystemInfo.Add("Build target", "");
            #endif

            #if ENABLE_MONO
            dicSystemInfo.Add ("Scripting backend", "Mono");
            #elif ENABLE_IL2CPP
            dicSystemInfo.Add("Scripting backend", "IL2CPP");
            #elif ENABLE_DOTNET
            dicSystemInfo.Add("Scripting backend", ".NET");
            #else
            dicSystemInfo.Add("Scripting backend", "");
            #endif

            dicSystemInfo.Add ("operatingSystem", SystemInfo.operatingSystem);

            #if UNITY_IOS
            #if UNITY_5_4_OR_NEWER
            dicSystemInfo.Add("iPhone.generation", UnityEngine.iOS.Device.generation.ToString());
            #else
            dicSystemInfo.Add("iPhone.generation", UnityEngine.iPhone.generation.ToString());
            #endif
            #else
            dicSystemInfo.Add ("iPhone.generation", "");
            #endif

            //dicSystemInfo.Add("deviceUniqueIdentifier", SystemInfo.deviceUniqueIdentifier);
            dicSystemInfo.Add ("deviceModel", SystemInfo.deviceModel);
            dicSystemInfo.Add ("deviceName", SystemInfo.deviceName);
            dicSystemInfo.Add ("deviceType", SystemInfo.deviceType.ToString ());
            dicSystemInfo.Add ("graphicsDeviceName", SystemInfo.graphicsDeviceName);
            dicSystemInfo.Add ("graphicsDeviceVendor", SystemInfo.graphicsDeviceVendor);
            dicSystemInfo.Add ("processorType", SystemInfo.processorType);
            dicSystemInfo.Add ("graphicsMemorySize", SystemInfo.graphicsMemorySize.ToString ());
            dicSystemInfo.Add ("systemMemorySize", SystemInfo.systemMemorySize.ToString ());

            dicSystemInfo.Add ("graphicsDeviceID", SystemInfo.graphicsDeviceID.ToString ());
            dicSystemInfo.Add ("graphicsDeviceType", SystemInfo.graphicsDeviceType.ToString ());
            dicSystemInfo.Add ("graphicsDeviceVendorID", SystemInfo.graphicsDeviceVendorID.ToString ());
            dicSystemInfo.Add ("graphicsDeviceVersion", SystemInfo.graphicsDeviceVersion);
            dicSystemInfo.Add ("graphicsMultiThreaded", SystemInfo.graphicsMultiThreaded.ToString ());
            dicSystemInfo.Add ("graphicsShaderLevel", SystemInfo.graphicsShaderLevel.ToString ());
            
            #if UNITY_5_4_OR_NEWER
            dicSystemInfo.Add("copyTextureSupport", SystemInfo.copyTextureSupport.ToString());
            #else
            dicSystemInfo.Add ("copyTextureSupport", "");
            #endif

            dicSystemInfo.Add ("supportsAccelerometer", SystemInfo.supportsAccelerometer.ToString ());
            dicSystemInfo.Add ("supportsGyroscope", SystemInfo.supportsGyroscope.ToString ());
            dicSystemInfo.Add ("supportsVibration", SystemInfo.supportsVibration.ToString ());
            dicSystemInfo.Add ("supportsLocationService", SystemInfo.supportsLocationService.ToString ());

            return dicSystemInfo;
        }

        public void OnBackButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("OpenCVForUnityExample");
            #else
            Application.LoadLevel ("OpenCVForUnityExample");
            #endif
        }
    }
}