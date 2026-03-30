using Godot;

namespace Wild.Data.Inventory
{
    [GlobalClass]
    public partial class MimbreItem : MaterialItem
    {
        [Export] public override string Id { get; set; } = "mimbre";
        [Export] public override string Name { get; set; } = "Mimbre";
        [Export] public override string Description { get; set; } = "Material fibroso utilizado para cestería y artesanías.";
        [Export] public override string IconPath { get; set; } = "res://assets/textures/items/mimbre/ultra.png";
        [Export] public override int StackSize { get; set; } = 100;
        [Export] public override float Weight { get; set; } = 0.01f;
    }
}
