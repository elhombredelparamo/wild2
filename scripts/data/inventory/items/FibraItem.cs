using Godot;

namespace Wild.Data.Inventory
{
    [GlobalClass]
    public partial class FibraItem : MaterialItem
    {
        [Export] public override string Id { get; set; } = "fibra";
        [Export] public override string Name { get; set; } = "Fibra Vegetal";
        [Export] public override string Description { get; set; } = "Fibras extraídas de tallos de hierba y juncos. Se utilizan para trenzar cordeles y elaborar tejidos básicos.";
        [Export] public override string IconPath { get; set; } = "res://assets/textures/items/fibra/ultra.png";
        
        // Peso realista muy bajo (20 gramos por unidad) y gran cantidad por stack
        [Export] public override int StackSize { get; set; } = 100;
        [Export] public override float Weight { get; set; } = 0.02f;
    }
}
