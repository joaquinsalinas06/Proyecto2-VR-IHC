using UnityEngine;

/// <summary>
/// Mantiene objetos dentro del bowl usando física mejorada
/// </summary>
public class BowlPhysicsContainer : MonoBehaviour
{
    [Header("Configuración del Bowl")]
    [Tooltip("Radio del bowl")]
    public float radioBowl = 0.20f;

    [Tooltip("Profundidad del bowl")]
    public float profundidadBowl = 0.15f;

    [Tooltip("Fuerza de contención (qué tan fuerte empuja hacia adentro)")]
    [Range(1f, 100f)]
    public float fuerzaContencion = 20f;

    [Tooltip("Tag de objetos que deben quedarse en el bowl")]
    public string tagIngredientes = "Ingredient";

    [Header("Debug")]
    public bool mostrarGizmos = true;

    private Vector3 centroBowl;

    void Start()
    {
        // Centro del bowl está en la parte superior
        centroBowl = transform.position;
    }

    void FixedUpdate()
    {
        // Buscar todos los objetos con el tag
        GameObject[] ingredientes = GameObject.FindGameObjectsWithTag(tagIngredientes);

        foreach (GameObject obj in ingredientes)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb == null) continue;

            // Calcular posición relativa al centro del bowl
            Vector3 posRelativa = obj.transform.position - centroBowl;

            // Distancia horizontal (X, Z)
            float distanciaHorizontal = Mathf.Sqrt(posRelativa.x * posRelativa.x + posRelativa.z * posRelativa.z);

            // Altura relativa (Y)
            float altura = posRelativa.y;

            // Si está fuera del radio horizontal
            if (distanciaHorizontal > radioBowl * 0.9f)
            {
                // Empujar hacia el centro
                Vector3 direccionAlCentro = new Vector3(-posRelativa.x, 0, -posRelativa.z).normalized;
                rb.AddForce(direccionAlCentro * fuerzaContencion, ForceMode.Force);

                // Reducir velocidad horizontal
                Vector3 velocidadHorizontal = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                rb.velocity -= velocidadHorizontal * 0.5f * Time.fixedDeltaTime;
            }

            // Si está muy arriba (intentando salir por arriba)
            if (altura > 0.05f)
            {
                // Empujar hacia abajo
                rb.AddForce(Vector3.down * fuerzaContencion * 0.5f, ForceMode.Force);
            }

            // Si está muy abajo (atravesó el fondo)
            if (altura < -profundidadBowl)
            {
                // Reposicionar en el fondo
                Vector3 nuevaPos = obj.transform.position;
                nuevaPos.y = centroBowl.y - profundidadBowl + 0.02f;
                obj.transform.position = nuevaPos;
                
                // Reducir velocidad vertical
                Vector3 vel = rb.velocity;
                vel.y = Mathf.Max(vel.y, 0);
                rb.velocity = vel;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!mostrarGizmos) return;

        Vector3 centro = Application.isPlaying ? centroBowl : transform.position;

        // Dibujar cilindro del bowl
        Gizmos.color = Color.green;
        
        // Círculo superior
        DrawCircle(centro, radioBowl, 32);
        
        // Círculo inferior
        DrawCircle(centro + Vector3.down * profundidadBowl, radioBowl * 0.3f, 32);

        // Líneas verticales
        Gizmos.color = Color.yellow;
        for (int i = 0; i < 8; i++)
        {
            float angulo = (i * 360f / 8f) * Mathf.Deg2Rad;
            Vector3 puntoSuperior = centro + new Vector3(Mathf.Cos(angulo) * radioBowl, 0, Mathf.Sin(angulo) * radioBowl);
            Vector3 puntoInferior = centro + new Vector3(Mathf.Cos(angulo) * radioBowl * 0.3f, -profundidadBowl, Mathf.Sin(angulo) * radioBowl * 0.3f);
            Gizmos.DrawLine(puntoSuperior, puntoInferior);
        }
    }

    void DrawCircle(Vector3 center, float radius, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float angulo1 = (i * 360f / segments) * Mathf.Deg2Rad;
            float angulo2 = ((i + 1) * 360f / segments) * Mathf.Deg2Rad;

            Vector3 p1 = center + new Vector3(Mathf.Cos(angulo1) * radius, 0, Mathf.Sin(angulo1) * radius);
            Vector3 p2 = center + new Vector3(Mathf.Cos(angulo2) * radius, 0, Mathf.Sin(angulo2) * radius);

            Gizmos.DrawLine(p1, p2);
        }
    }
}
