using UnityEngine;

/// <summary>
/// Detecta cuando un objeto se sale del área de juego y lo regresa a su posición inicial
/// </summary>
public class ObjectBoundsReset : MonoBehaviour
{
    [Header("Límites del Área de Juego")]
    [Tooltip("Altura mínima (Y) antes de resetear. Si el objeto cae más abajo, se resetea")]
    public float minHeight = 0.5f;

    [Tooltip("Usar límite de distancia horizontal (desactivar si solo quieres límite de altura)")]
    public bool useDistanceLimit = false;

    [Tooltip("Distancia máxima desde el centro antes de resetear (solo si useDistanceLimit = true)")]
    public float maxDistance = 5f;

    [Tooltip("Centro del área de juego (si está vacío, usa la posición inicial)")]
    public Transform playAreaCenter;

    [Header("Reset Behavior")]
    [Tooltip("Si está marcado, regresa a la posición inicial. Si no, regresa al centro del área")]
    public bool returnToStartPosition = true;

    [Tooltip("Tiempo de espera antes de resetear (segundos)")]
    public float resetDelay = 0.5f;

    [Header("Audio (opcional)")]
    public AudioClip resetSound;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 centerPosition;
    private Rigidbody rb;
    private AudioSource audioSource;
    private float outOfBoundsTime = -1f;

    void Start()
    {
        // Guardar posición y rotación inicial
        startPosition = transform.position;
        startRotation = transform.rotation;

        // Determinar centro del área
        if (playAreaCenter != null)
        {
            centerPosition = playAreaCenter.position;
        }
        else
        {
            centerPosition = startPosition;
        }

        // Obtener componentes
        rb = GetComponent<Rigidbody>();

        // Configurar audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && resetSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }

        Debug.Log($"[BOUNDS] {gameObject.name} - Start pos: {startPosition}, Center: {centerPosition}");
    }

    void Update()
    {
        // Verificar si está fuera de límites
        bool outOfBounds = IsOutOfBounds();

        if (outOfBounds)
        {
            // Empezar contador
            if (outOfBoundsTime < 0)
            {
                outOfBoundsTime = Time.time;
                Debug.Log($"[BOUNDS] {gameObject.name} fuera de límites, reseteando en {resetDelay}s...");
            }

            // Si ya pasó el tiempo de espera, resetear
            if (Time.time - outOfBoundsTime >= resetDelay)
            {
                ResetPosition();
                outOfBoundsTime = -1f;
            }
        }
        else
        {
            // Cancelar reset si volvió al área
            if (outOfBoundsTime >= 0)
            {
                Debug.Log($"[BOUNDS] {gameObject.name} volvió al área, reset cancelado");
                outOfBoundsTime = -1f;
            }
        }
    }

    bool IsOutOfBounds()
    {
        Vector3 currentPos = transform.position;

        // Verificar altura mínima
        if (currentPos.y < minHeight)
        {
            return true;
        }

        // Verificar distancia desde el centro (solo si está activado)
        if (useDistanceLimit)
        {
            float distanceFromCenter = Vector3.Distance(new Vector3(currentPos.x, centerPosition.y, currentPos.z),
                                                         new Vector3(centerPosition.x, centerPosition.y, centerPosition.z));
            if (distanceFromCenter > maxDistance)
            {
                return true;
            }
        }

        return false;
    }

    void ResetPosition()
    {
        Vector3 targetPosition = returnToStartPosition ? startPosition : centerPosition;
        Quaternion targetRotation = returnToStartPosition ? startRotation : Quaternion.identity;

        Debug.Log($"[BOUNDS] Reseteando {gameObject.name} a {targetPosition}");

        // Mover el objeto
        transform.position = targetPosition;
        transform.rotation = targetRotation;

        // Resetear física
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Sonido
        if (resetSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(resetSound);
        }
    }

    /// <summary>
    /// Llamar este método para resetear manualmente el objeto
    /// </summary>
    public void ManualReset()
    {
        outOfBoundsTime = -1f;
        ResetPosition();
    }

    /// <summary>
    /// Actualizar la posición inicial (útil si mueves el objeto en runtime)
    /// </summary>
    public void UpdateStartPosition()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        Debug.Log($"[BOUNDS] Nueva posición inicial para {gameObject.name}: {startPosition}");
    }

    // Gizmos para visualizar el área de juego en el Editor
    void OnDrawGizmosSelected()
    {
        Vector3 center = playAreaCenter != null ? playAreaCenter.position :
                        (Application.isPlaying ? centerPosition : transform.position);

        // Dibujar altura mínima
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        float visualSize = useDistanceLimit ? maxDistance * 2 : 10f;
        Gizmos.DrawCube(new Vector3(center.x, minHeight, center.z), new Vector3(visualSize, 0.1f, visualSize));

        // Dibujar límite de distancia (solo si está activado)
        if (useDistanceLimit)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(new Vector3(center.x, center.y, center.z), maxDistance);
        }

        // Dibujar posición inicial
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(startPosition, 0.1f);
        }
    }
}
