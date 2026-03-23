namespace Wild.Data
{
    public class DeployableData
    {
        public string TypeId { get; set; }
        public SerializableVector3 Position { get; set; }
        public SerializableVector3 Rotation { get; set; }
        public string CustomData { get; set; } // JSON string para datos persistentes específicos

        public DeployableData() {}

        public DeployableData(string typeId, Godot.Vector3 pos, Godot.Vector3 rot, string data = "")
        {
            TypeId = typeId;
            Position = new SerializableVector3(pos);
            Rotation = new SerializableVector3(rot);
            CustomData = data;
        }
    }
}
