using System;
using Godot;

namespace Wild.Data
{
    /// <summary>
    /// Estructura de datos para la persistencia del jugador en un mundo.
    /// </summary>
    public class PlayerData
    {
        public string id_personaje { get; set; } = "";
        public float pos_x { get; set; }
        public float pos_y { get; set; }
        public float pos_z { get; set; }
        public float rot_y { get; set; }
        
        // Stats
        public float salud { get; set; } = 100f;
        public float hambre { get; set; } = 100f;
        public float sed { get; set; } = 100f;
        public float energia { get; set; } = 100f;

        public DateTime ultima_actualizacion { get; set; }

        public Vector3 GetPosition() => new Vector3(pos_x, pos_y, pos_z);
        
        public void SetPosition(Vector3 pos)
        {
            pos_x = pos.X;
            pos_y = pos.Y;
            pos_z = pos.Z;
        }
    }
}
