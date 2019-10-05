using NatShareU;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine;
using UnityEngine.UI;

namespace NatCamWithOpenCVForUnityExample
{

    /// <summary>
    /// Integration With NatShare Example
    /// An example of the native sharing and save to the camera roll using NatShare.
    /// </summary>
    public class IntegrationWithNatShareExample : ExampleBase<NatCamSource>
    {
        public Toggle applyComicFilterToggle;

        Mat frameMatrix;
        Texture2D texture;
        ComicFilter comicFilter;

        FpsMonitor fpsMonitor;

        string exampleTitle = "";
        string exampleSceneTitle = "";
        string settingInfo1 = "";
        Scalar textColor = new Scalar(255, 255, 255, 255);
        Point textPos = new Point();

        protected override void Start()
        {
            // Load global camera benchmark settings.
            int width, height, framerate;
            NatCamWithOpenCVForUnityExample.CameraConfiguration(out width, out height, out framerate);
            NatCamWithOpenCVForUnityExample.ExampleSceneConfiguration(out performImageProcessingEachTime);
            // Create camera source
            cameraSource = new NatCamSource(width, height, framerate, useFrontCamera);
            if (!cameraSource.activeCamera)
                cameraSource = new NatCamSource(width, height, framerate, !useFrontCamera);
            cameraSource.StartPreview(OnStart, OnFrame);
            // Create comic filter
            comicFilter = new ComicFilter();

            exampleTitle = "[NatCamWithOpenCVForUnity Example] (" + NatCamWithOpenCVForUnityExample.GetNatCamVersion() + ")";
            exampleSceneTitle = "- Integration With NatShare Example";

            fpsMonitor = GetComponent<FpsMonitor>();
            if (fpsMonitor != null)
            {
                fpsMonitor.Add("Name", "IntegrationWithNatShareExample");
                fpsMonitor.Add("performImageProcessingEveryTime", performImageProcessingEachTime.ToString());
                fpsMonitor.Add("onFrameFPS", onFrameFPS.ToString("F1"));
                fpsMonitor.Add("drawFPS", drawFPS.ToString("F1"));
                fpsMonitor.Add("width", "");
                fpsMonitor.Add("height", "");
                fpsMonitor.Add("isFrontFacing", "");
                fpsMonitor.Add("orientation", "");
            }
        }

        protected override void OnStart()
        {
            settingInfo1 = "- resolution: " + cameraSource.width + "x" + cameraSource.height;

            // Create matrix
            if (frameMatrix != null)
                frameMatrix.Dispose();
            frameMatrix = new Mat(cameraSource.height, cameraSource.width, CvType.CV_8UC4);
            // Create texture
            if (texture != null)
                Texture2D.Destroy(texture);
            texture = new Texture2D(
                cameraSource.width,
                cameraSource.height,
                TextureFormat.RGBA32,
                false,
                false
            );
            // Display preview
            rawImage.texture = texture;
            aspectFitter.aspectRatio = cameraSource.width / (float)cameraSource.height;
            Debug.Log("NatCam camera source started with resolution: " + cameraSource.width + "x" + cameraSource.height + " isFrontFacing: " + cameraSource.isFrontFacing);

            if (fpsMonitor != null)
            {
                fpsMonitor.Add("width", cameraSource.width.ToString());
                fpsMonitor.Add("height", cameraSource.height.ToString());
                fpsMonitor.Add("isFrontFacing", cameraSource.isFrontFacing.ToString());
                fpsMonitor.Add("orientation", Screen.orientation.ToString());
            }
        }

        protected override void Update()
        {
            base.Update();

            if (updateCount == 0)
            {
                if (fpsMonitor != null)
                {
                    fpsMonitor.Add("onFrameFPS", onFrameFPS.ToString("F1"));
                    fpsMonitor.Add("drawFPS", drawFPS.ToString("F1"));
                    fpsMonitor.Add("orientation", Screen.orientation.ToString());
                }
            }
        }

        protected override void UpdateTexture()
        {
            cameraSource.CaptureFrame(frameMatrix);

            if (applyComicFilterToggle.isOn)
                comicFilter.Process(frameMatrix, frameMatrix);

            textPos.x = 5;
            textPos.y = frameMatrix.rows() - 50;
            Imgproc.putText(frameMatrix, exampleTitle, textPos, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, textColor, 1, Imgproc.LINE_AA, false);
            textPos.y = frameMatrix.rows() - 30;
            Imgproc.putText(frameMatrix, exampleSceneTitle, textPos, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, textColor, 1, Imgproc.LINE_AA, false);
            textPos.y = frameMatrix.rows() - 10;
            Imgproc.putText(frameMatrix, settingInfo1, textPos, Imgproc.FONT_HERSHEY_SIMPLEX, 0.5, textColor, 1, Imgproc.LINE_AA, false);

            // Convert to Texture2D
            Utils.fastMatToTexture2D(frameMatrix, texture, true, 0, false);
        }

        protected override void OnDestroy()
        {
            if (cameraSource != null)
            {
                cameraSource.Dispose();
                cameraSource = null;
            }
            if (frameMatrix != null)
                frameMatrix.Dispose();
            frameMatrix = null;
            Texture2D.Destroy(texture);
            texture = null;
            comicFilter.Dispose();
            comicFilter = null;
        }

        public void OnShareButtonClick()
        {
            NatShare.Share(texture,
                () =>
                {
                    Debug.Log("sharing is complete.");
                });
        }

        public void OnSaveToCameraRollButtonClick()
        {
            NatShare.SaveToCameraRoll(texture, "NatCamWithOpenCVForUnityExample");
        }
    }
}