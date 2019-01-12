using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity;
using NatCamU.Core;
using NatShareU;

namespace NatCamWithOpenCVForUnityExample
{

    /// <summary>
    /// Integration With NatShare Example
    /// An example of the native sharing and save to the camera roll using NatShare.
    /// </summary>
    public class IntegrationWithNatShareExample : ExampleBase<NatCamSource>
    {
        public Toggle applyComicFilterToggle;

        Mat frameMatrix;
        Texture2D texture;
        ComicFilter comicFilter;

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
            // Create comic filter
            comicFilter = new ComicFilter ();

            fpsMonitor = GetComponent<FpsMonitor> ();
            if (fpsMonitor != null) {
                fpsMonitor.Add ("Name", "IntegrationWithNatShareExample");
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
            // Create matrix
            if (frameMatrix != null)
                frameMatrix.Dispose (); 
            frameMatrix = new Mat (cameraSource.height, cameraSource.width, CvType.CV_8UC4);
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

            if (applyComicFilterToggle.isOn)
                comicFilter.Process (frameMatrix, frameMatrix);

            Imgproc.putText (frameMatrix, "[NatCam With OpenCVForUnity Example]", new Point (5, frameMatrix.rows () - 50), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
            Imgproc.putText (frameMatrix, "- Integration With NatShare Example", new Point (5, frameMatrix.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
            // Convert to Texture2D
            Utils.fastMatToTexture2D (frameMatrix, texture, true, 0, false);
        }

        protected override void OnDestroy ()
        {
            if (cameraSource != null) {
                cameraSource.Dispose ();
                cameraSource = null;
            }
            if (frameMatrix != null)
                frameMatrix.Dispose ();
            frameMatrix = null;
            Texture2D.Destroy (texture);
            texture = null;
            comicFilter.Dispose ();
            comicFilter = null;
        }

        public void OnShareButtonClick ()
        {
            NatShare.Share (texture,
                () => {
                    Debug.Log ("sharing is complete.");
                });
        }

        public void OnSaveToCameraRollButtonClick ()
        {
            NatShare.SaveToCameraRoll (texture, "NatCamWithOpenCVForUnityExample");
        }
    }
}