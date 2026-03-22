// -----------------------------------------------------------------------------
// Wild v2.0 - Menú Principal del Juego
// -----------------------------------------------------------------------------
// 
// RELACIONADO CON:
// - contexto/menus-y-escenas.md - Sistema de menús y escenas
// - codigo/core/ui.pseudo - Diseño de interfaz de usuario
// - contexto/resumen.md - Arquitectura general del proyecto
// 
// DESCRIPCIÓN:
// Menú principal con navegación a creación de mundos, selección de personajes,
// opciones y salida del juego. Punto de entrada principal del usuario.
// 
// -----------------------------------------------------------------------------
using Godot;

namespace Wild.UI
{
    public partial class MainMenu : Control
    {
        private Button _buttonNewWorld;
        private Button _buttonConnectWorld;
        private Button _buttonCharacterSelect;
        private Button _buttonOptions;
        private Button _buttonQuit;
        private ColorRect _backgroundRect;

        public override void _Ready()
        {
            // Los gestores (PersonajeManager, MundoManager) ahora son Autoloads globales.
            // Se inicializan automáticamente al arrancar el juego.
            
            LogUI("MainMenu._Ready() - Inicializando menú principal");
            
            try
            {
                SetupBackground();
                SetupButtons();
                ConnectEvents();
                
                LogUI("MainMenu._Ready() - Menú principal inicializado exitosamente");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("MainMenu", $"Error en _Ready(): {e.Message}");
                throw;
            }
        }

        private void SetupBackground()
        {
            try
            {
                _backgroundRect = GetNode<ColorRect>("ColorRect");
                _backgroundRect.Position = GetViewport().GetVisibleRect().Size / 2 - _backgroundRect.Size / 2;
                
                // Intentar cargar imagen de fondo
                var texturePath = "res://assets/ui/fondomenuprincipal.png";
                if (ResourceLoader.Exists(texturePath))
                {
                    var texture = GD.Load<Texture2D>(texturePath);
                    if (texture != null)
                    {
                        // Crear TextureRect para reemplazar ColorRect
                        var textureRect = new TextureRect();
                        textureRect.Name = "BackgroundTexture";
                        textureRect.LayoutMode = 1;
                        textureRect.AnchorsPreset = 15; // Full Rect
                        textureRect.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                        textureRect.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
                        textureRect.Texture = texture;
                        textureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                        textureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
                        
                        // Reemplazar ColorRect con TextureRect
                        GetParent().RemoveChild(_backgroundRect);
                        _backgroundRect.QueueFree();
                        AddChild(textureRect);
                        
                        // Mover CenterContainer al frente
                        var centerContainer = GetNode<CenterContainer>("CenterContainer");
                        MoveChild(centerContainer, GetChildCount() - 1);
                        
                        LogUI("MainMenu.SetupBackground() - Imagen de fondo cargada exitosamente");
                    }
                }
                else
                {
                    LogUI("MainMenu.SetupBackground() - Imagen de fondo no encontrada, usando ColorRect");
                }
            }
            catch (System.Exception e)
            {
                LogErrorSistema("MainMenu", $"Error en SetupBackground(): {e.Message}");
            }
        }

        private void SetupButtons()
        {
            try
            {
                _buttonNewWorld = GetNode<Button>("CenterContainer/VBoxContainer/ButtonNewWorld");
                _buttonConnectWorld = GetNode<Button>("CenterContainer/VBoxContainer/ButtonConnectWorld");
                _buttonCharacterSelect = GetNode<Button>("CenterContainer/VBoxContainer/ButtonCharacterSelect");
                _buttonOptions = GetNode<Button>("CenterContainer/VBoxContainer/ButtonOptions");
                _buttonQuit = GetNode<Button>("CenterContainer/VBoxContainer/ButtonQuit");

                LogUI("MainMenu.SetupButtons() - Todos los botones configurados");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("MainMenu", $"Error en SetupButtons(): {e.Message}");
            }
        }

        private void ConnectEvents()
        {
            try
            {
                if (_buttonNewWorld != null)
                    _buttonNewWorld.Pressed += OnNewWorldPressed;
                
                if (_buttonConnectWorld != null)
                    _buttonConnectWorld.Pressed += OnConnectWorldPressed;
                
                if (_buttonCharacterSelect != null)
                    _buttonCharacterSelect.Pressed += OnCharacterSelectPressed;
                
                if (_buttonOptions != null)
                    _buttonOptions.Pressed += OnOptionsPressed;
                
                if (_buttonQuit != null)
                    _buttonQuit.Pressed += OnQuitPressed;

                LogUI("MainMenu.ConnectEvents() - Eventos de botones conectados");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("MainMenu", $"Error en ConnectEvents(): {e.Message}");
            }
        }

        private void OnNewWorldPressed()
        {
            LogUI("MainMenu.OnNewWorldPressed() - Botón Crear Mundo presionado");
            try
            {
                LogUI("MainMenu: Navegando a new_game_menu.tscn");
                var result = GetTree().ChangeSceneToFile("res://scenes/ui/new_game_menu.tscn");
                LogUI($"MainMenu: ChangeSceneToFile resultado: {result}");
            }
            catch (System.Exception ex)
            {
                LogErrorSistema("MainMenu", $"ERROR en ChangeSceneToFile a new_game_menu.tscn: {ex.Message}");
            }
        }

        private void OnConnectWorldPressed()
        {
            LogUI("MainMenu.OnConnectWorldPressed() - Botón Conectar a Mundo presionado");
            try
            {
                LogUI("MainMenu: Navegando a world_select_menu.tscn");
                var result = GetTree().ChangeSceneToFile("res://scenes/ui/world_select_menu.tscn");
                LogUI($"MainMenu: ChangeSceneToFile resultado: {result}");
            }
            catch (System.Exception ex)
            {
                LogErrorSistema("MainMenu", $"ERROR en ChangeSceneToFile a world_select_menu.tscn: {ex.Message}");
            }
        }

        private void OnCharacterSelectPressed()
        {
            LogUI("MainMenu.OnCharacterSelectPressed() - Botón Seleccionar Personaje presionado");
            try
            {
                LogUI("MainMenu: Navegando a character_select_menu.tscn");
                var result = GetTree().ChangeSceneToFile("res://scenes/ui/character_select_menu.tscn");
                LogUI($"MainMenu: ChangeSceneToFile resultado: {result}");
            }
            catch (System.Exception ex)
            {
                LogErrorSistema("MainMenu", $"ERROR en ChangeSceneToFile a character_select_menu.tscn: {ex.Message}");
            }
        }

        private void OnOptionsPressed()
        {
            LogUI("MainMenu.OnOptionsPressed() - Botón Opciones presionado");
            try
            {
                LogUI("MainMenu: Navegando a options_menu.tscn");
                var result = GetTree().ChangeSceneToFile("res://scenes/ui/options_menu.tscn");
                LogUI($"MainMenu: ChangeSceneToFile resultado: {result}");
            }
            catch (System.Exception ex)
            {
                LogErrorSistema("MainMenu", $"ERROR en ChangeSceneToFile a options_menu.tscn: {ex.Message}");
            }
        }

        private void OnQuitPressed()
        {
            LogUI("MainMenu.OnQuitPressed() - Botón Salir presionado");
            // TODO: Implementar salida del juego
            // GameManager.Instance.QuitGame();
            GetTree().Quit();
        }

        // Funciones de logging
        private void LogUI(string mensaje)
        {
            // Usar el sistema estático Logger con nivel INFO
            GD.Print($"[UI][MainMenu] {mensaje}");
            Wild.Utils.Logger.LogInfo($"[UI][MainMenu] {mensaje}");
        }

        private void LogErrorSistema(string sistema, string mensaje)
        {
            // Usar el sistema estático Logger con nivel ERROR
            GD.PrintErr($"[ERROR][{sistema}] {mensaje}");
            Wild.Utils.Logger.LogError($"[{sistema}] {mensaje}");
        }
    }
}
