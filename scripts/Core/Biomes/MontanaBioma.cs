using Godot;

public partial class MontanaBioma : BiomaType
{
    public override string Name => "Montana";
    // Gris roca
    public override Color BaseColor => new Color(0.4f, 0.4f, 0.45f);
    // Grandes altitudes
    public override float BaseHeight => 60.0f;
    public override float HeightVariation => 30.0f;
}
