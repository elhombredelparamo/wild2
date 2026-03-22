using System;

namespace Wild.Data.Inventory
{
    public partial class InventorySlot
    {
        public InventoryItem Item { get; set; }
        public int Quantity { get; set; }
        
        public float TotalWeight => Item != null ? Item.Weight * Quantity : 0f;

        public InventorySlot()
        {
            Item = null;
            Quantity = 0;
        }

        public bool IsEmpty() => Item == null || Quantity <= 0;
    }
}
