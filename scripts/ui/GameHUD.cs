using Godot;
using Wild.Scripts.Terrain;
using Wild.Scripts.Player;

namespace Wild.Scripts.UI;

/// <summary>
/// Gestor del HUD del juego (Heads-Up Display)
/// Maneja la visualización de información en pantalla como coordenadas y estado del terreno
/// </summary>
public partial class GameHUD : Node
{
    private Label? _coordsLabel;
    private PlayerController? _playerController;
    private TerrainManager? _terrainManager;
    
    public GameHUD()
    {
        // Constructor vacío para Godot
    }
    
    /// <summary>
    /// Inicializa el HUD con las referencias necesarias
    /// </summary>
    /// <param name="coordsLabel">Label que mostrará las coordenadas</param>
    /// <param name="playerController">Controlador del jugador para obtener posición</param>
    /// <param name="terrainManager">Gestor de terreno para información del terreno</param>
    public void Initialize(Label coordsLabel, PlayerController playerController, TerrainManager? terrainManager = null)
    {
        _coordsLabel = coordsLabel;
        _playerController = playerController;
        _terrainManager = terrainManager;
        
        Logger.Log("GameHUD: Inicializado con referencias de UI y sistemas");
    }
    
    /// <summary>
    /// Actualiza el label de coordenadas con información actual del jugador y terreno
    /// </summary>
    public void UpdateCoordinatesDisplay()
    {
        if (_coordsLabel == null || _playerController == null)
        {
            Logger.LogWarning("GameHUD: No se puede actualizar - faltan referencias");
            return;
        }
        
        var pos = _playerController.GetPlayerGlobalPosition();
        var angles = _playerController.GetCameraAngles();
        
        // Añadir información del terreno si está disponible
        string terrainInfo = _terrainManager != null ? " | Terreno: PROCEDURAL" : " | Terreno: PLANO";
        
        _coordsLabel.Text = $"Pos: ({pos.X:F1}, {pos.Y:F1}, {pos.Z:F1}) | Cámara: pitch {angles.Y:F0}° yaw {angles.X:F0}°{terrainInfo}";
    }
    
    /// <summary>
    /// Verifica si el HUD está correctamente inicializado
    /// </summary>
    public bool IsInitialized()
    {
        return _coordsLabel != null && _playerController != null;
    }
    
    /// <summary>
    /// Limpia las referencias (útil para limpieza)
    /// </summary>
    public void Cleanup()
    {
        _coordsLabel = null;
        _playerController = null;
        _terrainManager = null;
        Logger.Log("GameHUD: Referencias limpiadas");
    }
}
