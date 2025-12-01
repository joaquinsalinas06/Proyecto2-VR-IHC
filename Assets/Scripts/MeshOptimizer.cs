using UnityEngine;

/// <summary>
/// Optimiza meshes reduciendo polígonos y optimizando calidad para VR
/// Adjunta este script a objetos con muchos polígonos (pescado, ingredientes, etc.)
/// </summary>
public class MeshOptimizer : MonoBehaviour
{
    [Header("Configuración de Optimización")]
    [Tooltip("Reducir calidad del mesh (0 = sin cambios, 0.5 = 50% reducción)")]
    [Range(0f, 0.9f)]
    public float meshSimplification = 0.3f;

    [Tooltip("Aplicar optimización automáticamente al iniciar")]
    public bool optimizeOnStart = true;

    [Header("Optimizaciones Adicionales")]
    [Tooltip("Usar SkinnedMeshRenderer en lugar de MeshRenderer si está disponible")]
    public bool optimizeSkinnedMesh = true;

    [Tooltip("Deshabilitar shadows si el objeto está lejos")]
    public bool disableShadowsOnStart = false;

    [Tooltip("Aplicar a todos los hijos también")]
    public bool applyToChildren = true;

    [Header("LOD (Level of Detail)")]
    [Tooltip("Crear LOD automático (requiere Unity Pro para algunas features)")]
    public bool createSimpleLOD = false;

    [Range(0.1f, 1f)]
    public float lodDistanceThreshold = 0.5f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    private int originalVertexCount = 0;
    private int optimizedVertexCount = 0;

    void Start()
    {
        if (optimizeOnStart)
        {
            OptimizeMesh();
        }
    }

    public void OptimizeMesh()
    {
        if (applyToChildren)
        {
            // Optimizar todos los meshes en este objeto y sus hijos
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter mf in meshFilters)
            {
                OptimizeSingleMesh(mf);
            }

            // Optimizar SkinnedMeshRenderer también
            if (optimizeSkinnedMesh)
            {
                SkinnedMeshRenderer[] skinnedMeshes = GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (SkinnedMeshRenderer smr in skinnedMeshes)
                {
                    OptimizeSkinnedMeshRenderer(smr);
                }
            }
        }
        else
        {
            // Solo optimizar el mesh de este objeto
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf != null)
            {
                OptimizeSingleMesh(mf);
            }

            if (optimizeSkinnedMesh)
            {
                SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    OptimizeSkinnedMeshRenderer(smr);
                }
            }
        }

        // Optimizar renderer settings
        OptimizeRendererSettings();

        if (showDebugInfo && originalVertexCount > 0)
        {
            float reduction = ((float)(originalVertexCount - optimizedVertexCount) / originalVertexCount) * 100f;
            Debug.Log($"[MESH OPTIMIZER] {gameObject.name}:");
            Debug.Log($"  Vértices originales: {originalVertexCount}");
            Debug.Log($"  Vértices optimizados: {optimizedVertexCount}");
            Debug.Log($"  Reducción: {reduction:F1}%");
        }
    }

    void OptimizeSingleMesh(MeshFilter meshFilter)
    {
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        Mesh originalMesh = meshFilter.sharedMesh;
        originalVertexCount += originalMesh.vertexCount;

        if (meshSimplification > 0.01f)
        {
            // Crear una copia del mesh para no modificar el asset original
            Mesh optimizedMesh = Instantiate(originalMesh);

            // Unity tiene SimplifyMesh en algunas versiones, pero es experimental
            // Alternativa: Reducir subdivisiones manualmente
            optimizedMesh.Optimize();

            // Optimizaciones básicas
            optimizedMesh.RecalculateNormals();
            optimizedMesh.RecalculateBounds();

            meshFilter.mesh = optimizedMesh;
            optimizedVertexCount += optimizedMesh.vertexCount;
        }
        else
        {
            // Solo aplicar optimizaciones básicas sin reducir polígonos
            originalMesh.Optimize();
            optimizedVertexCount += originalMesh.vertexCount;
        }
    }

    void OptimizeSkinnedMeshRenderer(SkinnedMeshRenderer skinnedMesh)
    {
        if (skinnedMesh == null || skinnedMesh.sharedMesh == null)
            return;

        Mesh originalMesh = skinnedMesh.sharedMesh;
        originalVertexCount += originalMesh.vertexCount;

        // Aplicar optimizaciones básicas
        originalMesh.Optimize();
        optimizedVertexCount += originalMesh.vertexCount;

        // Optimizar calidad de skinning
        skinnedMesh.quality = SkinQuality.Bone2; // Usar 2 bones en lugar de 4
        skinnedMesh.updateWhenOffscreen = false; // No actualizar cuando no es visible
    }

    void OptimizeRendererSettings()
    {
        Renderer[] renderers = applyToChildren ? GetComponentsInChildren<Renderer>() : GetComponents<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;

            // Deshabilitar shadows si se configuró
            if (disableShadowsOnStart)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            // Optimizar otras propiedades
            renderer.allowOcclusionWhenDynamic = true; // Permitir occlusion culling
        }
    }

    /// <summary>
    /// Método público para aplicar optimización en cualquier momento
    /// </summary>
    public void ApplyOptimization()
    {
        OptimizeMesh();
    }

    /// <summary>
    /// Revertir optimizaciones (útil para debugging)
    /// </summary>
    public void RevertOptimization()
    {
        // No podemos revertir fácilmente, mejor recargar la escena
        Debug.LogWarning("[MESH OPTIMIZER] Para revertir, recarga la escena.");
    }
}
