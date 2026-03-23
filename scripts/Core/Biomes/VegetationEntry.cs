/// <summary>
/// Define un tipo de vegetal que puede aparecer en un bioma.
/// El spawn es determinista (basado en semilla) e independiente por tipo:
/// cada planta "tira su propio dado" en cada tile.
/// </summary>
public struct VegetationEntry
{
    /// <summary>
    /// Ruta al modelo en calidad ultra. VegetationLibrary lo reemplazará
    /// por la calidad activa si existe la variante correspondiente.
    /// Ejemplo: "res://assets/models/vegetation/seta/1/ultra/seta1.glb"
    /// </summary>
    public string ModelPath;
    public string ItemId;
    public float SpawnChance;
    public float MinScale;
    public float MaxScale;

    public VegetationEntry(string modelPath, float spawnChance, float minScale = 0.8f, float maxScale = 1.2f, string itemId = null)
    {
        ModelPath   = modelPath;
        ItemId      = itemId;
        SpawnChance = spawnChance;
        MinScale    = minScale;
        MaxScale    = maxScale;
    }
}
