using Godot;

namespace Wild.Data.Inventory
{
    [GlobalClass]
    public partial class CordelItem : MaterialItem
    {
        [Export] public override string Id { get; set; } = "cordel";
        [Export] public override string Name { get; set; } = "Cordel";
        [Export] public override string Description { get; set; } = "Un metro de fino cordel trenzado a partir de fibra vegetal. Sirve para hacer ataduras simples o como base para formar sogas más gruesas.";
        [Export] public override string IconPath { get; set; } = "res://assets/textures/items/cordel/ultra.png";
        
        // Peso realista muy bajo (10 gramos por el metro de cordel)
        [Export] public override int StackSize { get; set; } = 50;
        [Export] public override float Weight { get; set; } = 0.01f;
    }
}
