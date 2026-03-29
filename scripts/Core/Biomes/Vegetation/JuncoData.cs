using System.Collections.Generic;

// REGLA: Nombre de clase = JuncoData
public static class JuncoData
{
    public static readonly VegetationData Data = new VegetationData(
        // RUTA: Siempre apuntar a la versión 'ultra'
        modelPath: "res://assets/models/plants/junco/1/ultra/junco1.glb",
        itemId: null, // null si es decorativo
        minScale: 0.014f, // Altura aprox 85cm
        maxScale: 0.018f  // Altura aprox 110cm
    )
    {
        // PROBABILIDADES: Suma no debe superar 1.0 por bioma
        SpawnChances = new Dictionary<BiomeId, float>
        {
            { BiomeId.Costa, 0.40f }
        }
    };
}
