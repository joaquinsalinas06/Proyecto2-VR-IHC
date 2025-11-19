using UnityEngine;

/// <summary>
/// Zona de snap para tabla de cortar - Los ingredientes se posicionan automáticamente
/// al tocar esta zona, con posición y orientación fijas según el tipo de ingrediente
/// </summary>
public class CuttingBoardSnapZone : MonoBehaviour
{
    [Header("Referencias Visuales")]
    [SerializeField] private GameObject visualIndicator; // El visual que ayuda a ver la zona

    [Header("Configuración de Snap")]
    [SerializeField] private Transform snapPoint; // Punto exacto donde se centra el ingrediente
    [SerializeField] private bool hideVisualWhenUsed = false; // Ocultar visual cuando hay ingredientes
    [SerializeField] private bool allowMultipleIngredients = true; // Permitir varios ingredientes
    [SerializeField] private int maxIngredients = 10; // Máximo de ingredientes permitidos

    [Header("Configuración de Orientación por Ingrediente")]
    [SerializeField] private IngredientSnapSettings[] ingredientSettings; // Configuración para cada tipo

    [Header("Audio y Feedback")]
    [SerializeField] private AudioClip snapSound; // Sonido al hacer snap
    [SerializeField] private bool hapticFeedback = true;
    [SerializeField] private float hapticIntensity = 0.3f;

    private System.Collections.Generic.List<GameObject> snappedObjects = new System.Collections.Generic.List<GameObject>();
    private AudioSource audioSource;

    [System.Serializable]
    public class IngredientSnapSettings
    {
        public string ingredientTag; // Tag del ingrediente (ej: "Fish", "Lemon", "Onion")
        public Vector3 positionOffset; // Offset desde el snapPoint
        public Vector3 rotation; // Rotación euler en grados
        public bool useLocalRotation = true; // Usar rotación local o world
    }

    void Start()
    {
        // Configurar AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && snapSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Si no hay snap point, usar este transform
        if (snapPoint == null)
        {
            snapPoint = transform;
        }

        // Configurar visual inicial
        UpdateVisualIndicator();

        // Validación
        if (ingredientSettings == null || ingredientSettings.Length == 0)
        {
            Debug.LogWarning($"CuttingBoardSnapZone en {gameObject.name}: No hay configuraciones de ingredientes!");
        }
    }

    void Update()
    {
        // Revisar cada objeto colocado para detectar si se movió de su posición
        for (int i = snappedObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = snappedObjects[i];
            if (obj == null)
            {
                snappedObjects.RemoveAt(i);
                continue;
            }

            // Detectar si el objeto se movió significativamente de su posición de snap
            // Esto indica que fue agarrado y movido
            IngredientSnapSettings settings = GetSettingsForIngredient(obj);
            if (settings != null)
            {
                Vector3 expectedPosition = snapPoint.position + snapPoint.TransformDirection(settings.positionOffset);
                float distance = Vector3.Distance(obj.transform.position, expectedPosition);

                // Si se movió más de 5cm, liberar del snap
                if (distance > 0.05f)
                {
                    Debug.Log($"[SNAP] {obj.name} se movió {distance}m de su posición, liberando...");
                    ReleaseSnap(obj);
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // IGNORAR colliders de las manos y otros objetos del sistema VR
        if (other.gameObject.name.Contains("Pinch") ||
            other.gameObject.name.Contains("Collider") ||
            other.gameObject.name == "PinchArea" ||
            other.gameObject.name == "PinchPointRange")
        {
            return; // Salir sin procesar
        }

        // Verificar si ya está en la lista (para evitar duplicados)
        if (snappedObjects.Contains(other.gameObject))
        {
            return;
        }

        // Verificar si se permite más ingredientes
        if (!allowMultipleIngredients && snappedObjects.Count > 0)
        {
            return;
        }

        // Verificar límite de ingredientes
        if (snappedObjects.Count >= maxIngredients)
        {
            return;
        }

        // Buscar configuración para este ingrediente
        IngredientSnapSettings settings = GetSettingsForIngredient(other.gameObject);

        if (settings != null)
        {
            SnapIngredient(other.gameObject, settings);
        }
    }

    void OnTriggerExit(Collider other)
    {
        // NO HACER NADA - El ingrediente se queda pegado permanentemente
        // Si quieres que se pueda liberar agarrándolo, descomenta el código de abajo:

        /*
        if (currentSnappedObject == other.gameObject)
        {
            if (IsBeingGrabbed(other.gameObject))
            {
                ReleaseSnap();
            }
        }
        */
    }

    void SnapIngredient(GameObject ingredient, IngredientSnapSettings settings)
    {
        Debug.Log($"[SNAP] Haciendo snap de {ingredient.name} con tag {ingredient.tag}");

        // Añadir a la lista de objetos colocados
        snappedObjects.Add(ingredient);

        // Calcular posición final (siempre en el centro del snap point)
        Vector3 finalPosition = snapPoint.position + snapPoint.TransformDirection(settings.positionOffset);

        // Calcular rotación final
        Quaternion finalRotation;
        if (settings.useLocalRotation)
        {
            finalRotation = snapPoint.rotation * Quaternion.Euler(settings.rotation);
        }
        else
        {
            finalRotation = Quaternion.Euler(settings.rotation);
        }

        // Aplicar transformación INMEDIATAMENTE
        ingredient.transform.position = finalPosition;
        ingredient.transform.rotation = finalRotation;

        // Desactivar RigidbodyKinematicLocker si existe (interfiere con el sistema)
        var kinematicLocker = ingredient.GetComponent<Oculus.Interaction.RigidbodyKinematicLocker>();
        if (kinematicLocker != null)
        {
            kinematicLocker.enabled = false;
        }

        // Bloquear físicas completamente
        Rigidbody rb = ingredient.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            // Congelar todas las restricciones para que se quede completamente quieto
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        // NO DESACTIVAR los componentes Grabbable - dejarlos activos para poder agarrar de nuevo
        // El Rigidbody kinematic con constraints congelados mantiene el objeto quieto

        // Feedback de audio
        if (snapSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(snapSound, 0.6f);
        }

        // Feedback háptico
        if (hapticFeedback)
        {
            OVRInput.SetControllerVibration(hapticIntensity, 0.2f, OVRInput.Controller.RTouch);
            OVRInput.SetControllerVibration(hapticIntensity, 0.2f, OVRInput.Controller.LTouch);
        }

        // Actualizar visual
        UpdateVisualIndicator();
    }

    void ReleaseSnap(GameObject ingredient)
    {
        if (!snappedObjects.Contains(ingredient))
            return;

        Debug.Log($"[SNAP] Liberando {ingredient.name} del snap");

        // Remover de la lista
        snappedObjects.Remove(ingredient);

        // NO reactivar RigidbodyKinematicLocker - causa problemas con la gravedad y el agarre
        // Dejarlo desactivado permanentemente después del primer snap

        // Restaurar físicas normales
        Rigidbody rb = ingredient.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.None; // Liberar constraints
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        // Los componentes Grabbable ya están activos, no necesitamos reactivarlos

        // Actualizar visual
        UpdateVisualIndicator();
    }

    IngredientSnapSettings GetSettingsForIngredient(GameObject ingredient)
    {
        if (ingredientSettings == null)
            return null;

        foreach (var settings in ingredientSettings)
        {
            if (ingredient.CompareTag(settings.ingredientTag))
            {
                return settings;
            }
        }

        return null;
    }

    bool IsBeingGrabbed(GameObject obj)
    {
        // Compatibilidad con Meta XR Building Blocks - Grabbable
        var grabbable = obj.GetComponent<Oculus.Interaction.Grabbable>();
        if (grabbable != null && grabbable.enabled)
        {
            return grabbable.SelectingPointsCount > 0;
        }

        // Compatibilidad con Meta XR Building Blocks - GrabInteractable
        var grabInteractable = obj.GetComponent<Oculus.Interaction.GrabInteractable>();
        if (grabInteractable != null && grabInteractable.enabled)
        {
            return grabInteractable.State == Oculus.Interaction.InteractableState.Select;
        }

        // Compatibilidad con OVRGrabbable (legacy)
        var ovrGrabbable = obj.GetComponent<OVRGrabbable>();
        if (ovrGrabbable != null && ovrGrabbable.enabled)
        {
            return ovrGrabbable.isGrabbed;
        }

        return false;
    }

    void UpdateVisualIndicator()
    {
        if (visualIndicator == null)
            return;

        if (hideVisualWhenUsed)
        {
            // Ocultar solo cuando hay ingredientes
            visualIndicator.SetActive(snappedObjects.Count == 0);
        }
        else
        {
            // Mantener siempre visible
            visualIndicator.SetActive(true);
        }
    }

    /// <summary>
    /// Método público para forzar la liberación de un ingrediente específico
    /// </summary>
    public void ForceRelease(GameObject ingredient)
    {
        ReleaseSnap(ingredient);
    }

    /// <summary>
    /// Liberar todos los ingredientes
    /// </summary>
    public void ReleaseAll()
    {
        // Crear copia de la lista para evitar modificar durante iteración
        var objectsToRelease = new System.Collections.Generic.List<GameObject>(snappedObjects);
        foreach (var obj in objectsToRelease)
        {
            ReleaseSnap(obj);
        }
    }

    /// <summary>
    /// Verificar si hay objetos colocados
    /// </summary>
    public bool HasSnappedObjects()
    {
        return snappedObjects.Count > 0;
    }

    /// <summary>
    /// Obtener cantidad de objetos colocados
    /// </summary>
    public int GetSnappedObjectCount()
    {
        return snappedObjects.Count;
    }

    /// <summary>
    /// Obtener lista de objetos colocados
    /// </summary>
    public System.Collections.Generic.List<GameObject> GetSnappedObjects()
    {
        return new System.Collections.Generic.List<GameObject>(snappedObjects);
    }

    // Debug visual en Scene View
    void OnDrawGizmos()
    {
        Transform point = snapPoint != null ? snapPoint : transform;

        // Dibujar punto de snap
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(point.position, 0.02f);

        // Dibujar ejes de orientación
        Gizmos.color = Color.red;
        Gizmos.DrawLine(point.position, point.position + point.right * 0.05f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(point.position, point.position + point.up * 0.05f);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(point.position, point.position + point.forward * 0.05f);

        // Dibujar preview de posiciones para cada ingrediente
        if (ingredientSettings != null)
        {
            foreach (var settings in ingredientSettings)
            {
                Vector3 previewPos = point.position + point.TransformDirection(settings.positionOffset);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(previewPos, Vector3.one * 0.03f);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Dibujar collider de la zona
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
        }
    }
}
