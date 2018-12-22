using UnityEngine;
using System;
using OpenCVForUnity;

namespace NatCamWithOpenCVForUnityExample {

    public class WebCamSource : ICameraSource {
        
        private WebCamTexture webCamTexture;
        private Action startCallback, frameCallback;
        private Texture2D uprightTexture;
        private Color32[] sourceBuffer, uprightBuffer;
        private int width, height, framerate;
        private int cameraIndex;
        private bool firstFrame;
        private readonly bool useOpenCVForOrientation;

        #region --Client API--

        public Texture Preview { get { return uprightTexture; } }
        public WebCamDevice ActiveCamera { get { return WebCamTexture.devices[cameraIndex]; } }

        public WebCamSource (int width, int height, int framerate = 30, bool front = false, bool useOpenCVForOrientation = false) {
            #if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices. (Pixel, pixel 2)
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            framerate = front ? 15 : framerate;
            #endif
            this.width = width;
            this.height = height;
            this.framerate = framerate;
            for (; cameraIndex < WebCamTexture.devices.Length; cameraIndex++)
                if (WebCamTexture.devices[cameraIndex].isFrontFacing == front)
                    break;
            this.webCamTexture = new WebCamTexture(ActiveCamera.name, width, height, framerate);
            this.useOpenCVForOrientation = useOpenCVForOrientation;
            Camera.onPostRender += OnFrame;
        }

        public void Dispose () {
            Camera.onPostRender -= OnFrame;
            Texture2D.Destroy(uprightTexture);
            webCamTexture.Stop();
            WebCamTexture.Destroy(webCamTexture);
        }

        public void StartPreview (Action startCallback, Action frameCallback) {
            this.startCallback = startCallback;
            this.frameCallback = frameCallback;
            sourceBuffer = null; // Lazily created
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

        public void SwitchCamera () {
            Dispose();
            cameraIndex = ++cameraIndex % WebCamTexture.devices.Length;
            webCamTexture = new WebCamTexture(ActiveCamera.name, width, height, framerate);
            StartPreview(startCallback, frameCallback);
        }
        #endregion


        #region --Operations--

        private void OnFrame (Camera camera) { // INCOMPLETE
            if (!webCamTexture || !webCamTexture.isPlaying)
                return;
            // Weird bug on macOS and macOS
            if (webCamTexture.width == 16 || webCamTexture.height == 16)
                return;
            // Create buffers and texture
            if (sourceBuffer == null) {
                sourceBuffer =  webCamTexture.GetPixels32();
                uprightBuffer = new Color32[sourceBuffer.Length];
                uprightTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false, false);
            }
            // Orient
            webCamTexture.GetPixels32(sourceBuffer);
            if (useOpenCVForOrientation) {
                // ...
            } else {
                // ...
            }
            Array.Copy(sourceBuffer, uprightBuffer, sourceBuffer.Length);
            // Invoke client callbacks
            if (firstFrame) {
                startCallback();
                firstFrame = false;
            }
            frameCallback();
        }
        #endregion


        #region --Utilities--

        private static void FlipVertical (Color32[] src, Color32[] dst, int width, int height) {
            // Flip by copying pixel rows
            for (int i = 0, j = height - i - 1; i < height; i++, j--)
                Buffer.BlockCopy(src, i * width, dst, j * width, width);
        }

        private static void FlipHorizontal (Color32[] src, Color32[] dst, int width, int height) {
            Array.Copy(src, dst, src.Length);
            // Flip by reversing pixel rows
            for (int i = 0; i < height; i++)
                Array.Reverse(dst, i * width, width);
        }

        private static void Rotate180 (Color32[] src, Color32[] dst) {
            Array.Copy(src, dst, src.Length);
            Array.Reverse(dst);
        }

        void Rotate90CW (Color32[] src, Color32[] dst, int width, int height) {
            for (int i = 0, x = height - 1; x >= 0; x--)
                for (int y = 0; y < width; y++, i++)
                    dst [i] = src [x + y * height];
        }

        void Rotate90CCW (Color32[] src, Color32[] dst, int width, int height) {
            for (int i = 0, x = 0; x < width; x++)
                for (int y = height - 1; y >= 0; y--, i++)
                    dst [i] = src [x + y * width];
        }
        #endregion
    }
}