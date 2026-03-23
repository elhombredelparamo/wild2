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
                query.CollisionMask = 8; // Solo Capa 4 (Interacciones/Mushroom), ignoramos Terreno
                query.CollideWithAreas = true;
                query.CollideWithBodies = false; // Solo queremos triggers
                query.HitFromInside = true;      // CRÍTICO

                var result = spaceState.IntersectRay(query);

                if (result.Count > 0)
                {
                    var collider = (Node)result["collider"];
                    Logger.LogInfo($"PLAYER: Intento interacción. Rayo colisiona con: {collider.Name} (Tipo: {collider.GetType()})");

                    if (collider is Area3D area && area.HasMeta("item_id"))
                    {
                        string itemId = area.GetMeta("item_id").ToString();
                        Logger.LogInfo($"PLAYER: ¡INTERACCIÓN EXITOSA con '{itemId}' en {area.GlobalPosition}!");
                        
                        bool added = Wild.Data.Inventory.InventoryManager.Instance?.GiveItem(itemId, 1) ?? false;
                        
                        if (added)
                        {
                            Logger.LogInfo($"PLAYER: Item '{itemId}' añadido al inventario.");
                            Wild.Core.Terrain.TerrainManager.Instance?.RemoveVegetationAt(area.GlobalPosition, itemId);
                        }
                        else
                        {
                            Logger.LogWarning($"PLAYER: Inventario lleno, no se puede recoger '{itemId}'.");
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

