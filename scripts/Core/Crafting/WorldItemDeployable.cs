using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using Wild.Core.Deployables.Base;
using Wild.Core.Terrain;
using Wild.Data.Inventory;
using Wild.Utils;

namespace Wild.Core.Crafting
{
    /// <summary>
    /// Objeto crafteable terminado que existe físicamente en el mundo.
    /// El jugador puede recogerlo con E (va al inventario) o dejarlo persistentemente en el chunk.
    /// Hereda de DeployableBase exclusivamente para aprovechar el sistema de persistencia de chunks.
    /// No forma parte del sistema de Deployables en ningún otro sentido.
    /// </summary>
    public partial class WorldItemDeployable : DeployableBase
    {
        // ── Estado ────────────────────────────────────────────────────────────

        /// <summary>ID del InventoryItem que se otorga al recoger este objeto.</summary>
        public string ItemId { get; private set; } = "";

        private Label3D _pickupLabel;
        private bool _isBeingPickedUp = false;

        // ── Inicialización ────────────────────────────────────────────────────

        public void SetItemId(string itemId)
        {
            ItemId = itemId;
        }

        public override void _Ready()
        {
            CreatePickupLabel();
            CreateCollision();
        }

        private void CreatePickupLabel()
        {
            _pickupLabel = new Label3D();
            _pickupLabel.Name = "PickupLabel";
            _pickupLabel.Text = "[E] Recoger";
            _pickupLabel.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
            _pickupLabel.Position = new Vector3(0, 0.8f, 0);
            _pickupLabel.FontSize = 48;
            _pickupLabel.OutlineSize = 12;
            _pickupLabel.Modulate = new Color(0.2f, 1f, 0.4f); // Verde
            AddChild(_pickupLabel);
        }

        private void CreateCollision()
        {
            // Usamos StaticBody3D en capa 4 (interacciones) para que el Raycast del jugador lo detecte
            var staticBody = new StaticBody3D();
            staticBody.CollisionLayer = 8; // Bit 4 (capa 4)
            staticBody.CollisionMask = 0;

            var colShape = new CollisionShape3D();
            var box = new BoxShape3D { Size = new Vector3(0.4f, 0.4f, 0.4f) };
            colShape.Shape = box;
            colShape.Position = new Vector3(0, 0.2f, 0);
            staticBody.AddChild(colShape);
            AddChild(staticBody);
        }

        // ── Interacción (recogida con E) ──────────────────────────────────────

        public override void Interact()
        {
            if (_isBeingPickedUp) return;
            if (string.IsNullOrEmpty(ItemId))
            {
                Logger.LogWarning($"WORLD_ITEM: Intento de recoger un item sin ItemId en {GlobalPosition}");
                return;
            }

            var invManager = InventoryManager.Instance;
            if (invManager == null)
            {
                Logger.LogWarning("WORLD_ITEM: InventoryManager no disponible.");
                return;
            }

            bool success = invManager.GiveItem(ItemId, 1);

            if (success)
            {
                Logger.LogInfo($"WORLD_ITEM: '{ItemId}' recogido correctamente. Eliminando del mundo.");
                _isBeingPickedUp = true;
                // Eliminar del mundo y del chunk JSON
                TerrainManager.Instance?.RemoveDeployable(this);
            }
            else
            {
                Logger.LogInfo($"WORLD_ITEM: No se pudo añadir '{ItemId}' al inventario (¿lleno?).");
                if (_pickupLabel != null)
                {
                    _pickupLabel.Text = "Inventario lleno";
                    _pickupLabel.Modulate = new Color(1f, 0.3f, 0.3f); // Rojo
                    // Restaurar label tras 2 segundos
                    GetTree().CreateTimer(2.0).Timeout += () =>
                    {
                        if (IsInstanceValid(_pickupLabel))
                        {
                            _pickupLabel.Text = "[E] Recoger";
                            _pickupLabel.Modulate = new Color(0.2f, 1f, 0.4f);
                        }
                    };
                }
            }
        }

        // ── Persistencia ──────────────────────────────────────────────────────

        public override string SaveData()
        {
            var state = new WorldItemSaveState { ItemId = ItemId };
            return JsonSerializer.Serialize(state);
        }

        public override void LoadData(string data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data) || data == "{}") return;

                var state = JsonSerializer.Deserialize<WorldItemSaveState>(data);
                if (state != null && !string.IsNullOrEmpty(state.ItemId))
                {
                    ItemId = state.ItemId;
                    Logger.LogInfo($"WORLD_ITEM: Cargado ítem '{ItemId}' desde chunk.");
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"WORLD_ITEM: Error al cargar datos: {e.Message}");
            }
        }
    }

    // ── DTO de persistencia ────────────────────────────────────────────────────

    public class WorldItemSaveState
    {
        [JsonInclude] public string ItemId { get; set; } = "";
    }
}
