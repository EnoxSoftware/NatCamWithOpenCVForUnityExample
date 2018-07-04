using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;

namespace NatCamWithOpenCVForUnityExample
{
    /// <summary>
    /// WebCamTexture To Mat Example
    /// An example of converting a WebCamTexture image to OpenCV's Mat format.
    /// </summary>
    public class WebCamTextureToMatExample : MonoBehaviour
    {
        /// <summary>
        /// Set this to specify the name of the device to use.
        /// </summary>
        public string requestedDeviceName = null;

        /// <summary>
        /// Set the requested width of the camera device.
        /// </summary>
        public int requestedWidth = 1280;
        
        /// <summary>
        /// Set the requested height of the camera device.
        /// </summary>
        public int requestedHeight = 720;

        /// <summary>
        /// Set the requested fps of the camera device.
        /// </summary>
        public int requestedFPS = 30;
        
        /// <summary>
        /// Set the requested to using the front camera.
        /// </summary>
        public bool requestedIsFrontFacing = false;

        /// <summary>
        /// The webcam texture.
        /// </summary>
        WebCamTexture webCamTexture;

        /// <summary>
        /// The webcam device.
        /// </summary>
        WebCamDevice webCamDevice;

        /// <summary>
        /// The frame mat.
        /// </summary>
        Mat frameMat;

        /// <summary>
        /// The rotated frame mat
        /// </summary>
        Mat rotatedFrameMat;

        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMatrix;

        /// <summary>
        /// The colors.
        /// </summary>
        Color32[] colors;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// Indicates whether this instance is waiting for initialization to complete.
        /// </summary>
        bool isInitWaiting = false;

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        bool hasInitDone = false;


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

        [Header("OpenCV")]
        public ImageProcessingType imageProcessingType = ImageProcessingType.None;
        public Dropdown imageProcessingTypeDropdown; 

        [Header("Preview")]
        public RawImage preview;
        public AspectRatioFitter aspectFitter;
        public ImageFlippingMethod imageFlippingMethod = ImageFlippingMethod.OpenCVForUnity_Flip;
        public Dropdown imageFlippingMethodDropdown; 
        Material originalMaterial;
        Material viewMaterial;


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


        // Use this for initialization
        void Start ()
        {
            // Load global camera benchmark settings.
            int width, height, fps; 
            NatCamWithOpenCVForUnityExample.GetCameraResolution (out width, out height);
            NatCamWithOpenCVForUnityExample.GetCameraFps (out fps);
            requestedWidth = width;
            requestedHeight = height;
            requestedFPS = fps;

            SetMaterials ();

            fpsMonitor = GetComponent<FpsMonitor> ();
            if (fpsMonitor != null){
                fpsMonitor.Add ("Name", "WebCamTextureToMatExample");
                fpsMonitor.Add ("onFrameFPS", onFrameFPS.ToString("F1"));
                fpsMonitor.Add ("drawFPS", drawFPS.ToString("F1"));
                fpsMonitor.Add ("width", "");
                fpsMonitor.Add ("height", "");
                fpsMonitor.Add ("orientation", "");
            }

            imageProcessingTypeDropdown.value = (int)imageProcessingType;
            imageFlippingMethodDropdown.value = (int)imageFlippingMethod;

            Initialize ();
        }

        /// <summary>
        /// Initializes webcam texture.
        /// </summary>
        private void Initialize ()
        {
            if (isInitWaiting)
                return;

            #if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices. (Pixel, pixel 2)
            // https://forum.unity.com/threads/android-webcamtexture-in-low-light-only-some-models.520656/
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            if (requestedIsFrontFacing) {
                int rearCameraFPS = requestedFPS;
                requestedFPS = 15;
                StartCoroutine (_Initialize ());
                requestedFPS = rearCameraFPS;
            } else {
            StartCoroutine (_Initialize ());
            }
            #else
            StartCoroutine (_Initialize ());
            #endif
        }

        /// <summary>
        /// Initializes webcam texture by coroutine.
        /// </summary>
        private IEnumerator _Initialize ()
        {
            if (hasInitDone)
                Dispose ();

            isInitWaiting = true;

            // Creates the camera
            if (!String.IsNullOrEmpty (requestedDeviceName)) {
                int requestedDeviceIndex = -1;
                if (Int32.TryParse (requestedDeviceName, out requestedDeviceIndex)) {
                    if (requestedDeviceIndex >= 0 && requestedDeviceIndex < WebCamTexture.devices.Length) {
                        webCamDevice = WebCamTexture.devices [requestedDeviceIndex];
                        if (requestedFPS <= 0) {
                            webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight);
                        } else {
                            webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                        }
                    }
                } else {
                    for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {
                        if (WebCamTexture.devices [cameraIndex].name == requestedDeviceName) {
                            webCamDevice = WebCamTexture.devices [cameraIndex];
                            if (requestedFPS <= 0) {
                                webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight);
                            } else {
                                webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                            }
                            break;
                        }
                    }
                }
                if (webCamTexture == null)
                    Debug.Log ("Cannot find camera device " + requestedDeviceName + ".");
            }

            if (webCamTexture == null) {
                // Checks how many and which cameras are available on the device
                for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {                   
                    if (WebCamTexture.devices [cameraIndex].isFrontFacing == requestedIsFrontFacing) {
                        webCamDevice = WebCamTexture.devices [cameraIndex];
                        if (requestedFPS <= 0) {
                            webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight);
                        } else {
                            webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                        }
                        break;
                    }
                }
            }

            if (webCamTexture == null) {
                if (WebCamTexture.devices.Length > 0) {
                    webCamDevice = WebCamTexture.devices [0];
                    if (requestedFPS <= 0) {
                        webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight);
                    } else {
                        webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                    }
                } else {
                    Debug.LogError ("Camera device does not exist.");
                    isInitWaiting = false;
                    yield break;
                }
            }

            // Starts the camera.
            webCamTexture.Play ();

            while (true) {
                // If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/).
                #if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
                if (webCamTexture.width > 16 && webCamTexture.height > 16) {
                #else
                if (webCamTexture.didUpdateThisFrame) {
                    #if UNITY_IOS && !UNITY_EDITOR && UNITY_5_2                                    
                    while (webCamTexture.width <= 16) {
                        webCamTexture.GetPixels32 ();
                        yield return new WaitForEndOfFrame ();
                    } 
                    #endif
                #endif

                    Debug.Log ("name:" + webCamTexture.deviceName + " width:" + webCamTexture.width + " height:" + webCamTexture.height + " fps:" + webCamTexture.requestedFPS);
                    Debug.Log ("videoRotationAngle:" + webCamTexture.videoRotationAngle + " videoVerticallyMirrored:" + webCamTexture.videoVerticallyMirrored + " isFrongFacing:" + webCamDevice.isFrontFacing);

                    isInitWaiting = false;
                    hasInitDone = true;

                    OnInited ();

                    break;
                } else {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Releases all resource.
        /// </summary>
        private void Dispose ()
        {
            isInitWaiting = false;
            hasInitDone = false;

            if (webCamTexture != null) {
                webCamTexture.Stop ();
                WebCamTexture.Destroy(webCamTexture);
                webCamTexture = null;
            }
            if (frameMat != null) {
                frameMat.Dispose ();
                frameMat = null;
            }
            if (grayMatrix != null) {
                grayMatrix.Dispose ();
                grayMatrix = null;
            }
            if (texture != null) {
                Texture2D.Destroy(texture);
                texture = null;
            }

            //Reset material
            if (preview) preview.material = originalMaterial;
            //Destroy view material
            Destroy(viewMaterial);
        }

        /// <summary>
        /// Raises the webcam texture initialized event.
        /// </summary>
        private void OnInited ()
        {
            if (colors == null || colors.Length != webCamTexture.width * webCamTexture.height)
                colors = new Color32[webCamTexture.width * webCamTexture.height];

            if (texture && (texture.width != webCamTexture.width || texture.height != webCamTexture.height)) {
                Texture2D.Destroy(texture);
                texture = null;
            }
            texture = texture ?? new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

            if (frameMat != null && (frameMat.cols() != webCamTexture.width || frameMat.rows() != webCamTexture.height)) {
                frameMat.release();
                frameMat = null;
                rotatedFrameMat.release();
                rotatedFrameMat = null;
            }
            frameMat = frameMat ?? new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);
            rotatedFrameMat = rotatedFrameMat ?? new Mat(webCamTexture.width, webCamTexture.height, CvType.CV_8UC4);

            // Scale the panel to match aspect ratios
            aspectFitter.aspectRatio = webCamTexture.width / (float)webCamTexture.height;

            // Display the result
            preview.texture = texture;

            Debug.Log ("OnInited (): " + frameMat.cols() + " " + frameMat.rows() + " " + texture.width + " " + texture.height);
        }

        // Update is called once per frame
        void Update ()
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

                Debug.Log ("didUpdateThisFrame: " + webCamTexture.didUpdateThisFrame + " updateFPS: " + updateFPS + " onFrameFPS: " + onFrameFPS + " drawFPS: " + drawFPS);
                if (fpsMonitor != null) {
                    fpsMonitor.Add ("onFrameFPS", onFrameFPS.ToString("F1"));
                    fpsMonitor.Add ("drawFPS", drawFPS.ToString("F1"));

                    if (frameMat != null) {
                        fpsMonitor.Add ("width", texture.width.ToString ());
                        fpsMonitor.Add ("height", texture.height.ToString ());
                    }
                    fpsMonitor.Add ("orientation", Screen.orientation.ToString());
                }
            }

            if (hasInitDone && webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame) {

                drawCount++;

                Mat matrix = GetMat ();

                if (matrix != null) {

                    ProcessImage (matrix, imageProcessingType);

                    // The Imgproc.putText method is too heavy to use for mobile device benchmark purposes.
                    //Imgproc.putText (matrix, "W:" + matrix.width () + " H:" + matrix.height () + " SO:" + Screen.orientation, new Point (5, matrix.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
                    //Imgproc.putText (matrix, "updateFPS:" + updateFPS.ToString("F1") + " onFrameFPS:" + onFrameFPS.ToString("F1") + " drawFPS:" + drawFPS.ToString("F1"), new Point (5, matrix.rows () - 50), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                    if (texture.width != matrix.width () || texture.height != matrix.height ()) {
                        Texture2D.Destroy(texture);
                        texture = new Texture2D (matrix.width (), matrix.height (), TextureFormat.RGBA32, false);
                        preview.texture = texture;
                        aspectFitter.aspectRatio = texture.width / (float)texture.height;
                    }

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

        /// <summary>
        /// Gets the current WebCameraTexture frame that converted to the correct direction in OpenCV Matrix format.
        /// </summary>
        private Mat GetMat ()
        {
            Utils.webCamTextureToMat (webCamTexture, frameMat, colors, false);

            #if !UNITY_EDITOR && !(UNITY_STANDALONE || UNITY_WEBGL)
            if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown) {                
                if (webCamDevice.isFrontFacing){ 
                    FlipMat (frameMat, true, true);
                }else{
                    FlipMat (frameMat, false, false);
                }
                Core.rotate (frameMat, rotatedFrameMat, Core.ROTATE_90_CLOCKWISE);
                return rotatedFrameMat;
            } else {
                FlipMat (frameMat, false, false);
                return frameMat;
            }
            #else
            FlipMat (frameMat, false, false);
            return frameMat;
            #endif
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

        /// <summary>
        /// Flips the mat.
        /// </summary>
        /// <param name="mat">Mat.</param>
        protected virtual void FlipMat (Mat mat, bool flipVertical, bool flipHorizontal)
        {
            //Since the order of pixels of WebCamTexture and Mat is opposite, the initial value of flipCode is set to 0 (flipVertical).
            int flipCode = 0;
                
            if (webCamDevice.isFrontFacing) {
                if (webCamTexture.videoRotationAngle == 0) {
                    flipCode = -1;
                } else if (webCamTexture.videoRotationAngle == 90) {
                    flipCode = -1;
                }
                if (webCamTexture.videoRotationAngle == 180) {
                    flipCode = int.MinValue;
                } else if (webCamTexture.videoRotationAngle == 270) {
                    flipCode = int.MinValue;
                }
            } else {
                if (webCamTexture.videoRotationAngle == 180) {
                    flipCode = 1;
                } else if (webCamTexture.videoRotationAngle == 270) {
                    flipCode = 1;
                }
            }
                
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

        private void SetMaterials () {
            //Cache the original material
            originalMaterial = preview.materialForRendering;
            //Create the view material
            viewMaterial = new Material(Shader.Find("Hidden/NatCamWithOpenCVForUnity/ImageFlipShader"));
            //Set the raw image material
            preview.material = viewMaterial;
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
            if (hasInitDone)
                webCamTexture.Play ();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick ()
        {
            if (hasInitDone)
                webCamTexture.Pause ();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick ()
        {
            if (hasInitDone)
                webCamTexture.Stop ();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick ()
        {
            if (hasInitDone) {
                requestedDeviceName = null;
                requestedIsFrontFacing = !requestedIsFrontFacing;
                Initialize ();
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