using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace NatCamWithOpenCVForUnityExample
{
    /// <summary>
    /// WebCamTexture Only Example
    /// An example of displaying the preview frame of camera only using WebCamTexture API.
    /// </summary>
    public class WebCamTextureOnlyExample : MonoBehaviour
    {
        /// <summary>
        /// Set this to specify the name of the device to use.
        /// </summary>
        public string requestedDeviceName = null;

        /// <summary>
        /// Set the requested width of the camera device.
        /// </summary>
        public int requestedWidth = 640;
        
        /// <summary>
        /// Set the requested height of the camera device.
        /// </summary>
        public int requestedHeight = 480;

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
        /// The colors.
        /// </summary>
        Color32[] colors;

        /// <summary>
        /// The rotated colors.
        /// </summary>
        Color32[] rotatedColors;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;
        
        /// <summary>
        /// Determines if rotates 90 degree.
        /// </summary>
        bool rotate90Degree = false;

        /// <summary>
        /// Indicates whether this instance is waiting for initialization to complete.
        /// </summary>
        bool isInitWaiting = false;

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        bool hasInitDone = false;
        
        /// <summary>
        /// The screenOrientation.
        /// </summary>
        ScreenOrientation screenOrientation;

        /// <summary>
        /// The width of the screen.
        /// </summary>
        int screenWidth;

        /// <summary>
        /// The height of the screen.
        /// </summary>
        int screenHeight;


        public enum ImageProcessingType
        {
            None,
            DrawLine,
            ConvertToGray,
        }

        [Header("OpenCV")]
        public ImageProcessingType imageProcessingType = ImageProcessingType.None;
        public Dropdown imageProcessingTypeDropdown; 

        [Header("Preview")]
        public RawImage preview;
        public AspectRatioFitter aspectFitter;


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
            fpsMonitor = GetComponent<FpsMonitor> ();
            if (fpsMonitor != null){
                fpsMonitor.Add ("Name", "WebCamTextureOnlyExample");
                fpsMonitor.Add ("onFrameFPS", onFrameFPS.ToString("F1"));
                fpsMonitor.Add ("drawFPS", drawFPS.ToString("F1"));
                fpsMonitor.Add ("width", "");
                fpsMonitor.Add ("height", "");
                fpsMonitor.Add ("orientation", "");
            }

            imageProcessingTypeDropdown.value = (int)imageProcessingType;

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

                    screenOrientation = Screen.orientation;
                    screenWidth = Screen.width;
                    screenHeight = Screen.height;
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
            if (texture != null) {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Raises the webcam texture initialized event.
        /// </summary>
        private void OnInited ()
        {
            if (colors == null || colors.Length != webCamTexture.width * webCamTexture.height) {
                colors = new Color32[webCamTexture.width * webCamTexture.height];
                rotatedColors = new Color32[webCamTexture.width * webCamTexture.height];
            }

            #if !UNITY_EDITOR && !(UNITY_STANDALONE || UNITY_WEBGL) 
            if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown) {
                rotate90Degree = true;
            }else{
                rotate90Degree = false;
            }
            #endif

            if (rotate90Degree) {
                texture = new Texture2D (webCamTexture.height, webCamTexture.width, TextureFormat.RGBA32, false);
            } else {
                texture = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            }  

            // Scale the panel to match aspect ratios
            aspectFitter.aspectRatio = texture.width / (float)texture.height;

            Debug.Log ("OnInited (): " + texture.width + " " + texture.height);
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

                    if (texture != null) {
                        fpsMonitor.Add ("width", texture.width.ToString ());
                        fpsMonitor.Add ("height", texture.height.ToString ());
                    }
                    fpsMonitor.Add ("orientation", Screen.orientation.ToString());
                }
            }
            

            // Catch the orientation change of the screen.
            if (screenOrientation != Screen.orientation && (screenWidth != Screen.width || screenHeight != Screen.height)) {
                Initialize ();
            } else {
                screenWidth = Screen.width;
                screenHeight = Screen.height;
            }

            if (hasInitDone && webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame) {

                drawCount++;

                Color32[] colors = GetColors ();

                if (colors != null) {

                    if (imageProcessingType != ImageProcessingType.None) {
                        // Process
                        ProcessImage (colors, texture.width, texture.height, colors.Length, imageProcessingType);
                    }

                    // Set texture data
                    texture.SetPixels32 (colors);
                    // Upload to GPU
                    texture.Apply ();
                    // Display the result
                    preview.texture = texture;
                }
            }
        }

        /// <summary>
        /// Gets the current WebCameraTexture frame that converted to the correct direction.
        /// </summary>
        private Color32[] GetColors ()
        {
            webCamTexture.GetPixels32 (colors);
           
            //Adjust an array of color pixels according to screen orientation and WebCamDevice parameter.
            if (rotate90Degree) {
                Rotate90CW (colors, rotatedColors, webCamTexture.width, webCamTexture.height);
                FlipColors (rotatedColors, webCamTexture.width, webCamTexture.height);
                return rotatedColors;
            } else {
                FlipColors (colors, webCamTexture.width, webCamTexture.height);
                return colors;
            }
        }

        /// <summary>
        /// Process the image.
        /// </summary>
        /// <param name="buffer">Colors.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="size">Size.</param>
        /// <param name="imageProcessingType">ImageProcessingType.</param>
        private void ProcessImage (Color32[] buffer, int width, int height, int size, ImageProcessingType imageProcessingType = ImageProcessingType.None)
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
                        int p = (x) + (y * width);
                        // Set pixels in the buffer
                        buffer.SetValue(new Color32(255, 0, 0, 255), p);
                    }
                }

                break;
            case ImageProcessingType.ConvertToGray:
                // Convert a four-channel pixel buffer to greyscale
                // Iterate over the buffer
                for (int i = 0; i < size; i++) {

                    Color32 p = (Color32)buffer.GetValue (i);
                    // Get channel intensities
                    byte
                    r = p.r, g = p.g,
                    b = p.b, a = p.a,
                    // Use quick luminance approximation to save time and memory
                    l = (byte)((r + r + r + b + g + g + g + g) >> 3);
                    // Set pixels in the buffer
                    buffer.SetValue(new Color32(l, l, l, a), i);
                }

                break;
            }
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
        /// Flips the colors.
        /// </summary>
        /// <param name="colors">Colors.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        void FlipColors (Color32[] colors, int width, int height)
        {
            int flipCode = int.MinValue;

            if (webCamDevice.isFrontFacing) {
                if (webCamTexture.videoRotationAngle == 0) {
                    flipCode = 1;
                } else if (webCamTexture.videoRotationAngle == 90) {
                    flipCode = 1;
                }
                if (webCamTexture.videoRotationAngle == 180) {
                    flipCode = 0;
                } else if (webCamTexture.videoRotationAngle == 270) {
                    flipCode = 0;
                }
            } else {
                if (webCamTexture.videoRotationAngle == 180) {
                    flipCode = -1;
                } else if (webCamTexture.videoRotationAngle == 270) {
                    flipCode = -1;
                }
            }                

            if (flipCode > int.MinValue) {
                if (rotate90Degree) {
                    if (flipCode == 0) {
                        FlipVertical (colors, colors, height, width);
                    } else if (flipCode == 1) {
                        FlipHorizontal (colors, colors, height, width);
                    } else if (flipCode < 0) {
                        Rotate180 (colors, colors, height, width);
                    }
                } else {
                    if (flipCode == 0) {
                        FlipVertical (colors, colors, width, height);
                    } else if (flipCode == 1) {
                        FlipHorizontal (colors, colors, width, height);
                    } else if (flipCode < 0) {
                        Rotate180 (colors, colors, height, width);
                    }
                }
            }
        }

        /// <summary>
        /// Flips vertical.
        /// </summary>
        /// <param name="src">Src colors.</param>
        /// <param name="dst">Dst colors.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        void FlipVertical (Color32[] src, Color32[] dst, int width, int height)
        {
            for(var i = 0; i < height / 2; i++) {
                var y = i * width;
                var x = (height - i - 1) * width;
                for(var j = 0; j < width; j++) {
                    int s = y + j;
                    int t = x + j;
                    Color32 c = src[s];
                    dst[s] = src[t];
                    dst[t] = c;
                }
            }
        }

        /// <summary>
        /// Flips horizontal.
        /// </summary>
        /// <param name="src">Src colors.</param>
        /// <param name="dst">Dst colors.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        void FlipHorizontal (Color32[] src, Color32[] dst, int width, int height)
        {
            for (int i = 0; i < height; i++) {
                int y = i * width;
                int x = y + width - 1;
                for(var j = 0; j < width / 2; j++) {
                    int s = y + j;
                    int t = x - j;
                    Color32 c = src[s];
                    dst[s] = src[t];
                    dst[t] = c;
                }
            }
        }

        /// <summary>
        /// Rotates 180 degrees.
        /// </summary>
        /// <param name="src">Src colors.</param>
        /// <param name="dst">Dst colors.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        void Rotate180 (Color32[] src, Color32[] dst, int height, int width)
        {
            int i = src.Length;
            for (int x = 0; x < i/2; x++) {
                Color32 t = src[x];
                dst[x] = src[i-x-1];
                dst[i-x-1] = t;
            }
        }

        /// <summary>
        /// Rotates 90 degrees (CLOCKWISE).
        /// </summary>
        /// <param name="src">Src colors.</param>
        /// <param name="dst">Dst colors.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        void Rotate90CW (Color32[] src, Color32[] dst, int height, int width)
        {
            int i = 0;
            for (int x = height - 1; x >= 0; x--) {
                for (int y = 0; y < width; y++) {
                    dst [i] = src [x + y * height];
                    i++;
                }
            }
        }

        /// <summary>
        /// Rotates 90 degrees (COUNTERCLOCKWISE).
        /// </summary>
        /// <param name="src">Src colors.</param>
        /// <param name="dst">Dst colors.</param>
        /// <param name="height">Height.</param>
        /// <param name="width">Width.</param>
        void Rotate90CCW (Color32[] src, Color32[] dst, int width, int height)
        {
            int i = 0;
            for (int x = 0; x < width; x++) {
                for (int y = height - 1; y >= 0; y--) {
                    dst [i] = src [x + y * width];
                    i++;
                }
            }
        }
    }
}