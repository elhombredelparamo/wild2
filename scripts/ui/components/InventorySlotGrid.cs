using Godot;
using System.Collections.Generic;
using Wild.Data.Inventory;
using Wild.UI;

namespace Wild.UI.Components
{
    /// <summary>
    /// Componente encargado de gestionar la cuadrícula de slots del inventario.
    /// </summary>
    public partial class InventorySlotGrid : GridContainer
    {
        public void Initialize(int columns = 6)
        {
            Columns = columns;
            AddThemeConstantOverride("h_separation", 10);
            AddThemeConstantOverride("v_separation", 10);
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        }

        public void Clear()
        {
            foreach (Node child in GetChildren())
            {
                child.QueueFree();
            }
        }

        public void UpdateGrid(InventoryContainer container, IInventoryView parentUI)
        {
            Clear();

            if (container == null) return;

            for (int i = 0; i < container.Slots.Count; i++)
            {
                var slotUI = new InventorySlotUI();
                slotUI.CustomMinimumSize = new Vector2(90, 90);
                
                // Estilo básico para el slot
                StyleBoxFlat style = new StyleBoxFlat();
                style.BgColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
                style.SetBorderWidthAll(2);
                style.BorderColor = new Color(0.3f, 0.3f, 0.3f);
                slotUI.AddThemeStyleboxOverride("panel", style);
                
                AddChild(slotUI);
                slotUI.Setup(container, i, parentUI);
            }
        }
    }
}
