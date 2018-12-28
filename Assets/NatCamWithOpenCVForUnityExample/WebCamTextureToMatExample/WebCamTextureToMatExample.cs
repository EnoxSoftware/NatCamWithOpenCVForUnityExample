using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Collections;
using OpenCVForUnity;

namespace NatCamWithOpenCVForUnityExample {

    /// <summary>
    /// WebCamTexture To Mat Example
    /// An example of converting a WebCamTexture image to OpenCV's Mat format.
    /// </summary>
    public class WebCamTextureToMatExample : ExampleBase<WebCamMatSource> {

        Mat frameMatrix, grayMatrix;
        Texture2D texture;

        protected override void Start () {
            // Load global camera benchmark settings.
            int width, height, framerate;
            NatCamWithOpenCVForUnityExample.CameraConfiguration(out width, out height, out framerate);
            // Create camera source
            cameraSource = new WebCamMatSource(width, height, framerate, useFrontCamera);
            cameraSource.StartPreview(OnStart, OnFrame);
            // Update UI
            imageProcessingTypeDropdown.value = (int)imageProcessingType;
        }

        protected override void OnStart () {
            // Create matrices
            frameMatrix = new Mat(cameraSource.height, cameraSource.width, CvType.CV_8UC4);
            grayMatrix = new Mat(cameraSource.height, cameraSource.width, CvType.CV_8UC1);
            // Create texture
            texture = new Texture2D(
                cameraSource.width,
                cameraSource.height,
                TextureFormat.RGBA32,
                false,
                false
            );
            // Display texture
            rawImage.texture = texture;
            aspectFitter.aspectRatio = cameraSource.width / (float)cameraSource.height;
            Debug.Log("WebCam camera source started with resolution: "+cameraSource.width+"x"+cameraSource.height);
        }

        protected override void OnFrame () {
            cameraSource.CaptureFrame(frameMatrix);
            ProcessImage(frameMatrix, grayMatrix, imageProcessingType);
            Utils.fastMatToTexture2D(frameMatrix, texture, true, 0, false);
        }

        protected override void OnDestroy () {
            cameraSource.Dispose();
            cameraSource = null;
            grayMatrix.Dispose();
            frameMatrix.Dispose();
            Texture2D.Destroy(texture);
            texture = null;
            grayMatrix =
            frameMatrix = null;
        }
    }
}