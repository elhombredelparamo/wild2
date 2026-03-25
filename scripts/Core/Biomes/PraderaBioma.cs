using Godot;

public partial class PraderaBioma : BiomaType
{
    public override string Name => "Pradera";
    public override BiomeId Id => BiomeId.Pradera;
    // Color verde hierba
    public override Color BaseColor => new Color(0.25f, 0.6f, 0.2f);
    // Zonas elevadas tierra adentro
    public override float BaseHeight => 20.0f;
    public override float HeightVariation => 5.0f;

}
