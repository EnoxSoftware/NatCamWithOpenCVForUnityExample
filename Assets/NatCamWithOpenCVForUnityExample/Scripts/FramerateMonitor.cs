using UnityEngine;
using UnityEngine.UI;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace NatCamWithOpenCVForUnityExample {

    public class FramerateMonitor : MonoBehaviour {
        
		public Text framerateText;
        private Stopwatch stopwatch;

        void Start () {
            stopwatch = Stopwatch.StartNew();
        }

        void Update () {
            // Compute framerate
            var time = stopwatch.ElapsedMilliseconds;
            var framerate = 1e+3f / time;
            framerateText.text = ((int)framerate).ToString();
            // Restart
            stopwatch.Reset();
            stopwatch.Start();
        }
    }
}