using Godot;
using System;
using Wild.Core.Deployables.Base;
using Wild.Core.Quality;
using Wild.Utils;

namespace Wild.Core.Deployables.Base
{
    public partial class PlacementManager : Node3D
    {
        private DeployableResource _activeRecipe;
        private Node3D _ghost;
        private Area3D _ghostArea;
        private bool _isPlacing = false;
        private StandardMaterial3D _ghostMaterial;
        private float _currentRotationY = 0f;
        private const float RotationSpeed = 3.0f;
        
        private readonly Color _validColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        private readonly Color _invalidColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);

        public bool IsPlacing => _isPlacing;

        public override void _Ready()
        {
            _ghostMaterial = new StandardMaterial3D();
            _ghostMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            _ghostMaterial.AlbedoColor = _validColor;
            _ghostMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        }

        public void StartPlacement(DeployableResource recipe)
        {
            CancelPlacement(); // Limpiar previo si existe

            _activeRecipe = recipe;
            _isPlacing = true;
            _currentRotationY = 0f;
            
            CreateGhost();
            Logger.LogInfo($"PLACEMENT: Iniciando colocación de '{recipe.Name}'");
        }

        public void CancelPlacement()
        {
            if (_ghost != null)
            {
                _ghost.QueueFree();
                _ghost = null;
                _ghostArea = null;
            }
            _activeRecipe = null;
            _isPlacing = false;
        }

        private void CreateGhost()
        {
            try
            {
                var quality = QualityManager.Instance.Settings.DeployableQuality.ToString().ToLower();
                string modelPath = "";
                if (_activeRecipe.TechnicalId == "cofre1")
                {
                    modelPath = $"res://assets/models/deploy/cofre/1/{quality}/cofreCesta1.glb";
                }

                if (string.IsNullOrEmpty(modelPath) || !ResourceLoader.Exists(modelPath))
                {
                    Logger.LogWarning($"PLACEMENT: No se encontró modelo para el ghost: {modelPath}");
                    return;
                }

                var scene = ResourceLoader.Load<PackedScene>(modelPath);
                _ghost = (Node3D)scene.Instantiate();
                AddChild(_ghost);

                ApplyGhostMaterial(_ghost);
                
                // Desactivar colisiones originales del modelo para que no interfieran
                DisableCollisionRecursive(_ghost);
                
                // Crear Area3D para validación de colisiones propias
                CreateGhostArea();
            }
            catch (Exception ex)
            {
                Logger.LogError($"PLACEMENT: Error creando ghost: {ex.Message}");
            }
        }

        private void CreateGhostArea()
        {
            _ghostArea = new Area3D();
            _ghostArea.CollisionLayer = 0;
            _ghostArea.CollisionMask = (1 << 3); // Solo otros deployables

            Aabb aabb = CalculateAABBRecursive(_ghost);
            
            var colShape = new CollisionShape3D();
            var box = new BoxShape3D();
            // Usamos un 90% del tamaño para mantener la flexibilidad que pidió el usuario
            box.Size = aabb.Size * 0.9f; 
            colShape.Shape = box;
            colShape.Position = aabb.Position + (aabb.Size * 0.5f);
            
            _ghostArea.AddChild(colShape);
            _ghost.AddChild(_ghostArea);
            
            Logger.LogDebug($"PLACEMENT: Ghost Area creada con tamaño dinámico: {box.Size}");
        }

        private Aabb CalculateAABBRecursive(Node node)
        {
            Aabb totalAABB = new Aabb();
            bool first = true;

            void Walk(Node n, Transform3D accumulatedTransform)
            {
                Transform3D currentTransform = accumulatedTransform;
                if (n is Node3D n3d)
                {
                    currentTransform = accumulatedTransform * n3d.Transform;
                }

                if (n is MeshInstance3D meshInstance)
                {
                    Aabb localAABB = meshInstance.GetAabb();
                    Aabb rootSpaceAABB = currentTransform * localAABB;

                    if (first) { totalAABB = rootSpaceAABB; first = false; }
                    else { totalAABB = totalAABB.Merge(rootSpaceAABB); }
                }

                foreach (Node child in n.GetChildren()) Walk(child, currentTransform);
            }

            Walk(node, Transform3D.Identity);
            
            if (first) return new Aabb(new Vector3(-0.4f, 0, -0.4f), new Vector3(0.8f, 0.8f, 0.8f));
            return totalAABB;
        }

        private void ApplyGhostMaterial(Node node)
        {
            if (node is MeshInstance3D mesh)
            {
                int surfaceCount = mesh.GetSurfaceOverrideMaterialCount();
                if (surfaceCount == 0)
                {
                     mesh.MaterialOverride = _ghostMaterial;
                }
                else
                {
                    for (int i = 0; i < surfaceCount; i++)
                    {
                        mesh.SetSurfaceOverrideMaterial(i, _ghostMaterial);
                    }
                }
            }

            foreach (Node child in node.GetChildren())
            {
                ApplyGhostMaterial(child);
            }
        }

        private void DisableCollisionRecursive(Node node)
        {
            if (node is CollisionObject3D col)
            {
                col.InputRayPickable = false;
                col.CollisionLayer = 0;
                col.CollisionMask = 0;
            }
            foreach (Node child in node.GetChildren())
            {
                DisableCollisionRecursive(child);
            }
        }

        public override void _Process(double delta)
        {
            if (!_isPlacing || _ghost == null) return;

            HandleRotationInput((float)delta);
            UpdateGhostPosition();
            UpdateGhostValidity();
        }

        private void HandleRotationInput(float delta)
        {
            if (Input.IsActionPressed("deploy_rotate_left"))
            {
                _currentRotationY += RotationSpeed * delta;
            }
            if (Input.IsActionPressed("deploy_rotate_right"))
            {
                _currentRotationY -= RotationSpeed * delta;
            }
        }

        private void UpdateGhostPosition()
        {
            var camera = GetViewport().GetCamera3D();
            if (camera == null) return;

            var origin = camera.GlobalPosition;
            var direction = -camera.GlobalTransform.Basis.Z;
            var reach = 2.0f;
            var end = origin + direction * reach;

            var spaceState = GetWorld3D().DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(origin, end);
            query.CollisionMask = 1; // Capa 1: Terreno
            
            var result = spaceState.IntersectRay(query);

            Vector3 normal = Vector3.Up;

            if (result.Count > 0)
            {
                _ghost.GlobalPosition = (Vector3)result["position"];
                normal = (Vector3)result["normal"];
            }
            else
            {
                // Si no golpea terreno, colocarlo a la distancia de alcance frente a la cámara
                _ghost.GlobalPosition = end;
            }
            
            // Alinear con la normal del suelo y mirar al jugador, aplicando rotación extra
            var forward = (origin - _ghost.GlobalPosition);
            forward.Y = 0; // Mantener orientación horizontal base
            
            if (forward.Length() > 0.1f)
            {
                // Crear una base que mire al jugador pero cuyo eje Y sea la normal del suelo
                var basis = Basis.LookingAt(-forward.Normalized(), normal);
                // Aplicar rotación manual alrededor de la normal
                basis = basis.Rotated(normal, _currentRotationY);
                _ghost.GlobalBasis = basis;
            }
            else
            {
                // Fallback si estamos muy cerca
                var basis = Basis.LookingAt(Vector3.Forward, normal);
                basis = basis.Rotated(normal, _currentRotationY);
                _ghost.GlobalBasis = basis;
            }
        }

        private void UpdateGhostValidity()
        {
            bool hasGround = false;
            
            // 1. Verificar si hay suelo directamente debajo del ghost
            var spaceState = GetWorld3D().DirectSpaceState;
            var start = _ghost.GlobalPosition + Vector3.Up * 0.1f;
            var end = _ghost.GlobalPosition + Vector3.Down * 0.2f;
            var query = PhysicsRayQueryParameters3D.Create(start, end);
            query.CollisionMask = 1; // Solo terreno
            
            var result = spaceState.IntersectRay(query);
            if (result.Count > 0) hasGround = true;

            // 2. Verificar colisiones con el Area3D (Obstrucciones)
            bool isObstructed = _ghostArea.GetOverlappingBodies().Count > 0;

            // 3. Resultado final
            bool isValid = hasGround && !isObstructed;
            
            _ghostMaterial.AlbedoColor = isValid ? _validColor : _invalidColor;
        }
    }
}
