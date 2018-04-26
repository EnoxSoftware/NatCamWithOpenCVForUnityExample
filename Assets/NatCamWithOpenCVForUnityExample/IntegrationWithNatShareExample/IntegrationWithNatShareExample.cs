using UnityEngine;
using NatCamU.Core;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using System.Collections;
using OpenCVForUnity;
using NatShareU;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace NatCamWithOpenCVForUnityExample
{
    /// <summary>
    /// Integration With NatShare Example
    /// An example of the native sharing and save to the camera roll using NatShare.
    /// </summary>
    public class IntegrationWithNatShareExample : MonoBehaviour
    {
        [Header("Camera")]
        public bool useFrontCamera;

        [Header("Preview")]
        public RawImage preview;
        public CameraResolution previewResolution = CameraResolution._1280x720;
        public int requestedFPS = 30;
        public AspectRatioFitter aspectFitter;

        bool didUpdateThisFrame = false;

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

        Mat matrix;
        byte[] pixelBuffer;
        Texture2D texture;
        const TextureFormat textureFormat = TextureFormat.RGBA32;

        ComicFilter comicFilter;

        public virtual void Start () 
        {
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

            fpsMonitor = GetComponent<FpsMonitor> ();
            if (fpsMonitor != null){
                fpsMonitor.Add ("Name", "NatCamPreviewToMatExample");
                fpsMonitor.Add ("onFrameFPS", onFrameFPS.ToString("F1"));
                fpsMonitor.Add ("drawFPS", drawFPS.ToString("F1"));
                fpsMonitor.Add ("width", "");
                fpsMonitor.Add ("height", "");
                fpsMonitor.Add ("orientation", "");
            }

            comicFilter = new ComicFilter ();
        }

        /// <summary>
        /// Method called when the camera preview starts
        /// </summary>
        public virtual void OnStart ()
        {
            // Create pixel buffer
            pixelBuffer = new byte[NatCam.Preview.width * NatCam.Preview.height * 4];

            // Get the preview data
            NatCam.CaptureFrame(pixelBuffer, true);

            // Create preview matrix
            if (matrix != null && (matrix.cols() != NatCam.Preview.width || matrix.rows() != NatCam.Preview.height)) {
                matrix.Dispose ();
                matrix = null;
            }
            matrix = matrix ?? new Mat(NatCam.Preview.height, NatCam.Preview.width, CvType.CV_8UC4);
            matrix.put(0, 0, pixelBuffer);

            // Create display texture
            if (texture && (texture.width != matrix.cols() || texture.height != matrix.rows())) {
                Texture2D.Destroy (texture);
                texture = null;
            }
            texture = texture ?? new Texture2D (matrix.cols(), matrix.rows(), textureFormat, false, false);

            // Scale the panel to match aspect ratios
            aspectFitter.aspectRatio = NatCam.Preview.width / (float)NatCam.Preview.height;

            // Display the result
            preview.texture = texture;

            Debug.Log ("OnStart (): " + matrix.cols() + " " + matrix.rows() + " " + NatCam.Preview.width + " " + NatCam.Preview.height + " " + texture.width + " " + texture.height);
        }

        /// <summary>
        /// Method called on every frame that the camera preview updates
        /// </summary>
        public virtual void OnFrame ()
        {
            onFrameCount++;

            didUpdateThisFrame = true;
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

                    if (matrix != null) {
                        fpsMonitor.Add ("width", matrix.width ().ToString ());
                        fpsMonitor.Add ("height", matrix.height ().ToString ());
                    }
                    fpsMonitor.Add ("orientation", Screen.orientation.ToString());
                }
            }                

            if (NatCam.IsPlaying && didUpdateThisFrame) {

                drawCount++;

                Mat matrix = GetMat ();

                if (matrix != null) {

                    comicFilter.Process (matrix, matrix);

                    Imgproc.putText (matrix, "[NatCam With OpenCVForUnity Example]", new Point (5, matrix.rows () - 50), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    Imgproc.putText (matrix, "- Integration With NatShare Example", new Point (5, matrix.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                    // Restore the coordinate system of the image by OpenCV's Flip function.
                    Utils.fastMatToTexture2D (matrix, texture, true, 0, false);
                }
            }
        }
       
        void LateUpdate ()
        {
            didUpdateThisFrame = false;
        }
            
        /// <summary>
        /// Gets the current camera preview frame that converted to the correct direction in OpenCV Matrix format.
        /// </summary>
        private Mat GetMat ()
        {
            if (matrix.cols () != NatCam.Preview.width || matrix.rows () != NatCam.Preview.height)
                return null;

            // Get the preview data
            // Set `flip` flag to true because OpenCV uses inverted Y-coordinate system
            NatCam.CaptureFrame(pixelBuffer, true);

            matrix.put(0, 0, pixelBuffer);

            return matrix;
        }

        /// <summary>
        /// Releases all resource.
        /// </summary>
        private void Dispose ()
        {
            NatCam.Release ();

            if (matrix != null) {
                matrix.Dispose ();
                matrix = null;
            }
            if (texture != null) {
                Texture2D.Destroy (texture);
                texture = null;
            }

            didUpdateThisFrame = false;

            if (comicFilter != null)
                comicFilter.Dispose ();
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
        /// Raises the share button click event.
        /// </summary>
        public void OnShareButtonClick ()
        {
            Debug.Log ("OnShareButtonClick ()");

            NatShare.Share (texture);
        }

        /// <summary>
        /// Raises the save to camera roll button click event.
        /// </summary>
        public void OnSaveToCameraRollButtonClick ()
        {
            Debug.Log ("OnSaveToCameraRollButtonClick ()");

            NatShare.SaveToCameraRoll (texture);
        }
    }
        
}