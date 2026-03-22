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
                var containerBtn = new InventoryContainerButtonUI();
                _containerList.AddChild(containerBtn);
                containerBtn.Setup(container, this, _selectedContainer == container);
            }
        }

        public void OnContainerSelected(InventoryContainer container)
        {
            _selectedContainer = container;
            UpdateBottomBar(); // Refrescar para visualización de selección
            UpdateContentArea(container);
            GD.Print($"[UI][InventoryUI] Contenedor seleccionado: {container.Name}");
        }

        public void RefreshAll()
        {
            UpdateBottomBar();
            if (_selectedContainer != null)
            {
                UpdateContentArea(_selectedContainer);
            }
        }

        private void UpdateContentArea(InventoryContainer container)
        {
            if (_slotGrid == null) return;

            // Limpiar slots anteriores
            foreach (Node child in _slotGrid.GetChildren())
            {
                child.QueueFree();
            }

            for (int i = 0; i < container.Slots.Count; i++)
            {
                var slotUI = new InventorySlotUI();
                slotUI.CustomMinimumSize = new Vector2(90, 90);
                
                // Estilo básico para el slot (puedes mover esto a InventorySlotUI.cs si prefieres)
                StyleBoxFlat style = new StyleBoxFlat();
                style.BgColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
                style.SetBorderWidthAll(2);
                style.BorderColor = new Color(0.3f, 0.3f, 0.3f);
                slotUI.AddThemeStyleboxOverride("panel", style);
                
                _slotGrid.AddChild(slotUI);
                slotUI.Setup(container, i, this);
            }
        }

        public bool IsOpen()
        {
            return Visible;
        }
    }
}
