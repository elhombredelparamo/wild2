using Godot;
using System;
using System.Collections.Generic;
using Wild.Data.Inventory;
using Wild.UI.Components;
using Wild.Core.Crafting;
using Wild.Utils;

namespace Wild.UI
{
    /// <summary>
    /// UI de aportación de materiales para el sistema de crafteos.
    /// Clon independiente de BuildUI, trabaja con CraftableResource y CraftingConstruction.
    /// </summary>
    public partial class CraftBuildUI : CanvasLayer, IInventoryView
    {
        [Signal] public delegate void ConfirmedEventHandler();
        [Signal] public delegate void CancelledEventHandler();

        public static CraftBuildUI Instance { get; private set; }

        private Control _contentPanel;
        private Control _bottomBar;
        private HBoxContainer _containerList;
        private InventorySlotGrid _slotGrid;
        private ScrollContainer _scrollContainer;
        private InventoryContainer _selectedContainer;
        private InventoryContainer _craftSiteContainer;
        private InventoryContextMenu _contextMenu;
        private CraftableResource _recipe;

        private VBoxContainer _requirementsList;

        private Button _btnConfirm;
        private Button _btnCancel;

        public override void _Ready()
        {
            Instance = this;
            try
            {
                _contentPanel = GetNode<Control>("ContentPanel");
                _bottomBar    = GetNode<Control>("BottomBar");
                _containerList = GetNode<HBoxContainer>("BottomBar/ContainerList");

                _btnConfirm = GetNode<Button>("ActionButtons/BtnConfirm");
                _btnCancel  = GetNode<Button>("ActionButtons/BtnCancel");

                _btnConfirm.Pressed += OnConfirmPressed;
                _btnCancel.Pressed  += OnCancelPressed;

                // Layout principal: slots (izquierda) + requisitos (derecha)
                var splitLayout = new HBoxContainer();
                splitLayout.Name = "SplitLayout";
                _contentPanel.AddChild(splitLayout);
                splitLayout.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.Minsize, 10);
                splitLayout.AddThemeConstantOverride("separation", 20);

                // Slot grid (izquierda)
                _scrollContainer = new ScrollContainer();
                _scrollContainer.Name = "ItemScroll";
                _scrollContainer.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
                _scrollContainer.VerticalScrollMode   = ScrollContainer.ScrollMode.Auto;
                _scrollContainer.SizeFlagsHorizontal  = Control.SizeFlags.ExpandFill;
                splitLayout.AddChild(_scrollContainer);

                _slotGrid = new InventorySlotGrid();
                _slotGrid.Name = "SlotGrid";
                _slotGrid.Initialize(6);
                _scrollContainer.AddChild(_slotGrid);

                // Lista de requisitos (derecha)
                _requirementsList = new VBoxContainer();
                _requirementsList.Name = "RequirementsList";
                _requirementsList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                splitLayout.AddChild(_requirementsList);

                var reqTitle = new Label();
                reqTitle.Text = "Materiales Necesarios";
                reqTitle.HorizontalAlignment = HorizontalAlignment.Center;
                reqTitle.AddThemeColorOverride("font_color", new Color(0.2f, 1f, 0.5f)); // Verde crafteo
                _requirementsList.AddChild(reqTitle);

                _requirementsList.AddChild(new HSeparator());

                Visible = false;
                InitializeContextMenu();

                Wild.Core.Quality.QualityManager.OnIconQualityChanged += RefreshAll;
            }
            catch (Exception e)
            {
                Logger.LogError($"[CraftBuildUI] Error inicializando: {e.Message}");
            }
        }

        private void InitializeContextMenu()
        {
            _contextMenu = new InventoryContextMenu();
            _contextMenu.Initialize(this);
            AddChild(_contextMenu);
        }

        // ── IInventoryView ────────────────────────────────────────────────────

        public void ShowContextMenu(Vector2 globalPos, InventoryContainer container, int slotIndex)
            => _contextMenu.ShowAt(globalPos, container, slotIndex);

        public void ShowContainerContextMenu(Vector2 globalPos, InventoryContainer container)
            => _contextMenu.ShowContainerOptionsAt(globalPos, container);

        // ── Setup ─────────────────────────────────────────────────────────────

        public void SetupForCrafting(CraftableResource recipe, InventoryContainer siteContainer)
        {
            _recipe = recipe;
            _craftSiteContainer = siteContainer;
            _craftSiteContainer.OnChanged += RefreshAll;

            PopulateRequirements(recipe);
        }

        private void PopulateRequirements(CraftableResource recipe)
        {
            if (_requirementsList == null) return;

            // Limpiar los hijos anteriores (mantener título y separador: primeros 2)
            for (int i = _requirementsList.GetChildCount() - 1; i >= 2; i--)
                _requirementsList.GetChild(i).QueueFree();

            foreach (var req in recipe.Requirements)
            {
                var itemObj = InventoryManager.Instance?.GetItemById(req.Key);
                string itemName = itemObj != null ? itemObj.Name : req.Key;

                var label = new Label();
                label.Text = $"{req.Value} x {itemName}";
                _requirementsList.AddChild(label);
            }
        }

        // ── Open / Close ──────────────────────────────────────────────────────

        public void Open()
        {
            Visible = true;
            UpdateBottomBar();

            if (_craftSiteContainer != null)
                OnContainerSelected(_craftSiteContainer);

            Logger.LogInfo($"[CraftBuildUI] Abierto. Receta: {_recipe?.Name}");
        }

        public void Close()
        {
            Visible = false;
            if (_craftSiteContainer != null)
            {
                _craftSiteContainer.OnChanged -= RefreshAll;
                _craftSiteContainer = null;
            }
            Logger.LogInfo("[CraftBuildUI] Cerrado.");
        }

        public bool IsOpen() => Visible;

        // ── Refresh ───────────────────────────────────────────────────────────

        public void UpdateBottomBar()
        {
            if (_containerList == null) return;

            foreach (Node child in _containerList.GetChildren()) child.QueueFree();

            if (InventoryManager.Instance == null) return;

            // Sitio de obra primero
            if (_craftSiteContainer != null)
            {
                var extBtn = new InventoryContainerButtonUI();
                _containerList.AddChild(extBtn);
                extBtn.Setup(_craftSiteContainer, this, _selectedContainer == _craftSiteContainer);
            }

            foreach (var container in InventoryManager.Instance.GetActiveContainers())
            {
                var btn = new InventoryContainerButtonUI();
                _containerList.AddChild(btn);
                btn.Setup(container, this, _selectedContainer == container);
            }
        }

        public void OnContainerSelected(InventoryContainer container)
        {
            _selectedContainer = container;
            UpdateBottomBar();
            _slotGrid?.UpdateGrid(container, this);
        }

        public void RefreshAll()
        {
            if (InventoryManager.Instance == null) return;

            var active = InventoryManager.Instance.GetActiveContainers();
            if (_selectedContainer != _craftSiteContainer && (_selectedContainer == null || !active.Contains(_selectedContainer)))
                _selectedContainer = _craftSiteContainer;

            UpdateBottomBar();
            _slotGrid?.UpdateGrid(_selectedContainer, this);

            if (_btnCancel != null && _craftSiteContainer != null)
            {
                _btnCancel.Disabled = !_craftSiteContainer.IsEmpty();
                _btnCancel.TooltipText = _btnCancel.Disabled
                    ? "Solo puedes cancelar una obra vacía."
                    : "Cancelar crafteo y recuperar el sitio.";
            }
        }

        // ── Acciones ──────────────────────────────────────────────────────────

        private void OnConfirmPressed()
        {
            if (_recipe == null || _craftSiteContainer == null) return;

            foreach (var req in _recipe.Requirements)
            {
                if (_craftSiteContainer.GetTotalQuantity(req.Key) < req.Value)
                {
                    Logger.LogInfo("[CraftBuildUI] Materiales insuficientes.");
                    return;
                }
            }

            Logger.LogInfo("[CraftBuildUI] Materiales CONFIRMADOS. Iniciando ensamblado.");
            Close();
            EmitSignal(SignalName.Confirmed);
        }

        private void OnCancelPressed()
        {
            if (_craftSiteContainer == null || !_craftSiteContainer.IsEmpty()) return;

            var dialog = new ConfirmationDialog();
            dialog.Title = "Cancelar Crafteo";
            dialog.DialogText = "¿Cancelar esta obra de crafteo? El sitio desaparecerá.";
            dialog.OkButtonText = "Sí, cancelar";
            dialog.CancelButtonText = "No, continuar";
            AddChild(dialog);
            dialog.PopupCentered();

            dialog.Confirmed += () => {
                Logger.LogInfo("[CraftBuildUI] Crafteo CANCELADO por el jugador.");
                EmitSignal(SignalName.Cancelled);
                dialog.QueueFree();
            };
            dialog.Canceled += () => dialog.QueueFree();
        }

        public override void _ExitTree()
        {
            Wild.Core.Quality.QualityManager.OnIconQualityChanged -= RefreshAll;
            if (_craftSiteContainer != null)
                _craftSiteContainer.OnChanged -= RefreshAll;
            if (Instance == this) Instance = null;
        }
    }
}
