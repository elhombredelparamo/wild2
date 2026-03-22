using System;
using Godot;
using System.Reflection;

public partial class InspectMesh : Node
{
    public override void _Ready()
    {
        Type meshType = typeof(Mesh);
        GD.Print($"Inspecting type: {meshType.FullName}");
        foreach (MethodInfo method in meshType.GetMethods())
        {
            if (method.Name.ToLower().Contains("surface") && method.Name.ToLower().Contains("name"))
            {
                GD.Print($"Found method: {method.Name}");
            }
        }
    }
}
