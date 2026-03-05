using Godot;
using System.Globalization;
using Wild.Network;
using Wild.Scripts.Player;

namespace Wild.Network;

/// <summary>
/// Gestiona toda la funcionalidad de red del juego (cliente-servidor)
/// </summary>
public partial class NetworkManager : Node
{
    // Componente de red
    private GameClient _gameClient = null!;
    
    // Estado de red
    private bool _isNetworkMode = false;
    private string _localPlayerId = string.Empty;
    
    // Sincronización con servidor
    private Vector3 _serverPosition = Vector3.Zero;
    private Vector3 _serverRotation = Vector3.Zero;
    
    // Control de envío de inputs al servidor
    private float _lastSentYaw = 0f;
    private float _lastSentPitch = 0f;
    
    // Control de envío de posición al servidor
    private ulong _lastPositionSendTime = 0;
    private const ulong PositionSendIntervalMs = 50; // 20 actualizaciones por segundo (1000/20 = 50ms)
    
    // Referencia al PlayerController para aplicar cambios
    private PlayerController _playerController = null!;
    
    // Eventos para comunicación con GameWorld
    [Signal]
    public delegate void NetworkModeChangedEventHandler(bool isNetworkMode);
    
    [Signal]
    public delegate void LocalPlayerIdAssignedEventHandler(string playerId);
    
    [Signal]
    public delegate void ServerPositionUpdatedEventHandler(Vector3 position);
    
    [Signal]
    public delegate void ServerRotationUpdatedEventHandler(Vector3 rotation);
    
    [Signal]
    public delegate void RemotePlayerJoinedEventHandler(string playerId, Vector3 position, Vector3 rotation);
    
    [Signal]
    public delegate void RemotePlayerLeftEventHandler(string playerId);
    
    [Signal]
    public delegate void RemotePlayerUpdatedEventHandler(string playerId, Vector3 position, Vector3 rotation);

    /// <summary>
    /// Inicializa el sistema de red
    /// </summary>
    public void Initialize()
    {
        Logger.Log("NetworkManager: Inicializando sistema de red...");
        
        // Obtener referencia del cliente de red desde GameFlow
        var gameFlow = GetNode<GameFlow>("/root/GameFlow");
        _gameClient = gameFlow.GetGameClient();
        
        Logger.Log($"NetworkManager: Cliente de red obtenido: {_gameClient != null}");
        
        if (_gameClient != null && _gameClient.IsConnected)
        {
            _isNetworkMode = true;
            Logger.Log("NetworkManager: 🌐 MODO RED ACTIVADO - conectado al servidor");
            
            // Conectar señales del cliente
            ConnectClientSignals();
            EmitSignal(SignalName.NetworkModeChanged, true);
        }
        else
        {
            Logger.Log("NetworkManager: 🏠 MODO LOCAL - sin conexión de red");
            EmitSignal(SignalName.NetworkModeChanged, false);
        }
    }
    
    /// <summary>
    /// Establece la referencia al PlayerController para aplicar cambios de red
    /// </summary>
    public void SetPlayerController(PlayerController playerController)
    {
        _playerController = playerController;
        Logger.Log($"NetworkManager: PlayerController establecido: {_playerController != null}");
    }
    
    /// <summary>
    /// Conecta las señales del cliente de red
    /// </summary>
    private void ConnectClientSignals()
    {
        Logger.Log("NetworkManager: Conectando señales del cliente de red");
        _gameClient.OnPositionUpdated += OnServerPositionUpdated;
        _gameClient.OnRotationUpdated += OnServerRotationUpdated;
        _gameClient.OnLocalPlayerIdAssigned += OnLocalPlayerIdAssigned;
        _gameClient.OnRemotePlayerJoined += OnRemotePlayerJoined;
        _gameClient.OnRemotePlayerLeft += OnRemotePlayerLeft;
        _gameClient.OnRemotePlayerUpdated += OnRemotePlayerUpdated;
        Logger.Log("NetworkManager: ✅ Señales del cliente conectadas");
    }
    
    /// <summary>
    /// Procesa el movimiento en modo red y envía inputs al servidor
    /// </summary>
    public void ProcessNetworkMovement(PlayerController playerController)
    {
        if (!_isNetworkMode)
            return;
        
        // Enviar inputs al servidor
        var playerPos = playerController.GetPlayerPosition();
        var angles = playerController.GetCameraAngles();
        
        if (playerPos != Vector3.Zero || angles.X != _lastSentYaw || angles.Y != _lastSentPitch)
        {
            // Calcular dirección de movimiento (esto necesitaría ser expuesto por PlayerController)
            // Por ahora, enviamos la posición directamente
            SendInputToServer(Vector3.Zero, new Vector3(angles.Y, angles.X, 0));
            _lastSentYaw = angles.X;
            _lastSentPitch = angles.Y;
        }
    }
    
    /// <summary>
    /// Envía inputs al servidor en lugar de posición
    /// </summary>
    private async void SendInputToServer(Vector3 direction, Vector3 rotation)
    {
        try
        {
            // Formato: "INPUT:movimiento:x,y,z|rotacion:pitch,yaw"
            var message = $"INPUT:movimiento:{direction.X.ToString(CultureInfo.InvariantCulture)},{direction.Y.ToString(CultureInfo.InvariantCulture)},{direction.Z.ToString(CultureInfo.InvariantCulture)}|rotacion:{rotation.X.ToString(CultureInfo.InvariantCulture)},{rotation.Y.ToString(CultureInfo.InvariantCulture)}";
            
            await _gameClient.SendPlayerInput(direction, rotation);
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"NetworkManager: Error al enviar input al servidor: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Notifica al servidor que una posición fue bloqueada por las barreras
    /// </summary>
    public async void NotifyBlockedPosition(Vector3 blockedPosition, Vector3 correctedPosition)
    {
        try
        {
            if (_gameClient != null)
            {
                // Enviar mensaje especial al servidor indicando posición bloqueada
                string blockMessage = $"BLOCKED_POS:{blockedPosition.X}:{blockedPosition.Y}:{blockedPosition.Z}|{correctedPosition.X}:{correctedPosition.Y}:{correctedPosition.Z}";
                await _gameClient.SendPositionUpdate(blockMessage);
                Logger.Log($"NetworkManager: Notificado al servidor - Posición bloqueada: {blockedPosition}, Corregida: {correctedPosition}");
            }
            else
            {
                Logger.Log("NetworkManager: No se pudo obtener el cliente de red para notificar posición bloqueada");
            }
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"NetworkManager: Error al notificar posición bloqueada: {ex.Message}");
        }
    }
    
    // Getters para estado de red
    public bool IsNetworkMode => _isNetworkMode;
    public string LocalPlayerId => _localPlayerId;
    public Vector3 ServerPosition => _serverPosition;
    public Vector3 ServerRotation => _serverRotation;
    
    // Event handlers del cliente de red
    private void OnLocalPlayerIdAssigned(string playerId)
    {
        _localPlayerId = playerId;
        Logger.Log($"NetworkManager: ID de jugador local asignado: {playerId}");
        EmitSignal(SignalName.LocalPlayerIdAssigned, playerId);
    }
    
    private void OnServerPositionUpdated(Vector3 position)
    {
        _serverPosition = position;
        
        // Aplicar posición directamente al PlayerController
        if (_playerController != null)
        {
            _playerController.SetPlayerGlobalPosition(position);
            Logger.Log($"NetworkManager: Posición sincronizada con servidor: {position}");
        }
        
        EmitSignal(SignalName.ServerPositionUpdated, position);
    }
    
    private void OnServerRotationUpdated(Vector3 rotation)
    {
        _serverRotation = rotation;
        
        // Aplicar rotación directamente al PlayerController
        if (_playerController != null)
        {
            _playerController.SetCameraAngles(rotation.Y, rotation.X);
            Logger.Log($"NetworkManager: Rotación sincronizada con servidor: {rotation}");
        }
        
        EmitSignal(SignalName.ServerRotationUpdated, rotation);
    }
    
    private void OnRemotePlayerJoined(string playerId, Vector3 position, Vector3 rotation)
    {
        Logger.Log($"NetworkManager: Jugador remoto {playerId} se unió en pos={position}, rot={rotation}");
        EmitSignal(SignalName.RemotePlayerJoined, playerId, position, rotation);
    }
    
    private void OnRemotePlayerLeft(string playerId)
    {
        Logger.Log($"NetworkManager: Jugador remoto {playerId} se desconectó");
        EmitSignal(SignalName.RemotePlayerLeft, playerId);
    }
    
    private void OnRemotePlayerUpdated(string playerId, Vector3 position, Vector3 rotation)
    {
        Logger.Log($"NetworkManager: Jugador remoto {playerId} actualizado: pos={position}, rot={rotation}");
        EmitSignal(SignalName.RemotePlayerUpdated, playerId, position, rotation);
    }
}
