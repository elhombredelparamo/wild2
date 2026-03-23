using Godot;
using Wild.Core.Terrain;
using Wild.Utils;

namespace Wild.UI.Commands
{
    public class SpawnDeployableCommand : IConsoleCommand
    {
        public string Name => "spawndeployable";
        public string Description => "Spawnea un deployable persistente donde apunte la cámara. Uso: spawndeployable {id}";

        private readonly DebugConsole _console;

        public SpawnDeployableCommand(DebugConsole console)
        {
            _console = console;
        }

        public void Execute(string[] args)
        {
            if (args.Length < 1)
            {
                _console.AddLog("Uso incorrecto. Ejemplo: spawndeployable cofre", Colors.Red);
                return;
            }

            string typeId = args[0];

            // 1. Raycast desde la cámara
            var viewport = _console.GetViewport();
            var camera = viewport?.GetCamera3D();
            if (camera == null)
            {
                _console.AddLog("Error: No se encontró cámara activa para el raycast.", Colors.Red);
                return;
            }

            var spaceState = camera.GetWorld3D().DirectSpaceState;
            var from = camera.GlobalPosition;
            var to = from + (-camera.GlobalTransform.Basis.Z.Normalized() * 20.0f);

            var query = PhysicsRayQueryParameters3D.Create(from, to);
            query.CollisionMask = 1; // Solo terreno (Capa 1)
            
            var result = spaceState.IntersectRay(query);
            if (result.Count > 0)
            {
                Vector3 position = (Vector3)result["position"];
                
                // Rotar para que mire al jugador (opcional, por ahora solo rotación cero o basada en la cámara Y)
                Vector3 rotation = new Vector3(0, camera.GlobalRotation.Y, 0);

                if (TerrainManager.Instance != null)
                {
                    TerrainManager.Instance.AddDeployable(typeId, position, rotation);
                    _console.AddLog($"Éxito: Deployable '{typeId}' spawneado en {position}", Colors.Green);
                }
                else
                {
                    _console.AddLog("Error: TerrainManager no disponible.", Colors.Red);
                }
            }
            else
            {
                _console.AddLog("Error: No se detectó suelo al alcance (máx 20m).", Colors.Orange);
            }
        }
    }
}
