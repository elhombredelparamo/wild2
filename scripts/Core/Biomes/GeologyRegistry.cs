using System.Collections.Generic;
using Wild.Core.Biomes;
using Wild.Utils;

/// <summary>
/// Gestiona la librería de geología de alto rendimiento.
/// </summary>
public static class GeologyRegistry
{
    private static GeologyEntry[][] _bakedEntries;
    private static Dictionary<string, GeologyData> _lootTables = new Dictionary<string, GeologyData>();
    private static bool _isBaked = false;

    public static void Bake()
    {
        if (_isBaked) return;

        Logger.LogInfo("GeologyRegistry: Iniciando proceso de Bake...");

        var biomeIds = (BiomeId[])System.Enum.GetValues(typeof(BiomeId));
        int biomeCount = biomeIds.Length;
        _bakedEntries = new GeologyEntry[biomeCount][];
        _lootTables.Clear();

        var registry = new List<GeologyData>
        {
            RocaData.Get()
        };

        foreach (var geo in registry)
        {
            if (!string.IsNullOrEmpty(geo.LootTableId))
                _lootTables[geo.LootTableId] = geo;
        }

        foreach (var biomeId in biomeIds)
        {
            var entriesForThisBiome = new GeologyEntry[registry.Count];

            for (int i = 0; i < registry.Count; i++)
            {
                var geo = registry[i];
                float chance = 0f;
                geo.SpawnChances.TryGetValue(biomeId, out chance);

                entriesForThisBiome[i] = new GeologyEntry(
                    modelPath: geo.ModelPath,
                    spawnChance: chance,
                    minScale: geo.MinScale,
                    maxScale: geo.MaxScale,
                    lootTableId: geo.LootTableId,
                    hasCollision: geo.HasCollision
                );
            }

            _bakedEntries[(int)biomeId] = entriesForThisBiome;
            Logger.LogDebug($"GeologyRegistry: Bioma {(int)biomeId} horneado con {registry.Count} ranuras.");
        }

        _isBaked = true;
        Logger.LogInfo("GeologyRegistry: Bake completado exitosamente.");
    }

    public static GeologyEntry[] GetEntriesForBiome(BiomeId biomeId)
    {
        if (!_isBaked) Bake();
        return _bakedEntries[(int)biomeId];
    }

    public static List<LootEntry> GetLootTable(string lootTableId)
    {
        if (_lootTables.TryGetValue(lootTableId, out var data))
            return data.LootTable;
        return null;
    }
}
