// -----------------------------------------------------------------------------
// Wild v2.0 - Spawner de Vegetación por Chunk (Optimización Asíncrona)
// -----------------------------------------------------------------------------
using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wild.Core.Biomes;
using Wild.Core.Quality;
using Wild.Data;
using Wild.Utils;

namespace Wild.Core.Terrain
{
    public class VegetationSpawner
    {
        private readonly int    _seed;
        private readonly BiomaManager _biomaManager;

        private const long SeedMixX     = 73856093L;
        private const long SeedMixZ     = 19349663L;
        private const long SeedMixEntry = 83492791L;

        public VegetationSpawner(int seed, BiomaManager biomaManager)
        {
            _seed         = seed;
            _biomaManager = biomaManager;
        }

        /// <summary>
        /// Genera y renderiza toda la vegetación de un chunk (Árboles y Plantas).
        /// Retorna la lista de instancias para que el TerrainManager las use en colisiones dinámicas.
        /// </summary>
        public async Task<List<VegetationInstance>> SpawnForChunk(ChunkRenderer renderer, ChunkData data, Vector2I coord)
        {
            int vertexCount = ChunkData.Resolution;
            int chunkSize   = ChunkData.Size;

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

            // PRE-CARGA de modelos en el HILO PRINCIPAL
            var biomaBase = _biomaManager.GetBiomaAt(coord.X * chunkSize, coord.Y * chunkSize);
            if (biomaBase is BosqueBioma bBase) VegetationLibrary.Preload(bBase.TreeModels);
            if (biomaBase.VegetationEntries != null)
            {
                var paths = new List<string>();
                foreach (var e in biomaBase.VegetationEntries) if (!string.IsNullOrEmpty(e.ModelPath)) paths.Add(e.ModelPath);
                VegetationLibrary.PreloadPlants(paths.ToArray());
            }

            // ── 1. Cálculo de Posiciones (Segundo Plano) ─────────────────────
            var instances = await Task.Run(() =>
            {
                var list = new List<VegetationInstance>();
                var rng = new RandomNumberGenerator();

                int indexCounter = 0;
                for (int z = 0; z < vertexCount; z++)
                for (int x = 0; x < vertexCount; x++)
                {
                    float worldX = (coord.X * chunkSize) + x;
                    float worldZ = (coord.Y * chunkSize) + z;
                    float height = data.Altitudes[(z * vertexCount) + x];
                    
                    BiomaType localBioma = _biomaManager.GetBiomaAt(worldX, worldZ);
                    
                    // A) Bosque (Árboles)
                    if (localBioma is BosqueBioma bosque)
                    {
                        rng.Seed = (ulong)(_seed + (long)worldX * 31337 + (long)worldZ * 73);
                        if (rng.Randf() < bosque.VegetationDensity * densityMultiplier)
                        {
                            list.Add(new VegetationInstance {
                                Index = indexCounter,
                                ModelPath = bosque.TreeModels[rng.Randi() % (uint)bosque.TreeModels.Length],
                                Position = new Vector3(worldX, height, worldZ),
                                RotationY = rng.Randf() * Mathf.Pi * 2.0f,
                                Scale = rng.RandfRange(0.8f, 1.4f)
                            });
                        }
                    }
                    indexCounter++;

                    // B) Otras entradas (Plantas)
                    if (localBioma.VegetationEntries != null)
                    {
                        for (int i = 0; i < localBioma.VegetationEntries.Length; i++)
                        {
                            var entry = localBioma.VegetationEntries[i];
                            rng.Seed = (ulong)(_seed + (long)worldX * SeedMixX + (long)worldZ * SeedMixZ + (long)i * SeedMixEntry);
                            if (rng.Randf() < entry.SpawnChance * densityMultiplier)
                            {
                                list.Add(new VegetationInstance {
                                    Index = indexCounter,
                                    ItemId = entry.ItemId,
                                    ModelPath = entry.ModelPath,
                                    Position = new Vector3(worldX, height, worldZ),
                                    RotationY = rng.Randf() * Mathf.Pi * 2.0f,
                                    Scale = rng.RandfRange(entry.MinScale, entry.MaxScale)
                                });
                            }
                            indexCounter++;
                        }
                    }
                }
                return list;
            });

            return instances;
        }
    }
}
