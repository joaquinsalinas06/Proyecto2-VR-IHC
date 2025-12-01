using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema de corte con una línea curva para ingredientes como limón.
/// El usuario debe cortar siguiendo la línea durante un tiempo determinado.
/// </summary>
public class SingleLineCut : MonoBehaviour
{
    [Header("Configuración de Curva")]
    [Tooltip("Puntos de control para la curva de corte (3 puntos para una curva de Bézier cuadrática)")]
    public Vector3[] curvePoints = new Vector3[3] {
        new Vector3(-0.05f, 0, 0),
        new Vector3(0, 0, 0.05f),
        new Vector3(0.05f, 0, 0)
    };

    [Tooltip("Resolución de la curva (cuántos segmentos la componen)")]
    [Range(2, 50)]
    public int curveResolution = 20;

    [Tooltip("Grosor de la línea")]
    [Range(0.001f, 0.02f)]
    public float lineThickness = 0.005f;

    [Header("Progreso de Corte")]
    [Tooltip("Tiempo mínimo cortando para completar (segundos)")]
    [Range(1f, 5f)]
    public float requiredCutTime = 2.5f;

    [Header("Visual")]
    public Material lineMaterial;
    public Color lineColor = Color.yellow;
    [Range(0f, 5f)]
    public float lineEmissionIntensity = 2f;

    [Header("Detección de Cuchillo")]
    [Tooltip("Tag del cuchillo")]
    public string knifeTag = "Knife";

    [Tooltip("Velocidad mínima del cuchillo para contar como corte")]
    public float minKnifeVelocity = 0.1f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    [Header("Audio")]
    public AudioClip cuttingSound; // Sonido al estar cortando
    public AudioClip cutCompleteSound; // Sonido al finalizar el corte
    [Range(0f, 1f)]
    public float cuttingSoundVolume = 0.5f;

    // --- Estado interno ---
    private GameObject lineVisualObject;      // Contiene el LineRenderer
    private GameObject collidersParent;       // Padre de la cadena de colliders
    private bool isActive = false;
    private bool isCompleted = false;
    private float currentCutTime = 0f;
    private bool isKnifeCutting = false;
    private int activeDetectors = 0; // Contador de detectores activos
    private AudioSource audioSource;
    private float lastCutSoundTime = 0f;
    private const float CUT_SOUND_INTERVAL = 0.25f; // Intervalo para el sonido de corte

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
    }

    /// <summary>
    /// Activar el sistema de corte
    /// </summary>
    public void Activate()
    {
        if (isActive || isCompleted) return;

        isActive = true;
        currentCutTime = 0f;
        activeDetectors = 0;

        // Mostrar la línea de corte
        ShowCutLine();

        Debug.LogWarning($"[SINGLE CUT] Sistema ACTIVADO en '{gameObject.name}'. Esperando al cuchillo...");

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
        if (curvePoints == null || curvePoints.Length < 2)
        {
            Debug.LogError("[SINGLE CUT] Se necesitan al menos 2 puntos para definir la curva.");
            return;
        }

        // --- 1. Crear la línea visual con LineRenderer ---
        lineVisualObject = new GameObject("CutLine_Visual");
        lineVisualObject.transform.SetParent(transform, false);

        LineRenderer lineRenderer = lineVisualObject.AddComponent<LineRenderer>();

        // Calcular puntos de la curva
        List<Vector3> points = new List<Vector3>();
        for (int i = 0; i <= curveResolution; i++)
        {
            float t = i / (float)curveResolution;
            points.Add(CalculateBezierPoint(t, curvePoints));
        }

        // Configurar LineRenderer
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
        lineRenderer.startWidth = lineThickness;
        lineRenderer.endWidth = lineThickness;
        lineRenderer.useWorldSpace = false;

        // Configurar material brillante
        if (lineMaterial != null)
        {
            lineRenderer.material = new Material(lineMaterial);
            lineRenderer.material.color = lineColor;
            lineRenderer.material.SetColor("_EmissionColor", lineColor * lineEmissionIntensity);
        }
        else
        {
            // Fallback a un material estándar con emisión
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = lineColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", lineColor * lineEmissionIntensity);
            lineRenderer.material = mat;
        }

        // --- 2. Crear la cadena de colliders ---
        collidersParent = new GameObject("CutLine_Colliders");
        collidersParent.transform.SetParent(transform, false);
        collidersParent.layer = gameObject.layer;

        for (int i = 0; i < points.Count - 1; i++)
        {
            GameObject segment = new GameObject($"Collider_Segment_{i}");
            segment.transform.SetParent(collidersParent.transform, false);

            Vector3 startPoint = points[i];
            Vector3 endPoint = points[i + 1];
            
            // Posicionar el segmento en el punto medio
            segment.transform.localPosition = (startPoint + endPoint) / 2;
            
            // Orientar el segmento para que apunte al siguiente punto
            segment.transform.LookAt(transform.TransformPoint(endPoint));

            // Añadir y configurar el BoxCollider
            BoxCollider col = segment.AddComponent<BoxCollider>();
            col.isTrigger = true;
            float segmentLength = Vector3.Distance(startPoint, endPoint);
            col.size = new Vector3(lineThickness * 5, lineThickness * 5, segmentLength);

            // Añadir el detector
            SingleLineCutDetector detector = segment.AddComponent<SingleLineCutDetector>();
            detector.parentCut = this;
            detector.minVelocity = minKnifeVelocity;
            detector.knifeTag = knifeTag;
        }
    }

    void HideCutLine()
    {
        if (lineVisualObject != null)
            Destroy(lineVisualObject);
        if (collidersParent != null)
            Destroy(collidersParent);
    }

    void Update()
    {
        if (!isActive || isCompleted)
            return;

        // Si el cuchillo está cortando (al menos un detector activo), incrementar tiempo
        if (isKnifeCutting)
        {
            currentCutTime += Time.deltaTime;

            // Reproducir sonido de corte a intervalos
            if (cuttingSound != null && Time.time - lastCutSoundTime > CUT_SOUND_INTERVAL)
            {
                audioSource.PlayOneShot(cuttingSound, cuttingSoundVolume);
                lastCutSoundTime = Time.time;
            }

            if (currentCutTime >= requiredCutTime)
            {
                CompleteCut();
            }
        }
    }

    /// <summary>
    /// Calcula un punto en una curva de Bézier (cuadrática, cúbica, etc.)
    /// </summary>
    Vector3 CalculateBezierPoint(float t, Vector3[] controlPoints)
    {
        if (controlPoints.Length < 2) return Vector3.zero;

        // Caso Lineal (2 puntos)
        if (controlPoints.Length == 2)
        {
            return Vector3.Lerp(controlPoints[0], controlPoints[1], t);
        }
        
        // Caso Cuadrático (3 puntos)
        if (controlPoints.Length == 3)
        {
            Vector3 p0 = controlPoints[0];
            Vector3 p1 = controlPoints[1];
            Vector3 p2 = controlPoints[2];
            float u = 1 - t;
            return (u * u * p0) + (2 * u * t * p1) + (t * t * p2);
        }

        // Caso Cúbico (4 puntos)
        if (controlPoints.Length == 4)
        {
            Vector3 p0 = controlPoints[0];
            Vector3 p1 = controlPoints[1];
            Vector3 p2 = controlPoints[2];
            Vector3 p3 = controlPoints[3];
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;
            return uuu * p0 + 3 * uu * t * p1 + 3 * u * tt * p2 + ttt * p3;
        }
        
        // Fallback para más de 4 puntos (no es una Bézier verdadera, pero funciona)
        // Se puede mejorar con splines (Catmull-Rom) si es necesario
        int numSegments = controlPoints.Length - 1;
        int currentSegment = Mathf.Min(Mathf.FloorToInt(t * numSegments), numSegments - 1);
        float localT = (t * numSegments) - currentSegment;
        return Vector3.Lerp(controlPoints[currentSegment], controlPoints[currentSegment + 1], localT);
    }

    /// <summary>
    /// Llamado por el detector cuando el cuchillo empieza a cortar
    /// </summary>
    public void OnKnifeCuttingEnter()
    {
        if (!isActive || isCompleted) return;
        activeDetectors++;
        isKnifeCutting = true;
        Debug.Log($"[SINGLE CUT] OnKnifeCuttingEnter. Detectores activos: {activeDetectors}. Se considera cortando.");
    }

    /// <summary>
    /// Llamado cuando el cuchillo sale de un detector
    /// </summary>
    public void OnKnifeCuttingExit()
    {
        if (!isActive || isCompleted) return;
        activeDetectors--;
        if (activeDetectors <= 0)
        {
            isKnifeCutting = false;
            activeDetectors = 0; // Prevenir números negativos
            Debug.LogWarning("[SINGLE CUT] OnKnifeCuttingExit. Último detector salido. Ya NO se considera cortando.");
        }
        else
        {
            Debug.Log($"[SINGLE CUT] OnKnifeCuttingExit. Detectores activos restantes: {activeDetectors}.");
        }
    }

    void CompleteCut()
    {
        if (isCompleted) return;

        isCompleted = true;
        isActive = false;

        if (showDebugInfo)
            Debug.Log($"[SINGLE CUT] ¡Corte completado! Notificando a la tabla de cortar.");

        // Reproducir sonido de corte completado
        if (cutCompleteSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(cutCompleteSound);
        }

        HideCutLine();
        OnCutCompleted?.Invoke();
    }

    public bool IsComplete() => isCompleted;
    public float GetProgress() => Mathf.Clamp01(currentCutTime / requiredCutTime);

    void OnDestroy()
    {
        HideCutLine();
    }

    void OnDrawGizmos()
    {
        if (curvePoints == null || curvePoints.Length < 2) return;

        Gizmos.color = lineColor;
        Vector3 lastPoint = transform.TransformPoint(curvePoints[0]);

        for (int i = 1; i <= curveResolution; i++)
        {
            float t = i / (float)curveResolution;
            Vector3 currentPoint = transform.TransformPoint(CalculateBezierPoint(t, curvePoints));
            Gizmos.DrawLine(lastPoint, currentPoint);
            lastPoint = currentPoint;
        }

        // Dibujar los puntos de control
        Gizmos.color = Color.red;
        for (int i = 0; i < curvePoints.Length; i++)
        {
            Gizmos.DrawSphere(transform.TransformPoint(curvePoints[i]), 0.01f);
        }
    }
}

/// <summary>
/// Detector de cuchillo para el sistema de corte simple. Ahora notifica al entrar y salir.
/// </summary>
public class SingleLineCutDetector : MonoBehaviour
{
    [HideInInspector] public SingleLineCut parentCut;
    [HideInInspector] public float minVelocity = 0.1f;
    [HideInInspector] public string knifeTag = "Knife";

    private bool isKnifeInside = false;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[CUT DETECTOR] OnTriggerEnter: Un objeto llamado '{other.name}' con tag '{other.tag}' ha entrado en un segmento de corte.");

        if (isKnifeInside) return; // Ya hay un cuchillo dentro

        if (other.CompareTag(knifeTag) || other.name.Contains("Blade"))
        {
            Debug.LogWarning($"[CUT DETECTOR] ¡CUCHILLO DETECTADO! '{other.name}' ha entrado.");
            isKnifeInside = true;
            parentCut?.OnKnifeCuttingEnter();
        }
        else
        {
            Debug.Log($"[CUT DETECTOR] El objeto '{other.name}' no es un cuchillo. Ignorando.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!isKnifeInside) return;

        if (other.CompareTag(knifeTag) || other.name.Contains("Blade"))
        {
            Debug.LogWarning($"[CUT DETECTOR] ¡CUCHILLO HA SALIDO! '{other.name}' ha salido.");
            isKnifeInside = false;
            parentCut?.OnKnifeCuttingExit();
        }
    }
}
