using Godot;
using System;

using Wild.UI.Commands;
using Wild.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Wild.UI
{
    public partial class DebugConsole : CanvasLayer
    {
        private LineEdit _commandInput;
        private RichTextLabel _outputLog;
        
        private Dictionary<string, IConsoleCommand> _commands = new Dictionary<string, IConsoleCommand>();

        public override void _Ready()
        {
            try
            {
                _commandInput = GetNode<LineEdit>("Panel/VBoxContainer/CommandInput");
                _outputLog = GetNode<RichTextLabel>("Panel/VBoxContainer/OutputLog");
                
                // Usamos GuiInput para tener control total sobre el Enter y evitar la pérdida de foco
                _commandInput.GuiInput += OnInputReceived;
                
                // BLOQUEO DE FOCO: Asegurar que el Tabulador o Enter no saquen el foco del input
                _commandInput.FocusNext = _commandInput.GetPath();
                _commandInput.FocusPrevious = _commandInput.GetPath();

                RegisterCommands();
                
                Visible = false;
                
                Logger.LogInfo("UI: Consola de Debug inicializada.");
            }
            catch (Exception e)
            {
                Logger.LogError($"UI: Error al inicializar consola: {e.Message}");
            }
        }

        private void RegisterCommands()
        {
            AddCommand(new HelpCommand(this, _commands));
            AddCommand(new ClearCommand(this));
            AddCommand(new GiveItemCommand(this));
            AddCommand(new SpawnDeployableCommand(this));
            AddCommand(new KillCommand(this));
            AddCommand(new HealCommand(this));
            AddCommand(new WoundCommand(this));
            AddCommand(new SetSpawnCommand(this));
            AddCommand(new FeedCommand(this));
            AddCommand(new DrinkCommand(this));
        }

        public void AddCommand(IConsoleCommand command)
        {
            _commands[command.Name.ToLower()] = command;
        }

        public void Open()
        {
            Visible = true;
            _commandInput.GrabFocus();
            _commandInput.Clear();
            Logger.LogInfo("UI: Consola abierta.");
        }

        public void Close()
        {
            Visible = false;
            _commandInput.ReleaseFocus();
            Logger.LogInfo("UI: Consola cerrada.");
        }

        private void OnInputReceived(InputEvent @event)
        {
            // Solo enviar con Enter o Enter del teclado numérico.
            // No usamos "ui_accept" porque suele incluir la tecla Espacio, lo que impedía escribir parámetros.
            bool isEnter = @event is InputEventKey k && k.Pressed && (k.Keycode == Key.Enter || k.Keycode == Key.KpEnter);
            
            if (isEnter)
            {
                string text = _commandInput.Text;
                _commandInput.Text = ""; // Limpiar antes de procesar para evitar eco visual
                
                OnCommandSubmitted(text);
                
                // IMPORTANTE: Consumir el evento para que Godot no intente navegar focus
                GetViewport().SetInputAsHandled(); 
                _commandInput.GrabFocus();
                _commandInput.CallDeferred(Control.MethodName.GrabFocus);
            }
        }

        private void OnCommandSubmitted(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            AddLog($"> {text}", Colors.Gray);

            string[] parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string commandName = parts[0].ToLower();
            string[] args = parts.Skip(1).ToArray();

            if (_commands.ContainsKey(commandName))
            {
                try
                {
                    _commands[commandName].Execute(args);
                }
                catch (Exception e)
                {
                    AddLog($"Error al ejecutar '{commandName}': {e.Message}", Colors.Red);
                }
            }
            else
            {
                AddLog($"Comando desconocido: '{commandName}'. Escribe 'help' para ver la lista.", Colors.Orange);
            }
            
            // Re-grabamos foco por si acaso
            _commandInput.CallDeferred(Control.MethodName.GrabFocus);
        }

        public bool IsOpen()
        {
            return Visible;
        }

        public void AddLog(string text, Color? color = null)
        {
            if (_outputLog == null) return;
            
            string hex = color?.ToHtml() ?? "ffffff";
            _outputLog.AppendText($"\n[color=#{hex}]{text}[/color]");
        }

        public void ClearLog()
        {
            if (_outputLog != null)
                _outputLog.Clear();
        }
    }
}
