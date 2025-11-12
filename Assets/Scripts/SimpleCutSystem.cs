using UnityEngine;

/// <summary>
/// Sistema simple de corte - Al tocar con cuchillo, reemplaza el objeto con trozos pre-hechos
/// Funciona para: pescado, limón, carne, cebolla, etc.
/// </summary>
public class SimpleCutSystem : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject[] cutPieces; // Los trozos cortados (prefabs o GameObjects desactivados)
    [SerializeField] private Transform spawnPoint; // Donde aparecen los trozos (opcional)
    [SerializeField] private string knifeTag = "Knife"; // Tag del cuchillo

    [Header("Configuración")]
    [SerializeField] private bool destroyAfterCut = true; // Destruir objeto original o solo desactivar
    [SerializeField] private bool addPhysicsToPieces = true; // Añadir Rigidbody a los trozos
    [SerializeField] private float piecesMass = 0.05f; // Masa de cada trozo
    [SerializeField] private bool piecesAreGrabbable = true; // Los trozos son agarrables

    [Header("Efectos (Opcional)")]
    [SerializeField] private AudioClip cutSound; // Sonido de cortar
    [SerializeField] private ParticleSystem cutParticles; // Partículas al cortar
    [SerializeField] private bool hapticFeedback = true; // Vibración al cortar

    [Header("Cooldown")]
    [SerializeField] private float cutCooldown = 0.5f; // Evitar cortes múltiples instantáneos

    private bool hasBeenCut = false;
    private AudioSource audioSource;
    private float lastCutTime = 0f;

    void Start()
    {
        // Obtener o crear AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && cutSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Validación
        if (cutPieces == null || cutPieces.Length == 0)
        {
            Debug.LogWarning($"SimpleCutSystem en {gameObject.name}: No hay trozos asignados!");
        }

        // Si no hay spawn point, usar la posición de este objeto
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Verificar si es el cuchillo y si no ha sido cortado ya
        if (other.CompareTag(knifeTag) && !hasBeenCut)
        {
            // Cooldown para evitar cortes múltiples
            if (Time.time - lastCutTime < cutCooldown)
                return;

            lastCutTime = Time.time;

            // Ejecutar corte
            PerformCut();
        }
    }

    void PerformCut()
    {
        hasBeenCut = true;

        // Feedback de audio
        if (cutSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(cutSound, 0.8f);
        }

        // Feedback háptico
        if (hapticFeedback)
        {
            OVRInput.SetControllerVibration(0.4f, 0.3f, OVRInput.Controller.RTouch);
        }

        // Partículas
        if (cutParticles != null)
        {
            cutParticles.transform.position = transform.position;
            cutParticles.Play();
        }

        // Instanciar los trozos cortados
        SpawnCutPieces();

        // Desactivar o destruir el objeto original
        if (destroyAfterCut)
        {
            Destroy(gameObject, 0.1f); // Pequeño delay para que suene el audio
        }
        else
        {
            gameObject.SetActive(false);
        }

        Debug.Log($"{gameObject.name} ha sido cortado!");
    }

    void SpawnCutPieces()
    {
        if (cutPieces == null || cutPieces.Length == 0)
            return;

        Vector3 spawnPosition = spawnPoint.position;
        Quaternion spawnRotation = spawnPoint.rotation;

        foreach (GameObject piecePrefab in cutPieces)
        {
            if (piecePrefab == null)
                continue;

            // Instanciar el trozo
            GameObject piece = Instantiate(piecePrefab, spawnPosition, spawnRotation);

            // Configurar física y agarre
            ConfigurePiece(piece);
        }
    }

    void ConfigurePiece(GameObject piece)
    {
        // Activar el trozo (en caso de que venga desactivado)
        piece.SetActive(true);

        // Añadir física si está configurado
        if (addPhysicsToPieces)
        {
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = piece.AddComponent<Rigidbody>();
            }
            rb.mass = piecesMass;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Fuerzas desactivadas - los trozos solo caen por gravedad
            // rb.AddForce(Vector3.up * 0.05f, ForceMode.Impulse);
            // rb.AddTorque(Random.insideUnitSphere * 0.02f, ForceMode.Impulse);
        }

        // Asegurar que tiene collider
        if (piece.GetComponent<Collider>() == null)
        {
            MeshCollider meshCollider = piece.AddComponent<MeshCollider>();
            meshCollider.convex = true;
        }

        // Hacer agarrable si está configurado (comentado por si no usas Meta XR)
        if (piecesAreGrabbable)
        {
            // Descomentar si usas Meta XR Interaction SDK:
            // if (piece.GetComponent<OVRGrabbable>() == null)
            // {
            //     piece.AddComponent<OVRGrabbable>();
            // }

            // O si usas Meta XR Building Blocks:
            // if (piece.GetComponent<Grabbable>() == null)
            // {
            //     piece.AddComponent<Grabbable>();
            //     piece.AddComponent<GrabInteractable>();
            // }
        }
    }

    /// <summary>
    /// Método público para resetear (útil para testing)
    /// </summary>
    public void ResetCut()
    {
        hasBeenCut = false;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Forzar el corte desde código (útil para debugging)
    /// </summary>
    public void ForceCut()
    {
        if (!hasBeenCut)
        {
            PerformCut();
        }
    }

    // Debug visual en Scene View
    void OnDrawGizmos()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.03f);
            Gizmos.DrawLine(transform.position, spawnPoint.position);
        }
    }
}
