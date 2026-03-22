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
                GarantizarDirectorioBase();
                CargarTodosLosMundos(); // Cargar al arrancar el Autoload

                // Optimización Global de Físicas
                // 90 FPS permite mayor estabilidad en colisiones rápidas y con el nuevo Gait
                Engine.PhysicsTicksPerSecond = 90;
                Logger.LogInfo("MundoManager: PhysicsTicksPerSecond ajustado a 90.");
            }
        }

        public override void _ExitTree()
        {
            if (Instance == this)
            {
                // No limpiar instancia en Autoload a menos que sea el fin del juego
                // Instance = null; 
                Logger.LogInfo("MundoManager: Autoload persistente.");
            }
        }

        private void GarantizarDirectorioBase()
        {
            try
            {
                string path = ProjectSettings.GlobalizePath(_basePath);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    Logger.LogInfo("MundoManager: Directorio de mundos creado.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"MundoManager: Error al crear directorio base: {ex.Message}");
            }
        }

        /// <summary>
        /// Carga todos los mundos disponibles globalmente.
        /// </summary>
        public void CargarTodosLosMundos()
        {
            _mundosCargados.Clear();
            _mundoActualId = "";

            string pathAbsoluto = ProjectSettings.GlobalizePath(_basePath);

            if (!Directory.Exists(pathAbsoluto))
            {
                Directory.CreateDirectory(pathAbsoluto);
                return;
            }

            try
            {
                foreach (string file in Directory.GetFiles(pathAbsoluto, "*.json"))
                {
                    string json = File.ReadAllText(file);
                    Mundo mundo = JsonSerializer.Deserialize<Mundo>(json);
                    if (mundo != null)
                    {
                        _mundosCargados[mundo.id] = mundo;
                    }
                }
                Logger.LogInfo($"MundoManager: {_mundosCargados.Count} mundos cargados globalmente.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"MundoManager: Error cargando mundos: {ex.Message}");
            }
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
            try
            {
                string worldDir = ProjectSettings.GlobalizePath($"{_basePath}{mundoId}/");
                string chunksDir = Path.Combine(worldDir, "chunks");
                string logsDir = Path.Combine(worldDir, "logs");
                string objectsDir = Path.Combine(worldDir, "objects");

                if (!Directory.Exists(worldDir)) Directory.CreateDirectory(worldDir);
                if (!Directory.Exists(chunksDir)) Directory.CreateDirectory(chunksDir);
                if (!Directory.Exists(logsDir)) Directory.CreateDirectory(logsDir);
                if (!Directory.Exists(objectsDir)) Directory.CreateDirectory(objectsDir);

                // seed.txt
                File.WriteAllText(Path.Combine(worldDir, "seed.txt"), semilla);
                
                Logger.LogInfo($"MundoManager: Estructura de carpetas para mundo {mundoId} garantizada.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"MundoManager: Error al crear estructura de mundo: {ex.Message}");
            }
        }

        public void GuardarMundo(Mundo mundo)
        {
            try
            {
                string dir = ProjectSettings.GlobalizePath(_basePath);
                
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                string file = Path.Combine(dir, $"{mundo.id}.json");
                string json = JsonSerializer.Serialize(mundo, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(file, json);
                
                Logger.LogInfo($"MundoManager: Mundo '{mundo.nombre}' guardado.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"MundoManager: Error al guardar mundo: {ex.Message}");
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

            try
            {
                // 1. Borrar JSON de metadatos
                string jsonPath = ProjectSettings.GlobalizePath($"{_basePath}{id}.json");
                if (File.Exists(jsonPath))
                {
                    File.Delete(jsonPath);
                }

                // 2. Borrar carpeta de contenido (chunks, logs, objects)
                string worldDir = ProjectSettings.GlobalizePath($"{_basePath}{id}/");
                if (Directory.Exists(worldDir))
                {
                    Directory.Delete(worldDir, true);
                    Logger.LogInfo($"MundoManager: Carpeta de contenido del mundo {id} eliminada.");
                }

                _mundosCargados.Remove(id);
                if (_mundoActualId == id) _mundoActualId = "";

                Logger.LogInfo($"MundoManager: Mundo {id} eliminado completamente.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"MundoManager: Error al eliminar mundo: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtiene la ruta de los datos persistentes de un personaje en un mundo específico.
        /// </summary>
        public string ObtenerRutaDatosJugadorEnMundo(string mundoId, string personajeId)
        {
            string dir = ProjectSettings.GlobalizePath($"{_basePath}{mundoId}/personajes/");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"{personajeId}.json");
        }

        public string ObtenerRutaChunksActual()
        {
            if (!HayMundoActual) return "";
            string dir = ProjectSettings.GlobalizePath($"{_basePath}{_mundoActualId}/chunks/");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        public string ObtenerRutaObjetosActual()
        {
            if (!HayMundoActual) return "";
            string dir = ProjectSettings.GlobalizePath($"{_basePath}{_mundoActualId}/objects/");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Guarda los datos del jugador en el mundo actual.
        /// </summary>
        public void GuardarDatosJugador(PlayerData data)
        {
            if (!HayMundoActual) return;

            try
            {
                string path = ObtenerRutaDatosJugadorEnMundo(_mundoActualId, data.id_personaje);
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
                Logger.LogDebug($"MundoManager: Datos de jugador {data.id_personaje} guardados en mundo {_mundoActualId}.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"MundoManager: Error al guardar datos de jugador: {ex.Message}");
            }
        }

        /// <summary>
        /// Carga los datos del jugador en el mundo actual.
        /// </summary>
        public PlayerData CargarDatosJugador(string personajeId)
        {
            if (!HayMundoActual) return null;

            try
            {
                string path = ObtenerRutaDatosJugadorEnMundo(_mundoActualId, personajeId);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<PlayerData>(json);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"MundoManager: Error al cargar datos de jugador: {ex.Message}");
            }

            return null;
        }
    }
}
