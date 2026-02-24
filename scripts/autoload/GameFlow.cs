using Godot;
using Wild.Network;

namespace Wild;

/// <summary>
/// Autoload que centraliza el flujo del juego: menú principal, partida, servidor local.
/// Ver contexto/menus-y-servidor.txt para la arquitectura.
/// </summary>
public partial class GameFlow : Node
{
    public const string SceneMainMenu = "res://scenes/main_menu.tscn";
    public const string SceneNewGameMenu = "res://scenes/new_game_menu.tscn";
    public const string SceneOptionsMenu = "res://scenes/options_menu.tscn";
    public const string SceneGameWorld = "res://scenes/game_world.tscn";
    
    // Componentes de red
    private GameServer _gameServer = null!;
    private GameClient _gameClient = null!;
    
    public override void _Ready()
    {
        Logger.Log("GameFlow: Inicializando componentes de red");
        
        // Crear componentes de red
        _gameServer = new GameServer();
        _gameClient = new GameClient();
        
        // Añadir como hijos para que se gestionen automáticamente
        AddChild(_gameServer);
        AddChild(_gameClient);
        
        Logger.Log("GameFlow: Componentes de red inicializados");
    }

    /// <summary>Abre el menú de opciones (controles, gráficos).</summary>
    public void OpenOptions()
    {
        GetTree().ChangeSceneToFile(SceneOptionsMenu);
    }

    /// <summary>Abre el menú de creación de nueva partida (semilla, personaje, mundo).</summary>
    public void OpenNewGameMenu()
    {
        GetTree().ChangeSceneToFile(SceneNewGameMenu);
    }

    /// <summary>Inicia una partida nueva (arranca servidor, conecta cliente y cambia escena).</summary>
    public async void StartNewGame()
    {
        Logger.Log("GameFlow: StartNewGame() - iniciando servidor local");
        
        try
        {
            // 1. Iniciar servidor local
            var serverStarted = await _gameServer.StartServer();
            if (!serverStarted)
            {
                Logger.LogError("GameFlow: No se pudo iniciar el servidor local");
                return;
            }
            
            // 2. Conectar cliente al servidor
            var clientConnected = await _gameClient.ConnectToServer();
            if (!clientConnected)
            {
                Logger.LogError("GameFlow: No se pudo conectar el cliente al servidor");
                _gameServer.StopServer();
                return;
            }
            
            // 3. Verificar escena y cambiar
            if (!Godot.FileAccess.FileExists(SceneGameWorld))
            {
                Logger.LogError($"GameFlow: ERROR - La escena no existe: {SceneGameWorld}");
                return;
            }
            
            Logger.Log($"GameFlow: Servidor y cliente listos - cargando escena: {SceneGameWorld}");
            GetTree().ChangeSceneToFile(SceneGameWorld);
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"GameFlow: Excepción al iniciar partida: {ex.Message}");
            _gameServer.StopServer();
            _gameClient.Disconnect();
        }
    }

    /// <summary>Carga una partida (por ahora igual que nueva; persistencia pendiente).</summary>
    public void LoadGame(string savePath = "")
    {
        // TODO: cargar estado desde disco y luego cambiar escena con ese estado
        GetTree().ChangeSceneToFile(SceneGameWorld);
    }

    /// <summary>Vuelve al menú principal (cierra servidor local y desconecta cliente).</summary>
    public void ReturnToMainMenu()
    {
        Logger.Log("GameFlow: ReturnToMainMenu() - deteniendo servidor y cliente");
        
        // Detener componentes de red
        _gameClient.Disconnect();
        _gameServer.StopServer();
        
        // Mostrar cursor para el menú principal
        Input.MouseMode = Input.MouseModeEnum.Visible;
        
        GetTree().ChangeSceneToFile(SceneMainMenu);
    }

    /// <summary>Sale del juego.</summary>
    public void QuitGame()
    {
        GetTree().Quit();
    }
}
