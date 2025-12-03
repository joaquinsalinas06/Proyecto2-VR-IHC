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
    public float minKnifeVelocity = 0f; // 0 = no requiere velocidad, solo contacto

    [Header("Visual Feedback")]
    [Tooltip("Material que parpadea en cada contacto")]
    public Material flashMaterial;

    [Tooltip("Duración del flash visual")]
    [Range(0.05f, 0.5f)]
    public float flashDuration = 0.1f;

    [Tooltip("Color del flash")]
    public Color flashColor = Color.white;

    [Tooltip("Reducir tamaño del ají con cada golpe (feedback visual)")]
    public bool enableScaleEffect = true;

    [Tooltip("Factor de escala por golpe (1.0 = sin cambio, 0.9 = 10% más pequeño)")]
    [Range(0.8f, 1.0f)]
    public float scaleFactorPerHit = 0.92f;

    [Header("Haptic Feedback")]
    [Tooltip("Activar vibración háptica en cada golpe")]
    public bool enableHaptics = true;

    [Tooltip("Duración de la vibración en segundos")]
    [Range(0.05f, 0.3f)]
    public float hapticDuration = 0.1f;

    [Tooltip("Intensidad de la vibración (0-1)")]
    [Range(0f, 1f)]
    public float hapticIntensity = 0.5f;

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
    private Vector3 originalScale;
    private OVRInput.Controller lastHitController = OVRInput.Controller.None;

    // Evento cuando se completa el corte
    public System.Action OnCutCompleted;

    void Awake()
    {
        // CRÍTICO: Asegurar que hay un BoxCollider como Trigger
        BoxCollider triggerCol = GetComponent<BoxCollider>();
        if (triggerCol == null)
        {
            triggerCol = gameObject.AddComponent<BoxCollider>();
            Debug.Log($"[MULTI CONTACT CUT] BoxCollider añadido automáticamente a {gameObject.name}");
        }
        
        // Forzar que sea trigger
        if (!triggerCol.isTrigger)
        {
            triggerCol.isTrigger = true;
            Debug.Log($"[MULTI CONTACT CUT] Trigger habilitado en {gameObject.name}");
        }

        // Ajustar tamaño si es muy pequeño
        if (triggerCol.size.magnitude < 0.05f)
        {
            triggerCol.size = new Vector3(0.1f, 0.1f, 0.1f);
            Debug.Log($"[MULTI CONTACT CUT] Tamaño del trigger ajustado en {gameObject.name}");
        }

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

        // Guardar escala original
        originalScale = transform.localScale;

        Debug.Log($"[MULTI CONTACT CUT] ✅ Sistema ACTIVADO en {gameObject.name}. Esperando {requiredContacts} contactos.");
    }

    /// <summary>
    /// Desactivar el sistema
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive || isCompleted) 
        {
            if (!isActive) Debug.Log($"[MULTI CONTACT CUT] ❌ Sistema NO activo en {gameObject.name}");
            if (isCompleted) Debug.Log($"[MULTI CONTACT CUT] ❌ Ya completado en {gameObject.name}");
            return;
        }

        // Verificar si es el cuchillo
        if (!other.gameObject.CompareTag(knifeTag) && 
            !other.gameObject.name.Contains("Blade"))
        {
            Debug.Log($"[MULTI CONTACT CUT] ⚠️ Objeto '{other.gameObject.name}' no es un cuchillo. Ignorando.");
            return;
        }

        Debug.Log($"[MULTI CONTACT CUT] 🔪 Cuchillo detectado: {other.gameObject.name}");

        // Verificar tiempo mínimo entre contactos
        float timeSinceLastContact = Time.time - lastContactTime;
        if (timeSinceLastContact < minimumTimeBetweenContacts)
        {
            Debug.Log($"[MULTI CONTACT CUT] ⏱️ Contacto muy rápido ({timeSinceLastContact:F2}s < {minimumTimeBetweenContacts}s). Ignorando.");
            return;
        }

        // Verificar velocidad del cuchillo
        Rigidbody knifeRb = other.attachedRigidbody;
        if (knifeRb != null)
        {
            float velocity = knifeRb.velocity.magnitude;
            if (velocity < minKnifeVelocity)
            {
                Debug.Log($"[MULTI CONTACT CUT] 🐌 Velocidad baja: {velocity:F2} m/s < {minKnifeVelocity} m/s. Ignorando.");
                return;
            }
            Debug.Log($"[MULTI CONTACT CUT] ⚡ Velocidad OK: {velocity:F2} m/s");
        }

        // Detectar qué mano/controller está golpeando
        DetectController(other.transform);

        // ¡Contacto válido!
        RegisterContact();
    }

    void RegisterContact()
    {
        currentContacts++;
        lastContactTime = Time.time;

        Debug.Log($"[MULTI CONTACT CUT] ✅ Contacto VÁLIDO {currentContacts}/{requiredContacts} en {gameObject.name}");

        // Feedback visual: Flash
        if (renderers.Length > 0)
        {
            StartCoroutine(FlashEffect());
        }

        // Feedback visual: Reducir tamaño progresivamente
        if (enableScaleEffect)
        {
            Vector3 newScale = transform.localScale * scaleFactorPerHit;
            transform.localScale = newScale;
            Debug.Log($"[MULTI CONTACT CUT] 📏 Escala reducida a {newScale.magnitude / originalScale.magnitude * 100:F0}% del original");
        }

        // Feedback háptico: Vibración en el controller
        if (enableHaptics && lastHitController != OVRInput.Controller.None)
        {
            TriggerHapticFeedback(lastHitController);
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

        Debug.Log($"[MULTI CONTACT CUT] 🎉 ¡CORTE COMPLETADO! {currentContacts} contactos registrados en {gameObject.name}.");

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

    /// <summary>
    /// Detectar qué controller VR está golpeando el ají
    /// </summary>
    void DetectController(Transform hitTransform)
    {
        // Buscar hacia arriba en la jerarquía para encontrar el controller
        Transform current = hitTransform;
        while (current != null)
        {
            string name = current.name.ToLower();
            
            if (name.Contains("left") || name.Contains("l_hand") || name.Contains("lefthand"))
            {
                lastHitController = OVRInput.Controller.LTouch;
                return;
            }
            else if (name.Contains("right") || name.Contains("r_hand") || name.Contains("righthand"))
            {
                lastHitController = OVRInput.Controller.RTouch;
                return;
            }
            
            current = current.parent;
        }
        
        // Si no se encuentra, usar ambos
        lastHitController = OVRInput.Controller.Touch;
    }

    /// <summary>
    /// Activar vibración háptica en el controller especificado
    /// </summary>
    void TriggerHapticFeedback(OVRInput.Controller controller)
    {
        // Crear array de samples para el clip de vibración
        int sampleCount = Mathf.RoundToInt(hapticDuration * OVRHaptics.Config.SampleRateHz);
        byte[] samples = new byte[sampleCount];
        
        // Llenar con intensidad constante
        byte intensity = (byte)(hapticIntensity * 255);
        for (int i = 0; i < sampleCount; i++)
        {
            samples[i] = intensity;
        }
        
        // Crear clip con el constructor que acepta el array
        OVRHapticsClip clip = new OVRHapticsClip(samples, samples.Length);
        
        // Reproducir en el controller correcto
        if (controller == OVRInput.Controller.LTouch)
        {
            OVRHaptics.LeftChannel.Preempt(clip);
            Debug.Log($"[MULTI CONTACT CUT] 📳 Vibración en controller IZQUIERDO");
        }
        else if (controller == OVRInput.Controller.RTouch)
        {
            OVRHaptics.RightChannel.Preempt(clip);
            Debug.Log($"[MULTI CONTACT CUT] 📳 Vibración en controller DERECHO");
        }
        else
        {
            // Ambos controllers
            OVRHaptics.LeftChannel.Preempt(clip);
            OVRHaptics.RightChannel.Preempt(clip);
            Debug.Log($"[MULTI CONTACT CUT] 📳 Vibración en AMBOS controllers");
        }
    }
}
