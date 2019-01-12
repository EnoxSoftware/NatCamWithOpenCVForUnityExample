using UnityEngine;
using System;
using OpenCVForUnity;
using NatCamU.Core;
using System.Runtime.InteropServices;

namespace NatCamWithOpenCVForUnityExample
{

    public class NatCamSource : ICameraSource
    {

        #region --Op vars--

        private DeviceCamera camera;
        private Action startCallback, frameCallback;
        private byte[] sourceBuffer;
        private int requestedWidth, requestedHeight, requestedFramerate;

        #endregion


        #region --Client API--

        public int width { get { return NatCam.Preview.width; } }

        public int height { get { return NatCam.Preview.height; } }

        public bool isRunning { get { return NatCam.IsRunning; } }

        public Texture Preview { get { return NatCam.Preview; } }

        public DeviceCamera ActiveCamera { get { return NatCam.Camera; } }

        public NatCamSource (int width, int height, int framerate, bool front)
        {
            this.requestedWidth = width;
            this.requestedHeight = height;
            this.requestedFramerate = framerate;

            camera = front ? DeviceCamera.FrontCamera : DeviceCamera.RearCamera;

            if (!camera) {
                Debug.LogError ((front ? "front" : "rear") + " camera does not exist. Consider using " + (front ? "rear" : "front") + " camera.");
                return;
            }

            camera.PreviewResolution = new Vector2Int (width, height);
            camera.Framerate = framerate;
        }

        public void Dispose ()
        {
            if (NatCam.IsRunning)
                NatCam.StopPreview ();
            sourceBuffer = null;
        }

        public void StartPreview (Action startCallback, Action frameCallback)
        {
            this.startCallback = startCallback;
            this.frameCallback = frameCallback;
            NatCam.StartPreview (
                camera,
                () => {
                    sourceBuffer = new byte[width * height * 4];
                    startCallback ();
                },
                frameCallback
            );
        }

        public void CaptureFrame (Mat matrix)
        {
            NatCam.CaptureFrame (sourceBuffer);
            Utils.copyToMat (sourceBuffer, matrix);
            Core.flip (matrix, matrix, 0);
        }

        public void CaptureFrame (Color32[] pixelBuffer)
        {
            NatCam.CaptureFrame (sourceBuffer);

            GCHandle pin = GCHandle.Alloc (pixelBuffer, GCHandleType.Pinned);
            Marshal.Copy (sourceBuffer, 0, pin.AddrOfPinnedObject (), sourceBuffer.Length);
            pin.Free ();
        }

        public void SwitchCamera ()
        {
            if (NatCam.IsRunning)
                NatCam.StopPreview ();
            int index = camera;
            camera = ++index % DeviceCamera.Cameras.Length;
            camera.PreviewResolution = new Vector2Int (requestedWidth, requestedHeight);
            camera.Framerate = requestedFramerate;
            StartPreview (startCallback, frameCallback);
        }

        #endregion
    }
}