using UnityEngine;

public class VRMenuController : MonoBehaviour
{
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            SceneLoader.LoadScene("CevicheScene");
        }
        
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            SceneLoader.LoadScene("LomoSaltadoScene");
        }
    }
}