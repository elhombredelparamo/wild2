using Godot;

namespace Wild.Data.Inventory
{
    [GlobalClass]
    public partial class Seta : ConsumibleItem
    {
        // Propiedades explícitas para facilitar la edición
        [Export] public override string Id { get; set; } = "seta1";
        [Export] public override string Name { get; set; }
        [Export] public override string Description { get; set; }
        [Export] public override string IconPath { get; set; }
        [Export] public override int StackSize { get; set; } = 20;
        [Export] public override float Weight { get; set; } = 0.05f;

        [Export] public bool IsPoisonous { get; set; } = false;
    }
}
