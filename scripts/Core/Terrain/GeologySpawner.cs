using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wild.Core.Biomes;
using Wild.Core.Quality;
using Wild.Data;
using Wild.Utils;

namespace Wild.Core.Terrain
{
    public class GeologySpawner
    {
        private readonly int    _seed;
        private readonly BiomaManager _biomaManager;

        // Mezcladores de semilla diferentes a la vegetación para variar la posición
        private const long SeedMixX     = 49281731L;
        private const long SeedMixZ     = 93847562L;
        private const long SeedMixEntry = 28173945L;

        public GeologySpawner(int seed, BiomaManager biomaManager)
        {
            _seed         = seed;
            _biomaManager = biomaManager;
        }

        public async Task<List<GeologyInstance>> SpawnForChunk(ChunkRenderer renderer, ChunkData data, Vector2I coord)
        {
            int vertexCount = ChunkData.Resolution;
            int chunkSize   = ChunkData.Size;

            // De momento usamos el mismo multiplicador de densidad que la vegetación
            float densityMultiplier = QualityManager.Instance.Settings.VegetationQuality switch
            {
                QualityLevel.Ultra   => 1.0f,
                QualityLevel.High    => 0.75f,
                QualityLevel.Medium  => 0.5f,
                QualityLevel.Low     => 0.25f,
                QualityLevel.Toaster => 0.1f,
                _ => 0.0f
            };

            if (densityMultiplier <= 0f || !GodotObject.IsInstanceValid(renderer)) return new();

            // PRE-CARGA
            var biomaBase = _biomaManager.GetBiomaAt(coord.X * chunkSize, coord.Y * chunkSize);
            if (biomaBase.GeologyEntries != null && biomaBase.GeologyEntries.Length > 0)
            {
                var paths = new List<string>();
                foreach (var e in biomaBase.GeologyEntries) 
                    if (!string.IsNullOrEmpty(e.ModelPath)) paths.Add(e.ModelPath);
                
                VegetationLibrary.Preload(paths.ToArray());
            }
            
            var instances = await Task.Run(() =>
            {
                var list = new List<GeologyInstance>();
                var rng = new RandomNumberGenerator();

                int indexCounter = 0;
                for (int z = 0; z < vertexCount; z++)
                for (int x = 0; x < vertexCount; x++)
                {
                    float worldX = (coord.X * chunkSize) + x;
                    float worldZ = (coord.Y * chunkSize) + z;
                    float height = data.Altitudes[(z * vertexCount) + x];
                    
                    BiomaType localBioma = _biomaManager.GetBiomaAt(worldX, worldZ);
                    var entries = localBioma.GeologyEntries;

                    if (entries != null)
                    {
                        for (int i = 0; i < entries.Length; i++)
                        {
                            var entry = entries[i];
                            rng.Seed = (ulong)(_seed + (long)worldX * SeedMixX + (long)worldZ * SeedMixZ + (long)i * SeedMixEntry);
                            
                            if (rng.Randf() < entry.SpawnChance * densityMultiplier)
                            {
                                list.Add(new GeologyInstance {
                                    Index = indexCounter,
                                    LootTableId = entry.LootTableId,
                                    ModelPath = entry.ModelPath,
                                    Position = new Vector3(worldX, height, worldZ),
                                    RotationY = rng.Randf() * Mathf.Pi * 2.0f,
                                    Scale = rng.RandfRange(entry.MinScale, entry.MaxScale),
                                    HasCollision = entry.HasCollision
                                });
                            }
                            indexCounter++;
                        }
                    }
                }
                return list;
            });

            if (instances.Count > 0)
                Logger.LogInfo($"GEOLOGY: Spawned {instances.Count} rocks for chunk {coord} (Multiplier: {densityMultiplier})");
            else if (biomaBase.GeologyEntries?.Length > 0)
                Logger.LogDebug($"GEOLOGY: No rocks spawned for chunk {coord} despite entries existing.");

            return instances;
        }
    }
}
