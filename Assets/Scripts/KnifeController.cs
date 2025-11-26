using UnityEngine;

/// <summary>
/// Controlador del cuchillo - Detecta cuando corta ingredientes en la tabla
/// </summary>
public class KnifeController : MonoBehaviour
{
    [Header("Referencias")]
    public CuttingBoardSnapZone cuttingBoard;  // Referencia a la tabla de cortar

    [Header("Configuración de Corte")]
    [Tooltip("Tags de objetos que pueden ser cortados")]
    public string[] cuttableTags = { "Pescado", "Limon", "Cebolla", "Aji" };

    [Tooltip("Velocidad mínima para que cuente como corte (m/s)")]
    public float minCutVelocity = 0.2f;

    [Tooltip("Cooldown entre cortes del mismo objeto (segundos)")]
    public float cutCooldown = 1.0f;

    [Header("Efectos Visuales")]
    public ParticleSystem cutParticles;        // Partículas al cortar
    public GameObject cutEffectPrefab;         // Prefab de efecto de corte

    [Header("Audio")]
    public AudioClip cutSound;                 // Sonido al cortar
    public AudioClip[] cutSounds;              // Array de sonidos de corte (aleatorios)

    [Header("Haptic Feedback")]
    public bool useHaptics = true;
    public float hapticIntensity = 0.7f;
    public float hapticDuration = 0.1f;

    private AudioSource audioSource;
    private Rigidbody knifeRigidbody;
    private GameObject lastCutObject;
    private float lastCutTime;

    // Manual velocity tracking (para cuando el Rigidbody es kinematic)
    private Vector3 lastPosition;
    private float lastPositionTime;
    private Vector3 previousFramePosition;

    [Header("Detección de Líneas de Corte")]
    [Tooltip("Distancia máxima del raycast para detectar líneas")]
    public float raycastMaxDistance = 0.5f;

    [Tooltip("Layer de las líneas de corte")]
    public LayerMask cuttingLineLayer = -1;

    void Start()
    {
        // Configurar AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Obtener Rigidbody para medir velocidad
        knifeRigidbody = GetComponent<Rigidbody>();

        // Buscar la tabla de cortar si no está asignada
        if (cuttingBoard == null)
        {
            cuttingBoard = FindObjectOfType<CuttingBoardSnapZone>();
            if (cuttingBoard == null)
            {
                Debug.LogWarning("[KNIFE] No se encontró CuttingBoardSnapZone en la escena!");
            }
        }

        // Inicializar tracking de posición
        lastPosition = transform.position;
        previousFramePosition = transform.position;
        lastPositionTime = Time.time;

        Debug.Log("[KNIFE] Cuchillo inicializado. Velocidad mínima de corte: " + minCutVelocity + " m/s");
    }

    void Update()
    {
        // Raycast sweep para detectar líneas de corte
        PerformRaycastSweep();

        // Actualizar posición cada frame para calcular velocidad manual
        previousFramePosition = lastPosition;
        lastPosition = transform.position;
        lastPositionTime = Time.time;
    }

    /// <summary>
    /// Realiza un raycast desde la posición anterior hasta la actual
    /// para detectar líneas de corte que el cuchillo atraviesa
    /// </summary>
    void PerformRaycastSweep()
    {
        Vector3 currentPos = transform.position;
        Vector3 direction = currentPos - previousFramePosition;
        float distance = direction.magnitude;

        // Solo hacer raycast si nos movimos una distancia significativa
        if (distance < 0.001f)
            return;

        // Normalizar dirección
        direction.Normalize();

        // Limitar distancia máxima del raycast
        distance = Mathf.Min(distance, raycastMaxDistance);

        // Realizar raycast
        RaycastHit[] hits = Physics.RaycastAll(previousFramePosition, direction, distance, cuttingLineLayer);

        // Procesar cada línea de corte detectada
        foreach (RaycastHit hit in hits)
        {
            CuttingLine line = hit.collider.GetComponent<CuttingLine>();
            if (line != null)
            {
                float velocity = CalculateVelocity();
                line.RegisterCut(velocity, Time.deltaTime);
            }
        }

        // Debug visual (opcional)
        if (hits.Length > 0)
        {
            Debug.DrawLine(previousFramePosition, currentPos, Color.green, 0.1f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Verificar si es un objeto cortable
        if (!IsCuttable(other.gameObject))
            return;

        // IMPORTANTE: Si el ingrediente tiene un sistema de corte progresivo, NO usar corte instantáneo
        IngredientCuttingPattern pattern = other.GetComponent<IngredientCuttingPattern>();
        if (pattern != null)
        {
            // Este ingrediente usa el sistema progresivo con líneas
            // El corte se maneja a través de las líneas individuales (RegisterCut en CuttingLine)
            Debug.Log($"[KNIFE] {other.gameObject.name} tiene sistema progresivo - usando líneas de corte en vez de corte instantáneo");
            return;
        }

        // Verificar cooldown (evitar cortes múltiples muy rápidos)
        if (lastCutObject == other.gameObject && Time.time - lastCutTime < cutCooldown)
        {
            Debug.Log($"[KNIFE] Cooldown activo para {other.gameObject.name}");
            return;
        }

        // Calcular velocidad del cuchillo
        float currentVelocity = CalculateVelocity();

        if (currentVelocity < minCutVelocity)
        {
            // Solo mostrar cada 30 frames para no saturar los logs
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"[KNIFE] Velocidad muy baja: {currentVelocity:F2} m/s (mínimo: {minCutVelocity} m/s)");
            }
            return;
        }

        // ¡Realizar el corte! (solo para ingredientes SIN sistema progresivo)
        Debug.Log($"[KNIFE] ¡Cortando {other.gameObject.name}! Velocidad: {currentVelocity:F2} m/s");
        PerformCut(other.gameObject, other.ClosestPoint(transform.position));
    }

    bool IsCuttable(GameObject obj)
    {
        foreach (string tag in cuttableTags)
        {
            if (obj.CompareTag(tag))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Calcula la velocidad del cuchillo usando seguimiento de posición
    /// Esto funciona incluso cuando el Rigidbody es kinematic (como cuando está siendo agarrado)
    /// </summary>
    public float CalculateVelocity()
    {
        // Calcular velocidad manualmente desde el cambio de posición
        float deltaTime = Time.time - lastPositionTime;

        if (deltaTime > 0)
        {
            Vector3 deltaPosition = transform.position - lastPosition;
            float manualVelocity = deltaPosition.magnitude / deltaTime;

            // Si hay Rigidbody y no es kinematic, usar su velocidad
            // Si es kinematic o no hay Rigidbody, usar la velocidad manual
            if (knifeRigidbody != null && !knifeRigidbody.isKinematic)
            {
                float rbVelocity = knifeRigidbody.velocity.magnitude;
                // Usar el mayor de los dos (más confiable)
                return Mathf.Max(rbVelocity, manualVelocity);
            }
            else
            {
                // Usar velocidad manual
                return manualVelocity;
            }
        }

        return 0f;
    }

    void PerformCut(GameObject ingredient, Vector3 cutPoint)
    {
        // Notificar a la tabla de cortar
        if (cuttingBoard != null)
        {
            cuttingBoard.OnIngredientCut(ingredient);
        }
        else
        {
            Debug.LogWarning("[KNIFE] No hay referencia a CuttingBoardSnapZone!");
        }

        // Efectos visuales
        PlayCutEffects(cutPoint);

        // Efectos de audio
        PlayCutSound();

        // Feedback háptico
        if (useHaptics)
        {
            TriggerHaptics();
        }

        // Actualizar cooldown
        lastCutObject = ingredient;
        lastCutTime = Time.time;

        Debug.Log($"[KNIFE] Corte completado en {ingredient.name}");
    }

    void PlayCutEffects(Vector3 position)
    {
        // Partículas
        if (cutParticles != null)
        {
            cutParticles.transform.position = position;
            cutParticles.Play();
        }

        // Prefab de efecto
        if (cutEffectPrefab != null)
        {
            GameObject effect = Instantiate(cutEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 2f); // Destruir después de 2 segundos
        }
    }

    void PlayCutSound()
    {
        if (audioSource == null)
            return;

        // Si hay array de sonidos, elegir uno aleatorio
        if (cutSounds != null && cutSounds.Length > 0)
        {
            AudioClip randomClip = cutSounds[Random.Range(0, cutSounds.Length)];
            if (randomClip != null)
            {
                audioSource.PlayOneShot(randomClip, 0.8f);
                return;
            }
        }

        // Si no, usar el sonido único
        if (cutSound != null)
        {
            audioSource.PlayOneShot(cutSound, 0.8f);
        }
    }

    void TriggerHaptics()
    {
        // Vibración en ambos controles (o el que está agarrando el cuchillo)
        OVRInput.SetControllerVibration(hapticIntensity, hapticIntensity, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(hapticIntensity, hapticIntensity, OVRInput.Controller.LTouch);

        // Detener vibración después del tiempo especificado
        Invoke("StopHaptics", hapticDuration);
    }

    void StopHaptics()
    {
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
    }

    // Métodos públicos para configuración externa
    public void SetCuttingBoard(CuttingBoardSnapZone board)
    {
        cuttingBoard = board;
    }

    public void SetMinCutVelocity(float velocity)
    {
        minCutVelocity = velocity;
    }

    // Debug: Mostrar velocidad actual en Scene View
    void OnDrawGizmos()
    {
        if (knifeRigidbody != null && Application.isPlaying)
        {
            float velocity = knifeRigidbody.velocity.magnitude;

            // Color según velocidad
            if (velocity >= minCutVelocity)
                Gizmos.color = Color.green; // Suficiente velocidad para cortar
            else
                Gizmos.color = Color.red;   // Muy lento

            // Línea que representa la velocidad
            Gizmos.DrawLine(transform.position, transform.position + knifeRigidbody.velocity);
        }
    }
}
