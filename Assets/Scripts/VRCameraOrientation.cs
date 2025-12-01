using UnityEngine;

/// <summary>
/// Fija la orientación inicial de la cámara VR para que el jugador mire hacia adelante
/// Adjunta este script al GameObject del Camera Rig
/// </summary>
public class VRCameraOrientation : MonoBehaviour
{
    [Header("Orientación Inicial")]
    [Tooltip("Rotación inicial en el eje Y (0 = Norte, 90 = Este, 180 = Sur, 270 = Oeste)")]
    [Range(0f, 360f)]
    public float initialYRotation = 0f;

    [Tooltip("Aplicar rotación automáticamente al iniciar")]
    public bool applyOnStart = true;

    [Header("Target de Mirada (Opcional)")]
    [Tooltip("GameObject hacia el que quieres mirar (ej: mesa de cocina)")]
    public GameObject lookAtTarget;

    [Tooltip("Usar target en lugar de rotación manual")]
    public bool useLookAtTarget = false;

    [Header("Debug")]
    public bool showDebugInfo = true;

    void Start()
    {
        if (applyOnStart)
        {
            ApplyOrientation();
        }
    }

    /// <summary>
    /// Aplica la orientación inicial configurada
    /// </summary>
    public void ApplyOrientation()
    {
        Vector3 currentPosition = transform.position;
        Quaternion targetRotation;

        if (useLookAtTarget && lookAtTarget != null)
        {
            // Mirar hacia el target especificado
            Vector3 direction = lookAtTarget.transform.position - transform.position;
            direction.y = 0; // Solo rotar en Y (mantener horizontal)

            if (direction != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(direction);

                if (showDebugInfo)
                {
                    Debug.Log($"[VR ORIENTATION] Mirando hacia: {lookAtTarget.name}");
                    Debug.Log($"[VR ORIENTATION] Dirección: {direction}");
                }
            }
            else
            {
                targetRotation = Quaternion.Euler(0f, initialYRotation, 0f);
            }
        }
        else
        {
            // Usar rotación manual
            targetRotation = Quaternion.Euler(0f, initialYRotation, 0f);
        }

        transform.position = currentPosition;
        transform.rotation = targetRotation;

        if (showDebugInfo)
        {
            Debug.Log($"[VR ORIENTATION] Orientación aplicada: Y = {targetRotation.eulerAngles.y}°");
            Debug.Log($"[VR ORIENTATION] Posición: {currentPosition}");
        }
    }

    /// <summary>
    /// Resetear a orientación por defecto (mirando hacia adelante)
    /// </summary>
    public void ResetToForward()
    {
        initialYRotation = 0f;
        ApplyOrientation();
    }

    /// <summary>
    /// Rotar 180 grados (útil si el setup está invertido)
    /// </summary>
    public void FlipOrientation()
    {
        initialYRotation = (initialYRotation + 180f) % 360f;
        ApplyOrientation();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Actualizar en tiempo real en el Editor (solo cuando no está jugando)
        if (!Application.isPlaying && applyOnStart)
        {
            ApplyOrientation();
        }
    }
#endif
}
