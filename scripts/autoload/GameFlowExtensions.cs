using Godot;
using System.Reflection;

namespace Wild;

/// <summary>
/// Extensiones para acceder a campos privados de GameFlow.
/// Necesario para que GameWorld acceda al cliente de red.
/// </summary>
public static class GameFlowExtensions
{
    /// <summary>Obtiene el valor de un campo privado mediante reflexión.</summary>
    public static T GetPrivateField<T>(this GameFlow gameFlow, string fieldName)
    {
        var field = typeof(GameFlow).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            return (T)field.GetValue(gameFlow)!;
        }
        throw new System.Exception($"Campo privado '{fieldName}' no encontrado en GameFlow");
    }
}
