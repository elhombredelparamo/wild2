using Godot;
using System;
using Wild.Utils;

namespace Wild.UI
{
    public partial class DeployMenu : CanvasLayer
    {
        [Signal] public delegate void OpenedEventHandler();
        [Signal] public delegate void ClosedEventHandler();

        private Control _mainContainer;
        private bool _isOpen = false;

        public override void _Ready()
        {
            _mainContainer = GetNode<Control>("MainContainer");
            Hide();
            _isOpen = false;
        }

        public void Open()
        {
            Show();
            _isOpen = true;
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

        // Implementación básica para cerrar con ESC o la misma tecla si no se maneja en GameWorld
        // Pero el plan dice que se maneja en GameWorld.cs centralizado.
    }
}
