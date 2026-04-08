using Godot;
using Wild.Core.Player;

namespace Wild.UI.Commands
{
    public class FeedCommand : IConsoleCommand
    {
        private DebugConsole _console;

        public FeedCommand(DebugConsole console)
        {
            _console = console;
        }

        public string Name => "feed";
        public string Description => "Restaura el hambre al 100%.";

        public void Execute(string[] args)
        {
            if (PlayerManager.Instance?.JugadorActual?.Stats != null)
            {
                PlayerManager.Instance.JugadorActual.Stats.Hambre = 100f;
                _console.AddLog("Hambre restaurada al 100% (Saciado).", Colors.Green);
            }
            else
            {
                _console.AddLog("Error: Jugador o Stats no encontrados.", Colors.Red);
            }
        }
    }
}
