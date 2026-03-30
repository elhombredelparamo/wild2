using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Wild.Utils;
using Wild.Core.Deployables.Base;

namespace Wild.UI
{
    public partial class DeployMenu : CanvasLayer
    {
        [Signal] public delegate void OpenedEventHandler();
        [Signal] public delegate void ClosedEventHandler();
        [Signal] public delegate void ItemSelectedEventHandler(DeployableResource recipe);

        [Export] public Godot.Collections.Array<DeployableResource> Recipes { get; set; } = new();

        private Control _mainContainer;
        private Container _categoryList;
        private Container _itemsGrid;
        private bool _isOpen = false;
        private string _currentCategory = "";

        public override void _Ready()
        {
            _mainContainer = GetNode<Control>("MainContainer");
            _categoryList = GetNode<Container>("MainContainer/Panel/Layout/CategoryPanel/VBoxContainer/CategoryList");
            _itemsGrid = GetNode<Container>("MainContainer/Panel/Layout/ItemsPanel/VBoxContainer/ScrollContainer/ItemsGrid");
            
            Hide();
            _isOpen = false;
            
            LoadRecipesFromDisk();
            PopulateCategories();
        }

        private void LoadRecipesFromDisk()
        {
            string path = "res://assets/data/deployables/";
            
            // Usar DirAccess para Godot (funciona en exportado)
            using var dir = DirAccess.Open(path);
            if (dir != null)
            {
                dir.ListDirBegin();
                string fileName = dir.GetNext();
                while (fileName != "")
                {
                    if (!dir.CurrentIsDir() && fileName.EndsWith(".tres"))
                    {
                        var res = ResourceLoader.Load<DeployableResource>(path + fileName);
                        if (res != null && !Recipes.Contains(res))
                        {
                            Recipes.Add(res);
                        }
                    }
                    fileName = dir.GetNext();
                }
            }
            Logger.LogInfo($"DeployMenu: {Recipes.Count} recetas cargadas.");
        }

        public void Open()
        {
            Show();
            _isOpen = true;
            
            // Si no hay categoría seleccionada, seleccionar la primera
            if (string.IsNullOrEmpty(_currentCategory) && Recipes.Count > 0)
            {
                DisplayCategory(Recipes[0].Category);
            }
            
            EmitSignal("Opened");
            Logger.LogInfo("DeployMenu: Abierto.");
        }

        public void Close()
        {
            Hide();
            _isOpen = false;
            EmitSignal("Closed");
            Logger.LogInfo("DeployMenu: Cerrado.");
        }

        public bool IsOpen() => _isOpen;

        private void PopulateCategories()
        {
            // Limpiar lista
            foreach (Node child in _categoryList.GetChildren()) child.QueueFree();

            var categories = Recipes.Select(r => r.Category).Distinct().OrderBy(c => c).ToList();
            
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
            
            // Limpiar grid
            foreach (Node child in _itemsGrid.GetChildren()) child.QueueFree();

            var items = Recipes.Where(r => r.Category == category).OrderBy(r => r.Name).ToList();
            
            foreach (var recipe in items)
            {
                var btn = new Button();
                btn.Text = recipe.Name;
                btn.CustomMinimumSize = new Vector2(120, 150);
                btn.VerticalIconAlignment = VerticalAlignment.Top;
                
                if (!string.IsNullOrEmpty(recipe.IconPath) && ResourceLoader.Exists(recipe.IconPath))
                {
                    btn.Icon = ResourceLoader.Load<Texture2D>(recipe.IconPath);
                    btn.ExpandIcon = true;
                }
                
                btn.Pressed += () => OnItemSelected(recipe);
                _itemsGrid.AddChild(btn);
            }
        }

        private void OnItemSelected(DeployableResource recipe)
        {
            Logger.LogInfo($"DeployMenu: Seleccionado objeto '{recipe.Name}' (ID: {recipe.TechnicalId})");
            EmitSignal("ItemSelected", recipe); // Usar string para evitar error de compilación temporal
            Close();
        }
    }
}
