using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NatCamWithOpenCVForUnityExample
{

    public abstract class ExampleBase<T> : MonoBehaviour where T : ICameraSource
    {

        [Header("Preview")]
        public RawImage rawImage;
        public AspectRatioFitter aspectFitter;

        [Header("Camera")]
        public bool useFrontCamera;

        [Header("Processing")]
        public bool performImageProcessingEachTime = false;
        public ImageProcessingType imageProcessingType = ImageProcessingType.None;
        public Dropdown imageProcessingTypeDropdown;

        protected T cameraSource;

        protected bool didUpdateThisFrame;
        protected int updateCount, onFrameCount, drawCount;
        protected float elapsed, updateFPS, onFrameFPS, drawFPS;


        #region --Lifecycle--

        protected abstract void Start();

        protected virtual void Update()
        {
            if (!performImageProcessingEachTime)
            {
                if (cameraSource != null && cameraSource.isRunning && didUpdateThisFrame)
                {
                    UpdateTexture();
                    drawCount++;
                }
            }

            updateCount++;
            elapsed += Time.deltaTime;
            if (elapsed >= 1f)
            {
                updateFPS = updateCount / elapsed;
                onFrameFPS = onFrameCount / elapsed;
                drawFPS = drawCount / elapsed;
                updateCount = onFrameCount = drawCount = 0;
                elapsed = 0f;
            }
        }

        protected virtual void LateUpdate()
        {
            didUpdateThisFrame = false;
        }

        protected virtual void OnStart()
        {
            //Debug.Log("##### OnStart() #####");

            useFrontCamera = cameraSource.isFrontFacing;
        }

        protected virtual void OnFrame()
        {
            onFrameCount++;
            didUpdateThisFrame = true;

            if (performImageProcessingEachTime)
            {
                UpdateTexture();
                drawCount++;
            }
        }

        protected abstract void OnDestroy();

        #endregion


        #region --UI Callbacks--

        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("NatCamWithOpenCVForUnityExample");
        }

        public void OnPlayButtonClick()
        {
            //Debug.Log("##### OnPlayButton() #####");

            if (cameraSource != null)
                return;
            Start();


        }

        public void OnStopButtonClick()
        {
            //Debug.Log("##### OnStopButton() #####");

            if (cameraSource == null)
                return;
            cameraSource.Dispose();
            cameraSource = default(T);


        }

        public void OnChangeCameraButtonClick()
        {
            if (cameraSource == null)
                return;
            cameraSource.SwitchCamera();
        }

        public void OnImageProcessingTypeDropdownValueChanged(int result)
        {
            imageProcessingType = (ImageProcessingType)result;
        }

        #endregion


        #region --Operations--

        protected abstract void UpdateTexture();

        protected void ProcessImage(Color32[] buffer, int width, int height, ImageProcessingType imageProcessingType)
        {
            switch (imageProcessingType)
            {
                case ImageProcessingType.DrawLine:
                    // Draw a diagonal line on our image
                    float inclination = height / (float)width;
                    for (int i = 0; i < 4; i++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int y = (int)(-inclination * x) + height - 2 + i;
                            y = Mathf.Clamp(y, 0, height - 1);
                            int p = (x) + (y * width);
                            // Set pixels in the buffer
                            buffer.SetValue(new Color32(255, 0, 0, 255), p);
                        }
                    }

                    break;
                case ImageProcessingType.ConvertToGray:
                    // Convert a four-channel pixel buffer to greyscale
                    // Iterate over the buffer
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        var p = buffer[i];
                        // Get channel intensities
                        byte r = p.r, g = p.g, b = p.b, a = p.a,
                        // Use quick luminance approximation to save time and memory
                        l = (byte)((r + r + r + b + g + g + g + g) >> 3);
                        // Set pixels in the buffer
                        buffer[i] = new Color32(l, l, l, a);
                    }
                    break;
            }
        }

        protected void ProcessImage(byte[] buffer, int width, int height, ImageProcessingType imageProcessingType)
        {
            switch (imageProcessingType)
            {
                case ImageProcessingType.DrawLine:
                    // Draw a diagonal line on our image
                    float inclination = height / (float)width;
                    for (int i = 0; i < 4; i++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int y = (int)(-inclination * x) + height - 2 + i;
                            y = Mathf.Clamp(y, 0, height - 1);
                            int p = (x * 4) + (y * width * 4);
                            // Set pixels in the buffer
                            buffer[p] = 255;
                            buffer[p + 1] = 0;
                            buffer[p + 2] = 0;
                            buffer[p + 3] = 255;
                        }
                    }

                    break;
                case ImageProcessingType.ConvertToGray:
                    // Convert a four-channel pixel buffer to greyscale
                    // Iterate over the buffer
                    for (int i = 0; i < buffer.Length; i = i + 4)
                    {
                        // Get channel intensities
                        byte r = buffer[i];
                        byte g = buffer[i + 1];
                        byte b = buffer[i + 2];
                        byte a = buffer[i + 3];
                        // Use quick luminance approximation to save time and memory
                        byte l = (byte)((r + r + r + b + g + g + g + g) >> 3);
                        // Set pixels in the buffer
                        buffer[i] = buffer[i + 1] = buffer[i + 2] = l;
                        buffer[i + 3] = a;
                    }
                    break;
            }
        }

        protected void ProcessImage(Mat frameMatrix, Mat grayMatrix, ImageProcessingType imageProcessingType)
        {
            switch (imageProcessingType)
            {
                case ImageProcessingType.DrawLine:
                    Imgproc.line(
                        frameMatrix,
                        new Point(0, 0),
                        new Point(frameMatrix.cols(), frameMatrix.rows()),
                        new Scalar(255, 0, 0, 255),
                        4
                    );
                    break;
                case ImageProcessingType.ConvertToGray:
                    Imgproc.cvtColor(frameMatrix, grayMatrix, Imgproc.COLOR_RGBA2GRAY);
                    Imgproc.cvtColor(grayMatrix, frameMatrix, Imgproc.COLOR_GRAY2RGBA);
                    break;
            }
        }

        #endregion
    }

    public enum ImageProcessingType
    {
        None,
        DrawLine,
        ConvertToGray,
    }
}