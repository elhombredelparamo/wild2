using Godot;
using System;
using Wild.Data.Inventory;

namespace Wild.UI
{
    public partial class InventoryContainerButtonUI : Button
    {
        private InventoryContainer _container;
        private IInventoryView _inventoryUI;
        private TextureRect _icon;
        private Label _label;

        public void Setup(InventoryContainer container, IInventoryView ui, bool isSelected)
        {
            _container = container;
            _inventoryUI = ui;
            
            CustomMinimumSize = new Vector2(80, 80);
            TooltipText = container.Name;

            if (isSelected)
            {
                Modulate = new Color(1.2f, 1.2f, 1, 1); // Amarillo suave/brillante
            }

            // Crear visuales (esto es lo que antes estaba en InventoryUI.CreateContainerSlotUI)
            MarginContainer margin = new MarginContainer();
            margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize, 5);
            margin.MouseFilter = Control.MouseFilterEnum.Ignore;
            AddChild(margin);

            if (!string.IsNullOrEmpty(container.GetIconPath()))
            {
                _icon = new TextureRect();
                _icon.Texture = GD.Load<Texture2D>(container.GetIconPath());
                _icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                _icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                _icon.MouseFilter = Control.MouseFilterEnum.Ignore;
                margin.AddChild(_icon);
            }
            else
            {
                _label = new Label();
                _label.Text = container.Name.Substring(0, Mathf.Min(2, container.Name.Length));
                _label.HorizontalAlignment = HorizontalAlignment.Center;
                _label.VerticalAlignment = VerticalAlignment.Center;
                _label.MouseFilter = Control.MouseFilterEnum.Ignore;
                margin.AddChild(_label);
            }

            Pressed += () => _inventoryUI.OnContainerSelected(_container);
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mb && mb.Pressed)
            {
                if (mb.ButtonIndex == MouseButton.Right)
                {
                    // Solo permitimos menú contextual en el contenedor de mochila
                    if (_container.Id == "backpack_storage")
                    {
                        _inventoryUI.ShowContainerContextMenu(GetGlobalMousePosition(), _container);
                    }
                }
            }
        }

        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            if (data.VariantType != Variant.Type.Dictionary) return false;
            var dict = data.AsGodotDictionary();
            return dict.ContainsKey("container") && dict.ContainsKey("slot_index");
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            var dict = data.AsGodotDictionary();
            var sourceContainer = dict["container"].As<InventoryContainer>();
            int sourceIndex = dict["slot_index"].AsInt32();

            // Evitar mover a sí mismo si ya es el origen (opcional, pero MoveItemToAnySlot lo gestionaría)
            if (sourceContainer == _container) return;

            if (_container.MoveItemToAnySlot(sourceIndex, sourceContainer))
            {
                _inventoryUI.RefreshAll();
            }
        }
    }
}
