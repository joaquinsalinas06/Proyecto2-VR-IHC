using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Script helper para configurar automáticamente una grilla 3x3 para el pescado
/// Los valores están hardcodeados para que NO se reinicien entre sesiones
/// </summary>
[RequireComponent(typeof(ProgressiveCutGrid))]
public class GridSetup3x3 : MonoBehaviour
{
    [Header("Auto Setup")]
    [Tooltip("Si está activado, se configura automáticamente al iniciar")]
    public bool autoSetupOnStart = true;

    [Header("Referencias (Opcional)")]
    public Material lineMaterial;
    public AudioClip lineCompleteSound;

    // ============================================================
    // VALORES FIJOS PARA EL PESCADO - Edita aquí para cambiar
    // Líneas visibles en la parte superior del pescado
    // ============================================================
    private const float LINE_THICKNESS = 0.003f;   // Grosor de líneas
    private const float REQUIRED_PROGRESS = 0.8f;  // 80% para completar

    // Líneas horizontales (2 líneas) - Element 0 y Element 1
    // Valores EXACTOS del Inspector - CONFIGURACIÓN FINAL
    private static readonly Vector3[] HORIZONTAL_LINES = new Vector3[]
    {
        new Vector3(0.04f, -0.055f, -0.107f),   // Element 0: Horizontal
        new Vector3(0f, -0.04f, 0.107f)         // Element 1: Horizontal
    };
    private static readonly float[] HORIZONTAL_LENGTHS = { 0.203f, 0.186f };

    // Líneas verticales (2 líneas) - Element 2 y Element 3
    private static readonly Vector3[] VERTICAL_LINES = new Vector3[]
    {
        new Vector3(-0.05768667f, -0.045f, 0f),  // Element 2: Vertical
        new Vector3(0.05766866f, -0.055f, 0f)    // Element 3: Vertical
    };
    private static readonly float[] VERTICAL_LENGTHS = { 0.461f, 0.367f };

    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupGrid();
        }
    }

    /// <summary>
    /// Configura automáticamente la grilla 3x3 con valores hardcodeados
    /// </summary>
    public void SetupGrid()
    {
        ProgressiveCutGrid grid = GetComponent<ProgressiveCutGrid>();
        if (grid == null)
        {
            Debug.LogError("[GRID SETUP] No hay componente ProgressiveCutGrid!");
            return;
        }

        // Crear array de 4 líneas (2 horizontales + 2 verticales)
        ProgressiveCutGrid.CutLine[] lines = new ProgressiveCutGrid.CutLine[4];

        // LÍNEAS HORIZONTALES (índices 0 y 1)
        for (int i = 0; i < 2; i++)
        {
            lines[i] = new ProgressiveCutGrid.CutLine();
            lines[i].type = ProgressiveCutGrid.CutLineType.Horizontal;
            lines[i].localPosition = HORIZONTAL_LINES[i];
            lines[i].length = HORIZONTAL_LENGTHS[i];
            lines[i].thickness = LINE_THICKNESS;
            lines[i].requiredProgress = REQUIRED_PROGRESS;
        }

        // LÍNEAS VERTICALES (índices 2 y 3)
        for (int i = 0; i < 2; i++)
        {
            lines[2 + i] = new ProgressiveCutGrid.CutLine();
            lines[2 + i].type = ProgressiveCutGrid.CutLineType.Vertical;
            lines[2 + i].localPosition = VERTICAL_LINES[i];
            lines[2 + i].length = VERTICAL_LENGTHS[i];
            lines[2 + i].thickness = LINE_THICKNESS;
            lines[2 + i].requiredProgress = REQUIRED_PROGRESS;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // En el editor, usar SerializedObject para guardar correctamente
            SerializedObject so = new SerializedObject(grid);
            SerializedProperty cutLinesProperty = so.FindProperty("cutLines");

            cutLinesProperty.arraySize = 4;

            for (int i = 0; i < 4; i++)
            {
                SerializedProperty lineElement = cutLinesProperty.GetArrayElementAtIndex(i);

                lineElement.FindPropertyRelative("type").enumValueIndex = (int)lines[i].type;
                lineElement.FindPropertyRelative("localPosition").vector3Value = lines[i].localPosition;
                lineElement.FindPropertyRelative("length").floatValue = lines[i].length;
                lineElement.FindPropertyRelative("thickness").floatValue = lines[i].thickness;
                lineElement.FindPropertyRelative("requiredProgress").floatValue = lines[i].requiredProgress;
            }

            // Configurar propiedades visuales
            if (lineMaterial != null)
                so.FindProperty("lineMaterial").objectReferenceValue = lineMaterial;

            so.FindProperty("lineColor").colorValue = Color.white;
            so.FindProperty("lineEmissionIntensity").floatValue = 2f;

            if (lineCompleteSound != null)
                so.FindProperty("lineCompleteSound").objectReferenceValue = lineCompleteSound;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(grid);

            Debug.Log("[GRID SETUP] ✓ Grilla 3x3 configurada! 4 líneas (2H + 2V)");
        }
        else
#endif
        {
            // En runtime, asignar directamente
            grid.cutLines = lines;
            if (lineMaterial != null)
                grid.lineMaterial = lineMaterial;
            grid.lineColor = Color.white;
            grid.lineEmissionIntensity = 2f;
            if (lineCompleteSound != null)
                grid.lineCompleteSound = lineCompleteSound;

            Debug.Log("[GRID SETUP] ✓ Grilla 3x3 configurada en runtime! 4 líneas (2H + 2V)");
        }
    }
}

#if UNITY_EDITOR
/// <summary>
/// Editor custom para mostrar un botón en el inspector
/// </summary>
[CustomEditor(typeof(GridSetup3x3))]
public class GridSetup3x3Editor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GridSetup3x3 setup = (GridSetup3x3)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Este script usa valores HARDCODEADOS.\nPresiona el botón para aplicar la configuración de grilla 3x3.", MessageType.Info);

        if (GUILayout.Button("Setup Grid 3x3", GUILayout.Height(40)))
        {
            setup.SetupGrid();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Para cambiar posiciones permanentemente, edita las constantes en el código:\n- HORIZONTAL_LINES\n- VERTICAL_LINES\n- HORIZONTAL_LENGTH\n- VERTICAL_LENGTH", MessageType.Info);
    }
}
#endif
