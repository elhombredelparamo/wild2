using System.Collections.Generic;
using Wild.Core.Biomes;

public static class SetaData
{
    public static readonly VegetationData Data = new VegetationData(
        modelPath: "res://assets/models/plants/seta/1/ultra/seta1.glb",
        lootTableId: "seta1",
        minScale: 0.7f,
        maxScale: 1.3f,
        hasCollision: false
    )
    {
        LootTable = new List<LootEntry> { new LootEntry("seta1", 1, 2) },
        SpawnChances = new Dictionary<BiomeId, float>
        {
            { BiomeId.Bosque, 0.02f },
            { BiomeId.Pradera, 0.00f }
        }
    };
}
