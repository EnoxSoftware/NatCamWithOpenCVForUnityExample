using UnityEngine;
using System;
using NatCam;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;

namespace NatCamWithOpenCVForUnityExample
{

    public class NatCamSource : ICameraSource
    {

        #region --Op vars--
        private Action startCallback, frameCallback;
        private int requestedWidth, requestedHeight, requestedFramerate;
        private int cameraIndex;
        private Texture2D previewTexture;
        #endregion


        #region --Client API--

        public int width {
            get; private set;
        }

        public int height {
            get; private set;
        }

        public bool isRunning {
            get { return cameraDevice.IsRunning; }
        }

        public Texture preview {
            get { return previewTexture; }
        }

        public CameraDevice cameraDevice {
            get; private set;
        }

        public NatCamSource (int width, int height, int framerate, bool front)
        {
            this.requestedWidth = width;
            this.requestedHeight = height;
            this.requestedFramerate = framerate;
            
            var cameraDevices = CameraDevice.GetDevices();
            for (; cameraIndex < cameraDevices.Length; cameraIndex++)
                if (cameraDevices[cameraIndex].IsFrontFacing == front)
                    break;

            if (cameraIndex == cameraDevices.Length) {
                Debug.LogError ("Camera is null. Consider using " + (front ? "rear" : "front") + " camera.");
                return;
            }

            cameraDevice = cameraDevices[cameraIndex];
            cameraDevice.PreviewResolution = new Vector2Int(width, height);
            cameraDevice.Framerate = framerate;
        }

        public void Dispose ()
        {
            if (cameraDevice.IsRunning)
                cameraDevice.StopPreview();
            previewTexture = null;
        }

        public void StartPreview (Action startCallback, Action frameCallback)
        {
            this.startCallback = startCallback;
            this.frameCallback = frameCallback;
            cameraDevice.StartPreview(
                (Texture2D preview) => {
                    previewTexture = preview;
                    width = preview.width;
                    height = preview.height;
                    startCallback ();
                },
                timestamp => frameCallback()
            );
        }

        public void CaptureFrame (Mat matrix)
        {
            Utils.texture2DToMat (previewTexture, matrix);
            //Core.flip (matrix, matrix, 0); // Shouldn't be necessary
        }

        public void CaptureFrame (Color32[] pixelBuffer)
        {
            previewTexture.GetRawTextureData<Color32>().CopyTo(pixelBuffer);
        }

        public void SwitchCamera ()
        {
            if (cameraDevice.IsRunning)
                cameraDevice.StopPreview ();
            var cameraDevices = CameraDevice.GetDevices();
            cameraIndex = ++cameraIndex % cameraDevices.Length;
            cameraDevice = cameraDevice[cameraIndex];
            cameraDevice.PreviewResolution = new Vector2Int(requestedWidth, requestedHeight);
            cameraDevice.Framerate = requestedFramerate;
            StartPreview(startCallback, frameCallback);
        }

        #endregion
    }
}
