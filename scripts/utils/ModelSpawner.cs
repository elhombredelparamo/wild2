// -----------------------------------------------------------------------------
// Wild v2.0 - Model Spawner Utility
// -----------------------------------------------------------------------------
// 
// DESCRIPCIÓN:
// Sistema encargado de cargar e instanciar modelos 3D (.glb, .tscn, meshes)
// en el mundo de forma dinámica.
// 
// -----------------------------------------------------------------------------
using Godot;
using System;
using Wild.Utils;

namespace Wild.Utils
{
    /// <summary>
    /// Utilidad para instanciar modelos 3D en el mundo.
    /// </summary>
    public partial class ModelSpawner : Node
    {
        private static readonly System.Collections.Generic.Dictionary<string, Resource> _modelCache = new();
        private Node3D _parent;

        /// <summary>
        /// Inicializa el spawner con el nodo padre dondé se colgarán los modelos.
        /// </summary>
        public ModelSpawner(Node3D parent)
        {
            _parent = parent;
        }

        /// <summary>
        /// Instancia un modelo 3D en la posición especificada.
        /// </summary>
        public Node3D SpawnModel(string modelPath, Vector3 position, string name = "Model", bool colisionable = false)
        {
            try
            {
                Logger.LogDebug($"ModelSpawner: Intentando spawnear {modelPath} en {position} (Colisión: {colisionable})");

                Resource modelResource = null;
                
                // Intentar obtener del caché primero
                lock (_modelCache)
                {
                    if (_modelCache.TryGetValue(modelPath, out var cached))
                    {
                        modelResource = cached;
                    }
                }

                if (modelResource == null)
                {
                    if (!FileAccess.FileExists(modelPath))
                    {
                        Logger.LogError($"ModelSpawner: El archivo no existe: {modelPath}");
                        return null;
                    }

                    modelResource = GD.Load(modelPath);
                    if (modelResource == null)
                    {
                        Logger.LogError($"ModelSpawner: No se pudo cargar el recurso: {modelPath}");
                        return null;
                    }

                    // Guardar en caché para la próxima vez
                    lock (_modelCache)
                    {
                        if (!_modelCache.ContainsKey(modelPath))
                        {
                            _modelCache[modelPath] = modelResource;
                            Logger.LogDebug($"ModelSpawner: Recurso {modelPath} añadido al caché.");
                        }
                    }
                }

                Node3D modelNode = null;

                // Caso 1: Es una escena (PackedScene - común para .glb o .tscn)
                if (modelResource is PackedScene packedScene)
                {
                    modelNode = packedScene.Instantiate<Node3D>();
                    modelNode.Name = name;
                    modelNode.GlobalPosition = position;
                    _parent.AddChild(modelNode);

                    if (colisionable)
                    {
                        GenerarColisionParaNodo(modelNode);
                    }

                    AplicarTexturaSiExiste(modelNode, modelPath);

                    Logger.LogInfo($"ModelSpawner: Modelo instanciado correctamente: {name} ({modelPath})");
                }
                // Caso 2: Es un Mesh directo
                else if (modelResource is Mesh mesh)
                {
                    var meshInstance = new MeshInstance3D();
                    meshInstance.Mesh = mesh;
                    meshInstance.Name = name;
                    meshInstance.GlobalPosition = position;
                    _parent.AddChild(meshInstance);
                    modelNode = meshInstance;

                    if (colisionable)
                    {
                        meshInstance.CreateTrimeshCollision();
                    }

                    AplicarTexturaSiExiste(meshInstance, modelPath);

                    Logger.LogInfo($"ModelSpawner: Mesh instanciado correctamente: {name}");
                }
                else
                {
                    Logger.LogWarning($"ModelSpawner: El recurso cargado no es un tipo soportado (PackedScene/Mesh): {modelResource.GetType()}");
                }

                return modelNode;
            }
            catch (Exception ex)
            {
                Logger.LogError($"ModelSpawner: Excepción al spawnear modelo: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Intenta encontrar una textura .png con el mismo nombre que el modelo y aplicarla.
        /// Útil si el exportador del GLB no embebió las texturas.
        /// </summary>
        private void AplicarTexturaSiExiste(Node node, string modelPath)
        {
            string texturePath = modelPath.Substring(0, modelPath.LastIndexOf('.')) + ".png";
            
            if (FileAccess.FileExists(texturePath))
            {
                var texture = GD.Load<Texture2D>(texturePath);
                if (texture != null)
                {
                    Logger.LogDebug($"ModelSpawner: Auto-asignando textura encontrada: {texturePath}");
                    AsignarTexturaRecursivo(node, texture);
                }
            }
        }

        private void AsignarTexturaRecursivo(Node node, Texture2D texture)
        {
            if (node is MeshInstance3D meshInstance)
            {
                for (int i = 0; i < meshInstance.GetSurfaceOverrideMaterialCount(); i++)
                {
                    var mat = meshInstance.GetActiveMaterial(i);
                    if (mat is StandardMaterial3D stdMat)
                    {
                        if (stdMat.AlbedoTexture == null)
                        {
                            stdMat.AlbedoTexture = texture;
                        }
                    }
                    else if (mat == null)
                    {
                        // Si no tiene material, creamos uno básico con la textura
                        var newMat = new StandardMaterial3D();
                        newMat.AlbedoTexture = texture;
                        meshInstance.SetSurfaceOverrideMaterial(i, newMat);
                    }
                }
            }

            foreach (var child in node.GetChildren())
            {
                AsignarTexturaRecursivo(child, texture);
            }
        }

        /// <summary>
        /// Busca todos los MeshInstance3D hijos y genera colisiones para ellos.
        /// Filtra mallas de follaje/hojas para evitar atascos.
        /// </summary>
        private void GenerarColisionParaNodo(Node node)
        {
            if (node is MeshInstance3D meshInstance)
            {
                string meshName = meshInstance.Name.ToString().ToLower();
                
                // Lista de términos que indican que la malla es follaje o ramas pequeñas atravesables
                bool esFolliage = meshName.Contains("leaf") || 
                                  meshName.Contains("hoja") || 
                                  meshName.Contains("branch") || 
                                  meshName.Contains("rama") || 
                                  meshName.Contains("foliage") ||
                                  meshName.Contains("canopy") ||
                                  meshName.Contains("follaje");

                if (!esFolliage)
                {
                    meshInstance.CreateTrimeshCollision();
                }
                else
                {
                    Logger.LogDebug($"ModelSpawner: Ignorando colisión para malla de follaje: {meshInstance.Name}");
                }
            }

            foreach (var child in node.GetChildren())
            {
                GenerarColisionParaNodo(child);
            }
        }
    }
}
