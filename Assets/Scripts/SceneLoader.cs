using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public static class SceneLoader
{
    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) { Debug.LogWarning("Empty scene name"); return; }
        if (!IsInBuild(sceneName)) { Debug.LogError($"Scene '{sceneName}' not in Build Settings"); return; }
        SceneManager.LoadScene(sceneName);
    }

    static bool IsInBuild(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (Path.GetFileNameWithoutExtension(path) == sceneName) return true;
        }
        return false;
    }
}
