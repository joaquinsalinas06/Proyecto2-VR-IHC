using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Configuración automática para cortar el limón a la mitad
/// Los valores están hardcodeados para persistir entre sesiones
/// </summary>
[RequireComponent(typeof(SingleLineCut))]
public class LemonCutSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [Tooltip("Configurar automáticamente al iniciar")]
    public bool autoSetupOnStart = true;

    [Header("Referencias")]
    public GameObject halfPrefab1;
    public GameObject halfPrefab2;
    public Material lineMaterial;

    // ============================================================
    // VALORES FIJOS PARA EL LIMÓN - Edita aquí para cambiar
    // ============================================================
    private const float LINE_LENGTH = 0.15f;          // Largo de la línea
    private const float LINE_THICKNESS = 0.005f;      // Grosor
    private const float REQUIRED_CUT_TIME = 2.5f;     // Tiempo de corte requerido
    private static readonly Vector3 LINE_POSITION = new Vector3(0f, 0f, 0f);  // Centro del limón
    private const bool IS_HORIZONTAL = true;          // Línea horizontal
    private static readonly Vector3 SEPARATION_OFFSET = new Vector3(0f, 0f, 0.02f);  // Separación de mitades

    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupLemonCut();
        }
    }

    /// <summary>
    /// Configura el sistema de corte con valores hardcodeados
    /// </summary>
    public void SetupLemonCut()
    {
        SingleLineCut cutSystem = GetComponent<SingleLineCut>();
        if (cutSystem == null)
        {
            Debug.LogError("[LEMON SETUP] No hay componente SingleLineCut!");
            return;
        }

        // Configurar valores
        cutSystem.linePosition = LINE_POSITION;
        cutSystem.lineLength = LINE_LENGTH;
        cutSystem.lineThickness = LINE_THICKNESS;
        cutSystem.isHorizontal = IS_HORIZONTAL;
        cutSystem.requiredCutTime = REQUIRED_CUT_TIME;
        cutSystem.separationOffset = SEPARATION_OFFSET;

        // Configurar visual
        cutSystem.lineColor = Color.yellow;
        cutSystem.lineEmissionIntensity = 2f;

        // Configurar referencias
        if (halfPrefab1 != null)
            cutSystem.halfPrefab1 = halfPrefab1;
        if (halfPrefab2 != null)
            cutSystem.halfPrefab2 = halfPrefab2;
        if (lineMaterial != null)
            cutSystem.lineMaterial = lineMaterial;

        cutSystem.knifeTag = "Knife";
        cutSystem.minKnifeVelocity = 0.1f;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(cutSystem);
            Debug.Log("[LEMON SETUP] ✓ Limón configurado! Línea en el centro");
        }
        else
#endif
        {
            Debug.Log("[LEMON SETUP] ✓ Limón configurado en runtime!");
        }
    }

    /// <summary>
    /// Ajustar posición de línea manualmente (útil para otros ingredientes)
    /// </summary>
    public void SetLinePosition(Vector3 position)
    {
        SingleLineCut cutSystem = GetComponent<SingleLineCut>();
        if (cutSystem != null)
        {
            cutSystem.linePosition = position;
#if UNITY_EDITOR
            EditorUtility.SetDirty(cutSystem);
#endif
        }
    }
}

#if UNITY_EDITOR
/// <summary>
/// Editor personalizado con botón de setup
/// </summary>
[CustomEditor(typeof(LemonCutSetup))]
public class LemonCutSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LemonCutSetup setup = (LemonCutSetup)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Este script usa valores HARDCODEADOS para el limón.\n" +
            "Presiona el botón para aplicar la configuración.",
            MessageType.Info
        );

        if (GUILayout.Button("Setup Lemon Cut", GUILayout.Height(40)))
        {
            setup.SetupLemonCut();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Para cambiar valores permanentemente:\n" +
            "- LINE_POSITION: Posición de la línea\n" +
            "- LINE_LENGTH: Largo de la línea\n" +
            "- REQUIRED_CUT_TIME: Tiempo necesario para cortar\n" +
            "- SEPARATION_OFFSET: Separación entre mitades",
            MessageType.Info
        );
    }
}
#endif
