using System.Collections.Generic;
using Wild.Core.Biomes;

// REGLA: Nombre de clase = JuncoData
public static class JuncoData
{
    public static readonly VegetationData Data = new VegetationData(
        modelPath: "res://assets/models/plants/junco/1/ultra/junco1.glb",
        lootTableId: null,
        minScale: 0.014f,
        maxScale: 0.018f,
        hasCollision: false
    )
    {
        LootTable = new List<LootEntry>(),
        // PROBABILIDADES: Suma no debe superar 1.0 por bioma
        SpawnChances = new Dictionary<BiomeId, float>
        {
            { BiomeId.Costa, 0.40f }
        }
    };
}
