using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using Godot;
using Wild.Utils;

namespace Wild.Core.Quality
{
    public class QualitySettings
    {
        // Perfil actual
        public QualityProfileType ProfileType { get; set; } = QualityProfileType.Medium;
        public string CustomProfileName { get; set; } = "Personalizado";
        public bool AutoDetect { get; set; } = true;

        // Configuración individual por componente
        public QualityLevel TreeQuality { get; set; } = QualityLevel.Medium;
        public QualityLevel VegetationQuality { get; set; } = QualityLevel.Medium;
        public QualityLevel TerrainQuality { get; set; } = QualityLevel.Medium;
        public QualityLevel PlayerModelQuality { get; set; } = QualityLevel.Medium;
        public QualityLevel BuildingModelQuality { get; set; } = QualityLevel.Medium;
        public QualityLevel ObjectModelQuality { get; set; } = QualityLevel.Medium;
        public QualityLevel DeployableQuality { get; set; } = QualityLevel.Medium;
        public QualityLevel IconQuality { get; set; } = QualityLevel.Medium;
        public QualityLevel GroundTextureQuality { get; set; } = QualityLevel.Medium;
        public QualityLevel CharacterTextureQuality { get; set; } = QualityLevel.Medium;
        public QualityLevel WaterTextureQuality { get; set; } = QualityLevel.Medium;
        public QualityLevel SkyTextureQuality { get; set; } = QualityLevel.Medium;
        public QualityLevel ShadowQuality { get; set; } = QualityLevel.Medium;
        public QualityLevel ParticleQuality { get; set; } = QualityLevel.Medium;
        public QualityLevel PostProcessingQuality { get; set; } = QualityLevel.Medium;

        // Configuración global
        public bool VSyncEnabled { get; set; } = true;
        public int TargetFPS { get; set; } = 60;
        public float RenderScale { get; set; } = 1.0f;

        // Perfiles personalizados guardados
        public Dictionary<string, QualityProfile> CustomProfiles { get; set; } = new();

        public void ApplyPresetProfile(QualityProfileType profileType)
        {
            if (profileType == QualityProfileType.Custom) return;

            ProfileType = profileType;
            QualityLevel level = profileType switch
            {
                QualityProfileType.Ultra => QualityLevel.Ultra,
                QualityProfileType.High => QualityLevel.High,
                QualityProfileType.Medium => QualityLevel.Medium,
                QualityProfileType.Low => QualityLevel.Low,
                QualityProfileType.Toaster => QualityLevel.Toaster,
                _ => QualityLevel.Medium
            };

            ApplyLevelToComponents(level);
            ApplyPresetGlobalSettings(profileType);
        }

        private void ApplyLevelToComponents(QualityLevel level)
        {
            TreeQuality = level;
            VegetationQuality = level;
            TerrainQuality = level;
            PlayerModelQuality = level;
            BuildingModelQuality = level;
            ObjectModelQuality = level;
            DeployableQuality = level;
            IconQuality = level;
            GroundTextureQuality = level == QualityLevel.Toaster ? QualityLevel.Toaster : level;
            CharacterTextureQuality = level;
            WaterTextureQuality = level;
            SkyTextureQuality = level;
            ShadowQuality = level == QualityLevel.Toaster ? QualityLevel.Disabled : level;
            ParticleQuality = level == QualityLevel.Toaster ? QualityLevel.Disabled : level;
            PostProcessingQuality = level == QualityLevel.Toaster ? QualityLevel.Disabled : level;
        }

        private void ApplyPresetGlobalSettings(QualityProfileType profileType)
        {
            switch (profileType)
            {
                case QualityProfileType.Ultra:
                    VSyncEnabled = false;
                    TargetFPS = 0;
                    RenderScale = 1.0f;
                    break;
                case QualityProfileType.High:
                    VSyncEnabled = true;
                    TargetFPS = 60;
                    RenderScale = 0.9f;
                    break;
                case QualityProfileType.Medium:
                    VSyncEnabled = true;
                    TargetFPS = 60;
                    RenderScale = 0.8f;
                    break;
                case QualityProfileType.Low:
                    VSyncEnabled = true;
                    TargetFPS = 30;
                    RenderScale = 0.7f;
                    break;
                case QualityProfileType.Toaster:
                    VSyncEnabled = true;
                    TargetFPS = 20;
                    RenderScale = 0.5f;
                    break;
            }
        }

        public void Save()
        {
            try
            {
                string userDataPath = OS.GetUserDataDir();
                string path = System.IO.Path.Combine(userDataPath, "quality_settings.json");
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                
                System.IO.File.WriteAllText(path, json);
                Logger.LogInfo($"QualitySettings: Guardado exitosamente en {path} ({CustomProfiles.Count} perfiles personalizados)");
            }
            catch (Exception e)
            {
                Logger.LogError($"QualitySettings: Error al guardar: {e.Message}");
            }
        }

        public static QualitySettings Load()
        {
            try
            {
                string userDataPath = OS.GetUserDataDir();
                string path = System.IO.Path.Combine(userDataPath, "quality_settings.json");
                
                if (!System.IO.File.Exists(path))
                {
                    Logger.LogInfo("QualitySettings: No se encontró archivo de configuración, usando valores por defecto.");
                    return new QualitySettings();
                }

                string json = System.IO.File.ReadAllText(path);
                var settings = JsonSerializer.Deserialize<QualitySettings>(json);
                
                if (settings != null)
                {
                    Logger.LogInfo($"QualitySettings: Cargado exitosamente desde {path} ({settings.CustomProfiles?.Count ?? 0} perfiles personalizados)");
                    return settings;
                }
                return new QualitySettings();
            }
            catch (Exception e)
            {
                Logger.LogError($"QualitySettings: Error al cargar: {e.Message}");
                return new QualitySettings();
            }
        }
    }

    public class QualityProfile
    {
        public string Name { get; set; }
        public QualityLevel TreeQuality { get; set; }
        public QualityLevel VegetationQuality { get; set; }
        public QualityLevel TerrainQuality { get; set; }
        public QualityLevel PlayerModelQuality { get; set; }
        public QualityLevel BuildingModelQuality { get; set; }
        public QualityLevel ObjectModelQuality { get; set; }
        public QualityLevel DeployableQuality { get; set; }
        public QualityLevel IconQuality { get; set; }
        public QualityLevel GroundTextureQuality { get; set; }
        public QualityLevel CharacterTextureQuality { get; set; }
        public QualityLevel WaterTextureQuality { get; set; }
        public QualityLevel SkyTextureQuality { get; set; }
        public QualityLevel ShadowQuality { get; set; }
        public QualityLevel ParticleQuality { get; set; }
        public QualityLevel PostProcessingQuality { get; set; }
        public bool VSyncEnabled { get; set; }
        public int TargetFPS { get; set; }
        public float RenderScale { get; set; }
    }
}
