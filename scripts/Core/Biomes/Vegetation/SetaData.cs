using System.Collections.Generic;

public static class SetaData
{
    public static readonly VegetationData Data = new VegetationData(
        modelPath: "res://assets/models/plants/seta/1/ultra/seta1.glb",
        itemId: "seta1",
        minScale: 0.7f,
        maxScale: 1.3f
    )
    {
        SpawnChances = new Dictionary<BiomeId, float>
        {
            { BiomeId.Bosque, 0.02f },
            { BiomeId.Pradera, 0.00f }
        }
    };
}
