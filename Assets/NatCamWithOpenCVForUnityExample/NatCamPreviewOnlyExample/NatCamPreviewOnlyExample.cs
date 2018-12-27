using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NatCamU.Core;

namespace NatCamWithOpenCVForUnityExample {

    /// <summary>
    /// NatCamPreview Only Example
    /// An example of displaying the preview frame of camera only using NatCam.
    /// </summary>
    public class NatCamPreviewOnlyExample : ExampleBase<NatCamSource> {

        Texture2D texture;
        Color32[] pixelBuffer;

        protected override void Start () {
            // Load global camera benchmark settings.
            int width, height, framerate;
            NatCamWithOpenCVForUnityExample.CameraConfiguration(out width, out height, out framerate);
            // Create camera source
            cameraSource = new NatCamSource(width, height, framerate, useFrontCamera);
            cameraSource.StartPreview(OnStart, OnFrame);
            // Update UI
            imageProcessingTypeDropdown.value = (int)imageProcessingType;
        }

        protected override void OnStart () {
            // Create pixel buffer
            pixelBuffer = new Color32[cameraSource.Preview.width * cameraSource.Preview.height];
            // Create texture
            texture = new Texture2D(
                cameraSource.Preview.width,
                cameraSource.Preview.height,
                TextureFormat.RGBA32,
                false,
                false
            );
            // Display preview
            rawImage.texture = texture;
            aspectFitter.aspectRatio = NatCam.Preview.width / (float)NatCam.Preview.height;
            Debug.Log("NatCam camera source started with resolution: "+cameraSource.Preview.width+"x"+cameraSource.Preview.height);
            // Log camera properties
            var cameraProps = new Dictionary<string, string> ();
            cameraProps.Add ("IsFrontFacing", NatCam.Camera.IsFrontFacing.ToString());
            cameraProps.Add ("Framerate", NatCam.Camera.Framerate.ToString());
            cameraProps.Add ("PreviewResolution", NatCam.Camera.PreviewResolution.x + "x" + NatCam.Camera.PreviewResolution.y);
            cameraProps.Add ("PhotoResolution", NatCam.Camera.PhotoResolution.x + "x" + NatCam.Camera.PhotoResolution.y);
            cameraProps.Add ("AutoExposure", NatCam.Camera.AutoexposeEnabled.ToString());
            cameraProps.Add ("ExposureBias", NatCam.Camera.ExposureBias.ToString());
            cameraProps.Add ("MinExposureBias", NatCam.Camera.MinExposureBias.ToString());
            cameraProps.Add ("MaxExposureBias", NatCam.Camera.MaxExposureBias.ToString());
            cameraProps.Add ("IsFlashSupported", NatCam.Camera.IsFlashSupported.ToString());
            cameraProps.Add ("FlashMode", NatCam.Camera.FlashMode.ToString());
            cameraProps.Add ("AutoFocus", NatCam.Camera.AutofocusEnabled.ToString());
            cameraProps.Add ("HorizontalFOV", NatCam.Camera.HorizontalFOV.ToString());
            cameraProps.Add ("VerticalFOV", NatCam.Camera.VerticalFOV.ToString());
            cameraProps.Add ("IsTorchSupported", NatCam.Camera.IsTorchSupported.ToString());
            cameraProps.Add ("TorchEnabled", NatCam.Camera.TorchEnabled.ToString());
            cameraProps.Add ("MaxZoomRatio", NatCam.Camera.MaxZoomRatio.ToString());
            cameraProps.Add ("ZoomRatio", NatCam.Camera.ZoomRatio.ToString());
            Debug.Log ("# Active Camera Properties #####################");
            foreach (string key in cameraProps.Keys)
                Debug.Log(key + ": " + cameraProps[key]);
            Debug.Log ("#######################################");
        }

        protected override void OnFrame () {
            cameraSource.CaptureFrame(pixelBuffer);
            ProcessImage(pixelBuffer, texture.width, texture.height, imageProcessingType);
            texture.SetPixels32(pixelBuffer);
            texture.Apply();
        }

        protected override void OnDestroy () {
            cameraSource.Dispose();
            cameraSource = null;
            Texture2D.Destroy(texture);
            texture = null;
            pixelBuffer = null;
        }
    }        
}