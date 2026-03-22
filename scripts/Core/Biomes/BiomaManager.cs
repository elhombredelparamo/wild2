// -----------------------------------------------------------------------------
// Wild v2.0 - Gestor de Biomas
// -----------------------------------------------------------------------------
// Por ahora devuelve siempre OceanoBioma (hardcoded).
// Se generalizará en features 5.2-5.5 con lógica por altitud/coordenada.
// -----------------------------------------------------------------------------
using Wild.Utils;

namespace Wild.Core.Biomes
{
    public class BiomaManager
    {
        private readonly OceanoBioma   _oceano = new OceanoBioma();
        private readonly CostaBioma    _costa  = new CostaBioma();
        private readonly PraderaBioma  _pradera = new PraderaBioma();
        private readonly BosqueBioma   _bosque  = new BosqueBioma();
        private readonly MontanaBioma  _montana = new MontanaBioma();
        private readonly NoiseGenerator _noise;

        public BiomaManager(int seed)
        {
            _noise = new NoiseGenerator(seed);
        }

        public BiomaManager() : this(12345) { }

        /// <summary>
        /// Determina el bioma según la altitud y humedad en una coordenada mundial.
        /// </summary>
        public BiomaType GetBiomaAt(float worldX, float worldZ)
        {
            float height = _noise.GetHeight(worldX, worldZ);
            
            if (height < -20f)
                return _oceano;
            else if (height < 10f)
            {
                // Solo permitimos Costa si hay Océano cerca (sistema de vecindad)
                if (IsNearOcean(worldX, worldZ))
                    return _costa;
                
                // Si es bajo pero no hay mar cerca, lo tratamos como tierra firme normal
                float humidity = _noise.GetHumidity(worldX, worldZ);
                return (humidity > 0.5f) ? _bosque : _pradera;
            }
            else if (height >= 50f)
                return _montana;
            else
            {
                // Para tierra firme media, usamos la humedad para diferenciar Pradera de Bosque
                float humidity = _noise.GetHumidity(worldX, worldZ);
                return (humidity > 0.5f) ? _bosque : _pradera;
            }
        }

        /// <summary>
        /// Comprueba si hay bioma de océano en un radio cercano muestreando la altura.
        /// </summary>
        private bool IsNearOcean(float worldX, float worldZ)
        {
            float checkRadius = 15f; 
            // Muestreamos en 4 puntos cardinales para ver si alguno entra en rango oceánico
            if (_noise.GetHeight(worldX + checkRadius, worldZ) < -20f) return true;
            if (_noise.GetHeight(worldX - checkRadius, worldZ) < -20f) return true;
            if (_noise.GetHeight(worldX, worldZ + checkRadius) < -20f) return true;
            if (_noise.GetHeight(worldX, worldZ - checkRadius) < -20f) return true;
            
            return false;
        }

        /// <summary>
        /// Versión por chunk (para el material del MeshInstance3D).
        /// </summary>
        public BiomaType GetBioma(Godot.Vector2I chunkPos)
        {
            // Tomamos el centro del chunk para decidir el material predominante
            return GetBiomaAt(chunkPos.X * 10 + 5, chunkPos.Y * 10 + 5);
        }
    }
}
