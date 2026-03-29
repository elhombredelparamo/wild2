using Godot;
using System;

[Tool]
public partial class InspectModel : EditorScript
{
    public override void _Run()
    {
        GD.Print("Iniciando inspección del modelo...");
        PackedScene scene = GD.Load<PackedScene>("res://scenes/player/animada_con_tree.tscn");
        if (scene != null)
        {
            Node instance = scene.Instantiate();
            PrintTree(instance, 0);
            instance.QueueFree();
            GD.Print("Inspección finalizada.");
        }
        else
        {
            GD.PrintErr("Error: No se pudo cargar la escena.");
        }
    }

    private void PrintTree(Node node, int level)
    {
        string indent = new string(' ', level * 2);
        string type = node.GetType().Name;
        GD.Print($"{indent}- {node.Name} ({type})");
        
        if (node is MeshInstance3D mesh)
        {
            GD.Print($"{indent}  Mesh: {(mesh.Mesh != null ? mesh.Mesh.ResourceName : "null")}");
            GD.Print($"{indent}  Layers: {mesh.Layers}");
            
            for (int i = 0; i < mesh.GetSurfaceOverrideMaterialCount(); i++)
            {
                Material mat = mesh.Mesh.SurfaceGetMaterial(i);
                string matName = mat != null ? mat.ResourceName : "null";
                GD.Print($"{indent}  Surface {i} Material: {matName}");
            }
        }
        
        foreach (Node child in node.GetChildren())
        {
            PrintTree(child, level + 1);
        }
    }
}
