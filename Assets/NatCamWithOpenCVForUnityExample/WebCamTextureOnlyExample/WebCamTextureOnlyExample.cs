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
    /// TODO The function to convert the camera frame image to the correct direction has not been implemented yet.
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

            #if UNITY_ANDROID && !UNITY_EDITOR
            // Set the requestedFPS parameter to avoid the problem of the WebCamTexture image becoming low light on some Android devices. (Pixel, pixel 2)
            // https://forum.unity.com/threads/android-webcamtexture-in-low-light-only-some-models.520656/
            // https://forum.unity.com/threads/released-opencv-for-unity.277080/page-33#post-3445178
            if (requestedIsFrontFacing) {
                int rearCameraFPS = requestedFPS;
                requestedFPS = 15;
                Initialize ();
                requestedFPS = rearCameraFPS;
            } else {
                Initialize ();
            }
            #else
            Initialize ();
            #endif
        }

        /// <summary>
        /// Initialize of web cam texture.
        /// </summary>
        private void Initialize ()
        {
            if (isInitWaiting)
                return;

            StartCoroutine (_Initialize ());
        }

        /// <summary>
        /// Initialize of webcam texture.
        /// </summary>
        /// <param name="deviceName">Device name.</param>
        /// <param name="requestedWidth">Requested width.</param>
        /// <param name="requestedHeight">Requested height.</param>
        /// <param name="requestedIsFrontFacing">If set to <c>true</c> requested to using the front camera.</param>
        private void Initialize (string deviceName, int requestedWidth, int requestedHeight, bool requestedIsFrontFacing)
        {
            if (isInitWaiting)
                return;

            this.requestedDeviceName = deviceName;
            this.requestedWidth = requestedWidth;
            this.requestedHeight = requestedHeight;
            this.requestedIsFrontFacing = requestedIsFrontFacing;

            StartCoroutine (_Initialize ());
        }

        /// <summary>
        /// Initialize of webcam texture by coroutine.
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
            if (texture != null) {
                Texture2D.Destroy(texture);
                texture = null;
            }
        }

        /// <summary>
        /// Initialize completion handler of the webcam texture.
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

            // Scale the panel to match aspect ratios
            aspectFitter.aspectRatio = webCamTexture.width / (float)webCamTexture.height;

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

            if (hasInitDone && webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame) {

                drawCount++;

                if (imageProcessingType == ImageProcessingType.None) {
                    preview.texture = webCamTexture;
                } else {
                    Color32[] pixels = webCamTexture.GetPixels32 ();
                    pixels.CopyTo (colors, 0);

                    // Process
                    ProcessImage (colors, webCamTexture.width, webCamTexture.height, colors.Length, imageProcessingType);

                    // Set texture data
                    texture.SetPixels32 (colors);
                    // Upload to GPU
                    texture.Apply ();
                    // Display the result
                    preview.texture = texture;
                }
            }
        }

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
                #if UNITY_ANDROID && !UNITY_EDITOR
                if (!requestedIsFrontFacing) {
                    int rearCameraFPS = requestedFPS;
                    requestedFPS = 15;
                    Initialize (null, requestedWidth, requestedHeight, !requestedIsFrontFacing);
                    requestedFPS = rearCameraFPS;
                } else {
                    Initialize (null, requestedWidth, requestedHeight, !requestedIsFrontFacing);
                }
                #else
                Initialize (null, requestedWidth, requestedHeight, !requestedIsFrontFacing);
                #endif
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