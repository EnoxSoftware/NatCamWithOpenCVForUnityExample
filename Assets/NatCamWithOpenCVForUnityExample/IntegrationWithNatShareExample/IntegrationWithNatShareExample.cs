using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity;
using NatCamU.Core;
using NatShareU;

namespace NatCamWithOpenCVForUnityExample {

    /// <summary>
    /// Integration With NatShare Example
    /// An example of the native sharing and save to the camera roll using NatShare.
    /// </summary>
    public class IntegrationWithNatShareExample : ExampleBase<NatCamSource> {

        Mat frameMatrix;
        Texture2D texture;
        ComicFilter comicFilter;

        protected override void Start () {
            // Load global camera benchmark settings.
            int width = 1280, height = 720, framerate = 30;
            //NatCamWithOpenCVForUnityExample.GetCameraResolution (out width, out height);
            //NatCamWithOpenCVForUnityExample.GetCameraFps (out fps);
            // Create camera source
            cameraSource = new NatCamSource(width, height, framerate, useFrontCamera);
            // Create comic filter
            comicFilter = new ComicFilter ();
        }

        protected override void OnStart () {
            // Create matrix
            frameMatrix = new Mat(cameraSource.Preview.height, cameraSource.Preview.width, CvType.CV_8UC4);
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
        }

        protected override void OnFrame () {
            cameraSource.CaptureFrame(frameMatrix);
            comicFilter.Process(frameMatrix, frameMatrix);
            Imgproc.putText(frameMatrix, "[NatCam With OpenCVForUnity Example]", new Point (5, frameMatrix.rows () - 50), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
            Imgproc.putText(frameMatrix, "- Integration With NatShare Example", new Point (5, frameMatrix.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
            Utils.fastMatToTexture2D(frameMatrix, texture, true, 0, false);
        }

        protected override void OnDestroy () {
            cameraSource.Dispose();
            cameraSource = null;
            if (frameMatrix != null)
                frameMatrix.Dispose ();
            frameMatrix = null;
            Texture2D.Destroy (texture);
            texture = null;
            comicFilter.Dispose ();
            comicFilter = null;
        }
    }
}