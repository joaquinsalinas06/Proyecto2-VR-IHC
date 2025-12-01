using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Configuración automática para cortar el limón a la mitad con una línea curva.
/// Los valores están hardcodeados para persistir entre sesiones.
/// </summary>
[RequireComponent(typeof(SingleLineCut))]
public class LemonCutSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [Tooltip("Configurar automáticamente al iniciar")]
    public bool autoSetupOnStart = true;

    [Header("Referencias")]
    public Material lineMaterial;

    // ============================================================
    // VALORES FIJOS PARA EL LIMÓN - Edita aquí para cambiar
    // ============================================================
    private const float LINE_THICKNESS = 0.01f;      // Grosor
    private const float REQUIRED_CUT_TIME = 2.5f;     // Tiempo de corte requerido
    
    // Puntos de control para una curva de 5 puntos que se ajusta a la forma del limón
    private static readonly Vector3[] DEFAULT_CURVE_POINTS = new Vector3[5] {
        new Vector3(2.4f, 2f, 0.6f),
        new Vector3(2.4f, 4.2f, -0.4f),
        new Vector3(2.4f, 4.6f, -2.3f),
        new Vector3(2.4f, 4.3f, -3.8f),
        new Vector3(2.4f, 2f, -4.6f)
    };

    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupLemonCut();
        }
    }

    /// <summary>
    /// Configura el sistema de corte con valores hardcodeados para la curva.
    /// </summary>
    public void SetupLemonCut()
    {
        SingleLineCut cutSystem = GetComponent<SingleLineCut>();
        if (cutSystem == null)
        {
            Debug.LogError("[LEMON SETUP] No hay componente SingleLineCut!");
            return;
        }

        // Configurar valores de la curva
        cutSystem.curvePoints = DEFAULT_CURVE_POINTS;
        cutSystem.curveResolution = 20; // Buena resolución por defecto
        cutSystem.lineThickness = LINE_THICKNESS;
        cutSystem.requiredCutTime = REQUIRED_CUT_TIME;

        // Configurar visual
        cutSystem.lineColor = Color.yellow;
        cutSystem.lineEmissionIntensity = 2f;

        // Configurar referencias
        if (lineMaterial != null)
            cutSystem.lineMaterial = lineMaterial;

        cutSystem.knifeTag = "Knife";
        cutSystem.minKnifeVelocity = 0.1f;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(cutSystem);
            Debug.Log("[LEMON SETUP] ✓ Limón configurado con la NUEVA curva de 5 puntos!");
        }
        else
#endif
        {
            Debug.Log("[LEMON SETUP] ✓ Limón configurado en runtime!");
        }
    }
}

#if UNITY_EDITOR
/// <summary>
/// Editor personalizado con botón de setup.
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
            "Aplica una configuración de línea de corte CURVA de 5 puntos por defecto.\n" +
            "Presiona el botón para aplicar la configuración.",
            MessageType.Info
        );

        if (GUILayout.Button("Setup Lemon Cut", GUILayout.Height(40)))
        {
            setup.SetupLemonCut();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Para cambiar valores permanentemente, edita los valores estáticos en este script.\n" +
            "Para ajustes visuales, modifica el array 'Curve Points' en el componente 'Single Line Cut' después de aplicar la configuración.",
            MessageType.Info
        );
    }
}
#endif
