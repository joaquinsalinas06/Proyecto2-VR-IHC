using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Script helper para configurar automáticamente una grilla 3x3
/// Agrega este componente al prefab del pescado y presiona el botón "Setup Grid"
/// </summary>
[RequireComponent(typeof(ProgressiveCutGrid))]
public class GridSetup3x3 : MonoBehaviour
{
    [Header("Configuración de Grilla 3x3")]
    [Tooltip("DESACTIVADO: Los valores están hardcodeados en el código")]
    public bool autoSetup = true;

    [Header("Solo Visual - Valores Hardcodeados en Código")]
    [Tooltip("Material para las líneas de corte")]
    public Material lineMaterial;

    [Tooltip("Sonido al completar cada línea")]
    public AudioClip lineCompleteSound;

    // VALORES FIJOS - Edita estos valores aquí para cambiar la configuración
    private const float GRID_WIDTH = 0.294f;     // Ancho del pescado (X)
    private const float GRID_LENGTH = 0.5f;      // Largo del pescado (Z)
    private const float LINE_Y_POSITION = -0.06f; // Altura Y de las líneas
    private const float LINE_THICKNESS = 0.003f;  // Grosor de líneas
    private const float REQUIRED_PROGRESS = 0.8f; // 80% para completar

    // Posiciones X de las líneas verticales (según tu imagen)
    private static readonly float[] VERTICAL_X_POSITIONS = { -0.05768667f, 0.05766866f };

    // Posiciones Z de las líneas horizontales (según tu imagen)
    private static readonly float[] HORIZONTAL_Z_POSITIONS = { -0.06f, 0.107f };

    void Start()
    {
        if (autoSetup)
        {
            SetupGrid();
        }
    }

    /// <summary>
    /// Configura automáticamente la grilla 3x3
    /// Crea 4 líneas horizontales y 4 líneas verticales
    /// </summary>
    public void SetupGrid()
    {
        ProgressiveCutGrid grid = GetComponent<ProgressiveCutGrid>();
        if (grid == null)
        {
            Debug.LogError("[GRID SETUP] No hay componente ProgressiveCutGrid!");
            return;
        }

        // Para una grilla 3x3, necesitamos 4 líneas horizontales y 4 verticales
        // Total: 8 líneas (primero horizontales, luego verticales para mejor UX)

        // Para grilla 3x3, solo necesitamos líneas INTERNAS (no los bordes)
        // 2 líneas horizontales + 2 líneas verticales = 4 líneas totales
        ProgressiveCutGrid.CutLine[] lines = new ProgressiveCutGrid.CutLine[4];

        // LÍNEAS HORIZONTALES INTERNAS (2 líneas para dividir en 3 filas)
        float horizontalSpacing = gridLength / 3f;

        for (int i = 0; i < 2; i++)
        {
            lines[i] = new ProgressiveCutGrid.CutLine();
            lines[i].type = ProgressiveCutGrid.CutLineType.Horizontal;

            // Posición: skip primera (i=0 es borde), usar i+1
            float zPos = -gridLength / 2f + ((i + 1) * horizontalSpacing);
            lines[i].localPosition = new Vector3(0, 0.01f, zPos); // Más arriba (1cm)

            lines[i].length = gridWidth;
            lines[i].thickness = 0.003f; // Más grueso para mejor detección
        }

        // LÍNEAS VERTICALES INTERNAS (2 líneas para dividir en 3 columnas)
        float verticalSpacing = gridWidth / 3f;

        for (int i = 0; i < 2; i++)
        {
            lines[2 + i] = new ProgressiveCutGrid.CutLine();
            lines[2 + i].type = ProgressiveCutGrid.CutLineType.Vertical;

            // Posición: skip primera (i=0 es borde), usar i+1
            float xPos = -gridWidth / 2f + ((i + 1) * verticalSpacing);
            lines[2 + i].localPosition = new Vector3(xPos, 0.01f, 0); // Más arriba (1cm)

            lines[2 + i].length = gridLength;
            lines[2 + i].thickness = 0.003f; // Más grueso
        }

        // Asignar las líneas al grid
#if UNITY_EDITOR
        // En el editor, modificar el objeto serializado
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
            lineElement.FindPropertyRelative("requiredProgress").floatValue = requiredProgress;
        }

        // Configurar propiedades visuales
        so.FindProperty("lineMaterial").objectReferenceValue = lineMaterial;
        so.FindProperty("lineColor").colorValue = lineColor;
        so.FindProperty("lineEmissionIntensity").floatValue = lineEmissionIntensity;
        so.FindProperty("lineCompleteSound").objectReferenceValue = lineCompleteSound;

        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(grid);
        EditorUtility.SetDirty(gameObject);

        // IMPORTANTE: Guardar en el prefab si este objeto es parte de un prefab
        UnityEngine.Object prefabRoot = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
        if (prefabRoot != null)
        {
            // Es una instancia de prefab, guardar cambios en el prefab
            PrefabUtility.RecordPrefabInstancePropertyModifications(grid);
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            Debug.Log("[GRID SETUP] ✓ Cambios guardados en el PREFAB automáticamente!");
        }
        else
        {
            // Es un objeto en la escena, guardar la escena
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        Debug.Log("[GRID SETUP] ✓ Grilla 3x3 configurada exitosamente! 4 líneas internas creadas (2 horizontales + 2 verticales)");
#else
        // En runtime, asignar directamente
        grid.cutLines = lines;
        grid.lineMaterial = lineMaterial;
        grid.lineColor = lineColor;
        grid.lineEmissionIntensity = lineEmissionIntensity;
        grid.lineCompleteSound = lineCompleteSound;

        Debug.Log("[GRID SETUP] ✓ Grilla 3x3 configurada en runtime!");
#endif
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
        EditorGUILayout.HelpBox("Presiona el botón para configurar automáticamente una grilla 3x3 con 8 líneas de corte.", MessageType.Info);

        if (GUILayout.Button("Setup Grid 3x3", GUILayout.Height(40)))
        {
            setup.SetupGrid();
        }
    }
}
#endif
