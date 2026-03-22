using Godot;
using System;
using Wild.Data.Inventory;

namespace Wild.UI
{
    public partial class InventorySlotUI : PanelContainer
    {
        private InventoryContainer _container;
        private int _slotIndex;
        private InventoryUI _inventoryUI;
        private TextureRect _icon;
        private Label _countLabel;

        public void Setup(InventoryContainer container, int index, InventoryUI ui)
        {
            _container = container;
            _slotIndex = index;
            _inventoryUI = ui;
            Refresh();
        }

        public void Refresh()
        {
            // Limpiar hijos existentes si es necesario o reutilizar
            if (_icon == null) 
            {
                MarginContainer margin = new MarginContainer();
                margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize, 5);
                AddChild(margin);

                _icon = new TextureRect();
                _icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                _icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                _icon.MouseFilter = Control.MouseFilterEnum.Ignore;
                margin.AddChild(_icon);

                _countLabel = new Label();
                _countLabel.HorizontalAlignment = HorizontalAlignment.Right;
                _countLabel.VerticalAlignment = VerticalAlignment.Bottom;
                _countLabel.AddThemeFontSizeOverride("font_size", 14);
                _countLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
                _countLabel.AddThemeConstantOverride("outline_size", 4);
                _countLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
                margin.AddChild(_countLabel);
            }

            var slot = _container.Slots[_slotIndex];

            if (!slot.IsEmpty())
            {
                if (!string.IsNullOrEmpty(slot.Item.IconPath))
                {
                    _icon.Texture = GD.Load<Texture2D>(slot.Item.IconPath);
                }
                _icon.Visible = true;
                
                _countLabel.Text = slot.Quantity > 1 ? slot.Quantity.ToString() : "";
                _countLabel.Visible = slot.Quantity > 1;
                
                TooltipText = $"{slot.Item.Name}\n{slot.Item.Description}\nPeso: {slot.TotalWeight:F2} kg";
            }
            else
            {
                _icon.Visible = false;
                _countLabel.Visible = false;
                TooltipText = "Espacio vacío";
            }
        }

        // --- Drag & Drop ---

        public override Variant _GetDragData(Vector2 atPosition)
        {
            var slot = _container.Slots[_slotIndex];
            if (slot.IsEmpty()) return default;

            // Datos que pasamos
            var drugData = new Godot.Collections.Dictionary();
            drugData["container"] = _container;
            drugData["slot_index"] = _slotIndex;

            // Visualización del drag
            TextureRect preview = new TextureRect();
            if (!string.IsNullOrEmpty(slot.Item.IconPath))
                preview.Texture = GD.Load<Texture2D>(slot.Item.IconPath);
            
            preview.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            preview.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            preview.CustomMinimumSize = new Vector2(64, 64);
            preview.Modulate = new Color(1, 1, 1, 0.7f);
            
            // Centrar el preview en el ratón
            Control previewContainer = new Control();
            previewContainer.AddChild(preview);
            preview.Position = -preview.CustomMinimumSize / 2;
            
            SetDragPreview(previewContainer);

            return drugData;
        }

        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            if (data.VariantType != Variant.Type.Dictionary) return false;
            var dict = (Godot.Collections.Dictionary)data;
            return dict.ContainsKey("container") && dict.ContainsKey("slot_index");
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            var dict = data.AsGodotDictionary();
            var sourceContainer = dict["container"].As<InventoryContainer>();
            int sourceIndex = dict["slot_index"].AsInt32();

            // Ejecutar la lógica de movimiento
            if (sourceContainer != null && sourceContainer.MoveItem(sourceIndex, _container, _slotIndex))
            {
                _inventoryUI.RefreshAll();
            }
        }
    }
}
