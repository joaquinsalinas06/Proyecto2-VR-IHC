using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel de debug en VR que muestra información útil para diagnosticar problemas
/// </summary>
public class VRDebugPanel : MonoBehaviour
{
    [Header("Referencias")]
    public Transform vrCamera;
    public HandRayInteractor[] rayInteractors;

    [Header("Configuración")]
    public bool showPanel = true;
    public float distanceFromCamera = 1.5f;
    public float updateInterval = 0.5f;

    private GameObject debugCanvas;
    private TextMeshProUGUI debugText;
    private float lastUpdateTime = 0f;

    void Start()
    {
        // Buscar cámara VR si no está asignada
        if (vrCamera == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                vrCamera = cam.transform;
        }

        // Buscar ray interactors si no están asignados
        if (rayInteractors == null || rayInteractors.Length == 0)
        {
            rayInteractors = FindObjectsOfType<HandRayInteractor>();
        }

        if (showPanel)
        {
            CreateDebugPanel();
        }
    }

    void CreateDebugPanel()
    {
        // Crear canvas
        debugCanvas = new GameObject("VR_DebugPanel");
        Canvas canvas = debugCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = debugCanvas.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        // Configurar tamaño
        RectTransform canvasRect = debugCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(600, 400);
        debugCanvas.transform.localScale = Vector3.one * 0.001f;

        // Crear panel de fondo
        GameObject panel = new GameObject("Background");
        panel.transform.SetParent(debugCanvas.transform, false);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        // Crear texto
        GameObject textObj = new GameObject("DebugText");
        textObj.transform.SetParent(panel.transform, false);

        debugText = textObj.AddComponent<TextMeshProUGUI>();
        debugText.fontSize = 24;
        debugText.color = Color.white;
        debugText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-20, -20);
        textRect.anchoredPosition = Vector2.zero;

        Debug.Log("[VR DEBUG] Panel de debug creado");
    }

    void Update()
    {
        if (!showPanel || debugCanvas == null || vrCamera == null)
            return;

        // Actualizar posición del panel para que siga a la cámara
        Vector3 targetPosition = vrCamera.position + vrCamera.forward * distanceFromCamera;
        targetPosition.y = vrCamera.position.y; // Mantener a la altura de la cámara

        debugCanvas.transform.position = targetPosition;
        debugCanvas.transform.rotation = Quaternion.LookRotation(debugCanvas.transform.position - vrCamera.position);

        // Actualizar texto periódicamente
        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdateDebugText();
            lastUpdateTime = Time.time;
        }
    }

    void UpdateDebugText()
    {
        if (debugText == null)
            return;

        string text = "=== VR DEBUG ===\n\n";

        // Estado del sistema VR
        text += "<color=yellow>SISTEMA VR:</color>\n";
        text += $"Cámara: {(vrCamera != null ? "✓" : "❌")}\n";
        text += $"FPS: {(1f / Time.deltaTime):F0}\n\n";

        // Estado de los rays
        text += "<color=yellow>RAY INTERACTORS:</color>\n";
        if (rayInteractors != null && rayInteractors.Length > 0)
        {
            text += $"Total: {rayInteractors.Length}\n";

            foreach (var ray in rayInteractors)
            {
                if (ray != null && ray.gameObject.activeInHierarchy)
                {
                    string hand = ray.controller == OVRInput.Controller.LTouch ? "IZQ" : "DER";
                    text += $"  • {hand}: Activo\n";
                }
            }
        }
        else
        {
            text += "<color=red>❌ NO HAY RAYS</color>\n";
        }
        text += "\n";

        // Instrucciones
        text += "<color=yellow>INSTRUCCIONES:</color>\n";
        text += "1. Apunta al botón\n";
        text += "2. Presiona grip trigger\n";
        text += "   (botón trasero)\n\n";

        // Estado de botones
        text += "<color=yellow>BOTONES UI:</color>\n";
        Button[] buttons = FindObjectsOfType<Button>();
        int buttonsOk = 0;
        int buttonsNeedFix = 0;

        foreach (Button btn in buttons)
        {
            BoxCollider col = btn.GetComponent<BoxCollider>();
            if (col != null && col.isTrigger && col.size.z > 0)
            {
                buttonsOk++;
            }
            else
            {
                buttonsNeedFix++;
            }
        }

        text += $"✓ OK: {buttonsOk}  ❌ SIN COLLIDER: {buttonsNeedFix}\n\n";

        // Controles
        text += "<color=yellow>CONTROLES:</color>\n";
        text += "Grip L: " + (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch) ? "✓" : "○") + "\n";
        text += "Grip R: " + (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch) ? "✓" : "○") + "\n";

        debugText.text = text;
    }

    public void TogglePanel()
    {
        showPanel = !showPanel;
        if (debugCanvas != null)
        {
            debugCanvas.SetActive(showPanel);
        }
    }
}
