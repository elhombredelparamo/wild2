using Godot;
using Wild.Core.Player;
using Wild.Utils;

namespace Wild.UI.Commands
{
    public class HealCommand : IConsoleCommand
    {
        public string Name => "heal";
        public string Description => "Sana al jugador y restaura hambre/sed.";
        private DebugConsole _console;

        public HealCommand(DebugConsole console)
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
                jugador.Stats.SaludData = new Wild.Data.HealthData();
                jugador.Stats.Hambre = 100f;
                jugador.Stats.Sed = 100f;
                jugador.Stats.Energia = 100f;
                _console.AddLog("Jugador sanado completamente.", Colors.Green);
            }
            else
            {
                _console.AddLog("Error: JugadorActual es NULL o no tiene estadísticas.", Colors.Orange);
            }
        }
    }
}
