using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Panel de instrucciones que se muestra durante el gameplay
/// Se sincroniza con el CuttingBoardSnapZone para mostrar qué hacer en cada paso
/// </summary>
public class InGameInstructionPanel : MonoBehaviour
{
    [System.Serializable]
    public class StepInstruction
    {
        public string stepName;                 // Debe coincidir con el stepName del CuttingBoardSnapZone

        [Header("Instrucciones")]
        [TextArea(2, 5)]
        public string placeInstructionText;     // Texto cuando hay que colocar el ingrediente
        [TextArea(2, 5)]
        public string cutInstructionText;       // Texto cuando hay que cortarlo

        [Header("Visuales")]
        public Sprite ingredientIcon;           // Icono del ingrediente
        public Sprite cutTypeIcon;              // Icono del tipo de corte
        public Color panelColor = Color.white; // Color del panel para este paso
    }

    [Header("Referencias")]
    public CuttingBoardSnapZone cuttingBoard;

    [Header("UI Elements")]
    public GameObject instructionPanel;
    public TextMeshProUGUI instructionTitleText;
    public TextMeshProUGUI instructionDescriptionText;
    public Image ingredientIconImage;
    public Image cutTypeIconImage;
    public Image panelBackgroundImage;
    public Slider progressSlider;               // Barra de progreso del corte
    public GameObject completionMessage;         // Mensaje de "¡Paso completado!"

    [Header("Step Instructions")]
    [Tooltip("Instrucciones para cada paso (orden debe coincidir con CuttingBoardSnapZone)")]
    public StepInstruction[] stepInstructions;

    [Header("Settings")]
    [Tooltip("Ocultar panel cuando todos los pasos estén completos")]
    public bool hideWhenComplete = false;
    [Tooltip("Tiempo para mostrar mensaje de completado (segundos)")]
    public float completionMessageDuration = 2f;

    private int lastStepIndex = -1;
    private bool isPlaced = false;

    void Start()
    {
        if (cuttingBoard == null)
        {
            Debug.LogWarning("[INSTRUCTION] No se asignó CuttingBoardSnapZone. Buscando en la escena...");
            cuttingBoard = FindObjectOfType<CuttingBoardSnapZone>();
        }

        if (cuttingBoard == null)
        {
            Debug.LogError("[INSTRUCTION] No se encontró CuttingBoardSnapZone. Desactivando panel.");
            if (instructionPanel != null)
                instructionPanel.SetActive(false);
            return;
        }

        // Ocultar mensaje de completado al inicio
        if (completionMessage != null)
            completionMessage.SetActive(false);

        // Mostrar la primera instrucción
        UpdateInstructionPanel();
    }

    void Update()
    {
        if (cuttingBoard == null)
            return;

        int currentStep = cuttingBoard.GetCurrentStep();

        // Detectar cambio de paso
        if (currentStep != lastStepIndex)
        {
            OnStepChanged(currentStep);
            lastStepIndex = currentStep;
        }

        // Actualizar estado de colocación
        if (currentStep < cuttingBoard.cuttingSteps.Length)
        {
            bool currentlyPlaced = cuttingBoard.cuttingSteps[currentStep].isPlaced;
            if (currentlyPlaced != isPlaced)
            {
                isPlaced = currentlyPlaced;
                UpdateInstructionPanel();
            }
        }

        // Actualizar barra de progreso si está cortando
        UpdateProgressBar();
    }

    void OnStepChanged(int newStep)
    {
        Debug.Log($"[INSTRUCTION] Cambio de paso detectado: Paso {newStep}");

        if (cuttingBoard.AllStepsCompleted())
        {
            ShowCompletionMessage();
            if (hideWhenComplete && instructionPanel != null)
            {
                Invoke("HidePanel", completionMessageDuration);
            }
        }
        else
        {
            isPlaced = false;
            UpdateInstructionPanel();
        }
    }

    void UpdateInstructionPanel()
    {
        if (cuttingBoard.AllStepsCompleted())
            return;

        int currentStep = cuttingBoard.GetCurrentStep();
        if (currentStep >= stepInstructions.Length)
        {
            Debug.LogWarning($"[INSTRUCTION] No hay instrucción configurada para el paso {currentStep}");
            return;
        }

        StepInstruction instruction = stepInstructions[currentStep];
        CuttingBoardSnapZone.CuttingStep step = cuttingBoard.cuttingSteps[currentStep];

        // Actualizar título
        if (instructionTitleText != null)
        {
            instructionTitleText.text = $"Paso {currentStep + 1}: {instruction.stepName}";
        }

        // Actualizar descripción según el estado
        if (instructionDescriptionText != null)
        {
            if (!step.isPlaced)
            {
                // Ingrediente no colocado
                instructionDescriptionText.text = instruction.placeInstructionText;
            }
            else
            {
                // Ingrediente colocado, hay que cortarlo
                instructionDescriptionText.text = instruction.cutInstructionText;
            }
        }

        // Actualizar icono del ingrediente
        if (ingredientIconImage != null && instruction.ingredientIcon != null)
        {
            ingredientIconImage.sprite = instruction.ingredientIcon;
            ingredientIconImage.gameObject.SetActive(true);
        }
        else if (ingredientIconImage != null)
        {
            ingredientIconImage.gameObject.SetActive(false);
        }

        // Actualizar icono del tipo de corte (solo si ya está colocado)
        if (cutTypeIconImage != null)
        {
            if (step.isPlaced && instruction.cutTypeIcon != null)
            {
                cutTypeIconImage.sprite = instruction.cutTypeIcon;
                cutTypeIconImage.gameObject.SetActive(true);
            }
            else
            {
                cutTypeIconImage.gameObject.SetActive(false);
            }
        }

        // Actualizar color del panel
        if (panelBackgroundImage != null)
        {
            panelBackgroundImage.color = instruction.panelColor;
        }

        Debug.Log($"[INSTRUCTION] Panel actualizado para paso {currentStep}: {instruction.stepName}");
    }

    void UpdateProgressBar()
    {
        if (progressSlider == null || cuttingBoard == null)
            return;

        int currentStep = cuttingBoard.GetCurrentStep();
        if (currentStep >= cuttingBoard.cuttingSteps.Length)
        {
            progressSlider.gameObject.SetActive(false);
            return;
        }

        CuttingBoardSnapZone.CuttingStep step = cuttingBoard.cuttingSteps[currentStep];

        // Solo mostrar progreso si el ingrediente está colocado
        if (!step.isPlaced)
        {
            progressSlider.gameObject.SetActive(false);
            return;
        }

        float progress = 0f;

        // Obtener progreso según el tipo de corte
        if (step.useProgressiveGrid && step.progressiveGrid != null)
        {
            progress = step.progressiveGrid.GetProgress();
        }
        else if (step.useSingleLineCut && step.singleLineCut != null)
        {
            progress = step.singleLineCut.GetProgress();
        }
        else if (step.useProgressiveCurveCut && step.progressiveCurveCut != null)
        {
            progress = step.progressiveCurveCut.GetProgress();
        }
        else if (step.useMultiContactCut && step.multiContactCut != null)
        {
            progress = step.multiContactCut.GetProgress();
        }

        // Mostrar barra solo si hay progreso
        if (progress > 0f)
        {
            progressSlider.gameObject.SetActive(true);
            progressSlider.value = progress;
        }
        else
        {
            progressSlider.gameObject.SetActive(false);
        }
    }

    void ShowCompletionMessage()
    {
        if (completionMessage != null)
        {
            completionMessage.SetActive(true);

            // Opcional: Animar o agregar efectos
            Debug.Log("[INSTRUCTION] ¡Todos los pasos completados!");
        }
    }

    void HidePanel()
    {
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }
    }

    // Métodos públicos para control externo
    public void ShowPanel()
    {
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(true);
            UpdateInstructionPanel();
        }
    }

    public void ForceUpdate()
    {
        UpdateInstructionPanel();
    }
}
