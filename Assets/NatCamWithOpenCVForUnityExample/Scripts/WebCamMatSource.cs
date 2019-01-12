using UnityEngine;
using System;
using System.Runtime.InteropServices;
using OpenCVForUnity;

namespace NatCamWithOpenCVForUnityExample {

    public class WebCamMatSource : ICameraSource {

        #region --Op vars--
        private WebCamTexture webCamTexture;
        private Action startCallback, frameCallback;
        private Mat sourceMatrix, previewMatrix;
        private int requestedWidth, requestedHeight, framerate;
        private int cameraIndex;
        private bool firstFrame;
        private DeviceOrientation orientation;     
        #endregion


        #region --Client API--

        public int width { get { return previewMatrix.width(); }}
        public int height { get { return previewMatrix.height(); }}

        public WebCamDevice ActiveCamera { get { return WebCamTexture.devices[cameraIndex]; } }

        public WebCamMatSource (int width, int height, int framerate = 30, bool front = false) {
            #if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices. (Pixel, Pixel 2)
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            framerate = front ? 15 : framerate;
            #endif
            requestedWidth = width;
            requestedHeight = height;
            this.framerate = framerate;
            for (; cameraIndex < WebCamTexture.devices.Length; cameraIndex++)
                if (WebCamTexture.devices[cameraIndex].isFrontFacing == front)
                    break;
            this.webCamTexture = new WebCamTexture(ActiveCamera.name, width, height, framerate);
        }

        public void Dispose () {
            Camera.onPostRender -= OnFrame;
            if (sourceMatrix != null)
                sourceMatrix.Dispose();
            if (previewMatrix != null)
                previewMatrix.Dispose();
            sourceMatrix =
            previewMatrix = null;
            webCamTexture.Stop();
            WebCamTexture.Destroy(webCamTexture);
            webCamTexture = null;
        }

        public void StartPreview (Action startCallback, Action frameCallback) {
            this.startCallback = startCallback;
            this.frameCallback = frameCallback;
            Camera.onPostRender += OnFrame;
            firstFrame = true;
            webCamTexture.Play();
        }

        public void CaptureFrame (Mat matrix) {
            previewMatrix.copyTo(matrix);
        }

        public void CaptureFrame (Color32[] pixelBuffer) {
            Utils.copyFromMat(previewMatrix, pixelBuffer);
        }

        public void SwitchCamera () {
            Dispose();
            cameraIndex = ++cameraIndex % WebCamTexture.devices.Length;
            webCamTexture = new WebCamTexture(ActiveCamera.name, requestedWidth, requestedHeight, framerate);
            StartPreview(startCallback, frameCallback);
        }
        #endregion


        #region --Operations--

        private void OnFrame (Camera camera) {
            if (!webCamTexture.isPlaying)
                return;
            // Weird bug on macOS and macOS
            if (webCamTexture.width == 16 || webCamTexture.height == 16)
                return;
            // Check matrix
            sourceMatrix = sourceMatrix ?? new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
            previewMatrix = previewMatrix ?? new Mat();
            // Update matrix
            Utils.webCamTextureToMat(webCamTexture, sourceMatrix);
            var reference = (DeviceOrientation)(int)Screen.orientation;
            switch (reference) {
                case DeviceOrientation.LandscapeLeft:
                    sourceMatrix.copyTo(previewMatrix);
                    if (ActiveCamera.isFrontFacing)
                        Core.flip(previewMatrix, previewMatrix, 0);
                    break;
                case DeviceOrientation.Portrait:
                    Core.rotate(sourceMatrix, previewMatrix, Core.ROTATE_90_CLOCKWISE);
                    if (ActiveCamera.isFrontFacing)
                        Core.flip(previewMatrix, previewMatrix, 1);
                    break;
                case DeviceOrientation.LandscapeRight:
                    Core.rotate(sourceMatrix, previewMatrix, Core.ROTATE_180);
                    if (ActiveCamera.isFrontFacing)
                        Core.flip(previewMatrix, previewMatrix, 0);
                    break;
                case DeviceOrientation.PortraitUpsideDown:
                    Core.rotate(sourceMatrix, previewMatrix, Core.ROTATE_90_COUNTERCLOCKWISE);
                    if (ActiveCamera.isFrontFacing)
                        Core.flip(previewMatrix, previewMatrix, 1);
                    break;
            }
            // Orientation checking
            if (orientation != reference) {
                orientation = reference;
                firstFrame = true;
            }
            // Invoke client callbacks
            if (firstFrame) {
                startCallback();
                firstFrame = false;
            }
            frameCallback();
        }
        #endregion
    }
}