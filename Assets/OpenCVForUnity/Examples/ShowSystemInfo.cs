using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// Show System Info
    /// A scene that displays build, device, and Unity SystemInfo properties for debugging and support.
    /// </summary>
    public class ShowSystemInfo : MonoBehaviour
    {
        // Constants
        private const string ASSET_NAME = "OpenCVForUnity";
        private const string MASKED_VALUE = "xxxxxxxxxxxxxxxxxxxxxxxx";

        // Public Fields
        public Text SystemInfoText;
        public InputField SystemInfoInputField;

        // Unity Lifecycle Methods
        private void Start()
        {
            StringBuilder sb = new StringBuilder();
            AppendSection(sb, "Build Info", GetBuildInfo());
            AppendSection(sb, "Application Info", GetApplicationInfo());
            AppendSection(sb, "Display Info", GetDisplayInfo());
            AppendSection(sb, "Device Info", GetDeviceInfo());
            AppendSection(sb, "Input / Sensor Info", GetInputSensorInfo());
            AppendSection(sb, "System Info", GetSystemInfo());
            sb.Append("#########################\n");

            SystemInfoText.text = SystemInfoInputField.text = sb.ToString();
            Debug.Log(sb.ToString(), this);
        }

        private void Update()
        {

        }

        // Public Methods

        /// <summary>
        /// Returns build-time information such as OpenCVForUnity version, Unity version, build target, and scripting backend.
        /// </summary>
        /// <returns>A dictionary of build property names and values.</returns>
        public Dictionary<string, string> GetBuildInfo()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            dict.Add(ASSET_NAME + " version", Core.NATIVE_LIBRARY_NAME + " " + OpenCVForUnityEnv.GetVersion() + " (" + Core.VERSION + ")");
            dict.Add("Build Unity version", Application.unityVersion);
            dict.Add("Application.identifier", Application.identifier);
            dict.Add("Application.version", Application.version);
            dict.Add("Application.buildGUID", Application.buildGUID);

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
#elif UNITY_VISIONOS
            dict.Add("Build target", "VisionOS");
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

            dict.Add("Allow 'unsafe' Code", "Enabled");

#if NET_STANDARD_2_1
            dict.Add("API Compatibility Level", ".NET Standard 2.1");
#elif NET_STANDARD_2_0
            dict.Add("API Compatibility Level", ".NET Standard 2.0");
#elif NETFRAMEWORK
            dict.Add("API Compatibility Level", ".NET Framework (.NET 4.x)");
#else
            dict.Add("API Compatibility Level", "");
#endif

            return dict;
        }

        /// <summary>
        /// Returns runtime application information from the Application class.
        /// </summary>
        /// <returns>A dictionary of Application property names and values.</returns>
        public Dictionary<string, string> GetApplicationInfo()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            dict.Add("Application.platform", Application.platform.ToString());
            dict.Add("Application.isMobilePlatform", Application.isMobilePlatform.ToString());
            dict.Add("Application.isConsolePlatform", Application.isConsolePlatform.ToString());
            dict.Add("Application.isEditor", Application.isEditor.ToString());
            dict.Add("Application.isFocused", Application.isFocused.ToString());
            dict.Add("Application.internetReachability", Application.internetReachability.ToString());
            dict.Add("Application.systemLanguage", Application.systemLanguage.ToString());
            dict.Add("Application.installMode", Application.installMode.ToString());
            dict.Add("Application.installerName", Application.installerName);
            dict.Add("Application.sandboxType", Application.sandboxType.ToString());
            dict.Add("Application.absoluteURL", Application.absoluteURL);
            dict.Add("Application.dataPath", Application.dataPath);
            dict.Add("Application.persistentDataPath", Application.persistentDataPath);
            dict.Add("Application.temporaryCachePath", Application.temporaryCachePath);
            dict.Add("Application.streamingAssetsPath", Application.streamingAssetsPath);
            dict.Add("Application.productName", Application.productName);
            dict.Add("Application.companyName", Application.companyName);
            dict.Add("Application.targetFrameRate", Application.targetFrameRate.ToString());

            return dict;
        }

        /// <summary>
        /// Returns display information from the Screen class, including resolution, DPI, safe area, and cutouts.
        /// </summary>
        /// <returns>A dictionary of Screen property names and values.</returns>
        public Dictionary<string, string> GetDisplayInfo()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            dict.Add("Screen.width", Screen.width.ToString());
            dict.Add("Screen.height", Screen.height.ToString());
            dict.Add("Screen.dpi", Screen.dpi.ToString());
            dict.Add("Screen.orientation", Screen.orientation.ToString());
            dict.Add("Screen.fullScreen", Screen.fullScreen.ToString());
            dict.Add("Screen.fullScreenMode", Screen.fullScreenMode.ToString());
            dict.Add("Screen.safeArea", Screen.safeArea.ToString());
            dict.Add("Screen.currentResolution", Screen.currentResolution.ToString());

            UnityEngine.Rect[] cutouts = Screen.cutouts;
            dict.Add("Screen.cutouts.Length", cutouts.Length.ToString());
            for (int i = 0; i < cutouts.Length; i++)
            {
                dict.Add("Screen.cutouts[" + i + "]", cutouts[i].ToString());
            }

            return dict;
        }

        /// <summary>
        /// Returns platform-specific device information such as iOS device state, Android permissions and configuration, or WebGL browser details.
        /// </summary>
        /// <returns>A dictionary of platform-specific property names and values. Entries vary by build target.</returns>
        public Dictionary<string, string> GetDeviceInfo()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

#if UNITY_IOS
            dict.Add("iOS.Device.generation", UnityEngine.iOS.Device.generation.ToString());
            dict.Add("iOS.Device.systemVersion", UnityEngine.iOS.Device.systemVersion.ToString());
            dict.Add("iOS.Device.runsOnSimulator", UnityEngine.iOS.Device.runsOnSimulator.ToString());
            dict.Add("iOS.Device.iosAppOnMac", UnityEngine.iOS.Device.iosAppOnMac.ToString());
            dict.Add("iOS.Device.lowPowerModeEnabled", UnityEngine.iOS.Device.lowPowerModeEnabled.ToString());
            dict.Add("iOS.Device.advertisingTrackingEnabled", UnityEngine.iOS.Device.advertisingTrackingEnabled.ToString());
            dict.Add("iOS.Device.vendorIdentifier", MASKED_VALUE);
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

#if UNITY_ANDROID && UNITY_6000_0_OR_NEWER
            AppendAndroidConfiguration(dict, UnityEngine.Android.AndroidApplication.currentConfiguration);
#endif

#if UNITY_ANDROID && UNITY_6000_5_OR_NEWER
            dict.Add("AndroidApplication.isInMultiWindowMode", UnityEngine.Android.AndroidApplication.isInMultiWindowMode.ToString());

            UnityEngine.Android.AndroidWindowInsets insets = UnityEngine.Android.AndroidApplication.currentWindowInsets;
            dict.Add("AndroidWindowInsets.StatusBars", insets.IsVisible(UnityEngine.Android.AndroidWindowInsets.Type.StatusBars).ToString());
            dict.Add("AndroidWindowInsets.NavigationBars", insets.IsVisible(UnityEngine.Android.AndroidWindowInsets.Type.NavigationBars).ToString());

            UnityEngine.Android.AndroidFoldingFeature[] foldingFeatures = UnityEngine.Android.AndroidApplication.currentFoldingFeatures;
            dict.Add("AndroidFoldingFeature.count", foldingFeatures.Length.ToString());
            for (int i = 0; i < foldingFeatures.Length; i++)
            {
                UnityEngine.Android.AndroidFoldingFeature feature = foldingFeatures[i];
                string prefix = "AndroidFoldingFeature[" + i + "].";
                dict.Add(prefix + "state", feature.state.ToString());
                dict.Add(prefix + "orientation", feature.orientation.ToString());
                dict.Add(prefix + "bounds", feature.bounds.ToString());
                dict.Add(prefix + "isSeparating", feature.isSeparating.ToString());
                dict.Add(prefix + "occlusionType", feature.occlusionType.ToString());
            }
#endif

#if UNITY_WEBGL
            dict.Add("Application.absoluteURL", Application.absoluteURL);
            dict.Add("Application.isMobilePlatform", Application.isMobilePlatform.ToString());
            dict.Add("Input.touchSupported", Input.touchSupported.ToString());
            dict.Add("UserAuthorization.WebCam", Application.HasUserAuthorization(UserAuthorization.WebCam).ToString());
#endif

            return dict;
        }

        /// <summary>
        /// Returns input and sensor information from the Input class and related SystemInfo capability flags.
        /// </summary>
        /// <returns>A dictionary of input and sensor property names and values.</returns>
        public Dictionary<string, string> GetInputSensorInfo()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            dict.Add("Input.touchSupported", Input.touchSupported.ToString());
            dict.Add("Input.multiTouchEnabled", Input.multiTouchEnabled.ToString());
            dict.Add("Input.deviceOrientation", Input.deviceOrientation.ToString());
            dict.Add("Input.mousePresent", Input.mousePresent.ToString());
            dict.Add("Input.stylusTouchSupported", Input.stylusTouchSupported.ToString());
            dict.Add("Input.touchPressureSupported", Input.touchPressureSupported.ToString());
            dict.Add("Input.compensateSensors", Input.compensateSensors.ToString());
            dict.Add("Input.acceleration", Input.acceleration.ToString());

            dict.Add("SystemInfo.supportsAccelerometer", SystemInfo.supportsAccelerometer.ToString());
            dict.Add("SystemInfo.supportsGyroscope", SystemInfo.supportsGyroscope.ToString());
            dict.Add("SystemInfo.supportsLocationService", SystemInfo.supportsLocationService.ToString());
            dict.Add("SystemInfo.supportsVibration", SystemInfo.supportsVibration.ToString());
            dict.Add("SystemInfo.supportsAudio", SystemInfo.supportsAudio.ToString());

            if (SystemInfo.supportsGyroscope)
            {
                dict.Add("Input.gyro.enabled", Input.gyro.enabled.ToString());
                dict.Add("Input.gyro.attitude", Input.gyro.attitude.eulerAngles.ToString());
            }

            return dict;
        }

        /// <summary>
        /// Returns all static properties of the SystemInfo class via reflection.
        /// Sensitive values such as deviceUniqueIdentifier are masked.
        /// </summary>
        /// <returns>A sorted dictionary of SystemInfo property names and values.</returns>
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
                            dict.Add(mb.Name, MASKED_VALUE);
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
                    Debug.Log("Exception: " + e, this);
                }
            }

            return dict;
        }

        /// <summary>
        /// Loads the main example scene when the back button is clicked.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Appends a titled section to the string builder when the dictionary contains entries.
        /// </summary>
        /// <param name="sb">The string builder to append to.</param>
        /// <param name="title">The section title.</param>
        /// <param name="info">The key-value pairs to display.</param>
        private static void AppendSection(StringBuilder sb, string title, IDictionary<string, string> info)
        {
            if (info == null || info.Count == 0)
            {
                return;
            }

            sb.Append("###### ").Append(title).Append(" ######\n");
            foreach (KeyValuePair<string, string> entry in info)
            {
                sb.Append(entry.Key).Append(" = ").Append(entry.Value).Append("\n");
            }
            sb.Append("\n");
        }

#if UNITY_ANDROID && UNITY_6000_0_OR_NEWER
        /// <summary>
        /// Appends AndroidConfiguration properties to the dictionary.
        /// </summary>
        /// <param name="dict">The dictionary to append to.</param>
        /// <param name="configuration">The current Android device configuration.</param>
        private static void AppendAndroidConfiguration(Dictionary<string, string> dict, UnityEngine.Android.AndroidConfiguration configuration)
        {
            dict.Add("AndroidConfiguration.densityDpi", configuration.densityDpi.ToString());
            dict.Add("AndroidConfiguration.orientation", configuration.orientation.ToString());
            dict.Add("AndroidConfiguration.screenWidthDp", configuration.screenWidthDp.ToString());
            dict.Add("AndroidConfiguration.screenHeightDp", configuration.screenHeightDp.ToString());
            dict.Add("AndroidConfiguration.smallestScreenWidthDp", configuration.smallestScreenWidthDp.ToString());
            dict.Add("AndroidConfiguration.screenLayoutSize", configuration.screenLayoutSize.ToString());
            dict.Add("AndroidConfiguration.screenLayoutLong", configuration.screenLayoutLong.ToString());
            dict.Add("AndroidConfiguration.screenLayoutRound", configuration.screenLayoutRound.ToString());
            dict.Add("AndroidConfiguration.screenLayoutDirection", configuration.screenLayoutDirection.ToString());
            dict.Add("AndroidConfiguration.uiModeType", configuration.uiModeType.ToString());
            dict.Add("AndroidConfiguration.uiModeNight", configuration.uiModeNight.ToString());
            dict.Add("AndroidConfiguration.colorModeHdr", configuration.colorModeHdr.ToString());
            dict.Add("AndroidConfiguration.colorModeWideColorGamut", configuration.colorModeWideColorGamut.ToString());
            dict.Add("AndroidConfiguration.fontScale", configuration.fontScale.ToString());
            dict.Add("AndroidConfiguration.fontWeightAdjustment", configuration.fontWeightAdjustment.ToString());
            dict.Add("AndroidConfiguration.keyboard", configuration.keyboard.ToString());
            dict.Add("AndroidConfiguration.keyboardHidden", configuration.keyboardHidden.ToString());
            dict.Add("AndroidConfiguration.hardKeyboardHidden", configuration.hardKeyboardHidden.ToString());
            dict.Add("AndroidConfiguration.navigation", configuration.navigation.ToString());
            dict.Add("AndroidConfiguration.navigationHidden", configuration.navigationHidden.ToString());
            dict.Add("AndroidConfiguration.touchScreen", configuration.touchScreen.ToString());
            dict.Add("AndroidConfiguration.mobileCountryCode", configuration.mobileCountryCode.ToString());
            dict.Add("AndroidConfiguration.mobileNetworkCode", configuration.mobileNetworkCode.ToString());

            UnityEngine.Android.AndroidLocale[] locales = configuration.locales;
            dict.Add("AndroidConfiguration.locales.Length", locales.Length.ToString());
            for (int i = 0; i < locales.Length; i++)
            {
                UnityEngine.Android.AndroidLocale locale = locales[i];
                dict.Add("AndroidConfiguration.locales[" + i + "]", locale.language + "-" + locale.country);
            }
        }
#endif
    }
}
