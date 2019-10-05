using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if OPENCV_USE_UNSAFE_CODE && UNITY_2018_2_OR_NEWER
using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
#endif

namespace NatCamWithOpenCVForUnityExample
{

    /// <summary>
    /// NatCamPreview To Mat Example
    /// An example of converting a NatCam preview image to OpenCV's Mat format.
    /// </summary>
    public class NatCamPreviewToMatExample : ExampleBase<NatCamSource>
    {

        public enum MatCaptureMethod
        {
            GetRawTextureData_ByteArray,
            GetRawTextureData_NativeArray,
        }

        [Header("OpenCV")]
        public MatCaptureMethod matCaptureMethod = MatCaptureMethod.GetRawTextureData_ByteArray;
        public Dropdown matCaptureMethodDropdown;

        Mat frameMatrix;
        Mat grayMatrix;
        Texture2D texture;

        FpsMonitor fpsMonitor;

        #region --ExampleBase--

        protected override void Start()
        {
            // Load global camera benchmark settings.
            int width, height, framerate;
            NatCamWithOpenCVForUnityExample.CameraConfiguration(out width, out height, out framerate);
            NatCamWithOpenCVForUnityExample.ExampleSceneConfiguration(out performImageProcessingEachTime);
            // Create camera source
            cameraSource = new NatCamSource(width, height, framerate, useFrontCamera);
            if (!cameraSource.activeCamera)
                cameraSource = new NatCamSource(width, height, framerate, !useFrontCamera);
            cameraSource.StartPreview(OnStart, OnFrame);
            // Update UI
            imageProcessingTypeDropdown.value = (int)imageProcessingType;
            matCaptureMethodDropdown.value = (int)matCaptureMethod;

            fpsMonitor = GetComponent<FpsMonitor>();
            if (fpsMonitor != null)
            {
                fpsMonitor.Add("Name", "NatCamPreviewToMatExample");
                fpsMonitor.Add("performImageProcessingEveryTime", performImageProcessingEachTime.ToString());
                fpsMonitor.Add("onFrameFPS", onFrameFPS.ToString("F1"));
                fpsMonitor.Add("drawFPS", drawFPS.ToString("F1"));
                fpsMonitor.Add("width", "");
                fpsMonitor.Add("height", "");
                fpsMonitor.Add("orientation", "");
            }
        }

        protected override void OnStart()
        {
            base.OnStart();

            // Create matrices
            if (frameMatrix != null)
                frameMatrix.Dispose();
            frameMatrix = new Mat(cameraSource.height, cameraSource.width, CvType.CV_8UC4);
            if (grayMatrix != null)
                grayMatrix.Dispose();
            grayMatrix = new Mat(cameraSource.height, cameraSource.width, CvType.CV_8UC1);
            // Create texture
            if (texture != null)
                Texture2D.Destroy(texture);
            texture = new Texture2D(
                cameraSource.width,
                cameraSource.height,
                TextureFormat.RGBA32,
                false,
                false
            );
            // Display preview
            rawImage.texture = texture;
            aspectFitter.aspectRatio = cameraSource.width / (float)cameraSource.height;
            Debug.Log("NatCam camera source started with resolution: " + cameraSource.width + "x" + cameraSource.height + " isFrontFacing: " + cameraSource.isFrontFacing);
            // Log camera properties
            var camera = cameraSource.activeCamera;
            var cameraProps = new Dictionary<string, string>();
            cameraProps.Add("ExposureBias", camera.ExposureBias.ToString());
            cameraProps.Add("ExposureLock", camera.ExposureLock.ToString());
            cameraProps.Add("FlashMode", camera.FlashMode.ToString());
            cameraProps.Add("FocusLock", camera.FocusLock.ToString());
            cameraProps.Add("Framerate", camera.Framerate.ToString());
            cameraProps.Add("HorizontalFOV", camera.HorizontalFOV.ToString());
            cameraProps.Add("IsExposureLockSupported", camera.IsExposureLockSupported.ToString());
            cameraProps.Add("IsFlashSupported", camera.IsFlashSupported.ToString());
            cameraProps.Add("IsFocusLockSupported", camera.IsFocusLockSupported.ToString());
            cameraProps.Add("IsFrontFacing", camera.IsFrontFacing.ToString());
            //cameraProps.Add("IsRunning", camera.IsRunning.ToString());
            cameraProps.Add("IsTorchSupported", camera.IsTorchSupported.ToString());
            cameraProps.Add("IsWhiteBalanceLockSupported", camera.IsWhiteBalanceLockSupported.ToString());
            cameraProps.Add("MaxExposureBias", camera.MaxExposureBias.ToString());
            cameraProps.Add("MaxZoomRatio", camera.MaxZoomRatio.ToString());
            cameraProps.Add("MinExposureBias", camera.MinExposureBias.ToString());
            cameraProps.Add("PhotoResolution", camera.PhotoResolution.width + "x" + camera.PhotoResolution.height);
            cameraProps.Add("PreviewResolution", camera.PreviewResolution.width + "x" + camera.PreviewResolution.height);
            cameraProps.Add("TorchEnabled", camera.TorchEnabled.ToString());
            cameraProps.Add("UniqueID", camera.UniqueID.ToString());
            cameraProps.Add("VerticalFOV", camera.VerticalFOV.ToString());
            cameraProps.Add("WhiteBalanceLock", camera.WhiteBalanceLock.ToString());
            cameraProps.Add("ZoomRatio", camera.ZoomRatio.ToString());
            Debug.Log("# Active Camera Properties #####################");
            foreach (string key in cameraProps.Keys)
                Debug.Log(key + ": " + cameraProps[key]);
            Debug.Log("#######################################");

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", cameraSource.width.ToString());
                fpsMonitor.Add("height", cameraSource.height.ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());

                fpsMonitor.boxWidth = 240;
                fpsMonitor.boxHeight = 800;
                fpsMonitor.LocateGUI();

                foreach (string key in cameraProps.Keys)
                    fpsMonitor.Add(key, cameraProps[key]);
            }
        }

        protected override void Update()
        {
            base.Update();

            if (updateCount == 0)
            {
                if (fpsMonitor != null)
                {
                    fpsMonitor.Add("onFrameFPS", onFrameFPS.ToString("F1"));
                    fpsMonitor.Add("drawFPS", drawFPS.ToString("F1"));
                    fpsMonitor.Add("orientation", Screen.orientation.ToString());
                }
            }
        }

        protected override void UpdateTexture()
        {
            // Get the matrix
            switch (matCaptureMethod)
            {
                case MatCaptureMethod.GetRawTextureData_ByteArray:
                    Utils.copyToMat(cameraSource.preview.GetRawTextureData(), frameMatrix);
                    Core.flip(frameMatrix, frameMatrix, 0);
                    break;
                case MatCaptureMethod.GetRawTextureData_NativeArray:

#if OPENCV_USE_UNSAFE_CODE && UNITY_2018_2_OR_NEWER
                    // non-memory allocation.
                    unsafe
                    {
                        var ptr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(cameraSource.preview.GetRawTextureData<byte>());
                        Utils.copyToMat(ptr, frameMatrix);
                    }
                    Core.flip(frameMatrix, frameMatrix, 0);
#else
                    Utils.copyToMat(cameraSource.preview.GetRawTextureData(), frameMatrix);
                    Core.flip(frameMatrix, frameMatrix, 0);
                    Imgproc.putText(frameMatrix, "NativeArray<T> GetRawTextureData() method can be used from Unity 2018.2 or later.", new Point(5, frameMatrix.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, new Scalar(255, 255, 255, 255), 1, Imgproc.LINE_AA, false);
#endif
                    break;
            }

            ProcessImage(frameMatrix, grayMatrix, imageProcessingType);

            // Convert to Texture2D
            Utils.fastMatToTexture2D(frameMatrix, texture);
        }

        protected override void OnDestroy()
        {
            if (cameraSource != null)
            {
                cameraSource.Dispose();
                cameraSource = null;
            }
            if (frameMatrix != null)
                frameMatrix.Dispose();
            if (grayMatrix != null)
                grayMatrix.Dispose();
            frameMatrix =
            grayMatrix = null;
            Texture2D.Destroy(texture);
            texture = null;
        }

        #endregion


        #region --UI Callbacks--

        public void OnMatCaptureMethodDropdownValueChanged(int result)
        {
            matCaptureMethod = (MatCaptureMethod)result;
        }

        #endregion

    }
}