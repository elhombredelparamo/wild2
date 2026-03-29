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
        public InventoryContainer Equipment { get; private set; }
        public InventoryContainer BackpackStorage { get; private set; }

        public override void _Ready()
        {
            Instance = this;
            InitializeHands();
            InitializeEquipment();
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

        /// <summary>
        /// Comprueba si todos los items especificados caben en el inventario (Peso y Slots).
        /// </summary>
        public bool CanFitItems(Dictionary<string, int> itemsToFit)
        {
            float extraWeight = 0;
            int neededSlots = 0;
            
            // Diccionario para rastrear cuánto espacio queda en stacks existentes por ItemId
            var stackSpace = new Dictionary<string, int>();

            foreach (var kvp in itemsToFit)
            {
                var item = GetItemById(kvp.Key);
                if (item == null) continue;
                
                extraWeight += item.Weight * kvp.Value;
                
                // Calcular cuánto de este item podemos meter en stacks existentes
                int currentInStacks = 0;
                foreach (var container in Containers)
                {
                    foreach (var slot in container.Slots)
                    {
                        if (!slot.IsEmpty() && slot.Item.Id == item.Id)
                        {
                            currentInStacks += (item.StackSize - slot.Quantity);
                        }
                    }
                }
                
                int remainingAfterStacks = Mathf.Max(0, kvp.Value - currentInStacks);
                if (remainingAfterStacks > 0)
                {
                    neededSlots += Mathf.CeilToInt((float)remainingAfterStacks / item.StackSize);
                }
            }

            // 1. Validar Peso Total
            float totalMaxWeight = 0;
            float currentTotalWeight = 0;
            foreach (var c in Containers) {
                totalMaxWeight += c.MaxWeight;
                currentTotalWeight += c.GetCurrentWeight();
            }
            if (currentTotalWeight + extraWeight > totalMaxWeight) return false;

            // 2. Validar Slots Vacíos
            int emptySlots = 0;
            foreach (var c in Containers) {
                foreach (var s in c.Slots) if (s.IsEmpty()) emptySlots++;
            }
            
            return emptySlots >= neededSlots;
        }

        /// <summary>
        /// Añade múltiples ítems al inventario. Solo usar tras CanFitItems.
        /// </summary>
        public void GiveItems(Dictionary<string, int> itemsToAdd)
        {
            foreach (var kvp in itemsToAdd)
            {
                GiveItem(kvp.Key, kvp.Value);
            }
        }

        public List<InventoryContainerData> GetPersistenceData()
        {
            var data = new List<InventoryContainerData>();
            foreach (var container in Containers)
            {
                var cData = new InventoryContainerData { container_id = container.Id };
                foreach (var slot in container.Slots)
                {
                    if (!slot.IsEmpty() && slot.Item != null)
                    {
                        cData.slots.Add(new InventorySlotData 
                        { 
                            item_id = slot.Item.Id, 
                            quantity = slot.Quantity 
                        });
                    }
                    else
                    {
                        cData.slots.Add(new InventorySlotData { item_id = null, quantity = 0 });
                    }
                }
                data.Add(cData);
            }
            return data;
        }

        public void LoadPersistenceData(List<InventoryContainerData> data)
        {
            if (data == null) return;

            foreach (var cData in data)
            {
                var container = Containers.Find(c => c.Id == cData.container_id);
                if (container != null)
                {
                    container.Slots.Clear();
                    for (int i = 0; i < container.MaxSlots; i++)
                    {
                        var slot = new InventorySlot();
                        if (i < cData.slots.Count)
                        {
                            var sData = cData.slots[i];
                            if (!string.IsNullOrEmpty(sData.item_id))
                            {
                                var item = GetItemById(sData.item_id);
                                if (item != null)
                                {
                                    slot.Item = item;
                                    slot.Quantity = sData.quantity;
                                }
                            }
                        }
                        container.Slots.Add(slot);
                    }
                }
            }
            GD.Print("[SISTEMA][InventoryManager] Datos de inventario restaurados.");
        }

        private void InitializeHands()
        {
            // Las manos solo tienen 1 slot y soportan max 10kg
            HandLeft = new InventoryContainer("hand_left", "Mano Izquierda", 1, 10.0f);
            HandLeft.IconPath = "res://assets/textures/items/manoizq.png";
            HandLeft.QualityIconId = "manoizq";
            
            HandRight = new InventoryContainer("hand_right", "Mano Derecha", 1, 10.0f);
            HandRight.IconPath = "res://assets/textures/items/manoder.png";
            HandRight.QualityIconId = "manoder";
            
            Containers.Add(HandLeft);
            Containers.Add(HandRight);
            
            GD.Print("[SISTEMA][InventoryManager] Contenedores de mano listos (1 slot, 10kg cada uno).");
        }

        private void InitializeEquipment()
        {
            Equipment = new InventoryContainer("equipment", "Equipamiento", 1, 10.0f);
            BackpackStorage = new InventoryContainer("backpack_storage", "Mochila", 20, 50.0f);
            BackpackStorage.IconPath = "res://assets/textures/items/mochila1.png";
            BackpackStorage.QualityIconId = "mochila1";
            
            // NO añadimos Equipment a Containers para que no salga en la UI
        }

        public bool IsBackpackEquipped()
        {
            return Equipment != null && !Equipment.Slots[0].IsEmpty();
        }

        public void EquipBackpack(Mochila mochilaItem, InventoryContainer source, int index)
        {
            if (mochilaItem == null || source == null) return;
            if (IsBackpackEquipped()) return;

            if (source.MoveItem(index, Equipment, 0))
            {
                BackpackStorage.MaxSlots = mochilaItem.Capacity;
                BackpackStorage.MaxWeight = mochilaItem.MaxWeight;
                while (BackpackStorage.Slots.Count < BackpackStorage.MaxSlots)
                    BackpackStorage.Slots.Add(new InventorySlot());
                
                AddContainer(BackpackStorage);
            }
        }

        public bool UnequipBackpack()
        {
            if (!IsBackpackEquipped()) return false;
            if (!BackpackStorage.IsEmpty()) return false;

            var mochilaItem = Equipment.Slots[0].Item;
            if (GiveItem(mochilaItem.Id, 1))
            {
                Equipment.Slots[0].Item = null;
                Equipment.Slots[0].Quantity = 0;
                Containers.Remove(BackpackStorage);
                return true;
            }
            return false;
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
