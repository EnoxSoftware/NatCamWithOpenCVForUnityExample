using UnityEngine;
using NatCamU.Core;
using NatCamU.Pro;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using System.Collections;
using OpenCVForUnity;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace NatCamWithOpenCVForUnityExample
{
    /// <summary>
    /// NatCamPreview To Mat Example
    /// An example of converting a NatCam preview image to OpenCV's Mat format.
    /// </summary>
    public class NatCamPreviewToMatExample : NatCamBehaviour
    {
        public enum MatCaptureMethod
        {
            NatCam_PreviewBuffer,
            BlitWithReadPixels,
            OpenCVForUnity_LowLevelTextureToMat,
            Graphics_CopyTexture,
        }

        public enum ImageProcessingType
        {
            None,
            DrawLine,
            ConvertToGray,
        }

        [Header("OpenCV")]
        public MatCaptureMethod matCaptureMethod = MatCaptureMethod.NatCam_PreviewBuffer;
        public Dropdown matCaptureMethodDropdown; 
        public ImageProcessingType imageProcessingType = ImageProcessingType.None;
        public Dropdown imageProcessingTypeDropdown; 

        [Header("Preview")]
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

        private Mat matrix;
        private Mat grayMatrix;
        private Texture2D texture;
        private const TextureFormat textureFormat =
        #if UNITY_IOS && !UNITY_EDITOR 
        TextureFormat.BGRA32;
        #else
        TextureFormat.RGBA32;
        #endif

        public override void Start () 
        {
            base.Start ();

            NatCam.Camera.SetFramerate (requestedFPS);

            fpsMonitor = GetComponent<FpsMonitor> ();
            if (fpsMonitor != null){
                fpsMonitor.Add ("Name", "NatCamPreviewToMatExample");
                fpsMonitor.Add ("onFrameFPS", onFrameFPS.ToString("F1"));
                fpsMonitor.Add ("drawFPS", drawFPS.ToString("F1"));
                fpsMonitor.Add ("width", "");
                fpsMonitor.Add ("height", "");
                fpsMonitor.Add ("orientation", "");
            }
                
            matCaptureMethodDropdown.value = (int)matCaptureMethod;
            imageProcessingTypeDropdown.value = (int)imageProcessingType;
        }

        /// <summary>
        /// Method called when the camera preview starts
        /// </summary>
        public override void OnStart ()
        {
            // Initialize the texture
            // NatCam.PreviewMatrix(ref matrix);
            IntPtr ptr; int width, height, size;
            if (!NatCam.PreviewBuffer (out ptr, out width, out height, out size)) {
                if (fpsMonitor != null) {
                    fpsMonitor.consoleText = "OnStart (): NatCam.PreviewBuffer() returned false.";
                }
                return;
            }
            if (matrix != null && (matrix.cols() != width || matrix.rows() != height)) {
                matrix.Dispose ();
                matrix = null;
            }
            matrix = matrix ?? new Mat(height, width, CvType.CV_8UC4);
            Utils.copyToMat(ptr, matrix);

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
        public override void OnFrame ()
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

                Mat matrix = GetMat (matCaptureMethod);

                if (matrix != null) {

                    ProcessImage (matrix, imageProcessingType);

                    // The Imgproc.putText method is too heavy to use for mobile device benchmark purposes.
                    //Imgproc.putText (matrix, "W:" + matrix.width () + " H:" + matrix.height () + " SO:" + Screen.orientation, new Point (5, matrix.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    //Imgproc.putText (matrix, "updateFPS:" + updateFPS.ToString("F1") + " onFrameFPS:" + onFrameFPS.ToString("F1") + " drawFPS:" + drawFPS.ToString("F1"), new Point (5, matrix.rows () - 50), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                    // Restore the coordinate system of the image.
                    Utils.fastMatToTexture2D (matrix, texture , true, 0, false);
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
        private Mat GetMat (MatCaptureMethod matCaptureMethod = MatCaptureMethod.NatCam_PreviewBuffer)
        {
            if (matrix.cols () != NatCam.Preview.width || matrix.rows () != NatCam.Preview.height)
                return null;

            switch (matCaptureMethod) {
            default:
            case MatCaptureMethod.NatCam_PreviewBuffer:

                // Get the current preview frame as an OpenCV matrix
                // NatCam.PreviewMatrix(ref matrix);
                IntPtr ptr; int width, height, size;
                if (!NatCam.PreviewBuffer (out ptr, out width, out height, out size)) {
                    return null;
                }
                if (matrix != null && (matrix.cols() != width || matrix.rows() != height)) {
                    matrix.Dispose ();
                    matrix = null;
                }
                matrix = matrix ?? new Mat(height, width, CvType.CV_8UC4);
                Utils.copyToMat(ptr, matrix);

                // OpenCV uses an inverted coordinate system. Y-0 is the top of the image, whereas in OpenGL (and so NatCam), Y-0 is the bottom of the image.
                Core.flip (matrix, matrix, 0);

                break;
            case MatCaptureMethod.BlitWithReadPixels:

                // When NatCam.PreviewMatrix function does not work properly. (Zenfone 2)
                // The workaround for a device like this is to use Graphics.Blit with Texture2D.ReadPixels and Texture2D.GetRawTextureData/GetPixels32 to download the pixel data from the GPU.
                // Blit the NatCam preview to a temporary render texture; set the RT active and readback into a Texture2D (using ReadPixels), then access the pixel data in the texture.
                // The texture2D's TextureFormat needs to be RGBA32(Unity5.5+), ARGB32, RGB24, RGBAFloat or RGBAHalf.
                Utils.textureToTexture2D (NatCam.Preview, texture);

                matrix.put (0, 0, texture.GetRawTextureData ());

                // OpenCV uses an inverted coordinate system. Y-0 is the top of the image, whereas in OpenGL (and so NatCam), Y-0 is the bottom of the image.
                Core.flip (matrix, matrix, 0);

                break;
            case MatCaptureMethod.OpenCVForUnity_LowLevelTextureToMat:

                // When NatCam.PreviewMatrix function does not work properly. (Zenfone 2)
                // Converts OpenCV Mat to Unity Texture using low-level native plugin interface.
                // It seems that if enables the Multithreaded Rendering option in Android Player Settings, not work.
                Utils.textureToMat (NatCam.Preview, matrix);

                break;
            case MatCaptureMethod.Graphics_CopyTexture:

                // When NatCam.PreviewMatrix function does not work properly. (Zenfone 2)
                if (SystemInfo.copyTextureSupport != UnityEngine.Rendering.CopyTextureSupport.None) {
                    Graphics.CopyTexture (NatCam.Preview, texture);

                    matrix.put (0, 0, texture.GetRawTextureData ());

                    // OpenCV uses an inverted coordinate system. Y-0 is the top of the image, whereas in OpenGL (and so NatCam), Y-0 is the bottom of the image.
                    Core.flip (matrix, matrix, 0);
                } else {
                    if (fpsMonitor != null) {
                        fpsMonitor.consoleText = "SystemInfo.copyTextureSupport: None";
                    }
                    return null;
                }

                break;
            }

            return matrix;
        }

        /// <summary>
        /// Process the image.
        /// </summary>
        /// <param name="matrix">Mat.</param>
        /// <param name="imageProcessingType">ImageProcessingType.</param>
        private void ProcessImage (Mat matrix, ImageProcessingType imageProcessingType = ImageProcessingType.None)
        {
            switch (imageProcessingType) {
            case ImageProcessingType.DrawLine:
                // Draw a diagonal line on our image
                #if UNITY_IOS && !UNITY_EDITOR 
                Imgproc.line (matrix, new Point (0, 0), new Point (matrix.cols (), matrix.rows ()), new Scalar (0, 0, 255, 255), 4);
                #else
                Imgproc.line (matrix, new Point (0, 0), new Point (matrix.cols (), matrix.rows ()), new Scalar (255, 0, 0, 255), 4);
                #endif

                break;
            case ImageProcessingType.ConvertToGray:
                // Convert a four-channel mat image to greyscale
                if (grayMatrix != null && (grayMatrix.width() != matrix.width() || grayMatrix.height() != matrix.height())) {
                    grayMatrix.Dispose();
                    grayMatrix = null;
                }
                grayMatrix = grayMatrix ?? new Mat(matrix.height(), matrix.width(), CvType.CV_8UC1);
                
                #if UNITY_IOS && !UNITY_EDITOR 
                Imgproc.cvtColor (matrix, grayMatrix, Imgproc.COLOR_BGRA2GRAY);
                Imgproc.cvtColor (grayMatrix, matrix, Imgproc.COLOR_GRAY2BGRA);
                #else
                Imgproc.cvtColor (matrix, grayMatrix, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.cvtColor (grayMatrix, matrix, Imgproc.COLOR_GRAY2RGBA);
                #endif

                break;
            }
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
            if (grayMatrix != null) {
                grayMatrix.Dispose ();
                grayMatrix = null;
            }
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
            SwitchCamera ();
        }

        /// <summary>
        /// Raises the mat capture method dropdown value changed event.
        /// </summary>
        public void OnMatCaptureMethodDropdownValueChanged (int result)
        {
            if ((int)matCaptureMethod != result) {
                matCaptureMethod = (MatCaptureMethod)result;
                if (fpsMonitor != null) {
                    fpsMonitor.consoleText = "";
                }
            }
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