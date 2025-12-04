using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script helper que agrega automáticamente Box Colliders a botones UI
/// para que puedan ser detectados por el HandRayInteractor.
///
/// USO:
/// 1. Adjunta este script al Canvas que contiene los botones
/// 2. Presiona Play - automáticamente agregará colliders a todos los botones
/// </summary>
public class UIButtonColliderSetup : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Profundidad del collider (Z). Debe ser > 0 para detectar el ray")]
    public float colliderDepth = 10f;

    [Tooltip("Ejecutar automáticamente al iniciar")]
    public bool autoSetupOnStart = true;

    [Tooltip("Mostrar mensajes de debug")]
    public bool showDebug = true;

    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupAllButtons();
        }
    }

    /// <summary>
    /// Busca todos los botones en este Canvas y les agrega Box Colliders
    /// </summary>
    [ContextMenu("Setup All Buttons")]
    public void SetupAllButtons()
    {
        // Buscar todos los botones en este Canvas
        Button[] buttons = GetComponentsInChildren<Button>(true);

        if (buttons.Length == 0)
        {
            Debug.LogWarning($"[BUTTON SETUP] No se encontraron botones en {gameObject.name}");
            return;
        }

        int setupCount = 0;
        int skippedCount = 0;

        foreach (Button button in buttons)
        {
            bool wasSetup = SetupButton(button);
            if (wasSetup)
                setupCount++;
            else
                skippedCount++;
        }

        Debug.Log($"[BUTTON SETUP] ✅ Completado en {gameObject.name}:");
        Debug.Log($"[BUTTON SETUP]   - {setupCount} botones configurados");
        Debug.Log($"[BUTTON SETUP]   - {skippedCount} botones ya tenían collider");

        // Verificar OnClick events
        Debug.Log($"[BUTTON SETUP] 🔍 Verificando OnClick events...");
        VerifyOnClickEvents();
    }

    /// <summary>
    /// Configura un botón individual
    /// </summary>
    bool SetupButton(Button button)
    {
        GameObject buttonObj = button.gameObject;

        // Verificar si ya tiene Box Collider
        BoxCollider existingCollider = buttonObj.GetComponent<BoxCollider>();
        if (existingCollider != null)
        {
            // Ya tiene collider, solo verificar configuración
            if (existingCollider.size.z < 1f)
            {
                existingCollider.size = new Vector3(existingCollider.size.x, existingCollider.size.y, colliderDepth);
                if (showDebug)
                    Debug.Log($"[BUTTON SETUP] 🔧 Actualizado Z en '{buttonObj.name}': Z = {colliderDepth}");
            }

            if (!existingCollider.isTrigger)
            {
                existingCollider.isTrigger = true;
                if (showDebug)
                    Debug.Log($"[BUTTON SETUP] 🔧 Activado Is Trigger en '{buttonObj.name}'");
            }

            return false; // No fue nuevo
        }

        // Obtener RectTransform para el tamaño
        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError($"[BUTTON SETUP] ❌ '{buttonObj.name}' no tiene RectTransform!");
            return false;
        }

        // Crear nuevo Box Collider
        BoxCollider collider = buttonObj.AddComponent<BoxCollider>();
        collider.isTrigger = true;

        // Configurar tamaño basado en el RectTransform
        Vector2 rectSize = rectTransform.rect.size;
        collider.size = new Vector3(rectSize.x, rectSize.y, colliderDepth);
        collider.center = Vector3.zero;

        if (showDebug)
        {
            Debug.Log($"[BUTTON SETUP] ✅ Box Collider agregado a '{buttonObj.name}':");
            Debug.Log($"[BUTTON SETUP]   - Size: ({rectSize.x:F0}, {rectSize.y:F0}, {colliderDepth})");
            Debug.Log($"[BUTTON SETUP]   - Is Trigger: {collider.isTrigger}");
        }

        return true;
    }

    /// <summary>
    /// Verifica la configuración actual de los botones
    /// </summary>
    [ContextMenu("Verify Button Setup")]
    public void VerifyButtonSetup()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        Debug.Log($"[BUTTON SETUP] 🔍 Verificando {buttons.Length} botones en {gameObject.name}:");

        foreach (Button button in buttons)
        {
            BoxCollider collider = button.GetComponent<BoxCollider>();

            if (collider == null)
            {
                Debug.LogWarning($"[BUTTON SETUP] ❌ '{button.name}' NO tiene Box Collider");
            }
            else
            {
                bool isOk = collider.isTrigger && collider.size.z > 0;
                string status = isOk ? "✅" : "⚠️";
                Debug.Log($"[BUTTON SETUP] {status} '{button.name}':");
                Debug.Log($"[BUTTON SETUP]   - Size: {collider.size}");
                Debug.Log($"[BUTTON SETUP]   - Is Trigger: {collider.isTrigger}");
                Debug.Log($"[BUTTON SETUP]   - Interactable: {button.interactable}");
                Debug.Log($"[BUTTON SETUP]   - OnClick listeners: {button.onClick.GetPersistentEventCount()}");
            }
        }
    }

    /// <summary>
    /// Elimina todos los Box Colliders de los botones
    /// </summary>
    [ContextMenu("Remove All Colliders")]
    public void RemoveAllColliders()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        int removedCount = 0;

        foreach (Button button in buttons)
        {
            BoxCollider collider = button.GetComponent<BoxCollider>();
            if (collider != null)
            {
                DestroyImmediate(collider);
                removedCount++;
            }
        }

        Debug.Log($"[BUTTON SETUP] 🗑️ Eliminados {removedCount} Box Colliders");
    }

    /// <summary>
    /// Verifica los OnClick events de todos los botones
    /// </summary>
    void VerifyOnClickEvents()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            int listenerCount = button.onClick.GetPersistentEventCount();

            if (listenerCount == 0)
            {
                Debug.LogWarning($"[BUTTON SETUP] ⚠️ '{button.name}' NO tiene OnClick events conectados!");
                Debug.LogWarning($"[BUTTON SETUP]    → Necesitas conectar manualmente en Inspector");
            }
            else
            {
                Debug.Log($"[BUTTON SETUP] ✓ '{button.name}' tiene {listenerCount} OnClick event(s)");

                // Mostrar detalles de los eventos
                for (int i = 0; i < listenerCount; i++)
                {
                    Object target = button.onClick.GetPersistentTarget(i);
                    string methodName = button.onClick.GetPersistentMethodName(i);

                    if (target != null && !string.IsNullOrEmpty(methodName))
                    {
                        Debug.Log($"[BUTTON SETUP]    → {i + 1}. {target.name}.{methodName}()");
                    }
                    else
                    {
                        Debug.LogWarning($"[BUTTON SETUP]    → {i + 1}. ⚠️ Evento inválido o vacío");
                    }
                }
            }
        }
    }
}
