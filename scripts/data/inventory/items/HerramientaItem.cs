using Godot;

namespace Wild.Data.Inventory
{
    [GlobalClass]
    public partial class HerramientaItem : InventoryItem
    {
        [Export] public override string Id { get; set; }
        [Export] public override string Name { get; set; }
        [Export] public override string Description { get; set; }
        [Export] public override string IconPath { get; set; }
        [Export] public override int StackSize { get; set; } = 1;
        [Export] public override float Weight { get; set; } = 0.5f;

        [Export] public int MaxDurability { get; set; }
        [Export] public int CurrentDurability { get; set; }
        [Export] public float Efficiency { get; set; } = 1.0f;
    }
}
