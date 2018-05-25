using UnityEngine;
using NatCamU.Core;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using System.Runtime.InteropServices;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace NatCamWithOpenCVForUnityExample
{
    /// <summary>
    /// NatCamPreview Only Example
    /// An example of displaying the preview frame of camera only using NatCam.
    /// </summary>
    public class NatCamPreviewOnlyExample : MonoBehaviour
    {
        public enum ImageProcessingType
        {
            None,
            DrawLine,
            ConvertToGray,
        }

        [Header("Camera")]
        public bool useFrontCamera;

        [Header("Preview")]
        public RawImage preview;
        public CameraResolution previewResolution = CameraResolution._1280x720;
        public int requestedFPS = 30;
        public AspectRatioFitter aspectFitter;

        [Header("ImageProcessing")]
        public ImageProcessingType imageProcessingType = ImageProcessingType.None;
        public Dropdown imageProcessingTypeDropdown; 

        bool didUpdateThisFrame = false;
        Texture2D texture;
        byte[] buffer;
        const TextureFormat textureFormat = TextureFormat.RGBA32;

        int updateCount = 0;
        int onFrameCount = 0;
        int drawCount = 0;

        float elapsed = 0;
        float updateFPS = 0;
        float onFrameFPS = 0;
        float drawFPS = 0;

        /// <summary>
        /// The FPS monitor.
        /// </summary>
        FpsMonitor fpsMonitor;

        public virtual void Start () 
        {
            fpsMonitor = GetComponent<FpsMonitor> ();

            if (!NatCam.Implementation.HasPermissions) {
                Debug.LogError ("NatCam.Implementation.HasPermissions == false");

                if (fpsMonitor != null)
                    fpsMonitor.consoleText = "NatCam.Implementation.HasPermissions == false";

                return;
            }

            // Load global camera benchmark settings.
            int width, height, fps; 
            NatCamWithOpenCVForUnityExample.GetCameraResolution (out width, out height);
            NatCamWithOpenCVForUnityExample.GetCameraFps (out fps);
            previewResolution = new NatCamU.Core.CameraResolution(width, height);
            requestedFPS = fps;

            // Set the active camera
            NatCam.Camera = useFrontCamera ? DeviceCamera.FrontCamera : DeviceCamera.RearCamera;

            // Null checking
            if (!NatCam.Camera) {
                Debug.LogError("Camera is null. Consider using "+(useFrontCamera ? "rear" : "front")+" camera");
                return;
            }
            if (!preview) {
                Debug.LogError("Preview RawImage has not been set");
                return;
            }

            // Set the camera's preview resolution
            NatCam.Camera.PreviewResolution = previewResolution;
            // Set the camera framerate
            NatCam.Camera.Framerate = requestedFPS;
            NatCam.Play();
            NatCam.OnStart += OnStart;
            NatCam.OnFrame += OnFrame;

            if (fpsMonitor != null){
                fpsMonitor.Add ("Name", "NatCamPreviewOnlyExample");
                fpsMonitor.Add ("onFrameFPS", onFrameFPS.ToString("F1"));
                fpsMonitor.Add ("drawFPS", drawFPS.ToString("F1"));
                fpsMonitor.Add ("width", "");
                fpsMonitor.Add ("height", "");
                fpsMonitor.Add ("orientation", "");
            }

            imageProcessingTypeDropdown.value = (int)imageProcessingType;
        }

        /// <summary>
        /// Method called when the camera preview starts
        /// </summary>
        public virtual void OnStart ()
        {
            // Set the preview RawImage texture once the preview starts
            preview.texture = NatCam.Preview;

            // Scale the panel to match aspect ratios
            aspectFitter.aspectRatio = NatCam.Preview.width / (float)NatCam.Preview.height;

            Debug.Log ("# Active Camera Properties #####################");

            try
            {
                Dictionary<string, string> cameraProps = new Dictionary<string, string> ();

                cameraProps.Add ("IsFrontFacing", NatCam.Camera.IsFrontFacing.ToString());

                cameraProps.Add ("Framerate", NatCam.Camera.Framerate.ToString());

                cameraProps.Add ("PreviewResolution", NatCam.Camera.PreviewResolution.width + "x" + NatCam.Camera.PreviewResolution.height);
                cameraProps.Add ("PhotoResolution", NatCam.Camera.PhotoResolution.width + "x" + NatCam.Camera.PhotoResolution.height);

                cameraProps.Add ("ExposureMode", NatCam.Camera.ExposureMode.ToString());
                cameraProps.Add ("ExposureBias", NatCam.Camera.ExposureBias.ToString());
                cameraProps.Add ("MinExposureBias", NatCam.Camera.MinExposureBias.ToString());
                cameraProps.Add ("MaxExposureBias", NatCam.Camera.MaxExposureBias.ToString());

                cameraProps.Add ("IsFlashSupported", NatCam.Camera.IsFlashSupported.ToString());
                cameraProps.Add ("FlashMode", NatCam.Camera.FlashMode.ToString());

                cameraProps.Add ("FocusMode", NatCam.Camera.FocusMode.ToString());

                cameraProps.Add ("HorizontalFOV", NatCam.Camera.HorizontalFOV.ToString());
                cameraProps.Add ("VerticalFOV", NatCam.Camera.VerticalFOV.ToString());

                cameraProps.Add ("IsTorchSupported", NatCam.Camera.IsTorchSupported.ToString());
                cameraProps.Add ("TorchEnabled", NatCam.Camera.TorchEnabled.ToString());

                cameraProps.Add ("MaxZoomRatio", NatCam.Camera.MaxZoomRatio.ToString());
                cameraProps.Add ("ZoomRatio", NatCam.Camera.ZoomRatio.ToString());

                foreach (string key in cameraProps.Keys)
                {
                    Debug.Log(key + ": " + cameraProps[key]);
                }

                if (fpsMonitor != null){
                    fpsMonitor.boxWidth = 200;
                    fpsMonitor.boxHeight = 620;
                    fpsMonitor.LocateGUI();

                    foreach (string key in cameraProps.Keys)
                    {
                        fpsMonitor.Add (key, cameraProps[key]);
                    }
                }
            }
            catch(Exception e)
            {
                Debug.Log ("Exception: " + e);
                if (fpsMonitor != null) {
                    fpsMonitor.consoleText = "Exception: " + e;
                }
            }
            Debug.Log ("#######################################");


            Debug.Log ("OnStart (): " + NatCam.Preview.width + " " + NatCam.Preview.height);
        }

        /// <summary>
        /// Method called on every frame that the camera preview updates
        /// </summary>
        public virtual void OnFrame ()
        {
            onFrameCount++;

            if (imageProcessingType == ImageProcessingType.None) {
                didUpdateThisFrame = true;
                preview.texture = NatCam.Preview;
            } else {

                // Size checking
                if (buffer != null && buffer.Length != NatCam.Preview.width * NatCam.Preview.height * 4) {
                    buffer = null;
                }

                // Create the managed buffer
                buffer = buffer ?? new byte[NatCam.Preview.width * NatCam.Preview.height * 4];

                // Capture the current frame
                if (!NatCam.CaptureFrame (buffer)) return;

                // Size checking
                if (texture && (texture.width != NatCam.Preview.width || texture.height != NatCam.Preview.height)) {
                    Texture2D.Destroy (texture);
                    texture = null;
                }
                // Create the texture
                texture = texture ?? new Texture2D (NatCam.Preview.width, NatCam.Preview.height, textureFormat, false, false);

                didUpdateThisFrame = true;
            }
        }

        // Update is called once per frame
        void Update()
        {
            updateCount++;
            elapsed += Time.deltaTime;
            if (elapsed >= 1f) {
                updateFPS = updateCount / elapsed;
                onFrameFPS = onFrameCount / elapsed;
                drawFPS = drawCount / elapsed;
                updateCount = 0;
                onFrameCount = 0;
                drawCount = 0;
                elapsed = 0;

                Debug.Log ("didUpdateThisFrame: " + didUpdateThisFrame + " updateFPS: " + updateFPS + " onFrameFPS: " + onFrameFPS + " drawFPS: " + drawFPS);
                if (fpsMonitor != null) {
                    fpsMonitor.Add ("onFrameFPS", onFrameFPS.ToString("F1"));
                    fpsMonitor.Add ("drawFPS", drawFPS.ToString("F1"));

                    if (NatCam.Preview != null) {
                        fpsMonitor.Add ("width", NatCam.Preview.width.ToString ());
                        fpsMonitor.Add ("height", NatCam.Preview.height.ToString ());
                    }
                    fpsMonitor.Add ("orientation", Screen.orientation.ToString());
                }
            }

            if (NatCam.IsPlaying && didUpdateThisFrame) {
                drawCount++;

                if (imageProcessingType == ImageProcessingType.None) {   
                    
                } else {
                    
                    if (texture == null || buffer == null || texture.width * texture.height * 4 != buffer.Length)
                        return;

                    // Process
                    ProcessImage (buffer, texture.width, texture.height, buffer.Length, imageProcessingType);

                    // Load texture data
                    texture.LoadRawTextureData (buffer);
                    // Upload to GPU
                    texture.Apply ();
                    // Set RawImage texture
                    preview.texture = texture;
                }
            }
        }

        void LateUpdate ()
        {
            didUpdateThisFrame = false;
        }

        /// <summary>
        /// Process the image.
        /// </summary>
        /// <param name="buffer">Bytes.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="size">Size.</param>
        /// <param name="imageProcessingType">ImageProcessingType.</param>
        private void ProcessImage (Byte[] buffer, int width, int height, int size, ImageProcessingType imageProcessingType = ImageProcessingType.None)
        {
            switch (imageProcessingType) {
            case ImageProcessingType.DrawLine:
                // Draw a diagonal line on our image
                float inclination = height / (float)width;
                for (int i = 0; i < 4; i++) {
                    for (int x = 0; x < width; x++) {
                        int y = (int)(-inclination*x) + height-2+i;
                        if (y < 0)
                            y = 0;
                        if (y > height-1)
                            y = height-1;
                        int p = (x * 4) + (y * width * 4);
                        // Set pixels in the buffer
                        buffer[p] = 255; buffer[p + 1] = 0; buffer[p + 2] = 0; buffer[p + 3] = 255;
                    }
                }

                break;
            case ImageProcessingType.ConvertToGray:
                // Convert a four-channel pixel buffer to greyscale
                // Iterate over the buffer
                for (int i = 0; i < size; i += 4) {
                    // Get channel intensities
                    byte
                    r = buffer[i + 0], g = buffer[i + 1],
                    b = buffer[i + 2], a = buffer[i + 3],
                    // Use quick luminance approximation to save time and memory
                    l = (byte)((r + r + r + b + g + g + g + g) >> 3);
                    // Set pixels in the buffer
                    buffer[i] = buffer[i + 1] = buffer[i + 2] = l; buffer[i + 3] = a;
                }

                break;
            }
        }

        /// <summary>
        /// Releases all resource.
        /// </summary>
        private void Dispose ()
        {
            NatCam.Release ();

            if (texture != null) {
                Texture2D.Destroy (texture);
                texture = null;
            }

            didUpdateThisFrame = false;
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            Dispose ();
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick ()
        {
            #if UNITY_5_3 || UNITY_5_3_OR_NEWER
            SceneManager.LoadScene ("NatCamWithOpenCVForUnityExample");
            #else
            Application.LoadLevel ("NatCamWithOpenCVForUnityExample");
            #endif
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick ()
        {
            NatCam.Play ();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick ()
        {
            NatCam.Pause ();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick ()
        {
            NatCam.Pause ();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick ()
        {
            // Switch camera
            if (NatCam.Camera.IsFrontFacing) NatCam.Camera = DeviceCamera.RearCamera;
            else NatCam.Camera = DeviceCamera.FrontCamera;
        }

        /// <summary>
        /// Raises the image processing type dropdown value changed event.
        /// </summary>
        public void OnImageProcessingTypeDropdownValueChanged (int result)
        {
            if ((int)imageProcessingType != result) {
                imageProcessingType = (ImageProcessingType)result;
            }
        }
    }        
}