using Godot;
using System.Collections.Generic;
using Wild.Core.Player;

namespace Wild.Data.Inventory
{
    [GlobalClass]
    public partial class Venda : ConsumibleItem
    {
        [Export] public override string Id { get; set; } = "venda";
        [Export] public override string Name { get; set; } = "Venda";
        [Export] public override string Description { get; set; } = "Tela limpia para detener hemorragias.";
        [Export] public override string IconPath { get; set; } = "res://assets/textures/items/venda/ultra.png";
        [Export] public override int StackSize { get; set; } = 10;
        [Export] public override float Weight { get; set; } = 0.1f;

        public override List<ItemAction> GetActions(InventoryContainer container, int slotIndex)
        {
            List<ItemAction> actions = base.GetActions(container, slotIndex);
            
            var stats = PlayerManager.Instance?.JugadorActual?.Stats;
            if (stats != null)
            {
                bool haySangrado = false;
                foreach(var part in stats.SaludData.BodyParts.Values)
                {
                    if (part.IsBleeding)
                    {
                        haySangrado = true;
                        break;
                    }
                }

                if (haySangrado)
                {
                    actions.Add(new ItemAction 
                    { 
                        Label = "Vendar Heridas", 
                        Execute = (targetContainer, index) => 
                        {
                            stats.TratarSangrado();
                            // Consumir 1 unidad
                            targetContainer.RemoveItem(index, 1);
                            // Refrescar UI (si existe instancia)
                            Wild.UI.InventoryUI.Instance?.RefreshAll();
                        }
                    });
                }
            }
            
            return actions;
        }
    }
}
