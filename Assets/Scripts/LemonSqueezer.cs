using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema para exprimir el limón con deformación visual y generación de jugo
/// </summary>
[RequireComponent(typeof(MeshFilter))]
public class LemonSqueezer : MonoBehaviour
{
    [Header("Configuración de Exprimido")]
    [Tooltip("Fuerza mínima necesaria para comenzar a exprimir")]
    [Range(0.1f, 10f)]
    public float fuerzaMinimaExprimido = 2f;

    [Tooltip("Máxima deformación del limón (0-1)")]
    [Range(0f, 0.8f)]
    public float deformacionMaxima = 0.5f;

    [Tooltip("Velocidad de deformación")]
    [Range(0.1f, 5f)]
    public float velocidadDeformacion = 2f;

    [Tooltip("Velocidad de recuperación cuando no se presiona")]
    [Range(0.1f, 5f)]
    public float velocidadRecuperacion = 1f;

    [Header("Generación de Jugo")]
    [Tooltip("Cantidad de jugo generado por segundo al exprimir")]
    [Range(0f, 100f)]
    public float tasaGeneracionJugo = 10f;

    [Tooltip("Jugo máximo que puede generar el limón")]
    [Range(0f, 200f)]
    public float jugoMaximo = 100f;

    [Tooltip("Prefab de partículas de jugo (opcional)")]
    public ParticleSystem particulasJugoPrefab;

    [Tooltip("Punto de spawn de las partículas")]
    public Transform puntoSpawnJugo;

    [Header("Audio (Opcional)")]
    [Tooltip("Sonido al exprimir")]
    public AudioClip sonidoExprimir;

    [Tooltip("Volumen del sonido")]
    [Range(0f, 1f)]
    public float volumenSonido = 0.5f;

    [Header("Detección")]
    [Tooltip("Tag del objeto exprimidor (ej: 'Squeezer', 'Hand')")]
    public string tagExprimidor = "Hand";

    [Tooltip("Usar velocidad de colisión para determinar fuerza")]
    public bool usarVelocidadColision = true;

    // Estado interno
    private MeshFilter meshFilter;
    private Mesh meshOriginal;
    private Mesh meshDeformado;
    private Vector3[] verticesOriginales;
    private Vector3[] verticesDeformados;
    
    private float deformacionActual = 0f;
    private float jugoRestante;
    private bool estaExprimiendo = false;
    private ParticleSystem particulasActivas;
    private AudioSource audioSource;
    
    private Vector3 centroMesh;
    private List<Collider> colisionadoresActivos = new List<Collider>();

    void Start()
    {
        InicializarMesh();
        InicializarComponentes();
        jugoRestante = jugoMaximo;
    }

    void InicializarMesh()
    {
        meshFilter = GetComponent<MeshFilter>();
        
        if (meshFilter.sharedMesh == null)
        {
            Debug.LogError("[LEMON SQUEEZER] No hay mesh asignado!");
            enabled = false;
            return;
        }

        // Crear copia del mesh original
        meshOriginal = meshFilter.sharedMesh;
        meshDeformado = Instantiate(meshOriginal);
        meshFilter.mesh = meshDeformado;

        // Guardar vértices originales
        verticesOriginales = meshOriginal.vertices;
        verticesDeformados = new Vector3[verticesOriginales.Length];
        System.Array.Copy(verticesOriginales, verticesDeformados, verticesOriginales.Length);

        // Calcular centro del mesh
        centroMesh = meshOriginal.bounds.center;
    }

    void InicializarComponentes()
    {
        // Crear partículas si hay prefab
        if (particulasJugoPrefab != null)
        {
            GameObject particulasObj = Instantiate(particulasJugoPrefab.gameObject, transform);
            particulasActivas = particulasObj.GetComponent<ParticleSystem>();
            
            if (puntoSpawnJugo != null)
            {
                particulasObj.transform.position = puntoSpawnJugo.position;
                particulasObj.transform.rotation = puntoSpawnJugo.rotation;
            }
            else
            {
                particulasObj.transform.localPosition = Vector3.down * 0.02f;
            }
            
            particulasActivas.Stop();
        }

        // Configurar audio
        if (sonidoExprimir != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = sonidoExprimir;
            audioSource.volume = volumenSonido;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        // Detectar si está cerca del bowl y exprimir automáticamente
        BowlLiquidManager bowl = FindObjectOfType<BowlLiquidManager>();
        if (bowl != null && jugoRestante > 0)
        {
            float distancia = Vector3.Distance(transform.position, bowl.transform.position);
            estaExprimiendo = distancia < 0.3f; // Se exprime cuando está a menos de 30cm
        }
        else
        {
            estaExprimiendo = false;
        }

        ActualizarDeformacion();
    }

    void ActualizarDeformacion()
    {
        float deformacionObjetivo = estaExprimiendo ? deformacionMaxima : 0f;
        float velocidad = estaExprimiendo ? velocidadDeformacion : velocidadRecuperacion;

        deformacionActual = Mathf.MoveTowards(deformacionActual, deformacionObjetivo, velocidad * Time.deltaTime);

        if (deformacionActual > 0.01f)
        {
            AplicarDeformacion(deformacionActual);
        }
        else if (deformacionActual < 0.01f && deformacionActual != 0f)
        {
            // Restaurar mesh original
            System.Array.Copy(verticesOriginales, verticesDeformados, verticesOriginales.Length);
            meshDeformado.vertices = verticesDeformados;
            meshDeformado.RecalculateBounds();
            meshDeformado.RecalculateNormals();
            deformacionActual = 0f;
        }

        // Generar jugo mientras se exprime
        if (estaExprimiendo && jugoRestante > 0)
        {
            float jugoGenerado = tasaGeneracionJugo * Time.deltaTime;
            jugoRestante = Mathf.Max(0, jugoRestante - jugoGenerado);

            // Notificar al bowl si existe
            NotificarGeneracionJugo(jugoGenerado);
        }

        // Controlar partículas
        if (particulasActivas != null)
        {
            if (estaExprimiendo && jugoRestante > 0)
            {
                if (!particulasActivas.isPlaying)
                    particulasActivas.Play();
            }
            else
            {
                if (particulasActivas.isPlaying)
                    particulasActivas.Stop();
            }
        }

        // Controlar audio
        if (audioSource != null)
        {
            if (estaExprimiendo && jugoRestante > 0)
            {
                if (!audioSource.isPlaying)
                    audioSource.Play();
            }
            else
            {
                if (audioSource.isPlaying)
                    audioSource.Stop();
            }
        }
    }

    void AplicarDeformacion(float intensidad)
    {
        for (int i = 0; i < verticesOriginales.Length; i++)
        {
            Vector3 verticeOriginal = verticesOriginales[i];
            Vector3 direccionAlCentro = centroMesh - verticeOriginal;
            
            // Deformar más en el eje Y (aplastar verticalmente)
            float factorY = 1.5f;
            float factorXZ = 0.3f;

            Vector3 deformacion = new Vector3(
                direccionAlCentro.x * factorXZ * intensidad,
                direccionAlCentro.y * factorY * intensidad,
                direccionAlCentro.z * factorXZ * intensidad
            );

            verticesDeformados[i] = verticeOriginal + deformacion;
        }

        meshDeformado.vertices = verticesDeformados;
        meshDeformado.RecalculateBounds();
        meshDeformado.RecalculateNormals();
    }

    void NotificarGeneracionJugo(float cantidad)
    {
        // Buscar bowl cercano y notificar
        BowlLiquidManager[] bowls = FindObjectsOfType<BowlLiquidManager>();
        
        foreach (var bowl in bowls)
        {
            float distancia = Vector3.Distance(transform.position, bowl.transform.position);
            if (distancia < 0.5f) // 50cm de distancia máxima
            {
                bowl.AgregarLiquido(cantidad, Color.yellow); // Jugo de limón amarillo
                break;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(tagExprimidor))
        {
            colisionadoresActivos.Add(collision.collider);
            VerificarExprimido(collision);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag(tagExprimidor))
        {
            VerificarExprimido(collision);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag(tagExprimidor))
        {
            colisionadoresActivos.Remove(collision.collider);
            
            if (colisionadoresActivos.Count == 0)
            {
                estaExprimiendo = false;
            }
        }
    }

    void VerificarExprimido(Collision collision)
    {
        if (jugoRestante <= 0)
        {
            estaExprimiendo = false;
            return;
        }

        float fuerza = 0f;

        if (usarVelocidadColision)
        {
            // Usar velocidad de impacto como fuerza
            fuerza = collision.relativeVelocity.magnitude;
        }
        else
        {
            // Usar número de puntos de contacto como indicador de presión
            fuerza = collision.contactCount * 2f;
        }

        estaExprimiendo = fuerza >= fuerzaMinimaExprimido;
    }

    // Método público para exprimir programáticamente
    public void Exprimir(bool activar)
    {
        estaExprimiendo = activar && jugoRestante > 0;
    }

    // Getters para información
    public float ObtenerJugoRestante() => jugoRestante;
    public float ObtenerPorcentajeJugo() => jugoRestante / jugoMaximo;
    public bool EstaExprimiendo() => estaExprimiendo;

    void OnDestroy()
    {
        if (meshDeformado != null)
        {
            Destroy(meshDeformado);
        }
    }

    // Visualización en editor
    void OnDrawGizmosSelected()
    {
        if (puntoSpawnJugo != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(puntoSpawnJugo.position, 0.02f);
            Gizmos.DrawLine(puntoSpawnJugo.position, puntoSpawnJugo.position + puntoSpawnJugo.forward * 0.05f);
        }
    }
}
