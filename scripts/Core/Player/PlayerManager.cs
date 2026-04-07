using Godot;
using System;
using System.Collections.Generic;
using Wild.Data;
using Wild.Data.Inventory;
using Wild.Utils;
using Wild.Core;

namespace Wild.Core.Player
{
    /// <summary>
    /// Gestiona la creación, persistencia y estado del jugador en el mundo.
    /// </summary>
    public partial class PlayerManager : Node
    {
        public static PlayerManager Instance { get; private set; }

        [Export] public PackedScene FichaJugadorScene; // El prefab del jugador (JugadorController + Stats + Interaccion)
        
        public JugadorController JugadorActual => _jugadorActual;
        private JugadorController _jugadorActual;

        private float _autoSaveTimer = 0f;
        [Export] public float AutoSaveInterval = 5f;

        public override void _Ready()
        {
            if (Instance == null)
            {
                Instance = this;
                SetProcess(true);
            }
            else
            {
                QueueFree();
            }
        }

        public override void _Process(double delta)
        {
            if (_jugadorActual == null) return;

            _autoSaveTimer += (float)delta;
            if (_autoSaveTimer >= AutoSaveInterval)
            {
                _autoSaveTimer = 0f;
                GuardarEstadoJugador();
            }
        }

        /// <summary>
        /// Spawnea al jugador en el mundo, cargando sus datos si existen.
        /// </summary>
        public void SpawnJugador(string personajeId, Vector3 posicionDefecto)
        {
            if (FichaJugadorScene == null)
            {
                Logger.LogError("PlayerManager: FichaJugadorScene no asignada.");
                return;
            }

            _jugadorActual = FichaJugadorScene.Instantiate<JugadorController>();
            Logger.LogInfo($"PlayerManager: Instantiating player for ID: {personajeId}");
            AddChild(_jugadorActual);
            
            _jugadorActual.PersonajeId = personajeId;
            _jugadorActual.WorldId = MundoManager.Instance.ObtenerMundoActual()?.id ?? "";

            // Asignar modelo visual según tipo de personaje
            var personaje = PersonajeManager.Instance.ObtenerPersonaje(personajeId);
            if (personaje != null)
            {
                _jugadorActual.TipoPersonaje = personaje.tipo_personaje;

                var modelo = PersonajeManager.Instance.InstanciarPersonaje(personaje);
                if (modelo != null)
                {
                    _jugadorActual.SetVisualModel(modelo);
                }
            }

            // Intentar obtener datos precargados desde GameLoader
            PlayerData savedData = GameLoader.Instance?.CachedPlayerData;
            
            // Si no hay datos precargados o son de otro personaje, cargarlos ahora (fallback)
            if (savedData == null || savedData.id_personaje != personajeId)
            {
                Logger.LogDebug($"PlayerManager: No hay datos precargados para {personajeId}. Cargando disco...");
                savedData = MundoManager.Instance.CargarDatosJugador(personajeId);
            }
            else
            {
                Logger.LogInfo($"PlayerManager: Reutilizando datos precargados desde GameLoader para {personajeId}.");
                GameLoader.Instance.CachedPlayerData = null;
            }

            if (savedData != null)
            {
                _jugadorActual.GlobalPosition = savedData.GetPosition();
                _jugadorActual.Rotation = new Vector3(0, savedData.rot_y, 0);
                
                if (_jugadorActual.Stats != null)
                {
                    _jugadorActual.Stats.Salud = savedData.salud;
                    _jugadorActual.Stats.Hambre = savedData.hambre;
                    _jugadorActual.Stats.Sed = savedData.sed;
                    _jugadorActual.Stats.Energia = savedData.energia;
                }

                if (savedData.inventario != null && InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.LoadPersistenceData(savedData.inventario);
                }
                
                Logger.LogInfo($"PlayerManager: Jugador {personajeId} ({personaje?.tipo_personaje}) cargado en el mundo.");
            }
            else
            {
                _jugadorActual.GlobalPosition = posicionDefecto;
                Logger.LogInfo($"PlayerManager: Jugador {personajeId} ({personaje?.tipo_personaje}) spawneado por primera vez.");
            }
        }

        public void GuardarEstadoJugador()
        {
            if (_jugadorActual == null) return;

            PlayerData data = new PlayerData
            {
                id_personaje = _jugadorActual.PersonajeId,
                rot_y = _jugadorActual.Rotation.Y,
                ultima_actualizacion = DateTime.Now
            };
            data.SetPosition(_jugadorActual.GlobalPosition);

            if (_jugadorActual.Stats != null)
            {
                data.salud = _jugadorActual.Stats.Salud;
                data.hambre = _jugadorActual.Stats.Hambre;
                data.sed = _jugadorActual.Stats.Sed;
                data.energia = _jugadorActual.Stats.Energia;
            }

            // Guardar Inventario
            if (InventoryManager.Instance != null)
            {
                data.inventario = InventoryManager.Instance.GetPersistenceData();
            }

            MundoManager.Instance.GuardarDatosJugador(data);
            Logger.LogInfo("PlayerManager: Estado del jugador guardado.");
        }

        public override void _ExitTree()
        {
            if (Instance == this)
            {
                // Guardar una última vez antes de que el nodo se destruya al cerrar el juego
                GuardarEstadoJugador();

                Instance = null;
                Logger.LogInfo("PlayerManager: Instancia limpiada y estado guardado al salir.");
            }
        }
    }
}
