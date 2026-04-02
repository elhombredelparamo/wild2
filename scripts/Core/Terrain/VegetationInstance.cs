using Godot;

namespace Wild.Core.Terrain
{
    public struct VegetationInstance
    {
        public int Index;
        public string LootTableId;
        public string ModelPath;
        public Vector3 Position;
        public float RotationY;
        public float Scale;
        public bool  HasCollision;
        public Vector3 Normal;
        public bool AlignToNormal;
    }
}
