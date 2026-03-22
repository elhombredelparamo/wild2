using Godot;

public partial class CostaBioma : BiomaType
{
    public override string Name => "Costa";
    // Color arena claro
    public override Color BaseColor => new Color(0.95f, 0.9f, 0.6f);
    // Justo por encima del nivel del mar (0)
    public override float BaseHeight => 2.0f;
    public override float HeightVariation => 3.0f;
}
