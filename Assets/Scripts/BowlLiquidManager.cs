using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestiona el líquido dentro del bowl con mesh procedural y animación de nivel
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BowlLiquidManager : MonoBehaviour
{
    [Header("Configuración del Bowl")]
    [Tooltip("Radio del bowl (debe coincidir con BowlColliderSetup)")]
    public float radioBowl = 0.20f;

    [Tooltip("Profundidad del bowl (debe coincidir con BowlColliderSetup)")]
    public float profundidadBowl = 0.15f;

    [Header("Configuración del Líquido")]
    [Tooltip("Nivel actual de líquido (0-100)")]
    [Range(0f, 100f)]
    public float nivelLiquido = 0f;

    [Tooltip("Nivel máximo de líquido antes de desbordarse")]
    [Range(0f, 100f)]
    public float nivelMaximo = 100f;

    [Tooltip("Velocidad de animación del nivel de líquido")]
    [Range(0.1f, 10f)]
    public float velocidadAnimacion = 2f;

    [Tooltip("Color base del líquido")]
    public Color colorLiquido = new Color(1f, 1f, 0f, 0.8f); // Amarillo brillante vistoso

    [Header("Configuración Visual")]
    [Tooltip("Material del líquido")]
    public Material materialLiquido;

    [Tooltip("Resolución del mesh circular (más = más suave)")]
    [Range(8, 64)]
    public int resolucionCircular = 32;

    [Tooltip("Offset vertical del líquido respecto al fondo del bowl")]
    public float offsetVertical = 0.01f;

    [Header("Efectos de Mezcla")]
    [Tooltip("Activar mezcla de colores cuando se agregan ingredientes")]
    public bool mezclarColores = true;

    [Tooltip("Velocidad de transición de color")]
    [Range(0.1f, 5f)]
    public float velocidadMezclaColor = 1f;

    [Header("Ondas (Opcional)")]
    [Tooltip("Activar efecto de ondas al agregar líquido")]
    public bool activarOndas = true;

    [Tooltip("Amplitud de las ondas")]
    [Range(0f, 0.05f)]
    public float amplitudOndas = 0.01f;

    [Tooltip("Frecuencia de las ondas")]
    [Range(0f, 10f)]
    public float frecuenciaOndas = 5f;

    // Estado interno
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh meshLiquido;
    
    private float nivelActual = 0f;
    private Color colorActual;
    private Color colorObjetivo;
    
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;
    
    private float tiempoOnda = 0f;
    private bool hayOndasActivas = false;

    // Historial de ingredientes para mezcla de colores
    private List<ColorIngrediente> ingredientes = new List<ColorIngrediente>();

    private struct ColorIngrediente
    {
        public Color color;
        public float cantidad;
    }

    void Start()
    {
        DetectarTamanoBowl(); // Auto-detectar tamaño del bowl
        InicializarComponentes();
        CrearMeshLiquido();
        colorActual = colorLiquido;
        colorObjetivo = colorLiquido;
    }

    /// <summary>
    /// Detecta automáticamente el radio y profundidad del bowl desde sus colliders o mesh
    /// </summary>
    void DetectarTamanoBowl()
    {
        // Intentar obtener del padre (el bowl)
        Transform bowlTransform = transform.parent;
        if (bowlTransform == null)
        {
            Debug.LogWarning("[BOWL LIQUID] No hay objeto padre (Bowl). Usando valores por defecto.");
            return;
        }

        // Opción 1: Buscar BowlColliderSetup en el padre
        BowlColliderSetup bowlSetup = bowlTransform.GetComponent<BowlColliderSetup>();
        if (bowlSetup != null)
        {
            radioBowl = bowlSetup.radioBowlSuperior;
            profundidadBowl = bowlSetup.profundidadBowl;
            Debug.Log($"[BOWL LIQUID] ✓ Tamaño detectado desde BowlColliderSetup: Radio={radioBowl}, Profundidad={profundidadBowl}");
            return;
        }

        // Opción 2: Calcular desde colliders del bowl
        Collider[] colliders = bowlTransform.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;
            foreach (var col in colliders)
            {
                bounds.Encapsulate(col.bounds);
            }
            
            radioBowl = Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f;
            profundidadBowl = bounds.size.y * 0.8f; // 80% de la altura
            Debug.Log($"[BOWL LIQUID] ✓ Tamaño calculado desde colliders: Radio={radioBowl:F2}, Profundidad={profundidadBowl:F2}");
            return;
        }

        // Opción 3: Calcular desde mesh del bowl
        MeshFilter bowlMesh = bowlTransform.GetComponentInChildren<MeshFilter>();
        if (bowlMesh != null)
        {
            Bounds bounds = bowlMesh.sharedMesh.bounds;
            radioBowl = Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f;
            profundidadBowl = bounds.size.y * 0.8f;
            Debug.Log($"[BOWL LIQUID] ✓ Tamaño calculado desde mesh: Radio={radioBowl:F2}, Profundidad={profundidadBowl:F2}");
            return;
        }

        Debug.LogWarning("[BOWL LIQUID] No se pudo detectar tamaño del bowl. Usando valores por defecto.");
    }

    void InicializarComponentes()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        // Crear material si no existe
        if (materialLiquido == null)
        {
            materialLiquido = new Material(Shader.Find("Standard"));
            materialLiquido.SetFloat("_Mode", 3); // Transparent
            materialLiquido.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            materialLiquido.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            materialLiquido.SetInt("_ZWrite", 0);
            materialLiquido.DisableKeyword("_ALPHATEST_ON");
            materialLiquido.EnableKeyword("_ALPHABLEND_ON");
            materialLiquido.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            materialLiquido.renderQueue = 3000;
            materialLiquido.SetFloat("_Smoothness", 0.8f);
            materialLiquido.SetFloat("_Metallic", 0.1f);
        }

        meshRenderer.material = materialLiquido;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void CrearMeshLiquido()
    {
        meshLiquido = new Mesh();
        meshLiquido.name = "Liquid Mesh";

        // Crear vértices en círculo + centro
        int numVertices = resolucionCircular + 1; // +1 para el centro
        vertices = new Vector3[numVertices];
        uvs = new Vector2[numVertices];

        // Centro
        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        // Vértices del borde circular
        for (int i = 0; i < resolucionCircular; i++)
        {
            float angulo = (i * 360f / resolucionCircular) * Mathf.Deg2Rad;
            float x = Mathf.Cos(angulo) * radioBowl;
            float z = Mathf.Sin(angulo) * radioBowl;

            vertices[i + 1] = new Vector3(x, 0, z);
            uvs[i + 1] = new Vector2(
                0.5f + Mathf.Cos(angulo) * 0.5f,
                0.5f + Mathf.Sin(angulo) * 0.5f
            );
        }

        // Crear triángulos
        triangles = new int[resolucionCircular * 3];
        for (int i = 0; i < resolucionCircular; i++)
        {
            int triIndex = i * 3;
            triangles[triIndex] = 0; // Centro
            triangles[triIndex + 1] = i + 1;
            triangles[triIndex + 2] = ((i + 1) % resolucionCircular) + 1;
        }

        meshLiquido.vertices = vertices;
        meshLiquido.triangles = triangles;
        meshLiquido.uv = uvs;
        meshLiquido.RecalculateNormals();
        meshLiquido.RecalculateBounds();

        meshFilter.mesh = meshLiquido;
    }

    void Update()
    {
        ActualizarNivelLiquido();
        ActualizarColor();
        ActualizarOndas();
    }

    void ActualizarNivelLiquido()
    {
        // Animar nivel de líquido
        nivelActual = Mathf.MoveTowards(nivelActual, nivelLiquido, velocidadAnimacion * Time.deltaTime);

        // Calcular altura basada en el nivel (0-100 -> 0 a profundidadBowl)
        float alturaLiquido = -profundidadBowl + (nivelActual / nivelMaximo) * profundidadBowl + offsetVertical;

        // Actualizar posición del mesh
        transform.localPosition = new Vector3(0, alturaLiquido, 0);

        // Mostrar/ocultar mesh según el nivel
        meshRenderer.enabled = nivelActual > 0.1f;
    }

    void ActualizarColor()
    {
        if (mezclarColores)
        {
            colorActual = Color.Lerp(colorActual, colorObjetivo, velocidadMezclaColor * Time.deltaTime);
            materialLiquido.color = colorActual;
        }
        else
        {
            materialLiquido.color = colorLiquido;
        }
    }

    void ActualizarOndas()
    {
        if (!activarOndas || !hayOndasActivas) return;

        tiempoOnda += Time.deltaTime * frecuenciaOndas;

        // Aplicar ondas a los vértices del borde
        for (int i = 1; i < vertices.Length; i++)
        {
            float angulo = (i * 360f / resolucionCircular) * Mathf.Deg2Rad;
            float onda = Mathf.Sin(tiempoOnda + angulo * 2f) * amplitudOndas;
            
            Vector3 verticeOriginal = vertices[i];
            verticeOriginal.y = onda;
            vertices[i] = verticeOriginal;
        }

        meshLiquido.vertices = vertices;
        meshLiquido.RecalculateNormals();

        // Detener ondas después de un tiempo
        if (tiempoOnda > Mathf.PI * 4)
        {
            hayOndasActivas = false;
            tiempoOnda = 0f;
            
            // Restaurar vértices planos
            for (int i = 1; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                v.y = 0;
                vertices[i] = v;
            }
            meshLiquido.vertices = vertices;
            meshLiquido.RecalculateNormals();
        }
    }

    /// <summary>
    /// Agrega líquido al bowl
    /// </summary>
    /// <param name="cantidad">Cantidad a agregar (0-100)</param>
    /// <param name="color">Color del líquido agregado</param>
    public void AgregarLiquido(float cantidad, Color color)
    {
        if (nivelLiquido >= nivelMaximo)
        {
            Debug.LogWarning("[BOWL LIQUID] Bowl lleno!");
            return;
        }

        // Agregar cantidad
        nivelLiquido = Mathf.Min(nivelLiquido + cantidad, nivelMaximo);

        // Mezclar color
        if (mezclarColores)
        {
            ingredientes.Add(new ColorIngrediente { color = color, cantidad = cantidad });
            RecalcularColorMezcla();
        }

        // Activar ondas
        if (activarOndas)
        {
            hayOndasActivas = true;
            tiempoOnda = 0f;
        }

        Debug.Log($"[BOWL LIQUID] Agregado {cantidad:F1} de líquido. Nivel: {nivelLiquido:F1}/{nivelMaximo}");
    }

    /// <summary>
    /// Vacía completamente el bowl
    /// </summary>
    public void VaciarBowl()
    {
        nivelLiquido = 0f;
        ingredientes.Clear();
        colorObjetivo = colorLiquido;
        Debug.Log("[BOWL LIQUID] Bowl vaciado");
    }

    /// <summary>
    /// Establece el nivel de líquido directamente
    /// </summary>
    public void EstablecerNivel(float nivel)
    {
        nivelLiquido = Mathf.Clamp(nivel, 0f, nivelMaximo);
    }

    void RecalcularColorMezcla()
    {
        if (ingredientes.Count == 0)
        {
            colorObjetivo = colorLiquido;
            return;
        }

        // Mezcla ponderada de colores
        Color mezcla = Color.clear;
        float cantidadTotal = 0f;

        foreach (var ingrediente in ingredientes)
        {
            mezcla += ingrediente.color * ingrediente.cantidad;
            cantidadTotal += ingrediente.cantidad;
        }

        if (cantidadTotal > 0)
        {
            colorObjetivo = mezcla / cantidadTotal;
            // Mantener algo de transparencia
            colorObjetivo.a = Mathf.Clamp(colorObjetivo.a, 0.5f, 0.9f);
        }
    }

    // Getters públicos
    public float ObtenerNivelActual() => nivelActual;
    public float ObtenerPorcentajeLlenado() => nivelActual / nivelMaximo;
    public bool EstaLleno() => nivelLiquido >= nivelMaximo;
    public bool EstaVacio() => nivelLiquido <= 0.1f;

    // Visualización en editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 0.5f, 1f, 0.3f);
        
        // Dibujar círculo del líquido
        int segments = 32;
        Vector3 centro = transform.position;
        
        for (int i = 0; i < segments; i++)
        {
            float angulo1 = (i * 360f / segments) * Mathf.Deg2Rad;
            float angulo2 = ((i + 1) * 360f / segments) * Mathf.Deg2Rad;
            
            Vector3 p1 = centro + new Vector3(Mathf.Cos(angulo1) * radioBowl, 0, Mathf.Sin(angulo1) * radioBowl);
            Vector3 p2 = centro + new Vector3(Mathf.Cos(angulo2) * radioBowl, 0, Mathf.Sin(angulo2) * radioBowl);
            
            Gizmos.DrawLine(p1, p2);
        }

        // Dibujar nivel actual si está en play mode
        if (Application.isPlaying && nivelActual > 0)
        {
            Gizmos.color = Color.cyan;
            float altura = -profundidadBowl + (nivelActual / nivelMaximo) * profundidadBowl;
            Vector3 centroNivel = transform.parent.position + Vector3.up * altura;
            
            for (int i = 0; i < segments; i++)
            {
                float angulo1 = (i * 360f / segments) * Mathf.Deg2Rad;
                float angulo2 = ((i + 1) * 360f / segments) * Mathf.Deg2Rad;
                
                Vector3 p1 = centroNivel + new Vector3(Mathf.Cos(angulo1) * radioBowl, 0, Mathf.Sin(angulo1) * radioBowl);
                Vector3 p2 = centroNivel + new Vector3(Mathf.Cos(angulo2) * radioBowl, 0, Mathf.Sin(angulo2) * radioBowl);
                
                Gizmos.DrawLine(p1, p2);
            }
        }
    }

    void OnDestroy()
    {
        if (meshLiquido != null)
        {
            Destroy(meshLiquido);
        }
    }
}
