using Godot;
using Wild.Network;
using Wild.Scripts.Player;
using Wild.Systems;
using Wild.Scripts.Terrain;

namespace Wild.Systems;

/// <summary>
/// Inicializador del mundo del juego - Encargado de configurar todos los sistemas
/// </summary>
public partial class GameWorldInitializer : Node
{
    private GameWorld _gameWorld;
    private CharacterBody3D _player;
    private Camera3D _camera;
    private Label _labelCoords;
    
    // Sistemas a inicializar
    private TerrainManager _terrainManager;
    private NetworkManager _networkManager;
    private PlayerController _playerController;
    private PlayerPersistence _playerPersistence;
    private CollisionHandler _collisionHandler;
    
    public GameWorldInitializer(GameWorld gameWorld)
    {
        _gameWorld = gameWorld;
    }
    
    /// <summary>
    /// Inicializa el mundo del juego de forma asíncrona
    /// </summary>
    public async void InitializeGameWorld()
    {
        try
        {
            Logger.Log("🎮 GameWorldInitializer: INICIADO - Configurando sistemas");
            
            // Obtener referencias a componentes importantes
            GetWorldReferences();
            
            // Configurar sistema de colisiones
            SetupCollisionSystem();
            
            // Inicializar sistema de terreno
            await SetupTerrainSystem();
            
            // Configurar sistema de red
            SetupNetworkSystem();
            
            // Configurar sistema de jugador
            SetupPlayerSystem();
            
            // Configurar persistencia del jugador
            SetupPlayerPersistence();
            
            // Conectar sistemas entre sí
            ConnectSystems();
            
            // Configurar posición inicial del jugador
            SetupInitialPlayerPosition();
            
            // Configurar controles
            SetupControls();
            
            // Iniciar sistemas automáticos
            StartAutomaticSystems();
            
            // Notificar completado
            NotifyCompletion();
            
            Logger.Log("🎮 GameWorldInitializer: ✅ SISTEMAS CONFIGURADOS CORRECTAMENTE");
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"GameWorldInitializer: ❌ ERROR CRÍTICO en InitializeGameWorld(): {ex.Message}");
            Logger.LogError($"GameWorldInitializer: Stack trace: {ex.StackTrace}");
        }
    }
    
    /// <summary>
    /// Obtiene las referencias a los componentes del mundo
    /// </summary>
    private void GetWorldReferences()
    {
        _player = _gameWorld.GetNode<CharacterBody3D>("Player");
        _camera = _gameWorld.GetNode<Camera3D>("Player/Camera3D");
        _labelCoords = _gameWorld.GetNode<Label>("UI/LabelCoords");
        
        Logger.Log("GameWorldInitializer: Referencias a componentes obtenidas");
    }
    
    /// <summary>
    /// Configura el sistema de colisiones
    /// </summary>
    private void SetupCollisionSystem()
    {
        _collisionHandler = new CollisionHandler();
        _collisionHandler.Name = "CollisionHandler";
        _gameWorld.AddChild(_collisionHandler);
        
        Logger.Log("GameWorldInitializer: Sistema de colisiones configurado");
    }
    
    /// <summary>
    /// Inicializa el sistema de terreno
    /// </summary>
    private async Task SetupTerrainSystem()
    {
        Logger.Log("GameWorldInitializer: Inicializando sistema de terreno...");
        
        // Crear TerrainManager
        _terrainManager = new TerrainManager();
        _terrainManager.Name = "TerrainManager";
        _gameWorld.AddChild(_terrainManager);
        
        // Obtener el nombre del mundo actual desde WorldManager
        string worldName = WorldManager.Instance.CurrentWorld ?? "test_world";
        Logger.Log($"GameWorldInitializer: Usando mundo actual: {worldName}");
        
        // Restaurar el estado del personaje actual al iniciar la escena
        CharacterManager.RestoreCurrentCharacterState();
        
        // Obtener la semilla desde WorldManager
        int seed = WorldManager.Instance.GetCurrentWorldSeed();
        Logger.Log($"GameWorldInitializer: Usando semilla: {seed}");
        
        // Obtener ID del personaje actual para logging
        string currentCharacterId = CharacterManager.Instance?.GetCurrentCharacterId() ?? "no_disponible";
        Logger.Log($"GameWorldInitializer: Personaje actual al cargar: {currentCharacterId}");
        
        _terrainManager.InitializeWorld(worldName, seed);
        
        // Determinar el chunk inicial basado en la posición del jugador
        Vector2I initialChunkPos = GetPlayerChunkPosition();
        Logger.Log($"GameWorldInitializer: Chunk inicial determinado por posición del jugador: ({initialChunkPos.X}, {initialChunkPos.Y})");
        
        // Cargar o generar el chunk inicial
        await LoadOrGenerateInitialChunk(initialChunkPos);
        
        Logger.Log("GameWorldInitializer: Sistema de terreno configurado");
    }
    
    /// <summary>
    /// Determina el chunk donde debería estar el jugador basado en su posición guardada
    /// </summary>
    private Vector2I GetPlayerChunkPosition()
    {
        try
        {
            // Crear PlayerPersistence temporal para verificar si hay datos guardados
            var tempPersistence = new PlayerPersistence();
            if (tempPersistence.HasSavedData())
            {
                // Si hay datos guardados, cargarlos para obtener la posición
                var tempController = new PlayerController();
                if (tempPersistence.LoadPlayerData(tempController))
                {
                    var position = tempController.GetPlayerGlobalPosition();
                    
                    // Convertir coordenadas de mundo a coordenadas de chunk
                    int chunkX = (int)Math.Floor(position.X / 100.0f);
                    int chunkZ = (int)Math.Floor(position.Z / 100.0f);
                    
                    Logger.Log($"GameWorldInitializer: Posición guardada encontrada: ({position.X}, {position.Z}) → Chunk: ({chunkX}, {chunkZ})");
                    return new Vector2I(chunkX, chunkZ);
                }
            }
            
            // Si no hay posición guardada, usar chunk (0,0) como fallback
            Logger.Log("GameWorldInitializer: No hay posición guardada, usando chunk (0,0) como fallback");
            return new Vector2I(0, 0);
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"GameWorldInitializer: Error al determinar chunk del jugador: {ex.Message}");
            return new Vector2I(0, 0);
        }
    }
    
    /// <summary>
    /// Carga o genera el chunk inicial
    /// </summary>
    private async Task LoadOrGenerateInitialChunk(Vector2I initialChunkPos)
    {
        // Verificar si el chunk inicial existe antes de generarlo
        string initialChunkPath = System.IO.Path.Combine(_terrainManager.ChunksDirectory, $"chunk_{initialChunkPos.X}_{initialChunkPos.Y}.dat");
        
        if (Godot.FileAccess.FileExists(initialChunkPath))
        {
            Logger.Log($"GameWorldInitializer: Chunk inicial ({initialChunkPos.X}, {initialChunkPos.Y}) ya existe, cargando desde disco...");
            Chunk existingChunk = await _terrainManager.LoadChunk(initialChunkPos);
            
            if (existingChunk != null)
            {
                Logger.Log($"GameWorldInitializer: ✅ Chunk inicial ({initialChunkPos.X}, {initialChunkPos.Y}) cargado correctamente desde disco");
            }
            else
            {
                Logger.LogError($"GameWorldInitializer: ❌ Error al cargar chunk inicial ({initialChunkPos.X}, {initialChunkPos.Y})");
            }
        }
        else
        {
            Logger.Log($"GameWorldInitializer: Chunk inicial ({initialChunkPos.X}, {initialChunkPos.Y}) no existe, generando nuevo...");
            Chunk initialChunk = await _terrainManager.LoadChunk(initialChunkPos);
            
            if (initialChunk != null)
            {
                Logger.Log($"GameWorldInitializer: ✅ Chunk inicial ({initialChunkPos.X}, {initialChunkPos.Y}) generado y cargado correctamente");
            }
            else
            {
                Logger.LogError($"GameWorldInitializer: ❌ Error al generar chunk inicial ({initialChunkPos.X}, {initialChunkPos.Y})");
            }
        }
    }
    
    /// <summary>
    /// Configura el sistema de red
    /// </summary>
    private void SetupNetworkSystem()
    {
        _networkManager = new NetworkManager();
        _networkManager.Name = "NetworkManager";
        _gameWorld.AddChild(_networkManager);
        
        // Inicializar el sistema de red
        _networkManager.Initialize();
        
        Logger.Log("GameWorldInitializer: Sistema de red configurado");
    }
    
    /// <summary>
    /// Configura el sistema de control del jugador
    /// </summary>
    private void SetupPlayerSystem()
    {
        _playerController = new PlayerController();
        _playerController.Name = "PlayerController";
        _gameWorld.AddChild(_playerController);
        
        Logger.Log("GameWorldInitializer: Sistema de control del jugador configurado");
    }
    
    /// <summary>
    /// Configura el sistema de persistencia del jugador
    /// </summary>
    private void SetupPlayerPersistence()
    {
        _playerPersistence = new PlayerPersistence();
        _playerPersistence.Name = "PlayerPersistence";
        _gameWorld.AddChild(_playerPersistence);
        
        Logger.Log("GameWorldInitializer: Sistema de persistencia del jugador configurado");
    }
    
    /// <summary>
    /// Conecta los sistemas entre sí
    /// </summary>
    private void ConnectSystems()
    {
        // Inicializar el controlador con las referencias
        _playerController.Initialize(_player, _camera, _networkManager.IsNetworkMode);
        
        // Establecer referencia del PlayerController en NetworkManager
        _networkManager.SetPlayerController(_playerController);
        
        // Inicializar PlayerPersistence con el PlayerController
        _playerPersistence.Initialize(_playerController);
        
        // Inicializar CollisionHandler con las referencias
        _collisionHandler.Initialize(_player, _playerController, _networkManager);
        
        // Conectar señales del PlayerController
        _playerController.PlayerMoved += OnPlayerMoved;
        _playerController.CameraRotated += OnCameraRotated;
        
        Logger.Log("GameWorldInitializer: Sistemas conectados entre sí");
    }
    
    /// <summary>
    /// Configura la posición inicial del jugador
    /// </summary>
    private void SetupInitialPlayerPosition()
    {
        // Cargar posición guardada del jugador
        _playerPersistence.LoadPlayerPosition();
        
        // Si no hay posición guardada, usar posición inicial
        bool hasLoadedPosition = _playerPersistence.HasSavedData();
        
        Logger.Log($"GameWorldInitializer: DEBUG - Posición después de cargar: {_player.Position}, HasLoaded: {hasLoadedPosition}");
        
        if (!hasLoadedPosition)
        {
            Logger.Log("GameWorldInitializer: Configurando posición inicial de cámara");
            
            // Posición inicial en el centro del chunk (50, 50) y altura sobre el terreno
            Vector3 initialPosition = new Vector3(50f, 2f, 50f);
            
            // Si el terreno está inicializado, obtener altura real del terreno
            if (_terrainManager != null)
            {
                // Obtener altura del terreno en la posición inicial
                float terrainHeight = GetTerrainHeightAt(50f, 50f);
                initialPosition.Y = terrainHeight + 2f; // 2 metros sobre el terreno
                
                Logger.Log($"GameWorldInitializer: Altura del terreno en (50,50): {terrainHeight}");
            }
            else
            {
                Logger.Log($"GameWorldInitializer: Terreno no inicializado, usando altura por defecto: 2");
            }
            
            _playerController.SetPlayerGlobalPosition(initialPosition);
            
            Logger.Log("GameWorldInitializer: Configurando ángulos iniciales de cámara");
            // Los ángulos iniciales se establecen en la inicialización del PlayerController
            
            Logger.Log($"GameWorldInitializer: Posición inicial establecida: {initialPosition}");
            var angles = _playerController.GetCameraAngles();
            Logger.Log($"GameWorldInitializer: Ángulos iniciales - Yaw: {angles.X:F1}°, Pitch: {angles.Y:F1}°");
        }
        else
        {
            Logger.Log("GameWorldInitializer: ✅ Usando posición guardada del jugador");
        }
    }
    
    /// <summary>
    /// Obtiene la altura del terreno en una posición específica
    /// </summary>
    private float GetTerrainHeightAt(float worldX, float worldZ)
    {
        try
        {
            if (_terrainManager == null)
                return 0f;
            
            // Obtener el generador de chunks del terrain manager
            var chunkGenerator = _terrainManager.GetChunkGenerator();
            if (chunkGenerator == null)
                return 0f;
            
            // Obtener altura usando el generador de ruido
            return chunkGenerator.GetHeightAt(worldX, worldZ);
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"GameWorldInitializer: Error al obtener altura del terreno: {ex.Message}");
            return 0f;
        }
    }
    
    /// <summary>
    /// Configura los controles del juego
    /// </summary>
    private void SetupControls()
    {
        Logger.Log("GameWorldInitializer: Capturando mouse para movimiento FPS");
        // Capturar el mouse para movimiento FPS
        Input.MouseMode = Input.MouseModeEnum.Captured;
        
        // Asegurar que el mouse esté capturado
        if (Input.MouseMode != Input.MouseModeEnum.Captured)
        {
            Logger.Log("GameWorldInitializer: ⚠️ El mouse no está capturado - intentando de nuevo");
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
        
        Logger.Log("GameWorldInitializer: Controles configurados");
    }
    
    /// <summary>
    /// Inicia los sistemas automáticos
    /// </summary>
    private void StartAutomaticSystems()
    {
        // Iniciar auto-save del jugador
        _playerPersistence.StartAutoSave();
        
        // Liberar el botón de nueva partida en GameFlow
        var gameFlowNode = _gameWorld.GetNode<GameFlow>("/root/GameFlow");
        gameFlowNode?.Call("UpdateNewGameButton", "Nueva Partida");
        
        Logger.Log("GameWorldInitializer: Sistemas automáticos iniciados");
    }
    
    /// <summary>
    /// Notifica la completitud de la inicialización
    /// </summary>
    private void NotifyCompletion()
    {
        Logger.Log("🎮 GameWorldInitializer: ✅ ESCENA DEL MUNDO CARGADA CORRECTAMENTE");
        Logger.Log("GameWorldInitializer: Movimiento WASD + Mouse habilitado");
        Logger.Log("GameWorldInitializer: UI de coordenadas activada");
        Logger.Log("GameWorldInitializer: Árbol con colisiones listo");
        Logger.Log("GameWorldInitializer: INICIALIZACIÓN COMPLETADA - ESCENA LISTA PARA JUGAR");
    }
    
    /// <summary>
    /// Se llama cuando el jugador se mueve (evento del PlayerController)
    /// </summary>
    private void OnPlayerMoved(Vector3 newPosition)
    {
        // Ajustar altura al terreno si está disponible y no está en modo red
        if (!_networkManager.IsNetworkMode && _terrainManager != null)
        {
            float terrainHeight = GetTerrainHeightAt(newPosition.X, newPosition.Z);
            _playerController.AdjustToTerrain(terrainHeight);
        }
    }
    
    /// <summary>
    /// Se llama cuando la cámara rota (evento del PlayerController)
    /// </summary>
    private void OnCameraRotated(Vector3 rotation)
    {
        // En modo local, la rotación ya se aplica en el PlayerController
        // En modo red, la rotación ya se aplica para respuesta inmediata
        // Este evento puede usarse para sincronización con servidor si es necesario
    }
    
    /// <summary>
    /// Obtiene referencias a los sistemas inicializados para uso externo
    /// </summary>
    public (TerrainManager terrain, NetworkManager network, PlayerController player, PlayerPersistence persistence, CollisionHandler collision) GetSystems()
    {
        return (_terrainManager, _networkManager, _playerController, _playerPersistence, _collisionHandler);
    }
}
