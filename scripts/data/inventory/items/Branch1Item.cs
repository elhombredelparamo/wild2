using Godot;

namespace Wild.Data.Inventory
{
    [GlobalClass]
    public partial class Branch1Item : MaterialItem
    {
        [Export] public override string Id { get; set; } = "branch1";
        [Export] public override string Name { get; set; } = "Rama";
        [Export] public override string Description { get; set; } = "Una rama seca que cayó de un árbol. Útil para herramientas básicas o fuego.";
        [Export] public override string IconPath { get; set; } = "res://assets/textures/items/branch1/ultra.png";
        
        [Export] public override int StackSize { get; set; } = 20;
        [Export] public override float Weight { get; set; } = 0.3f;
    }
}
