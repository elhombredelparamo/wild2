using Godot;
using System.Globalization;
using Wild.Network;

namespace Wild;

/// <summary>
/// Escena de partida (mundo) con movimiento básico y UI de coordenadas
/// </summary>
public partial class GameWorld : Node3D
{
    private Camera3D _camera = null!;
    private Label _labelCoords = null!;
    private bool _isFrozen = false;
    private bool _isCameraLocked = false;
    
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
    
    private const float MoveSpeed = 1.11f; // 4 km/h = 1.11 m/s exactos
    private const float MouseSensitivity = 0.15f; // Aumentada para mejor control
    private const float CameraHeight = 2f;
    private float _cameraYaw = 0f;   // grados, rotación horizontal
    private float _cameraPitch = 0f; // grados, rotación vertical (-90 a 90)

    public override void _Ready()
    {
        Logger.Log("GameWorld: _Ready() iniciado - ESCENA PRINCIPAL");
        
        _camera = GetNode<Camera3D>("Camera3D");
        _labelCoords = GetNode<Label>("UI/LabelCoords");
        
        // Obtener referencia del cliente de red desde GameFlow
        var gameFlow = GetNode<GameFlow>("/root/GameFlow");
        _gameClient = gameFlow.GetPrivateField<GameClient>("_gameClient");
        
        Logger.Log($"GameWorld: _gameClient obtenido: {_gameClient != null}");
        
        if (_gameClient != null && _gameClient.IsConnected)
        {
            _isNetworkMode = true;
            Logger.Log("GameWorld: Modo red activado - conectado al servidor");
            
            // Conectar señales del cliente
            _gameClient.OnPositionUpdated += OnServerPositionUpdated;
            _gameClient.OnRotationUpdated += OnServerRotationUpdated;
            _gameClient.OnLocalPlayerIdAssigned += OnLocalPlayerIdAssigned;
            _gameClient.OnRemotePlayerJoined += OnRemotePlayerJoined;
            _gameClient.OnRemotePlayerLeft += OnRemotePlayerLeft;
            _gameClient.OnRemotePlayerUpdated += OnRemotePlayerUpdated;
        }
        else
        {
            Logger.Log("GameWorld: Modo local - sin conexión de red");
        }
        
        // Posición inicial de la cámara
        _camera.GlobalPosition = new Vector3(0, CameraHeight, 5);
        
        // Ángulos iniciales desde la rotación actual
        var rot = _camera.GlobalRotation;
        _cameraYaw = Mathf.RadToDeg(rot.Y);
        _cameraPitch = Mathf.RadToDeg(rot.X);
        
        // Capturar el mouse para movimiento FPS
        Input.MouseMode = Input.MouseModeEnum.Captured;
        
        Logger.Log("GameWorld: ✅ Escena del mundo cargada correctamente");
        Logger.Log("GameWorld: Movimiento WASD + Mouse habilitado");
        Logger.Log("GameWorld: UI de coordenadas activada");
        Logger.Log("GameWorld: _Ready() completado");
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        
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
                move = move.Normalized() * MoveSpeed * dt;
                var pos = _camera.GlobalPosition;
                pos += move;
                pos.Y = CameraHeight; // Mantener altura constante
                _camera.GlobalPosition = pos;
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
            
            // En modo red, SÍ aplicamos movimiento localmente para respuesta inmediata
            // pero el servidor es la autoridad final
            if (move.LengthSquared() > 0.001f)
            {
                move = move.Normalized() * MoveSpeed * dt;
                var pos = _camera.GlobalPosition;
                pos += move;
                pos.Y = CameraHeight; // Mantener altura constante
                _camera.GlobalPosition = pos;
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
        var pos = _camera.GlobalPosition;
        _labelCoords.Text = $"Pos: ({pos.X:F1}, {pos.Y:F1}, {pos.Z:F1}) | Cámara: pitch {_cameraPitch:F0}° yaw {_cameraYaw:F0}°";
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
            _camera.GlobalPosition = position;
            Logger.Log($"GameWorld: Posición sincronizada con servidor: {position}");
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
            
            Logger.Log($"GameWorld: Rotación sincronizada con servidor: {rotation}");
        }
    }
    
    /// <summary>Maneja conexión de un jugador remoto.</summary>
    private void OnRemotePlayerJoined(string playerId, Vector3 position, Vector3 rotation)
    {
        Logger.Log($"GameWorld: Jugador remoto {playerId} se unió en pos={position}, rot={rotation}");
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
        Logger.Log($"GameWorld: Jugador remoto {playerId} actualizado: pos={position}, rot={rotation}");
        // TODO: Actualizar visualización del jugador remoto
    }
    
    /// <summary>Envía inputs al servidor en lugar de posición.</summary>
    private async void SendInputToServer(Vector3 direction, Vector3 rotation)
    {
        // Formato: "INPUT:movimiento:x,y,z|rotacion:pitch,yaw"
        var message = $"INPUT:movimiento:{direction.X.ToString(CultureInfo.InvariantCulture)},{direction.Y.ToString(CultureInfo.InvariantCulture)},{direction.Z.ToString(CultureInfo.InvariantCulture)}|rotacion:{rotation.X.ToString(CultureInfo.InvariantCulture)},{rotation.Y.ToString(CultureInfo.InvariantCulture)}";
        
        await _gameClient.SendPlayerInput(direction, rotation);
    }
}
