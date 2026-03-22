// -----------------------------------------------------------------------------
// Wild v2.0 - Controlador de Cámara Libre (Debug / Preview)
// -----------------------------------------------------------------------------
// Uso: adjuntar a Camera3D en la escena de juego para explorar el terreno
// Controles:
//   - Botón derecho del ratón + mover: rotar cámara
//   - W/A/S/D: mover adelante/izquierda/atrás/derecha
//   - Q/E: bajar/subir
//   - Rueda del ratón: zoom (acercar/alejar)
// -----------------------------------------------------------------------------
using Godot;
using Wild.Utils;

namespace Wild.UI
{
    public partial class FreeCameraController : Camera3D
    {
        [Export] public float MoveSpeed = 30.0f;
        [Export] public float SprintMultiplier = 3.0f;
        [Export] public float MouseSensitivity = 0.003f;
        [Export] public float ScrollSpeed = 10.0f;

        private bool _mouseCapturado = false;
        private float _yaw = 0f;    // Rotación horizontal
        private float _pitch = 0f;  // Rotación vertical

        public override void _Ready()
        {
            // Inicializar ángulos desde la rotación actual de la cámara en la escena
            _yaw = Rotation.Y;
            _pitch = Rotation.X;
            Logger.LogInfo("FreeCameraController: Cámara libre inicializada. Click derecho para rotar.");
        }

        public override void _Input(InputEvent @event)
        {
            // Activar/desactivar captura con click derecho
            if (@event is InputEventMouseButton mouseBtn)
            {
                /* DESACTIVADO: El click derecho rompía la captura del ratón en el juego
                if (mouseBtn.ButtonIndex == MouseButton.Right)
                {
                    _mouseCapturado = mouseBtn.Pressed;
                    Input.MouseMode = _mouseCapturado
                        ? Input.MouseModeEnum.Captured
                        : Input.MouseModeEnum.Visible;
                }
                */

                // Zoom con rueda del ratón
                if (mouseBtn.ButtonIndex == MouseButton.WheelUp)
                    Position += -Transform.Basis.Z * ScrollSpeed;
                if (mouseBtn.ButtonIndex == MouseButton.WheelDown)
                    Position += Transform.Basis.Z * ScrollSpeed;
            }

            // Rotar con movimiento del ratón (solo cuando está capturado)
            if (_mouseCapturado && @event is InputEventMouseMotion mouseMotion)
            {
                _yaw -= mouseMotion.Relative.X * MouseSensitivity;
                _pitch -= mouseMotion.Relative.Y * MouseSensitivity;
                _pitch = Mathf.Clamp(_pitch, -1.5f, 1.5f); // Limitar pitch ±85°

                Rotation = new Vector3(_pitch, _yaw, 0f);
            }
        }

        public override void _Process(double delta)
        {
            if (!_mouseCapturado) return;

            float speed = MoveSpeed * (float)delta;
            if (Input.IsKeyPressed(Key.Shift))
                speed *= SprintMultiplier;

            var dir = Vector3.Zero;
            if (Input.IsKeyPressed(Key.W)) dir -= Transform.Basis.Z;
            if (Input.IsKeyPressed(Key.S)) dir += Transform.Basis.Z;
            if (Input.IsKeyPressed(Key.A)) dir -= Transform.Basis.X;
            if (Input.IsKeyPressed(Key.D)) dir += Transform.Basis.X;
            if (Input.IsKeyPressed(Key.Q)) dir -= Vector3.Up;
            if (Input.IsKeyPressed(Key.E)) dir += Vector3.Up;

            if (dir != Vector3.Zero)
                Position += dir.Normalized() * speed;
        }
    }
}
