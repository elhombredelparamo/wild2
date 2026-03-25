using System.Collections.Generic;

public static class HierbaData
{
    public static readonly VegetationData Data = new VegetationData(
        modelPath: "res://assets/models/plants/hierba/1/ultra/hierba1.glb",
        itemId: null, // No recolectable por ahora
        minScale: 0.8f,
        maxScale: 1.4f
    )
    {
        SpawnChances = new Dictionary<BiomeId, float>
        {
            { BiomeId.Pradera, 0.60f },
            { BiomeId.Bosque, 0.10f },
            { BiomeId.Costa, 0.05f }
        }
    };
}
