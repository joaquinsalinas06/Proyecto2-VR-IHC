using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Un detector de colisiones genérico para sistemas de corte.
/// Utiliza UnityEvents para notificar a cualquier script que esté escuchando,
/// en lugar de depender de un tipo de componente padre específico.
/// </summary>
public class GenericCutDetector : MonoBehaviour
{
    [Header("Configuración de Detección")]
    [Tooltip("Tag del objeto a detectar (ej. 'Knife')")]
    public string objectTag = "Knife";
    [Tooltip("Una parte del nombre del objeto a detectar (ej. 'Blade')")]
    public string objectNameContains = "Blade";

    [Header("Eventos")]
    [Tooltip("Se dispara cuando el objeto correcto entra en el trigger.")]
    public UnityEvent OnObjectEnter;

    [Tooltip("Se dispara cuando el objeto correcto sale del trigger.")]
    public UnityEvent OnObjectExit;

    private bool isObjectInside = false;

    void Awake()
    {
        // CRÍTICO: Inicializar UnityEvents cuando se añade el componente en runtime
        if (OnObjectEnter == null)
        {
            OnObjectEnter = new UnityEvent();
        }
        if (OnObjectExit == null)
        {
            OnObjectExit = new UnityEvent();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isObjectInside) return; // Ya hay un objeto dentro

        // Comprobar si el objeto que entró es el que buscamos (por tag o nombre)
        bool tagMatch = !string.IsNullOrEmpty(objectTag) && other.CompareTag(objectTag);
        bool nameMatch = !string.IsNullOrEmpty(objectNameContains) && other.name.Contains(objectNameContains);

        if (tagMatch || nameMatch)
        {
            isObjectInside = true;
            if (OnObjectEnter != null)
            {
                OnObjectEnter.Invoke(); // Disparar el evento de entrada
            }
            
            // Debug.Log($"'{other.name}' entró en el detector.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!isObjectInside) return;

        // Comprobar si el objeto que salió es el que estaba dentro
        bool tagMatch = !string.IsNullOrEmpty(objectTag) && other.CompareTag(objectTag);
        bool nameMatch = !string.IsNullOrEmpty(objectNameContains) && other.name.Contains(objectNameContains);
        
        if (tagMatch || nameMatch)
        {
            isObjectInside = false;
            if (OnObjectExit != null)
            {
                OnObjectExit.Invoke(); // Disparar el evento de salida
            }

            // Debug.Log($"'{other.name}' salió del detector.");
        }
    }
}
