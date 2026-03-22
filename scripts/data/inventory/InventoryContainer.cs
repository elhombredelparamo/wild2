using System.Collections.Generic;

namespace Wild.Data.Inventory
{
    public partial class InventoryContainer
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string IconPath { get; set; }
        public List<InventorySlot> Slots { get; set; } = new List<InventorySlot>();
        public int MaxSlots { get; set; }
        public float MaxWeight { get; set; }

        public InventoryContainer(string id, string name, int maxSlots, float maxWeight)
        {
            Id = id;
            Name = name;
            MaxSlots = maxSlots;
            MaxWeight = maxWeight;
            
            // Inicializar slots vacíos
            for (int i = 0; i < maxSlots; i++)
            {
                Slots.Add(new InventorySlot());
            }
        }

        public float GetCurrentWeight()
        {
            float total = 0;
            foreach (var slot in Slots)
            {
                total += slot.TotalWeight;
            }
            return total;
        }

        public bool IsFull()
        {
            // Para el caso de las manos (MaxSlots = 1), verificamos si el slot está ocupado
            if (MaxSlots == 1) return !Slots[0].IsEmpty();
            
            // Para otros contenedores, se implementará luego la lógica de slots libres
            return false; 
        }
    }
}
