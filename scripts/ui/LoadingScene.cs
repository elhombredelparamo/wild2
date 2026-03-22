// -----------------------------------------------------------------------------
// Wild v2.0 - Escena de Carga Previa a la Escena de Juego
// -----------------------------------------------------------------------------
// 
// RELACIONADO CON:
// - contexto/menus-y-escenas.md - Sistema de menús y escenas
// - codigo/core/juego.pseudo - Integración con bucle principal
// - contexto/calidad.md - Adaptación a niveles de calidad
// 
// DESCRIPCIÓN:
// Escena de carga con barra de progreso, estados visuales y navegación
// automática al juego. Soporta carga asíncrona de sistemas y mundos.
// 
// -----------------------------------------------------------------------------
using Godot;
using System.Threading.Tasks;

namespace Wild.UI
{
    public partial class LoadingScene : Control
    {
        private Label _titleLabel;
        private Label _statusLabel;
        private ProgressBar _progressBar;
        private int _totalSteps = 5;
        private int _currentStep = 0;
        
        public override void _Ready()
        {
            LogUI("LoadingScene._Ready() - Inicializando escena de carga");
            
            try
            {
                // Asegurar que la escena sea visible
                Visible = true;
                
                CreateUI();
                _ = StartLoadingProcess();
                
                LogUI("LoadingScene._Ready() - Escena de carga inicializada exitosamente");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("LoadingScene", $"Error en _Ready(): {e.Message}");
                throw;
            }
        }
        
        private void CreateUI()
        {
            try
            {
                // Fondo oscuro
                var backgroundRect = new ColorRect();
                backgroundRect.Name = "Background";
                backgroundRect.LayoutMode = 1;
                backgroundRect.AnchorsPreset = 15; // Full Rect
                backgroundRect.Color = new Color(0.05f, 0.05f, 0.1f, 1.0f);
                AddChild(backgroundRect);
                
                // Contenedor principal centrado
                var centerContainer = new CenterContainer();
                centerContainer.Name = "CenterContainer";
                centerContainer.LayoutMode = 1;
                centerContainer.AnchorsPreset = 15; // Full Rect
                AddChild(centerContainer);
                
                // Contenedor vertical para el contenido
                var contentContainer = new VBoxContainer();
                contentContainer.Name = "ContentContainer";
                contentContainer.AddThemeConstantOverride("separation", 20);
                centerContainer.AddChild(contentContainer);
                
                // Título
                _titleLabel = new Label();
                _titleLabel.Name = "TitleLabel";
                _titleLabel.Text = "Wild";
                _titleLabel.AddThemeFontSizeOverride("font_size", 64);
                _titleLabel.AddThemeColorOverride("font_color", Colors.White);
                _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
                contentContainer.AddChild(_titleLabel);
                
                // Separador
                var separator = new Control();
                separator.CustomMinimumSize = new Vector2(0, 50);
                contentContainer.AddChild(separator);
                
                // Texto de estado
                _statusLabel = new Label();
                _statusLabel.Name = "StatusLabel";
                _statusLabel.Text = "Iniciando carga...";
                _statusLabel.AddThemeFontSizeOverride("font_size", 24);
                _statusLabel.AddThemeColorOverride("font_color", Colors.LightGray);
                _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
                contentContainer.AddChild(_statusLabel);
                
                // Barra de progreso
                _progressBar = new ProgressBar();
                _progressBar.Name = "ProgressBar";
                _progressBar.MaxValue = _totalSteps;
                _progressBar.Value = 0;
                _progressBar.CustomMinimumSize = new Vector2(400, 30);
                contentContainer.AddChild(_progressBar);
                
                LogUI("LoadingScene.CreateUI() - Interfaz creada exitosamente");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("LoadingScene", $"Error en CreateUI(): {e.Message}");
            }
        }
        
        private async Task StartLoadingProcess()
        {
            try
            {
                LogUI("LoadingScene.StartLoadingProcess() - Iniciando carga simulada de 5 segundos");
                
                // Paso 1: Inicializando sistemas
                await UpdateProgress(1, "Inicializando sistemas...");
                await Task.Delay(1000);
                
                // Paso 2: Cargando mundo
                await UpdateProgress(2, "Cargando mundo...");
                await Task.Delay(1000);
                
                // Paso 3: Generando terreno
                await UpdateProgress(3, "Generando terreno...");
                await Task.Delay(1000);
                
                // Paso 4: Preparando jugador
                await UpdateProgress(4, "Preparando jugador...");
                await Task.Delay(1000);
                
                // Paso 5: Finalizando
                await UpdateProgress(5, "Finalizando...");
                await Task.Delay(1000);
                
                // Carga completada - navegar a la escena del juego
                LogUI("LoadingScene.StartLoadingProcess() - Carga completada, navegando a game_world.tscn");
                var result = GetTree().ChangeSceneToFile("res://scenes/ui/game_world.tscn");
                LogUI($"LoadingScene: ChangeSceneToFile resultado: {result}");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("LoadingScene", $"Error en StartLoadingProcess(): {e.Message}");
                
                // Mostrar error
                _statusLabel.Text = $"Error en la carga: {e.Message}";
                _statusLabel.AddThemeColorOverride("font_color", Colors.Red);
            }
        }
        
        private async Task UpdateProgress(int step, string message)
        {
            try
            {
                _currentStep = step;
                _statusLabel.Text = message;
                _progressBar.Value = step;
                
                LogUI($"LoadingScene.UpdateProgress() - Paso {step}/{_totalSteps}: {message}");
                
                // Pequeña pausa para efecto visual
                await Task.Delay(100);
            }
            catch (System.Exception e)
            {
                LogErrorSistema("LoadingScene", $"Error en UpdateProgress(): {e.Message}");
            }
        }
        
        // Funciones de logging
        private void LogUI(string mensaje)
        {
            // TODO: Implementar cuando tengamos el sistema de logging
            GD.Print($"[UI][LoadingScene] {mensaje}");
        }
        
        private void LogErrorSistema(string sistema, string mensaje)
        {
            // TODO: Implementar cuando tengamos el sistema de logging
            GD.PrintErr($"[ERROR][{sistema}] {mensaje}");
        }
    }
}
