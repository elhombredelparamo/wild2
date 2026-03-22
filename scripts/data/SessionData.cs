// -----------------------------------------------------------------------------
// Wild v2.0 - Gestión de Sesión y Configuración Persistente
// -----------------------------------------------------------------------------
using Godot;
using System;
using System.IO;
using System.Text.Json;
using Wild.Utils;

namespace Wild.Data
{
    /// <summary>
    /// Singleton para gestionar datos de sesión y configuración global.
    /// Persiste en user://config.json
    /// </summary>
    public class SessionData
    {
        private static SessionData _instance;
        public static SessionData Instance => _instance ??= new SessionData();

        // Configuración de Gráficos
        public int RenderDistance { get; set; } = 50;
        public int ResolutionWidth { get; set; } = 0;
        public int ResolutionHeight { get; set; } = 0;
        
        // Configuración de Audio
        public float MasterVolume { get; set; } = 1.0f;
        public float MusicVolume { get; set; } = 0.5f;

        // Eventos
        public event Action OnSettingsApplied;

        private const string ConfigFileName = "config.json";

        /// <summary>
        /// DTO para la serialización de datos (necesita constructor público).
        /// </summary>
        public class ConfigData
        {
            public int RenderDistance { get; set; } = 50;
            public int ResolutionWidth { get; set; } = 0;
            public int ResolutionHeight { get; set; } = 0;
            public float MasterVolume { get; set; } = 1.0f;
            public float MusicVolume { get; set; } = 0.5f;
        }

        private SessionData()
        {
            LoadConfig();
        }

        public void SaveConfig()
        {
            try
            {
                string path = ProjectSettings.GlobalizePath("user://") + ConfigFileName;
                
                var dto = new ConfigData
                {
                    RenderDistance = this.RenderDistance,
                    ResolutionWidth = this.ResolutionWidth,
                    ResolutionHeight = this.ResolutionHeight,
                    MasterVolume = this.MasterVolume,
                    MusicVolume = this.MusicVolume
                };

                string json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
                Logger.LogInfo($"SessionData: Configuración guardada en {path}");
                
                OnSettingsApplied?.Invoke();
            }
            catch (Exception e)
            {
                Logger.LogError($"SessionData: Error al guardar config: {e.Message}");
            }
        }

        public void LoadConfig()
        {
            try
            {
                string path = ProjectSettings.GlobalizePath("user://") + ConfigFileName;
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var loaded = JsonSerializer.Deserialize<ConfigData>(json);
                    
                    if (loaded != null)
                    {
                        RenderDistance = loaded.RenderDistance;
                        ResolutionWidth = loaded.ResolutionWidth;
                        ResolutionHeight = loaded.ResolutionHeight;
                        MasterVolume = loaded.MasterVolume;
                        MusicVolume = loaded.MusicVolume;
                        Logger.LogInfo("SessionData: Configuración cargada desde disco exitosamente.");
                    }
                }
                else
                {
                    Logger.LogInfo("SessionData: No se encontró archivo de config, usando valores por defecto.");
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"SessionData: Error al cargar config: {e.Message}");
            }
        }
    }
}
