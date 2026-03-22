using Godot;
using System;
using System.Collections.Generic;
using Wild.Data.Inventory;

namespace Wild.UI
{
    public partial class InventoryUI : CanvasLayer
    {
        private Control _contentPanel;
        private Control _bottomBar;
        private HBoxContainer _containerList;
        private GridContainer _slotGrid;
        private InventoryContainer _selectedContainer;

        public override void _Ready()
        {
            try
            {
                _contentPanel = GetNode<Control>("ContentPanel");
                _bottomBar = GetNode<Control>("BottomBar");
                _containerList = GetNode<HBoxContainer>("BottomBar/ContainerList");
                
                // Inicializar cuadrícula de slots
                _slotGrid = new GridContainer();
                _slotGrid.Name = "SlotGrid";
                _slotGrid.Columns = 6; // Ajustable
                _slotGrid.AddThemeConstantOverride("h_separation", 10);
                _slotGrid.AddThemeConstantOverride("v_separation", 10);
                
                _contentPanel.AddChild(_slotGrid);
                
                // Configurar márgenes del grid
                _slotGrid.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize, 20);
                
                // Ocultar label de relleno del tscn
                if (_contentPanel.HasNode("Label"))
                    _contentPanel.GetNode<Control>("Label").Visible = false;

                // Ocultar por defecto
                Visible = false;
                
                GD.Print("[UI][InventoryUI] Sistema de Inventario UI inicializado.");
            }
            catch (Exception e)
            {
                GD.PrintErr($"[ERROR][InventoryUI] Error al inicializar: {e.Message}");
            }
        }

        public void Open()
        {
            Visible = true;
            UpdateBottomBar();
            
            // Seleccionar el primer contenedor por defecto (ej: Mano Izquierda)
            if (InventoryManager.Instance != null)
            {
                var containers = InventoryManager.Instance.GetActiveContainers();
                if (containers != null && containers.Count > 0)
                {
                    OnContainerSelected(containers[0]);
                }
            }
            
            GD.Print("[UI][InventoryUI] Inventario abierto.");
        }

        public void Close()
        {
            Visible = false;
            GD.Print("[UI][InventoryUI] Inventario cerrado.");
        }

        public void UpdateBottomBar()
        {
            if (_containerList == null) return;

            foreach (Node child in _containerList.GetChildren())
            {
                child.QueueFree();
            }

            if (InventoryManager.Instance == null) return;

            var containers = InventoryManager.Instance.GetActiveContainers();
            foreach (var container in containers)
            {
                var containerBtn = CreateContainerSlotUI(container);
                _containerList.AddChild(containerBtn);
            }
        }

        private Button CreateContainerSlotUI(InventoryContainer container)
        {
            Button btn = new Button();
            btn.CustomMinimumSize = new Vector2(80, 80);
            btn.TooltipText = container.Name;
            
            // Estilo básico para resaltar el seleccionado
            if (_selectedContainer == container)
            {
                btn.Modulate = new Color(1.2f, 1.2f, 1.2f); // Brillo
            }

            MarginContainer margin = new MarginContainer();
            margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize, 5);
            margin.MouseFilter = Control.MouseFilterEnum.Ignore;
            btn.AddChild(margin);

            if (!string.IsNullOrEmpty(container.IconPath))
            {
                TextureRect icon = new TextureRect();
                icon.Texture = GD.Load<Texture2D>(container.IconPath);
                icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                icon.MouseFilter = Control.MouseFilterEnum.Ignore;
                margin.AddChild(icon);
            }
            else
            {
                Label label = new Label();
                label.Text = container.Name.Substring(0, Mathf.Min(2, container.Name.Length));
                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.VerticalAlignment = VerticalAlignment.Center;
                margin.AddChild(label);
            }

            btn.Pressed += () => OnContainerSelected(container);

            return btn;
        }

        private void OnContainerSelected(InventoryContainer container)
        {
            _selectedContainer = container;
            UpdateBottomBar(); // Refrescar para visualización de selección
            UpdateContentArea(container);
            GD.Print($"[UI][InventoryUI] Contenedor seleccionado: {container.Name}");
        }

        private void UpdateContentArea(InventoryContainer container)
        {
            if (_slotGrid == null) return;

            // Limpiar slots anteriores
            foreach (Node child in _slotGrid.GetChildren())
            {
                child.QueueFree();
            }

            foreach (var slot in container.Slots)
            {
                var slotUI = CreateItemSlotUI(slot);
                _slotGrid.AddChild(slotUI);
            }
        }

        private PanelContainer CreateItemSlotUI(InventorySlot slot)
        {
            PanelContainer panel = new PanelContainer();
            panel.CustomMinimumSize = new Vector2(90, 90);
            
            // Fondo oscuro para el slot
            StyleBoxFlat style = new StyleBoxFlat();
            style.BgColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            style.SetBorderWidthAll(2);
            style.BorderColor = new Color(0.3f, 0.3f, 0.3f);
            panel.AddThemeStyleboxOverride("panel", style);

            if (!slot.IsEmpty())
            {
                MarginContainer margin = new MarginContainer();
                margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize, 5);
                panel.AddChild(margin);

                // Icono
                if (!string.IsNullOrEmpty(slot.Item.IconPath))
                {
                    TextureRect icon = new TextureRect();
                    icon.Texture = GD.Load<Texture2D>(slot.Item.IconPath);
                    icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                    icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                    margin.AddChild(icon);
                }

                // Cantidad
                if (slot.Quantity > 1)
                {
                    Label countLabel = new Label();
                    countLabel.Text = slot.Quantity.ToString();
                    countLabel.HorizontalAlignment = HorizontalAlignment.Right;
                    countLabel.VerticalAlignment = VerticalAlignment.Bottom;
                    countLabel.AddThemeFontSizeOverride("font_size", 14);
                    countLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
                    countLabel.AddThemeConstantOverride("outline_size", 4);
                    margin.AddChild(countLabel);
                }
                
                panel.TooltipText = $"{slot.Item.Name}\n{slot.Item.Description}\nPeso: {slot.TotalWeight:F2} kg";
            }

            return panel;
        }

        public bool IsOpen()
        {
            return Visible;
        }
    }
}
