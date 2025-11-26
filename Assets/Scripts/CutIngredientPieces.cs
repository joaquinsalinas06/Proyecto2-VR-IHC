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

        [Header("Física de Pedazos")]
        [Tooltip("Masa de cada pedazo en kg (más bajo = más liviano)")]
        public float pieceMass = 0.005f;

        [Tooltip("Resistencia al movimiento (0 = sin resistencia, 1 = mucha resistencia)")]
        public float pieceDrag = 0.5f;
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

    }

    void SpawnPiecesRandom(Vector3 centerPosition)
    {
        Debug.Log($"[SPAWN RANDOM] Centro: {centerPosition}, Cantidad: {configuration.pieceCount}, Area: {configuration.spawnArea}");

        for (int i = 0; i < configuration.pieceCount; i++)
        {
            // Posición aleatoria dentro del área
            Vector3 randomOffset = new Vector3(
                Random.Range(-configuration.spawnArea.x, configuration.spawnArea.x),
                Random.Range(0, configuration.spawnArea.y),
                Random.Range(-configuration.spawnArea.z, configuration.spawnArea.z)
            );

            Vector3 spawnPos = centerPosition + randomOffset;

            if (i == 0) // Solo mostrar la primera pieza para no llenar el log
            {
                Debug.Log($"[SPAWN RANDOM] Primera pieza en: {spawnPos}");
            }

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

        // CRÍTICO: Elevar el centro de spawn para evitar colisión con la tabla
        // Las piezas spawn en el aire y luego caen suavemente
        Vector3 elevatedCenter = centerPosition + Vector3.up * 0.05f; // 5cm arriba

        for (int i = 0; i < configuration.specificPieces.Length; i++)
        {
            SpecificPiece piece = configuration.specificPieces[i];

            if (piece.prefab == null)
            {
                Debug.LogWarning($"[CUT] Pieza específica {i} no tiene prefab asignado. Saltando...");
                continue;
            }

            // IMPORTANTE: Separar más las piezas para evitar que salgan volando
            // Multiplicar el offset por 2.0 para mayor separación horizontal
            Vector3 separatedOffset = piece.position * 2.0f;
            Vector3 spawnPos = elevatedCenter + separatedOffset;

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

        // IMPORTANTE: Empezar como kinematic para evitar explosiones
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.mass = configuration.pieceMass;
        rb.drag = configuration.pieceDrag;
        rb.angularDrag = 1f;

        // Asegurar que tiene Collider
        Collider col = piece.GetComponent<Collider>();
        if (col == null)
        {
            col = piece.AddComponent<BoxCollider>();
        }

        // Desactivar collider temporalmente
        col.enabled = false;

        // IMPORTANTE: Buscar Y IGNORAR colisiones con cuchillo INMEDIATAMENTE
        List<Collider> knifeColliders = new List<Collider>();
        GameObject[] knives = GameObject.FindGameObjectsWithTag("Knife");

        foreach (GameObject knife in knives)
        {
            // Obtener TODOS los colliders del cuchillo y sus hijos (incluyendo Blade)
            Collider[] colliders = knife.GetComponentsInChildren<Collider>();
            foreach (Collider knifeCol in colliders)
            {
                knifeColliders.Add(knifeCol);
            }
        }

        // Activar física después de un breve delay, pasando los colliders del cuchillo
        StartCoroutine(EnablePhysicsAfterDelay(piece, 0.3f, knifeColliders));

        // Guardar renderer para highlight
        Renderer renderer = piece.GetComponent<Renderer>();
        if (renderer != null)
        {
            pieceRenderers.Add(renderer);
            originalMaterials.Add(renderer.material);
        }

        pieces.Add(piece);
    }

    System.Collections.IEnumerator EnablePhysicsAfterDelay(GameObject piece, float delay, List<Collider> knifeColliders)
    {
        yield return new WaitForSeconds(delay);

        if (piece == null) yield break;

        Rigidbody rb = piece.GetComponent<Rigidbody>();
        Collider col = piece.GetComponent<Collider>();

        // IMPORTANTE: Ignorar colisiones con TODOS los colliders del cuchillo
        List<Collider> ignoredColliders = new List<Collider>();

        foreach (Collider knifeCol in knifeColliders)
        {
            if (knifeCol != null && col != null)
            {
                Physics.IgnoreCollision(col, knifeCol, true);
                ignoredColliders.Add(knifeCol);
            }
        }

        // Activar collider
        if (col != null)
        {
            col.enabled = true;
        }

        // Activar física suavemente
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Restaurar colisiones después de 1.5 segundos
        if (ignoredColliders.Count > 0)
        {
            StartCoroutine(RestoreCollisions(col, ignoredColliders, 1.5f));
        }
    }

    System.Collections.IEnumerator RestoreCollisions(Collider pieceCollider, List<Collider> ignoredColliders, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (pieceCollider == null) yield break;

        foreach (Collider ignored in ignoredColliders)
        {
            if (ignored != null && pieceCollider != null)
            {
                Physics.IgnoreCollision(pieceCollider, ignored, false);
            }
        }
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
        // Detectar INICIO de pinch (GetDown, no Get)
        // Funciona con hand tracking de Meta Quest

        // Método 1: Detectar con PrimaryIndexTrigger (recomendado para hand tracking)
        bool leftPinchDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LHand);
        bool rightPinchDown = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RHand);

        if (leftPinchDown || rightPinchDown)
        {
            Debug.Log("[CUT] Pinch detectado!");
            return true;
        }

        // Método 2: Detectar con Button.One/Two (alternativo)
        if (OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Two))
        {
            Debug.Log("[CUT] Button detectado!");
            return true;
        }

        return false;
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
        // Detectar cuando SUELTA el pinch (GetUp)
        bool leftPinchUp = OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LHand);
        bool rightPinchUp = OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RHand);

        if (leftPinchUp || rightPinchUp)
        {
            Debug.Log("[CUT] Pinch soltado!");
            return true;
        }

        // Método alternativo con Button.One/Two
        if (OVRInput.GetUp(OVRInput.Button.One) || OVRInput.GetUp(OVRInput.Button.Two))
        {
            Debug.Log("[CUT] Button soltado!");
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
