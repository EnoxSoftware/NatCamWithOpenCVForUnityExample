using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace NatCamWithOpenCVForUnityExample
{
    public class NatCamWithOpenCVForUnityExample : MonoBehaviour
    {
        public Text versionInfo;
        public ScrollRect scrollRect;
        static float verticalNormalizedPosition = 1f;

        [HeaderAttribute ("Benchmark")]

        public Dropdown cameraResolutionDropdown;
        static ResolutionPreset cameraResolution = ResolutionPreset._1280x720;
        public Dropdown cameraFPSDropdown;
        static FPSPreset cameraFPS = FPSPreset._30;

        // Use this for initialization
        void Start ()
        {
            versionInfo.text = OpenCVForUnity.Core.NATIVE_LIBRARY_NAME + " " + OpenCVForUnity.Utils.getVersion () + " (" + OpenCVForUnity.Core.VERSION + ")";
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
            versionInfo.text +=  " ";
            #if ENABLE_MONO
            versionInfo.text +=  "Mono";
            #elif ENABLE_IL2CPP
            versionInfo.text += "IL2CPP";
            #elif ENABLE_DOTNET
            versionInfo.text += ".NET";
            #endif

            scrollRect.verticalNormalizedPosition = verticalNormalizedPosition;

            // Update GUI state
            cameraResolutionDropdown.value = (byte)cameraResolution;
            string[] enumNames = System.Enum.GetNames (typeof(FPSPreset));
            int index = Array.IndexOf (enumNames, cameraFPS.ToString());
            cameraFPSDropdown.value = index;
        }

        // Update is called once per frame
        void Update ()
        {

        }
        
        public void OnScrollRectValueChanged ()
        {
            verticalNormalizedPosition = scrollRect.verticalNormalizedPosition;
        }


        public void OnShowSystemInfoButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("ShowSystemInfo");
            #else
            Application.LoadLevel ("ShowSystemInfo");
            #endif
        }

        public void OnShowLicenseButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
			SceneManager.LoadScene ("ShowLicense");
            #else
            Application.LoadLevel ("ShowLicense");
            #endif
        }

        public void OnNatCamPreviewOnlyExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("NatCamPreviewOnlyExample");
            #else
            Application.LoadLevel ("NatCamPreviewOnlyExample");
            #endif
        }

        public void OnWebCamTextureOnlyExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureOnlyExample");
            #else
            Application.LoadLevel ("WebCamTextureOnlyExample");
            #endif
        }

        public void OnNatCamPreviewToMatExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("NatCamPreviewToMatExample");
            #else
            Application.LoadLevel ("NatCamPreviewToMatExample");
            #endif
        }

        public void OnWebCamTextureToMatExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("WebCamTextureToMatExample");
            #else
            Application.LoadLevel ("WebCamTextureToMatExample");
            #endif
        }

        public void OnNatCamPreviewToMatHelperExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("NatCamPreviewToMatHelperExample");
            #else
            Application.LoadLevel ("NatCamPreviewToMatHelperExample");
            #endif
        }

        public void OnIntegrationWithNatShareExampleButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("IntegrationWithNatShareExample");
            #else
            Application.LoadLevel ("IntegrationWithNatShareExample");
            #endif
        }


        /// <summary>
        /// Raises the camera resolution dropdown value changed event.
        /// </summary>
        public void OnCameraResolutionDropdownValueChanged (int result)
        {
            if ((int)cameraResolution != result) {
                cameraResolution = (ResolutionPreset)result;
            }
        }

        /// <summary>
        /// Raises the camera FPS dropdown value changed event.
        /// </summary>
        public void OnCameraFPSDropdownValueChanged (int result)
        {
            string[] enumNames = Enum.GetNames (typeof(FPSPreset));
            int value = (int)System.Enum.Parse (typeof(FPSPreset), enumNames [result], true);

            if ((int)cameraFPS != value) {
                cameraFPS = (FPSPreset)value;
            }
        }

        public static void GetCameraResolution (out int width, out int height)
        {
            Dimensions (cameraResolution, out width, out height);
        }

        public static void GetCameraFps (out int fps)
        {
            fps = (int)cameraFPS;
        }

        private static void Dimensions (ResolutionPreset preset, out int width, out int height)
        {
            switch (preset) {
            case ResolutionPreset._50x50: width = 50; height = 50; break;
            case ResolutionPreset._640x480: width = 640; height = 480; break;
            case ResolutionPreset._1280x720: width = 1280; height = 720; break;
            case ResolutionPreset._1920x1080: width = 1920; height = 1080; break;
            case ResolutionPreset._9999x9999: width = 9999; height = 9999; break;
            default: width = height = 0; break;
            }
        }

        public enum FPSPreset : int
        {
            _0 = 0,
            _1 = 1,
            _5 = 5,
            _10 = 10,
            _15 = 15,
            _30 = 30,
            _60 = 60,
        }

        public enum ResolutionPreset : byte
        {
            _50x50 = 0,
            _640x480,
            _1280x720,
            _1920x1080,
            _9999x9999,
        }
    }
}
