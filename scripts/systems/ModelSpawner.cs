using Godot;
using Wild.Systems;

namespace Wild.Systems;

/// <summary>
/// Sistema responsable de cargar y instanciar modelos 3D en el mundo
/// </summary>
public partial class ModelSpawner : Node
{
    private Node3D _parent;

    public ModelSpawner(Node3D parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// Muestra un modelo 3D en coordenadas específicas.
    /// </summary>
    /// <param name="modelPath">Ruta al archivo del modelo (ej: "res://assets/models/realistic_tree.glb")</param>
    /// <param name="position">Posición donde mostrar el modelo</param>
    /// <param name="name">Nombre del objeto (opcional)</param>
    /// <returns>El nodo 3D del modelo instanciado, o null si falla</returns>
    public Node3D SpawnModel(string modelPath, Vector3 position, string name = "Model")
    {
        try
        {
            Logger.Log($"ModelSpawner: SpawnModel() - INICIADO para: {modelPath}");
            
            // Verificar si el archivo existe
            if (!Godot.FileAccess.FileExists(modelPath))
            {
                Logger.LogError($"ModelSpawner: ❌ El modelo no existe: {modelPath}");
                return null;
            }
            
            Logger.Log($"ModelSpawner: ✅ Archivo de modelo encontrado: {modelPath}");
            
            // Cargar el modelo
            var modelResource = GD.Load(modelPath);
            
            if (modelResource == null)
            {
                Logger.LogError($"ModelSpawner: ❌ No se pudo cargar el modelo: {modelPath}");
                return null;
            }
            
            Logger.Log($"ModelSpawner: ✅ Modelo cargado - Tipo: {modelResource.GetType().Name}");
            
            Node3D modelNode = null;
            
            // Si es una PackedScene, instanciarla
            if (modelResource is PackedScene packedScene)
            {
                Logger.Log("ModelSpawner: Modelo es PackedScene, instanciando");
                modelNode = packedScene.Instantiate<Node3D>();
                modelNode.Name = name;
                modelNode.Position = position;
                
                _parent.AddChild(modelNode);
                Logger.Log($"ModelSpawner: ✅ Modelo instanciado: {modelNode.Name}");
            }
            // Si es un mesh directo, crear MeshInstance3D
            else if (modelResource is Mesh mesh)
            {
                Logger.Log("ModelSpawner: Modelo es Mesh, creando MeshInstance3D");
                var meshInstance = new MeshInstance3D();
                meshInstance.Mesh = mesh;
                meshInstance.Name = name;
                meshInstance.Position = position;
                
                _parent.AddChild(meshInstance);
                modelNode = meshInstance;
                Logger.Log($"ModelSpawner: ✅ MeshInstance3D creado: {modelNode.Name}");
            }
            
            return modelNode;
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"ModelSpawner: ❌ Error instanciando modelo: {ex.Message}");
            return null;
        }
    }
}
