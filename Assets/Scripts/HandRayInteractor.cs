using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ray interactor simple para apuntar a botones UI desde las manos VR
/// Dibuja un rayo desde la mano y permite hacer click con pellizco
/// Compatible con Meta XR SDK - No requiere XR Interaction Toolkit
/// </summary>
public class HandRayInteractor : MonoBehaviour
{
    [Header("Ray Configuration")]
    [Tooltip("Origen del ray (ej: IndexTip de la mano)")]
    public Transform rayOrigin;

    [Tooltip("Longitud máxima del ray")]
    public float rayLength = 5f;

    [Tooltip("Grosor del LineRenderer")]
    public float rayWidth = 0.005f;

    [Header("Hand Tracking")]
    [Tooltip("Qué mano es (Left o Right) - Se auto-detecta si no se especifica")]
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;

    [Tooltip("Auto-detectar controlador según el nombre del Ray Origin")]
    public bool autoDetectController = true;

    [Header("Click Input")]
    [Tooltip("Qué botón usar para hacer click")]
    public ClickInputType clickInput = ClickInputType.GripTrigger;

    public enum ClickInputType
    {
        IndexTrigger,       // Gatillo del índice (frontal)
        GripTrigger,        // Gatillo lateral (grip) - RECOMENDADO
        ButtonA,            // Botón A (solo mano derecha)
        ButtonB,            // Botón B (solo mano derecha)
        ButtonX,            // Botón X (solo mano izquierda)
        ButtonY             // Botón Y (solo mano izquierda)
    }

    [Header("Visual Feedback")]
    public Color rayColorNormal = Color.cyan;
    public Color rayColorHover = Color.yellow;
    public Color rayColorClick = Color.green;

    [Tooltip("Material para el punto de destino")]
    public Material reticleMaterial;

    [Tooltip("Tamaño del punto de destino")]
    public float reticleSize = 0.02f;

    private LineRenderer lineRenderer;
    private GameObject reticle;
    private Camera vrCamera;
    private GameObject currentTarget;
    private bool wasPinching = false;

    void Start()
    {
        Debug.Log($"[RAY] ===== HandRayInteractor iniciando en {gameObject.name} =====");

        // Crear LineRenderer para el ray
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = rayWidth;
        lineRenderer.endWidth = rayWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = rayColorNormal;
        lineRenderer.endColor = rayColorNormal;
        lineRenderer.positionCount = 2;

        // Crear reticle (punto al final del ray)
        reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        reticle.name = "RayReticle";
        reticle.transform.localScale = Vector3.one * reticleSize;
        Destroy(reticle.GetComponent<Collider>());

        if (reticleMaterial != null)
            reticle.GetComponent<Renderer>().material = reticleMaterial;
        else
            reticle.GetComponent<Renderer>().material.color = rayColorNormal;

        // Obtener cámara VR
        vrCamera = Camera.main;
        if (vrCamera == null)
        {
            vrCamera = FindObjectOfType<Camera>();
        }

        // Si no hay ray origin, usar este transform
        if (rayOrigin == null)
        {
            rayOrigin = transform;
            Debug.LogWarning($"[RAY] Ray Origin no asignado, usando transform de {gameObject.name}");
        }

        // Auto-detectar controlador según el nombre del Ray Origin
        if (autoDetectController && rayOrigin != null)
        {
            string originName = rayOrigin.name.ToLower();
            if (originName.Contains("left"))
            {
                controller = OVRInput.Controller.LTouch;
                Debug.Log($"[RAY] 🔍 Auto-detectado: Mano IZQUIERDA (LTouch) desde '{rayOrigin.name}'");
            }
            else if (originName.Contains("right"))
            {
                controller = OVRInput.Controller.RTouch;
                Debug.Log($"[RAY] 🔍 Auto-detectado: Mano DERECHA (RTouch) desde '{rayOrigin.name}'");
            }
            else
            {
                Debug.LogWarning($"[RAY] ⚠️ No se pudo auto-detectar la mano desde '{rayOrigin.name}'. Usando controlador configurado: {controller}");
            }
        }

        if (vrCamera == null)
        {
            Debug.LogError($"[RAY] ❌ NO se encontró cámara VR! El script no funcionará.");
        }
        else
        {
            Debug.Log($"[RAY] ✓ Cámara VR encontrada: {vrCamera.name}");
        }

        Debug.Log($"[RAY] ✓ Configuración:");
        Debug.Log($"[RAY]   - Ray Origin: {rayOrigin.name}");
        Debug.Log($"[RAY]   - Controller: {controller}");
        Debug.Log($"[RAY]   - Click Input: {clickInput}");
    }

    void Update()
    {
        if (rayOrigin == null || vrCamera == null)
        {
            // Debug solo una vez
            if (Time.frameCount % 300 == 0) // Cada 5 segundos aprox
            {
                Debug.LogWarning($"[RAY] ⚠️ Update detenido: rayOrigin={rayOrigin != null}, vrCamera={vrCamera != null}");
            }
            return;
        }

        // Detectar input según el tipo configurado
        bool isPinching = false;

        switch (clickInput)
        {
            case ClickInputType.IndexTrigger:
                isPinching = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, controller);
                break;

            case ClickInputType.GripTrigger:
                isPinching = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, controller);
                break;

            case ClickInputType.ButtonA:
                isPinching = OVRInput.Get(OVRInput.Button.One, controller);
                break;

            case ClickInputType.ButtonB:
                isPinching = OVRInput.Get(OVRInput.Button.Two, controller);
                break;

            case ClickInputType.ButtonX:
                isPinching = OVRInput.Get(OVRInput.Button.Three, controller);
                break;

            case ClickInputType.ButtonY:
                isPinching = OVRInput.Get(OVRInput.Button.Four, controller);
                break;
        }

        // Debug: Detectar cuando se presiona el botón por primera vez
        if (isPinching && !wasPinching)
        {
            Debug.Log($"[RAY] 🎮 Botón {clickInput} PRESIONADO en {controller}");
        }

        // Raycast desde la mano
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        RaycastHit hit;
        // IMPORTANTE: QueryTriggerInteraction.Collide permite detectar colliders con isTrigger = true
        bool hitSomething = Physics.Raycast(ray, out hit, rayLength, ~0, QueryTriggerInteraction.Collide);

        // Debug temporal: mostrar SIEMPRE si el ray golpea algo
        if (Time.frameCount % 60 == 0) // Cada segundo
        {
            Debug.Log($"[RAY DEBUG] Raycast result: {hitSomething}. Ray: origin={rayOrigin.position}, direction={rayOrigin.forward}");
        }

        Vector3 endPoint = hitSomething ? hit.point : ray.GetPoint(rayLength);

        // Debug: Mostrar qué objeto está siendo apuntado (solo cuando cambia)
        if (hitSomething && (currentTarget == null || currentTarget != hit.collider.gameObject))
        {
            Debug.Log($"[RAY] 👉 Apuntando a: '{hit.collider.gameObject.name}' (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
        }
        else if (!hitSomething && currentTarget != null)
        {
            Debug.Log($"[RAY] Ya no apunta a nada");
        }

        // Actualizar LineRenderer
        lineRenderer.SetPosition(0, rayOrigin.position);
        lineRenderer.SetPosition(1, endPoint);

        // Actualizar reticle
        reticle.transform.position = endPoint;
        reticle.transform.rotation = Quaternion.LookRotation((endPoint - rayOrigin.position).normalized);

        // Verificar si apuntamos a un botón UI
        GameObject hitObject = hitSomething ? hit.collider.gameObject : null;
        Button button = null;

        if (hitObject != null)
        {
            // Buscar botón en el objeto o en sus padres
            button = hitObject.GetComponent<Button>();
            if (button == null)
            {
                button = hitObject.GetComponentInParent<Button>();
            }
        }

        // Cambiar color del ray según estado
        if (button != null && button.interactable)
        {
            if (isPinching)
            {
                lineRenderer.startColor = rayColorClick;
                lineRenderer.endColor = rayColorClick;
                reticle.GetComponent<Renderer>().material.color = rayColorClick;
            }
            else
            {
                lineRenderer.startColor = rayColorHover;
                lineRenderer.endColor = rayColorHover;
                reticle.GetComponent<Renderer>().material.color = rayColorHover;
            }
        }
        else
        {
            lineRenderer.startColor = rayColorNormal;
            lineRenderer.endColor = rayColorNormal;
            reticle.GetComponent<Renderer>().material.color = rayColorNormal;
        }

        // Click en el botón cuando se hace pellizco
        if (button != null)
        {
            // Debug detallado
            if (isPinching && !wasPinching)
            {
                Debug.Log($"[RAY] 🟢 INTENTANDO CLICK en: {button.gameObject.name}");
                Debug.Log($"[RAY] - Interactable: {button.interactable}");
                Debug.Log($"[RAY] - OnClick listeners: {button.onClick.GetPersistentEventCount()}");

                if (button.interactable)
                {
                    Debug.Log($"[RAY] ✅ EJECUTANDO CLICK!");
                    button.onClick.Invoke();

                    // Feedback háptico
                    OVRInput.SetControllerVibration(0.3f, 0.5f, controller);
                }
                else
                {
                    Debug.LogWarning($"[RAY] ❌ Botón NO es interactable!");
                }
            }
        }

        currentTarget = hitObject;
        wasPinching = isPinching;
    }

    void OnDestroy()
    {
        if (reticle != null)
            Destroy(reticle);
    }

    // Activar/desactivar el ray
    public void SetRayActive(bool active)
    {
        if (lineRenderer != null)
            lineRenderer.enabled = active;
        if (reticle != null)
            reticle.SetActive(active);
    }
}
