using Godot;
using System;
using Wild.Utils;

namespace Wild.Core.Player
{
    /// <summary>
    /// Maneja la interacción del jugador con el entorno mediante Raycasts.
    /// </summary>
    public partial class InteraccionJugador : Node3D
    {
        [Export] public float DistanciaInteraccion = 3.0f;
        [Export] public RayCast3D RayoInteraccion;

        public override void _Ready()
        {
            if (RayoInteraccion == null)
            {
                RayoInteraccion = GetNodeOrNull<RayCast3D>("RayCast3D");
            }
            
            if (RayoInteraccion != null)
            {
                RayoInteraccion.TargetPosition = new Vector3(0, 0, -DistanciaInteraccion);
                RayoInteraccion.Enabled = true;
            }
        }

        public override void _Process(double delta)
        {
            if (RayoInteraccion == null || !RayoInteraccion.IsColliding()) return;

            if (Input.IsActionJustPressed("ui_interact")) // Asumiendo que existe ui_interact
            {
                ProcesarInteraccion();
            }
        }

        private void ProcesarInteraccion()
        {
            GodotObject collider = RayoInteraccion.GetCollider();
            if (collider == null) return;

            Vector3 point = RayoInteraccion.GetCollisionPoint();
            Logger.LogDebug($"PLAYER: Interactuando con {collider} en {point}");

            // Aquí se integrará con el sistema de objetos e inventario más adelante
        }
    }
}
