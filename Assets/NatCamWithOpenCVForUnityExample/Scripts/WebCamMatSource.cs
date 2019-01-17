using UnityEngine;
using System;
using System.Runtime.InteropServices;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;

namespace NatCamWithOpenCVForUnityExample
{

    public class WebCamMatSource : ICameraSource
    {

        #region --Op vars--

        private WebCamTexture webCamTexture;
        private Action startCallback, frameCallback;
        private Mat sourceMatrix, uprightMatrix;
        private Color32[] bufferColors;
        private int requestedWidth, requestedHeight, requestedFramerate;
        private int cameraIndex;
        private bool firstFrame;
        private DeviceOrientation orientation;

        #endregion


        #region --Client API--

        public int width { get { return uprightMatrix.width (); } }

        public int height { get { return uprightMatrix.height (); } }

        public bool isRunning { get { return (webCamTexture == null) ? false : webCamTexture.isPlaying; } }

        public WebCamDevice ActiveCamera { get { return WebCamTexture.devices [cameraIndex]; } }

        public WebCamMatSource (int width, int height, int framerate = 30, bool front = false)
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices. (Pixel, Pixel 2)
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            framerate = front ? 15 : framerate;
            #endif
            this.requestedWidth = width;
            this.requestedHeight = height;
            this.requestedFramerate = framerate;
            for (; cameraIndex < WebCamTexture.devices.Length; cameraIndex++)
                if (WebCamTexture.devices [cameraIndex].isFrontFacing == front)
                    break;
            
            if (cameraIndex == WebCamTexture.devices.Length) {
                Debug.LogError ((front ? "front" : "rear") + " camera does not exist. Consider using " + (front ? "rear" : "front") + " camera.");
                return;
            }

            this.webCamTexture = new WebCamTexture (ActiveCamera.name, width, height, framerate);
        }

        public void Dispose ()
        {
            Camera.onPostRender -= OnFrame;
            if (sourceMatrix != null)
                sourceMatrix.Dispose ();
            if (uprightMatrix != null)
                uprightMatrix.Dispose ();
            sourceMatrix =
            uprightMatrix = null;
            bufferColors = null;
            webCamTexture.Stop ();
            WebCamTexture.Destroy (webCamTexture);
            webCamTexture = null;
        }

        public void StartPreview (Action startCallback, Action frameCallback)
        {
            if (webCamTexture == null)
                return;

            this.startCallback = startCallback;
            this.frameCallback = frameCallback;
            Camera.onPostRender += OnFrame;
            firstFrame = true;
            webCamTexture.Play ();
        }

        public void CaptureFrame (Mat matrix)
        {
            uprightMatrix.copyTo (matrix);
        }

        public void CaptureFrame (Color32[] pixelBuffer)
        {
            Utils.copyFromMat (uprightMatrix, pixelBuffer);
        }

        public void SwitchCamera ()
        {
            Dispose ();
            cameraIndex = ++cameraIndex % WebCamTexture.devices.Length;

            bool front = ActiveCamera.isFrontFacing;
            int framerate = requestedFramerate;
            #if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices. (Pixel, Pixel 2)
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            framerate = front ? 15 : framerate;
            #endif
            webCamTexture = new WebCamTexture (ActiveCamera.name, requestedWidth, requestedHeight, framerate);
            StartPreview (startCallback, frameCallback);
        }

        #endregion


        #region --Operations--

        private void OnFrame (Camera camera)
        {
            if (!webCamTexture.isPlaying || !webCamTexture.didUpdateThisFrame)
                return;
            // Weird bug on macOS and macOS
            if (webCamTexture.width == 16 || webCamTexture.height == 16)
                return;

            // Check matrix
            sourceMatrix = sourceMatrix ?? new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
            uprightMatrix = uprightMatrix ?? new Mat ();
            bufferColors = bufferColors ?? new Color32[webCamTexture.width * webCamTexture.height];

            // Update matrix
            Utils.webCamTextureToMat (webCamTexture, sourceMatrix, bufferColors, false);
            var reference = (DeviceOrientation)(int)Screen.orientation;
            bool rotate90Degree = false;
            #if !UNITY_EDITOR && !(UNITY_STANDALONE || UNITY_WEBGL) 
            switch (reference) {
            case DeviceOrientation.LandscapeLeft:
            case DeviceOrientation.LandscapeRight:
                break;
            case DeviceOrientation.Portrait: 
            case DeviceOrientation.PortraitUpsideDown:
                rotate90Degree = true;
                break;
            }
            #else
            #endif


            int flipCode = 0;
            if (ActiveCamera.isFrontFacing) {
                if (webCamTexture.videoRotationAngle == 0 || webCamTexture.videoRotationAngle == 90) {
                    flipCode = -1;
                } else if (webCamTexture.videoRotationAngle == 180 || webCamTexture.videoRotationAngle == 270) {
                    flipCode = int.MinValue;
                }
            } else {
                if (webCamTexture.videoRotationAngle == 180 || webCamTexture.videoRotationAngle == 270) {
                    flipCode = 1;
                }
            }

            if (rotate90Degree && ActiveCamera.isFrontFacing) {
                if (flipCode == int.MinValue) {
                    flipCode = -1;
                } else if (flipCode == 0) {
                    flipCode = 1;
                } else if (flipCode == 1) {
                    flipCode = 0;
                } else if (flipCode == -1) {
                    flipCode = int.MinValue;
                }
            }

            if (flipCode > int.MinValue) {
                Core.flip (sourceMatrix, sourceMatrix, flipCode);
            }

            if (rotate90Degree) {
                Core.rotate (sourceMatrix, uprightMatrix, Core.ROTATE_90_CLOCKWISE);
            } else {
                sourceMatrix.copyTo (uprightMatrix);
            }

            // Orientation checking
            if (orientation != reference) {
                orientation = reference;
                firstFrame = true;
            }
            // Invoke client callbacks
            if (firstFrame) {
                startCallback ();
                firstFrame = false;
            }

            frameCallback ();
        }

        #endregion
    }
}