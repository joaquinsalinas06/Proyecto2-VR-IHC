using UnityEngine;

/// <summary>
/// Script de DEBUG para ver qué está pasando con el limón y el bowl
/// </summary>
public class DebugLemonJuice : MonoBehaviour
{
    void Update()
    {
        // Buscar bowl
        BowlLiquidManager bowl = FindObjectOfType<BowlLiquidManager>();
        
        if (bowl == null)
        {
            Debug.LogError("❌ NO SE ENCUENTRA BowlLiquidManager en la escena!");
            return;
        }

        // Calcular distancia
        float distancia = Vector3.Distance(transform.position, bowl.transform.position);
        
        // Mostrar info cada segundo
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"🍋 Limón: {transform.name}");
            Debug.Log($"🥣 Bowl: {bowl.transform.name}");
            Debug.Log($"📏 Distancia: {distancia:F2}m");
            Debug.Log($"✅ Debería exprimir: {(distancia < 0.3f ? "SÍ" : "NO")}");
            Debug.Log("---");
        }

        // Dibujar línea en Scene view
        Debug.DrawLine(transform.position, bowl.transform.position, 
            distancia < 0.3f ? Color.green : Color.red);
    }

    void OnDrawGizmos()
    {
        BowlLiquidManager bowl = FindObjectOfType<BowlLiquidManager>();
        if (bowl != null)
        {
            float distancia = Vector3.Distance(transform.position, bowl.transform.position);
            
            // Dibujar esfera de activación
            Gizmos.color = distancia < 0.3f ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            
            // Dibujar línea al bowl
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, bowl.transform.position);
        }
    }
}
