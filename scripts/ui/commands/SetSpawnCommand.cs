using Godot;
using Wild.Core.Player;
using Wild.Data;
using Wild.Utils;

namespace Wild.UI.Commands
{
    public class SetSpawnCommand : IConsoleCommand
    {
        public string Name => "setspawn";
        public string Description => "Establece la ubicación actual como punto de respawn.";
        private DebugConsole _console;

        public SetSpawnCommand(DebugConsole console)
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
            if (jugador != null)
            {
                var pos = jugador.GlobalPosition;
                PlayerManager.Instance.SpawnPoint = pos;
                
                // Forzar guardado inmediato para persistencia
                PlayerManager.Instance.GuardarEstadoJugador();
                
                _console.AddLog($"Punto de spawn establecido en: {pos.X:F1}, {pos.Y:F1}, {pos.Z:F1}", Colors.Aqua);
            }
            else
            {
                _console.AddLog("Error: JugadorActual es NULL.", Colors.Red);
            }
        }
    }
}
