using UnityEngine;

/// <summary>
/// Sistema de corte con una sola línea para ingredientes como limón
/// El usuario debe cortar siguiendo la línea durante 2-3 segundos
/// </summary>
public class SingleLineCut : MonoBehaviour
{
    [Header("Configuración de Línea")]
    [Tooltip("Posición local de la línea de corte")]
    public Vector3 linePosition = Vector3.zero;

    [Tooltip("Longitud de la línea visible")]
    [Range(0.01f, 1f)]
    public float lineLength = 0.15f;

    [Tooltip("Grosor de la línea")]
    [Range(0.001f, 0.02f)]
    public float lineThickness = 0.005f;

    [Tooltip("La línea es horizontal (X) o vertical (Z)")]
    public bool isHorizontal = true;

    [Header("Progreso de Corte")]
    [Tooltip("Tiempo mínimo cortando para completar (segundos)")]
    [Range(1f, 5f)]
    public float requiredCutTime = 2.5f;

    [Header("Visual")]
    public Material lineMaterial;
    public Color lineColor = Color.yellow;
    [Range(0f, 5f)]
    public float lineEmissionIntensity = 2f;

    [Header("Mitades del Ingrediente")]
    [Tooltip("Prefab de la primera mitad")]
    public GameObject halfPrefab1;

    [Tooltip("Prefab de la segunda mitad")]
    public GameObject halfPrefab2;

    [Tooltip("Offset para separar las mitades al spawnear")]
    public Vector3 separationOffset = new Vector3(0f, 0f, 0.02f);

    [Header("Detección de Cuchillo")]
    [Tooltip("Tag del cuchillo")]
    public string knifeTag = "Knife";

    [Tooltip("Velocidad mínima del cuchillo para contar como corte")]
    public float minKnifeVelocity = 0.1f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    // Estado interno
    private GameObject lineObject;
    private GameObject colliderObject;
    private bool isActive = false;
    private bool isCompleted = false;
    private float currentCutTime = 0f;
    private bool isKnifeCutting = false;
    private float cutStartTime = 0f;

    // Referencias spawneadas
    private GameObject spawnedHalf1;
    private GameObject spawnedHalf2;
    private bool halvesSpawned = false;

    // Evento cuando se completa el corte
    public System.Action OnCutCompleted;

    void Start()
    {
        // No activar automáticamente, esperar a que se coloque en la tabla
    }

    /// <summary>
    /// Activar el sistema de corte
    /// </summary>
    public void Activate()
    {
        if (isActive || isCompleted) return;

        isActive = true;
        currentCutTime = 0f;
        cutStartTime = Time.time;

        // Mostrar la línea de corte
        ShowCutLine();

        if (showDebugInfo)
            Debug.Log($"[SINGLE CUT] Activado en {gameObject.name}");
    }

    /// <summary>
    /// Desactivar el sistema
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        HideCutLine();
    }

    void ShowCutLine()
    {
        // Crear objeto visual de la línea
        lineObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lineObject.name = "CutLine_Visual";
        lineObject.transform.SetParent(transform);
        lineObject.transform.localPosition = linePosition;

        // Escalar según orientación
        if (isHorizontal)
        {
            // Horizontal: largo en Z
            lineObject.transform.localScale = new Vector3(lineThickness, lineThickness, lineLength);
        }
        else
        {
            // Vertical: largo en X
            lineObject.transform.localScale = new Vector3(lineLength, lineThickness, lineThickness);
        }

        // Desactivar collider de la visual
        Collider visualCol = lineObject.GetComponent<Collider>();
        if (visualCol != null)
            Destroy(visualCol);

        // Aplicar material brillante
        Renderer renderer = lineObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(lineMaterial != null ? lineMaterial : Shader.Find("Standard"));
            mat.color = lineColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", lineColor * lineEmissionIntensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetFloat("_Metallic", 0.5f);
            mat.SetFloat("_Glossiness", 0.8f);
            renderer.material = mat;
        }

        // CREAR COLLIDER para detección
        colliderObject = new GameObject("CutLine_Collider");
        colliderObject.transform.SetParent(transform);
        colliderObject.transform.localPosition = linePosition;
        colliderObject.layer = gameObject.layer;

        BoxCollider col = colliderObject.AddComponent<BoxCollider>();
        col.isTrigger = true;

        // Hacer collider más grande para mejor detección
        if (isHorizontal)
        {
            col.size = new Vector3(lineThickness * 5f, lineThickness * 5f, lineLength);
        }
        else
        {
            col.size = new Vector3(lineLength, lineThickness * 5f, lineThickness * 5f);
        }

        // Agregar detector de cuchillo
        SingleLineCutDetector detector = colliderObject.AddComponent<SingleLineCutDetector>();
        detector.parentCut = this;
        detector.minVelocity = minKnifeVelocity;
        detector.knifeTag = knifeTag;
    }

    void HideCutLine()
    {
        if (lineObject != null)
            Destroy(lineObject);
        if (colliderObject != null)
            Destroy(colliderObject);
    }

    void Update()
    {
        if (!isActive || isCompleted)
            return;

        // Si el cuchillo está cortando, incrementar tiempo
        if (isKnifeCutting)
        {
            currentCutTime += Time.deltaTime;

            // Verificar si se completó el corte
            if (currentCutTime >= requiredCutTime)
            {
                CompleteCut();
            }
        }
    }

    /// <summary>
    /// Llamado por el detector cuando el cuchillo está cortando
    /// </summary>
    public void OnKnifeCutting()
    {
        if (!isActive || isCompleted) return;
        isKnifeCutting = true;
    }

    /// <summary>
    /// Llamado cuando el cuchillo sale de la zona
    /// </summary>
    public void OnKnifeExit()
    {
        isKnifeCutting = false;
    }

    void CompleteCut()
    {
        if (isCompleted) return;

        isCompleted = true;
        isActive = false;

        if (showDebugInfo)
            Debug.Log($"[SINGLE CUT] ¡Corte completado! Tiempo: {currentCutTime:F1}s");

        // Ocultar línea
        HideCutLine();

        // Spawnear las mitades después de un pequeño delay
        Invoke(nameof(SpawnHalves), 0.2f);

        OnCutCompleted?.Invoke();
    }

    void SpawnHalves()
    {
        if (halvesSpawned) return;
        halvesSpawned = true;

        Vector3 spawnPos = transform.position;
        Quaternion spawnRot = transform.rotation;

        // Spawnear primera mitad (ligeramente a la izquierda/arriba)
        if (halfPrefab1 != null)
        {
            spawnedHalf1 = Instantiate(halfPrefab1, spawnPos - separationOffset, spawnRot);
            PrepareHalf(spawnedHalf1);
        }

        // Spawnear segunda mitad (ligeramente a la derecha/abajo)
        if (halfPrefab2 != null)
        {
            spawnedHalf2 = Instantiate(halfPrefab2, spawnPos + separationOffset, spawnRot);
            PrepareHalf(spawnedHalf2);
        }

        // Ocultar el ingrediente original
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        // Deshabilitar colliders del original
        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }

        if (showDebugInfo)
            Debug.Log($"[SINGLE CUT] Mitades spawneadas en {spawnPos}");
    }

    void PrepareHalf(GameObject half)
    {
        // Asegurar que tenga Rigidbody
        Rigidbody rb = half.GetComponent<Rigidbody>();
        if (rb == null)
            rb = half.AddComponent<Rigidbody>();

        // Configurar física: quietas al inicio
        rb.isKinematic = true;
        rb.useGravity = false;

        // Después de un delay, habilitar física para que se puedan agarrar
        StartCoroutine(EnableHalfPhysicsAfterDelay(rb, 1f));
    }

    System.Collections.IEnumerator EnableHalfPhysicsAfterDelay(Rigidbody rb, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Habilitar física
        rb.isKinematic = false;
        rb.useGravity = true;

        if (showDebugInfo)
            Debug.Log($"[SINGLE CUT] Física habilitada en mitad");
    }

    public bool IsComplete()
    {
        return isCompleted;
    }

    public float GetProgress()
    {
        return Mathf.Clamp01(currentCutTime / requiredCutTime);
    }

    void OnDestroy()
    {
        HideCutLine();
    }

    // Gizmos para visualización en el editor
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.02f);

        // Dibujar línea de corte
        Gizmos.color = lineColor;
        Vector3 worldPos = transform.TransformPoint(linePosition);

        Vector3 start, end;
        if (isHorizontal)
        {
            start = worldPos - transform.forward * lineLength / 2f;
            end = worldPos + transform.forward * lineLength / 2f;
        }
        else
        {
            start = worldPos - transform.right * lineLength / 2f;
            end = worldPos + transform.right * lineLength / 2f;
        }

        // Línea gruesa
        Gizmos.DrawLine(start, end);
        Vector3 offset = transform.up * lineThickness;
        Gizmos.DrawLine(start + offset, end + offset);
        Gizmos.DrawLine(start - offset, end - offset);

        #if UNITY_EDITOR
        string progressText = Application.isPlaying && isActive
            ? $"{GetProgress() * 100f:F0}%"
            : "Cortar aquí";
        UnityEditor.Handles.Label(worldPos + Vector3.up * 0.02f, progressText);
        #endif
    }
}

/// <summary>
/// Detector de cuchillo para el sistema de corte simple
/// </summary>
public class SingleLineCutDetector : MonoBehaviour
{
    [HideInInspector] public SingleLineCut parentCut;
    [HideInInspector] public float minVelocity = 0.1f;
    [HideInInspector] public string knifeTag = "Knife";

    private Vector3 lastKnifePosition;
    private float lastKnifeTime;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(knifeTag) || other.name.Contains("Blade"))
        {
            lastKnifePosition = other.transform.position;
            lastKnifeTime = Time.time;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(knifeTag) || other.name.Contains("Blade"))
        {
            float deltaTime = Time.time - lastKnifeTime;

            if (deltaTime > 0.001f)
            {
                Vector3 deltaPosition = other.transform.position - lastKnifePosition;
                float velocity = deltaPosition.magnitude / deltaTime;

                if (velocity > minVelocity)
                {
                    parentCut?.OnKnifeCutting();
                }

                lastKnifePosition = other.transform.position;
                lastKnifeTime = Time.time;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(knifeTag) || other.name.Contains("Blade"))
        {
            parentCut?.OnKnifeExit();
        }
    }
}
