using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema de snap con pasos bloqueados - Los ingredientes se bloquean al colocarse
/// y solo se aceptan según el paso actual del proceso de cocina
/// </summary>
public class CuttingBoardSnapZone : MonoBehaviour
{
    [System.Serializable]
    public class IngredientSnapSettings
    {
        public string ingredientTag;            // Tag del ingrediente (ej: "Pescado", "Limon", "Cebolla", "Aji")
        public Vector3 positionOffset;          // Offset desde el snap point
        public Vector3 rotation;                // Rotación en grados Euler
        public bool useLocalRotation = true;    // Si true, usa rotación local
    }

    [System.Serializable]
    public class CuttingStep
    {
        public string stepName;                 // Nombre del paso (ej: "Cortar Pescado")
        public string requiredIngredientTag;    // Tag del ingrediente que se necesita

        [Header("Pedazos Cortados")]
        public CutIngredientPieces.PieceConfiguration pieceConfiguration;

        [HideInInspector] public bool isPlaced = false;   // Si el ingrediente fue colocado
        [HideInInspector] public bool isCut = false;      // Si el ingrediente fue cortado
        [HideInInspector] public GameObject placedObject; // Referencia al objeto colocado
        [HideInInspector] public CutIngredientPieces cutPieces; // Referencia a los pedazos
    }

    [Header("Snap Point")]
    public Transform snapPoint;

    [Header("Ingredient Settings")]
    public IngredientSnapSettings[] ingredientSettings;

    [Header("Step System - Define el orden de pasos")]
    public CuttingStep[] cuttingSteps;
    [HideInInspector] public int currentStep = 0;

    [Header("Visual Feedback")]
    public GameObject visualIndicator;
    public Material greenMaterial;              // Material verde = paso correcto
    public Material redMaterial;                // Material rojo = ingrediente incorrecto

    [Header("Audio Feedback")]
    public AudioClip snapSound;
    public AudioClip errorSound;
    public AudioClip cutCompleteSound;          // Sonido cuando se completa el corte

    [Header("Cut Effects")]
    public ParticleSystem cutCompleteParticles; // Partículas cuando se corta
    public GameObject cutCompleteEffectPrefab;  // Efecto visual cuando se libera

    private AudioSource audioSource;
    private Renderer visualRenderer;

    void Start()
    {
        // Configurar AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Si no hay snap point, usar este transform
        if (snapPoint == null)
        {
            snapPoint = transform;
        }

        // Obtener renderer del visual
        if (visualIndicator != null)
        {
            visualRenderer = visualIndicator.GetComponent<Renderer>();
        }

        // Configurar visual inicial (verde para el primer paso)
        UpdateVisualIndicator();

        Debug.Log($"[SNAP] Iniciado. Paso actual: {currentStep} - Esperando: {GetCurrentStepName()}");
    }

    void OnTriggerEnter(Collider other)
    {
        // IGNORAR colliders de las manos, controllers, cuchillo y otros objetos del sistema VR
        if (other.gameObject.name.Contains("Pinch") ||
            other.gameObject.name.Contains("Collider") ||
            other.gameObject.name == "PinchArea" ||
            other.gameObject.name == "PinchPointRange" ||
            other.gameObject.name.Contains("Controller") ||
            other.gameObject.name.Contains("Hand") ||
            other.CompareTag("Knife") ||
            other.CompareTag("Untagged"))
        {
            return;
        }

        // Si ya completamos todos los pasos, no aceptar más ingredientes
        if (currentStep >= cuttingSteps.Length)
        {
            Debug.Log("[SNAP] Todos los pasos completados. No se aceptan más ingredientes.");
            return;
        }

        // Obtener el paso actual
        CuttingStep step = cuttingSteps[currentStep];

        // Si ya hay un ingrediente colocado en este paso, rechazar
        if (step.isPlaced)
        {
            Debug.Log($"[SNAP] Ya hay un ingrediente en el paso actual: {step.stepName}");
            return;
        }

        // Verificar si es el ingrediente correcto para este paso
        string ingredientTag = other.gameObject.tag;

        if (ingredientTag == step.requiredIngredientTag)
        {
            // ¡Ingrediente correcto!
            Debug.Log($"[SNAP] Ingrediente correcto: {ingredientTag} para paso {step.stepName}");
            SnapIngredient(other.gameObject);
        }
        else
        {
            // Ingrediente incorrecto
            Debug.Log($"[SNAP] Ingrediente incorrecto: {ingredientTag}. Se espera: {step.requiredIngredientTag}");
            PlayErrorFeedback();
            FlashRedVisual();
        }
    }

    void SnapIngredient(GameObject ingredient)
    {
        CuttingStep step = cuttingSteps[currentStep];
        IngredientSnapSettings settings = GetSettingsForIngredient(ingredient);

        if (settings == null)
        {
            Debug.LogWarning($"[SNAP] No hay configuración para {ingredient.tag}");
            return;
        }

        Debug.Log($"[SNAP] Haciendo snap de {ingredient.name} en paso {currentStep}: {step.stepName}");

        // Calcular posición y rotación
        Vector3 finalPosition = snapPoint.position + snapPoint.TransformDirection(settings.positionOffset);
        Quaternion finalRotation = settings.useLocalRotation
            ? snapPoint.rotation * Quaternion.Euler(settings.rotation)
            : Quaternion.Euler(settings.rotation);

        // Aplicar transformación
        ingredient.transform.position = finalPosition;
        ingredient.transform.rotation = finalRotation;

        // BLOQUEAR COMPLETAMENTE EL INGREDIENTE
        LockIngredient(ingredient);

        // Marcar el paso como colocado
        step.isPlaced = true;
        step.placedObject = ingredient;

        // Feedback
        PlaySnapSound();
        UpdateVisualIndicator();

        Debug.Log($"[SNAP] Ingrediente bloqueado. Esperando que sea cortado para avanzar al siguiente paso.");
    }

    void LockIngredient(GameObject ingredient)
    {
        // Desactivar RigidbodyKinematicLocker si existe
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
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        // DESACTIVAR todos los componentes Grabbable para que NO se pueda agarrar
        var grabbable = ingredient.GetComponent<Oculus.Interaction.Grabbable>();
        if (grabbable != null)
        {
            grabbable.enabled = false;
        }

        var grabInteractable = ingredient.GetComponent<Oculus.Interaction.GrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.enabled = false;
        }

        var ovrGrabbable = ingredient.GetComponent<OVRGrabbable>();
        if (ovrGrabbable != null)
        {
            ovrGrabbable.enabled = false;
        }

        Debug.Log($"[SNAP] {ingredient.name} bloqueado completamente. NO se puede agarrar.");
    }

    /// <summary>
    /// Llamar este método cuando el cuchillo toca/corta el ingrediente
    /// </summary>
    public void OnIngredientCut(GameObject ingredient)
    {
        if (currentStep >= cuttingSteps.Length)
            return;

        CuttingStep step = cuttingSteps[currentStep];

        // Verificar que es el ingrediente del paso actual
        if (step.placedObject == ingredient && step.isPlaced && !step.isCut)
        {
            Debug.Log($"[SNAP] ¡Ingrediente cortado! Paso {currentStep} completado: {step.stepName}");

            step.isCut = true;

            // Guardar posición del ingrediente antes de destruirlo
            Vector3 ingredientPosition = ingredient.transform.position;

            // EFECTOS VISUALES Y DE AUDIO al completar el corte
            PlayCutCompleteEffects(ingredientPosition);

            // GENERAR PEDAZOS CORTADOS
            SpawnCutPieces(step, ingredientPosition);

            // DESTRUIR el ingrediente original (desaparece)
            Destroy(ingredient);
            step.placedObject = null;

            // Avanzar al siguiente paso
            currentStep++;

            if (currentStep < cuttingSteps.Length)
            {
                Debug.Log($"[SNAP] Avanzando al paso {currentStep}: {cuttingSteps[currentStep].stepName}");
                UpdateVisualIndicator();
            }
            else
            {
                Debug.Log("[SNAP] ¡Todos los pasos completados!");
                UpdateVisualIndicator();
            }
        }
    }

    void PlayCutCompleteEffects(Vector3 position)
    {
        // Sonido de corte completado
        if (cutCompleteSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(cutCompleteSound, 0.7f);
        }

        // Partículas
        if (cutCompleteParticles != null)
        {
            cutCompleteParticles.transform.position = position;
            cutCompleteParticles.Play();
        }

        // Efecto visual
        if (cutCompleteEffectPrefab != null)
        {
            GameObject effect = Instantiate(cutCompleteEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        Debug.Log($"[SNAP] Efectos de corte completado reproducidos en {position}");
    }

    void SpawnCutPieces(CuttingStep step, Vector3 position)
    {
        if (step.pieceConfiguration == null)
        {
            Debug.LogWarning($"[SNAP] No hay configuración de pedazos para {step.stepName}");
            return;
        }

        // Validar según el modo
        bool isValid = false;
        if (step.pieceConfiguration.spawnMode == CutIngredientPieces.SpawnMode.Random)
        {
            isValid = step.pieceConfiguration.piecePrefab != null;
        }
        else if (step.pieceConfiguration.spawnMode == CutIngredientPieces.SpawnMode.Specific)
        {
            isValid = step.pieceConfiguration.specificPieces != null &&
                      step.pieceConfiguration.specificPieces.Length > 0;
        }

        if (!isValid)
        {
            Debug.LogWarning($"[SNAP] No hay prefabs configurados para {step.stepName} (Modo: {step.pieceConfiguration.spawnMode})");
            return;
        }

        // Crear GameObject para manejar los pedazos
        GameObject piecesContainer = new GameObject($"CutPieces_{step.stepName}");
        piecesContainer.transform.position = position;

        // Agregar componente CutIngredientPieces
        CutIngredientPieces cutPieces = piecesContainer.AddComponent<CutIngredientPieces>();

        // Usar la configuración directamente desde el step
        cutPieces.configuration = step.pieceConfiguration;

        // Generar los pedazos
        cutPieces.SpawnPieces(position);

        // Guardar referencia
        step.cutPieces = cutPieces;

        // Determinar cantidad según el modo
        int count = step.pieceConfiguration.spawnMode == CutIngredientPieces.SpawnMode.Specific
            ? (step.pieceConfiguration.specificPieces != null ? step.pieceConfiguration.specificPieces.Length : 0)
            : step.pieceConfiguration.pieceCount;

        Debug.Log($"[SNAP] Generados {count} pedazos de {step.stepName} (Modo: {step.pieceConfiguration.spawnMode})");
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

    void UpdateVisualIndicator()
    {
        if (visualRenderer == null || greenMaterial == null)
            return;

        // Verde si estamos esperando un ingrediente, apagado si ya completamos
        if (currentStep < cuttingSteps.Length)
        {
            visualRenderer.material = greenMaterial;
            visualIndicator.SetActive(true);
        }
        else
        {
            visualIndicator.SetActive(false);
        }
    }

    void FlashRedVisual()
    {
        if (visualRenderer != null && redMaterial != null)
        {
            visualRenderer.material = redMaterial;
            // Volver a verde después de 0.5 segundos
            Invoke("UpdateVisualIndicator", 0.5f);
        }
    }

    void PlaySnapSound()
    {
        if (snapSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(snapSound, 0.6f);
        }
    }

    void PlayErrorFeedback()
    {
        if (errorSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(errorSound, 0.8f);
        }
    }

    string GetCurrentStepName()
    {
        if (currentStep < cuttingSteps.Length)
        {
            return cuttingSteps[currentStep].stepName;
        }
        return "Completado";
    }

    // Métodos públicos para consultar el estado
    public bool IsStepCompleted(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= cuttingSteps.Length)
            return false;
        return cuttingSteps[stepIndex].isCut;
    }

    public int GetCurrentStep()
    {
        return currentStep;
    }

    public bool AllStepsCompleted()
    {
        return currentStep >= cuttingSteps.Length;
    }

    // Debug visual
    void OnDrawGizmos()
    {
        Transform point = snapPoint != null ? snapPoint : transform;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(point.position, 0.02f);

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
}
