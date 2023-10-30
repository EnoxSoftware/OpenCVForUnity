using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    public class ShowSystemInfo : MonoBehaviour
    {
        public Text systemInfoText;
        public InputField systemInfoInputField;

        private const string ASSET_NAME = "OpenCVForUnity";

        // Use this for initialization
        void Start()
        {

            StringBuilder sb = new StringBuilder();
            sb.Append("###### Build Info ######\n");
            IDictionary<string, string> buildInfo = GetBuildInfo();
            foreach (string key in buildInfo.Keys)
            {
                sb.Append(key).Append(" = ").Append(buildInfo[key]).Append("\n");
            }
            sb.Append("\n");

#if UNITY_IOS || (UNITY_ANDROID && UNITY_2018_3_OR_NEWER)
            sb.Append("###### Device Info ######\n");
            IDictionary<string, string> deviceInfo = GetDeviceInfo();
            foreach (string key in deviceInfo.Keys)
            {
                sb.Append(key).Append(" = ").Append(deviceInfo[key]).Append("\n");
            }
            sb.Append("\n");
#endif

            sb.Append("###### System Info ######\n");
            IDictionary<string, string> systemInfo = GetSystemInfo();
            foreach (string key in systemInfo.Keys)
            {
                sb.Append(key).Append(" = ").Append(systemInfo[key]).Append("\n");
            }
            sb.Append("#########################\n");

            systemInfoText.text = systemInfoInputField.text = sb.ToString();
            Debug.Log(sb.ToString());
        }

        // Update is called once per frame
        void Update()
        {

        }

        public Dictionary<string, string> GetBuildInfo()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            dict.Add(ASSET_NAME + " version", Core.NATIVE_LIBRARY_NAME + " " + Utils.getVersion() + " (" + Core.VERSION + ")");
            dict.Add("Build Unity version", Application.unityVersion);

#if UNITY_EDITOR
            dict.Add("Build target", "Editor");
#elif UNITY_STANDALONE_WIN
            dict.Add("Build target", "Windows");
#elif UNITY_STANDALONE_OSX
            dict.Add("Build target", "Mac OSX");
#elif UNITY_STANDALONE_LINUX
            dict.Add("Build target", "Linux");
#elif UNITY_ANDROID
            dict.Add("Build target", "Android");
#elif UNITY_IOS
            dict.Add("Build target", "iOS");
#elif UNITY_WSA
            dict.Add("Build target", "WSA");
#elif UNITY_WEBGL
            dict.Add("Build target", "WebGL");
#else
            dict.Add("Build target", "");
#endif

#if ENABLE_MONO
            dict.Add("Scripting backend", "Mono");
#elif ENABLE_IL2CPP
            dict.Add("Scripting backend", "IL2CPP");
#elif ENABLE_DOTNET
            dict.Add("Scripting backend", ".NET");
#else
            dict.Add("Scripting backend", "");
#endif

#if OPENCV_USE_UNSAFE_CODE
            dict.Add("Allow 'unsafe' Code", "Enabled");
#else
            dict.Add("Allow 'unsafe' Code", "Disabled");
#endif

            return dict;
        }

        public Dictionary<string, string> GetDeviceInfo()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

#if UNITY_IOS
            dict.Add("iOS.Device.generation", UnityEngine.iOS.Device.generation.ToString());
            dict.Add("iOS.Device.systemVersion", UnityEngine.iOS.Device.systemVersion.ToString());
#endif
#if UNITY_IOS && UNITY_2018_1_OR_NEWER
            dict.Add("UserAuthorization.WebCam", Application.HasUserAuthorization(UserAuthorization.WebCam).ToString());
            dict.Add("UserAuthorization.Microphone", Application.HasUserAuthorization(UserAuthorization.Microphone).ToString());
#endif
#if UNITY_ANDROID && UNITY_2018_3_OR_NEWER
            dict.Add("Android.Permission.Camera", UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera).ToString());
            dict.Add("Android.Permission.CoarseLocation", UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.CoarseLocation).ToString());
            dict.Add("Android.Permission.ExternalStorageRead", UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageRead).ToString());
            dict.Add("Android.Permission.ExternalStorageWrite", UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageWrite).ToString());
            dict.Add("Android.Permission.FineLocation", UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation).ToString());
            dict.Add("Android.Permission.Microphone", UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone).ToString());
#endif

            return dict;
        }

        /// SystemInfo Class Propertys
        public SortedDictionary<string, string> GetSystemInfo()
        {
            SortedDictionary<string, string> dict = new SortedDictionary<string, string>();

            Type type = typeof(SystemInfo);
            MemberInfo[] members = type.GetMembers(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            foreach (MemberInfo mb in members)
            {
                try
                {
                    if (mb.MemberType == MemberTypes.Property)
                    {
                        if (mb.Name == "deviceUniqueIdentifier")
                        {
                            dict.Add(mb.Name, "xxxxxxxxxxxxxxxxxxxxxxxx");
                            continue;
                        }

                        PropertyInfo pr = type.GetProperty(mb.Name);

                        if (pr != null)
                        {
                            object resobj = pr.GetValue(type, null);
                            dict.Add(mb.Name, resobj.ToString());
                        }
                        else
                        {
                            dict.Add(mb.Name, "");
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Exception: " + e);
                }
            }

            return dict;
        }

        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }
    }
}