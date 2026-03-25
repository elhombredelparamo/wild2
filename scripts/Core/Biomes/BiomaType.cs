using Godot;

public abstract partial class BiomaType : RefCounted
{
    public abstract string Name { get; }
    public abstract BiomeId Id { get; }
    public abstract Color BaseColor { get; }
    public abstract float BaseHeight { get; }
    public abstract float HeightVariation { get; }

    /// <summary>
    /// Lista de vegetación que aparece en este bioma, consultada desde el Registro central.
    /// Acceso O(1) y Zero-Alloc tras el Bake inicial.
    /// </summary>
    public virtual VegetationEntry[] VegetationEntries => VegetationRegistry.GetEntriesForBiome(this.Id);
}
