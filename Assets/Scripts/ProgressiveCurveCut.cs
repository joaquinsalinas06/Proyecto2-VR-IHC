using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema de corte progresivo con múltiples líneas curvas.
/// Ideal para ingredientes que requieren varios cortes curvos, como una cebolla.
/// Combina la lógica de SingleLineCut y ProgressiveCutGrid.
/// </summary>
public class ProgressiveCurveCut : MonoBehaviour
{
    [System.Serializable]
    public class CurvedCutLine
    {
        [Header("Configuración de Curva")]
        [Tooltip("Puntos de control para la curva de corte (5 para Bézier cúbica)")]
        public Vector3[] curvePoints = new Vector3[5] {
            new Vector3(-0.05f, 0, 0),
            new Vector3(-0.025f, 0, 0.025f),
            new Vector3(0, 0, 0.05f),
            new Vector3(0.025f, 0, 0.025f),
            new Vector3(0.05f, 0, 0)
        };

        [Tooltip("Resolución de la curva")]
        [Range(2, 50)]
        public int curveResolution = 20;

        [Tooltip("Grosor de la línea")]
        [Range(0.001f, 0.02f)]
        public float lineThickness = 0.005f;

        [Header("Progreso de Corte")]
        [Tooltip("Tiempo mínimo cortando para completar (segundos)")]
        [Range(1f, 5f)]
        public float requiredCutTime = 2.0f;

        // --- Estado Interno de la Línea ---
        [HideInInspector] public GameObject lineVisualObject;
        [HideInInspector] public GameObject collidersParent;
        [HideInInspector] public bool isCompleted = false;
        [HideInInspector] public float currentCutTime = 0f;

        /// <summary>
        /// Copia los valores de otra línea (excepto el estado interno)
        /// </summary>
        public void CopyFrom(CurvedCutLine other)
        {
            curvePoints = new Vector3[other.curvePoints.Length];
            System.Array.Copy(other.curvePoints, curvePoints, other.curvePoints.Length);
            curveResolution = other.curveResolution;
            lineThickness = other.lineThickness;
            requiredCutTime = other.requiredCutTime;
        }
    }

    [Header("Configuración de Curvas Progresivas")]
    [Tooltip("Las líneas de corte curvas que aparecerán en orden")]
    public CurvedCutLine[] curvedCutLines;

    [Header("Visual")]
    public Material lineMaterial;
    public Color initialLineColor = Color.cyan;
    [Range(0f, 5f)]
    public float lineEmissionIntensity = 2.5f;

    [Header("Detección de Cuchillo")]
    [Tooltip("Tag del cuchillo")]
    public string knifeTag = "Knife";
    [Tooltip("Nombre alternativo del objeto cuchillo (para búsqueda por nombre)")]
    public string knifeNameContains = "Blade";
    [Tooltip("Velocidad mínima del cuchillo para contar como corte")]
    public float minKnifeVelocity = 0.1f;

    [Header("Audio")]
    public AudioClip lineCompleteSound;
    public AudioClip cuttingSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.5f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    // --- Estado Interno del Sistema ---
    private int currentLineIndex = 0;
    private bool isActive = false;
    private bool isKnifeCuttingCurrentLine = false;
    private int activeDetectors = 0;
    private AudioSource audioSource;
    private float lastCutSoundTime = 0f;
    private const float CUT_SOUND_INTERVAL = 0.25f;

    // Evento al completar todos los cortes
    public System.Action OnAllCutsCompleted;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f;
        }
    }

    public void Activate()
    {
        if (isActive) return;
        
        // Debug: Mostrar configuración actual
        Debug.Log($"[PROGRESSIVE CURVE] 🔍 Intentando activar en '{gameObject.name}'...");
        Debug.Log($"[PROGRESSIVE CURVE] - curvedCutLines: {(curvedCutLines == null ? "NULL" : $"Array con {curvedCutLines.Length} elementos")}");
        
        // Validación exhaustiva: verificar que exista el array
        if (curvedCutLines == null || curvedCutLines.Length == 0)
        {
            Debug.LogError($"[PROGRESSIVE CURVE] ❌ ¡CONFIGURACIÓN INCOMPLETA en '{gameObject.name}'!\n" +
                          "El array 'Curved Cut Lines' está vacío o es null.\n\n" +
                          "SOLUCIÓN:\n" +
                          "1. Selecciona el GameObject de la cebolla en Unity\n" +
                          "2. Encuentra el componente 'ProgressiveCurveCut'\n" +
                          "3. Establece 'Curved Cut Lines' → Size = 1\n" +
                          "4. Expande 'Element 0' y configura los 'Curve Points'\n" +
                          "5. Establece 'Curve Points' → Size = 5 (o más)\n" +
                          "6. Configura cada punto de la curva\n\n" +
                          "SIN ESTA CONFIGURACIÓN, LA CEBOLLA SE CORTARÁ INSTANTÁNEAMENTE.");
            
            // Desactivar el componente para evitar comportamiento erróneo
            enabled = false;
            return;
        }

        // CRÍTICO: Verificar que cada línea tenga curvePoints configurados
        for (int i = 0; i < curvedCutLines.Length; i++)
        {
            int pointCount = (curvedCutLines[i].curvePoints == null) ? 0 : curvedCutLines[i].curvePoints.Length;
            Debug.Log($"[PROGRESSIVE CURVE] - Element {i}: {pointCount} curve points");
            
            if (curvedCutLines[i].curvePoints == null || curvedCutLines[i].curvePoints.Length < 2)
            {
                Debug.LogError($"[PROGRESSIVE CURVE] ❌ ¡ERROR en '{gameObject.name}'!\n" +
                              $"La línea {i} tiene {pointCount} Curve Points (necesita mínimo 2).\n\n" +
                              "SOLUCIÓN:\n" +
                              $"1. Expande 'Curved Cut Lines' → Element {i}\n" +
                              "2. Establece 'Curve Points' → Size = 5 (recomendado para Bézier)\n" +
                              "3. Configura las coordenadas de cada punto\n\n" +
                              "Ejemplo para una curva suave:\n" +
                              "  Point 0: (0.04, 0.06, 0.09)\n" +
                              "  Point 1: (0.15, 0.02, 0.09)\n" +
                              "  Point 2: (0.11, -0.07, 0.09)\n" +
                              "  Point 3: (0.09, -0.15, 0.09)\n" +
                              "  Point 4: (0.01, -0.18, 0.09)");
                
                enabled = false;
                return;
            }
        }

        isActive = true;
        currentLineIndex = 0;
        ResetAllLines();
        ShowCurrentCurve();

        Debug.Log($"[PROGRESSIVE CURVE] ✅ Sistema activado en '{gameObject.name}'. Mostrando curva {currentLineIndex + 1}/{curvedCutLines.Length}.");
    }

    public void Deactivate()
    {
        isActive = false;
        HideAllCurves();
        if (showDebugInfo)
            Debug.Log("[PROGRESSIVE CURVE] Sistema desactivado.");
    }

    void Update()
    {
        if (!isActive || currentLineIndex >= curvedCutLines.Length)
            return;

        if (isKnifeCuttingCurrentLine)
        {
            CurvedCutLine currentLine = curvedCutLines[currentLineIndex];
            currentLine.currentCutTime += Time.deltaTime;

            // Reproducir sonido de corte
            if (cuttingSound != null && Time.time - lastCutSoundTime > CUT_SOUND_INTERVAL)
            {
                audioSource.PlayOneShot(cuttingSound, soundVolume);
                lastCutSoundTime = Time.time;
            }

            // Comprobar si se completó la línea actual
            if (currentLine.currentCutTime >= currentLine.requiredCutTime)
            {
                CompleteCurrentLine();
            }
        }
    }

    void ShowCurrentCurve()
    {
        if (currentLineIndex >= curvedCutLines.Length) return;

        CurvedCutLine line = curvedCutLines[currentLineIndex];
        
        // Validar que la línea tenga puntos de curva configurados
        if (line.curvePoints == null || line.curvePoints.Length == 0)
        {
            Debug.LogError($"[PROGRESSIVE CURVE] La línea {currentLineIndex} no tiene curvePoints configurados! Por favor configura los puntos en el Inspector.");
            return;
        }
        
        // --- 1. Crear Visual de la Curva ---
        line.lineVisualObject = new GameObject($"CurveVisual_{currentLineIndex}");
        line.lineVisualObject.transform.SetParent(transform, false);

        LineRenderer lineRenderer = line.lineVisualObject.AddComponent<LineRenderer>();
        List<Vector3> points = CalculateAllBezierPoints(line);

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
        lineRenderer.startWidth = line.lineThickness;
        lineRenderer.endWidth = line.lineThickness;
        lineRenderer.useWorldSpace = false;

        // Configurar material
        Color curveColor = GetLineColor(currentLineIndex);
        if (lineMaterial != null)
        {
            lineRenderer.material = new Material(lineMaterial);
            lineRenderer.material.color = curveColor;
            lineRenderer.material.SetColor("_EmissionColor", curveColor * lineEmissionIntensity);
        }
        else
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = curveColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", curveColor * lineEmissionIntensity);
            lineRenderer.material = mat;
        }

        // --- 2. Crear Colliders de la Curva ---
        line.collidersParent = new GameObject($"CurveColliders_{currentLineIndex}");
        line.collidersParent.transform.SetParent(transform, false);
        line.collidersParent.layer = gameObject.layer;

        for (int i = 0; i < points.Count - 1; i++)
        {
            GameObject segment = new GameObject($"Collider_Segment_{i}");
            segment.transform.SetParent(line.collidersParent.transform, false);

            Vector3 startPoint = points[i];
            Vector3 endPoint = points[i+1];

            segment.transform.localPosition = (startPoint + endPoint) / 2;
            segment.transform.LookAt(transform.TransformPoint(endPoint));

            BoxCollider col = segment.AddComponent<BoxCollider>();
            col.isTrigger = true;
            float segmentLength = Vector3.Distance(startPoint, endPoint);
            col.size = new Vector3(line.lineThickness * 5, line.lineThickness * 5, segmentLength);

            // Añadir el detector genérico y conectar los eventos
            var detector = segment.AddComponent<GenericCutDetector>();
            if (!string.IsNullOrEmpty(knifeTag))
            {
                detector.objectTag = knifeTag;
            }
            if (!string.IsNullOrEmpty(knifeNameContains))
            {
                detector.objectNameContains = knifeNameContains;
            }
            
            detector.OnObjectEnter.AddListener(HandleKnifeEnter);
            detector.OnObjectExit.AddListener(HandleKnifeExit);
        }
    }
    
    /// <summary>
    /// Manejador para cuando un detector registra la entrada del cuchillo.
    /// </summary>
    void HandleKnifeEnter()
    {
        if (!isActive || currentLineIndex >= curvedCutLines.Length) return;
        activeDetectors++;
        isKnifeCuttingCurrentLine = true;

        if(showDebugInfo) 
            Debug.Log($"[PROGRESSIVE CURVE] Knife enter. Active detectors: {activeDetectors}");
    }

    /// <summary>
    /// Manejador para cuando un detector registra la salida del cuchillo.
    /// </summary>
    void HandleKnifeExit()
    {
        if (!isActive || currentLineIndex >= curvedCutLines.Length) return;
        activeDetectors--;
        if (activeDetectors <= 0)
        {
            isKnifeCuttingCurrentLine = false;
            activeDetectors = 0; // Prevenir negativos
            if(showDebugInfo)
                Debug.LogWarning("[PROGRESSIVE CURVE] Last knife exit. No longer cutting.");
        }
        else
        {
            if(showDebugInfo)
                Debug.Log($"[PROGRESSIVE CURVE] Knife exit. Remaining detectors: {activeDetectors}");
        }
    }


    void CompleteCurrentLine()
    {
        if (currentLineIndex >= curvedCutLines.Length) return;

        CurvedCutLine completedLine = curvedCutLines[currentLineIndex];
        completedLine.isCompleted = true;
        isKnifeCuttingCurrentLine = false;
        activeDetectors = 0;

        if (showDebugInfo)
            Debug.Log($"[PROGRESSIVE CURVE] Curva {currentLineIndex + 1} completada!");

        // Sonido de línea completada
        if (lineCompleteSound != null)
        {
            audioSource.PlayOneShot(lineCompleteSound, soundVolume);
        }

        // Ocultar la curva actual
        HideCurve(completedLine);

        // Avanzar a la siguiente curva
        currentLineIndex++;

        if (currentLineIndex < curvedCutLines.Length)
        {
            ShowCurrentCurve();
            if (showDebugInfo)
                Debug.Log($"[PROGRESSIVE CURVE] Mostrando curva {currentLineIndex + 1}.");
        }
        else
        {
            // Todas las curvas completadas
            isActive = false;
            OnAllCutsCompleted?.Invoke();
            if (showDebugInfo)
                Debug.Log("[PROGRESSIVE CURVE] ¡Todos los cortes completados!");
        }
    }

    List<Vector3> CalculateAllBezierPoints(CurvedCutLine line)
    {
        List<Vector3> points = new List<Vector3>();
        for (int i = 0; i <= line.curveResolution; i++)
        {
            float t = i / (float)line.curveResolution;
            points.Add(CalculateBezierPoint(t, line.curvePoints));
        }
        return points;
    }

    Vector3 CalculateBezierPoint(float t, Vector3[] controlPoints)
    {
        if (controlPoints == null || controlPoints.Length == 0)
        {
            Debug.LogError("[PROGRESSIVE CURVE] ¡controlPoints es null o vacío!");
            return Vector3.zero;
        }
        
        int n = controlPoints.Length - 1;
        if (n < 0) return Vector3.zero;

        Vector3[] points = new Vector3[n + 1];
        System.Array.Copy(controlPoints, points, n + 1);

        for (int i = 1; i <= n; i++)
        {
            for (int j = 0; j <= n - i; j++)
            {
                points[j] = Vector3.Lerp(points[j], points[j + 1], t);
            }
        }
        return points[0];
    }

    void HideCurve(CurvedCutLine line)
    {
        if (line.lineVisualObject != null) Destroy(line.lineVisualObject);
        if (line.collidersParent != null) Destroy(line.collidersParent);
    }

    void HideAllCurves()
    {
        foreach (var line in curvedCutLines)
        {
            HideCurve(line);
        }
    }
    
    void ResetAllLines()
    {
        foreach (var line in curvedCutLines)
        {
            line.isCompleted = false;
            line.currentCutTime = 0f;
            HideCurve(line);
        }
    }

    Color GetLineColor(int index)
    {
        return Color.HSVToRGB((initialLineColor.r + index * 0.1f) % 1f, 0.8f, 1f);
    }

    public bool IsComplete() => currentLineIndex >= curvedCutLines.Length;
    public float GetProgress() => curvedCutLines.Length == 0 ? 0 : (float)currentLineIndex / curvedCutLines.Length;

    void OnDrawGizmos()
    {
        if (curvedCutLines == null) return;
        
        for(int i=0; i<curvedCutLines.Length; i++)
        {
            var line = curvedCutLines[i];
            if (line.curvePoints == null || line.curvePoints.Length < 2) continue;

            Gizmos.color = GetLineColor(i);
            Vector3 lastPoint = transform.TransformPoint(line.curvePoints[0]);

            List<Vector3> points = CalculateAllBezierPoints(line);
            foreach(var p in points)
            {
                Vector3 currentPoint = transform.TransformPoint(p);
                Gizmos.DrawLine(lastPoint, currentPoint);
                lastPoint = currentPoint;
            }

            // Dibujar los puntos de control
            Gizmos.color = Color.red;
            for (int j = 0; j < line.curvePoints.Length; j++)
            {
                Gizmos.DrawSphere(transform.TransformPoint(line.curvePoints[j]), 0.01f);
            }
        }
    }
}
