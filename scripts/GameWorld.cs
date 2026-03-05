using Godot;
using Wild.Network;
using Wild.Scripts.Terrain;
using Wild.Scripts.Player;
using Wild.Scripts.UI;
using Wild.Systems;
using FileAccess = Godot.FileAccess;

namespace Wild;

/// <summary>
/// Escena de partida (mundo) con movimiento básico y UI de coordenadas
/// </summary>
public partial class GameWorld : Node3D
{
    private CharacterBody3D _player;
    private Camera3D _camera;
    // Sistema de HUD
    private GameHUD _gameHUD;
    private bool _isFrozen = false;
    private bool _isCameraLocked = false;
    
    // Sistema de terreno
    private TerrainManager _terrainManager;
    
    // Gestor de red
    private NetworkManager _networkManager;
    
    // Controlador del jugador
    private PlayerController _playerController;
    
    // Sistema de persistencia del jugador
    private PlayerPersistence _playerPersistence;
    
    // Sistema de colisiones
    private CollisionHandler _collisionHandler;
    
    // Sistema de spawn de modelos
    private ModelSpawner? _modelSpawner;
    
    // Inicializador del mundo
    private GameWorldInitializer _initializer = null!;

    public override void _Ready()
    {
        Logger.Log("🎮 GameWorld: _Ready() INICIADO - ESCENA DEL MUNDO CARGÁNDOSE");
        
        // Crear y usar el inicializador
        _initializer = new GameWorldInitializer(this);
        _initializer.InitializeGameWorld();
        
        // Obtener referencias a los sistemas inicializados
        var systems = _initializer.GetSystems();
        _terrainManager = systems.terrain;
        _networkManager = systems.network;
        _playerController = systems.player;
        _playerPersistence = systems.persistence;
        _collisionHandler = systems.collision;
        
        // Obtener referencias a UI
        _player = GetNode<CharacterBody3D>("Player");
        _camera = GetNode<Camera3D>("Player/Camera3D");
        var labelCoords = GetNode<Label>("UI/LabelCoords");
        
        // Inicializar el sistema de HUD
        _gameHUD = new GameHUD();
        _gameHUD.Initialize(labelCoords, _playerController, _terrainManager);
        AddChild(_gameHUD);
        
        // Conectar eventos del PlayerController para actualizar UI
        _playerController.PlayerMoved += OnPlayerMoved;
        _playerController.CameraRotated += OnCameraRotated;
        
        // Inicializar el sistema de spawn de modelos
        _modelSpawner = new ModelSpawner(this);
        AddChild(_modelSpawner);
    }
    
    /// <summary>
    /// Se llama cuando el jugador se mueve (evento del PlayerController)
    /// </summary>
    private void OnPlayerMoved(Vector3 newPosition)
    {
        // Actualizar HUD de coordenadas
        _gameHUD.UpdateCoordinatesDisplay();
        
        // Ajustar altura al terreno si está disponible y no está en modo red
        if (!_networkManager.IsNetworkMode && _terrainManager != null)
        {
            float terrainHeight = _terrainManager.GetTerrainHeightAt(newPosition.X, newPosition.Z);
            _playerController.AdjustToTerrain(terrainHeight);
        }
    }
    
    /// <summary>
    /// Se llama cuando la cámara rota (evento del PlayerController)
    /// </summary>
    private void OnCameraRotated(Vector3 rotation)
    {
        // Actualizar HUD de coordenadas
        _gameHUD.UpdateCoordinatesDisplay();
    }
    
    
    
    


    public override void _Notification(int what)
    {
        base._Notification(what);
        
        switch ((int)what)
        {
            case (int)NotificationWMCloseRequest:
                Logger.Log("GameWorld: Notificación de cierre de ventana recibida");
                break;
            case (int)NotificationReady:
                Logger.Log("GameWorld: Notificación Ready recibida");
                break;
            case (int)NotificationPaused:
                Logger.Log("GameWorld: ⚠️ ESCENA PAUSADA - esto podría causar el problema");
                break;
            case (int)NotificationUnpaused:
                Logger.Log("GameWorld: Escena reanudada");
                break;
            case (int)NotificationExitTree:
                HandleExitTree();
                break;
        }
    }
    
    /// <summary>Maneja la salida del árbol de escenas.</summary>
    private void HandleExitTree()
    {
        Logger.Log("GameWorld: Saliendo del árbol de escenas");
        // El guardado automático lo maneja PlayerPersistence en su _ExitTree
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        
        // Procesar auto-save del jugador
        _playerPersistence.ProcessAutoSave(delta);
        
        // DEBUG: Logging de posición cada 10 segundos (comentado para producción)
        // if (Engine.GetFramesDrawn() % 600 == 0) // Cada ~600 frames (10 segundos a 60 FPS)
        // {
        //     Logger.Log($"GameWorld: DEBUG - Posición actual: Jugador Local: {_player.Position}, Jugador Global: {_player.GlobalPosition}");
        // }
        
        // Movimiento WASD en el plano XZ - SOLO si no está congelado y NO está en modo red
        if (!_networkManager.IsNetworkMode)
        {
            _playerController.ProcessMovement(delta);
        }
        
        // En modo red, procesar inputs locales y enviar inputs al servidor
        if (_networkManager.IsNetworkMode)
        {
            _playerController.ProcessMovement(delta);
            
            // Ajustar altura al terreno periódicamente
            if (_terrainManager != null)
            {
                float terrainHeight = _terrainManager.GetTerrainHeightAt(_playerController.GetPlayerPosition().X, _playerController.GetPlayerPosition().Z);
                _playerController.AdjustToTerrain(terrainHeight);
            }
            
            // Delegar procesamiento de red al NetworkManager
            _networkManager.ProcessNetworkMovement(_playerController);
        }
        
        _gameHUD.UpdateCoordinatesDisplay();
    }

    public override void _Input(InputEvent ev)
    {
        // Delegar el procesamiento de input al PlayerController
        _playerController.ProcessInput(ev);
    }

    public override void _UnhandledInput(InputEvent ev)
    {
        // Detectar tecla ESC para debug
        if (ev is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
        {
            Logger.Log("GameWorld: Tecla ESC presionada - verificando si hay pausa");
        }
        
        // ESC ahora lo maneja el PauseMenu
        // Este método queda vacío para evitar conflictos
    }


    
    /// <summary>Congela el movimiento del jugador y cámara.</summary>
    public void FreezePlayer()
    {
        _playerController.FreezePlayer();
    }

    /// <summary>Descongela el movimiento del jugador y cámara.</summary>
    public void UnfreezePlayer()
    {
        _playerController.UnfreezePlayer();
    }
    
    
    
    /// <summary>Muestra un modelo 3D en coordenadas específicas.</summary>
    /// <param name="modelPath">Ruta al archivo del modelo (ej: "res://assets/models/realistic_tree.glb")</param>
    /// <param name="position">Posición donde mostrar el modelo</param>
    /// <param name="name">Nombre del objeto (opcional)</param>
    /// <returns>El nodo 3D del modelo instanciado, o null si falla</returns>
    public Node3D SpawnModel(string modelPath, Vector3 position, string name = "Model")
    {
        return _modelSpawner?.SpawnModel(modelPath, position, name);
    }
}
