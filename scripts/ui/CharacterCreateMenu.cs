// -----------------------------------------------------------------------------
// Wild v2.0 - Sistema de Creación de Personajes
// -----------------------------------------------------------------------------
// 
// RELACIONADO CON:
// - scripts/data/Personaje.cs - Sistema de datos de personajes simplificado
// - scripts/data/PersonajeManager.cs - Gestor centralizado de personajes
// 
// DESCRIPCIÓN:
// Menú de creación de personajes con validación de nombres, generación de IDs
// únicos y persistencia local. Parte del sistema de interfaces básicas.
// 
// -----------------------------------------------------------------------------
using Godot;
using System;
using Wild.Data;
using Wild.Utils;
using Wild.Data.Inventory;

namespace Wild.UI
{
    public partial class CharacterCreateMenu : Control
    {
        private Label _labelFeedback;
        private LineEdit _editName;
        private Button _buttonCancel;
        private Button _buttonCreate;
        private OptionButton _optionGender;

        private string _returnToMenu = ""; // Menú al que volver después de crear

        public override void _Ready()
        {
            LogUI("CharacterCreateMenu._Ready() - Inicializando menú de creación de personaje");
            
            try
            {
                // Asegurar que el menú sea visible al cargar
                Visible = true;
                
                SetupControls();
                ConnectEvents();
                
                LogUI("CharacterCreateMenu._Ready() - Menú de creación de personaje inicializado exitosamente");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterCreateMenu", $"Error en _Ready(): {e.Message}");
                throw;
            }
        }

        private void SetupControls()
        {
            try
            {
                _labelFeedback = GetNode<Label>("CenterContainer/Panel/MarginContainer/VBox/LabelFeedback");
                _editName = GetNode<LineEdit>("CenterContainer/Panel/MarginContainer/VBox/EditName");
                _optionGender = GetNode<OptionButton>("CenterContainer/Panel/MarginContainer/VBox/OptionGender");
                _buttonCancel = GetNode<Button>("CenterContainer/Panel/MarginContainer/VBox/Buttons/ButtonCancel");
                _buttonCreate = GetNode<Button>("CenterContainer/Panel/MarginContainer/VBox/Buttons/ButtonCreate");

                // Configurar opciones de género
                if (_optionGender != null)
                {
                    _optionGender.Clear();
                    _optionGender.AddItem("Hombre", 0);
                    _optionGender.AddItem("Mujer", 1);
                    _optionGender.Selected = 0;
                }

                LogUI("CharacterCreateMenu.SetupControls() - Controles configurados");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterCreateMenu", $"Error en SetupControls(): {e.Message}");
            }
        }

        private void ConnectEvents()
        {
            try
            {
                if (_editName != null)
                    _editName.TextChanged += OnNameTextChanged;

                if (_optionGender != null)
                    _optionGender.ItemSelected += OnGenderChanged;
                
                if (_buttonCancel != null)
                    _buttonCancel.Pressed += OnCancelPressed;
                
                if (_buttonCreate != null)
                    _buttonCreate.Pressed += OnCreatePressed;

                LogUI("CharacterCreateMenu.ConnectEvents() - Eventos conectados");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterCreateMenu", $"Error en ConnectEvents(): {e.Message}");
            }
        }

        private void OnNameTextChanged(string newText)
        {
            var (isValid, feedback) = ValidationUtils.ValidateName(newText);
            
            if (_buttonCreate != null)
                _buttonCreate.Disabled = !isValid;
            
            UpdateFeedback(newText, isValid, feedback);
            LogUI($"CharacterCreateMenu.OnNameTextChanged() - Nombre: '{newText}', Válido: {isValid}");
        }

        private void OnGenderChanged(long index)
        {
            try
            {
                string gender = index == 0 ? "hombre" : "mujer";
                LogUI($"CharacterCreateMenu.OnGenderChanged() - Género seleccionado: {gender}");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterCreateMenu", $"Error en OnGenderChanged(): {e.Message}");
            }
        }

        // Eliminado ValidateCharacterName local ya que se usa ValidationUtils.ValidateName

        private void UpdateFeedback(string name, bool isValid, string feedback)
        {
            if (_labelFeedback == null) return;
            
            if (string.IsNullOrEmpty(name))
            {
                _labelFeedback.Text = "";
            }
            else
            {
                _labelFeedback.Text = feedback;
                _labelFeedback.Modulate = isValid ? Colors.Green : Colors.Red;
            }
        }

        private void OnCreatePressed()
        {
            try
            {
                var characterName = _editName?.Text?.Trim();
                var (isValid, _) = ValidationUtils.ValidateName(characterName);
                
                if (!isValid)
                {
                    LogUI("CharacterCreateMenu.OnCreatePressed() - Nombre inválido");
                    return;
                }

                LogUI($"CharacterCreateMenu.OnCreatePressed() - Creando personaje: {characterName}");
                
                // Crear personaje usando PersonajeManager
                if (PersonajeManager.Instance != null)
                {
                    string gender = _optionGender?.Selected == 1 ? "mujer" : "hombre";
                    var resultado = PersonajeManager.Instance.CrearPersonaje(characterName, gender);
                    
                    if (resultado.exito)
                    {
                        // Seleccionar automáticamente el personaje creado
                        PersonajeManager.Instance.SeleccionarPersonaje(resultado.personaje.id);
                        LogUI($"CharacterCreateMenu.OnCreatePressed() - Personaje {characterName} creado exitosamente");
                        
                        // Volver al menú anterior
                        ReturnToPreviousMenu();
                    }
                    else
                    {
                        // Mostrar error
                        if (_labelFeedback != null)
                        {
                            _labelFeedback.Text = resultado.error;
                            _labelFeedback.Modulate = Colors.Red;
                        }
                        LogUI($"CharacterCreateMenu.OnCreatePressed() - Error creando personaje: {resultado.error}");
                    }
                }
                else
                {
                    LogErrorSistema("CharacterCreateMenu", "PersonajeManager no disponible");
                    return;
                }
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterCreateMenu", $"Error en OnCreatePressed(): {e.Message}");
            }
        }

        private void OnCancelPressed()
        {
            try
            {
                LogUI("CharacterCreateMenu.OnCancelPressed() - Cancelando creación de personaje");
                
                ReturnToPreviousMenu();
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterCreateMenu", $"Error en OnCancelPressed(): {e.Message}");
            }
        }

        private string CreateCharacter(string name)
        {
            try
            {
                // Método obsoleto - ahora se usa PersonajeManager
                LogUI("CharacterCreateMenu.CreateCharacter() - Método obsoleto, usar PersonajeManager");
                return "";
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterCreateMenu", $"Error en CreateCharacter(): {e.Message}");
                return "";
            }
        }

        private void ReturnToPreviousMenu()
        {
            try
            {
                // Limpiar formulario
                if (_editName != null)
                {
                    _editName.Text = "";
                }
                if (_labelFeedback != null)
                {
                    _labelFeedback.Text = "";
                }
                if (_buttonCreate != null)
                {
                    _buttonCreate.Disabled = true;
                }
                
                // Volver al menú de selección de personajes
                LogUI("CharacterCreateMenu.ReturnToPreviousMenu() - Navegando al menú de selección de personajes");
                var result = GetTree().ChangeSceneToFile("res://scenes/ui/character_select_menu.tscn");
                LogUI($"CharacterCreateMenu: ChangeSceneToFile resultado: {result}");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterCreateMenu", $"Error en ReturnToPreviousMenu(): {e.Message}");
            }
        }

        public void OpenForNewGame()
        {
            try
            {
                _returnToMenu = "new_game";
                Show();
                LogUI("CharacterCreateMenu.OpenForNewGame() - Abierto para nuevo juego");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterCreateMenu", $"Error en OpenForNewGame(): {e.Message}");
            }
        }

        public void OpenForCharacterSelect()
        {
            try
            {
                _returnToMenu = "character_select";
                Show();
                LogUI("CharacterCreateMenu.OpenForCharacterSelect() - Abierto para selección de personaje");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("CharacterCreateMenu", $"Error en OpenForCharacterSelect(): {e.Message}");
            }
        }

        // Funciones de logging
        private void LogUI(string mensaje)
        {
            Wild.Utils.Logger.LogInfo($"[UI][CharacterCreateMenu] {mensaje}");
        }

        private void LogErrorSistema(string sistema, string mensaje)
        {
            Wild.Utils.Logger.LogError($"[ERROR][{sistema}] {mensaje}");
        }
    }
}
