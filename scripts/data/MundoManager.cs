using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using System.Text.Json;
using Wild.Utils;

namespace Wild.Data
{
    /// <summary>
    /// Gestiona la persistencia y el ciclo de vida de los mundos asociados a los personajes.
    /// </summary>
    public partial class MundoManager : Node
    {
        public static MundoManager Instance { get; private set; }

        private string _basePath = "user://worlds/";
        private Dictionary<string, Mundo> _mundosCargados = new Dictionary<string, Mundo>();
        private string _mundoActualId = "";

        public bool HayMundoActual => !string.IsNullOrEmpty(_mundoActualId) && _mundosCargados.ContainsKey(_mundoActualId);

        public override void _Ready()
        {
            if (Instance == null)
            {
                Instance = this;
                PersistenceService.EnsureDirectory(_basePath);
                CargarTodosLosMundos(); // Cargar al arrancar el Autoload

                // Optimización Global de Físicas
                Engine.PhysicsTicksPerSecond = 90;
                Logger.LogInfo("MundoManager: PhysicsTicksPerSecond ajustado a 90.");
            }
        }

        /// <summary>
        /// Carga todos los mundos disponibles globalmente.
        /// </summary>
        public void CargarTodosLosMundos()
        {
            _mundosCargados.Clear();
            _mundoActualId = "";

            string[] files = PersistenceService.GetFiles(_basePath, "*.json");
            foreach (string file in files)
            {
                Mundo mundo = PersistenceService.LoadJson<Mundo>(file);
                if (mundo != null)
                {
                    _mundosCargados[mundo.id] = mundo;
                }
            }
            Logger.LogInfo($"MundoManager: {_mundosCargados.Count} mundos cargados globalmente.");
        }

        public List<Mundo> GetListaMundos()
        {
            return _mundosCargados.Values.OrderByDescending(m => m.ultimo_acceso).ToList();
        }

        public Mundo CrearMundo(string nombre, string semilla)
        {
            Mundo nuevoMundo = new Mundo(nombre, semilla);
            _mundosCargados[nuevoMundo.id] = nuevoMundo;
            
            GarantizarEstructuraMundo(nuevoMundo.id, semilla);
            GuardarMundo(nuevoMundo);
            
            return nuevoMundo;
        }

        private void GarantizarEstructuraMundo(string mundoId, string semilla)
        {
            string worldDir = $"{_basePath}{mundoId}/";
            PersistenceService.EnsureDirectory(worldDir);
            PersistenceService.EnsureDirectory(Path.Combine(worldDir, "chunks"));
            PersistenceService.EnsureDirectory(Path.Combine(worldDir, "logs"));
            PersistenceService.EnsureDirectory(Path.Combine(worldDir, "objects"));

            // seed.txt (podría ser un simple archivo de texto, lo mantenemos directo por ahora o usamos un helper simple si existiera)
            string seedPath = ProjectSettings.GlobalizePath(Path.Combine(worldDir, "seed.txt"));
            File.WriteAllText(seedPath, semilla);
            
            Logger.LogInfo($"MundoManager: Estructura de carpetas para mundo {mundoId} garantizada.");
        }

        public void GuardarMundo(Mundo mundo)
        {
            string file = Path.Combine(_basePath, $"{mundo.id}.json");
            if (PersistenceService.SaveJson(file, mundo))
            {
                Logger.LogInfo($"MundoManager: Mundo '{mundo.nombre}' guardado.");
            }
        }

        public void SeleccionarMundo(string id)
        {
            if (_mundosCargados.ContainsKey(id))
            {
                _mundoActualId = id;
                _mundosCargados[id].ultimo_acceso = DateTime.Now;
                
                GarantizarEstructuraMundo(id, _mundosCargados[id].semilla);
                GuardarMundo(_mundosCargados[id]);
                
                Logger.LogInfo($"MundoManager: Mundo '{_mundosCargados[id].nombre}' seleccionado.");
            }
        }

        public Mundo ObtenerMundoActual()
        {
            if (HayMundoActual) return _mundosCargados[_mundoActualId];
            return null;
        }

        public bool EliminarMundo(string id)
        {
            if (!_mundosCargados.ContainsKey(id)) return false;

            // 1. Borrar JSON de metadatos
            PersistenceService.DeleteFile($"{_basePath}{id}.json");

            // 2. Borrar carpeta de contenido (chunks, logs, objects)
            PersistenceService.DeleteDirectory($"{_basePath}{id}/", true);

            _mundosCargados.Remove(id);
            if (_mundoActualId == id) _mundoActualId = "";

            Logger.LogInfo($"MundoManager: Mundo {id} eliminado completamente.");
            return true;
        }

        /// <summary>
        /// Obtiene la ruta de los datos persistentes de un personaje en un mundo específico.
        /// </summary>
        public string ObtenerRutaDatosJugadorEnMundo(string mundoId, string personajeId)
        {
            string dir = $"{_basePath}{mundoId}/personajes/";
            PersistenceService.EnsureDirectory(dir);
            return Path.Combine(dir, $"{personajeId}.json");
        }

        public string ObtenerRutaChunksActual()
        {
            if (!HayMundoActual) return "";
            string dir = $"{_basePath}{_mundoActualId}/chunks/";
            PersistenceService.EnsureDirectory(dir);
            return ProjectSettings.GlobalizePath(dir);
        }

        public string ObtenerRutaObjetosActual()
        {
            if (!HayMundoActual) return "";
            string dir = $"{_basePath}{_mundoActualId}/objects/";
            PersistenceService.EnsureDirectory(dir);
            return ProjectSettings.GlobalizePath(dir);
        }

        /// <summary>
        /// Guarda los datos del jugador en el mundo actual.
        /// </summary>
        public void GuardarDatosJugador(PlayerData data)
        {
            if (!HayMundoActual) return;

            string path = ObtenerRutaDatosJugadorEnMundo(_mundoActualId, data.id_personaje);
            PersistenceService.SaveJson(path, data);
        }

        /// <summary>
        /// Carga los datos del jugador en el mundo actual.
        /// </summary>
        public PlayerData CargarDatosJugador(string personajeId)
        {
            if (!HayMundoActual) return null;
            string path = ObtenerRutaDatosJugadorEnMundo(_mundoActualId, personajeId);
            return PersistenceService.LoadJson<PlayerData>(path);
        }
    }
}
