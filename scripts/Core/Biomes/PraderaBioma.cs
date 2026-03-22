using Godot;

public partial class PraderaBioma : BiomaType
{
    public override string Name => "Pradera";
    // Color verde hierba
    public override Color BaseColor => new Color(0.25f, 0.6f, 0.2f);
    // Zonas elevadas tierra adentro
    public override float BaseHeight => 20.0f;
    public override float HeightVariation => 5.0f;

    // Vegetación de pradera (sin colisión).
    // Para añadir más plantas: añade un new VegetationEntry(...) al array.
    public override VegetationEntry[] VegetationEntries => new VegetationEntry[]
    {
        // Hierba — muy frecuente en pradera (SpawnChance = 0.6 → ~60% por tile)
        new VegetationEntry("res://assets/models/plants/hierba/1/ultra/hierba1.glb",
            spawnChance: 0.60f, minScale: 0.8f, maxScale: 1.4f),

        // [ Aquí se añadirán más plantas de pradera: flores silvestres, arbustos, etc. ]
    };
}
