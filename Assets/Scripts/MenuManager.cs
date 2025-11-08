using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public void LoadCeviche() => SceneLoader.LoadScene("CevicheScene");
    public void LoadLomoSaltado() => SceneLoader.LoadScene("LomoSaltadoScene");
    public void LoadSceneByName(string sceneName) => SceneLoader.LoadScene(sceneName);
}
