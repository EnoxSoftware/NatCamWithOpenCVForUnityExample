using UnityEngine;
using System;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;

namespace NatCamWithOpenCVForUnityExample
{

    public class WebCamSource : ICameraSource
    {
        
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

        public bool isRunning { get { return (webCamTexture == null) ? false : webCamTexture.isPlaying; } }

        public WebCamDevice activeCamera { get { return WebCamTexture.devices [cameraIndex]; } }

        public WebCamSource (int width, int height, int framerate = 30, bool front = false)
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
                Debug.LogError ("Camera is null. Consider using " + (front ? "rear" : "front") + " camera.");
                return;
            }

            this.webCamTexture = new WebCamTexture (activeCamera.name, width, height, framerate);
        }

        public void Dispose ()
        {
            Camera.onPostRender -= OnFrame;
            webCamTexture.Stop ();
            WebCamTexture.Destroy (webCamTexture);
            webCamTexture = null;
            sourceBuffer = null;
            uprightBuffer = null;
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
            Utils.copyToMat (uprightBuffer, matrix);
            Core.flip (matrix, matrix, 0);
        }

        public void CaptureFrame (Color32[] pixelBuffer)
        {
            Array.Copy (uprightBuffer, pixelBuffer, uprightBuffer.Length);
        }

        public void SwitchCamera ()
        {
            Dispose ();
            cameraIndex = ++cameraIndex % WebCamTexture.devices.Length;

            bool front = activeCamera.isFrontFacing;
            int framerate = requestedFramerate;
            #if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices. (Pixel, Pixel 2)
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            framerate = front ? 15 : framerate;
            #endif
            webCamTexture = new WebCamTexture (activeCamera.name, requestedWidth, requestedHeight, framerate);
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
            // Check buffers
            sourceBuffer = sourceBuffer ?? webCamTexture.GetPixels32 ();
            uprightBuffer = uprightBuffer ?? new Color32[sourceBuffer.Length];
            webCamTexture.GetPixels32 (sourceBuffer);

            // Update buffers
            var reference = (DeviceOrientation)(int)Screen.orientation;
            bool rotate90Degree = false;
            #if !UNITY_EDITOR && !(UNITY_STANDALONE || UNITY_WEBGL) 
            switch (reference) {
            case DeviceOrientation.LandscapeLeft:
            case DeviceOrientation.LandscapeRight:
                width = webCamTexture.width;
                height = webCamTexture.height;
                break;
            case DeviceOrientation.Portrait:
            case DeviceOrientation.PortraitUpsideDown:
                width = webCamTexture.height;
                height = webCamTexture.width;
                rotate90Degree = true;
                break;
            }
            #else
            width = webCamTexture.width;
            height = webCamTexture.height;
            #endif


            int flipCode = int.MinValue;
            if (activeCamera.isFrontFacing) {
                if (webCamTexture.videoRotationAngle == 0 || webCamTexture.videoRotationAngle == 90) {
                    flipCode = 1;
                } else if (webCamTexture.videoRotationAngle == 180 || webCamTexture.videoRotationAngle == 270) {
                    flipCode = 0;
                }
            } else {
                if (webCamTexture.videoRotationAngle == 180 || webCamTexture.videoRotationAngle == 270) {
                    flipCode = -1;
                }
            }                

            if (rotate90Degree && activeCamera.isFrontFacing) {
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
                if (flipCode == 0) {
                    FlipVertical (sourceBuffer, webCamTexture.width, webCamTexture.height, sourceBuffer);
                } else if (flipCode == 1) {
                    FlipHorizontal (sourceBuffer, webCamTexture.width, webCamTexture.height, sourceBuffer);
                } else if (flipCode == -1) {
                    Rotate (sourceBuffer, webCamTexture.width, webCamTexture.height, sourceBuffer, 2);
                }
            }

            if (rotate90Degree) {
                Rotate (sourceBuffer, webCamTexture.width, webCamTexture.height, uprightBuffer, 1);
            } else {
                Array.Copy (sourceBuffer, uprightBuffer, sourceBuffer.Length);
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


        #region --Utilities--

        private static void FlipVertical (Color32[] src, int width, int height, Color32[] dst)
        {
            for (var i = 0; i < height / 2; i++) {
                var y = i * width;
                var x = (height - i - 1) * width;
                for (var j = 0; j < width; j++) {
                    int s = y + j;
                    int t = x + j;
                    Color32 c = src [s];
                    dst [s] = src [t];
                    dst [t] = c;
                }
            }
        }

        private static void FlipHorizontal (Color32[] src, int width, int height, Color32[] dst)
        {
            for (int i = 0; i < height; i++) {
                int y = i * width;
                int x = y + width - 1;
                for (var j = 0; j < width / 2; j++) {
                    int s = y + j;
                    int t = x - j;
                    Color32 c = src [s];
                    dst [s] = src [t];
                    dst [t] = c;
                }
            }
        }

        private static void Rotate (Color32[] src, int srcWidth, int srcHeight, Color32[] dst, int rotation)
        {
            int i;
            switch (rotation) {
            case 0:
                Array.Copy (src, dst, src.Length);
                break;
            case 1:
                // Rotate 90 degrees (CLOCKWISE)
                i = 0;
                for (int x = srcWidth - 1; x >= 0; x--) {
                    for (int y = 0; y < srcHeight; y++) {
                        dst [i] = src [x + y * srcWidth];
                        i++;
                    }
                }
                break;
            case 2:
                // Rotate 180 degrees
                i = src.Length;
                for (int x = 0; x < i / 2; x++) {
                    Color32 t = src [x];
                    dst [x] = src [i - x - 1];
                    dst [i - x - 1] = t;
                }
                break;
            case 3:
                // Rotate 90 degrees (COUNTERCLOCKWISE)
                i = 0;
                for (int x = 0; x < srcWidth; x++) {
                    for (int y = srcHeight - 1; y >= 0; y--) {
                        dst [i] = src [x + y * srcWidth];
                        i++;
                    }
                }
                break;
            }
        }

        #endregion
    }
}