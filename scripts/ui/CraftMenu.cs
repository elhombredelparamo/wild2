using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Wild.Core.Crafting;
using Wild.Utils;

namespace Wild.UI
{
    /// <summary>
    /// Menú de selección de recetas de crafteo (tecla C).
    /// Clon independiente de DeployMenu, trabaja con CraftableResource.
    /// </summary>
    public partial class CraftMenu : CanvasLayer
    {
        [Signal] public delegate void OpenedEventHandler();
        [Signal] public delegate void ClosedEventHandler();
        [Signal] public delegate void ItemSelectedEventHandler(CraftableResource recipe);

        private List<CraftableResource> _recipes = new();

        private Control _mainContainer;
        private Container _categoryList;
        private Container _itemsGrid;
        private bool _isOpen = false;
        private string _currentCategory = "";

        public override void _Ready()
        {
            try
            {
                _mainContainer = GetNode<Control>("MainContainer");
                _categoryList = GetNode<Container>("MainContainer/Panel/Layout/CategoryPanel/VBoxContainer/CategoryList");
                _itemsGrid = GetNode<Container>("MainContainer/Panel/Layout/ItemsPanel/VBoxContainer/ScrollContainer/ItemsGrid");

                Hide();
                _isOpen = false;

                LoadRecipesFromDisk();
                PopulateCategories();
            }
            catch (Exception e)
            {
                Logger.LogError($"[CraftMenu] Error en _Ready: {e.Message}");
            }
        }

        private void LoadRecipesFromDisk()
        {
            string path = "res://assets/data/craftables/";

            using var dir = DirAccess.Open(path);
            if (dir != null)
            {
                dir.ListDirBegin();
                string fileName = dir.GetNext();
                while (fileName != "")
                {
                    if (!dir.CurrentIsDir() && fileName.EndsWith(".tres"))
                    {
                        var res = ResourceLoader.Load<CraftableResource>(path + fileName);
                        if (res != null && !_recipes.Contains(res))
                        {
                            _recipes.Add(res);
                        }
                    }
                    fileName = dir.GetNext();
                }
            }
            else
            {
                Logger.LogInfo("CraftMenu: Carpeta 'assets/data/craftables/' aún vacía o no existe.");
            }

            Logger.LogInfo($"CraftMenu: {_recipes.Count} recetas de crafteo cargadas.");
        }

        public void Open()
        {
            Show();
            _isOpen = true;

            if (string.IsNullOrEmpty(_currentCategory) && _recipes.Count > 0)
            {
                DisplayCategory(_recipes[0].Category);
            }

            EmitSignal(SignalName.Opened);
            Logger.LogInfo("CraftMenu: Abierto.");
        }

        public void Close()
        {
            Hide();
            _isOpen = false;
            EmitSignal(SignalName.Closed);
            Logger.LogInfo("CraftMenu: Cerrado.");
        }

        public bool IsOpen() => _isOpen;

        private void PopulateCategories()
        {
            foreach (Node child in _categoryList.GetChildren()) child.QueueFree();

            var categories = _recipes.Select(r => r.Category).Distinct().OrderBy(c => c).ToList();

            foreach (var category in categories)
            {
                var btn = new Button();
                btn.Text = category;
                btn.Alignment = HorizontalAlignment.Left;
                btn.Pressed += () => DisplayCategory(category);
                _categoryList.AddChild(btn);
            }
        }

        private void DisplayCategory(string category)
        {
            _currentCategory = category;

            foreach (Node child in _itemsGrid.GetChildren()) child.QueueFree();

            var items = _recipes.Where(r => r.Category == category).OrderBy(r => r.Name).ToList();

            foreach (var recipe in items)
            {
                var btn = new Button();
                btn.Text = recipe.Name;
                btn.CustomMinimumSize = new Vector2(120, 150);
                btn.VerticalIconAlignment = VerticalAlignment.Top;
                btn.TooltipText = recipe.Description;

                if (!string.IsNullOrEmpty(recipe.IconPath) && ResourceLoader.Exists(recipe.IconPath))
                {
                    btn.Icon = ResourceLoader.Load<Texture2D>(recipe.IconPath);
                    btn.ExpandIcon = true;
                }

                var capturedRecipe = recipe; // Captura local para el closure
                btn.Pressed += () => OnItemSelected(capturedRecipe);
                _itemsGrid.AddChild(btn);
            }
        }

        private void OnItemSelected(CraftableResource recipe)
        {
            Logger.LogInfo($"CraftMenu: Seleccionada receta '{recipe.Name}' (ID: {recipe.Id})");
            EmitSignal(SignalName.ItemSelected, recipe);
            Close();
        }
    }
}
