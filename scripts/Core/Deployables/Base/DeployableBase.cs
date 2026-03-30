using Godot;
using Wild.Data;

namespace Wild.Core.Deployables.Base
{

    /// <summary>
    /// Clase base para todos los objetos desplegables en el mundo (cofres, hornos, etc).
    /// </summary>
    public abstract partial class DeployableBase : Node3D
    {
        public string TypeId { get; set; }
        public Vector2I ChunkCoord { get; set; }

        public abstract void LoadData(string data);
        public abstract string SaveData();

        public virtual void Interact()
        {
            // Lógica base de interacción
        }
    }
}
