using UnityEngine;

/// <summary>
/// Sistema de corte por múltiples contactos con el cuchillo.
/// Ideal para ingredientes pequeños como ají que solo necesitan varios golpes.
/// </summary>
public class MultiContactCut : MonoBehaviour
{
    [Header("Configuración de Corte")]
    [Tooltip("Número de contactos necesarios con el cuchillo para completar el corte")]
    [Range(1, 20)]
    public int requiredContacts = 5;

    [Tooltip("Tiempo mínimo entre contactos para que cuenten (evita spam)")]
    [Range(0.1f, 2f)]
    public float minimumTimeBetweenContacts = 0.3f;

    [Tooltip("Velocidad mínima del cuchillo para que cuente como corte")]
    [Range(0f, 2f)]
    public float minKnifeVelocity = 0.2f;

    [Header("Visual Feedback")]
    [Tooltip("Material que parpadea en cada contacto")]
    public Material flashMaterial;

    [Tooltip("Duración del flash visual")]
    [Range(0.05f, 0.5f)]
    public float flashDuration = 0.1f;

    [Tooltip("Color del flash")]
    public Color flashColor = Color.white;

    [Header("Audio")]
    public AudioClip cutSound; // Sonido en cada contacto
    public AudioClip completeSound; // Sonido al completar
    [Range(0f, 1f)]
    public float soundVolume = 0.7f;

    [Header("Detección de Cuchillo")]
    [Tooltip("Tag del cuchillo")]
    public string knifeTag = "Knife";

    [Header("Debug")]
    public bool showDebugInfo = false;

    // --- Estado interno ---
    private bool isActive = false;
    private bool isCompleted = false;
    private int currentContacts = 0;
    private float lastContactTime = 0f;
    private AudioSource audioSource;
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private bool isFlashing = false;

    // Evento cuando se completa el corte
    public System.Action OnCutCompleted;

    void Awake()
    {
        // Configurar AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f; // Sonido 3D
        }

        // Obtener todos los renderers para el efecto de flash
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            // Guardar materiales originales
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;
            }
        }
    }

    /// <summary>
    /// Activar el sistema de corte
    /// </summary>
    public void Activate()
    {
        if (isActive || isCompleted) return;

        isActive = true;
        currentContacts = 0;
        lastContactTime = 0f;

        if (showDebugInfo)
            Debug.Log($"[MULTI CONTACT CUT] Activado en {gameObject.name}. Esperando {requiredContacts} contactos.");
    }

    /// <summary>
    /// Desactivar el sistema
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isActive || isCompleted) return;

        // Verificar si es el cuchillo
        if (!collision.gameObject.CompareTag(knifeTag) && 
            !collision.gameObject.name.Contains("Blade"))
        {
            return;
        }

        // Verificar tiempo mínimo entre contactos
        if (Time.time - lastContactTime < minimumTimeBetweenContacts)
        {
            if (showDebugInfo)
                Debug.Log("[MULTI CONTACT CUT] Contacto muy rápido, ignorando.");
            return;
        }

        // Verificar velocidad del cuchillo
        Rigidbody knifeRb = collision.rigidbody;
        if (knifeRb != null)
        {
            float velocity = knifeRb.velocity.magnitude;
            if (velocity < minKnifeVelocity)
            {
                if (showDebugInfo)
                    Debug.Log($"[MULTI CONTACT CUT] Velocidad muy baja: {velocity:F2} < {minKnifeVelocity}");
                return;
            }
        }

        // ¡Contacto válido!
        RegisterContact();
    }

    void RegisterContact()
    {
        currentContacts++;
        lastContactTime = Time.time;

        if (showDebugInfo)
            Debug.Log($"[MULTI CONTACT CUT] Contacto {currentContacts}/{requiredContacts}");

        // Feedback visual
        if (renderers.Length > 0)
        {
            StartCoroutine(FlashEffect());
        }

        // Feedback de audio
        if (cutSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(cutSound, soundVolume);
        }

        // Verificar si se completó el corte
        if (currentContacts >= requiredContacts)
        {
            CompleteCut();
        }
    }

    void CompleteCut()
    {
        if (isCompleted) return;

        isCompleted = true;
        isActive = false;

        if (showDebugInfo)
            Debug.Log($"[MULTI CONTACT CUT] ¡Corte completado! {currentContacts} contactos registrados.");

        // Reproducir sonido de completado
        if (completeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(completeSound, soundVolume);
        }

        // Notificar al sistema
        OnCutCompleted?.Invoke();
    }

    System.Collections.IEnumerator FlashEffect()
    {
        if (isFlashing) yield break;
        isFlashing = true;

        // Cambiar a material de flash
        Material flashMat = flashMaterial != null ? flashMaterial : CreateFlashMaterial();
        
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.material = flashMat;
            }
        }

        // Esperar
        yield return new WaitForSeconds(flashDuration);

        // Restaurar materiales originales
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && i < originalMaterials.Length)
            {
                renderers[i].material = originalMaterials[i];
            }
        }

        isFlashing = false;
    }

    Material CreateFlashMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = flashColor;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", flashColor * 2f);
        return mat;
    }

    public bool IsComplete() => isCompleted;
    public float GetProgress() => Mathf.Clamp01((float)currentContacts / requiredContacts);
    public int GetCurrentContacts() => currentContacts;

    void OnDestroy()
    {
        // Limpiar materiales
        if (flashMaterial != null && flashMaterial != originalMaterials[0])
        {
            Destroy(flashMaterial);
        }
    }

    void OnDrawGizmos()
    {
        // Mostrar el estado visualmente en el editor
        if (!Application.isPlaying) return;

        Gizmos.color = isCompleted ? Color.green : (isActive ? Color.yellow : Color.gray);
        Gizmos.DrawWireSphere(transform.position, 0.1f);

        if (isActive && !isCompleted)
        {
            // Mostrar progreso
            float progress = GetProgress();
            Gizmos.color = Color.Lerp(Color.red, Color.green, progress);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.15f, 0.05f * progress);
        }
    }
}
