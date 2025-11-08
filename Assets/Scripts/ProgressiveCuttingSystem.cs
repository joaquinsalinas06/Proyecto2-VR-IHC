using UnityEngine;

/// <summary>
/// Sistema de corte progresivo - Requiere múltiples cortes antes de generar cubos
/// Perfecto para experiencia inmersiva de cocina VR
/// </summary>
public class ProgressiveCuttingSystem : MonoBehaviour
{
    [Header("Referencias Visuales")]
    [SerializeField] private GameObject fishModel; // El modelo completo del pescado
    [SerializeField] private GameObject[] cubePrefabs; // Prefabs de cubos de pescado
    [SerializeField] private Transform cubeSpawnPoint; // Transform HIJO del pescado - donde aparecen cubos

    [Header("Configuración de Corte")]
    [SerializeField] private int cutsNeeded = 3; // Cuántos cortes necesita
    [SerializeField] private int cubesPerCut = 8; // Cuántos cubos genera al completar
    [SerializeField] private string knifeTag = "Knife"; // Tag del cuchillo
    
    [Header("Feedback Visual")]
    [SerializeField] private Material normalMaterial; // Material original
    [SerializeField] private Material cutMaterial; // Material al recibir corte (opcional)
    [SerializeField] private GameObject[] cutMarks; // Marcas visuales de corte (opcional)
    
    [Header("Audio")]
    [SerializeField] private AudioClip chopSound; // Sonido de cortar
    [SerializeField] private AudioClip completeSound; // Sonido al completar cortes
    
    [Header("Efectos Visuales")]
    [SerializeField] private ParticleSystem cutParticles; // Partículas al cortar
    [SerializeField] private bool showCutMarks = true; // Mostrar marcas de corte

    // Estado interno
    private int currentCuts = 0;
    private bool isFullyCut = false;
    private Renderer fishRenderer;
    private AudioSource audioSource;
    private float lastCutTime = 0f;
    private float cutCooldown = 0.5f; // Tiempo entre cortes válidos

    void Start()
    {
        // Obtener componentes
        fishRenderer = fishModel.GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Esconder marcas de corte inicialmente
        if (cutMarks != null && cutMarks.Length > 0)
        {
            foreach (GameObject mark in cutMarks)
            {
                if (mark != null)
                    mark.SetActive(false);
            }
        }

        // Validar setup
        if (fishModel == null)
        {
            Debug.LogError("ProgressiveCuttingSystem: Falta asignar Fish Model!");
        }
        if (cubePrefabs == null || cubePrefabs.Length == 0)
        {
            Debug.LogError("ProgressiveCuttingSystem: Falta asignar Cube Prefabs!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Verificar si es el cuchillo y si no está completamente cortado
        if (other.CompareTag(knifeTag) && !isFullyCut)
        {
            // Cooldown para evitar múltiples cortes instantáneos
            if (Time.time - lastCutTime < cutCooldown)
                return;

            lastCutTime = Time.time;

            // Registrar el corte
            OnCut();
        }
    }

    void OnCut()
    {
        currentCuts++;

        // Feedback visual
        ShowCutFeedback();

        // Sonido de cortar
        if (chopSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(chopSound, 0.7f);
        }

        // Efecto de partículas
        if (cutParticles != null)
        {
            cutParticles.transform.position = transform.position;
            cutParticles.Play();
        }

        // Haptic feedback
        OVRInput.SetControllerVibration(0.3f, 0.2f, OVRInput.Controller.RTouch);

        // Mostrar marca de corte si está habilitado
        if (showCutMarks && cutMarks != null && currentCuts <= cutMarks.Length)
        {
            if (cutMarks[currentCuts - 1] != null)
            {
                cutMarks[currentCuts - 1].SetActive(true);
            }
        }

        // Feedback UI
        ShowCutProgress();

        // Verificar si completó los cortes necesarios
        if (currentCuts >= cutsNeeded)
        {
            CompleteCutting();
        }
    }

    void ShowCutFeedback()
    {
        // Cambiar brevemente el color del material para feedback
        if (fishRenderer != null && cutMaterial != null)
        {
            fishRenderer.material = cutMaterial;
            Invoke("ResetMaterial", 0.2f);
        }

        // Pequeña escala bounce
        StartCoroutine(ScaleBounce());
    }

    void ResetMaterial()
    {
        if (fishRenderer != null && normalMaterial != null)
        {
            fishRenderer.material = normalMaterial;
        }
    }

    System.Collections.IEnumerator ScaleBounce()
    {
        Vector3 originalScale = fishModel.transform.localScale;
        Vector3 targetScale = originalScale * 0.95f;

        // Reducir
        float elapsed = 0f;
        float duration = 0.1f;
        while (elapsed < duration)
        {
            fishModel.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Volver a normal
        elapsed = 0f;
        while (elapsed < duration)
        {
            fishModel.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        fishModel.transform.localScale = originalScale;
    }

    void ShowCutProgress()
    {
        // Mostrar progreso en consola
        string progressText = $"Cortando pescado... {currentCuts}/{cutsNeeded}";
        Debug.Log(progressText);
        
        // TODO: Añadir FeedbackSystem cuando esté listo
    }

    void CompleteCutting()
    {
        isFullyCut = true;

        // Sonido de completado
        if (completeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(completeSound);
        }

        // Haptic fuerte
        OVRInput.SetControllerVibration(0.6f, 0.4f, OVRInput.Controller.RTouch);

        // Esconder el pescado completo
        if (fishModel != null)
        {
            fishModel.SetActive(false);
        }

        // Generar los cubos
        SpawnCubes();

        // Feedback de éxito
        Debug.Log("¡Pescado cortado perfectamente!");
        
        // TODO: Añadir FeedbackSystem y RecipeManager cuando estén listos
    }

    void SpawnCubes()
    {
        if (cubePrefabs == null || cubePrefabs.Length == 0)
        {
            Debug.LogError("No hay cubos asignados para spawnar!");
            return;
        }

        Vector3 spawnPosition = cubeSpawnPoint != null ? cubeSpawnPoint.position : transform.position;

        // Generar cubos en patrón ordenado
        int cubesPerRow = 4;
        float spacing = 0.03f; // 3cm entre cubos

        for (int i = 0; i < cubesPerCut; i++)
        {
            // Calcular posición en grid
            int row = i / cubesPerRow;
            int col = i % cubesPerRow;

            Vector3 offset = new Vector3(
                col * spacing - (cubesPerRow - 1) * spacing / 2f, // Centrado
                0.01f, // Ligeramente elevado
                row * spacing
            );

            // Seleccionar prefab aleatorio
            GameObject cubePrefab = cubePrefabs[Random.Range(0, cubePrefabs.Length)];

            // Instanciar cubo
            GameObject cube = Instantiate(
                cubePrefab,
                spawnPosition + offset,
                Quaternion.identity
            );

            // Configurar el cubo
            ConfigureCube(cube);

            // Pequeña fuerza hacia arriba para efecto visual
            Rigidbody rb = cube.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(Vector3.up * 0.5f, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 0.2f, ForceMode.Impulse);
            }
        }
    }

    void ConfigureCube(GameObject cube)
    {
        // Asegurar que tiene Rigidbody
        Rigidbody rb = cube.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = cube.AddComponent<Rigidbody>();
        }
        rb.mass = 0.05f;
        rb.useGravity = true;

        // Asegurar que tiene Collider
        Collider col = cube.GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider boxCol = cube.AddComponent<BoxCollider>();
            boxCol.size = Vector3.one * 0.02f; // 2cm cubo
        }

        // Asegurar que tiene Grabbable (si usas Meta XR)
        if (cube.GetComponent<OVRGrabbable>() == null)
        {
            // Si usas Building Blocks, añadir los componentes necesarios
            // cube.AddComponent<GrabInteractable>();
            // cube.AddComponent<Grabbable>();
        }

        // Asignar tag
        cube.tag = "FishCube";

        // Asignar layer si es necesario
        cube.layer = LayerMask.NameToLayer("Default");
    }

    // Método público para resetear (útil para testing o retry)
    public void ResetCutting()
    {
        currentCuts = 0;
        isFullyCut = false;
        
        if (fishModel != null)
        {
            fishModel.SetActive(true);
        }

        if (cutMarks != null)
        {
            foreach (GameObject mark in cutMarks)
            {
                if (mark != null)
                    mark.SetActive(false);
            }
        }

        // Limpiar cubos generados (opcional)
        GameObject[] existingCubes = GameObject.FindGameObjectsWithTag("FishCube");
        foreach (GameObject cube in existingCubes)
        {
            Destroy(cube);
        }
    }

    // Getters públicos
    public int GetCurrentCuts() => currentCuts;
    public int GetCutsNeeded() => cutsNeeded;
    public bool IsFullyCut() => isFullyCut;

    // Método para cambiar el número de cortes necesarios en runtime (opcional)
    public void SetCutsNeeded(int cuts)
    {
        cutsNeeded = Mathf.Max(1, cuts);
    }

    // Debug visual en Scene View
    void OnDrawGizmos()
    {
        if (cubeSpawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(cubeSpawnPoint.position, 0.05f);
            Gizmos.DrawLine(transform.position, cubeSpawnPoint.position);
        }
    }
}
