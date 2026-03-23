using Godot;

namespace Wild.Data.Inventory
{
    public abstract partial class InventoryItem : Resource
    {
        public abstract string Id { get; set; }
        public abstract string Name { get; set; }
        public abstract string Description { get; set; }
        public abstract string IconPath { get; set; }
        public abstract int StackSize { get; set; }
        public abstract float Weight { get; set; }

        public string GetIconPath()
        {
            if (Wild.Core.Quality.QualityManager.Instance == null) return IconPath;
            string quality = Wild.Core.Quality.QualityManager.Instance.Settings.IconQuality.ToString().ToLower();
            return $"res://assets/textures/items/{Id.ToLower()}/{quality}.png";
        }
    }
}
