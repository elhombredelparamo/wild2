using Godot;

public partial class BosqueBioma : BiomaType
{
    public override string Name => "Bosque";
    public override BiomeId Id => BiomeId.Bosque;
    // Verde oscuro
    public override Color BaseColor => new Color(0.1f, 0.4f, 0.15f);
    // Terreno variado
    public override float BaseHeight => 15.0f;
    public override float HeightVariation => 10.0f;

    // Configuración de visualización (la lógica de spawn ahora está en VegetationRegistry)
    public float VegetationDensity = 0.08f; 
}

