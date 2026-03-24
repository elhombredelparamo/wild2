using Godot;
using System;
using Wild.Utils;

namespace Wild.Core.Player
{
    /// <summary>
    /// Maneja la interacción del jugador con el entorno mediante Raycasts desde la cámara.
    /// </summary>
    public partial class InteraccionJugador : Node3D
    {
        [Export] public float DistanciaInteraccion = 5.0f;

        public override void _Ready()
        {
            Logger.LogInfo("PLAYER: InteraccionJugador (Camera Raycast) configurado correctamente.");
        }

        public override void _Process(double delta)
        {
            bool isInteractPressed = Input.IsActionJustPressed("ui_interact") || Input.IsActionJustPressed("interactuar") || Input.IsKeyPressed(Key.E);

            if (isInteractPressed)
            {
                var camera = GetViewport().GetCamera3D();
                if (camera == null)
                {
                    Logger.LogWarning("PLAYER: No se encontró cámara activa para lanzar rayo de interacción.");
                    return;
                }

                float distanciaReal = 8.0f;
                var origin = camera.GlobalPosition;
                var end = origin + (-camera.GlobalTransform.Basis.Z.Normalized() * distanciaReal);

                var spaceState = GetWorld3D().DirectSpaceState;
                var query = PhysicsRayQueryParameters3D.Create(origin, end);
                query.CollisionMask = 8; // Capa 4: Interacciones
                query.CollideWithAreas = true;
                query.CollideWithBodies = true; // Soportar StaticBody3D de deployables
                query.HitFromInside = true;

                var result = spaceState.IntersectRay(query);

                if (result.Count > 0)
                {
                    var collider = (Node)result["collider"];
                    Logger.LogInfo($"PLAYER: Interacción con: {collider.Name} (Capa: {collider.GetMeta("_collision_layer", 0)})");

                    // Caso A: Recolectable (Area3D con metadatos)
                    if (collider is Area3D area && area.HasMeta("item_id"))
                    {
                        string itemId = area.GetMeta("item_id").ToString();
                        bool added = Wild.Data.Inventory.InventoryManager.Instance?.GiveItem(itemId, 1) ?? false;
                        if (added)
                        {
                            Logger.LogInfo($"PLAYER: Item '{itemId}' recogido.");
                            // Usamos la posición REAL de la vegetación guardada en metadata
                            // (area.GlobalPosition tiene un offset de +0.5Y del trigger)
                            Vector3 vegPos = area.GlobalPosition;
                            if (area.HasMeta("veg_pos_x"))
                            {
                                vegPos = new Vector3(
                                    (float)area.GetMeta("veg_pos_x"),
                                    (float)area.GetMeta("veg_pos_y"),
                                    (float)area.GetMeta("veg_pos_z")
                                );
                            }
                            Wild.Core.Terrain.TerrainManager.Instance?.RemoveVegetationAt(vegPos, itemId);
                        }
                    }
                    // Caso B: Deployable (StaticBody3D - buscamos DeployableBase en padres)
                    else
                    {
                        Node current = collider;
                        Logger.LogDebug($"PLAYER: Investigando posible deployable en {collider.Name}...");
                        
                        while (current != null && !(current is Wild.Core.Deployables.DeployableBase))
                        {
                            Logger.LogDebug($"   -> Subiendo desde {current.Name} (Tipo: {current.GetType().Name})");
                            current = current.GetParent();
                        }

                        if (current is Wild.Core.Deployables.DeployableBase deployable)
                        {
                            Logger.LogInfo($"PLAYER: Click en DEPLOYABLE '{deployable.TypeId}'");
                            deployable.Interact();
                        }
                        else
                        {
                            Logger.LogDebug("PLAYER: No se encontró DeployableBase en el árbol del colisionador.");
                        }
                    }
                }
                else
                {
                    Logger.LogInfo($"PLAYER: Intento interacción (E). El rayo de la cámara NO colisiona con nada (Distancia de rayo: {distanciaReal}).");
                    
                    // DEBUG: ¿Existen colliders de setas activos?
                    var terr = Wild.Core.Terrain.TerrainManager.Instance;
                    if (terr != null)
                    {
                        int areaCount = 0;
                        foreach (var kvp in terr.GetActiveCollidersForDebug())
                        {
                            if (kvp.Value is Area3D area)
                            {
                                areaCount++;
                                float d = camera.GlobalPosition.DistanceTo(area.GlobalPosition);
                                Logger.LogInfo($"   -> DEBUG: Existe Area3D en {area.GlobalPosition} a {d:0.00} metros de la cámara. ItemId: {(area.HasMeta("item_id") ? area.GetMeta("item_id").ToString() : "NINGUNO")}");
                            }
                        }
                        if (areaCount == 0)
                            Logger.LogInfo("   -> DEBUG: TerrainManager dice que NO HAY ningún Area3D recolectable activo en este momento.");
                    }
                }
            }
        }
    }
}

