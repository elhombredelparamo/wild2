// -----------------------------------------------------------------------------
// Wild v2.0 - Sistema de Logger
// -----------------------------------------------------------------------------
// 
// RELACIONADO CON:
// - contexto/logger.md - Sistema de logging estándar
// - codigo/utils/logger.pseudo - Diseño técnico del sistema
// - contexto/fdd-estructura.md - Metodología de desarrollo
// 
// DESCRIPCIÓN:
// Sistema de logging propio que reemplaza GD.Print() para depuración
// en modo pantalla completa, con persistencia en archivo y niveles de gravedad.
// 
// -----------------------------------------------------------------------------
using Godot;
using System;
using System.IO;

namespace Wild.Utils
{
    public static class Logger
    {
        private static bool _isInicializado = false;

        public static void Inicializar()
        {
            if (_isInicializado) return;
            
            try
            {
                // Obtener ruta del directorio de datos de usuario de Godot
                string userDataPath = OS.GetUserDataDir();
                string logPath = System.IO.Path.Combine(userDataPath, "latest.log");
                
                // Si existe un log anterior, eliminarlo
                if (System.IO.File.Exists(logPath))
                {
                    System.IO.File.Delete(logPath);
                }
                
                // Crear nuevo log con marca de inicio
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string inicioLog = $"[{timestamp}] [INFO] ===== INICIO DE EJECUCIÓN WILD v2.0 ====={System.Environment.NewLine}";
                System.IO.File.WriteAllText(logPath, inicioLog);
                
                _isInicializado = true;
                // También mostrar en consola para desarrollo
                GD.Print("Logger: Sistema de logging propio iniciado correctamente");
            }
            catch (System.Exception ex)
            {
                GD.Print($"ERROR iniciando logging: {ex.Message}");
            }
        }
        
        // -------------------------------------------------------------------------
        // LOGGING PROPIO - Escribir mensaje en latest.log
        // -------------------------------------------------------------------------
        public static void Log(string mensaje)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logEntry = $"[{timestamp}] [INFO] {mensaje}{System.Environment.NewLine}";
                
                // Usar el directorio de datos de usuario de Godot para nuestro propio log
                string userDataPath = OS.GetUserDataDir();
                string logPath = System.IO.Path.Combine(userDataPath, "latest.log");
                
                System.IO.File.AppendAllText(logPath, logEntry);
            }
            catch (System.Exception ex)
            {
                // Si falla el logging, no podemos hacer mucho
                GD.Print($"ERROR guardando log: {ex.Message}");
            }
        }
        
        // -------------------------------------------------------------------------
        // LOGGING INFO - Escribir mensaje informativo en latest.log
        // -------------------------------------------------------------------------
        public static void LogInfo(string mensaje)
        {
            // Derivar directamente a Log() ya que usa [INFO] por defecto
            Log(mensaje);
        }
        
        // -------------------------------------------------------------------------
        // LOGGING DEBUG - Escribir mensaje de debug en latest.log
        // -------------------------------------------------------------------------
        public static void LogDebug(string mensaje)
        {
            // Añadir cabecera de DEBUG y derivar a Log()
            Log($"[DEBUG] {mensaje}");
        }
        
        // -------------------------------------------------------------------------
        // LOGGING WARNING - Escribir mensaje de advertencia en latest.log
        // -------------------------------------------------------------------------
        public static void LogWarning(string mensaje)
        {
            // Añadir cabecera de WARNING y derivar a Log()
            Log($"[WARNING] {mensaje}");
        }
        
        // -------------------------------------------------------------------------
        // LOGGING ERROR - Escribir mensaje de error en latest.log
        // -------------------------------------------------------------------------
        public static void LogError(string mensaje)
        {
            // Añadir cabecera de ERROR y derivar a Log()
            Log($"[ERROR] {mensaje}");
        }

        // -------------------------------------------------------------------------
        // LOGGING CONTEXTO - Escribir mensaje con contexto y tag opcional
        // -------------------------------------------------------------------------
        public static void LogWithContext(string contexto, string mensaje, string tag = "")
        {
            string tagStr = string.IsNullOrEmpty(tag) ? "" : $" [{tag}]";
            Log($"[{contexto}]{tagStr} {mensaje}");
        }
    }
}
