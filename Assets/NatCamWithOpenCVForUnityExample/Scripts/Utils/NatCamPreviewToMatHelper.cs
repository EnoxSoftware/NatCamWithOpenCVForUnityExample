using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using OpenCVForUnity;
using NatCamU.Core;

namespace NatCamWithOpenCVForUnityExample
{
    /// <summary>
    /// NatCamPreview to mat helper.
    /// v 1.0.2
    /// Depends on NatCam version 2.0f1 or later.
    /// </summary>
    public class NatCamPreviewToMatHelper : WebCamTextureToMatHelper
    {
        public override float requestedFPS {
            get { return _requestedFPS; } 
            set {
                _requestedFPS = Mathf.Clamp(value, -1f, float.MaxValue);
                if (hasInitDone) {
                    Initialize ();
                }
            }
        }

        protected byte[] pixelBuffer;
        protected bool didUpdateThisFrame = false;

        /// <summary>
        /// Method called when the camera preview starts
        /// </summary>
        public virtual void OnStart ()
        {
            if (colors == null || colors.Length != NatCam.Preview.width * NatCam.Preview.height)
                colors = new Color32[NatCam.Preview.width * NatCam.Preview.height];

            // Create pixel buffer
            if (pixelBuffer == null || pixelBuffer.Length == NatCam.Preview.width * NatCam.Preview.height * 4) {
                pixelBuffer = new byte[NatCam.Preview.width * NatCam.Preview.height * 4];
            }

            if (hasInitDone) {
                if (frameMat.width () != NatCam.Preview.width || frameMat.height () != NatCam.Preview.height) {

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

                    frameMat = new Mat (NatCam.Preview.height, NatCam.Preview.width, CvType.CV_8UC4);

                    if (rotate90Degree)
                        rotatedFrameMat = new Mat (NatCam.Preview.width, NatCam.Preview.height, CvType.CV_8UC4);

                    if (onInitialized != null)
                        onInitialized.Invoke ();
                }
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
            if (!NatCam.Implementation.HasPermissions) {
                Debug.LogError ("NatCam.Implementation.HasPermissions == false");

                if (onErrorOccurred != null)
                    onErrorOccurred.Invoke (ErrorCode.CAMERA_DEVICE_NOT_EXIST);

                yield break;
            }

            if (hasInitDone)
            {
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
                        NatCam.Camera = DeviceCamera.Cameras [requestedDeviceIndex];
                    }
                }
                if (NatCam.Camera == null)
                    Debug.Log ("Cannot find camera device " + requestedDeviceName + ".");
            }

            NatCam.Camera = requestedIsFrontFacing ? DeviceCamera.FrontCamera : DeviceCamera.RearCamera;

            if (NatCam.Camera == null) {
                if (DeviceCamera.Cameras.Length > 0) {
                    NatCam.Camera = DeviceCamera.Cameras [0];
                } else {
                    isInitWaiting = false;

                    if (onErrorOccurred != null)
                        onErrorOccurred.Invoke (ErrorCode.CAMERA_DEVICE_NOT_EXIST);

                    yield break;
                }
            }

            NatCam.Camera.Framerate = requestedFPS;

            // Set the camera's preview resolution
            NatCam.Camera.PreviewResolution = new CameraResolution(requestedWidth, requestedHeight);

            // Register callback for when the preview starts
            // Note that this is a MUST when assigning the preview texture to anything
            NatCam.OnStart += OnStart;
            // Register for preview updates
            NatCam.OnFrame += OnFrame;

            // Starts the camera
            NatCam.Play();

            int initFrameCount = 0;
            bool isTimeout = false;

            while (true) {
                if (initFrameCount > timeoutFrameCount) {
                    isTimeout = true;
                    break;
                }
                else if (didUpdateThisFrame) {

                    Debug.Log ("NatCamPreviewToMatHelper:: " + " width:" + NatCam.Preview.width + " height:" + NatCam.Preview.height + " fps:" + NatCam.Camera.Framerate
                        + " isFrongFacing:" + NatCam.Camera.IsFrontFacing);

                    frameMat = new Mat (NatCam.Preview.height, NatCam.Preview.width, CvType.CV_8UC4);

                    if (rotate90Degree)
                        rotatedFrameMat = new Mat (NatCam.Preview.width, NatCam.Preview.height, CvType.CV_8UC4);

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
                // Unregister from NatCam callbacks
                NatCam.OnStart -= OnStart;
                NatCam.OnFrame -= OnFrame;
                NatCam.Release ();

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
            if (hasInitDone)
                NatCam.Play ();
        }

        /// <summary>
        /// Pauses the active camera.
        /// </summary>
        public override void Pause ()
        {
            if (hasInitDone)
                NatCam.Pause ();
        }

        /// <summary>
        /// Stops the active camera.
        /// </summary>
        public override void Stop ()
        {
            if (hasInitDone)
                NatCam.Pause ();
        }

        /// <summary>
        /// Indicates whether the active camera is currently playing.
        /// </summary>
        /// <returns><c>true</c>, if the active camera is playing, <c>false</c> otherwise.</returns>
        public override bool IsPlaying ()
        {
            return hasInitDone ? NatCam.IsPlaying : false;
        }

        /// <summary>
        /// Indicates whether the active camera device is currently front facng.
        /// </summary>
        /// <returns><c>true</c>, if the active camera device is front facng, <c>false</c> otherwise.</returns>
        public override bool IsFrontFacing ()
        {
            return hasInitDone ? NatCam.Camera.IsFrontFacing : false;
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
            return hasInitDone ? NatCam.Camera.Framerate : -1f;
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
            if (!hasInitDone || !NatCam.IsPlaying || pixelBuffer == null) {
                return (rotatedFrameMat != null) ? rotatedFrameMat : frameMat;
            }
                
            // Set `flip` flag to true because OpenCV uses inverted Y-coordinate system
            NatCam.CaptureFrame(pixelBuffer, true);
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
            int flipCode = int.MinValue;

            if (_flipVertical) {
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

            if (_flipHorizontal) {
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

            NatCam.Release ();

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