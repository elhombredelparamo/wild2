using Godot;
using System.Text.Json;
using Wild.Utils;

namespace Wild.Core.Quality
{
    public partial class QualityManager : Node
    {
        public static QualityManager Instance { get; private set; }

        /// <summary>
        /// Se dispara cuando la calidad de vegetación cambia.
        /// TerrainManager se suscribe para forzar la recarga de vegetación.
        /// </summary>
        public static event System.Action OnVegetationQualityChanged;
        public static event System.Action OnTerrainQualityChanged;
        public static event System.Action OnDeployableQualityChanged;
        public static event System.Action OnIconQualityChanged;

        private QualityLevel _lastVegetationQuality = QualityLevel.Medium;
        private QualityLevel _lastTerrainQuality = QualityLevel.Medium;
        private QualityLevel _lastDeployableQuality = QualityLevel.Medium;
        private QualityLevel _lastIconQuality = QualityLevel.Medium;

        [Signal]
        public delegate void QualityChangedEventHandler(QualityProfileType newProfile);

        public QualitySettings Settings { get; private set; }
        public HardwareCapabilities Capabilities { get; private set; }

        public override void _Ready()
        {
            if (Instance == null)
            {
                Instance = this;
                ProcessMode = ProcessModeEnum.Always; // Seguir procesando incluso en pausa si hace falta
            }

            LoadAndApply();
        }

        public void LoadAndApply()
        {
            Settings = QualitySettings.Load();
            Capabilities = HardwareCapabilities.Detect();

            if (Settings.AutoDetect)
            {
                var recommended = Capabilities.GetRecommendedQuality();
                Logger.LogInfo($"QualityManager: Detección automática recomendó: {recommended}");
                
                // Si es la primera vez o forzado, aplicar recomendado
                // Por ahora, aplicamos lo que diga el recomendador si AutoDetect está ON
                // pero mapeando de QualityLevel a ProfileType
                var profile = recommended switch
                {
                    QualityLevel.Ultra => QualityProfileType.Ultra,
                    QualityLevel.High => QualityProfileType.High,
                    QualityLevel.Medium => QualityProfileType.Medium,
                    QualityLevel.Low => QualityProfileType.Low,
                    QualityLevel.Toaster => QualityProfileType.Toaster,
                    _ => QualityProfileType.Medium
                };
                Settings.ApplyPresetProfile(profile);
            }

            ApplyCurrentSettings();
        }

        public void AutoDetectAndApply()
        {
            Logger.LogInfo("QualityManager: Ejecutando auto-detección de hardware...");
            var caps = HardwareCapabilities.Detect();
            var recommendedLevel = caps.GetRecommendedQuality();
            
            // Convertir QualityLevel a QualityProfileType
            QualityProfileType recommendedProfile = recommendedLevel switch
            {
                QualityLevel.Ultra => QualityProfileType.Ultra,
                QualityLevel.High => QualityProfileType.High,
                QualityLevel.Medium => QualityProfileType.Medium,
                QualityLevel.Low => QualityProfileType.Low,
                _ => QualityProfileType.Toaster
            };

            Settings.ApplyPresetProfile(recommendedProfile);
            Settings.AutoDetect = false; // Desactivar auto-detect persistente tras una acción manual/reset
            ApplyCurrentSettings();
            Settings.Save();
        }

        public void ApplyCurrentSettings()
        {
            Logger.LogInfo($"QualityManager: Aplicando ajustes de calidad (Perfil: {Settings.ProfileType})");

            // 1. VSync
            DisplayServer.WindowSetVsyncMode(Settings.VSyncEnabled ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);

            // 2. FPS Limit
            Engine.MaxFps = Settings.TargetFPS;

            // 3. Render Scale (Global UI + 3D)
            GetWindow().ContentScaleFactor = Settings.RenderScale; 

            // 4. Viewport settings (MSAA, etc)
            ApplyToViewport(GetViewport());

            Settings.Save();

            // Notificar si la calidad de vegetación ha cambiado
            if (Settings.VegetationQuality != _lastVegetationQuality)
            {
                _lastVegetationQuality = Settings.VegetationQuality;
                OnVegetationQualityChanged?.Invoke();
            }

            if (Settings.TerrainQuality != _lastTerrainQuality)
            {
                _lastTerrainQuality = Settings.TerrainQuality;
                OnTerrainQualityChanged?.Invoke();
            }

            if (Settings.DeployableQuality != _lastDeployableQuality)
            {
                _lastDeployableQuality = Settings.DeployableQuality;
                OnDeployableQualityChanged?.Invoke();
            }

            if (Settings.IconQuality != _lastIconQuality)
            {
                _lastIconQuality = Settings.IconQuality;
                OnIconQualityChanged?.Invoke();
            }
        }

        public void ApplyToEnvironment(Godot.Environment env)
        {
            if (env == null) return;

            var q = Settings.PostProcessingQuality;

            // ── Tonemapping ──────────────────────────────────────────────────
            // Filmic da un look cinematográfico y evita colores sobreexpuestos.
            // En Toaster usamos Reinhard (más simple y rápido).
            if (q >= QualityLevel.Low)
            {
                env.TonemapMode = Godot.Environment.ToneMapper.Filmic;
                env.TonemapExposure = 1.0f;
                env.TonemapWhite = 1.0f;
            }
            else
            {
                // Toaster: linear simple, sin ajustes de contraste dinámico
                env.TonemapMode = Godot.Environment.ToneMapper.Linear;
            }

            // ── Glow (Bloom) ──────────────────────────────────────────────────
            env.GlowEnabled = q >= QualityLevel.Low;
            if (env.GlowEnabled)
            {
                env.GlowBloom = q >= QualityLevel.High ? 0.1f : 0.05f;
                env.GlowIntensity = q >= QualityLevel.Ultra ? 0.9f : 0.7f;
                env.GlowStrength = 1.0f;
                env.GlowHdrThreshold = 1.0f;
            }

            // ── SSAO (Oclusión Ambiental) ─────────────────────────────────────
            env.SsaoEnabled = q >= QualityLevel.Medium;
            if (env.SsaoEnabled)
            {
                env.SsaoRadius = q >= QualityLevel.Ultra ? 1.5f : 1.0f;
                env.SsaoIntensity = q >= QualityLevel.High ? 2.0f : 1.0f;
            }

            // ── SSIL (Iluminación Indirecta) ──────────────────────────────────
            env.SsilEnabled = q >= QualityLevel.High;

            // ── SSR (Reflejos en Pantalla) ────────────────────────────────────
            env.SsrEnabled = q >= QualityLevel.Ultra;
            if (env.SsrEnabled)
            {
                env.SsrMaxSteps = 64;
                env.SsrFadeIn = 0.15f;
                env.SsrFadeOut = 2.0f;
            }

            // ── SDFGI (Iluminación Global) ────────────────────────────────────
            // Solo disponible en Ultra; iluminación global basada en probes SDF.
            env.SdfgiEnabled = q >= QualityLevel.Ultra;

            // ── Niebla Volumétrica ────────────────────────────────────────────
            env.VolumetricFogEnabled = q >= QualityLevel.High;
            env.VolumetricFogDensity = 0.005f; // Densidad muy baja para no bloquear el cielo
            env.FogEnabled = false; // Deshabilitamos la niebla estándar para evitar que tape el skybox

            // ── Ajustes adicionales según sombras ─────────────────────────────
            if (Settings.ShadowQuality == QualityLevel.Disabled)
            {
                env.SsaoEnabled = false;
            }

            // Aplicar configuración de Skybox
            var sq = Settings.SkyTextureQuality;
            if (sq == QualityLevel.Disabled)
            {
                // Cielo desactivado -> Fondo de color
                env.BackgroundMode = Godot.Environment.BGMode.Color;
                env.BackgroundColor = new Color(0.5f, 0.7f, 0.9f); // Azul cielo
            }
            else
            {
                env.BackgroundMode = Godot.Environment.BGMode.Sky;
                if (env.Sky == null)
                {
                    env.Sky = new Sky();
                }
                
                if (!(env.Sky.SkyMaterial is PanoramaSkyMaterial))
                {
                    env.Sky.SkyMaterial = new PanoramaSkyMaterial();
                }
                
                string qualityFolder = "medium";
                switch(sq)
                {
                    case QualityLevel.Ultra: qualityFolder = "ultra"; break;
                    case QualityLevel.High: qualityFolder = "high"; break;
                    case QualityLevel.Medium: qualityFolder = "medium"; break;
                    case QualityLevel.Low: qualityFolder = "low"; break;
                    case QualityLevel.Toaster: qualityFolder = "toaster"; break;
                }

                string texturePath = $"res://assets/textures/skybox/{qualityFolder}/cielo.png";
                if (ResourceLoader.Exists(texturePath))
                {
                    var texture = ResourceLoader.Load<Texture2D>(texturePath);
                    if (env.Sky.SkyMaterial is PanoramaSkyMaterial panorama)
                    {
                        panorama.Panorama = texture;
                    }
                }
                else
                {
                    Logger.LogWarning($"QualityManager: Textura de skybox no encontrada en {texturePath}");
                }
            }
        }

        public void ApplyToViewport(Viewport vp)
        {
            if (vp == null) return;
            
            var q = Settings.PostProcessingQuality;
            
            // Antialiasing (MSAA 3D)
            vp.Msaa3D = q switch
            {
                QualityLevel.Ultra => Viewport.Msaa.Msaa4X,
                QualityLevel.High => Viewport.Msaa.Msaa2X,
                _ => Viewport.Msaa.Disabled
            };
            
            // Resolución de sombras (Global para el viewport / servidor)
            var s = Settings.ShadowQuality;
            
            int shadowSize = 2048; // Default Medium
            RenderingServer.ShadowQuality filter = RenderingServer.ShadowQuality.SoftLow;

            switch(s)
            {
                case QualityLevel.Ultra: 
                    shadowSize = 4096;
                    filter = RenderingServer.ShadowQuality.SoftUltra;
                    break;
                case QualityLevel.High:
                    shadowSize = 2048;
                    filter = RenderingServer.ShadowQuality.SoftHigh;
                    break;
                case QualityLevel.Medium:
                    shadowSize = 1024;
                    filter = RenderingServer.ShadowQuality.SoftLow;
                    break;
                case QualityLevel.Low:
                    shadowSize = 512;
                    filter = RenderingServer.ShadowQuality.Hard;
                    break;
                case QualityLevel.Toaster:
                    shadowSize = 256;
                    filter = RenderingServer.ShadowQuality.Hard;
                    break;
                case QualityLevel.Disabled:
                    shadowSize = 0;
                    filter = RenderingServer.ShadowQuality.Hard;
                    // Al establecer el tamaño del atlas de sombras en 0, no se renderizan sombras,
                    // lo que es ideal para la opción "Disabled".
                    break;
            }

            // Aplicar configuración de filtro global
            RenderingServer.DirectionalShadowAtlasSetSize(shadowSize, true);
            RenderingServer.DirectionalSoftShadowFilterSetQuality(filter);
            
            // Asignar atlas posicional al viewport actual
            vp.PositionalShadowAtlasSize = shadowSize;
        }

        public void SetProfile(QualityProfileType profile)
        {
            Settings.ApplyPresetProfile(profile);
            Settings.AutoDetect = false;
            ApplyCurrentSettings();
            EmitSignal(SignalName.QualityChanged, (int)profile);
        }

        public void RestartGame()
        {
            Logger.LogInfo("QualityManager: Reiniciando escena para aplicar cambios de recursos.");
            GetTree().ReloadCurrentScene();
        }
    }
}
