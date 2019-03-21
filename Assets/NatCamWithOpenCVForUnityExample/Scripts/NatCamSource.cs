using UnityEngine;
using System;
using OpenCVForUnity;
using NatCam;

namespace NatCamWithOpenCVForUnityExample {

    public class NatCamSource : ICameraSource {

        #region --Op vars--
        private Action startCallback, frameCallback;
        private byte[] sourceBuffer;
        #endregion
        

        #region --Client API--

        public int width {
            get { return Preview.width; }
        }
        public int height {
            get { return Preview.height; }
        }
        public Texture Preview { get; private set; }
        public DeviceCamera ActiveCamera { get; private set; }

        public NatCamSource (int width, int height, int framerate, bool front) {
            ActiveCamera = front ? DeviceCamera.FrontCamera : DeviceCamera.RearCamera;
            ActiveCamera.PreviewResolution = new Vector2Int(width, height);
            ActiveCamera.Framerate = framerate;
        }

        public void Dispose () {
            ActiveCamera.StopPreview();
            ActiveCamera = null;
            Preview = null;
            sourceBuffer = null;
        }

        public void StartPreview (Action startCallback, Action frameCallback) {
            this.startCallback = startCallback;
            this.frameCallback = frameCallback;
            ActiveCamera.StartPreview(
                preview => {
                    this.Preview = preview;
                    sourceBuffer = new byte[preview.width * preview.height * 4];
                    startCallback();
                },
                frameCallback
            );
        }

        public void CaptureFrame (Mat matrix) {
            ActiveCamera.CaptureFrame(sourceBuffer);
            Utils.copyToMat(sourceBuffer, matrix);
            Core.flip(matrix, matrix, 0);
        }

        public void CaptureFrame (Color32[] pixelBuffer) {
            ActiveCamera.CaptureFrame(sourceBuffer);
            Buffer.BlockCopy(sourceBuffer, 0, pixelBuffer, 0, sourceBuffer.Length);
        }

        public void SwitchCamera () {
            ActiveCamera.StopPreview();
            ActiveCamera = ActiveCamera.IsFrontFacing ? DeviceCamera.RearCamera : DeviceCamera.FrontCamera;
            StartPreview(startCallback, frameCallback);
        }
        #endregion
    }
}
