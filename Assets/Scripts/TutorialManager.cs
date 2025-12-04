using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Gestor de tutorial inicial que explica los controles VR
/// Navega entre paneles con los botones del controlador
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [System.Serializable]
    public class TutorialPage
    {
        [TextArea(3, 10)]
        public string title;
        [TextArea(5, 15)]
        public string description;
        public Sprite illustration; // Opcional: imagen de referencia
    }

    [Header("Tutorial Pages")]
    [Tooltip("Páginas del tutorial en orden")]
    public TutorialPage[] tutorialPages;

    [Header("UI References")]
    public GameObject tutorialCanvas;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Image illustrationImage;
    public GameObject nextButton;
    public GameObject previousButton;
    public TextMeshProUGUI pageCounterText;

    [Header("Navigation")]
    [Tooltip("Usar botones físicos del controlador para navegar (si false, solo se puede con ray/click en UI)")]
    public bool useControllerButtons = false;

    [Tooltip("Botón para avanzar (X en Meta Quest - controlador izquierdo)")]
    public OVRInput.Button nextPageButton = OVRInput.Button.Three;
    [Tooltip("Botón para retroceder (Y en Meta Quest - controlador izquierdo)")]
    public OVRInput.Button previousPageButton = OVRInput.Button.Four;
    [Tooltip("Controlador para navegación")]
    public OVRInput.Controller navigationController = OVRInput.Controller.LTouch;

    [Header("Auto-Hide")]
    [Tooltip("Si true, oculta el tutorial automáticamente al finalizar")]
    public bool autoHideOnComplete = true;
    [Tooltip("Tiempo para ocultar después de completar (segundos)")]
    public float hideDelay = 2f;

    private int currentPageIndex = 0;
    private bool tutorialCompleted = false;
    private float buttonCooldown = 0.3f;
    private float lastButtonPressTime = 0f;

    void Start()
    {
        if (tutorialPages == null || tutorialPages.Length == 0)
        {
            Debug.LogWarning("[TUTORIAL] No hay páginas configuradas. Desactivando tutorial.");
            if (tutorialCanvas != null)
                tutorialCanvas.SetActive(false);
            return;
        }

        // Mostrar la primera página
        ShowPage(0);
    }

    void Update()
    {
        if (tutorialCompleted)
            return;

        // Si no se usan botones del controlador, salir (se controla por UI clicks)
        if (!useControllerButtons)
            return;

        // Cooldown para evitar doble pulsación
        if (Time.time - lastButtonPressTime < buttonCooldown)
            return;

        // Navegación con botones VR
        if (OVRInput.GetDown(nextPageButton, navigationController))
        {
            NextPage();
            lastButtonPressTime = Time.time;
        }
        else if (OVRInput.GetDown(previousPageButton, navigationController))
        {
            PreviousPage();
            lastButtonPressTime = Time.time;
        }
    }

    void ShowPage(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= tutorialPages.Length)
            return;

        currentPageIndex = pageIndex;
        TutorialPage page = tutorialPages[currentPageIndex];

        // Actualizar textos
        if (titleText != null)
            titleText.text = page.title;

        if (descriptionText != null)
            descriptionText.text = page.description;

        // Actualizar ilustración
        if (illustrationImage != null)
        {
            if (page.illustration != null)
            {
                illustrationImage.sprite = page.illustration;
                illustrationImage.gameObject.SetActive(true);
            }
            else
            {
                illustrationImage.gameObject.SetActive(false);
            }
        }

        // Actualizar botones de navegación
        UpdateNavigationButtons();

        // Actualizar contador de páginas
        if (pageCounterText != null)
        {
            pageCounterText.text = $"Página {currentPageIndex + 1} / {tutorialPages.Length}";
        }

        Debug.Log($"[TUTORIAL] Mostrando página {currentPageIndex + 1}: {page.title}");
    }

    void UpdateNavigationButtons()
    {
        // Botón "Anterior"
        if (previousButton != null)
        {
            previousButton.SetActive(currentPageIndex > 0);
        }

        // Botón "Siguiente" o "Finalizar"
        if (nextButton != null)
        {
            bool isLastPage = currentPageIndex >= tutorialPages.Length - 1;

            // Cambiar texto si es la última página
            TextMeshProUGUI buttonText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isLastPage ? "¡Empezar!" : "Siguiente";
            }
        }
    }

    public void NextPage()
    {
        if (currentPageIndex < tutorialPages.Length - 1)
        {
            ShowPage(currentPageIndex + 1);
        }
        else
        {
            // Última página - Completar tutorial
            CompleteTutorial();
        }
    }

    public void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            ShowPage(currentPageIndex - 1);
        }
    }

    void CompleteTutorial()
    {
        Debug.Log("[TUTORIAL] Tutorial completado!");
        tutorialCompleted = true;

        if (autoHideOnComplete)
        {
            Invoke("HideTutorial", hideDelay);
        }

        // Opcional: Evento para notificar que el tutorial terminó
        // Puedes usar esto para activar otros sistemas del juego
    }

    void HideTutorial()
    {
        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(false);
        }
        Debug.Log("[TUTORIAL] Tutorial ocultado.");
    }

    // Métodos públicos para control externo
    public void ShowTutorial()
    {
        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(true);
            tutorialCompleted = false;
            ShowPage(0);
        }
    }

    public void SkipTutorial()
    {
        CompleteTutorial();
    }
}
