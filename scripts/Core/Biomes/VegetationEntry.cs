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

    /// <summary>
    /// Probabilidad de aparición por tile (0.0 = nunca, 1.0 = siempre).
    /// Valores típicos: hierba común = 0.35, seta rara = 0.02.
    /// </summary>
    public float SpawnChance;

    /// <summary>Escala mínima aleatoria del modelo.</summary>
    public float MinScale;

    /// <summary>Escala máxima aleatoria del modelo.</summary>
    public float MaxScale;

    public VegetationEntry(string modelPath, float spawnChance, float minScale = 0.8f, float maxScale = 1.2f)
    {
        ModelPath   = modelPath;
        SpawnChance = spawnChance;
        MinScale    = minScale;
        MaxScale    = maxScale;
    }
}
