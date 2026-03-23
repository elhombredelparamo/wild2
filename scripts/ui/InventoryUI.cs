using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using Wild.Data.Inventory;

namespace Wild.UI
{
    public partial class InventoryUI : CanvasLayer
    {
        [Signal] public delegate void OpenedEventHandler();
        [Signal] public delegate void ClosedEventHandler();

        public static InventoryUI Instance { get; private set; }

        private Control _contentPanel;
        private Control _bottomBar;
        private HBoxContainer _containerList;
        private GridContainer _slotGrid;
        private ScrollContainer _scrollContainer;
        private InventoryContainer _selectedContainer;
        private InventoryContainer _externalContainer;
        private PopupMenu _contextMenu;
        private InventoryContainer _contextTargetContainer;
        private int _contextTargetSlotIndex;

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

                // Inicializar cuadrícula de slots
                _slotGrid = new GridContainer();
                _slotGrid.Name = "SlotGrid";
                _slotGrid.Columns = 6; // Ajustable
                _slotGrid.AddThemeConstantOverride("h_separation", 10);
                _slotGrid.AddThemeConstantOverride("v_separation", 10);
                
                // Hacer que la cuadrícula se expanda horizontalmente para el scroll vertical
                _slotGrid.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                
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
            _contextMenu = new PopupMenu();
            AddChild(_contextMenu);
            _contextMenu.IdPressed += (id) => {
                if (id == 0) // Destruir
                {
                    if (_contextTargetContainer != null && _contextTargetSlotIndex >= 0 && _contextTargetSlotIndex < _contextTargetContainer.Slots.Count)
                    {
                        var slot = _contextTargetContainer.Slots[_contextTargetSlotIndex];
                        GD.Print($"[SISTEMA][InventoryUI] Destruyendo {slot.Quantity}x {(slot.Item != null ? slot.Item.Name : "null")}");
                        slot.Item = null;
                        slot.Quantity = 0;
                        RefreshAll();
                    }
                }
                else if (id == 1) // Equipar
                {
                    if (_contextTargetContainer != null && _contextTargetSlotIndex >= 0)
                    {
                        var slot = _contextTargetContainer.Slots[_contextTargetSlotIndex];
                        if (slot.Item is Mochila mochila)
                        {
                            InventoryManager.Instance.EquipBackpack(mochila, _contextTargetContainer, _contextTargetSlotIndex);
                            RefreshAll();
                        }
                    }
                }
                else if (id == 2) // Desequipar
                {
                    if (InventoryManager.Instance != null && InventoryManager.Instance.UnequipBackpack())
                    {
                        RefreshAll();
                    }
                }
            };
        }

        public void ShowContextMenu(Vector2 globalPos, InventoryContainer container, int slotIndex)
        {
            _contextTargetContainer = container;
            _contextTargetSlotIndex = slotIndex;
            
            _contextMenu.Clear();
            _contextMenu.AddItem("Destruir", 0);
            
            var slot = container.Slots[slotIndex];
            if (!slot.IsEmpty() && slot.Item is Mochila)
            {
                _contextMenu.AddItem("Equipar", 1);
                
                // Deshabilitar si ya hay una mochila equipada
                if (InventoryManager.Instance != null && InventoryManager.Instance.IsBackpackEquipped())
                {
                    _contextMenu.SetItemDisabled(_contextMenu.GetItemIndex(1), true);
                }
            }

            _contextMenu.Position = (Vector2I)globalPos;
            _contextMenu.Popup();
        }

        public void ShowContainerContextMenu(Vector2 globalPos, InventoryContainer container)
        {
            _contextTargetContainer = container;
            _contextMenu.Clear();

            if (container.Id == "backpack_storage")
            {
                _contextMenu.AddItem("Desequipar", 2);
                
                // Solo si está vacía
                if (!container.IsEmpty())
                {
                    _contextMenu.SetItemDisabled(_contextMenu.GetItemIndex(2), true);
                }
            }

            _contextMenu.Position = (Vector2I)globalPos;
            _contextMenu.Popup();
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
            
            // Si el contenedor seleccionado ya no está activo (ej: mochila desequipada),
            // volvemos a seleccionar el primero disponible.
            if (_selectedContainer == null || !activeContainers.Contains(_selectedContainer))
            {
                if (activeContainers.Count > 0)
                    _selectedContainer = activeContainers[0];
                else
                    _selectedContainer = null;
            }

            UpdateBottomBar();
            
            if (_selectedContainer != null)
                UpdateContentArea(_selectedContainer);
            else
                ClearContentArea();
        }

        private void ClearContentArea()
        {
            if (_slotGrid == null) return;
            foreach (Node child in _slotGrid.GetChildren())
                child.QueueFree();
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

        public override void _ExitTree()
        {
            Wild.Core.Quality.QualityManager.OnIconQualityChanged -= RefreshAll;
            if (Instance == this) Instance = null;
        }
    }
}
