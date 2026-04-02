using Wild.Core.Biomes;

/// <summary>
/// Define un tipo de formación geológica que puede aparecer en un bioma.
/// </summary>
public struct GeologyEntry
{
    public string ModelPath;
    public string LootTableId;
    public float  SpawnChance;
    public float  MinScale;
    public float  MaxScale;
    public bool   HasCollision;

    public GeologyEntry(string modelPath, float spawnChance, float minScale = 0.8f, float maxScale = 1.2f, string lootTableId = null, bool hasCollision = true)
    {
        ModelPath   = modelPath;
        LootTableId = lootTableId;
        SpawnChance = spawnChance;
        MinScale    = minScale;
        MaxScale    = maxScale;
        HasCollision = hasCollision;
    }
}
