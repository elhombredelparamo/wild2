using Godot;
using Wild.Utils;
using Wild.Data.Inventory;
using Wild.UI;
using Wild.Core.Terrain;

namespace Wild.Core.Deployables
{
    /// <summary>
    /// Implementación de un cofre desplegable.
    /// </summary>
    public partial class CofreDeployable : DeployableBase
    {
        private InventoryContainer _inventory;
        private bool _isInitialized = false;

        public override void _Ready()
        {
            EnsureInventoryInitialized();
            Logger.LogInfo($"DEPLOYABLE: Cofre '{TypeId}' listo en {GlobalPosition}");
        }

        private void EnsureInventoryInitialized()
        {
            if (_isInitialized) return;
            
            _inventory = new InventoryContainer("chest_" + GetHashCode(), "Cofre", 10, 100.0f);
            _inventory.IconPath = "res://assets/textures/items/cofre.png";
            _inventory.QualityIconId = "cofre";
            _inventory.OnChanged += () => {
                TerrainManager.Instance?.SaveChunkState(ChunkCoord);
            };
            _isInitialized = true;
        }

        public override void LoadData(string data)
        {
            EnsureInventoryInitialized();
            if (!string.IsNullOrEmpty(data))
            {
                _inventory.FromData(data);
                Logger.LogInfo($"DEPLOYABLE: Inventario de cofre cargado ({_inventory.Slots.Count} slots)");
            }
        }

        public override string SaveData()
        {
            return _inventory?.ToData() ?? "";
        }

        public override void Interact()
        {
            Logger.LogInfo($"DEPLOYABLE: Interacción con COFRE en {GlobalPosition}. Abriendo inventario...");
            
            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.SetExternalContainer(_inventory);
                InventoryUI.Instance.Open();
            }
            else
            {
                Logger.LogWarning("DEPLOYABLE: No se encontró instancia de InventoryUI para abrir el cofre.");
            }
        }
    }
}
