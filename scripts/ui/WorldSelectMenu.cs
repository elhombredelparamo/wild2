// -----------------------------------------------------------------------------
// Wild v2.0 - Sistema de Selección de Mundos
// -----------------------------------------------------------------------------
// 
// RELACIONADO CON:
// - contexto/biomas.md - Sistema de biomas 100% naturales
// - codigo/data/world-connection.pseudo - Diseño de conexión a mundos
// - codigo/core/personajes.pseudo - Diseño de gestión de personajes
// 
// DESCRIPCIÓN:
// Menú de selección de mundos con lista de mundos disponibles, gestión
// de conexión y eliminación. Integra sistema de personajes y seeds.
// 
// -----------------------------------------------------------------------------
using Godot;
using System;

namespace Wild.UI
{
    public partial class WorldSelectMenu : Control
    {
        private Label _labelCharacterInfo;
        private VBoxContainer _worldList;
        private Button _buttonBack;
        private Button _buttonDelete;
        private Button _buttonConnect;

        private string _selectedCharacterId = "";
        private string _selectedWorldId = "";
        private string _selectedWorldName = "";

        public override void _Ready()
        {
            LogUI("WorldSelectMenu._Ready() - Inicializando menú de selección de mundo");
            
            try
            {
                SetupControls();
                ConnectEvents();
                LoadSelectedCharacter();
                LoadWorldsList();
                
                LogUI("WorldSelectMenu._Ready() - Menú de selección de mundo inicializado exitosamente");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("WorldSelectMenu", $"Error en _Ready(): {e.Message}");
                throw;
            }
        }

        private void SetupControls()
        {
            try
            {
                _labelCharacterInfo = GetNode<Label>("CenterContainer/Panel/MarginContainer/VBox/LabelCharacterInfo");
                _worldList = GetNode<VBoxContainer>("CenterContainer/Panel/MarginContainer/VBox/ScrollContainer/WorldList");
                _buttonBack = GetNode<Button>("CenterContainer/Panel/MarginContainer/VBox/Buttons/ButtonBack");
                _buttonDelete = GetNode<Button>("CenterContainer/Panel/MarginContainer/VBox/Buttons/ButtonDelete");
                _buttonConnect = GetNode<Button>("CenterContainer/Panel/MarginContainer/VBox/Buttons/ButtonConnect");

                LogUI("WorldSelectMenu.SetupControls() - Controles configurados");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("WorldSelectMenu", $"Error en SetupControls(): {e.Message}");
            }
        }

        private void ConnectEvents()
        {
            try
            {
                if (_buttonBack != null)
                    _buttonBack.Pressed += OnBackPressed;
                
                if (_buttonDelete != null)
                    _buttonDelete.Pressed += OnDeletePressed;
                
                if (_buttonConnect != null)
                    _buttonConnect.Pressed += OnConnectPressed;

                LogUI("WorldSelectMenu.ConnectEvents() - Eventos conectados");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("WorldSelectMenu", $"Error en ConnectEvents(): {e.Message}");
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
                    
                    if (_labelCharacterInfo != null)
                    {
                        _labelCharacterInfo.Text = $"PERSONAJE: {character.apodo}";
                    }
                    
                    // Cargar mundos reales (ahora globales)
                    if (Wild.Data.MundoManager.Instance != null)
                    {
                        Wild.Data.MundoManager.Instance.CargarTodosLosMundos();
                    }
                    if (_buttonConnect != null)
                    {
                        _buttonConnect.Disabled = false;
                    }
                }
                else
                {
                    _selectedCharacterId = "";
                    if (_labelCharacterInfo != null)
                    {
                        _labelCharacterInfo.Text = "Ningún personaje seleccionado";
                    }
                    if (_buttonConnect != null)
                    {
                        _buttonConnect.Disabled = true;
                    }
                }

                LogUI($"WorldSelectMenu.LoadSelectedCharacter() - Personaje cargado: {_selectedCharacterId}");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("WorldSelectMenu", $"Error en LoadSelectedCharacter(): {e.Message}");
            }
        }

        private void LoadWorldsList()
        {
            try
        {
            // Limpiar lista actual
            if (_worldList != null)
            {
                foreach (Node child in _worldList.GetChildren())
                {
                    child.QueueFree();
                }
            }

            // Cargar lista de mundos reales
            var worlds = Wild.Data.MundoManager.Instance?.GetListaMundos() ?? new System.Collections.Generic.List<Wild.Data.Mundo>();
            
            foreach (var world in worlds)
            {
                CreateWorldButton(world);
            }

            LogUI($"WorldSelectMenu.LoadWorldsList() - {worlds.Count} mundos cargados");
        }
        catch (System.Exception e)
            {
                LogErrorSistema("WorldSelectMenu", $"Error en LoadWorldsList(): {e.Message}");
            }
        }

        private System.Collections.Generic.List<WorldInfo> GetAvailableWorlds()
        {
            var worlds = new System.Collections.Generic.List<WorldInfo>();
            
            // TODO: Escanear directorio de mundos y cargar información
            // Por ahora, crear mundos de ejemplo
            worlds.Add(new WorldInfo
            {
                Name = "Mundo Ejemplo 1",
                Seed = 123456789,
                LastAccess = DateTime.Now.AddDays(-1),
                PlayerCount = 2,
                Size = "15.2 MB"
            });
            
            worlds.Add(new WorldInfo
            {
                Name = "Mundo Aventura",
                Seed = 987654321,
                LastAccess = DateTime.Now.AddDays(-3),
                PlayerCount = 5,
                Size = "32.7 MB"
            });

            return worlds;
        }

        private void CreateWorldButton(Wild.Data.Mundo world)
        {
            try
            {
                var button = new Button();
                button.Text = $"{world.nombre}\nSeed: {world.semilla}\nÚltimo acceso: {world.ultimo_acceso:dd/MM/yyyy HH:mm}";
                button.CustomMinimumSize = new Vector2(600, 80);
                
                // Conectar evento
                button.Pressed += () => OnWorldSelected(world);
                
                // Añadir a la lista
                if (_worldList != null)
                {
                    _worldList.AddChild(button);
                }

                LogUI($"WorldSelectMenu.CreateWorldButton() - Botón creado para mundo: {world.nombre}");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("WorldSelectMenu", $"Error en CreateWorldButton(): {e.Message}");
            }
        }

        private void OnWorldSelected(Wild.Data.Mundo world)
        {
            try
            {
                _selectedWorldId = world.id;
                _selectedWorldName = world.nombre;
                
                // Actualizar estado de botones
                if (_buttonConnect != null)
                {
                    _buttonConnect.Disabled = false;
                }
                if (_buttonDelete != null)
                {
                    _buttonDelete.Disabled = false;
                }

                LogUI($"WorldSelectMenu.OnWorldSelected() - Mundo seleccionado: {world.nombre} (ID: {world.id})");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("WorldSelectMenu", $"Error en OnWorldSelected(): {e.Message}");
            }
        }

        private void OnBackPressed()
        {
            try
            {
                LogUI("WorldSelectMenu.OnBackPressed() - Volviendo al menú principal");
                var result = GetTree().ChangeSceneToFile("res://scenes/ui/main_menu.tscn");
                LogUI($"WorldSelectMenu: ChangeSceneToFile resultado: {result}");
            }
            catch (System.Exception ex)
            {
                LogErrorSistema("WorldSelectMenu", $"ERROR en ChangeSceneToFile a main_menu.tscn: {ex.Message}");
            }
        }

        private void OnDeletePressed()
        {
            try
            {
                if (string.IsNullOrEmpty(_selectedWorldName))
                {
                    LogUI("WorldSelectMenu.OnDeletePressed() - No hay mundo seleccionado");
                    return;
                }

                LogUI($"WorldSelectMenu.OnDeletePressed() - Eliminando mundo: {_selectedWorldName} (ID: {_selectedWorldId})");
                
                // Eliminación real
                if (Wild.Data.MundoManager.Instance != null)
                {
                    Wild.Data.MundoManager.Instance.EliminarMundo(_selectedWorldId);
                }
                
                // Recargar lista de mundos
                LoadWorldsList();
                
                // Limpiar selección
                _selectedWorldId = "";
                _selectedWorldName = "";
                if (_buttonConnect != null)
                {
                    _buttonConnect.Disabled = true;
                }
                if (_buttonDelete != null)
                {
                    _buttonDelete.Disabled = true;
                }
            }
            catch (System.Exception e)
            {
                LogErrorSistema("WorldSelectMenu", $"Error en OnDeletePressed(): {e.Message}");
            }
        }

        private void OnConnectPressed()
        {
            try
            {
                if (string.IsNullOrEmpty(_selectedWorldName) || string.IsNullOrEmpty(_selectedCharacterId))
                {
                    LogUI("WorldSelectMenu.OnConnectPressed() - Faltan mundo o personaje");
                    return;
                }

                LogUI($"WorldSelectMenu.OnConnectPressed() - Conectando al mundo: {_selectedWorldName} con personaje: {_selectedCharacterId}");
                
                // Seleccionar mundo en el manager
                if (Wild.Data.MundoManager.Instance != null)
                {
                    Wild.Data.MundoManager.Instance.SeleccionarMundo(_selectedWorldId);
                }
                
                // Navegar a la escena de carga
                LogUI("WorldSelectMenu: Navegando a loading_scene.tscn");
                var result = GetTree().ChangeSceneToFile("res://scenes/ui/loading_scene.tscn");
                LogUI($"WorldSelectMenu: ChangeSceneToFile resultado: {result}");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("WorldSelectMenu", $"Error en OnConnectPressed(): {e.Message}");
            }
        }

        public void SetCharacter(string characterId)
        {
            try
            {
                _selectedCharacterId = characterId;
                LoadSelectedCharacter();
                Show(); // Mostrar menú cuando hay personaje seleccionado
                
                LogUI($"WorldSelectMenu.SetCharacter() - Personaje establecido: {characterId}");
            }
            catch (System.Exception e)
            {
                LogErrorSistema("WorldSelectMenu", $"Error en SetCharacter(): {e.Message}");
            }
        }

        // Clase para información de mundos
        private class WorldInfo
        {
            public string Name { get; set; }
            public int Seed { get; set; }
            public DateTime LastAccess { get; set; }
            public int PlayerCount { get; set; }
            public string Size { get; set; }
        }

        // Funciones de logging
        private void LogUI(string mensaje)
        {
            // TODO: Implementar cuando tengamos el sistema de logging
            GD.Print($"[UI][WorldSelectMenu] {mensaje}");
        }

        private void LogErrorSistema(string sistema, string mensaje)
        {
            // TODO: Implementar cuando tengamos el sistema de logging
            GD.PrintErr($"[ERROR][{sistema}] {mensaje}");
        }
    }
}
