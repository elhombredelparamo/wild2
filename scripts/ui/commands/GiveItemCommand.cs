using Godot;
using Wild.Data.Inventory;

namespace Wild.UI.Commands
{
    public class GiveItemCommand : IConsoleCommand
    {
        public string Name => "giveitem";
        public string Description => "Otorga un ítem al jugador. Uso: giveitem {id} {cantidad}";

        private readonly DebugConsole _console;

        public GiveItemCommand(DebugConsole console)
        {
            _console = console;
        }

        public void Execute(string[] args)
        {
            if (args.Length < 1)
            {
                _console.AddLog("Uso incorrecto. Ejemplo: giveitem seta1 5", Colors.Red);
                return;
            }

            string id = args[0];
            int quantity = 1;

            if (args.Length >= 2)
            {
                if (!int.TryParse(args[1], out quantity))
                {
                    _console.AddLog($"Cantidad inválida: {args[1]}", Colors.Red);
                    return;
                }
            }

            if (InventoryManager.Instance == null)
            {
                _console.AddLog("Error: InventoryManager no disponible.", Colors.Red);
                return;
            }

            bool success = InventoryManager.Instance.GiveItem(id, quantity);
            
            if (success)
            {
                _console.AddLog($"Éxito: Se ha otorgado {quantity}x '{id}'.", Colors.Green);
            }
            else
            {
                _console.AddLog($"Error: No se pudo otorgar el ítem '{id}'. Verifica el ID y el espacio en inventario.", Colors.Red);
            }
        }
    }
}
