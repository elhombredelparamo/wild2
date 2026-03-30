using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using Wild.Data.Inventory;
using Wild.UI.Components;

namespace Wild.UI
{
    public partial class InventoryUI : CanvasLayer, IInventoryView
    {
        [Signal] public delegate void OpenedEventHandler();
        [Signal] public delegate void ClosedEventHandler();

        public static InventoryUI Instance { get; private set; }

        private Control _contentPanel;
        private Control _bottomBar;
        private HBoxContainer _containerList;
        private InventorySlotGrid _slotGrid;
        private ScrollContainer _scrollContainer;
        private InventoryContainer _selectedContainer;
        private InventoryContainer _externalContainer;
        private InventoryContextMenu _contextMenu;

        public override void _Ready()
        {
            Instance = this;
            try
            {
                _contentPanel = GetNode<Control>("ContentPanel");
                _bottomBar = GetNode<Control>("BottomBar");
                _containerList = GetNode<HBoxContainer>("BottomBar/ContainerList");
                
                // Crear ScrollContainer para los slots
                _scrollContainer = new ScrollContainer();
                _scrollContainer.Name = "ItemScroll";
                _scrollContainer.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
                _scrollContainer.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
                _contentPanel.AddChild(_scrollContainer);
                _scrollContainer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize, 10);

                // Inicializar cuadrícula de slots usando el nuevo componente
                _slotGrid = new InventorySlotGrid();
                _slotGrid.Name = "SlotGrid";
                _slotGrid.Initialize(6);
                
                _scrollContainer.AddChild(_slotGrid);
                
                // Ocultar por defecto
                Visible = false;

                InitializeContextMenu();
                
                GD.Print("[UI][InventoryUI] Sistema de Inventario UI inicializado con Scroll.");
                
                Wild.Core.Quality.QualityManager.OnIconQualityChanged += RefreshAll;
            }
            catch (Exception e)
            {
                GD.PrintErr($"[ERROR][InventoryUI] Error al inicializar: {e.Message}");
            }
        }

        private void InitializeContextMenu()
        {
            _contextMenu = new InventoryContextMenu();
            _contextMenu.Initialize(this);
            AddChild(_contextMenu);
        }

        public void ShowContextMenu(Vector2 globalPos, InventoryContainer container, int slotIndex)
        {
            _contextMenu.ShowAt(globalPos, container, slotIndex);
        }

        public void ShowContainerContextMenu(Vector2 globalPos, InventoryContainer container)
        {
            _contextMenu.ShowContainerOptionsAt(globalPos, container);
        }

        public void SetExternalContainer(InventoryContainer container)
        {
            _externalContainer = container;
        }

        public void Open()
        {
            Visible = true;
            UpdateBottomBar();
            
            // Si hay un contenedor externo (cofre), seleccionarlo por defecto
            if (_externalContainer != null)
            {
                OnContainerSelected(_externalContainer);
            }
            // Seleccionar el primer contenedor por defecto (ej: Mano Izquierda)
            else if (InventoryManager.Instance != null)
            {
                var containers = InventoryManager.Instance.GetActiveContainers();
                if (containers != null && containers.Count > 0)
                {
                    OnContainerSelected(containers[0]);
                }
            }
            
            GD.Print("[UI][InventoryUI] Inventario abierto.");
            EmitSignal(SignalName.Opened);
        }

        public void Close()
        {
            Visible = false;
            _externalContainer = null;
            GD.Print("[UI][InventoryUI] Inventario cerrado.");
            EmitSignal(SignalName.Closed);
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
            
            // Añadir contenedor externo (cofre) al principio si existe
            if (_externalContainer != null)
            {
                var extBtn = new InventoryContainerButtonUI();
                _containerList.AddChild(extBtn);
                extBtn.Setup(_externalContainer, this, _selectedContainer == _externalContainer);
            }

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
            if (InventoryManager.Instance == null) return;
            
            var activeContainers = InventoryManager.Instance.GetActiveContainers();
            
            if (_selectedContainer == null || !activeContainers.Contains(_selectedContainer))
            {
                _selectedContainer = activeContainers.Count > 0 ? activeContainers[0] : null;
            }

            UpdateBottomBar();
            _slotGrid?.UpdateGrid(_selectedContainer, this);
        }

        private void UpdateContentArea(InventoryContainer container)
        {
            _slotGrid?.UpdateGrid(container, this);
        }

        public bool IsOpen()
        {
            return Visible;
        }

        public override void _ExitTree()
        {
            Wild.Core.Quality.QualityManager.OnIconQualityChanged -= RefreshAll;
            if (Instance == this) Instance = null;
        }
    }
}
