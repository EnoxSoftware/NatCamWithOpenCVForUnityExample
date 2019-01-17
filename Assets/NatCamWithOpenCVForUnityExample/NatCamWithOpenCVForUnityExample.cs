using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System;

namespace NatCamWithOpenCVForUnityExample
{

    public class NatCamWithOpenCVForUnityExample : MonoBehaviour
    {

        public enum FrameratePreset
        {
            _10,
            _15,
            _30,
            _60
        }

        public enum ResolutionPreset
        {
            Lowest,
            _640x480,
            _1280x720,
            _1920x1080,
            Highest,
        }

        [HeaderAttribute ("Benchmark")]
        public Dropdown cameraResolutionDropdown;
        public Dropdown cameraFPSDropdown;
        public Toggle performImageProcessingEachTimeToggle;
        private static ResolutionPreset cameraResolution = ResolutionPreset._1280x720;
        private static FrameratePreset cameraFramerate = FrameratePreset._30;
        private static bool performImageProcessingEachTime = false;

        [Header ("UI")]
        public Text exampleTitle;
        public Text versionInfo;
        public ScrollRect scrollRect;
        private static float verticalNormalizedPosition = 1f;


        #region --Client API--

        public static void CameraConfiguration (out int width, out int height, out int framerate)
        {
            switch (cameraResolution) {
            case ResolutionPreset.Lowest:
                width = height = 50;
                break;
            case ResolutionPreset._640x480:
                width = 640;
                height = 480;
                break;
            case ResolutionPreset._1920x1080:
                width = 1920;
                height = 1080;
                break;
            case ResolutionPreset.Highest:
                width = height = 9999;
                break;
            case ResolutionPreset._1280x720:
            default:
                width = 1280;
                height = 720;
                break;
            }
            switch (cameraFramerate) {
            case FrameratePreset._10:
                framerate = 10;
                break;
            case FrameratePreset._15:
                framerate = 15;
                break;
            case FrameratePreset._60:
                framerate = 60;
                break;
            case FrameratePreset._30:
            default:
                framerate = 30;
                break;
            }
        }

        public static void ExampleSceneConfiguration (out bool performImageProcessingEachTime)
        {
            performImageProcessingEachTime = NatCamWithOpenCVForUnityExample.performImageProcessingEachTime;
        }

        #endregion


        #region --Lifecycle--

        void Awake ()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        void Start ()
        {
            exampleTitle.text = "NatCamWithOpenCVForUnity Example " + Application.version;
            versionInfo.text = OpenCVForUnity.CoreModule.Core.NATIVE_LIBRARY_NAME + " " + OpenCVForUnity.UnityUtils.Utils.getVersion () + " (" + OpenCVForUnity.CoreModule.Core.VERSION + ")";
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

            // Update GUI state
            cameraResolutionDropdown.value = (int)cameraResolution;
            cameraFPSDropdown.value = (int)cameraFramerate;
            performImageProcessingEachTimeToggle.isOn = performImageProcessingEachTime;
        }

        #endregion


        #region --UI Callbacks--

        public void OnCameraResolutionDropdownValueChanged (int result)
        {
            cameraResolution = (ResolutionPreset)result;
        }

        public void OnCameraFPSDropdownValueChanged (int result)
        {
            cameraFramerate = (FrameratePreset)result;
        }

        public void OnPerformImageProcessingEachTimeToggleValueChanged ()
        {
            performImageProcessingEachTime = performImageProcessingEachTimeToggle.isOn;
        }

        public void OnScrollRectValueChanged ()
        {
            verticalNormalizedPosition = scrollRect.verticalNormalizedPosition;
        }

        public void OnShowSystemInfoButtonClick ()
        {
            SceneManager.LoadScene ("ShowSystemInfo");
        }

        public void OnShowLicenseButtonClick ()
        {
            SceneManager.LoadScene ("ShowLicense");
        }

        public void OnNatCamPreviewOnlyExampleButtonClick ()
        {
            SceneManager.LoadScene ("NatCamPreviewOnlyExample");
        }

        public void OnWebCamTextureOnlyExampleButtonClick ()
        {
            SceneManager.LoadScene ("WebCamTextureOnlyExample");
        }

        public void OnNatCamPreviewToMatExampleButtonClick ()
        {
            SceneManager.LoadScene ("NatCamPreviewToMatExample");
        }

        public void OnWebCamTextureToMatExampleButtonClick ()
        {
            SceneManager.LoadScene ("WebCamTextureToMatExample");
        }

        public void OnNatCamPreviewToMatHelperExampleButtonClick ()
        {
            SceneManager.LoadScene ("NatCamPreviewToMatHelperExample");
        }

        public void OnIntegrationWithNatShareExampleButtonClick ()
        {
            SceneManager.LoadScene ("IntegrationWithNatShareExample");
        }

        #endregion
    }
}