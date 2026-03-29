using Godot;
using Wild.Utils;

namespace Wild.Core.Player
{
    public class Hombre1Config : IModeloConfig
    {
        public string Id => "hombre1";
        public string NombreDisplay => "Hombre Típico";
        public string RutaEscena => "res://scenes/player/animado_con_tree.tscn";
        
        // ESCALA PROVISIONAL: Volvemos a 1.0f para medir su tamaño real en Godot
        public float EscalaBase => 1.0f; 
        public float AlturaCamara => 1.68f;
        public float OffsetCamaraFrontal => 0.44f;

        public void AplicarConfiguracion(Node3D visuales)
        {
            // Forzar visibilidad y capas correctas
            PrepararNodosRecursivo(visuales);
            
            // LOG DE DIAGNÓSTICO: Medir tamaño para ajustar escala final
            if (visuales.FindChild("*Mesh*", true, false) is MeshInstance3D mesh)
            {
                Aabb aabb = mesh.GetAabb();
                Logger.LogInfo($"[DIAGNÓSTICO HOMBRE] Malla: {mesh.Name}, Altura Local AABB: {aabb.Size.Y}. Si esto mide ~1.7 el personaje debería estar bien con escala 1.0.");
            }
        }

        private void PrepararNodosRecursivo(Node node)
        {
            if (node is VisualInstance3D vi)
            {
                vi.Layers = 1;
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

        public void ConfigurarMateriales(Node3D visuales) { }
        public void ActualizarAnimacionProcedural(Node3D visuales, Skeleton3D esqueleto, float delta, bool moviendose) { }
    }
}
