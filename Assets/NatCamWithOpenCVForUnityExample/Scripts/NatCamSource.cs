using UnityEngine;
using System;
using System.Runtime.InteropServices;
using NatCam;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;

namespace NatCamWithOpenCVForUnityExample
{

    public class NatCamSource : ICameraSource
    {

        #region --Op vars--

        private DeviceCamera camera;
        private Action startCallback, frameCallback;
        private byte[] sourceBuffer;
        private int requestedWidth, requestedHeight, requestedFramerate;
        private int cameraIndex;

        #endregion


        #region --Client API--

        public int width { get; private set; }

        public int height { get; private set; }

        public bool isRunning { get { return camera.IsRunning; } }

        public Texture preview { get; private set; }

        public DeviceCamera activeCamera { get { return camera; } }

        public NatCamSource (int width, int height, int framerate, bool front)
        {
            this.requestedWidth = width;
            this.requestedHeight = height;
            this.requestedFramerate = framerate;

            for (; cameraIndex < DeviceCamera.Cameras.Length; cameraIndex++)
                if (DeviceCamera.Cameras [cameraIndex].IsFrontFacing == front)
                    break;

            if (cameraIndex == DeviceCamera.Cameras.Length) {
                Debug.LogError ("Camera is null. Consider using " + (front ? "rear" : "front") + " camera.");
                return;
            }

            camera = DeviceCamera.Cameras [cameraIndex];
            camera.PreviewResolution = new Vector2Int (width, height);
            camera.Framerate = framerate;
        }

        public void Dispose ()
        {
            if (camera.IsRunning)
                camera.StopPreview ();
            sourceBuffer = null;
            preview = null;
        }

        public void StartPreview (Action startCallback, Action frameCallback)
        {
            this.startCallback = startCallback;
            this.frameCallback = frameCallback;
            camera.StartPreview (
                (Texture preview) => {
                    width = preview.width;
                    height = preview.height;
                    this.preview = preview;
                    sourceBuffer = new byte[width * height * 4];
                    startCallback ();
                },
                frameCallback
            );
        }

        public void CaptureFrame (Mat matrix)
        {
            camera.CaptureFrame (sourceBuffer);
            Utils.copyToMat (sourceBuffer, matrix);
            Core.flip (matrix, matrix, 0);
        }

        public void CaptureFrame (Color32[] pixelBuffer)
        {
            camera.CaptureFrame (sourceBuffer);

            GCHandle pin = GCHandle.Alloc (pixelBuffer, GCHandleType.Pinned);
            Marshal.Copy (sourceBuffer, 0, pin.AddrOfPinnedObject (), sourceBuffer.Length);
            pin.Free ();
        }

        public void SwitchCamera ()
        {
            if (camera.IsRunning)
                camera.StopPreview ();

            cameraIndex = ++cameraIndex % WebCamTexture.devices.Length;
            camera = DeviceCamera.Cameras [cameraIndex];
            camera.PreviewResolution = new Vector2Int (requestedWidth, requestedHeight);
            camera.Framerate = requestedFramerate;
            StartPreview (startCallback, frameCallback);
        }

        #endregion
    }
}
