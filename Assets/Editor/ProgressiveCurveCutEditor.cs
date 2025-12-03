using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ProgressiveCurveCut))]
public class ProgressiveCurveCutEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ProgressiveCurveCut script = (ProgressiveCurveCut)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Utilidades de Líneas", EditorStyles.boldLabel);

        if (script.curvedCutLines != null && script.curvedCutLines.Length > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Duplicar configuración de Element 0 a todas las líneas:");
            if (GUILayout.Button("Aplicar a Todas", GUILayout.Width(120)))
            {
                Undo.RecordObject(script, "Duplicar configuración de línea");
                
                var templateLine = script.curvedCutLines[0];
                for (int i = 1; i < script.curvedCutLines.Length; i++)
                {
                    script.curvedCutLines[i].CopyFrom(templateLine);
                }
                
                EditorUtility.SetDirty(script);
                Debug.Log($"Configuración de Element 0 copiada a {script.curvedCutLines.Length - 1} líneas.");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Este botón copia los puntos, resolución, grosor y tiempo de corte de Element 0 a todas las demás líneas. " +
                "Después puedes ajustar cada línea individualmente.",
                MessageType.Info
            );
        }
    }
}
