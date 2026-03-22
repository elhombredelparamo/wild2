// -----------------------------------------------------------------------------
// Wild v2.0 - Sistema de Selección de Personajes
// -----------------------------------------------------------------------------
// 
// RELACIONADO CON:
// - scripts/data/Personaje.cs - Sistema de datos de personajes simplificado
// - scripts/data/PersonajeManager.cs - Gestor centralizado de personajes
// 
// DESCRIPCIÓN:
// Menú de selección de personajes con lista de personajes disponibles,
// navegación a creación y gestión de selección de personaje activo.
// 
// -----------------------------------------------------------------------------
using Godot;
using System;
using System.Linq;
using Wild.Data;

namespace Wild.UI
{
    public partial class CharacterSelectMenu : Control
    {
        private VBoxContainer _characterList;
        private Button _buttonCreateNew;
        private Button _buttonSelect;
        private Button _buttonDelete;
        private Button _buttonBack;
        private Label _labelSelectedCharacter;

        private string _selectedCharacterId = "";

        public override void _Ready()
        {
            LogUI("CharacterSelectMenu._Ready() - Inicializando menú de selección de personaje");
            
            try
            {
                SetupControls();
                ConnectEvents();
                LoadCharactersList();
                UpdateSelectionLabel();
                
                LogUI("CharacterSelectMenu._Ready() - Menú de selección de personaje inicializado exitosamente");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterSelectMenu", $"Error en _Ready(): {e.Message}");
                throw;
            }
        }

        private void SetupControls()
        {
            try
            {
                _characterList = GetNode<VBoxContainer>("CenterContainer/Panel/MarginContainer/VBox/ScrollContainer/CharacterList");
                _buttonCreateNew = GetNode<Button>("CenterContainer/Panel/MarginContainer/VBox/Buttons/ButtonCreateNew");
                _buttonSelect = GetNode<Button>("CenterContainer/Panel/MarginContainer/VBox/Buttons/ButtonSelect");
                _buttonDelete = GetNode<Button>("CenterContainer/Panel/MarginContainer/VBox/Buttons/ButtonDelete");
                _buttonBack = GetNode<Button>("CenterContainer/Panel/MarginContainer/VBox/Buttons/ButtonBack");
                _labelSelectedCharacter = GetNode<Label>("CenterContainer/Panel/MarginContainer/VBox/LabelSelectedCharacter");

                LogUI("CharacterSelectMenu.SetupControls() - Controles configurados");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterSelectMenu", $"Error en SetupControls(): {e.Message}");
            }
        }

        private void ConnectEvents()
        {
            try
            {
                if (_buttonCreateNew != null)
                    _buttonCreateNew.Pressed += OnCreateNewPressed;
                
                if (_buttonSelect != null)
                    _buttonSelect.Pressed += OnSelectPressed;
                
                if (_buttonDelete != null)
                    _buttonDelete.Pressed += OnDeletePressed;
                
                if (_buttonBack != null)
                    _buttonBack.Pressed += OnBackPressed;

                LogUI("CharacterSelectMenu.ConnectEvents() - Eventos conectados");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterSelectMenu", $"Error en ConnectEvents(): {e.Message}");
            }
        }

        private void LoadCharactersList()
        {
            try
            {
                // Limpiar lista actual
                if (_characterList != null)
                {
                    foreach (Node child in _characterList.GetChildren())
                    {
                        child.QueueFree();
                    }
                }

                // Cargar personajes desde PersonajeManager
                if (PersonajeManager.Instance != null)
                {
                    var characters = PersonajeManager.Instance.ObtenerTodosPersonajes();
                    
                    foreach (var character in characters)
                    {
                        CreateCharacterButton(character);
                    }
                }
                else
                {
                    LogErrorSistema("CharacterSelectMenu", "PersonajeManager no inicializado");
                    return;
                }

                LogUI($"CharacterSelectMenu.LoadCharactersList() - {PersonajeManager.Instance.CantidadPersonajes} personajes cargados");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterSelectMenu", $"Error en LoadCharactersList(): {e.Message}");
            }
        }

        
        private void CreateCharacterButton(Personaje character)
        {
            try
            {
                var button = new Button();
                button.Text = $"{character.apodo}\n{character.genero}\nCreado: {character.fecha_creacion:dd/MM/yyyy}";
                button.CustomMinimumSize = new Vector2(500, 80);
                
                // Conectar evento
                button.Pressed += () => OnCharacterSelected(character);
                
                // Añadir a la lista
                if (_characterList != null)
                {
                    _characterList.AddChild(button);
                }

                LogUI($"CharacterSelectMenu.CreateCharacterButton() - Botón creado para personaje: {character.apodo}");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterSelectMenu", $"Error en CreateCharacterButton(): {e.Message}");
            }
        }

        private void OnCharacterSelected(Personaje character)
        {
            try
            {
                _selectedCharacterId = character.id;
                
                // Actualizar estado de botones
                if (_buttonSelect != null)
                {
                    _buttonSelect.Disabled = false;
                }
                if (_buttonDelete != null)
                {
                    _buttonDelete.Disabled = PersonajeManager.Instance.CantidadPersonajes <= 1;
                }

                if (_labelSelectedCharacter != null)
                {
                    _labelSelectedCharacter.Text = $"Seleccionado: {character.apodo}";
                }

                LogUI($"CharacterSelectMenu.OnCharacterSelected() - Personaje seleccionado: {character.apodo}");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterSelectMenu", $"Error en OnCharacterSelected(): {e.Message}");
            }
        }

        private void OnCreateNewPressed()
        {
            try
            {
                LogUI("CharacterSelectMenu.OnCreateNewPressed() - Abriendo creación de personaje");
                
                // Navegar al menú de creación de personaje
                var result = GetTree().ChangeSceneToFile("res://scenes/ui/character_create_menu.tscn");
                LogUI($"CharacterSelectMenu: ChangeSceneToFile resultado: {result}");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterSelectMenu", $"Error en OnCreateNewPressed(): {e.Message}");
            }
        }

        private void OnSelectPressed()
        {
            try
            {
                if (string.IsNullOrEmpty(_selectedCharacterId))
                {
                    LogUI("CharacterSelectMenu.OnSelectPressed() - No hay personaje seleccionado");
                    return;
                }

                LogUI($"CharacterSelectMenu.OnSelectPressed() - Personaje seleccionado: {_selectedCharacterId}");
                
                // Seleccionar personaje en PersonajeManager
                if (PersonajeManager.Instance != null)
                {
                    PersonajeManager.Instance.SeleccionarPersonaje(_selectedCharacterId);
                    LogUI($"CharacterSelectMenu.OnSelectPressed() - Personaje {_selectedCharacterId} seleccionado en PersonajeManager");
                }
                else
                {
                    LogErrorSistema("CharacterSelectMenu", "PersonajeManager no disponible");
                    return;
                }
                
                // Volver al menú principal
                LogUI("CharacterSelectMenu.OnSelectPressed() - Navegando al menú principal");
                var result = GetTree().ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
                LogUI($"CharacterSelectMenu: ChangeSceneToFile resultado: {result}");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterSelectMenu", $"Error en OnSelectPressed(): {e.Message}");
            }
        }

        private void OnDeletePressed()
        {
            try
            {
                if (string.IsNullOrEmpty(_selectedCharacterId))
                {
                    LogUI("CharacterSelectMenu.OnDeletePressed() - No hay personaje seleccionado");
                    return;
                }

                LogUI($"CharacterSelectMenu.OnDeletePressed() - Eliminando personaje: {_selectedCharacterId}");
                
                // Eliminar personaje usando PersonajeManager
                if (PersonajeManager.Instance != null)
                {
                    bool eliminado = PersonajeManager.Instance.EliminarPersonaje(_selectedCharacterId);
                    if (eliminado)
                    {
                        LogUI($"CharacterSelectMenu.OnDeletePressed() - Personaje {_selectedCharacterId} eliminado exitosamente");
                    }
                    else
                    {
                        LogUI($"CharacterSelectMenu.OnDeletePressed() - No se pudo eliminar personaje {_selectedCharacterId}");
                    }
                }
                else
                {
                    LogErrorSistema("CharacterSelectMenu", "PersonajeManager no disponible");
                }
                
                // Recargar lista de personajes
                LoadCharactersList();
                
                // Limpiar selección
                _selectedCharacterId = "";
                if (_buttonSelect != null)
                {
                    _buttonSelect.Disabled = true;
                }
                if (_buttonDelete != null)
                {
                    _buttonDelete.Disabled = true;
                }
                UpdateSelectionLabel();
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterSelectMenu", $"Error en OnDeletePressed(): {e.Message}");
            }
        }

        private void OnBackPressed()
        {
            try
            {
                LogUI("CharacterSelectMenu.OnBackPressed() - Volviendo al menú principal");
                var result = GetTree().ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
                LogUI($"CharacterSelectMenu: ChangeSceneToFile resultado: {result}");
            }
            catch (System.Exception ex)
            {
                LogErrorSistema("CharacterSelectMenu", $"ERROR en ChangeSceneToFile a main_menu.tscn: {ex.Message}");
            }
        }

        private void UpdateSelectionLabel()
        {
            if (_labelSelectedCharacter == null) return;

            if (PersonajeManager.Instance != null && PersonajeManager.Instance.HayPersonajeActual)
            {
                var current = PersonajeManager.Instance.ObtenerPersonajeActual();
                _labelSelectedCharacter.Text = $"Seleccionado: {current.apodo}";
                _selectedCharacterId = current.id;
                
                // Habilitar botones si ya hay selección cargada
                if (_buttonSelect != null) _buttonSelect.Disabled = false;
            }
            else
            {
                _labelSelectedCharacter.Text = "Seleccionado: Ninguno";
                if (_buttonSelect != null) _buttonSelect.Disabled = true;
            }
        }

        // Funciones de logging
        private void LogUI(string mensaje)
        {
            Wild.Utils.Logger.LogInfo($"[UI][CharacterSelectMenu] {mensaje}");
        }

        private void LogErrorSistema(string sistema, string mensaje)
        {
            Wild.Utils.Logger.LogError($"[ERROR][{sistema}] {mensaje}");
        }
    }
}
