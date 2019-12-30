using System.Collections.Generic;
using UnityEngine;

namespace NatCamWithOpenCVForUnityExample
{

    /// <summary>
    /// NatCamPreview Only Example
    /// An example of displaying the preview frame of camera only using NatCam.
    /// </summary>
    public class NatCamPreviewOnlyExample : ExampleBase<NatCamSource>
    {

        Texture2D texture;
        byte[] pixelBuffer;

        FpsMonitor fpsMonitor;

        protected override void Start()
        {
            // Load global camera benchmark settings.
            int width, height, framerate;
            NatCamWithOpenCVForUnityExample.CameraConfiguration(out width, out height, out framerate);
            NatCamWithOpenCVForUnityExample.ExampleSceneConfiguration(out performImageProcessingEachTime);
            // Create camera source
            cameraSource = new NatCamSource(width, height, framerate, useFrontCamera);
            if (cameraSource.activeCamera == null)
                cameraSource = new NatCamSource(width, height, framerate, !useFrontCamera);
            cameraSource.StartPreview(OnStart, OnFrame);
            // Update UI
            imageProcessingTypeDropdown.value = (int)imageProcessingType;

            fpsMonitor = GetComponent<FpsMonitor>();
            if (fpsMonitor != null)
            {
                fpsMonitor.Add("Name", "NatCamPreviewOnlyExample");
                fpsMonitor.Add("performImageProcessingEveryTime", performImageProcessingEachTime.ToString());
                fpsMonitor.Add("onFrameFPS", onFrameFPS.ToString("F1"));
                fpsMonitor.Add("drawFPS", drawFPS.ToString("F1"));
                fpsMonitor.Add("width", "");
                fpsMonitor.Add("height", "");
                fpsMonitor.Add("orientation", "");
            }
        }

        protected override void OnStart()
        {
            base.OnStart();

            // Create pixel buffer
            pixelBuffer = new byte[cameraSource.width * cameraSource.height * 4];
            // Create texture
            if (texture != null)
                Texture2D.Destroy(texture);
            texture = new Texture2D(
                cameraSource.width,
                cameraSource.height,
                TextureFormat.RGBA32,
                false,
                false
            );
            // Display preview
            rawImage.texture = texture;
            aspectFitter.aspectRatio = cameraSource.width / (float)cameraSource.height;
            Debug.Log("NatCam camera source started with resolution: " + cameraSource.width + "x" + cameraSource.height + " isFrontFacing: " + cameraSource.isFrontFacing);
            // Log camera properties
            var camera = cameraSource.activeCamera;
            var cameraProps = new Dictionary<string, string>();
            cameraProps.Add("ExposureBias", camera.ExposureBias.ToString());
            cameraProps.Add("ExposureLock", camera.ExposureLock.ToString());
            cameraProps.Add("FlashMode", camera.FlashMode.ToString());
            cameraProps.Add("FocusLock", camera.FocusLock.ToString());
            cameraProps.Add("Framerate", camera.Framerate.ToString());
            cameraProps.Add("HorizontalFOV", camera.HorizontalFOV.ToString());
            cameraProps.Add("IsExposureLockSupported", camera.IsExposureLockSupported.ToString());
            cameraProps.Add("IsFlashSupported", camera.IsFlashSupported.ToString());
            cameraProps.Add("IsFocusLockSupported", camera.IsFocusLockSupported.ToString());
            cameraProps.Add("IsFrontFacing", camera.IsFrontFacing.ToString());
            //cameraProps.Add("IsRunning", camera.IsRunning.ToString());
            cameraProps.Add("IsTorchSupported", camera.IsTorchSupported.ToString());
            cameraProps.Add("IsWhiteBalanceLockSupported", camera.IsWhiteBalanceLockSupported.ToString());
            cameraProps.Add("MaxExposureBias", camera.MaxExposureBias.ToString());
            cameraProps.Add("MaxZoomRatio", camera.MaxZoomRatio.ToString());
            cameraProps.Add("MinExposureBias", camera.MinExposureBias.ToString());
            cameraProps.Add("PhotoResolution", camera.PhotoResolution.width + "x" + camera.PhotoResolution.height);
            cameraProps.Add("PreviewResolution", camera.PreviewResolution.width + "x" + camera.PreviewResolution.height);
            cameraProps.Add("TorchEnabled", camera.TorchEnabled.ToString());
            cameraProps.Add("UniqueID", camera.UniqueID.ToString());
            cameraProps.Add("VerticalFOV", camera.VerticalFOV.ToString());
            cameraProps.Add("WhiteBalanceLock", camera.WhiteBalanceLock.ToString());
            cameraProps.Add("ZoomRatio", camera.ZoomRatio.ToString());
            Debug.Log("# Active Camera Properties #####################");
            foreach (string key in cameraProps.Keys)
                Debug.Log(key + ": " + cameraProps[key]);
            Debug.Log("#######################################");

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", cameraSource.width.ToString());
                fpsMonitor.Add("height", cameraSource.height.ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());

                fpsMonitor.boxWidth = 240;
                fpsMonitor.boxHeight = 800;
                fpsMonitor.LocateGUI();

                foreach (string key in cameraProps.Keys)
                    fpsMonitor.Add(key, cameraProps[key]);
            }
        }

        protected override void Update()
        {
            base.Update();

            if (updateCount == 0)
            {
                if (fpsMonitor != null)
                {
                    fpsMonitor.Add("onFrameFPS", onFrameFPS.ToString("F1"));
                    fpsMonitor.Add("drawFPS", drawFPS.ToString("F1"));
                    fpsMonitor.Add("orientation", Screen.orientation.ToString());
                }
            }
        }

        protected override void UpdateTexture()
        {
            cameraSource.CaptureFrame(pixelBuffer);
            ProcessImage(pixelBuffer, texture.width, texture.height, imageProcessingType);
            texture.LoadRawTextureData(pixelBuffer);
            texture.Apply();
        }

        protected override void OnDestroy()
        {
            if (cameraSource != null)
            {
                cameraSource.Dispose();
                cameraSource = null;
            }
            Texture2D.Destroy(texture);
            texture = null;
            pixelBuffer = null;
        }
    }
}