using Godot;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using Wild.Data;
using Wild.Utils;

namespace Wild.Data.Inventory
{
    public partial class InventoryContainer : GodotObject
    {
        public delegate int ItemValidationDelegate(InventoryItem item, int requestedQuantity);
        
        public event System.Action OnChanged;
        public string Id { get; set; }
        public string Name { get; set; }
        public string IconPath { get; set; }
        public string QualityIconId { get; set; }
        public List<InventorySlot> Slots { get; set; } = new List<InventorySlot>();
        public int MaxSlots { get; set; }
        public float MaxWeight { get; set; }
        public ItemValidationDelegate Validator { get; set; }

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

        public string GetIconPath()
        {
            if (string.IsNullOrEmpty(QualityIconId)) return IconPath;
            if (Wild.Core.Quality.QualityManager.Instance == null) return IconPath;
            string quality = Wild.Core.Quality.QualityManager.Instance.Settings.IconQuality.ToString().ToLower();
            return $"res://assets/textures/items/{QualityIconId.ToLower()}/{quality}.png";
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

            // 1. Validate if target can accept this item (fully or partially)
            int requestedQty = sourceSlot.Quantity;
            int allowedQty = target.Validator != null ? target.Validator(sourceSlot.Item, requestedQty) : requestedQty;

            if (allowedQty <= 0) return false;

            // 2. CASE: STACK (Same Item)
            if (!targetSlot.IsEmpty() && targetSlot.Item != null && targetSlot.Item.Id == sourceSlot.Item.Id)
            {
                int spaceInTarget = targetSlot.Item.StackSize - targetSlot.Quantity;
                int toMove = Mathf.Min(allowedQty, spaceInTarget);
                
                if (toMove > 0)
                {
                    targetSlot.Quantity += toMove;
                    sourceSlot.Quantity -= toMove;
                    if (sourceSlot.Quantity <= 0) sourceSlot.Item = null;
                    
                    OnChanged?.Invoke();
                    target.OnChanged?.Invoke();
                    return true;
                }
                return false;
            }

            // 3. CASE: Different Item (SWAP or MOVE TO EMPTY)
            // If target or source have validators, we MUST check if the resulting items are valid.
            // Note: Partial moves are NOT possible during a SWAP (different items).
            if (!targetSlot.IsEmpty())
            {
                // Can target accept the incoming item (FULL check)?
                if (target.Validator != null && target.Validator(sourceSlot.Item, sourceSlot.Quantity) < sourceSlot.Quantity)
                    return false;
                
                // Can WE accept the outgoing item from target (FULL check)?
                if (Validator != null && Validator(targetSlot.Item, targetSlot.Quantity) < targetSlot.Quantity)
                    return false;

                // VALID SWAP
                var tempItem = targetSlot.Item;
                int tempQty = targetSlot.Quantity;

                targetSlot.Item = sourceSlot.Item;
                targetSlot.Quantity = sourceSlot.Quantity;

                sourceSlot.Item = tempItem;
                sourceSlot.Quantity = tempQty;
            }
            else
            {
                // MOVE TO EMPTY (Supports partial move if allowedQty < sourceSlot.Quantity)
                int toMove = allowedQty;
                targetSlot.Item = sourceSlot.Item;
                targetSlot.Quantity = toMove;
                
                sourceSlot.Quantity -= toMove;
                if (sourceSlot.Quantity <= 0) sourceSlot.Item = null;
            }

            OnChanged?.Invoke();
            target.OnChanged?.Invoke();
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

        public int GetTotalQuantity(string itemId)
        {
            int total = 0;
            foreach (var slot in Slots)
            {
                if (!slot.IsEmpty() && slot.Item != null && slot.Item.Id == itemId)
                {
                    total += slot.Quantity;
                }
            }
            return total;
        }

        public bool AddItem(InventoryItem item, int quantity)
        {
            if (item == null || quantity <= 0) return false;

            // 1. Validate if we can accept this item
            int allowedQty = Validator != null ? Validator(item, quantity) : quantity;
            if (allowedQty <= 0) return false;

            int remaining = allowedQty;

            // 2. Intentar apilar en slots existentes
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
            
            if (remaining < quantity)
            {
                OnChanged?.Invoke();
                return true;
            }
            return false;
        }

        public string ToData()
        {
            var data = Slots.Select(slot => new InventorySlotData
            {
                item_id = slot.IsEmpty() ? null : slot.Item.Id,
                quantity = slot.Quantity
            }).ToList();

            return JsonSerializer.Serialize(data);
        }

        public void FromData(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var data = JsonSerializer.Deserialize<List<InventorySlotData>>(json);
                if (data == null) return;

                for (int i = 0; i < MaxSlots && i < data.Count; i++)
                {
                    var sData = data[i];
                    if (!string.IsNullOrEmpty(sData.item_id))
                    {
                        var item = InventoryManager.Instance?.GetItemById(sData.item_id);
                        if (item != null)
                        {
                            Slots[i].Item = item;
                            Slots[i].Quantity = sData.quantity;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"InventoryContainer: Error al cargar datos: {ex.Message}");
            }

            OnChanged?.Invoke();
        }
    }
}
