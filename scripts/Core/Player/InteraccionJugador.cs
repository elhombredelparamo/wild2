using Godot;
using System;
using System.Collections.Generic;
using Wild.Utils;
using Wild.Core.Biomes;

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

                    // Caso A: Recolectable (Area3D con metadatos de Loot Table)
                    if (collider is Area3D area && area.HasMeta("loot_id"))
                    {
                        string lootId = area.GetMeta("loot_id").ToString();
                        List<LootEntry> lootTable = null;
                        bool isGeo = area.HasMeta("geo_pos_x");

                        if (isGeo)
                            lootTable = GeologyRegistry.GetLootTable(lootId);
                        else
                            lootTable = VegetationRegistry.GetLootTable(lootId);
                        
                        if (lootTable == null || lootTable.Count == 0)
                        {
                            Logger.LogWarning($"PLAYER: El objeto '{lootId}' no tiene tabla de botín configurada.");
                            return;
                        }

                        // 1. Calcular botín final (Rolling por cada entrada)
                        var finalLoot = new Dictionary<string, int>();
                        var rng = new RandomNumberGenerator();
                        rng.Randomize();

                        foreach (var entry in lootTable)
                        {
                            int qty = rng.RandiRange(entry.MinAmount, entry.MaxAmount);
                            if (qty > 0) finalLoot[entry.ItemId] = qty;
                        }

                        if (finalLoot.Count == 0) {
                            Logger.LogInfo("PLAYER: El objeto no ha dado ningún botín esta vez.");
                            if (isGeo) ProcederARemoverGeologia(area, lootId);
                            else ProcederARemoverVegetacion(area, lootId);
                            return;
                        }

                        // 2. Comprobar espacio en inventario de forma ATÓMICA
                        var inv = Wild.Data.Inventory.InventoryManager.Instance;
                        if (inv != null && inv.CanFitItems(finalLoot))
                        {
                            inv.GiveItems(finalLoot);
                            foreach (var kvp in finalLoot) Logger.LogInfo($"PLAYER: Recogido {kvp.Value}x '{kvp.Key}'.");
                            
                            if (isGeo) ProcederARemoverGeologia(area, lootId);
                            else ProcederARemoverVegetacion(area, lootId);
                        }
                        else
                        {
                            Logger.LogInfo("PLAYER: No tienes espacio suficiente para recoger todo el botín.");
                        }
                    }
                    // Caso B: Deployable (StaticBody3D - buscamos DeployableBase en padres)
                    else
                    {
                        Node current = collider;
                        Logger.LogDebug($"PLAYER: Investigando posible deployable en {collider.Name}...");
                        
                        while (current != null && !(current is Wild.Core.Deployables.Base.DeployableBase))
                        {
                            Logger.LogDebug($"   -> Subiendo desde {current.Name} (Tipo: {current.GetType().Name})");
                            current = current.GetParent();
                        }

                        if (current is Wild.Core.Deployables.Base.DeployableBase deployable)
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

        private void ProcederARemoverVegetacion(Area3D area, string lootId)
        {
            Vector3 vegPos = area.GlobalPosition;
            if (area.HasMeta("veg_pos_x"))
            {
                vegPos = new Vector3(
                    (float)area.GetMeta("veg_pos_x"),
                    (float)area.GetMeta("veg_pos_y"),
                    (float)area.GetMeta("veg_pos_z")
                );
            }
            Wild.Core.Terrain.TerrainManager.Instance?.RemoveVegetationAt(vegPos, lootId);
        }

        private void ProcederARemoverGeologia(Area3D area, string lootId)
        {
            Vector3 geoPos = area.GlobalPosition;
            if (area.HasMeta("geo_pos_x"))
            {
                geoPos = new Vector3(
                    (float)area.GetMeta("geo_pos_x"),
                    (float)area.GetMeta("geo_pos_y"),
                    (float)area.GetMeta("geo_pos_z")
                );
            }
            Wild.Core.Terrain.TerrainManager.Instance?.RemoveGeologyAt(geoPos, lootId);
        }
    }
}

