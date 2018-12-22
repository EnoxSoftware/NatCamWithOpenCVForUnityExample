using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using UnityEngine.UI;

namespace NatCamWithOpenCVForUnityExample {

    /// <summary>
    /// WebCamTexture Only Example
    /// An example of displaying the preview frame of camera only using WebCamTexture API.
    /// </summary>
    public class WebCamTextureOnlyExample : ExampleBase<WebCamSource> {

        Texture2D texture;
        Color32[] pixelBuffer;

        protected override void Start () {
            // Load global camera benchmark settings.
            int width, height, framerate;
            NatCamWithOpenCVForUnityExample.CameraConfiguration(out width, out height, out framerate);
            // Create camera source
            cameraSource = new WebCamSource(width, height, framerate, useFrontCamera);
            // Update UI
            imageProcessingTypeDropdown.value = (int)imageProcessingType;
        }

        protected override void OnStart () {
            // Create pixel buffer
            pixelBuffer = new Color32[cameraSource.Preview.width * cameraSource.Preview.height * 4];
            // Create texture
            texture = new Texture2D(
                cameraSource.Preview.width,
                cameraSource.Preview.height,
                TextureFormat.RGBA32,
                false,
                false
            );
            // Display texture
            rawImage.texture = texture;
            aspectFitter.aspectRatio = cameraSource.Preview.width / (float)cameraSource.Preview.height;
            Debug.Log("WebCam camera source started with resolution: "+cameraSource.Preview.width+"x"+cameraSource.Preview.height);
        }

        protected override void OnFrame () {
            cameraSource.CaptureFrame(pixelBuffer);
            ProcessImage(pixelBuffer, texture.width, texture.height, imageProcessingType);
            texture.SetPixels32(pixelBuffer);
            texture.Apply ();
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