// -----------------------------------------------------------------------------
// Wild v2.0 - Sistema de Configuración del Juego
// -----------------------------------------------------------------------------
// 
// RELACIONADO CON:
// - contexto/calidad.md - Adaptación a niveles de calidad
// - codigo/core/ui.pseudo - Diseño de interfaz de usuario
// - codigo/utils/config.pseudo - Configuración del sistema
// 
// DESCRIPCIÓN:
// Menú de opciones con configuración gráfica, audio y controles.
// Gestiona niveles de calidad, distancia de render y volúmenes de audio.
// 
// -----------------------------------------------------------------------------
using Godot;
using Wild.Core.Quality;

namespace Wild.UI
{
    public partial class OptionsMenu : Control
    {
        // Botones de navegación
        private Button _buttonControls;
        private Button _buttonGraphics;
        private Button _buttonAudio;
        private Button _buttonQuality; // Nuevo
        private Button _buttonBack;

        // Paneles de contenido
        private Control _panelControls;
        private Control _panelGraphics;
        private Control _panelAudio;
        private Control _panelQuality; // Nuevo

        // Controles gráficos
        private OptionButton _optionResolution;
        private HSlider      _hSliderRenderDistance;
        private Label        _labelRenderValue;
        private Button       _buttonSaveGraphics;

        // Controles de audio
        private HSlider _hSliderMasterVolume;
        private Label _labelMasterValue;
        private HSlider _hSliderMusicVolume;
        private Label _labelMusicValue;
        private Button _buttonSaveAudio;

        // Índices de calidad
        private enum QualityLevel { Toaster, Low, Medium, High, Ultra }

        public override void _Ready()
        {
            LogUI("OptionsMenu._Ready() - Inicializando menú de opciones");
            
            try
            {
                // Asegurar que el menú sea visible al cargar
                Visible = true;
                
                SetupNavigationButtons();
                SetupContentPanels();
                SetupGraphicsControls();
                SetupAudioControls();
                ConnectEvents();
                
                // Mostrar controles por defecto
                ShowSubmenu(0);
                
                LogUI("OptionsMenu._Ready() - Menú de opciones inicializado exitosamente");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("OptionsMenu", $"Error en _Ready(): {e.Message}");
                throw;
            }
        }

        private void SetupNavigationButtons()
        {
            try
            {
                _buttonControls = GetNode<Button>("HBox/PanelLeft/VBox/ButtonControls");
                _buttonGraphics = GetNode<Button>("HBox/PanelLeft/VBox/ButtonGraphics");
                _buttonAudio = GetNode<Button>("HBox/PanelLeft/VBox/ButtonAudio");
                _buttonQuality = GetNode<Button>("HBox/PanelLeft/VBox/ButtonQuality");
                _buttonBack = GetNode<Button>("HBox/PanelLeft/VBox/ButtonBack");

                LogUI("OptionsMenu.SetupNavigationButtons() - Botones de navegación configurados");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("OptionsMenu", $"Error en SetupNavigationButtons(): {e.Message}");
            }
        }

        private void SetupContentPanels()
        {
            try
            {
                _panelControls = GetNode<Control>("HBox/PanelContent/Margin/ContentStack/PanelControls");
                _panelGraphics = GetNode<Control>("HBox/PanelContent/Margin/ContentStack/PanelGraphics");
                _panelAudio = GetNode<Control>("HBox/PanelContent/Margin/ContentStack/PanelAudio");
                _panelQuality = GetNode<Control>("HBox/PanelContent/Margin/ContentStack/PanelQuality");

                LogUI("OptionsMenu.SetupContentPanels() - Paneles de contenido configurados");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("OptionsMenu", $"Error en SetupContentPanels(): {e.Message}");
            }
        }

        private void SetupGraphicsControls()
        {
            try
            {
                _optionResolution = GetNode<OptionButton>("HBox/PanelContent/Margin/ContentStack/PanelGraphics/VBox/ResolutionRow/OptionResolution");
                _hSliderRenderDistance = GetNode<HSlider>("HBox/PanelContent/Margin/ContentStack/PanelGraphics/VBox/RenderRow/HSliderRenderDistance");
                _labelRenderValue = GetNode<Label>("HBox/PanelContent/Margin/ContentStack/PanelGraphics/VBox/RenderRow/LabelRenderValue");
                _buttonSaveGraphics = GetNode<Button>("HBox/PanelContent/Margin/ContentStack/PanelGraphics/VBox/ButtonSaveGraphics");

                // Configurar opciones de resolución
                PopulateResolutions();

                _hSliderRenderDistance.Value = Wild.Data.SessionData.Instance.RenderDistance;
                UpdateRenderDistanceLabel();

                LogUI("OptionsMenu.SetupGraphicsControls() - Controles gráficos configurados");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("OptionsMenu", $"Error en SetupGraphicsControls(): {e.Message}");
            }
        }

        private void SetupAudioControls()
        {
            try
            {
                _hSliderMasterVolume = GetNode<HSlider>("HBox/PanelContent/Margin/ContentStack/PanelAudio/VBox/MasterRow/HSliderMasterVolume");
                _labelMasterValue = GetNode<Label>("HBox/PanelContent/Margin/ContentStack/PanelAudio/VBox/MasterRow/LabelMasterValue");
                _hSliderMusicVolume = GetNode<HSlider>("HBox/PanelContent/Margin/ContentStack/PanelAudio/VBox/MusicRow/HSliderMusicVolume");
                _labelMusicValue = GetNode<Label>("HBox/PanelContent/Margin/ContentStack/PanelAudio/VBox/MusicRow/LabelMusicValue");
                _buttonSaveAudio = GetNode<Button>("HBox/PanelContent/Margin/ContentStack/PanelAudio/VBox/ButtonSaveAudio");

                // Valores iniciales desde QualityManager y SessionData
                _hSliderMasterVolume.Value = Wild.Data.SessionData.Instance.MasterVolume;
                _hSliderMusicVolume.Value = Wild.Data.SessionData.Instance.MusicVolume;
                
                // Aplicar settings de calidad iniciales si existen
                var qSettings = QualityManager.Instance.Settings;
                // VSync y FPS se manejan globalmente por el manager
                UpdateMasterVolumeLabel();
                UpdateMusicVolumeLabel();

                LogUI("OptionsMenu.SetupAudioControls() - Controles de audio configurados");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("OptionsMenu", $"Error en SetupAudioControls(): {e.Message}");
            }
        }

        private void ConnectEvents()
        {
            try
            {
                // Navegación
                if (_buttonControls != null)
                    _buttonControls.Pressed += () => ShowSubmenu(0);
                
                if (_buttonGraphics != null)
                    _buttonGraphics.Pressed += () => ShowSubmenu(1);
                
                if (_buttonAudio != null)
                    _buttonAudio.Pressed += () => ShowSubmenu(2);
                
                if (_buttonQuality != null)
                    _buttonQuality.Pressed += () => ShowSubmenu(3);
                
                if (_buttonBack != null)
                    _buttonBack.Pressed += OnBackPressed;

                // Gráficos
                if (_hSliderRenderDistance != null)
                    _hSliderRenderDistance.ValueChanged += OnRenderDistanceChanged;
                
                if (_buttonSaveGraphics != null)
                    _buttonSaveGraphics.Pressed += OnSaveGraphicsPressed;

                // Audio
                if (_hSliderMasterVolume != null)
                    _hSliderMasterVolume.ValueChanged += OnMasterVolumeChanged;
                
                if (_hSliderMusicVolume != null)
                    _hSliderMusicVolume.ValueChanged += OnMusicVolumeChanged;
                
                if (_buttonSaveAudio != null)
                    _buttonSaveAudio.Pressed += OnSaveAudioPressed;

                LogUI("OptionsMenu.ConnectEvents() - Eventos conectados");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("OptionsMenu", $"Error en ConnectEvents(): {e.Message}");
            }
        }

        private void ShowSubmenu(int index)
        {
            try
            {
                // Ocultar todos los paneles
                _panelControls.Visible = index == 0;
                _panelGraphics.Visible = index == 1;
                _panelAudio.Visible = index == 2;
                _panelQuality.Visible = index == 3;

                LogUI($"OptionsMenu.ShowSubmenu() - Mostrando submenu {index}");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("OptionsMenu", $"Error en ShowSubmenu(): {e.Message}");
            }
        }

        private void OnRenderDistanceChanged(double value)
        {
            try
            {
                UpdateRenderDistanceLabel();
                // Solo feedback visual, no guardamos hasta pulsar el botón
            }
            catch (System.Exception e)
            {
                LogErrorSistema("OptionsMenu", $"Error en OnRenderDistanceChanged(): {e.Message}");
            }
        }

        private void OnMasterVolumeChanged(double value)
        {
            try
            {
                UpdateMasterVolumeLabel();
            }
            catch (System.Exception e)
            {
                LogErrorSistema("OptionsMenu", $"Error en OnMasterVolumeChanged(): {e.Message}");
            }
        }

        private void OnMusicVolumeChanged(double value)
        {
            try
            {
                UpdateMusicVolumeLabel();
            }
            catch (System.Exception e)
            {
                LogErrorSistema("OptionsMenu", $"Error en OnMusicVolumeChanged(): {e.Message}");
            }
        }

        private void OnSaveGraphicsPressed()
        {
            try
            {
                // Guardar resolución seleccionada
                if (_optionResolution.Selected != -1)
                {
                    var resText = _optionResolution.GetItemText(_optionResolution.Selected);
                    var parts = resText.Split('x');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1].Split(' ')[0], out int h))
                    {
                        Wild.Data.SessionData.Instance.ResolutionWidth = w;
                        Wild.Data.SessionData.Instance.ResolutionHeight = h;
                        
                        // Implementar escalado proporcional para modo "Responsive"
                        // 1. La resolución de renderizado (ContentScaleSize) será la que el usuario elija
                        // 2. Ajustamos el factor de escala para que la UI diseñada para 720p quepa perfectamente
                        float scaleFactor = (float)h / 720.0f;
                        
                        GetWindow().ContentScaleSize = new Vector2I(w, h);
                        GetWindow().ContentScaleFactor = scaleFactor;
                        GetWindow().ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
                        GetWindow().ContentScaleAspect = Window.ContentScaleAspectEnum.Keep;
                        
                        // Mantener la ventana física en modo Fullscreen (que usa la nativa del monitor)
                        var screenSize = DisplayServer.ScreenGetSize();
                        var mode = (w == screenSize.X && h == screenSize.Y) 
                            ? DisplayServer.WindowMode.Fullscreen 
                            : DisplayServer.WindowMode.ExclusiveFullscreen;
                        
                        DisplayServer.WindowSetMode(mode);
                        DisplayServer.WindowSetSize(new Vector2I((int)screenSize.X, (int)screenSize.Y)); // Asegurar ventana nativa
                        
                        LogUI($"[REGLA: RESOLUCION] Aplicando resolución interna: {w}x{h} (Escala UI: {scaleFactor:F2})");
                        
                        // Verificar estados reales
                        var renderRes = GetWindow().ContentScaleSize;
                        var windowRes = DisplayServer.WindowGetSize();
                        LogUI($"[MOTOR: GODOT] Renderizado REAL: {renderRes.X}x{renderRes.Y} | Ventana Física: {windowRes.X}x{windowRes.Y}");
                    }
                }

                // Guardar configuración gráfica en la sesión y manager
                Wild.Data.SessionData.Instance.RenderDistance = (int)_hSliderRenderDistance.Value;
                Wild.Data.SessionData.Instance.SaveConfig();
                
                // Sincronizar con QualityManager
                var qSettings = QualityManager.Instance.Settings;
                // Nota: RenderDistance se mantiene en SessionData por petición del usuario
                QualityManager.Instance.ApplyCurrentSettings();
                
                LogUI($"OptionsMenu.OnSaveGraphicsPressed() - Guardado completado.");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("OptionsMenu", $"Error en OnSaveGraphicsPressed(): {e.Message}");
            }
        }

        private void OnSaveAudioPressed()
        {
            try
            {
                Wild.Data.SessionData.Instance.MasterVolume = (float)_hSliderMasterVolume.Value;
                Wild.Data.SessionData.Instance.MusicVolume = (float)_hSliderMusicVolume.Value;
                Wild.Data.SessionData.Instance.SaveConfig();
                
                LogUI("OptionsMenu.OnSaveAudioPressed() - Configuración de audio guardada");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("OptionsMenu", $"Error en OnSaveAudioPressed(): {e.Message}");
            }
        }

        private void OnBackPressed()
        {
            try
            {
                LogUI("OptionsMenu.OnBackPressed() - Volviendo al menú principal");
                var result = GetTree().ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
                LogUI($"OptionsMenu: ChangeSceneToFile resultado: {result}");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("OptionsMenu", $"Error en OnBackPressed(): {e.Message}");
            }
        }

        private void PopulateResolutions()
        {
            _optionResolution.Clear();
            
            Vector2I screenSize = DisplayServer.ScreenGetSize();
            var standardResolutions = new System.Collections.Generic.List<Vector2I>
            {
                new Vector2I(640, 360),
                new Vector2I(854, 480),
                new Vector2I(1024, 576),
                new Vector2I(1280, 720),
                new Vector2I(1366, 768),
                new Vector2I(1600, 900),
                new Vector2I(1920, 1080),
                new Vector2I(2560, 1440),
                new Vector2I(3840, 2160)
            };

            // Filtrar y añadir las que caben
            var addedResolutions = new System.Collections.Generic.HashSet<string>();
            foreach (var res in standardResolutions)
            {
                if (res.X <= screenSize.X && res.Y <= screenSize.Y)
                {
                    string label = $"{res.X}x{res.Y}";
                    if (res == screenSize) label += " (Nativo)";
                    _optionResolution.AddItem(label);
                    addedResolutions.Add($"{res.X}x{res.Y}");
                }
            }

            // Asegurar que la nativa esté si no es estándar
            string nativeStr = $"{screenSize.X}x{screenSize.Y}";
            if (!addedResolutions.Contains(nativeStr))
            {
                _optionResolution.AddItem(nativeStr + " (Nativo)");
            }

            // Seleccionar la actual de SessionData o la nativa por defecto
            int currentW = Wild.Data.SessionData.Instance.ResolutionWidth;
            int currentH = Wild.Data.SessionData.Instance.ResolutionHeight;
            
            if (currentW > 0 && currentH > 0)
            {
                string currentStr = $"{currentW}x{currentH}";
                for (int i = 0; i < _optionResolution.ItemCount; i++)
                {
                    if (_optionResolution.GetItemText(i).StartsWith(currentStr))
                    {
                        _optionResolution.Selected = i;
                        break;
                    }
                }
            }
            
            // Si no se encontró o no estaba definida, elegir la última (que suele ser la más alta/nativa)
            if (_optionResolution.Selected == -1)
            {
                _optionResolution.Selected = _optionResolution.ItemCount - 1;
            }
        }

        private void UpdateRenderDistanceLabel()
        {
            try
            {
                if (_labelRenderValue != null && _hSliderRenderDistance != null)
                {
                    _labelRenderValue.Text = $"{(int)_hSliderRenderDistance.Value} m";
                }
            }
            catch (System.Exception e)
            {
                LogErrorSistema("OptionsMenu", $"Error en UpdateRenderDistanceLabel(): {e.Message}");
            }
        }

        private void UpdateMasterVolumeLabel()
        {
            try
            {
                if (_labelMasterValue != null && _hSliderMasterVolume != null)
                {
                    _labelMasterValue.Text = $"{(int)(_hSliderMasterVolume.Value * 100)}%";
                }
            }
            catch (System.Exception e)
            {
                LogErrorSistema("OptionsMenu", $"Error en UpdateMasterVolumeLabel(): {e.Message}");
            }
        }

        private void UpdateMusicVolumeLabel()
        {
            try
            {
                if (_labelMusicValue != null && _hSliderMusicVolume != null)
                {
                    _labelMusicValue.Text = $"{(int)(_hSliderMusicVolume.Value * 100)}%";
                }
            }
            catch (System.Exception e)
            {
                LogErrorSistema("OptionsMenu", $"Error en UpdateMusicVolumeLabel(): {e.Message}");
            }
        }

        // Funciones de logging
        private void LogUI(string mensaje)
        {
            Wild.Utils.Logger.LogInfo($"[UI][OptionsMenu] {mensaje}");
        }

        private void LogErrorSistema(string sistema, string mensaje)
        {
            Wild.Utils.Logger.LogError($"[{sistema}] {mensaje}");
        }
    }
}
