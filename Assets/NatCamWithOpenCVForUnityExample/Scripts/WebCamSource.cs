using UnityEngine;
using System;
using OpenCVForUnity;

namespace NatCamWithOpenCVForUnityExample {

    public class WebCamSource : ICameraSource {
        
        #region --Op vars--
        private WebCamTexture webCamTexture;
        private Action startCallback, frameCallback;
        private Color32[] sourceBuffer, uprightBuffer;
        private int requestedWidth, requestedHeight, requestedFramerate;
        private int cameraIndex;
        private bool firstFrame;
        private DeviceOrientation orientation;
        #endregion
        

        #region --Client API--

        public int width { get; private set; }
        public int height { get; private set; }
        public WebCamDevice ActiveCamera { get { return WebCamTexture.devices[cameraIndex]; } }

        public WebCamSource (int width, int height, int framerate = 30, bool front = false) {
            #if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices. (Pixel, Pixel 2)
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            framerate = front ? 15 : framerate;
            #endif
            this.requestedWidth = width;
            this.requestedHeight = height;
            this.requestedFramerate = framerate;
            for (; cameraIndex < WebCamTexture.devices.Length; cameraIndex++)
                if (WebCamTexture.devices[cameraIndex].isFrontFacing == front)
                    break;
            this.webCamTexture = new WebCamTexture(ActiveCamera.name, width, height, framerate);
        }

        public void Dispose () {
            Camera.onPostRender -= OnFrame;
            webCamTexture.Stop();
            WebCamTexture.Destroy(webCamTexture);
            webCamTexture = null;
            sourceBuffer = null;
            uprightBuffer = null;
        }

        public void StartPreview (Action startCallback, Action frameCallback) {
            this.startCallback = startCallback;
            this.frameCallback = frameCallback;
            Camera.onPostRender += OnFrame;
            firstFrame = true;
            webCamTexture.Play();
        }

        public void CaptureFrame (Mat matrix) {
            Utils.copyToMat(uprightBuffer, matrix);
            Core.flip(matrix, matrix, 0);
        }

        public void CaptureFrame (Color32[] pixelBuffer) {
            Array.Copy(uprightBuffer, pixelBuffer, uprightBuffer.Length);
        }

        public void SwitchCamera () { // INCOMPLETE
            Dispose();
            cameraIndex = ++cameraIndex % WebCamTexture.devices.Length;
            webCamTexture = new WebCamTexture(ActiveCamera.name, requestedWidth, requestedHeight, requestedFramerate);
            StartPreview(startCallback, frameCallback);
        }
        #endregion


        #region --Operations--

        private void OnFrame (Camera camera) { // INCOMPLETE // Flipping
            if (!webCamTexture.isPlaying)
                return;
            // Weird bug on macOS and macOS
            if (webCamTexture.width == 16 || webCamTexture.height == 16)
                return;
            // Check buffers
            sourceBuffer = sourceBuffer ?? webCamTexture.GetPixels32();
            uprightBuffer = uprightBuffer ?? new Color32[sourceBuffer.Length];
            webCamTexture.GetPixels32(sourceBuffer);
            // Update buffers
            var reference = (DeviceOrientation)(int)Screen.orientation;
            switch (reference) {
                case DeviceOrientation.LandscapeLeft:
                    Rotate(sourceBuffer, webCamTexture.width, webCamTexture.height, uprightBuffer, 0);
                    width = webCamTexture.width;
                    height = webCamTexture.height;
                    break;
                case DeviceOrientation.Portrait:
                    Rotate(sourceBuffer, webCamTexture.width, webCamTexture.height, uprightBuffer, 1);
                    width = webCamTexture.height;
                    height = webCamTexture.width;
                    break;
                case DeviceOrientation.LandscapeRight:
                    Rotate(sourceBuffer, webCamTexture.width, webCamTexture.height, uprightBuffer, 2);
                    width = webCamTexture.width;
                    height = webCamTexture.height;
                    break;
                case DeviceOrientation.PortraitUpsideDown:
                    Rotate(sourceBuffer, webCamTexture.width, webCamTexture.height, uprightBuffer, 3);
                    width = webCamTexture.height;
                    height = webCamTexture.width;
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


        #region --Utilities--

        private static void FlipVertical (Color32[] src, int srcWidth, int srcHeight, Color32[] dst) {
            // Flip by copying pixel rows
            for (int i = 0, j = srcHeight - i - 1; i < srcHeight; i++, j--)
                Buffer.BlockCopy(src, i * srcWidth, dst, j * srcWidth, srcWidth);
        }

        private static void FlipHorizontal (Color32[] src, int srcWidth, int srcHeight, Color32[] dst) {
            Array.Copy(src, dst, src.Length);
            // Flip by reversing pixel rows
            for (int i = 0; i < srcHeight; i++)
                Array.Reverse(dst, i * srcWidth, srcWidth);
        }

        private static void Rotate (Color32[] src, int srcWidth, int srcHeight, Color32[] dst, int rotation) {
            Func<int, int> kernel90 = i => srcHeight * (srcWidth - 1 - i % srcWidth) + i / srcWidth;
            switch (rotation) {
                case 0:
                    Array.Copy(src, dst, src.Length);
                    break;
                case 1:
                    for (int i = 0; i < src.Length; i++)
                        dst[kernel90(i)] = src[i];
                    break;
                case 2:
                    Array.Copy(src, dst, src.Length);
                    Array.Reverse(dst);
                    break;
                case 3:
                    for (int i = 0; i < src.Length; i++)
                        dst[kernel90(i)] = src[i];
                    Array.Reverse(dst);
                    break;
            }
        }
        #endregion
    }
}