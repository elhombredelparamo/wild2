using Godot;

namespace Wild.UI.Commands
{
    public class ClearCommand : IConsoleCommand
    {
        public string Name => "clear";
        public string Description => "Limpia el log de la consola.";
        
        private readonly DebugConsole _console;

        public ClearCommand(DebugConsole console)
        {
            _console = console;
        }

        public void Execute(string[] args)
        {
            _console.ClearLog();
        }
    }
}
