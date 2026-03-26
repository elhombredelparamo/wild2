using Godot;
using Wild.Utils;

namespace Wild.Core.Player
{
    public class Mujer1Config : IModeloConfig
    {
        public string Id => "mujer1";
        public string NombreDisplay => "Mujer Típica";
        public string RutaEscena => "res://scenes/player/animada_con_tree.tscn";
        
        public float EscalaBase => 1.0f;
        public float AlturaCamara => 1.65f;
        public float OffsetCamaraFrontal => 0.22f;

        private float _proceduralTime = 0f;

        public void AplicarConfiguracion(Node3D visuales)
        {
            ConfigurarMateriales(visuales);
        }

        public void ConfigurarMateriales(Node3D visuales)
        {
            try
            {
                Material matCuerpo = GD.Load<Material>("res://assets/models/human/fem.tres");
                Material matOjos = GD.Load<Material>("res://assets/models/human/eyeball.tres");
                Material matMandibula = GD.Load<Material>("res://assets/models/human/jaw.tres");

                AsignarMaterialRecursivo(visuales, matCuerpo, matOjos, matMandibula);
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"Mujer1Config: Error al configurar materiales: {ex.Message}");
            }
        }

        public void ActualizarAnimacionProcedural(Node3D visuales, Skeleton3D esqueleto, float delta, bool moviendose)
        {
            if (esqueleto == null) return;

            if (moviendose)
            {
                _proceduralTime += delta * 10.0f;
                float swing = Mathf.Sin(_proceduralTime) * 0.5f;
                
                AplicarSwing(esqueleto, "Arm.L", swing);
                AplicarSwing(esqueleto, "Arm.R", -swing);
                AplicarSwing(esqueleto, "Leg.L", -swing);
                AplicarSwing(esqueleto, "Leg.R", swing);
            }
            else
            {
                _proceduralTime = 0;
                esqueleto.ResetBonePoses();
            }
        }

        private void AplicarSwing(Skeleton3D skeleton, string bonePrefix, float angle)
        {
            for (int i = 0; i < skeleton.GetBoneCount(); i++)
            {
                string name = skeleton.GetBoneName(i).ToLower();
                if (name.Contains(bonePrefix.ToLower()))
                {
                    Quaternion rest = new Quaternion(skeleton.GetBoneRest(i).Basis);
                    Quaternion rot = new Quaternion(new Vector3(1, 0, 0), angle);
                    skeleton.SetBonePoseRotation(i, rest * rot);
                }
            }
        }

        private void AsignarMaterialRecursivo(Node node, Material cuerpo, Material ojos, Material mandibula)
        {
            if (node is MeshInstance3D meshInstance)
            {
                for (int i = 0; i < meshInstance.GetSurfaceOverrideMaterialCount(); i++)
                {
                    Material surfaceMat = meshInstance.Mesh.SurfaceGetMaterial(i);
                    string matName = surfaceMat?.ResourceName.ToLower() ?? "";
                    
                    if (matName.Contains("eye")) meshInstance.SetSurfaceOverrideMaterial(i, ojos);
                    else if (matName.Contains("jaw")) meshInstance.SetSurfaceOverrideMaterial(i, mandibula);
                    else if (matName.Contains("skin") || matName.Contains("body") || matName.Contains("cuerpo"))
                    {
                        var matLocal = (BaseMaterial3D)cuerpo.Duplicate();
                        matLocal.CullMode = BaseMaterial3D.CullModeEnum.Back;
                        meshInstance.SetSurfaceOverrideMaterial(i, matLocal);
                    }
                }
            }
            foreach (Node child in node.GetChildren()) AsignarMaterialRecursivo(child, cuerpo, ojos, mandibula);
        }
    }
}
