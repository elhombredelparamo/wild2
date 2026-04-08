using Godot;
using Wild.Core.Player;
using Wild.Utils;

namespace Wild.UI.Commands
{
    public class KillCommand : IConsoleCommand
    {
        public string Name => "kill";
        public string Description => "Mata al jugador instantáneamente.";
        private DebugConsole _console;

        public KillCommand(DebugConsole console)
        {
            _console = console;
        }

        public void Execute(string[] args)
        {
            if (PlayerManager.Instance == null)
            {
                _console.AddLog("Error: PlayerManager.Instance es NULL.", Colors.Red);
                return;
            }

            var jugador = PlayerManager.Instance.JugadorActual;
            if (jugador != null && jugador.Stats != null)
            {
                _console.AddLog("Autoliquidación en curso...", Colors.Red);
                jugador.Stats.RecibirDaño(999, "Torso");
            }
            else
            {
                _console.AddLog("Error: JugadorActual es NULL o no tiene estadísticas.", Colors.Orange);
            }
        }
    }
}
