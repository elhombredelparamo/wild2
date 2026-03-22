using Godot;
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
            if (MaxSlots == 1) return !Slots[0].IsEmpty();
            
            foreach (var slot in Slots)
            {
                if (slot.IsEmpty()) return false;
            }
            return true;
        }

        public bool AddItem(InventoryItem item, int quantity)
        {
            if (item == null || quantity <= 0) return false;

            int remaining = quantity;

            // 1. Intentar apilar en slots existentes
            foreach (var slot in Slots)
            {
                if (!slot.IsEmpty() && slot.Item != null && slot.Item.Id == item.Id)
                {
                    int canAdd = slot.Item.StackSize - slot.Quantity;
                    if (canAdd > 0)
                    {
                        int toAdd = Mathf.Min(remaining, canAdd);
                        
                        // Verificar peso antes de añadir
                        float currentWeight = GetCurrentWeight();
                        float weightToAdd = toAdd * item.Weight;
                        
                        if (currentWeight + weightToAdd <= MaxWeight)
                        {
                            slot.Quantity += toAdd;
                            remaining -= toAdd;
                        }
                        else
                        {
                            // Intentar añadir solo lo que quepa por peso
                            int maxByWeight = (int)((MaxWeight - currentWeight) / item.Weight);
                            int toAddRestricted = Mathf.Min(toAdd, maxByWeight);
                            
                            if (toAddRestricted > 0)
                            {
                                slot.Quantity += toAddRestricted;
                                remaining -= toAddRestricted;
                            }
                            break; // No cabe más
                        }
                    }
                }
                if (remaining <= 0) break;
            }

            // 2. Usar slots vacíos si hay espacio
            if (remaining > 0)
            {
                foreach (var slot in Slots)
                {
                    if (slot.IsEmpty())
                    {
                        int toAdd = Mathf.Min(remaining, item.StackSize);
                        float currentWeight = GetCurrentWeight();
                        float weightToAdd = toAdd * item.Weight;

                        if (currentWeight + weightToAdd <= MaxWeight)
                        {
                            slot.Item = item;
                            slot.Quantity = toAdd;
                            remaining -= toAdd;
                        }
                        else
                        {
                            // Intentar añadir parcial por peso
                            int maxByWeight = (int)((MaxWeight - currentWeight) / item.Weight);
                            int toAddRestricted = Mathf.Min(toAdd, maxByWeight);
                            
                            if (toAddRestricted > 0)
                            {
                                slot.Item = item;
                                slot.Quantity = toAddRestricted;
                                remaining -= toAddRestricted;
                            }
                            break; // No cabe más
                        }
                    }
                    if (remaining <= 0) break;
                }
            }

            return remaining < quantity; // True si se añadió al menos algo
        }
    }
}
