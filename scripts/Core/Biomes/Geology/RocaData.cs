using System.Collections.Generic;
using Wild.Core.Biomes;

/// <summary>
/// Definición de la roca básica como objeto geológico recolectable.
/// </summary>
public static class RocaData
{
    public static GeologyData Get()
    {
        var data = new GeologyData(
            modelPath: "res://assets/models/rocks/1/high.glb", 
            lootTableId: "roca_comun",
            minScale: 0.001f, // 140m * 0.001 = 0.14m (tamaño de la palma de la mano aprox)
            maxScale: 0.0015f,
            hasCollision: true
        );

        // Probabilidades por bioma
        data.SpawnChances[BiomeId.Pradera] = 0.05f; // 5% de probabilidad por slot
        data.SpawnChances[BiomeId.Montana] = 0.20f; // 20% en montaña
        data.SpawnChances[BiomeId.Bosque]  = 0.08f; 
        data.SpawnChances[BiomeId.Costa]   = 0.10f;

        // Botín placeholder
        data.LootTable.Add(new LootEntry("piedra1", 1, 1));

        return data;
    }
}
