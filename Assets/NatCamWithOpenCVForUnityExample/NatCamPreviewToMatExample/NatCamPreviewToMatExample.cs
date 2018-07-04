using UnityEngine;
using NatCamU.Core;
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
    public class NatCamPreviewToMatExample : MonoBehaviour
    {
        public enum MatCaptureMethod
        {
            NatCam_CaptureFrame,
            NatCam_CaptureFrame_OpenCVFlip,
            BlitWithReadPixels,
            Graphics_CopyTexture,
        }

        public enum ImageProcessingType
        {
            None,
            DrawLine,
            ConvertToGray,
        }

        // An image flipping method for returning a display image from the OpenCV coordinate system (Y - 0 is the top of the image) to the OpenGL coordinate system (Y - 0 is the bottom of the image).
        public enum ImageFlippingMethod
        {
            OpenCVForUnity_Flip,
            Shader,
        }

        [Header("Camera")]
        public bool useFrontCamera;

        [Header("Preview")]
        public RawImage preview;
        public CameraResolution previewResolution = CameraResolution._1280x720;
        public int requestedFPS = 30;
        public AspectRatioFitter aspectFitter;
        public ImageFlippingMethod imageFlippingMethod = ImageFlippingMethod.OpenCVForUnity_Flip;
        public Dropdown imageFlippingMethodDropdown; 
        Material originalMaterial;
        Material viewMaterial;

        [Header("OpenCV")]
        public MatCaptureMethod matCaptureMethod = MatCaptureMethod.NatCam_CaptureFrame;
        public Dropdown matCaptureMethodDropdown; 
        public ImageProcessingType imageProcessingType = ImageProcessingType.None;
        public Dropdown imageProcessingTypeDropdown; 

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
        Mat grayMatrix;
        byte[] pixelBuffer;
        Texture2D texture;
        const TextureFormat textureFormat = TextureFormat.RGBA32;

        public virtual void Start () 
        {
            fpsMonitor = GetComponent<FpsMonitor> ();

            if (!NatCam.Implementation.HasPermissions) {
                Debug.LogError ("NatCam.Implementation.HasPermissions == false");

                if (fpsMonitor != null)
                    fpsMonitor.consoleText = "NatCam.Implementation.HasPermissions == false";
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
                
            SetMaterials ();

            // Set the camera's preview resolution
            NatCam.Camera.PreviewResolution = previewResolution;
            // Set the camera framerate
            NatCam.Camera.Framerate = requestedFPS;
            NatCam.Play();
            NatCam.OnStart += OnStart;
            NatCam.OnFrame += OnFrame;

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
            imageFlippingMethodDropdown.value = (int)imageFlippingMethod;
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
            Utils.copyToMat (pixelBuffer, matrix);

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

            if (fpsMonitor != null)
                fpsMonitor.consoleText = "";

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

                Mat matrix = GetMat (matCaptureMethod);

                if (matrix != null) {

                    ProcessImage (matrix, imageProcessingType);

                    // The Imgproc.putText method is too heavy to use for mobile device benchmark purposes.
                    //Imgproc.putText (matrix, "W:" + matrix.width () + " H:" + matrix.height () + " SO:" + Screen.orientation, new Point (5, matrix.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    //Imgproc.putText (matrix, "updateFPS:" + updateFPS.ToString("F1") + " onFrameFPS:" + onFrameFPS.ToString("F1") + " drawFPS:" + drawFPS.ToString("F1"), new Point (5, matrix.rows () - 50), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                    switch (imageFlippingMethod) {
                    default:
                    case ImageFlippingMethod.OpenCVForUnity_Flip:
                        // Restore the coordinate system of the image by OpenCV's Flip function.
                        Utils.fastMatToTexture2D (matrix, texture, true, 0, false);
                        break;
                    case ImageFlippingMethod.Shader:
                        // Restore the coordinate system of the image by Shader. (use GPU)
                        Utils.fastMatToTexture2D (matrix, texture, false, 0, false);
                        break;
                    }
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
        private Mat GetMat (MatCaptureMethod matCaptureMethod = MatCaptureMethod.NatCam_CaptureFrame)
        {
            if (matrix.cols () != NatCam.Preview.width || matrix.rows () != NatCam.Preview.height)
                return null;

            switch (matCaptureMethod) {
            default:
            case MatCaptureMethod.NatCam_CaptureFrame:
                // Get the preview data
                // Set `flip` flag to true because OpenCV uses inverted Y-coordinate system
                NatCam.CaptureFrame(pixelBuffer, true);

                Utils.copyToMat (pixelBuffer, matrix);

                break;
            case MatCaptureMethod.NatCam_CaptureFrame_OpenCVFlip:
                // Get the preview data
                NatCam.CaptureFrame(pixelBuffer, false);

                Utils.copyToMat (pixelBuffer, matrix);

                // OpenCV uses an inverted coordinate system. Y-0 is the top of the image, whereas in OpenGL (and so NatCam), Y-0 is the bottom of the image.
                Core.flip (matrix, matrix, 0);

                break;
            case MatCaptureMethod.BlitWithReadPixels:

                // When NatCam.PreviewMatrix function does not work properly. (Zenfone 2)
                // The workaround for a device like this is to use Graphics.Blit with Texture2D.ReadPixels and Texture2D.GetRawTextureData/GetPixels32 to download the pixel data from the GPU.
                // Blit the NatCam preview to a temporary render texture; set the RT active and readback into a Texture2D (using ReadPixels), then access the pixel data in the texture.
                // The texture2D's TextureFormat needs to be RGBA32(Unity5.5+), ARGB32, RGB24, RGBAFloat or RGBAHalf.
                Utils.textureToTexture2D (NatCam.Preview, texture);

                Utils.copyToMat (texture.GetRawTextureData (), matrix);

                // OpenCV uses an inverted coordinate system. Y-0 is the top of the image, whereas in OpenGL (and so NatCam), Y-0 is the bottom of the image.
                Core.flip (matrix, matrix, 0);

                break;
            case MatCaptureMethod.Graphics_CopyTexture:

                // When NatCam.PreviewMatrix function does not work properly. (Zenfone 2)
                if (SystemInfo.copyTextureSupport != UnityEngine.Rendering.CopyTextureSupport.None) {
                    Graphics.CopyTexture (NatCam.Preview, texture);

                    Utils.copyToMat (texture.GetRawTextureData (), matrix);

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
                Imgproc.line (matrix, new Point (0, 0), new Point (matrix.cols (), matrix.rows ()), new Scalar (255, 0, 0, 255), 4);

                break;
            case ImageProcessingType.ConvertToGray:
                // Convert a four-channel mat image to greyscale
                if (grayMatrix != null && (grayMatrix.width() != matrix.width() || grayMatrix.height() != matrix.height())) {
                    grayMatrix.Dispose();
                    grayMatrix = null;
                }
                grayMatrix = grayMatrix ?? new Mat(matrix.height(), matrix.width(), CvType.CV_8UC1);

                Imgproc.cvtColor (matrix, grayMatrix, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.cvtColor (grayMatrix, matrix, Imgproc.COLOR_GRAY2RGBA);

                break;
            }
        }

        private void SetMaterials () {
            //Cache the original material
            originalMaterial = preview.materialForRendering;
            //Create the view material
            viewMaterial = new Material(Shader.Find("Hidden/NatCamWithOpenCVForUnity/ImageFlipShader"));
            //Set the raw image material
            preview.material = viewMaterial;
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

            //Reset material
            if (preview) preview.material = originalMaterial;
            //Destroy view material
            Destroy(viewMaterial);
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

        /// <summary>
        /// Raises the image flipping method dropdown value changed event.
        /// </summary>
        public void OnImageFlippingMethodDropdownValueChanged (int result)
        {
            if ((int)imageFlippingMethod != result) {
                imageFlippingMethod = (ImageFlippingMethod)result;
            }

            if (imageFlippingMethod == ImageFlippingMethod.Shader) {
                preview.materialForRendering.SetVector("_Mirror", new Vector2(0f, 1f));
            } else {
                preview.materialForRendering.SetVector("_Mirror", Vector2.zero);
            }
        }
    }
        
}