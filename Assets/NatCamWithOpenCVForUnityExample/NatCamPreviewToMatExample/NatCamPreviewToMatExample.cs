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

namespace NatCamWithOpenCVForUnityExample
{

    /// <summary>
    /// NatCamPreview To Mat Example
    /// An example of converting a NatCam preview image to OpenCV's Mat format.
    /// </summary>
    public class NatCamPreviewToMatExample : ExampleBase<NatCamSource>
    {

        public enum MatCaptureMethod
        {
            NatCam_CaptureFrame_OpenCVFlip,
            BlitWithReadPixels,
            Graphics_CopyTexture,
        }

        [Header ("OpenCV")]
        public MatCaptureMethod matCaptureMethod = MatCaptureMethod.NatCam_CaptureFrame_OpenCVFlip;
        public Dropdown matCaptureMethodDropdown;

        Mat frameMatrix;
        Mat grayMatrix;
        Texture2D texture;

        FpsMonitor fpsMonitor;

        #region --ExampleBase--

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
            matCaptureMethodDropdown.value = (int)matCaptureMethod;

            fpsMonitor = GetComponent<FpsMonitor> ();
            if (fpsMonitor != null) {
                fpsMonitor.Add ("Name", "NatCamPreviewToMatExample");
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
            // Create matrices
            if (frameMatrix != null)
                frameMatrix.Dispose (); 
            frameMatrix = new Mat (cameraSource.height, cameraSource.width, CvType.CV_8UC4);
            if (grayMatrix != null)
                grayMatrix.Dispose (); 
            grayMatrix = new Mat (cameraSource.height, cameraSource.width, CvType.CV_8UC1);
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
            aspectFitter.aspectRatio = NatCam.Preview.width / (float)NatCam.Preview.height;
            Debug.Log ("NatCam camera source started with resolution: " + cameraSource.width + "x" + cameraSource.height);
            // Log camera properties
            var cameraProps = new Dictionary<string, string> ();
            cameraProps.Add ("IsFrontFacing", NatCam.Camera.IsFrontFacing.ToString ());
            cameraProps.Add ("Framerate", NatCam.Camera.Framerate.ToString ());
            cameraProps.Add ("PreviewResolution", NatCam.Camera.PreviewResolution.x + "x" + NatCam.Camera.PreviewResolution.y);
            cameraProps.Add ("PhotoResolution", NatCam.Camera.PhotoResolution.x + "x" + NatCam.Camera.PhotoResolution.y);
            cameraProps.Add ("ExposureLock", NatCam.Camera.ExposureLock.ToString ());
            cameraProps.Add ("ExposureBias", NatCam.Camera.ExposureBias.ToString ());
            cameraProps.Add ("MinExposureBias", NatCam.Camera.MinExposureBias.ToString ());
            cameraProps.Add ("MaxExposureBias", NatCam.Camera.MaxExposureBias.ToString ());
            cameraProps.Add ("IsFlashSupported", NatCam.Camera.IsFlashSupported.ToString ());
            cameraProps.Add ("FlashMode", NatCam.Camera.FlashMode.ToString ());
            cameraProps.Add ("FocusLock", NatCam.Camera.FocusLock.ToString ());
            cameraProps.Add ("HorizontalFOV", NatCam.Camera.HorizontalFOV.ToString ());
            cameraProps.Add ("VerticalFOV", NatCam.Camera.VerticalFOV.ToString ());
            cameraProps.Add ("IsTorchSupported", NatCam.Camera.IsTorchSupported.ToString ());
            cameraProps.Add ("TorchEnabled", NatCam.Camera.TorchEnabled.ToString ());
            cameraProps.Add ("MaxZoomRatio", NatCam.Camera.MaxZoomRatio.ToString ());
            cameraProps.Add ("ZoomRatio", NatCam.Camera.ZoomRatio.ToString ());
            Debug.Log ("# Active Camera Properties #####################");
            foreach (string key in cameraProps.Keys)
                Debug.Log (key + ": " + cameraProps [key]);
            Debug.Log ("#######################################");

            if (fpsMonitor != null) {
                fpsMonitor.Add ("width", cameraSource.width.ToString ());
                fpsMonitor.Add ("height", cameraSource.height.ToString ());
                fpsMonitor.Add ("orientation", Screen.orientation.ToString ());

                fpsMonitor.boxWidth = 200;
                fpsMonitor.boxHeight = 680;
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
            // Get the matrix
            switch (matCaptureMethod) {
            case MatCaptureMethod.NatCam_CaptureFrame_OpenCVFlip:
                cameraSource.CaptureFrame (frameMatrix);
                break;
            case MatCaptureMethod.BlitWithReadPixels:
                Utils.textureToTexture2D (cameraSource.Preview, texture);
                Utils.copyToMat (texture.GetRawTextureData (), frameMatrix);
                Core.flip (frameMatrix, frameMatrix, 0);
                break;
            case MatCaptureMethod.Graphics_CopyTexture:
                if (SystemInfo.copyTextureSupport == UnityEngine.Rendering.CopyTextureSupport.None) {
                    Debug.LogError ("This device does not support Graphics::CopyTexture");
                    return;
                }
                Graphics.CopyTexture (NatCam.Preview, texture);
                Utils.copyToMat (texture.GetRawTextureData (), frameMatrix);
                Core.flip (frameMatrix, frameMatrix, 0);
                break;
            }

            ProcessImage (frameMatrix, grayMatrix, imageProcessingType);

            // Convert to Texture2D
            Utils.fastMatToTexture2D (frameMatrix, texture);
        }

        protected override void OnDestroy ()
        {
            if (cameraSource != null) {
                cameraSource.Dispose ();
                cameraSource = null;
            }
            if (frameMatrix != null)
                frameMatrix.Dispose ();
            if (grayMatrix != null)
                grayMatrix.Dispose ();
            frameMatrix =
            grayMatrix = null;
            Texture2D.Destroy (texture);
            texture = null;
        }

        #endregion


        #region --UI Callbacks--

        public void OnMatCaptureMethodDropdownValueChanged (int result)
        {
            matCaptureMethod = (MatCaptureMethod)result;
        }

        #endregion
    }
}