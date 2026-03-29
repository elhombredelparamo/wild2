using System.Collections.Generic;

/// <summary>
/// Estructura base para los datos de un tipo de vegetación.
/// Utilizada por las clases estáticas individuales para definir sus propiedades.
/// </summary>
public struct VegetationData
{
    public string ModelPath;
    public string ItemId;
    public float MinScale;
    public float MaxScale;
    
    public Dictionary<BiomeId, float> SpawnChances;
    public bool HasCollision;

    public VegetationData(string modelPath, string itemId = null, float minScale = 0.8f, float maxScale = 1.2f, bool hasCollision = true)
    {
        ModelPath = modelPath;
        ItemId = itemId;
        MinScale = minScale;
        MaxScale = maxScale;
        HasCollision = hasCollision;
        SpawnChances = new Dictionary<BiomeId, float>();
    }
}
