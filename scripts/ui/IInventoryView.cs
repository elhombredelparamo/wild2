using Godot;
using Wild.Data.Inventory;

namespace Wild.UI
{
    public interface IInventoryView
    {
        void OnContainerSelected(InventoryContainer container);
        void RefreshAll();
        void ShowContextMenu(Vector2 globalPos, InventoryContainer container, int slotIndex);
        void ShowContainerContextMenu(Vector2 globalPos, InventoryContainer container);
    }
}
