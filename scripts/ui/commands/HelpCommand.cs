using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Wild.UI.Commands
{
    public class HelpCommand : IConsoleCommand
    {
        public string Name => "help";
        public string Description => "Muestra la lista de comandos disponibles.";
        
        private readonly DebugConsole _console;
        private readonly Dictionary<string, IConsoleCommand> _commands;

        public HelpCommand(DebugConsole console, Dictionary<string, IConsoleCommand> commands)
        {
            _console = console;
            _commands = commands;
        }

        public void Execute(string[] args)
        {
            _console.AddLog("--- Comandos Disponibles ---", Colors.Yellow);
            foreach (var cmd in _commands.Values.OrderBy(c => c.Name))
            {
                _console.AddLog($"{cmd.Name}: [color=gray]{cmd.Description}[/color]");
            }
        }
    }
}
