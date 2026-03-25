using System.Collections.Generic;

public static class RobleData
{
    public static readonly VegetationData Data = new VegetationData(
        modelPath: "res://assets/models/trees/roble/1/ultra/roble1.glb",
        itemId: null, // Si fuera recolectable
        minScale: 0.8f,
        maxScale: 1.4f
    )
    {
        SpawnChances = new Dictionary<BiomeId, float>
        {
            { BiomeId.Bosque, 0.08f },
            { BiomeId.Pradera, 0.01f }
        }
    };
}
