using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Collections;
using OpenCVForUnity;

namespace NatCamWithOpenCVForUnityExample
{

    /// <summary>
    /// WebCamTexture To Mat Example
    /// An example of converting a WebCamTexture image to OpenCV's Mat format.
    /// </summary>
    public class WebCamTextureToMatExample : ExampleBase<WebCamMatSource>
    {

        Mat frameMatrix, grayMatrix;
        Texture2D texture;

        FpsMonitor fpsMonitor;

        protected override void Start ()
        {
            // Load global camera benchmark settings.
            int width, height, framerate;
            NatCamWithOpenCVForUnityExample.CameraConfiguration (out width, out height, out framerate);
            NatCamWithOpenCVForUnityExample.ExampleSceneConfiguration (out performImageProcessingEachTime);
            // Create camera source
            cameraSource = new WebCamMatSource (width, height, framerate, useFrontCamera);
            cameraSource.StartPreview (OnStart, OnFrame);
            // Update UI
            imageProcessingTypeDropdown.value = (int)imageProcessingType;

            fpsMonitor = GetComponent<FpsMonitor> ();
            if (fpsMonitor != null) {
                fpsMonitor.Add ("Name", "WebCamTextureToMatExample");
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
            // Display texture
            rawImage.texture = texture;
            aspectFitter.aspectRatio = cameraSource.width / (float)cameraSource.height;
            Debug.Log ("WebCam camera source started with resolution: " + cameraSource.width + "x" + cameraSource.height);

            if (fpsMonitor != null) {
                fpsMonitor.Add ("width", cameraSource.width.ToString ());
                fpsMonitor.Add ("height", cameraSource.height.ToString ());
                fpsMonitor.Add ("orientation", Screen.orientation.ToString ());
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
            cameraSource.CaptureFrame (frameMatrix);
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
            grayMatrix.Dispose ();
            frameMatrix.Dispose ();
            Texture2D.Destroy (texture);
            texture = null;
            grayMatrix =
            frameMatrix = null;
        }
    }
}