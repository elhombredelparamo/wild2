using Godot;

namespace Wild.Core.Player
{
    /// <summary>
    /// Interfaz base para la configuración técnica de cada modelo de personaje.
    /// Permite desacoplar el controlador de los detalles de implementación de cada malla.
    /// </summary>
    public interface IModeloConfig
    {
        string Id { get; }
        string NombreDisplay { get; }
        string RutaEscena { get; }
        
        float EscalaBase { get; }
        float AlturaCamara { get; }
        float OffsetCamaraFrontal { get; }

        /// <summary>
        /// Aplica correcciones profundas al modelo instanciado (materiales, culling, huesos).
        /// </summary>
        void AplicarConfiguracion(Node3D visuales);
        
        /// <summary>
        /// Configura materiales específicos si el modelo lo requiere.
        /// </summary>
        void ConfigurarMateriales(Node3D visuales);

        /// <summary>
        /// Aplica animaciones por código (respaldo para modelos sin activos de animación).
        /// </summary>
        void ActualizarAnimacionProcedural(Node3D visuales, Skeleton3D esqueleto, float delta, bool moviendose);
    }
}
