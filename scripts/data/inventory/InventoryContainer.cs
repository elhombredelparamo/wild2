using Godot;
using System.Collections.Generic;

namespace Wild.Data.Inventory
{
    public partial class InventoryContainer : GodotObject
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

        /// <summary>
        /// Mueve o intercambia un ítem desde este contenedor hacia otro slot (mismo u otro contenedor).
        /// </summary>
        public bool MoveItem(int fromIndex, InventoryContainer target, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= Slots.Count) return false;
            if (toIndex < 0 || toIndex >= target.Slots.Count) return false;

            var sourceSlot = Slots[fromIndex];
            var targetSlot = target.Slots[toIndex];

            if (sourceSlot.IsEmpty()) return false;

            // Caso 1: Intentar combinar stacks (mismo ID de ítem)
            if (!targetSlot.IsEmpty() && targetSlot.Item != null && targetSlot.Item.Id == sourceSlot.Item.Id)
            {
                int spaceInTarget = targetSlot.Item.StackSize - targetSlot.Quantity;
                if (spaceInTarget > 0)
                {
                    int toMove = Mathf.Min(sourceSlot.Quantity, spaceInTarget);
                    targetSlot.Quantity += toMove;
                    sourceSlot.Quantity -= toMove;
                    if (sourceSlot.Quantity <= 0) sourceSlot.Item = null;
                    return true;
                }
            }

            // Caso 2: Intercambiar o mover a vacío
            // Validar límites de peso en ambos sentidos
            float sourceWeightWithoutItem = GetCurrentWeight() - sourceSlot.TotalWeight;
            float targetWeightWithoutItem = target.GetCurrentWeight() - targetSlot.TotalWeight;

            // ¿Cabe lo que viene del source al target?
            if (targetWeightWithoutItem + sourceSlot.TotalWeight > target.MaxWeight) return false;
            // ¿Cabe lo que viene del target (si es que hay algo) al source?
            if (sourceWeightWithoutItem + targetSlot.TotalWeight > MaxWeight) return false;

            // Ejecutar el intercambio (Swap)
            var tempItem = targetSlot.Item;
            int tempQty = targetSlot.Quantity;

            targetSlot.Item = sourceSlot.Item;
            targetSlot.Quantity = sourceSlot.Quantity;

            sourceSlot.Item = tempItem;
            sourceSlot.Quantity = tempQty;

            return true;
        }

        /// <summary>
        /// Mueve un ítem desde un sourceSlot hacia CUALQUIER slot disponible de este contenedor.
        /// </summary>
        public bool MoveItemToAnySlot(int fromIndex, InventoryContainer source)
        {
            if (source == null || fromIndex < 0 || fromIndex >= source.Slots.Count) return false;
            if (source.Slots[fromIndex].IsEmpty()) return false;

            // Primero intentar apilar en slots con el mismo item
            for (int i = 0; i < Slots.Count; i++)
            {
                if (!Slots[i].IsEmpty() && Slots[i].Item.Id == source.Slots[fromIndex].Item.Id)
                {
                    if (source.MoveItem(fromIndex, this, i)) return true;
                }
            }

            // Si no se pudo apilar (o no había del mismo tipo), buscar el primer hueco vacío
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].IsEmpty())
                {
                    if (source.MoveItem(fromIndex, this, i)) return true;
                }
            }

            return false;
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

        public bool IsEmpty()
        {
            foreach (var slot in Slots)
            {
                if (!slot.IsEmpty()) return false;
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
