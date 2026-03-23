using Godot;
using System.Collections.Generic;

namespace Wild.Data
{
    /// <summary>
    /// Estructura de datos para persistir objetos dinámicos del mundo.
    /// </summary>
    public class WorldObjectData
    {
        public string id { get; set; }           // UUID
        public string type { get; set; }         // Tipo del objeto (ej: "arbol1")
        public SerializableVector3 position { get; set; }
        public SerializableVector3 rotation { get; set; }
        public SerializableVector3 scale { get; set; }
        public Dictionary<string, object> metadata { get; set; } = new();

        public WorldObjectData() { }

        public WorldObjectData(string type, Vector3 pos, Vector3 rot, Vector3 scl)
        {
            this.id = System.Guid.NewGuid().ToString();
            this.type = type;
            this.position = new SerializableVector3(pos);
            this.rotation = new SerializableVector3(rot);
            this.scale = new SerializableVector3(scl);
        }
    }
}
