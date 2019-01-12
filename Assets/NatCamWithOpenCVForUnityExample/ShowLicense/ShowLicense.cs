using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace NatCamWithOpenCVForUnityExample {

    public class ShowLicense : MonoBehaviour {

        public void OnBackButtonClick () {
            SceneManager.LoadScene("NatCamWithOpenCVForUnityExample");
        }
    }
}
