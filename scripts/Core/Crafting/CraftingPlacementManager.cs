using Godot;
using System;
using Wild.Core.Crafting;
using Wild.Utils;

namespace Wild.Core.Crafting
{
    /// <summary>
    /// Gestiona la colocación visual del ghost de crafteo en el mundo.
    /// Clon independiente de PlacementManager para el sistema de Crafteos.
    /// </summary>
    public partial class CraftingPlacementManager : Node3D
    {
        private CraftableResource _activeRecipe;
        private Node3D _ghost;
        private Area3D _ghostArea;
        private bool _isPlacing = false;
        private StandardMaterial3D _ghostMaterial;
        private float _currentRotationY = 0f;
        private const float RotationSpeed = 3.0f;

        private readonly Color _validColor   = new Color(0.2f, 0.8f, 0.4f, 0.5f); // Verde crafteo
        private readonly Color _invalidColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);

        public bool IsPlacing => _isPlacing;

        /// <summary>
        /// Se emite cuando el jugador confirma la posición del ghost.
        /// Parámetros: receta activa, transform en el mundo, nodo CraftingConstruction creado.
        /// </summary>
        public event Action<CraftableResource, Transform3D, CraftingConstruction> OnPlacementConfirmed;

        private bool _isValidPosition = false;

        public override void _Ready()
        {
            _ghostMaterial = new StandardMaterial3D();
            _ghostMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            _ghostMaterial.AlbedoColor = _validColor;
            _ghostMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        }

        // ── API pública ────────────────────────────────────────────────────────

        public void StartPlacement(CraftableResource recipe)
        {
            CancelPlacement();

            _activeRecipe = recipe;
            _isPlacing = true;
            _currentRotationY = 0f;

            CreateGhost();
            UpdateGhostPosition();
            Logger.LogInfo($"CRAFT_PLACEMENT: Iniciando colocación de '{recipe.Name}'");
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

        // ── Ghost ──────────────────────────────────────────────────────────────

        private void CreateGhost()
        {
            try
            {
                if (string.IsNullOrEmpty(_activeRecipe?.ModelPath) || !ResourceLoader.Exists(_activeRecipe.ModelPath))
                {
                    Logger.LogWarning($"CRAFT_PLACEMENT: No se encontró modelo para el ghost: '{_activeRecipe?.ModelPath}'. Usando cubo por defecto.");
                    CreateFallbackGhost();
                    return;
                }

                var scene = ResourceLoader.Load<PackedScene>(_activeRecipe.ModelPath);
                var mesh = (Node3D)scene.Instantiate();
                
                // Las transformaciones de ajuste se aplican solo a la malla hija
                if (mesh != null)
                {
                    mesh.Scale = _activeRecipe.ModelScale;
                    mesh.RotationDegrees = _activeRecipe.ModelRotation;
                }

                _ghost = new Node3D();
                _ghost.AddChild(mesh);
                AddChild(_ghost);

                ApplyGhostMaterial(_ghost);
                DisableCollisionRecursive(_ghost);
                CreateGhostArea();
            }
            catch (Exception ex)
            {
                Logger.LogError($"CRAFT_PLACEMENT: Error creando ghost: {ex.Message}");
                CreateFallbackGhost();
            }
        }

        private void CreateFallbackGhost()
        {
            // Cubo semitransparente de fallback cuando no hay modelo
            _ghost = new Node3D();
            AddChild(_ghost);

            var meshInst = new MeshInstance3D();
            meshInst.Mesh = new BoxMesh { Size = new Vector3(0.4f, 0.4f, 0.4f) };
            meshInst.MaterialOverride = _ghostMaterial;
            _ghost.AddChild(meshInst);
            CreateGhostArea();
        }

        private void CreateGhostArea()
        {
            _ghostArea = new Area3D();
            _ghostArea.CollisionLayer = 0;
            _ghostArea.CollisionMask = (1 << 3); // Solo otros deployables

            Aabb aabb = CalculateAABBRecursive(_ghost);
            var colShape = new CollisionShape3D();
            var box = new BoxShape3D { Size = aabb.Size * 0.9f };
            colShape.Shape = box;
            colShape.Position = aabb.Position + (aabb.Size * 0.5f);

            _ghostArea.AddChild(colShape);
            _ghost.AddChild(_ghostArea);
        }

        private void ApplyGhostMaterial(Node node)
        {
            if (node is MeshInstance3D mesh)
            {
                int surfaces = mesh.GetSurfaceOverrideMaterialCount();
                if (surfaces == 0)
                    mesh.MaterialOverride = _ghostMaterial;
                else
                    for (int i = 0; i < surfaces; i++)
                        mesh.SetSurfaceOverrideMaterial(i, _ghostMaterial);
            }
            foreach (Node child in node.GetChildren()) ApplyGhostMaterial(child);
        }

        private void DisableCollisionRecursive(Node node)
        {
            if (node is CollisionObject3D col)
            {
                col.InputRayPickable = false;
                col.CollisionLayer = 0;
                col.CollisionMask = 0;
            }
            foreach (Node child in node.GetChildren()) DisableCollisionRecursive(child);
        }

        private void RemoveGhostMaterialRecursive(Node node)
        {
            if (node is MeshInstance3D mesh)
            {
                mesh.MaterialOverride = null;
                for (int i = 0; i < mesh.GetSurfaceOverrideMaterialCount(); i++)
                    mesh.SetSurfaceOverrideMaterial(i, null);
            }
            foreach (Node child in node.GetChildren()) RemoveGhostMaterialRecursive(child);
        }

        private void ApplyConstructionTransparencyRecursive(Node node)
        {
            if (node is GeometryInstance3D geom) geom.Transparency = 0.5f;
            foreach (Node child in node.GetChildren()) ApplyConstructionTransparencyRecursive(child);
        }

        // ── Process ────────────────────────────────────────────────────────────

        public override void _Process(double delta)
        {
            if (!_isPlacing || _ghost == null) return;

            HandleRotationInput((float)delta);
            UpdateGhostPosition();
            UpdateGhostValidity();
            HandlePlacementConfirmation();
        }

        private void HandlePlacementConfirmation()
        {
            if (Input.IsMouseButtonPressed(MouseButton.Left) && _isValidPosition)
            {
                Logger.LogInfo("CRAFT_PLACEMENT: Posición válida confirmada. Cambiando a estado de construcción.");
                _isPlacing = false;

                RemoveGhostMaterialRecursive(_ghost);

                // Crear el nodo CraftingConstruction que reemplaza al ghost
                var constructionSite = new CraftingConstruction();
                constructionSite.GlobalTransform = _ghost.GlobalTransform;
                GetParent()?.AddChild(constructionSite);

                // Mover visuales del ghost al nodo de construcción
                foreach (Node child in _ghost.GetChildren())
                {
                    if (child == _ghostArea) continue;
                    child.GetParent()?.RemoveChild(child);
                    constructionSite.AddChild(child);
                }

                ApplyConstructionTransparencyRecursive(constructionSite);

                var confirmedTransform = _ghost.GlobalTransform;
                _ghost.QueueFree();
                _ghost = null;

                OnPlacementConfirmed?.Invoke(_activeRecipe, confirmedTransform, constructionSite);
            }
        }

        private void HandleRotationInput(float delta)
        {
            if (Input.IsActionPressed("deploy_rotate_left"))  _currentRotationY += RotationSpeed * delta;
            if (Input.IsActionPressed("deploy_rotate_right")) _currentRotationY -= RotationSpeed * delta;
        }

        private void UpdateGhostPosition()
        {
            var camera = GetViewport().GetCamera3D();
            if (camera == null) return;

            var origin    = camera.GlobalPosition;
            var direction = -camera.GlobalTransform.Basis.Z;
            var end       = origin + direction * 2.0f;

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
                _ghost.GlobalPosition = end;
            }

            var forward = (origin - _ghost.GlobalPosition);
            forward.Y = 0;

            Basis basis;
            if (forward.Length() > 0.1f)
            {
                basis = Basis.LookingAt(-forward.Normalized(), normal);
            }
            else
            {
                basis = Basis.LookingAt(Vector3.Forward, normal);
            }
            
            basis = basis.Rotated(normal, _currentRotationY);
            var newTransform = new Transform3D(basis, _ghost.GlobalPosition);

            _ghost.GlobalTransform = newTransform;
        }

        private void UpdateGhostValidity()
        {
            var spaceState = GetWorld3D().DirectSpaceState;
            var start = _ghost.GlobalPosition + Vector3.Up * 0.1f;
            var end   = _ghost.GlobalPosition + Vector3.Down * 0.2f;
            var query = PhysicsRayQueryParameters3D.Create(start, end);
            query.CollisionMask = 1;

            var result = spaceState.IntersectRay(query);
            bool hasGround = result.Count > 0;
            bool isObstructed = _ghostArea != null && _ghostArea.GetOverlappingBodies().Count > 0;

            _isValidPosition = hasGround && !isObstructed;
            _ghostMaterial.AlbedoColor = _isValidPosition ? _validColor : _invalidColor;
        }

        // ── Utilidades ─────────────────────────────────────────────────────────

        private Aabb CalculateAABBRecursive(Node node)
        {
            Aabb totalAABB = new Aabb();
            bool first = true;

            void Walk(Node n, Transform3D acc)
            {
                Transform3D cur = acc;
                if (n != node && n is Node3D n3d) cur = acc * n3d.Transform;
                if (n is MeshInstance3D mesh)
                {
                    Aabb local = mesh.GetAabb();
                    Aabb world = cur * local;
                    if (first) { totalAABB = world; first = false; }
                    else         totalAABB = totalAABB.Merge(world);
                }
                foreach (Node child in n.GetChildren()) Walk(child, cur);
            }

            Walk(node, Transform3D.Identity);
            return first ? new Aabb(new Vector3(-0.2f, 0, -0.2f), new Vector3(0.4f, 0.4f, 0.4f)) : totalAABB;
        }
    }
}
