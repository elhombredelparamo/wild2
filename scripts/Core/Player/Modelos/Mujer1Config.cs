using Godot;
using Wild.Utils;

namespace Wild.Core.Player
{
    public class Mujer1Config : IModeloConfig
    {
        public string Id => "mujer1";
        public string NombreDisplay => "Mujer Típica";
        public string RutaEscena => "res://scenes/player/animada_con_tree.tscn";
        
        // ESCALA CORRECTORA: El nuevo modelo de Blender 5.0.1 llega 100 veces más pequeño (1cm unit)
        // Aplicamos 100.0f para recuperar el tamaño humano real (1.7m aprox)
        public float EscalaBase => 100.0f; 
        public float AlturaCamara => 1.65f;
        public float OffsetCamaraFrontal => 0.22f;

        public void AplicarConfiguracion(Node3D visuales)
        {
            // Forzar visibilidad y capas correctas para el renderizado
            PrepararNodosRecursivo(visuales);
            
            // NOTA: Se ha desactivado la sobreescritura de materiales vía código.
            // El nuevo modelo GLB ya trae sus materiales correctamente mapeados (UVs).
        }

        private void PrepararNodosRecursivo(Node node)
        {
            if (node is VisualInstance3D vi)
            {
                vi.Layers = 1; // Capa 1: Mundo/Jugador
            }
            
            if (node is MeshInstance3D mesh)
            {
                mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
            }

            foreach (Node child in node.GetChildren())
            {
                PrepararNodosRecursivo(child);
            }
        }

        // Métodos de la interfaz que deben estar presentes aunque no se usen para lógica compleja
        public void ConfigurarMateriales(Node3D visuales) { }
        public void ActualizarAnimacionProcedural(Node3D visuales, Skeleton3D esqueleto, float delta, bool moviendose) { }
    }
}
