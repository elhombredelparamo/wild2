using Godot;

namespace Wild.Data.Inventory
{
    [GlobalClass]
    public partial class HachaPiedraItem : HerramientaItem
    {
        [Export] public override string Id { get; set; } = "hachaPiedra";
        [Export] public override string Name { get; set; } = "Hacha de Piedra";
        [Export] public override string Description { get; set; } = "Un hacha rudimentaria hecha con una piedra tallada atada a un palo resistente con un cordel. Eficaz para talar árboles pequeños y arbustos gruesos.";
        [Export] public override string IconPath { get; set; } = "res://assets/textures/items/hachapiedra/ultra.png";
        
        [Export] public override int StackSize { get; set; } = 1;
        [Export] public override float Weight { get; set; } = 1.2f;

        // Constructor para asignar valores por defecto a propiedades heredadas si es necesario
        public HachaPiedraItem()
        {
            Efficiency = 1.5f;
            MaxDurability = 100;
            CurrentDurability = 100;
        }
    }
}
