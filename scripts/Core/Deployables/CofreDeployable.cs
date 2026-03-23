using Godot;
using Wild.Utils;

namespace Wild.Core.Deployables
{
    /// <summary>
    /// Implementación de un cofre desplegable.
    /// </summary>
    public partial class CofreDeployable : DeployableBase
    {
        private string _storedData = "";

        public override void _Ready()
        {
            // Aquí podríamos instanciar el modelo visual si no viene en la escena
            Logger.LogInfo($"DEPLOYABLE: Cofre '{TypeId}' listo en {GlobalPosition}");
        }

        public override void LoadData(string data)
        {
            _storedData = data;
            // Aquí se cargarían los ítems del inventario en un futuro
        }

        public override string SaveData()
        {
            return _storedData;
        }

        public override void Interact()
        {
            Logger.LogInfo($"DEPLOYABLE: Interacción con COFRE en {GlobalPosition}. (Datos: {_storedData})");
            // Aquí abriríamos la UI del inventario
        }
    }
}
