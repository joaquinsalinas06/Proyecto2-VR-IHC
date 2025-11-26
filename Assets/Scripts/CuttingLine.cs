using System.Collections;
using UnityEngine;

/// <summary>
/// Representa una línea de corte individual que el usuario debe seguir con el cuchillo.
/// Maneja el progreso visual, detección de corte y feedback.
/// </summary>
public class CuttingLine : MonoBehaviour
{
    [Header("Configuración de Progreso")]
    [Tooltip("Tiempo requerido para completar esta línea (segundos)")]
    public float requiredCutTime = 1.5f;

    [Tooltip("Progreso actual de 0 a 1")]
    [Range(0f, 1f)]
    public float cutProgress = 0f;

    [Tooltip("Umbral de progreso para considerar la línea completa (0.8 = 80%)")]
    [Range(0.5f, 1f)]
    public float completionThreshold = 0.8f;

    [Header("Detección de Corte")]
    [Tooltip("Velocidad mínima del cuchillo para registrar corte (m/s)")]
    public float minCutVelocity = 0.3f;

    [Tooltip("Tag del cuchillo para detección")]
    public string knifeTag = "Knife";

    [Header("Visual")]
    [Tooltip("LineRenderer para dibujar la línea")]
    public LineRenderer lineRenderer;

    [Tooltip("Color cuando la línea está vacía")]
    public Color emptyColor = new Color(1f, 0.3f, 0.1f, 1f); // Naranja rojizo

    [Tooltip("Color cuando la línea está llenándose")]
    public Color fillingColor = new Color(1f, 0.9f, 0.2f, 1f); // Amarillo

    [Tooltip("Color cuando la línea está completa")]
    public Color completeColor = new Color(0.2f, 1f, 0.3f, 1f); // Verde brillante

    [Tooltip("Ancho base de la línea")]
    public float baseWidth = 0.1f; // ULTRA GRUESO - 100mm para máxima visibilidad en VR

    [Tooltip("Multiplicador de ancho cuando está completa")]
    public float completeWidthMultiplier = 1.5f;

    [Header("Audio")]
    public AudioClip cuttingSound;
    public AudioClip completeSound;
    private AudioSource audioSource;

    [Header("Efectos")]
    public GameObject completeParticles;

    // Estado
    private bool isComplete = false;
    public bool isActive { get; private set; } = false; // Público para lectura, privado para escritura
    private bool isCutting = false;
    private Material lineMaterial;
    private float pulseTimer = 0f;

    // Callback cuando se completa
    public System.Action<CuttingLine> OnLineCompleted;

    void Awake()
    {
        // LOGS REDUCIDOS - solo errores críticos
        // Debug.Log($"[CUTTING LINE AWAKE] {gameObject.name} - Awake llamado");

        // Crear AudioSource si no existe
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.loop = true;
            audioSource.volume = 0.3f;
        }

        // Configurar LineRenderer
        if (lineRenderer != null)
        {
            // CRÍTICO: Configurar width ANTES de cualquier otra cosa
            lineRenderer.widthMultiplier = 1f; // Multiplicador global = 1
            lineRenderer.widthCurve = AnimationCurve.Constant(0, 1, 1); // Curva constante en 1.0
            lineRenderer.startWidth = baseWidth;
            lineRenderer.endWidth = baseWidth;

            // NUEVO: Usar shader Hidden/Internal-Colored que SIEMPRE existe en Unity
            lineRenderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));
            lineMaterial = lineRenderer.material;
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt("_ZWrite", 0);

            // CRÍTICO: Forzar colores directamente en el LineRenderer (no depender solo del material)
            lineRenderer.startColor = emptyColor;
            lineRenderer.endColor = emptyColor;

            Debug.Log($"[CUTTING LINE] Shader: Unlit/Color, Color: {emptyColor}, Width: {baseWidth}, WidthCurve: constante");
            UpdateVisual();
        }
        else
        {
            Debug.LogError($"[CUTTING LINE ERROR] ¡{gameObject.name} NO TIENE LineRenderer asignado!");
        }
    }

    void Start()
    {
        Debug.Log($"[CUTTING LINE START] {gameObject.name} - Start llamado, desactivando línea inicialmente");
        // Las líneas empiezan inactivas (invisibles)
        SetActive(false);
    }

    void Update()
    {
        if (!isActive || isComplete) return;

        // Animación de pulso mientras se corta
        if (isCutting)
        {
            pulseTimer += Time.deltaTime * 10f;
            float pulse = 1f + Mathf.Sin(pulseTimer) * 0.2f;
            lineRenderer.startWidth = baseWidth * pulse;
            lineRenderer.endWidth = baseWidth * pulse;
        }
        else
        {
            // Volver al tamaño normal
            lineRenderer.startWidth = baseWidth;
            lineRenderer.endWidth = baseWidth;
        }

        isCutting = false; // Se resetea cada frame, se activa en RegisterCut
    }

    /// <summary>
    /// Activa o desactiva esta línea de corte
    /// </summary>
    public void SetActive(bool active)
    {
        Debug.Log($"[CUTTING LINE SET ACTIVE] {gameObject.name} - SetActive({active}) llamado");

        isActive = active;

        if (lineRenderer != null)
        {
            // CRÍTICO: Forzar configuración cada vez que se activa para combatir valores cacheados del prefab
            if (active)
            {
                lineRenderer.widthMultiplier = 1f;
                lineRenderer.widthCurve = AnimationCurve.Constant(0, 1, 1);
                lineRenderer.startWidth = baseWidth;
                lineRenderer.endWidth = baseWidth;
                lineRenderer.startColor = emptyColor;
                lineRenderer.endColor = emptyColor;
            }

            lineRenderer.enabled = active;
            Debug.Log($"[CUTTING LINE SET ACTIVE]   LineRenderer.enabled = {active}");
            Debug.Log($"[CUTTING LINE SET ACTIVE]   Width: start={lineRenderer.startWidth}, end={lineRenderer.endWidth}");
            Debug.Log($"[CUTTING LINE SET ACTIVE]   Color: start={lineRenderer.startColor}");
            Debug.Log($"[CUTTING LINE SET ACTIVE]   LineRenderer tiene {lineRenderer.positionCount} posiciones");
            if (lineRenderer.positionCount >= 2)
            {
                Debug.Log($"[CUTTING LINE SET ACTIVE]   Posición 0: {lineRenderer.GetPosition(0)}");
                Debug.Log($"[CUTTING LINE SET ACTIVE]   Posición 1: {lineRenderer.GetPosition(1)}");
            }
        }
        else
        {
            Debug.LogError($"[CUTTING LINE SET ACTIVE ERROR]   ¡LineRenderer es NULL!");
        }

        if (active && !isComplete)
        {
            Debug.Log($"[CUTTING LINE SET ACTIVE]   ✓ Línea activada y visible: {gameObject.name}");
            PlayUnlockAnimation();
        }
        else if (!active)
        {
            Debug.Log($"[CUTTING LINE SET ACTIVE]   Línea desactivada/invisible: {gameObject.name}");
        }
    }

    /// <summary>
    /// Registra un corte en esta línea (llamado por KnifeController o trigger)
    /// </summary>
    public void RegisterCut(float knifeVelocity, float deltaTime)
    {
        if (!isActive || isComplete) return;

        // Solo registrar si la velocidad es suficiente
        if (knifeVelocity < minCutVelocity) return;

        isCutting = true;

        // Incrementar progreso basado en tiempo y velocidad
        float progressIncrement = (deltaTime / requiredCutTime) * (knifeVelocity / minCutVelocity);
        cutProgress += progressIncrement;
        cutProgress = Mathf.Clamp01(cutProgress);

        UpdateVisual();

        // Reproducir sonido de corte
        if (cuttingSound != null && !audioSource.isPlaying)
        {
            audioSource.clip = cuttingSound;
            audioSource.Play();
        }

        // Verificar si se completó
        if (cutProgress >= completionThreshold && !isComplete)
        {
            CompleteCut();
        }

        Debug.Log($"[CUT LINE] {gameObject.name} progreso: {cutProgress:F2} (velocidad: {knifeVelocity:F2} m/s)");
    }

    /// <summary>
    /// Actualiza el color y apariencia de la línea según el progreso
    /// </summary>
    void UpdateVisual()
    {
        if (lineRenderer == null) return;

        Color currentColor;
        if (cutProgress < 0.1f)
        {
            currentColor = emptyColor;
        }
        else if (cutProgress < completionThreshold)
        {
            // Lerp de empty a filling
            float t = (cutProgress - 0.1f) / (completionThreshold - 0.1f);
            currentColor = Color.Lerp(emptyColor, fillingColor, t);
        }
        else
        {
            currentColor = completeColor;
        }

        // CRÍTICO: Aplicar color directamente al LineRenderer para VR
        lineRenderer.startColor = currentColor;
        lineRenderer.endColor = currentColor;

        // También aplicar al material (Unlit/Color solo usa _Color, no tiene _Emission)
        if (lineMaterial != null)
        {
            lineMaterial.SetColor("_Color", currentColor);
        }
    }

    /// <summary>
    /// Completa el corte de esta línea
    /// </summary>
    void CompleteCut()
    {
        isComplete = true;
        cutProgress = 1f;
        UpdateVisual();

        // Detener sonido de corte
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Reproducir sonido de completado
        if (completeSound != null)
        {
            audioSource.PlayOneShot(completeSound, 0.7f);
        }

        // Hacer la línea más gruesa
        lineRenderer.startWidth = baseWidth * completeWidthMultiplier;
        lineRenderer.endWidth = baseWidth * completeWidthMultiplier;

        // Efecto de partículas
        if (completeParticles != null)
        {
            GameObject particles = Instantiate(completeParticles, transform.position, Quaternion.identity);
            Destroy(particles, 2f);
        }

        // Vibración háptica (si está disponible)
        // OVRInput.SetControllerVibration(0.5f, 0.3f, OVRInput.Controller.RTouch);

        Debug.Log($"[CUT LINE] ¡Línea completada! {gameObject.name}");

        // Notificar al patrón padre
        OnLineCompleted?.Invoke(this);

        // Fade out después de un momento
        StartCoroutine(FadeOutAfterDelay(0.5f));
    }

    /// <summary>
    /// Animación de desbloqueo cuando la línea se activa
    /// </summary>
    void PlayUnlockAnimation()
    {
        StartCoroutine(UnlockAnimationCoroutine());
    }

    IEnumerator UnlockAnimationCoroutine()
    {
        // Escala de 0.5 a 1.0
        float duration = 0.3f;
        float elapsed = 0f;

        Vector3 originalScale = transform.localScale;
        transform.localScale = originalScale * 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(originalScale * 0.5f, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;
    }

    /// <summary>
    /// Fade out de la línea después de completarse
    /// </summary>
    IEnumerator FadeOutAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        float duration = 0.5f;
        float elapsed = 0f;
        Color startColor = lineRenderer.startColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / duration);
            Color fadeColor = new Color(startColor.r, startColor.g, startColor.b, alpha * 0.5f);
            lineRenderer.startColor = fadeColor;
            lineRenderer.endColor = fadeColor;
            yield return null;
        }
    }

    /// <summary>
    /// Resetea esta línea a su estado inicial
    /// </summary>
    public void ResetLine()
    {
        cutProgress = 0f;
        isComplete = false;
        isCutting = false;
        pulseTimer = 0f;
        UpdateVisual();
        SetActive(false);
    }

    void OnTriggerStay(Collider other)
    {
        // Fallback: detección por trigger si el raycast falla
        if (!isActive || isComplete) return;

        if (other.CompareTag(knifeTag))
        {
            KnifeController knife = other.GetComponent<KnifeController>();
            if (knife != null)
            {
                float velocity = knife.CalculateVelocity();
                RegisterCut(velocity, Time.deltaTime);
            }
        }
    }
}
