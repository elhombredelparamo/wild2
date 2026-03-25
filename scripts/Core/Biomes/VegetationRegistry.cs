using System.Collections.Generic;
using Wild.Utils;

/// <summary>
/// Gestiona la librería de vegetación de alto rendimiento.
/// Realiza un "Bake" inicial para que el acceso durante la generación sea O(1)
/// y no genere alocaciones de memoria adicionales.
/// </summary>
public static class VegetationRegistry
{
    private static VegetationEntry[][] _bakedEntries;
    private static bool _isBaked = false;

    /// <summary>
    /// Escanea todas las clases estáticas de vegetación y genera arrays planos por bioma.
    /// Debe llamarse una sola vez durante la escena de carga.
    /// </summary>
    public static void Bake()
    {
        if (_isBaked) return;

        Logger.LogInfo("VegetationRegistry: Iniciando proceso de Bake...");

        // 1. Obtener número de biomas
        var biomeIds = (BiomeId[])System.Enum.GetValues(typeof(BiomeId));
        int biomeCount = biomeIds.Length;
        _bakedEntries = new VegetationEntry[biomeCount][];

        // 2. Definir manualmente (de momento) los tipos de vegetación registrados
        // En el futuro esto podría automatizarse con Reflection si fuera necesario.
        var registry = new List<VegetationData>
        {
            RobleData.Data,
            PinoData.Data,
            SetaData.Data,
            HierbaData.Data
        };

        // 3. Bake por cada bioma (Garantizando longitud fija para estabilidad de índices)
        foreach (var biomeId in biomeIds)
        {
            var entriesForThisBiome = new VegetationEntry[registry.Count];

            for (int i = 0; i < registry.Count; i++)
            {
                var veg = registry[i];
                float chance = 0f;
                veg.SpawnChances.TryGetValue(biomeId, out chance);

                // Convertimos VegetationData en el VegetationEntry que entiende el motor
                // Siempre añadimos la entrada, incluso con probabilidad 0, para mantener el índice.
                entriesForThisBiome[i] = new VegetationEntry(
                    modelPath: veg.ModelPath,
                    spawnChance: chance,
                    minScale: veg.MinScale,
                    maxScale: veg.MaxScale,
                    itemId: veg.ItemId
                );
            }

            _bakedEntries[(int)biomeId] = entriesForThisBiome;
            Logger.LogDebug($"VegetationRegistry: Bioma {(int)biomeId} horneado con {registry.Count} ranuras de índice deterministas.");
        }

        _isBaked = true;
        Logger.LogInfo("VegetationRegistry: Bake completado exitosamente.");
    }

    /// <summary>
    /// Devuelve las entradas de vegetación para un bioma específico.
    /// Acceso ultra-rápido O(1) sin alocaciones.
    /// </summary>
    public static VegetationEntry[] GetEntriesForBiome(BiomeId biomeId)
    {
        if (!_isBaked)
        {
            Logger.LogWarning("VegetationRegistry: Acceso detectado antes del Bake. Ejecutando Bake de emergencia...");
            Bake();
        }

        return _bakedEntries[(int)biomeId];
    }
}
