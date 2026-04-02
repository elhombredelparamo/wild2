using Godot;

namespace Wild.Core.Terrain
{
    public struct GeologyInstance
    {
        public int Index;
        public string LootTableId;
        public string ModelPath;
        public Vector3 Position;
        public float RotationY;
        public float Scale;
        public bool  HasCollision;
    }
}
