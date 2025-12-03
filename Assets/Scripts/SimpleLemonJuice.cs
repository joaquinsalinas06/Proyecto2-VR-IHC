using UnityEngine;

/// <summary>
/// Sistema SUPER SIMPLE para exprimir limón y generar líquido en el bowl
/// Solo acerca el limón al bowl y se exprime automáticamente
/// </summary>
public class SimpleLemonJuice : MonoBehaviour
{
    [Header("Configuración Simple")]
    [Tooltip("Distancia máxima para empezar a exprimir (metros)")]
    public float distanciaExprimido = 0.3f;

    [Tooltip("Velocidad de generación de jugo")]
    public float velocidadJugo = 15f;

    [Tooltip("Cantidad total de jugo")]
    public float jugoTotal = 100f;

    [Tooltip("Color del jugo")]
    public Color colorJugo = new Color(1f, 1f, 0f, 0.8f); // Amarillo brillante

    [Header("Partículas (Opcional)")]
    public ParticleSystem particulas;

    private float jugoRestante;
    private BowlLiquidManager bowl;
    private bool exprimiendo = false;

    void Start()
    {
        jugoRestante = jugoTotal;
        
        // Buscar el bowl en la escena
        bowl = FindObjectOfType<BowlLiquidManager>();
        
        if (bowl == null)
        {
            Debug.LogError("[SIMPLE LEMON] No se encontró BowlLiquidManager en la escena!");
        }

        // Configurar partículas si existen
        if (particulas != null)
        {
            particulas.Stop();
        }
    }

    void Update()
    {
        if (bowl == null || jugoRestante <= 0) 
        {
            exprimiendo = false;
            if (particulas != null && particulas.isPlaying)
                particulas.Stop();
            return;
        }

        // Calcular distancia al bowl
        float distancia = Vector3.Distance(transform.position, bowl.transform.position);

        // Exprimir si está cerca
        if (distancia < distanciaExprimido)
        {
            exprimiendo = true;
            
            // Generar jugo
            float jugoPorFrame = velocidadJugo * Time.deltaTime;
            jugoRestante -= jugoPorFrame;
            
            // Agregar al bowl
            bowl.AgregarLiquido(jugoPorFrame, colorJugo);

            // Activar partículas
            if (particulas != null && !particulas.isPlaying)
            {
                particulas.Play();
            }
        }
        else
        {
            exprimiendo = false;
            
            // Detener partículas
            if (particulas != null && particulas.isPlaying)
            {
                particulas.Stop();
            }
        }
    }

    // Visualización en editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaExprimido);
    }
}
