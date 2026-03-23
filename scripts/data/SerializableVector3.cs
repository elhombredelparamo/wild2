using Godot;

namespace Wild.Data
{
    /// <summary>
    /// Estructura interna para serializar vectores de Godot en JSON de forma compatible.
    /// </summary>
    public class SerializableVector3
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public SerializableVector3() { }
        
        public SerializableVector3(Vector3 v)
        {
            x = v.X;
            y = v.Y;
            z = v.Z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }
}
