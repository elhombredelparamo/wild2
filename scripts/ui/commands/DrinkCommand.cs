using Godot;
using Wild.Core.Player;

namespace Wild.UI.Commands
{
    public class DrinkCommand : IConsoleCommand
    {
        private DebugConsole _console;

        public DrinkCommand(DebugConsole console)
        {
            _console = console;
        }

        public string Name => "drink";
        public string Description => "Restaura la sed al 100%.";

        public void Execute(string[] args)
        {
            if (PlayerManager.Instance?.JugadorActual?.Stats != null)
            {
                PlayerManager.Instance.JugadorActual.Stats.Sed = 100f;
                _console.AddLog("Sed restaurada al 100% (Hidratado).", Colors.SkyBlue);
            }
            else
            {
                _console.AddLog("Error: Jugador o Stats no encontrados.", Colors.Red);
            }
        }
    }
}
