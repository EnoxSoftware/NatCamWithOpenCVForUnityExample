using OpenCVForUnity.CoreModule;
using System;
using UnityEngine;

namespace NatCamWithOpenCVForUnityExample
{

    public interface ICameraSource : IDisposable
    {

        #region --Properties--

        int width { get; }

        int height { get; }

        bool isRunning { get; }

        bool isFrontFacing { get; }

        #endregion


        #region --Operations--

        void StartPreview(Action startCallback, Action frameCallback);

        void CaptureFrame(Mat matrix);

        void CaptureFrame(Color32[] pixelBuffer);

        void CaptureFrame(byte[] pixelBuffer);

        void SwitchCamera();

        #endregion
    }
}