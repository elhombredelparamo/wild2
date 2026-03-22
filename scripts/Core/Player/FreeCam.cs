using Godot;
using System;
using Wild.Utils;

namespace Wild.Core.Player
{
    /// <summary>
    /// Cámara libre con vuelo y colisiones (sin gravedad).
    /// </summary>
    public partial class FreeCam : CharacterBody3D
    {
        [Export] public float Speed = 10.0f;
        [Export] public float BoostMultiplier = 2.5f;
        [Export] public float Sensitivity = 0.002f;

        private Camera3D _camera;
        private float _rotationX = 0f;

        public override void _Ready()
        {
            // Crear la cámara si no existe
            _camera = new Camera3D();
            AddChild(_camera);
            _camera.Current = true;

            // Añadir una colisión básica para que no atraviese el suelo/paredes
            var collision = new CollisionShape3D();
            var sphere = new SphereShape3D();
            sphere.Radius = 0.5f;
            collision.Shape = sphere;
            AddChild(collision);

            Logger.LogInfo("FreeCam: Cámara libre inicializada con colisiones.");
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                // Rotación horizontal del cuerpo
                RotateY(-mouseMotion.Relative.X * Sensitivity);

                // Rotación vertical de la cámara
                _rotationX -= mouseMotion.Relative.Y * Sensitivity;
                _rotationX = Mathf.Clamp(_rotationX, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
                _camera.Rotation = new Vector3(_rotationX, 0, 0);
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            Vector3 direction = Vector3.Zero;
            Vector3 basisZ = _camera.GlobalTransform.Basis.Z;
            Vector3 basisX = _camera.GlobalTransform.Basis.X;
            Vector3 basisY = Vector3.Up;

            // WASD Movement
            if (Input.IsKeyPressed(Key.W)) direction -= basisZ;
            if (Input.IsKeyPressed(Key.S)) direction += basisZ;
            if (Input.IsKeyPressed(Key.A)) direction -= basisX;
            if (Input.IsKeyPressed(Key.D)) direction += basisX;

            // Vertical Movement (Q/E)
            if (Input.IsKeyPressed(Key.E)) direction += basisY;
            if (Input.IsKeyPressed(Key.Q)) direction -= basisY;

            if (direction != Vector3.Zero)
            {
                direction = direction.Normalized();
                float currentSpeed = Speed * (Input.IsKeyPressed(Key.Shift) ? BoostMultiplier : 1.0f);
                Velocity = direction * currentSpeed;
            }
            else
            {
                Velocity = Velocity.MoveToward(Vector3.Zero, Speed * 0.5f);
            }

            MoveAndSlide();
        }

        public void SetInitialTransform(Transform3D targetTransform)
        {
            GlobalTransform = targetTransform;
            // Sincronizar rotación interna para evitar saltos
            _rotationX = targetTransform.Basis.GetEuler().X;
            Rotation = new Vector3(0, targetTransform.Basis.GetEuler().Y, 0);
            _camera.Rotation = new Vector3(_rotationX, 0, 0);
        }
    }
}
