using System.Collections.Generic;

namespace Wild.Core.Biomes
{
    /// <summary>
    /// Define un ítem posible que puede soltar un vegetal y su rango de cantidad.
    /// </summary>
    public struct LootEntry
    {
        public string ItemId;
        public int MinAmount;
        public int MaxAmount;

        public LootEntry(string itemId, int min = 1, int max = 1)
        {
            ItemId = itemId;
            MinAmount = min;
            MaxAmount = max;
        }
    }
}
