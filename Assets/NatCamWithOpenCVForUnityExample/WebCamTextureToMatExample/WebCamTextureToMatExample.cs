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
    public class WebCamTextureToMatExample : ExampleBase<WebCamSource> {

        Mat frameMatrix, grayMatrix;
        Texture2D texture;

        protected override void Start () {
            // Load global camera benchmark settings.
            int width, height, framerate;
            NatCamWithOpenCVForUnityExample.CameraConfiguration(out width, out height, out framerate);
            // Create camera source
            cameraSource = new WebCamSource(width, height, framerate, useFrontCamera);
            cameraSource.StartPreview(OnStart, OnFrame);
            // Update UI
            imageProcessingTypeDropdown.value = (int)imageProcessingType;
        }

        protected override void OnStart () {
            // Create matrices
            frameMatrix = new Mat(cameraSource.Preview.height, cameraSource.Preview.width, CvType.CV_8UC4);
            grayMatrix = new Mat(cameraSource.Preview.height, cameraSource.Preview.width, CvType.CV_8UC1);
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