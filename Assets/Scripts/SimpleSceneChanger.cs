using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Script SIMPLE para cambiar de escena cuando SnapVisual se desactiva
/// NO necesitas arrastrar nada - todo se auto-detecta
/// </summary>
public class SimpleSceneChanger : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Nombre de la siguiente escena (debe estar en Build Settings)")]
    public string nextSceneName = "paso2";

    [Tooltip("Segundos de espera antes de cambiar")]
    public float delaySeconds = 2f;

    [Tooltip("Nombre del objeto SnapVisual a detectar")]
    public string snapVisualName = "SnapVisual";

    [Header("Debug")]
    public bool showDebugInfo = true;

    // Referencias internas
    private GameObject snapVisual;
    private bool transitionStarted = false;
    private bool wasActiveLastFrame = true;

    void Start()
    {
        // Buscar SnapVisual automáticamente en TODA la escena
        snapVisual = GameObject.Find(snapVisualName);

        if (snapVisual == null)
        {
            Debug.LogError($"[SimpleSceneChanger] ❌ NO se encontró '{snapVisualName}' en la escena!");
            Debug.LogError("[SimpleSceneChanger] Verifica que el nombre sea correcto o cámbialo en el Inspector");
            enabled = false;
            return;
        }

        Debug.Log($"[SimpleSceneChanger] ✓ SnapVisual encontrado: {snapVisual.name}");
        Debug.Log($"[SimpleSceneChanger] ✓ Estado inicial: {(snapVisual.activeSelf ? "ACTIVO" : "INACTIVO")}");
        Debug.Log($"[SimpleSceneChanger] ✓ Escena destino: {nextSceneName}");
        Debug.Log($"[SimpleSceneChanger] Esperando que SnapVisual se DESACTIVE...");
    }

    void Update()
    {
        if (snapVisual == null || transitionStarted) return;

        // Detectar cuando SnapVisual se DESACTIVA
        bool isActiveNow = snapVisual.activeSelf;

        // Si estaba activo y ahora se desactivó = TODOS CORTADOS
        if (wasActiveLastFrame && !isActiveNow)
        {
            OnAllIngredientsCut();
        }

        wasActiveLastFrame = isActiveNow;
    }

    void OnAllIngredientsCut()
    {
        Debug.Log("[SimpleSceneChanger] 🎉 ¡SNAPVISUAL SE DESACTIVÓ! = TODOS LOS INGREDIENTES CORTADOS");
        Debug.Log($"[SimpleSceneChanger] ⏱️ Cambiando a '{nextSceneName}' en {delaySeconds} segundos...");

        transitionStarted = true;
        StartCoroutine(ChangeSceneAfterDelay());
    }

    IEnumerator ChangeSceneAfterDelay()
    {
        // Esperar
        yield return new WaitForSeconds(delaySeconds);

        Debug.Log($"[SimpleSceneChanger] 🔄 Cargando escena: {nextSceneName}");

        // Cambiar escena
        try
        {
            SceneManager.LoadScene(nextSceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SimpleSceneChanger] ❌ ERROR al cargar '{nextSceneName}': {e.Message}");
            Debug.LogError("[SimpleSceneChanger] ⚠️ SOLUCIÓN: Añade la escena en File > Build Settings > Scenes in Build");
        }
    }

    // Botón de emergencia para testear
    void OnGUI()
    {
        if (showDebugInfo && !transitionStarted)
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 120));

            GUILayout.Box("=== DEBUG: Simple Scene Changer ===", GUILayout.Width(390));

            string status = snapVisual != null
                ? (snapVisual.activeSelf ? "🟢 ACTIVO (esperando cortes...)" : "🔴 INACTIVO (cortados!)")
                : "❌ NO ENCONTRADO";

            GUILayout.Label($"SnapVisual: {status}");
            GUILayout.Label($"Próxima escena: {nextSceneName}");

            if (GUILayout.Button("⚡ FORZAR CAMBIO (TEST)"))
            {
                Debug.Log("[SimpleSceneChanger] 🧪 CAMBIO FORZADO MANUALMENTE");
                OnAllIngredientsCut();
            }

            GUILayout.EndArea();
        }
    }
}
