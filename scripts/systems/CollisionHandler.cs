using Godot;
using Wild.Scripts.Player;
using Wild.Network;

namespace Wild.Systems;

/// <summary>
/// Sistema de manejo de colisiones para el jugador
/// Gestiona la detección de barreras y teletransporte
/// </summary>
public partial class CollisionHandler : Node
{
    private CharacterBody3D _player = null!;
    private PlayerController _playerController = null!;
    private NetworkManager _networkManager = null!;
    
    // Detector de colisiones
    private Area3D _collisionDetector = null!;
    
    public override void _Ready()
    {
        Logger.Log("CollisionHandler: Inicializado");
    }
    
    /// <summary>
    /// Inicializa el sistema de colisiones
    /// </summary>
    public void Initialize(CharacterBody3D player, PlayerController playerController, NetworkManager networkManager)
    {
        _player = player;
        _playerController = playerController;
        _networkManager = networkManager;
        
        Logger.Log($"CollisionHandler: Inicializado con Player: {_player?.Name}, Controller: {_playerController?.Name}");
        
        SetupCollisionDetection();
    }
    
    /// <summary>
    /// Configura el sistema de detección de colisiones
    /// </summary>
    private void SetupCollisionDetection()
    {
        if (_player == null)
        {
            Logger.LogError("CollisionHandler: Player es nulo, no se puede configurar detección");
            return;
        }
        
        // Configurar capas de colisión del jugador para interactuar con barreras
        _player.CollisionLayer = 1;  // Jugador está en capa 1
        _player.CollisionMask = 2;  // Jugador puede colisionar con capa 2 (barreras)
        Logger.Log($"CollisionHandler: Jugador - Capas actualizadas - CollisionLayer: {_player.CollisionLayer}, CollisionMask: {_player.CollisionMask}");
        
        // Añadir Area3D para detección de colisiones
        _collisionDetector = new Area3D();
        _collisionDetector.Name = "CollisionDetector";
        _collisionDetector.CollisionLayer = 1;  // Mismo layer que el jugador
        _collisionDetector.CollisionMask = 2;  // Detectar barreras (capa 2)
        
        // Añadir shape de colisión (un poco más grande que el jugador)
        var detectorShape = new CollisionShape3D();
        var capsuleShape = new CapsuleShape3D();
        capsuleShape.Height = 2.0f;
        capsuleShape.Radius = 0.5f;
        detectorShape.Shape = capsuleShape;
        _collisionDetector.AddChild(detectorShape);
        
        _player.AddChild(_collisionDetector);
        
        // Conectar señales de colisión del Area3D
        _collisionDetector.BodyEntered += OnBodyEntered;
        _collisionDetector.BodyExited += OnBodyExited;
        
        Logger.Log("CollisionHandler: ✅ Sistema de detección de colisiones configurado");
    }
    
    /// <summary>
    /// Se llama cuando el jugador colisiona con otro cuerpo
    /// </summary>
    private void OnBodyEntered(Node body)
    {
        // Verificar si es una barrera (convertir StringName a string)
        string bodyName = body.Name;
        string parentName = body.GetParent()?.Name ?? "";
        
        if (bodyName.Contains("ChunkBoundaries") || parentName == "ChunkBoundaries")
        {
            HandleBarrierCollision();
        }
        else
        {
            // Logging normal para otras colisiones (comentado para flujo normal del juego)
            // Logger.Log($"CollisionHandler: Colisión normal con: {body.Name} (Tipo: {body.GetType().Name})");
        }
    }
    
    /// <summary>
    /// Se llama cuando el jugador deja de colisionar con otro cuerpo
    /// </summary>
    private void OnBodyExited(Node body)
    {
        // Verificar si es una barrera (convertir StringName a string)
        string bodyName = body.Name;
        string parentName = body.GetParent()?.Name ?? "";
        
        if (bodyName.Contains("Boundary") || parentName == "ChunkBoundaries")
        {
            // Logger.Log($"CollisionHandler: Jugador dejó de colisionar con barrera: {bodyName}");
        }
    }
    
    /// <summary>
    /// Maneja la colisión con barreras de chunk
    /// </summary>
    private void HandleBarrierCollision()
    {
        Logger.Log($"CollisionHandler: ¡COLISIÓN CON BARRERA! Teletransportando jugador hacia atrás");
        
        // Teletransportar al jugador hacia atrás
        Vector3 currentPosition = _playerController.GetPlayerGlobalPosition();
        Vector3 currentVelocity = _player.Velocity;
        
        // Calcular dirección opuesta al movimiento
        Vector3 pushDirection = -currentVelocity.Normalized();
        if (pushDirection == Vector3.Zero)
        {
            // Si está quieto, empujar hacia el centro del chunk actual
            // Calcular dinámicamente el centro del chunk donde está el jugador
            int chunkX = (int)Math.Floor(currentPosition.X / 100.0f);
            int chunkZ = (int)Math.Floor(currentPosition.Z / 100.0f);
            Vector3 chunkCenter = new Vector3(chunkX * 100 + 50f, currentPosition.Y, chunkZ * 100 + 50f);
            pushDirection = (chunkCenter - currentPosition).Normalized();
        }
        
        // Teletransportar una pequeña distancia hacia atrás
        Vector3 newPosition = currentPosition + pushDirection * 2.0f;
        _playerController.SetPlayerGlobalPosition(newPosition);
        
        // Detener el movimiento
        _player.Velocity = Vector3.Zero;
        
        Logger.Log($"CollisionHandler: Jugador teletransportado de {currentPosition} a {newPosition}");
        
        // Notificar al servidor para que ignore esta posición
        _networkManager?.NotifyBlockedPosition(currentPosition, newPosition);
    }
    
    /// <summary>
    /// Limpia los recursos del sistema de colisiones
    /// </summary>
    public override void _ExitTree()
    {
        // Desconectar señales
        if (_collisionDetector != null)
        {
            _collisionDetector.BodyEntered -= OnBodyEntered;
            _collisionDetector.BodyExited -= OnBodyExited;
        }
        
        Logger.Log("CollisionHandler: Sistema de colisiones destruido");
    }
}
