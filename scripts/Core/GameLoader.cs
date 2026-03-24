using Godot;
using Wild.Core.Terrain;
using Wild.Utils;
using Wild.Data;

namespace Wild.Core
{
    /// <summary>
    /// Autoload que gestiona el paso de recursos pesados entre escenas (Carga -> Juego).
    /// Permite que un TerrainManager o un Player pre-cargado sobrevivan al cambio de escena.
    /// </summary>
    public partial class GameLoader : Node
    {
        public static GameLoader Instance { get; private set; }

        // Caché de recursos precargados (Scenes, Materials, etc.)
        private readonly System.Collections.Generic.Dictionary<string, Resource> _resourceCache = new();

        // Caché de datos de persistencia
        public PlayerData CachedPlayerData { get; set; }

        // El terreno que estamos pre-generando
        public TerrainManager PreGeneratedTerrain { get; private set; }
        
        // Estado de la carga para la UI
        public float LoadingProgress { get; set; } = 0f;
        public string LoadingStatus { get; set; } = "";

        public override void _Ready()
        {
            if (Instance == null)
            {
                Instance = this;
                ProcessMode = ProcessModeEnum.Always;
                Logger.LogInfo("GameLoader: Singleton inicializado.");
            }
        }

        public void SetPreGeneratedTerrain(TerrainManager terrain)
        {
            // Si ya teníamos uno, lo limpiamos para evitar fugas
            if (PreGeneratedTerrain != null && PreGeneratedTerrain.GetParent() == this)
            {
                PreGeneratedTerrain.QueueFree();
            }

            PreGeneratedTerrain = terrain;
            
            if (terrain != null)
            {
                // Mantenemos el terreno vivo dentro del Autoload hasta que GameWorld lo reclame
                if (terrain.GetParent() != null) 
                {
                    terrain.IsTransferring = true;
                    terrain.GetParent().RemoveChild(terrain);
                    terrain.IsTransferring = false;
                }
                AddChild(terrain);
                // Restauramos el Singleton por si falló algo
                if (TerrainManager.Instance == null) {
                    var info = terrain.GetType().GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    info?.SetValue(null, terrain);
                }
                Logger.LogDebug("GameLoader: Terreno pre-generado almacenado.");
            }
        }

        public TerrainManager ClaimTerrain()
        {
            var terrain = PreGeneratedTerrain;
            if (terrain != null)
            {
                terrain.IsTransferring = true; // Protegemos el estado antes de moverlo
                RemoveChild(terrain);
                terrain.IsTransferring = false;
                
                PreGeneratedTerrain = null;
                Logger.LogDebug("GameLoader: Terreno reclamado por GameWorld.");
            }
            return terrain;
        }
        
        public void Cleanup()
        {
            if (PreGeneratedTerrain != null)
            {
                PreGeneratedTerrain.QueueFree();
                PreGeneratedTerrain = null;
            }
            _resourceCache.Clear();
            CachedPlayerData = null;
            LoadingProgress = 0f;
            LoadingStatus = "";
        }

        // --- Gestión de Recursos ---

        public void AddResource(string path, Resource res)
        {
            if (string.IsNullOrEmpty(path) || res == null) return;
            _resourceCache[path] = res;
            Logger.LogDebug($"GameLoader: Recurso cacheado: {path}");
        }

        public T GetResource<T>(string path) where T : Resource
        {
            if (_resourceCache.TryGetValue(path, out var res))
            {
                return res as T;
            }
            return null;
        }

        public bool HasResource(string path) => _resourceCache.ContainsKey(path);
    }
}
