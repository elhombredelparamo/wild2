// -----------------------------------------------------------------------------
// Wild v2.0 - Sistema de Creación de Mundos
// -----------------------------------------------------------------------------
// 
// RELACIONADO CON:
// - contexto/biomas.md - Sistema de biomas 100% naturales
// - codigo/data/world-connection.pseudo - Diseño de conexión a mundos
// - codigo/core/juego.pseudo - Integración con bucle principal
// 
// DESCRIPCIÓN:
// Menú de creación de nuevos mundos con generación de seeds, nombre de mundo
// y selección de personaje. Conecta con sistema de generación procedural.
// 
// -----------------------------------------------------------------------------
using Godot;
using System;

namespace Wild.UI
{
    public partial class NewGameMenu : Control
    {
        private LineEdit _editSeed;
        private Button _buttonRandomSeed;
        private Label _labelCharacterInfo;
        private LineEdit _editWorldName;
        private Button _buttonCreate;
        private Button _buttonBack;

        private string _selectedCharacterId = "";
        private int _currentSeed = 0;

        public override void _Ready()
        {
            LogUI("NewGameMenu._Ready() - Inicializando menú de nueva partida");
            
            try
            {
                SetupControls();
                ConnectEvents();
                GenerateRandomSeed();
                LoadSelectedCharacter();
                
                LogUI("NewGameMenu._Ready() - Menú de nueva partida inicializado exitosamente");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("NewGameMenu", $"Error en _Ready(): {e.Message}");
                throw;
            }
        }

        private void SetupControls()
        {
            try
            {
                _editSeed = GetNode<LineEdit>("CenterContainer/Panel/MarginContainer/VBox/GridSeed/EditSeed");
                _buttonRandomSeed = GetNode<Button>("CenterContainer/Panel/MarginContainer/VBox/GridSeed/ButtonRandomSeed");
                _editWorldName = GetNode<LineEdit>("CenterContainer/Panel/MarginContainer/VBox/EditWorldName");
                _labelCharacterInfo = GetNode<Label>("CenterContainer/Panel/MarginContainer/VBox/LabelCharacterInfo");
                _buttonCreate = GetNode<Button>("CenterContainer/Panel/MarginContainer/VBox/Buttons/ButtonCreate");
                _buttonBack = GetNode<Button>("CenterContainer/Panel/MarginContainer/VBox/Buttons/ButtonBack");

                LogUI("NewGameMenu.SetupControls() - Controles configurados");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("NewGameMenu", $"Error en SetupControls(): {e.Message}");
            }
        }

        private void ConnectEvents()
        {
            try
            {
                if (_buttonRandomSeed != null)
                    _buttonRandomSeed.Pressed += GenerateRandomSeed;
                
                if (_buttonCreate != null)
                    _buttonCreate.Pressed += OnCreatePressed;
                
                if (_buttonBack != null)
                    _buttonBack.Pressed += OnBackPressed;

                if (_editSeed != null)
                    _editSeed.TextChanged += OnSeedTextChanged;

                LogUI("NewGameMenu.ConnectEvents() - Eventos conectados");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("NewGameMenu", $"Error en ConnectEvents(): {e.Message}");
            }
        }

        private void GenerateRandomSeed()
        {
            try
            {
                var random = new Random();
                _currentSeed = (int)GD.RandRange(100000, 999999);
                
                if (_editSeed != null)
                {
                    _editSeed.Text = _currentSeed.ToString();
                }

                LogUI($"NewGameMenu.GenerateRandomSeed() - Seed aleatoria generada: {_currentSeed}");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("NewGameMenu", $"Error en GenerateRandomSeed(): {e.Message}");
            }
        }

        private void LoadSelectedCharacter()
        {
            try
            {
                if (Wild.Data.PersonajeManager.Instance != null && Wild.Data.PersonajeManager.Instance.HayPersonajeActual)
                {
                    var character = Wild.Data.PersonajeManager.Instance.ObtenerPersonajeActual();
                    _selectedCharacterId = character.id;
                    
                    // Cargar los mundos para detección de duplicados
                    if (Wild.Data.MundoManager.Instance != null)
                    {
                        Wild.Data.MundoManager.Instance.CargarTodosLosMundos();
                    }

                    if (_labelCharacterInfo != null)
                    {
                        _labelCharacterInfo.Text = $"Personaje: {character.apodo}";
                    }
                    if (_buttonCreate != null)
                    {
                        _buttonCreate.Disabled = false;
                    }
                }
                else
                {
                    _selectedCharacterId = "";
                    if (_labelCharacterInfo != null)
                    {
                        _labelCharacterInfo.Text = "Ningún personaje seleccionado";
                    }
                    if (_buttonCreate != null)
                    {
                        _buttonCreate.Disabled = true;
                    }
                }

                LogUI($"NewGameMenu.LoadSelectedCharacter() - Personaje cargado: {_selectedCharacterId}");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("NewGameMenu", $"Error en LoadSelectedCharacter(): {e.Message}");
            }
        }

        private void OnSeedTextChanged(string newText)
        {
            try
            {
                if (string.IsNullOrEmpty(newText))
                {
                    _currentSeed = 0; // 0 significa aleatorio
                    LogUI("NewGameMenu.OnSeedTextChanged() - Seed vacía, usará aleatoria");
                }
                else if (int.TryParse(newText, out int seed))
                {
                    _currentSeed = seed;
                    LogUI($"NewGameMenu.OnSeedTextChanged() - Seed manual: {_currentSeed}");
                }
                else
                {
                    // Si no es válido, mantener el anterior
                    if (_editSeed != null)
                    {
                        _editSeed.Text = _currentSeed == 0 ? "" : _currentSeed.ToString();
                    }
                    LogUI("NewGameMenu.OnSeedTextChanged() - Seed inválida, restaurando valor anterior");
                }
            }
            catch (System.Exception e)
            {
                LogErrorSistema("NewGameMenu", $"Error en OnSeedTextChanged(): {e.Message}");
            }
        }

        
        private void OnCreatePressed()
        {
            try
            {
                if (string.IsNullOrEmpty(_selectedCharacterId))
                {
                    LogUI("NewGameMenu.OnCreatePressed() - No hay personaje seleccionado");
                    return;
                }

                var worldName = _editWorldName?.Text?.Trim();
                if (string.IsNullOrEmpty(worldName))
                {
                    worldName = $"Mundo_{System.DateTime.Now:yyyyMMdd_HHmmss}";
                    if (_editWorldName != null)
                    {
                        _editWorldName.Text = worldName;
                    }
                }

                var finalSeed = _currentSeed == 0 ? (int)GD.RandRange(1, int.MaxValue) : _currentSeed;

                LogUI($"NewGameMenu.OnCreatePressed() - Creando mundo: {worldName}, Seed: {finalSeed}, Personaje: {_selectedCharacterId}");

                // Crear mundo persistente
                if (Wild.Data.MundoManager.Instance != null)
                {
                    var nuevoMundo = Wild.Data.MundoManager.Instance.CrearMundo(worldName, finalSeed.ToString());
                    if (nuevoMundo != null)
                    {
                        Wild.Data.MundoManager.Instance.SeleccionarMundo(nuevoMundo.id);
                        LogUI($"NewGameMenu: Mundo {worldName} creado y seleccionado exitosamente");
                    }
                }
                
                // Navegar a la escena de carga
                LogUI("NewGameMenu: Navegando a loading_scene.tscn");
                var result = GetTree().ChangeSceneToFile("res://scenes/ui/loading_scene.tscn");
                LogUI($"NewGameMenu: ChangeSceneToFile resultado: {result}");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("NewGameMenu", $"Error en OnCreatePressed(): {e.Message}");
            }
        }

        private void OnBackPressed()
        {
            try
            {
                LogUI("NewGameMenu.OnBackPressed() - Volviendo al menú principal");
                var result = GetTree().ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
                LogUI($"NewGameMenu: ChangeSceneToFile resultado: {result}");
            }
            catch (System.Exception ex)
            {
                LogErrorSistema("NewGameMenu", $"ERROR en ChangeSceneToFile a main_menu.tscn: {ex.Message}");
            }
        }

        public void SetCharacter(string characterId)
        {
            try
            {
                _selectedCharacterId = characterId;
                LoadSelectedCharacter();
                
                LogUI($"NewGameMenu.SetCharacter() - Personaje establecido: {characterId}");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("NewGameMenu", $"Error en SetCharacter(): {e.Message}");
            }
        }

        // Funciones de logging
        private void LogUI(string mensaje)
        {
            // TODO: Implementar cuando tengamos el sistema de logging
            GD.Print($"[UI][NewGameMenu] {mensaje}");
        }

        private void LogErrorSistema(string sistema, string mensaje)
        {
            // TODO: Implementar cuando tengamos el sistema de logging
            GD.PrintErr($"[ERROR][{sistema}] {mensaje}");
        }
    }
}
