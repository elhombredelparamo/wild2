// -----------------------------------------------------------------------------
// Wild v2.0 - Escena Principal del Juego
// -----------------------------------------------------------------------------
// 
// RELACIONADO CON:
// - contexto/menus-y-escenas.md - Sistema de menús y escenas
// - codigo/core/juego.pseudo    - Integración con bucle principal
// - contexto/personajes.md      - Sistema de gestión de personajes
// 
// DESCRIPCIÓN:
// Escena principal del juego. Gestiona el TerrainManager, el menú de pausa
// y la transición de overlay de carga → render 3D.
// El overlay desaparece cuando TerrainManager emite ChunkInicialListo.
// 
// -----------------------------------------------------------------------------
using Godot;
using Wild.Core.Terrain;
using Wild.Core.Player;
using Wild.Data;
using Wild.Utils;
using Wild.Core.Quality;

namespace Wild.UI
{
    public partial class GameWorld : Node3D
    {
        private PauseMenu   _pauseMenu;
        private InventoryUI _inventoryUI;
        private DebugConsole _debugConsole;
        private bool        _isPaused = false;
        private ColorRect   _loadingOverlay;
        private Label       _labelObjective;
        private Camera3D    _camera;

        private TerrainManager _terrainManager;
        private PlayerManager  _playerManager;
        private JugadorController _jugador;
        private Label          _labelCoords;
        private FreeCam        _freeCam;

        public override void _Ready()
        {
            Logger.LogInfo("GameWorld._Ready() - Inicializando mundo de juego");
            var renderRes = GetWindow().ContentScaleSize;
            var windowRes = DisplayServer.WindowGetSize();
            Logger.LogInfo($"[SISTEMA: MONITOR] Resolución de Renderizado: {renderRes.X}x{renderRes.Y} | Ventana Física: {windowRes.X}x{windowRes.Y}");

            try
            {
                ProcessMode = ProcessModeEnum.Always;
                SetProcess(true);
                SetProcessInput(true);
                SetProcessUnhandledInput(true);

                // Referencias UI
                _loadingOverlay = GetNode<ColorRect>("UI/ColorRect");
                _labelObjective = GetNode<Label>("UI/LabelObjective");
                _camera         = GetNode<Camera3D>("Camera3D");

                SetupPauseMenu();
                SetupInventoryUI();
                SetupDebugConsole();
                SetupHUD();
                SetupEnvironment();
                SetupPlayer();
                SetupTerrainManager(); // Setup terrain last to ensure all signals are connected before Update()

                Logger.LogInfo("GameWorld._Ready() - Mundo de juego inicializado exitosamente");
            }
            catch (System.Exception e)
            {
                Logger.LogError($"GameWorld._Ready(): {e.Message}");
                throw;
            }
        }

        // ── Setup ─────────────────────────────────────────────────────────────

        private void SetupTerrainManager()
        {
            _terrainManager = new TerrainManager();
            AddChild(_terrainManager);

            // Ocultar overlay cuando el primer lote de chunks este listo
            _terrainManager.Connect(TerrainManager.SignalName.ChunkInicialListo, Callable.From(OnChunkInicialListo));
            _terrainManager.Connect(TerrainManager.SignalName.ChunkInicialListo, Callable.From(OnTerrainReady));

            // Arrancar la primera actualización con la posición del jugador (o cámara si no hay)
            Vector3 startPos = (_jugador != null) ? _jugador.GlobalPosition : _camera.GlobalPosition;
            _terrainManager.Update(startPos);
            Logger.LogInfo("GameWorld: TerrainManager iniciado.");
        }

        private void SetupEnvironment()
        {
            try
            {
                var worldEnvironment = new WorldEnvironment();
                var environment = new Godot.Environment();
                
                // Cielo procedural
                environment.BackgroundMode = Godot.Environment.BGMode.Sky;
                var sky = new Sky();
                sky.SkyMaterial = new ProceduralSkyMaterial();
                environment.Sky = sky;
                
                // Iluminación ambiente
                environment.AmbientLightSource = Godot.Environment.AmbientSource.Sky;
                environment.ReflectedLightSource = Godot.Environment.ReflectionSource.Sky;
                
                // Aplicar ajustes del sistema de calidad al entorno
                QualityManager.Instance.ApplyToEnvironment(environment);

                worldEnvironment.Environment = environment;
                AddChild(worldEnvironment);
                
                Logger.LogInfo("GameWorld: Entorno (Cielo/Luz) configurado.");
            }
            catch (System.Exception e)
            {
                Logger.LogError($"GameWorld.SetupEnvironment(): {e.Message}");
            }
        }

        private void OnChunkInicialListo()
        {
            if (_loadingOverlay != null) _loadingOverlay.Visible = false;
            if (_labelObjective != null) _labelObjective.Visible = false;
            Logger.LogInfo("GameWorld: Primer lote listo. Overlay ocultado, render 3D activo.");
        }

        private void SetupPauseMenu()
        {
            try
            {
                _pauseMenu = GD.Load<PackedScene>("res://scenes/ui/pause_menu.tscn").Instantiate<PauseMenu>();

                if (_pauseMenu != null)
                {
                    var uiLayer = GetNode<CanvasLayer>("UI");
                    if (uiLayer != null)
                        uiLayer.AddChild(_pauseMenu);
                    else
                        AddChild(_pauseMenu);

                    _pauseMenu.Hide();
                    
                    // Conectar señal de cierre del menú si existe (ej. botón Continuar)
                    _pauseMenu.Connect("ContinuarPresionado", Callable.From(TogglePause));
                    _pauseMenu.Connect("SalirPresionado", Callable.From(OnSalirAlMenu));
                }
            }
            catch (System.Exception e)
            {
                Logger.LogError($"GameWorld.SetupPauseMenu(): {e.Message}");
            }
        }

        private void SetupInventoryUI()
        {
            try
            {
                _inventoryUI = GD.Load<PackedScene>("res://scenes/ui/inventory_ui.tscn").Instantiate<InventoryUI>();

                if (_inventoryUI != null)
                {
                    var uiLayer = GetNode<CanvasLayer>("UI"); // Usar el mismo CanvasLayer
                    if (uiLayer != null)
                        uiLayer.AddChild(_inventoryUI);
                    else
                        AddChild(_inventoryUI);

                    _inventoryUI.Hide();
                    Logger.LogInfo("GameWorld: InventoryUI cargado y oculto.");

                    // Conectar señales para congelar jugador cuando se abra (ej: desde un cofre)
                    _inventoryUI.Connect(InventoryUI.SignalName.Opened, Callable.From(UpdateCharacterState));
                    _inventoryUI.Connect(InventoryUI.SignalName.Closed, Callable.From(UpdateCharacterState));
                }
            }
            catch (System.Exception e)
            {
                Logger.LogError($"GameWorld.SetupInventoryUI(): {e.Message}");
            }
        }

        private void SetupDebugConsole()
        {
            try
            {
                _debugConsole = GD.Load<PackedScene>("res://scenes/ui/debug_console.tscn").Instantiate<DebugConsole>();

                if (_debugConsole != null)
                {
                    var uiLayer = GetNode<CanvasLayer>("UI");
                    if (uiLayer != null)
                        uiLayer.AddChild(_debugConsole);
                    else
                        AddChild(_debugConsole);

                    _debugConsole.Hide();
                    Logger.LogInfo("GameWorld: DebugConsole cargado y oculto.");
                }
            }
            catch (System.Exception e)
            {
                Logger.LogError($"GameWorld.SetupDebugConsole(): {e.Message}");
            }
        }

        private void SetupPlayer()
        {
            try
            {
                _playerManager = new PlayerManager();
                AddChild(_playerManager);

                // Cargar el prefab del jugador
                _playerManager.FichaJugadorScene = GD.Load<PackedScene>("res://scenes/player/ficha_jugador.tscn");

                // Spamear al jugador usando el personaje actual
                string personajeId = PersonajeManager.Instance?.PersonajeActualId ?? "default";
                _playerManager.SpawnJugador(personajeId, new Vector3(0, 0.3f, 0)); // Spawn a 30cm del suelo (Y=0 aprox)

                // Obtener referencia al jugador recién creado
                _jugador = _playerManager.JugadorActual;
                if (_jugador != null)
                {
                    _jugador.PhysicsEnabled = false; // Desactivar física hasta que haya suelo
                }

                // Desactivar la cámara de debug de GameWorld si existe
                if (_camera != null)
                {
                    _camera.Current = false;
                    _camera.Visible = false;
                    // Opcionalmente quitar el script de cámara libre si molesta
                    if (_camera.HasMethod("set_process")) _camera.SetProcess(false);
                }

                Logger.LogInfo("GameWorld: Jugador inicializado y cámara de debug desactivada.");
            }
            catch (System.Exception e)
            {
                Logger.LogError($"GameWorld.SetupPlayer(): {e.Message}");
            }
        }

        private void SetupHUD()
        {
            try
            {
                var uiLayer = GetNode<CanvasLayer>("UI");
                if (uiLayer == null) return;

                _labelCoords = new Label();
                _labelCoords.Name = "LabelCoords";
                _labelCoords.Position = new Vector2(20, 20); // Top-left with padding
                
                // Estilo
                _labelCoords.AddThemeFontSizeOverride("font_size", 20);
                _labelCoords.AddThemeColorOverride("font_color", Colors.Yellow);
                _labelCoords.AddThemeColorOverride("font_outline_color", Colors.Black);
                _labelCoords.AddThemeConstantOverride("outline_size", 4);
                _labelCoords.Text = "Cargando terreno...";
                
                uiLayer.AddChild(_labelCoords);
                uiLayer.Visible = true;
                Logger.LogInfo("GameWorld: HUD de coordenadas inicializado.");
            }
            catch (System.Exception e)
            {
                Logger.LogError($"GameWorld.SetupHUD(): {e.Message}");
            }
        }

        private void OnTerrainReady()
        {
            Logger.LogInfo("GameWorld: Terreno inicial listo. Activando física del jugador.");
            if (_jugador != null)
            {
                _jugador.PhysicsEnabled = true;
            }

            // Forzar el spawn de triggers de setas (chunks ya cargados, jugador puede interactuar)
            Vector3 pos = (_jugador != null) ? _jugador.GlobalPosition : _camera.GlobalPosition;
            _terrainManager?.ForceCollisionUpdate(pos);
        }

        // ── Process ───────────────────────────────────────────────────────────

        public override void _Process(double delta)
        {
            // Intentar obtener referencia al jugador si no la tenemos
            if (_jugador == null && _playerManager != null)
            {
                _jugador = _playerManager.JugadorActual;
            }

            // Actualizar terreno en torno al jugador (o cámara si no hay jugador aún)
            Vector3 refPos = (_jugador != null) ? _jugador.GlobalPosition : _camera.GlobalPosition;
            _terrainManager?.Update(refPos);

            // Actualizar HUD de coordenadas
            if (_labelCoords != null && _jugador != null)
            {
                _labelCoords.Text = $"X: {refPos.X:F1} Y: {refPos.Y:F1} Z: {refPos.Z:F1}";
            }
        }

        // ── Input ─────────────────────────────────────────────────────────────

        public override void _Input(InputEvent @event)
        {
            try
            {
                // Prioridad 1: ESC (ui_cancel)
                if (@event.IsActionPressed("ui_cancel"))
                {
                    // 1.1 Consola de Debug
                    if (_debugConsole != null && _debugConsole.IsOpen())
                    {
                        Logger.LogInfo("GameWorld._Input: ESC → cerrando consola");
                        ToggleDebugConsole();
                        GetViewport().SetInputAsHandled();
                        return;
                    }

                    // 1.2 Inventario
                    if (_inventoryUI != null && _inventoryUI.IsOpen())
                    {
                        Logger.LogInfo("GameWorld._Input: ESC → cerrando inventario");
                        ToggleInventory();
                        GetViewport().SetInputAsHandled();
                        return;
                    }
                    
                    // 1.3 Menú de Pausa
                    if (_pauseMenu != null)
                    {
                        Logger.LogInfo("GameWorld._Input: ESC → alternando pausa");
                        TogglePause();
                        GetViewport().SetInputAsHandled();
                        return;
                    }
                }

                // Prioridad 2: F1 (debug_console_toggle)
                if (@event.IsActionPressed("debug_console_toggle"))
                {
                    if (_isPaused) return; // No abrir consola en menú de pausa por ahora
                    
                    Logger.LogInfo("GameWorld._Input: F1 → alternando consola");
                    ToggleDebugConsole();
                    GetViewport().SetInputAsHandled();
                }

                // Prioridad 3: TAB (inventory_toggle)
                if (@event.IsActionPressed("inventory_toggle"))
                {
                    if (_isPaused || (_debugConsole != null && _debugConsole.IsOpen())) return;

                    Logger.LogInfo("GameWorld._Input: TAB → alternando inventario");
                    ToggleInventory();
                    GetViewport().SetInputAsHandled();
                }

                if (@event is InputEventKey { Pressed: true, Keycode: Key.F12 })
                {
                    ToggleFreeCam();
                }
            }
            catch (System.Exception e)
            {
                Logger.LogError($"GameWorld._Input(): {e.Message}");
            }
        }

        private void ToggleFreeCam()
        {
            if (_freeCam == null)
            {
                // Activar Cámara Libre
                if (_jugador == null) return;

                Logger.LogInfo("GameWorld: Activando Cámara Libre (F12)");
                _freeCam = new FreeCam();
                AddChild(_freeCam);
                
                // Posicionarla donde está la cámara actual del jugador
                Camera3D playerCamera = _jugador.Camara;
                if (playerCamera != null)
                {
                    _freeCam.SetInitialTransform(playerCamera.GlobalTransform);
                }
                else
                {
                    _freeCam.GlobalPosition = _jugador.GlobalPosition + Vector3.Up * 1.7f;
                }

                _jugador.SetFrozen(true);
            }
            else
            {
                // Desactivar Cámara Libre
                Logger.LogInfo("GameWorld: Desactivando Cámara Libre (F12)");
                _jugador.SetFrozen(false);
                
                // Volver a la cámara del jugador
                if (_jugador.Camara != null)
                {
                    _jugador.Camara.Current = true;
                }

                _freeCam.QueueFree();
                _freeCam = null;
            }
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            // El manejo de ESC y TAB ahora es centralizado en _Input para control de prioridades
        }

        private void ToggleInventory()
        {
            if (_inventoryUI == null) return;

            bool newState = !_inventoryUI.IsOpen();
            
            if (newState)
            {
                _inventoryUI.Open();
                Input.MouseMode = Input.MouseModeEnum.Visible;
                _jugador?.SetFrozen(true);
            }
            else
            {
                _inventoryUI.Close();
                UpdateCharacterState();
            }
        }

        private void ToggleDebugConsole()
        {
            if (_debugConsole == null) return;

            bool newState = !_debugConsole.IsOpen();
            
            if (newState)
            {
                _debugConsole.Open();
                Input.MouseMode = Input.MouseModeEnum.Visible;
                _jugador?.SetFrozen(true);
            }
            else
            {
                _debugConsole.Close();
                UpdateCharacterState();
            }
        }

        private void UpdateCharacterState()
        {
            bool anyBlockingUIOpen = _isPaused || 
                                     (_inventoryUI != null && _inventoryUI.IsOpen()) || 
                                     (_debugConsole != null && _debugConsole.IsOpen());
            
            if (anyBlockingUIOpen)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                _jugador?.SetFrozen(true);
            }
            else
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
                _jugador?.SetFrozen(false);
            }
        }

        private void TogglePause()
        {
            _isPaused = !_isPaused;
            Logger.LogInfo($"GameWorld.TogglePause() → {_isPaused}");

            if (_pauseMenu == null) return;

            if (_isPaused)
            {
                _pauseMenu.Show();
                Input.MouseMode = Input.MouseModeEnum.Visible;
                _jugador?.SetFrozen(true);
            }
            else
            {
                _pauseMenu.Hide();
                UpdateCharacterState();
            }
        }

        private void OnSalirAlMenu()
        {
            Logger.LogInfo("GameWorld: Guardando estado final del jugador y saliendo al menú principal...");
            if (_playerManager != null)
            {
                _playerManager.GuardarEstadoJugador();
            }
            
            // Limpiar tareas asíncronas del terreno
            _terrainManager?.Cleanup();
            
            GetTree().ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
        }
    }
}
