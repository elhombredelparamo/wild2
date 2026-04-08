using Godot;
using Wild.Core.Player;
using Wild.Data;
using Wild.Utils;
using System;

namespace Wild.UI.Commands
{
    public class WoundCommand : IConsoleCommand
    {
        public string Name => "wound";
        public string Description => "Provoca una herida. Uso: /wound <parte> <tipo> <gravedad>";
        private DebugConsole _console;

        public WoundCommand(DebugConsole console)
        {
            _console = console;
        }

        public void Execute(string[] args)
        {
            if (args.Length < 3)
            {
                _console.AddLog("Uso: /wound <parte> <tipo_id> <gravedad>", Colors.Orange);
                _console.AddLog("Tipos: 0:Rasguño, 1:Laceracion, 2:HeridaProfunda, 3:Fractura, 4:Infeccion", Colors.Gray);
                return;
            }

            string part = args[0];
            
            if (!int.TryParse(args[1], out int typeIdx) || !float.TryParse(args[2], out float severity))
            {
                _console.AddLog("Error: Tipo o gravedad inválidos. Asegura que sean números.", Colors.Red);
                return;
            }

            if (PlayerManager.Instance == null)
            {
                _console.AddLog("Error: PlayerManager.Instance es NULL.", Colors.Red);
                return;
            }

            var jugador = PlayerManager.Instance.JugadorActual;
            if (jugador == null)
            {
                _console.AddLog("Error: JugadorActual es NULL.", Colors.Red);
                return;
            }

            if (jugador.Stats != null)
            {
                if (!jugador.Stats.SaludData.BodyParts.ContainsKey(part))
                {
                    _console.AddLog($"Error: No existe la parte '{part}'.", Colors.Orange);
                    _console.AddLog("Válidas: Torso, Cabeza, BrazoIzquierdo, BrazoDerecho, ManoIzquierda, etc.", Colors.Gray);
                    return;
                }

                WoundType type = (WoundType)Math.Clamp(typeIdx, 0, 4);
                jugador.Stats.RecibirDaño(severity, part, type);
                _console.AddLog($"Herida '{type}' aplicada en {part} ({severity} HP).", Colors.Yellow);
            }
            else
            {
                _console.AddLog("Error: JugadorActual es NULL o no tiene estadísticas.", Colors.Red);
            }
        }
    }
}
