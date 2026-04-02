using System.Collections.Generic;
using Wild.Core.Biomes;

/// <summary>
/// Estructura base para los datos de un tipo de geología (rocas, minerales).
/// </summary>
public struct GeologyData
{
    public string ModelPath;
    public string LootTableId; 
    public List<LootEntry> LootTable; 
    public float MinScale;
    public float MaxScale;
    
    public Dictionary<BiomeId, float> SpawnChances;
    public bool HasCollision;

    public GeologyData(string modelPath, string lootTableId = null, float minScale = 0.8f, float maxScale = 1.2f, bool hasCollision = true)
    {
        ModelPath = modelPath;
        LootTableId = lootTableId;
        LootTable = new List<LootEntry>();
        MinScale = minScale;
        MaxScale = maxScale;
        HasCollision = hasCollision;
        SpawnChances = new Dictionary<BiomeId, float>();
    }
}
