using Godot;
using System;

namespace Wild.UI
{
    public partial class DebugConsole : CanvasLayer
    {
        private LineEdit _commandInput;
        private RichTextLabel _outputLog;

        public override void _Ready()
        {
            try
            {
                _commandInput = GetNode<LineEdit>("Panel/VBoxContainer/CommandInput");
                _outputLog = GetNode<RichTextLabel>("Panel/VBoxContainer/OutputLog");
                
                Visible = false;
                
                GD.Print("[UI][DebugConsole] Consola de Debug inicializada.");
            }
            catch (Exception e)
            {
                GD.PrintErr($"[ERROR][DebugConsole] Error al inicializar: {e.Message}");
            }
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
    }
}
