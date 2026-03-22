using Godot;

public abstract partial class BiomaType : RefCounted
{
    public abstract string Name { get; }
    public abstract Color BaseColor { get; }
    public abstract float BaseHeight { get; }
    public abstract float HeightVariation { get; }

    /// <summary>
    /// Lista de vegetación que puede aparecer en este bioma.
    /// Cada entrada define un tipo de planta con su propia probabilidad de spawn.
    /// Por defecto vacía — cada bioma concreto puede sobreescribirla.
    /// </summary>
    public virtual VegetationEntry[] VegetationEntries => System.Array.Empty<VegetationEntry>();
}
