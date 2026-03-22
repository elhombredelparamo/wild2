using Godot;

public partial class OceanoBioma : BiomaType
{
    public override string Name => "Océano";
    // Azul marino
    public override Color BaseColor => new Color(0.0f, 0.35f, 0.75f);
    // Fondo oceánico real: -100 a -60 metros (según biomas.pseudo: -80 ± 20)
    public override float BaseHeight => -80.0f;
    public override float HeightVariation => 20.0f;
}
