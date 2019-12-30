using NatCam;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

#if OPENCV_USE_UNSAFE_CODE && UNITY_2018_2_OR_NEWER
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
#endif

namespace NatCamWithOpenCVForUnityExample
{

    public class NatCamSource : ICameraSource
    {

        #region --Op vars--

        private Action startCallback, frameCallback;
        private int requestedWidth, requestedHeight, requestedFramerate;
        private int cameraIndex;

        #endregion


        #region --Client API--

        public int width { get; private set; }

        public int height { get; private set; }

        public bool isRunning { get { return activeCamera != null ? activeCamera.IsRunning : false; } }

        public bool isFrontFacing { get { return activeCamera != null ? activeCamera.IsFrontFacing : false; } }

        public Texture2D preview { get; private set; }

        public CameraDevice activeCamera { get; private set; }

        public NatCamSource(int width, int height, int framerate, bool front)
        {
            requestedWidth = width;
            requestedHeight = height;
            requestedFramerate = framerate;

            // Check permission
            var devices = CameraDevice.GetDevices();
            if (devices == null)
            {
                Debug.Log("User has not granted camera permission");
                return;
            }
            // Pick camera
            for (; cameraIndex < devices.Length; cameraIndex++)
                if (devices[cameraIndex].IsFrontFacing == front)
                    break;

            if (cameraIndex == devices.Length)
            {
                Debug.LogError("Camera is null. Consider using " + (front ? "rear" : "front") + " camera.");
                return;
            }

            activeCamera = devices[cameraIndex];
            activeCamera.PreviewResolution = (width: requestedWidth, height: requestedHeight);
            activeCamera.Framerate = requestedFramerate;
        }

        public void Dispose()
        {
            if (activeCamera != null && activeCamera.IsRunning)
            {

                //Debug.Log("##### activeCamera.StopPreview() #####");

                activeCamera.StopPreview();
                activeCamera = null;
            }
            cameraIndex = 0;
            preview = null;
            this.startCallback = null;
            this.frameCallback = null;

        }

        public void StartPreview(Action startCallback, Action frameCallback)
        {
            if (activeCamera == null)
                return;

            this.startCallback = startCallback;
            this.frameCallback = frameCallback;
            activeCamera.StartPreview(
                (Texture2D preview) =>
                {
                    width = preview.width;
                    height = preview.height;
                    this.preview = preview;
                    startCallback();
                },
                (long timestamp) =>
                {
                    frameCallback();
                }
            );

            //Debug.Log("##### activeCamera.StartPreview() #####");

        }

        public void CaptureFrame(Mat matrix)
        {
#if OPENCV_USE_UNSAFE_CODE && UNITY_2018_2_OR_NEWER
            unsafe
            {
                var ptr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(preview.GetRawTextureData<byte>());
                Utils.copyToMat(ptr, matrix);
            }
            Core.flip(matrix, matrix, 0);
#else
            Utils.copyToMat(preview.GetRawTextureData(), matrix);
            Core.flip(matrix, matrix, 0);
#endif
        }

        public void CaptureFrame(Color32[] pixelBuffer)
        {
#if OPENCV_USE_UNSAFE_CODE && UNITY_2018_2_OR_NEWER
            unsafe
            {
                NativeArray<Color32> rawTextureData = preview.GetRawTextureData<Color32>();
                int size = UnsafeUtility.SizeOf<Color32>() * rawTextureData.Length;
                Color32* srcAddr = (Color32*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(rawTextureData);

                fixed (Color32* dstAddr = pixelBuffer)
                {
                    UnsafeUtility.MemCpy(dstAddr, srcAddr, size);
                }
            }
#else
            byte[] rawTextureData = preview.GetRawTextureData();
            GCHandle pin = GCHandle.Alloc(pixelBuffer, GCHandleType.Pinned);
            Marshal.Copy(rawTextureData, 0, pin.AddrOfPinnedObject(), rawTextureData.Length);
            pin.Free();
#endif
        }

        public void CaptureFrame(byte[] pixelBuffer)
        {
#if OPENCV_USE_UNSAFE_CODE && UNITY_2018_2_OR_NEWER
            unsafe
            {
                NativeArray<byte> rawTextureData = preview.GetRawTextureData<byte>();
                int size = UnsafeUtility.SizeOf<byte>() * rawTextureData.Length;
                byte* srcAddr = (byte*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(rawTextureData);

                fixed (byte* dstAddr = pixelBuffer)
                {
                    UnsafeUtility.MemCpy(dstAddr, srcAddr, size);
                }
            }
#else
            byte[] rawTextureData = preview.GetRawTextureData();
            Buffer.BlockCopy(rawTextureData, 0, pixelBuffer, 0, rawTextureData.Length);
#endif
        }

        public void SwitchCamera()
        {
            if (activeCamera == null)
                return;

            if (activeCamera != null && activeCamera.IsRunning)
                activeCamera.StopPreview();

            var devices = CameraDevice.GetDevices();
            if (devices == null)
            {
                Debug.Log("User has not granted camera permission");
                return;
            }

            cameraIndex = ++cameraIndex % devices.Length;
            activeCamera = devices[cameraIndex];
            activeCamera.PreviewResolution = (width: requestedWidth, height: requestedHeight);
            activeCamera.Framerate = requestedFramerate;
            StartPreview(startCallback, frameCallback);
        }

        #endregion
    }
}