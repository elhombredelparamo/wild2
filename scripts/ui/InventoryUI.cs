using Godot;
using System;

namespace Wild.UI
{
    public partial class InventoryUI : CanvasLayer
    {
        private Control _contentPanel;
        private Control _bottomBar;

        public override void _Ready()
        {
            try
            {
                _contentPanel = GetNode<Control>("ContentPanel");
                _bottomBar = GetNode<Control>("BottomBar");
                
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
            GD.Print("[UI][InventoryUI] Inventario abierto.");
        }

        public void Close()
        {
            Visible = false;
            GD.Print("[UI][InventoryUI] Inventario cerrado.");
        }

        public bool IsOpen()
        {
            return Visible;
        }
    }
}
