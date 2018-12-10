using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using System;

namespace NatCamWithOpenCVForUnityExample {

    public class NatCamWithOpenCVForUnityExample : MonoBehaviour {

        public Text exampleTitle;
        public Text versionInfo;
        public ScrollRect scrollRect;
        static float verticalNormalizedPosition = 1f;

        void Awake () {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        void Start () {
            exampleTitle.text = "NatCamWithOpenCVForUnity Example " + Application.version;

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
        }
        
        public void OnScrollRectValueChanged () {
            verticalNormalizedPosition = scrollRect.verticalNormalizedPosition;
        }

        public void OnShowSystemInfoButtonClick () {
            SceneManager.LoadScene ("ShowSystemInfo");
        }

        public void OnShowLicenseButtonClick () {
			SceneManager.LoadScene("ShowLicense");
        }

        public void OnNatCamPreviewOnlyExampleButtonClick () {
            SceneManager.LoadScene("NatCamPreviewOnlyExample");
        }

        public void OnWebCamTextureOnlyExampleButtonClick () {
            SceneManager.LoadScene("WebCamTextureOnlyExample");
        }

        public void OnNatCamPreviewToMatExampleButtonClick () {
            SceneManager.LoadScene("NatCamPreviewToMatExample");
        }

        public void OnWebCamTextureToMatExampleButtonClick () {
            SceneManager.LoadScene("WebCamTextureToMatExample");
        }

        public void OnNatCamPreviewToMatHelperExampleButtonClick () {
            SceneManager.LoadScene("NatCamPreviewToMatHelperExample");
        }

        public void OnIntegrationWithNatShareExampleButtonClick () {
            SceneManager.LoadScene("IntegrationWithNatShareExample");
        }
    }
}