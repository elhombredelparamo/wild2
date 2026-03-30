using Godot;
using System;
using System.Collections.Generic;
using Wild.Data.Inventory;
using Wild.UI.Components;
using Wild.Core.Deployables.Base;
using Wild.Utils;

namespace Wild.UI
{
    public partial class BuildUI : CanvasLayer, IInventoryView
    {
        [Signal] public delegate void ConfirmedEventHandler();
        [Signal] public delegate void CancelledEventHandler();

        public static BuildUI Instance { get; private set; }

        private Control _contentPanel;
        private Control _bottomBar;
        private HBoxContainer _containerList;
        private InventorySlotGrid _slotGrid;
        private ScrollContainer _scrollContainer;
        private InventoryContainer _selectedContainer;
        private InventoryContainer _constructionContainer;
        private InventoryContextMenu _contextMenu;
        private DeployableResource _recipe;
        
        private VBoxContainer _requirementsList;
        
        private Button _btnConfirm;
        private Button _btnCancel;

        public override void _Ready()
        {
            Instance = this;
            try
            {
                _contentPanel = GetNode<Control>("ContentPanel");
                _bottomBar = GetNode<Control>("BottomBar");
                _containerList = GetNode<HBoxContainer>("BottomBar/ContainerList");
                
                _btnConfirm = GetNode<Button>("ActionButtons/BtnConfirm");
                _btnCancel = GetNode<Button>("ActionButtons/BtnCancel");
                
                _btnConfirm.Pressed += OnConfirmPressed;
                _btnCancel.Pressed += OnCancelPressed;
                
                // Create main layout split
                var splitLayout = new HBoxContainer();
                splitLayout.Name = "SplitLayout";
                _contentPanel.AddChild(splitLayout);
                splitLayout.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize, 10);
                splitLayout.AddThemeConstantOverride("separation", 20);

                // Setup Slot Grid on the Left
                _scrollContainer = new ScrollContainer();
                _scrollContainer.Name = "ItemScroll";
                _scrollContainer.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
                _scrollContainer.VerticalScrollMode = ScrollContainer.ScrollMode.Auto;
                _scrollContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                splitLayout.AddChild(_scrollContainer);

                _slotGrid = new InventorySlotGrid();
                _slotGrid.Name = "SlotGrid";
                _slotGrid.Initialize(6); // Space for throwing items into
                _scrollContainer.AddChild(_slotGrid);

                // Setup Requirements List on the Right
                _requirementsList = new VBoxContainer();
                _requirementsList.Name = "RequirementsList";
                _requirementsList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                splitLayout.AddChild(_requirementsList);
                
                var reqTitle = new Label();
                reqTitle.Text = "Materiales Necesarios";
                reqTitle.HorizontalAlignment = HorizontalAlignment.Center;
                reqTitle.AddThemeColorOverride("font_color", new Color(1, 0.8f, 0)); // Gold title
                _requirementsList.AddChild(reqTitle);
                
                var separator = new HSeparator();
                _requirementsList.AddChild(separator);
                
                Visible = false;

                InitializeContextMenu();
                
                Wild.Core.Quality.QualityManager.OnIconQualityChanged += RefreshAll;
            }
            catch (Exception e)
            {
                Logger.LogError($"[BuildUI] Error initialize: {e.Message}");
            }
        }

        private void InitializeContextMenu()
        {
            _contextMenu = new InventoryContextMenu();
            _contextMenu.Initialize(this);
            AddChild(_contextMenu);
        }

        // Methods to mirror InventoryUI for context menu interactions
        public void ShowContextMenu(Vector2 globalPos, InventoryContainer container, int slotIndex)
        {
            _contextMenu.ShowAt(globalPos, container, slotIndex);
        }

        public void ShowContainerContextMenu(Vector2 globalPos, InventoryContainer container)
        {
            _contextMenu.ShowContainerOptionsAt(globalPos, container);
        }

        public void SetupForDeployable(DeployableResource recipe, InventoryContainer pseudoContainer)
        {
            _recipe = recipe;
            _constructionContainer = pseudoContainer;
            _constructionContainer.OnChanged += RefreshAll;
            
            PopulateRequirements(recipe);
        }

        private void PopulateRequirements(DeployableResource recipe)
        {
            if (_requirementsList == null) return;
            
            // Re-populate children excluding the title & separator (first 2 children)
            for (int i = 2; i < _requirementsList.GetChildCount(); i++)
            {
                _requirementsList.GetChild(i).QueueFree();
            }
            
            foreach (var req in recipe.Requirements)
            {
                var itemId = req.Key;
                var requiredAmount = req.Value;
                var itemObj = InventoryManager.Instance?.GetItemById(itemId);
                string itemName = itemObj != null ? itemObj.Name : itemId;
                
                var label = new Label();
                label.Text = $"{requiredAmount} x {itemName}";
                _requirementsList.AddChild(label);
            }
        }

        public void Open()
        {
            Visible = true;
            UpdateBottomBar();
            
            if (_constructionContainer != null)
            {
                OnContainerSelected(_constructionContainer);
            }
            
            Logger.LogInfo($"[BuildUI] Construcción UI abierta. Visible: {Visible} (Node: {Name})");
        }

        public void Close()
        {
            Visible = false;
            if (_constructionContainer != null)
            {
                _constructionContainer.OnChanged -= RefreshAll;
                _constructionContainer = null;
            }
            Logger.LogInfo("[BuildUI] Construcción UI cerrada.");
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
            
            // Site Container first
            if (_constructionContainer != null)
            {
                var extBtn = new InventoryContainerButtonUI();
                _containerList.AddChild(extBtn);
                extBtn.Setup(_constructionContainer, this, _selectedContainer == _constructionContainer);
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
            Logger.LogInfo($"[BuildUI] Evento OnContainerSelected disparado para: {container?.Name}");
            _selectedContainer = container;
            UpdateBottomBar();
            UpdateContentArea(container);
        }

        public void RefreshAll()
        {
            if (InventoryManager.Instance == null) return;
            
            var activeContainers = InventoryManager.Instance.GetActiveContainers();
            
            if (_selectedContainer != _constructionContainer && (_selectedContainer == null || !activeContainers.Contains(_selectedContainer)))
            {
                Logger.LogInfo($"[BuildUI] Selected container '{_selectedContainer?.Name}' no es válido o desapareció. Revirtiendo a ConstructionSite.");
                _selectedContainer = _constructionContainer;
            }

            UpdateBottomBar();
            _slotGrid?.UpdateGrid(_selectedContainer, this);

            // Cancel button only operable if construction site is EMPTY
            if (_btnCancel != null && _constructionContainer != null)
            {
                _btnCancel.Disabled = !_constructionContainer.IsEmpty();
                _btnCancel.TooltipText = _btnCancel.Disabled ? "Solo puedes cancelar una obra vacía." : "Cancelar construcción y recuperar sitio.";
            }
        }

        private void UpdateContentArea(InventoryContainer container)
        {
            _slotGrid?.UpdateGrid(container, this);
        }

        public bool IsOpen() => Visible;

        private void OnConfirmPressed()
        {
            bool allMaterialsMet = true;
            if (_recipe != null && _constructionContainer != null)
            {
                foreach (var req in _recipe.Requirements)
                {
                    if (_constructionContainer.GetTotalQuantity(req.Key) < req.Value)
                    {
                        allMaterialsMet = false;
                        break;
                    }
                }
            }

            if (!allMaterialsMet)
            {
                Logger.LogInfo("[BuildUI] Materiales insuficientes para confirmar construcción.");
                return;
            }

            Logger.LogInfo("[BuildUI] Construcción CONFIRMADA. Se han depositado todos los materiales.");
            Close();
            EmitSignal(SignalName.Confirmed);
        }

        private void OnCancelPressed()
        {
            if (_constructionContainer == null || !_constructionContainer.IsEmpty()) return;

            var dialog = new ConfirmationDialog();
            dialog.Title = "Cancelar Construcción";
            dialog.DialogText = "¿Estás seguro de que deseas cancelar esta obra? El sitio de construcción desaparecerá.";
            dialog.OkButtonText = "Sí, cancelar";
            dialog.CancelButtonText = "No, continuar";
            
            AddChild(dialog);
            dialog.PopupCentered();
            
            dialog.Confirmed += () => {
                Logger.LogInfo("[BuildUI] Construcción CANCELADA por el jugador.");
                EmitSignal(SignalName.Cancelled);
                dialog.QueueFree();
            };
            
            dialog.Canceled += () => {
                dialog.QueueFree();
            };
        }

        public override void _ExitTree()
        {
            Wild.Core.Quality.QualityManager.OnIconQualityChanged -= RefreshAll;
            if (_constructionContainer != null)
                _constructionContainer.OnChanged -= RefreshAll;
            if (Instance == this) Instance = null;
        }
    }
}
