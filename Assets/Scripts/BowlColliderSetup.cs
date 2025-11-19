using UnityEngine;

/// <summary>
/// Script para crear colliders de bowl con dos zonas:
/// 1. Borde superior para agarrar
/// 2. Interior para física de ingredientes
/// </summary>
public class BowlColliderSetup : MonoBehaviour
{
    [Header("Configuración del Bowl")]
    [Tooltip("Radio del bowl en el borde superior (distancia del centro al borde)")]
    public float radioBowlSuperior = 0.20f;

    [Tooltip("Profundidad total del bowl (desde el borde hasta el fondo)")]
    public float profundidadBowl = 0.15f;

    [Tooltip("Número de segmentos alrededor del bowl (más = más preciso)")]
    [Range(8, 32)]
    public int segmentosCirculares = 16;

    [Tooltip("Número de niveles verticales (más = más preciso)")]
    [Range(3, 8)]
    public int segmentosVerticales = 5;

    [Header("Configuración del Borde (Agarre)")]
    [Tooltip("Radio del collider del borde (debe ser similar al radio superior del bowl)")]
    public float radioBorde = 0.20f;

    [Tooltip("Grosor del collider del borde")]
    public float grosorBorde = 0.05f;

    [Tooltip("Altura del borde respecto al centro del bowl (normalmente 0 o positivo)")]
    public float alturaBorde = 0.02f;

    [Header("Material de Física")]
    [Tooltip("Fricción dinámica (0-1)")]
    [Range(0f, 1f)]
    public float friccionDinamica = 0.6f;

    [Tooltip("Fricción estática (0-1)")]
    [Range(0f, 1f)]
    public float friccionEstatica = 0.6f;

    [Tooltip("Rebote (0-1)")]
    [Range(0f, 1f)]
    public float rebote = 0.1f;

    [Header("Visualización (Debug)")]
    [Tooltip("Mostrar gizmos de los colliders en el editor")]
    public bool mostrarGizmos = true;

    [Tooltip("Color de los gizmos del borde")]
    public Color colorBorde = Color.green;

    [Tooltip("Color de los gizmos del interior")]
    public Color colorInterior = Color.blue;

    // Referencias a los GameObjects creados
    private GameObject bordeObj;
    private GameObject interiorObj;

    /// <summary>
    /// Llama a este método desde el Inspector para crear los colliders
    /// </summary>
    [ContextMenu("Crear Colliders del Bowl")]
    public void CrearColliders()
    {
        // Limpiar colliders anteriores si existen
        LimpiarColliders();

        // Crear collider del borde
        CrearColliderBorde();

        // Crear colliders del interior
        CrearColliderInterior();

        Debug.Log($"Bowl colliders creados: Borde + {segmentosCirculares * segmentosVerticales + 1} colliders de interior");
    }

    /// <summary>
    /// Elimina los colliders existentes
    /// </summary>
    [ContextMenu("Limpiar Colliders")]
    public void LimpiarColliders()
    {
        // Buscar y eliminar objetos hijos creados anteriormente
        Transform bordeTransform = transform.Find("BowlBorde");
        if (bordeTransform != null)
        {
            DestroyImmediate(bordeTransform.gameObject);
        }

        Transform interiorTransform = transform.Find("BowlInterior");
        if (interiorTransform != null)
        {
            DestroyImmediate(interiorTransform.gameObject);
        }

        Debug.Log("Colliders del bowl eliminados");
    }

    /// <summary>
    /// Crea el collider circular del borde para agarrar
    /// </summary>
    private void CrearColliderBorde()
    {
        bordeObj = new GameObject("BowlBorde");
        bordeObj.transform.SetParent(transform);
        bordeObj.transform.localPosition = Vector3.zero;
        bordeObj.transform.localRotation = Quaternion.identity;
        bordeObj.transform.localScale = Vector3.one;

        // Añadir capsule collider para el borde
        CapsuleCollider capsule = bordeObj.AddComponent<CapsuleCollider>();
        capsule.direction = 2; // Eje Z (orientación horizontal)
        capsule.radius = radioBorde;
        capsule.height = grosorBorde;
        capsule.center = new Vector3(0, alturaBorde, 0);

        // Layer para agarre (opcional, ajusta según tu proyecto)
        // bordeObj.layer = LayerMask.NameToLayer("Interactable");
    }

    /// <summary>
    /// Crea múltiples box colliders formando el interior del bowl
    /// </summary>
    private void CrearColliderInterior()
    {
        interiorObj = new GameObject("BowlInterior");
        interiorObj.transform.SetParent(transform);
        interiorObj.transform.localPosition = Vector3.zero;
        interiorObj.transform.localRotation = Quaternion.identity;
        interiorObj.transform.localScale = Vector3.one;

        // Crear material de física
        PhysicMaterial physicsMat = new PhysicMaterial("BowlMaterial");
        physicsMat.dynamicFriction = friccionDinamica;
        physicsMat.staticFriction = friccionEstatica;
        physicsMat.bounciness = rebote;

        // Crear anillos de box colliders formando la pared del bowl
        // Empezamos desde el borde superior (altura 0) y bajamos
        for (int ring = 0; ring <= segmentosVerticales; ring++)
        {
            // Normalizado de 0 (arriba) a 1 (abajo)
            float alturaNormalizada = (float)ring / segmentosVerticales;

            // Cálculo para crear forma de bowl hemisférico
            // En el borde superior: radio máximo, altura = 0
            // En el fondo: radio mínimo, altura = -profundidad
            float anguloCurvatura = alturaNormalizada * Mathf.PI * 0.5f;

            // Radio decrece desde radioBowlSuperior hasta casi 0
            float radioActual = radioBowlSuperior * Mathf.Cos(anguloCurvatura);

            // Altura va desde 0 (arriba) hasta -profundidadBowl (abajo)
            float altura = -profundidadBowl * Mathf.Sin(anguloCurvatura);

            for (int seg = 0; seg < segmentosCirculares; seg++)
            {
                GameObject pared = new GameObject($"Pared_R{ring}_S{seg}");
                pared.transform.SetParent(interiorObj.transform);

                // Posición en círculo
                float anguloSegmento = (seg * 360f / segmentosCirculares) * Mathf.Deg2Rad;
                float x = Mathf.Cos(anguloSegmento) * radioActual;
                float z = Mathf.Sin(anguloSegmento) * radioActual;

                pared.transform.localPosition = new Vector3(x, altura, z);

                // Rotar hacia el centro del bowl
                Vector3 centroInterior = interiorObj.transform.position + Vector3.up * altura;
                pared.transform.LookAt(centroInterior);

                // Inclinar según la curvatura del bowl
                // Más vertical arriba (60°), más horizontal abajo (85°)
                float inclinacion = 60 + (alturaNormalizada * 25);
                pared.transform.Rotate(inclinacion, 0, 0);

                // Añadir box collider
                BoxCollider box = pared.AddComponent<BoxCollider>();
                float anchoPared = (2 * Mathf.PI * radioActual) / segmentosCirculares;
                box.size = new Vector3(anchoPared * 1.2f, profundidadBowl / segmentosVerticales * 1.5f, 0.01f);
                box.material = physicsMat;
            }
        }

        // Crear fondo del bowl (más pequeño)
        GameObject fondo = new GameObject("Fondo");
        fondo.transform.SetParent(interiorObj.transform);
        fondo.transform.localPosition = new Vector3(0, -profundidadBowl, 0);
        fondo.transform.localRotation = Quaternion.identity;

        BoxCollider fondoBox = fondo.AddComponent<BoxCollider>();
        float radioFondo = radioBowlSuperior * 0.3f; // Fondo más pequeño
        fondoBox.size = new Vector3(radioFondo, 0.01f, radioFondo);
        fondoBox.material = physicsMat;

        // Layer para física (opcional)
        // interiorObj.layer = LayerMask.NameToLayer("Container");
    }

    /// <summary>
    /// Dibuja gizmos en el editor para visualizar los colliders
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!mostrarGizmos) return;

        // Dibujar borde
        Gizmos.color = colorBorde;
        DrawWireTorus(transform.position + Vector3.up * alturaBorde, transform.rotation, radioBorde, grosorBorde / 2);

        // Dibujar interior
        Gizmos.color = colorInterior;

        // Dibujar líneas de la curvatura del bowl desde el borde superior hasta el fondo
        int numLineas = 16;
        for (int i = 0; i < numLineas; i++)
        {
            float angulo = (i * 360f / numLineas) * Mathf.Deg2Rad;
            Vector3 direccion = new Vector3(Mathf.Cos(angulo), 0, Mathf.Sin(angulo));

            Vector3 puntoAnterior = Vector3.zero;
            for (int j = 0; j <= 20; j++)
            {
                float t = j / 20f;
                float anguloCurva = t * Mathf.PI * 0.5f;

                // Radio desde radioBowlSuperior en el borde hasta 0 en el fondo
                float r = radioBowlSuperior * Mathf.Cos(anguloCurva);
                // Altura desde 0 en el borde hasta -profundidadBowl en el fondo
                float h = -profundidadBowl * Mathf.Sin(anguloCurva);

                Vector3 punto = transform.position + direccion * r + Vector3.up * h;

                if (j > 0)
                {
                    Gizmos.DrawLine(puntoAnterior, punto);
                }
                puntoAnterior = punto;
            }
        }

        // Dibujar círculo del borde superior
        Gizmos.color = Color.yellow;
        DrawWireCircle(transform.position, radioBowlSuperior);

        // Dibujar círculo del fondo
        DrawWireCircle(transform.position + Vector3.down * profundidadBowl, radioBowlSuperior * 0.3f);
    }

    /// <summary>
    /// Dibuja un círculo horizontal
    /// </summary>
    private void DrawWireCircle(Vector3 center, float radius)
    {
        int segments = 32;
        Vector3[] points = new Vector3[segments];

        for (int i = 0; i < segments; i++)
        {
            float angle = (i * 360f / segments) * Mathf.Deg2Rad;
            points[i] = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
        }

        for (int i = 0; i < segments; i++)
        {
            Gizmos.DrawLine(points[i], points[(i + 1) % segments]);
        }
    }

    /// <summary>
    /// Dibuja un toro (dona) para visualizar el borde
    /// </summary>
    private void DrawWireTorus(Vector3 position, Quaternion rotation, float radius, float thickness)
    {
        int segments = 32;
        Vector3[] points = new Vector3[segments];

        for (int i = 0; i < segments; i++)
        {
            float angle = (i * 360f / segments) * Mathf.Deg2Rad;
            points[i] = position + rotation * new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
        }

        for (int i = 0; i < segments; i++)
        {
            Gizmos.DrawLine(points[i], points[(i + 1) % segments]);
        }
    }
}
