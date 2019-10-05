using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using System;
using UnityEngine;

namespace NatCamWithOpenCVForUnityExample
{

    public class WebCamMatSource : ICameraSource
    {

        #region --Op vars--

        private WebCamDevice cameraDevice;
        private Action startCallback, frameCallback;
        private Mat sourceMatrix, uprightMatrix;
        private Color32[] bufferColors;
        private int requestedWidth, requestedHeight, requestedFramerate;
        private int cameraIndex;
        private bool firstFrame;
        private DeviceOrientation orientation;
        private bool rotate90Degree = false;

        #endregion


        #region --Client API--

        public int width { get { return uprightMatrix.width(); } }

        public int height { get { return uprightMatrix.height(); } }

        public bool isRunning { get { return (activeCamera && !firstFrame) ? activeCamera.isPlaying : false; } }

        public bool isFrontFacing { get { return activeCamera ? cameraDevice.isFrontFacing : false; } }

        public WebCamTexture activeCamera { get; private set; }

        public WebCamMatSource(int width, int height, int framerate = 30, bool front = false)
        {
            requestedWidth = width;
            requestedHeight = height;
            requestedFramerate = framerate;
#if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices. (Pixel, Pixel 2)
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            framerate = front ? 15 : framerate;
#endif

            var devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Debug.Log("Camera device does not exist");
                return;
            }

            // Pick camera
            for (; cameraIndex < devices.Length; cameraIndex++)
                if (devices[cameraIndex].isFrontFacing == front)
                    break;

            if (cameraIndex == devices.Length)
            {
                Debug.LogError("Camera is null. Consider using " + (front ? "rear" : "front") + " camera.");
                return;
            }

            cameraDevice = devices[cameraIndex];
            activeCamera = new WebCamTexture(cameraDevice.name, requestedWidth, requestedHeight, framerate);
        }

        public void Dispose()
        {
            Camera.onPostRender -= OnFrame;
            if (activeCamera != null)
            {
                activeCamera.Stop();
                WebCamTexture.Destroy(activeCamera);
                activeCamera = null;
            }

            if (sourceMatrix != null)
                sourceMatrix.Dispose();
            if (uprightMatrix != null)
                uprightMatrix.Dispose();
            sourceMatrix =
            uprightMatrix = null;
            bufferColors = null;
        }

        public void StartPreview(Action startCallback, Action frameCallback)
        {
            if (activeCamera == null)
                return;

            this.startCallback = startCallback;
            this.frameCallback = frameCallback;
            Camera.onPostRender += OnFrame;
            firstFrame = true;
            activeCamera.Play();
        }

        public void CaptureFrame(Mat matrix)
        {
            if (uprightMatrix == null) return;

            uprightMatrix.copyTo(matrix);
        }

        public void CaptureFrame(Color32[] pixelBuffer)
        {
            if (uprightMatrix == null) return;

            Utils.copyFromMat(uprightMatrix, pixelBuffer);
        }

        public void CaptureFrame(byte[] pixelBuffer)
        {
            if (uprightMatrix == null) return;

            Utils.copyFromMat(uprightMatrix, pixelBuffer);
        }

        public void SwitchCamera()
        {
            if (activeCamera == null)
                return;

            Dispose();

            var devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Debug.Log("Camera device does not exist");
                return;
            }

            cameraIndex = ++cameraIndex % devices.Length;
            cameraDevice = devices[cameraIndex];

            bool front = cameraDevice.isFrontFacing;
            int framerate = requestedFramerate;
#if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices. (Pixel, Pixel 2)
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            framerate = front ? 15 : framerate;
#endif
            activeCamera = new WebCamTexture(cameraDevice.name, requestedWidth, requestedHeight, framerate);
            StartPreview(startCallback, frameCallback);
        }

        #endregion


        #region --Operations--

        private void OnFrame(Camera camera)
        {
            if (!activeCamera.isPlaying || !activeCamera.didUpdateThisFrame)
                return;

            // Check matrix
            sourceMatrix = sourceMatrix ?? new Mat(activeCamera.height, activeCamera.width, CvType.CV_8UC4);
            uprightMatrix = uprightMatrix ?? new Mat();
            bufferColors = bufferColors ?? new Color32[activeCamera.width * activeCamera.height];

            // Update matrix
            Utils.webCamTextureToMat(activeCamera, sourceMatrix, bufferColors, false);


            if (firstFrame)
            {
                rotate90Degree = false;
                var reference = (DeviceOrientation)(int)Screen.orientation;

#if !UNITY_EDITOR && !(UNITY_STANDALONE || UNITY_WEBGL)
                switch (reference)
                {
                    case DeviceOrientation.LandscapeLeft:
                    case DeviceOrientation.LandscapeRight:
                        break;
                    case DeviceOrientation.Portrait:
                    case DeviceOrientation.PortraitUpsideDown:
                        rotate90Degree = true;
                        break;
                }
#endif
            }

            int flipCode = 0;
            if (cameraDevice.isFrontFacing)
            {
                if (activeCamera.videoRotationAngle == 0 || activeCamera.videoRotationAngle == 90)
                {
                    flipCode = -1;
                }
                else if (activeCamera.videoRotationAngle == 180 || activeCamera.videoRotationAngle == 270)
                {
                    flipCode = int.MinValue;
                }
            }
            else
            {
                if (activeCamera.videoRotationAngle == 180 || activeCamera.videoRotationAngle == 270)
                {
                    flipCode = 1;
                }
            }

            if (rotate90Degree && cameraDevice.isFrontFacing)
            {
                if (flipCode == int.MinValue)
                {
                    flipCode = -1;
                }
                else if (flipCode == 0)
                {
                    flipCode = 1;
                }
                else if (flipCode == 1)
                {
                    flipCode = 0;
                }
                else if (flipCode == -1)
                {
                    flipCode = int.MinValue;
                }
            }

            if (flipCode > int.MinValue)
            {
                Core.flip(sourceMatrix, sourceMatrix, flipCode);
            }

            if (rotate90Degree)
            {
                Core.rotate(sourceMatrix, uprightMatrix, Core.ROTATE_90_CLOCKWISE);
            }
            else
            {
                sourceMatrix.copyTo(uprightMatrix);
            }


            // Invoke client callbacks
            if (firstFrame)
            {
                startCallback();
                firstFrame = false;
            }

            frameCallback();
        }

        #endregion
    }
}