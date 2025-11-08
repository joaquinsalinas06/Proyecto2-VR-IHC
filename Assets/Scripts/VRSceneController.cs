using UnityEngine;

public class VRSceneController : MonoBehaviour
{
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
        {
            SceneLoader.LoadScene("MenuPrincipal");
        }
    }
}