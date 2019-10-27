using NatCam;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System;
using System.Collections;
using UnityEngine;

namespace NatCamWithOpenCVForUnity.UnityUtils.Helper
{
    /// <summary>
    /// NatCamPreview to mat helper.
    /// v 1.0.8
    /// Depends on NatCam version 2.3 or later.
    /// Depends on OpenCVForUnity version 2.3.7 or later.
    /// </summary>
    public class NatCamPreviewToMatHelper : WebCamTextureToMatHelper
    {
        public override float requestedFPS
        {
            get { return _requestedFPS; }
            set
            {
                _requestedFPS = Mathf.Clamp(value, -1f, float.MaxValue);
                if (hasInitDone)
                {
                    Initialize();
                }
            }
        }

        protected bool didUpdateThisFrame = false;

        protected CameraDevice natCamCameraDevice;
        protected Texture2D preview;

        /// <summary>
        /// Method called when the camera preview starts
        /// </summary>
        public virtual void OnStart(Texture2D preview)
        {
            this.preview = preview;

            if (hasInitDone)
            {
                if (onDisposed != null)
                    onDisposed.Invoke();

                if (frameMat != null)
                {
                    frameMat.Dispose();
                    frameMat = null;
                }
                if (rotatedFrameMat != null)
                {
                    rotatedFrameMat.Dispose();
                    rotatedFrameMat = null;
                }

                frameMat = new Mat(preview.height, preview.width, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));

                if (rotate90Degree)
                    rotatedFrameMat = new Mat(preview.width, preview.height, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));

                if (onInitialized != null)
                    onInitialized.Invoke();
            }
        }

        /// <summary>
        /// Method called on every frame that the camera preview updates
        /// </summary>
        public virtual void OnFrame(long timestamp)
        {
            didUpdateThisFrame = true;
        }

        // Update is called once per frame
        protected override void Update()
        {
            if (hasInitDone)
            {
                // Catch the orientation change of the screen and correct the mat image to the correct direction.
                if (screenOrientation != Screen.orientation && (screenWidth != Screen.width || screenHeight != Screen.height))
                {
                    Initialize();
                }
                else
                {
                    screenWidth = Screen.width;
                    screenHeight = Screen.height;
                }
            }
        }

        public virtual void LateUpdate()
        {
            didUpdateThisFrame = false;
        }

        /// <summary>
        /// Initializes this instance by coroutine.
        /// </summary>
        protected override IEnumerator _Initialize()
        {
            if (hasInitDone)
            {
                ReleaseResources();

                if (onDisposed != null)
                    onDisposed.Invoke();
            }

            isInitWaiting = true;

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            // Checks camera permission state.
            IEnumerator coroutine = hasUserAuthorizedCameraPermission();
            yield return coroutine;

            if (!(bool)coroutine.Current)
            {
                isInitWaiting = false;
                initCoroutine = null;

                if (onErrorOccurred != null)
                    onErrorOccurred.Invoke(ErrorCode.CAMERA_PERMISSION_DENIED);

                yield break;
            }
#endif

            // Creates the camera
            var devices = CameraDevice.GetDevices();
            if (!String.IsNullOrEmpty(requestedDeviceName))
            {
                int requestedDeviceIndex = -1;
                if (Int32.TryParse(requestedDeviceName, out requestedDeviceIndex))
                {
                    if (requestedDeviceIndex >= 0 && requestedDeviceIndex < devices.Length)
                    {
                        natCamCameraDevice = devices[requestedDeviceIndex];
                    }
                }
                else
                {
                    for (int cameraIndex = 0; cameraIndex < devices.Length; cameraIndex++)
                    {
                        if (devices[cameraIndex].UniqueID == requestedDeviceName)
                        {
                            natCamCameraDevice = devices[cameraIndex];
                            break;
                        }
                    }
                }
                if (natCamCameraDevice == null)
                    Debug.Log("Cannot find camera device " + requestedDeviceName + ".");
            }

            if (natCamCameraDevice == null)
            {
                // Checks how many and which cameras are available on the device
                for (int cameraIndex = 0; cameraIndex < devices.Length; cameraIndex++)
                {
                    if (devices[cameraIndex].IsFrontFacing == requestedIsFrontFacing)
                    {
                        natCamCameraDevice = devices[cameraIndex];
                        break;
                    }
                }
            }

            if (natCamCameraDevice == null)
            {
                if (devices.Length > 0)
                {
                    natCamCameraDevice = devices[0];
                }
                else
                {
                    isInitWaiting = false;
                    initCoroutine = null;

                    if (onErrorOccurred != null)
                        onErrorOccurred.Invoke(ErrorCode.CAMERA_DEVICE_NOT_EXIST);

                    yield break;
                }
            }

            natCamCameraDevice.Framerate = (int)requestedFPS;

            // Set the camera's preview resolution
            natCamCameraDevice.PreviewResolution = new Resolution { width = requestedWidth, height = requestedHeight };

            // Starts the camera
            // Register callback for when the preview starts
            // Register for preview updates
            natCamCameraDevice.StartPreview(OnStart, OnFrame);

            int initFrameCount = 0;
            bool isTimeout = false;

            while (true)
            {
                if (initFrameCount > timeoutFrameCount)
                {
                    isTimeout = true;
                    break;
                }
                else if (didUpdateThisFrame)
                {

                    Debug.Log("NatCamPreviewToMatHelper:: " + "UniqueID:" + natCamCameraDevice.UniqueID + " width:" + preview.width + " height:" + preview.height + " fps:" + natCamCameraDevice.Framerate
                    + " isFrongFacing:" + natCamCameraDevice.IsFrontFacing);

                    frameMat = new Mat(preview.height, preview.width, CvType.CV_8UC4);

                    screenOrientation = Screen.orientation;
                    screenWidth = Screen.width;
                    screenHeight = Screen.height;

                    if (rotate90Degree)
                        rotatedFrameMat = new Mat(preview.width, preview.height, CvType.CV_8UC4);

                    isInitWaiting = false;
                    hasInitDone = true;
                    initCoroutine = null;

                    if (onInitialized != null)
                        onInitialized.Invoke();

                    break;
                }
                else
                {
                    initFrameCount++;
                    yield return null;
                }
            }

            if (isTimeout)
            {
                if (natCamCameraDevice.IsRunning)
                    natCamCameraDevice.StopPreview();

                isInitWaiting = false;
                initCoroutine = null;

                if (onErrorOccurred != null)
                    onErrorOccurred.Invoke(ErrorCode.TIMEOUT);
            }
        }

        /// <summary>
        /// Starts the camera.
        /// </summary>
        public override void Play()
        {
            if (hasInitDone && !natCamCameraDevice.IsRunning)
                natCamCameraDevice.StartPreview(OnStart, OnFrame);
        }

        /// <summary>
        /// Pauses the active camera.
        /// </summary>
        public override void Pause()
        {
            if (hasInitDone && natCamCameraDevice.IsRunning)
                natCamCameraDevice.StopPreview();
        }

        /// <summary>
        /// Stops the active camera.
        /// </summary>
        public override void Stop()
        {
            if (hasInitDone && natCamCameraDevice.IsRunning)
                natCamCameraDevice.StopPreview();
        }

        /// <summary>
        /// Indicates whether the active camera is currently playing.
        /// </summary>
        /// <returns><c>true</c>, if the active camera is playing, <c>false</c> otherwise.</returns>
        public override bool IsPlaying()
        {
            return hasInitDone ? natCamCameraDevice.IsRunning : false;
        }

        /// <summary>
        /// Indicates whether the active camera device is currently front facng.
        /// </summary>
        /// <returns><c>true</c>, if the active camera device is front facng, <c>false</c> otherwise.</returns>
        public override bool IsFrontFacing()
        {
            return hasInitDone ? natCamCameraDevice.IsFrontFacing : false;
        }

        /// <summary>
        /// Returns the active camera device name.
        /// </summary>
        /// <returns>The active camera device name.</returns>
        public override string GetDeviceName()
        {
            return hasInitDone ? natCamCameraDevice.UniqueID : "";
        }

        /// <summary>
        /// Returns the active camera framerate.
        /// </summary>
        /// <returns>The active camera framerate.</returns>
        public override float GetFPS()
        {
            return hasInitDone ? natCamCameraDevice.Framerate : -1f;
        }

        /// <summary>
        /// Returns the active WebcamTexture.
        /// </summary>
        /// <returns>The active WebcamTexture.</returns>
        public override WebCamTexture GetWebCamTexture()
        {
            return null;
        }

        /// <summary>
        /// Indicates whether the video buffer of the frame has been updated.
        /// </summary>
        /// <returns><c>true</c>, if the video buffer has been updated <c>false</c> otherwise.</returns>
        public override bool DidUpdateThisFrame()
        {
            if (!hasInitDone)
                return false;

            return didUpdateThisFrame;
        }

        /// <summary>
        /// Returns the NatCam camera device.
        /// </summary>
        /// <returns>The NatCam camera device.</returns>
        public virtual CameraDevice GetNatCamCameraDevice()
        {
            return natCamCameraDevice;
        }

        /// <summary>
        /// Gets the mat of the current frame.
        /// The Mat object's type is 'CV_8UC4' (RGBA).
        /// </summary>
        /// <returns>The mat of the current frame.</returns>
        public override Mat GetMat()
        {
            if (!hasInitDone || !natCamCameraDevice.IsRunning)
            {
                return (rotatedFrameMat != null) ? rotatedFrameMat : frameMat;
            }

            Utils.fastTexture2DToMat(preview, frameMat, false);

            FlipMat(frameMat, flipVertical, flipHorizontal);
            if (rotatedFrameMat != null)
            {
                Core.rotate(frameMat, rotatedFrameMat, Core.ROTATE_90_CLOCKWISE);
                return rotatedFrameMat;
            }
            else
            {
                return frameMat;
            }
        }

        /// <summary>
        /// Flips the mat.
        /// </summary>
        /// <param name="mat">Mat.</param>
        protected override void FlipMat(Mat mat, bool flipVertical, bool flipHorizontal)
        {
            int flipCode = 0;

            if (flipVertical)
            {
                if (flipCode == int.MinValue)
                {
                    flipCode = 0;
                }
                else if (flipCode == 0)
                {
                    flipCode = int.MinValue;
                }
                else if (flipCode == 1)
                {
                    flipCode = -1;
                }
                else if (flipCode == -1)
                {
                    flipCode = 1;
                }
            }

            if (flipHorizontal)
            {
                if (flipCode == int.MinValue)
                {
                    flipCode = 1;
                }
                else if (flipCode == 0)
                {
                    flipCode = -1;
                }
                else if (flipCode == 1)
                {
                    flipCode = int.MinValue;
                }
                else if (flipCode == -1)
                {
                    flipCode = 0;
                }
            }

            if (flipCode > int.MinValue)
            {
                Core.flip(mat, mat, flipCode);
            }
        }

        /// <summary>
        /// To release the resources.
        /// </summary>
        protected override void ReleaseResources()
        {
            isInitWaiting = false;
            hasInitDone = false;

            if (natCamCameraDevice.IsRunning)
                natCamCameraDevice.StopPreview();

            natCamCameraDevice = null;
            preview = null;

            didUpdateThisFrame = false;

            if (frameMat != null)
            {
                frameMat.Dispose();
                frameMat = null;
            }
            if (rotatedFrameMat != null)
            {
                rotatedFrameMat.Dispose();
                rotatedFrameMat = null;
            }
        }
    }
}