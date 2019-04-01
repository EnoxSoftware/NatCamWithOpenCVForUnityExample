using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using NatCam;
using OpenCVForUnity.UnityUtils.Helper;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;

namespace NatCamWithOpenCVForUnityExample
{
    /// <summary>
    /// NatCamPreview to mat helper.
    /// v 1.0.6
    /// Depends on NatCam version 2.2 or later.
    /// Depends on OpenCVForUnity version 2.3.3 or later.
    /// </summary>
    public class NatCamPreviewToMatHelper : WebCamTextureToMatHelper
    {
        public override float requestedFPS {
            get { return _requestedFPS; } 
            set {
                _requestedFPS = Mathf.Clamp (value, -1f, float.MaxValue);
                if (hasInitDone) {
                    Initialize ();
                }
            }
        }

        protected byte[] pixelBuffer;
        protected bool didUpdateThisFrame = false;

        protected DeviceCamera natCamDeviceCamera;
        protected Texture preview;

        /// <summary>
        /// Method called when the camera preview starts
        /// </summary>
        public virtual void OnStart (Texture preview)
        {
            this.preview = preview;

            if (colors == null || colors.Length != preview.width * preview.height)
                colors = new Color32[preview.width * preview.height];

            // Create pixel buffer
            if (pixelBuffer == null || pixelBuffer.Length == preview.width * preview.height * 4) {
                pixelBuffer = new byte[preview.width * preview.height * 4];
            }

            if (hasInitDone) {
                if (onDisposed != null)
                    onDisposed.Invoke ();

                if (frameMat != null) {
                    frameMat.Dispose ();
                    frameMat = null;
                }
                if (rotatedFrameMat != null) {
                    rotatedFrameMat.Dispose ();
                    rotatedFrameMat = null;
                }

                frameMat = new Mat (preview.height, preview.width, CvType.CV_8UC4, new Scalar (0, 0, 0, 255));

                if (rotate90Degree)
                    rotatedFrameMat = new Mat (preview.width, preview.height, CvType.CV_8UC4, new Scalar (0, 0, 0, 255));

                if (onInitialized != null)
                    onInitialized.Invoke ();
            }
        }

        /// <summary>
        /// Method called on every frame that the camera preview updates
        /// </summary>
        public virtual void OnFrame ()
        {
            didUpdateThisFrame = true;
        }

        // Update is called once per frame
        protected override void Update ()
        {
            if (hasInitDone) {

                // Catch the orientation change of the screen and correct the mat image to the correct direction.
                if (screenOrientation != Screen.orientation && (screenWidth != Screen.width || screenHeight != Screen.height)) {

                    if (!natCamDeviceCamera.IsRunning) {

                        bool isRotatedFrame = false;
                        DeviceOrientation oldOrientation = (DeviceOrientation)(int)screenOrientation;
                        DeviceOrientation newOrientation = (DeviceOrientation)(int)Screen.orientation;
                        if (oldOrientation == DeviceOrientation.Portrait ||
                            oldOrientation == DeviceOrientation.PortraitUpsideDown) {
                            if (newOrientation == DeviceOrientation.LandscapeLeft ||
                                newOrientation == DeviceOrientation.LandscapeRight) {
                                isRotatedFrame = true;
                            }
                        } else if (oldOrientation == DeviceOrientation.LandscapeLeft ||
                                   oldOrientation == DeviceOrientation.LandscapeRight) {
                            if (newOrientation == DeviceOrientation.Portrait ||
                                newOrientation == DeviceOrientation.PortraitUpsideDown) {
                                isRotatedFrame = true;
                            }
                        }

                        if (isRotatedFrame) {

                            int width = frameMat.width ();
                            int height = frameMat.height ();

                            if (frameMat != null) {
                                frameMat.Dispose ();
                                frameMat = null;
                            }
                            if (rotatedFrameMat != null) {
                                rotatedFrameMat.Dispose ();
                                rotatedFrameMat = null;
                            }

                            frameMat = new Mat (width, height, CvType.CV_8UC4, new Scalar (0, 0, 0, 255));

                            if (rotate90Degree)
                                rotatedFrameMat = new Mat (height, width, CvType.CV_8UC4, new Scalar (0, 0, 0, 255));
                        }
                    }
                        
                    if (onDisposed != null)
                        onDisposed.Invoke ();
                    if (onInitialized != null)
                        onInitialized.Invoke ();

                    screenOrientation = Screen.orientation;
                    screenWidth = Screen.width;
                    screenHeight = Screen.height;

                } else {
                    screenWidth = Screen.width;
                    screenHeight = Screen.height;
                }
            }
        }

        public virtual void LateUpdate ()
        {
            didUpdateThisFrame = false;
        }

        /// <summary>
        /// Initializes this instance by coroutine.
        /// </summary>
        protected override IEnumerator _Initialize ()
        {
            if (hasInitDone) {
                ReleaseResources ();

                if (onDisposed != null)
                    onDisposed.Invoke ();
            }

            isInitWaiting = true;

            // Creates the camera
            if (!String.IsNullOrEmpty (requestedDeviceName)) {
                int requestedDeviceIndex = -1;
                if (Int32.TryParse (requestedDeviceName, out requestedDeviceIndex)) {
                    if (requestedDeviceIndex >= 0 && requestedDeviceIndex < DeviceCamera.Cameras.Length) {
                        natCamDeviceCamera = DeviceCamera.Cameras [requestedDeviceIndex];
                    }
                } else {                    
                    for (int cameraIndex = 0; cameraIndex < DeviceCamera.Cameras.Length; cameraIndex++) {
                        if (DeviceCamera.Cameras [cameraIndex].UniqueID == requestedDeviceName) {
                            natCamDeviceCamera = DeviceCamera.Cameras [cameraIndex];
                            break;
                        }
                    }
                }
                if (natCamDeviceCamera == null)
                    Debug.Log ("Cannot find camera device " + requestedDeviceName + ".");
            }
                
            if (natCamDeviceCamera == null) {
                natCamDeviceCamera = requestedIsFrontFacing ? DeviceCamera.FrontCamera : DeviceCamera.RearCamera;
            }

            if (natCamDeviceCamera == null) {
                if (DeviceCamera.Cameras.Length > 0) {
                    natCamDeviceCamera = DeviceCamera.Cameras [0];
                } else {
                    isInitWaiting = false;

                    if (onErrorOccurred != null)
                        onErrorOccurred.Invoke (ErrorCode.CAMERA_DEVICE_NOT_EXIST);

                    yield break;
                }
            }

            natCamDeviceCamera.Framerate = (int)requestedFPS;

            // Set the camera's preview resolution
            natCamDeviceCamera.PreviewResolution = new Vector2Int (requestedWidth, requestedHeight);

            // Starts the camera
            // Register callback for when the preview starts
            // Register for preview updates
            natCamDeviceCamera.StartPreview (OnStart, OnFrame);

            int initFrameCount = 0;
            bool isTimeout = false;

            while (true) {
                if (initFrameCount > timeoutFrameCount) {
                    isTimeout = true;
                    break;
                } else if (didUpdateThisFrame) {
                    
                    Debug.Log ("NatCamPreviewToMatHelper:: " + "UniqueID:" + natCamDeviceCamera.UniqueID + " width:" + preview.width + " height:" + preview.height + " fps:" + natCamDeviceCamera.Framerate
                    + " isFrongFacing:" + natCamDeviceCamera.IsFrontFacing);

                    frameMat = new Mat (preview.height, preview.width, CvType.CV_8UC4);

                    screenOrientation = Screen.orientation;
                    screenWidth = Screen.width;
                    screenHeight = Screen.height;

                    if (rotate90Degree)
                        rotatedFrameMat = new Mat (preview.width, preview.height, CvType.CV_8UC4);

                    isInitWaiting = false;
                    hasInitDone = true;
                    initCoroutine = null;

                    if (onInitialized != null)
                        onInitialized.Invoke ();

                    break;
                } else {
                    initFrameCount++;
                    yield return null;
                }
            }

            if (isTimeout) {
                natCamDeviceCamera.StopPreview ();

                isInitWaiting = false;
                initCoroutine = null;

                if (onErrorOccurred != null)
                    onErrorOccurred.Invoke (ErrorCode.TIMEOUT);
            }
        }

        /// <summary>
        /// Starts the camera.
        /// </summary>
        public override void Play ()
        {
            if (hasInitDone && !natCamDeviceCamera.IsRunning)
                natCamDeviceCamera.StartPreview (OnStart, OnFrame);
        }

        /// <summary>
        /// Pauses the active camera.
        /// </summary>
        public override void Pause ()
        {
            if (hasInitDone && natCamDeviceCamera.IsRunning)
                natCamDeviceCamera.StopPreview ();
        }

        /// <summary>
        /// Stops the active camera.
        /// </summary>
        public override void Stop ()
        {
            if (hasInitDone && natCamDeviceCamera.IsRunning)
                natCamDeviceCamera.StopPreview ();
        }

        /// <summary>
        /// Indicates whether the active camera is currently playing.
        /// </summary>
        /// <returns><c>true</c>, if the active camera is playing, <c>false</c> otherwise.</returns>
        public override bool IsPlaying ()
        {
            return hasInitDone ? natCamDeviceCamera.IsRunning : false;
        }

        /// <summary>
        /// Indicates whether the active camera device is currently front facng.
        /// </summary>
        /// <returns><c>true</c>, if the active camera device is front facng, <c>false</c> otherwise.</returns>
        public override bool IsFrontFacing ()
        {
            return hasInitDone ? natCamDeviceCamera.IsFrontFacing : false;
        }

        /// <summary>
        /// Returns the active camera device name.
        /// </summary>
        /// <returns>The active camera device name.</returns>
        public override string GetDeviceName ()
        {
            return "";
        }

        /// <summary>
        /// Returns the active camera framerate.
        /// </summary>
        /// <returns>The active camera framerate.</returns>
        public override float GetFPS ()
        {
            return hasInitDone ? natCamDeviceCamera.Framerate : -1f;
        }

        /// <summary>
        /// Returns the active WebcamTexture.
        /// </summary>
        /// <returns>The active WebcamTexture.</returns>
        public override WebCamTexture GetWebCamTexture ()
        {
            return null;
        }

        /// <summary>
        /// Indicates whether the video buffer of the frame has been updated.
        /// </summary>
        /// <returns><c>true</c>, if the video buffer has been updated <c>false</c> otherwise.</returns>
        public override bool DidUpdateThisFrame ()
        {
            if (!hasInitDone)
                return false;

            return didUpdateThisFrame;
        }

        /// <summary>
        /// Gets the mat of the current frame.
        /// The Mat object's type is 'CV_8UC4' (RGBA).
        /// </summary>
        /// <returns>The mat of the current frame.</returns>
        public override Mat GetMat ()
        {
            if (!hasInitDone || !natCamDeviceCamera.IsRunning || pixelBuffer == null) {
                return (rotatedFrameMat != null) ? rotatedFrameMat : frameMat;
            }

            // Set `flip` flag to true because OpenCV uses inverted Y-coordinate system
            natCamDeviceCamera.CaptureFrame (pixelBuffer);
            Utils.copyToMat (pixelBuffer, frameMat);

            FlipMat (frameMat, flipVertical, flipHorizontal);
            if (rotatedFrameMat != null) {                
                Core.rotate (frameMat, rotatedFrameMat, Core.ROTATE_90_CLOCKWISE);
                return rotatedFrameMat;
            } else {
                return frameMat;
            }
        }

        /// <summary>
        /// Flips the mat.
        /// </summary>
        /// <param name="mat">Mat.</param>
        protected override void FlipMat (Mat mat, bool flipVertical, bool flipHorizontal)
        {
            int flipCode = 0;

            if (flipVertical) {
                if (flipCode == int.MinValue) {
                    flipCode = 0;
                } else if (flipCode == 0) {
                    flipCode = int.MinValue;
                } else if (flipCode == 1) {
                    flipCode = -1;
                } else if (flipCode == -1) {
                    flipCode = 1;
                }
            }

            if (flipHorizontal) {
                if (flipCode == int.MinValue) {
                    flipCode = 1;
                } else if (flipCode == 0) {
                    flipCode = -1;
                } else if (flipCode == 1) {
                    flipCode = int.MinValue;
                } else if (flipCode == -1) {
                    flipCode = 0;
                }
            }

            if (flipCode > int.MinValue) {
                Core.flip (mat, mat, flipCode);
            }
        }

        /// <summary>
        /// To release the resources.
        /// </summary>
        protected override void ReleaseResources ()
        {
            isInitWaiting = false;
            hasInitDone = false;

            if (natCamDeviceCamera.IsRunning)
                natCamDeviceCamera.StopPreview ();

            natCamDeviceCamera = null;
            preview = null;

            pixelBuffer = null;
            didUpdateThisFrame = false;

            if (frameMat != null) {
                frameMat.Dispose ();
                frameMat = null;
            }
            if (rotatedFrameMat != null) {
                rotatedFrameMat.Dispose ();
                rotatedFrameMat = null;
            }
        }
    }
}