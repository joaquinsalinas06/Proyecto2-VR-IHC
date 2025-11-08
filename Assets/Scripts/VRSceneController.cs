using UnityEngine;

public class VRSceneController : MonoBehaviour
{
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Three, OVRInput.Controller.LTouch))
        {
            SceneLoader.LoadScene("MenuPrincipal");
        }
    }
}