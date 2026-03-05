using Godot;
using Wild;

namespace Wild.Scripts.Player;

/// <summary>
/// Controlador del jugador para movimiento FPS y gestión de cámara
/// </summary>
public partial class PlayerController : Node
{
    // Referencias a componentes del jugador
    private CharacterBody3D _player = null!;
    private Camera3D _camera = null!;
    
    // Estados del controlador
    private bool _isFrozen = false;
    private bool _isCameraLocked = false;
    private bool _isNetworkMode = false;
    
    // Constantes de movimiento
    private const float MoveSpeed = 5f;  // 5 metros por segundo = caminar normal
    private const float MouseSensitivity = 0.2f;
    private const float CameraHeight = 2f;
    
    // Ángulos de cámara
    private float _cameraYaw = 0f;   // grados, rotación horizontal
    private float _cameraPitch = 0f; // grados, rotación vertical (-90 a 90)
    
    // Eventos para comunicación con otros componentes
    [Signal]
    public delegate void PlayerMovedEventHandler(Vector3 newPosition);
    
    [Signal]
    public delegate void CameraRotatedEventHandler(Vector3 rotation);
    
    /// <summary>
    /// Inicializa el controlador con las referencias necesarias
    /// </summary>
    public void Initialize(CharacterBody3D player, Camera3D camera, bool networkMode = false)
    {
        _player = player;
        _camera = camera;
        _isNetworkMode = networkMode;
        
        // Establecer ángulos iniciales desde la rotación actual de la cámara
        var rot = _camera.GlobalRotation;
        _cameraYaw = Mathf.RadToDeg(rot.Y);
        _cameraPitch = Mathf.RadToDeg(rot.X);
        
        Logger.Log($"PlayerController: Inicializado - Modo red: {_isNetworkMode}");
    }
    
    /// <summary>
    /// Procesa el movimiento del jugador
    /// </summary>
    public void ProcessMovement(double delta)
    {
        if (_isFrozen) return;
        
        var dt = (float)delta;
        var move = Vector3.Zero;
        
        // Input de movimiento WASD
        if (Input.IsActionPressed("move_forward")) move += GetCameraForwardXZ();
        if (Input.IsActionPressed("move_back")) move -= GetCameraForwardXZ();
        if (Input.IsActionPressed("move_left")) move -= GetCameraRightXZ();
        if (Input.IsActionPressed("move_right")) move += GetCameraRightXZ();
        
        if (move.LengthSquared() > 0.001f)
        {
            move = move.Normalized() * MoveSpeed * dt;
            var newPosition = _player.Position + move;
            _player.Position = newPosition;
            _player.Velocity = Vector3.Zero;
            
            // Emitir señal de movimiento
            EmitSignal(SignalName.PlayerMoved, newPosition);
        }
        else
        {
            _player.Velocity = Vector3.Zero;
        }
    }
    
    /// <summary>
    /// Procesa el input del mouse para rotación de cámara
    /// </summary>
    public void ProcessInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && !_isCameraLocked)
        {
            _cameraYaw -= motion.Relative.X * MouseSensitivity;
            _cameraPitch -= motion.Relative.Y * MouseSensitivity;
            _cameraPitch = Mathf.Clamp(_cameraPitch, -89f, 89f);
            
            // Normalizar yaw a 0-360 grados
            _cameraYaw = Mathf.Wrap(_cameraYaw, 0f, 360f);
            
            // En modo red, aplicar rotación inmediatamente para respuesta
            if (_isNetworkMode)
            {
                ApplyCameraRotation();
            }
            
            // Emitir señal de rotación
            var rotation = new Vector3(_cameraPitch, _cameraYaw, 0);
            EmitSignal(SignalName.CameraRotated, rotation);
        }
    }
    
    /// <summary>
    /// Aplica la rotación de la cámara
    /// </summary>
    public void ApplyCameraRotation()
    {
        _camera.GlobalRotation = new Vector3(
            Mathf.DegToRad(_cameraPitch),
            Mathf.DegToRad(_cameraYaw),
            0f
        );
    }
    
    /// <summary>
    /// Ajusta la posición del jugador a una altura específica
    /// </summary>
    public void SetPlayerHeight(float height)
    {
        var currentPos = _player.Position;
        currentPos.Y = height;
        _player.Position = currentPos;
    }
    
    /// <summary>
    /// Ajusta la posición del jugador al terreno
    /// </summary>
    public void AdjustToTerrain(float terrainHeight)
    {
        SetPlayerHeight(terrainHeight + CameraHeight);
    }
    
    /// <summary>
    /// Congela el movimiento del jugador y cámara
    /// </summary>
    public void Freeze()
    {
        _isFrozen = true;
        _isCameraLocked = true;
        Logger.Log("PlayerController: Jugador y cámara congelados");
    }
    
    /// <summary>
    /// Descongela el movimiento del jugador y cámara
    /// </summary>
    public void Unfreeze()
    {
        _isFrozen = false;
        _isCameraLocked = false;
        Logger.Log("PlayerController: Jugador y cámara descongelados");
    }
    
    /// <summary>
    /// Obtiene la posición actual del jugador
    /// </summary>
    public Vector3 GetPlayerPosition() => _player.Position;
    
    /// <summary>
    /// Obtiene la posición global del jugador
    /// </summary>
    public Vector3 GetPlayerGlobalPosition() => _player.GlobalPosition;
    
    /// <summary>
    /// Establece la posición del jugador
    /// </summary>
    public void SetPlayerPosition(Vector3 position)
    {
        _player.Position = position;
    }
    
    /// <summary>
    /// Establece la posición global del jugador
    /// </summary>
    public void SetPlayerGlobalPosition(Vector3 position)
    {
        _player.GlobalPosition = position;
    }
    
    /// <summary>
    /// Obtiene los ángulos actuales de la cámara
    /// </summary>
    public Vector2 GetCameraAngles() => new(_cameraYaw, _cameraPitch);
    
    /// <summary>
    /// Establece los ángulos de la cámara
    /// </summary>
    public void SetCameraAngles(float yaw, float pitch)
    {
        _cameraYaw = yaw;
        _cameraPitch = Mathf.Clamp(pitch, -89f, 89f);
        ApplyCameraRotation();
    }
    
    /// <summary>
    /// Obtiene la dirección forward de la cámara en el plano XZ
    /// </summary>
    private Vector3 GetCameraForwardXZ()
    {
        var f = -_camera.GlobalTransform.Basis.Z;
        f.Y = 0;
        return f.Normalized();
    }
    
    /// <summary>
    /// Obtiene la dirección right de la cámara en el plano XZ
    /// </summary>
    private Vector3 GetCameraRightXZ()
    {
        var r = _camera.GlobalTransform.Basis.X;
        r.Y = 0;
        return r.Normalized();
    }
    
    /// <summary>
    /// Verifica si el jugador está congelado
    /// </summary>
    public bool IsFrozen() => _isFrozen;
    
    /// <summary>
    /// Verifica si la cámara está bloqueada
    /// </summary>
    public bool IsCameraLocked() => _isCameraLocked;
    
    /// <summary>
    /// Congela el movimiento del jugador y cámara (método público para GameWorld)
    /// </summary>
    public void FreezePlayer()
    {
        Freeze();
        Logger.Log("PlayerController: Jugador y cámara congelados");
    }

    /// <summary>
    /// Descongela el movimiento del jugador y cámara (método público para GameWorld)
    /// </summary>
    public void UnfreezePlayer()
    {
        Unfreeze();
        Logger.Log("PlayerController: Jugador y cámara descongelados");
    }
}
