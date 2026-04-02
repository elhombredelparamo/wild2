using System.Collections.Generic;
using Wild.Core.Biomes;

/// <summary>
/// Estructura base para los datos de un tipo de vegetación.
/// Utilizada por las clases estáticas individuales para definir sus propiedades.
/// </summary>
public struct VegetationData
{
    public string ModelPath;
    public string LootTableId; // Identificador único de la tabla de botín (ej: "junco")
    public List<LootEntry> LootTable; 
    public float MinScale;
    public float MaxScale;
    
    public Dictionary<BiomeId, float> SpawnChances;
    public bool HasCollision;
    public bool AlignToNormal;

    public VegetationData(string modelPath, string lootTableId = null, float minScale = 0.8f, float maxScale = 1.2f, bool hasCollision = true, bool alignToNormal = false)
    {
        ModelPath = modelPath;
        LootTableId = lootTableId;
        LootTable = new List<LootEntry>();
        MinScale = minScale;
        MaxScale = maxScale;
        HasCollision = hasCollision;
        AlignToNormal = alignToNormal;
        SpawnChances = new Dictionary<BiomeId, float>();
    }
}
