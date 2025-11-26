using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tipos de patrones de corte disponibles
/// </summary>
public enum CuttingPatternType
{
    Grid3x3,        // Pescado: grilla 3x3 (filas luego columnas)
    ConcentricArcs, // Cebolla: arcos concéntricos
    SingleLine,     // Limón: una línea por el centro
    MultiTouch      // Ají: múltiples puntos de toque
}

/// <summary>
/// Maneja el patrón de corte completo de un ingrediente.
/// Genera las líneas guía y controla la secuencia de corte.
/// </summary>
public class IngredientCuttingPattern : MonoBehaviour
{
    [Header("Configuración del Patrón")]
    [Tooltip("Tipo de patrón de corte para este ingrediente")]
    public CuttingPatternType patternType = CuttingPatternType.SingleLine;

    [Tooltip("Prefab de la línea de corte (debe tener CuttingLine component)")]
    public GameObject cuttingLinePrefab;

    [Header("Configuración Visual")]
    [Tooltip("Material para las líneas (con emisión)")]
    public Material lineMaterial;

    [Tooltip("Altura de las líneas sobre la superficie del ingrediente")]
    public float lineHeightOffset = 0.01f; // AUMENTADO para evitar que queden dentro del mesh

    [Header("Configuración por Patrón")]
    [Tooltip("Grid: espaciado entre líneas")]
    public float gridSpacing = 0.1f;

    [Tooltip("Arcos: número de arcos concéntricos")]
    public int arcCount = 4;

    [Tooltip("Arcos: radio base")]
    public float arcBaseRadius = 0.03f;

    [Tooltip("Multi-touch: número de puntos")]
    public int touchPointCount = 8;

    [Tooltip("Multi-touch: radio de los círculos")]
    public float touchCircleRadius = 0.01f;

    // Estado
    private List<CuttingLine> allLines = new List<CuttingLine>();
    private int currentLineIndex = 0;
    private bool isPatternComplete = false;
    private MeshRenderer ingredientRenderer;

    // Callback cuando se completa todo el patrón
    public System.Action<IngredientCuttingPattern> OnPatternCompleted;

    void Awake()
    {
        ingredientRenderer = GetComponent<MeshRenderer>();
    }

    void Start()
    {
        Debug.Log($"[PATTERN START] {gameObject.name} iniciado. Tipo: {patternType}, Prefab asignado: {(cuttingLinePrefab != null ? "SÍ" : "NO")}");

        if (cuttingLinePrefab == null)
        {
            Debug.LogError($"[PATTERN ERROR] {gameObject.name} NO TIENE cuttingLinePrefab asignado! Asigna el prefab en el Inspector.");
        }

        // NOTA: lineMaterial ya no es necesario - CuttingLine.cs crea su propio material Unlit/Color
        // if (lineMaterial == null)
        // {
        //     Debug.LogWarning($"[PATTERN WARNING] {gameObject.name} no tiene lineMaterial asignado. Las líneas no tendrán material.");
        // }
    }

    /// <summary>
    /// Genera el patrón de líneas para este ingrediente
    /// </summary>
    public void GeneratePattern()
    {
        // LOGS REDUCIDOS
        Debug.Log($"[PATTERN] Generando {patternType} para {gameObject.name}");

        if (cuttingLinePrefab == null)
        {
            Debug.LogError($"[PATTERN ERROR] cuttingLinePrefab es NULL en {gameObject.name}");
            return;
        }

        // Limpiar líneas anteriores si existen
        ClearPattern();

        switch (patternType)
        {
            case CuttingPatternType.Grid3x3:
                GenerateGrid3x3();
                break;
            case CuttingPatternType.ConcentricArcs:
                Debug.Log($"[PATTERN] → Generando ConcentricArcs...");
                GenerateConcentricArcs();
                break;
            case CuttingPatternType.SingleLine:
                Debug.Log($"[PATTERN] → Generando SingleLine...");
                GenerateSingleLine();
                break;
            case CuttingPatternType.MultiTouch:
                Debug.Log($"[PATTERN] → Generando MultiTouch...");
                GenerateMultiTouch();
                break;
        }

        Debug.Log($"[PATTERN] Total de líneas creadas: {allLines.Count}");

        if (allLines.Count == 0)
        {
            Debug.LogError($"[PATTERN ERROR] ¡No se crearon líneas! Revisa el método de generación para {patternType}");
            return;
        }

        // Configurar callbacks
        Debug.Log($"[PATTERN] Configurando callbacks para {allLines.Count} líneas...");
        foreach (CuttingLine line in allLines)
        {
            if (line != null)
            {
                line.OnLineCompleted += OnLineCompleted;
                Debug.Log($"[PATTERN]   - Callback configurado para: {line.name}");
            }
            else
            {
                Debug.LogError($"[PATTERN ERROR] Una de las líneas es NULL!");
            }
        }

        // Activar solo la primera línea con un delay para asegurar que Start() ya se ejecutó
        if (allLines.Count > 0)
        {
            currentLineIndex = 0;
            Debug.Log($"[PATTERN] Programando activación de primera línea: {allLines[currentLineIndex].name}");
            // Usar Invoke para activar después de que Start() termine
            StartCoroutine(ActivateFirstLineDelayed());
        }

        Debug.Log($"[PATTERN] ========== PATRÓN GENERADO EXITOSAMENTE ==========");
        Debug.Log($"[PATTERN] Total líneas: {allLines.Count}, Primera línea programada: {(allLines.Count > 0 ? allLines[0].name : "NINGUNA")}");
    }

    System.Collections.IEnumerator ActivateFirstLineDelayed()
    {
        // Esperar 1 frame para que Start() termine de ejecutarse en todas las líneas
        yield return null;

        Debug.Log($"[PATTERN] Activando primera línea AHORA: {allLines[currentLineIndex].name}");
        allLines[currentLineIndex].SetActive(true);
        Debug.Log($"[PATTERN] Primera línea activada: isActive={allLines[currentLineIndex].isActive}");
    }

    /// <summary>
    /// Genera grilla 3x3 para el pescado
    /// </summary>
    void GenerateGrid3x3()
    {
        Bounds bounds = GetIngredientBounds();
        Vector3 center = bounds.center;
        Vector3 size = bounds.size;

        // Calcular posiciones para 3 filas y 3 columnas
        float rowSpacing = size.z / 4f;  // Divide en 4 para crear 3 espacios
        float colSpacing = size.x / 4f;

        // Crear 3 filas horizontales (primero)
        for (int i = 0; i < 3; i++)
        {
            float zPos = center.z - size.z / 2f + rowSpacing * (i + 1);
            CreateHorizontalLine(
                new Vector3(center.x - size.x / 2f, center.y + lineHeightOffset, zPos),
                new Vector3(center.x + size.x / 2f, center.y + lineHeightOffset, zPos),
                $"Row_{i + 1}"
            );
        }

        // Crear 3 columnas verticales (después)
        for (int i = 0; i < 3; i++)
        {
            float xPos = center.x - size.x / 2f + colSpacing * (i + 1);
            CreateVerticalLine(
                new Vector3(xPos, center.y + lineHeightOffset, center.z - size.z / 2f),
                new Vector3(xPos, center.y + lineHeightOffset, center.z + size.z / 2f),
                $"Col_{i + 1}"
            );
        }
    }

    /// <summary>
    /// Genera arcos concéntricos para la cebolla
    /// </summary>
    void GenerateConcentricArcs()
    {
        Bounds bounds = GetIngredientBounds();
        Vector3 center = bounds.center;
        float maxRadius = Mathf.Min(bounds.extents.x, bounds.extents.z);

        for (int i = 0; i < arcCount; i++)
        {
            // Radio va desde el centro hacia afuera
            float radius = arcBaseRadius + (maxRadius / arcCount) * i;
            CreateArcLine(center, radius, $"Arc_{i + 1}");
        }
    }

    /// <summary>
    /// Genera una línea central para el limón
    /// </summary>
    void GenerateSingleLine()
    {
        Bounds bounds = GetIngredientBounds();
        Vector3 center = bounds.center;
        Vector3 size = bounds.size;

        // Línea vertical por el centro (de adelante hacia atrás)
        CreateVerticalLine(
            new Vector3(center.x, center.y + lineHeightOffset, center.z - size.z / 2f),
            new Vector3(center.x, center.y + lineHeightOffset, center.z + size.z / 2f),
            "CenterLine"
        );
    }

    /// <summary>
    /// Genera puntos de toque múltiples para el ají
    /// </summary>
    void GenerateMultiTouch()
    {
        Bounds bounds = GetIngredientBounds();
        Vector3 center = bounds.center;
        Vector3 size = bounds.size;

        // Generar puntos en posiciones específicas (no aleatorio para ser consistente)
        for (int i = 0; i < touchPointCount; i++)
        {
            // Distribuir en patrón semi-aleatorio pero determinístico
            float angle = (360f / touchPointCount) * i;
            float radiusRatio = (i % 2 == 0) ? 0.3f : 0.6f;

            float xOffset = Mathf.Cos(angle * Mathf.Deg2Rad) * size.x * radiusRatio;
            float zOffset = Mathf.Sin(angle * Mathf.Deg2Rad) * size.z * radiusRatio;

            Vector3 pointCenter = new Vector3(
                center.x + xOffset,
                center.y + lineHeightOffset,
                center.z + zOffset
            );

            CreateCircleLine(pointCenter, touchCircleRadius, $"Touch_{i + 1}");
        }
    }

    /// <summary>
    /// Crea una línea horizontal
    /// </summary>
    void CreateHorizontalLine(Vector3 start, Vector3 end, string lineName)
    {
        // LOGS REDUCIDOS para evitar truncamiento
        // Debug.Log($"[PATTERN CREATE] Creando línea horizontal: {lineName}");

        GameObject lineObj = Instantiate(cuttingLinePrefab, transform);
        lineObj.name = lineName;

        LineRenderer lr = lineObj.GetComponent<LineRenderer>();
        if (lr == null)
        {
            lr = lineObj.AddComponent<LineRenderer>();
        }

        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.useWorldSpace = false;

        // NOTA: Material ahora se configura en CuttingLine.Awake() con Unlit/Color
        // No asignar material aquí para evitar conflictos
        // if (lineMaterial != null)
        // {
        //     lr.material = lineMaterial;
        // }

        // Agregar collider para detección MÁS GRANDE
        BoxCollider col = lineObj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = (start + end) / 2f;
        col.size = new Vector3(Vector3.Distance(start, end), 0.05f, 0.05f); // Collider más grande = más fácil de tocar

        CuttingLine cuttingLine = lineObj.GetComponent<CuttingLine>();
        if (cuttingLine != null)
        {
            cuttingLine.lineRenderer = lr;

            // FORZAR valores correctos (en caso de que el prefab tenga valores viejos cacheados)
            cuttingLine.baseWidth = 0.1f; // ULTRA GRUESO - 100mm para visibilidad
            lr.widthCurve = AnimationCurve.Constant(0, 1, 1); // CRÍTICO: Curva constante
            lr.startWidth = 0.1f; // ULTRA GRUESO
            lr.endWidth = 0.1f; // ULTRA GRUESO
        }
        else
        {
            Debug.LogError($"[PATTERN CREATE ERROR] ¡Prefab no tiene componente CuttingLine!");
        }

        allLines.Add(cuttingLine);
        Debug.Log($"[PATTERN] ✓ {lineName} creada. Total: {allLines.Count}");
    }

    /// <summary>
    /// Crea una línea vertical
    /// </summary>
    void CreateVerticalLine(Vector3 start, Vector3 end, string lineName)
    {
        GameObject lineObj = Instantiate(cuttingLinePrefab, transform);
        lineObj.name = lineName;

        LineRenderer lr = lineObj.GetComponent<LineRenderer>();
        if (lr == null)
        {
            lr = lineObj.AddComponent<LineRenderer>();
        }

        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.useWorldSpace = false;

        // NOTA: Material ahora se configura en CuttingLine.Awake() con Unlit/Color
        // No asignar material aquí para evitar conflictos
        // if (lineMaterial != null)
        // {
        //     lr.material = lineMaterial;
        // }

        // Agregar collider para detección
        BoxCollider col = lineObj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = (start + end) / 2f;
        col.size = new Vector3(0.05f, 0.05f, Vector3.Distance(start, end)); // Collider más grande

        CuttingLine cuttingLine = lineObj.GetComponent<CuttingLine>();
        if (cuttingLine != null)
        {
            cuttingLine.lineRenderer = lr;

            // FORZAR valores correctos
            cuttingLine.baseWidth = 0.1f; // ULTRA GRUESO - 100mm
            lr.widthCurve = AnimationCurve.Constant(0, 1, 1); // CRÍTICO: Curva constante
            lr.startWidth = 0.1f; // ULTRA GRUESO
            lr.endWidth = 0.1f; // ULTRA GRUESO
        }

        allLines.Add(cuttingLine);
    }

    /// <summary>
    /// Crea una línea en forma de arco
    /// </summary>
    void CreateArcLine(Vector3 center, float radius, string lineName)
    {
        GameObject lineObj = Instantiate(cuttingLinePrefab, transform);
        lineObj.name = lineName;

        LineRenderer lr = lineObj.GetComponent<LineRenderer>();
        if (lr == null)
        {
            lr = lineObj.AddComponent<LineRenderer>();
        }

        // Crear arco con múltiples segmentos
        int segments = 30;
        lr.positionCount = segments + 1;
        lr.useWorldSpace = false;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (180f / segments) * i; // Arco de 180 grados (media luna)
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            float z = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

            Vector3 point = new Vector3(center.x + x, center.y + lineHeightOffset, center.z + z);
            lr.SetPosition(i, point);
        }

        // NOTA: Material ahora se configura en CuttingLine.Awake() con Unlit/Color
        // No asignar material aquí para evitar conflictos
        // if (lineMaterial != null)
        // {
        //     lr.material = lineMaterial;
        // }

        // Agregar collider en forma de cápsula que sigue el arco
        CapsuleCollider col = lineObj.AddComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.radius = 0.015f;
        col.height = radius * 2f;
        col.center = center;
        col.direction = 2; // Z-axis

        CuttingLine cuttingLine = lineObj.GetComponent<CuttingLine>();
        if (cuttingLine != null)
        {
            cuttingLine.lineRenderer = lr;

            // FORZAR valores correctos
            cuttingLine.baseWidth = 0.1f; // ULTRA GRUESO - 100mm
            lr.widthCurve = AnimationCurve.Constant(0, 1, 1); // CRÍTICO: Curva constante
            lr.startWidth = 0.1f; // ULTRA GRUESO
            lr.endWidth = 0.1f; // ULTRA GRUESO
        }

        allLines.Add(cuttingLine);
    }

    /// <summary>
    /// Crea una línea en forma de círculo pequeño (para multi-touch)
    /// </summary>
    void CreateCircleLine(Vector3 center, float radius, string lineName)
    {
        GameObject lineObj = Instantiate(cuttingLinePrefab, transform);
        lineObj.name = lineName;

        LineRenderer lr = lineObj.GetComponent<LineRenderer>();
        if (lr == null)
        {
            lr = lineObj.AddComponent<LineRenderer>();
        }

        // Crear círculo con múltiples segmentos
        int segments = 16;
        lr.positionCount = segments + 1;
        lr.useWorldSpace = false;
        lr.loop = true;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (360f / segments) * i;
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            float z = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;

            Vector3 point = new Vector3(center.x + x, center.y, center.z + z);
            lr.SetPosition(i, point);
        }

        // NOTA: Material ahora se configura en CuttingLine.Awake() con Unlit/Color
        // No asignar material aquí para evitar conflictos
        // if (lineMaterial != null)
        // {
        //     lr.material = lineMaterial;
        // }

        // Agregar collider esférico
        SphereCollider col = lineObj.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = radius;
        col.center = center;

        CuttingLine cuttingLine = lineObj.GetComponent<CuttingLine>();
        if (cuttingLine != null)
        {
            cuttingLine.lineRenderer = lr;
            cuttingLine.requiredCutTime = 0.3f; // Los toques son más rápidos

            // FORZAR valores correctos
            cuttingLine.baseWidth = 0.1f; // ULTRA GRUESO - 100mm
            lr.widthCurve = AnimationCurve.Constant(0, 1, 1); // CRÍTICO: Curva constante
            lr.startWidth = 0.1f; // ULTRA GRUESO
            lr.endWidth = 0.1f; // ULTRA GRUESO
        }

        allLines.Add(cuttingLine);
    }

    /// <summary>
    /// Obtiene los bounds del ingrediente
    /// </summary>
    Bounds GetIngredientBounds()
    {
        if (ingredientRenderer != null)
        {
            return ingredientRenderer.bounds;
        }

        // Fallback: calcular bounds de todos los colliders
        Collider[] colliders = GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;
            foreach (Collider col in colliders)
            {
                bounds.Encapsulate(col.bounds);
            }
            return bounds;
        }

        // Fallback final
        return new Bounds(transform.position, Vector3.one * 0.1f);
    }

    /// <summary>
    /// Callback cuando una línea se completa
    /// </summary>
    void OnLineCompleted(CuttingLine completedLine)
    {
        Debug.Log($"[PATTERN] Línea completada: {completedLine.name}");

        currentLineIndex++;

        if (currentLineIndex < allLines.Count)
        {
            // Activar siguiente línea
            allLines[currentLineIndex].SetActive(true);
            Debug.Log($"[PATTERN] Siguiente línea activada: {allLines[currentLineIndex].name}");
        }
        else
        {
            // Todas las líneas completadas
            CompletePattern();
        }
    }

    /// <summary>
    /// Se completa todo el patrón
    /// </summary>
    void CompletePattern()
    {
        isPatternComplete = true;
        Debug.Log($"[PATTERN] ¡Patrón completo para {gameObject.name}!");

        // Notificar al CuttingBoardSnapZone
        OnPatternCompleted?.Invoke(this);

        // Limpiar líneas después de un momento
        StartCoroutine(ClearPatternAfterDelay(1f));
    }

    /// <summary>
    /// Limpia todas las líneas del patrón
    /// </summary>
    public void ClearPattern()
    {
        foreach (CuttingLine line in allLines)
        {
            if (line != null)
            {
                Destroy(line.gameObject);
            }
        }
        allLines.Clear();
        currentLineIndex = 0;
        isPatternComplete = false;
    }

    IEnumerator ClearPatternAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ClearPattern();
    }

    /// <summary>
    /// Resetea el patrón a su estado inicial
    /// </summary>
    public void ResetPattern()
    {
        ClearPattern();
        GeneratePattern();
    }

    public bool IsPatternComplete()
    {
        return isPatternComplete;
    }

    public int GetTotalLines()
    {
        return allLines.Count;
    }

    public int GetCompletedLines()
    {
        return currentLineIndex;
    }
}
