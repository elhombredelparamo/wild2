using Godot;
using System.Collections.Generic;
using Wild.Utils;
using Wild.Core.Quality;

namespace Wild.Utils
{
    public static class VegetationLibrary
    {
        private struct VegetationData
        {
            public PackedScene Scene;
            public List<(Shape3D shape, Transform3D localTransform)> CollisionShapes;
            public List<(Mesh mesh, Material material, Transform3D localTransform)> VisualMeshes;
        }

        private static readonly Dictionary<string, VegetationData> _cache = new();
        private static readonly object _lock = new(); // Nueva cerradura

        public static PackedScene GetScene(string path) => GetOrLoad(ResolveQualityPath(path)).Scene;
        public static List<(Shape3D shape, Transform3D localTransform)> GetCollisionShapes(string path) => GetOrLoad(ResolveQualityPath(path)).CollisionShapes;
        public static List<(Mesh mesh, Material material, Transform3D localTransform)> GetVisualMeshes(string path) => GetOrLoad(ResolveQualityPath(path)).VisualMeshes;

        // Variante para plantas: usa VegetationQuality en lugar de TreeQuality
        public static PackedScene GetPlantScene(string path) => GetOrLoad(ResolvePlantQualityPath(path)).Scene;

        /// <summary>
        /// Pre-carga y extrae datos de una lista de modelos desde el hilo principal. 
        /// Evita crashes de hilos al instanciar escenas en segundo plano.
        /// </summary>
        public static void Preload(string[] paths)
        {
            foreach (var path in paths) GetOrLoad(ResolveQualityPath(path));
        }

        public static void PreloadPlants(string[] paths)
        {
            foreach (var path in paths) GetOrLoad(ResolvePlantQualityPath(path));
        }

        private static string ResolvePlantQualityPath(string path)
        {
            string qualitySuffix = GetQualitySuffix(QualityManager.Instance.Settings.VegetationQuality);
            
            // Si el path contiene /ultra/, reemplazamos la carpeta
            if (path.Contains("/ultra/"))
            {
                if (qualitySuffix == "ultra") return path;
                string newPath = path.Replace("/ultra/", $"/{qualitySuffix}/");
                return FileAccess.FileExists(newPath) ? newPath : path;
            }

            // Si es un archivo directo (ej: high.glb), intentamos reemplazar el nombre del archivo
            // Buscamos cualquier nivel de calidad y lo cambiamos por el actual
            string[] qualities = { "ultra", "high", "medium", "low", "toaster" };
            foreach (var q in qualities)
            {
                if (path.EndsWith($"/{q}.glb"))
                {
                    if (qualitySuffix == q) return path;
                    string newPath = path.Substring(0, path.LastIndexOf('/') + 1) + qualitySuffix + ".glb";
                    return FileAccess.FileExists(newPath) ? newPath : path;
                }
            }

            return path;
        }

        private static string ResolveQualityPath(string path)
        {
            string qualitySuffix = GetQualitySuffix(QualityManager.Instance.Settings.TreeQuality);
            
            if (path.Contains("/ultra/"))
            {
                if (qualitySuffix == "ultra") return path;
                string newPath = path.Replace("/ultra/", $"/{qualitySuffix}/");
                return FileAccess.FileExists(newPath) ? newPath : path;
            }

            string[] qualities = { "ultra", "high", "medium", "low", "toaster" };
            foreach (var q in qualities)
            {
                if (path.EndsWith($"/{q}.glb"))
                {
                    if (qualitySuffix == q) return path;
                    string newPath = path.Substring(0, path.LastIndexOf('/') + 1) + qualitySuffix + ".glb";
                    return FileAccess.FileExists(newPath) ? newPath : path;
                }
            }
            
            return path;
        }

        private static string GetQualitySuffix(QualityLevel level)
        {
            return level switch
            {
                QualityLevel.Ultra => "ultra",
                QualityLevel.High => "high",
                QualityLevel.Medium => "medium",
                QualityLevel.Low => "low",
                QualityLevel.Toaster => "toaster",
                _ => "ultra"
            };
        }

        private static VegetationData GetOrLoad(string path)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(path, out var data)) return data;
            }

            Logger.LogInfo($"VegetationLibrary: Cargando y cacheando (Thread-Safe): {path}");

            PackedScene resource = GD.Load<PackedScene>(path);
            var shapes = new List<(Shape3D, Transform3D)>();
            var visualMeshes = new List<(Mesh, Material, Transform3D)>();

            if (resource == null)
            {
                var mesh = GD.Load<Mesh>(path);
                if (mesh != null)
                {
                    var mi = new MeshInstance3D { Mesh = mesh };
                    var generated = new PackedScene();
                    generated.Pack(mi);
                    mi.QueueFree();
                    resource = generated;
                }
            }

            if (resource != null)
            {
                var tempInstance = resource.Instantiate();
                string texturePath = path.Substring(0, path.LastIndexOf('.')) + ".png";
                Texture2D texture = null;
                if (FileAccess.FileExists(texturePath))
                    texture = GD.Load<Texture2D>(texturePath);
                
                ExtractDataRecursivo(tempInstance, shapes, visualMeshes, texture, Transform3D.Identity);
                tempInstance.QueueFree();
            }

            var newData = new VegetationData { 
                Scene = resource, 
                CollisionShapes = shapes,
                VisualMeshes = visualMeshes 
            };

            lock (_lock)
            {
                _cache[path] = newData;
            }
            return newData;
        }

        private static void ExtractDataRecursivo(Node node, List<(Shape3D, Transform3D)> shapes, List<(Mesh, Material, Transform3D)> visualMeshes, Texture2D texture, Transform3D currentTransform)
        {
            Transform3D nextTransform = currentTransform;
            if (node is Node3D n3d)
            {
                nextTransform = currentTransform * n3d.Transform;
            }

            if (node is MeshInstance3D mi)
            {
                // 1. Extraer colisiones (solo no-follaje)
                string meshName = mi.Name.ToString().ToLower();
                bool esFolliage = meshName.Contains("leaf") || 
                                  meshName.Contains("hoja") || 
                                  meshName.Contains("branch") || 
                                  meshName.Contains("rama") || 
                                  meshName.Contains("foliage") ||
                                  meshName.Contains("canopy") ||
                                  meshName.Contains("follaje");

                if (!esFolliage && mi.Mesh != null)
                {
                    shapes.Add((mi.Mesh.CreateTrimeshShape(), nextTransform));
                }

                // 2. Extraer Mallas Visuales para MultiMesh
                if (mi.Mesh != null)
                {
                    Material mat = mi.GetActiveMaterial(0); 
                    
                    if (texture != null && mat is StandardMaterial3D stdMat && stdMat.AlbedoTexture == null)
                    {
                        stdMat.AlbedoTexture = texture;
                    }

                    // IMPORTANTE: Guardamos el transform relativo para MultiMesh
                    visualMeshes.Add((mi.Mesh, mat, nextTransform));
                    
                    Logger.LogInfo($"MESH DEBUG: Mesh '{mi.Name}' AABB Size: {mi.Mesh.GetAabb().Size}");
                }
            }

            foreach (var child in node.GetChildren())
            {
                ExtractDataRecursivo(child, shapes, visualMeshes, texture, nextTransform);
            }
        }

        // Método de utilidad para aplicar la textura a una instancia fresca
        public static void ApplyAutoTexture(Node node, string modelPath)
        {
            // En vez de usar el path original directamente (ej: ultra), resolvemos primero el path
            // dependiente de la calidad actualmente seleccionada para VegetationQuality.
            string resolvedPath = ResolveQualityPath(modelPath);
            string texturePath = resolvedPath.Substring(0, resolvedPath.LastIndexOf('.')) + ".png";
            
            if (FileAccess.FileExists(texturePath))
            {
                var texture = GD.Load<Texture2D>(texturePath);
                if (texture != null)
                {
                    AsignarTexturaRecursivo(node, texture);
                }
            }
        }

        private static void AsignarTexturaRecursivo(Node node, Texture2D texture)
        {
            if (node is MeshInstance3D mi)
            {
                for (int i = 0; i < mi.GetSurfaceOverrideMaterialCount(); i++)
                {
                    var mat = mi.GetActiveMaterial(i);
                    if (mat is StandardMaterial3D stdMat)
                    {
                        if (stdMat.AlbedoTexture == null) stdMat.AlbedoTexture = texture;
                    }
                    else if (mat == null)
                    {
                        var newMat = new StandardMaterial3D { AlbedoTexture = texture };
                        mi.SetSurfaceOverrideMaterial(i, newMat);
                    }
                }
            }
            foreach (var child in node.GetChildren())
            {
                AsignarTexturaRecursivo(child, texture);
            }
        }
    }
}
