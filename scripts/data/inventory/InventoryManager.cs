using System.Collections.Generic;
using Godot;

namespace Wild.Data.Inventory
{
    public partial class InventoryManager : Node
    {
        public static InventoryManager Instance { get; private set; }

        public List<InventoryContainer> Containers { get; private set; } = new List<InventoryContainer>();
        private Dictionary<string, InventoryItem> _itemRegistry = new Dictionary<string, InventoryItem>();

        public InventoryContainer HandLeft { get; private set; }
        public InventoryContainer HandRight { get; private set; }

        public override void _Ready()
        {
            Instance = this;
            InitializeHands();
            ScanForItems();
            GD.Print("[SISTEMA][InventoryManager] Inicializado.");
        }

        private void ScanForItems()
        {
            string path = "res://assets/data/items/";
            if (!DirAccess.DirExistsAbsolute(path))
            {
                GD.PrintErr($"[SISTEMA][InventoryManager] Directorio de items no existe: {path}");
                return;
            }

            using var dir = DirAccess.Open(path);
            if (dir != null)
            {
                dir.ListDirBegin();
                string fileName = dir.GetNext();
                while (!string.IsNullOrEmpty(fileName))
                {
                    if (!dir.CurrentIsDir() && fileName.EndsWith(".tres"))
                    {
                        var item = GD.Load<InventoryItem>(path + fileName);
                        if (item != null)
                        {
                            RegisterItem(item);
                        }
                    }
                    fileName = dir.GetNext();
                }
                dir.ListDirEnd();
            }
        }

        public void RegisterItem(InventoryItem item)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.Id))
            {
                _itemRegistry[item.Id.ToLower()] = item;
                GD.Print($"[SISTEMA][InventoryManager] Item registrado: {item.Id} ({item.Name})");
            }
        }

        public InventoryItem GetItemById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _itemRegistry.TryGetValue(id.ToLower(), out var item);
            return item;
        }

        public bool GiveItem(string id, int quantity)
        {
            var item = GetItemById(id);
            if (item == null) return false;

            // Intentar añadir a cualquier contenedor disponible (prioridad manos)
            foreach (var container in Containers)
            {
                if (container.AddItem(item, quantity))
                {
                    GD.Print($"[SISTEMA][InventoryManager] Otorgado {quantity}x {item.Name} al contenedor {container.Name}");
                    return true;
                }
            }
            
            return false;
        }

        private void InitializeHands()
        {
            // Las manos solo tienen 1 slot y soportan max 10kg
            HandLeft = new InventoryContainer("hand_left", "Mano Izquierda", 1, 10.0f);
            HandLeft.IconPath = "res://assets/textures/items/manoizq.png";
            
            HandRight = new InventoryContainer("hand_right", "Mano Derecha", 1, 10.0f);
            HandRight.IconPath = "res://assets/textures/items/manoder.png";
            
            Containers.Add(HandLeft);
            Containers.Add(HandRight);
            
            GD.Print("[SISTEMA][InventoryManager] Contenedores de mano listos (1 slot, 10kg cada uno).");
        }

        public void AddContainer(InventoryContainer container)
        {
            if (!Containers.Contains(container))
            {
                Containers.Add(container);
                GD.Print($"[SISTEMA][InventoryManager] Nuevo contenedor registrado: {container.Name}");
            }
        }

        public List<InventoryContainer> GetActiveContainers()
        {
            return Containers;
        }
    }
}
