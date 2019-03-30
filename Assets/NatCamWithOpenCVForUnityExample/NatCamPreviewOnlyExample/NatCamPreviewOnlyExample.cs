using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NatCam;

namespace NatCamWithOpenCVForUnityExample
{

    /// <summary>
    /// NatCamPreview Only Example
    /// An example of displaying the preview frame of camera only using NatCam.
    /// </summary>
    public class NatCamPreviewOnlyExample : ExampleBase<NatCamSource>
    {

        Texture2D texture;
        Color32[] pixelBuffer;

        FpsMonitor fpsMonitor;

        protected override void Start ()
        {
            // Load global camera benchmark settings.
            int width, height, framerate;
            NatCamWithOpenCVForUnityExample.CameraConfiguration (out width, out height, out framerate);
            NatCamWithOpenCVForUnityExample.ExampleSceneConfiguration (out performImageProcessingEachTime);
            // Create camera source
            cameraSource = new NatCamSource (width, height, framerate, useFrontCamera);
            cameraSource.StartPreview (OnStart, OnFrame);
            // Update UI
            imageProcessingTypeDropdown.value = (int)imageProcessingType;

            fpsMonitor = GetComponent<FpsMonitor> ();
            if (fpsMonitor != null) {
                fpsMonitor.Add ("Name", "NatCamPreviewOnlyExample");
                fpsMonitor.Add ("performImageProcessingEveryTime", performImageProcessingEachTime.ToString ());
                fpsMonitor.Add ("onFrameFPS", onFrameFPS.ToString ("F1"));
                fpsMonitor.Add ("drawFPS", drawFPS.ToString ("F1"));
                fpsMonitor.Add ("width", "");
                fpsMonitor.Add ("height", "");
                fpsMonitor.Add ("orientation", "");
            }
        }

        protected override void OnStart ()
        {
            // Create pixel buffer
            pixelBuffer = new Color32[cameraSource.width * cameraSource.height];
            // Create texture
            if (texture != null)
                Texture2D.Destroy (texture);            
            texture = new Texture2D (
                cameraSource.width,
                cameraSource.height,
                TextureFormat.RGBA32,
                false,
                false
            );
            // Display preview
            rawImage.texture = texture;
            aspectFitter.aspectRatio = cameraSource.width / (float)cameraSource.height;
            Debug.Log ("NatCam camera source started with resolution: " + cameraSource.width + "x" + cameraSource.height);
            // Log camera properties
            var cameraProps = new Dictionary<string, string> ();
            cameraProps.Add ("IsFrontFacing", cameraSource.activeCamera.IsFrontFacing.ToString ());
            cameraProps.Add ("Framerate", cameraSource.activeCamera.Framerate.ToString ());
            cameraProps.Add ("PreviewResolution", cameraSource.activeCamera.PreviewResolution.x + "x" + cameraSource.activeCamera.PreviewResolution.y);
            cameraProps.Add ("PhotoResolution", cameraSource.activeCamera.PhotoResolution.x + "x" + cameraSource.activeCamera.PhotoResolution.y);
            cameraProps.Add ("ExposureLock", cameraSource.activeCamera.ExposureLock.ToString ());
            cameraProps.Add ("ExposureBias", cameraSource.activeCamera.ExposureBias.ToString ());
            cameraProps.Add ("MinExposureBias", cameraSource.activeCamera.MinExposureBias.ToString ());
            cameraProps.Add ("MaxExposureBias", cameraSource.activeCamera.MaxExposureBias.ToString ());
            cameraProps.Add ("IsFlashSupported", cameraSource.activeCamera.IsFlashSupported.ToString ());
            cameraProps.Add ("FlashMode", cameraSource.activeCamera.FlashMode.ToString ());
            cameraProps.Add ("FocusLock", cameraSource.activeCamera.FocusLock.ToString ());
            cameraProps.Add ("HorizontalFOV", cameraSource.activeCamera.HorizontalFOV.ToString ());
            cameraProps.Add ("VerticalFOV", cameraSource.activeCamera.VerticalFOV.ToString ());
            cameraProps.Add ("IsTorchSupported", cameraSource.activeCamera.IsTorchSupported.ToString ());
            cameraProps.Add ("TorchEnabled", cameraSource.activeCamera.TorchEnabled.ToString ());
            cameraProps.Add ("MaxZoomRatio", cameraSource.activeCamera.MaxZoomRatio.ToString ());
            cameraProps.Add ("ZoomRatio", cameraSource.activeCamera.ZoomRatio.ToString ());
            cameraProps.Add ("UniqueID", cameraSource.activeCamera.UniqueID.ToString ());
            cameraProps.Add ("WhiteBalanceLock", cameraSource.activeCamera.WhiteBalanceLock.ToString ());
            Debug.Log ("# Active Camera Properties #####################");
            foreach (string key in cameraProps.Keys)
                Debug.Log (key + ": " + cameraProps [key]);
            Debug.Log ("#######################################");

            if (fpsMonitor != null) {
                fpsMonitor.Add ("width", cameraSource.width.ToString ());
                fpsMonitor.Add ("height", cameraSource.height.ToString ());
                fpsMonitor.Add ("orientation", Screen.orientation.ToString ());

                fpsMonitor.boxWidth = 200;
                fpsMonitor.boxHeight = 760;
                fpsMonitor.LocateGUI ();

                foreach (string key in cameraProps.Keys)
                    fpsMonitor.Add (key, cameraProps [key]);
            }
        }

        protected override void Update ()
        {
            base.Update ();

            if (updateCount == 0) {
                if (fpsMonitor != null) {
                    fpsMonitor.Add ("onFrameFPS", onFrameFPS.ToString ("F1"));
                    fpsMonitor.Add ("drawFPS", drawFPS.ToString ("F1"));
                    fpsMonitor.Add ("orientation", Screen.orientation.ToString ());
                }
            }
        }

        protected override void UpdateTexture ()
        {
            cameraSource.CaptureFrame (pixelBuffer);
            ProcessImage (pixelBuffer, texture.width, texture.height, imageProcessingType);
            texture.SetPixels32 (pixelBuffer);
            texture.Apply ();
        }

        protected override void OnDestroy ()
        {
            if (cameraSource != null) {
                cameraSource.Dispose ();
                cameraSource = null;
            }
            Texture2D.Destroy (texture);
            texture = null;
            pixelBuffer = null;
        }
    }
}