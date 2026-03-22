// -----------------------------------------------------------------------------
// Wild v2.0 - Sistema de Menú de Pausa
// -----------------------------------------------------------------------------
// 
// RELACIONADO CON:
// - contexto/menus-y-escenas.md - Sistema de menús y escenas
// - codigo/core/ui.pseudo - Diseño de interfaz de usuario
// - contexto/personajes.md - Sistema de gestión de personajes
// 
// DESCRIPCIÓN:
// Menú de pausa que nunca detiene el juego. Solo muestra UI local mientras
// el mundo continúa ejecutándose. Compatible con arquitectura servidor-cliente.
// 
// -----------------------------------------------------------------------------
using Godot;
using System;

namespace Wild.UI
{
    public partial class PauseMenu : Control
    {
        private Button _buttonContinue;
        private Button _buttonQuit;

        [Signal] public delegate void ContinuarPresionadoEventHandler();
        [Signal] public delegate void SalirPresionadoEventHandler();

        public bool IsPaused { get; private set; } = false;

        public override void _Ready()
        {
            LogUI("PauseMenu._Ready() - Inicializando menú de pausa");
            
            try
            {
                SetupButtons();
                ConnectEvents();
                Hide(); // Ocultar al inicio
                
                LogUI("PauseMenu._Ready() - Menú de pausa inicializado exitosamente");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("PauseMenu", $"Error en _Ready(): {e.Message}");
                throw;
            }
        }

        public override void _Input(InputEvent @event)
        {
            // El manejo de pausa ahora es centralizado en GameWorld.cs
        }

        public void TogglePause()
        {
            if (IsPaused)
            {
                ClosePauseMenu();
            }
            else
            {
                OpenPauseMenu();
            }
        }

        public void OpenPauseMenu()
        {
            if (IsPaused) return;

            LogUI("PauseMenu.OpenPauseMenu() - Abriendo menú de pausa local");
            
            IsPaused = true;
            Show();
            
            // Mostrar cursor
            Input.MouseMode = Input.MouseModeEnum.Visible;
            
            // NO pausar el juego - el mundo continúa para otros jugadores
            // GetTree().Paused = true; // ¡PROHIBIDO EN MULTIPLAYER!
            
            // Notificar al GameManager
            // GameManager.Instance.SetUIState(UIState.Paused);
            
            LogUI("PauseMenu.OpenPauseMenu() - Menú de pausa local abierto - mundo continúa");
        }

        public void ClosePauseMenu()
        {
            if (!IsPaused) return;

            LogUI("PauseMenu.ClosePauseMenu() - Cerrando menú de pausa local");
            
            IsPaused = false;
            Hide();
            
            // Ocultar cursor
            Input.MouseMode = Input.MouseModeEnum.Confined;
            
            // NO reanudar el juego - nunca fue pausado
            // GetTree().Paused = false; // ¡PROHIBIDO EN MULTIPLAYER!
            
            // Notificar al GameManager
            // GameManager.Instance.SetUIState(UIState.Playing);
            
            LogUI("PauseMenu.ClosePauseMenu() - Menú de pausa local cerrado - mundo continúa");
        }

        private void SetupButtons()
        {
            try
            {
                _buttonContinue = GetNode<Button>("CenterContainer/Panel/MarginContainer/VBoxContainer/ButtonContinue");
                _buttonQuit = GetNode<Button>("CenterContainer/Panel/MarginContainer/VBoxContainer/ButtonQuit");

                LogUI("PauseMenu.SetupButtons() - Todos los botones configurados");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("PauseMenu", $"Error en SetupButtons(): {e.Message}");
            }
        }

        private void ConnectEvents()
        {
            try
            {
                if (_buttonContinue != null)
                    _buttonContinue.Pressed += OnContinuePressed;
                
                
                if (_buttonQuit != null)
                    _buttonQuit.Pressed += OnQuitPressed;

                LogUI("PauseMenu.ConnectEvents() - Eventos de botones conectados");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("PauseMenu", $"Error en ConnectEvents(): {e.Message}");
            }
        }

        private void OnContinuePressed()
        {
            LogUI("PauseMenu.OnContinuePressed() - Botón Continuar presionado");
            EmitSignal(SignalName.ContinuarPresionado);
        }


        private void OnQuitPressed()
        {
            LogUI("PauseMenu.OnQuitPressed() - Botón Salir al Menú presionado");
            EmitSignal(SignalName.SalirPresionado);
        }

        // Funciones de logging
        private void LogUI(string mensaje)
        {
            // TODO: Implementar cuando tengamos el sistema de logging
            GD.Print($"[UI][PauseMenu] {mensaje}");
        }

        private void LogErrorSistema(string sistema, string mensaje)
        {
            // TODO: Implementar cuando tengamos el sistema de logging
            GD.PrintErr($"[ERROR][{sistema}] {mensaje}");
        }
    }
}
