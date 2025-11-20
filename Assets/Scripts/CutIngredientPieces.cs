using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Controla los pedazos de ingrediente cortado - Permite agarrar todos juntos
/// </summary>
public class CutIngredientPieces : MonoBehaviour
{
    public enum SpawnMode
    {
        Random,         // Posiciones aleatorias (para cubos de pescado)
        Specific        // Posiciones específicas (para mitades de limón)
    }

    [System.Serializable]
    public class SpecificPiece
    {
        public GameObject prefab;               // Prefab específico para esta pieza
        public Vector3 position;                // Posición relativa al centro
        public Vector3 rotation;                // Rotación (euler angles)
    }

    [System.Serializable]
    public class PieceConfiguration
    {
        public SpawnMode spawnMode = SpawnMode.Random;

        [Header("Modo Random (cubos, trozos)")]
        public GameObject piecePrefab;          // Prefab del pedazo (cubo, mitad, etc.)
        public int pieceCount = 10;             // Cantidad de pedazos
        public Vector3 spawnArea = new Vector3(0.2f, 0.1f, 0.2f); // Área de dispersión
        public bool randomRotation = true;      // Rotación aleatoria

        [Header("Modo Específico (mitades, gajos)")]
        public SpecificPiece[] specificPieces;  // Array de piezas específicas con sus prefabs
    }

    [Header("Configuración de Pedazos")]
    public PieceConfiguration configuration;

    [Header("Agarre Múltiple")]
    [Tooltip("Distancia para agarrar todos los pedazos")]
    public float grabRadius = 0.3f;             // Radio para detectar la mano

    [Tooltip("Tecla/botón para agarrar (opcional, también detecta pinch)")]
    public bool detectPinchGesture = true;

    [Header("Visual Feedback")]
    public Material highlightMaterial;          // Material cuando los pedazos están cerca de la mano
    public Color highlightColor = Color.yellow;

    [Header("Audio")]
    public AudioClip grabSound;
    public AudioClip releaseSound;

    private List<GameObject> pieces = new List<GameObject>();
    private List<Renderer> pieceRenderers = new List<Renderer>();
    private List<Material> originalMaterials = new List<Material>();

    private bool isGrabbed = false;
    private Transform grabTransform;            // Transform que agarra los pedazos
    private Vector3 grabOffset;
    private AudioSource audioSource;

    // Componentes de Oculus para detectar pinch
    private bool wasHandNear = false;

    void Start()
    {
        // Configurar audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }
    }

    /// <summary>
    /// Generar los pedazos del ingrediente cortado
    /// </summary>
    public void SpawnPieces(Vector3 centerPosition)
    {
        // Validar según el modo
        if (configuration.spawnMode == SpawnMode.Random && configuration.piecePrefab == null)
        {
            Debug.LogError("[CUT] Modo Random requiere un Piece Prefab configurado!");
            return;
        }

        if (configuration.spawnMode == SpawnMode.Specific &&
            (configuration.specificPieces == null || configuration.specificPieces.Length == 0))
        {
            Debug.LogError("[CUT] Modo Specific requiere configurar Specific Pieces!");
            return;
        }

        ClearPieces();

        if (configuration.spawnMode == SpawnMode.Specific)
        {
            // Modo específico: usar posiciones exactas con prefabs específicos
            SpawnPiecesSpecific(centerPosition);
        }
        else
        {
            // Modo random: dispersión aleatoria
            SpawnPiecesRandom(centerPosition);
        }

        Debug.Log($"[CUT] Generados {pieces.Count} pedazos en {centerPosition} (Modo: {configuration.spawnMode})");
    }

    void SpawnPiecesRandom(Vector3 centerPosition)
    {
        for (int i = 0; i < configuration.pieceCount; i++)
        {
            // Posición aleatoria dentro del área
            Vector3 randomOffset = new Vector3(
                Random.Range(-configuration.spawnArea.x, configuration.spawnArea.x),
                Random.Range(0, configuration.spawnArea.y),
                Random.Range(-configuration.spawnArea.z, configuration.spawnArea.z)
            );

            Vector3 spawnPos = centerPosition + randomOffset;

            // Rotación
            Quaternion rotation = configuration.randomRotation
                ? Random.rotation
                : Quaternion.identity;

            CreatePiece(spawnPos, rotation);
        }
    }

    void SpawnPiecesSpecific(Vector3 centerPosition)
    {
        if (configuration.specificPieces == null || configuration.specificPieces.Length == 0)
        {
            Debug.LogWarning("[CUT] No hay piezas específicas configuradas. Usando modo random.");
            SpawnPiecesRandom(centerPosition);
            return;
        }

        for (int i = 0; i < configuration.specificPieces.Length; i++)
        {
            SpecificPiece piece = configuration.specificPieces[i];

            if (piece.prefab == null)
            {
                Debug.LogWarning($"[CUT] Pieza específica {i} no tiene prefab asignado. Saltando...");
                continue;
            }

            // Posición relativa al centro
            Vector3 spawnPos = centerPosition + piece.position;

            // Rotación específica
            Quaternion rotation = Quaternion.Euler(piece.rotation);

            CreatePieceWithPrefab(piece.prefab, spawnPos, rotation);
        }
    }

    void CreatePiece(Vector3 position, Quaternion rotation)
    {
        CreatePieceWithPrefab(configuration.piecePrefab, position, rotation);
    }

    void CreatePieceWithPrefab(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("[CUT] Prefab es null, no se puede crear pieza.");
            return;
        }

        // Instanciar pedazo
        GameObject piece = Instantiate(prefab, position, rotation, transform);

        // Asegurar que tiene Rigidbody
        Rigidbody rb = piece.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = piece.AddComponent<Rigidbody>();
        }
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.mass = 0.05f; // Liviano

        // Asegurar que tiene Collider
        if (piece.GetComponent<Collider>() == null)
        {
            piece.AddComponent<BoxCollider>();
        }

        // Guardar renderer para highlight
        Renderer renderer = piece.GetComponent<Renderer>();
        if (renderer != null)
        {
            pieceRenderers.Add(renderer);
            originalMaterials.Add(renderer.material);
        }

        pieces.Add(piece);
    }

    void Update()
    {
        if (isGrabbed)
        {
            // Mover todos los pedazos con la mano
            UpdateGrabbedPieces();
        }
        else
        {
            // Detectar si la mano está cerca para agarrar
            CheckForHandNearby();
        }
    }

    void CheckForHandNearby()
    {
        if (pieces.Count == 0)
            return;

        // Calcular centro de los pedazos
        Vector3 centerPosition = GetPiecesCenter();

        // Buscar manos cercanas
        bool handNear = false;
        Transform nearestHand = null;
        float nearestDistance = float.MaxValue;

        // Buscar todos los objetos con "Hand" en el nombre (hand tracking)
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Hand") && obj.activeInHierarchy)
            {
                float distance = Vector3.Distance(obj.transform.position, centerPosition);
                if (distance < grabRadius && distance < nearestDistance)
                {
                    handNear = true;
                    nearestHand = obj.transform;
                    nearestDistance = distance;
                }
            }
        }

        // Highlight cuando la mano está cerca
        if (handNear && !wasHandNear)
        {
            HighlightPieces(true);
            wasHandNear = true;
        }
        else if (!handNear && wasHandNear)
        {
            HighlightPieces(false);
            wasHandNear = false;
        }

        // Detectar pinch para agarrar
        if (handNear && detectPinchGesture && DetectPinchGesture())
        {
            GrabAllPieces(nearestHand);
        }

        // También detectar botón de agarre (para controllers)
        if (handNear && (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger) ||
                        OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger)))
        {
            GrabAllPieces(nearestHand);
        }
    }

    bool DetectPinchGesture()
    {
        // Detectar pinch con hand tracking
        // Esto puede variar según tu setup, aquí hay una implementación genérica

        // Opción 1: Usar OVRInput (funciona con hand tracking)
        if (OVRInput.Get(OVRInput.Button.One) || OVRInput.Get(OVRInput.Button.Two))
        {
            return true;
        }

        // Opción 2: Detectar pinch strength (más preciso para hand tracking)
        float leftPinchStrength = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        float rightPinchStrength = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

        return leftPinchStrength > 0.8f || rightPinchStrength > 0.8f;
    }

    void GrabAllPieces(Transform hand)
    {
        if (isGrabbed || pieces.Count == 0)
            return;

        Debug.Log($"[CUT] Agarrando {pieces.Count} pedazos con {hand.name}");

        isGrabbed = true;
        grabTransform = hand;

        // Calcular offset del centro de los pedazos a la mano
        grabOffset = GetPiecesCenter() - hand.position;

        // Hacer todos los pedazos kinematic (no física mientras se agarran)
        foreach (GameObject piece in pieces)
        {
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        // Sonido
        if (grabSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(grabSound);
        }

        HighlightPieces(false);
    }

    void UpdateGrabbedPieces()
    {
        if (grabTransform == null || pieces.Count == 0)
        {
            ReleaseAllPieces();
            return;
        }

        // Calcular nueva posición central
        Vector3 targetCenter = grabTransform.position + grabOffset;
        Vector3 currentCenter = GetPiecesCenter();
        Vector3 deltaMove = targetCenter - currentCenter;

        // Mover todos los pedazos manteniendo sus posiciones relativas
        foreach (GameObject piece in pieces)
        {
            if (piece != null)
            {
                piece.transform.position += deltaMove;
            }
        }

        // Detectar release (soltar)
        if (DetectReleaseGesture())
        {
            ReleaseAllPieces();
        }
    }

    bool DetectReleaseGesture()
    {
        // Detectar cuando se suelta el pinch
        float leftPinchStrength = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
        float rightPinchStrength = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);

        // Soltó el pinch
        if (leftPinchStrength < 0.3f && rightPinchStrength < 0.3f)
        {
            return true;
        }

        // Soltó el botón (controllers)
        if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger) ||
            OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger))
        {
            return true;
        }

        return false;
    }

    void ReleaseAllPieces()
    {
        if (!isGrabbed)
            return;

        Debug.Log($"[CUT] Soltando {pieces.Count} pedazos");

        isGrabbed = false;
        grabTransform = null;

        // Restaurar física
        foreach (GameObject piece in pieces)
        {
            if (piece != null)
            {
                Rigidbody rb = piece.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }
            }
        }

        // Sonido
        if (releaseSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(releaseSound);
        }
    }

    void HighlightPieces(bool highlight)
    {
        for (int i = 0; i < pieceRenderers.Count; i++)
        {
            if (pieceRenderers[i] != null)
            {
                if (highlight)
                {
                    if (highlightMaterial != null)
                    {
                        pieceRenderers[i].material = highlightMaterial;
                    }
                    else
                    {
                        pieceRenderers[i].material.color = highlightColor;
                    }
                }
                else
                {
                    pieceRenderers[i].material = originalMaterials[i];
                }
            }
        }
    }

    Vector3 GetPiecesCenter()
    {
        if (pieces.Count == 0)
            return transform.position;

        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (GameObject piece in pieces)
        {
            if (piece != null)
            {
                sum += piece.transform.position;
                count++;
            }
        }

        return count > 0 ? sum / count : transform.position;
    }

    void ClearPieces()
    {
        foreach (GameObject piece in pieces)
        {
            if (piece != null)
            {
                Destroy(piece);
            }
        }

        pieces.Clear();
        pieceRenderers.Clear();
        originalMaterials.Clear();
    }

    void OnDestroy()
    {
        ClearPieces();
    }

    // Gizmos para debug
    void OnDrawGizmos()
    {
        if (pieces.Count > 0)
        {
            Vector3 center = GetPiecesCenter();

            // Radio de agarre
            Gizmos.color = isGrabbed ? Color.green : (wasHandNear ? Color.yellow : Color.blue);
            Gizmos.DrawWireSphere(center, grabRadius);
        }
    }
}
