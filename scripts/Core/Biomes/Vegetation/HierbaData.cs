using System.Collections.Generic;
using Wild.Core.Biomes;

public static class HierbaData
{
    public static readonly VegetationData Data = new VegetationData(
        modelPath: "res://assets/models/plants/hierba/1/ultra/hierba1.glb",
        lootTableId: "hierba_comun",
        minScale: 0.8f,
        maxScale: 1.4f,
        hasCollision: false
    )
    {
        LootTable = new List<LootEntry> { new LootEntry("fibra", 1, 3) },
        SpawnChances = new Dictionary<BiomeId, float>
        {
            { BiomeId.Pradera, 0.60f },
            { BiomeId.Bosque, 0.10f },
            { BiomeId.Costa, 0.05f }
        }
    };
}
