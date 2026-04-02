using System.Collections.Generic;
using Wild.Core.Biomes;

public static class RamaData
{
    public static readonly VegetationData Data = new VegetationData(
        modelPath: "res://assets/models/objects/branch/1/ultra.glb",
        lootTableId: "branch1",
        minScale: 0.02f, // La rama no debe medir 100 metros
        maxScale: 0.035f,
        hasCollision: false, // Para no bloquear al jugador físicamente al pisarla
        alignToNormal: true
    )
    {
        LootTable = new List<LootEntry> { new LootEntry("branch1", 1, 1) },
        SpawnChances = new Dictionary<BiomeId, float>
        {
            { BiomeId.Bosque, 0.05f },
            { BiomeId.Pradera, 0.01f }
        }
    };
}
