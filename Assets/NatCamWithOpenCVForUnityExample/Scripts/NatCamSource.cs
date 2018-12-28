using UnityEngine;
using System;
using OpenCVForUnity;
using NatCamU.Core;

namespace NatCamWithOpenCVForUnityExample {

    public class NatCamSource : ICameraSource {

        #region --Op vars--
        private DeviceCamera camera;
        private Action startCallback, frameCallback;
        private byte[] sourceBuffer;
        #endregion
        

        #region --Client API--

        public int width { get { return NatCam.Preview.width; } }
        public int height { get { return NatCam.Preview.height; }}
        public Texture Preview { get { return NatCam.Preview; }}
        public DeviceCamera ActiveCamera { get { return NatCam.Camera; }}

        public NatCamSource (int width, int height, int framerate, bool front) {
            camera = front ? DeviceCamera.FrontCamera : DeviceCamera.RearCamera;
            camera.PreviewResolution = new Vector2Int(width, height);
            camera.Framerate = framerate;
        }

        public void Dispose () {
            NatCam.StopPreview();
            sourceBuffer = null;
        }

        public void StartPreview (Action startCallback, Action frameCallback) {
            this.startCallback = startCallback;
            this.frameCallback = frameCallback;
            NatCam.StartPreview(
                camera,
                () => {
                    sourceBuffer = new byte[width * height * 4];
                    startCallback();
                },
                frameCallback
            );
        }

        public void CaptureFrame (Mat matrix) {
            NatCam.CaptureFrame(sourceBuffer);
            Utils.copyToMat(sourceBuffer, matrix);
            Core.flip(matrix, matrix, 0);
        }

        public void CaptureFrame (Color32[] pixelBuffer) {
            NatCam.CaptureFrame(sourceBuffer);
            Buffer.BlockCopy(sourceBuffer, 0, pixelBuffer, 0, sourceBuffer.Length);
        }

        public void SwitchCamera () {
            camera = ++camera % DeviceCamera.Cameras.Length;
            StartPreview(startCallback, frameCallback);
        }
        #endregion
    }
}