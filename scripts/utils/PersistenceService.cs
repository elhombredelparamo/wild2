using System;
using System.IO;
using System.Text.Json;
using Godot;

namespace Wild.Utils
{
    /// <summary>
    /// Servicio centralizado para operaciones de persistencia y manejo de archivos JSON.
    /// </summary>
    public static class PersistenceService
    {
        private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower 
        };

        public static bool SaveJson<T>(string path, T data)
        {
            try
            {
                string absolutePath = ProjectSettings.GlobalizePath(path);
                EnsureDirectory(Path.GetDirectoryName(absolutePath));
                
                string json = JsonSerializer.Serialize(data, DefaultOptions);
                File.WriteAllText(absolutePath, json);
                
                Logger.LogDebug($"PersistenceService: Datos guardados en {path}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"PersistenceService: Error al guardar JSON en {path}: {ex.Message}");
                return false;
            }
        }

        public static T LoadJson<T>(string path)
        {
            try
            {
                string absolutePath = ProjectSettings.GlobalizePath(path);
                if (!File.Exists(absolutePath))
                {
                    Logger.LogWarning($"PersistenceService: El archivo no existe: {path}");
                    return default;
                }

                string json = File.ReadAllText(absolutePath);
                return JsonSerializer.Deserialize<T>(json, DefaultOptions);
            }
            catch (Exception ex)
            {
                Logger.LogError($"PersistenceService: Error al cargar JSON desde {path}: {ex.Message}");
                return default;
            }
        }

        public static void EnsureDirectory(string path)
        {
            try
            {
                string absolutePath = ProjectSettings.GlobalizePath(path);
                if (!Directory.Exists(absolutePath))
                {
                    Directory.CreateDirectory(absolutePath);
                    Logger.LogInfo($"PersistenceService: Directorio creado: {path}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"PersistenceService: Error al asegurar directorio {path}: {ex.Message}");
            }
        }

        public static string[] GetFiles(string path, string searchPattern)
        {
            try
            {
                string absolutePath = ProjectSettings.GlobalizePath(path);
                if (!Directory.Exists(absolutePath)) return Array.Empty<string>();
                return Directory.GetFiles(absolutePath, searchPattern);
            }
            catch (Exception ex)
            {
                Logger.LogError($"PersistenceService: Error al listar archivos en {path}: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        public static void DeleteFile(string path)
        {
            try
            {
                string absolutePath = ProjectSettings.GlobalizePath(path);
                if (File.Exists(absolutePath)) File.Delete(absolutePath);
            }
            catch (Exception ex)
            {
                Logger.LogError($"PersistenceService: Error al eliminar archivo {path}: {ex.Message}");
            }
        }

        public static void DeleteDirectory(string path, bool recursive)
        {
            try
            {
                string absolutePath = ProjectSettings.GlobalizePath(path);
                if (Directory.Exists(absolutePath)) Directory.Delete(absolutePath, recursive);
            }
            catch (Exception ex)
            {
                Logger.LogError($"PersistenceService: Error al eliminar directorio {path}: {ex.Message}");
            }
        }
    }
}
