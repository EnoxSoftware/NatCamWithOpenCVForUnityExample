using UnityEngine;
using NatCamU.Core;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using System.Collections;
using OpenCVForUnity;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace NatCamWithOpenCVForUnityExample {

    /// <summary>
    /// NatCamPreview To Mat Example
    /// An example of converting a NatCam preview image to OpenCV's Mat format.
    /// </summary>
    public class NatCamPreviewToMatExample : ExampleBase<NatCamSource> {

        public enum MatCaptureMethod {
            NatCam_CaptureFrame_OpenCVFlip,
            BlitWithReadPixels,
            Graphics_CopyTexture,
        }

        [Header("OpenCV")]
        public MatCaptureMethod matCaptureMethod = MatCaptureMethod.NatCam_CaptureFrame_OpenCVFlip;
        public Dropdown matCaptureMethodDropdown; 

        Mat frameMatrix;
        Mat grayMatrix;
        Texture2D texture;

        protected override void Start () {
            // Load global camera benchmark settings.
            int width = 1280, height = 720, framerate = 30;
            //NatCamWithOpenCVForUnityExample.GetCameraResolution (out width, out height);
            //NatCamWithOpenCVForUnityExample.GetCameraFps (out fps);
            // Create camera source
            cameraSource = new NatCamSource(width, height, framerate, false);
            // Update UI
            imageProcessingTypeDropdown.value = (int)imageProcessingType;
            matCaptureMethodDropdown.value = (int)matCaptureMethod;
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
            // Display preview
            rawImage.texture = texture;
            aspectFitter.aspectRatio = NatCam.Preview.width / (float)NatCam.Preview.height;
            Debug.Log("NatCam camera source started with resolution: "+cameraSource.Preview.width+"x"+cameraSource.Preview.height);
            // Log stuff
            Debug.Log ("# Active Camera Properties #####################");
            var cameraProps = new Dictionary<string, string>();
            cameraProps.Add("IsFrontFacing", NatCam.Camera.IsFrontFacing.ToString());
            cameraProps.Add("Framerate", NatCam.Camera.Framerate.ToString());
            cameraProps.Add("PreviewResolution", NatCam.Camera.PreviewResolution.x + "x" + NatCam.Camera.PreviewResolution.y);
            cameraProps.Add("PhotoResolution", NatCam.Camera.PhotoResolution.x + "x" + NatCam.Camera.PhotoResolution.y);
            cameraProps.Add("AutoExposure", NatCam.Camera.AutoexposeEnabled.ToString());
            cameraProps.Add("ExposureBias", NatCam.Camera.ExposureBias.ToString());
            cameraProps.Add("MinExposureBias", NatCam.Camera.MinExposureBias.ToString());
            cameraProps.Add("MaxExposureBias", NatCam.Camera.MaxExposureBias.ToString());
            cameraProps.Add("IsFlashSupported", NatCam.Camera.IsFlashSupported.ToString());
            cameraProps.Add("FlashMode", NatCam.Camera.FlashMode.ToString());
            cameraProps.Add("AutoFocus", NatCam.Camera.AutofocusEnabled.ToString());
            cameraProps.Add("HorizontalFOV", NatCam.Camera.HorizontalFOV.ToString());
            cameraProps.Add("VerticalFOV", NatCam.Camera.VerticalFOV.ToString());
            cameraProps.Add("IsTorchSupported", NatCam.Camera.IsTorchSupported.ToString());
            cameraProps.Add("TorchEnabled", NatCam.Camera.TorchEnabled.ToString());
            cameraProps.Add("MaxZoomRatio", NatCam.Camera.MaxZoomRatio.ToString());
            cameraProps.Add("ZoomRatio", NatCam.Camera.ZoomRatio.ToString());
            Debug.Log ("#######################################");
        }

        protected override void OnFrame () {
            // Get the matrix
            switch (matCaptureMethod) {
                case MatCaptureMethod.NatCam_CaptureFrame_OpenCVFlip:
                    cameraSource.CaptureFrame(frameMatrix);
                    break;
                case MatCaptureMethod.BlitWithReadPixels:
                    Utils.textureToTexture2D(cameraSource.Preview, texture);
                    Utils.copyToMat(texture.GetRawTextureData(), frameMatrix);
                    Core.flip (frameMatrix, frameMatrix, 0);
                    break;
                case MatCaptureMethod.Graphics_CopyTexture:
                    if (SystemInfo.copyTextureSupport == UnityEngine.Rendering.CopyTextureSupport.None) {
                        Debug.LogError("This device does not support Graphics::CopyTexture");
                        return;
                    }
                    Graphics.CopyTexture (NatCam.Preview, texture);
                    Utils.copyToMat (texture.GetRawTextureData(), frameMatrix);
                    Core.flip (frameMatrix, frameMatrix, 0);
                    break;
            }
            // Process
            ProcessImage(frameMatrix, grayMatrix, imageProcessingType);
            // Convert to Texture2D
            Utils.fastMatToTexture2D(frameMatrix, texture, true, 0, false);
        }

        protected override void OnDestroy () {
            cameraSource.Dispose();
            cameraSource = null;
            if (frameMatrix != null)
                frameMatrix.Dispose();
            if (grayMatrix != null)
                grayMatrix.Dispose();
            frameMatrix =
            grayMatrix = null;
            Texture2D.Destroy(texture);
            texture = null;
        }
    }
}