using UnityEngine;
using System;
using OpenCVForUnity;

namespace NatCamWithOpenCVForUnityExample {

    public interface ICameraSource : IDisposable {

        #region --Properties--
        int width { get; }
        int height { get; }
        #endregion
        

        #region --Operations--
        void StartPreview (Action startCallback, Action frameCallback);
        void CaptureFrame (Mat matrix);
        void CaptureFrame (Color32[] pixelBuffer);
        void SwitchCamera ();
        #endregion
    }
}