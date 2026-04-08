using System;
using System.Text.Json.Serialization;
using Godot;

namespace Wild.Data
{
    /// <summary>
    /// Estructura de datos para la persistencia del jugador en un mundo.
    /// </summary>
    public class PlayerData
    {
        public string id_personaje { get; set; } = "";

        [JsonPropertyName("pos_x")]
        public float pos_x { get; set; } = 0f;

        [JsonPropertyName("pos_y")]
        public float pos_y { get; set; } = 0f;

        [JsonPropertyName("pos_z")]
        public float pos_z { get; set; } = 0f;

        public float rot_y { get; set; } = 0f;

        // Punto de Respawn
        public float spawn_x { get; set; } = 0f;
        public float spawn_y { get; set; } = 0.3f;
        public float spawn_z { get; set; } = 0f;
        
        // Stats
        public HealthData salud { get; set; } = new();
        public float hambre { get; set; } = 100f;
        public float sed { get; set; } = 100f;
        public float energia { get; set; } = 100f;

        // Inventario
        public System.Collections.Generic.List<InventoryContainerData> inventario { get; set; } = new();

        public DateTime ultima_actualizacion { get; set; }

        public Vector3 GetPosition() => new Vector3(pos_x, pos_y, pos_z);
        
        public void SetPosition(Vector3 pos)
        {
            pos_x = pos.X;
            pos_y = pos.Y;
            pos_z = pos.Z;
        }
    }

    public class InventoryContainerData
    {
        public string container_id { get; set; }
        public System.Collections.Generic.List<InventorySlotData> slots { get; set; } = new();
    }

    public class InventorySlotData
    {
        public string item_id { get; set; }
        public int quantity { get; set; }
    }
}
