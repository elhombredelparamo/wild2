using Godot;

public partial class BosqueBioma : BiomaType
{
    public override string Name => "Bosque";
    // Verde oscuro
    public override Color BaseColor => new Color(0.1f, 0.4f, 0.15f);
    // Terreno variado
    public override float BaseHeight => 15.0f;
    public override float HeightVariation => 10.0f;

    // Configuración de árboles
    public float VegetationDensity = 0.08f; // Probabilidad de spawn (0-1)
    public string[] TreeModels = new string[] 
    { 
        "res://assets/models/trees/roble/1/ultra/roble1.glb",
        "res://assets/models/trees/pino/1/ultra/pino1.glb"
    };

    // Vegetación del suelo del bosque (sin colisión).
    // Para añadir más plantas: añade un new VegetationEntry(...) al array.
    public override VegetationEntry[] VegetationEntries => new VegetationEntry[]
    {
        // Seta — rara en el suelo del bosque (SpawnChance = 0.02 → ~2% por tile)
        new VegetationEntry("res://assets/models/plants/seta/1/ultra/seta1.glb",
            spawnChance: 0.02f, minScale: 0.7f, maxScale: 1.3f),

        // [ Aquí se añadirán más plantas del bosque: helechos, hongos, etc. ]
    };
}

