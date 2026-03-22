using Godot;

namespace Wild.Data.Inventory
{
    [GlobalClass]
    public partial class Mochila : InventoryItem
    {
        [Export] public override string Id { get; set; } = "mochila1";
        [Export] public override string Name { get; set; } = "Mochila de Cuero";
        [Export] public override string Description { get; set; } = "Una mochila resistente para llevar tus trastos.";
        [Export] public override string IconPath { get; set; } = "res://assets/textures/items/mochila1.png";
        [Export] public override int StackSize { get; set; } = 1;
        [Export] public override float Weight { get; set; } = 1.0f;

        [Export] public int Capacity { get; set; } = 20;
        [Export] public float MaxWeight { get; set; } = 50.0f;
    }
}
