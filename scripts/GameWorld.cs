using Godot;
using System.Globalization;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using Wild.Network;
using Wild.Scripts.Terrain;
using FileAccess = Godot.FileAccess;

namespace Wild;

/// <summary>
/// Escena de partida (mundo) con movimiento básico y UI de coordenadas
/// </summary>
public partial class GameWorld : Node3D
{
    private CharacterBody3D _player = null!;
    private Camera3D _camera = null!;
    private Label _labelCoords = null!;
    private bool _isFrozen = false;
    private bool _isCameraLocked = false;
    
    // Sistema de terreno
    private TerrainManager _terrainManager = null!;
    private bool _terrainInitialized = false;
    
    // Componente de red
    private GameClient _gameClient = null!;
    
    // Sincronización con servidor
    private bool _isNetworkMode = false;
    private Vector3 _serverPosition = Vector3.Zero;
    private Vector3 _serverRotation = Vector3.Zero;
    private string _localPlayerId = string.Empty; // ID de nuestro jugador
    
    // Control de envío de inputs al servidor
    private float _lastSentYaw = 0f;
    private float _lastSentPitch = 0f;
    
    // Control de envío de posición al servidor
    private ulong _lastPositionSendTime = 0;
    private const ulong PositionSendIntervalMs = 50; // 20 actualizaciones por segundo (1000/20 = 50ms)
    
    // Guardado automático de posición
    private ulong _lastSaveTime = 0;
    private const ulong SaveIntervalMs = 5000; // Guardar cada 5 segundos
    
    // Constantes de movimiento (en metros por segundo)
    private const float MoveSpeed = 5f;  // 5 metros por segundo = caminar normal
    private const float MouseSensitivity = 0.2f; // Aumentada para mejor control
    private const float CameraHeight = 2f;
    private float _cameraYaw = 0f;   // grados, rotación horizontal
    private float _cameraPitch = 0f; // grados, rotación vertical (-90 a 90)

    public override void _Ready()
    {
        Logger.Log("🎮 GameWorld: _Ready() INICIADO - ESCENA DEL MUNDO CARGÁNDOSE");
        InitializeGameWorld();
    }
    
    /// <summary>
    /// Inicializa el mundo del juego de forma asíncrona
    /// </summary>
    private async void InitializeGameWorld()
    {
        try
        {
            // Logger.Log("GameWorld:// Obtener referencias a componentes importantes
            _player = GetNode<CharacterBody3D>("Player");
            _camera = GetNode<Camera3D>("Player/Camera3D");
            _labelCoords = GetNode<Label>("UI/LabelCoords");
            
            // Logging para depuración de colisiones
            Logger.Log($"GameWorld: Jugador - CollisionLayer: {_player.CollisionLayer}, CollisionMask: {_player.CollisionMask}");
            
            // Configurar capas de colisión del jugador para interactuar con barreras
            _player.CollisionLayer = 1;  // Jugador está en capa 1
            _player.CollisionMask = 2;  // Jugador puede colisionar con capa 2 (barreras)
            Logger.Log($"GameWorld: Jugador - Capas actualizadas - CollisionLayer: {_player.CollisionLayer}, CollisionMask: {_player.CollisionMask}");
            
            // Añadir Area3D para detección de colisiones
            var collisionDetector = new Area3D();
            collisionDetector.Name = "CollisionDetector";
            collisionDetector.CollisionLayer = 1;  // Mismo layer que el jugador
            collisionDetector.CollisionMask = 2;  // Detectar barreras (capa 2)
            
            // Añadir shape de colisión (un poco más grande que el jugador)
            var detectorShape = new CollisionShape3D();
            var capsuleShape = new CapsuleShape3D();
            capsuleShape.Height = 2.0f;
            capsuleShape.Radius = 0.5f;
            detectorShape.Shape = capsuleShape;
            collisionDetector.AddChild(detectorShape);
            
            _player.AddChild(collisionDetector);
            
            // Conectar señales de colisión del Area3D
            collisionDetector.BodyEntered += OnBodyEntered;
            collisionDetector.BodyExited += OnBodyExited;
            // Logger.Log("GameWorld: Area3D de detección de colisiones añadido al jugador y señales conectadas");
            
            // Inicializar sistema de terreno
            await InitializeTerrain();
            
            // Logger.Log($"GameWorld: ✅ Cámara obtenida: {_camera.Name}");
            // Logger.Log($"GameWorld: ✅ Label coordenadas obtenido: {_labelCoords.Name}");
            
            // Obtener referencia del cliente de red desde GameFlow
            Logger.Log("GameWorld: Obteniendo cliente de red desde GameFlow");
            var gameFlow = GetNode<GameFlow>("/root/GameFlow");
            _gameClient = gameFlow.GetGameClient();
            
            Logger.Log($"GameWorld: Cliente de red obtenido: {_gameClient != null}");
            
            // DEBUG: Verificar material del suelo
            try
            {
                var floor = GetNode<StaticBody3D>("Floor");
                Logger.Log($"GameWorld: DEBUG - Floor encontrado: {floor != null}");
                
                if (floor != null)
                {
                    var floorMesh = floor.GetNode<MeshInstance3D>("MeshInstance3D");
                    Logger.Log($"GameWorld: DEBUG - FloorMesh encontrado: {floorMesh != null}");
                    
                    if (floorMesh != null)
                    {
                        var floorMaterial = floorMesh.GetActiveMaterial(0);
                        Logger.Log($"GameWorld: DEBUG - Material del suelo: {floorMaterial?.GetType().Name ?? "null"}");
                        
                        if (floorMaterial is StandardMaterial3D stdMat)
                        {
                            Logger.Log($"GameWorld: DEBUG - Albedo texture: {stdMat.AlbedoTexture?.GetPath() ?? "null"}");
                            Logger.Log($"GameWorld: DEBUG - UV scale: {stdMat.Uv1Scale}");
                            Logger.Log($"GameWorld: DEBUG - Albedo color: {stdMat.AlbedoColor}");
                        }
                        else
                        {
                            Logger.Log($"GameWorld: DEBUG - Material no es StandardMaterial3D");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"GameWorld: ERROR DEBUG material suelo: {ex.Message}");
            }
            
            if (_gameClient != null && _gameClient.IsConnected)
            {
                _isNetworkMode = true;
                Logger.Log("GameWorld: 🌐 MODO RED ACTIVADO - conectado al servidor");
                
                // Conectar señales del cliente
                Logger.Log("GameWorld: Conectando señales del cliente de red");
                _gameClient.OnPositionUpdated += OnServerPositionUpdated;
                _gameClient.OnRotationUpdated += OnServerRotationUpdated;
                _gameClient.OnLocalPlayerIdAssigned += OnLocalPlayerIdAssigned;
                _gameClient.OnRemotePlayerJoined += OnRemotePlayerJoined;
                _gameClient.OnRemotePlayerLeft += OnRemotePlayerLeft;
                _gameClient.OnRemotePlayerUpdated += OnRemotePlayerUpdated;
                Logger.Log("GameWorld: ✅ Señales del cliente conectadas");
            }
            else
            {
                Logger.Log("GameWorld: 🏠 MODO LOCAL - sin conexión de red");
            }
            
            // Cargar posición guardada del jugador
            LoadPlayerPosition();
            
            // Si no hay posición guardada, usar posición inicial
            // NOTA: Usamos una bandera para evitar sobrescribir la posición cargada
            bool hasLoadedPosition = false;
            
            // Verificar si el archivo de posición guardada existe
            string worldPath = GetWorldPath();
            if (!string.IsNullOrEmpty(worldPath))
            {
                string playerDataPath = Path.Combine(worldPath, "player", $"player_{GetPlayerId()}.json");
                hasLoadedPosition = FileAccess.FileExists(playerDataPath);
            }
            
            Logger.Log($"GameWorld: DEBUG - Posición después de cargar: {_player.Position}, HasLoaded: {hasLoadedPosition}");
            
            if (!hasLoadedPosition)
            {
                Logger.Log("GameWorld: Configurando posición inicial de cámara");
                
                // Posición inicial en el centro del chunk (50, 50) y altura sobre el terreno
                Vector3 initialPosition = new Vector3(50f, 2f, 50f);
                
                // Si el terreno está inicializado, obtener altura real del terreno
                if (_terrainInitialized && _terrainManager != null)
                {
                    // Obtener altura del terreno en la posición inicial
                    float terrainHeight = GetTerrainHeightAt(50f, 50f);
                    initialPosition.Y = terrainHeight + 2f; // 2 metros sobre el terreno
                    
                    Logger.Log($"GameWorld: Altura del terreno en (50,50): {terrainHeight}");
                }
                else
                {
                    Logger.Log($"GameWorld: Terreno no inicializado, usando altura por defecto: 2");
                }
                
                _player.GlobalPosition = initialPosition;
                
                Logger.Log("GameWorld: Configurando ángulos iniciales de cámara");
                // Ángulos iniciales desde la rotación actual
                var rot = _camera.GlobalRotation;
                _cameraYaw = Mathf.RadToDeg(rot.Y);
                _cameraPitch = Mathf.RadToDeg(rot.X);
                
                Logger.Log($"GameWorld: Posición inicial establecida: {initialPosition}");
                Logger.Log($"GameWorld: Ángulos iniciales - Yaw: {_cameraYaw:F1}°, Pitch: {_cameraPitch:F1}°");
            }
            else
            {
                Logger.Log("GameWorld: ✅ Usando posición guardada del jugador");
            }
            
            Logger.Log("GameWorld: Capturando mouse para movimiento FPS");
            // Capturar el mouse para movimiento FPS
            Input.MouseMode = Input.MouseModeEnum.Captured;
            
            // Asegurar que el mouse esté capturado
            if (Input.MouseMode != Input.MouseModeEnum.Captured)
            {
                Logger.Log("GameWorld: ⚠️ El mouse no está capturado - intentando de nuevo");
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
            
            Logger.Log("🎮 GameWorld: ✅ ESCENA DEL MUNDO CARGADA CORRECTAMENTE");
            Logger.Log("GameWorld: Movimiento WASD + Mouse habilitado");
            Logger.Log("GameWorld: UI de coordenadas activada");
            Logger.Log("GameWorld: Árbol con colisiones listo");
            Logger.Log("GameWorld: _Ready() COMPLETADO - ESCENA LISTA PARA JUGAR");
            
            // Liberar el botón de nueva partida en GameFlow
            var gameFlowNode = GetNode<GameFlow>("/root/GameFlow");
            gameFlowNode?.Call("UpdateNewGameButton", "Nueva Partida");
            
            // Logger.Log("GameWorld: Iniciando bucle _Process - el juego debería responder ahora");
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"GameWorld: ❌ ERROR CRÍTICO en InitializeGameWorld(): {ex.Message}");
            Logger.LogError($"GameWorld: Stack trace: {ex.StackTrace}");
        }
    }
    
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
            Logger.LogError($"GameWorld: Error al obtener altura del terreno: {ex.Message}");
            return 0f;
        }
    }
    
    /// <summary>
    /// Obtiene el ChunkGenerator del TerrainManager
    /// </summary>
    private ChunkGenerator? GetChunkGenerator()
    {
        return _terrainManager?.GetNode<ChunkGenerator>("ChunkGenerator");
    }

    /// <summary>
    /// Inicializa el sistema de terreno procedural
    /// </summary>
    private async Task InitializeTerrain()
    {
        try
        {
            Logger.Log("GameWorld: Inicializando sistema de terreno...");
            
            // Crear TerrainManager
            _terrainManager = new TerrainManager();
            _terrainManager.Name = "TerrainManager";
            AddChild(_terrainManager);
            
            // Obtener el nombre del mundo actual desde WorldManager
            string worldName = WorldManager.Instance.CurrentWorld ?? "test_world";
            Logger.Log($"GameWorld: Usando mundo actual: {worldName}");
            Logger.Log($"GameWorld: DEBUG - WorldManager.Instance.CurrentWorld es nulo: {WorldManager.Instance.CurrentWorld == null}");
            
            // Restaurar el estado del personaje actual al iniciar la escena
            CharacterManager.RestoreCurrentCharacterState();
            
            // Obtener la semilla desde WorldManager
            int seed = WorldManager.Instance.GetCurrentWorldSeed();
            Logger.Log($"GameWorld: Usando semilla: {seed}");
            
            // Obtener ID del personaje actual para logging
            string currentCharacterId = CharacterManager.Instance?.GetCurrentCharacterId() ?? "no_disponible";
            Logger.Log($"GameWorld: Personaje actual al cargar: {currentCharacterId}");
            
            _terrainManager.InitializeWorld(worldName, seed);
            
            // Verificar si el chunk inicial existe antes de generarlo
            Vector2I initialChunkPos = new Vector2I(0, 0);
            string initialChunkPath = Path.Combine(_terrainManager.ChunksDirectory, $"chunk_0_0.dat");
            
            if (FileAccess.FileExists(initialChunkPath))
            {
                Logger.Log("GameWorld: Chunk inicial (0,0) ya existe, cargando desde disco...");
                Chunk existingChunk = await _terrainManager.LoadChunk(initialChunkPos);
                
                if (existingChunk != null)
                {
                    _terrainInitialized = true;
                    Logger.Log("GameWorld: ✅ Chunk inicial cargado correctamente desde disco");
                }
                else
                {
                    Logger.LogError("GameWorld: ❌ Error al cargar chunk inicial existente");
                }
            }
            else
            {
                Logger.Log("GameWorld: Chunk inicial (0,0) no existe, generando nuevo...");
                Chunk initialChunk = await _terrainManager.GenerateInitialChunk();
                
                if (initialChunk != null)
                {
                    _terrainInitialized = true;
                    Logger.Log("GameWorld: ✅ Chunk inicial generado y cargado correctamente");
                }
                else
                {
                    Logger.LogError("GameWorld: ❌ Error al generar chunk inicial");
                }
            }
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"GameWorld: Error al inicializar terreno: {ex.Message}");
        }
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
    
    /// <summary>Maneja la salida del árbol de escenas con guardado síncrono.</summary>
    private void HandleExitTree()
    {
        Logger.Log("GameWorld: Saliendo del árbol de escenas - guardando posición final");
        
        // DEBUG: Guardar posición ANTES de que el servidor se detenga (comentado para producción)
        // Logger.Log($"GameWorld: DEBUG - Guardando posición ANTES de salir - Jugador Global: {_player.GlobalPosition}");
        
        // Guardar posición síncronamente (sin await)
        SavePlayerPosition();
        
        // DEBUG: Guardado completado (comentado para producción)
        // Logger.Log($"GameWorld: DEBUG - Guardado completado - Jugador Global: {_player.GlobalPosition}");
        
        // Guardado de seguridad por si acaso
        SavePlayerPosition();
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        
        // Guardado automático de posición cada 5 segundos
        var currentTime = Time.GetTicksMsec();
        if (currentTime - _lastSaveTime >= SaveIntervalMs)
        {
            SavePlayerPosition();
            _lastSaveTime = currentTime;
        }
        
        // DEBUG: Logging de posición cada 10 segundos (comentado para producción)
        // if (Engine.GetFramesDrawn() % 600 == 0) // Cada ~600 frames (10 segundos a 60 FPS)
        // {
        //     Logger.Log($"GameWorld: DEBUG - Posición actual: Jugador Local: {_player.Position}, Jugador Global: {_player.GlobalPosition}");
        // }
        
        // Movimiento WASD en el plano XZ - SOLO si no está congelado y NO está en modo red
        if (!_isFrozen && !_isNetworkMode)
        {
            var move = Vector3.Zero;
            if (Input.IsActionPressed("move_forward")) move += GetCameraForwardXZ();
            if (Input.IsActionPressed("move_back")) move -= GetCameraForwardXZ();
            if (Input.IsActionPressed("move_left")) move -= GetCameraRightXZ();
            if (Input.IsActionPressed("move_right")) move += GetCameraRightXZ();
            
            if (move.LengthSquared() > 0.001f)
            {
                move = move.Normalized() * MoveSpeed * (float)delta;
                
                // Calcular nueva posición
                Vector3 newPosition = _player.Position + move;
                
                // Ajustar altura al terreno si está disponible
                if (_terrainInitialized && _terrainManager != null)
                {
                    float terrainHeight = GetTerrainHeightAt(newPosition.X, newPosition.Z);
                    newPosition.Y = terrainHeight + 2f; // 2 metros sobre el terreno
                }
                
                _player.Position = newPosition;
                _player.Velocity = Vector3.Zero;
            }
            else
            {
                _player.Velocity = Vector3.Zero;
            }
        }
        
        // En modo red, procesar inputs locales y enviar inputs al servidor
        if (_isNetworkMode)
        {
            // En modo red, procesamos movimiento localmente PERO solo enviamos inputs
            var move = Vector3.Zero;
            if (Input.IsActionPressed("move_forward")) move += GetCameraForwardXZ();
            if (Input.IsActionPressed("move_back")) move -= GetCameraForwardXZ();
            if (Input.IsActionPressed("move_left")) move -= GetCameraRightXZ();
            if (Input.IsActionPressed("move_right")) move += GetCameraRightXZ();
            
            if (move.LengthSquared() > 0.001f)
            {
                move = move.Normalized() * MoveSpeed * (float)delta;
                
                // Calcular nueva posición
                Vector3 newPosition = _player.Position + move;
                
                // Ajustar altura al terreno si está disponible
                if (_terrainInitialized && _terrainManager != null)
                {
                    float terrainHeight = GetTerrainHeightAt(newPosition.X, newPosition.Z);
                    newPosition.Y = terrainHeight + 2f; // 2 metros sobre el terreno
                }
                
                _player.Position = newPosition;
                _player.Velocity = Vector3.Zero;
            }
            else
            {
                _player.Velocity = Vector3.Zero;
            }
            
            // En modo red, ajustar altura al terreno periódicamente
            if (_terrainInitialized && _terrainManager != null)
            {
                float terrainHeight = GetTerrainHeightAt(_player.Position.X, _player.Position.Z);
                _player.Position = new Vector3(_player.Position.X, terrainHeight + 2f, _player.Position.Z);
            }
            
            // Enviar inputs al servidor en lugar de posición
            if (move.LengthSquared() > 0.001f || _cameraYaw != _lastSentYaw || _cameraPitch != _lastSentPitch)
            {
                SendInputToServer(move, new Vector3(_cameraPitch, _cameraYaw, 0));
                _lastSentYaw = _cameraYaw;
                _lastSentPitch = _cameraPitch;
            }
        }
        
        ApplyCameraRotation();
        UpdateCoordsLabel();
    }

    public override void _Input(InputEvent ev)
    {
        // El input del mouse se procesa siempre, pero la aplicación depende del modo
        if (ev is InputEventMouseMotion motion && !_isCameraLocked)
        {
            _cameraYaw -= motion.Relative.X * MouseSensitivity;
            _cameraPitch -= motion.Relative.Y * MouseSensitivity;
            _cameraPitch = Mathf.Clamp(_cameraPitch, -89f, 89f);
            
            // Normalizar yaw a 0-360 grados
            _cameraYaw = Mathf.Wrap(_cameraYaw, 0f, 360f);
            
            // En modo red, aplicar rotación inmediatamente para respuesta
            if (_isNetworkMode)
            {
                _camera.GlobalRotation = new Vector3(
                    Mathf.DegToRad(_cameraPitch),
                    Mathf.DegToRad(_cameraYaw),
                    0f
                );
            }
        }
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

    private Vector3 GetCameraForwardXZ()
    {
        var f = -_camera.GlobalTransform.Basis.Z;
        f.Y = 0;
        return f.Normalized();
    }

    private Vector3 GetCameraRightXZ()
    {
        var r = _camera.GlobalTransform.Basis.X;
        r.Y = 0;
        return r.Normalized();
    }

    private void ApplyCameraRotation()
    {
        // En modo local, aplicar rotación directamente
        // En modo red, la rotación ya se aplica en _Input para respuesta inmediata
        if (!_isNetworkMode)
        {
            _camera.GlobalRotation = new Vector3(
                Mathf.DegToRad(_cameraPitch),
                Mathf.DegToRad(_cameraYaw),
                0f
            );
        }
    }

    private void UpdateCoordsLabel()
    {
        var pos = _player.GlobalPosition;
        
        // Añadir información del terreno si está inicializado
        string terrainInfo = _terrainInitialized ? " | Terreno: PROCEDURAL" : " | Terreno: PLANO";
        
        _labelCoords.Text = $"Pos: ({pos.X:F1}, {pos.Y:F1}, {pos.Z:F1}) | Cámara: pitch {_cameraPitch:F0}° yaw {_cameraYaw:F0}°{terrainInfo}";
    }

    /// <summary>Congela el movimiento del jugador y cámara.</summary>
    public void FreezePlayer()
    {
        _isFrozen = true;
        _isCameraLocked = true;
        Logger.Log("GameWorld: Jugador y cámara congelados");
    }

    /// <summary>Descongela el movimiento del jugador y cámara.</summary>
    public void UnfreezePlayer()
    {
        _isFrozen = false;
        _isCameraLocked = false;
        Logger.Log("GameWorld: Jugador y cámara descongelados");
    }
    
    /// <summary>Maneja asignación de ID del jugador local.</summary>
    private void OnLocalPlayerIdAssigned(string playerId)
    {
        _localPlayerId = playerId;
        Logger.Log($"GameWorld: ID de jugador local asignado: {playerId}");
    }
    
    /// <summary>Maneja actualización de posición desde el servidor.</summary>
    private void OnServerPositionUpdated(Vector3 position)
    {
        _serverPosition = position;
        
        // En modo red, sincronizar posición local con servidor
        if (_isNetworkMode)
        {
            // Aplicar posición completa del servidor (incluyendo Y)
            _player.GlobalPosition = position;
            // Logger.Log($"GameWorld: Posición sincronizada con servidor: {position}");
        }
    }
    
    /// <summary>Maneja actualización de rotación desde el servidor.</summary>
    private void OnServerRotationUpdated(Vector3 rotation)
    {
        _serverRotation = rotation;
        
        // En modo red, sincronizar rotación local con servidor
        if (_isNetworkMode)
        {
            // Actualizar valores locales
            _cameraPitch = rotation.X;
            _cameraYaw = rotation.Y;
            
            // Aplicar rotación inmediatamente a la cámara
            _camera.GlobalRotation = new Vector3(
                Mathf.DegToRad(_cameraPitch),
                Mathf.DegToRad(_cameraYaw),
                0f
            );
            
            // Logger.Log($"GameWorld: Rotación sincronizada con servidor: {rotation}");
        }
    }
    
    /// <summary>Maneja conexión de un jugador remoto.</summary>
    private void OnRemotePlayerJoined(string playerId, Vector3 position, Vector3 rotation)
    {
        // Logger.Log($"GameWorld: Jugador remoto {playerId} se unió en pos={position}, rot={rotation}");
        // TODO: Crear visualización del jugador remoto
    }
    
    /// <summary>Maneja desconexión de un jugador remoto.</summary>
    private void OnRemotePlayerLeft(string playerId)
    {
        Logger.Log($"GameWorld: Jugador remoto {playerId} se desconectó");
        // TODO: Eliminar visualización del jugador remoto
    }
    
    /// <summary>Maneja actualización de un jugador remoto.</summary>
    private void OnRemotePlayerUpdated(string playerId, Vector3 position, Vector3 rotation)
    {
        // Logger.Log($"GameWorld: Jugador remoto {playerId} actualizado: pos={position}, rot={rotation}");
        // TODO: Actualizar visualización del jugador remoto
    }
    
    /// <summary>Envía inputs al servidor en lugar de posición.</summary>
    private async void SendInputToServer(Vector3 direction, Vector3 rotation)
    {
        // Formato: "INPUT:movimiento:x,y,z|rotacion:pitch,yaw"
        var message = $"INPUT:movimiento:{direction.X.ToString(CultureInfo.InvariantCulture)},{direction.Y.ToString(CultureInfo.InvariantCulture)},{direction.Z.ToString(CultureInfo.InvariantCulture)}|rotacion:{rotation.X.ToString(CultureInfo.InvariantCulture)},{rotation.Y.ToString(CultureInfo.InvariantCulture)}";
        
        await _gameClient.SendPlayerInput(direction, rotation);
    }
    
    /// <summary>Muestra un modelo 3D en coordenadas específicas.</summary>
    /// <param name="modelPath">Ruta al archivo del modelo (ej: "res://assets/models/realistic_tree.glb")</param>
    /// <param name="position">Posición donde mostrar el modelo</param>
    /// <param name="name">Nombre del objeto (opcional)</param>
    /// <returns>El nodo 3D del modelo instanciado, o null si falla</returns>
    public Node3D SpawnModel(string modelPath, Vector3 position, string name = "Model")
    {
        try
        {
            Logger.Log($"GameWorld: SpawnModel() - INICIADO para: {modelPath}");
            // Logger.Log($"GameWorld: Posición: {position}, Nombre: {name}");
            
            // Verificar si el archivo existe
            if (!Godot.FileAccess.FileExists(modelPath))
            {
                Logger.LogError($"GameWorld: ❌ El modelo no existe: {modelPath}");
                return null;
            }
            
            Logger.Log($"GameWorld: ✅ Archivo de modelo encontrado: {modelPath}");
            
            // Cargar el modelo
            var modelResource = GD.Load(modelPath);
            
            if (modelResource == null)
            {
                Logger.LogError($"GameWorld: ❌ No se pudo cargar el modelo: {modelPath}");
                return null;
            }
            
            Logger.Log($"GameWorld: ✅ Modelo cargado - Tipo: {modelResource.GetType().Name}");
            
            Node3D modelNode = null;
            
            // Si es una PackedScene, instanciarla
            if (modelResource is PackedScene packedScene)
            {
                Logger.Log("GameWorld: Modelo es PackedScene, instanciando");
                modelNode = packedScene.Instantiate<Node3D>();
                modelNode.Name = name;
                modelNode.Position = position;
                
                AddChild(modelNode);
                Logger.Log($"GameWorld: ✅ Modelo instanciado: {modelNode.Name}");
            }
            // Si es un mesh directo, crear MeshInstance3D
            else if (modelResource is Mesh mesh)
            {
                Logger.Log("GameWorld: Modelo es Mesh, creando MeshInstance3D");
                var meshInstance = new MeshInstance3D();
                meshInstance.Mesh = mesh;
                meshInstance.Name = name;
                meshInstance.Position = position;
                
                AddChild(meshInstance);
                modelNode = meshInstance;
                Logger.Log($"GameWorld: ✅ MeshInstance3D creado: {modelNode.Name}");
            }
            
            return modelNode;
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"GameWorld: ❌ Error instanciando modelo: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Se llama cuando el jugador colisiona con otro cuerpo
    /// </summary>
    private void OnBodyEntered(Node body)
    {
        // Logger.Log($"GameWorld: ¡COLISIÓN DETECTADA! Jugador entró en contacto con: {body.Name} (Tipo: {body.GetType().Name})");
        
        // Verificar si es una barrera (convertir StringName a string)
        string bodyName = body.Name;
        string parentName = body.GetParent()?.Name ?? "";
        
        if (bodyName.Contains("ChunkBoundaries") || parentName == "ChunkBoundaries")
        {
            // Logger.Log($"GameWorld: ¡COLISIÓN CON BARRERA! Teletransportando jugador hacia atrás");
            
            // Teletransportar al jugador hacia atrás
            Vector3 currentPosition = _player.GlobalPosition;
            Vector3 currentVelocity = _player.Velocity;
            
            // Calcular dirección opuesta al movimiento
            Vector3 pushDirection = -currentVelocity.Normalized();
            if (pushDirection == Vector3.Zero)
            {
                // Si está quieto, empujar hacia el centro del chunk (0,0 a 100,100)
                Vector3 chunkCenter = new Vector3(50f, currentPosition.Y, 50f);
                pushDirection = (chunkCenter - currentPosition).Normalized();
            }
            
            // Teletransportar una pequeña distancia hacia atrás
            Vector3 newPosition = currentPosition + pushDirection * 2.0f;
            _player.GlobalPosition = newPosition;
            
            // Detener el movimiento
            _player.Velocity = Vector3.Zero;
            
            // Logger.Log($"GameWorld: Jugador teletransportado de {currentPosition} a {newPosition}");
            
            // Notificar al servidor para que ignore esta posición
            NotifyServerBlockedPosition(currentPosition, newPosition);
        }
        else
        {
            // Logging normal para otras colisiones (comentado para flujo normal del juego)
            // Logger.Log($"GameWorld: Colisión normal con: {body.Name} (Tipo: {body.GetType().Name})");
        }
    } /// <summary>
    /// Se llama cuando el jugador deja de colisionar con otro cuerpo
    /// </summary>
    private void OnBodyExited(Node body)
    {
        //Logger.Log($"GameWorld: Jugador dejó de colisionar con: {body.Name} (Tipo: {body.GetType().Name})");
        
        // Verificar si es una barrera (convertir StringName a string)
        string bodyName = body.Name;
        string parentName = body.GetParent()?.Name ?? "";
        
        if (bodyName.Contains("Boundary") || parentName == "ChunkBoundaries")
        {
            //Logger.Log($"GameWorld: Jugador dejó de colisionar con barrera: {bodyName}");
        }
    }
    
    /// <summary>Guarda la posición actual del jugador en el directorio del mundo.</summary>
    public void SavePlayerPosition()
    {
        try
        {
            if (_player == null)
                return;
            
            // Obtener ID del jugador local
            string playerId = GetPlayerId();
            if (string.IsNullOrEmpty(playerId))
            {
                Logger.Log("GameWorld: No se puede guardar posición - no hay ID de jugador");
                return;
            }
            
            // Obtener posición y rotación actual
            var position = _player.Position;
            var rotation = new Vector3(_cameraYaw, _cameraPitch, 0);
            
            // DEBUG: Solo loggear guardado si hay movimiento real (comentado para producción)
            // if (position != Vector3.Zero)
            // {
            //     Logger.Log($"GameWorld: DEBUG - Guardando posición - Jugador Local: {position}, Jugador Global: {_player.GlobalPosition}");
            //     Logger.Log($"GameWorld: DEBUG - Guardando rotación - Cámara: {_cameraYaw:F1}°, {_cameraPitch:F1}°");
            // }
            
            // Crear datos del jugador
            var playerData = new
            {
                PlayerId = playerId,
                Position = new { X = position.X, Y = position.Y, Z = position.Z },
                GlobalPosition = new { X = _player.GlobalPosition.X, Y = _player.GlobalPosition.Y, Z = _player.GlobalPosition.Z },
                Rotation = new { Yaw = _cameraYaw, Pitch = _cameraPitch },
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            
            // Serializar a JSON
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            string jsonString = JsonSerializer.Serialize(playerData, jsonOptions);
            
            // Guardar en el directorio del mundo actual
            string worldPath = GetWorldPath();
            if (!string.IsNullOrEmpty(worldPath))
            {
                string playerDataPath = Path.Combine(worldPath, "player", $"player_{playerId}.json");
                
                // Asegurar que el directorio exista
                var dir = DirAccess.Open(worldPath);
                if (dir != null)
                {
                    dir.MakeDirRecursive("player");
                    
                    // Guardar archivo
                    var file = FileAccess.Open(playerDataPath, FileAccess.ModeFlags.Write);
                    if (file != null)
                    {
                        file.StoreString(jsonString);
                        file.Close();
                        
                        // Solo loggear si hay movimiento real
                        if (position != Vector3.Zero)
                        {
                            Logger.Log($"GameWorld: Posición guardada para jugador {playerId} en: {playerDataPath}");
                        }
                    }
                    else
                    {
                        Logger.LogError($"GameWorld: Error al guardar posición en: {playerDataPath}");
                    }
                }
                else
                {
                    Logger.LogError($"GameWorld: Error al acceder al directorio del mundo: {worldPath}");
                }
            }
            else
            {
                Logger.Log("GameWorld: No se puede guardar posición - no hay mundo activo");
            }
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"GameWorld: Error al guardar posición del jugador: {ex.Message}");
        }
    }
    
    /// <summary>Carga la posición guardada del jugador.</summary>
    private void LoadPlayerPosition()
    {
        try
        {
            if (_player == null)
                return;
            
            // Obtener ID del jugador local
            string playerId = GetPlayerId();
            if (string.IsNullOrEmpty(playerId))
            {
                Logger.Log("GameWorld: No se puede cargar posición - no hay ID de jugador");
                return;
            }
            
            // Obtener ruta del mundo actual
            string worldPath = GetWorldPath();
            if (string.IsNullOrEmpty(worldPath))
            {
                Logger.Log("GameWorld: No se puede cargar posición - no hay mundo activo");
                return;
            }
            
            string playerDataPath = Path.Combine(worldPath, "player", $"player_{playerId}.json");
            
            if (!FileAccess.FileExists(playerDataPath))
            {
                Logger.Log($"GameWorld: No hay posición guardada para jugador {playerId}, usando posición inicial");
                return;
            }
            
            Logger.Log($"GameWorld: DEBUG - Cargando archivo: {playerDataPath}");
            
            // Cargar archivo
            var file = FileAccess.Open(playerDataPath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                Logger.LogError($"GameWorld: Error al leer posición desde: {playerDataPath}");
                return;
            }
            
            string jsonString = file.GetAsText();
            file.Close();
            
            // DEBUG: JSON cargado (comentado para producción)
            // Logger.Log($"GameWorld: DEBUG - JSON cargado: {jsonString}");
            
            // Deserializar JSON
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            using var document = JsonDocument.Parse(jsonString);
            var root = document.RootElement;
            
            // Verificar que el archivo pertenezca al jugador correcto
            if (root.TryGetProperty("playerId", out var playerIdProp))
            {
                string savedPlayerId = playerIdProp.GetString();
                if (savedPlayerId != playerId)
                {
                    Logger.Log($"GameWorld: El archivo pertenece a otro jugador ({savedPlayerId}), ignorando");
                    return;
                }
            }
            
            Logger.Log($"GameWorld: DEBUG - Antes de cargar - Jugador Global: {_player.GlobalPosition}, Cámara: {_camera.GlobalPosition}");
            
            // Cargar posición
            Vector3 loadedPosition = Vector3.Zero;
            bool usedGlobalPosition = false;
            
            if (root.TryGetProperty("globalPosition", out var globalPositionProp))
            {
                float x = globalPositionProp.GetProperty("x").GetSingle();
                float y = globalPositionProp.GetProperty("y").GetSingle();
                float z = globalPositionProp.GetProperty("z").GetSingle();
                loadedPosition = new Vector3(x, y, z);
                
                // Verificar si la posición global es válida (no es 0,0,0)
                if (loadedPosition != Vector3.Zero)
                {
                    _player.GlobalPosition = loadedPosition;
                    usedGlobalPosition = true;
                    Logger.Log($"GameWorld: Posición global cargada para jugador {playerId}: ({x}, {y}, {z})");
                    // DEBUG: Después de cargar (comentado para producción)
                    // Logger.Log($"GameWorld: DEBUG - Después de cargar - Jugador Global: {_player.GlobalPosition}, Cámara: {_camera.GlobalPosition}");
                }
                else
                {
                    // DEBUG: Posición global inválida (comentado para producción)
                    // Logger.Log("GameWorld: Posición global es (0,0,0), intentando usar posición local");
                }
            }
            
            // Si no se usó posición global o era inválida, usar posición local
            if (!usedGlobalPosition && root.TryGetProperty("position", out var positionProp))
            {
                float x = positionProp.GetProperty("x").GetSingle();
                float y = positionProp.GetProperty("y").GetSingle();
                float z = positionProp.GetProperty("z").GetSingle();
                
                _player.Position = new Vector3(x, y, z);
                Logger.Log($"GameWorld: Posición local cargada para jugador {playerId}: ({x}, {y}, {z})");
                // DEBUG: Después de cargar local (comentado para producción)
                // Logger.Log($"GameWorld: DEBUG - Después de cargar (local) - Jugador Global: {_player.GlobalPosition}, Cámara: {_camera.GlobalPosition}");
            }
            
            // Cargar rotación
            if (root.TryGetProperty("rotation", out var rotationProp))
            {
                _cameraYaw = rotationProp.GetProperty("yaw").GetSingle();
                _cameraPitch = rotationProp.GetProperty("pitch").GetSingle();
                
                // Actualizar rotación de la cámara
                UpdateCameraRotation();
                Logger.Log($"GameWorld: Rotación cargada para jugador {playerId}: Yaw={_cameraYaw}, Pitch={_cameraPitch}");
            }
            
            // La cámara es un nodo hijo del jugador, se actualiza automáticamente
            // No necesitamos llamar a UpdateCameraPosition()
            Logger.Log($"GameWorld: ✅ Posición y rotación del jugador {playerId} cargadas correctamente");
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"GameWorld: Error al cargar posición del jugador: {ex.Message}");
        }
    }
    
    /// <summary>Actualiza la posición de la cámara para seguir al jugador.</summary>
    private void UpdateCameraPosition()
    {
        if (_camera != null && _player != null)
        {
            // La cámara es un nodo hijo del jugador, se actualiza automáticamente
            // Solo necesitamos actualizar la rotación, no la posición
            // _camera.Position = _player.Position + new Vector3(0, CameraHeight, 0);
            
            // Para debugging: mostrar la posición actual de la cámara
            // Logger.Log($"GameWorld: DEBUG - Cámara posición: {_camera.Position}, Jugador posición: {_player.Position}");
        }
    }
    
    /// <summary>Actualiza la rotación de la cámara.</summary>
    private void UpdateCameraRotation()
    {
        if (_camera != null)
        {
            _camera.RotationDegrees = new Vector3(_cameraPitch, _cameraYaw, 0);
        }
    }
    
    /// <summary>Obtiene la ruta del directorio del mundo actual.</summary>
    private string GetWorldPath()
    {
        try
        {
            // Obtener el nombre del mundo actual desde SessionData
            var sessionData = GetNode<SessionData>("/root/SessionData");
            if (sessionData != null && !string.IsNullOrEmpty(sessionData.WorldName))
            {
                return $"user://worlds/{sessionData.WorldName}";
            }
            return string.Empty;
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"GameWorld: Error al obtener ruta del mundo: {ex.Message}");
            return string.Empty;
        }
    }
    
    /// <summary>Obtiene el ID único del jugador local.</summary>
    private string GetPlayerId()
    {
        try
        {
            // Usar el CharacterManager para obtener el ID del personaje actual
            if (CharacterManager.Instance != null && CharacterManager.Instance.CurrentCharacter != null)
            {
                string characterId = CharacterManager.Instance.GetCurrentCharacterId();
                // DEBUG: ID de personaje (comentado para producción)
                // Logger.Log($"GameWorld: Usando ID de personaje: {characterId}");
                return characterId;
            }
            
            // DEBUG: CharacterManager no disponible (comentado para producción)
            // Logger.LogWarning("GameWorld: CharacterManager no disponible, usando fallback");
            return "jugador";
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"GameWorld: Error al obtener ID de personaje: {ex.Message}");
            return "jugador";
        }
    }
    
    /// <summary>
    /// Notifica al servidor que una posición fue bloqueada por las barreras
    /// </summary>
    private async void NotifyServerBlockedPosition(Vector3 blockedPosition, Vector3 correctedPosition)
    {
        try
        {
            var client = GetNode<GameFlow>("/root/GameFlow")?.GetGameClient();
            if (client != null)
            {
                // Enviar mensaje especial al servidor indicando posición bloqueada
                string blockMessage = $"BLOCKED_POS:{blockedPosition.X}:{blockedPosition.Y}:{blockedPosition.Z}|{correctedPosition.X}:{correctedPosition.Y}:{correctedPosition.Z}";
                await client.SendPositionUpdate(blockMessage);
                Logger.Log($"GameWorld: Notificado al servidor - Posición bloqueada: {blockedPosition}, Corregida: {correctedPosition}");
            }
            else
            {
                Logger.Log("GameWorld: No se pudo obtener el cliente de red para notificar posición bloqueada");
            }
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"GameWorld: Error al notificar posición bloqueada: {ex.Message}");
        }
    }
}
