using Godot;
using System.Collections.Generic;
using Wild.Utils;

namespace Wild.Core.Player
{
    /// <summary>
    /// Registro central de configuraciones de modelos de personajes.
    /// Centraliza la asociación entre IDs (tipoPersonaje) y sus clases de configuración.
    /// </summary>
    public static class ModeloRegistry
    {
        private static readonly Dictionary<string, IModeloConfig> _configs = new();

        static ModeloRegistry()
        {
            // Registrar modelos estándar
            Register(new Hombre1Config());
            Register(new Mujer1Config());
            
            Logger.LogInfo($"ModeloRegistry: {_configs.Count} modelos registrados.");
        }

        public static void Register(IModeloConfig config)
        {
            if (config == null) return;
            _configs[config.Id.ToLower()] = config;
        }

        public static IModeloConfig GetConfig(string id)
        {
            if (string.IsNullOrEmpty(id)) return GetDefault();
            
            string idLower = id.ToLower();
            if (_configs.TryGetValue(idLower, out var config))
            {
                return config;
            }

            Logger.LogWarning($"ModeloRegistry: Configuración no encontrada para ID '{id}'. Usando fallback.");
            return GetDefault();
        }

        public static IModeloConfig GetDefault()
        {
            return _configs["hombre1"];
        }

        public static IEnumerable<IModeloConfig> GetAllConfigs()
        {
            return _configs.Values;
        }
    }
}
