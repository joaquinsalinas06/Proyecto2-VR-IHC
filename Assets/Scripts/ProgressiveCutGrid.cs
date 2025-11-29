using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema de grilla progresiva para cortes realistas
/// El usuario debe seguir líneas de corte que aparecen una a una
/// </summary>
public class ProgressiveCutGrid : MonoBehaviour
{
    public enum CutLineType
    {
        Horizontal,
        Vertical
    }

    [System.Serializable]
    public class CutLine
    {
        [Header("Configuración")]
        public CutLineType type;
        public Vector3 localPosition;      // Posición local relativa al ingrediente

        [Range(0.01f, 0.5f)]
        public float length = 0.1f;        // Largo de la línea

        [Range(0.001f, 0.02f)]
        public float thickness = 0.003f;   // Grosor de la línea

        [Header("Progreso de Corte")]
        [Range(0f, 1f)]
        [Tooltip("Porcentaje requerido para completar (0.8 = 80%)")]
        public float requiredProgress = 0.8f;

        [HideInInspector] public GameObject lineObject;
        [HideInInspector] public GameObject colliderObject; // Collider para detectar cuchillo
        [HideInInspector] public bool isCompleted = false;
        [HideInInspector] public float currentProgress = 0f; // Progreso actual
    }

    [Header("Configuración de Grilla")]
    [Tooltip("Líneas de corte en orden (apareceran una a una)")]
    public CutLine[] cutLines;

    [Header("Visual de Líneas")]
    public Material lineMaterial;
    public Color lineColor = Color.white;
    public float lineEmissionIntensity = 2f;

    [Header("Detección de Corte")]
    [Tooltip("Distancia mínima del cuchillo a la línea para contar como corte")]
    public float cutDetectionRadius = 0.015f;

    [Tooltip("Tiempo que el cuchillo debe estar cerca para completar el corte")]
    public float cutHoldTime = 0.3f;

    [Header("Tiempo Mínimo de Corte (Seguro)")]
    [Tooltip("Tiempo mínimo que debe pasar cortando AUNQUE ya llegue al porcentaje requerido")]
    [Range(0.5f, 5f)]
    public float minimumCutTime = 1.5f;

    [Header("Audio")]
    public AudioClip lineCompleteSound;
    [Tooltip("Sonido de feedback cada vez que avanza el corte (opcional)")]
    public AudioClip cuttingProgressSound;
    [Range(0f, 1f)]
    [Tooltip("Volumen del sonido de progreso")]
    public float progressSoundVolume = 0.3f;

    private int currentLineIndex = 0;
    private bool isActive = false;
    private AudioSource audioSource;
    private float lastProgressSoundTime = 0f;
    private float lineStartTime = 0f; // Tiempo cuando empezó a cortar la línea actual
    private const float PROGRESS_SOUND_INTERVAL = 0.5f; // Reproducir sonido cada 0.5 segundos (menos molesto)

    // Evento cuando se completan todos los cortes
    public System.Action OnAllCutsCompleted;

    void Start()
    {
        // Configurar audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }

    /// <summary>
    /// Activar el sistema de grilla (llamar cuando el ingrediente se coloca en la tabla)
    /// </summary>
    public void Activate()
    {
        if (isActive) return;

        isActive = true;
        currentLineIndex = 0;

        // Mostrar la primera línea
        ShowCurrentLine();
    }

    /// <summary>
    /// Desactivar el sistema
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        HideAllLines();
    }

    void ShowCurrentLine()
    {
        if (currentLineIndex >= cutLines.Length)
            return;

        CutLine line = cutLines[currentLineIndex];

        // Reiniciar el temporizador de la línea
        lineStartTime = Time.time;
        line.currentProgress = 0f;

        // Crear objeto visual de la línea
        line.lineObject = CreateLineVisual(line);
    }

    GameObject CreateLineVisual(CutLine line)
    {
        // Crear cubo delgado como línea visual
        GameObject lineObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lineObj.name = $"CutLine_{currentLineIndex}_Visual";
        lineObj.transform.SetParent(transform);
        lineObj.transform.localPosition = line.localPosition;

        // Escalar según orientación
        if (line.type == CutLineType.Horizontal)
        {
            // Horizontal: largo en Z (hacia adelante/atrás), delgado en X y Y
            lineObj.transform.localScale = new Vector3(line.thickness, line.thickness, line.length);
        }
        else // Vertical
        {
            // Vertical: largo en X (izquierda/derecha), delgado en Y y Z
            lineObj.transform.localScale = new Vector3(line.length, line.thickness, line.thickness);
        }

        // Desactivar collider de la visual (no queremos que interactúe físicamente)
        Collider visualCol = lineObj.GetComponent<Collider>();
        if (visualCol != null)
        {
            Destroy(visualCol);
        }

        // COLOR ÚNICO para cada línea (para distinguirlas)
        Color uniqueColor = GetLineColor(currentLineIndex);

        // Aplicar material con efecto brillante/neon
        Renderer renderer = lineObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (lineMaterial != null)
            {
                renderer.material = new Material(lineMaterial);
                renderer.material.color = uniqueColor;
                renderer.material.SetColor("_EmissionColor", uniqueColor * lineEmissionIntensity);
            }
            else
            {
                // Material con emisión brillante tipo neon
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = uniqueColor;

                // Activar emisión con intensidad alta para efecto brillante
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", uniqueColor * lineEmissionIntensity);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

                // Hacer el material más brillante
                mat.SetFloat("_Metallic", 0.5f);
                mat.SetFloat("_Glossiness", 0.8f);

                renderer.material = mat;
            }
        }

        // CREAR COLLIDER SEPARADO para detección
        GameObject colliderObj = new GameObject($"CutLine_{currentLineIndex}_Collider");
        colliderObj.transform.SetParent(transform);
        colliderObj.transform.localPosition = line.localPosition;
        colliderObj.layer = gameObject.layer;

        // Agregar BoxCollider trigger
        BoxCollider col = colliderObj.AddComponent<BoxCollider>();
        col.isTrigger = true;

        // Hacer el collider más grande que la visual para mejor detección
        if (line.type == CutLineType.Horizontal)
        {
            // Horizontal: largo en Z, ancho en X e Y
            col.size = new Vector3(line.thickness * 5f, line.thickness * 5f, line.length);
        }
        else // Vertical
        {
            // Vertical: largo en X, ancho en Y y Z
            col.size = new Vector3(line.length, line.thickness * 5f, line.thickness * 5f);
        }

        // Agregar componente para identificar esta línea
        CutLineCollider lineCollider = colliderObj.AddComponent<CutLineCollider>();
        lineCollider.grid = this;
        lineCollider.lineIndex = currentLineIndex;

        // Guardar referencia
        line.colliderObject = colliderObj;

        return lineObj;
    }

    void Update()
    {
        if (!isActive || currentLineIndex >= cutLines.Length)
            return;

        // El sistema ahora usa colliders para detectar
        // Solo verificamos el progreso aquí
        CutLine currentLine = cutLines[currentLineIndex];

        // Verificar si alcanzó el progreso requerido Y ha pasado el tiempo mínimo
        float timeCutting = Time.time - lineStartTime;
        bool hasEnoughProgress = currentLine.currentProgress >= currentLine.requiredProgress;
        bool hasEnoughTime = timeCutting >= minimumCutTime;

        if (hasEnoughProgress && hasEnoughTime && !currentLine.isCompleted)
        {
            CompleteLine();
        }
    }

    /// <summary>
    /// Llamado por CutLineCollider cuando el cuchillo está cortando
    /// </summary>
    public void OnKnifeCutting(int lineIndex, Vector3 knifePosition)
    {
        if (lineIndex != currentLineIndex || !isActive)
            return;

        CutLine line = cutLines[lineIndex];

        // Incrementar progreso basado en tiempo
        float previousProgress = line.currentProgress;
        line.currentProgress += Time.deltaTime / cutHoldTime;
        line.currentProgress = Mathf.Clamp01(line.currentProgress);

        // Reproducir sonido de progreso si hay avance (solo si está asignado)
        if (cuttingProgressSound != null &&
            line.currentProgress > previousProgress &&
            Time.time - lastProgressSoundTime >= PROGRESS_SOUND_INTERVAL)
        {
            audioSource.PlayOneShot(cuttingProgressSound, progressSoundVolume);
            lastProgressSoundTime = Time.time;
        }
    }

    void CompleteLine()
    {
        if (currentLineIndex >= cutLines.Length)
            return;

        CutLine line = cutLines[currentLineIndex];
        line.isCompleted = true;

        Debug.Log($"[GRID] Línea {currentLineIndex + 1} completada! (Progreso: {line.currentProgress * 100f:F0}%)");

        // Destruir la línea visual y el collider
        if (line.lineObject != null)
        {
            Destroy(line.lineObject);
        }
        if (line.colliderObject != null)
        {
            Destroy(line.colliderObject);
        }

        // Sonido
        if (lineCompleteSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(lineCompleteSound, 0.5f);
        }

        // Avanzar a siguiente línea
        currentLineIndex++;

        if (currentLineIndex < cutLines.Length)
        {
            // Mostrar siguiente línea
            ShowCurrentLine();
        }
        else
        {
            // Todas las líneas completadas
            Debug.Log("[GRID] ¡Todas las líneas completadas! Generando cubos...");
            OnAllCutsCompleted?.Invoke();
            isActive = false;
        }

        // Resetear estado
    }

    void HideAllLines()
    {
        foreach (CutLine line in cutLines)
        {
            if (line.lineObject != null)
            {
                Destroy(line.lineObject);
            }
            if (line.colliderObject != null)
            {
                Destroy(line.colliderObject);
            }
        }
    }

    /// <summary>
    /// Obtener un color único para cada línea
    /// </summary>
    Color GetLineColor(int index)
    {
        Color[] colors = new Color[]
        {
            Color.red,      // Línea 0 - Roja
            Color.green,    // Línea 1 - Verde
            Color.blue,     // Línea 2 - Azul
            Color.yellow    // Línea 3 - Amarilla
        };

        if (index < colors.Length)
            return colors[index];

        // Si hay más líneas, generar color aleatorio pero consistente
        return Color.HSVToRGB((index * 0.25f) % 1f, 1f, 1f);
    }

    void OnDestroy()
    {
        HideAllLines();
    }

    // Debug visual - Se ve en el Editor
    void OnDrawGizmos()
    {
        // Siempre dibujar un cubo rojo en el centro para verificar que funciona
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.02f);

        if (cutLines == null || cutLines.Length == 0)
        {
            // Si no hay líneas, dibujar texto de ayuda
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.03f, "Sin líneas configuradas");
            #endif
            return;
        }

        for (int i = 0; i < cutLines.Length; i++)
        {
            CutLine line = cutLines[i];

            // Color según estado
            if (Application.isPlaying)
            {
                // En Play mode: Verde si completada, color único si activa, gris si pendiente
                if (line.isCompleted)
                    Gizmos.color = Color.green;
                else if (i == currentLineIndex && isActive)
                    Gizmos.color = GetLineColor(i);
                else
                    Gizmos.color = Color.gray;
            }
            else
            {
                // En Edit mode: USAR COLORES ÚNICOS para cada línea
                Gizmos.color = GetLineColor(i);
            }

            Vector3 worldPos = transform.TransformPoint(line.localPosition);

            // Dibujar línea gruesa (varias líneas paralelas para simular grosor)
            Vector3 start, end;

            if (line.type == CutLineType.Horizontal)
            {
                start = worldPos - transform.right * line.length / 2f;
                end = worldPos + transform.right * line.length / 2f;

                // Línea central gruesa
                Gizmos.DrawLine(start, end);

                // Líneas paralelas para simular grosor
                Vector3 offset = transform.up * line.thickness;
                Gizmos.DrawLine(start + offset, end + offset);
                Gizmos.DrawLine(start - offset, end - offset);
            }
            else // Vertical
            {
                start = worldPos - transform.forward * line.length / 2f;
                end = worldPos + transform.forward * line.length / 2f;

                // Línea central gruesa
                Gizmos.DrawLine(start, end);

                // Líneas paralelas para simular grosor
                Vector3 offset = transform.up * line.thickness;
                Gizmos.DrawLine(start + offset, end + offset);
                Gizmos.DrawLine(start - offset, end - offset);
            }

            // Dibujar número de línea y progreso
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // Mostrar número con nombre del color
                string[] colorNames = { "ROJA", "VERDE", "AZUL", "AMARILLA" };
                string colorName = i < colorNames.Length ? colorNames[i] : $"L{i + 1}";
                UnityEditor.Handles.Label(worldPos + Vector3.up * 0.01f, $"L{i + 1} ({colorName})");
            }
            else if (i == currentLineIndex && isActive)
            {
                // Mostrar progreso en play mode
                string progressText = $"L{i + 1}: {line.currentProgress * 100f:F0}%";
                UnityEditor.Handles.Label(worldPos + Vector3.up * 0.02f, progressText);
            }
            #endif

            // Dibujar collider en la línea actual (solo en play)
            if (Application.isPlaying && i == currentLineIndex && isActive && line.colliderObject != null)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Amarillo transparente
                BoxCollider col = line.colliderObject.GetComponent<BoxCollider>();
                if (col != null)
                {
                    Gizmos.matrix = line.colliderObject.transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(Vector3.zero, col.size);
                    Gizmos.matrix = Matrix4x4.identity;
                }
            }
        }
    }

    // Dibujar siempre seleccionado (más visible)
    void OnDrawGizmosSelected()
    {
        if (cutLines == null || cutLines.Length == 0)
            return;

        // Dibujar bounds del área completa
        Gizmos.color = Color.white;
        Vector3 center = transform.position;

        // Calcular bounds aproximados
        float maxLength = 0f;
        foreach (var line in cutLines)
        {
            maxLength = Mathf.Max(maxLength, line.length);
        }

        Gizmos.DrawWireCube(center, new Vector3(maxLength, 0.02f, maxLength));

        // Mostrar info de progreso en play mode
        #if UNITY_EDITOR
        if (Application.isPlaying && isActive)
        {
            string info = $"Progreso: {currentLineIndex}/{cutLines.Length} líneas";
            UnityEditor.Handles.Label(center + Vector3.up * 0.03f, info);
        }
        #endif
    }

    // Método público para verificar si está completo
    public bool IsComplete()
    {
        return currentLineIndex >= cutLines.Length;
    }

    // Progreso actual
    public float GetProgress()
    {
        if (cutLines.Length == 0) return 0f;
        return (float)currentLineIndex / cutLines.Length;
    }
}

/// <summary>
/// Componente auxiliar que detecta cuando el cuchillo toca la línea
/// </summary>
public class CutLineCollider : MonoBehaviour
{
    [HideInInspector] public ProgressiveCutGrid grid;
    [HideInInspector] public int lineIndex;

    private Vector3 lastKnifePosition;
    private float lastKnifeTime;
    private Transform knifeTransform;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Knife") || other.name.Contains("Blade"))
        {
            knifeTransform = other.transform;
            lastKnifePosition = other.transform.position;
            lastKnifeTime = Time.time;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Knife") || other.name.Contains("Blade"))
        {
            // Calcular velocidad manualmente usando cambio de posición
            float deltaTime = Time.time - lastKnifeTime;

            if (deltaTime > 0.001f) // Evitar división por cero
            {
                Vector3 deltaPosition = other.transform.position - lastKnifePosition;
                float manualVelocity = deltaPosition.magnitude / deltaTime;

                if (manualVelocity > 0.1f) // Velocidad mínima para contar como corte
                {
                    // Notificar al grid
                    if (grid != null)
                    {
                        grid.OnKnifeCutting(lineIndex, other.transform.position);
                    }
                }

                // Actualizar última posición
                lastKnifePosition = other.transform.position;
                lastKnifeTime = Time.time;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Knife") || other.name.Contains("Blade"))
        {
            Debug.Log($"[GRID COLLIDER] Cuchillo salió de línea {lineIndex + 1}");
        }
    }
}
