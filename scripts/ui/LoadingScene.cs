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
using System;
using System.Threading.Tasks;
using Wild.Core;
using Wild.Core.Terrain;
using Wild.Utils;
using Wild.Data;

namespace Wild.UI
{
    public partial class LoadingScene : Control
    {
        private Label _titleLabel;
        private Label _statusLabel;
        private ProgressBar _progressBar;
        private int _totalSteps = 7;
        private int _currentStep = 0;

        private readonly string[] _resourcesToLoad = {
            "res://scenes/player/ficha_jugador.tscn",
            "res://assets/models/character/animada_con_tree.tscn",
            "res://assets/models/character/animada_hombre_tree.tscn",
            "res://scenes/ui/inventory_ui.tscn",
            "res://scenes/ui/pause_menu.tscn"
        };
        
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
                
                // Paso 1: Iniciando
                await UpdateProgress(1, "Iniciando...");
                await Task.Delay(1000);
                
                // Paso 2: Perfil
                await UpdateProgress(2, "Perfil...");
                
                // Comprobaciones de seguridad para Singletons
                bool systemReady = true;
                if (PersonajeManager.Instance == null) { LogUI("LoadingScene: CRÍTICO - PersonajeManager no encontrado"); systemReady = false; }
                if (MundoManager.Instance == null) { LogUI("LoadingScene: CRÍTICO - MundoManager no encontrado"); systemReady = false; }
                if (GameLoader.Instance == null) { LogUI("LoadingScene: CRÍTICO - GameLoader no encontrado"); systemReady = false; }

                if (!systemReady)
                {
                    throw new Exception("Sistemas críticos (Autoloads) no inicializados. Revisa la configuración del proyecto.");
                }

                string personajeId = null;
                try { personajeId = PersonajeManager.Instance?.PersonajeActualId; } catch { }

                if (!string.IsNullOrEmpty(personajeId))
                {
                    LogUI($"LoadingScene: Intentando precargar datos para {personajeId}");
                    if (MundoManager.Instance != null && GameLoader.Instance != null)
                    {
                        var data = MundoManager.Instance.CargarDatosJugador(personajeId);
                        GameLoader.Instance.CachedPlayerData = data;
                        LogUI($"LoadingScene: Datos precargados correctamente para {personajeId}");
                    }
                    else
                    {
                        LogUI("LoadingScene: Saltando precarga de datos por falta de managers.");
                    }
                }
                
                await Task.Delay(200);
                
                // Paso 3: Mundo
                await UpdateProgress(3, "Mundo...");
                
                // Instanciamos el terreno aquí para que empiece a trabajar
                var terrain = new TerrainManager();
                AddChild(terrain);
                
                // Iniciamos la generación en el origen (0,0,0) o donde el manager diga
                terrain.Update(Vector3.Zero);
                
                // Esperamos a que el primer lote esté listo
                LogUI("LoadingScene: Esperando señal ChunkInicialListo...");
                await ToSignal(terrain, TerrainManager.SignalName.ChunkInicialListo);
                
                // Paso 4: Detalles
                await UpdateProgress(4, "Detalles...");
                if (terrain != null && GameLoader.Instance != null)
                {
                    Vector3 spawnPos = GameLoader.Instance.CachedPlayerData?.GetPosition() ?? Vector3.Zero;
                    await terrain.ForceCollisionUpdateAsync(spawnPos);
                }
                
                // Pasamos el terreno al GameLoader para que no se destruya al cambiar de escena
                if (GameLoader.Instance != null && terrain != null)
                {
                    GameLoader.Instance.SetPreGeneratedTerrain(terrain);
                }
                
                // Paso 5: Recursos
                await UpdateProgress(5, "Recursos...");
                await LoadAllResourcesAsync();
                
                // Paso 6: Shaders
                await UpdateProgress(6, "Shaders...");
                await PrewarmShadersAsync();
                
                // Paso 7: Listo
                await UpdateProgress(7, "Listo");
                await Task.Delay(200);
                
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
        
        private async Task PrewarmShadersAsync()
        {
            try
            {
                // Creamos un SubViewport invisible para renderizar materiales sin que el usuario los vea
                var viewport = new SubViewport();
                viewport.Name = "ShaderPrewarmer";
                viewport.Size = new Vector2I(128, 128); // Pequeño pero suficiente
                viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
                AddChild(viewport);

                var cam = new Camera3D();
                cam.Position = new Vector3(0, 0, 5);
                viewport.AddChild(cam);

                // Materiales a pre-calentar
                string[] matPaths = {
                    "res://assets/models/human/fem.tres",
                    "res://assets/models/human/male.tres",
                    "res://assets/models/human/eyeball.tres",
                    "res://assets/models/human/jaw.tres"
                };

                foreach (string path in matPaths)
                {
                    var mat = GD.Load<Material>(path);
                    if (mat == null) continue;

                    var mesh = new MeshInstance3D();
                    mesh.Mesh = new SphereMesh(); // Forma básica
                    mesh.MaterialOverride = mat;
                    mesh.Position = Vector3.Zero;
                    viewport.AddChild(mesh);
                    
                    LogUI($"LoadingScene: Pre-calentando shader de {path}");
                }

                // El material del terreno es especial (ShaderMaterial)
                var terrainShader = GD.Load<Shader>("res://shaders/terrain.gdshader");
                if (terrainShader != null)
                {
                    var mat = new ShaderMaterial();
                    mat.Shader = terrainShader;
                    var mesh = new MeshInstance3D();
                    mesh.Mesh = new SphereMesh();
                    mesh.MaterialOverride = mat;
                    mesh.Position = Vector3.Zero;
                    viewport.AddChild(mesh);
                    LogUI("LoadingScene: Pre-calentando shader de terreno");
                }

                // Esperamos un par de frames para que la GPU procese y compile
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

                // Limpieza
                viewport.QueueFree();
            }
            catch (System.Exception e)
            {
                Wild.Utils.Logger.LogWarning($"LoadingScene: Error en PrewarmShadersAsync: {e.Message}");
            }
        }

        private async Task LoadAllResourcesAsync()
        {
            foreach (string path in _resourcesToLoad)
            {
                if (GameLoader.Instance.HasResource(path)) continue;

                LogUI($"LoadingScene: Solicitando carga de {path}");
                ResourceLoader.LoadThreadedRequest(path);
                
                // Esperar a que este recurso específico termine
                while (true)
                {
                    var status = ResourceLoader.LoadThreadedGetStatus(path);
                    if (status == ResourceLoader.ThreadLoadStatus.Loaded)
                    {
                        var res = ResourceLoader.LoadThreadedGet(path);
                        GameLoader.Instance.AddResource(path, res);
                        break;
                    }
                    else if (status == ResourceLoader.ThreadLoadStatus.Failed || status == ResourceLoader.ThreadLoadStatus.InvalidResource)
                    {
                        Wild.Utils.Logger.LogWarning($"LoadingScene: Error cargando {path}");
                        break;
                    }
                    await Task.Delay(50); // Mantenemos el hilo principal libre
                }
            }
        }

        // Funciones de logging
        private void LogUI(string mensaje)
        {
            Wild.Utils.Logger.LogInfo(mensaje);
            // Ya NO sobrescribimos _statusLabel.Text aquí para evitar borrar 
            // los mensajes concisos de la carga (ej: "Mundo...", "Shaders...")
        }
        
        private void LogErrorSistema(string sistema, string mensaje)
        {
            Wild.Utils.Logger.LogError($"[{sistema}] {mensaje}");
            if (_statusLabel != null) _statusLabel.Text = $"ERROR: {mensaje}";
        }
    }
}
