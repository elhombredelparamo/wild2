using System;
using System.IO;
using Wild.Data;
using Wild.Utils;

namespace Wild.Utils
{
    /// <summary>
    /// Utilidad centralizada para registrar y persistir objetos del mundo en archivos JSON.
    /// </summary>
    public static class WorldObjectRegistrar
    {
        /// <summary>
        /// Registra un objeto en el sistema de persistencia del mundo actual.
        /// </summary>
        /// <param name="fileName">Nombre del archivo (ej: 10-5-20-UUID.json)</param>
        /// <param name="jsonContent">Contenido JSON serializado</param>
        public static void RegistrarObjeto(string fileName, string jsonContent)
        {
            try
            {
                string objectsDir = MundoManager.Instance.ObtenerRutaObjetosActual();
                
                if (string.IsNullOrEmpty(objectsDir))
                {
                    Logger.LogWarning("WorldObjectRegistrar: No hay mundo activo para registrar objeto.");
                    return;
                }

                Logger.LogDebug($"WorldObjectRegistrar: Intentando registrar {fileName}");

                string fullPath = Path.Combine(objectsDir, fileName);

                File.WriteAllText(fullPath, jsonContent);
                
                Logger.LogDebug($"WorldObjectRegistrar: OK -> {fileName}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"WorldObjectRegistrar: Error al registrar objeto {fileName}: {ex.Message}");
            }
        }
    }
}
