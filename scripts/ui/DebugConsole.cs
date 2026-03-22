using Godot;
using System;

using Wild.UI.Commands;
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
                
                _commandInput.TextSubmitted += OnCommandSubmitted;
                
                RegisterCommands();
                
                Visible = false;
                
                GD.Print("[UI][DebugConsole] Consola de Debug inicializada.");
            }
            catch (Exception e)
            {
                GD.PrintErr($"[ERROR][DebugConsole] Error al inicializar: {e.Message}");
            }
        }

        private void RegisterCommands()
        {
            AddCommand(new HelpCommand(this, _commands));
            AddCommand(new ClearCommand(this));
            AddCommand(new GiveItemCommand(this));
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
            GD.Print("[UI][DebugConsole] Consola abierta.");
        }

        public void Close()
        {
            Visible = false;
            _commandInput.ReleaseFocus();
            GD.Print("[UI][DebugConsole] Consola cerrada.");
        }

        private void OnCommandSubmitted(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            AddLog($"> {text}", Colors.Gray);
            _commandInput.Clear();

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
