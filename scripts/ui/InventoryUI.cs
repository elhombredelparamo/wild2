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

        public override void _Ready()
        {
            try
            {
                _contentPanel = GetNode<Control>("ContentPanel");
                _bottomBar = GetNode<Control>("BottomBar");
                _containerList = GetNode<HBoxContainer>("BottomBar/ContainerList");
                
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

            // Limpiar lista actual
            foreach (Node child in _containerList.GetChildren())
            {
                child.QueueFree();
            }

            // Obtener contenedores del manager asumiendo que ya está cargado como Autoload
            // Si InventoryManager.Instance es null, es que el Autoload no ha corrido o hay un error
            if (InventoryManager.Instance == null)
            {
                GD.PrintErr("[ERROR][InventoryUI] InventoryManager.Instance es null!");
                return;
            }

            var containers = InventoryManager.Instance.GetActiveContainers();
            
            foreach (var container in containers)
            {
                PanelContainer slot = CreateContainerSlotUI(container);
                _containerList.AddChild(slot);
            }
        }

        private PanelContainer CreateContainerSlotUI(InventoryContainer container)
        {
            PanelContainer panel = new PanelContainer();
            panel.CustomMinimumSize = new Vector2(80, 80); // Botones cuadrados
            
            MarginContainer margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 5);
            margin.AddThemeConstantOverride("margin_right", 5);
            margin.AddThemeConstantOverride("margin_top", 5);
            margin.AddThemeConstantOverride("margin_bottom", 5);
            panel.AddChild(margin);

            if (!string.IsNullOrEmpty(container.IconPath))
            {
                TextureRect icon = new TextureRect();
                try 
                {
                    icon.Texture = GD.Load<Texture2D>(container.IconPath);
                }
                catch (Exception e)
                {
                    GD.PrintErr($"[ERROR][InventoryUI] No se pudo cargar icono {container.IconPath}: {e.Message}");
                }
                icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                margin.AddChild(icon);
                
                panel.TooltipText = container.Name;
            }
            else
            {
                Label nameLabel = new Label();
                nameLabel.Text = container.Name;
                nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
                nameLabel.VerticalAlignment = VerticalAlignment.Center;
                nameLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                margin.AddChild(nameLabel);
            }

            return panel;
        }

        public bool IsOpen()
        {
            return Visible;
        }
    }
}
