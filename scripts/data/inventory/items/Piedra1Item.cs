using Godot;

namespace Wild.Data.Inventory
{
    [GlobalClass]
    public partial class Piedra1Item : MaterialItem
    {
        [Export] public override string Id { get; set; } = "piedra1";
        [Export] public override string Name { get; set; } = "Piedra";
        [Export] public override string Description { get; set; } = "Una piedra común recogida del suelo. Útil para herramientas básicas o construcción.";
        [Export] public override string IconPath { get; set; } = "res://assets/textures/items/piedra1/ultra.png";
        
        // Peso: ~0.5kg por piedra (tamaño mano)
        // Límite de stack: 10kg -> 20 unidades
        [Export] public override int StackSize { get; set; } = 20;
        [Export] public override float Weight { get; set; } = 0.5f;
    }
}
