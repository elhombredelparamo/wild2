using System.Collections.Generic;

public static class PinoData
{
    public static readonly VegetationData Data = new VegetationData(
        modelPath: "res://assets/models/trees/pino/1/ultra/pino1.glb",
        itemId: null,
        minScale: 0.9f,
        maxScale: 1.5f
    )
    {
        SpawnChances = new Dictionary<BiomeId, float>
        {
            { BiomeId.Bosque, 0.05f }
        }
    };
}
