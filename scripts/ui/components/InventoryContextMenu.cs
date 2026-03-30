using Godot;
using Wild.Data.Inventory;
using Wild.UI;

namespace Wild.UI.Components
{
    /// <summary>
    /// Componente encargado de gestionar los menús contextuales del inventario.
    /// </summary>
    public partial class InventoryContextMenu : PopupMenu
    {
        private InventoryContainer _targetContainer;
        private int _targetSlotIndex;
        private IInventoryView _parentUI;

        public void Initialize(IInventoryView parentUI)
        {
            _parentUI = parentUI;
            IdPressed += OnIdPressed;
        }

        private void OnIdPressed(long id)
        {
            if (id == 0) // Destruir
            {
                if (_targetContainer != null && _targetSlotIndex >= 0 && _targetSlotIndex < _targetContainer.Slots.Count)
                {
                    var slot = _targetContainer.Slots[_targetSlotIndex];
                    GD.Print($"[SISTEMA][InventoryUI] Destruyendo {slot.Quantity}x {(slot.Item != null ? slot.Item.Name : "null")}");
                    slot.Item = null;
                    slot.Quantity = 0;
                    _parentUI.RefreshAll();
                }
            }
            else if (id == 1) // Equipar
            {
                if (_targetContainer != null && _targetSlotIndex >= 0)
                {
                    var slot = _targetContainer.Slots[_targetSlotIndex];
                    if (slot.Item is Mochila mochila)
                    {
                        InventoryManager.Instance.EquipBackpack(mochila, _targetContainer, _targetSlotIndex);
                        _parentUI.RefreshAll();
                    }
                }
            }
            else if (id == 2) // Desequipar
            {
                if (InventoryManager.Instance != null && InventoryManager.Instance.UnequipBackpack())
                {
                    _parentUI.RefreshAll();
                }
            }
        }

        public void ShowAt(Vector2 globalPos, InventoryContainer container, int slotIndex)
        {
            _targetContainer = container;
            _targetSlotIndex = slotIndex;
            
            Clear();
            AddItem("Destruir", 0);
            
            var slot = container.Slots[slotIndex];
            if (!slot.IsEmpty() && slot.Item is Mochila)
            {
                AddItem("Equipar", 1);
                if (InventoryManager.Instance != null && InventoryManager.Instance.IsBackpackEquipped())
                {
                    SetItemDisabled(GetItemIndex(1), true);
                }
            }

            Position = (Vector2I)globalPos;
            Popup();
        }

        public void ShowContainerOptionsAt(Vector2 globalPos, InventoryContainer container)
        {
            _targetContainer = container;
            Clear();

            if (container.Id == "backpack_storage")
            {
                AddItem("Desequipar", 2);
                if (!container.IsEmpty())
                {
                    SetItemDisabled(GetItemIndex(2), true);
                }
            }

            Position = (Vector2I)globalPos;
            Popup();
        }
    }
}
