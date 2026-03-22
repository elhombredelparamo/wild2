using Godot;

namespace Wild.Data.Inventory
{
    [GlobalClass]
    public partial class ConsumibleItem : InventoryItem
    {
        [Export] public override string Id { get; set; }
        [Export] public override string Name { get; set; }
        [Export] public override string Description { get; set; }
        [Export] public override string IconPath { get; set; }
        [Export] public override int StackSize { get; set; } = 1;
        [Export] public override float Weight { get; set; } = 0.1f;

        [Export] public float HealthRestore { get; set; }
        [Export] public float HungerRestore { get; set; }
        [Export] public float ThirstRestore { get; set; }
    }
}
