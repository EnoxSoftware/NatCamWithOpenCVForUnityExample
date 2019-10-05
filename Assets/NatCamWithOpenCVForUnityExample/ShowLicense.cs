using UnityEngine;
using UnityEngine.SceneManagement;

namespace NatCamWithOpenCVForUnityExample
{

    public class ShowLicense : MonoBehaviour
    {

        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("NatCamWithOpenCVForUnityExample");
        }
    }
}
